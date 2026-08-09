using System;
using System.Collections.Generic;
using System.Linq;

public enum CustodesDetachment
{
    TalonsOfTheEmperor,
    ShieldHost,
    NullMaidenVigil,
    AuricChampions,
    SolarSpearhead,
    LionsOfTheEmperor,
    MightOfTheMoritoi,
    SilentHunters,
    TharanatoiHammerblow
}

/// <summary>
/// Edition 11 Adeptus Custodes detachment catalogue and multi-detachment
/// selection. Detachment Points use the same Core Rules allowance already
/// used by Warboard's Aeldari implementation.
/// </summary>
public static class CustodesDetachmentRuntime
{
    private static readonly Dictionary<string, List<CustodesDetachment>>
        selectedByFaction =
            new Dictionary<string, List<CustodesDetachment>>(
                StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<CustodesDetachment, string> names =
        new Dictionary<CustodesDetachment, string>
        {
            { CustodesDetachment.TalonsOfTheEmperor, "Talons of the Emperor" },
            { CustodesDetachment.ShieldHost, "Shield Host" },
            { CustodesDetachment.NullMaidenVigil, "Null Maiden Vigil" },
            { CustodesDetachment.AuricChampions, "Auric Champions" },
            { CustodesDetachment.SolarSpearhead, "Solar Spearhead" },
            { CustodesDetachment.LionsOfTheEmperor, "Lions of the Emperor" },
            { CustodesDetachment.MightOfTheMoritoi, "Might of the Moritoi" },
            { CustodesDetachment.SilentHunters, "Silent Hunters" },
            { CustodesDetachment.TharanatoiHammerblow, "Tharanatoi Hammerblow" }
        };

    private static readonly Dictionary<CustodesDetachment, int> costs =
        new Dictionary<CustodesDetachment, int>
        {
            { CustodesDetachment.TalonsOfTheEmperor, 3 },
            { CustodesDetachment.ShieldHost, 2 },
            { CustodesDetachment.NullMaidenVigil, 2 },
            { CustodesDetachment.AuricChampions, 2 },
            { CustodesDetachment.SolarSpearhead, 2 },
            { CustodesDetachment.LionsOfTheEmperor, 2 },
            { CustodesDetachment.MightOfTheMoritoi, 1 },
            { CustodesDetachment.SilentHunters, 1 },
            { CustodesDetachment.TharanatoiHammerblow, 1 }
        };

    private static readonly HashSet<CustodesDetachment> armoury =
        new HashSet<CustodesDetachment>
        {
            CustodesDetachment.SolarSpearhead,
            CustodesDetachment.MightOfTheMoritoi
        };

    private static readonly HashSet<CustodesDetachment> lions =
        new HashSet<CustodesDetachment>
        {
            CustodesDetachment.LionsOfTheEmperor,
            CustodesDetachment.TharanatoiHammerblow
        };

    public static IReadOnlyList<CustodesDetachment> GetSelected(
        string factionId)
    {
        List<CustodesDetachment> result;
        if (string.IsNullOrWhiteSpace(factionId) ||
            !selectedByFaction.TryGetValue(factionId, out result) ||
            result == null)
        {
            return new CustodesDetachment[0];
        }

        return result.ToArray();
    }

    public static void SetSelected(
        string factionId,
        IEnumerable<CustodesDetachment> detachments)
    {
        if (string.IsNullOrWhiteSpace(factionId))
            return;

        List<CustodesDetachment> result =
            detachments == null
            ? new List<CustodesDetachment>()
            : detachments.Distinct().ToList();

        selectedByFaction[factionId] = result;
    }

    public static void Clear(string factionId)
    {
        if (!string.IsNullOrWhiteSpace(factionId))
            selectedByFaction.Remove(factionId);
    }

    public static bool Has(
        string factionId,
        CustodesDetachment detachment)
    {
        List<CustodesDetachment> selected;
        return
            !string.IsNullOrWhiteSpace(factionId) &&
            selectedByFaction.TryGetValue(factionId, out selected) &&
            selected != null &&
            selected.Contains(detachment);
    }

    public static string Name(CustodesDetachment detachment)
    {
        string value;
        return names.TryGetValue(detachment, out value)
            ? value
            : detachment.ToString();
    }

    public static int Cost(CustodesDetachment detachment)
    {
        int value;
        return costs.TryGetValue(detachment, out value)
            ? value
            : 0;
    }

    public static int TotalCost(
        IEnumerable<CustodesDetachment> detachments)
    {
        return detachments == null
            ? 0
            : detachments.Distinct().Sum(Cost);
    }

    public static bool IsArmoury(CustodesDetachment detachment)
    {
        return armoury.Contains(detachment);
    }

    public static bool IsLions(CustodesDetachment detachment)
    {
        return lions.Contains(detachment);
    }

    public static int DetachmentPointLimit(string battleSize)
    {
        if (string.Equals(
                battleSize,
                "Incursion",
                StringComparison.OrdinalIgnoreCase))
            return 2;

        if (string.Equals(
                battleSize,
                "Strike Force",
                StringComparison.OrdinalIgnoreCase))
            return 3;

        return -1;
    }

    public static bool TryParse(
        string text,
        out CustodesDetachment detachment)
    {
        detachment = CustodesDetachment.ShieldHost;
        string normalized = Normalize(text);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        foreach (KeyValuePair<CustodesDetachment, string> pair
            in names.OrderByDescending(value => value.Value.Length))
        {
            string wanted = Normalize(pair.Value);
            if (normalized == wanted ||
                normalized.StartsWith(wanted + " ", StringComparison.Ordinal) ||
                normalized.Contains(" " + wanted + " ") ||
                normalized.EndsWith(" " + wanted, StringComparison.Ordinal))
            {
                detachment = pair.Key;
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        char[] chars =
            value
                .Replace('\u00A0', ' ')
                .Replace('’', '\'')
                .ToLowerInvariant()
                .Where(c =>
                    char.IsLetterOrDigit(c) ||
                    char.IsWhiteSpace(c) ||
                    c == '\'')
                .ToArray();

        return string.Join(
            " ",
            new string(chars).Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries));
    }
}
