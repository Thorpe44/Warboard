using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameController
{
    // WARBOARD_V41_CORE_COMPLETION

    private int core11TurnSerial;

    private readonly Dictionary<SquadController, int>
        core11LastShotTurn =
            new Dictionary<SquadController, int>();

    private readonly HashSet<SquadController>
        core11SmokescreenUnits =
            new HashSet<SquadController>();

    private readonly HashSet<ModelToken>
        core11EpicChallengeModels =
            new HashSet<ModelToken>();

    private readonly HashSet<string>
        core11InsaneBraveryUsedByFaction =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

    private readonly HashSet<SquadController>
        core11EmbarkedThisPhase =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        core11DisembarkedThisTurn =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        core11CannotChargeThisTurn =
            new HashSet<SquadController>();

    private bool core11EndMoveWindowResolved;
    private bool core11EndChargeWindowResolved;

    private SquadController core11DisembarkPassenger;
    private SquadController core11DisembarkTransport;
    private float core11DisembarkDistance;
    private string core11DisembarkMode = "";

    private readonly Queue<SquadController>
        core11EmergencyDisembarkQueue =
            new Queue<SquadController>();

    private SquadController core11EmergencyTransport;

    private readonly HashSet<SquadController>
        core11EmergencyHandledTransports =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        core11PendingDestroyedTransports =
            new HashSet<SquadController>();

    private SquadController core11HeroicChargeUnit;
    private SquadController core11HeroicChargeTarget;
    private float core11HeroicChargeDistance;

    private SquadController core11ForcedNextFightUnit;

    private readonly HashSet<SquadController>
        core11CounteroffensiveFightsFirstUnits =
            new HashSet<SquadController>();

    private bool core11CounteroffensiveDecisionPending;
    private SquadController core11CounteroffensiveCompletedUnit;

    private void Core11Install()
    {
        GameEventBus.Raised -=
            Core11HandleGameEvent;

        GameEventBus.Raised +=
            Core11HandleGameEvent;

        CoreRules11FlightRegistry.Clear();
        core11EmergencyHandledTransports.Clear();
    }

    private void Core11Uninstall()
    {
        GameEventBus.Raised -=
            Core11HandleGameEvent;

        CoreRules11FlightRegistry.Clear();
    }

    private void Core11HandleGameEvent(
        GameEventContext context)
    {
        if (context == null ||
            context.Game != this)
        {
            return;
        }

        switch (context.Type)
        {
            case GameEventType.TurnStarted:
                core11TurnSerial++;
                core11DisembarkedThisTurn.Clear();
                core11CannotChargeThisTurn.Clear();
                break;

            case GameEventType.PhaseStarted:
                foreach (SquadController unit
                    in core11CounteroffensiveFightsFirstUnits)
                {
                    if (unit != null)
                        unit.TemporaryFightsFirst = false;
                }
                core11CounteroffensiveFightsFirstUnits.Clear();

                core11EndMoveWindowResolved = false;
                core11EndChargeWindowResolved = false;
                core11EmbarkedThisPhase.Clear();
                core11SmokescreenUnits.Clear();
                core11EpicChallengeModels.Clear();
                CoreRules11FlightRegistry.Clear();

                if (context.Phase == Phase.Shoot)
                    Core11OfferSmokescreenWindow();
                break;

            case GameEventType.AttackResolved:
                Core11ResolvePendingDestroyedTransports();
                break;

            case GameEventType.UnitFinishedShooting:
                if (context.Source != null)
                {
                    core11LastShotTurn[
                        context.Source.JoinedActionController()
                    ] = core11TurnSerial;
                }
                break;

            case GameEventType.UnitSelectedToFight:
                if (context.Source != null)
                {
                    SquadController selected =
                        context.Source.JoinedActionController();

                    if (core11ForcedNextFightUnit == selected)
                        core11ForcedNextFightUnit = null;

                    Core11OfferEpicChallenge(selected);
                }
                break;

            case GameEventType.UnitFinishedFighting:
                Core11OfferCounteroffensive(
                    context.Source
                );
                break;

            case GameEventType.UnitDestroyed:
                if (context.Source != null &&
                    context.Source.HasKeyword("TRANSPORT") &&
                    context.Source.EmbarkedPassengers.Count > 0)
                {
                    Core11BeginEmergencyDisembark(
                        context.Source
                    );
                }
                break;

            case GameEventType.TurnEnded:
                Core11ReturnAircraftAtEndOfOpponentTurn(
                    context.ActingFaction
                );
                break;
        }
    }

    private bool Core11CanAdvancePhase(
        out string reason)
    {
        reason = "";

        Core11ResolvePendingDestroyedTransports();

        if (core11DisembarkPassenger != null)
        {
            reason =
                "Finish the pending disembark placement before changing phase.";
            return false;
        }

        if (core11HeroicChargeUnit != null)
        {
            reason =
                "Finish the Heroic Intervention charge move before changing phase.";
            return false;
        }

        if (core11EmergencyDisembarkQueue.Count > 0)
        {
            reason =
                "Finish all emergency disembark placements before continuing.";
            return false;
        }

        if (core11CounteroffensiveDecisionPending)
        {
            reason =
                "Resolve the pending Counteroffensive decision before continuing.";
            return false;
        }

        if (reservePlacementSquad != null)
        {
            reason =
                "Finish the reserve/ingress placement before changing phase.";
            return false;
        }

        if (phase == Phase.Move &&
            !core11EndMoveWindowResolved &&
            Core11OpenRapidIngressWindow())
        {
            reason =
                "Resolve the end-of-Movement Rapid Ingress window first.";
            return false;
        }

        if (phase == Phase.Charge &&
            !core11EndChargeWindowResolved &&
            Core11OpenHeroicInterventionWindow())
        {
            reason =
                "Resolve the end-of-Charge Heroic Intervention window first.";
            return false;
        }

        return true;
    }

    private bool Core11CanSeeModel(
        ModelToken observer,
        ModelToken target)
    {
        if (!CoreRules11Terrain
            .LineVisibleIgnoringHidden(
                observer,
                target
            ))
        {
            return false;
        }

        if (!Core11ModelIsHidden(target))
            return true;

        float detectionRange =
            CoreRules11Terrain.HiddenDetectionRange +
            CustodesFactionPack11.DetectionRangeBonus(
                target.Squad != null
                    ? target.Squad.JoinedActionController()
                    : null) +
            NecronsFactionPack11.DetectionRangeBonus(
                target.Squad != null
                    ? target.Squad.JoinedActionController()
                    : null) +
            // WARBOARD_V47_DETECTION_RANGE_STATE
            WarboardFactionExtensionHub.DetectionRangeModifier(
                target.Squad != null
                    ? target.Squad.JoinedActionController()
                    : null);

        if (Core11GoneToGround(target))
        {
            detectionRange -=
                CoreRules11Terrain
                    .GoneToGroundDetectionPenalty;
        }

        return CoreRules11Terrain.ModelDistance(
            observer,
            target
        ) <= detectionRange + 0.001f;
    }

    private bool Core11ModelIsHidden(
        ModelToken model)
    {
        if (model == null ||
            model.Squad == null)
        {
            return false;
        }

        SquadController unit =
            model.Squad.JoinedActionController();

        if (!(unit.HasKeyword("INFANTRY") ||
              unit.HasKeyword("BEASTS") ||
              unit.HasKeyword("SWARM")))
        {
            return false;
        }

        if (!CoreRules11Terrain
            .ModelInsideLightOrDenseArea(model))
        {
            return false;
        }

        int lastShot;
        if (!core11LastShotTurn.TryGetValue(
                unit,
                out lastShot))
        {
            return true;
        }

        return lastShot <
            core11TurnSerial - 1;
    }

    private bool Core11GoneToGround(
        ModelToken model)
    {
        if (!Core11ModelIsHidden(model))
            return false;

        if (model == null ||
            model.Squad == null)
        {
            return false;
        }

        SquadController unit =
            model.Squad.JoinedActionController();

        int lastShot;
        bool hasShotRecently =
            core11LastShotTurn.TryGetValue(
                unit,
                out lastShot) &&
            lastShot >= core11TurnSerial - 1;

        if (hasShotRecently)
            return false;

        // Warboard's dense collider is used as the physical proxy for the
        // intervening dense terrain requirement.
        return CoreRules11Terrain.AllTerrain().Any(
            terrain =>
                CoreRules11Terrain.Category(terrain) ==
                    CoreTerrainCategory11.Dense &&
                CoreRules11Terrain.ModelInsideTerrainArea(
                    model,
                    terrain
                )
        );
    }

    private bool Core11TargetUnitHasCoverFromShooter(
        ModelToken shooter,
        SquadController target)
    {
        if (shooter == null ||
            target == null)
        {
            return false;
        }

        target = target.JoinedActionController();

        if (core11SmokescreenUnits.Contains(target))
            return true;

        List<ModelToken> living =
            target.JoinedLivingModelTokens()
                .Where(model => model != null && model.IsAlive)
                .ToList();

        if (living.Count == 0)
            return false;

        return living.All(
            model =>
                CoreRules11Terrain
                    .ModelHasTerrainCoverCondition(
                        shooter,
                        model
                    ) ||
                Core11SmokeIntervenes(
                    shooter,
                    model
                )
        );
    }

    private bool Core11TargetModelHasCoverFromShooter(
        ModelToken shooter,
        ModelToken target)
    {
        if (shooter == null || target == null)
            return false;

        SquadController unit =
            target.Squad != null
            ? target.Squad.JoinedActionController()
            : null;

        return
            (unit != null &&
             core11SmokescreenUnits.Contains(unit)) ||
            CoreRules11Terrain
                .ModelHasTerrainCoverCondition(
                    shooter,
                    target
                ) ||
            Core11SmokeIntervenes(
                shooter,
                target
            );
    }

    private bool Core11SmokeIntervenes(
        ModelToken shooter,
        ModelToken target)
    {
        if (shooter == null || target == null)
            return false;

        Vector2 a = new Vector2(
            shooter.transform.position.x,
            shooter.transform.position.z
        );
        Vector2 b = new Vector2(
            target.transform.position.x,
            target.transform.position.z
        );

        foreach (SquadController smoke
            in core11SmokescreenUnits)
        {
            if (smoke == null || !smoke.IsAlive)
                continue;

            foreach (ModelToken model
                in smoke.JoinedLivingModelTokens())
            {
                if (model == null ||
                    model == shooter ||
                    model == target)
                {
                    continue;
                }

                Vector2 p = new Vector2(
                    model.transform.position.x,
                    model.transform.position.z
                );

                if (Core11DistancePointToSegment(
                        p,
                        a,
                        b) <=
                    Mathf.Max(
                        0.20f,
                        model.BaseRadiusInches
                    ))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static float Core11DistancePointToSegment(
        Vector2 p,
        Vector2 a,
        Vector2 b)
    {
        Vector2 ab = b - a;
        float square = ab.sqrMagnitude;

        if (square <= 0.0001f)
            return Vector2.Distance(p, a);

        float t = Mathf.Clamp01(
            Vector2.Dot(p - a, ab) /
            square
        );

        return Vector2.Distance(
            p,
            a + ab * t
        );
    }

    public bool Core11PlungingFireApplies(
        ModelToken attacker,
        SquadController target)
    {
        if (attacker == null ||
            target == null ||
            attacker.Squad == null)
        {
            return false;
        }

        SquadController attackUnit =
            attacker.Squad.JoinedActionController();

        target = target.JoinedActionController();

        if (attackUnit.HasKeyword("AIRCRAFT") ||
            target.HasKeyword("AIRCRAFT"))
        {
            return false;
        }

        bool targetOnGround =
            target.JoinedLivingModelTokens()
                .Any(
                    model =>
                        model != null &&
                        CoreRules11Geometry.ModelBasePlaneY(model) <=
                            0.15f
                );

        if (!targetOnGround)
            return false;

        if (CoreRules11Geometry
                .ModelBasePlaneY(attacker) >=
            3f)
        {
            return true;
        }

        if (attackUnit.HasKeyword("TOWERING") &&
            target.JoinedLivingModelTokens()
                .Any(
                    model =>
                        CoreRules11Terrain.ModelDistance(
                            attacker,
                            model
                        ) <= 12f + 0.001f
                ))
        {
            return true;
        }

        return false;
    }

    private bool Core11IsEngagedForNormalMovement(
        SquadController unit)
    {
        if (unit == null)
            return false;

        List<SquadController> enemies =
            EngagedEnemies(unit);

        if (enemies.Count == 0)
            return false;

        return enemies.Any(
            enemy =>
                enemy != null &&
                !enemy.HasKeyword("AIRCRAFT")
        );
    }

    private bool Core11NormalMovePathIsClear(
        ModelToken model,
        Vector3 destination)
    {
        if (model == null || model.Squad == null)
            return false;

        SquadController unit =
            model.Squad.JoinedActionController();

        if (CoreRules11FlightRegistry.IsTakingToSkies(unit))
            return true;

        Vector3 start = model.transform.position;
        Vector3 vector = destination - start;
        vector.y = 0f;

        float distance = vector.magnitude;
        if (distance <= 0.001f)
            return true;

        bool fallbackMove =
            Core11IsEngagedForNormalMovement(unit);

        RaycastHit[] hits = Physics.RaycastAll(
            start + Vector3.up * 0.35f,
            vector.normalized,
            distance,
            ~0,
            QueryTriggerInteraction.Collide
        );

        foreach (RaycastHit hit
            in hits.OrderBy(value => value.distance))
        {
            if (hit.collider == null)
                continue;

            ModelToken otherModel =
                hit.collider.GetComponentInParent<ModelToken>();

            if (otherModel != null)
            {
                if (otherModel == model ||
                    otherModel.Squad == null)
                {
                    continue;
                }

                SquadController otherUnit =
                    otherModel.Squad.JoinedActionController();

                // 03.01: models can always move through friendly models.
                if (otherUnit.FactionId == unit.FactionId)
                    continue;

                // 23.02: all move types can move through AIRCRAFT models.
                if (otherUnit.HasKeyword("AIRCRAFT"))
                    continue;

                // 17.01 applies to Normal/Advance only, not Fall Back.
                if (!fallbackMove &&
                    (unit.HasKeyword("MONSTER") ||
                     unit.HasKeyword("VEHICLE")) &&
                    !(otherUnit.HasKeyword("MONSTER") ||
                      otherUnit.HasKeyword("VEHICLE")))
                {
                    continue;
                }

                // Otherwise the moving base cannot pass through enemy models.
                return false;
            }

            TerrainFeature terrain =
                hit.collider.GetComponentInParent<TerrainFeature>();

            if (terrain == null)
                continue;

            CoreTerrainCategory11 category =
                CoreRules11Terrain.Category(terrain);

            if (category == CoreTerrainCategory11.Exposed ||
                category == CoreTerrainCategory11.Light)
            {
                continue;
            }

            if (unit.HasKeyword("INFANTRY") ||
                unit.HasKeyword("BEASTS") ||
                unit.HasKeyword("SWARM") ||
                unit.HasKeyword("MOBILE"))
            {
                continue;
            }

            Collider col =
                CoreRules11Terrain.TerrainCollider(terrain);

            if (col != null &&
                col.bounds.size.y <= 2f + 0.001f)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private bool Core11WholeSquadPathIsClear(
        SquadController unit,
        Vector3 delta)
    {
        if (unit == null)
            return false;

        return JoinedModels(unit).All(
            model =>
                model == null ||
                Core11NormalMovePathIsClear(
                    model,
                    model.transform.position + delta
                )
        );
    }

    private SquadController Core11FindNearestSurgeEnemy(
        SquadController unit)
    {
        if (unit == null)
            return null;

        unit = unit.JoinedActionController();

        IEnumerable<SquadController> candidates =
            squads
                .Where(
                    enemy =>
                        enemy != null &&
                        enemy.IsAlive &&
                        enemy.IsOnBattlefield &&
                        !enemy.IsAttachedLeader &&
                        enemy.FactionId != unit.FactionId
                )
                .Select(
                    enemy =>
                        enemy.JoinedActionController()
                )
                .Distinct();

        if (!unit.HasKeyword("FLY"))
        {
            candidates = candidates.Where(
                enemy =>
                    !enemy.HasKeyword("AIRCRAFT")
            );
        }

        return candidates
            .OrderBy(
                enemy =>
                    JoinedDistance(unit, enemy)
            )
            .FirstOrDefault();
    }

    private bool Core11AircraftChargeAllowed(
        SquadController attacker,
        SquadController target)
    {
        string reason;

        if (CoreRules11Aircraft.CanDeclareCharge(
                attacker,
                target,
                out reason))
        {
            return true;
        }

        status = reason;
        return false;
    }

    private List<SquadController>
        Core11ForcedFightSelection(
            string faction,
            bool fightsFirstOnly)
    {
        if (core11ForcedNextFightUnit == null)
            return null;

        SquadController forced =
            core11ForcedNextFightUnit
                .JoinedActionController();

        if (!forced.IsAlive ||
            forced.HasFought)
        {
            core11ForcedNextFightUnit = null;
            return null;
        }

        if (!string.Equals(
                forced.FactionId,
                faction,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (fightsFirstOnly &&
            !UnitHasFightsFirst(forced))
        {
            return new List<SquadController>();
        }

        return new List<SquadController>
        {
            forced
        };
    }

    private void DrawCore11ContextControls(
        Rect bar,
        ref float x)
    {
        if (!Core11HasContextOptions())
            return;

        float width = 118f;

        Rect button = new Rect(
            x,
            bar.y + 3f,
            width,
            Mathf.Max(24f, bar.height - 6f)
        );

        if (GUI.Button(
                button,
                "CORE 11e"))
        {
            Core11OpenContextMenu();
        }

        x += width + 6f;
    }

    private bool Core11HasContextOptions()
    {
        if (selectedSquad == null ||
            !selectedSquad.IsAlive)
        {
            return false;
        }

        SquadController unit =
            selectedSquad.JoinedActionController();

        if (phase == Phase.Move)
        {
            return
                (unit.HasKeyword("FLY") &&
                 !unit.HasMoved) ||
                Core11EmbarkTargets(unit).Count > 0 ||
                (unit.HasKeyword("TRANSPORT") &&
                 (unit.EmbarkedPassengers.Count > 0 ||
                  CoreRules11TransportRules.Capacity(unit) <= 0));
        }

        if (phase == Phase.Shoot)
        {
            return Core11CanUseExplosives(unit);
        }

        if (phase == Phase.Charge)
        {
            return
                (unit.HasKeyword("FLY") &&
                 !unit.MadeChargeMove) ||
                Core11CanUseCrushingImpact(unit);
        }

        return false;
    }

    private void Core11OpenContextMenu()
    {
        if (selectedSquad == null)
            return;

        SquadController unit =
            selectedSquad.JoinedActionController();

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        if ((phase == Phase.Move ||
             phase == Phase.Charge) &&
            unit.HasKeyword("FLY"))
        {
            bool taking =
                CoreRules11FlightRegistry
                    .IsTakingToSkies(unit);

            options.Add(
                new RuleChoiceOption(
                    taking
                    ? "Cancel Take to the Skies"
                    : "Take to the Skies (-2\" move)",
                    () =>
                    {
                        CoreRules11FlightRegistry
                            .SetTakingToSkies(
                                unit,
                                !taking
                            );
                        CloseRuleChoice();
                        RefreshMoveRing();
                        status =
                            !taking
                            ? unit.DisplayName +
                              " will Take to the Skies for its next eligible move."
                            : unit.DisplayName +
                              " will use normal movement rules.";
                    }
                )
            );
        }

        if (phase == Phase.Move)
        {
            foreach (SquadController transport
                in Core11EmbarkTargets(unit))
            {
                SquadController captured = transport;

                options.Add(
                    new RuleChoiceOption(
                        "Embark: " +
                        captured.DisplayName,
                        () =>
                        {
                            CloseRuleChoice();
                            Core11Embark(
                                unit,
                                captured
                            );
                        }
                    )
                );
            }

            if (unit.HasKeyword("TRANSPORT"))
            {
                if (CoreRules11TransportRules.Capacity(unit) <= 0)
                {
                    options.Add(
                        new RuleChoiceOption(
                            "Set Transport Capacity",
                            () =>
                            {
                                CloseRuleChoice();
                                Core11OpenManualTransportCapacity(
                                    unit
                                );
                            }
                        )
                    );
                }

                foreach (SquadController passenger
                    in unit.EmbarkedPassengers
                        .Where(value =>
                            value != null &&
                            value.IsAlive)
                        .ToList())
                {
                    SquadController captured = passenger;

                    options.Add(
                        new RuleChoiceOption(
                            "Disembark: " +
                            captured.DisplayName,
                            () =>
                            {
                                CloseRuleChoice();
                                Core11BeginDisembark(
                                    captured,
                                    unit
                                );
                            }
                        )
                    );
                }
            }
        }

        if (phase == Phase.Shoot &&
            Core11CanUseExplosives(unit))
        {
            options.Add(
                new RuleChoiceOption(
                    "Explosives (1CP)",
                    () =>
                    {
                        CloseRuleChoice();
                        Core11UseExplosives(unit);
                    }
                )
            );
        }

        if (phase == Phase.Charge &&
            Core11CanUseCrushingImpact(unit))
        {
            options.Add(
                new RuleChoiceOption(
                    "Crushing Impact (1CP)",
                    () =>
                    {
                        CloseRuleChoice();
                        Core11UseCrushingImpact(unit);
                    }
                )
            );
        }

        if (options.Count == 0)
            return;

        options.Add(
            new RuleChoiceOption(
                "Close",
                CloseRuleChoice
            )
        );

        OpenRuleChoice(
            "CORE 11e",
            "Contextual core rules available for " +
            unit.DisplayName + ".",
            options
        );
    }

    private void Core11OpenManualTransportCapacity(
        SquadController transport)
    {
        if (transport == null)
            return;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        for (int capacity = 1;
             capacity <= 30;
             capacity++)
        {
            int captured = capacity;

            options.Add(
                new RuleChoiceOption(
                    captured + " models",
                    () =>
                    {
                        CloseRuleChoice();

                        CoreRules11TransportRules
                            .SetManualCapacity(
                                transport,
                                captured
                            );

                        status =
                            transport.DisplayName +
                            " transport capacity set to " +
                            captured +
                            " for this battle.";
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "Cancel",
                CloseRuleChoice
            )
        );

        OpenRuleChoice(
            "TRANSPORT CAPACITY  -  " +
            transport.DisplayName,
            "The imported datasheet text did not expose a transport capacity. Select the printed datasheet capacity; Warboard will not invent one.",
            options
        );
    }

    private List<SquadController> Core11EmbarkTargets(
        SquadController passenger)
    {
        List<SquadController> result =
            new List<SquadController>();

        if (passenger == null ||
            !passenger.IsOnBattlefield ||
            passenger.WasSetUpThisTurn ||
            core11DisembarkedThisTurn.Contains(passenger))
        {
            return result;
        }

        if (!(passenger.HasMoved ||
              passenger.HasAdvanced ||
              passenger.HasFallenBack))
        {
            return result;
        }

        foreach (SquadController transport
            in squads)
        {
            if (transport == null ||
                !transport.IsAlive ||
                !transport.IsOnBattlefield ||
                transport.IsAttachedLeader ||
                transport.FactionId != passenger.FactionId)
            {
                continue;
            }

            string reason;
            if (!CoreRules11TransportRules.CanCarry(
                    transport,
                    passenger,
                    out reason))
            {
                continue;
            }

            if (!Core11EveryModelWithinTransportDistance(
                    passenger,
                    transport,
                    3f))
            {
                continue;
            }

            result.Add(transport);
        }

        return result;
    }

    private bool Core11EveryModelWithinTransportDistance(
        SquadController passenger,
        SquadController transport,
        float distance)
    {
        List<ModelToken> passengers =
            passenger.JoinedLivingModelTokens();

        List<ModelToken> transports =
            transport.JoinedLivingModelTokens();

        if (passengers.Count == 0 ||
            transports.Count == 0)
        {
            return false;
        }

        return passengers.All(
            model =>
                transports.Any(
                    carrier =>
                        CoreRules11Terrain.ModelDistance(
                            model,
                            carrier
                        ) <= distance + 0.001f
                )
        );
    }

    private void Core11Embark(
        SquadController passenger,
        SquadController transport)
    {
        if (passenger != null &&
            !Aeldari11CanEmbark(passenger))
        {
            status = passenger.DisplayName +
                " cannot embark this turn because of an Aeldari rule.";
            return;
        }


        if (passenger == null || transport == null)
            return;

        string reason;
        if (!CoreRules11TransportRules.CanCarry(
                transport,
                passenger,
                out reason) ||
            !Core11EveryModelWithinTransportDistance(
                passenger,
                transport,
                3f))
        {
            status =
                string.IsNullOrWhiteSpace(reason)
                ? "That unit cannot embark there."
                : reason;
            return;
        }

        if (!passenger.EmbarkWithin(transport))
        {
            status = "Embark failed.";
            return;
        }

        core11EmbarkedThisPhase.Add(passenger);

        GameEventBus.Raise(
            new GameEventContext
            {
                Type = GameEventType.UnitEmbarked,
                Game = this,
                ActingFaction = passenger.FactionId,
                Phase = phase,
                Source = passenger,
                Target = transport,
                Note = "18.02 Embark"
            }
        );

        AppendBattleLog(
            "TRANSPORT",
            "Embark",
            passenger.DisplayName +
            " embarked within " +
            transport.DisplayName + "."
        );

        ClearSelection();
        status =
            passenger.DisplayName +
            " is embarked within " +
            transport.DisplayName + ".";
    }

    private void Core11BeginDisembark(
        SquadController passenger,
        SquadController transport)
    {
        if (passenger == null ||
            transport == null ||
            !passenger.IsEmbarked ||
            passenger.EmbarkedTransport != transport)
        {
            return;
        }

        if (core11EmbarkedThisPhase.Contains(passenger))
        {
            status =
                "A unit cannot disembark in the same phase it embarked.";
            return;
        }

        if (transport.HasAdvanced ||
            transport.HasFallenBack)
        {
            status =
                "A unit cannot disembark from a TRANSPORT that Advanced or Fell Back this phase.";
            return;
        }

        core11DisembarkPassenger = passenger;
        core11DisembarkTransport = transport;

        // 18.04: after a Normal/Ingress move, Rapid is mandatory.
        if (transport.WasSetUpThisTurn ||
            transport.HasMoved)
        {
            core11DisembarkMode = "Rapid";
            core11DisembarkDistance = 3f;

            status =
                "RAPID DISEMBARK: click a legal unengaged position wholly within 3\" of " +
                transport.DisplayName +
                ". This unit cannot charge this turn.";
            return;
        }

        // Otherwise Tactical is mandatory if it can be set up. Combat is only
        // available when a Tactical setup is not possible.
        OpenRuleChoice(
            "DISEMBARK  -  " + passenger.DisplayName,
            "18.04: use Tactical Disembark if a legal 3\" unengaged setup is possible. Use Combat Disembark only if Tactical setup is impossible.",
            new[]
            {
                new RuleChoiceOption(
                    "Tactical Disembark (3\")",
                    () =>
                    {
                        CloseRuleChoice();
                        core11DisembarkMode = "Tactical";
                        core11DisembarkDistance = 3f;
                        status =
                            "TACTICAL DISEMBARK: click a legal unengaged position wholly within 3\" of " +
                            transport.DisplayName +
                            ". The unit must then make a Normal or Advance move.";
                    }
                ),
                new RuleChoiceOption(
                    "Combat Disembark (6\")",
                    () =>
                    {
                        CloseRuleChoice();
                        Core11BeginCombatDisembark(
                            passenger,
                            transport
                        );
                    }
                ),
                new RuleChoiceOption(
                    "Cancel",
                    () =>
                    {
                        CloseRuleChoice();
                        core11DisembarkPassenger = null;
                        core11DisembarkTransport = null;
                        core11DisembarkDistance = 0f;
                        core11DisembarkMode = "";
                        status = "Disembark cancelled.";
                    }
                )
            }
        );
    }

    private void Core11BeginCombatDisembark(
        SquadController passenger,
        SquadController transport)
    {
        if (passenger == null || transport == null)
            return;

        int modelCount =
            passenger.JoinedLivingModelTokens().Count;

        Action<int> applyFailures =
            failures =>
            {
                bool heavyHazard =
                    passenger.JoinedLivingModelTokens()
                        .Count > 0 &&
                    passenger.JoinedLivingModelTokens()
                        .All(
                            model =>
                                model != null &&
                                model.Squad != null &&
                                (model.Squad.HasKeyword("MONSTER") ||
                                 model.Squad.HasKeyword("VEHICLE"))
                        );

                Core11ApplyMortalWounds(
                    passenger,
                    Mathf.Max(0, failures) *
                        (heavyHazard ? 3 : 1),
                    "Combat Disembark hazard"
                );

                if (!passenger.IsAlive)
                {
                    passenger.DisembarkFromTransport(
                        transport.transform.position
                    );
                    core11DisembarkPassenger = null;
                    core11DisembarkTransport = null;
                    core11DisembarkDistance = 0f;
                    core11DisembarkMode = "";
                    status =
                        passenger.DisplayName +
                        " was destroyed by Combat Disembark hazard rolls.";
                    return;
                }

                core11DisembarkPassenger = passenger;
                core11DisembarkTransport = transport;
                core11DisembarkMode = "Combat";
                core11DisembarkDistance = 6f;

                status =
                    "COMBAT DISEMBARK: place " +
                    passenger.DisplayName +
                    " wholly within 6\" of " +
                    transport.DisplayName +
                    ". It may only end engaged with enemy units that the TRANSPORT is engaged with.";
            };

        if (!IsXcomMode)
        {
            OpenTraditionalNumericPrompt(
                "COMBAT DISEMBARK HAZARD",
                "Roll one hazard die per model. Enter the number of failed rolls (1-2).",
                0,
                modelCount,
                0,
                Mathf.Max(1, modelCount),
                applyFailures
            );
            return;
        }

        int failures = 0;

        for (int i = 0; i < modelCount; i++)
        {
            if (DiceRoller.RollD6(
                    "Combat Disembark hazard") <= 2)
            {
                failures++;
            }
        }

        applyFailures(failures);
    }

    private bool Core11HandleBoardPlacementClick(
        Vector3 destination)
    {
        if (core11HeroicChargeUnit != null)
        {
            Core11TryPlaceHeroicCharge(destination);
            return true;
        }

        if (core11DisembarkPassenger != null)
        {
            Core11TryPlaceDisembark(destination);
            return true;
        }

        return false;
    }

    private void Core11TryPlaceDisembark(
        Vector3 destination)
    {
        SquadController passenger =
            core11DisembarkPassenger;

        SquadController transport =
            core11DisembarkTransport;

        if (passenger == null || transport == null)
            return;

        Vector3 old = passenger.transform.position;

        passenger.DisembarkFromTransport(destination);
        Physics.SyncTransforms();

        bool withinTransportDistance =
            core11DisembarkMode == "Emergency"
            ? Core11EveryModelWithinDestroyedTransportDistance(
                passenger,
                transport,
                core11DisembarkDistance
              )
            : Core11EveryModelWithinTransportDistance(
                passenger,
                transport,
                core11DisembarkDistance
              );

        bool legal =
            AllModelsInsideBoard(passenger) &&
            AllModelsHaveLegalPlacement(passenger) &&
            passenger.IsCoherent() &&
            withinTransportDistance;

        if (legal &&
            (core11DisembarkMode == "Rapid" ||
             core11DisembarkMode == "Tactical"))
        {
            legal = !IsEngaged(passenger);
        }

        if (legal &&
            core11DisembarkMode == "Rapid" &&
            transport.WasSetUpThisTurn)
        {
            legal =
                ReserveEnemyDistanceIsLegal(passenger) &&
                (transport.HasDeepStrike ||
                 (StrategicReserveEdgeIsLegal(passenger) &&
                  Core11RapidDisembarkDeploymentZoneIsLegal(
                      passenger
                  )));
        }

        if (legal &&
            core11DisembarkMode == "Combat" &&
            IsEngaged(passenger))
        {
            HashSet<SquadController> transportEnemies =
                new HashSet<SquadController>(
                    EngagedEnemies(transport)
                        .Select(
                            enemy =>
                                enemy.JoinedActionController()
                        )
                );

            legal = EngagedEnemies(passenger)
                .All(
                    enemy =>
                        transportEnemies.Contains(
                            enemy.JoinedActionController()
                        )
                );
        }

        if (!legal)
        {
            passenger.ReembarkAfterFailedDisembark(
                transport
            );
            passenger.transform.position = old;
            status =
                "Illegal disembark position: stay on-board, preserve coherency/collision rules and remain wholly within the required distance.";
            return;
        }

        string mode = core11DisembarkMode;

        core11DisembarkPassenger = null;
        core11DisembarkTransport = null;
        core11DisembarkDistance = 0f;
        core11DisembarkMode = "";

        core11DisembarkedThisTurn.Add(passenger);

        if (mode == "Rapid")
        {
            passenger.HasMoved = true;
            core11CannotChargeThisTurn.Add(passenger);
        }
        else if (mode == "Combat")
        {
            passenger.HasMoved = true;
            passenger.SetBattleShocked(true, 0);
            core11CannotChargeThisTurn.Add(passenger);
        }
        else if (mode == "Emergency")
        {
            passenger.HasMoved = true;
            passenger.SetBattleShocked(true, 0);
            core11CannotChargeThisTurn.Add(passenger);
        }
        else
        {
            // Tactical Disembark requires the unit to be selected to make a
            // Normal or Advance move after setup, so leave HasMoved clear.
            passenger.HasMoved = false;
        }

        passenger.MarkSetUpThisTurn();

        GameEventBus.Raise(
            new GameEventContext
            {
                Type = GameEventType.UnitDisembarked,
                Game = this,
                ActingFaction = passenger.FactionId,
                Phase = phase,
                Source = passenger,
                Target = transport,
                Note = mode + " Disembark"
            }
        );

        status =
            passenger.DisplayName +
            " completed a " +
            mode +
            " Disembark.";

        RefreshObjectiveDisplays();

        if (mode == "Emergency")
            Core11BeginNextEmergencyPassenger();
    }

    private bool Core11RapidDisembarkDeploymentZoneIsLegal(
        SquadController passenger)
    {
        if (passenger == null ||
            round >= 3)
        {
            return true;
        }

        string opponent =
            factions.FirstOrDefault(
                faction =>
                    !string.Equals(
                        faction,
                        passenger.FactionId,
                        StringComparison.OrdinalIgnoreCase
                    )
            );

        MissionDeploymentZone opponentZone =
            DeploymentZoneForFaction(opponent);

        if (opponentZone == null)
            return true;

        return passenger.JoinedLivingModelTokens()
            .All(
                model =>
                    model == null ||
                    !opponentZone.ContainsBase(
                        model.transform.position,
                        model.BaseRadiusInches
                    )
            );
    }

    private bool Core11EveryModelWithinDestroyedTransportDistance(
        SquadController passenger,
        SquadController transport,
        float distance)
    {
        if (passenger == null || transport == null)
            return false;

        ModelToken[] carrierModels =
            transport.GetComponentsInChildren<ModelToken>(true);

        return passenger.JoinedLivingModelTokens().All(
            model =>
            {
                if (model == null)
                    return true;

                if (carrierModels != null &&
                    carrierModels.Length > 0)
                {
                    return carrierModels.Any(
                        carrier =>
                            carrier != null &&
                            CoreRules11Terrain.ModelDistance(
                                model,
                                carrier
                            ) <= distance + 0.001f
                    );
                }

                Vector2 a = new Vector2(
                    model.transform.position.x,
                    model.transform.position.z
                );
                Vector2 b = new Vector2(
                    transport.transform.position.x,
                    transport.transform.position.z
                );
                return Vector2.Distance(a, b) <=
                    distance + model.BaseRadiusInches + 0.001f;
            }
        );
    }

    private void Core11BeginEmergencyDisembark(
        SquadController transport)
    {
        if (transport == null)
            return;

        transport = transport.JoinedActionController();

        if (core11EmergencyTransport != null ||
            core11EmergencyDisembarkQueue.Count > 0 ||
            (core11DisembarkPassenger != null &&
             core11DisembarkMode == "Emergency"))
        {
            core11PendingDestroyedTransports.Add(
                transport
            );
            return;
        }

        if (!core11EmergencyHandledTransports.Add(
                transport))
        {
            return;
        }

        core11EmergencyTransport = transport;
        core11EmergencyDisembarkQueue.Clear();

        foreach (SquadController passenger
            in transport.EmbarkedPassengers
                .Where(unit => unit != null && unit.IsAlive)
                .ToList())
        {
            core11EmergencyDisembarkQueue.Enqueue(
                passenger
            );
        }

        Core11BeginNextEmergencyPassenger();
    }

    private void Core11BeginNextEmergencyPassenger()
    {
        if (core11EmergencyDisembarkQueue.Count == 0)
        {
            core11EmergencyTransport = null;
            Core11ResolvePendingDestroyedTransports();
            return;
        }

        SquadController passenger =
            core11EmergencyDisembarkQueue.Dequeue();

        if (passenger == null || !passenger.IsAlive)
        {
            Core11BeginNextEmergencyPassenger();
            return;
        }

        int modelCount =
            passenger.JoinedLivingModelTokens().Count;

        Action<int> applyFailures =
            failures =>
            {
                List<ModelToken> living =
                    passenger.JoinedLivingModelTokens();

                bool heavyHazard =
                    living.Count > 0 &&
                    living.All(
                        model =>
                            model != null &&
                            model.Squad != null &&
                            (model.Squad.HasKeyword("MONSTER") ||
                             model.Squad.HasKeyword("VEHICLE"))
                    );

                Core11ApplyMortalWounds(
                    passenger,
                    Mathf.Max(0, failures) *
                        (heavyHazard ? 3 : 1),
                    "Emergency Disembark hazard"
                );

                if (!passenger.IsAlive)
                {
                    passenger.DisembarkFromTransport(
                        core11EmergencyTransport != null
                        ? core11EmergencyTransport.transform.position
                        : passenger.transform.position
                    );

                    Core11BeginNextEmergencyPassenger();
                    return;
                }

                core11DisembarkPassenger = passenger;
                core11DisembarkTransport =
                    core11EmergencyTransport;
                core11DisembarkMode = "Emergency";
                core11DisembarkDistance = 6f;

                status =
                    "EMERGENCY DISEMBARK: place " +
                    passenger.DisplayName +
                    " wholly within 6\" of the destroyed transport, as close as possible. Use an engaged position only if an unengaged setup is impossible.";
            };

        if (!IsXcomMode)
        {
            OpenTraditionalNumericPrompt(
                "EMERGENCY DISEMBARK HAZARD",
                "Roll one hazard die per model. Enter the number of failed rolls (1-2).",
                0,
                modelCount,
                0,
                Mathf.Max(1, modelCount),
                applyFailures
            );
            return;
        }

        int failures = 0;
        for (int i = 0; i < modelCount; i++)
        {
            if (DiceRoller.RollD6(
                    "Emergency Disembark hazard") <= 2)
            {
                failures++;
            }
        }

        applyFailures(failures);
    }

    private void Core11CheckDestroyedTransportForEmergencyDisembark(
        ModelToken model)
    {
        if (model == null || model.Squad == null)
            return;

        SquadController unit =
            model.Squad.JoinedActionController();

        if (unit.IsAlive ||
            !unit.HasKeyword("TRANSPORT") ||
            unit.EmbarkedPassengers.Count == 0)
        {
            return;
        }

        if (interactiveAttack != null ||
            traditionalAttackPending)
        {
            core11PendingDestroyedTransports.Add(
                unit
            );
            return;
        }

        Core11BeginEmergencyDisembark(unit);
    }

    private void Core11ResolvePendingDestroyedTransports()
    {
        if (core11EmergencyTransport != null ||
            core11EmergencyDisembarkQueue.Count > 0 ||
            (core11DisembarkPassenger != null &&
             core11DisembarkMode == "Emergency"))
        {
            return;
        }

        SquadController transport =
            core11PendingDestroyedTransports
                .FirstOrDefault(
                    unit =>
                        unit != null &&
                        !unit.IsAlive &&
                        unit.EmbarkedPassengers.Count > 0 &&
                        !core11EmergencyHandledTransports.Contains(unit)
                );

        if (transport == null)
            return;

        core11PendingDestroyedTransports.Remove(
            transport
        );

        Core11BeginEmergencyDisembark(
            transport
        );
    }

    private void Core11ApplyMortalWounds(
        SquadController unit,
        int wounds,
        string source,
        SquadController attacker = null)
    {
        if (unit == null || wounds <= 0)
            return;

        for (int i = 0; i < wounds; i++)
        {
            ModelToken model =
                unit.GetAutomaticAllocationModel();

            if (model == null &&
                unit.AttachedLeader != null)
            {
                model = unit.AttachedLeader
                    .GetAutomaticAllocationModel();
            }

            if (model == null)
                break;

            int afterFnp =
                UniversalRuleRegistry.ApplyFeelNoPain(
                    model.Squad,
                    1,
                    "Mortal Wounds: " + source
                );

            if (afterFnp > 0)
            {
                bool wasAlive = model.IsAlive;
                model.ApplyDamage(afterFnp);

                if (wasAlive && !model.IsAlive)
                {
                    RecordModelDestroyed(
                        model,
                        attacker
                    );
                }
            }
        }

        unit.RefreshVisuals();
        if (unit.AttachedLeader != null)
            unit.AttachedLeader.RefreshVisuals();
    }

    private bool Core11CanUseExplosives(
        SquadController unit)
    {
        return
            unit != null &&
            unit.IsOnBattlefield &&
            !IsEngaged(unit) &&
            !unit.HasAdvanced &&
            !unit.HasShot &&
            (unit.HasKeyword("EXPLOSIVES") ||
             unit.HasKeyword("GRENADES")) &&
            GetCommandPoints(unit.FactionId) >= 1;
    }

    private void Core11UseExplosives(
        SquadController unit)
    {
        if (!Core11CanUseExplosives(unit))
            return;

        List<SquadController> targets =
            squads
                .Where(
                    enemy =>
                        enemy != null &&
                        enemy.IsAlive &&
                        enemy.IsOnBattlefield &&
                        !enemy.IsAttachedLeader &&
                        enemy.FactionId != unit.FactionId &&
                        !IsEngaged(enemy) &&
                        JoinedModels(unit).Any(
                            model =>
                                model != null &&
                                ModelHasLineOfSight(model, enemy) &&
                                enemy.JoinedLivingModelTokens().Any(
                                    targetModel =>
                                        targetModel != null &&
                                        CoreRules11Terrain.ModelDistance(
                                            model,
                                            targetModel
                                        ) <= 8f + 0.001f
                                )
                        )
                )
                .ToList();

        if (targets.Count == 0)
        {
            status =
                "Explosives: no unengaged visible enemy is within 8\".";
            return;
        }

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController target
            in targets)
        {
            SquadController captured = target;
            options.Add(
                new RuleChoiceOption(
                    captured.DisplayName,
                    () =>
                    {
                        CloseRuleChoice();

                        if (!SpendFactionStratagemCP(
                                unit,
                                1,
                                "Explosives"))
                        {
                            return;
                        }

                        Action<int> apply =
                            successes =>
                            {
                                Core11ApplyMortalWounds(
                                    captured,
                                    successes,
                                    "Explosives",
                                    unit
                                );

                                status =
                                    "Explosives inflicted " +
                                    successes +
                                    " mortal wound(s) on " +
                                    captured.DisplayName + ".";
                            };

                        if (!IsXcomMode)
                        {
                            OpenTraditionalNumericPrompt(
                                "EXPLOSIVES",
                                "Roll six D6. Enter the number of results of 4+.",
                                0,
                                6,
                                0,
                                6,
                                apply
                            );
                            return;
                        }

                        int successes = 0;
                        for (int die = 0; die < 6; die++)
                        {
                            if (DiceRoller.RollD6("Explosives") >= 4)
                                successes++;
                        }

                        apply(successes);
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "Cancel",
                CloseRuleChoice
            )
        );

        OpenRuleChoice(
            "EXPLOSIVES  -  1CP",
            "Select one visible unengaged enemy within 8\".",
            options
        );
    }

    private bool Core11CanUseCrushingImpact(
        SquadController unit)
    {
        return
            unit != null &&
            unit.MadeChargeMove &&
            (unit.HasKeyword("MONSTER") ||
             unit.HasKeyword("VEHICLE")) &&
            EngagedEnemies(unit).Count > 0 &&
            GetCommandPoints(unit.FactionId) >= 1;
    }

    private void Core11UseCrushingImpact(
        SquadController unit)
    {
        if (!Core11CanUseCrushingImpact(unit))
            return;

        List<SquadController> targets =
            EngagedEnemies(unit);

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController target
            in targets)
        {
            SquadController captured = target;

            options.Add(
                new RuleChoiceOption(
                    captured.DisplayName,
                    () =>
                    {
                        CloseRuleChoice();

                        if (!SpendFactionStratagemCP(
                                unit,
                                1,
                                "Crushing Impact"))
                        {
                            return;
                        }

                        int dice = Mathf.Max(1, unit.Toughness);

                        Action<int, int> apply =
                            (selfWounds, enemyWounds) =>
                            {
                                Core11ApplyMortalWounds(
                                    unit,
                                    selfWounds,
                                    "Crushing Impact recoil"
                                );

                                Core11ApplyMortalWounds(
                                    captured,
                                    Mathf.Min(6, enemyWounds),
                                    "Crushing Impact",
                                    unit
                                );

                                status =
                                    "Crushing Impact: " +
                                    selfWounds +
                                    " self mortal wound(s), " +
                                    Mathf.Min(6, enemyWounds) +
                                    " enemy mortal wound(s).";
                            };

                        if (!IsXcomMode)
                        {
                            OpenTraditionalNumericPrompt(
                                "CRUSHING IMPACT  -  1s",
                                "Roll " + dice +
                                " D6. Enter how many dice rolled a 1.",
                                0,
                                dice,
                                0,
                                dice,
                                selfWounds =>
                                    OpenTraditionalNumericPrompt(
                                        "CRUSHING IMPACT  -  5+",
                                        "Using the same roll, enter how many dice rolled 5+ (maximum 6 inflicted).",
                                        0,
                                        dice,
                                        0,
                                        dice,
                                        enemyWounds =>
                                            apply(
                                                selfWounds,
                                                enemyWounds
                                            )
                                    )
                            );
                            return;
                        }

                        int self = 0;
                        int enemy = 0;

                        for (int die = 0;
                             die < dice;
                             die++)
                        {
                            int roll = DiceRoller.RollD6(
                                "Crushing Impact"
                            );

                            if (roll == 1) self++;
                            if (roll >= 5) enemy++;
                        }

                        apply(self, enemy);
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "Cancel",
                CloseRuleChoice
            )
        );

        OpenRuleChoice(
            "CRUSHING IMPACT  -  1CP",
            "Select one enemy unit engaged with the charging MONSTER/VEHICLE.",
            options
        );
    }

    private void Core11OfferSmokescreenWindow()
    {
        string defendingFaction =
            factions.FirstOrDefault(
                faction =>
                    !string.Equals(
                        faction,
                        activeFaction,
                        StringComparison.OrdinalIgnoreCase
                    )
            );

        if (string.IsNullOrWhiteSpace(defendingFaction))
            return;

        List<SquadController> smokeUnits =
            squads
                .Where(
                    unit =>
                        unit != null &&
                        unit.IsAlive &&
                        unit.IsOnBattlefield &&
                        !unit.IsAttachedLeader &&
                        unit.FactionId == defendingFaction &&
                        unit.HasKeyword("SMOKE") &&
                        !unit.IsBattleShocked &&
                        GetCommandPoints(defendingFaction) >= 1
                )
                .ToList();

        if (smokeUnits.Count == 0)
            return;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController unit
            in smokeUnits)
        {
            SquadController captured = unit;
            options.Add(
                new RuleChoiceOption(
                    "Smokescreen: " + captured.DisplayName + " (1CP)",
                    () =>
                    {
                        CloseRuleChoice();

                        if (!SpendFactionStratagemCP(
                                captured,
                                1,
                                "Smokescreen"))
                        {
                            return;
                        }

                        core11SmokescreenUnits.Add(captured);
                        status =
                            captured.DisplayName +
                            " is protected by Smokescreen until the end of the phase.";
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "No Smokescreen",
                CloseRuleChoice
            )
        );

        OpenRuleChoice(
            "SMOKESCREEN",
            "Start of opponent's Shooting phase. The defending player may use Smokescreen.",
            options
        );
    }

    private bool Core11OpenRapidIngressWindow()
    {
        string opposingFaction =
            factions.FirstOrDefault(
                faction =>
                    !string.Equals(
                        faction,
                        activeFaction,
                        StringComparison.OrdinalIgnoreCase
                    )
            );

        List<SquadController> eligible =
            squads
                .Where(
                    unit =>
                        unit != null &&
                        unit.IsAlive &&
                        unit.IsInReserves &&
                        !unit.IsAttachedLeader &&
                        unit.FactionId == opposingFaction &&
                        !unit.HasKeyword("AIRCRAFT") &&
                        round >= 2 &&
                        !unit.IsBattleShocked &&
                        GetCommandPoints(opposingFaction) >= 1
                )
                .ToList();

        if (eligible.Count == 0)
        {
            core11EndMoveWindowResolved = true;
            return false;
        }

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController unit
            in eligible)
        {
            SquadController captured = unit;
            options.Add(
                new RuleChoiceOption(
                    "Rapid Ingress: " +
                    captured.DisplayName +
                    " (1CP)",
                    () =>
                    {
                        CloseRuleChoice();

                        if (!SpendFactionStratagemCP(
                                captured,
                                1,
                                "Rapid Ingress"))
                        {
                            core11EndMoveWindowResolved = true;
                            return;
                        }

                        core11EndMoveWindowResolved = true;
                        reservePlacementSquad = captured;
                        reserveCycleIndex = -1;

                        status =
                            "RAPID INGRESS: place " +
                            captured.DisplayName +
                            " using its normal Ingress restrictions, then press NEXT PHASE again.";
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "No Rapid Ingress",
                () =>
                {
                    CloseRuleChoice();
                    core11EndMoveWindowResolved = true;
                    status =
                        "Rapid Ingress window passed. Press NEXT PHASE again.";
                }
            )
        );

        OpenRuleChoice(
            "RAPID INGRESS  -  END OF MOVEMENT",
            "The opposing player may bring in one non-AIRCRAFT unit from Strategic Reserves for 1CP.",
            options
        );

        return true;
    }

    private bool Core11OpenHeroicInterventionWindow()
    {
        string opposingFaction =
            factions.FirstOrDefault(
                faction =>
                    !string.Equals(
                        faction,
                        activeFaction,
                        StringComparison.OrdinalIgnoreCase
                    )
            );

        List<SquadController> eligible =
            squads
                .Where(
                    unit =>
                        unit != null &&
                        unit.IsAlive &&
                        unit.IsOnBattlefield &&
                        !unit.IsAttachedLeader &&
                        unit.FactionId == opposingFaction &&
                        !IsEngaged(unit) &&
                        (!unit.HasKeyword("VEHICLE") ||
                         unit.HasKeyword("CHARACTER") ||
                         unit.HasKeyword("WALKER")) &&
                        squads.Any(
                            enemy =>
                                enemy != null &&
                                enemy.IsAlive &&
                                enemy.IsOnBattlefield &&
                                enemy.FactionId != unit.FactionId &&
                                JoinedDistance(unit, enemy) <= 12f + 0.001f
                        ) &&
                        GetCommandPoints(opposingFaction) >= 1
                )
                .ToList();

        if (eligible.Count == 0)
        {
            core11EndChargeWindowResolved = true;
            return false;
        }

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController unit
            in eligible)
        {
            SquadController captured = unit;

            options.Add(
                new RuleChoiceOption(
                    "Heroic Intervention: " +
                    captured.DisplayName,
                    () =>
                    {
                        CloseRuleChoice();
                        Core11ChooseHeroicTarget(captured);
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "No Heroic Intervention",
                () =>
                {
                    CloseRuleChoice();
                    core11EndChargeWindowResolved = true;
                    status =
                        "Heroic Intervention window passed. Press NEXT PHASE again.";
                }
            )
        );

        OpenRuleChoice(
            "HEROIC INTERVENTION  -  END OF CHARGE",
            "The opposing player may resolve a Heroic Intervention charge.",
            options
        );

        return true;
    }

    private void Core11ChooseHeroicTarget(
        SquadController unit)
    {
        List<SquadController> chargedTargets =
            squads
                .Where(
                    enemy =>
                        enemy != null &&
                        enemy.IsAlive &&
                        enemy.IsOnBattlefield &&
                        enemy.FactionId != unit.FactionId &&
                        enemy.MadeChargeMove &&
                        JoinedDistance(unit, enemy) <= 12f + 0.001f
                )
                .ToList();

        List<SquadController> intoFrayTargets =
            squads
                .Where(
                    enemy =>
                        enemy != null &&
                        enemy.IsAlive &&
                        enemy.IsOnBattlefield &&
                        enemy.FactionId != unit.FactionId &&
                        JoinedDistance(unit, enemy) <= 6f + 0.001f
                )
                .ToList();

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController target
            in chargedTargets)
        {
            SquadController captured = target;
            options.Add(
                new RuleChoiceOption(
                    "Leap to Defend: " +
                    captured.DisplayName +
                    " (1CP)",
                    () =>
                    {
                        CloseRuleChoice();
                        Core11ResolveHeroicIntervention(
                            unit,
                            captured,
                            false
                        );
                    }
                )
            );
        }

        foreach (SquadController target
            in intoFrayTargets)
        {
            SquadController captured = target;
            options.Add(
                new RuleChoiceOption(
                    "Into the Fray: " +
                    captured.DisplayName +
                    " (2CP)",
                    () =>
                    {
                        CloseRuleChoice();
                        Core11ResolveHeroicIntervention(
                            unit,
                            captured,
                            true
                        );
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "Cancel",
                () =>
                {
                    CloseRuleChoice();
                    core11EndChargeWindowResolved = true;
                }
            )
        );

        OpenRuleChoice(
            "HEROIC INTERVENTION TARGET",
            "Leap to Defend costs 1CP. Into the Fray costs 2CP and caps the charge roll at 6.",
            options
        );
    }

    private void Core11ResolveHeroicIntervention(
        SquadController unit,
        SquadController target,
        bool intoFray)
    {
        int cost = intoFray ? 2 : 1;

        if (!SpendFactionStratagemCP(
                unit,
                cost,
                "Heroic Intervention"))
        {
            core11EndChargeWindowResolved = true;
            return;
        }

        GameEventBus.Raise(
            new GameEventContext
            {
                Type = GameEventType.ChargeDeclared,
                Game = this,
                ActingFaction = unit.FactionId,
                Phase = phase,
                Source = unit,
                Target = target,
                Note = intoFray
                    ? "Heroic Intervention: Into the Fray"
                    : "Heroic Intervention: Leap to Defend"
            }
        );

        Action<int> useRoll =
            roll =>
            {
                int resolved =
                    intoFray
                    ? Mathf.Min(6, roll)
                    : roll;

                GameEventBus.Raise(
                    new GameEventContext
                    {
                        Type = GameEventType.ChargeRolled,
                        Game = this,
                        ActingFaction = unit.FactionId,
                        Phase = phase,
                        Source = unit,
                        Target = target,
                        RollTotal = resolved,
                        Note = "Heroic Intervention"
                    }
                );

                if (JoinedDistance(unit, target) >
                    resolved + EngagementRange + 0.05f)
                {
                    core11EndChargeWindowResolved = true;
                    status =
                        "Heroic Intervention failed: charge roll " +
                        resolved + ". Press NEXT PHASE again.";
                    return;
                }

                core11HeroicChargeUnit = unit;
                core11HeroicChargeTarget = target;
                core11HeroicChargeDistance = resolved;
                core11EndChargeWindowResolved = true;

                status =
                    "HEROIC INTERVENTION: click the board to move " +
                    unit.DisplayName +
                    " up to " + resolved +
                    "\" and end engaged with " +
                    target.DisplayName + ".";
            };

        if (!IsXcomMode)
        {
            OpenTraditionalNumericPrompt(
                "HEROIC INTERVENTION CHARGE",
                "Roll 2D6 and enter the final charge result.",
                2,
                12,
                7,
                2,
                useRoll
            );
            return;
        }

        useRoll(
            DiceRoller.RollD6("Heroic Intervention") +
            DiceRoller.RollD6("Heroic Intervention")
        );
    }

    private void Core11TryPlaceHeroicCharge(
        Vector3 destination)
    {
        SquadController unit = core11HeroicChargeUnit;
        SquadController target = core11HeroicChargeTarget;

        if (unit == null || target == null)
            return;

        List<ModelToken> joined = JoinedModels(unit);
        if (joined.Count == 0)
            return;

        Vector3 centre = Vector3.zero;
        foreach (ModelToken model in joined)
            centre += model.transform.position;
        centre /= joined.Count;

        destination.y = centre.y;
        Vector3 delta = destination - centre;
        delta.y = 0f;

        if (HorizontalMagnitude(delta) >
            core11HeroicChargeDistance + 0.001f)
        {
            status = "Heroic Intervention move is too far.";
            return;
        }

        Dictionary<ModelToken, Vector3> original =
            CaptureJoinedPositions(unit);

        TranslateJoinedModels(unit, delta);
        Physics.SyncTransforms();

        bool pathLegal =
            CoreRules11FlightRegistry.IsTakingToSkies(unit) ||
            joined.All(
                model =>
                    model == null ||
                    CombatMovePathIsClear(
                        model,
                        original[model],
                        model.transform.position
                    )
            );

        bool legal =
            pathLegal &&
            AllModelsInsideBoard(unit) &&
            AllModelsHaveLegalPlacement(unit) &&
            unit.IsCoherent() &&
            UnitsAreEngaged(unit, target);

        if (!legal)
        {
            RestoreJoinedPositions(original);
            status =
                "Heroic Intervention move rejected: the unit must end legally and engaged with its selected target.";
            return;
        }

        unit.HasCharged = true;
        unit.MarkMadeChargeMove();

        core11HeroicChargeUnit = null;
        core11HeroicChargeTarget = null;
        core11HeroicChargeDistance = 0f;

        status =
            unit.DisplayName +
            " completed Heroic Intervention. Press NEXT PHASE again.";
    }

    private bool Core11ModelHasOwnKeyword(
        ModelToken model,
        string keyword)
    {
        if (model == null ||
            model.Squad == null ||
            model.Squad.SourceData == null ||
            string.IsNullOrWhiteSpace(keyword))
        {
            return false;
        }

        string wanted =
            WeaponRuleParser.NormalizeRuleName(keyword);

        IEnumerable<string> values =
            (model.Squad.SourceData.keywords ??
                new string[0])
            .Concat(
                model.Squad.SourceData.factionKeywords ??
                new string[0]
            );

        return values.Any(
            value =>
                WeaponRuleParser.NormalizeRuleName(
                    value
                ) == wanted
        );
    }

    private void Core11OfferEpicChallenge(
        SquadController unit)
    {
        if (unit == null ||
            unit.IsBattleShocked ||
            GetCommandPoints(unit.FactionId) < 1)
        {
            return;
        }

        List<ModelToken> characters =
            unit.JoinedLivingModelTokens()
                .Where(
                    model =>
                        Core11ModelHasOwnKeyword(
                            model,
                            "CHARACTER"
                        )
                )
                .ToList();

        if (characters.Count == 0)
            return;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (ModelToken character
            in characters)
        {
            ModelToken captured = character;

            options.Add(
                new RuleChoiceOption(
                    "Epic Challenge: " +
                    captured.RoleName +
                    " (1CP)",
                    () =>
                    {
                        CloseRuleChoice();

                        SquadController owner =
                            captured.Squad != null
                            ? captured.Squad.JoinedActionController()
                            : unit;

                        if (!SpendFactionStratagemCP(
                                owner,
                                1,
                                "Epic Challenge"))
                        {
                            return;
                        }

                        core11EpicChallengeModels.Add(
                            captured
                        );

                        status =
                            "Epic Challenge: " +
                            captured.RoleName +
                            " has PRECISION on its melee weapons until the end of the phase.";
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "Do not use",
                CloseRuleChoice
            )
        );

        OpenRuleChoice(
            "EPIC CHALLENGE  -  1CP",
            "The unit was selected to fight. Select one CHARACTER model to gain PRECISION on its melee weapons until the end of the phase.",
            options
        );
    }

    public bool Core11HasEpicChallenge(
        ModelToken model)
    {
        return
            model != null &&
            core11EpicChallengeModels.Contains(model);
    }

    private void Core11OfferCounteroffensive(
        SquadController enemyThatFought)
    {
        if (phase != Phase.Fight ||
            fight11ForcedFightActive ||
            enemyThatFought == null ||
            !string.Equals(
                enemyThatFought.FactionId,
                activeFaction,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string faction =
            factions.FirstOrDefault(
                value =>
                    !string.Equals(
                        value,
                        enemyThatFought.FactionId,
                        StringComparison.OrdinalIgnoreCase
                    )
            );

        List<SquadController> eligible =
            squads
                .Where(
                    unit =>
                        unit != null &&
                        unit.IsAlive &&
                        unit.IsOnBattlefield &&
                        !unit.IsAttachedLeader &&
                        unit.FactionId == faction &&
                        Fight11IsEligibleToFightNow(unit) &&
                        !unit.IsBattleShocked &&
                        GetCommandPoints(faction) >= 2
                )
                .ToList();

        if (eligible.Count == 0)
            return;

        core11CounteroffensiveDecisionPending = true;
        core11CounteroffensiveCompletedUnit =
            enemyThatFought.JoinedActionController();

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController unit
            in eligible)
        {
            SquadController captured = unit;

            options.Add(
                new RuleChoiceOption(
                    "Counteroffensive: " +
                    captured.DisplayName +
                    " (2CP)",
                    () =>
                    {
                        CloseRuleChoice();

                        if (!SpendFactionStratagemCP(
                                captured,
                                2,
                                "Counteroffensive"))
                        {
                            Core11PassCounteroffensive();
                            return;
                        }

                        captured.TemporaryFightsFirst = true;
                        core11CounteroffensiveFightsFirstUnits.Add(
                            captured
                        );
                        core11ForcedNextFightUnit = captured;
                        fightSelectionFaction = captured.FactionId;
                        core11CounteroffensiveDecisionPending = false;
                        core11CounteroffensiveCompletedUnit = null;

                        status =
                            captured.DisplayName +
                            " must be the next unit selected to fight.";
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "No Counteroffensive",
                () =>
                {
                    CloseRuleChoice();
                    Core11PassCounteroffensive();
                }
            )
        );

        OpenRuleChoice(
            "COUNTEROFFENSIVE  -  2CP",
            "Just after the enemy resolved its attacks, select an eligible friendly unit to fight next.",
            options
        );
    }


    private bool Core11CounteroffensiveDecisionIsPending(
        SquadController completed)
    {
        return
            core11CounteroffensiveDecisionPending &&
            completed != null &&
            core11CounteroffensiveCompletedUnit ==
                completed.JoinedActionController();
    }

    private void Core11PassCounteroffensive()
    {
        SquadController completed =
            core11CounteroffensiveCompletedUnit;

        core11CounteroffensiveDecisionPending = false;
        core11CounteroffensiveCompletedUnit = null;

        if (completed != null &&
            phase == Phase.Fight)
        {
            Fight11AdvanceFightPriority(completed);
        }
    }

    private bool Core11OfferInsaneBraveryForTraditionalBattleShock(
        SquadController unit)
    {
        if (unit == null ||
            IsXcomMode ||
            unit.IsBattleShocked ||
            core11InsaneBraveryUsedByFaction.Contains(
                unit.FactionId) ||
            GetCommandPoints(unit.FactionId) < 1)
        {
            return false;
        }

        OpenRuleChoice(
            "INSANE BRAVERY  -  1CP",
            "Just before the Battle-shock roll for " +
            unit.DisplayName +
            ", make that roll automatically successful? This can only be used once per battle.",
            new[]
            {
                new RuleChoiceOption(
                    "Use Insane Bravery",
                    () =>
                    {
                        CloseRuleChoice();

                        if (!SpendFactionStratagemCP(
                                unit,
                                1,
                                "Insane Bravery"))
                        {
                            OpenTraditionalDicePrompt(2);
                            return;
                        }

                        core11InsaneBraveryUsedByFaction.Add(
                            unit.FactionId
                        );

                        unit.SetBattleShocked(false, 0);
                        traditionalBattleShockPending = false;
                        traditionalBattleShockUnit = null;

                        AppendBattleLog(
                            "STRATAGEM",
                            "Insane Bravery",
                            unit.DisplayName +
                            " automatically passed its Battle-shock roll."
                        );

                        BeginNextTraditionalBattleShock();
                        RefreshObjectiveDisplays();
                    }
                ),
                new RuleChoiceOption(
                    "Roll Battle-shock normally",
                    () =>
                    {
                        CloseRuleChoice();
                        OpenTraditionalDicePrompt(2);
                        status =
                            "BATTLE-SHOCK TEST REQUIRED: " +
                            unit.DisplayName +
                            ". Roll 2D6 manually, then mark PASS or FAIL.";
                    }
                )
            }
        );

        return true;
    }

    private void Core11ReturnAircraftAtEndOfOpponentTurn(
        string turnFaction)
    {
        if (string.IsNullOrWhiteSpace(turnFaction))
            return;

        foreach (SquadController aircraft
            in squads
                .Where(
                    unit =>
                        unit != null &&
                        unit.IsAlive &&
                        unit.IsOnBattlefield &&
                        !unit.IsAttachedLeader &&
                        unit.HasKeyword("AIRCRAFT") &&
                        !string.Equals(
                            unit.FactionId,
                            turnFaction,
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                .ToList())
        {
            aircraft.SendToReserves(true);

            AppendBattleLog(
                "AIRCRAFT",
                "Return to Strategic Reserves",
                aircraft.DisplayName +
                " returned to Strategic Reserves at the end of its opponent's turn."
            );
        }
    }

    private bool Core11PrepareAircraftAndValidateMuster()
    {
        string reason;
        if (!Core11ValidateMuster(out reason))
        {
            status =
                "MUSTER INVALID: " + reason;
            return false;
        }

        if (!Core11ResolveEmptyDedicatedTransports())
            return false;

        foreach (SquadController aircraft
            in squads
                .Where(
                    unit =>
                        unit != null &&
                        unit.IsAlive &&
                        !unit.IsAttachedLeader &&
                        unit.HasKeyword("AIRCRAFT")
                )
                .ToList())
        {
            aircraft.SendToReserves();
        }

        return true;
    }

    private bool Core11ValidateMuster(
        out string reason)
    {
        reason = "";

        int pointLimit = 0;
        int enhancementLimit = 0;
        int unitLimit = 0;

        if (string.Equals(
                battleSizeName,
                "Incursion",
                StringComparison.OrdinalIgnoreCase))
        {
            pointLimit = 1000;
            enhancementLimit = 2;
            unitLimit = 2;
        }
        else if (string.Equals(
                battleSizeName,
                "Strike Force",
                StringComparison.OrdinalIgnoreCase))
        {
            pointLimit = 2000;
            enhancementLimit = 4;
            unitLimit = 3;
        }

        foreach (string faction in factions)
        {
            WarboardRosterManifest manifest =
                RosterTextManifestStore.Get(faction);

            if (manifest != null)
            {
                if (pointLimit > 0 &&
                    manifest.TotalArmyPoints > pointLimit)
                {
                    reason =
                        faction + " roster is " +
                        manifest.TotalArmyPoints +
                        "pts; " + battleSizeName +
                        " allows " + pointLimit + "pts.";
                    return false;
                }

                if (enhancementLimit > 0 &&
                    manifest.Enhancements.Count >
                        enhancementLimit)
                {
                    reason =
                        faction + " exceeds the " +
                        enhancementLimit +
                        " enhancement limit for " +
                        battleSizeName + ".";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(
                        manifest.Warlord))
                {
                    reason =
                        faction +
                        " roster does not identify a WARLORD.";
                    return false;
                }

                SquadController warlord =
                    squads.FirstOrDefault(
                        unit =>
                            unit != null &&
                            unit.FactionId == faction &&
                            unit.HasKeyword("CHARACTER") &&
                            Core11NamesMatch(
                                unit.DisplayName,
                                manifest.Warlord
                            )
                    );

                if (warlord == null)
                {
                    reason =
                        "WARLORD " + manifest.Warlord +
                        " was not found as a CHARACTER in " +
                        faction + ".";
                    return false;
                }
            }

            if (unitLimit <= 0)
                continue;

            foreach (IGrouping<string, SquadController> group
                in squads
                    .Where(
                        unit =>
                            unit != null &&
                            unit.FactionId == faction &&
                            !unit.IsAttachedLeader
                    )
                    .GroupBy(
                        unit =>
                            (unit.DisplayName ?? "")
                                .Trim()
                                .ToLowerInvariant()
                    ))
            {
                SquadController sample =
                    group.FirstOrDefault();

                if (sample == null)
                    continue;

                int allowed = unitLimit;

                if (sample.HasKeyword("BATTLELINE") ||
                    sample.HasKeyword("DEDICATED TRANSPORT"))
                {
                    allowed *= 2;
                }

                if (sample.HasKeyword("EPIC HERO"))
                    allowed = 1;

                if (group.Count() > allowed)
                {
                    reason =
                        faction + " includes " +
                        group.Count() + " copies of " +
                        sample.DisplayName +
                        "; the limit is " + allowed + ".";
                    return false;
                }
            }
        }

        return true;
    }

    private static bool Core11NamesMatch(
        string left,
        string right)
    {
        string a = (left ?? "")
            .ToLowerInvariant()
            .Replace("char1:", "")
            .Replace("char2:", "")
            .Trim();

        string b = (right ?? "")
            .ToLowerInvariant()
            .Replace("char1:", "")
            .Replace("char2:", "")
            .Trim();

        return
            a == b ||
            a.Contains(b) ||
            b.Contains(a);
    }

    private bool Core11ResolveEmptyDedicatedTransports()
    {
        SquadController empty =
            squads.FirstOrDefault(
                unit =>
                    unit != null &&
                    unit.IsAlive &&
                    !unit.IsAttachedLeader &&
                    unit.HasKeyword("DEDICATED TRANSPORT") &&
                    unit.EmbarkedPassengers.Count == 0
            );

        if (empty == null)
            return true;

        if (CoreRules11TransportRules.Capacity(empty) <= 0)
        {
            List<RuleChoiceOption> capacityOptions =
                new List<RuleChoiceOption>();

            for (int capacity = 1;
                 capacity <= 30;
                 capacity++)
            {
                int captured = capacity;

                capacityOptions.Add(
                    new RuleChoiceOption(
                        captured + " models",
                        () =>
                        {
                            CloseRuleChoice();

                            CoreRules11TransportRules
                                .SetManualCapacity(
                                    empty,
                                    captured
                                );

                            status =
                                empty.DisplayName +
                                " capacity set to " +
                                captured +
                                ". Press BEGIN BATTLE again to assign its passenger.";
                        }
                    )
                );
            }

            capacityOptions.Add(
                new RuleChoiceOption(
                    "Leave empty  -  destroy transport",
                    () =>
                    {
                        CloseRuleChoice();
                        empty.DestroyReserveWithoutTriggers();
                        status =
                            empty.DisplayName +
                            " was destroyed because it was left empty. Press BEGIN BATTLE again.";
                    }
                )
            );

            OpenRuleChoice(
                "DECLARE BATTLE FORMATIONS  -  " +
                empty.DisplayName,
                "The imported datasheet did not expose this DEDICATED TRANSPORT's capacity. Select the printed capacity before assigning its passenger.",
                capacityOptions
            );

            return false;
        }

        List<SquadController> candidates =
            squads
                .Where(
                    unit =>
                        unit != null &&
                        unit.IsAlive &&
                        !unit.IsAttachedLeader &&
                        unit != empty &&
                        unit.FactionId == empty.FactionId &&
                        !unit.IsEmbarked
                )
                .Where(
                    unit =>
                    {
                        string reason;
                        return CoreRules11TransportRules.CanCarry(
                            empty,
                            unit,
                            out reason
                        );
                    })
                .ToList();

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController candidate
            in candidates)
        {
            SquadController captured = candidate;

            options.Add(
                new RuleChoiceOption(
                    "Start embarked: " +
                    captured.DisplayName,
                    () =>
                    {
                        CloseRuleChoice();
                        captured.EmbarkWithin(empty);
                        status =
                            captured.DisplayName +
                            " will start embarked within " +
                            empty.DisplayName +
                            ". Press BEGIN BATTLE again.";
                    }
                )
            );
        }

        options.Add(
            new RuleChoiceOption(
                "Leave empty  -  destroy transport",
                () =>
                {
                    CloseRuleChoice();
                    empty.DestroyReserveWithoutTriggers();
                    status =
                        empty.DisplayName +
                        " was destroyed because a DEDICATED TRANSPORT must start with an embarked friendly unit. Press BEGIN BATTLE again.";
                }
            )
        );

        OpenRuleChoice(
            "DECLARE BATTLE FORMATIONS  -  " +
            empty.DisplayName,
            "A friendly unit must start embarked within each DEDICATED TRANSPORT. Choose a passenger or leave it empty and destroy it.",
            options
        );

        status =
            "Resolve the DEDICATED TRANSPORT formation before starting the battle.";
        return false;
    }
}
