using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Edition 11 Necrons gameplay integration. This partial only orchestrates
/// player choices and core-engine entry points; deterministic rule math lives
/// in NecronsFactionPack11.
/// </summary>
public partial class GameController
{
    public void Necrons11OnBattleStarted(
        NecronGameController controller)
    {
        if (controller == null)
            return;

        AppendBattleLog(
            "NECRONS",
            "Reanimation Protocols",
            controller.DetachmentName +
            " • " +
            controller.DetachmentPointsSpent +
            (controller.DetachmentPointLimit > 0
                ? "/" +
                  controller.DetachmentPointLimit
                : "") +
            "DP"
        );
    }

    public void Necrons11OnBattleRoundStarted(
        NecronGameController controller)
    {
        if (controller == null)
            return;

        // Per-round state is reset by NecronsFactionPack11Runtime.
    }

    public void Necrons11OnTurnStarted(
        NecronGameController controller,
        GameEventContext context)
    {
        if (controller == null ||
            context == null)
        {
            return;
        }

        // Turn state is reset by NecronsFactionPack11Runtime.
    }

    public void Necrons11OnPhaseStarted(
        NecronGameController controller,
        GameEventContext context)
    {
        if (controller == null ||
            context == null)
        {
            return;
        }

        bool ownPhase =
            string.Equals(
                activeFaction,
                controller.FactionId,
                StringComparison.OrdinalIgnoreCase);

        if (phase == Phase.Command &&
            ownPhase &&
            controller.HasDetachment(
                NecronDetachment.ObeisancePhalanx))
        {
            Necrons11OfferWorthyFoes(
                controller);
        }

        if (phase == Phase.Command &&
            ownPhase)
        {
            Necrons11OfferCommandEnhancements(
                controller);
        }

        if (controller.HasDetachment(
                NecronDetachment.PantheonOfWoe))
        {
            List<SquadController> monsters =
                controller.ArmyUnits
                    .Where(unit =>
                        unit != null &&
                        unit.IsAlive &&
                        unit.IsOnBattlefield &&
                        unit.HasKeyword("monster"))
                    .ToList();

            if (monsters.Count > 0)
            {
                QueueTraditionalRuleAlert(
                    "COSMIC DISTORTION",
                    controller.FactionId +
                    ": at the start of this phase, each NECRONS MONSTER unit may suffer 3 mortal wounds to increase its Distortion Fields Aura from 6\" to 9\" until the end of the phase. Select a MONSTER and use the PANTHEON DISTORTION command if you want to extend it.",
                    0);
            }
        }

        if (phase == Phase.Fight)
        {
            foreach (SquadController unit
                in controller.ArmyUnits)
            {
                if (unit == null ||
                    !unit.IsAlive ||
                    !unit.IsOnBattlefield)
                {
                    continue;
                }

                if (NecronsFactionPack11
                    .UnitHasEnhancement(
                        unit,
                        "ELDRITCH NIGHTMARE"))
                {
                    QueueTraditionalRuleAlert(
                        "ELDRITCH NIGHTMARE",
                        unit.DisplayName +
                        ": each enemy unit within Engagement Range of the bearer takes a Battle-shock test at the start of the Fight phase.",
                        0);
                }
            }
        }
    }

