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
/// Installs the complete Edition 11 Aeldari faction-pack runtime against the
/// current split Warboard source. This is a one-time source migration only;
/// no bridge, polling loop or reflection layer remains at runtime.
/// </summary>
[InitializeOnLoad]
public static class WarboardV42AeldariFactionRules
{
    private const string SelfPath =
        "Assets/Editor/WarboardV42AeldariFactionRules.cs";

    private const string BackupRoot =
        "Library/WarboardBackups/V42";

    private const string ReportPath =
        "Library/WarboardV42AeldariFactionRulesReport.txt";

    private const string Marker =
        "WARBOARD_V42_FULL_AELDARI_FACTION_RULES";

    static WarboardV42AeldariFactionRules()
    {
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Warboard/Developer/Re-run v42 Full Aeldari Faction Rules")]
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
            ValidatePrerequisites();

            if (AlreadyApplied())
            {
                CleanupSelf();
                return;
            }

            Directory.CreateDirectory(BackupRoot);

            List<string> touched =
                new List<string>();

            PatchAeldariRulesSystem(touched);
            PatchGameController(touched);
            PatchInteractiveAttack(touched);
            PatchRulesEngine(touched);
            PatchSquadController(touched);
            PatchUniversalRuleEngine(touched);
            PatchCoreCompletionEmbark(touched);

            ValidateResult();
            WriteMarker();
            WriteReport(touched);

            Debug.Log(
                "[Warboard v42] Full Aeldari faction rules installed. " +
                "Unity will compile once more."
            );

            AssetDatabase.Refresh();
            EditorApplication.delayCall += CleanupSelf;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Warboard v42] Aeldari faction-rule migration failed. " +
                ex
            );
        }
    }

    private static void ValidatePrerequisites()
    {
        string[] required =
        {
            "Assets/Scripts/Core/GameController.cs",
            "Assets/Scripts/Core/AeldariRulesSystem.cs",
            "Assets/Scripts/Core/SquadController.cs",
            "Assets/Scripts/Core/RulesEngine.cs",
            "Assets/Scripts/Core/InteractiveAttackController.cs",
            "Assets/Scripts/Core/UniversalRuleEngine.cs",
            "Assets/Scripts/Factions/Aeldari/AeldariGameController.cs",
            "Assets/Scripts/Factions/Aeldari/AeldariBattleFocusController.cs",
            "Assets/Scripts/Factions/Aeldari/AeldariDetachmentRuntime.cs",
            "Assets/Scripts/Factions/Aeldari/AeldariFactionPack11.cs",
            "Assets/Scripts/Factions/Aeldari/AeldariFactionPack11Runtime.cs",
            "Assets/Scripts/Core/GameController.AeldariFaction11.cs"
        };

        foreach (string path in required)
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    "Required v42 file is missing: " + path
                );
            }
        }

        FileInfo main =
            new FileInfo(
                "Assets/Scripts/Core/GameController.cs"
            );

        if (main.Length > 120000)
        {
            throw new InvalidOperationException(
                "Safety stop: v42 expects the split GameController architecture."
            );
        }

        if (!File.Exists(
                "Assets/Scripts/Core/GameController.CoreCompletion11.cs"))
        {
            throw new InvalidOperationException(
                "v42 requires the v41 core-completion source."
            );
        }
    }

    private static bool AlreadyApplied()
    {
        string markerPath =
            "Assets/Scripts/Factions/Aeldari/AeldariFactionPack11.cs";

        if (!File.Exists(markerPath))
            return false;

        string catalog =
            File.ReadAllText(markerPath);

        if (!catalog.Contains(Marker))
            return false;

        string allGame =
            string.Join(
                "\n",
                ExistingGameFiles()
                    .Select(File.ReadAllText)
                    .ToArray()
            );

        return
            allGame.Contains("DrawAeldari11StratagemCards") &&
            allGame.Contains("Aeldari11PumpDeferredReactions") &&
            allGame.Contains("Aeldari11ModifyStratagemCost") &&
            File.ReadAllText(
                "Assets/Scripts/Core/AeldariRulesSystem.cs"
            ).Contains("AeldariFactionPack11.ApplyAttackModifiers") &&
            File.ReadAllText(
                "Assets/Scripts/Core/InteractiveAttackController.cs"
            ).Contains("AeldariFactionPack11.CriticalWoundThreshold") &&
            File.ReadAllText(
                "Assets/Scripts/Core/RulesEngine.cs"
            ).Contains("AeldariFactionPack11.AutomaticHitSucceeds");
    }

    private static void PatchAeldariRulesSystem(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/AeldariRulesSystem.cs";

        string source =
            File.ReadAllText(path);

        source = ReplaceMethodBody(
            source,
            "public void ApplyAttackModifiers(",
            "        AeldariFactionPack11.ApplyAttackModifiers(\n" +
            "            game, attacker, target, weapon, mode, state);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public int MinimumSustainedHits(",
            "        return AeldariFactionPack11.MinimumSustainedHits(\n" +
            "            attacker, weapon, mode);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public bool GrantsLethalHits(",
            "        return AeldariFactionPack11.GrantsLethalHits(\n" +
            "            attacker, mode);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public bool GrantsDevastatingWounds(",
            "        return AeldariFactionPack11.GrantsDevastatingWounds(\n" +
            "            attacker, null, mode);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public int ApModifier(",
            "        return AeldariFactionPack11.ApModifier(\n" +
            "            attacker, target, weapon, mode);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public int DamageModifier(",
            "        return AeldariFactionPack11.DamageModifier(\n" +
            "            attacker, weapon, mode);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public int InvulnerableOverride(",
            "        return AeldariFactionPack11.InvulnerableOverride(unit);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public float RangedRangeModifier(",
            "        return AeldariFactionPack11.RangedRangeModifier(\n" +
            "            attacker, weapon);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public bool IgnoresCover(",
            "        return AeldariFactionPack11.GrantsIgnoresCover(\n" +
            "            attacker, AttackMode.Ranged);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public bool CanMoveThroughEnemyModelsWhenCharging(",
            "        return AeldariFactionPack11.CanMoveThroughEnemyModelsWhenCharging(unit);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public bool CanRerollAdvance(",
            "        return AeldariFactionPack11.CanRerollAdvance(unit);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public bool CanChargeAfterAdvance(",
            "        return AeldariFactionPack11.CanChargeAfterAdvance(unit);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public bool CanChargeAfterFallBack(",
            "        return AeldariFactionPack11.CanChargeAfterFallBack(unit);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public bool CanShootAfterFallBack(",
            "        return AeldariFactionPack11.CanShootAfterFallBack(unit);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public bool VehicleRangedHasAssault(",
            "        return AeldariFactionPack11.VehicleRangedHasAssault(unit);\n"
        );

        source = ReplaceMethodBody(
            source,
            "public bool HasRange18Protection(",
            "        return AeldariFactionPack11.HasRange18Protection(unit);\n"
        );

        WriteChanged(path, source, touched);
    }

    private static void PatchGameController(
        List<string> touched)
    {
        PatchGameMethod(
            "Update",
            method =>
            {
                if (method.Contains(
                        "Aeldari11PumpDeferredReactions();"))
                {
                    return method;
                }

                return InsertAtMethodStart(
                    method,
                    "        Aeldari11PumpDeferredReactions();\n\n"
                );
            },
            touched
        );

        PatchGameMethod(
            "DrawStratagemMenu",
            method =>
            {
                if (method.Contains(
                        "DrawAeldari11StratagemCards("))
                {
                    return method;
                }

                int start =
                    method.IndexOf(
                        "if (isDevotedOfYnnead",
                        StringComparison.Ordinal);

                int necrons =
                    start >= 0
                    ? method.IndexOf(
                        "else if (isNecrons)",
                        start,
                        StringComparison.Ordinal)
                    : -1;

                if (start < 0 ||
                    necrons < 0)
                {
                    // Fallback: replace only the generic Aeldari branch.
                    int generic =
                        method.IndexOf(
                            "else if (isAeldari)",
                            StringComparison.Ordinal);

                    if (generic < 0)
                    {
                        throw new InvalidOperationException(
                            "DrawStratagemMenu Aeldari branch was not found."
                        );
                    }

                    int open =
                        method.IndexOf('{', generic);
                    int close =
                        FindMatchingBrace(method, open);

                    return
                        method.Substring(0, generic) +
                        "else if (isAeldari)\n" +
                        "        {\n" +
                        "            DrawAeldari11StratagemCards(\n" +
                        "                left, right, y, cardWidth);\n" +
                        "        }" +
                        method.Substring(close + 1);
                }

                string replacement =
                    "if (isAeldari)\n" +
                    "        {\n" +
                    "            DrawAeldari11StratagemCards(\n" +
                    "                left, right, y, cardWidth);\n" +
                    "        }\n        ";

                return
                    method.Substring(0, start) +
                    replacement +
                    method.Substring(necrons);
            },
            touched
        );

        PatchGameMethod(
            "SpendBattleFocusFor",
            method =>
            {
                if (Regex.IsMatch(
                        method,
                        @"SpendBattleFocus\s*\(\s*1\s*,\s*manoeuvre\s*,\s*unit\s*\)"))
                {
                    return method;
                }

                string patched =
                    new Regex(
                        @"factionController\s*\.\s*SpendBattleFocus\s*\(\s*1\s*,\s*manoeuvre\s*\)"
                    ).Replace(
                        method,
                        "factionController.SpendBattleFocus(1, manoeuvre, unit)",
                        1
                    );

                if (patched == method)
                {
                    throw new InvalidOperationException(
                        "SpendBattleFocusFor spend call was not found."
                    );
                }

                return patched;
            },
            touched
        );

        PatchGameMethod(
            "SpendFactionStratagemCP",
            method =>
            {
                if (method.Contains(
                        "Aeldari11ModifyStratagemCost("))
                {
                    return method;
                }

                Regex cost =
                    new Regex(
                        @"int\s+cost\s*=\s*Mathf\.Max\s*\(\s*0\s*,\s*baseCost\s*\)\s*;",
                        RegexOptions.Singleline
                    );

                Match match =
                    cost.Match(method);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "SpendFactionStratagemCP cost anchor was not found."
                    );
                }

                return method.Insert(
                    match.Index +
                    match.Length,
                    "\n\n        cost =\n" +
                    "            Aeldari11ModifyStratagemCost(\n" +
                    "                unit, label, cost);"
                );
            },
            touched
        );

        PatchGameMethod(
            "TryCharge",
            method =>
            {
                if (method.Contains(
                        "Aeldari11CanCharge(attacker)"))
                {
                    return method;
                }

                return InsertAtMethodStart(
                    method,
                    "        if (attacker != null &&\n" +
                    "            !Aeldari11CanCharge(attacker))\n" +
                    "        {\n" +
                    "            status = attacker.DisplayName +\n" +
                    "                \" cannot declare a charge this turn because of an Aeldari rule.\";\n" +
                    "            return;\n" +
                    "        }\n\n"
                );
            },
            touched
        );
    }

    private static void PatchInteractiveAttack(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/InteractiveAttackController.cs";

        string source =
            File.ReadAllText(path);

        MethodLocation build =
            FindMethodInSource(
                path,
                source,
                "BuildVolleys"
            );

        string method =
            build.Text;

        if (!method.Contains(
                "AeldariFactionPack11.StrengthModifier"))
        {
            method = new Regex(
                @"volley\.effectiveStrength\s*=\s*weapon\.strength\s*;"
            ).Replace(
                method,
                "volley.effectiveStrength =\n" +
                "                weapon.strength +\n" +
                "                AeldariFactionPack11.StrengthModifier(\n" +
                "                    attacker, weapon, mode);",
                1
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.AdditionalAttacks"))
        {
            // Remove the old bespoke Borrowed Vigour branch so the complete
            // faction-pack helper owns both Borrowed Vigour and Weavers' Wail.
            method = new Regex(
                @"\s*if\s*\(mode\s*==\s*AttackMode\.Melee\s*&&\s*UniversalRuleRegistry\.UnitHasRule\s*\(\s*selection\.model\.Squad\s*,\s*""Borrowed Vigour""\s*\)\s*\)\s*\{\s*oneModelAttacks\s*\+=\s*2\s*;\s*\}",
                RegexOptions.Singleline
            ).Replace(
                method,
                "",
                1
            );

            int one =
                method.IndexOf(
                    "int oneModelAttacks =",
                    StringComparison.Ordinal);

            if (one < 0)
            {
                throw new InvalidOperationException(
                    "Interactive BuildVolleys attack-count anchor was not found."
                );
            }

            int semi =
                FindStatementSemicolon(method, one);

            method = method.Insert(
                semi + 1,
                "\n\n                oneModelAttacks +=\n" +
                "                    AeldariFactionPack11.AdditionalAttacks(\n" +
                "                        attacker, selection.model, weapon, mode);"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.AdditionalRapidFire"))
        {
            int rapid =
                method.IndexOf(
                    "int rapid =",
                    StringComparison.Ordinal);

            if (rapid < 0)
            {
                throw new InvalidOperationException(
                    "Interactive BuildVolleys Rapid Fire anchor was not found."
                );
            }

            int semi =
                FindStatementSemicolon(method, rapid);

            method = method.Insert(
                semi + 1,
                "\n\n                rapid +=\n" +
                "                    AeldariFactionPack11.AdditionalRapidFire(\n" +
                "                        attacker, weapon, mode);"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.GrantsPrecision"))
        {
            int precision =
                method.IndexOf(
                    "volley.precision =",
                    StringComparison.Ordinal);

            if (precision < 0)
            {
                throw new InvalidOperationException(
                    "Interactive BuildVolleys Precision anchor was not found."
                );
            }

            int semi =
                FindStatementSemicolon(method, precision);

            method = method.Insert(
                semi + 1,
                "\n\n            volley.precision =\n" +
                "                volley.precision ||\n" +
                "                AeldariFactionPack11.GrantsPrecision(\n" +
                "                    attacker, weapon, mode);"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.CriticalWoundThreshold"))
        {
            int critical =
                method.IndexOf(
                    "volley.criticalWoundThreshold =",
                    StringComparison.Ordinal);

            if (critical < 0)
            {
                throw new InvalidOperationException(
                    "Interactive BuildVolleys critical-wound anchor was not found."
                );
            }

            int semi =
                FindStatementSemicolon(method, critical);

            method = method.Insert(
                semi + 1,
                "\n\n            volley.criticalWoundThreshold =\n" +
                "                AeldariFactionPack11.CriticalWoundThreshold(\n" +
                "                    attacker, target, weapon,\n" +
                "                    volley.criticalWoundThreshold);"
            );
        }

        source = ReplaceLocation(
            source,
            build,
            method
        );

        MethodLocation hits =
            FindMethodInSource(
                path,
                source,
                "RecalculateHitResults"
            );

        method = hits.Text;

        if (!method.Contains(
                "AeldariFactionPack11.IsCriticalHit"))
        {
            method = method.Replace(
                "            if (roll == 6)\n",
                "            if (AeldariFactionPack11.IsCriticalHit(\n" +
                "                    attacker, target, volley.weapon,\n" +
                "                    roll, success))\n"
            );
        }

        source = ReplaceLocation(
            source,
            hits,
            method
        );

        WriteChanged(path, source, touched);
    }

    private static void PatchRulesEngine(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/RulesEngine.cs";

        string source =
            File.ReadAllText(path);

        MethodLocation location =
            FindMethodInSource(
                path,
                source,
                "ResolveWeaponAttacks"
            );

        string method =
            location.Text;

        if (!method.Contains(
                "AeldariFactionPack11.AdditionalAttacks"))
        {
            int attacks =
                method.IndexOf(
                    "int attacks =",
                    StringComparison.Ordinal);

            if (attacks < 0)
            {
                throw new InvalidOperationException(
                    "RulesEngine attack-count anchor was not found."
                );
            }

            int semi =
                FindStatementSemicolon(method, attacks);

            method = method.Insert(
                semi + 1,
                "\n\n            attacks +=\n" +
                "                AeldariFactionPack11.AdditionalAttacks(\n" +
                "                    attacker, model, weapon, mode);"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.AdditionalRapidFire"))
        {
            int rapid =
                method.IndexOf(
                    "int rapidFire =",
                    StringComparison.Ordinal);

            if (rapid < 0)
            {
                throw new InvalidOperationException(
                    "RulesEngine Rapid Fire anchor was not found."
                );
            }

            int semi =
                FindStatementSemicolon(method, rapid);

            method = method.Insert(
                semi + 1,
                "\n\n            rapidFire +=\n" +
                "                AeldariFactionPack11.AdditionalRapidFire(\n" +
                "                    attacker, weapon, mode);"
            );
        }

        if (!method.Contains(
                "aeldari11UniversalState"))
        {
            int torrent =
                method.IndexOf(
                    "bool torrent =",
                    StringComparison.Ordinal);

            if (torrent < 0)
            {
                throw new InvalidOperationException(
                    "RulesEngine weapon-rule anchor was not found."
                );
            }

            method = method.Insert(
                torrent,
                "            UniversalAttackRuleState aeldari11UniversalState =\n" +
                "                UniversalRuleRegistry.BuildAttackState(\n" +
                "                    game, attacker, target, model, weapon, mode);\n\n"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.GrantsLethalHits"))
        {
            int lethal =
                method.IndexOf(
                    "bool lethalHits =",
                    StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, lethal);
            method = method.Insert(
                semi + 1,
                "\n\n            lethalHits =\n" +
                "                lethalHits ||\n" +
                "                AeldariFactionPack11.GrantsLethalHits(\n" +
                "                    attacker, mode);"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.MinimumSustainedHits"))
        {
            int sustained =
                method.IndexOf(
                    "int sustainedHits =",
                    StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, sustained);
            method = method.Insert(
                semi + 1,
                "\n\n            sustainedHits =\n" +
                "                Mathf.Max(\n" +
                "                    sustainedHits,\n" +
                "                    AeldariFactionPack11.MinimumSustainedHits(\n" +
                "                        attacker, weapon, mode));"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.GrantsDevastatingWounds"))
        {
            int dev =
                method.IndexOf(
                    "bool devastating =",
                    StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, dev);
            method = method.Insert(
                semi + 1,
                "\n\n            devastating =\n" +
                "                devastating ||\n" +
                "                AeldariFactionPack11.GrantsDevastatingWounds(\n" +
                "                    attacker, weapon, mode);"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.GrantsPrecision"))
        {
            int precision =
                method.IndexOf(
                    "bool precision =",
                    StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, precision);
            method = method.Insert(
                semi + 1,
                "\n\n            precision =\n" +
                "                precision ||\n" +
                "                AeldariFactionPack11.GrantsPrecision(\n" +
                "                    attacker, weapon, mode);"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.AutomaticHitSucceeds"))
        {
            method = new Regex(
                @"if\s*\(hitRoll\s*<\s*skill\)"
            ).Replace(
                method,
                "if (!AeldariFactionPack11.AutomaticHitSucceeds(\n" +
                "                        hitRoll, skill, aeldari11UniversalState))",
                2
            );

            method = method.Replace(
                "                if (hitRoll == 6)\n",
                "                if (AeldariFactionPack11.IsCriticalHit(\n" +
                "                        attacker, target, weapon, hitRoll, true))\n"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.AutomaticWoundSucceeds"))
        {
            method = new Regex(
                @"bool\s+success\s*=\s*woundRoll\s*>=\s*woundTarget\s*;"
            ).Replace(
                method,
                "bool success =\n" +
                "                    AeldariFactionPack11.AutomaticWoundSucceeds(\n" +
                "                        woundRoll, woundTarget, criticalThreshold,\n" +
                "                        aeldari11UniversalState.woundRollModifier);",
                1
            );

            method = new Regex(
                @"success\s*=\s*woundRoll\s*>=\s*woundTarget\s*;"
            ).Replace(
                method,
                "success =\n" +
                "                        AeldariFactionPack11.AutomaticWoundSucceeds(\n" +
                "                            woundRoll, woundTarget, criticalThreshold,\n" +
                "                            aeldari11UniversalState.woundRollModifier);",
                2
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.StrengthModifier"))
        {
            method = method.Replace(
                "                    weapon.strength,\n                    target.Toughness",
                "                    weapon.strength +\n" +
                "                    AeldariFactionPack11.StrengthModifier(\n" +
                "                        attacker, weapon, mode),\n" +
                "                    target.Toughness"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.CriticalWoundThreshold"))
        {
            int critical =
                method.IndexOf(
                    "int criticalThreshold =",
                    StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, critical);
            method = method.Insert(
                semi + 1,
                "\n\n            criticalThreshold =\n" +
                "                AeldariFactionPack11.CriticalWoundThreshold(\n" +
                "                    attacker, target, weapon, criticalThreshold);"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.ApModifier"))
        {
            method = method.Replace(
                "                        ) -\n                        weapon.ap,",
                "                        ) -\n" +
                "                        (weapon.ap +\n" +
                "                         AeldariFactionPack11.ApModifier(\n" +
                "                            attacker, target, weapon, mode)),"
            );
        }

        if (!method.Contains(
                "AeldariFactionPack11.DamageModifier"))
        {
            method = method.Replace(
                "                            ) +\n                            melta\n",
                "                            ) +\n" +
                "                            melta +\n" +
                "                            AeldariFactionPack11.DamageModifier(\n" +
                "                                attacker, weapon, mode)\n"
            );

            method = method.Replace(
                "                        ) +\n                        melta\n",
                "                        ) +\n" +
                "                        melta +\n" +
                "                        AeldariFactionPack11.DamageModifier(\n" +
                "                            attacker, weapon, mode)\n"
            );
        }

        // A die can never be re-rolled more than once. If an Aeldari rule
        // has already re-rolled the wound die, Twin-linked must not re-roll it
        // again.
        method = method.Replace(
            "                if (!success &&\n                    twinLinked)\n",
            "                if (!success &&\n                    !alreadyRerolled &&\n                    twinLinked)\n"
        );

        // Automatic rerolls from Aeldari rule state and Morbid Might.
        if (!method.Contains(
                "AeldariFactionPack11.AutomaticRerollHit"))
        {
            string hitAnchor =
                "                if (!AeldariFactionPack11.AutomaticHitSucceeds(";

            int hitCheck =
                method.IndexOf(
                    hitAnchor,
                    StringComparison.Ordinal);

            if (hitCheck >= 0)
            {
                method = method.Insert(
                    hitCheck,
                    "                if (!aeldari11UniversalState.cannotRerollHits &&\n" +
                    "                    AeldariFactionPack11.AutomaticRerollHit(\n" +
                    "                        attacker, hitRoll, skill, aeldari11UniversalState))\n" +
                    "                {\n" +
                    "                    hitRoll = DiceRoller.RollD6(\n" +
                    "                        \"Aeldari Hit re-roll: \" + weapon.displayName);\n" +
                    "                }\n\n"
                );
            }
        }

        if (!method.Contains(
                "AeldariFactionPack11.AutomaticRerollWound"))
        {
            // The existing automatic resolver declares `alreadyRerolled`
            // immediately after the first wound-success calculation. Insert
            // Aeldari automatic rerolls only after that declaration so the
            // generated source never references the variable before it is in
            // scope.
            string rerollStateAnchor =
                "                bool alreadyRerolled =";

            int rerollState =
                method.IndexOf(
                    rerollStateAnchor,
                    StringComparison.Ordinal);

            if (rerollState >= 0)
            {
                int declarationEnd =
                    method.IndexOf(';', rerollState);

                method = method.Insert(
                    declarationEnd + 1,
                    "\n\n                if (AeldariFactionPack11.AutomaticRerollWound(\n" +
                    "                        attacker, woundRoll, success, mode))\n" +
                    "                {\n" +
                    "                    woundRoll = DiceRoller.RollD6(\n" +
                    "                        \"Aeldari Wound re-roll: \" + weapon.displayName);\n" +
                    "                    success = AeldariFactionPack11.AutomaticWoundSucceeds(\n" +
                    "                        woundRoll, woundTarget, criticalThreshold,\n" +
                    "                        aeldari11UniversalState.woundRollModifier);\n" +
                    "                    alreadyRerolled = true;\n" +
                    "                }"
                );
            }
        }

        source = ReplaceLocation(
            source,
            location,
            method
        );

        WriteChanged(path, source, touched);
    }

    private static void PatchSquadController(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/SquadController.cs";

        string source =
            File.ReadAllText(path);

        MethodLocation location =
            FindMethodInSource(
                path,
                source,
                "EffectiveObjectiveControl"
            );

        string method =
            location.Text;

        if (!method.Contains(
                "AeldariFactionPack11.ModifyObjectiveControl"))
        {
            int returnIndex =
                method.LastIndexOf(
                    "        return Mathf.Max(",
                    StringComparison.Ordinal);

            if (returnIndex < 0)
            {
                throw new InvalidOperationException(
                    "EffectiveObjectiveControl return anchor was not found."
                );
            }

            method = method.Insert(
                returnIndex,
                "        objectiveControl =\n" +
                "            AeldariFactionPack11.ModifyObjectiveControl(\n" +
                "                JoinedActionController(), model, objectiveControl);\n\n"
            );
        }

        source = ReplaceLocation(
            source,
            location,
            method
        );

        WriteChanged(path, source, touched);
    }

    private static void PatchUniversalRuleEngine(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/UniversalRuleEngine.cs";

        string source =
            File.ReadAllText(path);

        MethodLocation hasRule =
            FindMethodInSource(
                path,
                source,
                "UnitHasRule"
            );

        string method =
            hasRule.Text;

        if (!method.Contains(
                "AeldariFactionPack11.GrantsCoreAbility"))
        {
            int data =
                method.IndexOf(
                    "        UnitData data =",
                    StringComparison.Ordinal);

            if (data < 0)
            {
                throw new InvalidOperationException(
                    "UniversalRuleEngine.UnitHasRule data anchor was not found."
                );
            }

            method = method.Insert(
                data,
                "        if (AeldariFactionPack11.GrantsCoreAbility(\n" +
                "                squad, ruleName))\n" +
                "        {\n" +
                "            return true;\n" +
                "        }\n\n"
            );
        }

        source = ReplaceLocation(
            source,
            hasRule,
            method
        );

        if (!source.Contains(
                "Aeldari11HasRuneOfMistsCover"))
        {
            source = new Regex(
                @"game\.TargetUnitHasCoverFromShooter\s*\(\s*shooter\s*,\s*target\s*\)"
            ).Replace(
                source,
                "(game.TargetUnitHasCoverFromShooter(shooter, target) ||\n" +
                "                 game.Aeldari11HasRuneOfMistsCover(target, shooter))",
                1
            );
        }

        WriteChanged(path, source, touched);
    }

    private static void PatchCoreCompletionEmbark(
        List<string> touched)
    {
        PatchGameMethod(
            "Core11Embark",
            method =>
            {
                if (method.Contains(
                        "Aeldari11CanEmbark(passenger)"))
                {
                    return method;
                }

                return InsertAtMethodStart(
                    method,
                    "        if (passenger != null &&\n" +
                    "            !Aeldari11CanEmbark(passenger))\n" +
                    "        {\n" +
                    "            status = passenger.DisplayName +\n" +
                    "                \" cannot embark this turn because of an Aeldari rule.\";\n" +
                    "            return;\n" +
                    "        }\n\n"
                );
            },
            touched
        );
    }

    private static void ValidateResult()
    {
        string catalog =
            File.ReadAllText(
                "Assets/Scripts/Factions/Aeldari/AeldariFactionPack11.cs"
            );

        int stratagems =
            Regex.Matches(
                catalog,
                "new AeldariStratagem11"
            ).Count;

        int enhancements =
            Regex.Matches(
                catalog,
                "new AeldariEnhancement11"
            ).Count;

        if (stratagems != 78 ||
            enhancements != 52)
        {
            throw new InvalidOperationException(
                "Faction-pack catalogue is incomplete: " +
                stratagems + " stratagems / " +
                enhancements + " enhancements."
            );
        }

        string rules =
            File.ReadAllText(
                "Assets/Scripts/Core/AeldariRulesSystem.cs"
            );

        string interactive =
            File.ReadAllText(
                "Assets/Scripts/Core/InteractiveAttackController.cs"
            );

        string automatic =
            File.ReadAllText(
                "Assets/Scripts/Core/RulesEngine.cs"
            );

        string squad =
            File.ReadAllText(
                "Assets/Scripts/Core/SquadController.cs"
            );

        string universal =
            File.ReadAllText(
                "Assets/Scripts/Core/UniversalRuleEngine.cs"
            );

        string allGame =
            string.Join(
                "\n",
                ExistingGameFiles()
                    .Select(File.ReadAllText)
                    .ToArray()
            );

        if (!rules.Contains(
                "AeldariFactionPack11.ApplyAttackModifiers") ||
            !interactive.Contains(
                "AeldariFactionPack11.CriticalWoundThreshold") ||
            !automatic.Contains(
                "AeldariFactionPack11.AutomaticHitSucceeds") ||
            !squad.Contains(
                "AeldariFactionPack11.ModifyObjectiveControl") ||
            !universal.Contains(
                "AeldariFactionPack11.GrantsCoreAbility") ||
            !allGame.Contains(
                "DrawAeldari11StratagemCards") ||
            !allGame.Contains(
                "Aeldari11PumpDeferredReactions") ||
            !allGame.Contains(
                "Aeldari11CanEmbark(passenger)"))
        {
            throw new InvalidOperationException(
                "v42 validation failed: one or more faction-rule integrations were not installed."
            );
        }
    }

    private static void WriteMarker()
    {
        const string path =
            "Assets/Scripts/Factions/Aeldari/AeldariFactionPack11.cs";

        string source =
            File.ReadAllText(path);

        if (source.Contains(Marker))
            return;

        int classIndex =
            source.IndexOf(
                "public static class AeldariFactionPack11",
                StringComparison.Ordinal);

        int brace =
            classIndex >= 0
            ? source.IndexOf('{', classIndex)
            : -1;

        if (brace < 0)
        {
            throw new InvalidOperationException(
                "Could not install v42 marker."
            );
        }

        source = source.Insert(
            brace + 1,
            "\n    // " + Marker + "\n"
        );

        File.WriteAllText(path, source);
    }

    private static void WriteReport(
        List<string> touched)
    {
        StringBuilder report =
            new StringBuilder();

        report.AppendLine("WARBOARD v42 — FULL AELDARI FACTION RULES");
        report.AppendLine();
        report.AppendLine("Installed against Aeldari Faction Pack 11e v1.1, July 2026.");
        report.AppendLine("15 detachments / 78 stratagems / 52 enhancements.");
        report.AppendLine();
        report.AppendLine("Touched source:");

        foreach (string path in touched.Distinct())
            report.AppendLine(" - " + path);

        File.WriteAllText(
            ReportPath,
            report.ToString()
        );
    }

    private static IEnumerable<string> ExistingGameFiles()
    {
        return Directory
            .GetFiles(
                "Assets/Scripts/Core",
                "GameController*.cs",
                SearchOption.TopDirectoryOnly
            )
            .OrderBy(path => path)
            .ToArray();
    }

    private static void PatchGameMethod(
        string methodName,
        Func<string, string> patch,
        List<string> touched)
    {
        string path = null;
        MethodLocation location = null;
        string source = null;

        foreach (string candidate in ExistingGameFiles())
        {
            string candidateSource =
                File.ReadAllText(candidate);

            MethodLocation found =
                TryFindMethodInSource(
                    candidate,
                    candidateSource,
                    methodName
                );

            if (found == null)
                continue;

            path = candidate;
            source = candidateSource;
            location = found;
            break;
        }

        if (path == null ||
            location == null)
        {
            throw new InvalidOperationException(
                "GameController method not found: " + methodName
            );
        }

        string patchedMethod =
            patch(location.Text);

        if (patchedMethod == location.Text)
            return;

        string result =
            ReplaceLocation(
                source,
                location,
                patchedMethod
            );

        WriteChanged(path, result, touched);
    }

    private static string ReplaceMethodBody(
        string source,
        string signatureStart,
        string body)
    {
        int start =
            source.IndexOf(
                signatureStart,
                StringComparison.Ordinal);

        if (start < 0)
        {
            throw new InvalidOperationException(
                "Method signature was not found: " + signatureStart
            );
        }

        int open =
            source.IndexOf('{', start);

        if (open < 0)
            throw new InvalidOperationException("Method body open brace missing: " + signatureStart);

        int close =
            FindMatchingBrace(source, open);

        return
            source.Substring(0, open + 1) +
            "\n" +
            body +
            source.Substring(close);
    }

    private static string InsertAtMethodStart(
        string method,
        string text)
    {
        int open =
            method.IndexOf('{');

        if (open < 0)
            throw new InvalidOperationException("Method open brace missing.");

        return method.Insert(
            open + 1,
            "\n" + text
        );
    }

    private sealed class MethodLocation
    {
        public string Path;
        public int Start;
        public int EndExclusive;
        public string Text;
    }

    private static MethodLocation FindMethodInSource(
        string path,
        string source,
        string methodName)
    {
        MethodLocation result =
            TryFindMethodInSource(
                path,
                source,
                methodName
            );

        if (result == null)
        {
            throw new InvalidOperationException(
                "Method not found in " + path + ": " + methodName
            );
        }

        return result;
    }

    private static MethodLocation TryFindMethodInSource(
        string path,
        string source,
        string methodName)
    {
        Regex signature =
            new Regex(
                @"(?m)^\s*(?:public|private|protected|internal)\s+(?:static\s+)?[^\n\r;=]+\b" +
                Regex.Escape(methodName) +
                @"\s*\("
            );

        Match match =
            signature.Match(source);

        if (!match.Success)
            return null;

        int open =
            source.IndexOf('{', match.Index);

        if (open < 0)
            return null;

        int close =
            FindMatchingBrace(source, open);

        int lineStart =
            source.LastIndexOf('\n', match.Index);

        if (lineStart < 0)
            lineStart = 0;
        else
            lineStart += 1;

        return new MethodLocation
        {
            Path = path,
            Start = lineStart,
            EndExclusive = close + 1,
            Text = source.Substring(
                lineStart,
                close + 1 - lineStart
            )
        };
    }

    private static string ReplaceLocation(
        string source,
        MethodLocation location,
        string replacement)
    {
        return
            source.Substring(0, location.Start) +
            replacement +
            source.Substring(location.EndExclusive);
    }

    private static int FindStatementSemicolon(
        string text,
        int start)
    {
        int paren = 0;
        bool inString = false;
        bool inChar = false;
        bool escape = false;

        for (int i = start;
             i < text.Length;
             i++)
        {
            char c = text[i];

            if (inString)
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
                    inString = false;

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

                if (c == '\'')
                    inChar = false;

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

            if (c == '(') paren++;
            else if (c == ')') paren--;
            else if (c == ';' && paren <= 0) return i;
        }

        throw new InvalidOperationException(
            "Statement semicolon was not found."
        );
    }

    private static int FindMatchingBrace(
        string text,
        int open)
    {
        if (open < 0 ||
            open >= text.Length ||
            text[open] != '{')
        {
            throw new ArgumentException("Invalid opening brace index.");
        }

        int depth = 0;
        bool inString = false;
        bool inVerbatim = false;
        bool inChar = false;
        bool lineComment = false;
        bool blockComment = false;
        bool escape = false;

        for (int i = open;
             i < text.Length;
             i++)
        {
            char c = text[i];
            char next =
                i + 1 < text.Length
                ? text[i + 1]
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
                }
                continue;
            }

            if (inString)
            {
                if (inVerbatim)
                {
                    if (c == '"')
                    {
                        if (next == '"')
                        {
                            i++;
                            continue;
                        }

                        inString = false;
                        inVerbatim = false;
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

                if (c == '"')
                    inString = false;

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

                if (c == '\'')
                    inChar = false;

                continue;
            }

            if (c == '/' && next == '/')
            {
                lineComment = true;
                i++;
                continue;
            }

            if (c == '/' && next == '*')
            {
                blockComment = true;
                i++;
                continue;
            }

            if (c == '@' && next == '"')
            {
                inString = true;
                inVerbatim = true;
                i++;
                continue;
            }

            if (c == '"')
            {
                inString = true;
                inVerbatim = false;
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
                    return i;
            }
        }

        throw new InvalidOperationException(
            "Matching closing brace was not found."
        );
    }

    private static void WriteChanged(
        string path,
        string source,
        List<string> touched)
    {
        string current =
            File.ReadAllText(path);

        if (current == source)
            return;

        Backup(path);
        File.WriteAllText(path, source);
        touched.Add(path);
    }

    private static void Backup(
        string path)
    {
        string name =
            path.Replace('/', '_')
                .Replace('\\', '_');

        string backup =
            Path.Combine(
                BackupRoot,
                name + ".txt"
            );

        if (!File.Exists(backup))
            File.Copy(path, backup, true);
    }

    private static void CleanupSelf()
    {
        if (EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += CleanupSelf;
            return;
        }

        try
        {
            if (File.Exists(SelfPath))
                AssetDatabase.DeleteAsset(SelfPath);

            string meta = SelfPath + ".meta";
            if (File.Exists(meta))
                AssetDatabase.DeleteAsset(meta);

            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[Warboard v42] Could not remove one-time migration automatically: " +
                ex.Message
            );
        }
    }
}
#endif
