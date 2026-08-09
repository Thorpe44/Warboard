/// <summary>
/// v43.3 temporary compile shim.
///
/// An earlier v43 migration inserted the identifier `squad` into
/// ReserveCanArriveThisRound in GameController.Setup.cs. That method uses
/// GameController's reservePlacementSquad state instead.
///
/// This property only exists to let the already-mutated source compile once.
/// WarboardV43CustodesFactionRules repairs GameController.Setup.cs to use
/// reservePlacementSquad directly, then deletes this shim.
/// </summary>
public partial class GameController
{
    private SquadController squad
    {
        get { return reservePlacementSquad; }
    }
}
