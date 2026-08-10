using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
}


public interface IFactionPreGameController
{
    bool IsReadyForDeployment
    {
        get;
    }

    string DeploymentBlockReason
    {
        get;
    }
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

    protected bool EventConcernsFaction(
        GameEventContext context)
    {
        if (context == null)
            return false;

        switch (context.Type)
        {
            case GameEventType.BattleStarted:
            case GameEventType.BattleRoundStarted:
            case GameEventType.BattleRoundEnded:
            case GameEventType.TurnStarted:
            case GameEventType.TurnEnded:
            case GameEventType.PhaseStarted:
            case GameEventType.PhaseEnded:
                return true;
        }

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

        if (army != null &&
            army.Any(
                unit =>
                    unit != null &&
                    unit.HasIntrinsicKeyword(
                        "adeptus custodes")))
        {
            return new CustodesGameController();
        }

        // WARBOARD_V46_STANDARD_FACTION_FACTORY
        IFactionGameController extension =
            WarboardFactionExtensionHub
                .TryCreateController(
                    army
                );

        if (extension != null)
            return extension;

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
/// Event-driven faction-controller host.
///
/// v36 removes the old 0.20 second roster polling loop. The host binds once
/// to GameController, rebuilds controllers only when GameController reports a
/// roster change, and otherwise only routes the authoritative core event bus.
/// </summary>
public sealed class FactionControllerHost :
    MonoBehaviour
{
    public static FactionControllerHost Instance
    {
        get;
        private set;
    }

    private GameController game;

    private readonly Dictionary<
        string,
        IFactionGameController
    > controllers =
        new Dictionary<
            string,
            IFactionGameController
        >(StringComparer.OrdinalIgnoreCase);

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

    private void Start()
    {
        GameController owner =
            GameController.Current;

        if (owner == null)
        {
            owner =
                UnityEngine.Object
                    .FindAnyObjectByType<
                        GameController>();
        }

        Attach(owner);
    }

    private void OnDestroy()
    {
        GameEventBus.Raised -=
            HandleGameEvent;

        Attach(null);

        if (Instance == this)
            Instance = null;
    }

    private void Attach(
        GameController owner)
    {
        if (game == owner)
            return;

        if (game != null)
        {
            game.RostersChanged -=
                HandleRostersChanged;
        }

        game = owner;

        if (game != null)
        {
            game.RostersChanged +=
                HandleRostersChanged;

            RefreshControllers();
        }
        else
        {
            controllers.Clear();
        }
    }

    private void HandleRostersChanged()
    {
        RefreshControllers();
    }

    private void RefreshControllers()
    {
        IReadOnlyList<SquadController> allUnits =
            game != null
            ? game.AllSquads
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

        foreach (
            IFactionGameController controller
            in controllers.Values.ToArray())
        {
            controller.OnGameEvent(
                context);
        }
    }


    public bool CanBeginDeployment(
        out string reason)
    {
        foreach (
            IFactionGameController controller
            in controllers.Values)
        {
            IFactionPreGameController preGame =
                controller as
                    IFactionPreGameController;

            if (preGame == null ||
                preGame.IsReadyForDeployment)
            {
                continue;
            }

            reason =
                string.IsNullOrWhiteSpace(
                    preGame.DeploymentBlockReason)
                ? controller.DisplayName +
                  " pre-game setup is incomplete."
                : preGame.DeploymentBlockReason;

            return false;
        }

        reason = "";
        return true;
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
