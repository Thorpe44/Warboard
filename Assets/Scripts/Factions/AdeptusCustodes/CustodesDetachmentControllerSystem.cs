using System;

public interface ICustodesDetachmentController
{
    CustodesDetachment Detachment { get; }

    void Initialize(
        CustodesGameController faction);

    void OnGameEvent(
        GameEventContext context);
}

/// <summary>
/// Runtime ownership object for one selected Custodes detachment. The actual
/// Edition 11 rule logic lives in CustodesFactionPack11/GameController hooks,
/// while these objects preserve one-controller-per-selected-detachment
/// ownership and event routing.
/// </summary>
public sealed class CustodesDetachmentController :
    ICustodesDetachmentController
{
    public CustodesDetachment Detachment
    {
        get;
        private set;
    }

    private CustodesGameController faction;

    public CustodesDetachmentController(
        CustodesDetachment detachment)
    {
        Detachment = detachment;
    }

    public void Initialize(
        CustodesGameController owner)
    {
        faction = owner;
    }

    public void OnGameEvent(
        GameEventContext context)
    {
        // Detachment-specific rule bodies are deliberately not duplicated
        // here. The faction pack is the single rules authority and queries
        // CustodesDetachmentRuntime for every active detachment.
    }
}

public static class CustodesDetachmentControllerFactory
{
    public static ICustodesDetachmentController Create(
        CustodesDetachment detachment)
    {
        return new CustodesDetachmentController(
            detachment);
    }
}
