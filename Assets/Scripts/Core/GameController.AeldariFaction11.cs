using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Complete Edition 11 Aeldari faction-pack integration.
/// This partial lives inside GameController so faction rules can use the real
/// core movement/attack/transport/objective operations without bridges.
/// </summary>
public partial class GameController
{
    private sealed class Aeldari11DeferredReaction
    {
        public AeldariGameController Faction;
        public GameEventContext Context;
        public List<AeldariStratagem11> Rules;
        public SquadController Unit;
    }

    private readonly Queue<Aeldari11DeferredReaction> aeldari11DeferredReactions =
        new Queue<Aeldari11DeferredReaction>();
    private readonly Queue<System.Action> aeldari11DeferredChoices =
        new Queue<System.Action>();
    private bool aeldari11OpeningReaction;
    private readonly List<int> aeldari11ManualFateEntry = new List<int>();

    public void Aeldari11SynchronizeFaction(AeldariGameController faction)
    {
        if (faction == null) return;
        AeldariFactionPack11Runtime.SynchronizePersistent(faction);
    }

    public void Aeldari11OnBattleStarted(AeldariGameController faction)
    {
        if (faction == null) return;
        AeldariFactionPack11Runtime.SynchronizePersistent(faction);

        // Rules whose choice happens after deployment are surfaced before the
        // first turn. The existing board placement tools remain available.
        foreach (SquadController unit in faction.ArmyUnits)
        {
            if (unit == null) continue;
            if (AeldariFactionPack11.UnitHasEnhancement(unit, "Shedskin Raiment"))
            {
                QueueTraditionalRuleAlert(
                    "SHEDSKIN RAIMENT",
                    "After both players have deployed, select up to three HARLEQUINS units from your army and redeploy them. Warboard has retained this enhancement and the board remains editable for the redeployment.",
                    0);
            }
        }
    }

    public void Aeldari11OnBattleRoundStarted(AeldariGameController faction)
    {
        if (faction == null) return;

        int bonus = 0;
        if (faction.HasDetachment(AeldariDetachment.Warhost))
            bonus += 1;

        foreach (SquadController unit in faction.ArmyUnits)
        {
            if (unit == null || !unit.IsAlive) continue;
            if (!AeldariFactionPack11.UnitHasEnhancement(unit, "Timeless Strategist")) continue;
            if (unit.IsOnBattlefield || unit.IsEmbarked)
                bonus += 1;
        }

        if (bonus > 0)
            faction.AddBattleFocusTokens(bonus);

        if (round == 1 && faction.HasDetachment(AeldariDetachment.SeerCouncil) &&
            AeldariFactionPack11Runtime.FateDice(faction.FactionId).Count == 0)
        {
            Aeldari11GenerateFateDice(faction);
        }

        AppendBattleLog(
            "AELDARI",
            faction.DisplayName,
            "Battle Focus: " + faction.BattleFocusTokens + " token(s) at start of round" +
            (bonus > 0 ? " (" + bonus + " bonus)." : "."));
    }

    public void Aeldari11OnPhaseStarted(AeldariGameController faction, GameEventContext context)
    {
        if (faction == null || context == null) return;
        AeldariFactionPack11Runtime.SynchronizePersistent(faction);

        if (context.Phase == Phase.Command &&
            string.Equals(context.ActingFaction, faction.FactionId, StringComparison.OrdinalIgnoreCase))
        {
            Aeldari11OfferCommandEnhancements(faction);
        }

        if (context.Phase == Phase.Shoot &&
            string.Equals(context.ActingFaction, faction.FactionId, StringComparison.OrdinalIgnoreCase))
        {
            Aeldari11OfferGuidingPresence(faction);
        }

        if (context.Phase == Phase.Fight &&
            faction.HasDetachment(AeldariDetachment.DevotedOfYnnead))
        {
            Aeldari11OfferLethalReprisal(faction);
        }
    }

    public void Aeldari11OnPhaseEnded(AeldariGameController faction, GameEventContext context)
    {
        if (faction == null || context == null) return;
        // End-of-phase stratagems are routed through the same exact event list.
        Aeldari11OfferEventRules(faction, context);
    }

    public void Aeldari11OnTurnEnded(AeldariGameController faction, GameEventContext context)
    {
        if (faction == null || context == null) return;

        // Ride the Wind extraction remains owned by the existing end-of-turn
        // flow, which is already multi-detachment aware after v38.

        // Webway Pathstone once per battle.
        foreach (SquadController unit in faction.ArmyUnits)
        {
            if (unit == null || !unit.IsAlive || !unit.IsOnBattlefield || IsEngaged(unit)) continue;
            if (!AeldariFactionPack11.UnitHasEnhancement(unit, "Webway Pathstone")) continue;
            if (AeldariFactionPack11Runtime.HasUsedThisBattle(faction.FactionId, "webway_pathstone:" + unit.UnitId)) continue;
            SquadController captured = unit.JoinedActionController();
            Aeldari11QueueChoice(
                "WEBWAY PATHSTONE",
                captured.DisplayName + " can be placed into Strategic Reserves (once per battle).",
                new RuleChoiceOption("Use Webway Pathstone", () =>
                {
                    CloseRuleChoice();
                    if (AeldariFactionPack11Runtime.MarkOncePerBattle(faction.FactionId, "webway_pathstone:" + captured.UnitId))
                        captured.SendToReserves(true);
                }),
                new RuleChoiceOption("Skip", CloseRuleChoice));
        }

        Aeldari11OfferEventRules(faction, context);
    }

    public void Aeldari11OfferEventRules(AeldariGameController faction, GameEventContext context)
    {
        if (faction == null || context == null || !faction.DetachmentLocked) return;

        // Enhancement reactions that are not Stratagems.
        Aeldari11OfferEnhancementReactions(faction, context);

        List<AeldariStratagem11> matching = AeldariFactionPack11.StratagemsFor(faction.FactionId)
            .Where(rule => Aeldari11EventMatches(rule, faction, context))
            .ToList();
        if (matching.Count == 0) return;

        SquadController unit = Aeldari11ReactionUnit(faction, context);
        matching = matching.Where(rule => Aeldari11RuleTargetsUnit(rule, unit, context, true)).ToList();
        if (matching.Count == 0) return;

        Aeldari11DeferredReaction request = new Aeldari11DeferredReaction
        {
            Faction = faction,
            Context = context,
            Rules = matching,
            Unit = unit
        };

        if (showRuleChoiceWindow || interactiveAttack != null || aeldari11OpeningReaction)
        {
            aeldari11DeferredReactions.Enqueue(request);
            return;
        }

        Aeldari11OpenReaction(request);
    }

