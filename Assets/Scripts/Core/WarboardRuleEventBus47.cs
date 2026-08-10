using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WarboardRuleEventType47
{
    CoreEvent,
    UnitSelectedAsTarget,
    HitResolved,
    CriticalHitResolved,
    WoundResolved,
    CriticalWoundResolved,
    AttackSummary,
    UnitEnteredReserves,
    UnitSetUpFromReserves,
    UnitDestroyed,
    ModelDestroyed,
    ObjectiveControlChanged,
    MarkerPlaced,
    MarkerRemoved,
    DatasheetChoiceMade
}

public sealed class WarboardRuleEvent47
{
    public WarboardRuleEventType47 Type;
    public GameController Game;
    public string ActingFaction = "";
    public SquadController Source;
    public SquadController Target;
    public ModelToken SourceModel;
    public WeaponData Weapon;
    public AttackMode AttackMode;
    public int Roll;
    public bool Success;
    public bool Critical;
    public bool WasRerolled;
    public int Attacks;
    public int Hits;
    public int Wounds;
    public int ModelsKilled;
    public int WoundsLost;
    public GameEventContext CoreContext;
    public WarboardRuleMarker47 Marker;
    public string RuleId = "";
    public string StringValue = "";
    public int IntValue;
    public string Note = "";
}

/// <summary>
/// Rich, faction-agnostic rules event bus layered over the existing core bus.
/// It adds missing state-transition and per-roll events without expanding the
/// old GameEventType enum every time a faction needs a new reaction window.
/// </summary>
public static class WarboardRuleEventBus47
{
    public static event Action<WarboardRuleEvent47> Raised;

    public static void Raise(
        WarboardRuleEvent47 context)
    {
        if (context == null)
            return;

        Action<WarboardRuleEvent47> handler = Raised;

        if (handler != null)
            handler(context);
    }

    public static void RaiseTargetSelected(
        GameController game,
        SquadController source,
        SquadController target,
        AttackMode mode)
    {
        Raise(
            new WarboardRuleEvent47
            {
                Type = WarboardRuleEventType47.UnitSelectedAsTarget,
                Game = game,
                ActingFaction = source != null
                    ? source.FactionId
                    : "",
                Source = Normalize(source),
                Target = Normalize(target),
                AttackMode = mode
            }
        );
    }

    public static void RaiseHit(
        GameController game,
        SquadController source,
        SquadController target,
        ModelToken sourceModel,
        WeaponData weapon,
        AttackMode mode,
        int roll,
        bool success,
        bool critical,
        bool wasRerolled)
    {
        Raise(
            new WarboardRuleEvent47
            {
                Type = critical && success
                    ? WarboardRuleEventType47.CriticalHitResolved
                    : WarboardRuleEventType47.HitResolved,
                Game = game,
                ActingFaction = source != null
                    ? source.FactionId
                    : "",
                Source = Normalize(source),
                Target = Normalize(target),
                SourceModel = sourceModel,
                Weapon = weapon,
                AttackMode = mode,
                Roll = roll,
                Success = success,
                Critical = critical,
                WasRerolled = wasRerolled
            }
        );
    }

    public static void RaiseWound(
        GameController game,
        SquadController source,
        SquadController target,
        ModelToken sourceModel,
        WeaponData weapon,
        AttackMode mode,
        int roll,
        bool success,
        bool critical,
        bool wasRerolled)
    {
        Raise(
            new WarboardRuleEvent47
            {
                Type = critical && success
                    ? WarboardRuleEventType47.CriticalWoundResolved
                    : WarboardRuleEventType47.WoundResolved,
                Game = game,
                ActingFaction = source != null
                    ? source.FactionId
                    : "",
                Source = Normalize(source),
                Target = Normalize(target),
                SourceModel = sourceModel,
                Weapon = weapon,
                AttackMode = mode,
                Roll = roll,
                Success = success,
                Critical = critical,
                WasRerolled = wasRerolled
            }
        );
    }

    public static void RaiseAttackSummary(
        GameController game,
        SquadController source,
        SquadController target,
        AttackMode mode,
        int attacks,
        int hits,
        int wounds,
        int woundsLost,
        int modelsKilled,
        string note = "")
    {
        Raise(
            new WarboardRuleEvent47
            {
                Type = WarboardRuleEventType47.AttackSummary,
                Game = game,
                ActingFaction = source != null
                    ? source.FactionId
                    : "",
                Source = Normalize(source),
                Target = Normalize(target),
                AttackMode = mode,
                Attacks = attacks,
                Hits = hits,
                Wounds = wounds,
                WoundsLost = woundsLost,
                ModelsKilled = modelsKilled,
                Note = note ?? ""
            }
        );
    }

