using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Runtime contract for faction-specific game controllers.
///
/// GameController remains the owner of universal 40K rules. Faction
/// controllers listen to the core event stream and own faction/detachment
/// state that sits on top of those universal rules.
/// </summary>
public interface IFactionGameController
{
    string FactionId { get; }
    string DisplayName { get; }

    void Initialize(
        GameController game,
        string factionId);

    void RefreshArmy(
        IReadOnlyList<SquadController> army);

    void OnGameEvent(
        GameEventContext context);

    void Tick();
}

public abstract class FactionGameControllerBase :
    IFactionGameController
{
    protected GameController Game
    {
        get;
        private set;
    }

    public string FactionId
    {
        get;
        private set;
    }

    protected readonly List<SquadController> army =
        new List<SquadController>();

    public abstract string DisplayName
    {
        get;
    }

    public virtual void Initialize(
        GameController game,
        string factionId)
    {
        Game = game;
        FactionId = factionId ?? "";
    }

    public virtual void RefreshArmy(
        IReadOnlyList<SquadController> units)
    {
        army.Clear();

        if (units == null)
            return;

        foreach (SquadController unit in units)
        {
            if (unit != null)
                army.Add(unit);
        }
    }

    public virtual void OnGameEvent(
        GameEventContext context)
    {
    }

    public virtual void Tick()
    {
    }

    protected bool EventConcernsFaction(
        GameEventContext context)
    {
        if (context == null)
            return false;

        if (string.Equals(
                context.ActingFaction,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (context.Source != null &&
            string.Equals(
                context.Source.FactionId,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return
            context.Target != null &&
            string.Equals(
                context.Target.FactionId,
                FactionId,
                StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class GenericFactionGameController :
    FactionGameControllerBase
{
    public override string DisplayName
    {
        get { return "Generic Faction"; }
    }
}

/// <summary>
/// Creates only the controllers required by the armies currently loaded.
/// New factions are added here rather than by adding faction branches to
/// GameController.
/// </summary>
public static class FactionGameControllerFactory
{
    public static IFactionGameController Create(
        IReadOnlyList<SquadController> army)
    {
        if (army != null &&
            army.Any(IsAeldariUnit))
        {
            return new AeldariGameController();
        }

        if (army != null &&
            army.Any(
                unit =>
                    unit != null &&
                    unit.HasIntrinsicKeyword(
                        "necrons")))
        {
            return new NecronGameController();
        }

        return new GenericFactionGameController();
    }

    private static bool IsAeldariUnit(
        SquadController unit)
    {
        if (unit == null)
            return false;

        return
            unit.HasIntrinsicKeyword("aeldari") ||
            unit.HasIntrinsicKeyword("asuryani") ||
            unit.HasIntrinsicKeyword("ynnari") ||
            unit.HasIntrinsicKeyword("harlequins") ||
            unit.HasIntrinsicKeyword("anhrathe") ||
            (!string.IsNullOrWhiteSpace(
                 unit.DisplayName) &&
             (unit.DisplayName.IndexOf(
                  "Yvraine",
                  StringComparison.OrdinalIgnoreCase) >= 0 ||
              unit.DisplayName.IndexOf(
                  "Yncarne",
                  StringComparison.OrdinalIgnoreCase) >= 0));
    }
}

/// <summary>
/// Migration host for v33.
///
/// It attaches automatically to the running Warboard game, discovers the
/// factions present in loaded rosters, instantiates one controller per
/// faction, and routes GameEventBus events to those controllers.
///
/// This lets the existing GameController remain untouched during the
/// architecture migration. Later versions can move more faction behaviour
/// behind IFactionGameController without another large GameController rewrite.
/// </summary>
public sealed class FactionControllerHost :
    MonoBehaviour
{
    public static FactionControllerHost Instance { get; private set; }

    private GameController game;

    private readonly Dictionary<
        string,
        IFactionGameController
    > controllers =
        new Dictionary<
            string,
            IFactionGameController
        >(StringComparer.OrdinalIgnoreCase);

    private float nextRefreshTime;

    public IReadOnlyDictionary<
        string,
        IFactionGameController
    > Controllers
    {
        get { return controllers; }
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object
            .FindAnyObjectByType<
                FactionControllerHost>() != null)
        {
            return;
        }

        GameObject hostObject =
            new GameObject(
                "WarboardFactionControllers");

        hostObject.AddComponent<
            FactionControllerHost>();
    }

    private void Awake()
    {
        Instance = this;

        GameEventBus.Raised +=
            HandleGameEvent;
    }

    private void OnDestroy()
    {
        GameEventBus.Raised -=
            HandleGameEvent;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (game == null)
        {
            game =
                UnityEngine.Object
                    .FindAnyObjectByType<
                        GameController>();

            if (game == null)
                return;
        }

        if (Time.unscaledTime >=
            nextRefreshTime)
        {
            nextRefreshTime =
                Time.unscaledTime +
                0.20f;

            RefreshControllers();
        }

        foreach (
            IFactionGameController controller
            in controllers.Values.ToArray())
        {
            controller.Tick();
        }
    }

    private void RefreshControllers()
    {
        IReadOnlyList<SquadController> allUnits =
            game != null
            ? game.CoreSquads
            : new List<SquadController>();

        Dictionary<
            string,
            List<SquadController>
        > armies =
            allUnits
                .Where(
                    unit =>
                        unit != null &&
                        !string.IsNullOrWhiteSpace(
                            unit.FactionId))
                .GroupBy(
                    unit => unit.FactionId,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList(),
                    StringComparer.OrdinalIgnoreCase);

        foreach (
            KeyValuePair<
                string,
                List<SquadController>
            > pair
            in armies)
        {
            IFactionGameController wanted =
                FactionGameControllerFactory
                    .Create(pair.Value);

            IFactionGameController current;

            bool replace =
                !controllers.TryGetValue(
                    pair.Key,
                    out current) ||
                current == null ||
                current.GetType() !=
                    wanted.GetType();

            if (replace)
            {
                wanted.Initialize(
                    game,
                    pair.Key);

                controllers[
                    pair.Key] =
                    wanted;

                current = wanted;
            }

            current.RefreshArmy(
                pair.Value);
        }

        List<string> stale =
            controllers.Keys
                .Where(
                    faction =>
                        !armies.ContainsKey(
                            faction))
                .ToList();

        foreach (string faction in stale)
            controllers.Remove(faction);
    }

    private void HandleGameEvent(
        GameEventContext context)
    {
        if (context == null)
            return;

        // A roster can be loaded between refresh ticks. Refresh immediately
        // before routing so the faction controller exists for the event.
        RefreshControllers();

        foreach (
            IFactionGameController controller
            in controllers.Values.ToArray())
        {
            controller.OnGameEvent(
                context);
        }
    }

    public IFactionGameController Get(
        string faction)
    {
        IFactionGameController result;

        return
            !string.IsNullOrWhiteSpace(
                faction) &&
            controllers.TryGetValue(
                faction,
                out result)
            ? result
            : null;
    }
}
