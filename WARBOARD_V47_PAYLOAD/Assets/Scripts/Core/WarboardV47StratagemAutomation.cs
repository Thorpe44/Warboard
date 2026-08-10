using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Automates faction Stratagems that v46 had to leave as SPEND + LOG because
/// the engine lacked persistent target state, arbitrary markers and generic
/// repositioning. Unrecognised Stratagems deliberately return false so the
/// existing exact-rule/manual path remains available.
/// </summary>
public static class WarboardV47StratagemAutomation
{
    public static bool TryUse(
        StandardFactionGameController controller,
        StandardFactionStratagem11 stratagem)
    {
        if (controller == null ||
            stratagem == null)
        {
            return false;
        }

        string name =
            StandardFactionPack11.Normalize(
                stratagem.name
            );

        if (controller.PackId == "space_marines" &&
            controller.HasDetachment(
                "BASTION TASK FORCE"))
        {
            if (name == Normalize("CODEX DISCIPLINE"))
                return UseCodexDiscipline(controller, stratagem);

            if (name == Normalize("GUIDED DISRUPTION"))
                return UseGuidedDisruption(controller, stratagem);

            if (name == Normalize("SHOCK BOMBARDMENT"))
                return UseShockBombardment(controller, stratagem);

            if (name == Normalize("LIGHT OF VENGEANCE"))
                return UseLightOfVengeance(controller, stratagem);

            if (name == Normalize("HERESY UNDONE"))
                return UseHeresyUndone(controller, stratagem);
        }

        if (controller.PackId == "tyranids" &&
            controller.HasDetachment(
                "SUBTERRANEAN ASSAULT"))
        {
            if (name == Normalize("TUNNEL NETWORK"))
                return UseTunnelNetwork(controller, stratagem);
        }

        if (controller.PackId == "space_marines" &&
            controller.HasDetachment(
                "SUBVERSION ASSETS") &&
            name == Normalize("CLOAKED POSITION"))
        {
            return UseCloakedPosition(controller, stratagem);
        }

        return false;
    }

    private static bool UseCodexDiscipline(
        StandardFactionGameController controller,
        StandardFactionStratagem11 stratagem)
    {
        GameController game = GameController.Current;
        SquadController unit = FriendlySelected(controller);

        if (game == null || unit == null)
            return HandledFailure(game, "Select the Space Marines unit that will use Codex Discipline.");

        if (game.CurrentPhase != GameController.Phase.Shoot &&
            game.CurrentPhase != GameController.Phase.Fight)
        {
            return HandledFailure(game, "Codex Discipline can only be used in the Shooting or Fight phase.");
        }

        if ((game.CurrentPhase == GameController.Phase.Shoot &&
             unit.HasShot) ||
            (game.CurrentPhase == GameController.Phase.Fight &&
             unit.HasFought))
        {
            return HandledFailure(
                game,
                "Codex Discipline targets a unit that has not yet been selected to shoot or fight this phase.");
        }

        if (!Spend(controller, stratagem, unit))
            return true;

        WarboardRuleStateStore47.SetUnitTarget(
            "CODEX_DISCIPLINE",
            controller.FactionId,
            unit,
            unit,
            WarboardRuleScope47.Phase,
            "Re-roll Hit rolls of 1; also Wound rolls of 1 against an auspex scanned target."
        );

        LogAutomated(game, controller, stratagem, unit.DisplayName + " is under Codex Discipline until end of phase.");
        return true;
    }

