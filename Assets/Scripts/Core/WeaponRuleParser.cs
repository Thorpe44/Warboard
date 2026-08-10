using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public struct AntiWeaponRule
{
    public string targetKeyword;
    public int criticalThreshold;

    public AntiWeaponRule(
        string keyword,
        int threshold)
    {
        targetKeyword = keyword;
        criticalThreshold = threshold;
    }
}

public static class WeaponRuleParser
{
    public static bool Has(
        WeaponData weapon,
        string ruleName)
    {
        if (weapon == null ||
            string.IsNullOrWhiteSpace(
                ruleName))
        {
            return false;
        }

        string wanted =
            NormalizeRuleName(
                ruleName
            );

        foreach (string keyword
            in weapon.keywords ??
               new string[0])
        {
            string normalized =
                NormalizeRuleName(
                    keyword
                );

            if (normalized == wanted ||
                normalized.StartsWith(
                    wanted + "_",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        string raw =
            NormalizeText(
                weapon.rawAbilities
            );

        string words =
            wanted.Replace('_', ' ');

        return raw.IndexOf(
            words,
            StringComparison.OrdinalIgnoreCase
        ) >= 0;
    }

    public static int GetValue(
        WeaponData weapon,
        string ruleName,
        int fallback)
    {
        if (weapon == null)
            return fallback;

        string wanted =
            NormalizeRuleName(
                ruleName
            );

        foreach (string keyword
            in weapon.keywords ??
               new string[0])
        {
            string normalized =
                NormalizeRuleName(
                    keyword
                );

            string prefix =
                wanted + "_";

            if (!normalized.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string suffix =
                normalized.Substring(
                    prefix.Length
                );

            int parsed;

            if (int.TryParse(
                suffix,
                out parsed))
            {
                return parsed;
            }
        }

        string raw =
            NormalizeText(
                weapon.rawAbilities
            );

        string words =
            Regex.Escape(
                wanted.Replace('_', ' ')
            );

        Match match =
            Regex.Match(
                raw,
                @"\b" +
                words +
                @"\s*(\d+)",
                RegexOptions.IgnoreCase
            );

        int value;

        if (match.Success &&
            int.TryParse(
                match.Groups[1].Value,
                out value))
        {
            return value;
        }

        return fallback;
    }

    public static IReadOnlyList<AntiWeaponRule>
        GetAntiRules(
            WeaponData weapon)
    {
        List<AntiWeaponRule> result =
            new List<AntiWeaponRule>();

        if (weapon == null)
            return result;

        foreach (string keyword
            in weapon.keywords ??
               new string[0])
        {
            string normalized =
                NormalizeRuleName(
                    keyword
                );

            if (!normalized.StartsWith(
                "anti_",
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] pieces =
                normalized.Split(
                    new[] { '_' },
                    StringSplitOptions
                        .RemoveEmptyEntries
                );

            if (pieces.Length < 3)
                continue;

            int threshold;

            if (!int.TryParse(
                pieces[
                    pieces.Length - 1
                ],
                out threshold))
            {
                continue;
            }

            string target =
                string.Join(
                    "_",
                    pieces
                        .Skip(1)
                        .Take(
                            pieces.Length - 2
                        )
                        .ToArray()
                );

            AddAnti(
                result,
                target,
                threshold
            );
        }

        string raw =
            NormalizeText(
                weapon.rawAbilities
            );

        MatchCollection matches =
            Regex.Matches(
                raw,
                @"\banti[\s\-]+([a-z][a-z0-9\s\-]*?)\s+([2-6])\+",
                RegexOptions.IgnoreCase
            );

        foreach (Match match in matches)
        {
            string target =
                NormalizeRuleName(
                    match.Groups[1].Value
                );

            int threshold;

            if (int.TryParse(
                match.Groups[2].Value,
                out threshold))
            {
                AddAnti(
                    result,
                    target,
                    threshold
                );
            }
        }

        return result;
    }

    public static int GetCriticalWoundThreshold(
        WeaponData weapon,
        SquadController target)
    {
        int threshold = 6;

        if (target == null)
            return threshold;

        foreach (AntiWeaponRule anti
            in GetAntiRules(weapon))
        {
            if (!target.HasKeyword(
                anti.targetKeyword))
            {
                continue;
            }

            threshold =
                Math.Min(
                    threshold,
                    anti.criticalThreshold
                );
        }

        return threshold;
    }

    public static string NormalizeRuleName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return "";
        }

        string normalized =
            NormalizeText(value);

        char[] chars =
            normalized
                .Select(
                    c =>
                        char.IsLetterOrDigit(c)
                        ? c
                        : '_'
                )
                .ToArray();

        return string.Join(
            "_",
            new string(chars)
                .Split(
                    new[] { '_' },
                    StringSplitOptions
                        .RemoveEmptyEntries
                )
        );
    }

    private static void AddAnti(
        List<AntiWeaponRule> result,
        string target,
        int threshold)
    {
        if (string.IsNullOrWhiteSpace(
                target))
        {
            return;
        }

        threshold =
            Math.Max(
                2,
                Math.Min(
                    6,
                    threshold
                )
            );

        if (result.Any(
            existing =>
                existing.targetKeyword ==
                    target &&
                existing.criticalThreshold ==
                    threshold))
        {
            return;
        }

        result.Add(
            new AntiWeaponRule(
                target,
                threshold
            )
        );
    }

    private static string NormalizeText(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
            value))
        {
            return "";
        }

        return value
            .ToLowerInvariant()
            .Replace('\u2011', '-')
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('_', ' ')
            .Replace('[', ' ')
            .Replace(']', ' ');
    }
}
