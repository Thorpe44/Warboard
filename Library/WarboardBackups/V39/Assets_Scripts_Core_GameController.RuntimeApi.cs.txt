using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Stable surface used by faction controllers and other subsystems.
// v36 deliberately keeps faction code out of GameController's private state
// and gives the real core systems direct event publication helpers.
public partial class GameController : MonoBehaviour
{
    public static GameController Current
    {
        get;
        private set;
    }

    public string ActiveFactionId
    {
        get { return activeFaction; }
    }

    public int BattleRound
    {
        get { return round; }
    }

    public string BattleSizeName
    {
        get { return battleSizeName; }
    }

    public int BattlePoints
    {
        get { return battlePoints; }
    }

    public bool DeploymentStarted
    {
        get
        {
            return
                deploymentMode ||
                round > 0;
        }
    }

    public bool PreGameReady
    {
        get
        {
            return
                (playerOneLoaded &&
                 playerTwoLoaded) ||
                deploymentMode ||
                missionSetupMode;
        }
    }

    public bool PlayerOneRosterLoaded
    {
        get { return playerOneLoaded; }
    }

    public bool PlayerTwoRosterLoaded
    {
        get { return playerTwoLoaded; }
    }

    public IReadOnlyList<string> FactionIds
    {
        get { return factions; }
    }

    public IReadOnlyList<SquadController> AllSquads
    {
        get { return squads; }
    }

    public AeldariRulesSystem AeldariRules
    {
        get { return aeldariRules; }
    }

    public FactionControllerHost FactionControllers
    {
        get { return FactionControllerHost.Instance; }
    }

    public event Action RostersChanged;

    private void Awake()
    {
        BindAsCurrent();
    }

    private readonly HashSet<SquadController>
        coreMoveSelectionsThisPhase =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        coreMoveStartsThisPhase =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        coreMoveEndsThisPhase =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        coreAdvanceEventsThisPhase =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        coreFallBackEventsThisPhase =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        coreSetUpEventsThisTurn =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        coreFinishedShootingThisTurn =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        coreChargeDeclarationsThisPhase =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        coreFightSelectionsThisPhase =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        coreFinishedFightsThisPhase =
            new HashSet<SquadController>();

    public string GetRosterCode(
        string factionId)
    {
        if (string.IsNullOrWhiteSpace(
                factionId))
        {
            return "";
        }

        int index =
            factions.FindIndex(
                faction =>
                    string.Equals(
                        faction,
                        factionId,
                        StringComparison.OrdinalIgnoreCase));

        if (index == 0)
            return yellowCodePlayerOne ?? "";

        if (index == 1)
            return yellowCodePlayerTwo ?? "";

        return "";
    }

