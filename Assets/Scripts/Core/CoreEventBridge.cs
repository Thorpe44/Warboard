using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// v34 bridge between the existing core game engine and faction controllers.
///
/// The bridge observes authoritative GameController/SquadController state and
/// publishes timing events that faction and detachment controllers can react
/// to. This is a migration layer: the universal rules remain in GameController
/// while faction-specific behaviour moves outward.
/// </summary>
[DefaultExecutionOrder(-31000)]
public sealed class CoreEventBridge :
    MonoBehaviour
{
    private sealed class UnitSnapshot
    {
        public bool HasMoved;
        public bool HasAdvanced;
        public bool HasFallenBack;
        public bool HasShot;
        public bool HasFought;
        public bool WasSetUpThisTurn;
        public bool IsAlive;
        public int LivingModels;
        public Vector3 PositionSignature;
        public bool MoveStartedRaised;
    }

    private GameController game;

    private readonly Dictionary<
        SquadController,
        UnitSnapshot
    > snapshots =
        new Dictionary<
            SquadController,
            UnitSnapshot>();

    private readonly HashSet<SquadController>
        selectedToMoveThisPhase =
            new HashSet<SquadController>();

    private GameController.Phase lastPhase;
    private int lastRound;
    private string lastActiveFaction = "";

    private SquadController lastSelectedMoveUnit;
    private SquadController lastPendingChargeAttacker;
    private SquadController lastFightActivationUnit;

    private bool initialized;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindAnyObjectByType<
                CoreEventBridge>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "WarboardCoreEventBridge");

        Object.DontDestroyOnLoad(go);

        go.AddComponent<
            CoreEventBridge>();
    }

    private void Update()
    {
        if (game == null)
        {
            game =
                Object.FindAnyObjectByType<
                    GameController>();

            if (game == null)
                return;
        }

        if (!initialized)
        {
            InitializeSnapshots();
            return;
        }

        PublishRoundTransitions();
        PublishPhaseTransitions();
        PublishTurnTransitions();
        PublishSelectionTransitions();
        PublishUnitTransitions();
        RemoveStaleSnapshots();
    }

    private void InitializeSnapshots()
    {
        lastPhase =
            game.CurrentPhase;

        lastRound =
            game.CurrentRoundNumber;

        lastActiveFaction =
            ReadPrivateString(
                "activeFaction");

        foreach (SquadController unit
            in CurrentActionUnits())
        {
            snapshots[unit] =
                Capture(unit);
        }

        initialized = true;
    }

    private void PublishRoundTransitions()
    {
        int current =
            game.CurrentRoundNumber;

        if (current == lastRound)
            return;

        if (lastRound > 0)
        {
            Raise(
                GameEventType.BattleRoundEnded,
                null,
                null,
                lastActiveFaction,
                "Battle round " +
                lastRound +
                " ended.",
                lastRound);
        }

        lastRound = current;

        if (current > 0)
        {
            Raise(
                GameEventType.BattleRoundStarted,
                null,
                null,
                ReadPrivateString(
                    "activeFaction"),
                "Battle round " +
                current +
                " started.",
                current);
        }
    }

    private void PublishPhaseTransitions()
    {
        GameController.Phase current =
            game.CurrentPhase;

        if (current == lastPhase)
            return;

        Raise(
            GameEventType.PhaseEnded,
            null,
            null,
            ReadPrivateString(
                "activeFaction"),
            lastPhase.ToString() +
            " phase ended.",
            0,
            lastPhase);

        selectedToMoveThisPhase.Clear();
        lastSelectedMoveUnit = null;

        foreach (UnitSnapshot snapshot
            in snapshots.Values)
        {
            snapshot.MoveStartedRaised =
                false;
        }

        lastPhase = current;

        // GameController already raises PhaseStarted in the existing event
        // system. The bridge only supplies the missing end timing here so
        // faction controllers do not receive a duplicate start event.
    }

    private void PublishTurnTransitions()
    {
        string current =
            ReadPrivateString(
                "activeFaction");

        if (string.Equals(
                current,
                lastActiveFaction,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(
                lastActiveFaction))
        {
            Raise(
                GameEventType.TurnEnded,
                null,
                null,
                lastActiveFaction,
                lastActiveFaction +
                " turn ended.");
        }

        lastActiveFaction =
            current;

        // Existing GameController already raises TurnStarted.
    }

    private void PublishSelectionTransitions()
    {
        PublishMoveSelection();
        PublishChargeDeclaration();
        PublishFightSelection();
    }

    private void PublishMoveSelection()
    {
        if (game.CurrentPhase !=
            GameController.Phase.Move)
        {
            lastSelectedMoveUnit = null;
            return;
        }

        SquadController selected =
            ReadPrivate<SquadController>(
                "selectedSquad");

        if (selected != null)
        {
            selected =
                selected
                    .JoinedActionController();
        }

        if (selected == null ||
            selected.IsAttachedLeader ||
            selected.HasMoved)
        {
            lastSelectedMoveUnit =
                selected;
            return;
        }

        if (selected !=
                lastSelectedMoveUnit &&
            !selectedToMoveThisPhase.Contains(
                selected))
        {
            selectedToMoveThisPhase.Add(
                selected);

            Raise(
                GameEventType.UnitSelectedToMove,
                selected,
                null,
                selected.FactionId,
                selected.DisplayName +
                " selected to move.");
        }

        lastSelectedMoveUnit = selected;
    }

    private void PublishChargeDeclaration()
    {
        SquadController attacker =
            ReadPrivate<SquadController>(
                "pendingChargeAttacker");

        if (attacker != null)
        {
            attacker =
                attacker
                    .JoinedActionController();
        }

        if (attacker != null &&
            attacker !=
                lastPendingChargeAttacker)
        {
            SquadController target =
                ReadPrivate<SquadController>(
                    "pendingChargeTarget");

            if (target != null)
            {
                target =
                    target
                        .JoinedActionController();
            }

            Raise(
                GameEventType.ChargeDeclared,
                attacker,
                target,
                attacker.FactionId,
                attacker.DisplayName +
                " declared a charge.");
        }

        lastPendingChargeAttacker =
            attacker;
    }

    private void PublishFightSelection()
    {
        SquadController unit =
            ReadPrivate<SquadController>(
                "fightActivationUnit");

        if (unit != null)
        {
            unit =
                unit
                    .JoinedActionController();
        }

        if (unit != null &&
            unit !=
                lastFightActivationUnit)
        {
            Raise(
                GameEventType.UnitSelectedToFight,
                unit,
                null,
                unit.FactionId,
                unit.DisplayName +
                " selected to fight.");
        }

        lastFightActivationUnit =
            unit;
    }

    private void PublishUnitTransitions()
    {
        foreach (SquadController unit
            in CurrentActionUnits())
        {
            UnitSnapshot previous;

            if (!snapshots.TryGetValue(
                    unit,
                    out previous))
            {
                snapshots[unit] =
                    Capture(unit);
                continue;
            }

            Vector3 signature =
                PositionSignature(unit);

            if (!previous.MoveStartedRaised &&
                !unit.HasMoved &&
                HorizontalDelta(
                    previous.PositionSignature,
                    signature) >
                    0.003f)
            {
                previous.MoveStartedRaised =
                    true;

                Raise(
                    GameEventType.MoveStarted,
                    unit,
                    null,
                    unit.FactionId,
                    unit.DisplayName +
                    " started a move.");
            }

            if (!previous.HasAdvanced &&
                unit.HasAdvanced)
            {
                Raise(
                    GameEventType.UnitAdvanced,
                    unit,
                    null,
                    unit.FactionId,
                    unit.DisplayName +
                    " selected an Advance move.");
            }

            if (!previous.HasFallenBack &&
                unit.HasFallenBack)
            {
                Raise(
                    GameEventType.UnitFellBack,
                    unit,
                    null,
                    unit.FactionId,
                    unit.DisplayName +
                    " Fell Back.");
            }

            if (!previous.WasSetUpThisTurn &&
                unit.WasSetUpThisTurn)
            {
                Raise(
                    GameEventType.UnitSetUp,
                    unit,
                    null,
                    unit.FactionId,
                    unit.DisplayName +
                    " was set up on the battlefield.");
            }

            if (!previous.HasMoved &&
                unit.HasMoved)
            {
                Raise(
                    GameEventType.MoveEnded,
                    unit,
                    null,
                    unit.FactionId,
                    unit.DisplayName +
                    " ended its move.");

                previous.MoveStartedRaised =
                    false;
            }

            if (!previous.HasShot &&
                unit.HasShot)
            {
                Raise(
                    GameEventType.UnitFinishedShooting,
                    unit,
                    null,
                    unit.FactionId,
                    unit.DisplayName +
                    " finished shooting.");
            }

            if (!previous.HasFought &&
                unit.HasFought)
            {
                Raise(
                    GameEventType.UnitFinishedFighting,
                    unit,
                    null,
                    unit.FactionId,
                    unit.DisplayName +
                    " finished fighting.");
            }

            int living =
                AllLivingModels(
                    unit);

            if (living <
                previous.LivingModels)
            {
                Raise(
                    GameEventType.ModelDestroyed,
                    unit,
                    null,
                    unit.FactionId,
                    (previous.LivingModels -
                     living) +
                    " model(s) destroyed in " +
                    unit.DisplayName +
                    ".",
                    previous.LivingModels -
                    living);
            }

            CopyCurrentState(
                previous,
                unit,
                signature);
        }
    }

    private IEnumerable<SquadController>
        CurrentActionUnits()
    {
        return Object
            .FindObjectsByType<
                SquadController>(
                FindObjectsSortMode.None)
            .Where(
                unit =>
                    unit != null &&
                    !unit.IsAttachedLeader);
    }

    private void RemoveStaleSnapshots()
    {
        HashSet<SquadController> current =
            new HashSet<SquadController>(
                CurrentActionUnits());

        List<SquadController> stale =
            snapshots.Keys
                .Where(
                    unit =>
                        unit == null ||
                        !current.Contains(unit))
                .ToList();

        foreach (SquadController unit
            in stale)
        {
            snapshots.Remove(unit);
        }
    }

    private UnitSnapshot Capture(
        SquadController unit)
    {
        UnitSnapshot result =
            new UnitSnapshot();

        CopyCurrentState(
            result,
            unit,
            PositionSignature(unit));

        return result;
    }

    private void CopyCurrentState(
        UnitSnapshot snapshot,
        SquadController unit,
        Vector3 signature)
    {
        snapshot.HasMoved =
            unit.HasMoved;

        snapshot.HasAdvanced =
            unit.HasAdvanced;

        snapshot.HasFallenBack =
            unit.HasFallenBack;

        snapshot.HasShot =
            unit.HasShot;

        snapshot.HasFought =
            unit.HasFought;

        snapshot.WasSetUpThisTurn =
            unit.WasSetUpThisTurn;

        snapshot.IsAlive =
            unit.IsAlive;

        snapshot.LivingModels =
            AllLivingModels(
                unit);

        snapshot.PositionSignature =
            signature;
    }

    private static int AllLivingModels(
        SquadController unit)
    {
        if (unit == null)
            return 0;

        SquadController actionUnit =
            unit.JoinedActionController();

        int result =
            actionUnit
                .AllLivingModelTokens()
                .Count;

        if (actionUnit.AttachedLeader != null &&
            actionUnit.AttachedLeader.IsAlive)
        {
            result +=
                actionUnit
                    .AttachedLeader
                    .AllLivingModelTokens()
                    .Count;
        }

        return result;
    }

    private static Vector3 PositionSignature(
        SquadController unit)
    {
        if (unit == null)
            return Vector3.zero;

        List<ModelToken> models =
            unit.JoinedLivingModelTokens();

        Vector3 value =
            Vector3.zero;

        for (int i = 0;
             i < models.Count;
             i++)
        {
            if (models[i] == null)
                continue;

            float weight =
                i + 1f;

            Vector3 p =
                models[i]
                    .transform
                    .position;

            value.x += p.x * weight;
            value.z += p.z * weight;
        }

        return value;
    }

    private static float HorizontalDelta(
        Vector3 a,
        Vector3 b)
    {
        return Vector2.Distance(
            new Vector2(
                a.x,
                a.z),
            new Vector2(
                b.x,
                b.z));
    }

    private void Raise(
        GameEventType type,
        SquadController source,
        SquadController target,
        string faction,
        string note,
        int amount = 0,
        GameController.Phase? phaseOverride = null)
    {
        GameEventBus.Raise(
            new GameEventContext
            {
                Type = type,
                Game = game,
                ActingFaction =
                    faction ?? "",
                Phase =
                    phaseOverride ??
                    game.CurrentPhase,
                Source = source,
                Target = target,
                Amount = amount,
                Note = note ?? ""
            });
    }

    private T ReadPrivate<T>(
        string fieldName)
        where T : class
    {
        if (game == null)
            return null;

        FieldInfo field =
            typeof(GameController)
                .GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

        return field != null
            ? field.GetValue(game) as T
            : null;
    }

    private string ReadPrivateString(
        string fieldName)
    {
        if (game == null)
            return "";

        FieldInfo field =
            typeof(GameController)
                .GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

        object value =
            field != null
            ? field.GetValue(game)
            : null;

        return value != null
            ? value.ToString()
            : "";
    }
}
