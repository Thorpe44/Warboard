using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Shared pre-game and in-battle UI for the v46 data-driven faction packs.
/// Source-derived rule text comes from the JSON faction packs. Deterministic rules
/// are handled by WarboardFactionExtensionHub; rules that require arbitrary
/// targets/placement remain explicit player choices rather than guesses.
/// </summary>
[DefaultExecutionOrder(-31970)]
public sealed class StandardFactionSetupUI :
    MonoBehaviour
{
    private static StandardFactionSetupUI instance;

    private void Awake()
    {
        instance = this;
    }

    public static void OpenFactionRules(
        string factionId)
    {
        if (instance == null)
            return;

        instance.rulesFaction =
            factionId ?? "";

        instance.showRules = true;
    }

    private readonly Dictionary<string, HashSet<string>>
        selections =
            new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string>
        pastedRosterText =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string>
        pasteStatus =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

    private Vector2 ruleScroll;
    private bool showRules;
    private string rulesFaction = "";

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object
            .FindAnyObjectByType<
                StandardFactionSetupUI>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "WarboardStandardFactionSetupUI");

        UnityEngine.Object
            .DontDestroyOnLoad(go);

        go.AddComponent<
            StandardFactionSetupUI>();
    }

    private List<StandardFactionGameController>
        Controllers()
    {
        FactionControllerHost host =
            FactionControllerHost.Instance;

        if (host == null)
        {
            return new List<
                StandardFactionGameController>();
        }

        return host.Controllers.Values
            .OfType<
                StandardFactionGameController>()
            .Where(
                controller =>
                    controller != null &&
                    controller.Pack != null)
            .OrderBy(
                controller =>
                    controller.FactionId)
            .ToList();
    }

    private void OnGUI()
    {
        List<StandardFactionGameController>
            controllers =
                Controllers();

        if (controllers.Count == 0)
            return;

        foreach (
            StandardFactionGameController
                controller
            in controllers)
        {
            if (!controller.DetachmentLocked)
            {
                DrawDetachmentModal(
                    controller);
                return;
            }

            if (controller.HasDetachment(
                    "ORBITAL ASSAULT FORCE") &&
                !controller.IsReadyForDeployment &&
                controller.RequiredRapidDropUnits() > 0)
            {
                DrawRapidDropModal(
                    controller);
                return;
            }
        }

        foreach (
            StandardFactionGameController
                controller
            in controllers)
        {
            if (DrawRequiredBattleChoice(
                    controller))
            {
                return;
            }
        }

        DrawOutOfTurnArmyRuleActions(
            controllers);

        DrawFactionRulesButton(
            controllers);

        if (showRules)
        {
            StandardFactionGameController
                controller =
                    controllers.FirstOrDefault(
                        value =>
                            string.Equals(
                                value.FactionId,
                                rulesFaction,
                                StringComparison.OrdinalIgnoreCase))
                    ?? controllers.FirstOrDefault();

            if (controller != null)
                DrawRulesPanel(controller);
        }
    }

    private HashSet<string> SelectionFor(
        StandardFactionGameController
            controller)
    {
        HashSet<string> result;

        if (!selections.TryGetValue(
                controller.FactionId,
                out result) ||
            result == null)
        {
            result =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            selections[
                controller.FactionId] =
                    result;
        }

        return result;
    }

    private string PasteTextFor(
        StandardFactionGameController
            controller)
    {
        string value;

        if (pastedRosterText.TryGetValue(
                controller.FactionId,
                out value))
        {
            return value ?? "";
        }

        WarboardRosterManifest manifest =
            RosterTextManifestStore.Get(
                controller.FactionId);

        value =
            manifest != null
            ? manifest.RawText ?? ""
            : "";

        pastedRosterText[
            controller.FactionId] =
                value;

        return value;
    }

    private void DrawBackdrop()
    {
        Color old = GUI.color;

        GUI.color =
            new Color(
                0f,
                0f,
                0f,
                0.88f);

        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height),
            Texture2D.whiteTexture);

        GUI.color = old;
    }

    private void DrawDetachmentModal(
        StandardFactionGameController
            controller)
    {
        int oldDepth = GUI.depth;
        GUI.depth = -25000;

        DrawBackdrop();

        float width =
            Mathf.Min(
                1120f,
                Screen.width - 28f);

        float height =
            Mathf.Min(
                820f,
                Screen.height - 28f);

        Rect panel =
            new Rect(
                (Screen.width - width) *
                    0.5f,
                (Screen.height - height) *
                    0.5f,
                width,
                height);

        GUI.Box(panel, "");

        GUIStyle title =
            new GUIStyle(
                GUI.skin.label);

        title.fontSize = 22;
        title.fontStyle =
            FontStyle.Bold;

        title.alignment =
            TextAnchor.MiddleCenter;

        GUIStyle section =
            new GUIStyle(
                GUI.skin.label);

        section.fontSize = 15;
        section.fontStyle =
            FontStyle.Bold;

        GUIStyle body =
            new GUIStyle(
                GUI.skin.label);

        body.wordWrap = true;

        GUI.Label(
            new Rect(
                panel.x + 18f,
                panel.y + 10f,
                panel.width - 36f,
                34f),
            controller.FactionId +
            " - " +
            controller.DisplayName.ToUpper() +
            " CONFIGURATION",
            title);

        GUI.Label(
            new Rect(
                panel.x + 28f,
                panel.y + 48f,
                panel.width - 56f,
                48f),
            controller.Pack.version +
            ". Paste the New Recruit roster text to detect Detachments automatically, or select them manually. Crusade and Boarding Actions are intentionally not part of this matched-play pack.",
            body);

        float leftX =
            panel.x + 28f;

        float y =
            panel.y + 104f;

        float gap = 18f;

        float leftWidth =
            Mathf.Min(
                438f,
                panel.width * 0.42f);

        float rightX =
            leftX +
            leftWidth +
            gap;

        float rightWidth =
            panel.x +
            panel.width -
            28f -
            rightX;

        GUI.Label(
            new Rect(
                leftX,
                y,
                leftWidth,
                24f),
            "NEW RECRUIT ROSTER TEXT",
            section);

        string text =
            PasteTextFor(
                controller);

        Rect textRect =
            new Rect(
                leftX,
                y + 28f,
                leftWidth,
                Mathf.Max(
                    290f,
                    panel.height - 330f));

        text =
            GUI.TextArea(
                textRect,
                text);

        pastedRosterText[
            controller.FactionId] =
                text;

        float buttonY =
            textRect.yMax + 8f;

        if (GUI.Button(
            new Rect(
                leftX,
                buttonY,
                205f,
                34f),
            "APPLY PASTED ROSTER"))
        {
            ApplyPastedRoster(
                controller,
                text);
        }

        if (GUI.Button(
            new Rect(
                leftX + 215f,
                buttonY,
                115f,
                34f),
            "CLEAR"))
        {
            RosterTextManifestStore
                .Clear(
                    controller.FactionId);

            pastedRosterText[
                controller.FactionId] =
                    "";

            pasteStatus[
                controller.FactionId] =
                    "Roster manifest cleared.";

            controller
                .TryUnlockBeforeDeployment();

            SelectionFor(
                controller).Clear();
        }

        string status;

        if (!pasteStatus.TryGetValue(
                controller.FactionId,
                out status))
        {
            status =
                controller.RosterProbeStatus;
        }

        GUI.Label(
            new Rect(
                leftX,
                buttonY + 42f,
                leftWidth,
                72f),
            string.IsNullOrWhiteSpace(
                status)
            ? controller.RosterProbeStatus
            : status,
            body);

        GUI.Label(
            new Rect(
                rightX,
                y,
                rightWidth,
                24f),
            "MANUAL DETACHMENT SELECTION",
            section);

        HashSet<string> selected =
            SelectionFor(
                controller);

        float rowY =
            y + 32f;

        foreach (
            StandardFactionDetachment11
                detachment
            in controller
                .AvailableDetachments())
        {
            if (detachment == null)
                continue;

            bool was =
                selected.Contains(
                    detachment.name);

            string tagText =
                detachment.tags != null &&
                detachment.tags.Length > 0
                ? " [" +
                  string.Join(
                      ", ",
                      detachment.tags) +
                  "]"
                : "";

            bool now =
                GUI.Toggle(
                    new Rect(
                        rightX,
                        rowY,
                        rightWidth,
                        25f),
                    was,
                    detachment.name +
                    " - " +
                    detachment.dp +
                    "DP" +
                    tagText);

            if (now != was)
            {
                if (now)
                {
                    selected.Add(
                        detachment.name);
                }
                else
                {
                    selected.Remove(
                        detachment.name);
                }
            }

            rowY += 28f;
        }

        string validation;

        bool valid =
            controller
                .TryValidateDetachmentSelection(
                    selected,
                    out validation);

        int spent =
            selected
                .Select(
                    name =>
                        controller
                            .GetDetachment(
                                name))
                .Where(
                    definition =>
                        definition != null)
                .Sum(
                    definition =>
                        definition.dp);

        GUI.Label(
            new Rect(
                rightX,
                rowY + 4f,
                rightWidth,
                28f),
            "Selected: " +
            spent +
            (controller
                .DetachmentPointLimit > 0
                ? "/" +
                  controller
                    .DetachmentPointLimit
                : "") +
            " DP",
            section);

        GUI.Label(
            new Rect(
                rightX,
                rowY + 34f,
                rightWidth,
                62f),
            valid
            ? "Valid matched-play Detachment selection."
            : validation,
            body);

        GUI.enabled = valid;

        if (GUI.Button(
            new Rect(
                rightX,
                panel.yMax - 60f,
                Mathf.Min(
                    360f,
                    rightWidth),
                40f),
            "CONFIRM " +
            controller.DisplayName.ToUpper() +
            " DETACHMENTS"))
        {
            if (controller
                .TryLockDetachments(
                    selected,
                    "Manual pre-game selection"))
            {
                pasteStatus[
                    controller.FactionId] =
                        controller.DisplayName +
                        " Detachments locked.";
            }
        }

        GUI.enabled = true;

        if (!string.IsNullOrWhiteSpace(
                controller.SelectionError))
        {
            GUI.Label(
                new Rect(
                    rightX,
                    panel.yMax - 108f,
                    rightWidth,
                    42f),
                controller.SelectionError,
                body);
        }

        GUI.depth = oldDepth;
    }

    private void ApplyPastedRoster(
        StandardFactionGameController
            controller,
        string text)
    {
        WarboardRosterManifest manifest;
        string error;

        if (!RosterTextManifestStore
            .TrySet(
                controller.FactionId,
                text,
                out manifest,
                out error))
        {
            pasteStatus[
                controller.FactionId] =
                    error;
            return;
        }

        pasteStatus[
            controller.FactionId] =
                "Parsed: " +
                manifest.Summary();

        // RefreshArmy will re-probe the manifest on the next roster event,
        // but the controller already polls the current manifest while unlocked.
        controller.RefreshArmy(
            GameController.Current != null
            ? GameController.Current
                .GetArmy(
                    controller.FactionId)
            : new List<SquadController>());
    }

    private void DrawRapidDropModal(
        StandardFactionGameController
            controller)
    {
        int oldDepth = GUI.depth;
        GUI.depth = -25000;

        DrawBackdrop();

        float width =
            Mathf.Min(
                760f,
                Screen.width - 32f);

        float height =
            Mathf.Min(
                650f,
                Screen.height - 32f);

        Rect panel =
            new Rect(
                (Screen.width - width) *
                    0.5f,
                (Screen.height - height) *
                    0.5f,
                width,
                height);

        GUI.Box(panel, "");

        GUIStyle title =
            new GUIStyle(
                GUI.skin.label);

        title.fontSize = 21;
        title.fontStyle =
            FontStyle.Bold;

        GUI.Label(
            new Rect(
                panel.x + 20f,
                panel.y + 16f,
                panel.width - 40f,
                30f),
            "RAPID-DROP DEPLOYMENT",
            title);

        int required =
            controller
                .RequiredRapidDropUnits();

        GUI.Label(
            new Rect(
                panel.x + 20f,
                panel.y + 52f,
                panel.width - 40f,
                44f),
            "Select exactly " +
            required +
            " non-TITANIC ADEPTUS ASTARTES unit(s). Models in those units gain Deep Strike.");

        float y =
            panel.y + 105f;

        foreach (SquadController unit
            in controller
                .RapidDropCandidates())
        {
            bool selected =
                controller
                    .RapidDropContains(
                        unit);

            bool now =
                GUI.Toggle(
                    new Rect(
                        panel.x + 28f,
                        y,
                        panel.width - 56f,
                        28f),
                    selected,
                    unit.DisplayName);

            if (now != selected)
            {
                controller
                    .ToggleRapidDropUnit(
                        unit);
            }

            y += 30f;

            if (y >
                panel.yMax - 88f)
            {
                break;
            }
        }

        GUI.enabled =
            controller
                .RapidDropCandidates()
                .Count() >= required;

        if (GUI.Button(
            new Rect(
                panel.x +
                    panel.width -
                    250f,
                panel.yMax - 58f,
                225f,
                38f),
            "LOCK RAPID-DROP UNITS"))
        {
            controller
                .LockRapidDropUnits();
        }

        GUI.enabled = true;
        GUI.depth = oldDepth;
    }

    private bool DrawRequiredBattleChoice(
        StandardFactionGameController
            controller)
    {
        GameController game =
            GameController.Current;

        if (game == null ||
            controller == null ||
            game.BattleRound <= 0)
        {
            return false;
        }

        if (controller.HyperAdaptationRequired)
        {
            DrawSimpleChoiceModal(
                "HYPER-ADAPTATIONS",
                "Select the Invasion Fleet Hyper-adaptation that remains active for this battle.",
                new[]
                {
                    "SWARMING INSTINCTS",
                    "HYPER-AGGRESSION",
                    "HIVE PREDATORS"
                },
                controller
                    .SelectHyperAdaptation
            );

            return true;
        }

        if (controller.PsychicDisciplineRequired)
        {
            DrawSimpleChoiceModal(
                "PSYCHIC DISCIPLINES",
                "Select the Librarius Conclave discipline active until the end of this battle round.",
                new[]
                {
                    "BIOMANCY DISCIPLINE",
                    "DIVINATION DISCIPLINE",
                    "PYROMANCY DISCIPLINE",
                    "TELEKINESIS DISCIPLINE",
                    "TELEPATHY DISCIPLINE"
                },
                controller
                    .SelectPsychicDiscipline
            );

            return true;
        }

        if (controller.OathSelectionRequired)
        {
            DrawUnitChoiceModal(
                "OATH OF MOMENT",
                "Select one enemy unit from the opponent's army. It remains the Oath target until the start of the next Space Marines Command phase. Units in Reserves or embarked units remain valid source targets.",
                game.StandardEnemyUnits(
                    controller.FactionId),
                controller.SetOathTarget
            );

            return true;
        }

        if (controller.PreySelectionRequired)
        {
            List<SquadController> candidates =
                game.StandardEnemyUnits(
                        controller.FactionId)
                    .Where(
                        unit =>
                            unit.HasKeyword(
                                "MONSTER") ||
                            unit.HasKeyword(
                                "VEHICLE") ||
                            unit.HasKeyword(
                                "CHARACTER"))
                    .ToList();

            if (candidates.Count > 0)
            {
                DrawUnitChoiceModal(
                    "DA HUNT IS ON",
                    "Select the Prey for this Orks Command phase.",
                    candidates,
                    controller.SetPreyTarget
                );

                return true;
            }
        }

        if (controller.LootSelectionRequired &&
            game.StandardObjectives != null &&
            game.StandardObjectives.Count > 0)
        {
            DrawObjectiveChoiceModal(
                controller);

            return true;
        }

        return false;
    }

    private void DrawSimpleChoiceModal(
        string titleText,
        string description,
        IEnumerable<string> values,
        Action<string> selected)
    {
        int oldDepth = GUI.depth;
        GUI.depth = -26000;

        DrawBackdrop();

        float width =
            Mathf.Min(
                650f,
                Screen.width - 30f);

        float height =
            Mathf.Min(
                430f,
                Screen.height - 30f);

        Rect panel =
            new Rect(
                (Screen.width - width) *
                    0.5f,
                (Screen.height - height) *
                    0.5f,
                width,
                height);

        GUI.Box(panel, "");

        GUIStyle title =
            new GUIStyle(
                GUI.skin.label);

        title.fontSize = 21;
        title.fontStyle =
            FontStyle.Bold;
        title.alignment =
            TextAnchor.MiddleCenter;

        GUIStyle body =
            new GUIStyle(
                GUI.skin.label);

        body.wordWrap = true;

        GUI.Label(
            new Rect(
                panel.x + 18f,
                panel.y + 14f,
                panel.width - 36f,
                30f),
            titleText,
            title);

        GUI.Label(
            new Rect(
                panel.x + 28f,
                panel.y + 54f,
                panel.width - 56f,
                60f),
            description,
            body);

        float y =
            panel.y + 126f;

        foreach (string value
            in values)
        {
            if (GUI.Button(
                new Rect(
                    panel.x + 70f,
                    y,
                    panel.width - 140f,
                    38f),
                value))
            {
                selected(value);
            }

            y += 46f;
        }

        GUI.depth = oldDepth;
    }

    private void DrawUnitChoiceModal(
        string titleText,
        string description,
        IEnumerable<SquadController>
            units,
        Action<SquadController> selected)
    {
        int oldDepth = GUI.depth;
        GUI.depth = -26000;

        DrawBackdrop();

        List<SquadController> candidates =
            (units ??
                Enumerable.Empty<
                    SquadController>())
            .Where(
                unit => unit != null)
            .Distinct()
            .ToList();

        float width =
            Mathf.Min(
                720f,
                Screen.width - 30f);

        float height =
            Mathf.Min(
                650f,
                Screen.height - 30f);

        Rect panel =
            new Rect(
                (Screen.width - width) *
                    0.5f,
                (Screen.height - height) *
                    0.5f,
                width,
                height);

        GUI.Box(panel, "");

        GUIStyle title =
            new GUIStyle(
                GUI.skin.label);

        title.fontSize = 21;
        title.fontStyle =
            FontStyle.Bold;
        title.alignment =
            TextAnchor.MiddleCenter;

        GUIStyle body =
            new GUIStyle(
                GUI.skin.label);

        body.wordWrap = true;

        GUI.Label(
            new Rect(
                panel.x + 20f,
                panel.y + 14f,
                panel.width - 40f,
                30f),
            titleText,
            title);

        GUI.Label(
            new Rect(
                panel.x + 28f,
                panel.y + 52f,
                panel.width - 56f,
                68f),
            description,
            body);

        float y =
            panel.y + 128f;

        foreach (SquadController unit
            in candidates)
        {
            string suffix =
                unit.IsInReserves
                ? " [RESERVES]"
                : unit.IsEmbarked
                    ? " [EMBARKED]"
                    : "";

            if (GUI.Button(
                new Rect(
                    panel.x + 50f,
                    y,
                    panel.width - 100f,
                    34f),
                unit.DisplayName +
                suffix))
            {
                selected(unit);
            }

            y += 40f;

            if (y >
                panel.yMax - 48f)
            {
                break;
            }
        }

        GUI.depth = oldDepth;
    }

    private void DrawObjectiveChoiceModal(
        StandardFactionGameController
            controller)
    {
        int oldDepth = GUI.depth;
        GUI.depth = -26000;

        DrawBackdrop();

        GameController game =
            GameController.Current;

        float width =
            Mathf.Min(
                650f,
                Screen.width - 30f);

        float height =
            Mathf.Min(
                500f,
                Screen.height - 30f);

        Rect panel =
            new Rect(
                (Screen.width - width) *
                    0.5f,
                (Screen.height - height) *
                    0.5f,
                width,
                height);

        GUI.Box(panel, "");

        GUIStyle title =
            new GUIStyle(
                GUI.skin.label);

        title.fontSize = 21;
        title.fontStyle =
            FontStyle.Bold;
        title.alignment =
            TextAnchor.MiddleCenter;

        GUI.Label(
            new Rect(
                panel.x + 20f,
                panel.y + 14f,
                panel.width - 40f,
                30f),
            "HERE BE LOOT",
            title);

        GUI.Label(
            new Rect(
                panel.x + 28f,
                panel.y + 52f,
                panel.width - 56f,
                52f),
            "Select one objective marker to be the Freebooter Krew Loot objective until your next Command phase.");

        float y =
            panel.y + 116f;

        int index = 1;

        foreach (ObjectiveController objective
            in game.StandardObjectives)
        {
            if (objective == null)
                continue;

            if (GUI.Button(
                new Rect(
                    panel.x + 70f,
                    y,
                    panel.width - 140f,
                    34f),
                "OBJECTIVE " +
                index +
                " - " +
                objective.name))
            {
                controller
                    .SetLootObjective(
                        objective);
            }

            y += 40f;
            index++;
        }

        GUI.depth = oldDepth;
    }

    private void DrawOutOfTurnArmyRuleActions(
        List<StandardFactionGameController>
            controllers)
    {
        GameController game =
            GameController.Current;

        if (game == null ||
            game.BattleRound <= 0 ||
            game.CurrentPhase !=
                GameController.Phase.Command)
        {
            return;
        }

        StandardFactionGameController
            tyranids =
                controllers
                    .FirstOrDefault(
                        value =>
                            value != null &&
                            value.PackId ==
                                "tyranids" &&
                            value
                                .CanUseShadowInTheWarp
                    );

        if (tyranids == null)
            return;

        Rect button =
            new Rect(
                12f,
                Screen.height - 78f,
                196f,
                30f
            );

        if (GUI.Button(
                button,
                "SHADOW IN THE WARP"))
        {
            BeginShadowInTheWarp(
                tyranids
            );
        }

        if (tyranids.HasDetachment(
                "SYNAPTIC NEXUS") &&
            string.IsNullOrWhiteSpace(
                tyranids.SynapticImperative) &&
            tyranids.SynapticImperativesUsed.Count <
                3)
        {
            if (GUI.Button(
                new Rect(
                    12f,
                    Screen.height - 112f,
                    196f,
                    30f
                ),
                "TYRANID OPTIONS"))
            {
                showRules = true;
                rulesFaction =
                    tyranids.FactionId;
                ruleScroll =
                    Vector2.zero;
            }
        }
    }

    private void DrawFactionRulesButton(
        List<StandardFactionGameController>
            controllers)
    {
        GameController game =
            GameController.Current;

        if (game == null ||
            game.BattleRound <= 0 ||
            controllers.Count == 0)
        {
            return;
        }

        StandardFactionGameController
            active =
                controllers
                    .FirstOrDefault(
                        value =>
                            string.Equals(
                                value.FactionId,
                                game.ActiveFactionId,
                                StringComparison.OrdinalIgnoreCase))
                ?? controllers[0];

        Rect button =
            new Rect(
                Screen.width - 154f,
                Screen.height - 42f,
                142f,
                30f);

        if (GUI.Button(
            button,
            showRules
            ? "CLOSE FACTION RULES"
            : "FACTION RULES"))
        {
            if (showRules &&
                string.Equals(
                    rulesFaction,
                    active.FactionId,
                    StringComparison.OrdinalIgnoreCase))
            {
                showRules = false;
            }
            else
            {
                showRules = true;
                rulesFaction =
                    active.FactionId;
                ruleScroll =
                    Vector2.zero;
            }
        }
    }

    private void DrawRulesPanel(
        StandardFactionGameController
            controller)
    {
        GameController game =
            GameController.Current;

        if (game == null ||
            controller == null ||
            controller.Pack == null)
        {
            return;
        }

        float width =
            Mathf.Min(
                760f,
                Screen.width - 36f);

        float height =
            Mathf.Min(
                760f,
                Screen.height - 100f);

        Rect panel =
            new Rect(
                Screen.width -
                    width -
                    18f,
                86f,
                width,
                height);

        GUI.Box(panel, "");

        GUIStyle title =
            new GUIStyle(
                GUI.skin.label);

        title.fontSize = 20;
        title.fontStyle =
            FontStyle.Bold;

        GUIStyle heading =
            new GUIStyle(
                GUI.skin.label);

        heading.fontSize = 15;
        heading.fontStyle =
            FontStyle.Bold;

        GUIStyle wrap =
            new GUIStyle(
                GUI.skin.label);

        wrap.wordWrap = true;

        GUI.Label(
            new Rect(
                panel.x + 16f,
                panel.y + 10f,
                panel.width - 120f,
                28f),
            controller.DisplayName +
            " - FACTION RULES",
            title);

        if (GUI.Button(
            new Rect(
                panel.x +
                    panel.width -
                    52f,
                panel.y + 8f,
                36f,
                30f),
            "X"))
        {
            showRules = false;
            return;
        }

        DrawArmyRuleActions(
            controller,
            panel);

        Rect scrollOuter =
            new Rect(
                panel.x + 14f,
                panel.y + 106f,
                panel.width - 28f,
                panel.height - 122f);

        float contentHeight =
            RulesContentHeight(
                controller);

        Rect scrollInner =
            new Rect(
                0f,
                0f,
                scrollOuter.width - 20f,
                contentHeight);

        ruleScroll =
            GUI.BeginScrollView(
                scrollOuter,
                ruleScroll,
                scrollInner);

        float y = 4f;

        GUI.Label(
            new Rect(
                4f,
                y,
                scrollInner.width - 8f,
                24f),
            controller.Pack.armyRuleName,
            heading);

        y += 26f;

        y += DrawWrappedText(
            controller.Pack.armyRuleText,
            new Rect(
                4f,
                y,
                scrollInner.width - 8f,
                10f),
            wrap);

        y += 16f;

        foreach (string name
            in controller.SelectedDetachments)
        {
            StandardFactionDetachment11
                detachment =
                    controller
                        .GetDetachment(
                            name);

            if (detachment == null)
                continue;

            GUI.Label(
                new Rect(
                    4f,
                    y,
                    scrollInner.width - 8f,
                    24f),
                detachment.name +
                " - " +
                detachment.dp +
                "DP - " +
                detachment.ruleName,
                heading);

            y += 26f;

            y += DrawWrappedText(
                detachment.ruleText,
                new Rect(
                    4f,
                    y,
                    scrollInner.width - 8f,
                    10f),
                wrap);

            y += 14f;

            if (detachment.enhancements !=
                    null &&
                detachment.enhancements.Length >
                    0)
            {
                GUI.Label(
                    new Rect(
                        14f,
                        y,
                        scrollInner.width - 24f,
                        22f),
                    "ENHANCEMENTS",
                    heading);

                y += 24f;

                foreach (
                    StandardFactionEnhancement11
                        enhancement
                    in detachment.enhancements)
                {
                    if (enhancement == null)
                        continue;

                    GUI.Label(
                        new Rect(
                            18f,
                            y,
                            scrollInner.width - 28f,
                            20f),
                        enhancement.name +
                        " - " +
                        enhancement.points +
                        " PTS");

                    y += 21f;

                    y += DrawWrappedText(
                        enhancement.rule,
                        new Rect(
                            24f,
                            y,
                            scrollInner.width - 34f,
                            10f),
                        wrap);

                    y += 8f;
                }
            }

            if (detachment.stratagems !=
                    null &&
                detachment.stratagems.Length >
                    0)
            {
                GUI.Label(
                    new Rect(
                        14f,
                        y,
                        scrollInner.width - 24f,
                        22f),
                    "STRATAGEMS",
                    heading);

                y += 24f;

                foreach (
                    StandardFactionStratagem11
                        stratagem
                    in detachment.stratagems)
                {
                    if (stratagem == null)
                        continue;

                    GUI.Label(
                        new Rect(
                            18f,
                            y,
                            scrollInner.width - 160f,
                            22f),
                        stratagem.name +
                        " - " +
                        stratagem.cost +
                        "CP",
                        heading);

                    if (GUI.Button(
                        new Rect(
                            scrollInner.width -
                                128f,
                            y,
                            118f,
                            24f),
                        "SPEND + LOG"))
                    {
                        SpendAndLogStratagem(
                            controller,
                            stratagem);
                    }

                    y += 25f;

                    y += DrawWrappedText(
                        stratagem.FullRule,
                        new Rect(
                            24f,
                            y,
                            scrollInner.width - 34f,
                            10f),
                        wrap);

                    y += 12f;
                }
            }

            y += 18f;
        }

        GUI.EndScrollView();
    }

    private void DrawArmyRuleActions(
        StandardFactionGameController
            controller,
        Rect panel)
    {
        GameController game =
            GameController.Current;

        if (game == null)
            return;

        float x =
            panel.x + 16f;

        float y =
            panel.y + 48f;

        if (controller.PackId == "orks")
        {
            GUI.enabled =
                game.CurrentPhase ==
                    GameController.Phase.Command &&
                string.Equals(
                    game.ActiveFactionId,
                    controller.FactionId,
                    StringComparison.OrdinalIgnoreCase) &&
                !controller.WaaaghActive;

            if (GUI.Button(
                new Rect(
                    x,
                    y,
                    130f,
                    34f),
                controller.WaaaghActive
                ? "WAAAGH! ACTIVE"
                : "CALL WAAAGH!"))
            {
                controller.ActivateWaaagh(
                    false);
            }

            x += 138f;

            if (controller.HasDetachment(
                    "BULLY BOYZ") &&
                controller.WaaaghUsed)
            {
                if (GUI.Button(
                    new Rect(
                        x,
                        y,
                        178f,
                        34f),
                    "SECOND BULLY WAAAGH!"))
                {
                    controller
                        .ActivateWaaagh(
                            true);
                }

                x += 186f;
            }

            GUI.enabled = true;

            if (controller.PreyTarget != null)
            {
                GUI.Label(
                    new Rect(
                        x,
                        y + 7f,
                        panel.xMax -
                            x -
                            12f,
                        24f),
                    "PREY: " +
                    controller
                        .PreyTarget
                        .DisplayName);
            }
        }
        else if (controller.PackId ==
                 "tyranids")
        {
            GUI.enabled =
                game.CurrentPhase ==
                    GameController.Phase.Command &&
                controller.CanUseShadowInTheWarp;

            if (GUI.Button(
                new Rect(
                    x,
                    y,
                    180f,
                    34f),
                "SHADOW IN THE WARP"))
            {
                BeginShadowInTheWarp(
                    controller);
            }

            GUI.enabled = true;
            x += 188f;

            if (controller.HasDetachment(
                    "SYNAPTIC NEXUS"))
            {
                string[] imperatives =
                {
                    "SYNAPTIC AUGMENTATION",
                    "SURGING VITALITY",
                    "GOADED TO SLAUGHTER"
                };

                foreach (string imperative
                    in imperatives)
                {
                    bool used =
                        controller
                            .SynapticImperativesUsed
                            .Contains(
                                imperative);

                    GUI.enabled = !used;

                    if (GUI.Button(
                        new Rect(
                            x,
                            y,
                            140f,
                            34f),
                        ShortImperative(
                            imperative)))
                    {
                        controller
                            .SelectSynapticImperative(
                                imperative);
                    }

                    x += 146f;
                }

                GUI.enabled = true;
            }
        }
        else if (controller.PackId ==
                 "space_marines")
        {
            if (controller.HasDetachment(
                    "GLADIUS TASK FORCE"))
            {
                string[] doctrines =
                {
                    "DEVASTATOR DOCTRINE",
                    "TACTICAL DOCTRINE",
                    "ASSAULT DOCTRINE"
                };

                foreach (string doctrine
                    in doctrines)
                {
                    bool used =
                        controller
                            .CombatDoctrinesUsed
                            .Contains(
                                doctrine);

                    GUI.enabled =
                        !used &&
                        game.CurrentPhase ==
                            GameController.Phase.Command &&
                        string.Equals(
                            game.ActiveFactionId,
                            controller.FactionId,
                            StringComparison.OrdinalIgnoreCase);

                    if (GUI.Button(
                        new Rect(
                            x,
                            y,
                            142f,
                            34f),
                        doctrine.Replace(
                            " DOCTRINE",
                            "")))
                    {
                        controller
                            .SelectCombatDoctrine(
                                doctrine);
                    }

                    x += 148f;
                }

                GUI.enabled = true;
            }

            if (controller.HasDetachment(
                    "1ST COMPANY TASK FORCE") &&
                !controller.ExtremisUsed)
            {
                GUI.enabled =
                    game.CurrentPhase ==
                        GameController.Phase.Command &&
                    string.Equals(
                        game.ActiveFactionId,
                        controller.FactionId,
                        StringComparison.OrdinalIgnoreCase);

                if (GUI.Button(
                    new Rect(
                        x,
                        y,
                        164f,
                        34f),
                    "EXTREMIS THREAT"))
                {
                    controller
                        .ActivateExtremisLevelThreat();
                }

                GUI.enabled = true;
                x += 172f;
            }

            if (controller.OathTarget != null)
            {
                GUI.Label(
                    new Rect(
                        x,
                        y + 7f,
                        panel.xMax -
                            x -
                            12f,
                        24f),
                    "OATH: " +
                    controller
                        .OathTarget
                        .DisplayName);
            }
        }
    }

    private string ShortImperative(
        string value)
    {
        if (value.StartsWith(
                "SYNAPTIC",
                StringComparison.OrdinalIgnoreCase))
        {
            return "AUGMENTATION";
        }

        if (value.StartsWith(
                "SURGING",
                StringComparison.OrdinalIgnoreCase))
        {
            return "SURGING";
        }

        return "GOADED";
    }

    private void BeginShadowInTheWarp(
        StandardFactionGameController
            controller)
    {
        GameController game =
            GameController.Current;

        if (game == null ||
            controller == null ||
            !controller.CanUseShadowInTheWarp)
        {
            return;
        }

        controller
            .MarkShadowInTheWarpUsed();

        Queue<SquadController> enemies =
            new Queue<SquadController>(
                game.StandardEnemyUnits(
                        controller.FactionId)
                    .Where(
                        unit =>
                            unit != null &&
                            unit.IsAlive &&
                            unit.IsOnBattlefield)
                    .ToList()
            );

        Action next = null;

        next =
            () =>
            {
                if (enemies.Count == 0)
                {
                    game.StandardSetStatus(
                        "Shadow in the Warp resolved.");

                    game.StandardLog(
                        "TYRANIDS",
                        "Shadow in the Warp",
                        "All enemy units on the battlefield resolved their Battle-shock tests. Insane Bravery was not offered, matching the source FAQ."
                    );

                    return;
                }

                SquadController enemy =
                    enemies.Dequeue();

                int modifier =
                    EnemyWithinFriendlySynapse(
                        game,
                        controller.FactionId,
                        enemy)
                    ? -1
                    : 0;

                game.StandardResolveBattleShock(
                    enemy,
                    modifier,
                    WarboardFactionExtensionHub
                        .BattleShockDice(
                            game,
                            enemy),
                    "SHADOW IN THE WARP",
                    next
                );
            };

        next();
    }

    private bool EnemyWithinFriendlySynapse(
        GameController game,
        string tyranidFaction,
        SquadController enemy)
    {
        if (game == null ||
            enemy == null)
        {
            return false;
        }

        return game.AllSquads
            .Where(
                unit =>
                    unit != null &&
                    unit.IsAlive &&
                    unit.IsOnBattlefield &&
                    !unit.IsAttachedLeader &&
                    string.Equals(
                        unit.FactionId,
                        tyranidFaction,
                        StringComparison.OrdinalIgnoreCase) &&
                    unit.HasKeyword(
                        "SYNAPSE"))
            .Any(
                synapse =>
                    game.StandardDistance(
                        synapse,
                        enemy) <=
                    6.001f);
    }

    private void SpendAndLogStratagem(
        StandardFactionGameController
            controller,
        StandardFactionStratagem11
            stratagem)
    {
        GameController game =
            GameController.Current;

        if (game == null ||
            controller == null ||
            stratagem == null)
        {
            return;
        }

        SquadController selected =
            game.StandardSelectedSquad;

        bool spent;

        if (selected != null &&
            string.Equals(
                selected.FactionId,
                controller.FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            spent =
                game.SpendStratagemCPForUnit(
                    selected,
                    stratagem.cost,
                    stratagem.name);
        }
        else
        {
            spent =
                game.TrySpendCommandPoints(
                    controller.FactionId,
                    stratagem.cost);
        }

        if (!spent)
        {
            game.StandardSetStatus(
                stratagem.name +
                ": insufficient CP or the selected unit cannot currently be targeted by another Stratagem.");
            return;
        }

        game.StandardLog(
            "STRATAGEM",
            stratagem.name,
            stratagem.FullRule +
            "\nSource page " +
            stratagem.sourcePage +
            ". CP spent. Any target/model/placement choice not represented by a deterministic v46 hook remains player-resolved."
        );

        game.StandardSetStatus(
            stratagem.name +
            " - " +
            stratagem.cost +
            "CP spent. Resolve the displayed source rule; deterministic v46 hooks apply automatically where supported.");
    }

    private float DrawWrappedText(
        string text,
        Rect rect,
        GUIStyle style)
    {
        text = text ?? "";

        GUIContent content =
            new GUIContent(text);

        float height =
            style.CalcHeight(
                content,
                rect.width);

        rect.height =
            Mathf.Max(
                20f,
                height);

        GUI.Label(
            rect,
            text,
            style);

        return rect.height;
    }

    private float RulesContentHeight(
        StandardFactionGameController
            controller)
    {
        int chars =
            (controller.Pack.armyRuleText ??
             "").Length;

        int items = 1;

        foreach (string name
            in controller.SelectedDetachments)
        {
            StandardFactionDetachment11
                detachment =
                    controller
                        .GetDetachment(
                            name);

            if (detachment == null)
                continue;

            chars +=
                (detachment.ruleText ??
                 "").Length;

            if (detachment.enhancements !=
                    null)
            {
                items +=
                    detachment
                        .enhancements
                        .Length;

                chars +=
                    detachment
                        .enhancements
                        .Where(
                            value =>
                                value != null)
                        .Sum(
                            value =>
                                (value.rule ??
                                 "").Length);
            }

            if (detachment.stratagems !=
                    null)
            {
                items +=
                    detachment
                        .stratagems
                        .Length;

                chars +=
                    detachment
                        .stratagems
                        .Where(
                            value =>
                                value != null)
                        .Sum(
                            value =>
                                (value.FullRule ??
                                 "").Length);
            }
        }

        return Mathf.Max(
            900f,
            items * 78f +
            chars * 0.21f);
    }
}
