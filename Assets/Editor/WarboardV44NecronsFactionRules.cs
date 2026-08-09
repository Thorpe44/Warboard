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
/// One-time v44 installer for Necrons Faction Pack 11e v1.1 (July 2026).
/// The v41 core architecture stays frozen; this only installs faction hooks.
/// </summary>
[InitializeOnLoad]
public static class WarboardV44NecronsFactionRules
{
    private const string SelfPath =
        "Assets/Editor/WarboardV44NecronsFactionRules.cs";

    private const string BackupRoot =
        "Library/WarboardBackups/V44";

    private const string ReportPath =
        "Library/WarboardV44NecronsFactionRulesReport.txt";

    private const string Marker =
        "WARBOARD_V44_FULL_NECRONS_FACTION_RULES";

    static WarboardV44NecronsFactionRules()
    {
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Warboard/Developer/Re-run v44 Full Necrons Faction Rules")]
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

            PatchGameController(touched);
            PatchInteractiveAttack(touched);
            PatchRulesEngine(touched);
            PatchSquadController(touched);
            PatchUniversalRuleEngine(touched);
            PatchCoreCompletion(touched);
            PatchMissionSystem(touched);
            PatchLegacyFactionRules(touched);

            ValidateResult();
            WriteMarker();
            WriteReport(touched);

            Debug.Log(
                "[Warboard v44] Full Necrons faction rules installed. " +
                "Unity will compile once more."
            );

            AssetDatabase.Refresh();
            EditorApplication.delayCall += CleanupSelf;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Warboard v44] Necrons faction-rule migration failed. " +
                ex
            );
        }
    }

    private static void ValidatePrerequisites()
    {
        string[] required =
        {
            "Assets/Scripts/Core/GameController.cs",
            "Assets/Scripts/Core/FactionControllerSystem.cs",
            "Assets/Scripts/Core/FactionRuleSystem.cs",
            "Assets/Scripts/Core/SquadController.cs",
            "Assets/Scripts/Core/RulesEngine.cs",
            "Assets/Scripts/Core/InteractiveAttackController.cs",
            "Assets/Scripts/Core/UniversalRuleEngine.cs",
            "Assets/Scripts/Core/MissionSystem.cs",
            "Assets/Scripts/Core/GameController.CoreCompletion11.cs",
            "Assets/Scripts/Factions/Necrons/NecronDetachmentRuntime.cs",
            "Assets/Scripts/Factions/Necrons/NecronDetachmentControllerSystem.cs",
            "Assets/Scripts/Factions/Necrons/NecronsFactionPack11.cs",
            "Assets/Scripts/Factions/Necrons/NecronsFactionPack11Runtime.cs",
            "Assets/Scripts/Factions/Necrons/NecronGameController.cs",
            "Assets/Scripts/Factions/Necrons/NecronsSetupUI.cs",
            "Assets/Scripts/Core/GameController.NecronsFaction11.cs"
        };

        foreach (string path in required)
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    "Required v44 file is missing: " + path
                );
            }
        }

        string build =
            File.ReadAllText(
                "Assets/Scripts/Core/WarboardBuildInfo.cs");

        if (!build.Contains("v44"))
        {
            throw new InvalidOperationException(
                "v44 expects the WARBOARD v44 build marker."
            );
        }
    }

    private static bool AlreadyApplied()
    {
        const string catalogPath =
            "Assets/Scripts/Factions/Necrons/NecronsFactionPack11.cs";

        if (!File.Exists(catalogPath) ||
            !File.ReadAllText(catalogPath).Contains(Marker))
        {
            return false;
        }

        string allGame =
            string.Join(
                "\n",
                ExistingGameFiles()
                    .Select(File.ReadAllText)
                    .ToArray());

        return
            allGame.Contains("DrawNecrons11StratagemCards") &&
            allGame.Contains("Necrons11ModifyStratagemCost") &&
            File.ReadAllText(
                "Assets/Scripts/Core/RulesEngine.cs")
                .Contains("NecronsFactionPack11.AdditionalAttacks") &&
            File.ReadAllText(
                "Assets/Scripts/Core/InteractiveAttackController.cs")
                .Contains("NecronsFactionPack11.AdditionalAttacks") &&
            File.ReadAllText(
                "Assets/Scripts/Core/SquadController.cs")
                .Contains("NecronsFactionPack11.ModifyObjectiveControl") &&
            File.ReadAllText(
                "Assets/Scripts/Core/UniversalRuleEngine.cs")
                .Contains("NecronsFactionPack11.GrantsCoreAbility");
    }

    private static void PatchGameController(
        List<string> touched)
    {
        PatchGameMethod(
            "DrawStratagemMenu",
            method =>
            {
                if (method.Contains("DrawNecrons11StratagemCards("))
                    return method;

                int legacy =
                    method.IndexOf(
                        "else if (isNecrons)",
                        StringComparison.Ordinal);

                if (legacy < 0)
                {
                    throw new InvalidOperationException(
                        "DrawStratagemMenu legacy Necron branch was not found."
                    );
                }

                string branch =
                    "else if (NecronsFactionPack11Runtime.Controller(activeFaction) != null)\n" +
                    "        {\n" +
                    "            DrawNecrons11StratagemCards(\n" +
                    "                left, right, y, cardWidth);\n" +
                    "        }\n        ";

                return method.Insert(legacy, branch);
            },
            touched
        );

        PatchGameMethod(
            "SpendFactionStratagemCP",
            method =>
            {
                if (method.Contains("Necrons11ModifyStratagemCost("))
                    return method;

                int existing =
                    method.IndexOf(
                        "Custodes11ModifyStratagemCost(",
                        StringComparison.Ordinal);

                if (existing < 0)
                {
                    existing =
                        method.IndexOf(
                            "Aeldari11ModifyStratagemCost(",
                            StringComparison.Ordinal);
                }

                if (existing >= 0)
                {
                    int semi = FindStatementSemicolon(method, existing);
                    return method.Insert(
                        semi + 1,
                        "\n\n        cost =\n" +
                        "            Necrons11ModifyStratagemCost(\n" +
                        "                unit, label, cost);"
                    );
                }

                Regex cost =
                    new Regex(
                        @"int\s+cost\s*=\s*Mathf\.Max\s*\(\s*0\s*,\s*baseCost\s*\)\s*;",
                        RegexOptions.Singleline
                    );

                Match match = cost.Match(method);
                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "SpendFactionStratagemCP cost anchor was not found."
                    );
                }

                return method.Insert(
                    match.Index + match.Length,
                    "\n\n        cost =\n" +
                    "            Necrons11ModifyStratagemCost(\n" +
                    "                unit, label, cost);"
                );
            },
            touched
        );

        PatchGameMethod(
            "ApplyFactionAttackRules",
            method =>
            {
                if (method.Contains("NecronsFactionPack11.ApplyAttackModifiers("))
                    return method;

                int close = method.LastIndexOf('}');
                if (close < 0)
                    return method;

                string block =
                    "\n        NecronsFactionPack11.ApplyAttackModifiers(\n" +
                    "            this, attacker, target, null, weapon, attackMode, state);\n";

                return method.Insert(close, block);
            },
            touched
        );

        PatchGameMethod(
            "TryShoot",
            method =>
            {
                if (!method.Contains("Necrons11CanAttackTarget("))
                {
                    method = InsertAtMethodStart(
                        method,
                        "        string necronsTargetReason;\n" +
                        "        if (attacker != null && target != null &&\n" +
                        "            !Necrons11CanAttackTarget(\n" +
                        "                attacker, target, AttackMode.Ranged,\n" +
                        "                out necronsTargetReason))\n" +
                        "        {\n" +
                        "            status = necronsTargetReason;\n" +
                        "            return;\n" +
                        "        }\n\n" +
                        "        if (attacker != null &&\n" +
                        "            !Necrons11EnsureCryptekAugmentation(attacker))\n" +
                        "        {\n" +
                        "            return;\n" +
                        "        }\n\n"
                    );
                }

                if (!method.Contains("Necrons11CanShootAfterFallBack"))
                {
                    method = new Regex(
                        @"attacker\.HasFallenBack\s*&&"
                    ).Replace(
                        method,
                        "attacker.HasFallenBack &&\n" +
                        "            !Necrons11CanShootAfterFallBack(attacker) &&",
                        1
                    );
                }

                return method;
            },
            touched
        );

        PatchGameMethodIfExists(
            "GetEligibleRangedWeapons",
            method => PatchAdvancedShootingAndRange(method),
            touched
        );

        PatchGameMethodIfExists(
            "GetEligibleModelRangedWeapons",
            method => PatchAdvancedShootingAndRange(method),
            touched
        );

        PatchGameMethod(
            "TryCharge",
            method =>
            {
                if (!method.Contains("Necrons11CanChargeAfterAdvance"))
                {
                    method = new Regex(
                        @"attacker\.HasAdvanced\s*&&"
                    ).Replace(
                        method,
                        "attacker.HasAdvanced &&\n" +
                        "            !Necrons11CanChargeAfterAdvance(attacker) &&",
                        1
                    );
                }

                if (!method.Contains("Necrons11CanChargeAfterFallBack"))
                {
                    method = new Regex(
                        @"attacker\.HasFallenBack\s*&&"
                    ).Replace(
                        method,
                        "attacker.HasFallenBack &&\n" +
                        "            !Necrons11CanChargeAfterFallBack(attacker) &&",
                        1
                    );
                }

                return method;
            },
            touched
        );

        PatchGameMethod(
            "ResolveChargeRoll",
            method =>
            {
                if (method.Contains("Necrons11OfferChargeReroll("))
                    return method;

                string anchor =
                    "        float targetDistance =";

                int at =
                    method.IndexOf(
                        anchor,
                        StringComparison.Ordinal);

                if (at < 0)
                {
                    throw new InvalidOperationException(
                        "ResolveChargeRoll target-distance anchor was not found."
                    );
                }

                string before =
                    "        if (Necrons11OfferChargeReroll(\n" +
                    "                attacker, target, roll, wasRerolled))\n" +
                    "        {\n" +
                    "            return;\n" +
                    "        }\n\n" +
                    "        roll +=\n" +
                    "            Necrons11ChargeRollModifier(\n" +
                    "                attacker, target);\n\n";

                return method.Insert(at, before);
            },
            touched
        );
    }

    private static string PatchAdvancedShootingAndRange(
        string method)
    {
        if (!method.Contains("Necrons11CanShootAfterAdvance"))
        {
            method = new Regex(
                @"(?<unit>(?:attacker|selectedSquad|unit|actionUnit))\.HasAdvanced\s*&&"
            ).Replace(
                method,
                match =>
                    match.Groups["unit"].Value +
                    ".HasAdvanced &&\n" +
                    "                    !Necrons11CanShootAfterAdvance(" +
                    match.Groups["unit"].Value +
                    ") &&"
            );
        }

        if (!method.Contains("NecronsFactionPack11.RangeModifier"))
        {
            string aeldari =
                "AeldariRangedRangeModifier(\n" +
                "                        attacker,\n" +
                "                        weapon\n" +
                "                    )";

            if (method.Contains(aeldari))
            {
                method = method.Replace(
                    aeldari,
                    aeldari +
                    " +\n                    NecronsFactionPack11.RangeModifier(\n" +
                    "                        attacker, weapon, AttackMode.Ranged\n" +
                    "                    )"
                );
            }
        }

        return method;
    }

    private static void PatchInteractiveAttack(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/InteractiveAttackController.cs";

        string source = File.ReadAllText(path);

        MethodLocation build =
            FindMethodInSource(path, source, "BuildVolleys");
        string method = build.Text;

        if (!method.Contains("NecronsFactionPack11.GrantsLethalHits"))
        {
            int anchor =
                method.IndexOf(
                    "CustodesFactionPack11.GrantsLethalHits(",
                    StringComparison.Ordinal);

            if (anchor >= 0)
            {
                int semi = FindStatementSemicolon(method, anchor);
                method = method.Insert(
                    semi + 1,
                    "\n\n            volley.lethalHits =\n" +
                    "                volley.lethalHits ||\n" +
                    "                NecronsFactionPack11.GrantsLethalHits(\n" +
                    "                    attacker, mode);"
                );
            }
        }

        if (!method.Contains("NecronsFactionPack11.MinimumSustainedHits"))
        {
            int anchor =
                method.IndexOf(
                    "volley.twinLinked =",
                    StringComparison.Ordinal);

            if (anchor < 0)
                throw new InvalidOperationException(
                    "Interactive sustained-hits anchor not found.");

            method = method.Insert(
                anchor,
                "            volley.sustainedHits =\n" +
                "                Mathf.Max(\n" +
                "                    volley.sustainedHits,\n" +
                "                    NecronsFactionPack11.MinimumSustainedHits(\n" +
                "                        attacker, weapon, mode));\n\n"
            );
        }

        if (!method.Contains("NecronsFactionPack11.GrantsDevastatingWounds"))
        {
            int precision =
                method.IndexOf(
                    "volley.precision =",
                    StringComparison.Ordinal);

            if (precision >= 0)
            {
                method = method.Insert(
                    precision,
                    "            volley.devastating =\n" +
                    "                volley.devastating ||\n" +
                    "                NecronsFactionPack11.GrantsDevastatingWounds(\n" +
                    "                    attacker, weapon, mode);\n\n"
                );
            }
        }

        if (!method.Contains("NecronsFactionPack11.GrantsPrecision"))
        {
            int woundTarget =
                method.IndexOf(
                    "volley.woundTarget =",
                    StringComparison.Ordinal);

            method = method.Insert(
                woundTarget,
                "            volley.precision =\n" +
                "                volley.precision ||\n" +
                "                NecronsFactionPack11.GrantsPrecision(\n" +
                "                    attacker, weapon, mode);\n\n"
            );
        }

        if (!method.Contains("NecronsFactionPack11.StrengthModifier"))
        {
            int ap =
                method.IndexOf(
                    "volley.effectiveAp =",
                    StringComparison.Ordinal);

            method = method.Insert(
                ap,
                "            volley.effectiveStrength +=\n" +
                "                NecronsFactionPack11.StrengthModifier(\n" +
                "                    attacker, first.model, weapon, mode);\n\n"
            );
        }

        if (!method.Contains("NecronsFactionPack11.ApModifier"))
        {
            int woundTarget =
                method.IndexOf(
                    "volley.woundTarget =",
                    StringComparison.Ordinal);

            method = method.Insert(
                woundTarget,
                "            volley.effectiveAp +=\n" +
                "                NecronsFactionPack11.ApModifier(\n" +
                "                    game, attacker, target, first.model, weapon, mode);\n\n"
            );
        }

        if (!method.Contains("NecronsFactionPack11.CriticalWoundThreshold"))
        {
            int attacks =
                method.IndexOf(
                    "            int attacks = 0;",
                    StringComparison.Ordinal);

            method = method.Insert(
                attacks,
                "            volley.criticalWoundThreshold =\n" +
                "                NecronsFactionPack11.CriticalWoundThreshold(\n" +
                "                    attacker, target, weapon, mode,\n" +
                "                    volley.criticalWoundThreshold);\n\n"
            );
        }

        if (!method.Contains("NecronsFactionPack11.AdditionalAttacks"))
        {
            int aeldari =
                method.IndexOf(
                    "oneModelAttacks +=\n                    AeldariFactionPack11.AdditionalAttacks(",
                    StringComparison.Ordinal);

            if (aeldari >= 0)
            {
                method = method.Insert(
                    aeldari,
                    "                oneModelAttacks +=\n" +
                    "                    NecronsFactionPack11.AdditionalAttacks(\n" +
                    "                        game, attacker, selection.model,\n" +
                    "                        weapon, mode, target);\n\n"
                );
            }
        }

        if (!method.Contains("NecronsFactionPack11.AdditionalRapidFire"))
        {
            int aeldari =
                method.IndexOf(
                    "rapid +=\n                    AeldariFactionPack11.AdditionalRapidFire(",
                    StringComparison.Ordinal);

            if (aeldari >= 0)
            {
                method = method.Insert(
                    aeldari,
                    "                rapid +=\n" +
                    "                    NecronsFactionPack11.AdditionalRapidFire(\n" +
                    "                        attacker, weapon, mode);\n\n"
                );
            }
        }

        if (!method.Contains("NecronsFactionPack11.RangeModifier"))
        {
            method = method.Replace(
                "weapon.range * 0.5f +",
                "(weapon.range +\n" +
                "                         NecronsFactionPack11.RangeModifier(\n" +
                "                             attacker, weapon, mode)) * 0.5f +"
            );
        }

        source = ReplaceLocation(source, build, method);

        MethodLocation hits =
            FindMethodInSource(path, source, "RollHits");
        method = hits.Text;

        if (!method.Contains("NecronsFactionPack11.AutomaticRerollHit"))
        {
            int recalc =
                method.IndexOf(
                    "        RecalculateHitResults();",
                    StringComparison.Ordinal);

            string block =
                "        if (!volley.cannotRerollHits)\n" +
                "        {\n" +
                "            bool necronsRerolled = false;\n" +
                "            for (int i = 0; i < volley.hitRolls.Count; i++)\n" +
                "            {\n" +
                "                int roll = volley.hitRolls[i];\n" +
                "                bool success = roll != 1 &&\n" +
                "                    (roll == 6 ||\n" +
                "                     roll + volley.hitRollModifier >= volley.skill);\n" +
                "                if (!NecronsFactionPack11.AutomaticRerollHit(\n" +
                "                        game, attacker, target, roll, success, mode))\n" +
                "                    continue;\n" +
                "                volley.hitRolls[i] = DiceRoller.RollD6(\n" +
                "                    \"Necrons Hit re-roll: \" + volley.weapon.displayName);\n" +
                "                necronsRerolled = true;\n" +
                "            }\n" +
                "            if (necronsRerolled)\n" +
                "                volley.automaticHitRerolls = true;\n" +
                "        }\n\n";

            if (recalc < 0)
                throw new InvalidOperationException(
                    "Interactive hit reroll anchor not found.");

            method = method.Insert(recalc, block);
        }

        if (!method.Contains("NecronsFactionPack11.RangeModifier"))
        {
            method = method.Replace(
                "volley.weapon.range * 0.5f +",
                "(volley.weapon.range +\n" +
                "                     NecronsFactionPack11.RangeModifier(\n" +
                "                         attacker, volley.weapon, mode)) * 0.5f +"
            );
        }

        source = ReplaceLocation(source, hits, method);

        MethodLocation recalcHits =
            FindMethodInSource(path, source, "RecalculateHitResults");
        method = recalcHits.Text;

        if (!method.Contains("NecronsFactionPack11.IsCriticalHit"))
        {
            string custodes =
                "if (CustodesFactionPack11.IsCriticalHit(\n" +
                "                    attacker, roll, success))";

            if (method.Contains(custodes))
            {
                method = method.Replace(
                    custodes,
                    "if (CustodesFactionPack11.IsCriticalHit(\n" +
                    "                    attacker, roll, success) ||\n" +
                    "                NecronsFactionPack11.IsCriticalHit(\n" +
                    "                    attacker, roll, success))"
                );
            }
            else
            {
                method = method.Replace(
                    "if (roll == 6)",
                    "if (NecronsFactionPack11.IsCriticalHit(\n" +
                    "                    attacker, roll, success))"
                );
            }
        }

        source = ReplaceLocation(source, recalcHits, method);

        MethodLocation wounds =
            FindMethodInSource(path, source, "RollWounds");
        method = wounds.Text;

        if (!method.Contains("NecronsFactionPack11.AutomaticRerollWound"))
        {
            int recalc =
                method.IndexOf(
                    "        RecalculateWoundResults();",
                    StringComparison.Ordinal);

            string block =
                "        bool necronsWoundRerolled = false;\n" +
                "        for (int i = 0; i < volley.woundRolls.Count; i++)\n" +
                "        {\n" +
                "            int roll = volley.woundRolls[i];\n" +
                "            bool critical = roll >= volley.criticalWoundThreshold;\n" +
                "            bool success = roll != 1 &&\n" +
                "                (critical || roll == 6 ||\n" +
                "                 roll + volley.woundRollModifier >= volley.woundTarget);\n" +
                "            if (!NecronsFactionPack11.AutomaticRerollWound(\n" +
                "                    game, attacker, target, roll, success, mode))\n" +
                "                continue;\n" +
                "            volley.woundRolls[i] = DiceRoller.RollD6(\n" +
                "                \"Necrons Wound re-roll: \" + volley.weapon.displayName);\n" +
                "            necronsWoundRerolled = true;\n" +
                "        }\n" +
                "        if (necronsWoundRerolled)\n" +
                "            volley.automaticWoundRerolls = true;\n\n";

            if (recalc < 0)
                throw new InvalidOperationException(
                    "Interactive wound reroll anchor not found.");

            method = method.Insert(recalc, block);
        }

        source = ReplaceLocation(source, wounds, method);

        MethodLocation damageRoll =
            FindMethodInSource(path, source, "RollDamage");
        method = damageRoll.Text;

        if (!method.Contains("NecronsFactionPack11.DamageModifier"))
        {
            string marker =
                "            volley.damageValues.Add(";

            int at =
                method.IndexOf(marker, StringComparison.Ordinal);

            if (at >= 0)
            {
                method = method.Insert(
                    at,
                    "            damage +=\n" +
                    "                NecronsFactionPack11.DamageModifier(\n" +
                    "                    attacker,\n" +
                    "                    volley.selections.Count > 0\n" +
                    "                        ? volley.selections[0].model\n" +
                    "                        : null,\n" +
                    "                    volley.weapon, mode);\n\n"
                );
            }
        }

        source = ReplaceLocation(source, damageRoll, method);

        MethodLocation apply =
            FindMethodInSource(path, source, "ApplyDamage");
        method = apply.Text;

        if (!method.Contains("NecronsFactionPack11.ModifyIncomingDamage"))
        {
            method = method.Replace(
                "attackDamage =\n                CustodesFactionPack11.ModifyIncomingDamage(\n                    allocated, attacker, volley.weapon, attackDamage);",
                "attackDamage =\n                CustodesFactionPack11.ModifyIncomingDamage(\n                    allocated, attacker, volley.weapon, attackDamage);\n\n            attackDamage =\n                NecronsFactionPack11.ModifyIncomingDamage(\n                    allocated.Squad, attackDamage);"
            );

            method = method.Replace(
                "attackDamage =\n                CustodesFactionPack11.ModifyIncomingDamage(\n                    allocated, attacker, volley.weapon, attackDamage, false);",
                "attackDamage =\n                CustodesFactionPack11.ModifyIncomingDamage(\n                    allocated, attacker, volley.weapon, attackDamage, false);\n\n            attackDamage =\n                NecronsFactionPack11.ModifyIncomingDamage(\n                    allocated.Squad, attackDamage);"
            );
        }

        source = ReplaceLocation(source, apply, method);

        MethodLocation hazardous =
            TryFindMethodInSource(path, source, "ResolveHazardous");

        if (hazardous != null &&
            !hazardous.Text.Contains("NecronsFactionPack11.GrantsHazardous"))
        {
            method = hazardous.Text;
            method = method.Replace(
                "if (!WeaponRuleParser.Has(\n            volley.weapon,\n            \"hazardous\"))",
                "if (!WeaponRuleParser.Has(\n            volley.weapon,\n            \"hazardous\") &&\n            !NecronsFactionPack11.GrantsHazardous(\n                attacker, volley.weapon, mode))"
            );
            source = ReplaceLocation(source, hazardous, method);
        }

        WriteChanged(path, source, touched);
    }

    private static void PatchRulesEngine(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/RulesEngine.cs";

        string source = File.ReadAllText(path);
        MethodLocation location =
            FindMethodInSource(path, source, "ResolveWeaponAttacks");
        string method = location.Text;

        if (!method.Contains("NecronsFactionPack11.AdditionalAttacks"))
        {
            int aeldari =
                method.IndexOf(
                    "attacks +=\n                AeldariFactionPack11.AdditionalAttacks(",
                    StringComparison.Ordinal);

            if (aeldari >= 0)
            {
                method = method.Insert(
                    aeldari,
                    "            attacks +=\n" +
                    "                NecronsFactionPack11.AdditionalAttacks(\n" +
                    "                    game, attacker, model, weapon, mode, target);\n\n"
                );
            }
        }

        if (!method.Contains("NecronsFactionPack11.AdditionalRapidFire"))
        {
            int aeldari =
                method.IndexOf(
                    "rapidFire +=\n                AeldariFactionPack11.AdditionalRapidFire(",
                    StringComparison.Ordinal);

            if (aeldari >= 0)
            {
                method = method.Insert(
                    aeldari,
                    "            rapidFire +=\n" +
                    "                NecronsFactionPack11.AdditionalRapidFire(\n" +
                    "                    attacker, weapon, mode);\n\n"
                );
            }
        }

        if (!method.Contains("NecronsFactionPack11.GrantsLethalHits"))
        {
            int sustained =
                method.IndexOf(
                    "int sustainedHits =",
                    StringComparison.Ordinal);

            method = method.Insert(
                sustained,
                "            lethalHits = lethalHits ||\n" +
                "                NecronsFactionPack11.GrantsLethalHits(\n" +
                "                    attacker, mode);\n\n"
            );
        }

        if (!method.Contains("NecronsFactionPack11.MinimumSustainedHits"))
        {
            int twin =
                method.IndexOf(
                    "bool twinLinked =",
                    StringComparison.Ordinal);

            method = method.Insert(
                twin,
                "            sustainedHits = Mathf.Max(\n" +
                "                sustainedHits,\n" +
                "                NecronsFactionPack11.MinimumSustainedHits(\n" +
                "                    attacker, weapon, mode));\n\n"
            );
        }

        if (!method.Contains("NecronsFactionPack11.GrantsDevastatingWounds"))
        {
            int precision =
                method.IndexOf(
                    "bool precision =",
                    StringComparison.Ordinal);

            method = method.Insert(
                precision,
                "            devastating = devastating ||\n" +
                "                NecronsFactionPack11.GrantsDevastatingWounds(\n" +
                "                    attacker, weapon, mode);\n\n"
            );
        }

        if (!method.Contains("NecronsFactionPack11.GrantsPrecision"))
        {
            int melta =
                method.IndexOf(
                    "int melta =",
                    StringComparison.Ordinal);

            method = method.Insert(
                melta,
                "            precision = precision ||\n" +
                "                NecronsFactionPack11.GrantsPrecision(\n" +
                "                    attacker, weapon, mode);\n\n"
            );
        }

        if (!method.Contains("NecronsFactionPack11.StrengthModifier"))
        {
            method = method.Replace(
                "                    weapon.strength,\n                    target.Toughness",
                "                    weapon.strength +\n" +
                "                    NecronsFactionPack11.StrengthModifier(\n" +
                "                        attacker, model, weapon, mode),\n" +
                "                    target.Toughness"
            );

            method = method.Replace(
                "                    weapon.strength +\n                    AeldariFactionPack11.StrengthModifier(\n                        attacker, weapon, mode),",
                "                    weapon.strength +\n" +
                "                    AeldariFactionPack11.StrengthModifier(\n" +
                "                        attacker, weapon, mode) +\n" +
                "                    NecronsFactionPack11.StrengthModifier(\n" +
                "                        attacker, model, weapon, mode),"
            );
        }

        if (!method.Contains("NecronsFactionPack11.ApModifier"))
        {
            string anchor =
                "AeldariFactionPack11.ApModifier(";
            int at = method.IndexOf(anchor, StringComparison.Ordinal);

            if (at >= 0)
            {
                int semi = FindStatementSemicolon(method, at);
                method = method.Insert(
                    semi + 1,
                    "\n\n                    effectiveAp +=\n" +
                    "                        NecronsFactionPack11.ApModifier(\n" +
                    "                            game, attacker, target, model, weapon, mode);"
                );
            }
            else
            {
                int saveLoop = method.IndexOf("int failedSaves =", StringComparison.Ordinal);
                if (saveLoop >= 0)
                {
                    method = method.Insert(
                        saveLoop,
                        "            int necronsApModifier =\n" +
                        "                NecronsFactionPack11.ApModifier(\n" +
                        "                    game, attacker, target, model, weapon, mode);\n\n"
                    );
                    method = method.Replace(
                        "weapon.ap,",
                        "weapon.ap + necronsApModifier,"
                    );
                }
            }
        }

        if (!method.Contains("NecronsFactionPack11.CriticalWoundThreshold"))
        {
            int loop =
                method.IndexOf(
                    "for (int i = 0;\n                 i < normalWoundRolls;",
                    StringComparison.Ordinal);

            method = method.Insert(
                loop,
                "            criticalThreshold =\n" +
                "                NecronsFactionPack11.CriticalWoundThreshold(\n" +
                "                    attacker, target, weapon, mode, criticalThreshold);\n\n"
            );
        }

        if (!method.Contains("NecronsFactionPack11.AutomaticRerollHit"))
        {
            int aeldari =
                method.IndexOf(
                    "if (!aeldari11UniversalState.cannotRerollHits &&\n                    AeldariFactionPack11.AutomaticRerollHit(",
                    StringComparison.Ordinal);

            if (aeldari >= 0)
            {
                method = method.Insert(
                    aeldari,
                    "                bool necronsHitSuccess =\n" +
                    "                    AeldariFactionPack11.AutomaticHitSucceeds(\n" +
                    "                        hitRoll, skill, aeldari11UniversalState);\n" +
                    "                if (!aeldari11UniversalState.cannotRerollHits &&\n" +
                    "                    NecronsFactionPack11.AutomaticRerollHit(\n" +
                    "                        game, attacker, target, hitRoll,\n" +
                    "                        necronsHitSuccess, mode))\n" +
                    "                {\n" +
                    "                    hitRoll = DiceRoller.RollD6(\n" +
                    "                        \"Necrons Hit re-roll: \" + weapon.displayName);\n" +
                    "                }\n\n"
                );
            }
        }

        if (!method.Contains("NecronsFactionPack11.IsCriticalHit"))
        {
            method = method.Replace(
                "if (hitRoll == 6)",
                "if (NecronsFactionPack11.IsCriticalHit(\n" +
                "                        attacker, hitRoll, true))"
            );
        }

        if (!method.Contains("NecronsFactionPack11.AutomaticRerollWound"))
        {
            int state =
                method.IndexOf(
                    "bool alreadyRerolled =",
                    StringComparison.Ordinal);

            if (state >= 0)
            {
                int semi = FindStatementSemicolon(method, state);
                method = method.Insert(
                    semi + 1,
                    "\n\n                if (NecronsFactionPack11.AutomaticRerollWound(\n" +
                    "                        game, attacker, target, woundRoll, success, mode))\n" +
                    "                {\n" +
                    "                    woundRoll = DiceRoller.RollD6(\n" +
                    "                        \"Necrons Wound re-roll: \" + weapon.displayName);\n" +
                    "                    success = AeldariFactionPack11.AutomaticWoundSucceeds(\n" +
                    "                        woundRoll, woundTarget, criticalThreshold,\n" +
                    "                        aeldari11UniversalState.woundRollModifier);\n" +
                    "                    alreadyRerolled = true;\n" +
                    "                }"
                );
            }
        }

        if (!method.Contains("NecronsFactionPack11.ModifyIncomingDamage"))
        {
            method = method.Replace(
                "rolledDamage =\n                        CustodesFactionPack11.ModifyIncomingDamage(\n                            allocated, attacker, weapon, rolledDamage);",
                "rolledDamage =\n                        CustodesFactionPack11.ModifyIncomingDamage(\n                            allocated, attacker, weapon, rolledDamage);\n\n                    rolledDamage =\n                        NecronsFactionPack11.ModifyIncomingDamage(\n                            allocated.Squad, rolledDamage);"
            );

            method = method.Replace(
                "mortalDamage =\n                    CustodesFactionPack11.ModifyIncomingDamage(\n                        allocated, attacker, weapon, mortalDamage, false);",
                "mortalDamage =\n                    CustodesFactionPack11.ModifyIncomingDamage(\n                        allocated, attacker, weapon, mortalDamage, false);\n\n                mortalDamage =\n                    NecronsFactionPack11.ModifyIncomingDamage(\n                        allocated.Squad, mortalDamage);"
            );
        }

        if (!method.Contains("NecronsFactionPack11.RangeModifier"))
        {
            method = method.Replace(
                "weapon.range * 0.5f +",
                "(weapon.range +\n" +
                "                     NecronsFactionPack11.RangeModifier(\n" +
                "                         attacker, weapon, mode)) * 0.5f +"
            );
        }

        source = ReplaceLocation(source, location, method);
        WriteChanged(path, source, touched);
    }

    private static void PatchSquadController(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/SquadController.cs";

        string source = File.ReadAllText(path);

        MethodLocation keyword =
            FindMethodInSource(path, source, "HasKeyword");
        string method = keyword.Text;

        if (!method.Contains("NecronsFactionPack11.GrantsKeyword"))
        {
            method = InsertAtMethodStart(
                method,
                "        if (NecronsFactionPack11.GrantsKeyword(\n" +
                "                this, keyword))\n" +
                "        {\n" +
                "            return true;\n" +
                "        }\n\n"
            );
        }

        source = ReplaceLocation(source, keyword, method);

        MethodLocation move =
            FindMethodInSource(path, source, "GetMovementAllowanceFor");
        method = move.Text;

        if (!method.Contains("NecronsFactionPack11.MoveModifier"))
        {
            method = method.Replace(
                "CustodesFactionPack11.MoveModifier(actionUnit) +",
                "CustodesFactionPack11.MoveModifier(actionUnit) +\n" +
                "            NecronsFactionPack11.MoveModifier(actionUnit) +"
            );
        }

        source = ReplaceLocation(source, move, method);

        MethodLocation advance =
            FindMethodInSource(path, source, "DeclareAdvance");
        method = advance.Text;

        if (!method.Contains("NecronsFactionPack11.FixedAdvanceResult"))
        {
            method = InsertAtMethodStart(
                method,
                "        int necronsFixedAdvance =\n" +
                "            NecronsFactionPack11.FixedAdvanceResult(this);\n" +
                "        if (necronsFixedAdvance > 0)\n" +
                "            roll = necronsFixedAdvance;\n\n"
            );
        }

        source = ReplaceLocation(source, advance, method);

        MethodLocation oc =
            FindMethodInSource(path, source, "EffectiveObjectiveControl");
        method = oc.Text;

        if (!method.Contains("NecronsFactionPack11.ModifyObjectiveControl"))
        {
            int ret =
                method.IndexOf(
                    "        return Mathf.Max(",
                    StringComparison.Ordinal);

            method = method.Insert(
                ret,
                "        objectiveControl =\n" +
                "            NecronsFactionPack11.ModifyObjectiveControl(\n" +
                "                JoinedActionController(), model, objectiveControl);\n\n"
            );
        }

        source = ReplaceLocation(source, oc, method);
        WriteChanged(path, source, touched);
    }

    private static void PatchUniversalRuleEngine(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/UniversalRuleEngine.cs";

        string source = File.ReadAllText(path);

        MethodLocation hasRule =
            FindMethodInSource(path, source, "UnitHasRule");
        string method = hasRule.Text;

        if (!method.Contains("NecronsFactionPack11.GrantsCoreAbility"))
        {
            int data =
                method.IndexOf(
                    "        UnitData data =",
                    StringComparison.Ordinal);

            method = method.Insert(
                data,
                "        if (NecronsFactionPack11.GrantsCoreAbility(\n" +
                "                squad, ruleName))\n" +
                "        {\n" +
                "            return true;\n" +
                "        }\n\n"
            );
        }

        source = ReplaceLocation(source, hasRule, method);

        MethodLocation fnp =
            FindMethodInSource(path, source, "ApplyFeelNoPain");
        method = fnp.Text;

        if (!method.Contains("NecronsFactionPack11.ConditionalFeelNoPain"))
        {
            int current =
                method.IndexOf(
                    "        int fnp =",
                    StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, current);

            method = method.Insert(
                semi + 1,
                "\n\n        fnp =\n" +
                "            NecronsFactionPack11.ConditionalFeelNoPain(\n" +
                "                squad, label, fnp);"
            );
        }

        source = ReplaceLocation(source, fnp, method);
        WriteChanged(path, source, touched);
    }

    private static void PatchCoreCompletion(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/GameController.CoreCompletion11.cs";

        string source = File.ReadAllText(path);

        if (source.Contains("NecronsFactionPack11.DetectionRangeBonus"))
            return;

        string custodes =
            "CustodesFactionPack11.DetectionRangeBonus(\n" +
            "                target.Squad != null\n" +
            "                    ? target.Squad.JoinedActionController()\n" +
            "                    : null)";

        if (source.Contains(custodes))
        {
            source = source.Replace(
                custodes,
                custodes +
                " +\n            NecronsFactionPack11.DetectionRangeBonus(\n" +
                "                target.Squad != null\n" +
                "                    ? target.Squad.JoinedActionController()\n" +
                "                    : null)"
            );
        }

        WriteChanged(path, source, touched);
    }

    private static void PatchMissionSystem(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/MissionSystem.cs";

        string source = File.ReadAllText(path);
        MethodLocation location =
            FindMethodInSource(path, source, "CanStartMissionAction");
        string method = location.Text;

        if (!method.Contains("NecronsFactionPack11.CanStartActionAfterAdvance"))
        {
            string custodes =
                "!CustodesFactionPack11.CanStartActionAfterAdvance(actionUnit)";

            if (method.Contains(custodes))
            {
                method = method.Replace(
                    custodes,
                    custodes +
                    " &&\n             !NecronsFactionPack11.CanStartActionAfterAdvance(actionUnit)"
                );
            }
            else
            {
                string gate =
                    "actionUnit.HasAdvanced";
                int at = method.IndexOf(gate, StringComparison.Ordinal);
                if (at >= 0)
                {
                    method = method.Replace(
                        gate,
                        "(actionUnit.HasAdvanced &&\n" +
                        "             !NecronsFactionPack11.CanStartActionAfterAdvance(actionUnit))"
                    );
                }
            }
        }

        source = ReplaceLocation(source, location, method);
        WriteChanged(path, source, touched);
    }

    private static void PatchLegacyFactionRules(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/FactionRuleSystem.cs";

        string source = File.ReadAllText(path);

        MethodLocation attack =
            FindMethodInSource(path, source, "ApplyAttackModifiers");
        string method = attack.Text;

        if (!method.Contains("NecronsFactionPack11Runtime.Controller(actionAttacker.FactionId) == null"))
        {
            method = method.Replace(
                "profile.IsNecrons &&\n            actionAttacker.AttachedLeader != null",
                "profile.IsNecrons &&\n" +
                "            NecronsFactionPack11Runtime.Controller(actionAttacker.FactionId) == null &&\n" +
                "            actionAttacker.AttachedLeader != null"
            );

            method = method.Replace(
                "profile.IsNecrons &&\n            game.FriendlyEnhancementAuraWithin(",
                "profile.IsNecrons &&\n" +
                "            NecronsFactionPack11Runtime.Controller(actionAttacker.FactionId) == null &&\n" +
                "            game.FriendlyEnhancementAuraWithin("
            );
        }

        source = ReplaceLocation(source, attack, method);

        MethodLocation reanimation =
            FindMethodInSource(path, source, "EndCommandPhase");
        method = reanimation.Text;

        if (!method.Contains("NecronsFactionPack11.ModifyReanimationRoll"))
        {
            int roll =
                method.IndexOf(
                    "int reanimation =",
                    StringComparison.Ordinal);

            if (roll >= 0)
            {
                int semi = FindStatementSemicolon(method, roll);
                method = method.Insert(
                    semi + 1,
                    "\n\n            reanimation =\n" +
                    "                NecronsFactionPack11.ModifyReanimationRoll(\n" +
                    "                    squad, reanimation);"
                );
            }
        }

        source = ReplaceLocation(source, reanimation, method);

        MethodLocation detect =
            FindMethodInSource(path, source, "DetectProfile");
        method = detect.Text;

        method = method.Replace(
            "\"Awakened Dynasty — Command Protocols\"",
            "\"Faction controller\""
        );

        source = ReplaceLocation(source, detect, method);
        WriteChanged(path, source, touched);
    }

    private static void ValidateResult()
    {
        string catalog =
            File.ReadAllText(
                "Assets/Scripts/Factions/Necrons/NecronsFactionPack11.cs");

        int stratagems =
            Regex.Matches(
                catalog,
                @"new\s+NecronStratagem11\b")
                .Count;

        int enhancements =
            Regex.Matches(
                catalog,
                @"new\s+NecronEnhancement11\b")
                .Count;

        int rules =
            Regex.Matches(
                catalog,
                @"new\s+NecronDetachmentRule11\b")
                .Count;

        if (stratagems != 63 ||
            enhancements != 42 ||
            rules != 12)
        {
            throw new InvalidOperationException(
                "v44 faction catalogue validation failed: " +
                stratagems + " Stratagems / " +
                enhancements + " Enhancements/Bindings / " +
                rules + " Detachment rules."
            );
        }

        string allGame =
            string.Join(
                "\n",
                ExistingGameFiles()
                    .Select(File.ReadAllText)
                    .ToArray());

        Require(allGame, "DrawNecrons11StratagemCards", "stratagem UI hook");
        Require(allGame, "Necrons11ModifyStratagemCost", "stratagem cost hook");
        Require(allGame, "Necrons11CanAttackTarget", "target restriction hook");
        Require(
            File.ReadAllText("Assets/Scripts/Core/RulesEngine.cs"),
            "NecronsFactionPack11.AdditionalAttacks",
            "automatic attack integration");
        Require(
            File.ReadAllText("Assets/Scripts/Core/InteractiveAttackController.cs"),
            "NecronsFactionPack11.AdditionalAttacks",
            "interactive attack integration");
        Require(
            File.ReadAllText("Assets/Scripts/Core/SquadController.cs"),
            "NecronsFactionPack11.ModifyObjectiveControl",
            "objective control integration");
        Require(
            File.ReadAllText("Assets/Scripts/Core/UniversalRuleEngine.cs"),
            "NecronsFactionPack11.GrantsCoreAbility",
            "core ability integration");
        Require(
            File.ReadAllText("Assets/Scripts/Core/FactionRuleSystem.cs"),
            "NecronsFactionPack11.ModifyReanimationRoll",
            "Reanimation Protocol integration");
    }

    private static void Require(
        string source,
        string marker,
        string label)
    {
        if (source == null ||
            !source.Contains(marker))
        {
            throw new InvalidOperationException(
                "v44 validation failed: missing " + label + "."
            );
        }
    }

    private static void WriteMarker()
    {
        const string path =
            "Assets/Scripts/Factions/Necrons/NecronsFactionPack11.cs";

        string source = File.ReadAllText(path);

        if (source.Contains(Marker))
            return;

        source =
            "// " + Marker + "\n" +
            source;

        File.WriteAllText(path, source);
    }

    private static void WriteReport(
        List<string> touched)
    {
        StringBuilder report =
            new StringBuilder();

        report.AppendLine("WARBOARD v44 — FULL NECRONS FACTION RULES");
        report.AppendLine();
        report.AppendLine("Installed against Necrons Faction Pack 11e v1.1, July 2026.");
        report.AppendLine("12 detachments / 63 stratagems / 42 enhancements and Necrodermal Bindings.");
        report.AppendLine("Standard matched-play faction pack only; Crusade and Boarding Actions are not included.");
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
        foreach (string candidate in ExistingGameFiles())
        {
            string source = File.ReadAllText(candidate);
            MethodLocation location =
                TryFindMethodInSource(
                    candidate,
                    source,
                    methodName);

            if (location == null)
                continue;

            string patched = patch(location.Text);
            if (patched != location.Text)
            {
                WriteChanged(
                    candidate,
                    ReplaceLocation(
                        source,
                        location,
                        patched),
                    touched);
            }
            return;
        }

        throw new InvalidOperationException(
            "GameController method not found: " + methodName
        );
    }

    private static void PatchGameMethodIfExists(
        string methodName,
        Func<string, string> patch,
        List<string> touched)
    {
        foreach (string candidate in ExistingGameFiles())
        {
            string source = File.ReadAllText(candidate);
            MethodLocation location =
                TryFindMethodInSource(
                    candidate,
                    source,
                    methodName);

            if (location == null)
                continue;

            string patched = patch(location.Text);
            if (patched != location.Text)
            {
                WriteChanged(
                    candidate,
                    ReplaceLocation(source, location, patched),
                    touched);
            }
            return;
        }
    }

    private static string InsertAtMethodStart(
        string method,
        string text)
    {
        int open = method.IndexOf('{');
        if (open < 0)
            throw new InvalidOperationException("Method open brace missing.");

        return method.Insert(open + 1, "\n" + text);
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
                methodName);

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
                @"(?ms)^\s*(?:public|private|protected|internal)\s+" +
                @"(?:static\s+)?[^;={}]+?\b" +
                Regex.Escape(methodName) +
                @"\s*\("
            );

        Match match = signature.Match(source);
        if (!match.Success)
            return null;

        int open = source.IndexOf('{', match.Index);
        if (open < 0)
            return null;

        int close = FindMatchingBrace(source, open);
        int lineStart = source.LastIndexOf('\n', match.Index);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        return new MethodLocation
        {
            Path = path,
            Start = lineStart,
            EndExclusive = close + 1,
            Text = source.Substring(
                lineStart,
                close + 1 - lineStart)
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
        if (start < 0)
        {
            throw new InvalidOperationException(
                "Statement start was not found."
            );
        }

        int paren = 0;
        bool inString = false;
        bool inChar = false;
        bool escape = false;

        for (int i = start; i < text.Length; i++)
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

        for (int i = open; i < text.Length; i++)
        {
            char c = text[i];
            char next =
                i + 1 < text.Length
                ? text[i + 1]
                : '\0';

            if (lineComment)
            {
                if (c == '\n') lineComment = false;
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
                if (depth == 0) return i;
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
        string current = File.ReadAllText(path);
        if (current == source)
            return;

        Backup(path);
        File.WriteAllText(path, source);
        touched.Add(path);
    }

    private static void Backup(string path)
    {
        string name =
            path.Replace('/', '_')
                .Replace('\\', '_');

        string backup =
            Path.Combine(
                BackupRoot,
                name + ".txt");

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
                "[Warboard v44] Could not remove one-time installer: " + ex.Message
            );
        }
    }
}
#endif
