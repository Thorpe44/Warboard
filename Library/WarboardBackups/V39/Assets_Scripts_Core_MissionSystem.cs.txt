using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ForceDisposition
{
    TakeAndHold,
    PurgeTheFoe,
    Disruption,
    Reconnaissance,
    PriorityAssets
}

public enum MissionSecondaryMode
{
    Tactical,
    Fixed,
    Manual
}

public enum MissionObjectiveRole
{
    Neutral,
    PlayerOneHome,
    PlayerTwoHome,
    Central,
    Expansion
}

public enum MissionActionTargetType
{
    None,
    Objective,
    Terrain,
    EnemyUnit,
    OperationMarkerTerrain
}

public class MissionActionDefinition
{
    public string Id = "";
    public string DisplayName = "";
    public string MissionName = "";
    public MissionActionTargetType TargetType;

    public bool CompletesImmediately;
    public bool ExcludeHomeObjective;
    public bool RequireCentralObjectiveRange;
    public bool RequireCentralObjectiveTarget;
    public bool RequireOpponentTerritoryTerrain;
    public bool RequireOperationMarkerTarget;
    public bool RequireControlAtCompletion;
}

public class ActiveMissionAction
{
    public SquadController Unit;
    public MissionActionDefinition Definition;
    public ObjectiveController ObjectiveTarget;
    public TerrainFeature TerrainTarget;
    public SquadController EnemyTarget;
    public int StartedRound;

    public readonly Dictionary<
        ModelToken,
        Vector3
    > StartPositions =
        new Dictionary<
            ModelToken,
            Vector3
        >();
}

public static class MissionActionRegistry
{
    public static MissionActionDefinition ForPrimary(
        string missionName)
    {
        switch (missionName)
        {
            case "Smoke and Mirrors":
                return
                    new MissionActionDefinition
                    {
                        Id = "decoy",
                        DisplayName = "Decoy Objective",
                        MissionName = missionName,
                        TargetType =
                            MissionActionTargetType
                                .Objective,
                        ExcludeHomeObjective = true,
                        RequireControlAtCompletion = true
                    };

            case "Death Trap":
                return
                    new MissionActionDefinition
                    {
                        Id = "trap_terrain",
                        DisplayName = "Trap Terrain",
                        MissionName = missionName,
                        TargetType =
                            MissionActionTargetType
                                .Terrain
                    };

            case "Extract Relic":
                return
                    new MissionActionDefinition
                    {
                        Id = "sensor_sweep",
                        DisplayName = "Sensor Sweep",
                        MissionName = missionName,
                        TargetType =
                            MissionActionTargetType
                                .OperationMarkerTerrain,
                        RequireCentralObjectiveRange = true,
                        RequireOperationMarkerTarget = true
                    };

            case "Sabotage":
                return
                    new MissionActionDefinition
                    {
                        Id = "sabotage",
                        DisplayName = "Commit Sabotage",
                        MissionName = missionName,
                        TargetType =
                            MissionActionTargetType
                                .None
                    };

            case "Vanguard Operation":
                return
                    new MissionActionDefinition
                    {
                        Id = "vanguard_operation",
                        DisplayName = "Vanguard Operation",
                        MissionName = missionName,
                        TargetType =
                            MissionActionTargetType
                                .Terrain,
                        RequireOpponentTerritoryTerrain = true
                    };

            case "Triangulation":
                return
                    new MissionActionDefinition
                    {
                        Id = "triangulate",
                        DisplayName = "Triangulate Objective",
                        MissionName = missionName,
                        TargetType =
                            MissionActionTargetType
                                .Objective,
                        ExcludeHomeObjective = true,
                        RequireControlAtCompletion = true
                    };

            case "Gather Intel":
                return
                    new MissionActionDefinition
                    {
                        Id = "extract_intelligence",
                        DisplayName = "Extract Intelligence",
                        MissionName = missionName,
                        TargetType =
                            MissionActionTargetType
                                .Objective,
                        ExcludeHomeObjective = true
                    };

            case "Surveil the Foe":
                return
                    new MissionActionDefinition
                    {
                        Id = "surveil",
                        DisplayName = "Surveil Enemy",
                        MissionName = missionName,
                        TargetType =
                            MissionActionTargetType
                                .EnemyUnit,
                        CompletesImmediately = true
                    };

            case "Secure Asset":
                return
                    new MissionActionDefinition
                    {
                        Id = "secure_asset",
                        DisplayName = "Secure Asset",
                        MissionName = missionName,
                        TargetType =
                            MissionActionTargetType
                                .Objective,
                        RequireCentralObjectiveTarget = true,
                        RequireControlAtCompletion = true
                    };

            case "Vital Link":
                return
                    new MissionActionDefinition
                    {
                        Id = "establish_link",
                        DisplayName = "Establish Vital Link",
                        MissionName = missionName,
                        TargetType =
                            MissionActionTargetType
                                .Objective,
                        RequireCentralObjectiveTarget = true
                    };
        }

        return null;
    }
}

public class MissionPlayerState
{
    public string FactionId = "";
    public ForceDisposition Disposition;
    public string PrimaryMission = "";
    public MissionSecondaryMode SecondaryMode =
        MissionSecondaryMode.Tactical;

    public readonly List<string> SecondaryDeck =
        new List<string>();

    public readonly List<string> SecondaryHand =
        new List<string>();

    public readonly List<string> FixedSecondaries =
        new List<string>();

    public readonly HashSet<ObjectiveController>
        ControlledAtTurnStart =
            new HashSet<ObjectiveController>();

    // Generic mission-operation state. These counters deliberately model
    // mission state rather than card wording, so the rules engine can be
    // extended without baking UI prose into GameController.
    public int PersistentMarks;
    public int PersistentBonusMarks;
    public int OperationMarkers;
    public int TurnActionCompletions;
    public int TurnBonusCompletions;
    public int TurnSpecialEvents;

    public int EnemyUnitsDestroyedThisTurn;
    public int FriendlyUnitsDestroyedPreviousTurn;

    public int FixedSecondaryOneIndex;
    public int FixedSecondaryTwoIndex = 1;
}

public static class MissionRegistry
{
    public static readonly ForceDisposition[] AllDispositions =
    {
        ForceDisposition.TakeAndHold,
        ForceDisposition.PurgeTheFoe,
        ForceDisposition.Disruption,
        ForceDisposition.Reconnaissance,
        ForceDisposition.PriorityAssets
    };

    public static readonly string[] SecondaryMissions =
    {
        "A Grievous Blow",
        "A Tempting Target",
        "Assassination",
        "Beacon",
        "Behind Enemy Lines",
        "Bring it Down",
        "Burden of Trust",
        "Centre Ground",
        "Cleanse",
        "Defend Stronghold",
        "Display of Might",
        "Engage on All Fronts",
        "Forward Position",
        "No Prisoners",
        "Outflank",
        "Overwhelming Force",
        "Plunder",
        "Secure No Man's Land"
    };

    public static readonly string[] FixedEligibleSecondaries =
    {
        "A Grievous Blow",
        "Assassination",
        "Bring it Down",
        "Engage on All Fronts"
    };

    public static string Display(
        ForceDisposition disposition)
    {
        switch (disposition)
        {
            case ForceDisposition.TakeAndHold:
                return "Take and Hold";
            case ForceDisposition.PurgeTheFoe:
                return "Purge the Foe";
            case ForceDisposition.Disruption:
                return "Disruption";
            case ForceDisposition.Reconnaissance:
                return "Reconnaissance";
            default:
                return "Priority Assets";
        }
    }

    public static string ResolvePrimary(
        ForceDisposition own,
        ForceDisposition opponent)
    {
        if (own == ForceDisposition.TakeAndHold)
        {
            if (opponent == ForceDisposition.TakeAndHold)
                return "Battlefield Dominance";
            if (opponent == ForceDisposition.PurgeTheFoe)
                return "Immovable Object";
            if (opponent == ForceDisposition.Disruption)
                return "Determined Acquisition";
            if (opponent == ForceDisposition.Reconnaissance)
                return "Purge and Secure";
            return "Inescapable Dominion";
        }

        if (own == ForceDisposition.PurgeTheFoe)
        {
            if (opponent == ForceDisposition.TakeAndHold)
                return "Unstoppable Force";
            if (opponent == ForceDisposition.PurgeTheFoe)
                return "Meatgrinder";
            if (opponent == ForceDisposition.Disruption)
                return "Punishment";
            if (opponent == ForceDisposition.Reconnaissance)
                return "Consecrate";
            return "Destroyer's Wrath";
        }

        if (own == ForceDisposition.Disruption)
        {
            if (opponent == ForceDisposition.TakeAndHold)
                return "Death Trap";
            if (opponent == ForceDisposition.PurgeTheFoe)
                return "Delaying Action";
            if (opponent == ForceDisposition.Disruption)
                return "Outmanoeuvre";
            if (opponent == ForceDisposition.Reconnaissance)
                return "Smoke and Mirrors";
            return "Locate and Deny";
        }

        if (own == ForceDisposition.Reconnaissance)
        {
            if (opponent == ForceDisposition.TakeAndHold)
                return "Reconnaissance Sweep";
            if (opponent == ForceDisposition.PurgeTheFoe)
                return "Triangulation";
            if (opponent == ForceDisposition.Disruption)
                return "Surveil the Foe";
            if (opponent == ForceDisposition.Reconnaissance)
                return "Gather Intel";
            return "Search and Scour";
        }

        if (opponent == ForceDisposition.TakeAndHold)
            return "Secure Asset";
        if (opponent == ForceDisposition.PurgeTheFoe)
            return "Vital Link";
        if (opponent == ForceDisposition.Disruption)
            return "Extract Relic";
        if (opponent == ForceDisposition.Reconnaissance)
            return "Vanguard Operation";
        return "Sabotage";
    }

