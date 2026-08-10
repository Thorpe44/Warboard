using UnityEngine;

public class WarboardV45HudOverlay : MonoBehaviour
{
    private void OnGUI()
    {
        GUIStyle style =
            new GUIStyle(GUI.skin.label);

        style.fontSize = 11;
        style.fontStyle =
            FontStyle.Bold;

        style.normal.textColor =
            new Color(
                1f,
                1f,
                1f,
                0.28f
            );

        GUI.Label(
            new Rect(
                10f,
                Screen.height - 24f,
                190f,
                18f
            ),
            "WARBOARD " +
            WarboardBuildInfo.CurrentVersion,
            style
        );
    }
}