    public void Aeldari11PumpDeferredReactions()
    {
        if (showRuleChoiceWindow || interactiveAttack != null || aeldari11OpeningReaction) return;
        if (aeldari11DeferredChoices.Count > 0)
        {
            System.Action choice = aeldari11DeferredChoices.Dequeue();
            if (choice != null) choice();
            return;
        }
        if (aeldari11DeferredReactions.Count == 0) return;
        Aeldari11OpenReaction(aeldari11DeferredReactions.Dequeue());
    }

    private void Aeldari11OpenReaction(Aeldari11DeferredReaction request)
    {
        if (request == null || request.Rules == null || request.Rules.Count == 0) return;
        aeldari11OpeningReaction = true;
        List<RuleChoiceOption> options = new List<RuleChoiceOption>();
        foreach (AeldariStratagem11 rule in request.Rules)
        {
            AeldariStratagem11 capturedRule = rule;
            options.Add(new RuleChoiceOption(
                capturedRule.Name + "  -  " + capturedRule.Cost + " CP",
                () =>
                {
                    CloseRuleChoice();
                    Aeldari11TryUseStratagem(capturedRule, request.Unit, request.Context, true);
                }));
        }
        options.Add(new RuleChoiceOption("Skip reaction", () =>
        {
            CloseRuleChoice();
            Aeldari11PumpDeferredReactions();
        }));
        OpenRuleChoice(
            "AELDARI REACTION",
            request.Unit != null ? request.Unit.DisplayName + "  -  choose an available rule." : "Choose an available Aeldari rule.",
            options.ToArray());
        aeldari11OpeningReaction = false;
    }

    public void DrawAeldari11StratagemCards(float left, float right, float y, float cardWidth)
    {
        IReadOnlyList<AeldariStratagem11> rules = AeldariFactionPack11.StratagemsFor(activeFaction);
        for (int i = 0; i < rules.Count; i++)
        {
            AeldariStratagem11 rule = rules[i];
            bool rightColumn = i % 2 == 1;
            int row = i / 2;
            Rect card = new Rect(rightColumn ? right : left, y + row * 54f, cardWidth, 42f);
            string label = rule.Name + "  -  " + rule.Cost + " CP";
            bool reactive = Aeldari11IsReactiveOnly(rule);
            if (DrawStratagemActionButton(
                    card,
                    label + (reactive ? " [REACTIVE]" : ""),
                    rule.FullRule))
            {
                // Reactive rules are opened automatically when Warboard has an
                // exact core event for the WHEN clause. Keeping the card
                // clickable is the Traditional/manual fallback for rules whose
                // trigger involves a player-declared sub-step that cannot be
                // inferred from board state alone.
                Aeldari11TryUseStratagem(
                    rule,
                    selectedSquad,
                    null,
                    reactive);
            }
        }
    }

    public bool Aeldari11TryUseStratagem(AeldariStratagem11 rule, SquadController requestedUnit, GameEventContext context, bool reactive)
    {
        if (rule == null) return false;
        SquadController unit = requestedUnit != null ? requestedUnit.JoinedActionController() :
            selectedSquad != null ? selectedSquad.JoinedActionController() : null;

        if (unit == null)
        {
            status = rule.Name + ": select the unit that will be targeted by this Stratagem.";
            return false;
        }

        if (!AeldariDetachmentRuntime.Has(unit.FactionId, rule.Detachment))
        {
            status = rule.Name + ": that unit's army does not have the required detachment.";
            return false;
        }

        if (!reactive && !Aeldari11TimingMatchesCurrent(rule, unit))
        {
            status = rule.Name + " is not in its WHEN window. " + rule.When;
            return false;
        }

        if (!Aeldari11RuleTargetsUnit(rule, unit, context, reactive))
        {
            status = unit.DisplayName + " does not satisfy the TARGET/RESTRICTIONS for " + rule.Name + ".";
            return false;
        }

        int fateValue = AeldariFactionPack11Runtime.RequiredFateValue(rule.Name);
        if (fateValue > 0 && AeldariFactionPack11Runtime.HasFateDie(unit.FactionId, fateValue) && rule.Cost > 0)
        {
            Aeldari11QueueChoice(
                "STRANDS OF FATE  -  " + rule.Name,
                "Discard a Fate die showing " + fateValue + " to reduce this Stratagem by 1CP?",
                new RuleChoiceOption("Discard " + fateValue + "  -  pay " + Mathf.Max(0, rule.Cost - 1) + " CP", () =>
                {
                    CloseRuleChoice();
                    if (AeldariFactionPack11Runtime.SpendFateDie(unit.FactionId, fateValue))
                        Aeldari11SpendAndExecute(rule, unit, context, Mathf.Max(0, rule.Cost - 1));
                }),
                new RuleChoiceOption("Keep Fate die  -  pay " + rule.Cost + " CP", () =>
                {
                    CloseRuleChoice();
                    Aeldari11SpendAndExecute(rule, unit, context, rule.Cost);
                }));
            return true;
        }

        return Aeldari11SpendAndExecute(rule, unit, context, rule.Cost);
    }

    private bool Aeldari11SpendAndExecute(AeldariStratagem11 rule, SquadController unit, GameEventContext context, int cost)
    {
        if (!SpendFactionStratagemCP(unit, cost, rule.Name))
            return false;

        bool automatic = Aeldari11ExecuteStratagem(rule, unit, context);
        AppendBattleLog("AELDARI STRATAGEM", rule.Name,
            unit.DisplayName + "  |  " + cost + " CP  |  " + rule.FullRule);

        if (!automatic)
        {
            if (!IsXcomMode)
            {
                QueueTraditionalRuleAlert(rule.Name.ToUpperInvariant(), rule.FullRule, Aeldari11SuggestedDice(rule));
            }
            else
            {
                status = rule.Name + " activated. Resolve the displayed rule text for any choice Warboard cannot infer automatically.";
            }
        }
        showStratagemMenu = false;
        Aeldari11PumpDeferredReactions();
        return true;
    }

