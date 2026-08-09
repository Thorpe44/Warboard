using System;
using System.Collections.Generic;
using System.Linq;

public enum NecronDetachment
{
    AwakenedDynasty,
    AnnihilationLegion,
    CanoptekCourt,
    ObeisancePhalanx,
    HypercryptLegion,
    StarshatterArsenal,
    CryptekConclave,
    CursedLegion,
    PantheonOfWoe,
    HandOfTheDynasty,
    SkyshroudSpearhead,
    ThePhaeronsArmoury
}

public static class NecronDetachmentRuntime
{
    private static readonly Dictionary<string, List<NecronDetachment>>
        selectedByFaction =
            new Dictionary<string, List<NecronDetachment>>(
                StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<NecronDetachment, string> names =
        new Dictionary<NecronDetachment, string>
        {
            { NecronDetachment.AwakenedDynasty, "Awakened Dynasty" },
            { NecronDetachment.AnnihilationLegion, "Annihilation Legion" },
            { NecronDetachment.CanoptekCourt, "Canoptek Court" },
            { NecronDetachment.ObeisancePhalanx, "Obeisance Phalanx" },
            { NecronDetachment.HypercryptLegion, "Hypercrypt Legion" },
            { NecronDetachment.StarshatterArsenal, "Starshatter Arsenal" },
            { NecronDetachment.CryptekConclave, "Cryptek Conclave" },
            { NecronDetachment.CursedLegion, "Cursed Legion" },
            { NecronDetachment.PantheonOfWoe, "Pantheon of Woe" },
            { NecronDetachment.HandOfTheDynasty, "Hand of the Dynasty" },
            { NecronDetachment.SkyshroudSpearhead, "Skyshroud Spearhead" },
            { NecronDetachment.ThePhaeronsArmoury, "The Phaeron's Armoury" }
        };

    private static readonly Dictionary<NecronDetachment, int> costs =
        new Dictionary<NecronDetachment, int>
        {
            { NecronDetachment.AwakenedDynasty, 3 },
            { NecronDetachment.AnnihilationLegion, 2 },
            { NecronDetachment.CanoptekCourt, 3 },
            { NecronDetachment.ObeisancePhalanx, 2 },
            { NecronDetachment.HypercryptLegion, 2 },
            { NecronDetachment.StarshatterArsenal, 3 },
            { NecronDetachment.CryptekConclave, 2 },
            { NecronDetachment.CursedLegion, 2 },
            { NecronDetachment.PantheonOfWoe, 2 },
            { NecronDetachment.HandOfTheDynasty, 1 },
            { NecronDetachment.SkyshroudSpearhead, 1 },
            { NecronDetachment.ThePhaeronsArmoury, 1 }
        };

    private static readonly HashSet<NecronDetachment> dynasty =
        new HashSet<NecronDetachment>
        {
            NecronDetachment.AwakenedDynasty,
            NecronDetachment.HandOfTheDynasty
        };

    private static readonly HashSet<NecronDetachment> hypercrypt =
        new HashSet<NecronDetachment>
        {
            NecronDetachment.HypercryptLegion,
            NecronDetachment.ThePhaeronsArmoury
        };

    public static IReadOnlyList<NecronDetachment> GetSelected(string factionId)
    {
        List<NecronDetachment> result;
        if (string.IsNullOrWhiteSpace(factionId) ||
            !selectedByFaction.TryGetValue(factionId, out result) ||
            result == null)
        {
            return new NecronDetachment[0];
        }
        return result.ToArray();
    }

    public static void SetSelected(
        string factionId,
        IEnumerable<NecronDetachment> detachments)
    {
        if (string.IsNullOrWhiteSpace(factionId))
            return;

        selectedByFaction[factionId] =
            detachments == null
            ? new List<NecronDetachment>()
            : detachments.Distinct().ToList();
    }

    public static void Clear(string factionId)
    {
        if (!string.IsNullOrWhiteSpace(factionId))
            selectedByFaction.Remove(factionId);
    }

    public static bool Has(string factionId, NecronDetachment detachment)
    {
        List<NecronDetachment> selected;
        return
            !string.IsNullOrWhiteSpace(factionId) &&
            selectedByFaction.TryGetValue(factionId, out selected) &&
            selected != null &&
            selected.Contains(detachment);
    }

    public static string Name(NecronDetachment detachment)
    {
        string value;
        return names.TryGetValue(detachment, out value)
            ? value
            : detachment.ToString();
    }

    public static int Cost(NecronDetachment detachment)
    {
        int value;
        return costs.TryGetValue(detachment, out value)
            ? value
            : 0;
    }

    public static int TotalCost(IEnumerable<NecronDetachment> detachments)
    {
        return detachments == null
            ? 0
            : detachments.Distinct().Sum(Cost);
    }

    public static bool IsDynasty(NecronDetachment detachment)
    {
        return dynasty.Contains(detachment);
    }

    public static bool IsHypercrypt(NecronDetachment detachment)
    {
        return hypercrypt.Contains(detachment);
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
        out NecronDetachment detachment)
    {
        detachment = NecronDetachment.AwakenedDynasty;
        string normalized = Normalize(text);

        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        foreach (KeyValuePair<NecronDetachment, string> pair
            in names.OrderByDescending(value => value.Value.Length))
        {
            string wanted = Normalize(pair.Value);

            if (normalized == wanted ||
                normalized.StartsWith(
                    wanted + " ",
                    StringComparison.Ordinal) ||
                normalized.Contains(
                    " " + wanted + " ") ||
                normalized.EndsWith(
                    " " + wanted,
                    StringComparison.Ordinal))
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