    private static bool UseGuidedDisruption(
        StandardFactionGameController controller,
        StandardFactionStratagem11 stratagem)
    {
        GameController game = GameController.Current;
        SquadController source = FriendlySelected(controller);

        if (game == null || source == null ||
            !source.HasKeyword("BATTLELINE"))
        {
            return HandledFailure(game, "Select the ADEPTUS ASTARTES BATTLELINE unit that just finished making its attacks.");
        }

        WarboardRuleState47 scan = LatestScan(controller, source);

        if (scan == null || scan.TargetUnit == null)
            return HandledFailure(game, "That unit has not just auspex scanned an enemy through its attacks this turn.");

        if (scan.TargetUnit.HasKeyword("MONSTER") ||
            scan.TargetUnit.HasKeyword("VEHICLE"))
        {
            return HandledFailure(game, "Guided Disruption cannot pin a MONSTER or VEHICLE.");
        }

        if (!Spend(controller, stratagem, source))
            return true;

        WarboardRuleStateStore47.SetUnitTarget(
            "BASTION_PINNED",
            controller.FactionId,
            source,
            scan.TargetUnit,
            WarboardRuleScope47.OwnerNextTurn,
            "Pinned: -2 Move and -2 Charge until the start of the Space Marines player's next turn."
        );

        LogAutomated(game, controller, stratagem, scan.TargetUnit.DisplayName + " is PINNED until the start of your next turn.");
        return true;
    }

    private static bool UseShockBombardment(
        StandardFactionGameController controller,
        StandardFactionStratagem11 stratagem)
    {
        GameController game = GameController.Current;
        SquadController source = FriendlySelected(controller);

        if (game == null || source == null ||
            !source.HasKeyword("BATTLELINE"))
        {
            return HandledFailure(game, "Select the ADEPTUS ASTARTES BATTLELINE unit that just finished making its attacks.");
        }

        WarboardRuleState47 scan = LatestScan(controller, source);

        if (scan == null || scan.TargetUnit == null)
            return HandledFailure(game, "That unit has not just auspex scanned an enemy through its attacks this turn.");

        if (!Spend(controller, stratagem, source))
            return true;

        WarboardRuleStateStore47.SetUnitTarget(
            "BASTION_SUPPRESSED",
            controller.FactionId,
            source,
            scan.TargetUnit,
            WarboardRuleScope47.OwnerNextTurn,
            "Suppressed: -1 Hit until the start of the Space Marines player's next turn."
        );

        LogAutomated(game, controller, stratagem, scan.TargetUnit.DisplayName + " is SUPPRESSED until the start of your next turn.");
        return true;
    }

    private static bool UseLightOfVengeance(
        StandardFactionGameController controller,
        StandardFactionStratagem11 stratagem)
    {
        GameController game = GameController.Current;
        SquadController unit = FriendlySelected(controller);

        if (game == null || unit == null)
            return HandledFailure(game, "Select the ADEPTUS ASTARTES unit that will use Light of Vengeance.");

        if (game.CurrentPhase != GameController.Phase.Shoot &&
            game.CurrentPhase != GameController.Phase.Fight)
        {
            return HandledFailure(
                game,
                "Light of Vengeance is used in the Shooting or Fight phase.");
        }

        if ((game.CurrentPhase == GameController.Phase.Shoot &&
             unit.HasShot) ||
            (game.CurrentPhase == GameController.Phase.Fight &&
             unit.HasFought))
        {
            return HandledFailure(
                game,
                "Light of Vengeance targets a unit that has not yet been selected to shoot or fight this phase.");
        }

        if (!Spend(controller, stratagem, unit))
            return true;

        WarboardDatasheetChoice47.Request(
            game,
            "LIGHT OF VENGEANCE",
            "Choose the weapon ability for this unit until the end of the phase. The ability applies while targeting an auspex scanned unit, or to attacks made by BATTLELINE bearers as stated in the source rule.",
            "LIGHT_OF_VENGEANCE",
            controller.FactionId,
            unit,
            WarboardRuleScope47.Phase,
            new[]
            {
                new WarboardDatasheetChoiceOption47(
                    "LETHAL_HITS",
                    "LETHAL HITS"),
                new WarboardDatasheetChoiceOption47(
                    "SUSTAINED_HITS_1",
                    "SUSTAINED HITS 1")
            },
            value => LogAutomated(
                game,
                controller,
                stratagem,
                unit.DisplayName + " selected " + value + ".")
        );

        return true;
    }

