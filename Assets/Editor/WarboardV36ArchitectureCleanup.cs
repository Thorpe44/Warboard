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
/// One-time v36 source migration.
///
/// This does not create a runtime bridge. It edits the already-split v35.1
/// GameController partials so the authoritative core methods publish their
/// events directly, then removes the migration scripts/bridge leftovers.
/// </summary>
[InitializeOnLoad]
public static class WarboardV36ArchitectureCleanup
{
    private const string BackupDirectory =
        "Library/WarboardBackups/V36";

    private const string ReportPath =
        "Library/WarboardV36ArchitectureReport.txt";

    private const string SelfPath =
        "Assets/Editor/WarboardV36ArchitectureCleanup.cs";

    private static readonly string[] GameControllerPaths =
    {
        "Assets/Scripts/Core/GameController.cs",
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
    }

    static WarboardV36ArchitectureCleanup()
    {
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        try
        {
            ValidateSplitExists();
            BackupSources();

            PatchLifecycle();
            PatchCoreFlowEvents();
            PatchMovementEvents();
            PatchSetupEvents();
            PatchChargeEvents();
            PatchCombatEvents();
            PatchFightEvents();
            PatchBattleFocusCallPath();
            PatchModelDestroyedEvent();
            PatchLegacyAeldariRulesFacade();

            DeleteLegacyArtifacts();
            ValidateResult();
            WriteReport();

            // The migration is source-only and one-time. Delete it from the
            // project after success so v36 leaves no editor/runtime shim.
            AssetDatabase.DeleteAsset(SelfPath);
            AssetDatabase.Refresh();

            Debug.Log(
                "[Warboard v36] Architecture cleanup complete: direct core events are wired, faction polling/reflection is removed, and legacy bridge/editor migration files are gone.");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Warboard v36] Architecture cleanup failed. No cleanup script was deleted. " +
                ex);
        }
    }

    private static void ValidateSplitExists()
    {
        string[] required =
        {
            "Assets/Scripts/Core/GameController.cs",
            "Assets/Scripts/Core/GameController.Core.cs",
            "Assets/Scripts/Core/GameController.Setup.cs",
            "Assets/Scripts/Core/GameController.Movement.cs",
            "Assets/Scripts/Core/GameController.Charge.cs",
            "Assets/Scripts/Core/GameController.Combat.cs",
            "Assets/Scripts/Core/GameController.Fight.cs",
            "Assets/Scripts/Core/GameController.Rules.cs",
            "Assets/Scripts/Core/GameController.RuntimeApi.cs"
        };

        foreach (string path in required)
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    "v35.1 split file is missing: " +
                    path);
            }
        }

        FileInfo main =
            new FileInfo(
                "Assets/Scripts/Core/GameController.cs");

        if (main.Length > 120000)
        {
            throw new InvalidOperationException(
                "Safety stop: GameController.cs is still monolithic. v36 expects the completed v35.1 split before it runs.");
        }
    }

    private static void BackupSources()
    {
        Directory.CreateDirectory(
            BackupDirectory);

        foreach (string path
            in GameControllerPaths.Concat(
                new[]
                {
                    "Assets/Scripts/Core/ModelToken.cs",
                    "Assets/Scripts/Core/AeldariRulesSystem.cs",
                    "Assets/Scripts/Core/FactionControllerSystem.cs",
                    "Assets/Scripts/Core/FactionControllerRuntime.cs",
                    "Assets/Scripts/Core/FactionRuleSystem.cs",
                    "Assets/Scripts/Factions/Aeldari/AeldariGameController.cs"
                }))
        {
            if (!File.Exists(path))
                continue;

            string backupName =
                path.Replace('/', '_') +
                ".txt";

            string backupPath =
                Path.Combine(
                    BackupDirectory,
                    backupName);

            if (!File.Exists(backupPath))
            {
                File.Copy(
                    path,
                    backupPath);
            }
        }
    }

    private static void PatchLifecycle()
    {
        PatchNamedMethod(
            "OnDestroy",
            method =>
                InsertAtMethodStart(
                    method,
                    "        UnbindAsCurrent();\n"));
    }

    private static void PatchCoreFlowEvents()
    {
        PatchNamedMethod(
            "BeginBattle",
            method =>
            {
                if (method.Contains(
                        "NotifyBattleStarted();"))
                {
                    return method;
                }

                Regex roundOne =
                    new Regex(
                        @"(?m)^(?<indent>\s*)round\s*=\s*1\s*;");

                Match match =
                    roundOne.Match(method);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "BeginBattle round=1 assignment not found.");
                }

                string indent =
                    match.Groups["indent"].Value;

                return roundOne.Replace(
                    method,
                    match.Value +
                    Environment.NewLine +
                    indent +
                    "NotifyBattleStarted();" +
                    Environment.NewLine +
                    indent +
                    "NotifyBattleRoundStarted();",
                    1);
            });

        PatchNamedMethod(
            "CompletePhaseTransition",
            method =>
            {
                if (method.Contains(
                        "NotifyPhaseEnded(leavingPhase);"))
                {
                    return method;
                }

                Regex leaving =
                    new Regex(
                        @"Phase\s+leavingPhase\s*=\s*phase\s*;");

                Match match =
                    leaving.Match(method);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "CompletePhaseTransition leavingPhase capture not found.");
                }

                return leaving.Replace(
                    method,
                    match.Value +
                    Environment.NewLine +
                    Environment.NewLine +
                    "        NotifyPhaseEnded(leavingPhase);",
                    1);
            });

        PatchNamedMethod(
            "EndTurn",
            method =>
            {
                method =
                    InsertAtMethodStart(
                        method,
                        "        NotifyTurnEnded();\n");

                if (!method.Contains(
                        "NotifyBattleRoundEnded();"))
                {
                    Regex reset =
                        new Regex(
                            @"(?m)^(?<indent>\s*)turnsCompletedThisRound\s*=\s*0\s*;");

                    Match match =
                        reset.Match(method);

                    if (!match.Success)
                    {
                        throw new InvalidOperationException(
                            "EndTurn round reset not found.");
                    }

                    string indent =
                        match.Groups["indent"].Value;

                    method =
                        reset.Replace(
                            method,
                            indent +
                            "NotifyBattleRoundEnded();" +
                            Environment.NewLine +
                            Environment.NewLine +
                            match.Value,
                            1);
                }

                if (!method.Contains(
                        "NotifyBattleRoundStarted();"))
                {
                    Regex increment =
                        new Regex(
                            @"(?m)^(?<indent>\s*)round\s*(?:\+\+|\+=\s*1)\s*;");

                    Match match =
                        increment.Match(method);

                    if (!match.Success)
                    {
                        throw new InvalidOperationException(
                            "EndTurn round increment not found.");
                    }

                    method =
                        increment.Replace(
                            method,
                            match.Value +
                            Environment.NewLine +
                            match.Groups["indent"].Value +
                            "NotifyBattleRoundStarted();",
                            1);
                }

                return method;
            });
    }

    private static void PatchMovementEvents()
    {
        PatchNamedMethod(
            "HandleFriendlyClick",
            method =>
            {
                if (method.Contains(
                        "NotifyUnitSelectedToMove("))
                {
                    return method;
                }

                return InsertAtMethodStart(
                    method,
@"        if (phase == Phase.Move &&
            squad != null)
        {
            NotifyUnitSelectedToMove(
                squad);
        }
");
            });

        PatchNamedMethod(
            "ApplyAdvanceRoll",
            method =>
            {
                if (method.Contains(
                        "NotifyUnitAdvanced("))
                {
                    return method;
                }

                Regex declaration =
                    new Regex(
                        @"unit\.DeclareAdvance\(\s*roll\s*\)\s*;");

                Match match =
                    declaration.Match(method);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "ApplyAdvanceRoll DeclareAdvance call not found.");
                }

                return declaration.Replace(
                    method,
                    match.Value +
                    Environment.NewLine +
                    Environment.NewLine +
                    "        NotifyUnitAdvanced(" +
                    Environment.NewLine +
                    "            unit);",
                    1);
            });

        const string movementPath =
            "Assets/Scripts/Core/GameController.Movement.cs";

        string source =
            File.ReadAllText(
                movementPath);

        if (!source.Contains(
                "// WARBOARD_V36_DIRECT_MOVEMENT_EVENTS"))
        {
            source =
                Regex.Replace(
                    source,
                    @"public\s+partial\s+class\s+GameController\s*:\s*MonoBehaviour\s*\{",
                    match =>
                        match.Value +
                        Environment.NewLine +
                        "    // WARBOARD_V36_DIRECT_MOVEMENT_EVENTS",
                    RegexOptions.None);

            source =
                Regex.Replace(
                    source,
                    @"(?m)^(?<indent>\s*)selectedModel\.transform\.position\s*=\s*destination\s*;",
                    match =>
                        match.Groups["indent"].Value +
                        "NotifyMoveStarted(selectedSquad);" +
                        Environment.NewLine +
                        match.Value);

            source =
                Regex.Replace(
                    source,
                    @"(?m)^(?<indent>\s*)selectedSquad\.HasMoved\s*=\s*true\s*;",
                    match =>
                        match.Groups["indent"].Value +
                        "NotifyMoveStarted(selectedSquad);" +
                        Environment.NewLine +
                        match.Value +
                        Environment.NewLine +
                        match.Groups["indent"].Value +
                        "NotifyMoveEnded(selectedSquad);");

            source =
                Regex.Replace(
                    source,
                    @"(?m)^(?<indent>\s*)squad\.HasMoved\s*=\s*true\s*;",
                    match =>
                        match.Groups["indent"].Value +
                        "NotifyMoveStarted(squad);" +
                        Environment.NewLine +
                        match.Value +
                        Environment.NewLine +
                        match.Groups["indent"].Value +
                        "NotifyMoveEnded(squad);");

            source =
                Regex.Replace(
                    source,
                    @"(?m)^(?<indent>\s*)selectedSquad\.HasFallenBack\s*=\s*true\s*;",
                    match =>
                        match.Value +
                        Environment.NewLine +
                        match.Groups["indent"].Value +
                        "NotifyUnitFellBack(selectedSquad);");

            WriteTextIfChanged(
                movementPath,
                source);
        }
    }

    private static void PatchSetupEvents()
    {
        const string path =
            "Assets/Scripts/Core/GameController.Setup.cs";

        string source =
            File.ReadAllText(path);

        if (!source.Contains(
                "// WARBOARD_V36_DIRECT_SETUP_EVENTS"))
        {
            source =
                Regex.Replace(
                    source,
                    @"public\s+partial\s+class\s+GameController\s*:\s*MonoBehaviour\s*\{",
                    match =>
                        match.Value +
                        Environment.NewLine +
                        "    // WARBOARD_V36_DIRECT_SETUP_EVENTS",
                    RegexOptions.None);

            source =
                Regex.Replace(
                    source,
                    @"(?m)^(?<indent>\s*)(?<unit>[A-Za-z_][A-Za-z0-9_]*)\.MarkSetUpThisTurn\(\);",
                    match =>
                        match.Value +
                        Environment.NewLine +
                        match.Groups["indent"].Value +
                        "NotifyUnitSetUp(" +
                        match.Groups["unit"].Value +
                        ");");

            List<MethodSpan> methods =
                FindTopLevelMethods(source);

            foreach (MethodSpan method
                in methods
                    .OrderByDescending(
                        item => item.Start))
            {
                bool changesRoster =
                    method.Text.Contains(
                        "playerOneLoaded = true") ||
                    method.Text.Contains(
                        "playerTwoLoaded = true") ||
                    string.Equals(
                        method.Name,
                        "RemovePlayerArmy",
                        StringComparison.Ordinal);

                if (!changesRoster ||
                    method.Text.Contains(
                        "NotifyRostersChanged();"))
                {
                    continue;
                }

                string patched =
                    InsertBeforeMethodClose(
                        method.Text,
                        "        NotifyRostersChanged();\n");

                source =
                    source.Substring(
                        0,
                        method.Start) +
                    patched +
                    source.Substring(
                        method.EndExclusive);
            }

            WriteTextIfChanged(
                path,
                source);
        }
    }

    private static void PatchChargeEvents()
    {
        PatchNamedMethod(
            "TryCharge",
            method =>
            {
                if (method.Contains(
                        "NotifyChargeDeclared("))
                {
                    return method;
                }

                Regex charged =
                    new Regex(
                        @"(?m)^(?<indent>\s*)attacker\.HasCharged\s*=\s*true\s*;");

                Match match =
                    charged.Match(method);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "TryCharge declaration commit point not found.");
                }

                return charged.Replace(
                    method,
                    match.Groups["indent"].Value +
                    "NotifyChargeDeclared(" +
                    Environment.NewLine +
                    match.Groups["indent"].Value +
                    "    attacker," +
                    Environment.NewLine +
                    match.Groups["indent"].Value +
                    "    target);" +
                    Environment.NewLine +
                    Environment.NewLine +
                    match.Value,
                    1);
            });
    }

    private static void PatchCombatEvents()
    {
        PatchNamedMethod(
            "CompleteSelectedUnitShooting",
            method =>
            {
                if (method.Contains(
                        "NotifyUnitFinishedShooting("))
                {
                    return method;
                }

                Regex finished =
                    new Regex(
                        @"(?m)^(?<indent>\s*)selectedSquad\.HasShot\s*=\s*true\s*;");

                Match match =
                    finished.Match(method);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "CompleteSelectedUnitShooting HasShot assignment not found.");
                }

                return finished.Replace(
                    method,
                    match.Value +
                    Environment.NewLine +
                    match.Groups["indent"].Value +
                    "NotifyUnitFinishedShooting(selectedSquad);",
                    1);
            });

        const string combatPath =
            "Assets/Scripts/Core/GameController.Combat.cs";

        string source =
            File.ReadAllText(combatPath);

        if (!source.Contains(
                "// WARBOARD_V36_DIRECT_COMBAT_EVENTS"))
        {
            source =
                Regex.Replace(
                    source,
                    @"public\s+partial\s+class\s+GameController\s*:\s*MonoBehaviour\s*\{",
                    match =>
                        match.Value +
                        Environment.NewLine +
                        "    // WARBOARD_V36_DIRECT_COMBAT_EVENTS",
                    RegexOptions.None);

            source =
                Regex.Replace(
                    source,
                    @"(?m)^(?<indent>\s*)attacker\.HasShot\s*=\s*true\s*;",
                    match =>
                        match.Value +
                        Environment.NewLine +
                        match.Groups["indent"].Value +
                        "NotifyUnitFinishedShooting(attacker);");

            WriteTextIfChanged(
                combatPath,
                source);
        }
    }

    private static void PatchFightEvents()
    {
        PatchNamedMethod(
            "BeginFightActivation",
            method =>
            {
                if (method.Contains(
                        "NotifyUnitSelectedToFight("))
                {
                    return method;
                }

                Regex target =
                    new Regex(
                        @"fightActivationInitialTarget\s*=\s*target\.JoinedActionController\(\)\s*;");

                Match match =
                    target.Match(method);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "BeginFightActivation target assignment not found.");
                }

                return target.Replace(
                    method,
                    match.Value +
                    Environment.NewLine +
                    Environment.NewLine +
                    "        NotifyUnitSelectedToFight(" +
                    Environment.NewLine +
                    "            fightActivationUnit," +
                    Environment.NewLine +
                    "            fightActivationInitialTarget);",
                    1);
            });

        PatchNamedMethod(
            "CompleteFightConsolidation",
            method =>
            {
                if (method.Contains(
                        "NotifyUnitFinishedFighting("))
                {
                    return method;
                }

                Regex complete =
                    new Regex(
                        @"(?m)^(?<indent>\s*)completed\.HasFought\s*=\s*true\s*;");

                Match match =
                    complete.Match(method);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "CompleteFightConsolidation completion assignment not found.");
                }

                return complete.Replace(
                    method,
                    match.Value +
                    Environment.NewLine +
                    match.Groups["indent"].Value +
                    "NotifyUnitFinishedFighting(completed);",
                    1);
            });
    }

    private static void PatchBattleFocusCallPath()
    {
        PatchNamedMethod(
            "SpendBattleFocusFor",
            method =>
            {
                if (method.Contains(
                        "factionController.SpendBattleFocus("))
                {
                    return method;
                }

                Regex legacy =
                    new Regex(
@"if\s*\(factionRules\.GetBattleFocusTokens\(\s*unit\.FactionId\s*\)\s*>\s*0\)\s*\{\s*spent\s*=\s*factionRules\.SpendBattleFocus\(\s*unit\.FactionId\s*,\s*1\s*\)\s*;\s*\}",
                        RegexOptions.Singleline);

                Match match =
                    legacy.Match(method);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "SpendBattleFocusFor legacy FactionRuleSystem block not found.");
                }

                string replacement =
@"AeldariGameController factionController =
            FactionControllerRuntime.GetAeldari(
                unit.FactionId);

        if (factionController != null &&
            factionController.BattleFocusTokens > 0)
        {
            spent =
                factionController.SpendBattleFocus(
                    1,
                    manoeuvre);
        }";

                return legacy.Replace(
                    method,
                    replacement,
                    1);
            });
    }

    private static void PatchModelDestroyedEvent()
    {
        const string path =
            "Assets/Scripts/Core/ModelToken.cs";

        if (!File.Exists(path))
            return;

        string source =
            File.ReadAllText(path);

        if (source.Contains(
                "NotifyModelDestroyed(Squad)"))
        {
            return;
        }

        Regex death =
            new Regex(
@"if\s*\(CurrentWounds\s*<=\s*0\)\s*\r?\n\s*gameObject\.SetActive\(false\);",
                RegexOptions.Multiline);

        if (!death.IsMatch(source))
        {
            throw new InvalidOperationException(
                "ModelToken death transition was not found.");
        }

        source =
            death.Replace(
                source,
@"if (CurrentWounds <= 0)
        {
            gameObject.SetActive(false);

            if (GameController.Current != null)
            {
                GameController.Current.NotifyModelDestroyed(
                    Squad);
            }
        }",
                1);

        WriteTextIfChanged(
            path,
            source);
    }

    private static void PatchLegacyAeldariRulesFacade()
    {
        const string path =
            "Assets/Scripts/Core/AeldariRulesSystem.cs";

        if (!File.Exists(path))
            return;

        string source =
            File.ReadAllText(path);

        source =
            ReplaceNamedMethodInSource(
                source,
                "ApplyDetachmentKeywords",
@"    public void ApplyDetachmentKeywords(
        string faction,
        IList<SquadController> squads)
    {
        // v36: temporary keyword grants are owned by AeldariGameController so
        // imported roster keywords are never accidentally removed later.
        AeldariGameController controller =
            FactionControllerRuntime.GetAeldari(
                faction);

        if (controller != null)
        {
            controller.RefreshDetachmentState();
        }
    }");

        source =
            ReplaceNamedMethodInSource(
                source,
                "NextDetachment",
@"    public void NextDetachment(
        string faction)
    {
        // Detachments are roster-driven, selected once before deployment and
        // locked for the battle. Runtime cycling no longer exists.
    }");

        WriteTextIfChanged(
            path,
            source);
    }

    private static void DeleteLegacyArtifacts()
    {
        string[] obsolete =
        {
            "Assets/Scripts/Core/CoreEventBridge.cs",
            "Assets/Editor/WarboardV35GameControllerRefactor.cs"
        };

        foreach (string path in obsolete)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(
                    path);
            }
        }
    }

    private static void ValidateResult()
    {
        string runtimeApi =
            File.ReadAllText(
                "Assets/Scripts/Core/GameController.RuntimeApi.cs");

        if (!runtimeApi.Contains(
                "NotifyUnitSelectedToMove") ||
            !runtimeApi.Contains(
                "NotifyBattleRoundStarted") ||
            !runtimeApi.Contains(
                "RostersChanged"))
        {
            throw new InvalidOperationException(
                "v36 runtime API is incomplete.");
        }

        string allCore =
            string.Join(
                "\n",
                GameControllerPaths
                    .Where(File.Exists)
                    .Select(File.ReadAllText));

        string[] requiredDirectHooks =
        {
            "BindAsCurrent();",
            "NotifyPhaseEnded(leavingPhase);",
            "NotifyTurnEnded();",
            "NotifyBattleRoundStarted();",
            "NotifyUnitSelectedToMove(",
            "NotifyUnitAdvanced(",
            "NotifyMoveEnded(",
            "NotifyChargeDeclared(",
            "NotifyUnitSelectedToFight(",
            "NotifyUnitFinishedFighting(",
            "NotifyUnitFinishedShooting("
        };

        foreach (string hook in requiredDirectHooks)
        {
            if (!allCore.Contains(hook))
            {
                throw new InvalidOperationException(
                    "Direct core event hook is missing: " +
                    hook);
            }
        }

        string aeldariController =
            File.ReadAllText(
                "Assets/Scripts/Factions/Aeldari/AeldariGameController.cs");

        if (aeldariController.Contains(
                "System.Reflection") ||
            aeldariController.Contains(
                "ObserveCoreTiming") ||
            aeldariController.Contains(
                "ReadPrivate"))
        {
            throw new InvalidOperationException(
                "AeldariGameController still contains reflection/polling migration code.");
        }

        string factionHost =
            File.ReadAllText(
                "Assets/Scripts/Core/FactionControllerSystem.cs");

        if (factionHost.Contains(
                "nextRefreshTime") ||
            factionHost.Contains(
                "private void Update()"))
        {
            throw new InvalidOperationException(
                "FactionControllerHost still contains roster polling.");
        }

        string factionRules =
            File.ReadAllText(
                "Assets/Scripts/Core/FactionRuleSystem.cs");

        if (factionRules.Contains(
                "ResolveBattleFocusManoeuvreFromCallStack") ||
            factionRules.Contains(
                "StackTrace"))
        {
            throw new InvalidOperationException(
                "Legacy Battle Focus call-stack bridge is still present.");
        }
    }

    private static void WriteReport()
    {
        StringBuilder report =
            new StringBuilder();

        report.AppendLine(
            "WARBOARD v36 ARCHITECTURE CLEANUP");
        report.AppendLine();
        report.AppendLine(
            "GameController is split and now publishes missing rule timing events directly from the authoritative methods.");
        report.AppendLine(
            "FactionControllerHost is roster-event driven; no 0.20s polling Update remains.");
        report.AppendLine(
            "AeldariGameController uses the public GameController runtime API; reflection and ObserveCoreTiming are removed.");
        report.AppendLine(
            "Battle Focus base resource state lives in AeldariBattleFocusController.");
        report.AppendLine(
            "Battle Focus Agile Manoeuvre names are passed directly; StackTrace inference is removed.");
        report.AppendLine(
            "Temporary Aeldari keyword grants track provenance instead of removing imported roster keywords.");
        report.AppendLine(
            "CoreEventBridge and the v35 one-time refactor script are removed.");

        File.WriteAllText(
            ReportPath,
            report.ToString(),
            Encoding.UTF8);
    }

    private static void PatchNamedMethod(
        string methodName,
        Func<string, string> transform)
    {
        foreach (string path in GameControllerPaths)
        {
            if (!File.Exists(path))
                continue;

            string source =
                File.ReadAllText(path);

            MethodSpan method =
                FindTopLevelMethods(source)
                    .FirstOrDefault(
                        item =>
                            string.Equals(
                                item.Name,
                                methodName,
                                StringComparison.Ordinal));

            if (method == null)
                continue;

            string patched =
                transform(method.Text);

            if (patched == method.Text)
                return;

            source =
                source.Substring(
                    0,
                    method.Start) +
                patched +
                source.Substring(
                    method.EndExclusive);

            WriteTextIfChanged(
                path,
                source);

            return;
        }

        throw new InvalidOperationException(
            "GameController method not found: " +
            methodName);
    }

    private static string ReplaceNamedMethodInSource(
        string source,
        string methodName,
        string replacement)
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
        {
            throw new InvalidOperationException(
                "Method not found while cleaning source: " +
                methodName);
        }

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

    private static string InsertAtMethodStart(
        string method,
        string statement)
    {
        string marker =
            statement.Trim();

        if (method.Contains(marker))
            return method;

        int open =
            method.IndexOf('{');

        if (open < 0)
            return method;

        return method.Insert(
            open + 1,
            Environment.NewLine +
            statement);
    }

    private static string InsertBeforeMethodClose(
        string method,
        string statement)
    {
        if (method.Contains(
                statement.Trim()))
        {
            return method;
        }

        int close =
            method.LastIndexOf('}');

        if (close < 0)
            return method;

        return method.Insert(
            close,
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

            int start =
                match.Index;

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
                    Start = start,
                    EndExclusive = end,
                    Text = source.Substring(
                        start,
                        end - start)
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

            char c = source[i];
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
                if (c == '*' && next == '/')
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
                if (c == '"' && next == '"')
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

            if (c == '/' && next == '/')
            {
                lineComment = true;
                i++;
                if (i < depth.Length)
                    depth[i] = current;
                continue;
            }

            if (c == '/' && next == '*')
            {
                blockComment = true;
                i++;
                if (i < depth.Length)
                    depth[i] = current;
                continue;
            }

            if (c == '@' && next == '"')
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
                depth[i] == wantedDepth + 1)
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
