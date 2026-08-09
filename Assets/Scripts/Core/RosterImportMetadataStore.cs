using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Metadata extracted alongside a roster import.
///
/// This deliberately stores neutral roster structure rather than
/// faction-specific rules. Individual faction controllers decide how to
/// interpret values such as detachment names.
/// </summary>
public sealed class RosterImportMetadata
{
    public string GameFactionId = "";
    public string SourceFaction = "";
    public int Revision;

    public string[] ExplicitDetachmentValues =
        new string[0];

    public string[] StructuralSelectionNames =
        new string[0];

    public string[] ImportedUnitIds =
        new string[0];

    public bool MatchesArmy(
        IEnumerable<SquadController> army)
    {
        HashSet<string> expected =
            new HashSet<string>(
                ImportedUnitIds ??
                    new string[0],
                StringComparer.OrdinalIgnoreCase);

        HashSet<string> actual =
            new HashSet<string>(
                (army ??
                    new SquadController[0])
                .Where(
                    unit =>
                        unit != null &&
                        !string.IsNullOrWhiteSpace(
                            unit.UnitId))
                .Select(
                    unit =>
                        unit.UnitId),
                StringComparer.OrdinalIgnoreCase);

        return
            expected.Count > 0 &&
            expected.SetEquals(
                actual);
    }
}

/// <summary>
/// Short-lived metadata store for imported rosters.
///
/// YellowScribeImporter records metadata from the exact JSON payload it
/// already parsed. Faction controllers can then resolve their own selected
/// detachment without re-fetching the roster or inspecting GameController
/// private state.
/// </summary>
public static class RosterImportMetadataStore
{
    private static readonly Dictionary<
        string,
        RosterImportMetadata
    > ByFaction =
        new Dictionary<
            string,
            RosterImportMetadata>(
                StringComparer.OrdinalIgnoreCase);

    private static int nextRevision = 1;

    public static RosterImportMetadata Get(
        string factionId)
    {
        if (string.IsNullOrWhiteSpace(
                factionId))
        {
            return null;
        }

        RosterImportMetadata result;

        return ByFaction.TryGetValue(
            factionId,
            out result)
            ? result
            : null;
    }

    public static void Clear(
        string factionId)
    {
        if (string.IsNullOrWhiteSpace(
                factionId))
        {
            return;
        }

        ByFaction.Remove(
            factionId);
    }

    public static void RecordYellowScribe(
        string factionId,
        string json,
        string sourceFaction,
        IEnumerable<UnitData> units)
    {
        if (string.IsNullOrWhiteSpace(
                factionId))
        {
            return;
        }

        HashSet<string> explicitDetachments =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        HashSet<string> structuralNames =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        object root = null;

        if (!string.IsNullOrWhiteSpace(
                json))
        {
            try
            {
                root =
                    MiniJson.Deserialize(
                        json);
            }
            catch
            {
                root = null;
            }
        }

        Collect(
            root,
            "",
            false,
            explicitDetachments,
            structuralNames);

        RosterImportMetadata metadata =
            new RosterImportMetadata
            {
                GameFactionId =
                    factionId,
                SourceFaction =
                    sourceFaction ?? "",
                Revision =
                    nextRevision++,
                ExplicitDetachmentValues =
                    explicitDetachments
                        .Where(
                            value =>
                                !string.IsNullOrWhiteSpace(
                                    value))
                        .ToArray(),
                StructuralSelectionNames =
                    structuralNames
                        .Where(
                            value =>
                                !string.IsNullOrWhiteSpace(
                                    value))
                        .ToArray(),
                ImportedUnitIds =
                    (units ??
                        new UnitData[0])
                    .Where(
                        unit =>
                            unit != null &&
                            !string.IsNullOrWhiteSpace(
                                unit.id))
                    .Select(
                        unit =>
                            unit.id)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            };

        ByFaction[factionId] =
            metadata;
    }

    private static void Collect(
        object node,
        string keyHint,
        bool insideDetachmentContext,
        HashSet<string> explicitDetachments,
        HashSet<string> structuralNames)
    {
        if (node == null)
            return;

        string text =
            node as string;

        if (text != null)
        {
            string cleaned =
                text.Trim();

            if (string.IsNullOrWhiteSpace(
                    cleaned))
            {
                return;
            }

            // Only a direct string value on a detachment-like key is
            // authoritative. Do not treat every descendant of an object whose
            // key contains "detachment" as a detachment value; that can pull
            // in rule names, labels and unrelated options and manufacture a
            // false "ambiguous" result.
            if (LooksLikeDetachmentKey(
                    keyHint))
            {
                explicitDetachments.Add(
                    cleaned);
            }

            if (LooksLikeStructuralNameKey(
                    keyHint))
            {
                structuralNames.Add(
                    cleaned);
            }

            return;
        }

        Dictionary<string, object> map =
            node as
                Dictionary<string, object>;

        if (map != null)
        {
            foreach (
                KeyValuePair<string, object>
                    pair
                in map)
            {
                Collect(
                    pair.Value,
                    pair.Key,
                    false,
                    explicitDetachments,
                    structuralNames);
            }

            return;
        }

        List<object> list =
            node as List<object>;

        if (list != null)
        {
            foreach (object item in list)
            {
                Collect(
                    item,
                    keyHint,
                    insideDetachmentContext,
                    explicitDetachments,
                    structuralNames);
            }
        }
    }

    private static bool LooksLikeDetachmentKey(
        string key)
    {
        return
            !string.IsNullOrWhiteSpace(
                key) &&
            key.IndexOf(
                "detachment",
                StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool LooksLikeStructuralNameKey(
        string key)
    {
        if (string.IsNullOrWhiteSpace(
                key))
        {
            return false;
        }

        string normalized =
            key.Trim()
                .ToLowerInvariant()
                .Replace("_", "")
                .Replace("-", "")
                .Replace(" ", "");

        switch (normalized)
        {
            case "name":
            case "title":
            case "label":
            case "selection":
            case "selectionname":
            case "forcename":
            case "force":
            case "category":
            case "categoryname":
            case "type":
                return true;
        }

        return false;
    }
}
