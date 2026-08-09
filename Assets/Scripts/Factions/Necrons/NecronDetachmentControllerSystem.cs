public interface INecronDetachmentController
{
    NecronDetachment Detachment { get; }

    void Initialize(NecronGameController owner);

    void OnGameEvent(GameEventContext context);
}

public sealed class NecronDetachmentController :
    INecronDetachmentController
{
    private NecronGameController owner;

    public NecronDetachment Detachment
    {
        get;
        private set;
    }

    public NecronDetachmentController(
        NecronDetachment detachment)
    {
        Detachment = detachment;
    }

    public void Initialize(
        NecronGameController controller)
    {
        owner = controller;
    }

    public void OnGameEvent(
        GameEventContext context)
    {
        // Rule execution is centralised in NecronsFactionPack11Runtime so
        // several simultaneously-selected detachments can stack cleanly.
    }
}

public static class NecronDetachmentControllerFactory
{
    public static INecronDetachmentController Create(
        NecronDetachment detachment)
    {
        return new NecronDetachmentController(detachment);
    }
}