    public static bool UsesMissionOperation(
        string mission)
    {
        return
            mission == "Consecrate" ||
            mission == "Gather Intel" ||
            mission == "Surveil the Foe" ||
            mission == "Triangulation" ||
            mission == "Extract Relic" ||
            mission == "Sabotage" ||
            mission == "Secure Asset" ||
            mission == "Vanguard Operation" ||
            mission == "Vital Link" ||
            mission == "Death Trap" ||
            mission == "Locate and Deny" ||
            mission == "Smoke and Mirrors" ||
            mission == "Punishment" ||
            mission == "Purge and Secure";
    }

    public static string OperationLabel(
        string mission)
    {
        switch (mission)
        {
            case "Consecrate":
                return "Consecrated objectives";
            case "Gather Intel":
                return "Operation markers";
            case "Surveil the Foe":
                return "Operation markers";
            case "Triangulation":
                return "Triangulated objectives";
            case "Vital Link":
                return "Operation markers";
            case "Death Trap":
                return "Trapped terrain areas";
            case "Smoke and Mirrors":
                return "Decoyed objectives";
            case "Locate and Deny":
                return "Operation markers";
            case "Extract Relic":
                return "Operation markers";
            default:
                return "Mission marks";
        }
    }

    public static string TurnActionLabel(
        string mission)
    {
        switch (mission)
        {
            case "Gather Intel":
                return "Extract Intelligence completions";
            case "Extract Relic":
                return "Sensor Sweep completions";
            case "Sabotage":
                return "Sabotage completions";
            case "Secure Asset":
                return "Secure Asset completions";
            case "Vanguard Operation":
                return "Vanguard Operation completions";
            default:
                return "Mission action completions";
        }
    }

    public static string TurnBonusLabel(
        string mission)
    {
        switch (mission)
        {
            case "Sabotage":
                return "Sabotage completions in opponent territory";
            case "Death Trap":
                return "Trapped objective terrain this turn";
            default:
                return "Turn bonus events";
        }
    }

    public static string SpecialEventLabel(
        string mission)
    {
        switch (mission)
        {
            case "Purge and Secure":
                return "Objective-related destruction condition";
            case "Punishment":
                return "Condemned enemy left battlefield";
            case "Surveil the Foe":
                return "Enemy unit successfully surveilled";
            case "Extract Relic":
                return "Relic/marker condition";
            case "Locate and Deny":
                return "Locate/marker condition";
            case "Death Trap":
                return "Enemy destroyed in trapped terrain";
            case "Search and Scour":
                return "Enemy destroyed after starting in terrain";
            default:
                return "Special mission condition";
        }
    }

    public static string BonusLabel(
        string mission)
    {
        switch (mission)
        {
            case "Consecrate":
                return "Opponent home consecrated";
            case "Gather Intel":
                return "Operation marker at opponent home";
            case "Extract Relic":
            case "Locate and Deny":
                return "Single-marker condition active";
            case "Sabotage":
                return "Sabotage completions in opponent territory";
            case "Death Trap":
                return "Trapped terrain areas that are objectives";
            case "Smoke and Mirrors":
                return "Decoyed objectives in opponent territory";
            default:
                return "Bonus mission state";
        }
    }
}

public class MissionSystem
{
    private readonly GameController game;
    private readonly List<SquadController> squads;
    private readonly List<ObjectiveController> objectives;
    private readonly List<string> factions;

    private readonly Dictionary<string, MissionPlayerState>
        playerStates =
            new Dictionary<string, MissionPlayerState>();

    private readonly Dictionary<string, int>
        destroyedVictimsPreviousTurn =
            new Dictionary<string, int>();

    private readonly Dictionary<string, int>
        destroyedVictimsCurrentTurn =
            new Dictionary<string, int>();

    private string currentTurnFaction = "";

    private readonly Dictionary<string, int>
        characterModelsDestroyedThisTurn =
            new Dictionary<string, int>();

    private readonly Dictionary<string, int>
        characterW4ModelsDestroyedThisTurn =
            new Dictionary<string, int>();

    private readonly Dictionary<string, int>
        largeModelsDestroyedThisTurn =
            new Dictionary<string, int>();

    private readonly Dictionary<string, int>
        startingCharacterModels =
            new Dictionary<string, int>();

    private readonly Dictionary<
        SquadController,
        ActiveMissionAction
    > activeMissionActions =
        new Dictionary<
            SquadController,
            ActiveMissionAction
        >();

    private readonly HashSet<
        SquadController
    > surveilledThisTurn =
        new HashSet<SquadController>();

    public int LayoutIndex { get; private set; } = 1;
    public int AttackerIndex { get; private set; } = 0;

    public MissionSystem(
        GameController owner,
        List<SquadController> allSquads,
        List<ObjectiveController> allObjectives,
        List<string> allFactions)
    {
        game = owner;
        squads = allSquads;
        objectives = allObjectives;
        factions = allFactions;
    }

    public void Configure(
        ForceDisposition playerOne,
        ForceDisposition playerTwo,
        MissionSecondaryMode playerOneSecondaries,
        MissionSecondaryMode playerTwoSecondaries,
        int layoutIndex,
        int attackerIndex)
    {
        playerStates.Clear();

        LayoutIndex =
            Mathf.Clamp(
                layoutIndex,
                1,
                3
            );

        AttackerIndex =
            Mathf.Clamp(
                attackerIndex,
                0,
                1
            );

        if (factions.Count < 2)
            return;

        MissionPlayerState p1 =
            CreatePlayerState(
                factions[0],
                playerOne,
                playerTwo,
                playerOneSecondaries
            );

        MissionPlayerState p2 =
            CreatePlayerState(
                factions[1],
                playerTwo,
                playerOne,
                playerTwoSecondaries
            );

        playerStates[p1.FactionId] = p1;
        playerStates[p2.FactionId] = p2;

        startingCharacterModels.Clear();

        foreach (string faction in factions)
        {
            startingCharacterModels[faction] =
                squads
                    .Where(
                        unit =>
                            unit != null &&
                            unit.FactionId == faction &&
                            unit.HasIntrinsicKeyword(
                                "CHARACTER"
                            )
                    )
                    .Sum(
                        unit =>
                            unit.AllLivingModelTokens()
                                .Count
                    );
        }

        InitializeMissionWorldState();
    }

    private MissionPlayerState CreatePlayerState(
        string faction,
        ForceDisposition own,
        ForceDisposition opponent,
        MissionSecondaryMode secondaryMode)
    {
        MissionPlayerState state =
            new MissionPlayerState();

        state.FactionId = faction;
        state.Disposition = own;
        state.PrimaryMission =
            MissionRegistry.ResolvePrimary(
                own,
                opponent
            );

        state.SecondaryMode =
            secondaryMode;

        ResetSecondaryDeck(
            state
        );

        if (secondaryMode ==
            MissionSecondaryMode.Fixed)
        {
            state.FixedSecondaries.Add(
                MissionRegistry
                    .FixedEligibleSecondaries[0]
            );

            state.FixedSecondaries.Add(
                MissionRegistry
                    .FixedEligibleSecondaries[1]
            );
        }

        return state;
    }

    public MissionPlayerState State(
        string faction)
    {
        MissionPlayerState state;

        return playerStates.TryGetValue(
            faction,
            out state)
            ? state
            : null;
    }

    public string PrimaryFor(
        string faction)
    {
        MissionPlayerState state =
            State(faction);

        return state != null
            ? state.PrimaryMission
            : "No mission";
    }

    public string DispositionFor(
        string faction)
    {
        MissionPlayerState state =
            State(faction);

        return state != null
            ? MissionRegistry.Display(
                state.Disposition
            )
            : "Unassigned";
    }

    public void BeginTurn(
        string faction,
        int round)
    {
        currentTurnFaction = faction;

        destroyedVictimsCurrentTurn.Clear();
        characterModelsDestroyedThisTurn.Clear();
        characterW4ModelsDestroyedThisTurn.Clear();
        largeModelsDestroyedThisTurn.Clear();
        surveilledThisTurn.Clear();

        List<SquadController> staleActions =
            activeMissionActions
                .Keys
                .Where(
                    unit =>
                        unit == null ||
                        unit.FactionId ==
                            faction
                )
                .ToList();

        foreach (SquadController unit
            in staleActions)
        {
            if (unit != null)
                unit.CancelMissionAction();

            activeMissionActions.Remove(
                unit
            );
        }

        MissionPlayerState state =
            State(faction);

        if (state == null)
            return;

        state.ControlledAtTurnStart.Clear();

        foreach (ObjectiveController objective
            in objectives)
        {
            if (objective != null &&
                objective.Controller(
                    squads
                ) ==
                faction)
            {
                state.ControlledAtTurnStart.Add(
                    objective
                );
            }
        }

        int previousFriendlyDestroyed = 0;

        destroyedVictimsPreviousTurn
            .TryGetValue(
                faction,
                out previousFriendlyDestroyed
            );

        state.FriendlyUnitsDestroyedPreviousTurn =
            previousFriendlyDestroyed;

        state.EnemyUnitsDestroyedThisTurn = 0;
        state.TurnActionCompletions = 0;
        state.TurnBonusCompletions = 0;
        state.TurnSpecialEvents = 0;
    }