    private bool Aeldari11ExecuteStratagem(AeldariStratagem11 rule, SquadController unit, GameEventContext context)
    {
        string key = AeldariFactionPack11Runtime.NormalizeKey(rule.Name);
        bool simple = Aeldari11ApplySimpleEffect(rule, unit);

        switch (key)
        {
            case "fire_and_fade":
            case "nomads_of_the_hidden_way":
            case "phatasmal_mirage":
            case "phantasmal_mirage":
            case "into_the_breach":
            {
                int add = key == "fire_and_fade" || key == "into_the_breach" ? 1 : 0;
                Aeldari11RollMove(unit, rule.Name, add, key == "phantasmal_mirage" ? 0 : 0);
                if (key == "fire_and_fade" || key == "nomads_of_the_hidden_way" || key == "phantasmal_mirage")
                    AeldariFactionPack11Runtime.SetTurnFlag(unit, "cannot_charge");
                if (key == "fire_and_fade" || key == "nomads_of_the_hidden_way")
                    AeldariFactionPack11Runtime.SetTurnFlag(unit, "cannot_embark");
                return true;
            }
            case "overflight":
                BeginSpecialMove(unit, 7f, rule.Name, null);
                return true;
            case "tricksters_retort":
            case "weaving_stride":
                BeginSpecialMove(unit, 6f, rule.Name, null);
                return true;
            case "deceptive_feint":
                Aeldari11RollMoveD3Plus(unit, rule.Name, 3);
                return true;
            case "vengeful_sorrow":
                Aeldari11RollMove(unit, rule.Name, 1, 0);
                return true;
            case "webway_tunnel":
            case "exit_the_stage":
            case "skyward_lunge":
            case "withdraw_and_reinforce":
                if (IsEngaged(unit)) return false;
                unit.SendToReserves(true);
                status = rule.Name + ": " + unit.DisplayName + " placed into Strategic Reserves.";
                return true;
            case "unshrouded_truth":
                unit.SendToReserves(true);
                unit.TemporaryDeepStrike = true;
                unit.MustIngressFromVeil = true;
                return true;
            case "cost_of_victory":
                if (IsEngaged(unit)) return false;
                unit.SendToReserves(true);
                QueueTraditionalRuleAlert(rule.Name.ToUpperInvariant(),
                    "Return every destroyed GUARDIANS model to this unit, then keep the unit in Strategic Reserves. Warboard has placed the unit in Reserves; use RESTORE to return its destroyed models if Traditional mode is active.", 0);
                return true;
            case "captivating_performance":
            {
                ObjectiveController objective = objectives.FirstOrDefault(value => value != null && value.UnitWithinRange(unit) && value.Controller(squads) == unit.FactionId);
                if (objective == null) return false;
                objective.SecureFor(unit.FactionId);
                RefreshObjectiveDisplays();
                return true;
            }
            case "pall_of_dread":
                // Pall of Dread secures a specific objective previously
                // controlled by the destroyed unit's player. The exact marker
                // is a player choice, so surface the rule rather than guessing.
                return false;
            case "presentiment_of_dread":
            case "eldritch_suppression":
                return Aeldari11BattleShockTarget(rule, unit, context);
            case "psychic_shield":
                AeldariFactionPack11Runtime.SetPhaseFlag(unit, "psychic_shield");
                // Conditional mortal/Psychic/Devastating FNP needs the exact
                // incoming damage context. Keep the state and surface the rule.
                return false;
            case "layered_wards":
                AeldariFactionPack11Runtime.SetPhaseFlag(unit, "layered_wards");
                // Mortal-wound-only FNP is context dependent. Keep the state
                // and surface the exact rule to Traditional mode.
                return false;
            case "spirit_token":
                AeldariFactionPack11Runtime.SetPhaseFlag(unit, "spirit_token");
                return true;
            case "crushing_strides":
                return Aeldari11CrushingStrides(unit);
            case "skyborne_sanctuary":
                return Aeldari11TrySkyborneSanctuary(unit);
            case "soul_bridge":
                return Aeldari11SoulBridge(unit);
            case "staged_death":
                AeldariFactionPack11Runtime.SetPhaseFlag(unit, "staged_death");
                return false;
            case "heroes_fall":
            case "parting_the_veil":
            case "to_their_final_breath":
                AeldariFactionPack11Runtime.SetPhaseFlag(unit, key);
                return false;
            case "death_answers_death":
                BeginSpecialShoot(unit, null, rule.Name, null);
                return true;
            case "vaul_s_vengeance":
                if (context != null && context.Source != null)
                    BeginSpecialShoot(unit, context.Source, rule.Name, null);
                else
                    BeginSpecialShoot(unit, null, rule.Name, null);
                return true;
            case "lethal_ruse":
                unit.AeldariCanChargeAfterFallBack = true;
                if (unit.HasKeyword("anhrathe")) return false;
                return true;
            case "fate_inescapable":
            case "isha_s_fury":
            case "impeding_fire":
            case "fangs_of_the_brood":
            case "venomous_wrath":
            case "weavers_coils":
            case "bloody_dance":
            case "casting_back_the_veil":
                return false;
        }

        return simple;
    }

