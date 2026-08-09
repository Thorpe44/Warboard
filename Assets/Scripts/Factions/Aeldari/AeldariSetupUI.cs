using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// One-time Aeldari pre-game detachment selection.
///
/// Normal path:
/// imported roster metadata -> Aeldari controller -> detachment auto-lock.
///
/// Fallback path:
/// if the imported roster does not expose one unambiguous detachment, the
/// player selects the roster's detachment once before deployment.
/// </summary>
[DefaultExecutionOrder(-32000)]
public sealed class AeldariSetupUI :
    MonoBehaviour
{
    private readonly Dictionary<
        string,
        AeldariDetachment
    > selections =
        new Dictionary<
            string,
            AeldariDetachment>(
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
                .OfType<
                    AeldariGameController>()
                .OrderBy(
                    controller =>
                        controller.FactionId)
                .ToList();

        foreach (
            AeldariGameController controller
            in aeldari)
        {
            if (controller == null)
                continue;

            if (controller
                .ShouldShowDetachmentSelection())
            {
                DrawSelectionModal(
                    controller);

                return;
            }
        }

        DrawLockedDetachmentBadges(
            aeldari);
    }

    private void DrawSelectionModal(
        AeldariGameController controller)
    {
        int previousDepth =
            GUI.depth;

        GUI.depth = -20000;

        Color previousColor =
            GUI.color;

        GUI.color =
            new Color(
                0f,
                0f,
                0f,
                0.84f);

        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height),
            Texture2D.whiteTexture);

        GUI.color =
            previousColor;

        float width =
            Mathf.Min(
                820f,
                Screen.width -
                    40f);

        float height =
            Mathf.Min(
                660f,
                Screen.height -
                    50f);

        Rect panel =
            new Rect(
                (Screen.width -
                 width) * 0.5f,
                (Screen.height -
                 height) * 0.5f,
                width,
                height);

        GUI.Box(
            panel,
            "");

        GUIStyle title =
            new GUIStyle(
                GUI.skin.label);

        title.fontSize = 22;
        title.fontStyle =
            FontStyle.Bold;
        title.alignment =
            TextAnchor.MiddleCenter;

        GUI.Label(
            new Rect(
                panel.x + 20f,
                panel.y + 14f,
                panel.width - 40f,
                34f),
            controller.FactionId +
            " — AELDARI DETACHMENT",
            title);

        GUIStyle body =
            new GUIStyle(
                GUI.skin.label);

        body.wordWrap = true;
        body.alignment =
            TextAnchor.UpperLeft;

        GUI.Label(
            new Rect(
                panel.x + 28f,
                panel.y + 55f,
                panel.width - 56f,
                56f),
            "Warboard could not read one unambiguous Aeldari detachment from the imported roster. Select the detachment shown on the roster once. Deployment is blocked until it is confirmed.",
            body);

        AeldariDetachment selected;

        if (!selections.TryGetValue(
                controller.FactionId,
                out selected))
        {
            selected =
                controller
                    .SuggestedDetachment;

            selections[
                controller.FactionId] =
                    selected;
        }

        AeldariDetachment[] options =
            controller
                .AvailableDetachments();

        float gridTop =
            panel.y + 122f;

        float gap = 8f;

        float buttonWidth =
            (panel.width -
             56f -
             gap) * 0.5f;

        float buttonHeight =
            34f;

        for (int i = 0;
             i < options.Length;
             i++)
        {
            int column =
                i % 2;

            int row =
                i / 2;

            Rect button =
                new Rect(
                    panel.x +
                        28f +
                        column *
                        (buttonWidth +
                         gap),
                    gridTop +
                        row *
                        (buttonHeight +
                         6f),
                    buttonWidth,
                    buttonHeight);

            bool isSelected =
                options[i] ==
                    selected;

            string label =
                (isSelected
                    ? "✓  "
                    : "") +
                controller
                    .GetDetachmentDisplayName(
                        options[i]);

            if (GUI.Button(
                    button,
                    label))
            {
                selections[
                    controller.FactionId] =
                        options[i];

                selected =
                    options[i];
            }
        }

        float infoY =
            gridTop +
            8f *
            (buttonHeight +
             6f) +
            8f;

        string sourceText =
            string.IsNullOrWhiteSpace(
                controller.RosterProbeStatus)
            ? "No roster detachment metadata was available."
            : controller.RosterProbeStatus;

        GUI.Label(
            new Rect(
                panel.x + 28f,
                infoY,
                panel.width - 56f,
                52f),
            sourceText,
            body);

        if (!string.IsNullOrWhiteSpace(
                controller.SelectionError))
        {
            GUIStyle error =
                new GUIStyle(body);

            error.fontStyle =
                FontStyle.Bold;

            GUI.Label(
                new Rect(
                    panel.x + 28f,
                    infoY + 48f,
                    panel.width - 56f,
                    46f),
                controller.SelectionError,
                error);
        }

        Rect confirm =
            new Rect(
                panel.x +
                    panel.width -
                    258f,
                panel.y +
                    panel.height -
                    58f,
                230f,
                38f);

        if (GUI.Button(
                confirm,
                "CONFIRM DETACHMENT"))
        {
            controller.TryLockDetachment(
                selected,
                "Pre-game detachment selection");
        }

        if (Event.current != null &&
            (Event.current.type ==
                 EventType.MouseDown ||
             Event.current.type ==
                 EventType.MouseUp))
        {
            Event.current.Use();
        }

        GUI.depth =
            previousDepth;
    }

    private void DrawLockedDetachmentBadges(
        List<AeldariGameController> controllers)
    {
        int index = 0;

        foreach (
            AeldariGameController controller
            in controllers)
        {
            if (controller == null ||
                !controller.DetachmentLocked)
            {
                continue;
            }

            int previousDepth =
                GUI.depth;

            GUI.depth = -15000;

            float width =
                440f;

            string source =
                controller.DetachmentLockSource
                    .IndexOf(
                        "YellowScribe",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                controller.DetachmentLockSource
                    .IndexOf(
                        "New Recruit",
                        StringComparison.OrdinalIgnoreCase) >= 0
                ? "ROSTER"
                : "CONFIRMED";

            Rect badge =
                new Rect(
                    Screen.width -
                        width -
                        12f,
                    48f +
                        index * 34f,
                    width,
                    28f);

            GUI.Box(
                badge,
                controller.FactionId +
                " • AELDARI • " +
                controller.DetachmentName +
                " • " +
                source +
                " LOCKED");

            GUI.depth =
                previousDepth;

            index++;
        }
    }
}
