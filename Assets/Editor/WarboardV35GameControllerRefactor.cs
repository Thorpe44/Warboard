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
/// One-time structural refactor for Warboard v35.
///
/// This runs inside the user's project because the authoritative GameController
/// source is local there. It converts GameController into a partial class,
/// physically moves methods into focused partial files, creates a small runtime
/// API for faction controllers, removes the temporary v34 CoreEventBridge, and
/// cleans the Aeldari controller's reflection dependency.
///
/// The transformation is source-only: method bodies are moved verbatim, so
/// gameplay behaviour is preserved while the giant controller is decomposed.
/// </summary>
[InitializeOnLoad]
public static class WarboardV35GameControllerRefactor
{
    private const string MainPath =
        "Assets/Scripts/Core/GameController.cs";

    private const string Marker =
        "WARBOARD_V35_GAMECONTROLLER_REFACTORED";

    private const string BackupPath =
        "Library/WarboardBackups/GameController_PreV35.cs.txt";

    private const string ReportPath =
        "Library/WarboardV35RefactorReport.txt";

    private static readonly string[] GeneratedModulePaths =
    {
        "Assets/Scripts/Core/GameController.Core.cs",
        "Assets/Scripts/Core/GameController.Setup.cs",
        "Assets/Scripts/Core/GameController.Movement.cs",
        "Assets/Scripts/Core/GameController.Charge.cs",
        "Assets/Scripts/Core/GameController.Combat.cs",
        "Assets/Scripts/Core/GameController.Fight.cs",
        "Assets/Scripts/Core/GameController.Missions.cs",
        "Assets/Scripts/Core/GameController.Rules.cs",
        "Assets/Scripts/Core/GameController.Traditional.cs",
        "Assets/Scripts/Core/GameController.UI.cs",
        "Assets/Scripts/Core/GameController.RuntimeApi.cs"
    };

    private sealed class MethodSpan
    {
        public string Name;
        public int Start;
        public int EndExclusive;
        public string Text;
        public string Module;
    }

