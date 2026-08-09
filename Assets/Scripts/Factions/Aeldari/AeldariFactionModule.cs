public static class AeldariFactionModule
{
    public static void Register()
    {
        AbilityRegistry.Register("swift", () => new SwiftAbility());
        AbilityRegistry.Register("precision", () => new PrecisionAbility());
    }
}

public class SwiftAbility : UnitAbilityBase
{
    public override string Id => "swift";

    public override float ModifyMove(SquadController squad, float value)
    {
        return value + 1f;
    }
}

public class PrecisionAbility : UnitAbilityBase
{
    public override string Id => "precision";

    public override int ModifyRangedSkill(
        SquadController squad,
        SquadController target,
        int value)
    {
        return value - 1;
    }
}
