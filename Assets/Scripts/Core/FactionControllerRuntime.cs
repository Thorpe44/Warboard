using UnityEngine;

/// <summary>
/// Small runtime lookup facade for the faction-controller layer.
///
/// Core systems can ask whether a faction controller exists without owning
/// or constructing faction-specific rule systems themselves.
/// </summary>
public static class FactionControllerRuntime
{
    public static IFactionGameController Get(
        string factionId)
    {
        if (string.IsNullOrWhiteSpace(
                factionId))
        {
            return null;
        }

        FactionControllerHost host =
            Object.FindAnyObjectByType<
                FactionControllerHost>();

        return host != null
            ? host.Get(factionId)
            : null;
    }

    public static AeldariGameController GetAeldari(
        string factionId)
    {
        return Get(factionId)
            as AeldariGameController;
    }

    public static NecronGameController GetNecrons(
        string factionId)
    {
        return Get(factionId)
            as NecronGameController;
    }
}