    private bool Aeldari11ApplySimpleEffect(AeldariStratagem11 rule, SquadController unit)
    {
        if (rule == null || unit == null) return false;
        string effect = (rule.Effect ?? "").ToLowerInvariant();
        string key = AeldariFactionPack11Runtime.NormalizeKey(rule.Name);
        bool applied = false;

        if (effect.Contains("subtract 1 from the hit roll")) { unit.AeldariDefensiveHitModifier -= 1; applied = true; }
        if (effect.Contains("subtract 1 from the wound roll")) { unit.AeldariDefensiveWoundModifier -= 1; applied = true; }
        if (effect.Contains("add 1 to the hit roll")) { unit.AeldariOffensiveHitModifier += 1; applied = true; }
        if (effect.Contains("add 1 to the wound roll")) { unit.AeldariOffensiveWoundModifier += 1; applied = true; }
        if (effect.Contains("re-roll the hit roll")) { unit.AeldariRerollAllHits = true; applied = true; }
        if (effect.Contains("re-roll a hit roll of 1")) { unit.AeldariRerollHitOnes = true; applied = true; }
        if (effect.Contains("re-roll the wound roll")) { unit.AeldariRerollAllWounds = true; applied = true; }
        if (effect.Contains("re-roll a wound roll of 1")) { unit.AeldariRerollWoundOnes = true; applied = true; }
        if (effect.Contains("[lethal hits]")) { unit.AeldariLethalHits = true; applied = true; }
        if (effect.Contains("[ignores cover]")) { unit.AeldariIgnoresCover = true; applied = true; }
        if (effect.Contains("[devastating wounds]")) { unit.AeldariDevastatingWounds = true; applied = true; }
        if (effect.Contains("[sustained hits 1]")) { unit.AeldariSustainedHits = Mathf.Max(1, unit.AeldariSustainedHits); applied = true; }
        if (effect.Contains("improve the armour penetration characteristic") || effect.Contains("+1 ap")) { unit.AeldariApModifier -= 1; applied = true; }
        if (effect.Contains("add 1 to the damage characteristic")) { unit.AeldariDamageModifier += 1; applied = true; }
        if (effect.Contains("4+ invulnerable save")) { unit.AeldariInvulnerableOverride = 4; applied = true; }
        if (effect.Contains("5+ invulnerable save")) { unit.AeldariInvulnerableOverride = 5; applied = true; }
        if (effect.Contains("eligible to shoot") && effect.Contains("fell back")) { unit.AeldariCanShootAfterFallBack = true; applied = true; }
        if (effect.Contains("eligible to declare a charge") && effect.Contains("fell back")) { unit.AeldariCanChargeAfterFallBack = true; applied = true; }
        if (effect.Contains("eligible to declare a charge") && effect.Contains("advanced")) { unit.AeldariCanChargeAfterAdvance = true; applied = true; }

        if (key == "blitzing_firepower") { AeldariFactionPack11Runtime.SetPhaseFlag(unit, "blitzing_firepower_crit5"); applied = true; }
        if (key == "forewarned") { AeldariFactionPack11Runtime.SetPhaseFlag(unit, "forewarned"); applied = true; }
        if (key == "cloak_and_shadow") { AeldariFactionPack11Runtime.SetPhaseFlag(unit, "cloak_and_shadow"); applied = true; }
        if (key == "no_prey_too_big") { AeldariFactionPack11Runtime.SetPhaseFlag(unit, "no_prey_too_big"); applied = true; }
        if (key == "outcast_ambush") { AeldariFactionPack11Runtime.SetPhaseFlag(unit, "outcast_ambush"); unit.AeldariIgnoresCover = true; applied = true; }
        if (key == "ruthless_killers") { AeldariFactionPack11Runtime.SetPhaseFlag(unit, "ruthless_killers"); applied = true; }
        if (key == "presaged_rehearsal") { AeldariFactionPack11Runtime.SetPhaseFlag(unit, "presaged_rehearsal"); applied = true; }
        if (key == "doom_inescapable") { AeldariFactionPack11Runtime.SetPhaseFlag(unit, "doom_inescapable"); applied = true; }
        if (key == "raiders_spoils") { AeldariFactionPack11Runtime.SetCommandFlag(unit, "raiders_spoils"); applied = true; }
        if (key == "vectored_engines") { unit.AeldariVectoredEnginesActive = true; applied = true; }
        if (key == "mocking_flight") { unit.AeldariCanShootAfterFallBack = true; unit.AeldariCanChargeAfterFallBack = true; applied = true; }
        if (key == "wind_of_blades") { unit.AeldariCanShootAfterFallBack = true; unit.AeldariCanChargeAfterFallBack = true; unit.AeldariCanChargeAfterAdvance = true; applied = true; }
        if (key == "time_to_strike") { unit.AeldariCanChargeAfterAdvance = true; applied = false; }
        if (key == "preternatural_precision") applied = false;

        return applied;
    }

    private void Aeldari11RollMove(SquadController unit, string label, int flatBonus, int minimum)
    {
        if (unit == null) return;
        if (IsXcomMode)
        {
            int roll = DiceRoller.RollD6(label) + flatBonus;
            BeginSpecialMove(unit, Mathf.Max(minimum, roll), label, null);
        }
        else
        {
            OpenTraditionalNumericPrompt(label, "Roll D6" + (flatBonus != 0 ? (flatBonus > 0 ? "+" : "") + flatBonus : "") + " for the move distance.",
                1 + flatBonus, 7 + flatBonus, 4 + flatBonus, 1,
                value => BeginSpecialMove(unit, value, label, null));
        }
    }

    private void Aeldari11RollMoveD3Plus(SquadController unit, string label, int flatBonus)
    {
        if (unit == null) return;
        if (IsXcomMode)
        {
            int roll = DiceRoller.RollD6(label);
            int d3 = (roll + 1) / 2;
            BeginSpecialMove(unit, d3 + flatBonus, label, null);
        }
        else
        {
            OpenTraditionalNumericPrompt(label, "Roll D3+" + flatBonus + " for the move distance.",
                1 + flatBonus, 3 + flatBonus, 2 + flatBonus, 1,
                value => BeginSpecialMove(unit, value, label, null));
        }
    }

    private bool Aeldari11BattleShockTarget(AeldariStratagem11 rule, SquadController source, GameEventContext context)
    {
        SquadController target = context != null && context.Source != null && context.Source.FactionId != source.FactionId
            ? context.Source.JoinedActionController()
            : squads.Where(unit => unit != null && unit.IsAlive && unit.IsOnBattlefield && unit.FactionId != source.FactionId)
                .OrderBy(unit => JoinedDistance(source, unit)).FirstOrDefault();
        if (target == null) return false;
        if (!IsXcomMode)
        {
            QueueTraditionalRuleAlert(rule.Name.ToUpperInvariant(), target.DisplayName + " must take the Battle-shock test described by the Stratagem. " + rule.Effect, 2);
            return true;
        }
        int modifier = (rule.Effect ?? "").IndexOf("-1", StringComparison.OrdinalIgnoreCase) >= 0 ? -1 : 0;
        int roll = DiceRoller.RollD6(rule.Name) + DiceRoller.RollD6(rule.Name) + modifier;
        target.SetBattleShocked(roll < target.BestLeadership(), roll);
        RefreshObjectiveDisplays();
        return true;
    }

    private bool Aeldari11CrushingStrides(SquadController unit)
    {
        SquadController target = squads.Where(enemy => enemy != null && enemy.IsAlive && enemy.IsOnBattlefield && enemy.FactionId != unit.FactionId && UnitsAreEngaged(unit, enemy))
            .OrderBy(enemy => JoinedDistance(unit, enemy)).FirstOrDefault();
        if (target == null) return false;
        int dice = unit.JoinedLivingModels;
        if (!IsXcomMode)
        {
            QueueTraditionalRuleAlert("CRUSHING STRIDES", "Roll one D6 for each model in " + unit.DisplayName + " as instructed by the Stratagem and apply the resulting mortal wounds to " + target.DisplayName + ".", dice);
            return true;
        }
        int mortals = 0;
        for (int i = 0; i < dice; i++) if (DiceRoller.RollD6("Crushing Strides") >= 4) mortals++;
        Core11ApplyMortalWounds(target, mortals, "Crushing Strides");
        return true;
    }

