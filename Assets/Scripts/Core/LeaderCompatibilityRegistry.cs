using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class LeaderCompatibilityOverrideEntry
{
    public string leaderName;
    public string[] bodyguardNames;
}

[Serializable]
public class LeaderCompatibilityOverrideCollection
{
    public LeaderCompatibilityOverrideEntry[] entries;
}

public static class LeaderCompatibilityRegistry
{
    private const string ResourcePath =
        "Core/LeaderCompatibilityOverrides";

    private static LeaderCompatibilityOverrideCollection cached;

    public static IReadOnlyList<string>
        GetBodyguardNames(
            string leaderName)
    {
        EnsureLoaded();

        if (cached == null ||
            cached.entries == null ||
            string.IsNullOrWhiteSpace(
                leaderName))
        {
            return new string[0];
        }

        string wanted =
            Normalize(leaderName);

        foreach (
            LeaderCompatibilityOverrideEntry entry
            in cached.entries)
        {
            if (entry == null ||
                Normalize(
                    entry.leaderName
                ) != wanted)
            {
                continue;
            }

            return entry.bodyguardNames ??
                new string[0];
        }

        return new string[0];
    }

    public static bool HasOverride(
        string leaderName)
    {
        return
            GetBodyguardNames(
                leaderName
            ).Count > 0;
    }

    public static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return "";
        }

        char[] chars =
            value
                .ToLowerInvariant()
                .Select(
                    c =>
                        char.IsLetterOrDigit(c)
                        ? c
                        : ' '
                )
                .ToArray();

        return string.Join(
            " ",
            new string(chars)
                .Split(
                    new[] { ' ' },
                    StringSplitOptions
                        .RemoveEmptyEntries
                )
        );
    }

    private static void EnsureLoaded()
    {
        if (cached != null)
            return;

        TextAsset asset =
            Resources.Load<TextAsset>(
                ResourcePath
            );

        if (asset == null)
        {
            cached =
                new LeaderCompatibilityOverrideCollection
                {
                    entries =
                        new LeaderCompatibilityOverrideEntry[0]
                };

            return;
        }

        cached =
            JsonUtility.FromJson<
                LeaderCompatibilityOverrideCollection
            >(asset.text);

        if (cached == null)
        {
            cached =
                new LeaderCompatibilityOverrideCollection
                {
                    entries =
                        new LeaderCompatibilityOverrideEntry[0]
                };
        }
    }
}
