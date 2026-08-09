/// <summary>
/// Small lookup facade for the loaded faction-controller layer.
/// No scene searches or polling are performed here.
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
            FactionControllerHost.Instance;

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
