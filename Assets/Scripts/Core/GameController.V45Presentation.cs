using UnityEngine;

// WARBOARD_V45_UI_THEME
public partial class GameController : MonoBehaviour
{
    private void DrawV45SelectedUnitCard()
    {
        if (selectedSquad == null ||
            deploymentMode ||
            armyImportMode ||
            battleSetupMode ||
            missionSetupMode)
        {
            return;
        }

        float width =
            Mathf.Min(
                560f,
                Screen.width - 28f
            );

        Rect card =
            new Rect(
                14f,
                Screen.height - 158f,
                width,
                78f
            );

        Color accent =
            FactionColor(
                selectedSquad.FactionId
            );

        WarboardV45Presentation.DrawPanel(
            card,
            accent,
            true
        );

        GUI.Label(
            new Rect(
                card.x + 16f,
                card.y + 9f,
                card.width - 210f,
                24f
            ),
            selectedSquad.DisplayName,
            WarboardV45Presentation
                .SelectedTitleStyle
        );

        string state =
            selectedSquad.HasAdvanced
            ? "ADVANCED"
            : selectedSquad.HasMoved
                ? "MOVED"
                : "READY";

        string modelText =
            selectedSquad.LivingModels +
            "/" +
            selectedSquad.StartingModels +
            " MODELS";

        string selectedModelText =
            selectedModel != null &&
            selectedModel.IsAlive
            ? "   •   MODEL " +
              selectedModel.CurrentWounds +
              "/" +
              selectedModel.MaxWounds +
              " W"
            : "";

        string stats =
            modelText +
            selectedModelText +
            "   •   M " +
            selectedSquad.GetMove()
                .ToString("0.#") +
            "\"   T " +
            selectedSquad.Toughness +
            "   SV " +
            selectedSquad.BaseSave +
            "+   •   " +
            state;

        GUI.Label(
            new Rect(
                card.x + 16f,
                card.y + 36f,
                card.width - 200f,
                30f
            ),
            stats,
            WarboardV45Presentation
                .SelectedBodyStyle
        );

        if (GUI.Button(
            new Rect(
                card.x +
                    card.width -
                    184f,
                card.y + 12f,
                82f,
                28f
            ),
            "DATASHEET",
            WarboardV45Presentation
                .ToolbarButtonStyle))
        {
            OpenDatasheetForSelection();
        }

        GUI.enabled =
            phase == Phase.Command;

        if (GUI.Button(
            new Rect(
                card.x +
                    card.width -
                    94f,
                card.y + 12f,
                78f,
                28f
            ),
            "ABILITIES",
            WarboardV45Presentation
                .ToolbarButtonStyle))
        {
            TryOpenCommandAbilities();
        }

        GUI.enabled = true;

        string leader =
            selectedSquad.LeaderSummary();

        if (!string.IsNullOrWhiteSpace(
                leader))
        {
            GUI.Label(
                new Rect(
                    card.x +
                        card.width -
                        184f,
                    card.y + 47f,
                    168f,
                    20f
                ),
                leader,
                WarboardV45Presentation
                    .SubHeaderStyle
            );
        }
    }
}