    private bool Aeldari11TrySkyborneSanctuary(SquadController unit)
    {
        List<SquadController> transports = Core11EmbarkTargets(unit)
            .Where(transport => JoinedDistance(unit, transport) <= 6.001f).ToList();
        if (transports.Count == 0) return false;
        List<RuleChoiceOption> options = new List<RuleChoiceOption>();
        foreach (SquadController transport in transports)
        {
            SquadController captured = transport;
            options.Add(new RuleChoiceOption("Embark in " + captured.DisplayName, () =>
            {
                CloseRuleChoice();
                Core11Embark(unit, captured);
            }));
        }
        OpenRuleChoice("SKYBORNE SANCTUARY", "Choose the friendly TRANSPORT within 6 inches.", options.ToArray());
        return true;
    }

    private bool Aeldari11SoulBridge(SquadController unit)
    {
        QueueTraditionalRuleAlert("SOUL BRIDGE", "Resolve Soul Bridge exactly as shown in the Aeldari faction pack. Warboard has recorded the CP expenditure and target; any model-return/placement choice remains yours.", 0);
        return true;
    }

    private void Aeldari11GenerateFateDice(AeldariGameController faction)
    {
        int count = AeldariFactionPack11Runtime.FateDiceCountForBattle(this);
        if (IsXcomMode)
        {
            List<int> values = new List<int>();
            for (int i = 0; i < count; i++) values.Add(DiceRoller.RollD6("Strands of Fate"));
            AeldariFactionPack11Runtime.SetFateDice(faction.FactionId, values);
            AppendBattleLog("AELDARI", "Strands of Fate", "Fate dice: " + string.Join(", ", values.Select(v => v.ToString()).ToArray()));
            return;
        }

        aeldari11ManualFateEntry.Clear();
        Aeldari11PromptNextFateDie(faction, count);
    }

    private void Aeldari11PromptNextFateDie(AeldariGameController faction, int total)
    {
        if (aeldari11ManualFateEntry.Count >= total)
        {
            AeldariFactionPack11Runtime.SetFateDice(faction.FactionId, aeldari11ManualFateEntry);
            AppendBattleLog("AELDARI", "Strands of Fate", "Fate dice: " + string.Join(", ", aeldari11ManualFateEntry.Select(v => v.ToString()).ToArray()));
            return;
        }
        int number = aeldari11ManualFateEntry.Count + 1;
        OpenTraditionalNumericPrompt("STRANDS OF FATE", "Enter Fate die " + number + " of " + total + ".", 1, 6, 3, 1,
            value =>
            {
                aeldari11ManualFateEntry.Add(value);
                Aeldari11PromptNextFateDie(faction, total);
            });
    }

    private void Aeldari11OfferCommandEnhancements(AeldariGameController faction)
    {
        // Spirit Conclave command enhancements.
        foreach (SquadController bearer in faction.ArmyUnits.Where(unit => unit != null && unit.IsAlive && unit.IsOnBattlefield))
        {
            if (AeldariFactionPack11.UnitHasEnhancement(bearer, "Light Of Clarity"))
                Aeldari11OfferWraithCommandTarget(bearer, "LIGHT OF CLARITY", "light_of_clarity", false);
            if (AeldariFactionPack11.UnitHasEnhancement(bearer, "Stave Of Kurnous"))
                Aeldari11OfferWraithCommandTarget(bearer, "STAVE OF KURNOUS", "stave_of_kurnous", true);
            if (AeldariFactionPack11.UnitHasEnhancement(bearer, "Rune Of Mists"))
                Aeldari11OfferWraithCommandTarget(bearer, "RUNE OF MISTS", "rune_of_mists", false);
        }

        // Lucid Eye: alter one Fate die by +/-1.
        SquadController lucid = faction.ArmyUnits.FirstOrDefault(unit => AeldariFactionPack11.UnitHasEnhancement(unit, "Lucid Eye"));
        IReadOnlyList<int> dice = AeldariFactionPack11Runtime.FateDice(faction.FactionId);
        if (lucid != null && dice.Count > 0)
        {
            List<RuleChoiceOption> options = new List<RuleChoiceOption>();
            for (int i = 0; i < dice.Count; i++)
            {
                int index = i;
                int value = dice[i];
                if (value < 6) options.Add(new RuleChoiceOption("Die " + (i + 1) + ": " + value + " -> " + (value + 1), () => { CloseRuleChoice(); AeldariFactionPack11Runtime.AdjustFateDie(faction.FactionId, index, 1); }));
                if (value > 1) options.Add(new RuleChoiceOption("Die " + (i + 1) + ": " + value + " -> " + (value - 1), () => { CloseRuleChoice(); AeldariFactionPack11Runtime.AdjustFateDie(faction.FactionId, index, -1); }));
            }
            options.Add(new RuleChoiceOption("Do not use Lucid Eye", CloseRuleChoice));
            Aeldari11QueueChoice("LUCID EYE", "Add 1 to or subtract 1 from one Fate die in your pool.", options.ToArray());
        }

        // Echoes of Ulthanesh CP roll.
        foreach (SquadController bearer in faction.ArmyUnits.Where(unit => AeldariFactionPack11.UnitHasEnhancement(unit, "Echoes Of Ulthanesh") && unit.IsAlive && unit.IsOnBattlefield))
        {
            int bonus = Aeldari11InsideOpponentDeployment(bearer) ? 2 : Aeldari11InsideOwnDeployment(bearer) ? 0 : 1;
            if (!IsXcomMode)
            {
                SquadController captured = bearer;
                OpenTraditionalNumericPrompt("ECHOES OF ULTHANESH", "Roll one D6. Add " + bonus + "; on a modified 5+ gain 1CP.", 1, 6, 3, 1,
                    value => { if (value + bonus >= 5) Aeldari11GainCP(captured.FactionId, 1); });
            }
            else if (DiceRoller.RollD6("Echoes of Ulthanesh") + bonus >= 5)
                Aeldari11GainCP(bearer.FactionId, 1);
        }
    }

    private void Aeldari11OfferWraithCommandTarget(SquadController bearer, string title, string flag, bool excludeTitanic)
    {
        List<SquadController> targets = squads.Where(unit => unit != null && unit.IsAlive && unit.IsOnBattlefield && unit.FactionId == bearer.FactionId &&
            unit.HasKeyword("wraith construct") && (!excludeTitanic || !unit.HasKeyword("titanic")) && JoinedDistance(bearer, unit) <= 12.001f).ToList();
        if (targets.Count == 0) return;
        List<RuleChoiceOption> options = targets.Select(target =>
        {
            SquadController captured = target;
            return new RuleChoiceOption(captured.DisplayName, () =>
            {
                CloseRuleChoice();
                AeldariFactionPack11Runtime.SetCommandFlag(captured, flag);
            });
        }).ToList();
        options.Add(new RuleChoiceOption("Skip", CloseRuleChoice));
        Aeldari11QueueChoice(title, "Select one friendly WRAITH CONSTRUCT within 12 inches.", options.ToArray());
    }