    public void Necrons11OfferEventRules(
        NecronGameController controller,
        GameEventContext context)
    {
        if (controller == null ||
            context == null)
        {
            return;
        }

        if (context.Type ==
                GameEventType.TurnEnded &&
            !string.Equals(
                context.ActingFaction,
                controller.FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            if (controller.HasDetachment(
                    NecronDetachment.HypercryptLegion))
            {
                int maximum =
                    string.Equals(
                        BattleSizeName,
                        "Incursion",
                        StringComparison.OrdinalIgnoreCase)
                    ? 1
                    : string.Equals(
                        BattleSizeName,
                        "Strike Force",
                        StringComparison.OrdinalIgnoreCase)
                        ? 2
                        : 3;

                QueueTraditionalRuleAlert(
                    "HYPERPHASING",
                    controller.FactionId +
                    ": at the end of the opponent's turn you can select up to " +
                    maximum +
                    " unengaged NECRONS unit(s), remove them from the battlefield and place them into Strategic Reserves.",
                    0);
            }

            foreach (SquadController unit
                in controller.ArmyUnits)
            {
                if (unit != null &&
                    unit.IsAlive &&
                    unit.IsOnBattlefield &&
                    NecronsFactionPack11
                        .UnitHasEnhancement(
                            unit,
                            "VEIL OF DARKNESS"))
                {
                    QueueTraditionalRuleAlert(
                        "VEIL OF DARKNESS",
                        unit.DisplayName +
                        ": once per battle per army, at the end of the opponent's turn while unengaged, this unit may enter Strategic Reserves, gains Deep Strike until the start of your next Shooting phase and must ingress in your next Movement phase (including your first turn).",
                        0);
                }
            }
        }

        if (context.Type ==
                GameEventType.UnitFinishedShooting &&
            context.Source != null &&
            string.Equals(
                context.Source.FactionId,
                controller.FactionId,
                StringComparison.OrdinalIgnoreCase) &&
            NecronsFactionPack11
                .UnitHasEnhancement(
                    context.Source,
                    "GRAVITIC BOLAS"))
        {
            QueueTraditionalRuleAlert(
                "GRAVITIC BOLAS",
                context.Source.DisplayName +
                ": after the bearer has shot, select one non-TITANIC enemy unit hit by one or more of those attacks. It is pinned until your next turn (-2 Move and -2 Charge).",
                0);
        }

        if (context.Type ==
                GameEventType.UnitDestroyed &&
            controller.HasDetachment(
                NecronDetachment.CursedLegion))
        {
            QueueTraditionalRuleAlert(
                "COLD FERVOUR",
                "If this destruction was caused by attacks from a DESTROYER CULT unit and it is the first qualifying trigger this turn, until end of turn friendly qualifying NECRONS weapons gain +2 Strength. Use COLD FERVOUR EMPOWERED when applicable.",
                0);
        }
    }

    public void DrawNecrons11StratagemCards(
        float left,
        float right,
        float y,
        float cardWidth)
    {
        IReadOnlyList<NecronStratagem11> rules =
            NecronsFactionPack11
                .StratagemsFor(activeFaction);

        for (int i = 0;
             i < rules.Count;
             i++)
        {
            NecronStratagem11 rule = rules[i];
            bool rightColumn = i % 2 == 1;
            int row = i / 2;

            Rect card =
                new Rect(
                    rightColumn ? right : left,
                    y + row * 54f,
                    cardWidth,
                    42f);

            if (DrawStratagemActionButton(
                    card,
                    rule.Name +
                    " — " +
                    rule.Cost +
                    " CP",
                    rule.FullRule))
            {
                Necrons11TryUseStratagem(
                    rule,
                    selectedSquad,
                    null);
            }
        }
    }

    public bool Necrons11TryUseStratagem(
        NecronStratagem11 rule,
        SquadController requestedUnit,
        GameEventContext context)
    {
        if (rule == null)
            return false;

        NecronGameController faction =
            requestedUnit != null
            ? NecronsFactionPack11Runtime
                .Controller(
                    requestedUnit.FactionId)
            : NecronsFactionPack11Runtime
                .Controller(activeFaction);

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
                : null;

        if (unit == null ||
            !NecronsFactionPack11.IsNecrons(unit))
        {
            status =
                rule.Name +
                ": select a NECRONS target unit first.";
            return false;
        }

        if (!Necrons11TimingMatchesCurrent(
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
            Necrons11ApplyStratagemEffect(
                rule,
                unit,
                context);

        AppendBattleLog(
            "NECRONS",
            rule.Name,
            unit.DisplayName +
            " used " +
            rule.Name +
            " (" +
            rule.Cost +
            " CP)." +
            (!resolved
                ? " Resolve the player-choice/model-placement portion shown in the rules prompt."
                : ""));

        showStratagemMenu = false;
        return true;
    }

    private bool Necrons11ApplyStratagemEffect(
        NecronStratagem11 rule,
        SquadController unit,
        GameEventContext context)
    {
        string name =
            NecronsFactionPack11.Normalize(
                rule.Name);

        switch (name)
        {
            case "protocol of the eternal revenant":
                return Necrons11ManualRule(rule, unit);

            case "protocol of the undying legions":
                return Necrons11ActivateReanimation(
                    unit,
                    unit.AttachedLeader != null &&
                    unit.AttachedLeader.HasKeyword("necrons") &&
                    unit.AttachedLeader.HasKeyword("character")
                        ? 1
                        : 0,
                    rule.Name);

            case "protocol of the hungry void":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "hungry_void");
                return true;

            case "protocol of the sudden storm":
                NecronsFactionPack11Runtime.SetTurnFlag(
                    unit,
                    "shoot_after_advance");

                if (unit.AttachedLeader != null &&
                    unit.AttachedLeader.HasKeyword("necrons") &&
                    unit.AttachedLeader.HasKeyword("character"))
                {
                    NecronsFactionPack11Runtime.SetPhaseFlag(
                        unit,
                        "reroll_advance");
                }

                return true;

            case "protocol of the conquering tyrant":
            case "protocol of the vengeful stars":
                return Necrons11ManualRule(rule, unit);

            case "masks of death":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "masks_of_death");
                return true;

            case "the spoor of frailty":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "spoor_of_frailty");
                return true;

            case "murderous reanimation":
                return Necrons11ActivateReanimation(
                    unit,
                    0,
                    rule.Name);

            case "pitiless hunters":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "pile_consolidate_6");
                return Necrons11ManualRule(rule, unit);

            case "blood fuelled cruelty":
                return Necrons11ManualRule(rule, unit);

            case "insanity s ire":
                Necrons11RollMove(
                    unit,
                    rule.Name,
                    1,
                    6,
                    3);
                return true;

            case "curse of the cryptek":
                return Necrons11ManualRule(rule, unit);

            case "cynosure of eradication":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "cynosure_of_eradication");
                return true;

