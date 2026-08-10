using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Edition 11 Adeptus Custodes faction-pack integration. The controller uses
/// the existing core board, dice, attack, movement, objective and reserve
/// systems directly. Rules that require a genuine player choice not inferable
/// from board state are surfaced at their timing window instead of guessed.
/// </summary>
public partial class GameController
{
    private sealed class Custodes11DeferredReaction
    {
        public CustodesGameController Faction;
        public GameEventContext Context;
        public List<CustodesStratagem11> Rules;
        public SquadController Unit;
    }

    private readonly Queue<Custodes11DeferredReaction>
        custodes11DeferredReactions =
            new Queue<Custodes11DeferredReaction>();

    private readonly Queue<System.Action>
        custodes11DeferredChoices =
            new Queue<System.Action>();

    private bool custodes11OpeningReaction;

    public void Custodes11OnBattleStarted(
        CustodesGameController faction)
    {
        if (faction == null)
            return;

        foreach (SquadController unit
            in faction.ArmyUnits)
        {
            if (unit == null)
                continue;

            if (CustodesFactionPack11.UnitHasEnhancement(
                    unit,
                    "CASTELLAN’S MARK"))
            {
                Custodes11QueueChoice(
                    "CASTELLAN’S MARK",
                    "After both players have deployed, you can redeploy up to two ADEPTUS CUSTODES units (excluding ANATHEMA PSYKANA), including placing them into Strategic Reserves regardless of the normal reserve limit. Use the normal deployment/reserve tools to resolve the redeployment.",
                    new RuleChoiceOption(
                        "ACKNOWLEDGE",
                        CloseRuleChoice));
            }

            if (CustodesFactionPack11.UnitHasEnhancement(
                    unit,
                    "ENCIRCLING HUNTER"))
            {
                Custodes11QueueChoice(
                    "ENCIRCLING HUNTER",
                    "After both players have deployed, you can redeploy up to three friendly ANATHEMA PSYKANA INFANTRY units, including placing them into Strategic Reserves regardless of the normal reserve limit. Use the normal deployment/reserve tools to resolve this choice.",
                    new RuleChoiceOption(
                        "ACKNOWLEDGE",
                        CloseRuleChoice));
            }

            if (CustodesFactionPack11.UnitHasEnhancementDirect(
                    unit,
                    "AURIC MANTLE"))
            {
                foreach (ModelToken model
                    in unit.LivingModelTokens())
                {
                    if (model != null)
                        model.ApplyFactionMaxWoundsModifier(2);
                }

                AppendBattleLog(
                    "ADEPTUS CUSTODES",
                    "Auric Mantle",
                    unit.DisplayName +
                    " gained +2 Wounds on its bearer model."
                );
            }
        }
    }

    public void Custodes11OnBattleRoundStarted(
        CustodesGameController faction)
    {
        if (faction == null ||
            !faction.DetachmentLocked)
        {
            return;
        }

        if (!faction.HasDetachment(
                CustodesDetachment.ShieldHost))
        {
            CustodesFactionPack11Runtime
                .SetMartialMastery(
                    faction.FactionId,
                    "");
            return;
        }

        Custodes11QueueChoice(
            "SHIELD HOST  -  MARTIAL MASTERY",
            "At the start of the battle round, choose the Martial Mastery effect until the start of the next battle round, or choose neither.",
            new RuleChoiceOption(
                "Critical Hits on unmodified 5+",
                () =>
                {
                    CloseRuleChoice();
                    CustodesFactionPack11Runtime
                        .SetMartialMastery(
                            faction.FactionId,
                            "crit5");
                }),
            new RuleChoiceOption(
                "Improve Martial Ka’tah melee AP by 1",
                () =>
                {
                    CloseRuleChoice();
                    CustodesFactionPack11Runtime
                        .SetMartialMastery(
                            faction.FactionId,
                            "ap");
                }),
            new RuleChoiceOption(
                "Select neither",
                () =>
                {
                    CloseRuleChoice();
                    CustodesFactionPack11Runtime
                        .SetMartialMastery(
                            faction.FactionId,
                            "");
                }));
    }

