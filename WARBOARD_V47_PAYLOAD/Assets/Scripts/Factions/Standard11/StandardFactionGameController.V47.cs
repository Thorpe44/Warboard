using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class StandardFactionGameController
{
    private static bool v47RouterInstalled;

    private void V47RefreshArmy()
    {
        EnsureV47Router();

        WarboardEnhancementRegistry47.SyncFromController(
            this
        );
    }

    private void V47HandleCoreGameEvent(
        GameEventContext context)
    {
        EnsureV47Router();

        if (context == null)
            return;

        // Scope cleanup itself is generic. These mirrors make the old v46
        // properties and the new state store describe the same live choices.
        if (!Game.IsXcomMode &&
            context.Type ==
                GameEventType.AttackResolved &&
            packId == "space_marines" &&
            HasDetachment(
                "BASTION TASK FORCE") &&
            context.Source != null &&
            context.Target != null &&
            context.Source.HasKeyword(
                "BATTLELINE") &&
            string.Equals(
                context.Source.FactionId,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            SquadController scanSource =
                context.Source.JoinedActionController();

            SquadController scanTarget =
                context.Target.JoinedActionController();

            Game.StandardOpenRuleChoice(
                "AUSPEX SCAN - MANUAL DICE",
                "Did the BATTLELINE unit score one or more hits in the attack you just resolved?",
                new[]
                {
                    new RuleChoiceOption(
                        "YES - ONE OR MORE HITS",
                        () =>
                        {
                            Game.StandardCloseRuleChoice();

                            WarboardRuleStateStore47.SetUnitTarget(
                                "BASTION_AUSPEX_SCANNED",
                                FactionId,
                                scanSource,
                                scanTarget,
                                WarboardRuleScope47.Turn,
                                "Manual Traditional-mode auspex scan confirmation."
                            );

                            Game.StandardLog(
                                "BASTION",
                                "Auspex Scanned",
                                scanTarget.DisplayName +
                                " marked scanned from the manually resolved attack."
                            );
                        }),
                    new RuleChoiceOption(
                        "NO HITS",
                        () =>
                        {
                            Game.StandardCloseRuleChoice();
                        })
                }
            );
        }

        if (context.Type ==
                GameEventType.TurnStarted)
        {
            foreach (WarboardEnhancementAssignment47 assignment
                in WarboardEnhancementRegistry47
                    .ForFaction(FactionId))
            {
                if (assignment == null ||
                    assignment.Bearer == null ||
                    StandardFactionPack11.Normalize(
                        assignment.EnhancementName) !=
                    StandardFactionPack11.Normalize(
                        "ADAPTIVE BIOLOGY"))
                {
                    continue;
                }

                if (assignment.Bearer
                    .HasAnyLostWoundsOrModels())
                {
                    WarboardRuleStateStore47.SetUnitFlag(
                        "ADAPTIVE_BIOLOGY_FNP4",
                        FactionId,
                        assignment.Bearer,
                        WarboardRuleScope47.Battle,
                        "Adaptive Biology upgraded to Feel No Pain 4+ at the start of a turn while the bearer was wounded."
                    );
                }
            }
        }

        if (context.Type ==
                GameEventType.PhaseStarted &&
            context.Phase ==
                GameController.Phase.Command &&
            string.Equals(
                context.ActingFaction,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            if (packId == "space_marines")
            {
                WarboardRuleStateStore47.Remove(
                    "OATH_OF_MOMENT",
                    FactionId
                );
            }

            if (packId == "orks")
            {
                WarboardRuleStateStore47.Remove(
                    "ORK_PREY",
                    FactionId
                );
                WarboardRuleStateStore47.Remove(
                    "ORK_LOOT_OBJECTIVE",
                    FactionId
                );
            }
        }
    }

    private static void EnsureV47Router()
    {
        if (v47RouterInstalled)
            return;

        WarboardRuleEventBus47.Raised +=
            RouteV47Event;

        v47RouterInstalled = true;
    }

    private static void RouteV47Event(
        WarboardRuleEvent47 context)
    {
        if (context == null)
            return;

        FactionControllerHost host =
            FactionControllerHost.Instance;

        if (host == null)
            return;

        foreach (StandardFactionGameController controller
            in host.Controllers.Values
                .OfType<StandardFactionGameController>()
                .ToArray())
        {
            if (controller != null)
                controller.HandleV47RuleEvent(context);
        }
    }

    private void HandleV47RuleEvent(
        WarboardRuleEvent47 context)
    {
        if (context == null ||
            Game == null ||
            pack == null)
        {
            return;
        }

        if (context.Type ==
                WarboardRuleEventType47.CoreEvent &&
            context.CoreContext != null &&
            context.CoreContext.Type ==
                GameEventType.PhaseStarted &&
            context.CoreContext.Phase ==
                GameController.Phase.Move &&
            packId == "space_marines" &&
            HasDetachment(
                "SUBVERSION ASSETS") &&
            !string.Equals(
                context.CoreContext.ActingFaction,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            OfferCloakedPositionReaction47();
        }

        if (context.Type ==
                WarboardRuleEventType47
                    .UnitSetUpFromReserves &&
            packId == "tyranids" &&
            HasDetachment(
                "SUBTERRANEAN ASSAULT") &&
            context.Source != null &&
            string.Equals(
                context.Source.FactionId,
                FactionId,
                StringComparison.OrdinalIgnoreCase) &&
            context.Source.HasKeyword(
                "BURROWER"))
        {
            BeginTunnelMarkerPlacement(
                context.Source
            );
        }

        if (context.Type ==
                WarboardRuleEventType47.AttackSummary &&
            packId == "space_marines" &&
            HasDetachment(
                "BASTION TASK FORCE") &&
            context.Source != null &&
            context.Target != null &&
            context.Hits > 0 &&
            string.Equals(
                context.Source.FactionId,
                FactionId,
                StringComparison.OrdinalIgnoreCase) &&
            context.Source.HasKeyword(
                "BATTLELINE"))
        {
            WarboardRuleStateStore47.SetUnitTarget(
                "BASTION_AUSPEX_SCANNED",
                FactionId,
                context.Source,
                context.Target,
                WarboardRuleScope47.Turn,
                "Auspex scanned by a BATTLELINE attack that scored one or more hits."
            );

            Game.StandardLog(
                "BASTION",
                "Auspex Scanned",
                context.Target.DisplayName +
                " is auspex scanned until the end of this turn."
            );
        }
    }

    private void BeginTunnelMarkerPlacement(
        SquadController burrower)
    {
        WarboardMarkerSystem47 markers =
            WarboardMarkerSystem47.Instance;

        if (markers == null ||
            burrower == null)
        {
            return;
        }

        markers.BeginPlacement(
            new WarboardMarkerPlacementRequest47
            {
                Type = "TYRANID_TUNNEL",
                Label = "40MM TUNNEL MARKER",
                FactionId = FactionId,
                VisualDiameter = 1.57f,
                SourceUnit =
                    burrower.JoinedActionController(),
                MaximumDistanceFromSource = 1f,
                MinimumEnemyDistance = 3f,
                Scope = WarboardRuleScope47.Battle,
                Color = new Color(
                    0.62f,
                    0.18f,
                    0.76f,
                    1f
                ),
                Completed = marker =>
                {
                    Game.StandardLog(
                        "TYRANIDS",
                        "Tunnel Marker",
                        "Placed within 1 inch of " +
                        burrower.DisplayName +
                        " and more than 3 inches from enemy units."
                    );
                }
            }
        );
    }

    private void OfferCloakedPositionReaction47()
    {
        if (Game == null ||
            Pack == null ||
            Game.GetCommandPoints(
                FactionId) < 1)
        {
            return;
        }

        StandardFactionStratagem11 stratagem =
            V47FindStratagem(
                "CLOAKED POSITION");

        if (stratagem == null)
            return;

        List<SquadController> eligible =
            army
                .Where(unit =>
                    unit != null &&
                    unit.IsAlive &&
                    unit.IsOnBattlefield &&
                    !Game.StandardIsEngaged(unit) &&
                    (unit.HasKeyword("PHOBOS") ||
                     unit.DisplayName.IndexOf(
                        "Scout Squad",
                        StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(unit =>
                    unit.JoinedActionController())
                .Distinct()
                .ToList();

        if (eligible.Count == 0)
            return;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController unit in eligible)
        {
            SquadController captured = unit;

            options.Add(
                new RuleChoiceOption(
                    "USE ON " +
                    captured.DisplayName +
                    " (" +
                    stratagem.cost +
                    " CP)",
                    () =>
                    {
                        Game.StandardCloseRuleChoice();

                        if (!Game.SpendStratagemCPForUnit(
                                captured,
                                stratagem.cost,
                                stratagem.name))
                        {
                            Game.StandardSetStatus(
                                "Cloaked Position is not currently available for that unit."
                            );
                            return;
                        }

                        WarboardRuleStateStore47.SetUnitTarget(
                            "SUBVERSION_CLOAKED_POSITION",
                            FactionId,
                            captured,
                            captured,
                            WarboardRuleScope47.Turn,
                            "-3 inch detection range until end of turn."
                        );

                        Game.StandardLog(
                            "STRATAGEM",
                            stratagem.name,
                            stratagem.FullRule +
                            "\nV47 REACTION: " +
                            captured.DisplayName +
                            " has -3 inches detection range until the end of the turn."
                        );
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "DECLINE",
                () =>
                {
                    Game.StandardCloseRuleChoice();
                })
        );

        Game.StandardOpenRuleChoice(
            "REACTION - CLOAKED POSITION",
            "Start of the opponent's Movement phase. Choose one eligible unengaged PHOBOS/SCOUT SQUAD unit, or decline.",
            options.ToArray()
        );
    }

    private StandardFactionStratagem11 V47FindStratagem(
        string wantedName)
    {
        if (Pack == null ||
            Pack.detachments == null)
        {
            return null;
        }

        string wanted =
            StandardFactionPack11.Normalize(
                wantedName);

        HashSet<string> active =
            new HashSet<string>(
                SelectedDetachments.Select(
                    StandardFactionPack11.Normalize),
                StringComparer.OrdinalIgnoreCase);

        return Pack.detachments
            .Where(detachment =>
                detachment != null &&
                active.Contains(
                    StandardFactionPack11.Normalize(
                        detachment.name)))
            .SelectMany(detachment =>
                detachment.stratagems ??
                new StandardFactionStratagem11[0])
            .FirstOrDefault(stratagem =>
                stratagem != null &&
                StandardFactionPack11.Normalize(
                    stratagem.name) == wanted);
    }

    public void V47BeginDetection(
        SquadController source)
    {
        if (Game == null ||
            source == null ||
            packId != "space_marines" ||
            !HasDetachment(
                "SUBVERSION ASSETS"))
        {
            return;
        }

        source = source.JoinedActionController();

        List<SquadController> targets =
            Game.StandardEnemyUnits(
                FactionId)
                .Where(
                    enemy =>
                        enemy != null &&
                        enemy.IsAlive &&
                        enemy.IsOnBattlefield &&
                        Game.StandardDistance(
                            source,
                            enemy) <= 12.001f &&
                        Game.StandardUnitVisibleToUnit(
                            source,
                            enemy))
                .ToList();

        if (targets.Count == 0)
        {
            Game.StandardSetStatus(
                "No visible enemy unit is within 12 inches for detection."
            );
            return;
        }

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController enemy in targets)
        {
            SquadController captured = enemy;

            options.Add(
                new RuleChoiceOption(
                    captured.DisplayName,
                    () =>
                    {
                        Game.StandardCloseRuleChoice();

                        WarboardRuleStateStore47.SetUnitTarget(
                            "SUBVERSION_DETECTED",
                            FactionId,
                            source,
                            captured,
                            WarboardRuleScope47.Battle,
                            "Detected enemy unit"
                        );

                        Game.StandardLog(
                            "SUBVERSION",
                            "Detected Target",
                            captured.DisplayName +
                            " has been detected."
                        );
                    }
                )
            );
        }

        Game.StandardOpenRuleChoice(
            "DETECT ENEMY UNIT",
            "Select a visible enemy unit within 12 inches.",
            options.ToArray()
        );
    }
}