    static WarboardV35GameControllerRefactor()
    {
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Warboard/Developer/Re-run v35 GameController Refactor")]
    private static void RunFromMenu()
    {
        RunOnce(true);
    }

    private static void RunOnce()
    {
        RunOnce(false);
    }

    private static void RunOnce(
        bool force)
    {
        try
        {
            if (!File.Exists(MainPath))
            {
                Debug.LogWarning(
                    "[Warboard v35] GameController.cs was not found. Refactor skipped.");

                return;
            }

            string source =
                File.ReadAllText(MainPath);

            bool alreadyDone =
                source.Contains(Marker) &&
                GeneratedModulePaths.All(
                    File.Exists);

            if (alreadyDone &&
                !force)
            {
                CleanupLegacyBridge();
                return;
            }

            CreateBackupIfNeeded(
                source);

            source =
                MakeGameControllerPartial(
                    source);

            source =
                ReplaceLegacyVersionLabels(
                    source);

            if (Regex.IsMatch(
                    source,
                    "\"WARBOARD v[0-9]"))
            {
                throw new InvalidOperationException(
                    "Safety stop: a hard-coded Warboard version label remains in GameController.");
            }

            source =
                RemoveLegacyDetachmentCycling(
                    source);

            Dictionary<string, List<MethodSpan>>
                modules;

            string refactoredMain =
                ExtractMethodsIntoModules(
                    source,
                    out modules);

            ValidateRefactor(
                source,
                refactoredMain,
                modules);

            refactoredMain =
                AddRefactorMarker(
                    refactoredMain);

            WriteTextIfChanged(
                MainPath,
                refactoredMain);

            string usingBlock =
                BuildUsingBlock(source);

            string[] moduleNames =
            {
                "Core",
                "Setup",
                "Movement",
                "Charge",
                "Combat",
                "Fight",
                "Missions",
                "Rules",
                "Traditional",
                "UI"
            };

            foreach (string moduleName
                in moduleNames)
            {
                List<MethodSpan> methodList;

                if (!modules.TryGetValue(
                        moduleName,
                        out methodList))
                {
                    methodList =
                        new List<MethodSpan>();
                }

                WritePartialModule(
                    moduleName,
                    methodList,
                    usingBlock);
            }

            WriteRuntimeApi();

            CleanupFactionControllerHost();
            CleanupFactionControllerRuntime();
            CleanupAeldariController();
            CleanupAeldariSetupUI();
            CleanupAeldariRulesSystem();
            CleanupLegacyBridge();

            WriteReport(
                source,
                refactoredMain,
                modules);

            AssetDatabase.Refresh();

            Debug.Log(
                "[Warboard v35] GameController refactor complete. " +
                "The monolith has been split into partial modules and the v34 event bridge was removed.");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Warboard v35] Refactor failed. " +
                ex);
        }
    }

    private static void CreateBackupIfNeeded(
        string source)
    {
        if (File.Exists(BackupPath))
            return;

        string directory =
            Path.GetDirectoryName(
                BackupPath);

        if (!string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        File.WriteAllText(
            BackupPath,
            source,
            Encoding.UTF8);
    }

    private static string MakeGameControllerPartial(
        string source)
    {
        if (Regex.IsMatch(
            source,
            @"public\s+partial\s+class\s+GameController\s*:\s*MonoBehaviour"))
        {
            return source;
        }

        Regex declaration =
            new Regex(
                @"public\s+class\s+GameController\s*:\s*MonoBehaviour");

        return declaration.Replace(
            source,
            "public partial class GameController : MonoBehaviour",
            1);
    }

    private static string ReplaceLegacyVersionLabels(
        string source)
    {
        source =
            Regex.Replace(
                source,
                "\"WARBOARD v[0-9]+(?:\\.[0-9]+)? \\| \"",
                "\"WARBOARD \" + WarboardBuildInfo.CurrentVersion + \" | \"");

        source =
            Regex.Replace(
                source,
                "\"WARBOARD v[0-9]+(?:\\.[0-9]+)?\\s+•\\s+\"",
                "\"WARBOARD \" + WarboardBuildInfo.CurrentVersion + \"   •   \"");

        return source;
    }

    private static string RemoveLegacyDetachmentCycling(
        string source)
    {
        const string needle =
            "aeldariRules.NextDetachment(";

        int guard = 0;

        while (source.IndexOf(
                   needle,
                   StringComparison.Ordinal) >= 0 &&
               guard < 10)
        {
            guard++;

            int callIndex =
                source.IndexOf(
                    needle,
                    StringComparison.Ordinal);

            int ifStart =
                source.LastIndexOf(
                    "if (GUI.Button",
                    callIndex,
                    StringComparison.Ordinal);

            if (ifStart < 0)
                break;

            int lineStart =
                source.LastIndexOf(
                    '\n',
                    ifStart);

            lineStart =
                lineStart < 0
                ? 0
                : lineStart + 1;

            int openBrace =
                source.IndexOf(
                    '{',
                    ifStart);

            if (openBrace < 0 ||
                openBrace > callIndex)
            {
                break;
            }

            int closeBrace =
                FindMatchingBrace(
                    source,
                    openBrace);

            if (closeBrace < 0)
                break;

            int end =
                closeBrace + 1;

            while (end <
                       source.Length &&
                   (source[end] == '\r' ||
                    source[end] == '\n'))
            {
                end++;
            }

            source =
                source.Remove(
                    lineStart,
                    end - lineStart);
        }

        return source;
    }

    private static string ExtractMethodsIntoModules(
        string source,
        out Dictionary<
            string,
            List<MethodSpan>
        > modules)
    {
        modules =
            new Dictionary<
                string,
                List<MethodSpan>>(
                    StringComparer.OrdinalIgnoreCase);

        List<MethodSpan> methods =
            FindTopLevelMethods(
                source);

        List<MethodSpan> extracted =
            new List<MethodSpan>();

        foreach (MethodSpan method
            in methods)
        {
            if (ShouldRemainInMain(
                    method.Name))
            {
                continue;
            }

            method.Module =
                Categorize(
                    method.Name);

            List<MethodSpan> bucket;

            if (!modules.TryGetValue(
                    method.Module,
                    out bucket))
            {
                bucket =
                    new List<MethodSpan>();

                modules[
                    method.Module] =
                    bucket;
            }

            bucket.Add(method);
            extracted.Add(method);
        }

        StringBuilder main =
            new StringBuilder(
                source);

        foreach (MethodSpan method
            in extracted
                .OrderByDescending(
                    item =>
                        item.Start))
        {
            main.Remove(
                method.Start,
                method.EndExclusive -
                    method.Start);
        }

        return main.ToString();
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
                @"(?:where\s+[^{}]+)?\{",
                RegexOptions.Compiled);

        MatchCollection matches =
            methodRegex.Matches(
                source);

        foreach (Match match
            in matches)
        {
            int open =
                match.Index +
                match.Value.LastIndexOf(
                    '{');

            if (open < 0 ||
                open >= depth.Length)
            {
                continue;
            }

            // GameController's own class body is brace depth 1.
            // Methods of nested helper classes are deeper and stay where they are.
            if (depth[open] != 1)
                continue;

            string name =
                match.Groups[
                    "name"].Value;

            if (string.IsNullOrWhiteSpace(
                    name))
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

            int start =
                ExpandMethodStart(
                    source,
                    match.Index);

            int end =
                close + 1;

            while (end <
                       source.Length &&
                   (source[end] == '\r' ||
                    source[end] == '\n'))
            {
                end++;
            }

            result.Add(
                new MethodSpan
                {
                    Name = name,
                    Start = start,
                    EndExclusive = end,
                    Text = source.Substring(
                        start,
                        end - start)
                });
        }

        return result
            .OrderBy(
                item =>
                    item.Start)
            .ToList();
    }

    private static int ExpandMethodStart(
        string source,
        int methodStart)
    {
        int start =
            source.LastIndexOf(
                '\n',
                Math.Max(
                    0,
                    methodStart - 1));

        start =
            start < 0
            ? 0
            : start + 1;

        int cursor = start;

        while (cursor > 0)
        {
            int previousLineEnd =
                cursor - 1;

            if (previousLineEnd >= 0 &&
                source[
                    previousLineEnd] == '\n')
            {
                previousLineEnd--;
            }

            if (previousLineEnd >= 0 &&
                source[
                    previousLineEnd] == '\r')
            {
                previousLineEnd--;
            }

            int previousLineStart =
                source.LastIndexOf(
                    '\n',
                    Math.Max(
                        0,
                        previousLineEnd));

            previousLineStart =
                previousLineStart < 0
                ? 0
                : previousLineStart + 1;

            if (previousLineEnd <
                previousLineStart)
            {
                break;
            }

            string line =
                source.Substring(
                    previousLineStart,
                    previousLineEnd -
                    previousLineStart + 1)
                .Trim();

            bool decorator =
                line.StartsWith(
                    "///",
                    StringComparison.Ordinal) ||
                line.StartsWith(
                    "[",
                    StringComparison.Ordinal);

            if (!decorator)
                break;

            cursor =
                previousLineStart;
        }

        return cursor;
    }

    private static int[] ComputeBraceDepthBefore(
        string source)
    {
        int[] depth =
            new int[
                source.Length];

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
            depth[i] =
                current;

            char c =
                source[i];

            char next =
                i + 1 <
                source.Length
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

                    if (i <
                        depth.Length)
                    {
                        depth[i] =
                            current;
                    }
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

                    if (i <
                        depth.Length)
                    {
                        depth[i] =
                            current;
                    }

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

                if (i <
                    depth.Length)
                {
                    depth[i] =
                        current;
                }

                continue;
            }

            if (c == '/' &&
                next == '*')
            {
                blockComment = true;
                i++;

                if (i <
                    depth.Length)
                {
                    depth[i] =
                        current;
                }

                continue;
            }

            if (c == '@' &&
                next == '"')
            {
                verbatimString = true;
                i++;

                if (i <
                    depth.Length)
                {
                    depth[i] =
                        current;
                }

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
            {
                current++;
                continue;
            }

            if (c == '}')
            {
                current =
                    Math.Max(
                        0,
                        current - 1);
            }
        }

        return depth;
    }

    private static int FindMethodClosingBrace(
        string source,
        int[] depth,
        int open)
    {
        for (int i = open + 1;
             i < source.Length;
             i++)
        {
            if (source[i] == '}' &&
                depth[i] == 2)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool ShouldRemainInMain(
        string name)
    {
        switch (name)
        {
            case "Awake":
            case "Start":
            case "Update":
            case "LateUpdate":
            case "FixedUpdate":
            case "OnEnable":
            case "OnDisable":
            case "OnDestroy":
                return true;
        }

        return false;
    }

    private static string Categorize(
        string name)
    {
        string lower =
            name.ToLowerInvariant();

        if (name == "OnGUI" ||
            ContainsAny(
                lower,
                "gui",
                "draw",
                "panel",
                "window",
                "datasheet",
                "battlelog",
                "battlefieldworldui",
                "sidepanel",
                "header",
                "tooltip"))
        {
            return "UI";
        }

        if (ContainsAny(
                lower,
                "traditional",
                "dicetray",
                "diceroll",
                "rolltabletop",
                "manual"))
        {
            return "Traditional";
        }

        if (ContainsAny(
                lower,
                "deployment",
                "reserve",
                "yellow",
                "roster",
                "armyimport",
                "army",
                "setup",
                "attachleader"))
        {
            return "Setup";
        }

        if (ContainsAny(
                lower,
                "mission",
                "objective",
                "secondary",
                "primaryscore",
                "victory",
                "score",
                "operationmarker"))
        {
            return "Missions";
        }

        if (ContainsAny(
                lower,
                "charge"))
        {
            return "Charge";
        }

        if (ContainsAny(
                lower,
                "fight",
                "pilein",
                "consolidat",
                "melee"))
        {
            return "Fight";
        }

        if (ContainsAny(
                lower,
                "movement",
                "move",
                "advance",
                "fallback",
                "fall_back",
                "coher",
                "surge",
                "translate"))
        {
            return "Movement";
        }

        if (ContainsAny(
                lower,
                "attack",
                "shoot",
                "weapon",
                "wound",
                "damage",
                "casualty",
                "save",
                "hitroll",
                "target"))
        {
            return "Combat";
        }

        if (ContainsAny(
                lower,
                "faction",
                "stratagem",
                "aeldari",
                "ynnari",
                "necron",
                "battlefocus",
                "commandpoint",
                "commandreroll",
                "enhancement",
                "rulechoice",
                "reaction"))
        {
            return "Rules";
        }

        return "Core";
    }

    private static bool ContainsAny(
        string value,
        params string[] needles)
    {
        return needles.Any(
            needle =>
                value.Contains(
                    needle));
    }

    private static string BuildUsingBlock(
        string source)
    {
        MatchCollection matches =
            Regex.Matches(
                source,
                @"(?m)^using\s+[^;]+;\s*$");

        return string.Join(
            Environment.NewLine,
            matches
                .Cast<Match>()
                .Select(
                    match =>
                        match.Value.TrimEnd())
                .Distinct()
                .ToArray());
    }

    private static void WritePartialModule(
        string module,
        List<MethodSpan> methods,
        string usingBlock)
    {
        if (methods == null)
        {
            methods =
                new List<MethodSpan>();
        }

        string path =
            "Assets/Scripts/Core/GameController." +
            module +
            ".cs";

        StringBuilder builder =
            new StringBuilder();

        builder.AppendLine(
            usingBlock);

        builder.AppendLine();
        builder.AppendLine(
            "// Generated by the Warboard v35 structural refactor.");
        builder.AppendLine(
            "// Method bodies were moved verbatim from GameController.cs.");
        builder.AppendLine(
            "public partial class GameController : MonoBehaviour");
        builder.AppendLine(
            "{");

        foreach (MethodSpan method
            in methods
                .OrderBy(
                    item =>
                        item.Start))
        {
            builder.Append(
                method.Text.TrimEnd());

            builder.AppendLine();
            builder.AppendLine();
        }

        builder.AppendLine(
            "}");

        WriteTextIfChanged(
            path,
            builder.ToString());
    }

    private static void WriteRuntimeApi()
    {
        const string path =
            "Assets/Scripts/Core/GameController.RuntimeApi.cs";

        string content =
@"using System;
using System.Collections.Generic;
using UnityEngine;

// Stable internal surface used by faction controllers and other subsystems.
// This replaces reflection against GameController's private fields.
public partial class GameController : MonoBehaviour
{
    internal IReadOnlyList<SquadController> CoreSquads
    {
        get { return squads; }
    }

    internal IReadOnlyList<string> CoreFactions
    {
        get { return factions; }
    }

    internal AeldariRulesSystem CoreAeldariRules
    {
        get { return aeldariRules; }
    }

    internal string CoreActiveFaction
    {
        get { return activeFaction; }
    }

    internal string CoreBattleSizeName
    {
        get { return battleSizeName; }
    }

    internal int CoreBattlePoints
    {
        get { return battlePoints; }
    }

    internal bool CorePreGameReady
    {
        get
        {
            return
                (playerOneLoaded &&
                 playerTwoLoaded) ||
                deploymentMode ||
                missionSetupMode;
        }
    }

    internal string CoreYellowCodeForFaction(
        string factionId)
    {
        if (string.IsNullOrWhiteSpace(
                factionId))
        {
            return """";
        }

        int index =
            factions.FindIndex(
                faction =>
                    string.Equals(
                        faction,
                        factionId,
                        StringComparison.OrdinalIgnoreCase));

        if (index == 0)
            return yellowCodePlayerOne ?? """";

        if (index == 1)
            return yellowCodePlayerTwo ?? """";

        return """";
    }

    internal void RaiseCoreEvent(
        GameEventType type,
        SquadController source = null,
        SquadController target = null,
        int amount = 0,
        string note = """")
    {
        GameEventBus.Raise(
            new GameEventContext
            {
                Type = type,
                Game = this,
                ActingFaction =
                    source != null
                    ? source.FactionId
                    : activeFaction,
                Phase = phase,
                Source = source,
                Target = target,
                Amount = amount,
                Note = note ?? """"
            });
    }
}
";

        WriteTextIfChanged(
            path,
            content);
    }

    private static void ValidateRefactor(
        string original,
        string refactored,
        Dictionary<
            string,
            List<MethodSpan>
        > modules)
    {
        int moved =
            modules.Values.Sum(
                list =>
                    list.Count);

        // The current Warboard controller is a very large monolith. If the
        // parser sees only a handful of methods, stop rather than rewriting
        // an unfamiliar source shape.
        if (moved < 40)
        {
            throw new InvalidOperationException(
                "Safety stop: only " +
                moved +
                " GameController methods were identified for extraction.");
        }

        if (refactored.Length >=
            original.Length * 0.90f)
        {
            throw new InvalidOperationException(
                "Safety stop: GameController did not shrink enough for a valid structural refactor.");
        }

        if (!refactored.Contains(
                "partial class GameController"))
        {
            throw new InvalidOperationException(
                "Safety stop: partial GameController declaration was not produced.");
        }

        int[] depth =
            ComputeBraceDepthBefore(
                refactored);

        int last =
            refactored.Length - 1;

        while (last >= 0 &&
               char.IsWhiteSpace(
                   refactored[last]))
        {
            last--;
        }

        // The source should finish by closing the GameController class. The
        // depth immediately before that final structural brace must be one.
        if (last < 0 ||
            refactored[last] != '}' ||
            depth[last] != 1)
        {
            throw new InvalidOperationException(
                "Safety stop: refactored GameController braces are unbalanced.");
        }
    }

    private static string AddRefactorMarker(
        string source)
    {
        if (source.Contains(
                Marker))
        {
            return source;
        }

        Match classMatch =
            Regex.Match(
                source,
                @"public\s+partial\s+class\s+GameController\s*:\s*MonoBehaviour\s*\{");

        if (!classMatch.Success)
            return source;

        int insert =
            classMatch.Index +
            classMatch.Length;

        return source.Insert(
            insert,
            Environment.NewLine +
            "    // " +
            Marker +
            Environment.NewLine +
            "    // v35: this file now owns state/lifecycle; functional areas live in GameController.*.cs partials.");
    }

    private static void CleanupFactionControllerHost()
    {
        const string path =
            "Assets/Scripts/Core/FactionControllerSystem.cs";

        if (!File.Exists(path))
            return;

        string source =
            File.ReadAllText(path);

        if (!source.Contains(
                "public static FactionControllerHost Instance"))
        {
            Match classMatch =
                Regex.Match(
                    source,
                    @"public\s+sealed\s+class\s+FactionControllerHost\s*:\s*MonoBehaviour\s*\{");

            if (classMatch.Success)
            {
                int insert =
                    classMatch.Index +
                    classMatch.Length;

                source =
                    source.Insert(
                        insert,
                        Environment.NewLine +
                        "    public static FactionControllerHost Instance { get; private set; }" +
                        Environment.NewLine);
            }
        }

        source =
            ReplaceNamedMethod(
                source,
                "Awake",
@"    private void Awake()
    {
        Instance = this;

        GameEventBus.Raised +=
            HandleGameEvent;
    }
");

        source =
            ReplaceNamedMethod(
                source,
                "OnDestroy",
@"    private void OnDestroy()
    {
        GameEventBus.Raised -=
            HandleGameEvent;

        if (Instance == this)
            Instance = null;
    }
");

        string replacement =
@"    private void RefreshControllers()
    {
        IReadOnlyList<SquadController> allUnits =
            game != null
            ? game.CoreSquads
            : new List<SquadController>();

        Dictionary<
            string,
            List<SquadController>
        > armies =
            allUnits
                .Where(
                    unit =>
                        unit != null &&
                        !string.IsNullOrWhiteSpace(
                            unit.FactionId))
                .GroupBy(
                    unit => unit.FactionId,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList(),
                    StringComparer.OrdinalIgnoreCase);

        foreach (
            KeyValuePair<
                string,
                List<SquadController>
            > pair
            in armies)
        {
            IFactionGameController wanted =
                FactionGameControllerFactory
                    .Create(pair.Value);

            IFactionGameController current;

            bool replace =
                !controllers.TryGetValue(
                    pair.Key,
                    out current) ||
                current == null ||
                current.GetType() !=
                    wanted.GetType();

            if (replace)
            {
                wanted.Initialize(
                    game,
                    pair.Key);

                controllers[
                    pair.Key] =
                    wanted;

                current = wanted;
            }

            current.RefreshArmy(
                pair.Value);
        }

        List<string> stale =
            controllers.Keys
                .Where(
                    faction =>
                        !armies.ContainsKey(
                            faction))
                .ToList();

        foreach (string faction in stale)
            controllers.Remove(faction);
    }
";

        source =
            ReplaceNamedMethod(
                source,
                "RefreshControllers",
                replacement);

        WriteTextIfChanged(
            path,
            source);
    }

    private static void CleanupFactionControllerRuntime()
    {
        const string path =
            "Assets/Scripts/Core/FactionControllerRuntime.cs";

        if (!File.Exists(path))
            return;

        string source =
            File.ReadAllText(path);

        source =
            ReplaceNamedMethod(
                source,
                "Get",
@"    public static IFactionGameController Get(
        string factionId)
    {
        if (string.IsNullOrWhiteSpace(
                factionId))
        {
            return null;
        }

        FactionControllerHost host =
            FactionControllerHost.Instance;

        return host != null
            ? host.Get(factionId)
            : null;
    }
");

        WriteTextIfChanged(
            path,
            source);
    }

    private static void CleanupAeldariSetupUI()
    {
        const string path =
            "Assets/Scripts/Factions/Aeldari/AeldariSetupUI.cs";

        if (!File.Exists(path))
            return;

        string source =
            File.ReadAllText(path);

        source =
            Regex.Replace(
                source,
                @"FactionControllerHost\s+host\s*=\s*Object\.FindAnyObjectByType<\s*FactionControllerHost\s*>\s*\(\s*\)\s*;",
                "FactionControllerHost host =" +
                Environment.NewLine +
                "            FactionControllerHost.Instance;");

        WriteTextIfChanged(
            path,
            source);
    }

    private static void CleanupAeldariRulesSystem()
    {
        const string path =
            "Assets/Scripts/Core/AeldariRulesSystem.cs";

        if (!File.Exists(path))
            return;

        string source =
            File.ReadAllText(path);

        source =
            ReplaceNamedMethod(
                source,
                "NextDetachment",
@"    public void NextDetachment(
        string faction)
    {
        // v35: detachment selection is a pre-game roster decision and is
        // locked for the battle. Runtime cycling is intentionally disabled.
        return;
    }
");

        WriteTextIfChanged(
            path,
            source);
    }

    private static void CleanupAeldariController()
    {
        const string path =
            "Assets/Scripts/Factions/Aeldari/AeldariGameController.cs";

        if (!File.Exists(path))
            return;

        string source =
            File.ReadAllText(path);

        source =
            ReplaceNamedMethod(
                source,
                "EnsureRulesBinding",
@"    private void EnsureRulesBinding()
    {
        if (rules != null ||
            Game == null)
        {
            return;
        }

        rules =
            Game.CoreAeldariRules;
    }
");

        source =
            ReplaceNamedMethod(
                source,
                "ReadyForPreGameSelection",
@"    private bool ReadyForPreGameSelection()
    {
        return
            Game != null &&
            Game.CorePreGameReady;
    }
");

        source =
            ReplaceNamedMethod(
                source,
                "ResolveYellowScribeCode",
@"    private string ResolveYellowScribeCode()
    {
        return
            Game != null
            ? Game.CoreYellowCodeForFaction(
                FactionId)
            : """";
    }
");

        source =
            ReplaceNamedMethod(
                source,
                "BaseBattleFocusForCurrentSize",
@"    private int BaseBattleFocusForCurrentSize()
    {
        string battleSize =
            Game != null
            ? Game.CoreBattleSizeName
            : """";

        if (string.Equals(
                battleSize,
                ""Incursion"",
                StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(
                battleSize,
                ""Strike Force"",
                StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (string.Equals(
                battleSize,
                ""Onslaught"",
                StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        int points =
            Game != null
            ? Game.CoreBattlePoints
            : 2000;

        if (points <= 1000)
            return 2;

        if (points <= 2000)
            return 4;

        return 6;
    }
");

        source =
            RemoveNamedMethod(
                source,
                "ReadPrivateString");

        source =
            RemoveNamedMethod(
                source,
                "ReadPrivateBool");

        source =
            RemoveNamedMethod(
                source,
                "ReadPrivateInt");

        if (!source.Contains(
                "ObserveCoreTiming();"))
        {
            source =
                InsertIntoMethodStart(
                    source,
                    "Tick",
                    "        ObserveCoreTiming();" +
                    Environment.NewLine);
        }

        if (!source.Contains(
                "private void ObserveCoreTiming()"))
        {
            source =
                InsertBeforeFinalClassBrace(
                    source,
@"
    private GameController.Phase observedPhase;
    private bool hasObservedPhase;
    private int observedRound = -1;

    private void ObserveCoreTiming()
    {
        if (Game == null)
            return;

        GameController.Phase currentPhase =
            Game.CurrentPhase;

        if (!hasObservedPhase)
        {
            observedPhase = currentPhase;
            hasObservedPhase = true;
        }
        else if (observedPhase != currentPhase)
        {
            agileManoeuvresUsedThisPhase.Clear();
            observedPhase = currentPhase;
        }

        int currentRound =
            Game.CurrentRoundNumber;

        if (currentRound > 0 &&
            currentRound != observedRound)
        {
            observedRound = currentRound;
            StartBattleRound(
                currentRound);
        }
    }
");
        }

        if (!source.Contains(
                "BindingFlags.") &&
            !source.Contains(
                "FieldInfo "))
        {
            source =
                Regex.Replace(
                    source,
                    @"(?m)^using\s+System\.Reflection;\s*\r?\n",
                    "");
        }

        WriteTextIfChanged(
            path,
            source);
    }

    private static string ReplaceNamedMethod(
        string source,
        string methodName,
        string replacement)
    {
        MethodSpan method =
            FindTopLevelMethods(
                source)
            .FirstOrDefault(
                item =>
                    string.Equals(
                        item.Name,
                        methodName,
                        StringComparison.Ordinal));

        if (method == null)
            return source;

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

    private static string RemoveNamedMethod(
        string source,
        string methodName)
    {
        MethodSpan method =
            FindTopLevelMethods(
                source)
            .FirstOrDefault(
                item =>
                    string.Equals(
                        item.Name,
                        methodName,
                        StringComparison.Ordinal));

        if (method == null)
            return source;

        return source.Remove(
            method.Start,
            method.EndExclusive -
                method.Start);
    }

    private static string InsertIntoMethodStart(
        string source,
        string methodName,
        string statement)
    {
        MethodSpan method =
            FindTopLevelMethods(
                source)
            .FirstOrDefault(
                item =>
                    string.Equals(
                        item.Name,
                        methodName,
                        StringComparison.Ordinal));

        if (method == null)
            return source;

        int open =
            source.IndexOf(
                '{',
                method.Start);

        if (open < 0 ||
            open >=
                method.EndExclusive)
        {
            return source;
        }

        return source.Insert(
            open + 1,
            Environment.NewLine +
            statement);
    }

    private static string InsertBeforeFinalClassBrace(
        string source,
        string addition)
    {
        int[] depth =
            ComputeBraceDepthBefore(
                source);

        for (int i =
                 source.Length - 1;
             i >= 0;
             i--)
        {
            if (source[i] == '}' &&
                depth[i] == 1)
            {
                return source.Insert(
                    i,
                    addition.TrimEnd() +
                    Environment.NewLine);
            }
        }

        return source;
    }

    private static void CleanupLegacyBridge()
    {
        const string bridge =
            "Assets/Scripts/Core/CoreEventBridge.cs";

        if (File.Exists(bridge))
        {
            AssetDatabase.DeleteAsset(
                bridge);
        }

        string meta =
            bridge +
            ".meta";

        if (File.Exists(meta))
        {
            File.Delete(meta);
        }
    }

    private static int FindMatchingBrace(
        string source,
        int open)
    {
        if (open < 0 ||
            open >=
                source.Length ||
            source[open] != '{')
        {
            return -1;
        }

        int depth = 0;

        bool lineComment = false;
        bool blockComment = false;
        bool normalString = false;
        bool verbatimString = false;
        bool charLiteral = false;
        bool escape = false;

        for (int i = open;
             i < source.Length;
             i++)
        {
            char c =
                source[i];

            char next =
                i + 1 <
                source.Length
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
                continue;
            }

            if (c == '/' &&
                next == '*')
            {
                blockComment = true;
                i++;
                continue;
            }

            if (c == '@' &&
                next == '"')
            {
                verbatimString = true;
                i++;
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
            {
                depth++;
                continue;
            }

            if (c == '}')
            {
                depth--;

                if (depth == 0)
                    return i;
            }
        }

        return -1;
    }

    private static void WriteReport(
        string original,
        string refactored,
        Dictionary<
            string,
            List<MethodSpan>
        > modules)
    {
        StringBuilder report =
            new StringBuilder();

        report.AppendLine(
            "Warboard v35 GameController structural refactor");
        report.AppendLine(
            "==============================================");
        report.AppendLine();
        report.AppendLine(
            "Original GameController characters: " +
            original.Length);
        report.AppendLine(
            "Refactored GameController characters: " +
            refactored.Length);
        report.AppendLine(
            "Methods moved: " +
            modules.Values.Sum(
                list =>
                    list.Count));
        report.AppendLine();

        foreach (
            KeyValuePair<
                string,
                List<MethodSpan>
            > pair
            in modules
                .OrderBy(
                    item =>
                        item.Key))
        {
            report.AppendLine(
                pair.Key +
                ": " +
                pair.Value.Count +
                " methods");

            foreach (MethodSpan method
                in pair.Value)
            {
                report.AppendLine(
                    "  - " +
                    method.Name);
            }

            report.AppendLine();
        }

        string directory =
            Path.GetDirectoryName(
                ReportPath);

        if (!string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        File.WriteAllText(
            ReportPath,
            report.ToString(),
            Encoding.UTF8);
    }

    private static void WriteTextIfChanged(
        string path,
        string content)
    {
        string directory =
            Path.GetDirectoryName(
                path);

        if (!string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        if (File.Exists(path))
        {
            string existing =
                File.ReadAllText(path);

            if (string.Equals(
                    existing,
                    content,
                    StringComparison.Ordinal))
            {
                return;
            }
        }

        File.WriteAllText(
            path,
            content,
            Encoding.UTF8);
    }
}
#endif