    private void Aeldari11OfferGuidingPresence(AeldariGameController faction)
    {
        foreach (SquadController bearer in faction.ArmyUnits.Where(unit => unit != null && unit.IsAlive && unit.IsOnBattlefield && AeldariFactionPack11.UnitHasEnhancement(unit, "Guiding Presence")))
        {
            List<SquadController> targets = faction.ArmyUnits.Where(unit => unit != null && unit.IsAlive && unit.IsOnBattlefield && unit.HasKeyword("vehicle") && JoinedDistance(bearer, unit) <= 6.001f).ToList();
            if (targets.Count == 0) continue;
            List<RuleChoiceOption> options = targets.Select(target =>
            {
                SquadController captured = target;
                return new RuleChoiceOption(captured.DisplayName, () => { CloseRuleChoice(); AeldariFactionPack11Runtime.SetPhaseFlag(captured, "guiding_presence"); });
            }).ToList();
            options.Add(new RuleChoiceOption("Skip", CloseRuleChoice));
            Aeldari11QueueChoice("GUIDING PRESENCE", "Select one visible friendly AELDARI VEHICLE within 6 inches; its ranged attacks have +1 to hit this phase.", options.ToArray());
        }
    }

    private void Aeldari11OfferLethalReprisal(AeldariGameController faction)
    {
        List<SquadController> eligible = faction.ArmyUnits.Where(unit => unit != null && unit.IsAlive && unit.IsOnBattlefield && unit.HasKeyword("ynnari") &&
            !unit.HasKeyword("titanic") && unit.JoinedLivingModels < unit.JoinedStartingStrength()).Select(unit => unit.JoinedActionController()).Distinct().ToList();
        if (eligible.Count == 0) return;
        List<RuleChoiceOption> options = eligible.Select(unit =>
        {
            SquadController captured = unit;
            return new RuleChoiceOption(captured.DisplayName, () => { CloseRuleChoice(); captured.TemporaryFightsFirst = true; });
        }).ToList();
        options.Add(new RuleChoiceOption("Skip Lethal Reprisal", CloseRuleChoice));
        Aeldari11QueueChoice("LETHAL REPRISAL", "At the start of the Fight phase select one eligible below-starting-strength YNNARI unit to gain Fights First.", options.ToArray());
    }

    private void Aeldari11OfferEnhancementReactions(AeldariGameController faction, GameEventContext context)
    {
        Aeldari11ResolveRelentlessRaiders(faction, context);

        if (context.Type == GameEventType.MoveEnded && context.Source != null &&
            !string.Equals(context.Source.FactionId, faction.FactionId, StringComparison.OrdinalIgnoreCase))
        {
            foreach (SquadController bearer in faction.ArmyUnits.Where(unit => unit != null && unit.IsAlive && unit.IsOnBattlefield && !IsEngaged(unit) &&
                AeldariFactionPack11.UnitHasEnhancement(unit, "Higher Duty") && JoinedDistance(unit, context.Source) <= 8.001f))
            {
                SquadController captured = bearer;
                Aeldari11QueueChoice("HIGHER DUTY", captured.DisplayName + " can make a Normal move of up to 6 inches.",
                    new RuleChoiceOption("Move", () => { CloseRuleChoice(); BeginSpecialMove(captured, 6f, "Higher Duty", null); }),
                    new RuleChoiceOption("Skip", CloseRuleChoice));
            }
        }

        if (context.Type == GameEventType.UnitFinishedShooting && context.Source != null &&
            string.Equals(context.Source.FactionId, faction.FactionId, StringComparison.OrdinalIgnoreCase) &&
            AeldariFactionPack11.UnitHasEnhancement(context.Source, "Storm Of Whispers"))
        {
            QueueTraditionalRuleAlert("STORM OF WHISPERS", "After the bearer has shot, select one enemy unit hit by those attacks. That unit must take a Battle-shock test.", 2);
        }
    }

