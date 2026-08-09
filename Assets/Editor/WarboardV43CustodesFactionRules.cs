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
/// One-time v43 installer for the Edition 11 Adeptus Custodes faction pack.
/// It adds direct Custodes hooks to the frozen v42/v41 core without creating
/// a runtime bridge or reflection layer.
/// </summary>
[InitializeOnLoad]
public static class WarboardV43CustodesFactionRules
{
    private const string SelfPath =
        "Assets/Editor/WarboardV43CustodesFactionRules.cs";

    private const string CompileShimPath =
        "Assets/Scripts/Factions/AdeptusCustodes/CustodesModelTokenCompileShim.cs";

    private const string SetupCompileShimPath =
        "Assets/Scripts/Core/GameController.CustodesSetupCompileShim.cs";

    private const string BackupRoot =
        "Library/WarboardBackups/V43";

    private const string ReportPath =
        "Library/WarboardV43CustodesFactionRulesReport.txt";

    private const string Marker =
        "WARBOARD_V43_FULL_ADEPTUS_CUSTODES_FACTION_RULES";

    static WarboardV43CustodesFactionRules()
    {
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Warboard/Developer/Re-run v43 Full Adeptus Custodes Faction Rules")]
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

            PatchFactionControllerFactory(touched);
            PatchGameController(touched);
            PatchInteractiveAttack(touched);
            PatchRulesEngine(touched);
            PatchSquadController(touched);
            PatchModelToken(touched);
            PatchUniversalRuleEngine(touched);
            PatchCoreCompletion(touched);
            PatchMissionSystem(touched);

            ValidateResult();
            WriteMarker();
            WriteReport(touched);

            Debug.Log(
                "[Warboard v43] Full Adeptus Custodes faction rules installed. " +
                "Unity will compile once more."
            );

            AssetDatabase.Refresh();
            EditorApplication.delayCall += CleanupSelf;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Warboard v43] Adeptus Custodes faction-rule migration failed. " +
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
            "Assets/Scripts/Core/SquadController.cs",
            "Assets/Scripts/Core/ModelToken.cs",
            "Assets/Scripts/Core/RulesEngine.cs",
            "Assets/Scripts/Core/InteractiveAttackController.cs",
            "Assets/Scripts/Core/UniversalRuleEngine.cs",
            "Assets/Scripts/Core/MissionSystem.cs",
            "Assets/Scripts/Core/GameController.CoreCompletion11.cs",
            "Assets/Scripts/Factions/Aeldari/AeldariFactionPack11.cs",
            "Assets/Scripts/Core/GameController.AeldariFaction11.cs",
            "Assets/Scripts/Factions/AdeptusCustodes/CustodesDetachmentRuntime.cs",
            "Assets/Scripts/Factions/AdeptusCustodes/CustodesDetachmentControllerSystem.cs",
            "Assets/Scripts/Factions/AdeptusCustodes/CustodesFactionPack11.cs",
            "Assets/Scripts/Factions/AdeptusCustodes/CustodesFactionPack11Runtime.cs",
            "Assets/Scripts/Factions/AdeptusCustodes/CustodesGameController.cs",
            "Assets/Scripts/Factions/AdeptusCustodes/CustodesSetupUI.cs",
            "Assets/Scripts/Core/GameController.CustodesFaction11.cs"
        };

        foreach (string path in required)
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    "Required v43 file is missing: " + path
                );
            }
        }

        if (!File.ReadAllText(
                "Assets/Scripts/Factions/Aeldari/AeldariFactionPack11.cs")
            .Contains("WARBOARD_V42_FULL_AELDARI_FACTION_RULES"))
        {
            throw new InvalidOperationException(
                "v43 expects the working v42 Aeldari/core state."
            );
        }
    }

    private static bool AlreadyApplied()
    {
        const string catalogPath =
            "Assets/Scripts/Factions/AdeptusCustodes/CustodesFactionPack11.cs";

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
                    .ToArray()
            );

        return
            allGame.Contains("Custodes11PumpDeferredReactions") &&
            allGame.Contains("DrawCustodes11StratagemCards") &&
            allGame.Contains("Custodes11ModifyStratagemCost") &&
            File.ReadAllText(
                "Assets/Scripts/Core/FactionControllerSystem.cs")
                .Contains("new CustodesGameController") &&
            File.ReadAllText(
                "Assets/Scripts/Core/InteractiveAttackController.cs")
                .Contains("CustodesFactionPack11.AdditionalAttacks") &&
            File.ReadAllText(
                "Assets/Scripts/Core/UniversalRuleEngine.cs")
                .Contains("CustodesFactionPack11.ApplyAttackModifiers");
    }

    private static void PatchFactionControllerFactory(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/FactionControllerSystem.cs";

        string source = File.ReadAllText(path);

        if (source.Contains("new CustodesGameController"))
            return;

        MethodLocation create =
            FindMethodInSource(path, source, "Create");

        string method = create.Text;

        string necronAnchor =
            "        if (army != null &&\n" +
            "            army.Any(\n" +
            "                unit =>\n" +
            "                    unit != null &&\n" +
            "                    unit.HasIntrinsicKeyword(\n" +
            "                        \"necrons\")))";

        int insert =
            method.IndexOf(
                necronAnchor,
                StringComparison.Ordinal);

        if (insert < 0)
        {
            insert =
                method.IndexOf(
                    "        return new GenericFactionGameController();",
                    StringComparison.Ordinal);
        }

        if (insert < 0)
        {
            throw new InvalidOperationException(
                "Faction controller factory insertion point was not found."
            );
        }

        string custodes =
            "        if (army != null &&\n" +
            "            army.Any(\n" +
            "                unit =>\n" +
            "                    unit != null &&\n" +
            "                    unit.HasIntrinsicKeyword(\n" +
            "                        \"adeptus custodes\")))\n" +
            "        {\n" +
            "            return new CustodesGameController();\n" +
            "        }\n\n";

        method = method.Insert(insert, custodes);

        source = ReplaceLocation(source, create, method);
        WriteChanged(path, source, touched);
    }

    private static void PatchGameController(
        List<string> touched)
    {
        RepairBadReserveIngressIdentifier(touched);

        PatchGameMethod(
            "Update",
            method =>
            {
                if (method.Contains("Custodes11PumpDeferredReactions();"))
                    return method;

                return InsertAtMethodStart(
                    method,
                    "        Custodes11PumpDeferredReactions();\n\n"
                );
            },
            touched
        );

        PatchGameMethod(
            "DrawStratagemMenu",
            method =>
            {
                if (method.Contains("DrawCustodes11StratagemCards("))
                    return method;

                int necrons =
                    method.IndexOf(
                        "else if (isNecrons)",
                        StringComparison.Ordinal);

                if (necrons < 0)
                {
                    throw new InvalidOperationException(
                        "DrawStratagemMenu Necron branch was not found."
                    );
                }

                string branch =
                    "else if (CustodesFactionPack11Runtime.Controller(activeFaction) != null)\n" +
                    "        {\n" +
                    "            DrawCustodes11StratagemCards(\n" +
                    "                left, right, y, cardWidth);\n" +
                    "        }\n        ";

                return method.Insert(necrons, branch);
            },
            touched
        );

        PatchGameMethod(
            "SpendFactionStratagemCP",
            method =>
            {
                if (method.Contains("Custodes11ModifyStratagemCost("))
                    return method;

                int aeldari =
                    method.IndexOf(
                        "Aeldari11ModifyStratagemCost(",
                        StringComparison.Ordinal);

                if (aeldari >= 0)
                {
                    int semi = FindStatementSemicolon(method, aeldari);
                    return method.Insert(
                        semi + 1,
                        "\n\n        cost =\n" +
                        "            Custodes11ModifyStratagemCost(\n" +
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
                    "            Custodes11ModifyStratagemCost(\n" +
                    "                unit, label, cost);"
                );
            },
            touched
        );

        PatchGameMethod(
            "TryShoot",
            method =>
            {
                if (!method.Contains("Custodes11CanAttackTarget("))
                {
                    method = InsertAtMethodStart(
                        method,
                        "        string custodesTargetReason;\n" +
                        "        if (attacker != null && target != null &&\n" +
                        "            !Custodes11CanAttackTarget(\n" +
                        "                attacker, target, AttackMode.Ranged,\n" +
                        "                out custodesTargetReason))\n" +
                        "        {\n" +
                        "            status = custodesTargetReason;\n" +
                        "            return;\n" +
                        "        }\n\n"
                    );
                }

                if (!method.Contains("Custodes11CanShootAfterFallBack"))
                {
                    method = new Regex(
                        @"attacker\.HasFallenBack\s*&&"
                    ).Replace(
                        method,
                        "attacker.HasFallenBack &&\n" +
                        "            !Custodes11CanShootAfterFallBack(attacker) &&",
                        1
                    );
                }

                return method;
            },
            touched
        );

        PatchGameMethodIfExists(
            "GetEligibleRangedWeapons",
            method => PatchAdvancedShootingGate(method),
            touched
        );

        PatchGameMethodIfExists(
            "GetEligibleModelRangedWeapons",
            method => PatchAdvancedShootingGate(method),
            touched
        );

        PatchGameMethod(
            "TryCharge",
            method =>
            {
                if (!method.Contains("Custodes11CanChargeAfterAdvance"))
                {
                    method = new Regex(
                        @"attacker\.HasAdvanced\s*&&"
                    ).Replace(
                        method,
                        "attacker.HasAdvanced &&\n" +
                        "            !Custodes11CanChargeAfterAdvance(attacker) &&",
                        1
                    );
                }

                if (!method.Contains("Custodes11CanChargeAfterFallBack"))
                {
                    method = new Regex(
                        @"attacker\.HasFallenBack\s*&&"
                    ).Replace(
                        method,
                        "attacker.HasFallenBack &&\n" +
                        "            !Custodes11CanChargeAfterFallBack(attacker) &&",
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
                if (!method.Contains("Custodes11OfferHammerFallsChargeReroll"))
                {
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
                        "        if (Custodes11OfferHammerFallsChargeReroll(\n" +
                        "                attacker, target, roll, wasRerolled))\n" +
                        "        {\n" +
                        "            return;\n" +
                        "        }\n\n" +
                        "        roll +=\n" +
                        "            CustodesFactionPack11.ChargeRollModifier(\n" +
                        "                attacker);\n\n";

                    method = method.Insert(at, before);
                }

                if (!method.Contains("Custodes11AfterSuccessfulCharge(attacker)"))
                {
                    int mark =
                        method.IndexOf(
                            "attacker.MarkMadeChargeMove();",
                            StringComparison.Ordinal);

                    if (mark < 0)
                    {
                        throw new InvalidOperationException(
                            "ResolveChargeRoll charge-completion anchor was not found."
                        );
                    }

                    int semi = FindStatementSemicolon(method, mark);
                    method = method.Insert(
                        semi + 1,
                        "\n\n        Custodes11AfterSuccessfulCharge(attacker);"
                    );
                }

                return method;
            },
            touched
        );

        PatchGameMethodIfExists(
            "ReserveCanArriveThisRound",
            method =>
            {
                if (method.Contains("CanIngressFirstMovement"))
                    return method;

                return InsertAtMethodStart(
                    method,
                    "        if (reservePlacementSquad != null &&\n" +
                    "            CurrentRoundNumber == 1 &&\n" +
                    "            CustodesFactionPack11.CanIngressFirstMovement(reservePlacementSquad))\n" +
                    "        {\n" +
                    "            return true;\n" +
                    "        }\n\n"
                );
            },
            touched
        );

        PatchGameMethod(
            "TryFight",
            method =>
            {
                if (method.Contains("Custodes11EnsureKatahChoice("))
                    return method;

                return InsertAtMethodStart(
                    method,
                    "        string custodesTargetReason;\n" +
                    "        if (attacker != null && target != null &&\n" +
                    "            !Custodes11CanAttackTarget(\n" +
                    "                attacker, target, AttackMode.Melee,\n" +
                    "                out custodesTargetReason))\n" +
                    "        {\n" +
                    "            status = custodesTargetReason;\n" +
                    "            return;\n" +
                    "        }\n\n" +
                    "        if (attacker != null && target != null &&\n" +
                    "            Custodes11EnsureKatahChoice(attacker, target))\n" +
                    "        {\n" +
                    "            return;\n" +
                    "        }\n\n"
                );
            },
            touched
        );
    }

    private static void RepairBadReserveIngressIdentifier(
        List<string> touched)
    {
        foreach (string path in ExistingGameFiles())
        {
            if (!File.Exists(path))
                continue;

            string source = File.ReadAllText(path);

            string bad =
                "        if (squad != null &&\n" +
                "            CurrentRoundNumber == 1 &&\n" +
                "            CustodesFactionPack11.CanIngressFirstMovement(squad))\n";

            if (!source.Contains(bad))
                continue;

            string good =
                "        if (reservePlacementSquad != null &&\n" +
                "            CurrentRoundNumber == 1 &&\n" +
                "            CustodesFactionPack11.CanIngressFirstMovement(reservePlacementSquad))\n";

            source = source.Replace(
                bad,
                good
            );

            WriteChanged(
                path,
                source,
                touched
            );
        }
    }

    private static string PatchAdvancedShootingGate(
        string method)
    {
        if (method.Contains("Custodes11CanShootAfterAdvance"))
            return method;

        string patched =
            new Regex(
                @"(?<unit>(?:attacker|selectedSquad|unit))\.HasAdvanced\s*&&"
            ).Replace(
                method,
                match =>
                    match.Groups["unit"].Value +
                    ".HasAdvanced &&\n" +
                    "                    !Custodes11CanShootAfterAdvance(" +
                    match.Groups["unit"].Value +
                    ") &&"
            );

        return patched;
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

        if (!method.Contains("CustodesFactionPack11.GrantsLethalHits"))
        {
            int lethal =
                method.IndexOf(
                    "volley.lethalHits =",
                    StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, lethal);
            method = method.Insert(
                semi + 1,
                "\n\n            volley.lethalHits =\n" +
                "                volley.lethalHits ||\n" +
                "                CustodesFactionPack11.GrantsLethalHits(\n" +
                "                    attacker, mode);"
            );
        }

        if (!method.Contains("CustodesFactionPack11.MinimumSustainedHits"))
        {
            int sustainedAnchor =
                method.IndexOf(
                    "if (game != null)\n            {\n                volley.sustainedHits =",
                    StringComparison.Ordinal);

            if (sustainedAnchor < 0)
            {
                sustainedAnchor =
                    method.IndexOf(
                        "volley.twinLinked =",
                        StringComparison.Ordinal);
            }

            if (sustainedAnchor < 0)
            {
                throw new InvalidOperationException(
                    "Interactive sustained-hits insertion point was not found."
                );
            }

            method = method.Insert(
                sustainedAnchor,
                "            volley.sustainedHits =\n" +
                "                Mathf.Max(\n" +
                "                    volley.sustainedHits,\n" +
                "                    CustodesFactionPack11.MinimumSustainedHits(\n" +
                "                        attacker, weapon, mode));\n\n"
            );
        }

        if (!method.Contains("CustodesFactionPack11.GrantsPrecision"))
        {
            int precision =
                method.IndexOf(
                    "volley.precision =",
                    StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, precision);
            method = method.Insert(
                semi + 1,
                "\n\n            volley.precision =\n" +
                "                volley.precision ||\n" +
                "                CustodesFactionPack11.GrantsPrecision(\n" +
                "                    attacker, weapon, mode);"
            );
        }

        if (!method.Contains("CustodesFactionPack11.StrengthModifier"))
        {
            int strength =
                method.IndexOf(
                    "volley.effectiveStrength =",
                    StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, strength);
            method = method.Insert(
                semi + 1,
                "\n\n            volley.effectiveStrength +=\n" +
                "                CustodesFactionPack11.StrengthModifier(\n" +
                "                    attacker, first.model, weapon, mode);"
            );
        }

        if (!method.Contains("CustodesFactionPack11.ApModifier"))
        {
            int woundTarget =
                method.IndexOf(
                    "volley.woundTarget =",
                    StringComparison.Ordinal);

            if (woundTarget < 0)
            {
                throw new InvalidOperationException(
                    "Interactive wound-target anchor was not found."
                );
            }

            method = method.Insert(
                woundTarget,
                "            volley.effectiveAp +=\n" +
                "                CustodesFactionPack11.ApModifier(\n" +
                "                    attacker, target, first.model, weapon, mode);\n\n"
            );
        }

        if (!method.Contains("CustodesFactionPack11.ToughnessModifier"))
        {
            method = method.Replace(
                "                    target.Toughness\n                );",
                "                    target.Toughness +\n" +
                "                    CustodesFactionPack11.ToughnessModifier(target)\n" +
                "                );"
            );
        }

        if (!method.Contains("CustodesFactionPack11.AdditionalAttacks"))
        {
            int attacks =
                method.IndexOf(
                    "int oneModelAttacks =",
                    StringComparison.Ordinal);

            if (attacks < 0)
            {
                throw new InvalidOperationException(
                    "Interactive attack-count anchor was not found."
                );
            }

            int semi = FindStatementSemicolon(method, attacks);
            method = method.Insert(
                semi + 1,
                "\n\n                oneModelAttacks +=\n" +
                "                    CustodesFactionPack11.AdditionalAttacks(\n" +
                "                        game, attacker, selection.model,\n" +
                "                        weapon, mode, target);"
            );
        }

        if (!method.Contains("CustodesFactionPack11.AdditionalRapidFire"))
        {
            int rapid =
                method.IndexOf(
                    "int rapid =",
                    StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, rapid);
            method = method.Insert(
                semi + 1,
                "\n\n                rapid +=\n" +
                "                    CustodesFactionPack11.AdditionalRapidFire(\n" +
                "                        attacker, weapon, mode);"
            );
        }

        if (!method.Contains("CustodesFactionPack11.GrantsBlast"))
        {
            string blastGate =
                "if (WeaponRuleParser.Has(\n" +
                "                weapon,\n" +
                "                \"blast\"))";

            string blastReplacement =
                "if (WeaponRuleParser.Has(\n" +
                "                weapon,\n" +
                "                \"blast\") ||\n" +
                "                CustodesFactionPack11.GrantsBlast(\n" +
                "                    attacker, weapon, mode))";

            if (method.Contains(blastGate))
                method = method.Replace(blastGate, blastReplacement);
        }

        source = ReplaceLocation(source, build, method);

        MethodLocation hits =
            FindMethodInSource(path, source, "RollHits");
        method = hits.Text;

        if (!method.Contains("CustodesFactionPack11.AutomaticRerollHit"))
        {
            int recalc =
                method.IndexOf(
                    "        RecalculateHitResults();",
                    StringComparison.Ordinal);

            if (recalc < 0)
            {
                throw new InvalidOperationException(
                    "Interactive hit-reroll insertion point was not found."
                );
            }

            string block =
                "        if (!volley.cannotRerollHits)\n" +
                "        {\n" +
                "            bool custodesRerolled = false;\n" +
                "            for (int i = 0; i < volley.hitRolls.Count; i++)\n" +
                "            {\n" +
                "                int roll = volley.hitRolls[i];\n" +
                "                bool success = roll != 1 &&\n" +
                "                    (roll == 6 ||\n" +
                "                     roll + volley.hitRollModifier >= volley.skill);\n" +
                "                if (!CustodesFactionPack11.AutomaticRerollHit(\n" +
                "                        game, attacker, roll, success, mode))\n" +
                "                    continue;\n" +
                "                volley.hitRolls[i] = DiceRoller.RollD6(\n" +
                "                    \"Custodes Hit re-roll: \" + volley.weapon.displayName);\n" +
                "                custodesRerolled = true;\n" +
                "            }\n" +
                "            if (custodesRerolled)\n" +
                "                volley.automaticHitRerolls = true;\n" +
                "        }\n\n";

            method = method.Insert(recalc, block);
        }

        source = ReplaceLocation(source, hits, method);

        MethodLocation recalcHits =
            FindMethodInSource(path, source, "RecalculateHitResults");
        method = recalcHits.Text;

        if (!method.Contains("CustodesFactionPack11.IsCriticalHit"))
        {
            method = method.Replace(
                "            if (AeldariFactionPack11.IsCriticalHit(\n" +
                "                    attacker, target, volley.weapon,\n" +
                "                    roll, success))",
                "            if (AeldariFactionPack11.IsCriticalHit(\n" +
                "                    attacker, target, volley.weapon,\n" +
                "                    roll, success) ||\n" +
                "                CustodesFactionPack11.IsCriticalHit(\n" +
                "                    attacker, roll, success))"
            );

            if (!method.Contains("CustodesFactionPack11.IsCriticalHit"))
            {
                method = method.Replace(
                    "            if (roll == 6)",
                    "            if (CustodesFactionPack11.IsCriticalHit(\n" +
                    "                    attacker, roll, success))"
                );
            }
        }

        source = ReplaceLocation(source, recalcHits, method);

        MethodLocation wounds =
            FindMethodInSource(path, source, "RollWounds");
        method = wounds.Text;

        if (!method.Contains("CustodesFactionPack11.AutomaticRerollWound"))
        {
            int recalc =
                method.IndexOf(
                    "        RecalculateWoundResults();",
                    StringComparison.Ordinal);

            if (recalc < 0)
            {
                throw new InvalidOperationException(
                    "Interactive wound-reroll insertion point was not found."
                );
            }

            string block =
                "        bool custodesWoundRerolled = false;\n" +
                "        for (int i = 0; i < volley.woundRolls.Count; i++)\n" +
                "        {\n" +
                "            int roll = volley.woundRolls[i];\n" +
                "            bool critical = roll >= volley.criticalWoundThreshold;\n" +
                "            bool success = roll != 1 &&\n" +
                "                (critical || roll == 6 ||\n" +
                "                 roll + volley.woundRollModifier >= volley.woundTarget);\n" +
                "            if (!CustodesFactionPack11.AutomaticRerollWound(\n" +
                "                    attacker, target, roll, success, mode))\n" +
                "                continue;\n" +
                "            volley.woundRolls[i] = DiceRoller.RollD6(\n" +
                "                \"Custodes Wound re-roll: \" + volley.weapon.displayName);\n" +
                "            custodesWoundRerolled = true;\n" +
                "        }\n" +
                "        if (custodesWoundRerolled)\n" +
                "            volley.automaticWoundRerolls = true;\n\n";

            method = method.Insert(recalc, block);
        }

        source = ReplaceLocation(source, wounds, method);

        MethodLocation rollDamage =
            FindMethodInSource(path, source, "RollDamage");
        method = rollDamage.Text;

        if (!method.Contains("CustodesFactionPack11.DamageModifier"))
        {
            int add =
                method.IndexOf(
                    "(game != null\n                    ? game.AeldariDamageModifier(",
                    StringComparison.Ordinal);

            if (add >= 0)
            {
                int semi = FindStatementSemicolon(method, add);
                // `add` lies inside the damage expression, so insert before the
                // statement's terminating semicolon by changing the terminal
                // `: 0)` to also add Custodes damage.
                int terminal =
                    method.LastIndexOf(
                        ": 0)",
                        semi,
                        StringComparison.Ordinal);

                if (terminal >= 0)
                {
                    method = method.Insert(
                        terminal + 4,
                        " +\n                CustodesFactionPack11.DamageModifier(\n" +
                        "                    attacker, volley.selections.Count > 0\n" +
                        "                        ? volley.selections[0].model : null,\n" +
                        "                    volley.weapon, mode)"
                    );
                }
            }
        }

        source = ReplaceLocation(source, rollDamage, method);

        MethodLocation apply =
            FindMethodInSource(path, source, "ApplyDamage");
        method = apply.Text;

        if (!method.Contains("CustodesFactionPack11.ModifyIncomingDamage"))
        {
            Regex incomingPattern =
                new Regex(
                    @"int\s+incoming\s*=\s*Mathf\.Min\s*\(\s*allocated\.CurrentWounds\s*,\s*attackDamage\s*\)\s*;",
                    RegexOptions.Singleline
                );

            int occurrence = 0;

            method = incomingPattern.Replace(
                method,
                match =>
                {
                    occurrence++;

                    return
                        "attackDamage =\n" +
                        "                CustodesFactionPack11.ModifyIncomingDamage(\n" +
                        "                    allocated, attacker, volley.weapon, attackDamage" +
                        (occurrence == 1
                            ? ""
                            : ", false") +
                        ");\n\n" +
                        "            int incoming =\n" +
                        "                Mathf.Min(\n" +
                        "                    allocated.CurrentWounds,\n" +
                        "                    attackDamage);";
                },
                2
            );

            method = method.Replace(
                "                        volley.weapon.displayName\n                    );",
                "                        (WeaponRuleParser.Has(volley.weapon, \"psychic\")\n" +
                "                            ? \"Psychic Attack: \"\n" +
                "                            : \"\") +\n" +
                "                        volley.weapon.displayName\n                    );"
            );

            method = method.Replace(
                "                        \"Devastating Wounds: \" +\n                        volley.weapon.displayName\n                    );",
                "                        \"Devastating Wounds: \" +\n" +
                "                        (WeaponRuleParser.Has(volley.weapon, \"psychic\")\n" +
                "                            ? \"Psychic Attack: \"\n" +
                "                            : \"\") +\n" +
                "                        volley.weapon.displayName\n                    );"
            );
        }

        source = ReplaceLocation(source, apply, method);

        MethodLocation hazardous =
            TryFindMethodInSource(path, source, "ResolveHazardous");

        if (hazardous != null &&
            !hazardous.Text.Contains("ApplyFeelNoPain"))
        {
            string hazardMethod = hazardous.Text;

            hazardMethod = new Regex(
                @"allocated\.ApplyDamage\s*\(\s*mortal\s*\)",
                RegexOptions.Singleline
            ).Replace(
                hazardMethod,
                "allocated.ApplyDamage(\n" +
                "                    UniversalRuleRegistry.ApplyFeelNoPain(\n" +
                "                        allocated.Squad,\n" +
                "                        mortal,\n" +
                "                        \"Hazardous\"\n" +
                "                    )\n" +
                "                )",
                1
            );

            source = ReplaceLocation(
                source,
                hazardous,
                hazardMethod);
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

        if (!method.Contains("CustodesFactionPack11.AdditionalAttacks"))
        {
            int attacks = method.IndexOf("int attacks =", StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, attacks);
            method = method.Insert(
                semi + 1,
                "\n\n            attacks +=\n" +
                "                CustodesFactionPack11.AdditionalAttacks(\n" +
                "                    game, attacker, model, weapon, mode, target);"
            );
        }

        if (!method.Contains("CustodesFactionPack11.AdditionalRapidFire"))
        {
            int rapid = method.IndexOf("int rapidFire =", StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, rapid);
            method = method.Insert(
                semi + 1,
                "\n\n            rapidFire +=\n" +
                "                CustodesFactionPack11.AdditionalRapidFire(\n" +
                "                    attacker, weapon, mode);"
            );
        }

        if (!method.Contains("CustodesFactionPack11.GrantsBlast"))
        {
            string blastGate =
                "if (WeaponRuleParser.Has(\n" +
                "                weapon,\n" +
                "                \"blast\"))";

            string blastReplacement =
                "if (WeaponRuleParser.Has(\n" +
                "                weapon,\n" +
                "                \"blast\") ||\n" +
                "                CustodesFactionPack11.GrantsBlast(\n" +
                "                    attacker, weapon, mode))";

            if (method.Contains(blastGate))
                method = method.Replace(blastGate, blastReplacement);
        }

        if (!method.Contains("CustodesFactionPack11.GrantsLethalHits"))
        {
            int lethal = method.IndexOf("bool lethalHits =", StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, lethal);
            method = method.Insert(
                semi + 1,
                "\n\n            lethalHits = lethalHits ||\n" +
                "                CustodesFactionPack11.GrantsLethalHits(attacker, mode);"
            );
        }

        if (!method.Contains("CustodesFactionPack11.MinimumSustainedHits"))
        {
            int sustained = method.IndexOf("int sustainedHits =", StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, sustained);
            method = method.Insert(
                semi + 1,
                "\n\n            sustainedHits = Mathf.Max(\n" +
                "                sustainedHits,\n" +
                "                CustodesFactionPack11.MinimumSustainedHits(\n" +
                "                    attacker, weapon, mode));"
            );
        }

        if (!method.Contains("CustodesFactionPack11.GrantsPrecision"))
        {
            int precision = method.IndexOf("bool precision =", StringComparison.Ordinal);
            int semi = FindStatementSemicolon(method, precision);
            method = method.Insert(
                semi + 1,
                "\n\n            precision = precision ||\n" +
                "                CustodesFactionPack11.GrantsPrecision(\n" +
                "                    attacker, weapon, mode);"
            );
        }

        if (!method.Contains("CustodesFactionPack11.StrengthModifier"))
        {
            method = method.Replace(
                "                    weapon.strength +\n" +
                "                    AeldariFactionPack11.StrengthModifier(\n" +
                "                        attacker, weapon, mode),\n" +
                "                    target.Toughness",
                "                    weapon.strength +\n" +
                "                    AeldariFactionPack11.StrengthModifier(\n" +
                "                        attacker, weapon, mode) +\n" +
                "                    CustodesFactionPack11.StrengthModifier(\n" +
                "                        attacker, model, weapon, mode),\n" +
                "                    target.Toughness +\n" +
                "                    CustodesFactionPack11.ToughnessModifier(target)"
            );

            if (!method.Contains("CustodesFactionPack11.StrengthModifier"))
            {
                method = method.Replace(
                    "                    weapon.strength,\n                    target.Toughness",
                    "                    weapon.strength +\n" +
                    "                    CustodesFactionPack11.StrengthModifier(\n" +
                    "                        attacker, model, weapon, mode),\n" +
                    "                    target.Toughness +\n" +
                    "                    CustodesFactionPack11.ToughnessModifier(target)"
                );
            }
        }

        if (!method.Contains("CustodesFactionPack11.ApModifier"))
        {
            method = method.Replace(
                "                         AeldariFactionPack11.ApModifier(\n" +
                "                            attacker, target, weapon, mode)),",
                "                         AeldariFactionPack11.ApModifier(\n" +
                "                            attacker, target, weapon, mode) +\n" +
                "                         CustodesFactionPack11.ApModifier(\n" +
                "                            attacker, target, model, weapon, mode)),"
            );
        }

        if (!method.Contains("CustodesFactionPack11.DamageModifier"))
        {
            method = method.Replace(
                "                            AeldariFactionPack11.DamageModifier(\n" +
                "                                attacker, weapon, mode)",
                "                            AeldariFactionPack11.DamageModifier(\n" +
                "                                attacker, weapon, mode) +\n" +
                "                            CustodesFactionPack11.DamageModifier(\n" +
                "                                attacker, model, weapon, mode)"
            );

            method = method.Replace(
                "                        AeldariFactionPack11.DamageModifier(\n" +
                "                            attacker, weapon, mode)",
                "                        AeldariFactionPack11.DamageModifier(\n" +
                "                            attacker, weapon, mode) +\n" +
                "                        CustodesFactionPack11.DamageModifier(\n" +
                "                            attacker, model, weapon, mode)"
            );
        }

        if (!method.Contains("CustodesFactionPack11.AutomaticRerollHit"))
        {
            string anchor =
                "                if (!aeldari11UniversalState.cannotRerollHits &&\n" +
                "                    AeldariFactionPack11.AutomaticRerollHit(";

            int at = method.IndexOf(anchor, StringComparison.Ordinal);

            if (at >= 0)
            {
                method = method.Insert(
                    at,
                    "                bool custodesHitSuccess =\n" +
                    "                    AeldariFactionPack11.AutomaticHitSucceeds(\n" +
                    "                        hitRoll, skill, aeldari11UniversalState);\n" +
                    "                if (!aeldari11UniversalState.cannotRerollHits &&\n" +
                    "                    CustodesFactionPack11.AutomaticRerollHit(\n" +
                    "                        game, attacker, hitRoll, custodesHitSuccess, mode))\n" +
                    "                {\n" +
                    "                    hitRoll = DiceRoller.RollD6(\n" +
                    "                        \"Custodes Hit re-roll: \" + weapon.displayName);\n" +
                    "                }\n\n"
                );
            }
        }

        if (!method.Contains("CustodesFactionPack11.IsCriticalHit"))
        {
            method = method.Replace(
                "                if (AeldariFactionPack11.IsCriticalHit(\n" +
                "                        attacker, target, weapon, hitRoll, true))",
                "                if (AeldariFactionPack11.IsCriticalHit(\n" +
                "                        attacker, target, weapon, hitRoll, true) ||\n" +
                "                    CustodesFactionPack11.IsCriticalHit(\n" +
                "                        attacker, hitRoll, true))"
            );
        }

        if (!method.Contains("CustodesFactionPack11.AutomaticRerollWound"))
        {
            int rerollState =
                method.IndexOf(
                    "                bool alreadyRerolled =",
                    StringComparison.Ordinal);

            if (rerollState >= 0)
            {
                int end = method.IndexOf(';', rerollState);
                method = method.Insert(
                    end + 1,
                    "\n\n                if (CustodesFactionPack11.AutomaticRerollWound(\n" +
                    "                        attacker, target, woundRoll, success, mode))\n" +
                    "                {\n" +
                    "                    woundRoll = DiceRoller.RollD6(\n" +
                    "                        \"Custodes Wound re-roll: \" + weapon.displayName);\n" +
                    "                    success = AeldariFactionPack11.AutomaticWoundSucceeds(\n" +
                    "                        woundRoll, woundTarget, criticalThreshold,\n" +
                    "                        aeldari11UniversalState.woundRollModifier);\n" +
                    "                    alreadyRerolled = true;\n" +
                    "                }"
                );
            }
        }

        if (!method.Contains("CustodesFactionPack11.ModifyIncomingDamage"))
        {
            int rolledDamage =
                method.IndexOf(
                    "int rolledDamage =",
                    StringComparison.Ordinal);

            if (rolledDamage >= 0)
            {
                int lost =
                    method.IndexOf(
                        "int lost =",
                        rolledDamage,
                        StringComparison.Ordinal);

                if (lost > rolledDamage)
                {
                    method = method.Insert(
                        lost,
                        "rolledDamage =\n" +
                        "                        CustodesFactionPack11.ModifyIncomingDamage(\n" +
                        "                            allocated, attacker, weapon, rolledDamage);\n\n                    "
                    );
                }
            }

            int mortalDamage =
                method.IndexOf(
                    "int mortalDamage =",
                    StringComparison.Ordinal);

            if (mortalDamage >= 0)
            {
                int lost =
                    method.IndexOf(
                        "int lost =",
                        mortalDamage,
                        StringComparison.Ordinal);

                if (lost > mortalDamage)
                {
                    method = method.Insert(
                        lost,
                        "mortalDamage =\n" +
                        "                    CustodesFactionPack11.ModifyIncomingDamage(\n" +
                        "                        allocated, attacker, weapon, mortalDamage, false);\n\n                "
                    );
                }
            }

            method = method.Replace(
                "                                weapon.displayName\n" +
                "                            )",
                "                                (WeaponRuleParser.Has(weapon, \"psychic\")\n" +
                "                                    ? \"Psychic Attack: \"\n" +
                "                                    : \"\") +\n" +
                "                                weapon.displayName\n" +
                "                            )"
            );

            method = method.Replace(
                "                            \"Devastating Wounds: \" +\n" +
                "                            weapon.displayName\n" +
                "                        )",
                "                            \"Devastating Wounds: \" +\n" +
                "                            (WeaponRuleParser.Has(weapon, \"psychic\")\n" +
                "                                ? \"Psychic Attack: \"\n" +
                "                                : \"\") +\n" +
                "                            weapon.displayName\n" +
                "                        )"
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

        MethodLocation move =
            FindMethodInSource(path, source, "GetMovementAllowanceFor");
        string method = move.Text;

        if (!method.Contains("CustodesFactionPack11.MoveModifier"))
        {
            method = method.Replace(
                "            model.Squad.GetMove() +",
                "            model.Squad.GetMove() +\n" +
                "            CustodesFactionPack11.MoveModifier(actionUnit) +"
            );

            method = method.Replace(
                "? actionUnit.AdvanceBonus\n                : 0);",
                "? actionUnit.AdvanceBonus +\n" +
                "                  CustodesFactionPack11.AdvanceRollModifier(actionUnit)\n" +
                "                : 0);"
            );
        }

        source = ReplaceLocation(source, move, method);

        MethodLocation oc =
            FindMethodInSource(path, source, "EffectiveObjectiveControl");
        method = oc.Text;

        if (!method.Contains("CustodesFactionPack11.ModifyObjectiveControl"))
        {
            int returnIndex =
                method.LastIndexOf(
                    "        return Mathf.Max(",
                    StringComparison.Ordinal);

            if (returnIndex < 0)
            {
                throw new InvalidOperationException(
                    "Squad Objective Control return anchor was not found."
                );
            }

            method = method.Insert(
                returnIndex,
                "        objectiveControl =\n" +
                "            CustodesFactionPack11.ModifyObjectiveControl(\n" +
                "                JoinedActionController(), model, objectiveControl);\n\n"
            );
        }

        source = ReplaceLocation(source, oc, method);

        MethodLocation leadership =
            FindMethodInSource(path, source, "BestLeadership");
        method = leadership.Text;

        if (!method.Contains("CustodesFactionPack11.ModifyLeadership"))
        {
            method = method.Replace(
                "        return living.Min(\n            model => model.Leadership\n        );",
                "        int result = living.Min(\n" +
                "            model => model.Leadership\n" +
                "        );\n\n" +
                "        return CustodesFactionPack11.ModifyLeadership(\n" +
                "            actionUnit, result);"
            );
        }

        source = ReplaceLocation(source, leadership, method);
        WriteChanged(path, source, touched);
    }

    private static void PatchModelToken(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/ModelToken.cs";

        string source = File.ReadAllText(path);

        if (source.Contains("ApplyFactionMaxWoundsModifier("))
            return;

        int initialize =
            source.IndexOf(
                "    public void Initialize(\n",
                StringComparison.Ordinal);

        if (initialize < 0)
        {
            throw new InvalidOperationException(
                "ModelToken Initialize anchor was not found."
            );
        }

        string helper =
            "    public void ApplyFactionMaxWoundsModifier(int amount)\n" +
            "    {\n" +
            "        if (amount == 0)\n" +
            "            return;\n\n" +
            "        MaxWounds = Mathf.Max(1, MaxWounds + amount);\n" +
            "        CurrentWounds = Mathf.Clamp(\n" +
            "            CurrentWounds + amount,\n" +
            "            0,\n" +
            "            MaxWounds);\n\n" +
            "        RefreshWoundDisplay();\n" +
            "    }\n\n";

        source = source.Insert(
            initialize,
            helper);

        WriteChanged(path, source, touched);
    }

    private static void PatchUniversalRuleEngine(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/UniversalRuleEngine.cs";
        string source = File.ReadAllText(path);

        MethodLocation build =
            FindMethodInSource(path, source, "BuildAttackState");
        string method = build.Text;

        if (!method.Contains("CustodesFactionPack11.ApplyAttackModifiers"))
        {
            string anchor =
                "        if (attacker != null &&";

            int at =
                method.IndexOf(
                    anchor,
                    StringComparison.Ordinal);

            if (at < 0)
            {
                at = method.LastIndexOf(
                    "        state.hitRollModifier =",
                    StringComparison.Ordinal);
            }

            if (at < 0)
            {
                throw new InvalidOperationException(
                    "Universal BuildAttackState insertion point was not found."
                );
            }

            method = method.Insert(
                at,
                "        CustodesFactionPack11.ApplyAttackModifiers(\n" +
                "            game, attacker, target, shooter, weapon, mode, state);\n\n"
            );
        }

        source = ReplaceLocation(source, build, method);

        MethodLocation hasRule =
            FindMethodInSource(path, source, "UnitHasRule");
        method = hasRule.Text;

        if (!method.Contains("CustodesFactionPack11.GrantsCoreAbility"))
        {
            int data =
                method.IndexOf(
                    "        UnitData data =",
                    StringComparison.Ordinal);

            if (data < 0)
            {
                throw new InvalidOperationException(
                    "Universal UnitHasRule data anchor was not found."
                );
            }

            method = method.Insert(
                data,
                "        if (CustodesFactionPack11.GrantsCoreAbility(\n" +
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

        if (!method.Contains("CustodesFactionPack11.ConditionalFeelNoPain"))
        {
            int declaration =
                method.IndexOf(
                    "        int fnp =",
                    StringComparison.Ordinal);

            if (declaration < 0)
            {
                throw new InvalidOperationException(
                    "ApplyFeelNoPain FNP declaration was not found."
                );
            }

            int semi = FindStatementSemicolon(method, declaration);
            method = method.Insert(
                semi + 1,
                "\n\n        fnp =\n" +
                "            CustodesFactionPack11.ConditionalFeelNoPain(\n" +
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

        if (!source.Contains("CustodesFactionPack11.DetectionRangeBonus"))
        {
            source = new Regex(
                @"float\s+detectionRange\s*=\s*CoreRules11Terrain\.HiddenDetectionRange\s*;"
            ).Replace(
                source,
                "float detectionRange =\n" +
                "            CoreRules11Terrain.HiddenDetectionRange +\n" +
                "            CustodesFactionPack11.DetectionRangeBonus(\n" +
                "                target.Squad != null\n" +
                "                    ? target.Squad.JoinedActionController()\n" +
                "                    : null);",
                1
            );
        }

        MethodLocation mortal =
            TryFindMethodInSource(
                path,
                source,
                "Core11ApplyMortalWounds");

        if (mortal != null &&
            !mortal.Text.Contains("Mortal Wounds:"))
        {
            string method = mortal.Text;

            method = new Regex(
                @"UniversalRuleRegistry\.ApplyFeelNoPain\s*\(\s*model\.Squad\s*,\s*1\s*,\s*source\s*\)",
                RegexOptions.Singleline
            ).Replace(
                method,
                "UniversalRuleRegistry.ApplyFeelNoPain(\n" +
                "                    model.Squad,\n" +
                "                    1,\n" +
                "                    \"Mortal Wounds: \" + source\n" +
                "                )",
                1
            );

            source = ReplaceLocation(source, mortal, method);
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

        if (!method.Contains("CustodesFactionPack11.CanStartActionAfterAdvance"))
        {
            string anchor =
                "        if (actionUnit.HasAdvanced ||\n" +
                "            actionUnit.HasFallenBack)";

            string replacement =
                "        if ((actionUnit.HasAdvanced &&\n" +
                "             !CustodesFactionPack11.CanStartActionAfterAdvance(actionUnit)) ||\n" +
                "            actionUnit.HasFallenBack)";

            if (!method.Contains(anchor))
            {
                throw new InvalidOperationException(
                    "Mission action Advance/Fall Back gate was not found."
                );
            }

            method = method.Replace(
                anchor,
                replacement
            );
        }

        source = ReplaceLocation(source, location, method);
        WriteChanged(path, source, touched);
    }

    private static void ValidateResult()
    {
        string catalog =
            File.ReadAllText(
                "Assets/Scripts/Factions/AdeptusCustodes/CustodesFactionPack11.cs");

        int stratagems =
            Regex.Matches(
                catalog,
                @"new\s+CustodesStratagem11\b")
                .Count;

        int enhancements =
            Regex.Matches(
                catalog,
                @"new\s+CustodesEnhancement11\b")
                .Count;

        int rules =
            Regex.Matches(
                catalog,
                @"new\s+CustodesDetachmentRule11\b")
                .Count;

        if (stratagems != 45 ||
            enhancements != 30 ||
            rules != 9)
        {
            throw new InvalidOperationException(
                "v43 faction catalogue validation failed: " +
                stratagems + " Stratagems / " +
                enhancements + " Enhancements / " +
                rules + " Detachment rules."
            );
        }

        string allGame =
            string.Join(
                "\n",
                ExistingGameFiles()
                    .Select(File.ReadAllText)
                    .ToArray()
            );

        Require(
            allGame,
            "Custodes11PumpDeferredReactions",
            "deferred reaction hook");
        Require(
            allGame,
            "DrawCustodes11StratagemCards",
            "stratagem UI hook");
        Require(
            allGame,
            "Custodes11ModifyStratagemCost",
            "stratagem cost hook");
        Require(
            File.ReadAllText(
                "Assets/Scripts/Core/FactionControllerSystem.cs"),
            "new CustodesGameController",
            "faction controller factory");
        Require(
            File.ReadAllText(
                "Assets/Scripts/Core/InteractiveAttackController.cs"),
            "CustodesFactionPack11.AdditionalAttacks",
            "interactive attack integration");
        Require(
            File.ReadAllText(
                "Assets/Scripts/Core/UniversalRuleEngine.cs"),
            "CustodesFactionPack11.ApplyAttackModifiers",
            "universal attack integration");
        Require(
            File.ReadAllText(
                "Assets/Scripts/Core/SquadController.cs"),
            "CustodesFactionPack11.ModifyObjectiveControl",
            "objective-control integration");
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
                "v43 validation failed: missing " + label + "."
            );
        }
    }

    private static void WriteMarker()
    {
        const string path =
            "Assets/Scripts/Factions/AdeptusCustodes/CustodesFactionPack11.cs";

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

        report.AppendLine("WARBOARD v43 — FULL ADEPTUS CUSTODES FACTION RULES");
        report.AppendLine();
        report.AppendLine("Installed against Adeptus Custodes Faction Pack 11e v1.1, July 2026.");
        report.AppendLine("9 detachments / 45 stratagems / 30 enhancements.");
        report.AppendLine("Standard matched-play faction pack only; Crusade and Boarding Actions are not part of v43.");
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
        // Match method declarations even when the return type and method
        // name are split across lines. v42/v41 deliberately formats several
        // long signatures this way (for example UniversalRuleRegistry's
        // BuildAttackState), so the old single-line matcher rejected valid
        // methods before the migration could patch them.
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

            if (File.Exists(CompileShimPath))
                AssetDatabase.DeleteAsset(CompileShimPath);

            string shimMeta = CompileShimPath + ".meta";
            if (File.Exists(shimMeta))
                AssetDatabase.DeleteAsset(shimMeta);

            if (File.Exists(SetupCompileShimPath))
                AssetDatabase.DeleteAsset(SetupCompileShimPath);

            string setupShimMeta = SetupCompileShimPath + ".meta";
            if (File.Exists(setupShimMeta))
                AssetDatabase.DeleteAsset(setupShimMeta);

            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[Warboard v43] Could not remove one-time migration automatically: " +
                ex.Message
            );
        }
    }
}
#endif
