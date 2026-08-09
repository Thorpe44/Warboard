using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Necrons pre-game configuration UI. New Recruit text is the preferred
/// authority; manual multi-detachment selection remains available.
/// </summary>
[DefaultExecutionOrder(-31980)]
public sealed class NecronsSetupUI : MonoBehaviour
{
    private readonly Dictionary<string, HashSet<NecronDetachment>>
        selections =
            new Dictionary<string, HashSet<NecronDetachment>>(
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
            .FindAnyObjectByType<NecronsSetupUI>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "WarboardNecronsSetupUI");

        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<NecronsSetupUI>();
    }

    private void OnGUI()
    {
        FactionControllerHost host =
            FactionControllerHost.Instance;

        if (host == null)
            return;

        List<NecronGameController> controllers =
            host.Controllers.Values
                .OfType<NecronGameController>()
                .OrderBy(value => value.FactionId)
                .ToList();

        foreach (NecronGameController controller
            in controllers)
        {
            if (controller != null &&
                controller.ShouldShowDetachmentSelection())
            {
                DrawDetachmentModal(controller);
                return;
            }
        }

        DrawLockedBadges(controllers);
    }

    private HashSet<NecronDetachment> SelectionFor(
        NecronGameController controller)
    {
        HashSet<NecronDetachment> selected;

        if (!selections.TryGetValue(
                controller.FactionId,
                out selected) ||
            selected == null)
        {
            selected =
                new HashSet<NecronDetachment>();

            selections[controller.FactionId] =
                selected;
        }

        return selected;
    }

    private string PasteTextFor(
        NecronGameController controller)
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
        NecronGameController controller)
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
                800f,
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
            " — NECRONS ROSTER CONFIGURATION",
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
                    300f,
                    panel.height - 320f));

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

        HashSet<NecronDetachment> selected =
            SelectionFor(controller);

        float rowY = y + 34f;

        foreach (NecronDetachment detachment
            in controller.AvailableDetachments())
        {
            bool was = selected.Contains(detachment);

            string tag =
                NecronDetachmentRuntime.IsDynasty(
                    detachment)
                ? " [DYNASTY]"
                : NecronDetachmentRuntime.IsHypercrypt(
                    detachment)
                    ? " [HYPERCRYPT]"
                    : "";

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
                tag);

            if (now != was)
            {
                if (now)
                    selected.Add(detachment);
                else
                    selected.Remove(detachment);
            }

            rowY += 30f;
        }

        int spent =
            NecronDetachmentRuntime.TotalCost(
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
                rowY + 6f,
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
                rowY + 34f,
                rightWidth,
                58f),
            valid
                ? "Valid selection. DYNASTY and HYPERCRYPT tag conflicts are enforced."
                : validation,
            body);

        GUI.enabled = valid;

        if (GUI.Button(
                new Rect(
                    rightX,
                    panel.yMax - 62f,
                    Mathf.Min(
                        330f,
                        rightWidth),
                    40f),
                "CONFIRM NECRON DETACHMENTS"))
        {
            if (controller.TryLockDetachments(
                    selected,
                    "Manual pre-game selection"))
            {
                pasteStatus[controller.FactionId] =
                    "Necrons detachment configuration locked.";
            }
        }

        GUI.enabled = true;

        if (!string.IsNullOrWhiteSpace(
                controller.SelectionError))
        {
            GUI.Label(
                new Rect(
                    rightX,
                    panel.yMax - 110f,
                    rightWidth,
                    42f),
                controller.SelectionError,
                body);
        }

        GUI.depth = oldDepth;
    }

    private void ApplyPastedRoster(
        NecronGameController controller,
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
    }

    private void DrawLockedBadges(
        List<NecronGameController> controllers)
    {
        if (controllers == null ||
            controllers.Count == 0)
        {
            return;
        }

        int occupiedRows = 0;

        FactionControllerHost host =
            FactionControllerHost.Instance;

        if (host != null)
        {
            occupiedRows +=
                host.Controllers.Values
                    .OfType<AeldariGameController>()
                    .Count(
                        value =>
                            value != null &&
                            value.DetachmentLocked);

            occupiedRows +=
                host.Controllers.Values
                    .OfType<CustodesGameController>()
                    .Count(
                        value =>
                            value != null &&
                            value.DetachmentLocked);
        }

        int necronRow = 0;

        foreach (NecronGameController controller
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
                " • NECRONS • " +
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
                    (occupiedRows + necronRow) *
                    36f,
                    width - 74f,
                    30f);

            GUI.Box(
                badge,
                text);

            necronRow++;
        }
    }
}