    public void RecordModelDestroyed(
        ModelToken model)
    {
        if (model == null ||
            model.Squad == null)
        {
            return;
        }

        string victimFaction =
            model.Squad.FactionId;

        if (model.Squad.HasIntrinsicKeyword(
                "CHARACTER"))
        {
            if (!characterModelsDestroyedThisTurn
                .ContainsKey(victimFaction))
            {
                characterModelsDestroyedThisTurn[
                    victimFaction] = 0;
            }

            characterModelsDestroyedThisTurn[
                victimFaction]++;

            if (model.MaxWounds >= 4)
            {
                if (!characterW4ModelsDestroyedThisTurn
                    .ContainsKey(victimFaction))
                {
                    characterW4ModelsDestroyedThisTurn[
                        victimFaction] = 0;
                }

                characterW4ModelsDestroyedThisTurn[
                    victimFaction]++;
            }
        }

        if (model.MaxWounds >= 10)
        {
            if (!largeModelsDestroyedThisTurn
                .ContainsKey(victimFaction))
            {
                largeModelsDestroyedThisTurn[
                    victimFaction] = 0;
            }

            largeModelsDestroyedThisTurn[
                victimFaction]++;
        }
    }

    public void RecordUnitDestroyed(
        SquadController destroyed,
        SquadController attacker)
    {
        if (destroyed == null)
            return;

        string victim =
            destroyed.FactionId;

        if (!destroyedVictimsCurrentTurn
            .ContainsKey(victim))
        {
            destroyedVictimsCurrentTurn[
                victim] = 0;
        }

        destroyedVictimsCurrentTurn[
            victim]++;

        MissionPlayerState active =
            State(
                currentTurnFaction
            );

        if (active != null &&
            victim !=
                currentTurnFaction)
        {
            active.EnemyUnitsDestroyedThisTurn++;
        }
    }

    public void EndTurnSnapshot()
    {
        destroyedVictimsPreviousTurn.Clear();

        foreach (
            KeyValuePair<string, int> pair
            in destroyedVictimsCurrentTurn)
        {
            destroyedVictimsPreviousTurn[
                pair.Key] = pair.Value;
        }
    }

    public int ControlledCount(
        string faction)
    {
        return objectives.Count(
            objective =>
                objective != null &&
                objective.Controller(
                    squads
                ) ==
                faction
        );
    }

    public int ControlledNonHomeCount(
        string faction)
    {
        return objectives.Count(
            objective =>
                objective != null &&
                !IsHomeObjective(
                    objective,
                    faction
                ) &&
                objective.Controller(
                    squads
                ) ==
                faction
        );
    }

    public int ControlledCentralCount(
        string faction)
    {
        return objectives.Count(
            objective =>
                objective != null &&
                objective.MissionRole ==
                    MissionObjectiveRole.Central &&
                objective.Controller(
                    squads
                ) ==
                faction
        );
    }

    public int ControlledExpansionCount(
        string faction)
    {
        return objectives.Count(
            objective =>
                objective != null &&
                objective.MissionRole ==
                    MissionObjectiveRole.Expansion &&
                objective.Controller(
                    squads
                ) ==
                faction
        );
    }

    public bool ControlsHome(
        string faction)
    {
        ObjectiveController home =
            HomeObjective(
                faction
            );

        return
            home != null &&
            home.Controller(
                squads
            ) ==
                faction;
    }

    public bool ControlsOpponentHome(
        string faction)
    {
        ObjectiveController home =
            OpponentHomeObjective(
                faction
            );

        return
            home != null &&
            home.Controller(
                squads
            ) ==
                faction;
    }

    private bool IsHomeObjective(
        ObjectiveController objective,
        string faction)
    {
        return
            objective ==
            HomeObjective(
                faction
            );
    }

    private ObjectiveController HomeObjective(
        string faction)
    {
        if (factions.Count < 2)
            return null;

        MissionObjectiveRole role =
            faction == factions[0]
            ? MissionObjectiveRole.PlayerOneHome
            : MissionObjectiveRole.PlayerTwoHome;

        return objectives.FirstOrDefault(
            objective =>
                objective != null &&
                objective.MissionRole ==
                    role
        );
    }

    private ObjectiveController
        OpponentHomeObjective(
            string faction)
    {
        if (factions.Count < 2)
            return null;

        MissionObjectiveRole role =
            faction == factions[0]
            ? MissionObjectiveRole.PlayerTwoHome
            : MissionObjectiveRole.PlayerOneHome;

        return objectives.FirstOrDefault(
            objective =>
                objective != null &&
                objective.MissionRole ==
                    role
        );
    }

    private int ControlledInOpponentTerritory(
        string faction)
    {
        if (factions.Count < 2)
            return 0;

        bool playerOne =
            faction == factions[0];

        return objectives.Count(
            objective =>
                objective != null &&
                objective.Controller(
                    squads
                ) ==
                    faction &&
                (playerOne
                    ? objective.transform.position.x >
                        0f
                    : objective.transform.position.x <
                        0f)
        );
    }

    private int NewlyControlledNonHomeCount(
        MissionPlayerState state)
    {
        if (state == null)
            return 0;

        return objectives.Count(
            objective =>
                objective != null &&
                !IsHomeObjective(
                    objective,
                    state.FactionId
                ) &&
                objective.Controller(
                    squads
                ) ==
                    state.FactionId &&
                !state.ControlledAtTurnStart
                    .Contains(
                        objective
                    )
        );
    }

    private int ReconQuarterCount(
        string faction,
        out int qualifyingUnits)
    {
        HashSet<int> quarters =
            new HashSet<int>();

        qualifyingUnits = 0;

        foreach (SquadController unit
            in squads)
        {
            if (unit == null ||
                unit.IsAttachedLeader ||
                !unit.IsAlive ||
                !unit.IsOnBattlefield ||
                unit.FactionId !=
                    faction)
            {
                continue;
            }

            List<ModelToken> models =
                unit.JoinedLivingModelTokens();

            if (models.Count == 0)
                continue;

            bool outsideCentre =
                models.All(
                    model =>
                        Vector2.Distance(
                            new Vector2(
                                model.transform.position.x,
                                model.transform.position.z
                            ),
                            Vector2.zero
                        ) >
                        6f
                );

            if (!outsideCentre)
                continue;

            bool allPositiveX =
                models.All(
                    model =>
                        model.transform.position.x >
                        0f
                );

            bool allNegativeX =
                models.All(
                    model =>
                        model.transform.position.x <
                        0f
                );

            bool allPositiveZ =
                models.All(
                    model =>
                        model.transform.position.z >
                        0f
                );

            bool allNegativeZ =
                models.All(
                    model =>
                        model.transform.position.z <
                        0f
                );

            int quarter = -1;

            if (allPositiveX &&
                allPositiveZ)
            {
                quarter = 0;
            }
            else if (allPositiveX &&
                     allNegativeZ)
            {
                quarter = 1;
            }
            else if (allNegativeX &&
                     allPositiveZ)
            {
                quarter = 2;
            }
            else if (allNegativeX &&
                     allNegativeZ)
            {
                quarter = 3;
            }

            if (quarter < 0)
                continue;

            qualifyingUnits++;
            quarters.Add(
                quarter
            );
        }

        return quarters.Count;
    }

    private bool NoEnemyWhollyInOwnTerritory(
        string faction)
    {
        if (factions.Count < 2)
            return false;

        bool playerOne =
            faction == factions[0];

        foreach (SquadController enemy
            in squads)
        {
            if (enemy == null ||
                enemy.IsAttachedLeader ||
                !enemy.IsAlive ||
                !enemy.IsOnBattlefield ||
                enemy.FactionId ==
                    faction)
            {
                continue;
            }

            bool wholly =
                enemy
                    .JoinedLivingModelTokens()
                    .All(
                        model =>
                            playerOne
                            ? model.transform.position.x <
                                0f
                            : model.transform.position.x >
                                0f
                    );

            if (wholly)
                return false;
        }

        return true;
    }