    private static bool UseHeresyUndone(
        StandardFactionGameController controller,
        StandardFactionStratagem11 stratagem)
    {
        GameController game = GameController.Current;
        SquadController unit = FriendlySelected(controller);

        if (game == null || unit == null ||
            unit.HasKeyword("BATTLELINE"))
        {
            return HandledFailure(game, "Select a non-BATTLELINE ADEPTUS ASTARTES unit for Heresy Undone.");
        }

        if (game.CurrentPhase != GameController.Phase.Shoot &&
            game.CurrentPhase != GameController.Phase.Charge)
        {
            return HandledFailure(game, "Heresy Undone is used in your Shooting or Charge phase.");
        }

        if (!Spend(controller, stratagem, unit))
            return true;

        WarboardRuleStateStore47.SetUnitTarget(
            "HERESY_UNDONE",
            controller.FactionId,
            unit,
            unit,
            WarboardRuleScope47.Phase,
            "Eligible after Advance/Fall Back. Every attack/charge target must be auspex scanned."
        );

        LogAutomated(game, controller, stratagem, unit.DisplayName + " may shoot/charge after Advance or Fall Back this phase, but only into auspex scanned targets.");
        return true;
    }

    private static bool UseTunnelNetwork(
        StandardFactionGameController controller,
        StandardFactionStratagem11 stratagem)
    {
        GameController game = GameController.Current;
        SquadController unit = FriendlySelected(controller);
        WarboardMarkerSystem47 markers = WarboardMarkerSystem47.Instance;

        if (game == null || unit == null || markers == null)
            return HandledFailure(game, "Select the TYRANIDS unit using Tunnel Network.");

        if (game.CurrentPhase != GameController.Phase.Move)
            return HandledFailure(game, "Tunnel Network is used at the end of your Movement phase.");

        if (game.StandardIsEngaged(unit))
            return HandledFailure(game, "Tunnel Network cannot target an engaged unit.");

        List<WarboardRuleMarker47> all =
            markers.ForFaction(
                    controller.FactionId,
                    "TYRANID_TUNNEL")
                .ToList();

        List<WarboardRuleMarker47> origin =
            all.Where(marker =>
                    markers.UnitWhollyWithin(
                        unit,
                        marker,
                        9f))
                .ToList();

        if (origin.Count == 0 || all.Count < 2)
            return HandledFailure(game, "The unit must be wholly within 9 inches of one Tunnel Marker and another Tunnel Marker must exist.");

        List<WarboardRuleMarker47> destinations =
            all.Where(marker => !origin.Contains(marker))
                .ToList();

        if (destinations.Count == 0)
            return HandledFailure(game, "Choose another Tunnel Marker; there is no separate destination marker available.");

        if (!Spend(controller, stratagem, unit))
            return true;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (WarboardRuleMarker47 marker in destinations)
        {
            WarboardRuleMarker47 captured = marker;

            options.Add(
                new RuleChoiceOption(
                    captured.Label + "  " + captured.Id,
                    () =>
                    {
                        game.StandardCloseRuleChoice();

                        WarboardSpecialPlacement47 system =
                            WarboardSpecialPlacement47.Instance;

                        if (system == null)
                        {
                            game.StandardSetStatus(
                                "Special-placement engine is unavailable."
                            );
                            return;
                        }

                        system.Begin(
                            new WarboardSpecialPlacementRequest47
                            {
                                Unit = unit,
                                Label = "TUNNEL NETWORK",
                                Kind = WarboardSpecialPlacementKind47.Reposition,
                                MinimumEnemyDistance = 6f,
                                MustFinishWithinMarker = captured,
                                MarkerDistance = 9f,
                                RequireUnengagedEnd = true,
                                IgnorePathObstructions = true,
                                Completed = () =>
                                    LogAutomated(
                                        game,
                                        controller,
                                        stratagem,
                                        unit.DisplayName +
                                        " repositioned wholly within 9 inches of " +
                                        captured.Label +
                                        " and more than 6 inches from enemies."
                                    )
                            }
                        );
                    }
                )
            );
        }

        game.StandardOpenRuleChoice(
            "TUNNEL NETWORK",
            "Choose the other Tunnel Marker, then click the legal final position for the unit.",
            options.ToArray()
        );

        return true;
    }

