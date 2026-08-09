using System.Collections.Generic;

/// <summary>
/// Necron faction-controller entry point.
///
/// v33 intentionally keeps existing Necron gameplay behaviour in
/// FactionRuleSystem. This controller establishes the same architecture used
/// by Aeldari so the Necron rules can be migrated without changing
/// GameController.
/// </summary>
public sealed class NecronGameController :
    FactionGameControllerBase
{
    public override string DisplayName
    {
        get { return "Necrons"; }
    }

    public override void RefreshArmy(
        IReadOnlyList<SquadController> units)
    {
        base.RefreshArmy(units);
    }

    public override void OnGameEvent(
        GameEventContext context)
    {
        if (!EventConcernsFaction(
                context))
        {
            return;
        }

        // Existing Necron rules remain live through FactionRuleSystem.
        // New Necron faction/detachment behaviour should be routed here.
    }
}
