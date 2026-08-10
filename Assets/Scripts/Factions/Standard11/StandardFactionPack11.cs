using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class StandardFactionEnhancement11
{
    public string name = "";
    public int points;
    public string rule = "";
    public int sourcePage;
}

[Serializable]
public sealed class StandardFactionStratagem11
{
    public string name = "";
    public int cost;
    public string category = "";
    public string when = "";
    public string target = "";
    public string effect = "";
    public string restrictions = "";
    public int sourcePage;

    public string FullRule
    {
        get
        {
            string text =
                "WHEN: " + (when ?? "") +
                "\nTARGET: " + (target ?? "") +
                "\nEFFECT: " + (effect ?? "");

            if (!string.IsNullOrWhiteSpace(
                    restrictions))
            {
                text +=
                    "\nRESTRICTIONS: " +
                    restrictions;
            }

            return text;
        }
    }
}

[Serializable]
public sealed class StandardFactionDetachment11
{
    public string name = "";
    public int dp;
    public string ruleName = "";
    public string ruleText = "";
    public int sourcePage;
    public string[] tags = new string[0];
    public StandardFactionEnhancement11[] enhancements =
        new StandardFactionEnhancement11[0];
    public StandardFactionStratagem11[] stratagems =
        new StandardFactionStratagem11[0];
}

[Serializable]
public sealed class StandardFactionPack11Data
{
    public string id = "";
    public string displayName = "";
    public string keyword = "";
    public string version = "";
    public string armyRuleName = "";
    public string armyRuleText = "";
    public StandardFactionDetachment11[] detachments =
        new StandardFactionDetachment11[0];
}

/// <summary>
/// Data loader for standard matched-play faction packs added after v45.
///
/// The faction content lives in JSON TextAssets so adding the next faction no
/// longer requires another 100k-line source patch. Runtime mechanics are
/// supplied by WarboardFactionExtensionHub.
/// </summary>
public static class StandardFactionPack11
{
    private static readonly Dictionary<
        string,
        StandardFactionPack11Data
    > cache =
        new Dictionary<
            string,
            StandardFactionPack11Data
        >(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] ids =
    {
        "tyranids",
        "orks",
        "space_marines"
    };

    public static IReadOnlyList<string> Ids
    {
        get { return ids; }
    }

    public static StandardFactionPack11Data Get(
        string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        StandardFactionPack11Data value;

        if (cache.TryGetValue(id, out value))
            return value;

        TextAsset asset =
            Resources.Load<TextAsset>(
                "FactionPacks11/" + id
            );

        if (asset == null)
        {
            Debug.LogError(
                "WARBOARD: missing faction pack Resources/FactionPacks11/" +
                id +
                ".json"
            );

            cache[id] = null;
            return null;
        }

        try
        {
            value =
                JsonUtility.FromJson<
                    StandardFactionPack11Data
                >(asset.text);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "WARBOARD: failed to parse faction pack " +
                id +
                ": " +
                exception.Message
            );

            value = null;
        }

        cache[id] = value;

        return value;
    }

    public static StandardFactionDetachment11
        FindDetachment(
            StandardFactionPack11Data pack,
            string text)
    {
        if (pack == null ||
            pack.detachments == null ||
            string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        string normalized =
            Normalize(text);

        foreach (
            StandardFactionDetachment11 detachment
            in pack.detachments
                .Where(value => value != null)
                .OrderByDescending(
                    value =>
                        (value.name ?? "").Length))
        {
            string wanted =
                Normalize(detachment.name);

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
                return detachment;
            }
        }

        return null;
    }

    public static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        char[] chars =
            value
                .Replace('\u00A0', ' ')
                .Replace('’', '\'')
                .ToUpperInvariant()
                .Where(
                    c =>
                        char.IsLetterOrDigit(c) ||
                        char.IsWhiteSpace(c) ||
                        c == '\'')
                .ToArray();

        return string.Join(
            " ",
            new string(chars).Split(
                new[]
                {
                    ' ',
                    '\t',
                    '\r',
                    '\n'
                },
                StringSplitOptions
                    .RemoveEmptyEntries
            )
        );
    }

    public static IEnumerable<
        StandardFactionStratagem11
    > StratagemsFor(
        StandardFactionPack11Data pack,
        IEnumerable<string> selectedDetachments)
    {
        if (pack == null ||
            pack.detachments == null ||
            selectedDetachments == null)
        {
            return Enumerable.Empty<
                StandardFactionStratagem11>();
        }

        HashSet<string> selected =
            new HashSet<string>(
                selectedDetachments
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Select(Normalize),
                StringComparer.OrdinalIgnoreCase
            );

        return pack.detachments
            .Where(
                detachment =>
                    detachment != null &&
                    selected.Contains(
                        Normalize(
                            detachment.name)))
            .SelectMany(
                detachment =>
                    detachment.stratagems ??
                    new StandardFactionStratagem11[0]
            );
    }

    public static IEnumerable<
        StandardFactionEnhancement11
    > EnhancementsFor(
        StandardFactionPack11Data pack,
        IEnumerable<string> selectedDetachments)
    {
        if (pack == null ||
            pack.detachments == null ||
            selectedDetachments == null)
        {
            return Enumerable.Empty<
                StandardFactionEnhancement11>();
        }

        HashSet<string> selected =
            new HashSet<string>(
                selectedDetachments
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Select(Normalize),
                StringComparer.OrdinalIgnoreCase
            );

        return pack.detachments
            .Where(
                detachment =>
                    detachment != null &&
                    selected.Contains(
                        Normalize(
                            detachment.name)))
            .SelectMany(
                detachment =>
                    detachment.enhancements ??
                    new StandardFactionEnhancement11[0]
            );
    }
}
