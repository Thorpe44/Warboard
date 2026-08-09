using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// v38 Aeldari pre-game roster/configuration UI.
///
/// Preferred path:
///   paste New Recruit roster text -> parse configuration -> lock every
///   selected detachment and its DP cost automatically.
///
/// YellowScribe remains the datasheet/profile source. If no pasted manifest
/// is available, explicit YellowScribe detachment metadata can still be used,
/// otherwise the player can select the roster detachments manually once.
/// </summary>
[DefaultExecutionOrder(-32000)]
public sealed class AeldariSetupUI : MonoBehaviour
{
    private readonly Dictionary<
        string,
        HashSet<AeldariDetachment>
    > selections =
        new Dictionary<
            string,
            HashSet<AeldariDetachment>>(
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
            .FindAnyObjectByType<
                AeldariSetupUI>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "WarboardAeldariSetupUI");

        UnityEngine.Object
            .DontDestroyOnLoad(go);

        go.AddComponent<
            AeldariSetupUI>();
    }

    private void OnGUI()
    {
        FactionControllerHost host =
            FactionControllerHost.Instance;

        if (host == null)
            return;

        List<AeldariGameController> aeldari =
            host.Controllers
                .Values
                .OfType<AeldariGameController>()
                .OrderBy(
                    controller =>
                        controller.FactionId)
                .ToList();

        foreach (AeldariGameController controller
            in aeldari)
        {
            if (controller == null)
                continue;

            if (controller
                .ShouldShowDetachmentSelection())
            {
                DrawSelectionModal(controller);
                return;
            }
        }

        DrawLockedDetachmentBadges(aeldari);
    }

    private HashSet<AeldariDetachment>
        SelectionFor(
            AeldariGameController controller)
    {
        HashSet<AeldariDetachment> selected;

        if (!selections.TryGetValue(
                controller.FactionId,
                out selected) ||
            selected == null)
        {
            selected =
                new HashSet<AeldariDetachment>();

            selections[controller.FactionId] =
                selected;
        }

        return selected;
    }

    private string PasteTextFor(
        AeldariGameController controller)
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

    private void DrawSelectionModal(
        AeldariGameController controller)
    {
        int previousDepth = GUI.depth;
        GUI.depth = -20000;

        Color previousColor = GUI.color;
        GUI.color =
            new Color(0f, 0f, 0f, 0.86f);

        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height),
            Texture2D.whiteTexture);

        GUI.color = previousColor;

        float width =
            Mathf.Min(
                1100f,
                Screen.width - 30f);

        float height =
            Mathf.Min(
                790f,
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
        title.alignment =
            TextAnchor.MiddleCenter;

        GUI.Label(
            new Rect(
                panel.x + 20f,
                panel.y + 12f,
                panel.width - 40f,
                34f),
            controller.FactionId +
            " — AELDARI ROSTER CONFIGURATION",
            title);

        GUIStyle body =
            new GUIStyle(GUI.skin.label);

        body.wordWrap = true;
        body.alignment =
            TextAnchor.UpperLeft;

        GUI.Label(
            new Rect(
                panel.x + 28f,
                panel.y + 50f,
                panel.width - 56f,
                42f),
            "Paste the New Recruit text export to make roster configuration authoritative. YellowScribe still supplies the datasheet and weapon profiles. You can also select the detachments manually below.",
            body);

        float leftX = panel.x + 28f;
        float contentY = panel.y + 102f;
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
                contentY,
                leftWidth,
                24f),
            "NEW RECRUIT ROSTER TEXT",
            section);

        string text = PasteTextFor(controller);

        Rect textRect =
            new Rect(
                leftX,
                contentY + 28f,
                leftWidth,
                Mathf.Max(
                    280f,
                    panel.height - 300f));

        text = GUI.TextArea(
            textRect,
            text);

        pastedRosterText[controller.FactionId] =
            text;

        float pasteButtonY =
            textRect.yMax + 8f;

        if (GUI.Button(
                new Rect(
                    leftX,
                    pasteButtonY,
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
                    pasteButtonY,
                    leftWidth - 215f,
                    34f),
                "CLEAR"))
        {
            pastedRosterText[
                controller.FactionId] = "";

            RosterTextManifestStore.Clear(
                controller.FactionId);

            controller.TryUnlockBeforeDeployment();

            GameController game =
                GameController.Current;

            if (game != null)
                game.NotifyRostersChanged();

            pasteStatus[
                controller.FactionId] =
                    "Pasted roster configuration cleared.";
        }

        string localPasteStatus;

        if (!pasteStatus.TryGetValue(
                controller.FactionId,
                out localPasteStatus))
        {
            localPasteStatus = "";
        }

        string rosterStatus =
            !string.IsNullOrWhiteSpace(
                localPasteStatus)
            ? localPasteStatus
            : controller.RosterProbeStatus;

        GUI.Label(
            new Rect(
                leftX,
                pasteButtonY + 42f,
                leftWidth,
                76f),
            rosterStatus ?? "",
            body);

        GUI.Label(
            new Rect(
                rightX,
                contentY,
                rightWidth,
                24f),
            "DETACHMENTS / DETACHMENT POINTS",
            section);

        HashSet<AeldariDetachment> selected =
            SelectionFor(controller);

        AeldariDetachment[] options =
            controller.AvailableDetachments();

        float optionTop = contentY + 30f;
        float optionGap = 6f;
        float optionWidth =
            (rightWidth - optionGap) * 0.5f;
        float optionHeight = 34f;

        for (int i = 0;
             i < options.Length;
             i++)
        {
            AeldariDetachment option = options[i];

            int column = i % 2;
            int row = i / 2;

            Rect button =
                new Rect(
                    rightX +
                        column *
                        (optionWidth + optionGap),
                    optionTop +
                        row *
                        (optionHeight + 6f),
                    optionWidth,
                    optionHeight);

            bool isSelected =
                selected.Contains(option);

            string label =
                (isSelected ? "✓  " : "") +
                controller
                    .GetDetachmentDisplayName(option) +
                " — " +
                controller
                    .GetDetachmentPointCost(option) +
                "DP" +
                (AeldariDetachmentRuntime
                    .IsAcrobatic(option)
                    ? " [ACROBATIC]"
                    : "");

            if (GUI.Button(button, label))
            {
                if (isSelected)
                    selected.Remove(option);
                else
                    selected.Add(option);
            }
        }

        float infoY =
            optionTop +
            8f *
                (optionHeight + 6f) +
            10f;

        List<AeldariDetachment> ordered =
            options
                .Where(selected.Contains)
                .ToList();

        int spent =
            AeldariDetachmentRuntime.TotalCost(
                ordered);

        int limit =
            controller.DetachmentPointLimit;

        string dpText =
            limit > 0
            ? spent + " / " + limit + " DP"
            : spent + " DP • battle-size DP limit not defined";

        GUIStyle dpStyle =
            new GUIStyle(GUI.skin.label);

        dpStyle.fontSize = 17;
        dpStyle.fontStyle = FontStyle.Bold;

        GUI.Label(
            new Rect(
                rightX,
                infoY,
                rightWidth,
                28f),
            dpText,
            dpStyle);

        string validation;
        bool valid =
            controller
                .TryValidateDetachmentSelection(
                    ordered,
                    out validation);

        if (valid)
        {
            validation =
                ordered.Count == 1
                ? "Selection is valid."
                : "Selection is valid. All selected detachment controllers will load together.";
        }

        GUI.Label(
            new Rect(
                rightX,
                infoY + 32f,
                rightWidth,
                62f),
            validation ?? "",
            body);

        if (!string.IsNullOrWhiteSpace(
                controller.SelectionError))
        {
            GUIStyle error =
                new GUIStyle(body);

            error.fontStyle = FontStyle.Bold;

            GUI.Label(
                new Rect(
                    rightX,
                    infoY + 92f,
                    rightWidth,
                    52f),
                controller.SelectionError,
                error);
        }

        Rect confirm =
            new Rect(
                panel.x + panel.width - 288f,
                panel.y + panel.height - 56f,
                260f,
                38f);

        GUI.enabled = valid;

        if (GUI.Button(
                confirm,
                "CONFIRM DETACHMENTS"))
        {
            controller.TryLockDetachments(
                ordered,
                "Pre-game manual detachment selection");
        }

        GUI.enabled = true;

        if (Event.current != null &&
            (Event.current.type ==
                 EventType.MouseDown ||
             Event.current.type ==
                 EventType.MouseUp))
        {
            Event.current.Use();
        }

        GUI.depth = previousDepth;
    }

    private void ApplyPastedRoster(
        AeldariGameController controller,
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
            pasteStatus[
                controller.FactionId] =
                    error;
            return;
        }

        pasteStatus[
            controller.FactionId] =
                "Roster parsed: " +
                manifest.Summary();

        HashSet<AeldariDetachment> selected =
            SelectionFor(controller);

        selected.Clear();

        foreach (string label
            in manifest.Detachments)
        {
            AeldariDetachment detachment;

            if (AeldariDetachmentRuntime.TryParse(
                    label,
                    out detachment))
            {
                selected.Add(detachment);
            }
        }

        GameController game =
            GameController.Current;

        if (game != null)
            game.NotifyRostersChanged();
    }

    private void DrawLockedDetachmentBadges(
        List<AeldariGameController> controllers)
    {
        int index = 0;

        foreach (AeldariGameController controller
            in controllers)
        {
            if (controller == null ||
                !controller.DetachmentLocked)
            {
                continue;
            }

            int previousDepth = GUI.depth;
            GUI.depth = -15000;

            int limit =
                controller.DetachmentPointLimit;

            string dp =
                controller.DetachmentPointsSpent +
                (limit > 0
                    ? "/" + limit
                    : "") +
                "DP";

            string disposition =
                string.IsNullOrWhiteSpace(
                    controller.ForceDisposition)
                ? ""
                : " • " +
                  controller.ForceDisposition;

            float width =
                Mathf.Min(
                    760f,
                    Screen.width - 24f);

            Rect badge =
                new Rect(
                    Screen.width - width - 12f,
                    48f + index * 36f,
                    width - 74f,
                    30f);

            GUI.Box(
                badge,
                controller.FactionId +
                " • AELDARI • " +
                controller.DetachmentName +
                " • " + dp +
                disposition +
                " • LOCKED");

            GameController game =
                GameController.Current;

            if (game != null &&
                !game.DeploymentStarted)
            {
                Rect edit =
                    new Rect(
                        badge.xMax + 6f,
                        badge.y,
                        68f,
                        badge.height);

                if (GUI.Button(edit, "EDIT"))
                {
                    List<AeldariDetachment> prior =
                        controller
                            .LockedDetachments
                            .ToList();

                    WarboardRosterManifest manifest =
                        controller.RosterManifest;

                    if (manifest != null)
                    {
                        pastedRosterText[
                            controller.FactionId] =
                                manifest.RawText ?? "";

                        RosterTextManifestStore.Clear(
                            controller.FactionId);
                    }

                    if (controller
                        .TryUnlockBeforeDeployment())
                    {
                        HashSet<AeldariDetachment> selected =
                            SelectionFor(controller);

                        selected.Clear();

                        foreach (AeldariDetachment detachment
                            in prior)
                        {
                            selected.Add(detachment);
                        }

                        game.NotifyRostersChanged();
                    }
                }
            }

            GUI.depth = previousDepth;
            index++;
        }
    }
}
