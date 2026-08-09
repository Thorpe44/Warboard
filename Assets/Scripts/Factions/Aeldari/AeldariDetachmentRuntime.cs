using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 11e Aeldari detachment catalogue + locked runtime selection.
///
/// Core 11e allows multiple detachments to be selected by spending Detachment
/// Points. The legacy AeldariRulesSystem was originally built around a single
/// detachment enum, so this runtime is the authoritative multi-detachment
/// selection while the remaining legacy rule bodies migrate out.
/// </summary>
public static class AeldariDetachmentRuntime
{
    private static readonly Dictionary<
        string,
        List<AeldariDetachment>
    > selectedByFaction =
        new Dictionary<
            string,
            List<AeldariDetachment>
        >(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<
        AeldariDetachment,
        string
    > names =
        new Dictionary<AeldariDetachment, string>
        {
            { AeldariDetachment.Warhost, "Warhost" },
            { AeldariDetachment.WindriderHost, "Windrider Host" },
            { AeldariDetachment.SpiritConclave, "Spirit Conclave" },
            { AeldariDetachment.GuardianBattlehost, "Guardian Battlehost" },
            { AeldariDetachment.GhostsOfTheWebway, "Ghosts of the Webway" },
            { AeldariDetachment.DevotedOfYnnead, "Devoted of Ynnead" },
            { AeldariDetachment.SeerCouncil, "Seer Council" },
            { AeldariDetachment.AspectHost, "Aspect Host" },
            { AeldariDetachment.ArmouredWarhost, "Armoured Warhost" },
            { AeldariDetachment.FatefulPerformance, "Fateful Performance" },
            { AeldariDetachment.PathOfTheOutcast, "Path of the Outcast" },
            { AeldariDetachment.TwilightFlickers, "Twilight Flickers" },
            { AeldariDetachment.SerpentsBrood, "Serpent's Brood" },
            { AeldariDetachment.EldritchRaiders, "Eldritch Raiders" },
            { AeldariDetachment.CorsairCoterie, "Corsair Coterie" }
        };

    // Aeldari Faction Pack 11e v1.1, July 2026.
    private static readonly Dictionary<
        AeldariDetachment,
        int
    > costs =
        new Dictionary<AeldariDetachment, int>
        {
            { AeldariDetachment.Warhost, 3 },
            { AeldariDetachment.WindriderHost, 2 },
            { AeldariDetachment.SpiritConclave, 2 },
            { AeldariDetachment.GuardianBattlehost, 2 },
            { AeldariDetachment.GhostsOfTheWebway, 2 },
            { AeldariDetachment.DevotedOfYnnead, 2 },
            { AeldariDetachment.SeerCouncil, 2 },
            { AeldariDetachment.AspectHost, 3 },
            { AeldariDetachment.ArmouredWarhost, 1 },
            { AeldariDetachment.FatefulPerformance, 1 },
            { AeldariDetachment.PathOfTheOutcast, 1 },
            { AeldariDetachment.TwilightFlickers, 1 },
            { AeldariDetachment.SerpentsBrood, 2 },
            { AeldariDetachment.EldritchRaiders, 2 },
            { AeldariDetachment.CorsairCoterie, 2 }
        };

    private static readonly HashSet<AeldariDetachment>
        acrobatic =
            new HashSet<AeldariDetachment>
            {
                AeldariDetachment.GhostsOfTheWebway,
                AeldariDetachment.FatefulPerformance,
                AeldariDetachment.TwilightFlickers,
                AeldariDetachment.SerpentsBrood
            };

    public static IReadOnlyList<AeldariDetachment> GetSelected(
        string factionId)
    {
        List<AeldariDetachment> result;

        if (string.IsNullOrWhiteSpace(factionId) ||
            !selectedByFaction.TryGetValue(
                factionId,
                out result) ||
            result == null)
        {
            return new AeldariDetachment[0];
        }

        return result.ToArray();
    }

    public static void SetSelected(
        string factionId,
        IEnumerable<AeldariDetachment> detachments)
    {
        if (string.IsNullOrWhiteSpace(factionId))
            return;

        List<AeldariDetachment> selected =
            new List<AeldariDetachment>();

        if (detachments != null)
        {
            foreach (AeldariDetachment detachment in detachments)
            {
                if (!selected.Contains(detachment))
                    selected.Add(detachment);
            }
        }

        selectedByFaction[factionId] = selected;
    }

    public static void Clear(
        string factionId)
    {
        if (string.IsNullOrWhiteSpace(factionId))
            return;

        selectedByFaction.Remove(factionId);
    }

    public static bool Has(
        string factionId,
        AeldariDetachment detachment)
    {
        List<AeldariDetachment> selected;

        return
            !string.IsNullOrWhiteSpace(factionId) &&
            selectedByFaction.TryGetValue(
                factionId,
                out selected) &&
            selected != null &&
            selected.Contains(detachment);
    }

    public static AeldariDetachment Primary(
        string factionId,
        AeldariDetachment fallback =
            AeldariDetachment.Warhost)
    {
        List<AeldariDetachment> selected;

        if (selectedByFaction.TryGetValue(
                factionId ?? "",
                out selected) &&
            selected != null &&
            selected.Count > 0)
        {
            return selected[0];
        }

        return fallback;
    }

    public static string Name(
        AeldariDetachment detachment)
    {
        string name;

        return names.TryGetValue(
            detachment,
            out name)
            ? name
            : detachment.ToString();
    }

    public static int Cost(
        AeldariDetachment detachment)
    {
        int cost;

        return costs.TryGetValue(
            detachment,
            out cost)
            ? cost
            : 0;
    }

    public static int TotalCost(
        IEnumerable<AeldariDetachment> detachments)
    {
        if (detachments == null)
            return 0;

        return detachments
            .Distinct()
            .Sum(Cost);
    }

    public static bool IsAcrobatic(
        AeldariDetachment detachment)
    {
        return acrobatic.Contains(detachment);
    }

    /// <summary>
    /// Core Rules 25.03 currently defines 2DP for Incursion and 3DP for
    /// Strike Force. Unknown/custom sizes are returned as -1 so Warboard does
    /// not invent an unsupported DP allowance.
    /// </summary>
    public static int DetachmentPointLimit(
        string battleSize)
    {
        if (string.Equals(
                battleSize,
                "Incursion",
                StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(
                battleSize,
                "Strike Force",
                StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return -1;
    }

    public static bool TryParse(
        string text,
        out AeldariDetachment detachment)
    {
        detachment = AeldariDetachment.Warhost;

        string normalized = Normalize(text);

        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        foreach (
            KeyValuePair<AeldariDetachment, string> pair
            in names.OrderByDescending(
                value => value.Value.Length))
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

    private static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        char[] chars =
            value
                .Replace('\u00A0', ' ')
                .Replace('’', '\'')
                .ToLowerInvariant()
                .Where(
                    c =>
                        char.IsLetterOrDigit(c) ||
                        char.IsWhiteSpace(c) ||
                        c == '\'')
                .ToArray();

        return string.Join(
            " ",
            new string(chars)
                .Split(
                    new[] { ' ', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries));
    }
}
