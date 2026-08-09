#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class WarboardV40FightPhaseCompliance
{
    private const string MigrationPath =
        "Assets/Editor/WarboardV40FightPhaseCompliance.cs";

    private const string Fight11Path =
        "Assets/Scripts/Core/GameController.Fight11.cs";

    private const string Marker =
        "WARBOARD_V40_FIGHT_PHASE_COMPLIANCE";

    private const string ReportPath =
        "Library/WarboardV40FightPhaseReport.txt";

    private const string BackupRoot =
        "Library/WarboardBackups/V40";

    static WarboardV40FightPhaseCompliance()
    {
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Warboard/Developer/Re-run v40 Fight Phase Compliance")]
    private static void RunFromMenu()
    {
        RunOnce();
    }

    private static void RunOnce()
    {
        if (EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunOnce;
            return;
        }

        try
        {
            if (!File.Exists(Fight11Path))
            {
                throw new InvalidOperationException(
                    "v40 requires " + Fight11Path + "."
                );
            }

            string fight11 = File.ReadAllText(Fight11Path);

            if (!fight11.Contains(Marker))
            {
                throw new InvalidOperationException(
                    "v40 Fight11 source marker is missing."
                );
            }

            Directory.CreateDirectory(BackupRoot);

            List<string> touched = new List<string>();

            PatchMethod(
                "BeginFightSequence",
                "        Fight11BeginPileInStep();\n",
                touched);

            PatchMethod(
                "IsEligibleToFightNow",
                "        return Fight11IsEligibleToFightNow(unit);\n",
                touched);

            PatchMethod(
                "EligibleFightUnits",
                "        return Fight11EligibleFightUnits(faction, fightsFirstOnly);\n",
                touched);

            PatchMethod(
                "AnyEligibleFightUnits",
                "        return Fight11AnyEligibleFightUnits();\n",
                touched);

            PatchMethod(
                "ResolveFightSelector",
                "        Fight11ResolveFightSelector(preferredFaction);\n",
                touched);

            PatchMethod(
                "AdvanceFightPriority",
                "        Fight11AdvanceFightPriority(unitThatFought);\n",
                touched);

            PatchMethod(
                "FightPriorityText",
                "        return Fight11FightPriorityText();\n",
                touched);

            PatchMethod(
                "FightStageDestinationLegal",
                "        return Fight11FightStageDestinationLegal(model, destination, out reason);\n",
                touched);

            PatchMethod(
                "CompleteFightPileIn",
                "        Fight11CompletePileIn();\n",
                touched);

            PatchMethod(
                "CompleteFightModelAttack",
                "        Fight11CompleteFightModelAttack(model);\n",
                touched);

            PatchMethod(
                "SkipSelectedFightModel",
                "        Fight11SkipSelectedFightModel();\n",
                touched);

            PatchMethod(
                "CompleteFightAttacks",
                "        Fight11CompleteFightAttacks();\n",
                touched);

            // Old callers used BeginFightConsolidation immediately after a
            // unit attacked. In 11e this now means "finish this unit's Fight
            // step selection"; actual consolidation is phase-wide later.
            PatchMethod(
                "BeginFightConsolidation",
                "        Fight11FinishSelectedFightAttacks();\n",
                touched);

            PatchMethod(
                "CompleteFightConsolidation",
                "        Fight11CompleteConsolidation();\n",
                touched);

            PatchMethod(
                "TryFight",
                "        Fight11TryFight(attacker, target);\n",
                touched);

            PatchMethod(
                "BeginFightActivation",
                "        Fight11BeginNormalFight(attacker, target, false);\n",
                touched);

            PatchFightContextUi(touched);
            PatchNextPhaseGate(touched);

            Validate();
            WriteReport(touched);

            Debug.Log(
                "[Warboard v40] 11e Fight phase compliance installed. " +
                "Unity will compile once more."
            );

            AssetDatabase.DeleteAsset(MigrationPath);
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Warboard v40] Fight phase migration failed. " + ex
            );
        }
    }

    private static void PatchMethod(
        string methodName,
        string body,
        List<string> touched)
    {
        MethodLocation location = FindGameMethod(methodName);
        string source = File.ReadAllText(location.Path);
        string current = source.Substring(
            location.OpenBrace + 1,
            location.CloseBrace - location.OpenBrace - 1
        );

        string normalizedBody = "\n" + body + "    ";

        if (Normalize(current) == Normalize(normalizedBody))
            return;

        Backup(location.Path);

        string replacement =
            source.Substring(0, location.OpenBrace + 1) +
            normalizedBody +
            source.Substring(location.CloseBrace);

        WriteSource(location.Path, replacement);
        AddTouched(touched, location.Path);
    }

    private static void PatchFightContextUi(List<string> touched)
    {
        MethodLocation location = FindGameMethod("DrawContextActionBar");
        string source = File.ReadAllText(location.Path);

        string method = source.Substring(
            location.SignatureStart,
            location.CloseBrace - location.SignatureStart + 1
        );

        if (method.Contains("DrawFight11ContextControls(bar, ref x);"))
            return;

        int relativeIf = method.LastIndexOf(
            "if (phase == Phase.Fight)",
            StringComparison.Ordinal
        );

        if (relativeIf < 0)
        {
            throw new InvalidOperationException(
                "Could not locate Fight UI block in DrawContextActionBar."
            );
        }

        int absoluteIf = location.SignatureStart + relativeIf;
        int open = source.IndexOf('{', absoluteIf);

        if (open < 0 || open > location.CloseBrace)
            throw new InvalidOperationException("Fight UI opening brace not found.");

        int close = FindMatchingBrace(source, open);

        if (close < 0 || close > location.CloseBrace)
            throw new InvalidOperationException("Fight UI closing brace not found.");

        string replacement =
            "if (phase == Phase.Fight)\n" +
            "        {\n" +
            "            DrawFight11ContextControls(bar, ref x);\n" +
            "        }";

        Backup(location.Path);

        source =
            source.Substring(0, absoluteIf) +
            replacement +
            source.Substring(close + 1);

        WriteSource(location.Path, source);
        AddTouched(touched, location.Path);
    }

    private static void PatchNextPhaseGate(List<string> touched)
    {
        MethodLocation location = FindGameMethod("NextPhase");
        string source = File.ReadAllText(location.Path);
        string method = source.Substring(
            location.SignatureStart,
            location.CloseBrace - location.SignatureStart + 1
        );

        if (method.Contains("Fight11CanLeaveFightPhase"))
            return;

        string insert =
            "\n        // v40 / 11e 12: Pile In, Fight and Consolidate are distinct\n" +
            "        // phase-wide steps and cannot be bypassed with NEXT PHASE.\n" +
            "        if (phase == Phase.Fight)\n" +
            "        {\n" +
            "            string fight11Reason;\n" +
            "            if (!Fight11CanLeaveFightPhase(out fight11Reason))\n" +
            "            {\n" +
            "                status = fight11Reason;\n" +
            "                return;\n" +
            "            }\n" +
            "        }\n";

        Backup(location.Path);

        source =
            source.Substring(0, location.OpenBrace + 1) +
            insert +
            source.Substring(location.OpenBrace + 1);

        WriteSource(location.Path, source);
        AddTouched(touched, location.Path);
    }

    private static MethodLocation FindGameMethod(string methodName)
    {
        string core = "Assets/Scripts/Core";
        string[] files = Directory
            .GetFiles(core, "GameController*.cs", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith("GameController.Fight11.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Regex regex = new Regex(
            @"^[ \t]*(?:public|private|protected|internal)\s+(?:static\s+)?[^{};=]*?\b" +
            Regex.Escape(methodName) +
            @"\s*\(",
            RegexOptions.CultureInvariant |
            RegexOptions.Multiline |
            RegexOptions.Singleline
        );

        List<MethodLocation> matches = new List<MethodLocation>();

        foreach (string path in files)
        {
            string source = File.ReadAllText(path);

            foreach (Match match in regex.Matches(source))
            {
                int open = FindMethodOpeningBrace(source, match.Index);
                if (open < 0)
                    continue;

                int close = FindMatchingBrace(source, open);
                if (close < 0)
                    continue;

                matches.Add(
                    new MethodLocation
                    {
                        Path = path,
                        SignatureStart = match.Index,
                        OpenBrace = open,
                        CloseBrace = close
                    }
                );
            }
        }

        if (matches.Count != 1)
        {
            throw new InvalidOperationException(
                "Expected exactly one GameController method named " +
                methodName + ", found " + matches.Count + "."
            );
        }

        return matches[0];
    }

    private static int FindMethodOpeningBrace(string source, int start)
    {
        int paren = source.IndexOf('(', start);
        if (paren < 0)
            return -1;

        int depth = 0;
        bool inString = false;
        bool inChar = false;
        bool escape = false;

        for (int i = paren; i < source.Length; i++)
        {
            char c = source[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if ((inString || inChar) && c == '\\')
            {
                escape = true;
                continue;
            }

            if (!inChar && c == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString && c == '\'')
            {
                inChar = !inChar;
                continue;
            }

            if (inString || inChar)
                continue;

            if (c == '(') depth++;
            else if (c == ')')
            {
                depth--;
                if (depth == 0)
                {
                    int brace = source.IndexOf('{', i + 1);
                    int semicolon = source.IndexOf(';', i + 1);
                    if (semicolon >= 0 && semicolon < brace)
                        return -1;
                    return brace;
                }
            }
        }

        return -1;
    }

    private static int FindMatchingBrace(string source, int open)
    {
        int depth = 0;
        bool inString = false;
        bool inChar = false;
        bool inLineComment = false;
        bool inBlockComment = false;
        bool escape = false;
        bool verbatim = false;

        for (int i = open; i < source.Length; i++)
        {
            char c = source[i];
            char next = i + 1 < source.Length ? source[i + 1] : '\0';

            if (inLineComment)
            {
                if (c == '\n') inLineComment = false;
                continue;
            }

            if (inBlockComment)
            {
                if (c == '*' && next == '/')
                {
                    inBlockComment = false;
                    i++;
                }
                continue;
            }

            if (inString)
            {
                if (verbatim)
                {
                    if (c == '"' && next == '"')
                    {
                        i++;
                        continue;
                    }
                    if (c == '"')
                    {
                        inString = false;
                        verbatim = false;
                    }
                    continue;
                }

                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (c == '\\')
                {
                    escape = true;
                    continue;
                }

                if (c == '"') inString = false;
                continue;
            }

            if (inChar)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }
                if (c == '\\')
                {
                    escape = true;
                    continue;
                }
                if (c == '\'') inChar = false;
                continue;
            }

            if (c == '/' && next == '/')
            {
                inLineComment = true;
                i++;
                continue;
            }

            if (c == '/' && next == '*')
            {
                inBlockComment = true;
                i++;
                continue;
            }

            if (c == '@' && next == '"')
            {
                inString = true;
                verbatim = true;
                i++;
                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == '\'')
            {
                inChar = true;
                continue;
            }

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        return -1;
    }


    private static void WriteSource(string path, string source)
    {
        source = (source ?? "")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", "\r\n");

        File.WriteAllText(path, source, new UTF8Encoding(false));
    }

    private static void Backup(string path)
    {
        string safe = path.Replace('/', '_').Replace('\\', '_');
        string backup = Path.Combine(BackupRoot, safe + ".txt");

        if (!File.Exists(backup))
            File.Copy(path, backup, true);
    }

    private static void AddTouched(List<string> touched, string path)
    {
        if (!touched.Contains(path))
            touched.Add(path);
    }

    private static void Validate()
    {
        string[] requiredMethods =
        {
            "BeginFightSequence",
            "TryFight",
            "CompleteFightPileIn",
            "CompleteFightAttacks",
            "CompleteFightConsolidation",
            "ResolveFightSelector"
        };

        foreach (string method in requiredMethods)
        {
            MethodLocation location = FindGameMethod(method);
            string source = File.ReadAllText(location.Path);
            string body = source.Substring(
                location.OpenBrace,
                location.CloseBrace - location.OpenBrace + 1
            );

            if (!body.Contains("Fight11"))
            {
                throw new InvalidOperationException(
                    "v40 validation failed: " + method +
                    " does not delegate to Fight11."
                );
            }
        }

        MethodLocation ui = FindGameMethod("DrawContextActionBar");
        string uiSource = File.ReadAllText(ui.Path);
        if (!uiSource.Contains("DrawFight11ContextControls(bar, ref x);"))
            throw new InvalidOperationException("v40 Fight UI was not installed.");

        MethodLocation next = FindGameMethod("NextPhase");
        string nextSource = File.ReadAllText(next.Path);
        if (!nextSource.Contains("Fight11CanLeaveFightPhase"))
            throw new InvalidOperationException("v40 Fight phase exit gate was not installed.");
    }

    private static void WriteReport(List<string> touched)
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("Warboard v40 - 11e Fight Phase Compliance");
        report.AppendLine(DateTime.Now.ToString("u"));
        report.AppendLine();
        report.AppendLine("Installed phase-wide steps:");
        report.AppendLine("1. Pile In - active player all moves, then opponent");
        report.AppendLine("2. Fight - Fights First then Remaining, alternating");
        report.AppendLine("3. Consolidate - active player all moves, then opponent");
        report.AppendLine("4. Normal Fight / Overrun Fight selection");
        report.AppendLine("5. Ongoing / Engaging / Objective Consolidation");
        report.AppendLine("6. New Foes To Face forced fights from Engaging Consolidation");
        report.AppendLine();
        report.AppendLine("Touched files:");

        foreach (string path in touched)
            report.AppendLine("- " + path);

        File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(false));
    }

    private static string Normalize(string value)
    {
        return Regex.Replace(value ?? "", @"\s+", " ").Trim();
    }

    private sealed class MethodLocation
    {
        public string Path;
        public int SignatureStart;
        public int OpenBrace;
        public int CloseBrace;
    }
}
#endif