            case "solar pulse":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "solar_pulse");
                return Necrons11ManualRule(rule, unit);

            case "reactive subroutines":
                BeginSpecialMove(
                    unit,
                    6f,
                    rule.Name,
                    null);
                return true;

            case "countertemporal shift":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "countertemporal_shift");
                return true;

            case "suboptimal facade":
                return Necrons11ActivateReanimation(
                    unit,
                    0,
                    rule.Name);

            case "your time is nigh":
                NecronsFactionPack11Runtime.SetYourTimeIsNigh(
                    unit.FactionId,
                    true);
                return Necrons11ManualRule(rule, unit);

            case "enslaved artifice":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "enslaved_artifice");
                return true;

            case "nanoassembly protocols":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "nanoassembly_protocols");
                return true;

            case "sentinels of eternity":
                return Necrons11ManualRule(rule, unit);

            case "suffer no rival":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "suffer_no_rival");
                return true;

            case "territorial obsession":
                NecronsFactionPack11Runtime.SetTurnFlag(
                    unit,
                    "territorial_obsession");
                return true;

            case "hyperphasic recall":
                return Necrons11ManualRule(rule, unit);

            case "quantum deflection":
                return Necrons11ManualRule(rule, unit);

            case "reanimation crypts":
                return Necrons11ReanimateReserves(
                    unit.FactionId,
                    rule.Name);

            case "cosmic precision":
                NecronsFactionPack11Runtime.SetTurnFlag(
                    unit,
                    "cosmic_precision_no_charge");
                return Necrons11ManualRule(rule, unit);

            case "dimensional corridor":
                NecronsFactionPack11Runtime.SetTurnFlag(
                    unit,
                    "charge_after_setup");
                return true;

            case "entropic damping":
                return Necrons11ManualRule(rule, unit);

            case "merciless reclamation":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "merciless_reclamation");
                return true;

            case "unyielding forms":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "unyielding_forms");
                return true;

            case "chronoshift":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "advance_fixed_6");
                return Necrons11ManualRule(rule, unit);

            case "dimensional tunnel":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "move_through_models_terrain");
                return Necrons11ManualRule(rule, unit);

            case "endless servitude":
                return Necrons11ActivateReanimation(
                    unit,
                    0,
                    rule.Name);

            case "reactive reposition":
                Necrons11RollMove(
                    unit,
                    rule.Name,
                    1,
                    6,
                    3);
                return true;

            case "molecular targeting":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "molecular_targeting");
                return true;

            case "microscarab swarm":
                return Necrons11ManualRule(rule, unit);

            case "animus curse":
                return Necrons11ManualRule(rule, unit);

            case "synergistic empowerment":
                return Necrons11ManualRule(rule, unit);

            case "untapped power":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "untapped_power");
                Necrons11ChooseAugmentation(
                    unit,
                    true);
                return true;

            case "potentiality syphon":
                return Necrons11ActivateReanimation(
                    unit,
                    unit.HasKeyword("cryptek")
                        ? 1
                        : 0,
                    rule.Name);

            case "methodical murder":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "methodical_murder");
                return true;

            case "image of death":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "image_of_death");
                return true;

            case "mortis protocols":
                return Necrons11ManualRule(rule, unit);

            case "driven to butchery":
                NecronsFactionPack11Runtime.SetTurnFlag(
                    unit,
                    "shoot_after_advance");
                NecronsFactionPack11Runtime.SetTurnFlag(
                    unit,
                    "charge_after_advance");
                return true;

            case "spreading madness":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "spreading_madness");
                return true;

            case "unnatural aggression":
                return Necrons11ManualRule(rule, unit);

            case "disharmonisation cascade":
            case "molecular erosion":
            case "mass transmogrification":
            case "chronodistortion":
            case "phase melding":
                return Necrons11ManualRule(rule, unit);

            case "entrophasic aura targeting":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "entrophasic_aura_targeting");
                return true;

            case "dominance protocols":
                NecronsFactionPack11Runtime.SetTurnFlag(
                    unit,
                    "territorial_obsession");
                return true;

            case "will of the conqueror":
                return Necrons11SecureObjective(
                    unit,
                    rule.Name);

            case "nanosaturation":
                return Necrons11ManualRule(rule, unit);

            case "omnilocked strafing":
                NecronsFactionPack11Runtime.SetTurnFlag(
                    unit,
                    "shoot_after_fallback");
                return true;

            case "swift as death":
                Necrons11RollMove(
                    unit,
                    rule.Name,
                    4,
                    6,
                    5);
                return true;

            case "evasive protocols":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "evasive_protocols");
                return true;

            case "subsurface quantumweave":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "subsurface_quantumweave");
                return true;

            case "particle pulse":
                return Necrons11ManualRule(rule, unit);

            case "cosmic storm":
                NecronsFactionPack11Runtime.SetPhaseFlag(
                    unit,
                    "cosmic_storm");
                return true;
        }

        return Necrons11ManualRule(
            rule,
            unit);
    }

    private bool Necrons11ManualRule(
        NecronStratagem11 rule,
        SquadController unit)
    {
        QueueTraditionalRuleAlert(
            rule.Name,
            (unit != null
                ? unit.DisplayName + ": "
                : "") +
            rule.FullRule +
            "\nWarboard has spent the CP and recorded the target. Resolve the remaining player-choice, model-placement, exact target-selection or special datasheet portion exactly as written.",
            Necrons11SuggestedDice(rule));

        return false;
    }

    private bool Necrons11ActivateReanimation(
        SquadController unit,
        int flatBonus,
        string label)
    {
        if (unit == null)
            return false;

        unit = unit.JoinedActionController();

        int rolled =
            RollTabletopD3(
                label + ": " +
                unit.DisplayName);

        rolled =
            NecronsFactionPack11
                .ModifyReanimationRoll(
                    unit,
                    rolled);

        int amount =
            Mathf.Max(
                0,
                rolled + flatBonus);

        int restored =
            ReanimateUnit(
                unit,
                amount);

        AppendBattleLog(
            "NECRONS",
            label,
            unit.DisplayName +
            " reanimated " +
            restored +
            " wound(s) from " +
            amount +
            " available.");

        return true;
    }

    private bool Necrons11ReanimateReserves(
        string faction,
        string label)
    {
        NecronGameController controller =
            NecronsFactionPack11Runtime
                .Controller(faction);

        if (controller == null)
            return false;

        int units = 0;
        int restored = 0;

        foreach (SquadController unit
            in controller.ArmyUnits)
        {
            if (unit == null ||
                unit.IsAttachedLeader ||
                unit.IsOnBattlefield ||
                !unit.IsAlive ||
                !unit.HasAnyLostWoundsOrModels())
            {
                continue;
            }

            int rolled =
                RollTabletopD3(
                    label + ": " +
                    unit.DisplayName);

            rolled =
                NecronsFactionPack11
                    .ModifyReanimationRoll(
                        unit,
                        rolled);

            restored +=
                ReanimateUnit(
                    unit,
                    rolled);

            units++;
        }

        status =
            label +
            ": resolved Reanimation Protocols for " +
            units +
            " reserve unit(s), restoring " +
            restored +
            " wound(s).";

        return true;
    }

    private void Necrons11RollMove(
        SquadController unit,
        string label,
        int minimum,
        int maximum,
        int suggested)
    {
        if (unit == null)
            return;

        if (IsXcomMode)
        {
            int distance =
                minimum == 4 &&
                maximum == 6
                ? DiceRoller.RollD3(label) + 3
                : DiceRoller.RollD6(label);

            BeginSpecialMove(
                unit,
                distance,
                label,
                null);
            return;
        }

        OpenTraditionalNumericPrompt(
            label,
            minimum == 4 &&
            maximum == 6
                ? "Roll D3+3 for the move distance."
                : "Roll D6 for the move distance.",
            minimum,
            maximum,
            suggested,
            1,
            value =>
                BeginSpecialMove(
                    unit,
                    value,
                    label,
                    null));
    }

    private bool Necrons11SecureObjective(
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
        {
            status =
                label +
                ": the selected unit is not controlling an objective.";
            return false;
        }

        objective.SecureFor(
            unit.FactionId);

        RefreshObjectiveDisplays();

        status =
            label +
            ": objective secured by " +
            DisplayFactionName(
                unit.FactionId) +
            ".";

        return true;
    }

    private void Necrons11OfferWorthyFoes(
        NecronGameController controller)
    {
        if (controller == null)
            return;

        List<SquadController> enemies =
            squads
                .Where(unit =>
                    unit != null &&
                    !unit.IsAttachedLeader &&
                    unit.IsAlive &&
                    unit.IsOnBattlefield &&
                    !string.Equals(
                        unit.FactionId,
                        controller.FactionId,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (enemies.Count == 0)
            return;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController enemy
            in enemies)
        {
            SquadController captured =
                enemy;

            options.Add(
                new RuleChoiceOption(
                    captured.DisplayName,
                    () =>
                    {
                        CloseRuleChoice();

                        NecronsFactionPack11Runtime
                            .SetWorthyFoe(
                                controller.FactionId,
                                captured);

                        status =
                            "Worthy Foes: " +
                            captured.DisplayName +
                            " marked until the start of the next Command phase.";
                    }));
        }

        OpenRuleChoice(
            "WORTHY FOES",
            "Select one enemy unit. NOBLE, LYCHGUARD and TRIARCH units gain +1 to Wound against it until the start of your next Command phase.",
            options.ToArray());
    }

    private void Necrons11OfferCommandEnhancements(
        NecronGameController controller)
    {
        if (controller == null)
            return;

        foreach (SquadController bearer
            in controller.ArmyUnits)
        {
            if (bearer == null ||
                !bearer.IsAlive ||
                !bearer.IsOnBattlefield)
            {
                continue;
            }

            if (NecronsFactionPack11
                .UnitHasEnhancement(
                    bearer,
                    "DEMANDING LEADER"))
            {
                QueueTraditionalRuleAlert(
                    "DEMANDING LEADER",
                    bearer.DisplayName +
                    ": in your Command phase select one friendly NECRONS VEHICLE or MOUNTED unit (excluding TITANIC) within 6\". Until your next Command phase it can shoot after Falling Back. Select that unit and use the rule from the faction panel.",
                    0);
            }

            if (NecronsFactionPack11
                .UnitHasEnhancement(
                    bearer,
                    "CHRONO-IMPEDANCE FIELDS"))
            {
                QueueTraditionalRuleAlert(
                    "CHRONO-IMPEDANCE FIELDS",
                    bearer.DisplayName +
                    ": in your Command phase select one friendly NECRONS VEHICLE or MOUNTED unit (excluding TITANIC) within 6\". Until your next Command phase subtract 1 from Damage allocated to it.",
                    0);
            }
        }
    }

    public bool Necrons11EnsureCryptekAugmentation(
        SquadController unit)
    {
        if (unit == null)
            return true;

        unit = unit.JoinedActionController();

        if (!NecronsFactionPack11.IsNecrons(unit) ||
            !NecronsFactionPack11.Has(
                unit.FactionId,
                NecronDetachment.CryptekConclave) ||
            !unit.HasKeyword("cryptek") ||
            NecronsFactionPack11Runtime
                .HasAnyAugmentation(unit))
        {
            return true;
        }

        Necrons11ChooseAugmentation(
            unit,
            false);

        status =
            unit.DisplayName +
            ": choose Technosorcerous Augmentation, then select the attack again.";

        return false;
    }

    private void Necrons11ChooseAugmentation(
        SquadController unit,
        bool additional)
    {
        if (unit == null)
            return;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        Action<string> add =
            value =>
            {
                NecronsFactionPack11Runtime
                    .SetAugmentation(
                        unit,
                        value,
                        !additional);

                CloseRuleChoice();

                status =
                    unit.DisplayName +
                    " gains " +
                    value.ToUpperInvariant() +
                    " until the end of the phase.";
            };

        options.Add(
            new RuleChoiceOption(
                "ANTI-INFANTRY 3+",
                () => add("anti infantry 3")));

        options.Add(
            new RuleChoiceOption(
                "ANTI-MOUNTED 4+",
                () => add("anti mounted 4")));

        options.Add(
            new RuleChoiceOption(
                "ASSAULT",
                () => add("assault")));

        options.Add(
            new RuleChoiceOption(
                "HEAVY",
                () => add("heavy")));

        options.Add(
            new RuleChoiceOption(
                "IGNORES COVER",
                () => add("ignores cover")));

        if (NecronsFactionPack11
            .UnitHasEnhancement(
                unit,
                "ATOMIC DISINTEGRATORS"))
        {
            options.Add(
                new RuleChoiceOption(
                    "ANTI-MONSTER 5+",
                    () => add("anti monster 5")));

            options.Add(
                new RuleChoiceOption(
                    "ANTI-VEHICLE 5+",
                    () => add("anti vehicle 5")));
        }

        OpenRuleChoice(
            "TECHNOSORCEROUS AUGMENTATIONS",
            additional
                ? "Select the additional ability granted by Untapped Power."
                : "Select one ability for this CRYPTEK unit's ranged weapons until the end of the phase.",
            options.ToArray());
    }

    public bool Necrons11OfferChargeReroll(
        SquadController attacker,
        SquadController target,
        int roll,
        bool wasRerolled)
    {
        if (attacker == null ||
            target == null ||
            wasRerolled ||
            !NecronsFactionPack11
                .CanRerollCharge(attacker))
        {
            return false;
        }

        attacker =
            attacker.JoinedActionController();

        OpenRuleChoice(
            "ANNIHILATION PROTOCOL — CHARGE RE-ROLL",
            attacker.DisplayName +
            " can re-roll its Charge roll of " +
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
                    IsXcomMode
                        ? "Re-roll 2D6"
                        : "Enter re-roll result",
                    () =>
                    {
                        CloseRuleChoice();

                        if (IsXcomMode)
                        {
                            int reroll =
                                DiceRoller.RollD6(
                                    "Annihilation Protocol") +
                                DiceRoller.RollD6(
                                    "Annihilation Protocol");

                            ResolveChargeRoll(
                                attacker,
                                target,
                                reroll,
                                true,
                                roll);

                            return;
                        }

                        OpenTraditionalNumericPrompt(
                            "ANNIHILATION PROTOCOL",
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

    public int Necrons11ChargeRollModifier(
        SquadController attacker,
        SquadController target)
    {
        return NecronsFactionPack11
            .ChargeRollModifier(
                this,
                attacker,
                target);
    }

    public int Necrons11ModifyStratagemCost(
        SquadController target,
        string label,
        int currentCost)
    {
        return NecronsFactionPack11
            .ModifyStratagemCost(
                target,
                label,
                currentCost);
    }

    public bool Necrons11CanShootAfterFallBack(
        SquadController unit)
    {
        return NecronsFactionPack11
            .CanShootAfterFallBack(unit);
    }

    public bool Necrons11CanChargeAfterFallBack(
        SquadController unit)
    {
        return NecronsFactionPack11
            .CanChargeAfterFallBack(unit);
    }

    public bool Necrons11CanShootAfterAdvance(
        SquadController unit)
    {
        return NecronsFactionPack11
            .CanShootAfterAdvance(unit);
    }

    public bool Necrons11CanChargeAfterAdvance(
        SquadController unit)
    {
        return NecronsFactionPack11
            .CanChargeAfterAdvance(unit);
    }

    public bool Necrons11CanAttackTarget(
        SquadController attacker,
        SquadController target,
        AttackMode mode,
        out string reason)
    {
        reason = "";

        if (attacker == null ||
            target == null)
        {
            return true;
        }

        if (mode == AttackMode.Ranged &&
            !NecronsFactionPack11
                .CanBeRangedTarget(
                    this,
                    attacker,
                    target,
                    out reason))
        {
            return false;
        }

        return true;
    }

    public bool Necrons11ControlsHalfNoMansLandObjectives(
        string faction)
    {
        if (string.IsNullOrWhiteSpace(faction) ||
            activeMissionBattlefield == null)
        {
            return false;
        }

        MissionDeploymentZone own =
            DeploymentZoneForFaction(
                faction);

        string opponent =
            factions.FirstOrDefault(
                value =>
                    !string.Equals(
                        value,
                        faction,
                        StringComparison.OrdinalIgnoreCase));

        MissionDeploymentZone enemy =
            !string.IsNullOrWhiteSpace(opponent)
            ? DeploymentZoneForFaction(
                opponent)
            : null;

        List<ObjectiveController> nml =
            objectives
                .Where(objective =>
                    objective != null &&
                    (own == null ||
                     !own.ContainsBase(
                         objective.transform.position,
                         0f)) &&
                    (enemy == null ||
                     !enemy.ContainsBase(
                         objective.transform.position,
                         0f)))
                .ToList();

        if (nml.Count == 0)
            return false;

        int controlled =
            nml.Count(objective =>
                string.Equals(
                    objective.Controller(squads),
                    faction,
                    StringComparison.OrdinalIgnoreCase));

        return controlled * 2 >= nml.Count;
    }

    public bool Necrons11ControlsHalfOpponentZoneObjectives(
        string faction)
    {
        if (string.IsNullOrWhiteSpace(faction) ||
            activeMissionBattlefield == null)
        {
            return false;
        }

        string opponent =
            factions.FirstOrDefault(
                value =>
                    !string.Equals(
                        value,
                        faction,
                        StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(opponent))
            return false;

        MissionDeploymentZone zone =
            DeploymentZoneForFaction(
                opponent);

        if (zone == null)
            return false;

        List<ObjectiveController> values =
            objectives
                .Where(objective =>
                    objective != null &&
                    zone.ContainsBase(
                        objective.transform.position,
                        0f))
                .ToList();

        if (values.Count == 0)
            return false;

        int controlled =
            values.Count(objective =>
                string.Equals(
                    objective.Controller(squads),
                    faction,
                    StringComparison.OrdinalIgnoreCase));

        return controlled * 2 >= values.Count;
    }

    public bool Necrons11UnitWhollyInNoMansLand(
        SquadController unit)
    {
        if (unit == null ||
            factions.Count < 2 ||
            activeMissionBattlefield == null)
        {
            return false;
        }

        MissionDeploymentZone first =
            DeploymentZoneForFaction(
                factions[0]);

        MissionDeploymentZone second =
            DeploymentZoneForFaction(
                factions[1]);

        List<ModelToken> models =
            unit.JoinedLivingModelTokens();

        if (models.Count == 0)
            return false;

        return models.All(
            model =>
                model != null &&
                (first == null ||
                 !first.ContainsBase(
                     model.transform.position,
                     model.BaseRadiusInches)) &&
                (second == null ||
                 !second.ContainsBase(
                     model.transform.position,
                     model.BaseRadiusInches)));
    }

    public bool Necrons11IsClosestEnemyUnit(
        SquadController attacker,
        SquadController target)
    {
        if (attacker == null ||
            target == null)
        {
            return false;
        }

        float targetDistance =
            JoinedDistancePublic(
                attacker,
                target);

        return !squads.Any(
            enemy =>
                enemy != null &&
                !enemy.IsAttachedLeader &&
                enemy.IsAlive &&
                enemy.IsOnBattlefield &&
                !string.Equals(
                    enemy.FactionId,
                    attacker.FactionId,
                    StringComparison.OrdinalIgnoreCase) &&
                enemy.JoinedActionController() !=
                    target.JoinedActionController() &&
                JoinedDistancePublic(
                    attacker,
                    enemy) <
                    targetDistance - 0.001f);
    }

    public bool Necrons11EnemyEngagedByFriendly(
        SquadController attacker,
        SquadController target)
    {
        if (attacker == null ||
            target == null)
        {
            return false;
        }

        return squads.Any(
            friendly =>
                friendly != null &&
                !friendly.IsAttachedLeader &&
                friendly.IsAlive &&
                friendly.IsOnBattlefield &&
                friendly.JoinedActionController() !=
                    attacker.JoinedActionController() &&
                string.Equals(
                    friendly.FactionId,
                    attacker.FactionId,
                    StringComparison.OrdinalIgnoreCase) &&
                UnitsAreEngaged(
                    friendly,
                    target));
    }

    public bool Necrons11ExtendDistortionField(
        SquadController monster)
    {
        if (monster == null ||
            !NecronsFactionPack11.IsNecrons(monster) ||
            !monster.HasKeyword("monster") ||
            !NecronsFactionPack11.Has(
                monster.FactionId,
                NecronDetachment.PantheonOfWoe))
        {
            return false;
        }

        Core11ApplyMortalWounds(
            monster,
            3,
            "Cosmic Distortion");

        NecronsFactionPack11Runtime
            .SetDistortionExtended(
                monster,
                true);

        return true;
    }

    public void Necrons11SetColdFervourEmpowered(
        string faction)
    {
        NecronsFactionPack11Runtime
            .SetColdFervourEmpowered(
                faction,
                true);
    }

    public void Necrons11SetPinned(
        SquadController target)
    {
        NecronsFactionPack11Runtime
            .SetPinned(
                target,
                true);
    }

    public void Necrons11SetDemandingLeaderTarget(
        SquadController target)
    {
        NecronsFactionPack11Runtime
            .SetTurnFlag(
                target,
                "demanding_leader");
    }

    public void Necrons11SetChronoImpedanceTarget(
        SquadController target)
    {
        NecronsFactionPack11Runtime
            .SetTurnFlag(
                target,
                "chrono_impedance");
    }

    private bool Necrons11TimingMatchesCurrent(
        NecronStratagem11 rule,
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

        if (when.Contains("any phase"))
            return true;

        if (when.Contains("command phase") &&
            phase != Phase.Command)
            return false;

        if (when.Contains("movement phase") &&
            phase != Phase.Movement)
            return false;

        if (when.Contains("shooting phase") &&
            phase != Phase.Shooting)
            return false;

        if (when.Contains("charge phase") &&
            phase != Phase.Charge)
            return false;

        if (when.Contains("fight phase") &&
            phase != Phase.Fight)
            return false;

        bool ownTurn =
            string.Equals(
                activeFaction,
                unit.FactionId,
                StringComparison.OrdinalIgnoreCase);

        if (when.Contains("your opponent") &&
            ownTurn)
        {
            return false;
        }

        if (when.StartsWith("your ") &&
            !when.StartsWith("your opponent") &&
            !ownTurn)
        {
            return false;
        }

        return true;
    }

    private int Necrons11SuggestedDice(
        NecronStratagem11 rule)
    {
        if (rule == null)
            return 0;

        string text =
            (rule.Effect ?? "")
                .ToLowerInvariant();

        if (text.Contains("d6") ||
            text.Contains("d3"))
        {
            return 1;
        }

        return 0;
    }
}
