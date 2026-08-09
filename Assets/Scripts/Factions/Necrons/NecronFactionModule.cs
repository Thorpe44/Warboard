public static class NecronFactionModule
{
    public static void Register()
    {
        AbilityRegistry.Register("repair", () => new RepairAbility());
        AbilityRegistry.Register("reinforced", () => new ReinforcedAbility());
    }
}

public class RepairAbility : UnitAbilityBase
{
    public override string Id => "repair";

    public override void OnTurnStart(SquadController squad)
    {
        squad.HealWounds(1);
    }
}

public class ReinforcedAbility : UnitAbilityBase
{
    public override string Id => "reinforced";

    public override int ModifySave(
        SquadController squad,
        SquadController attacker,
        int value)
    {
        return value - 1;
    }
}
