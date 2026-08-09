using System.Reflection;
using UnityEngine;

/// <summary>
/// Single visible build marker for Warboard.
///
/// IMPORTANT:
/// Every Warboard release must update CurrentVersion. Keeping the visible
/// build number in this small file means future patches do not need to replace
/// GameController just to prove which build Unity is running.
/// </summary>
public static class WarboardBuildInfo
{
    public const string CurrentVersion = "v33";
}

public sealed class WarboardBuildHeader :
    MonoBehaviour
{
    private GameController game;

    private static readonly Color HeaderBackground =
        new Color(
            0.025f,
            0.03f,
            0.04f,
            1f);

    private static readonly Color HeaderText =
        new Color(
            0.82f,
            0.84f,
            0.87f,
            1f);

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindAnyObjectByType<
                WarboardBuildHeader>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "WarboardBuildHeader");

        Object.DontDestroyOnLoad(go);

        go.AddComponent<
            WarboardBuildHeader>();
    }

    private void Update()
    {
        if (game == null)
        {
            game =
                Object.FindAnyObjectByType<
                    GameController>();
        }
    }

    private void OnGUI()
    {
        if (game == null)
            return;

        // Negative GUI depth renders this after the existing IMGUI header,
        // allowing this one small script to be the authoritative visible
        // version marker without touching the large GameController file.
        int previousDepth =
            GUI.depth;

        GUI.depth = -10000;

        Color previousColor =
            GUI.color;

        GUI.color =
            HeaderBackground;

        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                23f),
            Texture2D.whiteTexture);

        GUI.color =
            previousColor;

        GUIStyle style =
            new GUIStyle(
                GUI.skin.label);

        style.fontSize = 14;
        style.fontStyle =
            FontStyle.Bold;
        style.normal.textColor =
            HeaderText;
        style.alignment =
            TextAnchor.MiddleLeft;

        string mode =
            game.IsXcomMode
            ? "XCOM"
            : "TRADITIONAL";

        string battleSize =
            ReadPrivateString(
                game,
                "battleSizeName",
                "");

        string header =
            "WARBOARD " +
            WarboardBuildInfo.CurrentVersion +
            "  •  " +
            mode;

        if (!string.IsNullOrWhiteSpace(
                battleSize))
        {
            header +=
                "  •  " +
                battleSize.ToUpper();
        }

        header += "  •";

        GUI.Label(
            new Rect(
                9f,
                1f,
                Screen.width - 18f,
                22f),
            header,
            style);

        GUI.depth =
            previousDepth;
    }

    private static string ReadPrivateString(
        object instance,
        string fieldName,
        string fallback)
    {
        if (instance == null)
            return fallback;

        FieldInfo field =
            instance.GetType()
                .GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

        if (field == null)
            return fallback;

        object value =
            field.GetValue(
                instance);

        return value != null
            ? value.ToString()
            : fallback;
    }
}
