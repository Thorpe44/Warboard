using System;

public interface IAeldariDetachmentController
{
    AeldariDetachment Detachment
    {
        get;
    }

    string DisplayName
    {
        get;
    }

    void Initialize(
        AeldariGameController faction);

    void OnGameEvent(
        GameEventContext context);

    void Tick();
}

public abstract class AeldariDetachmentControllerBase :
    IAeldariDetachmentController
{
    protected AeldariGameController Faction
    {
        get;
        private set;
    }

    public abstract AeldariDetachment Detachment
    {
        get;
    }

    public abstract string DisplayName
    {
        get;
    }

    public virtual void Initialize(
        AeldariGameController faction)
    {
        Faction = faction;
    }

    public virtual void OnGameEvent(
        GameEventContext context)
    {
    }

    public virtual void Tick()
    {
    }
}

public static class AeldariDetachmentControllerFactory
{
    public static IAeldariDetachmentController Create(
        AeldariDetachment detachment)
    {
        switch (detachment)
        {
            case AeldariDetachment.Warhost:
                return new WarhostDetachmentController();

            case AeldariDetachment.WindriderHost:
                return new WindriderHostDetachmentController();

            case AeldariDetachment.SpiritConclave:
                return new SpiritConclaveDetachmentController();

            case AeldariDetachment.GuardianBattlehost:
                return new GuardianBattlehostDetachmentController();

            case AeldariDetachment.GhostsOfTheWebway:
                return new GhostsOfTheWebwayDetachmentController();

            case AeldariDetachment.DevotedOfYnnead:
                return new DevotedOfYnneadDetachmentController();

            case AeldariDetachment.SeerCouncil:
                return new SeerCouncilDetachmentController();

            case AeldariDetachment.AspectHost:
                return new AspectHostDetachmentController();

            case AeldariDetachment.ArmouredWarhost:
                return new ArmouredWarhostDetachmentController();

            case AeldariDetachment.FatefulPerformance:
                return new FatefulPerformanceDetachmentController();

            case AeldariDetachment.PathOfTheOutcast:
                return new PathOfTheOutcastDetachmentController();

            case AeldariDetachment.TwilightFlickers:
                return new TwilightFlickersDetachmentController();

            case AeldariDetachment.SerpentsBrood:
                return new SerpentsBroodDetachmentController();

            case AeldariDetachment.EldritchRaiders:
                return new EldritchRaidersDetachmentController();

            case AeldariDetachment.CorsairCoterie:
                return new CorsairCoterieDetachmentController();
        }

        return new WarhostDetachmentController();
    }
}

public sealed class WarhostDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.Warhost;

    public override string DisplayName
        => "Warhost";
}

public sealed class WindriderHostDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.WindriderHost;

    public override string DisplayName
        => "Windrider Host";
}

public sealed class SpiritConclaveDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.SpiritConclave;

    public override string DisplayName
        => "Spirit Conclave";
}

public sealed class GuardianBattlehostDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.GuardianBattlehost;

    public override string DisplayName
        => "Guardian Battlehost";
}

public sealed class GhostsOfTheWebwayDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.GhostsOfTheWebway;

    public override string DisplayName
        => "Ghosts of the Webway";
}

public sealed class DevotedOfYnneadDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.DevotedOfYnnead;

    public override string DisplayName
        => "Devoted of Ynnead";
}

public sealed class SeerCouncilDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.SeerCouncil;

    public override string DisplayName
        => "Seer Council";
}

public sealed class AspectHostDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.AspectHost;

    public override string DisplayName
        => "Aspect Host";
}

public sealed class ArmouredWarhostDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.ArmouredWarhost;

    public override string DisplayName
        => "Armoured Warhost";
}

public sealed class FatefulPerformanceDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.FatefulPerformance;

    public override string DisplayName
        => "Fateful Performance";
}

public sealed class PathOfTheOutcastDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.PathOfTheOutcast;

    public override string DisplayName
        => "Path of the Outcast";
}

public sealed class TwilightFlickersDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.TwilightFlickers;

    public override string DisplayName
        => "Twilight Flickers";
}

public sealed class SerpentsBroodDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.SerpentsBrood;

    public override string DisplayName
        => "Serpent's Brood";
}

public sealed class EldritchRaidersDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.EldritchRaiders;

    public override string DisplayName
        => "Eldritch Raiders";
}

public sealed class CorsairCoterieDetachmentController :
    AeldariDetachmentControllerBase
{
    public override AeldariDetachment Detachment
        => AeldariDetachment.CorsairCoterie;

    public override string DisplayName
        => "Corsair Coterie";
}
