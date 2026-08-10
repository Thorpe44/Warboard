using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WarboardRuleScope47
{
    Phase,
    Turn,
    Round,
    OwnerNextTurn,
    Battle
}

/// <summary>
/// Generic persistent rule state. This replaces one-off faction fields for
/// concepts such as Oath/Prey targets, detected/scanned units, pinned units,
/// objective designations and future faction marks.
/// </summary>
public sealed class WarboardRuleState47
{
    public string Id = "";
    public string OwnerFaction = "";
    public SquadController SourceUnit;
    public SquadController TargetUnit;
    public ObjectiveController TargetObjective;
    public string StringValue = "";
    public int IntValue;
    public float FloatValue;
    public WarboardRuleScope47 Scope;
    public int CreatedRound;
    public int CreatedTurnSerial;
    public GameController.Phase CreatedPhase;
    public string Note = "";
}

public static class WarboardRuleStateStore47
{
    private static readonly List<WarboardRuleState47> states =
        new List<WarboardRuleState47>();

    public static IReadOnlyList<WarboardRuleState47> All
    {
        get { return states.ToArray(); }
    }

    public static void SetUnitTarget(
        string id,
        string ownerFaction,
        SquadController source,
        SquadController target,
        WarboardRuleScope47 scope,
        string note = "")
    {
        SquadController normalizedSource =
            Normalize(source);

        SquadController normalizedTarget =
            Normalize(target);

        if (normalizedSource != null)
        {
            // A source normally owns one current target for a named rule
            // (Oath, Prey, scanned-by-this-unit, etc.). Do not remove the
            // same rule produced by a different source unit.
            Remove(id, ownerFaction, normalizedSource, null);
        }
        else if (normalizedTarget != null)
        {
            // Flag-style state can exist on several targets at once.
            RemoveUnitTarget(
                id,
                ownerFaction,
                normalizedTarget
            );
        }
        else
        {
            Remove(id, ownerFaction, null, null);
        }

        WarboardRuleState47 value =
            NewState(id, ownerFaction, scope, note);

        value.SourceUnit = normalizedSource;
        value.TargetUnit = normalizedTarget;

        states.Add(value);
    }

    public static void SetUnitFlag(
        string id,
        string ownerFaction,
        SquadController target,
        WarboardRuleScope47 scope,
        string note = "")
    {
        SquadController normalizedTarget =
            Normalize(target);

        if (normalizedTarget == null)
            return;

        RemoveUnitTarget(
            id,
            ownerFaction,
            normalizedTarget
        );

        WarboardRuleState47 value =
            NewState(id, ownerFaction, scope, note);

        value.TargetUnit = normalizedTarget;
        states.Add(value);
    }

    public static void SetObjectiveTarget(
        string id,
        string ownerFaction,
        ObjectiveController objective,
        WarboardRuleScope47 scope,
        string note = "")
    {
        Remove(id, ownerFaction, null, objective);

        WarboardRuleState47 value =
            NewState(id, ownerFaction, scope, note);

        value.TargetObjective = objective;

        states.Add(value);
    }

    public static void SetValue(
        string id,
        string ownerFaction,
        string stringValue,
        int intValue,
        float floatValue,
        WarboardRuleScope47 scope,
        string note = "")
    {
        Remove(id, ownerFaction, null, null);

        WarboardRuleState47 value =
            NewState(id, ownerFaction, scope, note);

        value.StringValue = stringValue ?? "";
        value.IntValue = intValue;
        value.FloatValue = floatValue;

        states.Add(value);
    }

    public static void SetSourceValue(
        string id,
        string ownerFaction,
        SquadController source,
        string stringValue,
        int intValue,
        float floatValue,
        WarboardRuleScope47 scope,
        string note = "")
    {
        SquadController normalizedSource =
            Normalize(source);

        if (normalizedSource == null)
        {
            SetValue(
                id,
                ownerFaction,
                stringValue,
                intValue,
                floatValue,
                scope,
                note
            );
            return;
        }

        Remove(
            id,
            ownerFaction,
            normalizedSource,
            null
        );

        WarboardRuleState47 value =
            NewState(id, ownerFaction, scope, note);

        value.SourceUnit = normalizedSource;
        value.StringValue = stringValue ?? "";
        value.IntValue = intValue;
        value.FloatValue = floatValue;

        states.Add(value);
    }

    public static bool HasUnitTarget(
        string id,
        string ownerFaction,
        SquadController target)
    {
        SquadController wanted = Normalize(target);

        return states.Any(
            value =>
                IdEquals(value, id) &&
                FactionEquals(value, ownerFaction) &&
                Normalize(value.TargetUnit) == wanted
        );
    }

    public static bool HasUnitFlag(
        string id,
        SquadController target)
    {
        SquadController wanted = Normalize(target);

        return states.Any(
            value =>
                IdEquals(value, id) &&
                Normalize(value.TargetUnit) == wanted
        );
    }

    public static WarboardRuleState47 GetLatest(
        string id,
        string ownerFaction = "")
    {
        return states
            .Where(
                value =>
                    IdEquals(value, id) &&
                    (string.IsNullOrWhiteSpace(ownerFaction) ||
                     FactionEquals(value, ownerFaction)))
            .LastOrDefault();
    }