    public void Custodes11OnTurnStarted(
        CustodesGameController faction,
        GameEventContext context)
    {
        if (faction == null || context == null)
            return;

        if (string.Equals(
                context.ActingFaction,
                faction.FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            // Nulled lasts while the source unit remains relevant; the normal
            // Silent Hunters selection in the Shooting phase replaces it.
        }
    }

    public void Custodes11OnPhaseStarted(
        CustodesGameController faction,
        GameEventContext context)
    {
        if (faction == null ||
            context == null ||
            !faction.DetachmentLocked)
        {
            return;
        }

        bool ownTurn =
            string.Equals(
                context.ActingFaction,
                faction.FactionId,
                StringComparison.OrdinalIgnoreCase);

        if (context.Phase == Phase.Command &&
            ownTurn)
        {
            if (faction.HasDetachment(
                    CustodesDetachment.AuricChampions))
            {
                Custodes11OfferAssemblageTarget(
                    faction);
            }

            Custodes11OfferCommandEnhancements(
                faction);
        }

        if (context.Phase == Phase.Command &&
            !ownTurn &&
            faction.HasDetachment(
                CustodesDetachment.NullMaidenVigil))
        {
            Custodes11ResolveCreepingDread(
                faction);
        }

        if (context.Phase == Phase.Shoot &&
            ownTurn &&
            faction.HasDetachment(
                CustodesDetachment.SilentHunters))
        {
            Custodes11OfferCeaselessVigilance(
                faction);
        }
    }

    public void Custodes11OfferEventRules(
        CustodesGameController faction,
        GameEventContext context)
    {
        if (faction == null ||
            context == null ||
            !faction.DetachmentLocked)
        {
            return;
        }

        Custodes11OfferEnhancementReactions(
            faction,
            context);

        List<CustodesStratagem11> matching =
            CustodesFactionPack11
                .StratagemsFor(
                    faction.FactionId)
                .Where(rule =>
                    Custodes11EventMatches(
                        rule,
                        faction,
                        context))
                .ToList();

        if (matching.Count == 0)
            return;

        SquadController unit =
            Custodes11ReactionUnit(
                faction,
                context);

        matching =
            matching
                .Where(rule =>
                    Custodes11RuleTargetsUnit(
                        rule,
                        unit,
                        context,
                        true))
                .ToList();

        if (matching.Count == 0)
            return;

        Custodes11DeferredReaction request =
            new Custodes11DeferredReaction
            {
                Faction = faction,
                Context = context,
                Rules = matching,
                Unit = unit
            };

        if (showRuleChoiceWindow ||
            interactiveAttack != null ||
            custodes11OpeningReaction)
        {
            custodes11DeferredReactions.Enqueue(
                request);
            return;
        }

        Custodes11OpenReaction(request);
    }

    public void Custodes11PumpDeferredReactions()
    {
        if (showRuleChoiceWindow ||
            interactiveAttack != null ||
            custodes11OpeningReaction)
        {
            return;
        }

        if (custodes11DeferredChoices.Count > 0)
        {
            System.Action choice =
                custodes11DeferredChoices.Dequeue();

            if (choice != null)
                choice();

            return;
        }

        if (custodes11DeferredReactions.Count == 0)
            return;

        Custodes11OpenReaction(
            custodes11DeferredReactions.Dequeue());
    }

    private void Custodes11OpenReaction(
        Custodes11DeferredReaction request)
    {
        if (request == null ||
            request.Rules == null ||
            request.Rules.Count == 0)
        {
            return;
        }

        custodes11OpeningReaction = true;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (CustodesStratagem11 rule
            in request.Rules)
        {
            CustodesStratagem11 captured = rule;

            options.Add(
                new RuleChoiceOption(
                    captured.Name +
                    "  -  " +
                    captured.Cost +
                    " CP",
                    () =>
                    {
                        CloseRuleChoice();

                        Custodes11TryUseStratagem(
                            captured,
                            request.Unit,
                            request.Context,
                            true);
                    }));
        }

        options.Add(
            new RuleChoiceOption(
                "Skip reaction",
                () =>
                {
                    CloseRuleChoice();
                    Custodes11PumpDeferredReactions();
                }));

        OpenRuleChoice(
            "ADEPTUS CUSTODES REACTION",
            request.Unit != null
                ? request.Unit.DisplayName +
                  "  -  choose an available faction rule."
                : "Choose an available Adeptus Custodes faction rule.",
            options.ToArray());

        custodes11OpeningReaction = false;
    }

    public void DrawCustodes11StratagemCards(
        float left,
        float right,
        float y,
        float cardWidth)
    {
        IReadOnlyList<CustodesStratagem11> rules =
            CustodesFactionPack11
                .StratagemsFor(activeFaction);

        for (int i = 0;
             i < rules.Count;
             i++)
        {
            CustodesStratagem11 rule = rules[i];
            bool rightColumn = i % 2 == 1;
            int row = i / 2;

            Rect card =
                new Rect(
                    rightColumn ? right : left,
                    y + row * 54f,
                    cardWidth,
                    42f);

            bool reactive =
                Custodes11IsReactiveOnly(rule);

            if (DrawStratagemActionButton(
                    card,
                    rule.Name +
                    "  -  " +
                    rule.Cost +
                    " CP" +
                    (reactive
                        ? " [REACTIVE]"
                        : ""),
                    rule.FullRule))
            {
                Custodes11TryUseStratagem(
                    rule,
                    selectedSquad,
                    null,
                    reactive);
            }
        }
    }

    public bool Custodes11TryUseStratagem(
        CustodesStratagem11 rule,
        SquadController requestedUnit,
        GameEventContext context,
        bool reactive)
    {
        if (rule == null)
            return false;

        CustodesGameController faction =
            CustodesFactionPack11Runtime
                .Controller(activeFaction);

        if (faction == null &&
            requestedUnit != null)
        {
            faction =
                CustodesFactionPack11Runtime
                    .Controller(
                        requestedUnit.FactionId);
        }

        if (faction == null ||
            !faction.DetachmentLocked ||
            !faction.HasDetachment(
                rule.Detachment))
        {
            status =
                rule.Name +
                ": its detachment is not active for this army.";
            return false;
        }

        SquadController unit =
            requestedUnit != null
            ? requestedUnit.JoinedActionController()
            : selectedSquad != null
                ? selectedSquad.JoinedActionController()
                : Custodes11ReactionUnit(
                    faction,
                    context);

        if (unit == null ||
            !Custodes11RuleTargetsUnit(
                rule,
                unit,
                context,
                reactive))
        {
            status =
                rule.Name +
                ": select a legal target unit first.";
            return false;
        }

        if (!reactive &&
            !Custodes11TimingMatchesCurrent(
                rule,
                unit))
        {
            status =
                rule.Name +
                ": not available at the current timing.";
            return false;
        }

        if (!SpendFactionStratagemCP(
                unit,
                rule.Cost,
                rule.Name))
        {
            return false;
        }

        bool resolved =
            Custodes11ApplyStratagemEffect(
                rule,
                unit,
                context);

        AppendBattleLog(
            "ADEPTUS CUSTODES",
            rule.Name,
            unit.DisplayName +
            " used " +
            rule.Name +
            " (" +
            rule.Cost +
            " CP)." +
            (!resolved
                ? " Resolve the player-choice portion shown in the rules prompt."
                : ""));

        showStratagemMenu = false;
        Custodes11PumpDeferredReactions();
        return true;
    }

    private bool Custodes11ApplyStratagemEffect(
        CustodesStratagem11 rule,
        SquadController unit,
        GameEventContext context)
    {
        string name =
            CustodesFactionPack11.Normalize(
                rule.Name);

        switch (name)
        {
            case "hunt as one":
                CustodesFactionPack11Runtime.SetTurnFlag(unit, "shoot_after_fallback");
                CustodesFactionPack11Runtime.SetTurnFlag(unit, "charge_after_fallback");
                return Custodes11MultiTargetNotice(rule, unit);

            case "talons interlocked":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "talons_interlocked");
                return Custodes11MultiTargetNotice(rule, unit);

            case "empyric severance":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "empyric_severance");
                return true;

            case "emperor s executioners":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "emperor_executioners");
                return Custodes11MultiTargetNotice(rule, unit);

            case "taloned pincer":
                BeginSpecialMove(unit, 6f, rule.Name, null);
                return Custodes11MultiTargetNotice(rule, unit);

            case "shield of honour":
                return Custodes11ManualRule(rule, unit);

