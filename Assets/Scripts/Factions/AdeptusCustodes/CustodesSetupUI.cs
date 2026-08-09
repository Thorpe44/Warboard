using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Adeptus Custodes pre-game configuration UI. New Recruit text is the
/// preferred authority; manual multi-detachment selection remains available.
/// </summary>
[DefaultExecutionOrder(-31990)]
public sealed class CustodesSetupUI : MonoBehaviour
{
    private readonly Dictionary<string, HashSet<CustodesDetachment>>
        selections =
            new Dictionary<string, HashSet<CustodesDetachment>>(
                StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string>
        pastedRosterText =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string>
        pasteStatus =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object
            .FindAnyObjectByType<CustodesSetupUI>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "WarboardCustodesSetupUI");

        UnityEngine.Object
            .DontDestroyOnLoad(go);

        go.AddComponent<CustodesSetupUI>();
    }

    private void OnGUI()
    {
        FactionControllerHost host =
            FactionControllerHost.Instance;

        if (host == null)
            return;

        List<CustodesGameController> controllers =
            host.Controllers.Values
                .OfType<CustodesGameController>()
                .OrderBy(value => value.FactionId)
                .ToList();

        foreach (CustodesGameController controller
            in controllers)
        {
            if (controller == null)
                continue;

            if (controller.ShouldShowDetachmentSelection())
            {
                DrawDetachmentModal(controller);
                return;
            }

            if (controller.RequiresSolarWalkerChoice)
            {
                DrawSolarWalkerModal(controller);
                return;
            }
        }

        DrawLockedBadges(controllers);
    }

    private HashSet<CustodesDetachment> SelectionFor(
        CustodesGameController controller)
    {
        HashSet<CustodesDetachment> selected;

        if (!selections.TryGetValue(
                controller.FactionId,
                out selected) ||
            selected == null)
        {
            selected =
                new HashSet<CustodesDetachment>();

            selections[controller.FactionId] =
                selected;
        }

        return selected;
    }

    private string PasteTextFor(
        CustodesGameController controller)
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

        pastedRosterText[controller.FactionId] =
            value;

        return value;
    }

    private void DrawDetachmentModal(
        CustodesGameController controller)
    {
        int oldDepth = GUI.depth;
        GUI.depth = -20000;

        Color oldColor = GUI.color;
        GUI.color =
            new Color(0f, 0f, 0f, 0.87f);

        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height),
            Texture2D.whiteTexture);

        GUI.color = oldColor;

        float width =
            Mathf.Min(
                1100f,
                Screen.width - 30f);

        float height =
            Mathf.Min(
                780f,
                Screen.height - 30f);

        Rect panel =
            new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

        GUI.Box(panel, "");

        GUIStyle title =
            new GUIStyle(GUI.skin.label);

        title.fontSize = 22;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;

        GUI.Label(
            new Rect(
                panel.x + 20f,
                panel.y + 12f,
                panel.width - 40f,
                34f),
            controller.FactionId +
            " — ADEPTUS CUSTODES ROSTER CONFIGURATION",
            title);

        GUIStyle body =
            new GUIStyle(GUI.skin.label);

        body.wordWrap = true;

        GUI.Label(
            new Rect(
                panel.x + 28f,
                panel.y + 50f,
                panel.width - 56f,
                44f),
            "Paste the New Recruit roster text to lock the exact detachment configuration automatically. YellowScribe still supplies datasheet and weapon profiles. You can also select detachments manually.",
            body);

        float leftX = panel.x + 28f;
        float y = panel.y + 104f;
        float gap = 18f;
        float leftWidth =
            Mathf.Min(
                430f,
                panel.width * 0.42f);
        float rightX =
            leftX + leftWidth + gap;
        float rightWidth =
            panel.x + panel.width - 28f - rightX;

        GUIStyle section =
            new GUIStyle(GUI.skin.label);

        section.fontStyle = FontStyle.Bold;
        section.fontSize = 15;

        GUI.Label(
            new Rect(
                leftX,
                y,
                leftWidth,
                24f),
            "NEW RECRUIT ROSTER TEXT",
            section);

        string text = PasteTextFor(controller);

        Rect textRect =
            new Rect(
                leftX,
                y + 28f,
                leftWidth,
                Mathf.Max(
                    280f,
                    panel.height - 310f));

        text = GUI.TextArea(
            textRect,
            text);

        pastedRosterText[controller.FactionId] =
            text;

        float buttonY = textRect.yMax + 8f;

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
                    120f,
                    34f),
                "CLEAR"))
        {
            RosterTextManifestStore.Clear(
                controller.FactionId);

            pastedRosterText[controller.FactionId] =
                "";

            pasteStatus[controller.FactionId] =
                "Roster manifest cleared.";

            controller.TryUnlockBeforeDeployment();
        }

        string status;
        if (!pasteStatus.TryGetValue(
                controller.FactionId,
                out status))
        {
            status = controller.RosterProbeStatus;
        }

        GUI.Label(
            new Rect(
                leftX,
                buttonY + 42f,
                leftWidth,
                72f),
            string.IsNullOrWhiteSpace(status)
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

        HashSet<CustodesDetachment> selected =
            SelectionFor(controller);

        float rowY = y + 34f;

        foreach (CustodesDetachment detachment
            in controller.AvailableDetachments())
        {
            bool was = selected.Contains(detachment);
            bool now = GUI.Toggle(
                new Rect(
                    rightX,
                    rowY,
                    rightWidth,
                    26f),
                was,
                controller.GetDetachmentDisplayName(
                    detachment) +
                " — " +
                controller.GetDetachmentPointCost(
                    detachment) +
                "DP" +
                (CustodesDetachmentRuntime.IsArmoury(
                    detachment)
                    ? " [ARMOURY]"
                    : CustodesDetachmentRuntime.IsLions(
                        detachment)
                        ? " [LIONS]"
                        : ""));

            if (now != was)
            {
                if (now)
                    selected.Add(detachment);
                else
                    selected.Remove(detachment);
            }

            rowY += 31f;
        }

        int spent =
            CustodesDetachmentRuntime.TotalCost(
                selected);

        int limit = controller.DetachmentPointLimit;

        string validation;
        bool valid =
            controller.TryValidateDetachmentSelection(
                selected,
                out validation);

        GUI.Label(
            new Rect(
                rightX,
                rowY + 8f,
                rightWidth,
                26f),
            "Selected: " +
            spent +
            (limit > 0
                ? "/" + limit
                : "") +
            " DP",
            section);

        GUI.Label(
            new Rect(
                rightX,
                rowY + 38f,
                rightWidth,
                64f),
            valid
                ? "Valid selection. ARMOURY and LIONS tag conflicts are enforced."
                : validation,
            body);

        GUI.enabled = valid;

        if (GUI.Button(
                new Rect(
                    rightX,
                    panel.yMax - 66f,
                    Mathf.Min(
                        330f,
                        rightWidth),
                    40f),
                "CONFIRM CUSTODES DETACHMENTS"))
        {
            if (controller.TryLockDetachments(
                    selected,
                    "Manual pre-game selection"))
            {
                pasteStatus[controller.FactionId] =
                    "Custodes detachment configuration locked.";
            }
        }

        GUI.enabled = true;

        if (!string.IsNullOrWhiteSpace(
                controller.SelectionError))
        {
            GUI.Label(
                new Rect(
                    rightX,
                    panel.yMax - 118f,
                    rightWidth,
                    46f),
                controller.SelectionError,
                body);
        }

        GUI.depth = oldDepth;
    }

    private void ApplyPastedRoster(
        CustodesGameController controller,
        string text)
    {
        WarboardRosterManifest manifest;
        string error;

        if (!RosterTextManifestStore.TrySet(
                controller.FactionId,
                text,
                out manifest,
                out error))
        {
            pasteStatus[controller.FactionId] =
                error;
            return;
        }

        pasteStatus[controller.FactionId] =
            "Parsed: " +
            manifest.Summary();

        // RefreshArmy is driven by the existing roster-changed notification;
        // for a paste-only config change, resolving in OnGUI on the next frame
        // also causes the controller to observe the new manifest revision.
    }

    private void DrawSolarWalkerModal(
        CustodesGameController controller)
    {
        int oldDepth = GUI.depth;
        GUI.depth = -20000;

        Color oldColor = GUI.color;
        GUI.color =
            new Color(0f, 0f, 0f, 0.87f);

        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height),
            Texture2D.whiteTexture);

        GUI.color = oldColor;

        float width =
            Mathf.Min(
                720f,
                Screen.width - 30f);

        float height =
            Mathf.Min(
                560f,
                Screen.height - 30f);

        Rect panel =
            new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

        GUI.Box(panel, "");

        GUIStyle title =
            new GUIStyle(GUI.skin.label);

        title.fontSize = 21;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;

        GUIStyle body =
            new GUIStyle(GUI.skin.label);

        body.wordWrap = true;

        GUI.Label(
            new Rect(
                panel.x + 18f,
                panel.y + 14f,
                panel.width - 36f,
                30f),
            "SOLAR SPEARHEAD — WALKER CHARACTERS",
            title);

        GUI.Label(
            new Rect(
                panel.x + 24f,
                panel.y + 54f,
                panel.width - 48f,
                62f),
            "Muster Armies: select up to two ADEPTUS CUSTODES WALKER models to gain CHARACTER. Selecting none is legal; confirm the choice before deployment.",
            body);

        List<SquadController> walkers =
            controller.EligibleSolarWalkers();

        float y = panel.y + 128f;

        foreach (SquadController walker in walkers)
        {
            bool selected =
                controller.IsSolarWalkerSelected(
                    walker);

            bool now = GUI.Toggle(
                new Rect(
                    panel.x + 34f,
                    y,
                    panel.width - 68f,
                    30f),
                selected,
                walker.DisplayName +
                (selected
                    ? " — CHARACTER"
                    : ""));

            if (now != selected)
                controller.ToggleSolarWalker(walker);

            y += 34f;
        }

        if (!string.IsNullOrWhiteSpace(
                controller.SelectionError))
        {
            GUI.Label(
                new Rect(
                    panel.x + 28f,
                    panel.yMax - 116f,
                    panel.width - 56f,
                    42f),
                controller.SelectionError,
                body);
        }

        if (GUI.Button(
                new Rect(
                    panel.x +
                        (panel.width - 310f) * 0.5f,
                    panel.yMax - 62f,
                    310f,
                    40f),
                "CONFIRM WALKER CHARACTER CHOICES"))
        {
            controller.ConfirmSolarWalkerSelection();
        }

        GUI.depth = oldDepth;
    }

    private void DrawLockedBadges(
        List<CustodesGameController> controllers)
    {
        if (controllers == null ||
            controllers.Count == 0)
        {
            return;
        }

        // Share the same top-right status area used by Aeldari instead of
        // drawing a faction badge in the middle of the battlefield.
        int occupiedRows = 0;

        FactionControllerHost host =
            FactionControllerHost.Instance;

        if (host != null)
        {
            occupiedRows =
                host.Controllers.Values
                    .OfType<AeldariGameController>()
                    .Count(
                        value =>
                            value != null &&
                            value.DetachmentLocked
                    );
        }

        int custodesRow = 0;

        foreach (CustodesGameController controller
            in controllers)
        {
            if (controller == null ||
                !controller.DetachmentLocked)
            {
                continue;
            }

            int spent =
                controller.DetachmentPointsSpent;

            int limit =
                controller.DetachmentPointLimit;

            string text =
                controller.FactionId +
                " • ADEPTUS CUSTODES • " +
                controller.DetachmentName +
                " • " +
                spent +
                (limit > 0
                    ? "/" + limit
                    : "") +
                "DP" +
                (!string.IsNullOrWhiteSpace(
                    controller.ForceDisposition)
                    ? " • " +
                      controller.ForceDisposition
                    : "") +
                " • LOCKED";

            float width =
                Mathf.Min(
                    760f,
                    Screen.width - 24f);

            Rect badge =
                new Rect(
                    Screen.width - width - 12f,
                    48f +
                    (occupiedRows + custodesRow) *
                    36f,
                    width - 74f,
                    30f);

            GUI.Box(
                badge,
                text);

            custodesRow++;
        }
    }
}
