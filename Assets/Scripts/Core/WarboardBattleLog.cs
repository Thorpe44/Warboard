using System;

public class WarboardBattleLogEntry
{
    public int Sequence;
    public int Round;
    public string Faction = "";
    public string Phase = "";
    public string Category = "";
    public string Title = "";
    public string Detail = "";
    public DateTime Time;

    public string Header
    {
        get
        {
            string round =
                Round > 0
                ? "R" + Round
                : "PRE";

            return
                "#" +
                Sequence +
                "  |  " +
                round +
                "  |  " +
                (string.IsNullOrWhiteSpace(
                    Faction)
                    ? "SYSTEM"
                    : Faction) +
                "  |  " +
                (string.IsNullOrWhiteSpace(
                    Phase)
                    ? "SETUP"
                    : Phase) +
                "  |  " +
                Category;
        }
    }
}
