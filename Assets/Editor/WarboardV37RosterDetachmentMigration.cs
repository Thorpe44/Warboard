#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time v37 source migration.
///
/// v37 does not add a runtime bridge. It connects the existing roster importer
/// to the roster metadata store, removes legacy Aeldari detachment guessing /
/// cycling code, and hard-blocks deployment until faction pre-game setup is
/// complete.
/// </summary>
[InitializeOnLoad]
public static class WarboardV37RosterDetachmentMigration
{
    private const string SelfPath =
        "Assets/Editor/WarboardV37RosterDetachmentMigration.cs";

    private const string BackupDirectory =
        "Library/WarboardBackups/V37";

    private const string ReportPath =
        "Library/WarboardV37RosterDetachmentReport.txt";

    private sealed class MethodSpan
    {
        public string Name;
        public int Start;
        public int EndExclusive;
        public string Text;
    }

    static WarboardV37RosterDetachmentMigration()
    {
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        try
        {
            ValidateRequiredFiles();
            BackupSources();

            PatchYellowScribeImporter();
            PatchAeldariRulesSystem();
            PatchDeploymentGate();

            ValidateResult();
            WriteReport();

            AssetDatabase.DeleteAsset(
                SelfPath);

            AssetDatabase.Refresh();

            Debug.Log(
                "[Warboard v37] Roster-driven detachment migration complete. " +
                "Imported roster metadata now drives Aeldari detachment locking, " +
                "legacy guessing/cycling is removed, and deployment is gated on faction setup.");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Warboard v37] Roster-detachment migration failed. " +
                "The migration script was kept so the source can be inspected. " +
                ex);
        }
    }

    private static void ValidateRequiredFiles()
    {
        string[] required =
        {
            "Assets/Scripts/Core/YellowScribeImporter.cs",
            "Assets/Scripts/Core/AeldariRulesSystem.cs",
            "Assets/Scripts/Core/GameController.Setup.cs",
            "Assets/Scripts/Core/GameController.RuntimeApi.cs",
            "Assets/Scripts/Core/FactionControllerSystem.cs",
            "Assets/Scripts/Core/RosterImportMetadataStore.cs",
            "Assets/Scripts/Factions/Aeldari/AeldariGameController.cs"
        };

        foreach (string path in required)
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    "Required v37 source file is missing: " +
                    path);
            }
        }
    }

    private static void BackupSources()
    {
        Directory.CreateDirectory(
            BackupDirectory);

        string[] paths =
        {
            "Assets/Scripts/Core/YellowScribeImporter.cs",
            "Assets/Scripts/Core/AeldariRulesSystem.cs",
            "Assets/Scripts/Core/GameController.Setup.cs"
        };

        foreach (string path in paths)
        {
            string backupName =
                path.Replace('/', '_') +
                ".txt";

            string destination =
                Path.Combine(
                    BackupDirectory,
                    backupName);

            if (!File.Exists(destination))
            {
                File.Copy(
                    path,
                    destination);
            }
        }
    }

    private static void PatchYellowScribeImporter()
    {
        const string path =
            "Assets/Scripts/Core/YellowScribeImporter.cs";

        string source =
            File.ReadAllText(path);

        MethodSpan parse =
            FindTopLevelMethods(source)
                .FirstOrDefault(
                    method =>
                        string.Equals(
                            method.Name,
                            "Parse",
                            StringComparison.Ordinal));

        if (parse == null)
        {
            throw new InvalidOperationException(
                "YellowScribeImporter.Parse was not found.");
        }

        string methodText =
            parse.Text;

        if (!methodText.Contains(
                "RosterImportMetadataStore.Clear("))
        {
            methodText =
                InsertAtMethodStart(
                    methodText,
@"        // v37: clear stale metadata before attempting a new import.
        RosterImportMetadataStore.Clear(
            gameFactionId);
");
        }

        if (!methodText.Contains(
                "RosterImportMetadataStore.RecordYellowScribe("))
        {
            Regex resultReturn =
                new Regex(
                    @"(?m)^(?<indent>\s*)return\s+result\s*;");

            Match match =
                resultReturn.Match(
                    methodText);

            if (!match.Success)
            {
                throw new InvalidOperationException(
                    "YellowScribeImporter.Parse return result statement was not found.");
            }

            string indent =
                match.Groups[
                    "indent"].Value;

            string replacement =
                indent +
                "RosterImportMetadataStore.RecordYellowScribe(" +
                Environment.NewLine +
                indent +
                "    gameFactionId," +
                Environment.NewLine +
                indent +
                "    json," +
                Environment.NewLine +
                indent +
                "    result.SourceFaction," +
                Environment.NewLine +
                indent +
                "    result.Units);" +
                Environment.NewLine +
                Environment.NewLine +
                match.Value;

            methodText =
                resultReturn.Replace(
                    methodText,
                    replacement,
                    1);
        }

        source =
            ReplaceMethodSpan(
                source,
                parse,
                methodText);

        WriteTextIfChanged(
            path,
            source);
    }

    private static void PatchAeldariRulesSystem()
    {
        const string path =
            "Assets/Scripts/Core/AeldariRulesSystem.cs";

        string source =
            File.ReadAllText(path);

        if (!source.Contains(
                "WARBOARD_V37_ROSTER_DRIVEN_DETACHMENT"))
        {
            Regex classDeclaration =
                new Regex(
                    @"public\s+class\s+AeldariRulesSystem\s*\{");

            source =
                classDeclaration.Replace(
                    source,
                    match =>
                        match.Value +
                        Environment.NewLine +
                        "    // WARBOARD_V37_ROSTER_DRIVEN_DETACHMENT",
                    1);
        }

        Regex autoDetectAssignment =
            new Regex(
                @"detachmentByFaction\s*\[\s*faction\s*\]\s*=\s*" +
                @"AutoDetectDefault\s*\(\s*faction\s*,\s*squads\s*\)\s*;",
                RegexOptions.Singleline);

        if (autoDetectAssignment.IsMatch(
                source))
        {
            source =
                autoDetectAssignment.Replace(
                    source,
                    "detachmentByFaction[faction] = AeldariDetachment.Warhost;",
                    1);
        }

        source =
            RemoveNamedMethodIfPresent(
                source,
                "AutoDetectDefault");

        source =
            RemoveNamedMethodIfPresent(
                source,
                "NextDetachment");

        source =
            Regex.Replace(
                source,
                @"(?ms)^[ \t]*private\s+static\s+readonly\s+" +
                @"AeldariDetachment\[\]\s+Order\s*=\s*\{.*?^[ \t]*\};[ \t]*\r?\n",
                "");

        if (source.Contains(
                "AutoDetectDefault(") ||
            source.Contains(
                "NextDetachment("))
        {
            throw new InvalidOperationException(
                "Legacy Aeldari detachment guessing/cycling still remains after v37 patch.");
        }

        WriteTextIfChanged(
            path,
            source);
    }

    private static void PatchDeploymentGate()
    {
        const string path =
            "Assets/Scripts/Core/GameController.Setup.cs";

        string source =
            File.ReadAllText(path);

        MethodSpan deployment =
            FindTopLevelMethods(source)
                .FirstOrDefault(
                    method =>
                        string.Equals(
                            method.Name,
                            "BeginDeployment",
                            StringComparison.Ordinal));

        if (deployment == null)
        {
            throw new InvalidOperationException(
                "GameController.BeginDeployment was not found in GameController.Setup.cs.");
        }

        string patched =
            deployment.Text;

        if (!patched.Contains(
                "EnsureFactionControllersReadyForDeployment()"))
        {
            patched =
                InsertAtMethodStart(
                    patched,
@"        if (!EnsureFactionControllersReadyForDeployment())
        {
            return;
        }

");
        }

        source =
            ReplaceMethodSpan(
                source,
                deployment,
                patched);

        WriteTextIfChanged(
            path,
            source);
    }

    private static void ValidateResult()
    {
        string importer =
            File.ReadAllText(
                "Assets/Scripts/Core/YellowScribeImporter.cs");

        if (!importer.Contains(
                "RosterImportMetadataStore.Clear(") ||
            !importer.Contains(
                "RosterImportMetadataStore.RecordYellowScribe("))
        {
            throw new InvalidOperationException(
                "YellowScribe importer is not connected to the v37 metadata store.");
        }

        string aeldariRules =
            File.ReadAllText(
                "Assets/Scripts/Core/AeldariRulesSystem.cs");

        if (aeldariRules.Contains(
                "AutoDetectDefault(") ||
            aeldariRules.Contains(
                "NextDetachment("))
        {
            throw new InvalidOperationException(
                "Legacy Aeldari detachment inference/cycling remains.");
        }

        string setup =
            File.ReadAllText(
                "Assets/Scripts/Core/GameController.Setup.cs");

        if (!setup.Contains(
                "EnsureFactionControllersReadyForDeployment()"))
        {
            throw new InvalidOperationException(
                "Deployment gate was not installed.");
        }

        string controller =
            File.ReadAllText(
                "Assets/Scripts/Factions/Aeldari/AeldariGameController.cs");

        string[] forbidden =
        {
            "UnityWebRequest",
            "AutoDetectDefault",
            "BeginRosterProbeWhenPossible",
            "ProbeRosterDetachment",
            "ResolveYellowScribeCode"
        };

        foreach (string value in forbidden)
        {
            if (controller.Contains(value))
            {
                throw new InvalidOperationException(
                    "AeldariGameController still contains legacy detachment-loading code: " +
                    value);
            }
        }
    }

    private static void WriteReport()
    {
        StringBuilder report =
            new StringBuilder();

        report.AppendLine(
            "WARBOARD v37 — ROSTER-DRIVEN DETACHMENTS");

        report.AppendLine();

        report.AppendLine(
            "YellowScribeImporter now records detachment-related metadata from the same JSON payload used to import the army.");

        report.AppendLine(
            "AeldariGameController resolves and locks a single unambiguous roster detachment without a second network request.");

        report.AppendLine(
            "If no single detachment can be resolved, the one-time pre-game selector is required.");

        report.AppendLine(
            "BeginDeployment is blocked until every faction pre-game controller reports ready.");

        report.AppendLine(
            "AeldariRulesSystem no longer guesses Devoted of Ynnead from Yvraine/Yncarne, guesses Ghosts from army composition, or exposes runtime NextDetachment cycling.");

        File.WriteAllText(
            ReportPath,
            report.ToString(),
            new UTF8Encoding(false));
    }

    private static string ReplaceMethodSpan(
        string source,
        MethodSpan method,
        string replacement)
    {
        return
            source.Substring(
                0,
                method.Start) +
            replacement.TrimEnd() +
            Environment.NewLine +
            Environment.NewLine +
            source.Substring(
                method.EndExclusive);
    }

    private static string RemoveNamedMethodIfPresent(
        string source,
        string methodName)
    {
        MethodSpan method =
            FindTopLevelMethods(source)
                .FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Name,
                            methodName,
                            StringComparison.Ordinal));

        if (method == null)
            return source;

        return
            source.Remove(
                method.Start,
                method.EndExclusive -
                    method.Start);
    }

    private static string InsertAtMethodStart(
        string method,
        string statement)
    {
        if (method.Contains(
                statement.Trim()))
        {
            return method;
        }

        int open =
            method.IndexOf('{');

        if (open < 0)
            return method;

        return method.Insert(
            open + 1,
            Environment.NewLine +
            statement);
    }

    private static List<MethodSpan>
        FindTopLevelMethods(
            string source)
    {
        List<MethodSpan> result =
            new List<MethodSpan>();

        int[] depth =
            ComputeBraceDepthBefore(
                source);

        Regex methodRegex =
            new Regex(
                @"(?ms)^[ \t]*" +
                @"(?:public|private|protected|internal)\s+" +
                @"(?:(?:static|virtual|override|sealed|async|extern|new|unsafe)\s+)*" +
                @"[A-Za-z_][A-Za-z0-9_<>\[\],\.\?\s]*?\s+" +
                @"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*" +
                @"\((?<params>.*?)\)\s*" +
                @"(?:where\s+[^{}]+)?\{");

        foreach (Match match
            in methodRegex.Matches(source))
        {
            int open =
                match.Index +
                match.Value.LastIndexOf('{');

            if (open < 0 ||
                open >= depth.Length ||
                depth[open] != 1)
            {
                continue;
            }

            int close =
                FindMethodClosingBrace(
                    source,
                    depth,
                    open);

            if (close < 0)
                continue;

            int end =
                close + 1;

            while (end < source.Length &&
                   (source[end] == '\r' ||
                    source[end] == '\n'))
            {
                end++;
            }

            result.Add(
                new MethodSpan
                {
                    Name =
                        match.Groups[
                            "name"].Value,
                    Start =
                        match.Index,
                    EndExclusive =
                        end,
                    Text =
                        source.Substring(
                            match.Index,
                            end -
                                match.Index)
                });
        }

        return result;
    }

    private static int[] ComputeBraceDepthBefore(
        string source)
    {
        int[] depth =
            new int[source.Length];

        int current = 0;
        bool lineComment = false;
        bool blockComment = false;
        bool normalString = false;
        bool verbatimString = false;
        bool charLiteral = false;
        bool escape = false;

        for (int i = 0;
             i < source.Length;
             i++)
        {
            depth[i] = current;

            char c =
                source[i];

            char next =
                i + 1 < source.Length
                ? source[i + 1]
                : '\0';

            if (lineComment)
            {
                if (c == '\n')
                    lineComment = false;

                continue;
            }

            if (blockComment)
            {
                if (c == '*' &&
                    next == '/')
                {
                    blockComment = false;
                    i++;

                    if (i < depth.Length)
                        depth[i] = current;
                }

                continue;
            }

            if (normalString)
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

                if (c == '"')
                    normalString = false;

                continue;
            }

            if (verbatimString)
            {
                if (c == '"' &&
                    next == '"')
                {
                    i++;

                    if (i < depth.Length)
                        depth[i] = current;

                    continue;
                }

                if (c == '"')
                    verbatimString = false;

                continue;
            }

            if (charLiteral)
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

                if (c == '\'')
                    charLiteral = false;

                continue;
            }

            if (c == '/' &&
                next == '/')
            {
                lineComment = true;
                i++;

                if (i < depth.Length)
                    depth[i] = current;

                continue;
            }

            if (c == '/' &&
                next == '*')
            {
                blockComment = true;
                i++;

                if (i < depth.Length)
                    depth[i] = current;

                continue;
            }

            if (c == '@' &&
                next == '"')
            {
                verbatimString = true;
                i++;

                if (i < depth.Length)
                    depth[i] = current;

                continue;
            }

            if (c == '"')
            {
                normalString = true;
                continue;
            }

            if (c == '\'')
            {
                charLiteral = true;
                continue;
            }

            if (c == '{')
                current++;
            else if (c == '}')
                current--;
        }

        return depth;
    }

    private static int FindMethodClosingBrace(
        string source,
        int[] depth,
        int openBrace)
    {
        int wantedDepth =
            depth[openBrace];

        for (int i = openBrace + 1;
             i < source.Length;
             i++)
        {
            if (source[i] == '}' &&
                depth[i] ==
                    wantedDepth + 1)
            {
                return i;
            }
        }

        return -1;
    }

    private static void WriteTextIfChanged(
        string path,
        string content)
    {
        string existing =
            File.Exists(path)
            ? File.ReadAllText(path)
            : "";

        if (existing == content)
            return;

        File.WriteAllText(
            path,
            content,
            new UTF8Encoding(false));
    }
}
#endif