    private int CalculateCommandPrimary(
        MissionPlayerState state,
        string faction,
        int round)
    {
        if (state == null ||
            round < 2)
        {
            return 0;
        }

        int controlled =
            ControlledCount(
                faction
            );

        int nonHome =
            ControlledNonHomeCount(
                faction
            );

        int opponentControlled =
            factions
                .Where(
                    value =>
                        value != faction
                )
                .Select(
                    ControlledCount
                )
                .DefaultIfEmpty(0)
                .Max();

        int gained = 0;
        string mission =
            state.PrimaryMission;

        if (round == 5)
        {
            gained +=
                CalculateCommandPrimary(
                    state,
                    faction,
                    round
                );
        }

        switch (mission)
        {
            case "Battlefield Dominance":
                gained +=
                    controlled * 3;

                if (ControlsHome(faction))
                    gained +=
                        nonHome * 2;
                break;

            case "Determined Acquisition":
                gained +=
                    controlled * 3;

                gained +=
                    ControlledInOpponentTerritory(
                        faction
                    ) * 3;
                break;

            case "Immovable Object":
                if (round <= 4)
                    gained +=
                        nonHome * 5;
                break;

            case "Inescapable Dominion":
                if (controlled >= 2)
                    gained += 5;

                if (controlled >
                    opponentControlled)
                {
                    gained += 4;
                }
                break;

            case "Purge and Secure":
                gained +=
                    nonHome * 4;
                break;

            case "Consecrate":
                if (nonHome >= 1)
                    gained += 4;

                if (controlled >
                    opponentControlled)
                {
                    gained += 4;
                }
                break;

            case "Destroyer's Wrath":
                if (nonHome >= 1)
                    gained += 4;

                if (controlled >
                    opponentControlled)
                {
                    gained += 6;
                }
                break;

            case "Meatgrinder":
                if (nonHome >= 1)
                    gained += 4;
                break;

            case "Punishment":
                if (nonHome >= 1)
                    gained += 4;

                if (controlled >
                    opponentControlled)
                {
                    gained += 5;
                }
                break;

            case "Unstoppable Force":
                gained +=
                    nonHome * 4;
                break;

            case "Gather Intel":
                if (nonHome >= 1)
                    gained += 4;
                break;

            case "Reconnaissance Sweep":
                if (nonHome >= 1)
                    gained += 3;
                break;

            case "Search and Scour":
                gained +=
                    nonHome * 4;
                break;

            case "Surveil the Foe":
                if (nonHome >= 1)
                    gained += 4;

                if (controlled >
                    opponentControlled)
                {
                    gained += 4;
                }
                break;

            case "Triangulation":
                if (nonHome >= 1)
                    gained += 4;
                break;

            case "Extract Relic":
            case "Sabotage":
            case "Vanguard Operation":
            case "Death Trap":
            case "Delaying Action":
            case "Locate and Deny":
            case "Smoke and Mirrors":
                if (nonHome >= 1)
                    gained += 4;
                break;

            case "Secure Asset":
                if (nonHome >= 1)
                    gained += 4;

                if (controlled >= 3)
                    gained += 4;
                break;

            case "Vital Link":
                if (nonHome >= 1)
                    gained += 4;

                if (ControlledCentralCount(
                        faction) >= 1)
                {
                    gained += 4;
                }
                break;

            case "Outmanoeuvre":
                if (round == 2 ||
                    round == 3)
                {
                    gained +=
                        nonHome * 5;
                }
                break;
        }

        return gained;
    }

    private void InitializeMissionWorldState()
    {
        activeMissionActions.Clear();
        surveilledThisTurn.Clear();

        TerrainFeature[] terrain =
            Object.FindObjectsByType<
                TerrainFeature
            >();

        foreach (TerrainFeature feature
            in terrain)
        {
            if (feature != null)
                feature.ClearOperationMarker();
        }

        foreach (MissionPlayerState state
            in playerStates.Values)
        {
            if (state == null ||
                state.PrimaryMission !=
                    "Locate and Deny")
            {
                continue;
            }

            List<TerrainFeature> candidates =
                terrain
                    .Where(
                        feature =>
                            feature != null &&
                            !game
                                .MissionPositionInEitherDeploymentZone(
                                    feature.transform.position
                                )
                    )
                    .OrderByDescending(
                        feature =>
                            Mathf.Abs(
                                feature.transform.position.x
                            ) +
                            Mathf.Abs(
                                feature.transform.position.z
                            )
                    )
                    .ToList();

            if (candidates.Count < 5)
            {
                candidates =
                    terrain
                        .Where(
                            feature =>
                                feature != null
                        )
                        .ToList();
            }

            foreach (TerrainFeature feature
                in candidates.Take(5))
            {
                feature.SetOperationMarker(
                    state.FactionId
                );
            }

            state.OperationMarkers =
                candidates
                    .Take(5)
                    .Count();
        }
    }

    public MissionActionDefinition PrimaryAction(
        string faction)
    {
        MissionPlayerState state =
            State(faction);

        return
            state == null
            ? null
            : MissionActionRegistry
                .ForPrimary(
                    state.PrimaryMission
                );
    }

    public bool CanStartMissionAction(
        SquadController unit,
        out string reason)
    {
        reason = "";

        if (unit == null)
        {
            reason =
                "Select a unit first.";
            return false;
        }

        SquadController actionUnit =
            unit.JoinedActionController();

        if (actionUnit == null ||
            !actionUnit.IsAlive ||
            !actionUnit.IsOnBattlefield)
        {
            reason =
                "That unit is not on the battlefield.";
            return false;
        }

        if (actionUnit.FactionId !=
            currentTurnFaction)
        {
            reason =
                "Only the active player's unit can start this mission action.";
            return false;
        }

        if (game.CurrentPhase !=
            GameController.Phase.Shoot)
        {
            reason =
                "This mission action starts in the Shooting phase.";
            return false;
        }

        if (actionUnit.HasKeyword(
                "aircraft") ||
            actionUnit.HasKeyword(
                "fortification"))
        {
            reason =
                "AIRCRAFT and FORTIFICATION units cannot start actions.";
            return false;
        }

        if (actionUnit.IsBattleShocked)
        {
            reason =
                "Battle-shocked units cannot start actions.";
            return false;
        }

        bool hasObjectiveControl =
            actionUnit
                .JoinedLivingModelTokens()
                .Any(
                    model =>
                        model != null &&
                        model.IsAlive &&
                        model.ObjectiveControl > 0
                );

        if (!hasObjectiveControl)
        {
            reason =
                "That unit has no model with Objective Control.";
            return false;
        }

        if (game.MissionUnitIsEngaged(
                actionUnit) &&
            !actionUnit.HasKeyword(
                "titanic"))
        {
            reason =
                "An engaged unit cannot start an action unless it is TITANIC.";
            return false;
        }

        if (actionUnit.HasAdvanced ||
            actionUnit.HasFallenBack)
        {
            reason =
                "A unit that Advanced or Fell Back this turn cannot start an action.";
            return false;
        }

        if (actionUnit.HasShot)
        {
            reason =
                "A unit that has already shot this phase cannot start an action.";
            return false;
        }

        if (actionUnit
            .StartedMissionActionThisTurn)
        {
            reason =
                "That unit already started an action this turn.";
            return false;
        }

        MissionActionDefinition definition =
            PrimaryAction(
                actionUnit.FactionId
            );

        if (definition == null)
        {
            reason =
                "This Primary Mission has no unit-started Objective Action.";
            return false;
        }

        if (definition
                .RequireCentralObjectiveRange &&
            !objectives.Any(
                objective =>
                    objective != null &&
                    objective.MissionRole ==
                        MissionObjectiveRole
                            .Central &&
                    objective.UnitWithinRange(
                        actionUnit
                    )
            ))
        {
            reason =
                definition.DisplayName +
                " requires the unit to be within range of a central objective.";
            return false;
        }

        return true;
    }

    public bool ValidateMissionActionTarget(
        SquadController unit,
        MissionActionDefinition definition,
        ObjectiveController objective,
        TerrainFeature terrain,
        SquadController enemy,
        out string reason)
    {
        reason = "";

        if (unit == null ||
            definition == null)
        {
            reason =
                "No mission action is selected.";
            return false;
        }

        SquadController actionUnit =
            unit.JoinedActionController();

        if (definition.TargetType ==
            MissionActionTargetType.None)
        {
            return true;
        }

        if (definition.TargetType ==
            MissionActionTargetType.Objective)
        {
            if (objective == null)
            {
                reason =
                    "Click an objective marker.";
                return false;
            }

            if (!objective.UnitWithinRange(
                    actionUnit))
            {
                reason =
                    "The selected unit is not within range of that objective.";
                return false;
            }

            if (definition
                    .ExcludeHomeObjective &&
                game.IsHomeObjectiveForFaction(
                    objective,
                    actionUnit.FactionId))
            {
                reason =
                    "Your home objective is not a legal target for this action.";
                return false;
            }

            if (definition
                    .RequireCentralObjectiveTarget &&
                objective.MissionRole !=
                    MissionObjectiveRole.Central)
            {
                reason =
                    "Select a central objective for this action.";
                return false;
            }

            if (definition.Id ==
                    "decoy" &&
                objective.HasMissionState(
                    actionUnit.FactionId,
                    "decoyed"))
            {
                reason =
                    "That objective is already decoyed by your army.";
                return false;
            }

            if (definition.Id ==
                    "triangulate" &&
                objective.HasMissionState(
                    actionUnit.FactionId,
                    "triangulated"))
            {
                reason =
                    "That objective is already triangulated by your army.";
                return false;
            }

            return true;
        }

        if (definition.TargetType ==
            MissionActionTargetType.Terrain ||
            definition.TargetType ==
            MissionActionTargetType
                .OperationMarkerTerrain)
        {
            if (terrain == null)
            {
                reason =
                    "Click a terrain area.";
                return false;
            }

            if (definition
                    .RequireOpponentTerritoryTerrain &&
                !game.IsPositionInOpponentTerritory(
                    actionUnit.FactionId,
                    terrain.transform.position))
            {
                reason =
                    "Select terrain in your opponent's territory.";
                return false;
            }

            if (definition
                    .RequireOperationMarkerTarget)
            {
                string opponent =
                    factions.FirstOrDefault(
                        faction =>
                            faction !=
                                actionUnit.FactionId
                    );

                if (string.IsNullOrWhiteSpace(
                        opponent) ||
                    !terrain.HasOperationMarker(
                        opponent))
                {
                    reason =
                        "Select terrain containing one of your opponent's operation markers.";
                    return false;
                }
            }
            else if (!game.UnitWithinTerrainArea(
                         actionUnit,
                         terrain))
            {
                reason =
                    "The selected unit is not within that terrain area.";
                return false;
            }

            return true;
        }

        if (definition.TargetType ==
            MissionActionTargetType.EnemyUnit)
        {
            if (enemy == null ||
                enemy.FactionId ==
                    actionUnit.FactionId ||
                !enemy.IsAlive ||
                !enemy.IsOnBattlefield)
            {
                reason =
                    "Select an enemy unit.";
                return false;
            }

            if (actionUnit.DistanceTo(
                    enemy) >
                18.001f)
            {
                reason =
                    "The enemy unit must be within 18 inches.";
                return false;
            }

            if (!game.MissionUnitsHaveLineOfSight(
                    actionUnit,
                    enemy))
            {
                reason =
                    "The enemy unit is not visible.";
                return false;
            }

            if (surveilledThisTurn.Contains(
                    enemy))
            {
                reason =
                    "That enemy unit has already been surveilled this turn.";
                return false;
            }

            return true;
        }

        return true;
    }

