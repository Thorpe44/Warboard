using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public sealed class WarboardEnhancementAssignment47
{
    public string FactionId = "";
    public string DetachmentName = "";
    public string EnhancementName = "";
    public string RuleText = "";
    public int Points;
    public SquadController Bearer;
    public bool FromRosterManifest;

    public bool Assigned
    {
        get { return Bearer != null; }
    }
}

/// <summary>
/// Maps roster Enhancements/upgrades to their actual bearer. v46 deliberately
/// refused to guess this relationship; v47 makes it an explicit pre-game/live
/// assignment and exposes the result to all rules hooks.
/// </summary>
public static class WarboardEnhancementRegistry47
{
    private static readonly List<WarboardEnhancementAssignment47>
        assignments =
            new List<WarboardEnhancementAssignment47>();

    public static IReadOnlyList<WarboardEnhancementAssignment47>
        All
    {
        get { return assignments.ToArray(); }
    }

    public static IReadOnlyList<WarboardEnhancementAssignment47>
        ForFaction(
            string factionId)
    {
        return assignments
            .Where(
                value =>
                    value != null &&
                    string.Equals(
                        value.FactionId,
                        factionId,
                        StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public static void SyncFromController(
        StandardFactionGameController controller)
    {
        if (controller == null ||
            controller.Pack == null ||
            string.IsNullOrWhiteSpace(
                controller.FactionId))
        {
            return;
        }

        WarboardRosterManifest manifest =
            RosterTextManifestStore.Get(
                controller.FactionId);

        HashSet<string> manifestNames =
            new HashSet<string>(
                manifest != null
                ? manifest.Enhancements
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Select(Normalize)
                : Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase
            );

        foreach (string detachmentName
            in controller.SelectedDetachments)
        {
            StandardFactionDetachment11 detachment =
                controller.GetDetachment(
                    detachmentName);

            if (detachment == null ||
                detachment.enhancements == null)
            {
                continue;
            }

            foreach (StandardFactionEnhancement11 enhancement
                in detachment.enhancements)
            {
                if (enhancement == null)
                    continue;

                bool inManifest =
                    manifestNames.Contains(
                        Normalize(
                            enhancement.name));

                WarboardEnhancementAssignment47 existing =
                    Find(
                        controller.FactionId,
                        enhancement.name);

                if (existing != null)
                {
                    existing.DetachmentName =
                        detachment.name ?? "";
                    existing.RuleText =
                        enhancement.rule ?? "";
                    existing.Points =
                        enhancement.points;
                    existing.FromRosterManifest =
                        existing.FromRosterManifest ||
                        inManifest;
                    continue;
                }

                if (!inManifest)
                    continue;

                assignments.Add(
                    new WarboardEnhancementAssignment47
                    {
                        FactionId = controller.FactionId,
                        DetachmentName = detachment.name ?? "",
                        EnhancementName = enhancement.name ?? "",
                        RuleText = enhancement.rule ?? "",
                        Points = enhancement.points,
                        FromRosterManifest = true
                    }
                );
            }
        }

        // Remove manifest-derived entries that no longer exist in the roster,
        // but preserve explicit manual registrations until the user removes them.
        assignments.RemoveAll(
            value =>
                value != null &&
                string.Equals(
                    value.FactionId,
                    controller.FactionId,
                    StringComparison.OrdinalIgnoreCase) &&
                value.FromRosterManifest &&
                !manifestNames.Contains(
                    Normalize(
                        value.EnhancementName))
        );
    }

    public static WarboardEnhancementAssignment47 RegisterManual(
        StandardFactionGameController controller,
        StandardFactionDetachment11 detachment,
        StandardFactionEnhancement11 enhancement)
    {
        if (controller == null ||
            detachment == null ||
            enhancement == null)
        {
            return null;
        }

        WarboardEnhancementAssignment47 existing =
            Find(
                controller.FactionId,
                enhancement.name);

        if (existing != null)
            return existing;

        existing =
            new WarboardEnhancementAssignment47
            {
                FactionId = controller.FactionId,
                DetachmentName = detachment.name ?? "",
                EnhancementName = enhancement.name ?? "",
                RuleText = enhancement.rule ?? "",
                Points = enhancement.points,
                FromRosterManifest = false
            };

        assignments.Add(existing);
        return existing;
    }

    public static bool RemoveManual(
        string factionId,
        string enhancementName)
    {
        WarboardEnhancementAssignment47 value =
            Find(factionId, enhancementName);

        if (value == null ||
            value.FromRosterManifest)
        {
            return false;
        }

        return assignments.Remove(value);
    }


    public static bool IsEligibleBearer(
        WarboardEnhancementAssignment47 assignment,
        SquadController unit,
        out string reason)
    {
        reason = "";

        if (assignment == null ||
            unit == null)
        {
            reason = "No Enhancement/unit was supplied.";
            return false;
        }

        if (!string.Equals(
                assignment.FactionId,
                unit.FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            reason = "The bearer must belong to the same army as the Enhancement.";
            return false;
        }

        string text =
            NormalizeText(
                assignment.RuleText);

        Match restriction =
            Regex.Match(
                text,
                @"(?:^|[.!?]\s+)(?<kind>[A-Z0-9 '/\-]+?)\s+(?:MODEL|UNIT)\s+ONLY\.",
                RegexOptions.IgnoreCase);

        if (!restriction.Success)
        {
            // The source rule does not expose a safely machine-readable bearer
            // restriction. Do not invent one.
            return true;
        }

        string wanted =
            restriction.Groups["kind"]
                .Value
                .Trim();

        string[] alternatives =
            Regex.Split(
                wanted,
                @"\s+OR\s+|/",
                RegexOptions.IgnoreCase);

        foreach (string alternative in alternatives)
        {
            if (BearerMatchesRestriction(
                    unit,
                    alternative))
            {
                return true;
            }
        }

        reason =
            assignment.EnhancementName +
            " requires: " +
            wanted +
            ".";

        return false;
    }

    private static bool BearerMatchesRestriction(
        SquadController unit,
        string restriction)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(
                restriction))
        {
            return false;
        }

        string wanted =
            restriction.Trim();

        if (unit.HasKeyword(wanted) ||
            NormalizeText(
                unit.DisplayName)
                .Contains(wanted))
        {
            return true;
        }

        // Multi-keyword restrictions such as "ADEPTUS ASTARTES PSYKER"
        // are common. Preserve the faction keyword as one compound, then
        // require every remaining token/compound.
        List<string> parts =
            new List<string>();

        string remainder = wanted;

        string[] compounds =
        {
            "ADEPTUS ASTARTES",
            "BEAST SNAGGA",
            "BIG MEK",
            "WARBOSS IN MEGA ARMOUR",
            "BEASTBOSS ON SQUIGOSAUR",
            "WINGED TYRANID PRIME",
            "TYRANID PRIME WITH LASH WHIP",
            "VON RYAN'S LEAPERS",
            "NORN ASSIMILATOR",
            "NORN EMISSARY"
        };

        foreach (string compound in compounds)
        {
            if (remainder.Contains(compound))
            {
                parts.Add(compound);
                remainder =
                    remainder.Replace(
                        compound,
                        " "
                    );
            }
        }

        parts.AddRange(
            remainder.Split(
                new[] { ' ' },
                StringSplitOptions
                    .RemoveEmptyEntries));

        if (parts.Count == 0)
            return true;

        string display =
            NormalizeText(
                unit.DisplayName);

        return parts.All(
            part =>
                unit.HasKeyword(part) ||
                display.Contains(part));
    }

    public static void AssignBearer(
        WarboardEnhancementAssignment47 assignment,
        SquadController bearer)
    {
        if (assignment == null)
            return;

        if (bearer != null)
        {
            string reason;

            if (!IsEligibleBearer(
                    assignment,
                    bearer,
                    out reason))
            {
                GameController rejectedGame =
                    GameController.Current;

                if (rejectedGame != null)
                {
                    rejectedGame.StandardSetStatus(
                        reason
                    );
                }

                return;
            }
        }

        assignment.Bearer = bearer;

        GameController game =
            GameController.Current;

        if (game != null)
        {
            game.StandardLog(
                "ENHANCEMENT",
                assignment.EnhancementName,
                bearer != null
                ? "Assigned to " +
                  bearer.DisplayName +
                  "."
                : "Bearer assignment cleared."
            );
        }
    }

    public static WarboardEnhancementAssignment47 Find(
        string factionId,
        string enhancementName)
    {
        string wanted = Normalize(enhancementName);

        return assignments.FirstOrDefault(
            value =>
                value != null &&
                string.Equals(
                    value.FactionId,
                    factionId,
                    StringComparison.OrdinalIgnoreCase) &&
                Normalize(value.EnhancementName) ==
                    wanted
        );
    }

    public static bool UnitContainsBearer(
        SquadController unit,
        string enhancementName)
    {
        if (unit == null)
            return false;

        SquadController action =
            unit.JoinedActionController();

        return assignments.Any(
            value =>
                value != null &&
                value.Bearer != null &&
                Normalize(value.EnhancementName) ==
                    Normalize(enhancementName) &&
                value.Bearer
                    .JoinedActionController() ==
                    action
        );
    }

    public static bool ModelOwnerIsBearer(
        ModelToken model,
        string enhancementName)
    {
        if (model == null ||
            model.Squad == null)
        {
            return false;
        }

        SquadController owner = model.Squad;

        return assignments.Any(
            value =>
                value != null &&
                value.Bearer != null &&
                Normalize(value.EnhancementName) ==
                    Normalize(enhancementName) &&
                value.Bearer == owner
        );
    }

    public static IEnumerable<WarboardEnhancementAssignment47>
        ApplicableToUnit(
            SquadController unit)
    {
        if (unit == null)
            return Enumerable.Empty<
                WarboardEnhancementAssignment47>();

        SquadController action =
            unit.JoinedActionController();

        return assignments
            .Where(
                value =>
                    value != null &&
                    value.Bearer != null &&
                    value.Bearer
                        .JoinedActionController() ==
                        action)
            .ToArray();
    }

    public static IEnumerable<WarboardEnhancementAssignment47>
        ApplicableToAttack(
            SquadController attacker,
            ModelToken shooter)
    {
        if (attacker == null)
            return Enumerable.Empty<
                WarboardEnhancementAssignment47>();

        SquadController action =
            attacker.JoinedActionController();

        return assignments
            .Where(
                value =>
                {
                    if (value == null ||
                        value.Bearer == null ||
                        value.Bearer
                            .JoinedActionController() !=
                            action)
                    {
                        return false;
                    }

                    string text =
                        NormalizeText(
                            value.RuleText);

                    bool bearerOnly =
                        text.Contains(
                            "WEAPONS EQUIPPED BY THE BEARER") ||
                        text.Contains(
                            "THIS MODEL'S") ||
                        text.Contains(
                            "THIS MODELS") ||
                        text.Contains(
                            "THE BEARER'S MELEE") ||
                        text.Contains(
                            "THE BEARERS MELEE") ||
                        text.Contains(
                            "THE BEARER'S RANGED") ||
                        text.Contains(
                            "THE BEARERS RANGED");

                    return
                        !bearerOnly ||
                        (shooter != null &&
                         shooter.Squad ==
                            value.Bearer);
                })
            .ToArray();
    }

    public static int ParsedFeelNoPain(
        SquadController unit,
        int existing,
        string label)
    {
        int best = existing;

        if (unit == null)
            return best;

        SquadController actualOwner = unit;
        SquadController action =
            unit.JoinedActionController();

        foreach (WarboardEnhancementAssignment47 value
            in ApplicableToUnit(unit))
        {
            string text =
                NormalizeText(
                    value.RuleText);

            MatchCollection matches =
                Regex.Matches(
                    text,
                    @"FEEL NO PAIN\s+(\d)\+",
                    RegexOptions.IgnoreCase);

            if (matches.Count == 0)
                continue;

            bool bearerOnly =
                text.Contains(
                    "THE BEARER HAS THE FEEL NO PAIN") ||
                text.Contains(
                    "THE BEARER HAS FEEL NO PAIN");

            bool wholeUnit =
                text.Contains(
                    "MODELS IN THAT UNIT HAVE THE FEEL NO PAIN") ||
                text.Contains(
                    "MODELS IN THE BEARER'S UNIT HAVE THE FEEL NO PAIN") ||
                text.Contains(
                    "MODELS IN THE BEARERS UNIT HAVE THE FEEL NO PAIN");

            if (bearerOnly &&
                value.Bearer != actualOwner)
            {
                continue;
            }

            if (wholeUnit &&
                (value.Bearer == null ||
                 value.Bearer
                    .JoinedActionController() != action))
            {
                continue;
            }

            bool objectiveCondition =
                text.Contains(
                    "WHILE THEY ARE WITHIN RANGE OF AN OBJECTIVE MARKER YOU CONTROL");

            if (objectiveCondition)
            {
                GameController game =
                    GameController.Current;

                if (game == null ||
                    !game.StandardUnitWithinControlledObjective(
                        action,
                        action.FactionId))
                {
                    continue;
                }
            }

            // Once-per-battle and attack-type FNP activations need an actual
            // activation state. Never turn those card clauses into passive
            // protection just because the text contains an FNP number.
            if ((text.Contains("ONCE PER BATTLE") ||
                 text.Contains("AGAINST PSYCHIC ATTACKS")) &&
                !bearerOnly &&
                !wholeUnit)
            {
                continue;
            }

            string enhancementName =
                Normalize(
                    value.EnhancementName);

            if (enhancementName ==
                    Normalize(
                        "ADAPTIVE BIOLOGY") &&
                value.Bearer == actualOwner &&
                WarboardRuleStateStore47
                    .HasUnitFlag(
                        "ADAPTIVE_BIOLOGY_FNP4",
                        actualOwner))
            {
                if (best <= 0 || 4 < best)
                    best = 4;

                continue;
            }

            // For mixed bearer rules such as Iron Resolve, the first FNP is
            // the always-on bearer value; later values belong to the activated
            // unit-wide clause and are not applied until that activation exists.
            int limit =
                bearerOnly
                ? 1
                : matches.Count;

            for (int i = 0;
                 i < limit;
                 i++)
            {
                int parsed;

                if (!int.TryParse(
                        matches[i].Groups[1].Value,
                        out parsed))
                {
                    continue;
                }

                if (best <= 0 || parsed < best)
                    best = parsed;
            }
        }

        return best;
    }

    public static string NormalizeText(
        string value)
    {
        return (value ?? "")
            .Replace('\u00A0', ' ')
            .Replace('’', '\'')
            .ToUpperInvariant();
    }

    private static string Normalize(
        string value)
    {
        return StandardFactionPack11.Normalize(
            value ?? ""
        );
    }
}
