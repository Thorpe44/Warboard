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
/// v39 source migration: applies high-confidence 11e core-rule corrections to
/// the already-split GameController project without reintroducing a runtime
/// bridge or polling shim.
/// </summary>
[InitializeOnLoad]
public static class WarboardV39CoreRulesCompliance
{
    private const string BackupDirectory =
        "Library/WarboardBackups/V39";

    private const string ReportPath =
        "Library/WarboardV39CoreRulesReport.txt";

    private const string SelfPath =
        "Assets/Editor/WarboardV39CoreRulesCompliance.cs";

    private const string Marker =
        "WARBOARD_V39_CORE_RULES_COMPLIANCE";

    private static readonly string[] GamePaths =
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
        "Assets/Scripts/Core/GameController.RuntimeApi.cs",
        "Assets/Scripts/Core/GameController.CoreRules11.cs"
    };

    private sealed class MethodLocation
    {
        public string Path;
        public string Source;
        public int Start;
        public int EndExclusive;
        public string Text;
    }

    static WarboardV39CoreRulesCompliance()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Warboard/Developer/Re-run v39 Core Rules Compliance")]
    public static void Run()
    {
        try
        {
            ValidateSplit();

            if (MigrationAlreadyApplied())
            {
                DeleteSelf();
                return;
            }

            BackupSources();

            PatchSquadGeometry();
            PatchObjectiveGeometry();
            PatchEngagementGeometry();
            PatchObjectiveTiming();
            PatchMovementSelectionCompletion();
            PatchMissionActionEligibility();
            PatchAutomaticAttackCoreRules();
            InstallMarker();

            ValidateResult();
            WriteReport();
            DeleteSelf();

            Debug.Log(
                "[Warboard v39] Core Rules compliance pass installed. Geometry, objective timing, Move-step completion, action eligibility and automatic combat cover/FNP corrections are active.");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Warboard v39] Core Rules compliance migration failed. " +
                ex);
        }
    }

    private static void ValidateSplit()
    {
        string[] required =
        {
            "Assets/Scripts/Core/GameController.cs",
            "Assets/Scripts/Core/GameController.Movement.cs",
            "Assets/Scripts/Core/GameController.Missions.cs",
            "Assets/Scripts/Core/GameController.RuntimeApi.cs",
            "Assets/Scripts/Core/SquadController.cs",
            "Assets/Scripts/Core/ObjectiveController.cs",
            "Assets/Scripts/Core/MissionSystem.cs",
            "Assets/Scripts/Core/RulesEngine.cs",
            "Assets/Scripts/Core/GameController.CoreRules11.cs"
        };

        foreach (string path in required)
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    "Required v39 source file is missing: " +
                    path);
            }
        }

        FileInfo main =
            new FileInfo(
                "Assets/Scripts/Core/GameController.cs");

        if (main.Length > 120000)
        {
            throw new InvalidOperationException(
                "Safety stop: v39 expects the completed split GameController architecture.");
        }
    }

    private static bool MigrationAlreadyApplied()
    {
        string path =
            "Assets/Scripts/Core/GameController.CoreRules11.cs";

        if (!File.Exists(path))
            return false;

        string source =
            File.ReadAllText(path);

        return source.Contains(
            Marker);
    }

    private static void BackupSources()
    {
        Directory.CreateDirectory(
            BackupDirectory);

        string[] paths =
            GamePaths.Concat(
                new[]
                {
                    "Assets/Scripts/Core/SquadController.cs",
                    "Assets/Scripts/Core/ObjectiveController.cs",
                    "Assets/Scripts/Core/MissionSystem.cs",
                    "Assets/Scripts/Core/RulesEngine.cs"
                })
                .Distinct()
                .ToArray();

        foreach (string path in paths)
        {
            if (!File.Exists(path))
                continue;

            string backup =
                Path.Combine(
                    BackupDirectory,
                    path.Replace('/', '_') +
                    ".txt"
                );

            if (!File.Exists(backup))
            {
                File.Copy(path, backup);
            }
        }
    }

    private static void PatchSquadGeometry()
    {
        const string path =
            "Assets/Scripts/Core/SquadController.cs";

        string source =
            File.ReadAllText(path);

        source = ReplaceMethod(
            source,
            "ListIsCoherent",
@"    private bool ListIsCoherent(
        List<ModelToken> living)
    {
        if (living == null ||
            living.Count <= 1)
        {
            return true;
        }

        foreach (ModelToken model
            in living)
        {
            bool hasNeighbour =
                living.Any(
                    other =>
                        other != model &&
                        CoreRules11Geometry
                            .WithinCoherencyNeighbour(
                                model,
                                other
                            )
                );

            bool withinNine =
                living.All(
                    other =>
                        other == model ||
                        CoreRules11Geometry
                            .WithinCoherencyAll(
                                model,
                                other
                            )
                );

            if (!hasNeighbour ||
                !withinNine)
            {
                return false;
            }
        }

        return true;
    }");

        source = ReplaceMethod(
            source,
            "IncoherentModels",
@"    public List<ModelToken> IncoherentModels()
    {
        SquadController actionUnit =
            JoinedActionController();

        List<ModelToken> living =
            actionUnit
                .JoinedLivingModelTokens()
                .Where(
                    model =>
                        model != null &&
                        model.IsAlive
                )
                .ToList();

        List<ModelToken> invalid =
            new List<ModelToken>();

        if (living.Count <= 1)
            return invalid;

        foreach (ModelToken model
            in living)
        {
            bool hasNeighbour =
                living.Any(
                    other =>
                        other != model &&
                        CoreRules11Geometry
                            .WithinCoherencyNeighbour(
                                model,
                                other
                            )
                );

            bool withinNine =
                living.All(
                    other =>
                        other == model ||
                        CoreRules11Geometry
                            .WithinCoherencyAll(
                                model,
                                other
                            )
                );

            if (!hasNeighbour ||
                !withinNine)
            {
                invalid.Add(model);
            }
        }

        return invalid;
    }");

        source = ReplaceMethod(
            source,
            "TotalObjectiveControlWithin",
@"    public int TotalObjectiveControlWithin(
        Vector3 point,
        float radius)
    {
        SquadController actionUnit =
            JoinedActionController();

        if (actionUnit.IsBattleShocked)
            return 0;

        int total = 0;

        foreach (ModelToken model
            in actionUnit
                .JoinedLivingModelTokens())
        {
            if (model == null ||
                !model.IsAlive ||
                !CoreRules11Geometry
                    .ModelWithinObjective(
                        model,
                        point,
                        radius
                    ))
            {
                continue;
            }

            int oc =
                actionUnit
                    .AeldariObjectiveControlOverride > 0
                ? actionUnit
                    .AeldariObjectiveControlOverride
                : model.ObjectiveControl;

            total +=
                Mathf.Max(0, oc);
        }

        return total;
    }");

        // Keep connected-component casualty helpers consistent with the same
        // 2" horizontal / 5" vertical neighbour definition.
        source = Regex.Replace(
            source,
            @"HorizontalDistance\(\s*current\.transform\.position,\s*other\.transform\.position\s*\)\s*<=\s*2\.0f",
            "CoreRules11Geometry.WithinCoherencyNeighbour(current, other)");

        File.WriteAllText(path, source);
    }

    private static void PatchObjectiveGeometry()
    {
        const string path =
            "Assets/Scripts/Core/ObjectiveController.cs";

        string source =
            File.ReadAllText(path);

        source = ReplaceMethod(
            source,
            "UnitWithinRange",
@"    public bool UnitWithinRange(
        SquadController squad)
    {
        if (squad == null ||
            !squad.IsAlive ||
            !squad.IsOnBattlefield)
        {
            return false;
        }

        return squad
            .JoinedLivingModelTokens()
            .Any(
                model =>
                    CoreRules11Geometry
                        .ModelWithinObjective(
                            model,
                            transform.position,
                            ControlRadius
                        )
            );
    }");

        File.WriteAllText(path, source);
    }

    private static void PatchEngagementGeometry()
    {
        MethodLocation location =
            FindGameMethod(
                "UnitsAreEngaged");

        string replacement =
@"    public bool UnitsAreEngaged(
        SquadController a,
        SquadController b)
    {
        return
            CoreRules11Geometry
                .UnitsEngaged(a, b);
    }";

        ReplaceGameMethod(
            location,
            replacement);
    }

    private static void PatchObjectiveTiming()
    {
        PatchGameMethod(
            "CompletePhaseTransition",
            method =>
            {
                if (method.Contains(
                        "ResolveCoreObjectiveControlTiming();"))
                {
                    return method;
                }

                return InsertAtMethodStart(
                    method,
                    "        // 11e 14.02.01: objective control is determined before other end-of-phase rules.\n" +
                    "        ResolveCoreObjectiveControlTiming();\n\n");
            });

        PatchGameMethod(
            "EndTurn",
            method =>
            {
                if (method.Contains(
                        "ResolveCoreObjectiveControlTiming();"))
                {
                    return method;
                }

                return InsertAtMethodStart(
                    method,
                    "        // 11e: objective control is determined first at end of turn, before other rules and mission resolution.\n" +
                    "        ResolveCoreObjectiveControlTiming();\n\n");
            });
    }

    private static void PatchMovementSelectionCompletion()
    {
        PatchGameMethod(
            "NextPhase",
            method =>
            {
                if (method.Contains(
                        "ResolveImplicitRemainStationarySelections();"))
                {
                    return method;
                }

                Regex moveBlock =
                    new Regex(
                        @"if\s*\(phase\s*==\s*Phase\.Move\)\s*\{");

                Match moveMatch =
                    moveBlock.Match(method);

                if (!moveMatch.Success)
                {
                    throw new InvalidOperationException(
                        "NextPhase Movement-phase validation block not found.");
                }

                int brace =
                    method.IndexOf(
                        '{',
                        moveMatch.Index);

                return method.Insert(
                    brace + 1,
                    "\n            // 11e 09.02.01: every unit must be selected in the Move Units step. Untouched units resolve as Remain Stationary without move-start/end triggers.\n" +
                    "            ResolveImplicitRemainStationarySelections();\n");
            });
    }

    private static void PatchMissionActionEligibility()
    {
        const string path =
            "Assets/Scripts/Core/MissionSystem.cs";

        string source =
            File.ReadAllText(path);

        MethodLocation location =
            FindMethodInSource(
                path,
                source,
                "CanStartMissionAction");

        if (location.Text.Contains(
                "CoreRules11Actions.CanStart"))
        {
            return;
        }

        string patched =
            InsertAtMethodStart(
                location.Text,
                "        if (!CoreRules11Actions.CanStart(\n" +
                "                game,\n" +
                "                unit,\n" +
                "                out reason))\n" +
                "        {\n" +
                "            return false;\n" +
                "        }\n\n");

        source =
            source.Substring(
                0,
                location.Start) +
            patched +
            source.Substring(
                location.EndExclusive);

        File.WriteAllText(path, source);
    }

    private static void PatchAutomaticAttackCoreRules()
    {
        const string path =
            "Assets/Scripts/Core/RulesEngine.cs";

        string source =
            File.ReadAllText(path);

        MethodLocation location =
            FindMethodInSource(
                path,
                source,
                "ResolveWeaponAttacks");

        string method =
            location.Text;

        if (!method.Contains(
                "v39 11e Benefit of Cover"))
        {
            const string skillAnchor =
                "            bool torrent =";

            int skillIndex =
                method.IndexOf(
                    skillAnchor,
                    StringComparison.Ordinal);

            if (skillIndex < 0)
            {
                throw new InvalidOperationException(
                    "RulesEngine skill/weapon-rule anchor not found.");
            }

            string coverSkill =
@"            // v39 11e Benefit of Cover: cover worsens the attacking BS
            // characteristic by 1; it does not improve the target's save.
            bool v39BenefitOfCover =
                mode == AttackMode.Ranged &&
                game != null &&
                (game.TargetUnitHasCoverFromShooter(
                    model,
                    target
                 ) ||
                 UniversalRuleRegistry.UnitHasRule(
                    target.JoinedActionController(),
                    "stealth"
                 ));

            bool v39IgnoresCover =
                WeaponRuleParser.Has(
                    weapon,
                    "ignores_cover"
                );

            if (v39BenefitOfCover &&
                !v39IgnoresCover)
            {
                skill =
                    Mathf.Min(
                        7,
                        skill + 1
                    );
            }

";

            method =
                method.Insert(
                    skillIndex,
                    coverSkill);
        }

        // Remove the old 10e-style save improvement block. It starts at the
        // local 'bool hasCover' declaration and ends immediately before the
        // save roll declaration.
        int oldCoverStart =
            method.IndexOf(
                "                bool hasCover =",
                StringComparison.Ordinal);

        if (oldCoverStart >= 0)
        {
            int saveRollStart =
                method.IndexOf(
                    "                int saveRoll =",
                    oldCoverStart,
                    StringComparison.Ordinal);

            if (saveRollStart < 0)
            {
                throw new InvalidOperationException(
                    "RulesEngine old cover block found but save roll anchor is missing.");
            }

            method =
                method.Remove(
                    oldCoverStart,
                    saveRollStart -
                    oldCoverStart);
        }

        // The interactive attack path already applies FNP. Bring the automatic
        // RulesEngine path into parity for normal, devastating and hazard damage.
        method = Regex.Replace(
            method,
            @"allocated\.ApplyDamage\(\s*rolledDamage\s*\)",
            "allocated.ApplyDamage(\n" +
            "                            UniversalRuleRegistry.ApplyFeelNoPain(\n" +
            "                                allocated.Squad,\n" +
            "                                rolledDamage,\n" +
            "                                weapon.displayName\n" +
            "                            )\n" +
            "                        )");

        method = Regex.Replace(
            method,
            @"allocated\.ApplyDamage\(\s*mortalDamage\s*\)",
            "allocated.ApplyDamage(\n" +
            "                        UniversalRuleRegistry.ApplyFeelNoPain(\n" +
            "                            allocated.Squad,\n" +
            "                            mortalDamage,\n" +
            "                            \"Devastating Wounds: \" +\n" +
            "                            weapon.displayName\n" +
            "                        )\n" +
            "                    )");

        method = Regex.Replace(
            method,
            @"hazardTarget\.ApplyDamage\(\s*mortalWounds\s*\)",
            "hazardTarget.ApplyDamage(\n" +
            "                    UniversalRuleRegistry.ApplyFeelNoPain(\n" +
            "                        hazardTarget.Squad,\n" +
            "                        mortalWounds,\n" +
            "                        \"Hazardous\"\n" +
            "                    )\n" +
            "                )");

        source =
            source.Substring(
                0,
                location.Start) +
            method +
            source.Substring(
                location.EndExclusive);

        File.WriteAllText(path, source);
    }

    private static void InstallMarker()
    {
        const string path =
            "Assets/Scripts/Core/GameController.CoreRules11.cs";

        string source =
            File.ReadAllText(path);

        if (source.Contains(Marker))
            return;

        Regex classOpen =
            new Regex(
                @"public\s+partial\s+class\s+GameController\s*\{");

        Match match =
            classOpen.Match(source);

        if (!match.Success)
        {
            throw new InvalidOperationException(
                "Could not install the v39 marker in GameController.CoreRules11.cs.");
        }

        source =
            source.Insert(
                match.Index +
                match.Length,
                "\n    // " +
                Marker);

        File.WriteAllText(path, source);
    }

    private static void ValidateResult()
    {
        string squad =
            File.ReadAllText(
                "Assets/Scripts/Core/SquadController.cs");

        string objective =
            File.ReadAllText(
                "Assets/Scripts/Core/ObjectiveController.cs");

        string rules =
            File.ReadAllText(
                "Assets/Scripts/Core/RulesEngine.cs");

        string actions =
            File.ReadAllText(
                "Assets/Scripts/Core/MissionSystem.cs");

        string core =
            File.ReadAllText(
                "Assets/Scripts/Core/GameController.CoreRules11.cs");

        if (!squad.Contains(
                "WithinCoherencyNeighbour") ||
            !squad.Contains(
                "ModelWithinObjective") ||
            !objective.Contains(
                "ModelWithinObjective") ||
            !rules.Contains(
                "v39 11e Benefit of Cover") ||
            !actions.Contains(
                "CoreRules11Actions.CanStart") ||
            !core.Contains(Marker))
        {
            throw new InvalidOperationException(
                "v39 validation failed: one or more core-rule corrections were not installed.");
        }

        MethodLocation engagement =
            FindGameMethod(
                "UnitsAreEngaged");

        if (!engagement.Text.Contains(
                "CoreRules11Geometry"))
        {
            throw new InvalidOperationException(
                "v39 validation failed: engagement geometry was not installed.");
        }
    }

    private static void WriteReport()
    {
        StringBuilder report =
            new StringBuilder();

        report.AppendLine(
            "Warboard v39 Core Rules compliance migration complete.");
        report.AppendLine();
        report.AppendLine(
            "Installed:");
        report.AppendLine(
            "- 11e base-to-base 2/5 and 9/5 coherency geometry");
        report.AppendLine(
            "- 11e base-to-base 2/5 engagement geometry");
        report.AppendLine(
            "- 11e 3/5 objective range geometry");
        report.AppendLine(
            "- objective-control pre-pass before end-of-phase/end-of-turn rules");
        report.AppendLine(
            "- automatic Remain Stationary selection for untouched units when Movement ends");
        report.AppendLine(
            "- generic action eligibility gate");
        report.AppendLine(
            "- 11e cover as BS degradation in the automatic attack path");
        report.AppendLine(
            "- Feel No Pain parity in the automatic attack path");
        report.AppendLine();
        report.AppendLine(
            "See CORE_RULES_AUDIT_V39.md for full section-by-section status and remaining work.");

        File.WriteAllText(
            ReportPath,
            report.ToString());
    }

    private static void DeleteSelf()
    {
        AssetDatabase.DeleteAsset(SelfPath);
        AssetDatabase.Refresh();
    }

    private static void PatchGameMethod(
        string name,
        Func<string, string> patch)
    {
        MethodLocation location =
            FindGameMethod(name);

        string changed =
            patch(location.Text);

        ReplaceGameMethod(
            location,
            changed);
    }

    private static void ReplaceGameMethod(
        MethodLocation location,
        string replacement)
    {
        string source =
            File.ReadAllText(
                location.Path);

        source =
            source.Substring(
                0,
                location.Start) +
            replacement +
            source.Substring(
                location.EndExclusive);

        File.WriteAllText(
            location.Path,
            source);
    }

    private static MethodLocation FindGameMethod(
        string name)
    {
        foreach (string path in GamePaths)
        {
            if (!File.Exists(path))
                continue;

            string source =
                File.ReadAllText(path);

            try
            {
                return FindMethodInSource(
                    path,
                    source,
                    name);
            }
            catch (InvalidOperationException)
            {
                // Continue searching the remaining partials.
            }
        }

        throw new InvalidOperationException(
            "Could not locate GameController method: " +
            name);
    }

    private static string ReplaceMethod(
        string source,
        string name,
        string replacement)
    {
        MethodLocation location =
            FindMethodInSource(
                "<memory>",
                source,
                name);

        return
            source.Substring(
                0,
                location.Start) +
            replacement +
            source.Substring(
                location.EndExclusive);
    }

    private static MethodLocation FindMethodInSource(
        string path,
        string source,
        string name)
    {
        Regex signature =
            new Regex(
                @"(?m)^[ \t]*(?:public|private|protected|internal)\s+(?:(?:static|virtual|override|sealed|async|new)\s+)*(?:[A-Za-z0-9_<>,\.\[\]\?]+\s+)+" +
                Regex.Escape(name) +
                @"\s*\(");

        Match match =
            signature.Match(source);

        if (!match.Success)
        {
            throw new InvalidOperationException(
                "Method not found: " +
                name +
                " in " +
                path);
        }

        int brace =
            source.IndexOf(
                '{',
                match.Index +
                match.Length);

        if (brace < 0)
        {
            throw new InvalidOperationException(
                "Method body not found: " +
                name);
        }

        int depth = 0;
        bool inString = false;
        bool verbatim = false;
        bool inChar = false;
        bool escape = false;

        for (int i = brace;
             i < source.Length;
             i++)
        {
            char c = source[i];

            if (inString)
            {
                if (verbatim)
                {
                    if (c == '"')
                    {
                        if (i + 1 < source.Length &&
                            source[i + 1] == '"')
                        {
                            i++;
                        }
                        else
                        {
                            inString = false;
                            verbatim = false;
                        }
                    }
                }
                else if (escape)
                {
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (inChar)
            {
                if (escape)
                    escape = false;
                else if (c == '\\')
                    escape = true;
                else if (c == '\'')
                    inChar = false;

                continue;
            }

            if (c == '"')
            {
                inString = true;
                verbatim =
                    i > 0 &&
                    source[i - 1] == '@';
                continue;
            }

            if (c == '\'')
            {
                inChar = true;
                continue;
            }

            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return new MethodLocation
                    {
                        Path = path,
                        Source = source,
                        Start = match.Index,
                        EndExclusive = i + 1,
                        Text =
                            source.Substring(
                                match.Index,
                                i + 1 -
                                match.Index)
                    };
                }
            }
        }

        throw new InvalidOperationException(
            "Unterminated method body: " +
            name);
    }

    private static string InsertAtMethodStart(
        string method,
        string text)
    {
        int brace =
            method.IndexOf('{');

        if (brace < 0)
            return method;

        return method.Insert(
            brace + 1,
            "\n" +
            text);
    }
}
#endif
