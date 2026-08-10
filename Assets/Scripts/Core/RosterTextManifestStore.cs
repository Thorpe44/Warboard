using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public sealed class WarboardRosterManifest
{
    public string FactionId = "";
    public string FactionKeyword = "";
    public readonly List<string> Detachments =
        new List<string>();
    public string ForceDisposition = "";
    public int TotalArmyPoints;
    public string Warlord = "";
    public readonly List<string> Enhancements =
        new List<string>();
    public int NumberOfUnits;
    public readonly List<string> UnitNames =
        new List<string>();
    public string RawText = "";
    public int Revision;

    public string Summary()
    {
        List<string> parts =
            new List<string>();

        if (Detachments.Count > 0)
        {
            parts.Add(
                string.Join(
                    " + ",
                    Detachments.ToArray()));
        }

        if (!string.IsNullOrWhiteSpace(
                ForceDisposition))
        {
            parts.Add(
                ForceDisposition);
        }

        if (TotalArmyPoints > 0)
            parts.Add(TotalArmyPoints + "pts");

        if (!string.IsNullOrWhiteSpace(
                Warlord))
        {
            parts.Add("Warlord " + Warlord);
        }

        return string.Join(
            "  |  ",
            parts.ToArray());
    }
}

/// <summary>
/// Parses the plain-text roster export produced by New Recruit/BattleScribe
/// style roster views. This manifest is configuration authority only; the
/// YellowScribe import can continue to provide detailed datasheet/weapon
/// profiles.
/// </summary>
public static class RosterTextManifestStore
{
    private static readonly Dictionary<
        string,
        WarboardRosterManifest
    > manifests =
        new Dictionary<
            string,
            WarboardRosterManifest
        >(StringComparer.OrdinalIgnoreCase);

    private static int nextRevision = 1;

    private static readonly Regex pointsRegex =
        new Regex(
            @"(?<points>\d+)\s*pts?",
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled);

    private static readonly Regex unitRegex =
        new Regex(
            @"^(?:Char\d+\s*:\s*)?(?<count>\d+)x\s+(?<name>.+?)\s+\((?<points>\d+)\s*pts?\)",
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled);

    public static WarboardRosterManifest Get(
        string factionId)
    {
        WarboardRosterManifest manifest;

        return
            !string.IsNullOrWhiteSpace(factionId) &&
            manifests.TryGetValue(
                factionId,
                out manifest)
            ? manifest
            : null;
    }

    public static bool TrySet(
        string factionId,
        string text,
        out WarboardRosterManifest manifest,
        out string error)
    {
        manifest = null;
        error = "";

        if (string.IsNullOrWhiteSpace(
                factionId))
        {
            error = "No player/faction slot was supplied.";
            return false;
        }

        if (!TryParse(
                text,
                out manifest,
                out error))
        {
            return false;
        }

        manifest.FactionId = factionId;
        manifest.Revision = nextRevision++;

        manifests[factionId] = manifest;

        return true;
    }

    public static void Clear(
        string factionId)
    {
        if (string.IsNullOrWhiteSpace(factionId))
            return;

        manifests.Remove(factionId);
    }

    public static bool TryParse(
        string text,
        out WarboardRosterManifest manifest,
        out string error)
    {
        manifest =
            new WarboardRosterManifest();

        error = "";

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Paste the roster text first.";
            return false;
        }

        manifest.RawText = text;

        string normalizedText =
            text
                .Replace('\u00A0', ' ')
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

        string[] lines =
            normalizedText.Split('\n');

