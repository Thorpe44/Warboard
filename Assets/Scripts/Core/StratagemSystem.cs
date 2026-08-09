using System;
using System.Collections.Generic;
using System.Linq;

public enum GameEventType
{
    BattleStarted,
    BattleRoundStarted,
    BattleRoundEnded,

    TurnStarted,
    TurnEnded,

    PhaseStarted,
    PhaseEnded,

    UnitSelectedToMove,
    MoveStarted,
    MoveEnded,
    UnitSetUp,
    UnitAdvanced,
    UnitFellBack,

    AttackStarted,
    AttackResolved,
    HitRolled,
    WoundRolled,

    UnitFinishedShooting,

    ChargeDeclared,
    ChargeRolled,

    UnitSelectedToFight,
    UnitFinishedFighting,

    ModelDestroyed,
    UnitDestroyed,

    ObjectiveControlChanged,

    UnitEmbarked,
    UnitDisembarked
}

public enum CommandRerollStage
{
    None,
    Hit,
    Wound,
    Save,
    Damage
}

public class GameEventContext
{
    public GameEventType Type;

    public GameController Game;
    public string ActingFaction;

    public GameController.Phase Phase;

    public SquadController Source;
    public SquadController Target;

    public AttackMode AttackMode;

    public int RollTotal;
    public int PreviousRollTotal;
    public bool IsReroll;

    public int Amount;
    public string Note;
}

public static class GameEventBus
{
    public static event Action<GameEventContext> Raised;

    public static void Raise(
        GameEventContext context)
    {
        if (context == null)
            return;

        Action<GameEventContext> handler =
            Raised;

        if (handler != null)
            handler(context);
    }
}

public interface IStratagem
{
    string Id { get; }
    string DisplayName { get; }
    int Cost { get; }

    bool CanUse(
        GameController game,
        string faction,
        GameEventContext context);

    bool Use(
        GameController game,
        string faction,
        GameEventContext context);
}

public abstract class StratagemBase :
    IStratagem
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract int Cost { get; }

    public abstract bool CanUse(
        GameController game,
        string faction,
        GameEventContext context);

    public virtual bool Use(
        GameController game,
        string faction,
        GameEventContext context)
    {
        if (!CanUse(
            game,
            faction,
            context))
        {
            return false;
        }

        if (context != null &&
            context.Source != null)
        {
            return game.SpendStratagemCPForUnit(
                context.Source,
                Cost,
                DisplayName
            );
        }

        return game.TrySpendCommandPoints(
            faction,
            Cost
        );
    }
}

public class CommandRerollStratagem :
    StratagemBase
{
    public override string Id
    {
        get { return "command_reroll"; }
    }

    public override string DisplayName
    {
        get { return "Command Re-roll"; }
    }

    public override int Cost
    {
        get { return 1; }
    }

    public override bool CanUse(
        GameController game,
        string faction,
        GameEventContext context)
    {
        if (game == null ||
            context == null)
        {
            return false;
        }

        if (context.Type !=
            GameEventType.ChargeRolled)
        {
            return false;
        }

        if (context.IsReroll)
            return false;

        if (context.Source != null &&
            context.Source
                .JoinedActionController()
                .IsBattleShocked)
        {
            return false;
        }

        if (!string.Equals(
            context.ActingFaction,
            faction,
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return
            game.GetCommandPoints(
                faction
            ) >= Cost;
    }
}

public static class StratagemRegistry
{
    private static readonly
        Dictionary<string, IStratagem> Stratagems =
            new Dictionary<string, IStratagem>(
                StringComparer.OrdinalIgnoreCase
            );

    static StratagemRegistry()
    {
        Register(
            new CommandRerollStratagem()
        );
    }

    public static void Register(
        IStratagem stratagem)
    {
        if (stratagem == null ||
            string.IsNullOrWhiteSpace(
                stratagem.Id))
        {
            return;
        }

        Stratagems[
            stratagem.Id
        ] = stratagem;
    }

    public static IStratagem Get(
        string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        IStratagem stratagem;

        return Stratagems.TryGetValue(
            id,
            out stratagem)
            ? stratagem
            : null;
    }

    public static IReadOnlyList<IStratagem> All
    {
        get
        {
            return Stratagems
                .Values
                .OrderBy(
                    stratagem =>
                        stratagem.DisplayName
                )
                .ToList();
        }
    }
}