    private static SquadController Normalize(
        SquadController unit)
    {
        return unit != null
            ? unit.JoinedActionController()
            : null;
    }
}

[DefaultExecutionOrder(-31820)]
public sealed class WarboardRuleEventRuntime47 : MonoBehaviour
{
    private readonly Dictionary<
        SquadController,
        SquadBattlefieldState
    > lastStates =
        new Dictionary<
            SquadController,
            SquadBattlefieldState>();

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object.FindAnyObjectByType<
                WarboardRuleEventRuntime47>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "Warboard Rule Events v47"
            );

        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<WarboardRuleEventRuntime47>();
    }

    private void Awake()
    {
        GameEventBus.Raised += HandleCoreEvent;
    }

    private void OnDestroy()
    {
        GameEventBus.Raised -= HandleCoreEvent;
    }

    private void HandleCoreEvent(
        GameEventContext context)
    {
        if (context == null)
            return;

        WarboardRuleEventBus47.Raise(
            new WarboardRuleEvent47
            {
                Type = WarboardRuleEventType47.CoreEvent,
                Game = context.Game,
                ActingFaction = context.ActingFaction ?? "",
                Source = context.Source != null
                    ? context.Source.JoinedActionController()
                    : null,
                Target = context.Target != null
                    ? context.Target.JoinedActionController()
                    : null,
                AttackMode = context.AttackMode,
                Roll = context.RollTotal,
                WasRerolled = context.IsReroll,
                CoreContext = context,
                Note = context.Note ?? ""
            }
        );

        WarboardRuleEventType47? mapped = null;

        switch (context.Type)
        {
            case GameEventType.ModelDestroyed:
                mapped =
                    WarboardRuleEventType47.ModelDestroyed;
                break;

            case GameEventType.UnitDestroyed:
                mapped =
                    WarboardRuleEventType47.UnitDestroyed;
                break;

            case GameEventType.ObjectiveControlChanged:
                mapped =
                    WarboardRuleEventType47.ObjectiveControlChanged;
                break;
        }

        if (mapped.HasValue)
        {
            WarboardRuleEventBus47.Raise(
                new WarboardRuleEvent47
                {
                    Type = mapped.Value,
                    Game = context.Game,
                    ActingFaction = context.ActingFaction ?? "",
                    Source = context.Source != null
                        ? context.Source.JoinedActionController()
                        : null,
                    Target = context.Target != null
                        ? context.Target.JoinedActionController()
                        : null,
                    CoreContext = context,
                    Note = context.Note ?? ""
                }
            );
        }
    }

    private void Update()
    {
        GameController game =
            GameController.Current;

        if (game == null)
        {
            lastStates.Clear();
            return;
        }

        HashSet<SquadController> current =
            new HashSet<SquadController>();

        foreach (SquadController raw
            in game.AllSquads)
        {
            if (raw == null ||
                raw.IsAttachedLeader)
            {
                continue;
            }

            SquadController unit =
                raw.JoinedActionController();

            current.Add(unit);

            SquadBattlefieldState previous;

            if (!lastStates.TryGetValue(
                    unit,
                    out previous))
            {
                lastStates[unit] =
                    unit.BattlefieldState;
                continue;
            }

            SquadBattlefieldState now =
                unit.BattlefieldState;

            if (previous != now)
            {
                if (previous ==
                        SquadBattlefieldState.Reserves &&
                    now ==
                        SquadBattlefieldState.Battlefield)
                {
                    WarboardRuleEventBus47.Raise(
                        new WarboardRuleEvent47
                        {
                            Type = WarboardRuleEventType47.UnitSetUpFromReserves,
                            Game = game,
                            ActingFaction = unit.FactionId,
                            Source = unit,
                            Note = unit.DisplayName +
                                " transitioned from Reserves to the battlefield."
                        }
                    );
                }

                if (previous ==
                        SquadBattlefieldState.Battlefield &&
                    now ==
                        SquadBattlefieldState.Reserves)
                {
                    WarboardRuleEventBus47.Raise(
                        new WarboardRuleEvent47
                        {
                            Type = WarboardRuleEventType47.UnitEnteredReserves,
                            Game = game,
                            ActingFaction = unit.FactionId,
                            Source = unit,
                            Note = unit.DisplayName +
                                " entered Reserves."
                        }
                    );
                }

                lastStates[unit] = now;
            }
        }

        List<SquadController> stale =
            lastStates.Keys
                .Where(
                    unit =>
                        unit == null ||
                        !current.Contains(unit))
                .ToList();

        foreach (SquadController unit in stale)
            lastStates.Remove(unit);
    }
}