    public IReadOnlyList<SquadController> GetArmy(
        string factionId)
    {
        if (string.IsNullOrWhiteSpace(
                factionId))
        {
            return new List<SquadController>();
        }

        return squads
            .Where(
                unit =>
                    unit != null &&
                    string.Equals(
                        unit.FactionId,
                        factionId,
                        StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    internal void BindAsCurrent()
    {
        Current = this;
    }

    internal void UnbindAsCurrent()
    {
        if (Current == this)
            Current = null;
    }

    internal void NotifyRostersChanged()
    {
        Action changed =
            RostersChanged;

        if (changed != null)
            changed();
    }

    internal bool
        EnsureFactionControllersReadyForDeployment()
    {
        FactionControllerHost host =
            FactionControllers;

        if (host == null)
            return true;

        string reason;

        if (host.CanBeginDeployment(
                out reason))
        {
            return true;
        }

        status =
            string.IsNullOrWhiteSpace(
                reason)
            ? "Faction pre-game setup is incomplete."
            : reason;

        return false;
    }

    internal void RaiseCoreEvent(
        GameEventType type,
        SquadController source = null,
        SquadController target = null,
        int amount = 0,
        string note = "",
        Phase? phaseOverride = null,
        string actingFactionOverride = null)
    {
        SquadController actionSource =
            source != null
            ? source.JoinedActionController()
            : null;

        SquadController actionTarget =
            target != null
            ? target.JoinedActionController()
            : null;

        string actingFaction =
            !string.IsNullOrWhiteSpace(
                actingFactionOverride)
            ? actingFactionOverride
            : actionSource != null
                ? actionSource.FactionId
                : activeFaction;

        GameEventBus.Raise(
            new GameEventContext
            {
                Type = type,
                Game = this,
                ActingFaction =
                    actingFaction ?? "",
                Phase =
                    phaseOverride ?? phase,
                Source = actionSource,
                Target = actionTarget,
                Amount = amount,
                Note = note ?? ""
            });
    }

    internal void NotifyBattleStarted()
    {
        RaiseCoreEvent(
            GameEventType.BattleStarted,
            note: "Battle started.");
    }

    internal void NotifyBattleRoundStarted()
    {
        RaiseCoreEvent(
            GameEventType.BattleRoundStarted,
            amount: round,
            note:
                "Battle round " +
                round +
                " started.");
    }

    internal void NotifyBattleRoundEnded()
    {
        RaiseCoreEvent(
            GameEventType.BattleRoundEnded,
            amount: round,
            note:
                "Battle round " +
                round +
                " ended.");
    }

    internal void NotifyTurnEnded()
    {
        string endingFaction =
            activeFaction;

        RaiseCoreEvent(
            GameEventType.TurnEnded,
            note:
                endingFaction +
                " turn ended.",
            actingFactionOverride:
                endingFaction);

        coreSetUpEventsThisTurn.Clear();
        coreFinishedShootingThisTurn.Clear();
    }

    internal void NotifyPhaseEnded(
        Phase endingPhase)
    {
        RaiseCoreEvent(
            GameEventType.PhaseEnded,
            note:
                endingPhase +
                " phase ended.",
            phaseOverride:
                endingPhase);

        coreMoveSelectionsThisPhase.Clear();
        coreMoveStartsThisPhase.Clear();
        coreMoveEndsThisPhase.Clear();
        coreAdvanceEventsThisPhase.Clear();
        coreFallBackEventsThisPhase.Clear();
        coreChargeDeclarationsThisPhase.Clear();
        coreFightSelectionsThisPhase.Clear();
        coreFinishedFightsThisPhase.Clear();
    }

    internal void NotifyUnitSelectedToMove(
        SquadController unit)
    {
        SquadController action =
            NormalizeActionUnit(unit);

        if (action == null ||
            phase != Phase.Move ||
            action.HasMoved ||
            !action.IsAlive ||
            !action.IsOnBattlefield)
        {
            return;
        }

        if (!coreMoveSelectionsThisPhase.Add(
                action))
        {
            return;
        }

        RaiseCoreEvent(
            GameEventType.UnitSelectedToMove,
            action,
            note:
                action.DisplayName +
                " selected to move.");
    }

    internal void NotifyMoveStarted(
        SquadController unit)
    {
        SquadController action =
            NormalizeActionUnit(unit);

        if (action == null ||
            !coreMoveStartsThisPhase.Add(
                action))
        {
            return;
        }

        RaiseCoreEvent(
            GameEventType.MoveStarted,
            action,
            note:
                action.DisplayName +
                " started a move.");
    }

    internal void NotifyMoveEnded(
        SquadController unit)
    {
        SquadController action =
            NormalizeActionUnit(unit);

        if (action == null ||
            !coreMoveEndsThisPhase.Add(
                action))
        {
            return;
        }

        RaiseCoreEvent(
            GameEventType.MoveEnded,
            action,
            note:
                action.DisplayName +
                " ended its move.");
    }

    internal void NotifyUnitAdvanced(
        SquadController unit)
    {
        SquadController action =
            NormalizeActionUnit(unit);

        if (action == null ||
            !coreAdvanceEventsThisPhase.Add(
                action))
        {
            return;
        }

        RaiseCoreEvent(
            GameEventType.UnitAdvanced,
            action,
            note:
                action.DisplayName +
                " declared an Advance.");
    }

    internal void NotifyUnitFellBack(
        SquadController unit)
    {
        SquadController action =
            NormalizeActionUnit(unit);

        if (action == null ||
            !coreFallBackEventsThisPhase.Add(
                action))
        {
            return;
        }

        RaiseCoreEvent(
            GameEventType.UnitFellBack,
            action,
            note:
                action.DisplayName +
                " Fell Back.");
    }

    internal void NotifyUnitSetUp(
        SquadController unit)
    {
        SquadController action =
            NormalizeActionUnit(unit);

        if (action == null ||
            !coreSetUpEventsThisTurn.Add(
                action))
        {
            return;
        }

        RaiseCoreEvent(
            GameEventType.UnitSetUp,
            action,
            note:
                action.DisplayName +
                " was set up on the battlefield.");
    }

    internal void NotifyUnitFinishedShooting(
        SquadController unit)
    {
        SquadController action =
            NormalizeActionUnit(unit);

        if (action == null ||
            !coreFinishedShootingThisTurn.Add(
                action))
        {
            return;
        }

        RaiseCoreEvent(
            GameEventType.UnitFinishedShooting,
            action,
            note:
                action.DisplayName +
                " finished shooting.");
    }

    internal void NotifyChargeDeclared(
        SquadController attacker,
        SquadController target)
    {
        SquadController action =
            NormalizeActionUnit(attacker);

        if (action == null ||
            !coreChargeDeclarationsThisPhase.Add(
                action))
        {
            return;
        }

        RaiseCoreEvent(
            GameEventType.ChargeDeclared,
            action,
            NormalizeActionUnit(target),
            note:
                action.DisplayName +
                " declared a charge.");
    }

    internal void NotifyUnitSelectedToFight(
        SquadController unit,
        SquadController target = null)
    {
        SquadController action =
            NormalizeActionUnit(unit);

        if (action == null ||
            !coreFightSelectionsThisPhase.Add(
                action))
        {
            return;
        }

        RaiseCoreEvent(
            GameEventType.UnitSelectedToFight,
            action,
            NormalizeActionUnit(target),
            note:
                action.DisplayName +
                " selected to fight.");
    }

    internal void NotifyUnitFinishedFighting(
        SquadController unit)
    {
        SquadController action =
            NormalizeActionUnit(unit);

        if (action == null ||
            !coreFinishedFightsThisPhase.Add(
                action))
        {
            return;
        }

        RaiseCoreEvent(
            GameEventType.UnitFinishedFighting,
            action,
            note:
                action.DisplayName +
                " finished fighting.");
    }

    internal void NotifyModelDestroyed(
        SquadController unit)
    {
        SquadController action =
            NormalizeActionUnit(unit);

        if (action == null)
            return;

        RaiseCoreEvent(
            GameEventType.ModelDestroyed,
            action,
            amount: 1,
            note:
                "A model was destroyed in " +
                action.DisplayName +
                ".");
    }

    private static SquadController NormalizeActionUnit(
        SquadController unit)
    {
        if (unit == null)
            return null;

        return unit.JoinedActionController();
    }
}