    private void Aeldari11ResolveRelentlessRaiders(
        AeldariGameController faction,
        GameEventContext context)
    {
        if (faction == null ||
            context == null ||
            !faction.HasDetachment(AeldariDetachment.CorsairCoterie) ||
            context.Type != GameEventType.MoveEnded ||
            context.Source == null ||
            string.Equals(
                context.Source.FactionId,
                faction.FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SquadController enemy =
            context.Source.JoinedActionController();

        ObjectiveController controlled =
            objectives.FirstOrDefault(
                objective =>
                    objective != null &&
                    objective.Controller(squads) == faction.FactionId &&
                    objective.UnitWithinRange(enemy));

        if (controlled == null)
            return;

        if (!IsXcomMode)
        {
            QueueTraditionalRuleAlert(
                "RELENTLESS RAIDERS",
                enemy.DisplayName +
                " ended a move within range of controlled objective " +
                controlled.name +
                ". Roll one D6; on a 2+ it suffers D3 mortal wounds.",
                1);
            return;
        }

        int trigger =
            DiceRoller.RollD6(
                "Relentless Raiders: " + enemy.DisplayName);

        if (trigger < 2)
            return;

        int d3 =
            (DiceRoller.RollD6(
                "Relentless Raiders mortal wounds") + 1) / 2;

        Core11ApplyMortalWounds(
            enemy,
            d3,
            "Relentless Raiders");
    }

    private void Aeldari11OfferReturnUnitsToReserves(string title, AeldariGameController faction, List<SquadController> eligible, int limit)
    {
        if (eligible == null || eligible.Count == 0 || limit <= 0) return;
        // One-at-a-time choice, allowing the player to stop early.
        Aeldari11OfferReturnOne(title, faction, eligible, limit);
    }

    private void Aeldari11OfferReturnOne(string title, AeldariGameController faction, List<SquadController> remaining, int left)
    {
        if (left <= 0 || remaining.Count == 0) return;
        List<RuleChoiceOption> options = new List<RuleChoiceOption>();
        foreach (SquadController unit in remaining)
        {
            SquadController captured = unit;
            options.Add(new RuleChoiceOption(captured.DisplayName, () =>
            {
                CloseRuleChoice();
                captured.SendToReserves(true);
                List<SquadController> next = remaining.Where(value => value != captured).ToList();
                Aeldari11OfferReturnOne(title, faction, next, left - 1);
            }));
        }
        options.Add(new RuleChoiceOption("Done", CloseRuleChoice));
        Aeldari11QueueChoice(title, "You may place up to " + left + " more eligible unit(s) into Strategic Reserves.", options.ToArray());
    }

    private bool Aeldari11EventMatches(AeldariStratagem11 rule, AeldariGameController faction, GameEventContext context)
    {
        string key = AeldariFactionPack11Runtime.NormalizeKey(rule != null ? rule.Name : "");

        // These four Devoted reactions already have mature, attack-aware
        // handlers in the existing GameController flow. Do not open a second
        // v42 reaction for the same timing window.
        if (key == "macabre_resilience" ||
            key == "emissaries_of_ynnead" ||
            key == "parting_the_veil" ||
            key == "death_answers_death")
        {
            return false;
        }

        string when = (rule.When ?? "").ToLowerInvariant();
        bool ownSource = context.Source != null && string.Equals(context.Source.FactionId, faction.FactionId, StringComparison.OrdinalIgnoreCase);
        bool ownTarget = context.Target != null && string.Equals(context.Target.FactionId, faction.FactionId, StringComparison.OrdinalIgnoreCase);
        bool opponentSource = context.Source != null && !ownSource;

        switch (context.Type)
        {
            case GameEventType.AttackStarted:
                return ownTarget && (when.Contains("selected its targets") || when.Contains("targets a friendly") || when.Contains("enemy unit targets"));
            case GameEventType.AttackResolved:
                return (opponentSource && when.Contains("enemy unit has shot")) || (ownSource && when.Contains("has shot"));
            case GameEventType.UnitFinishedShooting:
                return (ownSource && (when.Contains("has shot") || when.Contains("destroyed one or more enemy units"))) ||
                       (opponentSource && when.Contains("enemy unit has shot"));
            case GameEventType.UnitFellBack:
                return (ownSource && when.Contains("falls back")) || (opponentSource && when.Contains("enemy unit") && when.Contains("fall back"));
            case GameEventType.MoveEnded:
                return opponentSource && when.Contains("opponent") && when.Contains("movement phase") && when.Contains("ends");
            case GameEventType.UnitSelectedToFight:
                return ownSource && when.Contains("selected to fight");
            case GameEventType.UnitDestroyed:
                return when.Contains("destroys") || when.Contains("destroyed");
            case GameEventType.PhaseEnded:
                if (context.Phase == Phase.Shoot) return when.Contains("end of your shooting phase") || when.Contains("end of your opponent") && when.Contains("shooting phase");
                if (context.Phase == Phase.Fight) return when.Contains("end of the fight phase") || when.Contains("end of your opponent") && when.Contains("fight phase");
                if (context.Phase == Phase.Move) return when.Contains("end of your movement phase");
                return false;
            case GameEventType.ChargeDeclared:
                return ownSource && when.Contains("declares a charge");
            case GameEventType.UnitSetUp:
                return ownSource && when.Contains("reinforcements step");
            default:
                return false;
        }
    }

    private SquadController Aeldari11ReactionUnit(AeldariGameController faction, GameEventContext context)
    {
        if (faction == null || context == null) return null;
        if (context.Target != null && string.Equals(context.Target.FactionId, faction.FactionId, StringComparison.OrdinalIgnoreCase)) return context.Target.JoinedActionController();
        if (context.Source != null && string.Equals(context.Source.FactionId, faction.FactionId, StringComparison.OrdinalIgnoreCase)) return context.Source.JoinedActionController();

        // Some post-attack reactions target a different friendly unit. Use the
        // selected unit if it belongs to this faction; otherwise choose the
        // nearest legal battlefield unit and let target validation reject it.
        if (selectedSquad != null && string.Equals(selectedSquad.FactionId, faction.FactionId, StringComparison.OrdinalIgnoreCase))
            return selectedSquad.JoinedActionController();
        return faction.ArmyUnits.FirstOrDefault(unit => unit != null && unit.IsAlive && unit.IsOnBattlefield && !unit.IsAttachedLeader);
    }

    private bool Aeldari11TimingMatchesCurrent(AeldariStratagem11 rule, SquadController unit)
    {
        if (rule == null || unit == null) return false;
        string when = (rule.When ?? "").ToLowerInvariant();
        if (when.Contains("opponent") && string.Equals(activeFaction, unit.FactionId, StringComparison.OrdinalIgnoreCase)) return false;
        if (when.Contains("command phase") && phase != Phase.Command) return false;
        if (when.Contains("movement phase") && phase != Phase.Move) return false;
        if (when.Contains("shooting phase") && phase != Phase.Shoot) return false;
        if (when.Contains("fight phase") && phase != Phase.Fight) return false;
        if (when.Contains("reinforcements step") && phase != Phase.Move) return false;
        if (Aeldari11IsReactiveOnly(rule)) return false;
        return true;
    }

    private bool Aeldari11IsReactiveOnly(AeldariStratagem11 rule)
    {
        string when = (rule.When ?? "").ToLowerInvariant();
        return when.Contains("just after") || when.Contains("when an enemy") || when.Contains("when a friendly") ||
               when.Contains("when a model") || when.Contains("when setting up") ||
               when.Contains("end of your opponent") || when.StartsWith("any phase, when");
    }

    private bool Aeldari11RuleTargetsUnit(AeldariStratagem11 rule, SquadController unit, GameEventContext context, bool reactive)
    {
        if (rule == null || unit == null || !unit.IsAlive) return false;
        string target = (rule.Target ?? "").ToLowerInvariant();
        string restrictions = (rule.Restrictions ?? "").ToLowerInvariant();

        if (target.Contains("asuryani") && !unit.HasKeyword("asuryani")) return false;
        if (target.Contains("ynnari") && !unit.HasKeyword("ynnari")) return false;
        if (target.Contains("harlequins") && !unit.HasKeyword("harlequins")) return false;
        if (target.Contains("anhrathe") && !unit.HasKeyword("anhrathe") && !target.Contains("rangers")) return false;
        if (target.Contains("wraith construct") && !unit.HasKeyword("wraith construct") && !target.Contains("excluding wraith construct")) return false;
        if (target.Contains("vehicle") && !target.Contains("monster or vehicle") && !unit.HasKeyword("vehicle") && !target.Contains("or vyper")) return false;
        if (target.Contains("mounted") && !unit.HasKeyword("mounted") && !target.Contains("or vyper")) return false;
        if (target.Contains("troupe unit") && !AeldariFactionPack11.NameOrKeyword(unit, "Troupe")) return false;
        if (target.Contains("storm guardians") && !AeldariFactionPack11.NameOrKeyword(unit, "Storm Guardians")) return false;
        if ((target.Contains("dire avengers or guardians") || target.Contains("dire avengers/guardians")) &&
            !(AeldariFactionPack11.NameOrKeyword(unit, "Dire Avenger") || AeldariFactionPack11.NameOrKeyword(unit, "Guardian"))) return false;
        if (target.Contains("aspect warriors") && !unit.HasKeyword("aspect warriors") && !target.Contains("or avatar")) return false;
        if (target.Contains("rangers/shroud runners") && !(AeldariFactionPack11.NameOrKeyword(unit, "Rangers") || AeldariFactionPack11.NameOrKeyword(unit, "Shroud Runners"))) return false;
        if (target.Contains("corsair voidscarred") && !AeldariFactionPack11.NameOrKeyword(unit, "Corsair Voidscarred")) return false;
        if (target.Contains("war walkers") && !AeldariFactionPack11.NameOrKeyword(unit, "War Walker")) return false;
        if (target.Contains("infantry") && !unit.HasKeyword("infantry") && !target.Contains("mounted")) return false;
        if (target.Contains("psyker") && !unit.HasKeyword("psyker") && !target.Contains("and one friendly")) return false;

        if (target.Contains("excluding wraith construct") && unit.HasKeyword("wraith construct")) return false;
        if (target.Contains("excluding titanic") && unit.HasKeyword("titanic")) return false;
        if (target.Contains("excluding aircraft") && unit.HasKeyword("aircraft")) return false;
        if (target.Contains("excluding asurmen") && AeldariFactionPack11.NameOrKeyword(unit, "Asurmen")) return false;
        if (target.Contains("unengaged") && IsEngaged(unit)) return false;
        if (target.Contains("has not been selected to shoot") && unit.HasShot) return false;
        if (target.Contains("has not been selected to fight") && unit.HasFought) return false;
        if (target.Contains("has not been selected to move") && unit.HasMoved) return false;
        if (target.Contains("set up") && target.Contains("from reserves") && !unit.WasSetUpThisTurn) return false;
        if (target.Contains("within range of one or more objective") && !UnitWithinAnyObjective(unit)) return false;
        if (restrictions.Contains("once per battle round") && AeldariFactionPack11Runtime.HasUsedThisRound(unit.FactionId, rule.Name)) return false;
        if (restrictions.Contains("once per battle") && AeldariFactionPack11Runtime.HasUsedThisBattle(unit.FactionId, rule.Name + ":" + unit.UnitId)) return false;
        return true;
    }

    private int Aeldari11SuggestedDice(AeldariStratagem11 rule)
    {
        string text = ((rule.Effect ?? "") + " " + (rule.Restrictions ?? "")).ToLowerInvariant();
        if (text.Contains("six d6")) return 6;
        if (text.Contains("one d6") || text.Contains("roll one d6")) return 1;
        if (text.Contains("battle-shock")) return 2;
        return 0;
    }

    private void Aeldari11QueueChoice(string title, string description, params RuleChoiceOption[] options)
    {
        System.Action open = () => OpenRuleChoice(title, description, options);
        if (!showRuleChoiceWindow && interactiveAttack == null && !aeldari11OpeningReaction)
        {
            open();
            return;
        }
        aeldari11DeferredChoices.Enqueue(open);
    }

    public void Aeldari11ResolvePiratePrinceRefund(SquadController unit)
    {
        if (unit == null) return;
        unit = unit.JoinedActionController();

        if (IsXcomMode)
        {
            int roll = DiceRoller.RollD6("Pirate Prince Battle Focus refund");
            if (roll >= 3)
            {
                AeldariGameController faction =
                    FactionControllerRuntime.GetAeldari(unit.FactionId);
                if (faction != null) faction.AddBattleFocusTokens(1);
            }
            return;
        }

        OpenTraditionalNumericPrompt(
            "PIRATE PRINCE",
            "A Battle Focus token was spent for the bearer’s unit. Roll one D6; on a 3+, regain 1 Battle Focus token.",
            1,
            6,
            3,
            1,
            value =>
            {
                if (value >= 3)
                {
                    AeldariGameController faction =
                        FactionControllerRuntime.GetAeldari(unit.FactionId);
                    if (faction != null) faction.AddBattleFocusTokens(1);
                }
            });
    }

    private void Aeldari11GainCP(string faction, int amount)
    {
        if (string.IsNullOrWhiteSpace(faction) || amount <= 0) return;
        if (!commandPoints.ContainsKey(faction)) commandPoints[faction] = 0;
        commandPoints[faction] += amount;
        AppendBattleLog("AELDARI", "Command Points", DisplayFactionName(faction) + " gained " + amount + " CP.");
    }

    private bool Aeldari11InsideOwnDeployment(SquadController unit)
    {
        MissionDeploymentZone zone = DeploymentZoneForFaction(unit != null ? unit.FactionId : "");
        return zone != null && unit != null && unit.JoinedLivingModelTokens().All(model => zone.ContainsBase(model.transform.position, model.BaseRadiusInches));
    }

    private bool Aeldari11InsideOpponentDeployment(SquadController unit)
    {
        if (unit == null) return false;
        string enemy = factions.FirstOrDefault(value => value != unit.FactionId);
        MissionDeploymentZone zone = DeploymentZoneForFaction(enemy);
        return zone != null && unit.JoinedLivingModelTokens().All(model => zone.ContainsBase(model.transform.position, model.BaseRadiusInches));
    }

    public int Aeldari11ModifyStratagemCost(SquadController target, string label, int currentCost)
    {
        if (target == null) return currentCost;
        target = target.JoinedActionController();
        if (string.Equals(label, "Command Re-roll", StringComparison.OrdinalIgnoreCase) &&
            AeldariFactionPack11.UnitHasEnhancement(target, "Gift Of Foresight") &&
            !AeldariFactionPack11Runtime.HasUsedThisRound(target.FactionId, "gift_of_foresight"))
        {
            AeldariFactionPack11Runtime.MarkOncePerRound(target.FactionId, "gift_of_foresight");
            return 0;
        }
        return currentCost;
    }

    public bool Aeldari11CanEmbark(SquadController unit)
    {
        return unit == null || !AeldariFactionPack11Runtime.HasFlag(unit, "cannot_embark");
    }

    public bool Aeldari11CanCharge(SquadController unit)
    {
        return unit == null || !AeldariFactionPack11Runtime.HasFlag(unit, "cannot_charge");
    }

    public bool Aeldari11HasRuneOfMistsCover(SquadController target, ModelToken shooter)
    {
        if (target == null || shooter == null || !AeldariFactionPack11Runtime.HasFlag(target, "rune_of_mists")) return false;
        return DistancePointToSquad(shooter.transform.position, target) > 18.001f;
    }
}