    public static IEnumerable<WarboardRuleState47> GetAll(
        string id,
        string ownerFaction = "")
    {
        return states
            .Where(
                value =>
                    IdEquals(value, id) &&
                    (string.IsNullOrWhiteSpace(ownerFaction) ||
                     FactionEquals(value, ownerFaction)))
            .ToArray();
    }

    public static void Remove(
        string id,
        string ownerFaction = "",
        SquadController source = null,
        ObjectiveController objective = null)
    {
        SquadController normalizedSource = Normalize(source);

        states.RemoveAll(
            value =>
                IdEquals(value, id) &&
                (string.IsNullOrWhiteSpace(ownerFaction) ||
                 FactionEquals(value, ownerFaction)) &&
                (source == null ||
                 Normalize(value.SourceUnit) == normalizedSource) &&
                (objective == null ||
                 value.TargetObjective == objective)
        );
    }

    public static void RemoveUnitTarget(
        string id,
        string ownerFaction,
        SquadController target)
    {
        SquadController wanted = Normalize(target);

        states.RemoveAll(
            value =>
                IdEquals(value, id) &&
                FactionEquals(value, ownerFaction) &&
                Normalize(value.TargetUnit) == wanted
        );
    }

    public static void ClearScope(
        WarboardRuleScope47 scope)
    {
        states.RemoveAll(
            value => value.Scope == scope
        );
    }

    public static void ClearFaction(
        string factionId)
    {
        states.RemoveAll(
            value => FactionEquals(value, factionId)
        );
    }

    public static void ClearAll()
    {
        states.Clear();
    }

    private static WarboardRuleState47 NewState(
        string id,
        string ownerFaction,
        WarboardRuleScope47 scope,
        string note)
    {
        GameController game =
            GameController.Current;

        return new WarboardRuleState47
        {
            Id = NormalizeId(id),
            OwnerFaction = ownerFaction ?? "",
            Scope = scope,
            CreatedRound =
                game != null
                ? game.BattleRound
                : 0,
            CreatedTurnSerial =
                WarboardRuleStateRuntime47.CurrentTurnSerial,
            CreatedPhase =
                game != null
                ? game.CurrentPhase
                : GameController.Phase.Command,
            Note = note ?? ""
        };
    }

    private static bool IdEquals(
        WarboardRuleState47 value,
        string id)
    {
        return value != null &&
            string.Equals(
                value.Id,
                NormalizeId(id),
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static bool FactionEquals(
        WarboardRuleState47 value,
        string faction)
    {
        return value != null &&
            string.Equals(
                value.OwnerFaction ?? "",
                faction ?? "",
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static SquadController Normalize(
        SquadController unit)
    {
        return unit != null
            ? unit.JoinedActionController()
            : null;
    }

    private static string NormalizeId(
        string value)
    {
        return WeaponRuleParser.NormalizeRuleName(
            value ?? ""
        );
    }
}

[DefaultExecutionOrder(-31840)]
public sealed class WarboardRuleStateRuntime47 : MonoBehaviour
{
    public static int CurrentTurnSerial { get; private set; }
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object.FindAnyObjectByType<
                WarboardRuleStateRuntime47>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "Warboard Rule State v47"
            );

        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<WarboardRuleStateRuntime47>();
    }

    private void Awake()
    {
        GameEventBus.Raised += HandleGameEvent;
    }

    private void OnDestroy()
    {
        GameEventBus.Raised -= HandleGameEvent;
    }

    private void HandleGameEvent(
        GameEventContext context)
    {
        if (context == null)
            return;

        switch (context.Type)
        {
            case GameEventType.BattleStarted:
                CurrentTurnSerial = 0;
                WarboardRuleStateStore47.ClearAll();
                break;

            case GameEventType.TurnStarted:
                CurrentTurnSerial++;

                foreach (WarboardRuleState47 state
                    in WarboardRuleStateStore47.All
                        .Where(value =>
                            value != null &&
                            value.Scope ==
                                WarboardRuleScope47.OwnerNextTurn &&
                            string.Equals(
                                value.OwnerFaction,
                                context.ActingFaction,
                                StringComparison.OrdinalIgnoreCase) &&
                            value.CreatedTurnSerial <
                                CurrentTurnSerial)
                        .ToArray())
                {
                    WarboardRuleStateStore47.RemoveUnitTarget(
                        state.Id,
                        state.OwnerFaction,
                        state.TargetUnit
                    );

                    if (state.TargetUnit == null)
                    {
                        WarboardRuleStateStore47.Remove(
                            state.Id,
                            state.OwnerFaction,
                            state.SourceUnit,
                            state.TargetObjective
                        );
                    }
                }
                break;

            case GameEventType.PhaseEnded:
                WarboardRuleStateStore47.ClearScope(
                    WarboardRuleScope47.Phase);
                break;

            case GameEventType.TurnEnded:
                WarboardRuleStateStore47.ClearScope(
                    WarboardRuleScope47.Turn);
                break;

            case GameEventType.BattleRoundEnded:
                WarboardRuleStateStore47.ClearScope(
                    WarboardRuleScope47.Round);
                break;
        }
    }
}