            case "arcane genetic alchemy":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "arcane_genetic_alchemy");
                return true;

            case "avenge the fallen":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "avenge_fallen");
                return true;

            case "unwavering sentinels":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "unwavering_sentinels");
                return true;

            case "multipotentiality":
            case "manoeuvre and fire":
                CustodesFactionPack11Runtime.SetTurnFlag(unit, "shoot_after_fallback");
                CustodesFactionPack11Runtime.SetTurnFlag(unit, "charge_after_fallback");
                return true;

            case "vigilance eternal":
                return Custodes11SecureObjective(unit, rule.Name);

            case "archeotech munitions":
                return Custodes11ChooseLethalOrSustained(unit, "archeotech");

            case "desperation s price":
                return Custodes11ManualRule(rule, unit);

            case "witch hunters":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "witch_hunters_only_psyker");
                return Custodes11ChooseLethalOrSustained(unit, "witch_hunters");

            case "anathema blademastery":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "anathema_blademastery");
                return true;

            case "psy chaff volley":
            case "psy chaff volley 1cp":
                return Custodes11SelectProsecutedTarget(unit);

            case "purgation sweep":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "purgation_sweep");
                return true;

            case "psychic abominations":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "psychic_abominations");
                return true;

            case "slayer of champions":
                return Custodes11SelectAssemblageTarget(
                    unit.FactionId,
                    true);

            case "superhuman reserves":
                return Custodes11ManualRule(rule, unit);

            case "the emperor s auspice":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "emperors_auspice");
                return true;

            case "earning of a name":
                return Custodes11ManualRule(rule, unit);

            case "vigil unending":
                return Custodes11ManualRule(rule, unit);

            case "shoulder the mantle":
                return Custodes11ShoulderMantle(unit, rule);

            case "flawless construction":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "flawless_construction");
                return true;

            case "emperor s vengeance":
                return Custodes11ManualRule(rule, unit);

            case "wrathful advance":
                return Custodes11ManualRule(rule, unit);

            case "unstoppable":
            case "unstoppable advance":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "mobile");
                return true;

            case "relentless persecution":
                CustodesFactionPack11Runtime.SetTurnFlag(unit, "shoot_after_advance");
                if (unit.HasKeyword("walker"))
                    CustodesFactionPack11Runtime.SetTurnFlag(unit, "charge_after_advance");
                return true;

            case "punishment inescapable":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "punishment_inescapable");
                return true;

            case "gilded champion":
                return Custodes11ManualRule(rule, unit);

            case "defiant to the last":
                return Custodes11ManualRule(rule, unit);

            case "peerless warrior":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "peerless_warrior");
                return true;

            case "unleash the lions":
                return Custodes11ManualRule(rule, unit);

            case "swift as the eagle":
                Custodes11RollMoveD6Plus(unit, rule.Name, 0);
                return true;

            case "prioritised eradication":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "prioritised_eradication");
                return true;

            case "deathsong scythes":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "deathsong_scythes");
                return true;

            case "umbral prosecution":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "umbral_prosecution");
                return true;

            case "synchronised inferno":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "synchronised_inferno");
                return true;

            case "hardened resolve":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "hardened_resolve");
                return true;

            case "electroexorcist saturation":
                CustodesFactionPack11Runtime.SetPhaseFlag(unit, "electroexorcist_saturation");
                return true;
        }

        return Custodes11ManualRule(rule, unit);
    }

    private bool Custodes11ManualRule(
        CustodesStratagem11 rule,
        SquadController unit)
    {
        QueueTraditionalRuleAlert(
            rule.Name,
            (unit != null
                ? unit.DisplayName + ": "
                : "") +
            rule.FullRule +
            "\nWarboard has spent the CP and recorded the target. Resolve the remaining player-choice/model-placement portion exactly as written.",
            Custodes11SuggestedDice(rule));

        return false;
    }

    private bool Custodes11MultiTargetNotice(
        CustodesStratagem11 rule,
        SquadController first)
    {
        string target =
            rule.Target ?? "";

        if (target.IndexOf(
                "Up to two",
                StringComparison.OrdinalIgnoreCase) < 0)
        {
            return true;
        }

        QueueTraditionalRuleAlert(
            rule.Name,
            first.DisplayName +
            " is the first selected unit. The Stratagem can select a second unit only if its listed restriction is satisfied. If using a second unit, select it and apply the same temporary effect manually; Warboard will not guess the paired unit.",
            0);

        return true;
    }

    private bool Custodes11ChooseLethalOrSustained(
        SquadController unit,
        string prefix)
    {
        if (unit == null)
            return false;

        Custodes11QueueChoice(
            prefix == "archeotech"
                ? "ARCHEOTECH MUNITIONS"
                : "WITCH HUNTERS",
            "Select the weapon ability for " +
            unit.DisplayName +
            " until the end of the phase.",
            new RuleChoiceOption(
                "LETHAL HITS",
                () =>
                {
                    CloseRuleChoice();
                    CustodesFactionPack11Runtime.SetPhaseFlag(
                        unit,
                        prefix + "_lethal");
                }),
            new RuleChoiceOption(
                "SUSTAINED HITS 1",
                () =>
                {
                    CloseRuleChoice();
                    CustodesFactionPack11Runtime.SetPhaseFlag(
                        unit,
                        prefix + "_sustained");
                }));

        return true;
    }

    private bool Custodes11SecureObjective(
        SquadController unit,
        string label)
    {
        ObjectiveController objective =
            objectives.FirstOrDefault(value =>
                value != null &&
                value.UnitWithinRange(unit) &&
                value.Controller(squads) ==
                    unit.FactionId);

        if (objective == null)
            return false;

        objective.SecureFor(unit.FactionId);
        RefreshObjectiveDisplays();

        status =
            label +
            ": objective remains under " +
            DisplayFactionName(unit.FactionId) +
            " control until the opponent takes it at the start or end of a turn.";

        return true;
    }

    private bool Custodes11SelectProsecutedTarget(
        SquadController source)
    {
        if (source == null)
            return false;

        List<SquadController> enemies =
            squads
                .Where(enemy =>
                    enemy != null &&
                    enemy.IsAlive &&
                    enemy.IsOnBattlefield &&
                    !enemy.IsAttachedLeader &&
                    enemy.FactionId !=
                        source.FactionId)
                .ToList();

        if (enemies.Count == 0)
            return false;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController enemy in enemies)
        {
            SquadController captured =
                enemy.JoinedActionController();

            options.Add(
                new RuleChoiceOption(
                    "Prosecute " +
                    captured.DisplayName,
                    () =>
                    {
                        CloseRuleChoice();
                        CustodesFactionPack11Runtime.SetProsecuted(
                            captured,
                            source.FactionId);
                    }));
        }

        Custodes11QueueChoice(
            "PSY-CHAFF VOLLEY",
            "Select the enemy unit that was hit by the Prosecutors. The selected unit is prosecuted until the start of your next turn while the Prosecutors remain on the battlefield.",
            options.ToArray());

        return true;
    }

    private bool Custodes11ShoulderMantle(
        SquadController character,
        CustodesStratagem11 rule)
    {
        if (character == null ||
            !character.HasKeyword("character") ||
            character.IsAttachedLeader)
        {
            return false;
        }

        List<SquadController> candidates =
            squads
                .Where(unit =>
                    unit != null &&
                    unit.IsAlive &&
                    unit.IsOnBattlefield &&
                    !unit.IsAttachedLeader &&
                    unit.FactionId ==
                        character.FactionId &&
                    !unit.IsBattleShocked &&
                    unit != character &&
                    JoinedDistance(
                        character,
                        unit) <= 2.001f &&
                    character.CanAttachTo(unit))
                .ToList();

        if (candidates.Count == 0)
            return false;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController bodyguard in candidates)
        {
            SquadController captured = bodyguard;

            options.Add(
                new RuleChoiceOption(
                    "Lead " +
                    captured.DisplayName,
                    () =>
                    {
                        CloseRuleChoice();
                        character.AttachToBodyguard(captured);
                        status =
                            "SHOULDER THE MANTLE: " +
                            character.DisplayName +
                            " attached to " +
                            captured.DisplayName +
                            ". Starting Strength updates through the attached-unit core state.";
                    }));
        }

        Custodes11QueueChoice(
            rule.Name,
            "Select the eligible unit this CHARACTER will attach to.",
            options.ToArray());

        return true;
    }

    public bool Custodes11EnsureKatahChoice(
        SquadController attacker,
        SquadController target)
    {
        if (attacker == null ||
            !CustodesFactionPack11.IsCustodes(attacker) ||
            !CustodesFactionPack11.HasMartialKatah(attacker))
        {
            return false;
        }

        attacker = attacker.JoinedActionController();

        if (!string.IsNullOrWhiteSpace(
                CustodesFactionPack11Runtime.Katah(attacker)))
        {
            return false;
        }

        OpenRuleChoice(
            "MARTIAL KA’TAH  -  " +
            attacker.DisplayName,
            "Each time this unit is selected to fight, choose one Ka’tah Stance until it has finished making its attacks.",
            new[]
            {
                new RuleChoiceOption(
                    "DACATARAI  -  SUSTAINED HITS 1",
                    () =>
                    {
                        CloseRuleChoice();
                        CustodesFactionPack11Runtime.SetKatah(
                            attacker,
                            "dacatarai");
                        TryFight(attacker, target);
                    }),
                new RuleChoiceOption(
                    "RENDAX  -  LETHAL HITS",
                    () =>
                    {
                        CloseRuleChoice();
                        CustodesFactionPack11Runtime.SetKatah(
                            attacker,
                            "rendax");
                        TryFight(attacker, target);
                    })
            });

        return true;
    }

    public void Custodes11OfferKatah(
        SquadController unit)
    {
        // TryFight owns the actionable stance prompt so it can resume the
        // exact declared target after the choice. This event hook is kept for
        // out-of-turn/forced fight paths and only surfaces a reminder if they
        // bypass TryFight.
        if (unit == null ||
            !CustodesFactionPack11.HasMartialKatah(unit) ||
            !string.IsNullOrWhiteSpace(
                CustodesFactionPack11Runtime.Katah(unit)))
        {
            return;
        }

        status =
            unit.DisplayName +
            " has Martial Ka’tah: choose Dacatarai or Rendax when its fight is declared.";
    }

    public bool Custodes11OfferHammerFallsChargeReroll(
        SquadController attacker,
        SquadController target,
        int roll,
        bool wasRerolled)
    {
        if (attacker == null ||
            target == null ||
            wasRerolled ||
            !CustodesFactionPack11.CanRerollCharge(attacker))
        {
            return false;
        }

        attacker = attacker.JoinedActionController();

        if (IsXcomMode)
        {
            OpenRuleChoice(
                "THE HAMMER FALLS  -  CHARGE RE-ROLL",
                attacker.DisplayName +
                " made an ingress move this turn and can re-roll its Charge roll of " +
                roll +
                ".",
                new[]
                {
                    new RuleChoiceOption(
                        "Keep " + roll,
                        () =>
                        {
                            CloseRuleChoice();
                            ResolveChargeRoll(
                                attacker,
                                target,
                                roll,
                                true,
                                roll);
                        }),
                    new RuleChoiceOption(
                        "Re-roll 2D6",
                        () =>
                        {
                            CloseRuleChoice();
                            int reroll =
                                DiceRoller.RollD6("The Hammer Falls") +
                                DiceRoller.RollD6("The Hammer Falls");
                            ResolveChargeRoll(
                                attacker,
                                target,
                                reroll,
                                true,
                                roll);
                        })
                });

            return true;
        }

        OpenRuleChoice(
            "THE HAMMER FALLS  -  CHARGE RE-ROLL",
            attacker.DisplayName +
            " can re-roll its Charge roll of " +
            roll +
            " because it made an ingress move this turn.",
            new[]
            {
                new RuleChoiceOption(
                    "Keep " + roll,
                    () =>
                    {
                        CloseRuleChoice();
                        ResolveChargeRoll(
                            attacker,
                            target,
                            roll,
                            true,
                            roll);
                    }),
                new RuleChoiceOption(
                    "Enter re-roll result",
                    () =>
                    {
                        CloseRuleChoice();
                        OpenTraditionalNumericPrompt(
                            "THE HAMMER FALLS",
                            "Enter the re-rolled 2D6 Charge result.",
                            2,
                            12,
                            7,
                            1,
                            value =>
                                ResolveChargeRoll(
                                    attacker,
                                    target,
                                    value,
                                    true,
                                    roll));
                    })
            });

        return true;
    }

    public void Custodes11AfterSuccessfulCharge(
        SquadController unit)
    {
        if (unit == null)
            return;

        SquadController bearerUnit =
            unit.JoinedActionController();

        if (!CustodesFactionPack11.UnitHasEnhancement(
                bearerUnit,
                "BLADE IMPERATOR"))
        {
            return;
        }

        List<SquadController> enemies =
            squads
                .Where(enemy =>
                    enemy != null &&
                    enemy.IsAlive &&
                    enemy.IsOnBattlefield &&
                    enemy.FactionId !=
                        bearerUnit.FactionId &&
                    UnitsAreEngaged(
                        bearerUnit,
                        enemy))
                .ToList();

        if (enemies.Count == 0)
            return;

        SquadController target = enemies[0];

        System.Action resolveImpact = () =>
        {
            if (!IsXcomMode)
            {
                QueueTraditionalRuleAlert(
                    "BLADE IMPERATOR",
                    "Select one enemy unit within Engagement Range of the bearer and roll one D6; on a 4+, it suffers D3 mortal wounds.",
                    1);
                return;
            }

            int trigger =
                DiceRoller.RollD6(
                    "Blade Imperator");

            if (trigger >= 4)
            {
                int d3 =
                    (DiceRoller.RollD6(
                        "Blade Imperator mortal wounds") +
                     1) / 2;

                Core11ApplyMortalWounds(
                    target,
                    d3,
                    "Blade Imperator");
            }
        };

        resolveImpact();

        string onceKey =
            "blade_imperator_shock:" +
            bearerUnit.UnitId;

        if (CustodesFactionPack11Runtime
            .HasUsedThisBattle(
                bearerUnit.FactionId,
                onceKey))
        {
            return;
        }

        Custodes11QueueChoice(
            "BLADE IMPERATOR  -  ONCE PER BATTLE",
            "After this Charge move, all enemy units within 6 inches of the bearer can be forced to take a Battle-shock test. Use the once-per-battle effect now?",
            new RuleChoiceOption(
                "Use Battle-shock pulse",
                () =>
                {
                    CloseRuleChoice();

                    if (!CustodesFactionPack11Runtime
                        .MarkOncePerBattle(
                            bearerUnit.FactionId,
                            onceKey))
                    {
                        return;
                    }

                    foreach (SquadController enemy
                        in squads.Where(enemy =>
                            enemy != null &&
                            enemy.IsAlive &&
                            enemy.IsOnBattlefield &&
                            enemy.FactionId !=
                                bearerUnit.FactionId &&
                            JoinedDistance(
                                bearerUnit,
                                enemy) <= 6.001f))
                    {
                        Custodes11BattleShock(
                            enemy,
                            0,
                            "Blade Imperator");
                    }
                }),
            new RuleChoiceOption(
                "Save it",
                CloseRuleChoice));
    }

    private void Custodes11OfferAssemblageTarget(
        CustodesGameController faction)
    {
        Custodes11SelectAssemblageTarget(
            faction.FactionId,
            false);
    }

    private bool Custodes11SelectAssemblageTarget(
        string factionId,
        bool fromSlayer)
    {
        List<SquadController> enemies =
            squads
                .Where(unit =>
                    unit != null &&
                    unit.IsAlive &&
                    unit.IsOnBattlefield &&
                    !unit.IsAttachedLeader &&
                    unit.FactionId != factionId)
                .ToList();

        if (enemies.Count == 0)
            return false;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController enemy in enemies)
        {
            SquadController captured =
                enemy.JoinedActionController();

            options.Add(
                new RuleChoiceOption(
                    captured.DisplayName,
                    () =>
                    {
                        CloseRuleChoice();
                        CustodesFactionPack11Runtime
                            .SetAssemblageTarget(
                                factionId,
                                captured);

                        status =
                            "ASSEMBLAGE OF MIGHT: ADEPTUS CUSTODES CHARACTER models gain +1 to Wound against " +
                            captured.DisplayName +
                            " until the start of the next Command phase.";
                    }));
        }

        Custodes11QueueChoice(
            fromSlayer
                ? "SLAYER OF CHAMPIONS  -  NEW QUARRY"
                : "AURIC CHAMPIONS  -  ASSEMBLAGE OF MIGHT",
            "Select the enemy unit to mark.",
            options.ToArray());

        return true;
    }

    private void Custodes11ResolveCreepingDread(
        CustodesGameController faction)
    {
        if (faction == null)
            return;

        List<SquadController> anathema =
            faction.ArmyUnits
                .Where(unit =>
                    unit != null &&
                    unit.IsAlive &&
                    unit.IsOnBattlefield &&
                    unit.HasKeyword(
                        "anathema psykana"))
                .ToList();

        if (anathema.Count == 0)
            return;

        foreach (SquadController enemy
            in squads)
        {
            if (enemy == null ||
                !enemy.IsAlive ||
                !enemy.IsOnBattlefield ||
                enemy.IsAttachedLeader ||
                enemy.FactionId ==
                    faction.FactionId)
            {
                continue;
            }

            bool inAura =
                anathema.Any(source =>
                    JoinedDistance(
                        source,
                        enemy) <= 12.001f);

            if (!inAura)
                continue;

            bool belowStart =
                CustodesFactionPack11
                    .IsBelowStartingStrength(enemy);

            if (!enemy.HasKeyword("psyker") &&
                !belowStart)
            {
                continue;
            }

            int modifier =
                enemy.IsAtOrBelowHalfStrength()
                ? -1
                : 0;

            Custodes11BattleShock(
                enemy,
                modifier,
                "Creeping Dread");
        }
    }

    private void Custodes11OfferCeaselessVigilance(
        CustodesGameController faction)
    {
        foreach (SquadController source
            in faction.ArmyUnits)
        {
            if (source == null ||
                !source.IsAlive ||
                !source.IsOnBattlefield ||
                !source.HasKeyword(
                    "anathema psykana"))
            {
                continue;
            }

            SquadController capturedSource =
                source.JoinedActionController();

            List<SquadController> targets =
                squads
                    .Where(enemy =>
                        enemy != null &&
                        enemy.IsAlive &&
                        enemy.IsOnBattlefield &&
                        !enemy.IsAttachedLeader &&
                        enemy.FactionId !=
                            capturedSource.FactionId &&
                        JoinedDistance(
                            capturedSource,
                            enemy) <= 12.001f &&
                        capturedSource
                            .JoinedLivingModelTokens()
                            .Any(model =>
                                ModelCanSeeUnit(
                                    model,
                                    enemy)))
                    .ToList();

            if (targets.Count == 0)
                continue;

            List<RuleChoiceOption> options =
                new List<RuleChoiceOption>();

            foreach (SquadController enemy in targets)
            {
                SquadController capturedTarget =
                    enemy.JoinedActionController();

                options.Add(
                    new RuleChoiceOption(
                        "Null " +
                        capturedTarget.DisplayName,
                        () =>
                        {
                            CloseRuleChoice();
                            CustodesFactionPack11Runtime
                                .SetNulled(
                                    capturedTarget,
                                    true);
                        }));
            }

            options.Add(
                new RuleChoiceOption(
                    "Skip for " +
                    capturedSource.DisplayName,
                    CloseRuleChoice));

            Custodes11QueueChoice(
                "CEASELESS VIGILANCE  -  " +
                capturedSource.DisplayName,
                "Select one visible enemy unit within 12 inches. It is nulled and has +3 inches detection range.",
                options.ToArray());
        }
    }

    private void Custodes11OfferCommandEnhancements(
        CustodesGameController faction)
    {
        foreach (SquadController unit
            in faction.ArmyUnits)
        {
            if (unit == null ||
                !unit.IsAlive ||
                !unit.IsOnBattlefield)
            {
                continue;
            }

            SquadController captured =
                unit.JoinedActionController();

            if (CustodesFactionPack11.UnitHasEnhancement(
                    captured,
                    "HUNTRESS’ EYE"))
            {
                List<SquadController> targets =
                    squads.Where(enemy =>
                        enemy != null &&
                        enemy.IsAlive &&
                        enemy.IsOnBattlefield &&
                        enemy.FactionId !=
                            captured.FactionId &&
                        JoinedDistance(
                            captured,
                            enemy) <= 12.001f)
                    .ToList();

                if (targets.Count > 0)
                {
                    List<RuleChoiceOption> options =
                        new List<RuleChoiceOption>();

                    foreach (SquadController enemy
                        in targets)
                    {
                        SquadController target =
                            enemy.JoinedActionController();

                        options.Add(
                            new RuleChoiceOption(
                                target.DisplayName,
                                () =>
                                {
                                    CloseRuleChoice();
                                    Custodes11BattleShock(
                                        target,
                                        0,
                                        "Huntress’ Eye");
                                }));
                    }

                    options.Add(
                        new RuleChoiceOption(
                            "Skip",
                            CloseRuleChoice));

                    Custodes11QueueChoice(
                        "HUNTRESS’ EYE",
                        "Select one enemy unit within 12 inches to take a Battle-shock test.",
                        options.ToArray());
                }
            }

            if (CustodesFactionPack11.UnitHasEnhancement(
                    captured,
                    "VETERAN OF THE KATAPHRAKTOI"))
            {
                List<SquadController> targets =
                    faction.ArmyUnits
                        .Where(target =>
                            target != null &&
                            target.IsAlive &&
                            target.IsOnBattlefield &&
                            (target.HasKeyword("vehicle") ||
                             target.HasKeyword("mounted")) &&
                            JoinedDistance(
                                captured,
                                target) <= 6.001f)
                        .ToList();

                if (targets.Count > 0)
                {
                    List<RuleChoiceOption> options =
                        new List<RuleChoiceOption>();

                    foreach (SquadController target
                        in targets)
                    {
                        SquadController selected =
                            target.JoinedActionController();

                        options.Add(
                            new RuleChoiceOption(
                                selected.DisplayName,
                                () =>
                                {
                                    CloseRuleChoice();
                                    CustodesFactionPack11Runtime
                                        .SetRoundFlag(
                                            selected,
                                            "veteran_kataphraktoi");
                                }));
                    }

                    options.Add(
                        new RuleChoiceOption(
                            "Skip",
                            CloseRuleChoice));

                    Custodes11QueueChoice(
                        "VETERAN OF THE KATAPHRAKTOI",
                        "Select one ADEPTUS CUSTODES VEHICLE or MOUNTED unit within 6 inches. It can shoot after Falling Back until the start of your next Command phase.",
                        options.ToArray());
                }
            }

            if (CustodesFactionPack11.UnitHasEnhancement(
                    captured,
                    "INSPIRATIONAL EXEMPLAR"))
            {
                string key =
                    "inspirational_exemplar:" +
                    captured.UnitId;

                List<SquadController> shocked =
                    faction.ArmyUnits
                        .Where(target =>
                            target != null &&
                            target.IsAlive &&
                            target.IsOnBattlefield &&
                            target.IsBattleShocked &&
                            JoinedDistance(
                                captured,
                                target) <= 12.001f)
                        .ToList();

                if (shocked.Count > 0 &&
                    !CustodesFactionPack11Runtime
                        .HasUsedThisBattle(
                            captured.FactionId,
                            key))
                {
                    List<RuleChoiceOption> options =
                        new List<RuleChoiceOption>();

                    foreach (SquadController target
                        in shocked)
                    {
                        SquadController selected =
                            target.JoinedActionController();

                        options.Add(
                            new RuleChoiceOption(
                                "Steady " +
                                selected.DisplayName,
                                () =>
                                {
                                    CloseRuleChoice();
                                    if (CustodesFactionPack11Runtime
                                        .MarkOncePerBattle(
                                            captured.FactionId,
                                            key))
                                    {
                                        selected.SetBattleShocked(
                                            false,
                                            0);
                                        RefreshObjectiveDisplays();
                                    }
                                }));
                    }

                    options.Add(
                        new RuleChoiceOption(
                            "Save it",
                            CloseRuleChoice));

                    Custodes11QueueChoice(
                        "INSPIRATIONAL EXEMPLAR  -  ONCE PER BATTLE",
                        "Select a Battle-shocked friendly ADEPTUS CUSTODES unit within 12 inches to cease being Battle-shocked.",
                        options.ToArray());
                }
            }

            if (CustodesFactionPack11.UnitHasEnhancement(
                    captured,
                    "VEILED BLADE") &&
                !CustodesFactionPack11Runtime
                    .HasUsedThisBattle(
                        captured.FactionId,
                        "veiled_blade:" +
                        captured.UnitId))
            {
                string key =
                    "veiled_blade:" +
                    captured.UnitId;

                Custodes11QueueChoice(
                    "VEILED BLADE  -  ONCE PER BATTLE",
                    "Triple the bearer’s Objective Control until the end of this turn?",
                    new RuleChoiceOption(
                        "Use Veiled Blade",
                        () =>
                        {
                            CloseRuleChoice();
                            if (CustodesFactionPack11Runtime
                                .MarkOncePerBattle(
                                    captured.FactionId,
                                    key))
                            {
                                CustodesFactionPack11Runtime
                                    .SetTurnFlag(
                                        captured,
                                        "veiled_blade_triple_oc");
                            }
                        }),
                    new RuleChoiceOption(
                        "Save it",
                        CloseRuleChoice));
            }
        }
    }

    private void Custodes11OfferEnhancementReactions(
        CustodesGameController faction,
        GameEventContext context)
    {
        if (faction == null || context == null)
            return;

        if (context.Type == GameEventType.MoveEnded &&
            context.Source != null &&
            context.Source.FactionId !=
                faction.FactionId)
        {
            foreach (SquadController unit
                in faction.ArmyUnits)
            {
                if (unit == null ||
                    !unit.IsAlive ||
                    !unit.IsOnBattlefield ||
                    IsEngaged(unit) ||
                    !CustodesFactionPack11.UnitHasEnhancement(
                        unit,
                        "MARTIAL PHILOSOPHER") ||
                    CustodesFactionPack11Runtime
                        .HasUsedThisBattle(
                            faction.FactionId,
                            "martial_philosopher:" +
                            unit.UnitId) ||
                    JoinedDistance(
                        unit,
                        context.Source) > 8.001f)
                {
                    continue;
                }

                SquadController captured =
                    unit.JoinedActionController();

                Custodes11QueueChoice(
                    "MARTIAL PHILOSOPHER  -  ONCE PER BATTLE",
                    context.Source.DisplayName +
                    " ended a move within 8 inches of " +
                    captured.DisplayName +
                    ". Make the Normal move of up to 6 inches?",
                    new RuleChoiceOption(
                        "Move up to 6 inches",
                        () =>
                        {
                            CloseRuleChoice();
                            if (CustodesFactionPack11Runtime
                                .MarkOncePerBattle(
                                    faction.FactionId,
                                    "martial_philosopher:" +
                                    captured.UnitId))
                            {
                                BeginSpecialMove(
                                    captured,
                                    6f,
                                    "Martial Philosopher",
                                    null);
                            }
                        }),
                    new RuleChoiceOption(
                        "Skip",
                        CloseRuleChoice));
            }
        }

        if (context.Type == GameEventType.AttackResolved &&
            context.Target != null &&
            context.Target.FactionId ==
                faction.FactionId &&
            context.Target.HasKeyword("terminator") &&
            CustodesFactionPack11.UnitHasEnhancement(
                context.Target,
                "EFFICIENT AGGRESSION") &&
            !CustodesFactionPack11Runtime
                .HasUsedThisTurn(
                    faction.FactionId,
                    "efficient_aggression"))
        {
            SquadController unit =
                context.Target.JoinedActionController();

            Custodes11QueueChoice(
                "EFFICIENT AGGRESSION",
                "If this TERMINATOR unit lost a wound as a result of the enemy Shooting attacks, it can make a surge move of up to D6+1 inches. Use the move only if the wound-loss condition was satisfied.",
                new RuleChoiceOption(
                    "Resolve D6+1 surge move",
                    () =>
                    {
                        CloseRuleChoice();
                        if (CustodesFactionPack11Runtime
                            .MarkOncePerTurn(
                                faction.FactionId,
                                "efficient_aggression"))
                        {
                            Custodes11RollMoveD6Plus(
                                unit,
                                "Efficient Aggression",
                                1);
                        }
                    }),
                new RuleChoiceOption(
                    "Not eligible / skip",
                    CloseRuleChoice));
        }
    }

    private void Custodes11BattleShock(
        SquadController target,
        int modifier,
        string label)
    {
        if (target == null)
            return;

        if (!IsXcomMode)
        {
            QueueTraditionalRuleAlert(
                label,
                target.DisplayName +
                " must take a Battle-shock test" +
                (modifier != 0
                    ? " with " +
                      modifier +
                      " to the test"
                    : "") +
                ". Resolve 2D6 and mark the result using the existing Battle-shock controls.",
                2);
            return;
        }

        int roll =
            DiceRoller.RollD6(label) +
            DiceRoller.RollD6(label) +
            modifier;

        target.SetBattleShocked(
            roll < target.BestLeadership(),
            roll);

        RefreshObjectiveDisplays();
    }

    private void Custodes11RollMoveD6Plus(
        SquadController unit,
        string label,
        int flatBonus)
    {
        if (unit == null)
            return;

        if (IsXcomMode)
        {
            int value =
                DiceRoller.RollD6(label) +
                flatBonus;

            BeginSpecialMove(
                unit,
                value,
                label,
                null);
            return;
        }

        OpenTraditionalNumericPrompt(
            label,
            "Roll D6" +
            (flatBonus != 0
                ? "+" + flatBonus
                : "") +
            " for the move distance.",
            1 + flatBonus,
            6 + flatBonus,
            3 + flatBonus,
            1,
            value =>
                BeginSpecialMove(
                    unit,
                    value,
                    label,
                    null));
    }

    private bool Custodes11EventMatches(
        CustodesStratagem11 rule,
        CustodesGameController faction,
        GameEventContext context)
    {
        if (rule == null ||
            faction == null ||
            context == null)
        {
            return false;
        }

        string when =
            (rule.When ?? "")
                .ToLowerInvariant();

        bool ownTurn =
            string.Equals(
                context.ActingFaction,
                faction.FactionId,
                StringComparison.OrdinalIgnoreCase);

        switch (context.Type)
        {
            case GameEventType.AttackStarted:
                return
                    !ownTurn &&
                    (when.Contains("selected its targets") ||
                     when.Contains("targets a friendly"));

            case GameEventType.UnitFinishedShooting:
                return
                    (ownTurn &&
                     (when.Contains("has just shot") ||
                      when.Contains("selected to shoot"))) ||
                    (!ownTurn &&
                     when.Contains("enemy unit has shot"));

            case GameEventType.UnitSelectedToMove:
                return
                    ownTurn &&
                    when.Contains("selected to move");

            case GameEventType.UnitAdvanced:
                return
                    ownTurn &&
                    when.Contains("advances");

            case GameEventType.UnitFellBack:
                return
                    (ownTurn &&
                     when.Contains("falls back")) ||
                    (!ownTurn &&
                     when.Contains("enemy unit") &&
                     when.Contains("fall back"));

            case GameEventType.MoveEnded:
                return
                    !ownTurn &&
                    when.Contains("enemy unit") &&
                    (when.Contains("normal") ||
                     when.Contains("advance") ||
                     when.Contains("fall back"));

            case GameEventType.UnitSelectedToFight:
                return
                    when.Contains("selected to fight") ||
                    when.Contains("piles in");

            case GameEventType.ChargeRolled:
                return
                    ownTurn &&
                    when.Contains("charge");

            case GameEventType.UnitDestroyed:
            case GameEventType.ModelDestroyed:
                return
                    when.Contains("destroyed");

            case GameEventType.PhaseStarted:
                return
                    (context.Phase == Phase.Move &&
                     ownTurn &&
                     when.Contains("start of your movement")) ||
                    (context.Phase == Phase.Fight &&
                     when.Contains("start of the fight"));
        }

        return false;
    }

    private SquadController Custodes11ReactionUnit(
        CustodesGameController faction,
        GameEventContext context)
    {
        if (faction == null)
            return null;

        if (context != null &&
            context.Target != null &&
            context.Target.FactionId ==
                faction.FactionId)
        {
            return context.Target.JoinedActionController();
        }

        if (context != null &&
            context.Source != null &&
            context.Source.FactionId ==
                faction.FactionId)
        {
            return context.Source.JoinedActionController();
        }

        if (selectedSquad != null &&
            selectedSquad.FactionId ==
                faction.FactionId)
        {
            return selectedSquad.JoinedActionController();
        }

        return faction.ArmyUnits
            .FirstOrDefault(unit =>
                unit != null &&
                unit.IsAlive &&
                unit.IsOnBattlefield &&
                !unit.IsAttachedLeader);
    }

    private bool Custodes11TimingMatchesCurrent(
        CustodesStratagem11 rule,
        SquadController unit)
    {
        if (rule == null ||
            unit == null)
        {
            return false;
        }

        string when =
            (rule.When ?? "")
                .ToLowerInvariant();

        if (when.Contains("opponent") &&
            string.Equals(
                activeFaction,
                unit.FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (when.Contains("command phase") &&
            phase != Phase.Command)
        {
            return false;
        }

        if (when.Contains("movement phase") &&
            phase != Phase.Move)
        {
            return false;
        }

        if (when.Contains("shooting phase") &&
            phase != Phase.Shoot)
        {
            return false;
        }

        if (when.Contains("charge phase") &&
            phase != Phase.Charge)
        {
            return false;
        }

        if (when.Contains("fight phase") &&
            phase != Phase.Fight)
        {
            return false;
        }

        if (Custodes11IsReactiveOnly(rule))
            return false;

        return true;
    }

    private bool Custodes11IsReactiveOnly(
        CustodesStratagem11 rule)
    {
        string when =
            (rule != null
                ? rule.When
                : "") ?? "";

        when = when.ToLowerInvariant();

        return
            when.Contains("just after") ||
            when.Contains("when an enemy") ||
            when.Contains("when a friendly") ||
            when.Contains("when an adeptus") ||
            when.Contains("when a model") ||
            when.Contains("was just destroyed") ||
            when.Contains("has just destroyed") ||
            when.Contains("just shot");
    }

    private bool Custodes11RuleTargetsUnit(
        CustodesStratagem11 rule,
        SquadController unit,
        GameEventContext context,
        bool reactive)
    {
        if (rule == null ||
            unit == null)
        {
            return false;
        }

        if (!unit.IsAlive &&
            (rule.Target ?? "").IndexOf(
                "destroyed",
                StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        string target =
            (rule.Target ?? "")
                .ToLowerInvariant();

        if (target.Contains("adeptus custodes") &&
            !CustodesFactionPack11.IsCustodes(unit))
        {
            return false;
        }

        if (target.Contains("anathema psykana") &&
            !target.Contains("excluding anathema") &&
            !unit.HasKeyword("anathema psykana"))
        {
            return false;
        }

        if (target.Contains("excluding anathema psykana") &&
            unit.HasKeyword("anathema psykana"))
        {
            return false;
        }

        if (target.Contains("infantry") &&
            !unit.HasKeyword("infantry"))
        {
            return false;
        }

        if (target.Contains("vehicle") &&
            !target.Contains("vehicle or mounted") &&
            !unit.HasKeyword("vehicle"))
        {
            return false;
        }

        if (target.Contains("vehicle or mounted") &&
            !(unit.HasKeyword("vehicle") ||
              unit.HasKeyword("mounted")))
        {
            return false;
        }

        if (target.Contains("walker") &&
            !unit.HasKeyword("walker"))
        {
            return false;
        }

        if (target.Contains("terminator") &&
            !unit.HasKeyword("terminator"))
        {
            return false;
        }

        if (target.Contains("character") &&
            !unit.HasKeyword("character"))
        {
            return false;
        }

        if (target.Contains("vigilators") &&
            !CustodesFactionPack11.NameOrKeyword(
                unit,
                "Vigilators"))
        {
            return false;
        }

        if (target.Contains("prosecutors") &&
            !CustodesFactionPack11.NameOrKeyword(
                unit,
                "Prosecutors"))
        {
            return false;
        }

        if (target.Contains("witchseekers") &&
            !CustodesFactionPack11.NameOrKeyword(
                unit,
                "Witchseekers"))
        {
            return false;
        }

        if (target.Contains("telemon heavy dreadnought") &&
            !CustodesFactionPack11.NameOrKeyword(
                unit,
                "Telemon Heavy Dreadnought"))
        {
            return false;
        }

        if ((target.Contains("allarus custodians") ||
             target.Contains("aquilon custodians")) &&
            !(CustodesFactionPack11.NameOrKeyword(
                  unit,
                  "Allarus Custodians") ||
              CustodesFactionPack11.NameOrKeyword(
                  unit,
                  "Aquilon Custodians")))
        {
            return false;
        }

        if (target.Contains("has not been selected to shoot") &&
            unit.HasShot)
        {
            return false;
        }

        if (target.Contains("has not been selected to fight") &&
            unit.HasFought)
        {
            return false;
        }

        if (target.Contains("has not been selected to move") &&
            unit.HasMoved)
        {
            return false;
        }

        if (target.Contains("fell back") &&
            !unit.HasFallenBack)
        {
            return false;
        }

        if (target.Contains("below its starting strength") &&
            !CustodesFactionPack11
                .IsBelowStartingStrength(unit))
        {
            return false;
        }

        if (target.Contains("within range of an objective marker you control"))
        {
            bool onControlled =
                objectives.Any(objective =>
                    objective != null &&
                    objective.UnitWithinRange(unit) &&
                    objective.Controller(squads) ==
                        unit.FactionId);

            if (!onControlled)
                return false;
        }

        return true;
    }

    private int Custodes11SuggestedDice(
        CustodesStratagem11 rule)
    {
        string text =
            ((rule != null
                ? rule.Effect
                : "") ?? "")
                .ToLowerInvariant();

        if (text.Contains("d6+1"))
            return 1;

        if (text.Contains("d3"))
            return 1;

        if (text.Contains("one d6") ||
            text.Contains("roll one d6"))
        {
            return 1;
        }

        if (text.Contains("battle-shock") ||
            text.Contains("leadership test"))
        {
            return 2;
        }

        return 0;
    }

    private void Custodes11QueueChoice(
        string title,
        string description,
        params RuleChoiceOption[] options)
    {
        System.Action open =
            () => OpenRuleChoice(
                title,
                description,
                options);

        if (!showRuleChoiceWindow &&
            interactiveAttack == null &&
            !custodes11OpeningReaction)
        {
            open();
            return;
        }

        custodes11DeferredChoices.Enqueue(open);
    }

    public int Custodes11ModifyStratagemCost(
        SquadController target,
        string label,
        int currentCost)
    {
        return CustodesFactionPack11
            .ModifyStratagemCost(
                target,
                label,
                currentCost);
    }

    public bool Custodes11CanShootAfterFallBack(
        SquadController unit)
    {
        return CustodesFactionPack11
            .CanShootAfterFallBack(unit);
    }

    public bool Custodes11CanChargeAfterFallBack(
        SquadController unit)
    {
        return CustodesFactionPack11
            .CanChargeAfterFallBack(unit);
    }

    public bool Custodes11CanShootAfterAdvance(
        SquadController unit)
    {
        return CustodesFactionPack11
            .CanShootAfterAdvance(unit);
    }

    public bool Custodes11CanChargeAfterAdvance(
        SquadController unit)
    {
        return CustodesFactionPack11
            .CanChargeAfterAdvance(unit);
    }

    public bool Custodes11CanAttackTarget(
        SquadController attacker,
        SquadController target,
        AttackMode mode,
        out string reason)
    {
        reason = "";

        if (attacker == null ||
            target == null ||
            !CustodesFactionPack11.IsCustodes(attacker))
        {
            return true;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "witch_hunters_only_psyker") &&
            !target.HasKeyword("psyker"))
        {
            reason =
                "Witch Hunters: this unit can only target PSYKER units for the rest of the phase.";
            return false;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "talons_interlocked_target_lock"))
        {
            // Exact multi-unit enemy selection is surfaced by the rule prompt;
            // no target is guessed here.
        }

        if (mode == AttackMode.Ranged)
        {
            return CustodesFactionPack11
                .CanBeRangedTarget(
                    this,
                    attacker,
                    target,
                    out reason);
        }

        return true;
    }
}