    public bool StartMissionAction(
        SquadController unit,
        ObjectiveController objective,
        TerrainFeature terrain,
        SquadController enemy,
        out string result)
    {
        result = "";

        string reason;

        if (!CanStartMissionAction(
                unit,
                out reason))
        {
            result = reason;
            return false;
        }

        SquadController actionUnit =
            unit.JoinedActionController();

        MissionActionDefinition definition =
            PrimaryAction(
                actionUnit.FactionId
            );

        if (!ValidateMissionActionTarget(
                actionUnit,
                definition,
                objective,
                terrain,
                enemy,
                out reason))
        {
            result = reason;
            return false;
        }

        actionUnit.BeginMissionAction(
            definition.Id
        );

        ActiveMissionAction action =
            new ActiveMissionAction
            {
                Unit = actionUnit,
                Definition = definition,
                ObjectiveTarget = objective,
                TerrainTarget = terrain,
                EnemyTarget = enemy,
                StartedRound =
                    game.CurrentRoundNumber
            };

        foreach (ModelToken model
            in actionUnit
                .JoinedLivingModelTokens())
        {
            action.StartPositions[
                model] =
                model.transform.position;
        }

        if (definition.CompletesImmediately)
        {
            bool completed =
                CompleteMissionAction(
                    action,
                    out result
                );

            actionUnit
                .CompleteMissionAction();

            return completed;
        }

        activeMissionActions[
            actionUnit] =
            action;

        result =
            actionUnit.DisplayName +
            " started " +
            definition.DisplayName +
            ". It cannot shoot or declare a charge this turn.";

        return true;
    }

    public bool UnitHasStartedAction(
        SquadController unit)
    {
        if (unit == null)
            return false;

        SquadController actionUnit =
            unit.JoinedActionController();

        return
            actionUnit != null &&
            actionUnit
                .StartedMissionActionThisTurn;
    }

    public string ActiveActionName(
        SquadController unit)
    {
        if (unit == null)
            return "";

        SquadController actionUnit =
            unit.JoinedActionController();

        ActiveMissionAction action;

        if (actionUnit != null &&
            activeMissionActions.TryGetValue(
                actionUnit,
                out action) &&
            action != null &&
            action.Definition != null)
        {
            return
                action.Definition.DisplayName;
        }

        return "";
    }

    public void CancelActionIfUnitLeavesBattlefield(
        SquadController unit)
    {
        if (unit == null)
            return;

        SquadController actionUnit =
            unit.JoinedActionController();

        ActiveMissionAction action;

        if (!activeMissionActions.TryGetValue(
                actionUnit,
                out action))
        {
            return;
        }

        actionUnit.CancelMissionAction();

        activeMissionActions.Remove(
            actionUnit
        );
    }

    private void ResolvePendingActionsAtEndTurn(
        string faction)
    {
        List<ActiveMissionAction> actions =
            activeMissionActions
                .Values
                .Where(
                    action =>
                        action != null &&
                        action.Unit != null &&
                        action.Unit.FactionId ==
                            faction
                )
                .ToList();

        foreach (ActiveMissionAction action
            in actions)
        {
            string ignored;

            CompleteMissionAction(
                action,
                out ignored
            );

            if (action.Unit != null)
                action.Unit.CompleteMissionAction();

            activeMissionActions.Remove(
                action.Unit
            );
        }
    }

    private bool CompleteMissionAction(
        ActiveMissionAction action,
        out string result)
    {
        result = "";

        if (action == null ||
            action.Unit == null ||
            action.Definition == null ||
            !action.Unit.IsAlive ||
            !action.Unit.IsOnBattlefield)
        {
            result =
                "Mission action failed because the performing unit left the battlefield or was destroyed.";
            return false;
        }

        MissionActionDefinition definition =
            action.Definition;

        SquadController unit =
            action.Unit;

        if (!unit.HasKeyword(
                "titanic"))
        {
            foreach (
                KeyValuePair<
                    ModelToken,
                    Vector3
                > pair
                in action.StartPositions)
            {
                if (pair.Key == null ||
                    !pair.Key.IsAlive)
                {
                    continue;
                }

                float moved =
                    Vector2.Distance(
                        new Vector2(
                            pair.Key.transform.position.x,
                            pair.Key.transform.position.z
                        ),
                        new Vector2(
                            pair.Value.x,
                            pair.Value.z
                        )
                    );

                if (moved > 0.01f)
                {
                    result =
                        definition.DisplayName +
                        " failed because the performing unit moved before the action completed.";
                    return false;
                }
            }
        }

        if (definition
                .RequireControlAtCompletion &&
            action.ObjectiveTarget != null &&
            action.ObjectiveTarget.Controller(
                squads
            ) !=
                unit.FactionId)
        {
            result =
                definition.DisplayName +
                " failed because the unit did not control the target objective when the action completed.";
            return false;
        }

        MissionPlayerState state =
            State(
                unit.FactionId
            );

        if (state == null)
            return false;

        switch (definition.Id)
        {
            case "decoy":
                action.ObjectiveTarget
                    .SetMissionState(
                        unit.FactionId,
                        "decoyed"
                    );

                state.TurnActionCompletions++;

                result =
                    action.ObjectiveTarget
                        .name +
                    " is now decoyed.";
                break;

            case "trap_terrain":
                action.TerrainTarget.Trap(
                    unit.FactionId,
                    game.CurrentRoundNumber
                );

                state.TurnActionCompletions++;

                if (action.TerrainTarget
                    .IsMissionObjectiveArea)
                {
                    state.TurnBonusCompletions++;
                }

                result =
                    "Terrain " +
                    action.TerrainTarget
                        .MissionTerrainId +
                    " is trapped.";
                break;

            case "sensor_sweep":
                action.TerrainTarget
                    .ClearOperationMarker();

                state.TurnActionCompletions++;

                result =
                    "Sensor Sweep completed; the selected enemy operation marker was removed.";
                break;

            case "sabotage":
                state.TurnActionCompletions++;

                if (objectives.Any(
                    objective =>
                        objective != null &&
                        game
                            .IsPositionInOpponentTerritory(
                                unit.FactionId,
                                objective.transform.position
                            ) &&
                        objective.UnitWithinRange(
                            unit
                        )
                ))
                {
                    state.TurnBonusCompletions++;
                }

                result =
                    unit.DisplayName +
                    " committed sabotage.";
                break;

            case "vanguard_operation":
                bool enemyPresent =
                    squads.Any(
                        candidate =>
                            candidate != null &&
                            candidate.IsAlive &&
                            candidate.IsOnBattlefield &&
                            candidate.FactionId !=
                                unit.FactionId &&
                            game.UnitWithinTerrainArea(
                                candidate,
                                action.TerrainTarget
                            )
                    );

                if (enemyPresent)
                {
                    result =
                        "Vanguard Operation failed: an enemy unit is in the selected terrain area.";
                    return false;
                }

                state.TurnActionCompletions++;

                result =
                    "Vanguard Operation completed.";
                break;

            case "triangulate":
                action.ObjectiveTarget
                    .SetMissionState(
                        unit.FactionId,
                        "triangulated"
                    );

                result =
                    "Objective triangulated.";
                break;

            case "extract_intelligence":
                action.ObjectiveTarget
                    .SetMissionState(
                        unit.FactionId,
                        "intel"
                    );

                state.TurnActionCompletions++;

                result =
                    "Extract Intelligence completed.";
                break;

            case "surveil":
                surveilledThisTurn.Add(
                    action.EnemyTarget
                );

                state.TurnSpecialEvents++;

                result =
                    action.EnemyTarget.DisplayName +
                    " is surveilled until end of turn.";
                break;

            case "secure_asset":
                action.ObjectiveTarget
                    .SetMissionState(
                        unit.FactionId,
                        "secured_asset"
                    );

                state.TurnActionCompletions++;

                result =
                    "Central asset secured.";
                break;

            case "establish_link":
                action.ObjectiveTarget
                    .SetMissionState(
                        unit.FactionId,
                        "vital_link"
                    );

                state.OperationMarkers++;

                result =
                    "Vital Link operation marker established.";
                break;

            default:
                result =
                    definition.DisplayName +
                    " completed.";
                break;
        }

        SynchronizeMissionWorldState(
            unit.FactionId
        );

        return true;
    }

