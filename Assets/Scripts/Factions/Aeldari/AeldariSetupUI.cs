using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// One-time Aeldari pre-game detachment selector.
///
/// Normal path:
/// YellowScribe/New Recruit roster -> auto-detected detachment -> locked.
///
/// Fallback path:
/// if the roster JSON does not expose a single detachment value, the player
/// selects it once here before deployment. It cannot be cycled during play.
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
        if (UnityEngine.Object.FindAnyObjectByType<
                AeldariSetupUI>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "WarboardAeldariSetupUI");

        UnityEngine.Object.DontDestroyOnLoad(go);

        go.AddComponent<
            AeldariSetupUI>();
    }

    private void OnGUI()
    {
        FactionControllerHost host =
            UnityEngine.Object.FindAnyObjectByType<
                FactionControllerHost>();

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
                0.82f);

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
                640f,
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
            "AELDARI DETACHMENT",
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
                52f),
            "The imported roster did not expose one unambiguous detachment. Select the detachment recorded on the roster once. Warboard will lock it for the battle.",
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
            panel.y + 118f;

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
            ? ""
            : controller.RosterProbeStatus;

        GUI.Label(
            new Rect(
                panel.x + 28f,
                infoY,
                panel.width - 56f,
                42f),
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
                    infoY + 40f,
                    panel.width - 56f,
                    40f),
                controller.SelectionError,
                error);
        }

        Rect confirm =
            new Rect(
                panel.x +
                    panel.width -
                    248f,
                panel.y +
                    panel.height -
                    58f,
                220f,
                38f);

        if (GUI.Button(
                confirm,
                "CONFIRM & LOCK"))
        {
            controller.TryLockDetachment(
                selected,
                "Pre-game detachment selection");
        }

        // Consume mouse input before the legacy GameController OnGUI receives
        // it. This makes the selector genuinely modal instead of letting
        // clicks fall through onto the old setup controls.
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
                360f;

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
                "AELDARI • " +
                controller.DetachmentName +
                " • LOCKED");

            GUI.depth =
                previousDepth;

            index++;
        }
    }
}
