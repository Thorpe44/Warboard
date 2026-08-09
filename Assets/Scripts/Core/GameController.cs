using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public partial class GameController : MonoBehaviour
{
    // WARBOARD_V35_GAMECONTROLLER_REFACTORED
    // v35: this file now owns state/lifecycle; functional areas live in GameController.*.cs partials.
    public enum Phase
    {
        Command,
        Move,
        Shoot,
        Charge,
        Fight,
        End
    }

    private enum FightActivationStage
    {
        None,
        PileIn,
        Attacks,
        Consolidate
    }

    // Battlefield dimensions are selected before army loading.
    // X is the distance between the two deployment edges; Z is the frontage.
    public static float BoardWidth { get; private set; } = 44f;
    public static float BoardDepth { get; private set; } = 60f;

    // Compatibility accessor for older helper code. New geometry should use
    // BoardWidth / BoardDepth explicitly.
    public static float BoardSize
    {
        get { return Mathf.Max(BoardWidth, BoardDepth); }
    }

    public const float EngagementRange = 2.0f;
    public const float PileInDistance = 3f;
    public const float ConsolidateDistance = 3f;
    public const float MinimumModelCentreDistance = 0.78f;
    public const float CombatPathSampleStep = 0.18f;

    public static float DeploymentZoneWidth { get; private set; } = 10.0f;
    public const float StrategicReserveEdgeDistance = 6.0f;
    public const float ReserveEnemyExclusionDistance = 8.0f;

    private const float DoubleClickWindow = 0.34f;

    private Camera gameCamera;

    private SquadController selectedSquad;
    private ModelToken selectedModel;

    private bool showDatasheet;
    private SquadController datasheetSquad;
    private ModelToken datasheetModel;
    private Vector2 datasheetScroll;

    private bool wholeSquadMoveMode;

    private float lastFriendlyClickTime = -10f;
    private SquadController lastFriendlyClickedSquad;

    private LineRenderer moveRing;

    private LineRenderer movementCursorLine;
    private bool movementCursorMeasureActive;
    private string movementCursorMeasureText = "";
    private bool movementCursorMeasureLegal = true;

    private readonly List<SquadController> squads =
        new List<SquadController>();

    private readonly List<ObjectiveController> objectives =
        new List<ObjectiveController>();

    private readonly List<string> factions =
        new List<string>();

    private readonly Dictionary<string, int> score =
        new Dictionary<string, int>();

    private readonly Dictionary<
        string,
        Dictionary<int, int>
    > primaryScoreByRound =
        new Dictionary<
            string,
            Dictionary<int, int>
        >();

    private readonly Dictionary<
        string,
        Dictionary<int, int>
    > secondaryScoreByRound =
        new Dictionary<
            string,
            Dictionary<int, int>
        >();

    private Vector2 playerOneSidePanelScroll;
    private Vector2 playerTwoSidePanelScroll;

    private readonly Dictionary<string, int> commandPoints =
        new Dictionary<string, int>();

    private readonly HashSet<string> stratagemsUsedThisPhase =
        new HashSet<string>();

    private bool showDiceTray = false;
    private TraditionalDiceTray3D traditionalDiceTray;

    private bool manualWoundEditMode;
    private bool manualRestoreEditMode;
    private int manualRestoreDeadIndex;
    private ModelToken pendingTraditionalRemovalCandidate;

    private bool traditionalFirstTurnPending;

    private bool traditionalNumericPromptPending;
    private string traditionalNumericTitle = "";
    private string traditionalNumericText = "";
    private int traditionalNumericValue;
    private int traditionalNumericMin;
    private int traditionalNumericMax;
    private System.Action<int> traditionalNumericApply;
    private System.Action traditionalNumericCancel;

    private bool traditionalAdvancePending;
    private SquadController traditionalAdvanceUnit;
    private int traditionalAdvanceResult = 1;

    private bool traditionalChargePending;
    private SquadController traditionalChargeAttacker;
    private SquadController traditionalChargeTarget;
    private int traditionalChargeResult = 2;

    private readonly Queue<SquadController>
        traditionalBattleShockQueue =
            new Queue<SquadController>();

    private SquadController traditionalBattleShockUnit;
    private string traditionalBattleShockLabel = "";
    private bool traditionalBattleShockPending;

    private bool traditionalReanimationPending;
    private readonly List<SquadController>
        traditionalReanimationUnits =
            new List<SquadController>();
    private SquadController traditionalReanimationUnit;
    private int traditionalReanimationDeadIndex;

    private bool traditionalRuleAlertPending;
    private string traditionalRuleAlertTitle = "";
    private string traditionalRuleAlertText = "";
    private int traditionalRuleAlertSuggestedDice = 1;
    private System.Action traditionalRuleAlertCompletion;

    private bool traditionalAttackPending;
    private SquadController traditionalAttackAttacker;
    private SquadController traditionalAttackTarget;
    private List<WeaponAttackSelection> traditionalAttackSelections =
        new List<WeaponAttackSelection>();
    private AttackMode traditionalAttackMode;
    private bool traditionalAttackConsumesAction;
    private bool traditionalAttackModelLevel;
    private System.Action traditionalAttackCompletionCallback;

    private bool showWarboardPanel;
    private bool showBasicCommandsPanel;
    private bool showBattleLog;

    private Vector2 battleLogScroll;
    private readonly List<WarboardBattleLogEntry>
        battleLog =
            new List<WarboardBattleLogEntry>();

    private int battleLogSequence;
    private float nextBattlefieldVisualRefreshTime;

    private readonly Dictionary<
        ObjectiveController,
        string
    > objectiveLiveControl =
        new Dictionary<
            ObjectiveController,
            string
        >();

    private BattlefieldWorldUI battlefieldWorldUI;

    private InteractiveAttackController interactiveAttack;
    private FactionRuleSystem factionRules;
    private AeldariRulesSystem aeldariRules;

    public WarboardResolutionMode ResolutionMode
    {
        get;
        private set;
    } =
        WarboardResolutionMode.TraditionalManual;

    public bool IsXcomMode
    {
        get
        {
            return ResolutionMode ==
                WarboardResolutionMode.XcomAutomatic;
        }
    }

    private bool interactiveAttackModelLevelShooting;

    private ModelToken pendingCasualtyCandidate;
    private bool pendingCasualtyCoherencyWarning;

    // Generic asynchronous rules / reaction flow.
    private bool showRuleChoiceWindow;
    private string ruleChoiceTitle = "";
    private string ruleChoiceDescription = "";
    private readonly List<RuleChoiceOption> ruleChoiceOptions =
        new List<RuleChoiceOption>();
    private Vector2 ruleChoiceScroll;

    private SquadController specialMoveSquad;
    private float specialMoveMaxDistance;
    private string specialMoveLabel = "";
    private System.Action specialMoveCompleted;
    private bool specialMoveIsSurge;
    private SquadController specialMoveSurgeTarget;
    private float specialMoveSurgeStartDistance;

    private SquadController specialShootUnit;
    private SquadController specialShootForcedTarget;
    private string specialShootLabel = "";
    private System.Action specialShootCompleted;

    private System.Action interactiveAttackCompletionCallback;
    private bool interactiveAttackConsumesNormalAction = true;
    private bool interactiveAttackSuppressesPostReactions;

    private readonly Queue<System.Action> endPhaseFlowQueue =
        new Queue<System.Action>();
    private bool endPhaseFlowRunning;

    private readonly Queue<System.Action> postAttackFlowQueue =
        new Queue<System.Action>();
    private System.Action postAttackFinalizer;

    private readonly List<DestroyedModelRecord> destroyedModelsThisPhase =
        new List<DestroyedModelRecord>();
    private readonly List<DestroyedUnitRecord> destroyedUnitsThisPhase =
        new List<DestroyedUnitRecord>();
    private readonly HashSet<SquadController> destroyedUnitsRecordedThisPhase =
        new HashSet<SquadController>();

    private readonly Dictionary<ObjectiveController, string> objectiveControlAtPhaseStart =
        new Dictionary<ObjectiveController, string>();

    private FightPriorityStep fightPriorityStep = FightPriorityStep.None;
    private string fightSelectionFaction = "";
    private bool fightSequenceActive;
    private readonly HashSet<SquadController> fightEligibleAtStart =
        new HashSet<SquadController>();

    private FightActivationStage fightActivationStage =
        FightActivationStage.None;

    private SquadController fightActivationUnit;
    private SquadController fightActivationInitialTarget;

    private readonly HashSet<ModelToken>
        fightModelsResolvedThisActivation =
            new HashSet<ModelToken>();

    private readonly HashSet<ModelToken>
        fightStageMovedModels =
            new HashSet<ModelToken>();

    private readonly Dictionary<ModelToken, Vector3>
        fightStageStartPositions =
            new Dictionary<ModelToken, Vector3>();

    private ModelToken fightPreparedAttackModel;
    private WeaponData fightPreparedMeleeWeapon;

    private bool resurrectionOrbUsedThisTurn;

    private SquadController heraldOfYnneadTarget;
    private string heraldOfYnneadFaction = "";

    private bool showStratagemMenu;
    private CommandRerollStage armedCommandRerollStage =
        CommandRerollStage.None;
    private string armedCommandRerollFaction = "";

    private bool showStratagemReaction;
    private GameEventContext pendingReactionContext;

    private SquadController pendingChargeAttacker;
    private SquadController pendingChargeTarget;
    private int pendingChargeRoll;
    private int pendingOriginalChargeRoll;

    private int activeFactionIndex;
    private int round = 0;

    private bool battleSetupMode = true;
    private string battleSizeName = "Strike Force";
    private int battlePoints = 2000;
    private string missionPresetName = "Warboard Open War";

    private string customPointsText = "1500";
    private string customWidthText = "44";
    private string customDepthText = "60";
    private string customDeploymentText = "10";

    private bool armyImportMode = false;
    private bool missionSetupMode;
    private bool showMissionPanel;
    private bool battleOver;

    private MissionSystem missionSystem;
    private MissionBattlefieldDefinition
        activeMissionBattlefield;

    private SquadController
        missionActionTargetingUnit;

    private MissionActionDefinition
        missionActionTargetingDefinition;

    private ForceDisposition missionDispositionPlayerOne =
        ForceDisposition.TakeAndHold;

    private ForceDisposition missionDispositionPlayerTwo =
        ForceDisposition.PurgeTheFoe;

    private MissionSecondaryMode missionSecondaryPlayerOne =
        MissionSecondaryMode.Tactical;

    private MissionSecondaryMode missionSecondaryPlayerTwo =
        MissionSecondaryMode.Tactical;

    private int missionLayoutIndex = 1;
    private int missionAttackerIndex = 0;

    private int firstTurnFactionIndex;
    private int turnsCompletedThisRound;
    private string firstTurnRollSummary = "";
    private string battleSummary = "";

    private readonly Dictionary<
        string,
        Dictionary<string, int>
    > fixedSecondaryScoreByCard =
        new Dictionary<
            string,
            Dictionary<string, int>
        >();

    private bool loadingYellowScribe;

    private string yellowCodePlayerOne = "4b5074c8";
    private string yellowCodePlayerTwo = "";

    private string playerOneRosterLabel = "Player 1";
    private string playerTwoRosterLabel = "Player 2";

    private bool playerOneLoaded;
    private bool playerTwoLoaded;

    private const string YellowScribeEndpoint =
        "https://yellowscribe.link/get_army_by_id?id=";

    private bool deploymentMode = true;
    private SquadController currentDeploymentSquad;
    private Vector2 deploymentRosterScroll;
    private int deploymentBodyguardChoiceIndex;
    private SquadController reservePlacementSquad;
    private int reserveCycleIndex = -1;

    private Phase phase = Phase.Command;

    public Phase CurrentPhase
    {
        get { return phase; }
    }

    public int CurrentRoundNumber
    {
        get { return round; }
    }
    private string activeFaction = "";
    private string status = "Ready.";

    private void Start()
    {
        Core11Install();

        DiceRoller.Rolled +=
            HandleLoggedDiceRoll;

        factionRules =
            new FactionRuleSystem(this);

        aeldariRules =
            new AeldariRulesSystem(this);

        battleSetupMode = true;
        armyImportMode = false;
        deploymentMode = false;
        activeFaction = "";

        status =
            "Choose a battle size and battlefield before loading armies.";
    }

    private void OnDestroy()
    {
        Core11Uninstall();

        UnbindAsCurrent();

        DiceRoller.Rolled -=
            HandleLoggedDiceRoll;
    }

    private void Update()
    {
        if (battleSetupMode ||
            missionSetupMode ||
            battleOver)
        {
            return;
        }

        if (armyImportMode)
        {
            HandleCamera();
            return;
        }

        if (Time.unscaledTime >=
            nextBattlefieldVisualRefreshTime)
        {
            nextBattlefieldVisualRefreshTime =
                Time.unscaledTime +
                0.12f;

            RefreshObjectiveDisplays();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (TraditionalStateResolutionPending())
            {
                status =
                    "Finish the current Traditional tabletop state prompt first.";
            }
            else if (interactiveAttack != null &&
                (!interactiveAttack.IsComplete ||
                 interactiveAttack
                    .HasPendingCasualtyChoices))
            {
                status =
                    interactiveAttack
                        .HasPendingCasualtyChoices
                    ? "Finish casualty selection first."
                    : "Finish the current dice sequence first.";
            }
            else if (showRuleChoiceWindow ||
                     specialMoveSquad != null ||
                     specialShootUnit != null ||
                     endPhaseFlowRunning)
            {
                status =
                    "Resolve the current rules/reaction window first.";
            }
            else if (showStratagemReaction)
            {
                status =
                    "Resolve the stratagem reaction window first.";
            }
            else if (deploymentMode)
            {
                status =
                    "Deployment is active. Place the current unit or send it to Reserves.";
            }
            else
            {
                NextPhase();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (missionActionTargetingDefinition !=
                    null)
            {
                missionActionTargetingUnit = null;
                missionActionTargetingDefinition =
                    null;

                status =
                    "Mission action targeting cancelled.";
            }
            else if (interactiveAttack != null &&
                (!interactiveAttack.IsComplete ||
                 interactiveAttack
                    .HasPendingCasualtyChoices))
            {
                status =
                    interactiveAttack
                        .HasPendingCasualtyChoices
                    ? "Casualty selection must be completed."
                    : (IsXcomMode
                        ? "Resolve the current XCOM decision."
                        : "The attack must be resolved; use the dice popup controls.");
            }
            else if (showRuleChoiceWindow ||
                     specialMoveSquad != null ||
                     specialShootUnit != null)
            {
                status =
                    "Resolve or skip the current rules window first.";
            }
            else if (showStratagemReaction)
            {
                KeepPendingChargeRoll();
            }
            else if (showStratagemMenu)
            {
                showStratagemMenu = false;
            }
            else if (showBattleLog)
            {
                showBattleLog = false;
            }
            else if (showMissionPanel)
            {
                showMissionPanel = false;
            }
            else if (showDatasheet)
            {
                CloseDatasheet();
            }
            else
            {
                ClearSelection();
            }
        }

        if (Input.GetMouseButtonDown(0) &&
            (interactiveAttack == null ||
             interactiveAttack
                .HasPendingCasualtyChoices) &&
            !showDatasheet &&
            !showRuleChoiceWindow &&
            !showStratagemReaction &&
            !showStratagemMenu)
        {
            HandleClick(Input.mousePosition);
        }

        HandleCamera();
        UpdateMovementCursorMeasure();
    }

    public int WorldUiFactionCount
    {
        get { return factions.Count; }
    }

}

public class BoardSurface : MonoBehaviour { }
