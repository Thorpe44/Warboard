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
/// v41 is the final single-pass 11e core completion migration. It runs against
/// the already split v39/v40 sources, writes direct runtime partials, patches
/// the exact owning methods, validates the source graph, then removes itself.
/// No runtime bridge or reflection shim is installed.
/// </summary>
[InitializeOnLoad]
public static class WarboardV41CoreRulesCompletion
{
    private const string SelfPath =
        "Assets/Editor/WarboardV41CoreRulesCompletion.cs";

    private const string PayloadRoot =
        "Assets/Editor/WarboardV41";

    private const string BackupRoot =
        "Library/WarboardBackups/V41";

    private const string ReportPath =
        "Library/WarboardV41CoreRulesCompletionReport.txt";

    private const string RuntimeCorePath =
        "Assets/Scripts/Core/CoreRules11Completion.cs";

    private const string RuntimeGamePath =
        "Assets/Scripts/Core/GameController.CoreCompletion11.cs";

    private const string Marker =
        "WARBOARD_V41_CORE_COMPLETION";

    private static readonly string[] GameFiles =
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
        "Assets/Scripts/Core/GameController.CoreRules11.cs",
        "Assets/Scripts/Core/GameController.Fight11.cs"
    };

    static WarboardV41CoreRulesCompletion()
    {
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Warboard/Developer/Re-run v41 Core Rules Completion")]
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
                return;
            }

            Directory.CreateDirectory(BackupRoot);

            List<string> touched =
                new List<string>();

            WriteGeneratedRuntime(touched);
            PatchSquadController(touched);
            PatchGameController(touched);
            PatchRulesEngine(touched);
            PatchInteractiveAttack(touched);
            PatchFight11(touched);

            ValidateResult();
            WriteReport(touched);

            Debug.Log(
                "[Warboard v41] Core Rules Completion installed. " +
                "Unity will compile once more; if that compile is clean, the core rules layer is frozen."
            );

            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Warboard v41] Core Rules Completion migration failed. " +
                ex
            );
        }
    }

    private static void ValidatePrerequisites()
    {
        string[] required =
        {
            "Assets/Scripts/Core/GameController.cs",
            "Assets/Scripts/Core/GameController.CoreRules11.cs",
            "Assets/Scripts/Core/GameController.Fight11.cs",
            "Assets/Scripts/Core/SquadController.cs",
            "Assets/Scripts/Core/RulesEngine.cs",
            "Assets/Scripts/Core/InteractiveAttackController.cs",
            "Assets/Scripts/Core/TerrainFeature.cs",
            "Assets/Scripts/Core/RosterTextManifestStore.cs",
            "Assets/Scripts/Factions/Aeldari/AeldariDetachmentRuntime.cs",
            PayloadRoot + "/Generated/CoreRules11Completion.cs.txt",
            PayloadRoot + "/Generated/GameController.CoreCompletion11.cs.txt"
        };

        foreach (string path in required)
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    "Required v41 prerequisite is missing: " + path
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
                "Safety stop: v41 requires the split GameController architecture."
            );
        }

        string fight11 =
            File.ReadAllText(
                "Assets/Scripts/Core/GameController.Fight11.cs"
            );

        if (!fight11.Contains(
                "WARBOARD_V40_FIGHT_PHASE_COMPLIANCE"))
        {
            throw new InvalidOperationException(
                "v41 requires the v40 Fight-phase compliance source."
            );
        }
    }

    private static bool AlreadyApplied()
    {
        if (!File.Exists(RuntimeGamePath) ||
            !File.Exists(RuntimeCorePath) ||
            !File.Exists("Assets/Scripts/Core/SquadController.cs") ||
            !File.Exists("Assets/Scripts/Core/RulesEngine.cs") ||
            !File.Exists("Assets/Scripts/Core/GameController.Fight11.cs"))
        {
            return false;
        }

        string runtime = File.ReadAllText(RuntimeGamePath);
        string squad = File.ReadAllText("Assets/Scripts/Core/SquadController.cs");
        string rules = File.ReadAllText("Assets/Scripts/Core/RulesEngine.cs");
        string fight = File.ReadAllText("Assets/Scripts/Core/GameController.Fight11.cs");
        string allGame = string.Join("\n", ExistingGameFiles().Select(File.ReadAllText).ToArray());

        return
            runtime.Contains(Marker) &&
            runtime.Contains("Core11BeginCombatDisembark") &&
            squad.Contains("public bool IsEmbarked") &&
            squad.Contains("public bool EmbarkWithin") &&
            allGame.Contains("Core11CanAdvancePhase") &&
            allGame.Contains("Core11HandleBoardPlacementClick") &&
            allGame.Contains("Core11CheckDestroyedTransportForEmergencyDisembark") &&
            rules.Contains("Core11PlungingFireApplies") &&
            fight.Contains("Core11ForcedFightSelection") &&
            fight.Contains("CoreRules11Aircraft.CanFightTarget(unit, enemy)");
    }

    private static void WriteGeneratedRuntime(
        List<string> touched)
    {
        WritePayload(
            PayloadRoot +
            "/Generated/CoreRules11Completion.cs.txt",
            RuntimeCorePath,
            touched
        );

        WritePayload(
            PayloadRoot +
            "/Generated/GameController.CoreCompletion11.cs.txt",
            RuntimeGamePath,
            touched
        );
    }

    private static void WritePayload(
        string sourcePath,
        string destinationPath,
        List<string> touched)
    {
        Backup(destinationPath);

        string source =
            File.ReadAllText(sourcePath);

        WriteSource(destinationPath, source);
        AddTouched(touched, destinationPath);
    }

    private static void PatchSquadController(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/SquadController.cs";

        string source = File.ReadAllText(path);
        string original = source;

        if (!source.Contains(
                "SquadBattlefieldState.Embarked"))
        {
            source = Regex.Replace(
                source,
                @"public enum SquadBattlefieldState\s*\{\s*Undeployed,\s*Battlefield,\s*Reserves\s*\}",
                "public enum SquadBattlefieldState\r\n" +
                "{\r\n" +
                "    Undeployed,\r\n" +
                "    Battlefield,\r\n" +
                "    Reserves,\r\n" +
                "    Embarked\r\n" +
                "}",
                RegexOptions.Singleline
            );
        }

        if (!source.Contains(
                "public bool IsEmbarked"))
        {
            const string anchor =
                "    public bool HasMoved { get; set; }";

            int index = source.IndexOf(
                anchor,
                StringComparison.Ordinal
            );

            if (index < 0)
            {
                throw new InvalidOperationException(
                    "SquadController v41 state anchor not found."
                );
            }

            string block =
@"    private readonly List<SquadController>
        embarkedPassengers =
            new List<SquadController>();

    public SquadController EmbarkedTransport
    {
        get;
        private set;
    }

    public IReadOnlyList<SquadController> EmbarkedPassengers
    {
        get { return embarkedPassengers; }
    }

    public bool IsEmbarked
    {
        get
        {
            return BattlefieldState ==
                SquadBattlefieldState.Embarked;
        }
    }

";

            source = source.Insert(index, block);
        }

        source = ReplaceMethodInSource(
            path,
            source,
            "GetMovementAllowanceFor",
@"    public float GetMovementAllowanceFor(
        ModelToken model)
    {
        if (model == null)
            return 0f;

        SquadController actionUnit =
            JoinedActionController();

        float allowance =
            model.Squad.GetMove() +
            actionUnit.BattleFocusMoveBonus +
            (actionUnit.HasAdvanced
                ? actionUnit.AdvanceBonus
                : 0);

        if (CoreRules11FlightRegistry
            .IsTakingToSkies(actionUnit))
        {
            allowance -= 2f;
        }

        return Mathf.Max(0f, allowance);
    }"
        );

        if (!source.Contains(
                "public bool EmbarkWithin("))
        {
            int last = source.LastIndexOf("\n}",
                StringComparison.Ordinal);

            if (last < 0)
                throw new InvalidOperationException(
                    "SquadController closing brace not found."
                );

            string methods =
@"
    public bool EmbarkWithin(
        SquadController transport)
    {
        SquadController actionUnit =
            JoinedActionController();

        if (transport == null ||
            actionUnit == transport ||
            !actionUnit.IsAlive)
        {
            return false;
        }

        transport = transport.JoinedActionController();

        actionUnit.ClearEmbarkmentLinks();

        actionUnit.EmbarkedTransport = transport;
        actionUnit.BattlefieldState =
            SquadBattlefieldState.Embarked;

        if (!transport.embarkedPassengers.Contains(
                actionUnit))
        {
            transport.embarkedPassengers.Add(
                actionUnit
            );
        }

        actionUnit.SetModelPresentation(false);
        actionUnit.SetSelected(false);

        if (actionUnit.AttachedLeader != null &&
            actionUnit.AttachedLeader.IsAlive)
        {
            SquadController leader =
                actionUnit.AttachedLeader;

            leader.EmbarkedTransport = transport;
            leader.BattlefieldState =
                SquadBattlefieldState.Embarked;
            leader.SetModelPresentation(false);
            leader.SetSelected(false);
        }

        return true;
    }

    public void DisembarkFromTransport(
        Vector3 rootPosition)
    {
        SquadController actionUnit =
            JoinedActionController();

        SquadController transport =
            actionUnit.EmbarkedTransport;

        if (transport != null)
        {
            transport.embarkedPassengers.Remove(
                actionUnit
            );
        }

        actionUnit.EmbarkedTransport = null;
        actionUnit.BattlefieldState =
            SquadBattlefieldState.Battlefield;
        actionUnit.transform.position = rootPosition;
        actionUnit.SetModelPresentation(true);

        if (actionUnit.AttachedLeader != null)
        {
            SquadController leader = actionUnit.AttachedLeader;
            leader.EmbarkedTransport = null;
            leader.BattlefieldState =
                SquadBattlefieldState.Battlefield;
            leader.transform.position =
                rootPosition + new Vector3(1.1f, 0f, 0f);
            leader.SetModelPresentation(true);
        }
    }

    public void ReembarkAfterFailedDisembark(
        SquadController transport)
    {
        if (transport == null)
            return;

        EmbarkWithin(transport);
    }

    private void ClearEmbarkmentLinks()
    {
        SquadController actionUnit =
            IsAttachedLeader &&
            AttachedBodyguard != null
            ? AttachedBodyguard
            : this;

        if (actionUnit.EmbarkedTransport != null)
        {
            actionUnit.EmbarkedTransport
                .embarkedPassengers
                .Remove(actionUnit);
        }

        actionUnit.EmbarkedTransport = null;

        if (actionUnit.AttachedLeader != null)
        {
            actionUnit.AttachedLeader
                .EmbarkedTransport = null;
        }
    }
";

            source = source.Insert(last, methods);
        }

        source = InsertAtMethodStartIfPresent(
            path,
            source,
            "StageForDeployment",
            "        ClearEmbarkmentLinks();\r\n"
        );

        source = InsertAtMethodStartIfPresent(
            path,
            source,
            "SendToReserves",
            "        ClearEmbarkmentLinks();\r\n"
        );

        source = InsertAtMethodStartIfPresent(
            path,
            source,
            "DeployToBattlefield",
            "        ClearEmbarkmentLinks();\r\n"
        );

        if (Normalize(source) != Normalize(original))
        {
            Backup(path);
            WriteSource(path, source);
            AddTouched(touched, path);
        }
    }

    private static void PatchGameController(
        List<string> touched)
    {
        PatchGameMethodStart(
            "Start",
            "        Core11Install();\r\n",
            touched
        );

        PatchGameMethodStart(
            "OnDestroy",
            "        Core11Uninstall();\r\n",
            touched
        );

        PatchGameMethodStart(
            "BeginBattle",
            "        if (!Core11PrepareAircraftAndValidateMuster())\r\n" +
            "            return;\r\n\r\n",
            touched
        );

        PatchGameMethodStart(
            "NextPhase",
            "        string core11PhaseReason;\r\n" +
            "        if (!Core11CanAdvancePhase(out core11PhaseReason))\r\n" +
            "        {\r\n" +
            "            status = core11PhaseReason;\r\n" +
            "            return;\r\n" +
            "        }\r\n\r\n",
            touched
        );

        PatchGameMethodBody(
            "ModelHasLineOfSightToModel",
            "        return Core11CanSeeModel(shooter, targetModel);\r\n",
            touched
        );

        PatchGameMethodBodyIfPresent(
            "TargetUnitHasCoverFromShooter",
            "        return Core11TargetUnitHasCoverFromShooter(shooter, target);\r\n",
            touched
        );

        PatchGameMethodBodyIfPresent(
            "TargetModelHasCoverFromShooter",
            "        return Core11TargetModelHasCoverFromShooter(shooter, targetModel);\r\n",
            touched
        );

        PatchGameMethodStart(
            "TryCharge",
            "        if (!Core11AircraftChargeAllowed(attacker, target))\r\n" +
            "            return;\r\n\r\n" +
            "        if (attacker != null && core11CannotChargeThisTurn.Contains(attacker.JoinedActionController()))\r\n" +
            "        {\r\n" +
            "            status = attacker.DisplayName + \" cannot declare a charge this turn.\";\r\n" +
            "            return;\r\n" +
            "        }\r\n\r\n",
            touched
        );

        PatchGameMethodStart(
            "TryDeclareAdvance",
            "        if (selectedSquad != null && selectedSquad.HasKeyword(\"AIRCRAFT\"))\r\n" +
            "        {\r\n" +
            "            status = \"AIRCRAFT units are only eligible to make an Ingress move.\";\r\n" +
            "            return;\r\n" +
            "        }\r\n\r\n",
            touched
        );

        PatchAdvanceAircraftEngagement(touched);
        PatchCombatMovementRules(touched);
        PatchSurgeRules(touched);
        PatchChargeFlightDistance(touched);
        PatchDestroyedTransportFallback(touched);
        PatchMovementMethods(touched);
        PatchContextBar(touched);
        PatchBoardPlacementClick(touched);
        PatchTraditionalInsaneBravery(touched);
    }

    private static void PatchAdvanceAircraftEngagement(
        List<string> touched)
    {
        MethodLocation location =
            FindGameMethod("TryDeclareAdvance");

        string source = File.ReadAllText(location.Path);
        string method = Extract(location, source);
        string original = method;

        method = Regex.Replace(
            method,
            @"IsEngaged\s*\(\s*selectedSquad\s*\)",
            "Core11IsEngagedForNormalMovement(selectedSquad)"
        );

        if (Normalize(method) != Normalize(original))
        {
            Backup(location.Path);
            source = ReplaceExtract(location, source, method);
            WriteSource(location.Path, source);
            AddTouched(touched, location.Path);
        }
    }

    private static void PatchCombatMovementRules(
        List<string> touched)
    {
        MethodLocation combat =
            FindGameMethod("CombatMovePathIsClear");

        string source = File.ReadAllText(combat.Path);
        string method = Extract(combat, source);
        string original = method;

        if (!method.Contains(
                "CoreRules11FlightRegistry.IsTakingToSkies"))
        {
            int brace = method.IndexOf('{');

            method = method.Insert(
                brace + 1,
                "\r\n        if (movingModel != null &&\r\n" +
                "            movingModel.Squad != null &&\r\n" +
                "            CoreRules11FlightRegistry.IsTakingToSkies(\r\n" +
                "                movingModel.Squad.JoinedActionController()))\r\n" +
                "        {\r\n" +
                "            return true;\r\n" +
                "        }\r\n"
            );
        }

        method = method.Replace(
            "terrain != null &&\r\n                terrain.BlocksMovement",
            "terrain != null &&\r\n" +
            "                !CoreRules11Terrain.MovementDestinationAllowsTerrain(\r\n" +
            "                    movingModel != null && movingModel.Squad != null\r\n" +
            "                    ? movingModel.Squad.JoinedActionController()\r\n" +
            "                    : null,\r\n" +
            "                    terrain\r\n" +
            "                )"
        );

        method = method.Replace(
            "terrain != null &&\n                terrain.BlocksMovement",
            "terrain != null &&\n" +
            "                !CoreRules11Terrain.MovementDestinationAllowsTerrain(\n" +
            "                    movingModel != null && movingModel.Squad != null\n" +
            "                    ? movingModel.Squad.JoinedActionController()\n" +
            "                    : null,\n" +
            "                    terrain\n" +
            "                )"
        );

        if (method.Contains("terrain.BlocksMovement"))
        {
            method = Regex.Replace(
                method,
                @"terrain\s*!=\s*null\s*&&\s*terrain\.BlocksMovement",
                "terrain != null &&\r\n" +
                "                !CoreRules11Terrain.MovementDestinationAllowsTerrain(\r\n" +
                "                    movingModel != null && movingModel.Squad != null\r\n" +
                "                    ? movingModel.Squad.JoinedActionController()\r\n" +
                "                    : null,\r\n" +
                "                    terrain\r\n" +
                "                )"
            );
        }

        if (Normalize(method) != Normalize(original))
        {
            Backup(combat.Path);
            source = ReplaceExtract(combat, source, method);
            WriteSource(combat.Path, source);
            AddTouched(touched, combat.Path);
        }

        MethodLocation place =
            FindGameMethod("CanPlaceModel");

        source = File.ReadAllText(place.Path);
        method = Extract(place, source);
        original = method;

        method = method.Replace(
            "terrain != null &&\r\n                terrain.BlocksMovement",
            "terrain != null &&\r\n" +
            "                !CoreRules11Terrain.MovementDestinationAllowsTerrain(\r\n" +
            "                    movingModel != null && movingModel.Squad != null\r\n" +
            "                    ? movingModel.Squad.JoinedActionController()\r\n" +
            "                    : null,\r\n" +
            "                    terrain\r\n" +
            "                )"
        );

        method = method.Replace(
            "terrain != null &&\n                terrain.BlocksMovement",
            "terrain != null &&\n" +
            "                !CoreRules11Terrain.MovementDestinationAllowsTerrain(\n" +
            "                    movingModel != null && movingModel.Squad != null\n" +
            "                    ? movingModel.Squad.JoinedActionController()\n" +
            "                    : null,\n" +
            "                    terrain\n" +
            "                )"
        );

        if (method.Contains("terrain.BlocksMovement"))
        {
            method = Regex.Replace(
                method,
                @"terrain\s*!=\s*null\s*&&\s*terrain\.BlocksMovement",
                "terrain != null &&\r\n" +
                "                !CoreRules11Terrain.MovementDestinationAllowsTerrain(\r\n" +
                "                    movingModel != null && movingModel.Squad != null\r\n" +
                "                    ? movingModel.Squad.JoinedActionController()\r\n" +
                "                    : null,\r\n" +
                "                    terrain\r\n" +
                "                )"
            );
        }

        if (Normalize(method) != Normalize(original))
        {
            Backup(place.Path);
            source = ReplaceExtract(place, source, method);
            WriteSource(place.Path, source);
            AddTouched(touched, place.Path);
        }
    }

    private static void PatchSurgeRules(
        List<string> touched)
    {
        MethodLocation location =
            FindGameMethod("BeginSurgeMove");

        string source = File.ReadAllText(location.Path);
        string method = Extract(location, source);
        string original = method;

        method = Regex.Replace(
            method,
            @"FindNearestEnemy\s*\(\s*squad\s*\)",
            "Core11FindNearestSurgeEnemy(squad)"
        );

        if (Normalize(method) != Normalize(original))
        {
            Backup(location.Path);
            source = ReplaceExtract(location, source, method);
            WriteSource(location.Path, source);
            AddTouched(touched, location.Path);
        }
    }

    private static void PatchChargeFlightDistance(
        List<string> touched)
    {
        MethodLocation location =
            FindGameMethod("ResolveChargeRoll");

        string source = File.ReadAllText(location.Path);
        string method = Extract(location, source);

        if (method.Contains(
                "v41 / 21.03 Take to the Skies"))
        {
            return;
        }

        int brace = method.IndexOf('{');
        if (brace < 0)
            throw new InvalidOperationException(
                "ResolveChargeRoll opening brace missing."
            );

        string insert =
            "\r\n        // v41 / 21.03 Take to the Skies reduces the maximum\r\n" +
            "        // distance of this Charge move by 2 inches.\r\n" +
            "        if (attacker != null &&\r\n" +
            "            CoreRules11FlightRegistry.IsTakingToSkies(attacker))\r\n" +
            "        {\r\n" +
            "            roll = Mathf.Max(0, roll - 2);\r\n" +
            "        }\r\n";

        method = method.Insert(brace + 1, insert);

        Backup(location.Path);
        source = ReplaceExtract(location, source, method);
        WriteSource(location.Path, source);
        AddTouched(touched, location.Path);
    }

    private static void PatchDestroyedTransportFallback(
        List<string> touched)
    {
        PatchGameMethodStart(
            "RecordModelDestroyed",
            "        Core11CheckDestroyedTransportForEmergencyDisembark(model);\r\n",
            touched
        );
    }

    private static void PatchMovementMethods(
        List<string> touched)
    {
        MethodLocation single =
            FindGameMethod("TryMoveSelectedModel");

        string source = File.ReadAllText(single.Path);
        string method = Extract(single, source);
        string original = method;

        if (!method.Contains(
                "AIRCRAFT units are only eligible to make an Ingress move."))
        {
            int brace = method.IndexOf('{');
            method = method.Insert(
                brace + 1,
                "\r\n        if (selectedSquad != null && selectedSquad.HasKeyword(\"AIRCRAFT\"))\r\n" +
                "        {\r\n" +
                "            status = \"AIRCRAFT units are only eligible to make an Ingress move.\";\r\n" +
                "            return;\r\n" +
                "        }\r\n"
            );
        }

        method = method.Replace(
            "bool wasEngagedBeforeMove =\r\n            IsEngaged(selectedSquad);",
            "bool wasEngagedBeforeMove =\r\n            Core11IsEngagedForNormalMovement(selectedSquad);"
        );

        method = method.Replace(
            "bool wasEngagedBeforeMove =\n            IsEngaged(selectedSquad);",
            "bool wasEngagedBeforeMove =\n            Core11IsEngagedForNormalMovement(selectedSquad);"
        );

        if (!method.Contains(
                "Core11NormalMovePathIsClear"))
        {
            int nullGuard = method.IndexOf(
                "        bool wasEngagedBeforeMove",
                StringComparison.Ordinal
            );

            if (nullGuard < 0)
                throw new InvalidOperationException(
                    "TryMoveSelectedModel engagement anchor missing."
                );

            string insert =
                "        if (!Core11NormalMovePathIsClear(selectedModel, destination))\r\n" +
                "        {\r\n" +
                "            status = \"That movement path is blocked by terrain or an enemy model.\";\r\n" +
                "            return;\r\n" +
                "        }\r\n\r\n";

            method = method.Insert(nullGuard, insert);
        }

        if (Normalize(method) != Normalize(original))
        {
            Backup(single.Path);
            source = ReplaceExtract(single, source, method);
            WriteSource(single.Path, source);
            AddTouched(touched, single.Path);
        }

        MethodLocation whole =
            FindGameMethod("TryMoveWholeSquad");

        source = File.ReadAllText(whole.Path);
        method = Extract(whole, source);
        original = method;

        if (!method.Contains(
                "AIRCRAFT units are only eligible to make an Ingress move."))
        {
            int brace = method.IndexOf('{');
            method = method.Insert(
                brace + 1,
                "\r\n        if (selectedSquad != null && selectedSquad.HasKeyword(\"AIRCRAFT\"))\r\n" +
                "        {\r\n" +
                "            status = \"AIRCRAFT units are only eligible to make an Ingress move.\";\r\n" +
                "            return;\r\n" +
                "        }\r\n"
            );
        }

        method = method.Replace(
            "bool wasEngagedBeforeMove =\r\n            IsEngaged(selectedSquad);",
            "bool wasEngagedBeforeMove =\r\n            Core11IsEngagedForNormalMovement(selectedSquad);"
        );

        method = method.Replace(
            "bool wasEngagedBeforeMove =\n            IsEngaged(selectedSquad);",
            "bool wasEngagedBeforeMove =\n            Core11IsEngagedForNormalMovement(selectedSquad);"
        );

        if (!method.Contains(
                "Core11WholeSquadPathIsClear"))
        {
            string anchor =
                "        if (!selectedSquad\r\n            .CanTranslateWithinNormalMove(delta))";

            int index = method.IndexOf(anchor,
                StringComparison.Ordinal);

            if (index < 0)
            {
                anchor =
                    "        if (!selectedSquad\n            .CanTranslateWithinNormalMove(delta))";
                index = method.IndexOf(anchor,
                    StringComparison.Ordinal);
            }

            if (index < 0)
                throw new InvalidOperationException(
                    "TryMoveWholeSquad movement allowance anchor missing."
                );

            string insert =
                "        if (!Core11WholeSquadPathIsClear(selectedSquad, delta))\r\n" +
                "        {\r\n" +
                "            status = \"That whole-unit movement path is blocked by terrain or enemy models.\";\r\n" +
                "            return;\r\n" +
                "        }\r\n\r\n";

            method = method.Insert(index, insert);
        }

        if (Normalize(method) != Normalize(original))
        {
            Backup(whole.Path);
            source = ReplaceExtract(whole, source, method);
            WriteSource(whole.Path, source);
            AddTouched(touched, whole.Path);
        }
    }

    private static void PatchContextBar(
        List<string> touched)
    {
        MethodLocation location =
            FindGameMethod("DrawContextActionBar");

        string source = File.ReadAllText(location.Path);
        string method = Extract(location, source);

        if (method.Contains(
                "DrawCore11ContextControls"))
        {
            return;
        }

        Match x = Regex.Match(
            method,
            @"\bfloat\s+x\s*=.*?;",
            RegexOptions.Singleline
        );

        if (!x.Success)
        {
            throw new InvalidOperationException(
                "DrawContextActionBar x-position declaration not found."
            );
        }

        string insert =
            "\r\n\r\n        DrawCore11ContextControls(bar, ref x);";

        method = method.Insert(
            x.Index + x.Length,
            insert
        );

        Backup(location.Path);
        source = ReplaceExtract(location, source, method);
        WriteSource(location.Path, source);
        AddTouched(touched, location.Path);
    }

    private static void PatchBoardPlacementClick(
        List<string> touched)
    {
        foreach (string path in ExistingGameFiles())
        {
            string source = File.ReadAllText(path);

            if (source.Contains(
                    "Core11HandleBoardPlacementClick(hit.point)"))
            {
                return;
            }

            Match reserveBlock =
                Regex.Match(
                    source,
                    @"(?m)^(?<indent>\s*)if\s*\(\s*reservePlacementSquad\s*!=\s*null\s*&&"
                );

            if (!reserveBlock.Success)
                continue;

            string indent =
                reserveBlock.Groups["indent"].Value;

            string insert =
                indent +
                "if (hit.collider.GetComponent<BoardSurface>() != null &&\r\n" +
                indent +
                "    Core11HandleBoardPlacementClick(hit.point))\r\n" +
                indent +
                "{\r\n" +
                indent +
                "    return;\r\n" +
                indent +
                "}\r\n\r\n";

            Backup(path);
            source = source.Insert(
                reserveBlock.Index,
                insert
            );
            WriteSource(path, source);
            AddTouched(touched, path);
            return;
        }

        throw new InvalidOperationException(
            "Reserve placement board-click block was not found."
        );
    }

    private static void PatchTraditionalInsaneBravery(
        List<string> touched)
    {
        MethodLocation location =
            FindGameMethod("BeginNextTraditionalBattleShock");

        string source = File.ReadAllText(location.Path);
        string method = Extract(location, source);

        if (method.Contains(
                "Core11OfferInsaneBraveryForTraditionalBattleShock"))
        {
            return;
        }

        string anchor =
            "            traditionalBattleShockPending =\r\n                true;";

        int index = method.IndexOf(anchor,
            StringComparison.Ordinal);

        if (index < 0)
        {
            anchor =
                "            traditionalBattleShockPending =\n                true;";
            index = method.IndexOf(anchor,
                StringComparison.Ordinal);
        }

        if (index < 0)
        {
            throw new InvalidOperationException(
                "Traditional Battle-shock pending anchor not found."
            );
        }

        index += anchor.Length;

        string insert =
            "\r\n\r\n            if (Core11OfferInsaneBraveryForTraditionalBattleShock(\r\n" +
            "                    traditionalBattleShockUnit))\r\n" +
            "            {\r\n" +
            "                return;\r\n" +
            "            }";

        method = method.Insert(index, insert);

        Backup(location.Path);
        source = ReplaceExtract(location, source, method);
        WriteSource(location.Path, source);
        AddTouched(touched, location.Path);
    }

    private static void PatchRulesEngine(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/RulesEngine.cs";

        string source = File.ReadAllText(path);
        string original = source;

        MethodLocation location =
            FindMethodInSource(
                path,
                source,
                "ResolveWeaponAttacks"
            );

        string method = Extract(location, source);

        if (!method.Contains(
                "Core11PlungingFireApplies"))
        {
            const string anchor =
                "            bool torrent =";

            int index = method.IndexOf(
                anchor,
                StringComparison.Ordinal
            );

            if (index < 0)
                throw new InvalidOperationException(
                    "RulesEngine Plunging Fire anchor not found."
                );

            string block =
@"            // v41 / 22.05 Plunging Fire improves BS by 1. It does not
            // apply to attacks made by or targeting AIRCRAFT.
            if (mode == AttackMode.Ranged &&
                game != null &&
                game.Core11PlungingFireApplies(
                    model,
                    target
                ))
            {
                skill = Mathf.Max(2, skill - 1);
            }

";

            method = method.Insert(index, block);
        }

        if (!method.Contains(
                "Core11HasEpicChallenge"))
        {
            string pattern =
                @"bool\s+precision\s*=\s*WeaponRuleParser\.Has\(\s*weapon,\s*""precision""\s*\)\s*;";

            method = Regex.Replace(
                method,
                pattern,
                "bool precision =\r\n" +
                "                WeaponRuleParser.Has(\r\n" +
                "                    weapon,\r\n" +
                "                    \"precision\"\r\n" +
                "                ) ||\r\n" +
                "                (game != null &&\r\n" +
                "                 game.Core11HasEpicChallenge(model));",
                RegexOptions.Singleline
            );
        }

        source = ReplaceExtract(location, source, method);

        if (Normalize(source) != Normalize(original))
        {
            Backup(path);
            WriteSource(path, source);
            AddTouched(touched, path);
        }
    }

    private static void PatchInteractiveAttack(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/InteractiveAttackController.cs";

        string source = File.ReadAllText(path);
        string original = source;

        if (!source.Contains(
                "Core11PlungingFireApplies"))
        {
            const string skillAnchor =
                "            volley.hitRollModifier =";

            int skillIndex =
                source.IndexOf(
                    skillAnchor,
                    StringComparison.Ordinal
                );

            if (skillIndex < 0)
            {
                throw new InvalidOperationException(
                    "InteractiveAttack Plunging Fire skill anchor not found."
                );
            }

            string skillBlock =
                "            if (mode == AttackMode.Ranged &&\r\n" +
                "                game != null &&\r\n" +
                "                game.Core11PlungingFireApplies(\r\n" +
                "                    first.model,\r\n" +
                "                    target\r\n" +
                "                ))\r\n" +
                "            {\r\n" +
                "                volley.skill = Mathf.Max(2, volley.skill - 1);\r\n" +
                "            }\r\n\r\n";

            source = source.Insert(
                skillIndex,
                skillBlock
            );
        }

        if (!source.Contains(
                "Core11HasEpicChallenge"))
        {
            Regex precisionAssignment =
                new Regex(
                    @"(\.precision\s*=\s*[^;]+;)",
                    RegexOptions.Singleline
                );

            source = precisionAssignment.Replace(
                source,
                "$1\r\n            volley.precision = volley.precision ||\r\n" +
                "                (game != null &&\r\n" +
                "                 game.Core11HasEpicChallenge(first.model));",
                1
            );
        }

        if (Normalize(source) != Normalize(original))
        {
            Backup(path);
            WriteSource(path, source);
            AddTouched(touched, path);
        }
    }

    private static void PatchFight11(
        List<string> touched)
    {
        const string path =
            "Assets/Scripts/Core/GameController.Fight11.cs";

        string source = File.ReadAllText(path);
        string original = source;

        source = InsertAtMethodStartIfPresent(
            path,
            source,
            "Fight11EligibleFightUnits",
            "        List<SquadController> core11Forced =\r\n" +
            "            Core11ForcedFightSelection(faction, fightsFirstOnly);\r\n" +
            "        if (core11Forced != null)\r\n" +
            "            return core11Forced;\r\n\r\n"
        );

        source = InsertAtMethodStartIfPresent(
            path,
            source,
            "Fight11TryFight",
            "        if (attacker != null && target != null &&\r\n" +
            "            !CoreRules11Aircraft.CanFightTarget(attacker, target))\r\n" +
            "        {\r\n" +
            "            status = \"AIRCRAFT melee can only interact with FLYING units/models.\";\r\n" +
            "            return;\r\n" +
            "        }\r\n\r\n"
        );

        source = InsertAtMethodStartIfPresent(
            path,
            source,
            "TryFightModelAttack",
            "        if (model != null && target != null &&\r\n" +
            "            !CoreRules11Aircraft.CanFightTarget(model.Squad, target))\r\n" +
            "        {\r\n" +
            "            status = \"That model cannot make melee attacks against that AIRCRAFT/FLY target.\";\r\n" +
            "            return;\r\n" +
            "        }\r\n\r\n"
        );

        source = source.Replace(
            "enemy.FactionId != unit.FactionId &&\r\n                    UnitsAreEngaged(unit, enemy)",
            "enemy.FactionId != unit.FactionId &&\r\n" +
            "                    CoreRules11Aircraft.CanFightTarget(unit, enemy) &&\r\n" +
            "                    UnitsAreEngaged(unit, enemy)"
        );

        source = source.Replace(
            "enemy.FactionId != unit.FactionId &&\n                    UnitsAreEngaged(unit, enemy)",
            "enemy.FactionId != unit.FactionId &&\n" +
            "                    CoreRules11Aircraft.CanFightTarget(unit, enemy) &&\n" +
            "                    UnitsAreEngaged(unit, enemy)"
        );

        source = source.Replace(
            "enemy.FactionId != unit.FactionId &&\r\n                    Fight11UnitDistance(unit, enemy)",
            "enemy.FactionId != unit.FactionId &&\r\n" +
            "                    CoreRules11Aircraft.CanFightTarget(unit, enemy) &&\r\n" +
            "                    Fight11UnitDistance(unit, enemy)"
        );

        source = source.Replace(
            "enemy.FactionId != unit.FactionId &&\n                    Fight11UnitDistance(unit, enemy)",
            "enemy.FactionId != unit.FactionId &&\n" +
            "                    CoreRules11Aircraft.CanFightTarget(unit, enemy) &&\n" +
            "                    Fight11UnitDistance(unit, enemy)"
        );

        if (!source.Contains(
                "Core11CounteroffensiveDecisionIsPending(completed)"))
        {
            source = source.Replace(
                "        Fight11AdvanceFightPriority(completed);",
                "        if (Core11CounteroffensiveDecisionIsPending(completed))\r\n" +
                "            return;\r\n\r\n" +
                "        Fight11AdvanceFightPriority(completed);"
            );
        }

        if (Normalize(source) != Normalize(original))
        {
            Backup(path);
            WriteSource(path, source);
            AddTouched(touched, path);
        }
    }

    private static void PatchGameMethodStart(
        string method,
        string text,
        List<string> touched)
    {
        MethodLocation location = FindGameMethod(method);
        string source = File.ReadAllText(location.Path);
        string current = Extract(location, source);

        string marker = Normalize(text);
        if (Normalize(current).Contains(marker))
            return;

        int brace = current.IndexOf('{');
        if (brace < 0)
            throw new InvalidOperationException(
                method + " opening brace missing."
            );

        current = current.Insert(
            brace + 1,
            "\r\n" + text
        );

        Backup(location.Path);
        source = ReplaceExtract(location, source, current);
        WriteSource(location.Path, source);
        AddTouched(touched, location.Path);
    }

    private static void PatchGameMethodBody(
        string method,
        string body,
        List<string> touched)
    {
        MethodLocation location = FindGameMethod(method);
        string source = File.ReadAllText(location.Path);
        string current = Extract(location, source);
        int brace = current.IndexOf('{');
        int close = current.LastIndexOf('}');

        string replacement =
            current.Substring(0, brace + 1) +
            "\r\n" + body + "    " +
            current.Substring(close);

        if (Normalize(current) == Normalize(replacement))
            return;

        Backup(location.Path);
        source = ReplaceExtract(location, source, replacement);
        WriteSource(location.Path, source);
        AddTouched(touched, location.Path);
    }

    private static void PatchGameMethodBodyIfPresent(
        string method,
        string body,
        List<string> touched)
    {
        MethodLocation location;
        if (!TryFindGameMethod(method, out location))
            return;

        string source = File.ReadAllText(location.Path);
        string current = Extract(location, source);
        int brace = current.IndexOf('{');
        int close = current.LastIndexOf('}');

        string replacement =
            current.Substring(0, brace + 1) +
            "\r\n" + body + "    " +
            current.Substring(close);

        if (Normalize(current) == Normalize(replacement))
            return;

        Backup(location.Path);
        source = ReplaceExtract(location, source, replacement);
        WriteSource(location.Path, source);
        AddTouched(touched, location.Path);
    }

    private static string InsertAtMethodStartIfPresent(
        string path,
        string source,
        string method,
        string text)
    {
        MethodLocation location;
        if (!TryFindMethodInSource(
                path,
                source,
                method,
                out location))
        {
            return source;
        }

        string current = Extract(location, source);
        if (Normalize(current).Contains(
                Normalize(text)))
        {
            return source;
        }

        int brace = current.IndexOf('{');
        current = current.Insert(
            brace + 1,
            "\r\n" + text
        );

        return ReplaceExtract(
            location,
            source,
            current
        );
    }

    private static string ReplaceMethodInSource(
        string path,
        string source,
        string method,
        string replacement)
    {
        MethodLocation location =
            FindMethodInSource(
                path,
                source,
                method
            );

        return
            source.Substring(0, location.Start) +
            replacement +
            source.Substring(location.EndExclusive);
    }

    private static MethodLocation FindGameMethod(
        string method)
    {
        MethodLocation location;
        if (!TryFindGameMethod(method, out location))
        {
            throw new InvalidOperationException(
                "Could not locate GameController method: " +
                method
            );
        }

        return location;
    }

    private static bool TryFindGameMethod(
        string method,
        out MethodLocation found)
    {
        found = null;
        List<MethodLocation> matches =
            new List<MethodLocation>();

        foreach (string path in ExistingGameFiles())
        {
            string source = File.ReadAllText(path);
            MethodLocation location;

            if (TryFindMethodInSource(
                    path,
                    source,
                    method,
                    out location))
            {
                matches.Add(location);
            }
        }

        if (matches.Count == 1)
        {
            found = matches[0];
            return true;
        }

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                "Expected one GameController method named " +
                method + ", found " +
                matches.Count + "."
            );
        }

        return false;
    }

    private static string[] ExistingGameFiles()
    {
        return GameFiles
            .Where(File.Exists)
            .ToArray();
    }

    private static MethodLocation FindMethodInSource(
        string path,
        string source,
        string method)
    {
        MethodLocation result;
        if (!TryFindMethodInSource(
                path,
                source,
                method,
                out result))
        {
            throw new InvalidOperationException(
                "Method not found: " + method +
                " in " + path
            );
        }

        return result;
    }

    private static bool TryFindMethodInSource(
        string path,
        string source,
        string method,
        out MethodLocation result)
    {
        result = null;

        Regex signature = new Regex(
            @"(?m)^[ \t]*(?:public|private|protected|internal)\s+" +
            @"(?:(?:static|virtual|override|sealed|async|new)\s+)*" +
            @"(?:[A-Za-z0-9_<>,\.\[\]\?]+\s+)+" +
            Regex.Escape(method) +
            @"\s*\("
        );

        Match match = signature.Match(source);
        if (!match.Success)
            return false;

        int open = FindMethodOpeningBrace(
            source,
            match.Index
        );

        if (open < 0)
            return false;

        int close = FindMatchingBrace(source, open);
        if (close < 0)
            return false;

        result = new MethodLocation
        {
            Path = path,
            Start = match.Index,
            OpenBrace = open,
            CloseBrace = close,
            EndExclusive = close + 1
        };

        return true;
    }

    private static int FindMethodOpeningBrace(
        string source,
        int start)
    {
        int paren = source.IndexOf('(', start);
        if (paren < 0)
            return -1;

        int depth = 0;
        bool inString = false;
        bool inChar = false;
        bool escape = false;

        for (int i = paren;
             i < source.Length;
             i++)
        {
            char c = source[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if ((inString || inChar) &&
                c == '\\')
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
                    int brace =
                        source.IndexOf('{', i + 1);
                    int semicolon =
                        source.IndexOf(';', i + 1);

                    if (semicolon >= 0 &&
                        semicolon < brace)
                    {
                        return -1;
                    }

                    return brace;
                }
            }
        }

        return -1;
    }

    private static int FindMatchingBrace(
        string source,
        int open)
    {
        int depth = 0;
        bool inString = false;
        bool inChar = false;
        bool inLineComment = false;
        bool inBlockComment = false;
        bool escape = false;
        bool verbatim = false;

        for (int i = open;
             i < source.Length;
             i++)
        {
            char c = source[i];
            char next =
                i + 1 < source.Length
                ? source[i + 1]
                : '\0';

            if (inLineComment)
            {
                if (c == '\n')
                    inLineComment = false;
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

    private static string Extract(
        MethodLocation location,
        string source)
    {
        return source.Substring(
            location.Start,
            location.EndExclusive -
            location.Start
        );
    }

    private static string ReplaceExtract(
        MethodLocation location,
        string source,
        string replacement)
    {
        return
            source.Substring(0, location.Start) +
            replacement +
            source.Substring(location.EndExclusive);
    }

    private static void ValidateResult()
    {
        if (!File.Exists(RuntimeCorePath) ||
            !File.Exists(RuntimeGamePath))
        {
            throw new InvalidOperationException(
                "v41 generated runtime files are missing."
            );
        }

        string gameRuntime =
            File.ReadAllText(RuntimeGamePath);

        string coreRuntime =
            File.ReadAllText(RuntimeCorePath);

        string squad =
            File.ReadAllText(
                "Assets/Scripts/Core/SquadController.cs"
            );

        string rules =
            File.ReadAllText(
                "Assets/Scripts/Core/RulesEngine.cs"
            );

        string fight =
            File.ReadAllText(
                "Assets/Scripts/Core/GameController.Fight11.cs"
            );

        string allGame = string.Join(
            "\n",
            ExistingGameFiles()
                .Select(File.ReadAllText)
                .ToArray()
        );

        string[] required =
        {
            Marker,
            "Core11Install",
            "Core11PrepareAircraftAndValidateMuster",
            "Core11OpenRapidIngressWindow",
            "Core11OpenHeroicInterventionWindow",
            "Core11UseExplosives",
            "Core11UseCrushingImpact",
            "Core11BeginDisembark",
            "Core11OfferSmokescreenWindow",
            "Core11BeginCombatDisembark",
            "Core11CheckDestroyedTransportForEmergencyDisembark",
            "Core11FindNearestSurgeEnemy"
        };

        foreach (string value in required)
        {
            if (!gameRuntime.Contains(value))
            {
                throw new InvalidOperationException(
                    "v41 validation failed: runtime symbol missing " +
                    value
                );
            }
        }

        if (!coreRuntime.Contains(
                "CoreRules11Terrain") ||
            !coreRuntime.Contains(
                "CoreRules11TransportRules") ||
            !coreRuntime.Contains(
                "CoreRules11Aircraft"))
        {
            throw new InvalidOperationException(
                "v41 core helper runtime validation failed."
            );
        }

        if (!squad.Contains(
                "SquadBattlefieldState.Embarked") ||
            !squad.Contains(
                "public bool EmbarkWithin") ||
            !squad.Contains(
                "CoreRules11FlightRegistry"))
        {
            throw new InvalidOperationException(
                "v41 SquadController transport/fly migration was not installed."
            );
        }

        if (!allGame.Contains(
                "Core11CanAdvancePhase") ||
            !allGame.Contains(
                "Core11HandleBoardPlacementClick") ||
            !allGame.Contains(
                "Core11CanSeeModel"))
        {
            throw new InvalidOperationException(
                "v41 GameController ownership hooks are incomplete."
            );
        }

        if (!rules.Contains(
                "Core11PlungingFireApplies") ||
            !fight.Contains(
                "Core11ForcedFightSelection"))
        {
            throw new InvalidOperationException(
                "v41 attack/fight integration validation failed."
            );
        }
    }

    private static void WriteReport(
        List<string> touched)
    {
        StringBuilder report =
            new StringBuilder();

        report.AppendLine(
            "Warboard v41 - Core Rules Completion"
        );
        report.AppendLine(DateTime.Now.ToString("u"));
        report.AppendLine();
        report.AppendLine("Installed direct 11e systems:");
        report.AppendLine("- terrain categories, cover, Hidden/detection, obscuring/solid LOS approximation, Plunging Fire");
        report.AppendLine("- transports: capacity parsing, embark, disembark, emergency disembark, dedicated transport formation gate");
        report.AppendLine("- Fly: Take to the Skies -2 move and terrain/model pass-through path handling");
        report.AppendLine("- Aircraft: reserves lifecycle, charge/fight restrictions, movement interactions");
        report.AppendLine("- core Stratagem completion: preserved Command Re-roll/Fire Overwatch and added Epic Challenge, Insane Bravery, Explosives, Crushing Impact, Rapid Ingress, Smokescreen, Heroic Intervention and Counteroffensive");
        report.AppendLine("- muster validation: points, enhancements, Warlord, unit limits, Battleline/Dedicated Transport doubling, Epic Hero limit");
        report.AppendLine();
        report.AppendLine("Touched/generated files:");

        foreach (string path in touched.Distinct())
            report.AppendLine("- " + path);

        File.WriteAllText(
            ReportPath,
            report.ToString(),
            new UTF8Encoding(false)
        );
    }

    private static void CleanupInstaller()
    {
        if (AssetDatabase.IsValidFolder(PayloadRoot))
            AssetDatabase.DeleteAsset(PayloadRoot);

        if (File.Exists(SelfPath))
            AssetDatabase.DeleteAsset(SelfPath);
    }

    private static void Backup(string path)
    {
        if (!File.Exists(path))
            return;

        string safe =
            path.Replace('/', '_')
                .Replace('\\', '_');

        string backup =
            Path.Combine(
                BackupRoot,
                safe + ".txt"
            );

        if (!File.Exists(backup))
            File.Copy(path, backup, true);
    }

    private static void WriteSource(
        string path,
        string source)
    {
        string directory =
            Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        source = (source ?? "")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", "\r\n");

        File.WriteAllText(
            path,
            source,
            new UTF8Encoding(false)
        );
    }

    private static void AddTouched(
        List<string> touched,
        string path)
    {
        if (!touched.Contains(path))
            touched.Add(path);
    }

    private static string Normalize(
        string value)
    {
        return Regex.Replace(
            value ?? "",
            @"\s+",
            " "
        ).Trim();
    }

    private sealed class MethodLocation
    {
        public string Path;
        public int Start;
        public int OpenBrace;
        public int CloseBrace;
        public int EndExclusive;
    }
}
#endif