        foreach (string rawLine in lines)
        {
            string line =
                CleanLine(rawLine);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string value;

            if (TryHeader(
                    line,
                    "FACTION KEYWORD",
                    out value))
            {
                manifest.FactionKeyword = value;
                continue;
            }

            if (TryHeader(
                    line,
                    "DETACHMENT",
                    out value))
            {
                foreach (string detachment
                    in SplitDetachmentValue(value))
                {
                    manifest.Detachments.Add(
                        detachment);
                }

                continue;
            }

            if (TryHeader(
                    line,
                    "FORCE DISPOSITION",
                    out value))
            {
                manifest.ForceDisposition = value;
                continue;
            }

            if (TryHeader(
                    line,
                    "TOTAL ARMY POINTS",
                    out value))
            {
                manifest.TotalArmyPoints =
                    ParsePoints(value);
                continue;
            }

            if (TryHeader(
                    line,
                    "WARLORD",
                    out value))
            {
                manifest.Warlord =
                    StripCharacterPrefix(value);
                continue;
            }

            if (TryHeader(
                    line,
                    "ENHANCEMENT",
                    out value))
            {
                string enhancement =
                    value.Trim();

                if (!string.IsNullOrWhiteSpace(
                        enhancement) &&
                    enhancement != "-")
                {
                    manifest.Enhancements.Add(
                        enhancement);
                }

                continue;
            }

            if (TryHeader(
                    line,
                    "NUMBER OF UNITS",
                    out value))
            {
                manifest.NumberOfUnits =
                    ParseInteger(value);
                continue;
            }

            Match unit =
                unitRegex.Match(line);

            if (unit.Success)
            {
                string unitName =
                    unit.Groups["name"]
                        .Value
                        .Trim();

                if (!string.IsNullOrWhiteSpace(
                        unitName))
                {
                    manifest.UnitNames.Add(
                        unitName);
                }
            }
        }

        if (manifest.Detachments.Count == 0 &&
            string.IsNullOrWhiteSpace(
                manifest.FactionKeyword) &&
            manifest.TotalArmyPoints <= 0 &&
            manifest.UnitNames.Count == 0)
        {
            error =
                "That text does not look like a supported roster export.";

            manifest = null;
            return false;
        }

        return true;
    }

    private static string CleanLine(
        string line)
    {
        if (line == null)
            return "";

        string result =
            line
                .Replace('\u00A0', ' ')
                .Trim();

        while (result.StartsWith(
                   "+",
                   StringComparison.Ordinal) ||
               result.StartsWith(
                   "-",
                   StringComparison.Ordinal))
        {
            result =
                result.Substring(1)
                    .TrimStart();
        }

        return result;
    }

    private static bool TryHeader(
        string line,
        string wanted,
        out string value)
    {
        value = "";

        int colon = line.IndexOf(':');

        if (colon < 0)
            return false;

        string key =
            line.Substring(0, colon)
                .Trim();

        if (!string.Equals(
                key,
                wanted,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value =
            line.Substring(colon + 1)
                .Trim();

        return true;
    }

    private static IEnumerable<string>
        SplitDetachmentValue(
            string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            yield break;

        string[] pieces =
            Regex.Split(
                value.Trim(),
                @"\s*(?:;|\||,|\s\+\s)\s*");

        foreach (string piece in pieces)
        {
            string result = piece.Trim();

            // New Recruit commonly appends the detachment rule in brackets,
            // e.g. "Devoted of Ynnead (Strength From Death)". Strip that
            // suffix per detachment so a multi-detachment line remains intact.
            int bracket = result.IndexOf(" (");

            if (bracket > 0)
            {
                result =
                    result.Substring(0, bracket)
                        .Trim();
            }

            if (!string.IsNullOrWhiteSpace(result))
                yield return result;
        }
    }

    private static string StripCharacterPrefix(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return Regex.Replace(
                value.Trim(),
                @"^Char\d+\s*:\s*",
                "",
                RegexOptions.IgnoreCase)
            .Trim();
    }

    private static int ParsePoints(
        string value)
    {
        Match match =
            pointsRegex.Match(
                value ?? "");

        int points;

        return
            match.Success &&
            int.TryParse(
                match.Groups["points"]
                    .Value,
                out points)
            ? points
            : 0;
    }

    private static int ParseInteger(
        string value)
    {
        Match match =
            Regex.Match(
                value ?? "",
                @"\d+");

        int result;

        return
            match.Success &&
            int.TryParse(
                match.Value,
                out result)
            ? result
            : 0;
    }
}