    private static bool UseCloakedPosition(
        StandardFactionGameController controller,
        StandardFactionStratagem11 stratagem)
    {
        GameController game = GameController.Current;
        SquadController unit = FriendlySelected(controller);

        if (game == null || unit == null ||
            (!unit.HasKeyword("PHOBOS") &&
             unit.DisplayName.IndexOf(
                 "Scout Squad",
                 StringComparison.OrdinalIgnoreCase) < 0))
        {
            return HandledFailure(game, "Select the unengaged PHOBOS/SCOUT SQUAD unit for Cloaked Position.");
        }

        if (game.CurrentPhase !=
                GameController.Phase.Move ||
            string.Equals(
                game.ActiveFactionId,
                controller.FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            return HandledFailure(
                game,
                "Cloaked Position is used at the start of your opponent's Movement phase.");
        }

        if (game.StandardIsEngaged(unit))
            return HandledFailure(game, "Cloaked Position requires an unengaged unit.");

        if (!Spend(controller, stratagem, unit))
            return true;

        WarboardRuleStateStore47.SetUnitTarget(
            "SUBVERSION_CLOAKED_POSITION",
            controller.FactionId,
            unit,
            unit,
            WarboardRuleScope47.Turn,
            "-3 inch detection range until end of turn."
        );

        LogAutomated(game, controller, stratagem, unit.DisplayName + " has -3 inches detection range until the end of the turn.");
        return true;
    }

    private static WarboardRuleState47 LatestScan(
        StandardFactionGameController controller,
        SquadController source)
    {
        SquadController action =
            source != null
            ? source.JoinedActionController()
            : null;

        return WarboardRuleStateStore47
            .GetAll(
                "BASTION_AUSPEX_SCANNED",
                controller.FactionId)
            .LastOrDefault(
                value =>
                    value != null &&
                    value.SourceUnit == action);
    }

    private static SquadController FriendlySelected(
        StandardFactionGameController controller)
    {
        GameController game = GameController.Current;

        if (game == null || controller == null)
            return null;

        SquadController selected =
            game.StandardSelectedSquad;

        if (selected == null ||
            !string.Equals(
                selected.FactionId,
                controller.FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return selected.JoinedActionController();
    }

    private static bool Spend(
        StandardFactionGameController controller,
        StandardFactionStratagem11 stratagem,
        SquadController unit)
    {
        GameController game = GameController.Current;

        bool spent =
            unit != null
            ? game.SpendStratagemCPForUnit(
                unit,
                stratagem.cost,
                stratagem.name)
            : game.TrySpendCommandPoints(
                controller.FactionId,
                stratagem.cost);

        if (!spent)
        {
            game.StandardSetStatus(
                stratagem.name +
                ": insufficient CP or that unit cannot currently be targeted by another Stratagem."
            );
        }

        return spent;
    }

    private static bool HandledFailure(
        GameController game,
        string message)
    {
        if (game != null)
            game.StandardSetStatus(message);

        return true;
    }

    private static void LogAutomated(
        GameController game,
        StandardFactionGameController controller,
        StandardFactionStratagem11 stratagem,
        string result)
    {
        if (game == null)
            return;

        game.StandardLog(
            "STRATAGEM",
            stratagem.name,
            stratagem.FullRule +
            "\nSource page " +
            stratagem.sourcePage +
            ".\nV47 AUTOMATION: " +
            result
        );

        game.StandardSetStatus(
            stratagem.name +
            " resolved by the v47 rules engine."
        );
    }

    private static string Normalize(
        string value)
    {
        return StandardFactionPack11.Normalize(
            value ?? ""
        );
    }
}