    private void SynchronizeMissionWorldState(
        string faction)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null)
            return;

        if (state.PrimaryMission ==
            "Smoke and Mirrors")
        {
            state.PersistentMarks =
                objectives.Count(
                    objective =>
                        objective != null &&
                        objective.HasMissionState(
                            faction,
                            "decoyed"
                        )
                );

            state.PersistentBonusMarks =
                objectives.Count(
                    objective =>
                        objective != null &&
                        objective.HasMissionState(
                            faction,
                            "decoyed"
                        ) &&
                        game
                            .IsPositionInOpponentTerritory(
                                faction,
                                objective.transform.position
                            )
                );
        }
        else if (state.PrimaryMission ==
                 "Triangulation")
        {
            state.PersistentMarks =
                objectives.Count(
                    objective =>
                        objective != null &&
                        objective.HasMissionState(
                            faction,
                            "triangulated"
                        )
                );
        }
        else if (state.PrimaryMission ==
                 "Gather Intel")
        {
            state.OperationMarkers =
                objectives.Count(
                    objective =>
                        objective != null &&
                        objective.HasMissionState(
                            faction,
                            "intel"
                        )
                );

            ObjectiveController enemyHome =
                objectives.FirstOrDefault(
                    objective =>
                        objective != null &&
                        game.IsOpponentHomeObjectiveForFaction(
                            objective,
                            faction
                        )
                );

            state.PersistentBonusMarks =
                enemyHome != null &&
                enemyHome.HasMissionState(
                    faction,
                    "intel")
                ? 1
                : 0;
        }
        else if (state.PrimaryMission ==
                 "Locate and Deny")
        {
            state.OperationMarkers =
                Object.FindObjectsByType<
                    TerrainFeature
                >()
                .Count(
                    feature =>
                        feature != null &&
                        feature.HasOperationMarker(
                            faction
                        )
                );
        }
        else if (state.PrimaryMission ==
                 "Vital Link")
        {
            state.OperationMarkers =
                objectives.Count(
                    objective =>
                        objective != null &&
                        objective.HasMissionState(
                            faction,
                            "vital_link"
                        )
                );
        }
    }

    public string ResolveCommandScoring(
        string faction,
        int round)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null ||
            round < 2 ||
            round >= 5)
        {
            return "";
        }

        int gained =
            CalculateCommandPrimary(
                state,
                faction,
                round
            );

        gained =
            game.AddPrimaryScoreCapped(
                faction,
                gained,
                round
            );

        return gained > 0
            ? state.PrimaryMission +
              " primary: +" +
              gained +
              " VP."
            : "";
    }

    public string ResolveEndTurnScoring(
        string faction,
        int round)
    {
        ResolvePendingActionsAtEndTurn(
            faction
        );

        SynchronizeMissionWorldState(
            faction
        );

        MissionPlayerState state =
            State(faction);

        if (state == null)
            return "";

        int controlled =
            ControlledCount(
                faction
            );

        int nonHome =
            ControlledNonHomeCount(
                faction
            );

        int opponentControlled =
            factions
                .Where(
                    value =>
                        value != faction
                )
                .Select(
                    ControlledCount
                )
                .DefaultIfEmpty(0)
                .Max();

        int gained = 0;
        string mission =
            state.PrimaryMission;

        switch (mission)
        {
            case "Battlefield Dominance":
                if (round <= 2 &&
                    controlled >
                        opponentControlled)
                {
                    gained += 2;
                }
                break;

            case "Determined Acquisition":
                gained +=
                    NewlyControlledNonHomeCount(
                        state
                    ) * 2;
                break;

            case "Immovable Object":
                if (ControlledCentralCount(
                        faction) >= 1)
                {
                    gained += 3;
                }

                if (round == 5)
                    gained +=
                        nonHome * 5;
                break;

            case "Inescapable Dominion":
                if (controlled >= 3)
                    gained += 4;
                break;

            case "Purge and Secure":
                if (state.TurnSpecialEvents > 0)
                    gained += 3;

                if (round >= 2 &&
                    NewlyControlledNonHomeCount(
                        state) >= 1)
                {
                    gained += 3;
                }
                break;

            case "Consecrate":
                if (state.PersistentMarks >= 3)
                    gained += 6;
                else if (state.PersistentMarks >= 1)
                    gained += 3;
                break;

            case "Destroyer's Wrath":
                if (state.EnemyUnitsDestroyedThisTurn >
                    0)
                {
                    gained += 3;
                }

                if (round >= 2 &&
                    state.EnemyUnitsDestroyedThisTurn >
                    state.FriendlyUnitsDestroyedPreviousTurn)
                {
                    gained += 4;
                }
                break;

            case "Meatgrinder":
                if (state.EnemyUnitsDestroyedThisTurn >
                    0)
                {
                    gained += 3;
                }

                if (round >= 2 &&
                    state.EnemyUnitsDestroyedThisTurn >
                    state.FriendlyUnitsDestroyedPreviousTurn)
                {
                    gained += 5;
                }

                if (round >= 2 &&
                    ControlsOpponentHome(
                        faction))
                {
                    gained += 5;
                }
                break;

            case "Punishment":
                if (state.TurnSpecialEvents > 0)
                    gained += 5;
                break;

            case "Unstoppable Force":
                if (state.EnemyUnitsDestroyedThisTurn >
                    0)
                {
                    gained += 3;
                }

                if (round >= 2 &&
                    NewlyControlledNonHomeCount(
                        state) >= 1)
                {
                    gained += 3;
                }
                break;

            case "Gather Intel":
                if (round == 1 &&
                    ControlledCentralCount(
                        faction) >= 1)
                {
                    gained += 6;
                }

                if (round >= 2)
                {
                    gained +=
                        state.TurnActionCompletions *
                        7;
                }
                break;

            case "Reconnaissance Sweep":
                int qualifyingUnits;
                int quarters =
                    ReconQuarterCount(
                        faction,
                        out qualifyingUnits
                    );

                if (quarters >= 4 &&
                    qualifyingUnits >= 4)
                {
                    gained += 6;
                }
                else if (quarters >= 3 &&
                         qualifyingUnits >= 3)
                {
                    gained += 3;
                }

                gained +=
                    state.EnemyUnitsDestroyedThisTurn;
                break;

            case "Search and Scour":
                if (ControlledCentralCount(
                        faction) >= 1)
                {
                    gained += 3;
                }

                if (state.TurnSpecialEvents > 0)
                    gained += 2;
                break;

            case "Surveil the Foe":
                if (state.TurnSpecialEvents > 0)
                    gained += 4;

                if (round >= 2 &&
                    OpponentOperationMarkerCount(
                        faction) == 0)
                {
                    gained += 5;
                }
                break;

            case "Triangulation":
                if (round >= 2)
                {
                    if (state.PersistentMarks >= 3)
                        gained += 10;
                    else if (state.PersistentMarks == 2)
                        gained += 6;
                    else if (state.PersistentMarks == 1)
                        gained += 3;
                }
                break;

            case "Extract Relic":
                gained +=
                    Mathf.Min(
                        1,
                        state.TurnActionCompletions
                    ) * 4;

                if (state.TurnSpecialEvents > 0)
                    gained += 4;

                break;

            case "Sabotage":
                gained +=
                    state.TurnActionCompletions *
                    3;

                gained +=
                    state.TurnBonusCompletions *
                    2;
                break;

            case "Secure Asset":
                gained +=
                    Mathf.Min(
                        1,
                        state.TurnActionCompletions
                    ) * 4;

                if (state.TurnSpecialEvents > 0)
                    gained += 2;
                break;

            case "Vanguard Operation":
                gained +=
                    Mathf.Min(
                        1,
                        state.TurnActionCompletions
                    ) * 4;

                if (state.EnemyUnitsDestroyedThisTurn >
                    0)
                {
                    gained += 2;
                }
                break;

            case "Vital Link":
                if (ControlledCentralCount(
                        faction) >= 1)
                {
                    gained += 2;
                    gained +=
                        state.OperationMarkers;
                }
                break;

            case "Death Trap":
                gained +=
                    state.TurnActionCompletions *
                    2;

                gained +=
                    state.TurnBonusCompletions *
                    3;

                if (state.TurnSpecialEvents > 0)
                    gained += 3;
                break;

            case "Delaying Action":
                gained +=
                    state.EnemyUnitsDestroyedThisTurn *
                    2;

                if (round >= 2 &&
                    ControlledCentralCount(
                        faction) >= 1 &&
                    ControlledExpansionCount(
                        faction) >= 1)
                {
                    gained += 3;
                }
                break;

            case "Locate and Deny":
                if (state.TurnSpecialEvents > 0)
                    gained += 4;
                break;

            case "Outmanoeuvre":
                if (ControlsOpponentHome(
                        faction))
                {
                    gained += 10;
                }

                if (round == 1)
                    gained +=
                        nonHome * 4;

                if (round >= 4)
                    gained +=
                        nonHome * 6;
                break;

            case "Smoke and Mirrors":
                gained +=
                    state.PersistentMarks *
                    2;

                gained +=
                    state.PersistentBonusMarks *
                    2;
                break;
        }

        gained =
            game.AddPrimaryScoreCapped(
                faction,
                gained,
                round
            );

        return gained > 0
            ? mission +
              " primary: +" +
              gained +
              " VP."
            : "";
    }

    private string OpponentFaction(
        string faction)
    {
        return factions.FirstOrDefault(
            value =>
                value != faction
        );
    }

    private bool IsDefenderFaction(
        string faction)
    {
        int index =
            factions.IndexOf(
                faction
            );

        return
            index >= 0 &&
            index != AttackerIndex;
    }

    private bool SecondaryEligibleUnit(
        SquadController unit,
        string faction)
    {
        return
            unit != null &&
            !unit.IsAttachedLeader &&
            unit.IsAlive &&
            unit.IsOnBattlefield &&
            unit.FactionId == faction &&
            !unit.IsBattleShocked &&
            !unit.HasKeyword(
                "AIRCRAFT"
            );
    }

    private int SecondaryQuarterPresence(
        string faction)
    {
        HashSet<int> quarters =
            new HashSet<int>();

        foreach (SquadController unit
            in squads)
        {
            if (!SecondaryEligibleUnit(
                    unit,
                    faction))
            {
                continue;
            }

            List<ModelToken> models =
                unit.JoinedLivingModelTokens();

            if (models.Count == 0)
                continue;

            bool outsideCentre =
                models.All(
                    model =>
                        Vector2.Distance(
                            new Vector2(
                                model.transform.position.x,
                                model.transform.position.z
                            ),
                            Vector2.zero
                        ) -
                        model.BaseRadiusInches >
                        6f
                );

            if (!outsideCentre)
                continue;

            bool positiveX =
                models.All(
                    model =>
                        model.transform.position.x -
                        model.BaseRadiusInches >
                        0f
                );

            bool negativeX =
                models.All(
                    model =>
                        model.transform.position.x +
                        model.BaseRadiusInches <
                        0f
                );

            bool positiveZ =
                models.All(
                    model =>
                        model.transform.position.z -
                        model.BaseRadiusInches >
                        0f
                );

            bool negativeZ =
                models.All(
                    model =>
                        model.transform.position.z +
                        model.BaseRadiusInches <
                        0f
                );

            int quarter = -1;

            if (positiveX && positiveZ)
                quarter = 0;
            else if (positiveX && negativeZ)
                quarter = 1;
            else if (negativeX && positiveZ)
                quarter = 2;
            else if (negativeX && negativeZ)
                quarter = 3;

            if (quarter >= 0)
                quarters.Add(quarter);
        }

        return quarters.Count;
    }

    private bool EligibleUnitWithinCentre(
        string faction,
        float distance)
    {
        return squads.Any(
            unit =>
                SecondaryEligibleUnit(
                    unit,
                    faction
                ) &&
                unit.JoinedLivingModelTokens()
                    .Any(
                        model =>
                            Vector2.Distance(
                                new Vector2(
                                    model.transform.position.x,
                                    model.transform.position.z
                                ),
                                Vector2.zero
                            ) -
                            model.BaseRadiusInches <=
                            distance
                    )
        );
    }

    private int EnemyUnitsWithinCentre(
        string faction,
        float distance)
    {
        string opponent =
            OpponentFaction(
                faction
            );

        if (string.IsNullOrWhiteSpace(
                opponent))
        {
            return 0;
        }

        return squads.Count(
            unit =>
                unit != null &&
                !unit.IsAttachedLeader &&
                unit.IsAlive &&
                unit.IsOnBattlefield &&
                unit.FactionId == opponent &&
                unit.JoinedLivingModelTokens()
                    .Any(
                        model =>
                            Vector2.Distance(
                                new Vector2(
                                    model.transform.position.x,
                                    model.transform.position.z
                                ),
                                Vector2.zero
                            ) -
                            model.BaseRadiusInches <=
                            distance
                    )
        );
    }

    private int ControlledNoMansLandCount(
        string faction)
    {
        return objectives.Count(
            objective =>
                objective != null &&
                objective.MissionRole !=
                    MissionObjectiveRole.PlayerOneHome &&
                objective.MissionRole !=
                    MissionObjectiveRole.PlayerTwoHome &&
                objective.Controller(
                    squads
                ) ==
                    faction
        );
    }

    private int DestroyedUnitsOfFactionThisTurn(
        string faction)
    {
        int value = 0;

        destroyedVictimsCurrentTurn
            .TryGetValue(
                faction,
                out value
            );

        return value;
    }

    private int DestroyedCharacterModelsOfFactionThisTurn(
        string faction)
    {
        int value = 0;

        characterModelsDestroyedThisTurn
            .TryGetValue(
                faction,
                out value
            );

        return value;
    }

    private int DestroyedCharacterW4ModelsOfFactionThisTurn(
        string faction)
    {
        int value = 0;

        characterW4ModelsDestroyedThisTurn
            .TryGetValue(
                faction,
                out value
            );

        return value;
    }

    private int DestroyedLargeModelsOfFactionThisTurn(
        string faction)
    {
        int value = 0;

        largeModelsDestroyedThisTurn
            .TryGetValue(
                faction,
                out value
            );

        return value;
    }

    private bool AllEnemyCharactersDestroyed(
        string faction)
    {
        string opponent =
            OpponentFaction(
                faction
            );

        if (string.IsNullOrWhiteSpace(
                opponent))
        {
            return false;
        }

        int starting = 0;

        startingCharacterModels
            .TryGetValue(
                opponent,
                out starting
            );

        if (starting <= 0)
            return false;

        int alive =
            squads
                .Where(
                    unit =>
                        unit != null &&
                        unit.FactionId ==
                            opponent &&
                        unit.HasIntrinsicKeyword(
                            "CHARACTER"
                        )
                )
                .Sum(
                    unit =>
                        unit.AllLivingModelTokens()
                            .Count(
                                model =>
                                    model != null &&
                                    model.IsAlive
                            )
                );

        return alive == 0;
    }

    private int UnitsWhollyWithinOpponentDeployment(
        string faction)
    {
        return squads.Count(
            unit =>
                SecondaryEligibleUnit(
                    unit,
                    faction
                ) &&
                game
                    .MissionUnitWhollyWithinOpponentDeploymentZone(
                        unit,
                        faction
                    )
        );
    }

    private int EvaluateVerifiedDefenderSecondary(
        string faction,
        string card,
        bool fixedMode,
        string turnFaction)
    {
        if (!IsDefenderFaction(
                faction) ||
            string.IsNullOrWhiteSpace(
                card))
        {
            return 0;
        }

        string opponent =
            OpponentFaction(
                faction
            );

        if (string.IsNullOrWhiteSpace(
                opponent))
        {
            return 0;
        }

        bool ownTurn =
            turnFaction ==
            faction;

        switch (card)
        {
            case "Behind Enemy Lines":
                if (!ownTurn || fixedMode)
                    return 0;

                return Mathf.Min(
                    5,
                    UnitsWhollyWithinOpponentDeployment(
                        faction
                    ) * 3
                );

            case "Secure No Man's Land":
                if (!ownTurn || fixedMode)
                    return 0;

                return
                    ControlledNoMansLandCount(
                        faction
                    ) >= 2
                    ? 5
                    : 0;

            case "Engage on All Fronts":
                if (!ownTurn)
                    return 0;

                int quarters =
                    SecondaryQuarterPresence(
                        faction
                    );

                if (quarters >= 4)
                    return fixedMode
                        ? 4
                        : 5;

                if (quarters >= 3)
                    return fixedMode
                        ? 2
                        : 3;

                return 0;

            case "Centre Ground":
                if (!ownTurn ||
                    fixedMode ||
                    !EligibleUnitWithinCentre(
                        faction,
                        3f
                    ))
                {
                    return 0;
                }

                if (EnemyUnitsWithinCentre(
                        faction,
                        6f) == 0)
                {
                    return 5;
                }

                return
                    EnemyUnitsWithinCentre(
                        faction,
                        3f) == 0
                    ? 3
                    : 0;

            case "No Prisoners":
                if (fixedMode)
                    return 0;

                return Mathf.Min(
                    5,
                    DestroyedUnitsOfFactionThisTurn(
                        opponent
                    ) * 2
                );

            case "Assassination":
                int characters =
                    DestroyedCharacterModelsOfFactionThisTurn(
                        opponent
                    );

                if (fixedMode)
                {
                    int toughCharacters =
                        DestroyedCharacterW4ModelsOfFactionThisTurn(
                            opponent
                        );

                    return
                        characters * 3 +
                        toughCharacters;
                }

                return
                    characters > 0 ||
                    AllEnemyCharactersDestroyed(
                        faction
                    )
                    ? 5
                    : 0;

            case "Bring it Down":
                int largeModels =
                    DestroyedLargeModelsOfFactionThisTurn(
                        opponent
                    );

                if (fixedMode)
                    return largeModels * 4;

                return largeModels > 0
                    ? 5
                    : 0;
        }

        return 0;
    }

    public string ResolveAutomaticSecondaryScoring(
        string turnFaction,
        int round)
    {
        List<string> scored =
            new List<string>();

        foreach (MissionPlayerState state
            in playerStates.Values.ToList())
        {
            if (state == null ||
                state.SecondaryMode ==
                    MissionSecondaryMode.Manual)
            {
                continue;
            }

            if (state.SecondaryMode ==
                MissionSecondaryMode.Fixed)
            {
                for (int i = 0;
                     i < state.FixedSecondaries.Count;
                     i++)
                {
                    string card =
                        state.FixedSecondaries[i];

                    int requested =
                        EvaluateVerifiedDefenderSecondary(
                            state.FactionId,
                            card,
                            true,
                            turnFaction
                        );

                    if (requested <= 0)
                        continue;

                    int awarded =
                        game.AddSecondaryScoreCapped(
                            state.FactionId,
                            requested,
                            round,
                            card
                        );

                    if (awarded > 0)
                    {
                        scored.Add(
                            state.FactionId +
                            " " +
                            card +
                            " +" +
                            awarded +
                            "VP"
                        );
                    }
                }

                continue;
            }

            for (int i =
                     state.SecondaryHand.Count - 1;
                 i >= 0;
                 i--)
            {
                string card =
                    state.SecondaryHand[i];

                int requested =
                    EvaluateVerifiedDefenderSecondary(
                        state.FactionId,
                        card,
                        false,
                        turnFaction
                    );

                if (requested <= 0)
                    continue;

                int awarded =
                    game.AddSecondaryScoreCapped(
                        state.FactionId,
                        requested,
                        round,
                        null
                    );

                if (awarded > 0)
                {
                    state.SecondaryHand.RemoveAt(
                        i
                    );

                    scored.Add(
                        state.FactionId +
                        " " +
                        card +
                        " +" +
                        awarded +
                        "VP"
                    );
                }
            }
        }

        return scored.Count > 0
            ? "AUTO SECONDARIES: " +
              string.Join(
                  " | ",
                  scored.ToArray()
              )
            : "";
    }

    public string VerifiedSecondaryAutomationSummary(
        string faction)
    {
        if (!IsDefenderFaction(
                faction))
        {
            return
                "Attacker secondary cards remain manual until their card-side data is verified.";
        }

        return
            "Auto Defender: Behind Enemy Lines, Secure No Man's Land, Engage, Centre Ground, No Prisoners, Assassination, Bring it Down.";
    }

    public string ResolveEndBattleScoring(
        string faction)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null)
            return "";

        int gained = 0;

        switch (state.PrimaryMission)
        {
            case "Inescapable Dominion":
                if (ControlsOpponentHome(
                        faction))
                {
                    gained += 5;
                }
                break;

            case "Consecrate":
                if (state.PersistentBonusMarks >
                    0)
                {
                    gained += 5;
                }
                break;

            case "Punishment":
                if (ControlsOpponentHome(
                        faction))
                {
                    gained += 8;
                }
                break;

            case "Unstoppable Force":
                if (ControlledCentralCount(
                        faction) >= 1)
                {
                    gained += 5;
                }
                break;

            case "Gather Intel":
                if (state.OperationMarkers >= 3)
                    gained += 5;

                if (state.PersistentBonusMarks > 0)
                    gained += 5;
                break;

            case "Search and Scour":
                if (NoEnemyWhollyInOwnTerritory(
                        faction))
                {
                    gained += 5;
                }
                break;

            case "Triangulation":
                if (ControlledCount(
                        faction) >= 4)
                {
                    gained += 10;
                }
                break;

            case "Extract Relic":
            case "Locate and Deny":
                if (state.PersistentBonusMarks > 0)
                    gained += 5;
                break;

            case "Vanguard Operation":
            case "Vital Link":
                if (ControlsOpponentHome(
                        faction))
                {
                    gained += 10;
                }
                break;

            case "Smoke and Mirrors":
                if (state.PersistentMarks >= 4)
                    gained += 10;
                break;
        }

        gained =
            game.AddPrimaryScoreCapped(
                faction,
                gained,
                5
            );

        return gained > 0
            ? state.PrimaryMission +
              " end-of-battle: +" +
              gained +
              " VP."
            : "";
    }

    private int OpponentOperationMarkerCount(
        string faction)
    {
        MissionPlayerState opponent =
            playerStates
                .Values
                .FirstOrDefault(
                    state =>
                        state.FactionId !=
                            faction
                );

        return opponent != null
            ? opponent.OperationMarkers
            : 0;
    }

    public void IncrementPersistentMarks(
        string faction,
        int delta)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null)
            return;

        state.PersistentMarks =
            Mathf.Max(
                0,
                state.PersistentMarks +
                delta
            );
    }

    public void IncrementBonusMarks(
        string faction,
        int delta)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null)
            return;

        state.PersistentBonusMarks =
            Mathf.Max(
                0,
                state.PersistentBonusMarks +
                delta
            );
    }

    public void IncrementOperationMarkers(
        string faction,
        int delta)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null)
            return;

        state.OperationMarkers =
            Mathf.Max(
                0,
                state.OperationMarkers +
                delta
            );
    }

    public void IncrementTurnActions(
        string faction,
        int delta)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null)
            return;

        state.TurnActionCompletions =
            Mathf.Max(
                0,
                state.TurnActionCompletions +
                delta
            );
    }

    public void IncrementTurnBonus(
        string faction,
        int delta)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null)
            return;

        state.TurnBonusCompletions =
            Mathf.Max(
                0,
                state.TurnBonusCompletions +
                delta
            );
    }

    public void IncrementTurnSpecialEvents(
        string faction,
        int delta)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null)
            return;

        state.TurnSpecialEvents =
            Mathf.Max(
                0,
                state.TurnSpecialEvents +
                delta
            );
    }

    private void ResetSecondaryDeck(
        MissionPlayerState state)
    {
        state.SecondaryDeck.Clear();
        state.SecondaryHand.Clear();

        state.SecondaryDeck.AddRange(
            MissionRegistry
                .SecondaryMissions
        );

        for (int i =
                 state.SecondaryDeck.Count - 1;
             i > 0;
             i--)
        {
            int j =
                Random.Range(
                    0,
                    i + 1
                );

            string temp =
                state.SecondaryDeck[i];

            state.SecondaryDeck[i] =
                state.SecondaryDeck[j];

            state.SecondaryDeck[j] =
                temp;
        }
    }

    public string DrawSecondary(
        string faction)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null)
            return "";

        if (state.SecondaryMode ==
            MissionSecondaryMode.Fixed)
        {
            return
                "Fixed secondary mode does not draw cards.";
        }

        if (state.SecondaryHand.Count >= 2)
        {
            return
                "Secondary hand already contains two cards.";
        }

        if (state.SecondaryDeck.Count == 0)
        {
            ResetSecondaryDeck(
                state
            );
        }

        if (state.SecondaryDeck.Count == 0)
            return "Secondary deck is empty.";

        string card =
            state.SecondaryDeck[0];

        state.SecondaryDeck.RemoveAt(0);

        state.SecondaryHand.Add(
            card
        );

        return
            "Drew secondary: " +
            card +
            ".";
    }

    public string DiscardSecondary(
        string faction,
        int handIndex)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null ||
            handIndex < 0 ||
            handIndex >=
                state.SecondaryHand.Count)
        {
            return "";
        }

        string card =
            state.SecondaryHand[
                handIndex];

        state.SecondaryHand.RemoveAt(
            handIndex
        );

        return
            "Discarded secondary: " +
            card +
            ".";
    }

    public int ScoreSecondary(
        string faction,
        int handIndex,
        int requestedVp,
        int round)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null ||
            requestedVp <= 0)
        {
            return 0;
        }

        string card = "";

        if (state.SecondaryMode ==
            MissionSecondaryMode.Fixed)
        {
            if (handIndex < 0 ||
                handIndex >=
                    state.FixedSecondaries.Count)
            {
                return 0;
            }

            card =
                state.FixedSecondaries[
                    handIndex];
        }
        else
        {
            if (handIndex < 0 ||
                handIndex >=
                    state.SecondaryHand.Count)
            {
                return 0;
            }

            card =
                state.SecondaryHand[
                    handIndex];
        }

        int awarded =
            game.AddSecondaryScoreCapped(
                faction,
                requestedVp,
                round,
                state.SecondaryMode ==
                    MissionSecondaryMode.Fixed
                    ? card
                    : null
            );

        if (awarded > 0 &&
            state.SecondaryMode !=
                MissionSecondaryMode.Fixed)
        {
            state.SecondaryHand.RemoveAt(
                handIndex
            );
        }

        return awarded;
    }

    public void CycleFixedSecondary(
        string faction,
        int slot)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null ||
            state.SecondaryMode !=
                MissionSecondaryMode.Fixed)
        {
            return;
        }

        string[] fixedCards =
            MissionRegistry
                .FixedEligibleSecondaries;

        if (fixedCards.Length == 0)
            return;

        if (state.FixedSecondaries.Count < 2)
        {
            state.FixedSecondaries.Clear();
            state.FixedSecondaries.Add(
                fixedCards[0]
            );
            state.FixedSecondaries.Add(
                fixedCards[
                    Mathf.Min(
                        1,
                        fixedCards.Length - 1
                    )
                ]
            );
        }

        int current =
            System.Array.IndexOf(
                fixedCards,
                state.FixedSecondaries[
                    slot]
            );

        current =
            (current + 1) %
            fixedCards.Length;

        state.FixedSecondaries[
            slot] =
            fixedCards[current];

        if (state.FixedSecondaries[0] ==
            state.FixedSecondaries[1])
        {
            current =
                (current + 1) %
                fixedCards.Length;

            state.FixedSecondaries[
                slot] =
                fixedCards[current];
        }
    }

    public string MissionStateSummary(
        string faction)
    {
        MissionPlayerState state =
            State(faction);

        if (state == null)
            return "No mission configured.";

        return
            MissionRegistry.Display(
                state.Disposition
            ) +
            " • " +
            state.PrimaryMission +
            " • " +
            state.SecondaryMode.ToString();
    }
}
