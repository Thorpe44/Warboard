using System;
using System.Collections.Generic;

public interface IUnitAbility
{
    string Id { get; }

    float ModifyMove(SquadController squad, float value);
    int ModifyRangedSkill(SquadController squad, SquadController target, int value);
    int ModifyMeleeSkill(SquadController squad, SquadController target, int value);
    int ModifySave(SquadController squad, SquadController attacker, int value);

    void OnTurnStart(SquadController squad);
}

public abstract class UnitAbilityBase : IUnitAbility
{
    public abstract string Id { get; }

    public virtual float ModifyMove(SquadController squad, float value) => value;
    public virtual int ModifyRangedSkill(SquadController squad, SquadController target, int value) => value;
    public virtual int ModifyMeleeSkill(SquadController squad, SquadController target, int value) => value;
    public virtual int ModifySave(SquadController squad, SquadController attacker, int value) => value;

    public virtual void OnTurnStart(SquadController squad) { }
}

public static class AbilityRegistry
{
    private static readonly Dictionary<string, Func<IUnitAbility>> Factories =
        new Dictionary<string, Func<IUnitAbility>>(StringComparer.OrdinalIgnoreCase);

    public static void Register(string id, Func<IUnitAbility> factory)
    {
        Factories[id] = factory;
    }

    public static IUnitAbility Create(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (Factories.TryGetValue(id, out var factory))
            return factory();

        UnityEngine.Debug.LogWarning("Unknown ability id: " + id);
        return null;
    }
}
