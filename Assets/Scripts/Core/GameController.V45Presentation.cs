using System.Collections.Generic;
using UnityEngine;

// WARBOARD_V45_6_SELECTED_UNIT_CARD
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
                570f,
                Screen.width - 24f
            );

        float height =
            manualRestoreEditMode
            ? 148f
            : manualWoundEditMode
                ? 132f
                : 96f;

        Rect card =
            new Rect(
                12f,
                82f,
                width,
                height
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
                card.x + 14f,
                card.y + 8f,
                245f,
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
            ? " | MODEL " +
              selectedModel.CurrentWounds +
              "/" +
              selectedModel.MaxWounds +
              " W"
            : "";

        string stats =
            modelText +
            selectedModelText +
            " | M " +
            selectedSquad.GetMove()
                .ToString("0.#") +
            "  T " +
            selectedSquad.Toughness +
            "  SV " +
            selectedSquad.BaseSave +
            "+ | " +
            state;

        GUI.Label(
            new Rect(
                card.x + 14f,
                card.y + 34f,
                card.width - 28f,
                22f
            ),
            stats,
            WarboardV45Presentation
                .SelectedBodyStyle
        );

        float buttonY =
            card.y + 60f;

        float x =
            card.x + 14f;

        if (!IsXcomMode)
        {
            Color old = GUI.color;

            if (manualWoundEditMode)
            {
                GUI.color =
                    new Color(
                        1f,
                        0.72f,
                        0.62f,
                        1f
                    );
            }

            if (GUI.Button(
                new Rect(
                    x,
                    buttonY,
                    108f,
                    28f
                ),
                manualWoundEditMode
                ? "WOUNDS: ON"
                : "WOUND EDIT"))
            {
                manualWoundEditMode =
                    !manualWoundEditMode;

                if (manualWoundEditMode)
                    manualRestoreEditMode = false;

                pendingTraditionalRemovalCandidate = null;

                status =
                    manualWoundEditMode
                    ? "MANUAL WOUNDS: click any model, then use -1 / +1 / REMOVE."
                    : "Manual wound editing disabled.";
            }

            GUI.color = old;
            x += 114f;

            if (manualRestoreEditMode)
            {
                GUI.color =
                    new Color(
                        0.72f,
                        0.90f,
                        1f,
                        1f
                    );
            }

            if (GUI.Button(
                new Rect(
                    x,
                    buttonY,
                    112f,
                    28f
                ),
                manualRestoreEditMode
                ? "RESTORE: ON"
                : "RESTORE EDIT"))
            {
                manualRestoreEditMode =
                    !manualRestoreEditMode;

                if (manualRestoreEditMode)
                    manualWoundEditMode = false;

                manualRestoreDeadIndex = 0;
                pendingTraditionalRemovalCandidate = null;

                status =
                    manualRestoreEditMode
                    ? "MANUAL RESTORE: select a unit, choose the destroyed model, then return it."
                    : "Manual restore editing disabled.";
            }

            GUI.color = old;
            x += 118f;
        }

        if (GUI.Button(
            new Rect(
                x,
                buttonY,
                92f,
                28f
            ),
            "DATASHEET"))
        {
            OpenDatasheetForSelection();
        }

        x += 98f;

        GUI.enabled =
            phase == Phase.Command;

        if (GUI.Button(
            new Rect(
                x,
                buttonY,
                92f,
                28f
            ),
            "ABILITIES"))
        {
            TryOpenCommandAbilities();
        }

        GUI.enabled = true;

        if (manualWoundEditMode &&
            selectedModel != null &&
            selectedModel.IsAlive)
        {
            float editY =
                card.y + 98f;

            GUI.Label(
                new Rect(
                    card.x + 14f,
                    editY + 4f,
                    94f,
                    24f
                ),
                selectedModel.CurrentWounds +
                "/" +
                selectedModel.MaxWounds +
                " W"
            );

            if (GUI.Button(
                new Rect(
                    card.x + 108f,
                    editY,
                    52f,
                    28f
                ),
                "-1"))
            {
                TraditionalAdjustSelectedWounds(-1);

                if (selectedModel == null ||
                    !selectedModel.IsAlive)
                {
                    GUI.enabled = true;
                    return;
                }
            }

            GUI.enabled =
                selectedModel.CurrentWounds <
                selectedModel.MaxWounds;

            if (GUI.Button(
                new Rect(
                    card.x + 166f,
                    editY,
                    52f,
                    28f
                ),
                "+1"))
            {
                TraditionalAdjustSelectedWounds(1);
            }

            GUI.enabled = true;

            bool confirm =
                pendingTraditionalRemovalCandidate ==
                    selectedModel;

            if (GUI.Button(
                new Rect(
                    card.x + 224f,
                    editY,
                    confirm ? 132f : 86f,
                    28f
                ),
                confirm
                ? "REMOVE ANYWAY"
                : "REMOVE"))
            {
                if (confirm)
                {
                    ConfirmTraditionalModelRemoval(
                        selectedModel
                    );
                }
                else
                {
                    TryTraditionalRemoveSelectedModel();
                }
            }
        }

        if (manualRestoreEditMode)
        {
            float restoreY =
                card.y + 98f;

            List<ModelToken> restoreCandidates =
                TraditionalRestoreCandidates();

            if (restoreCandidates.Count == 0)
            {
                GUI.Label(
                    new Rect(
                        card.x + 14f,
                        restoreY + 4f,
                        180f,
                        24f
                    ),
                    "No destroyed models"
                );
            }
            else
            {
                manualRestoreDeadIndex =
                    Mathf.Clamp(
                        manualRestoreDeadIndex,
                        0,
                        restoreCandidates.Count - 1
                    );

                ModelToken candidate =
                    restoreCandidates[
                        manualRestoreDeadIndex
                    ];

                if (GUI.Button(
                    new Rect(
                        card.x + 14f,
                        restoreY,
                        36f,
                        28f
                    ),
                    "<"))
                {
                    CycleTraditionalRestoreCandidate(-1);
                }

                GUI.Label(
                    new Rect(
                        card.x + 58f,
                        restoreY + 4f,
                        170f,
                        24f
                    ),
                    candidate.RoleName +
                    " " +
                    (manualRestoreDeadIndex + 1) +
                    "/" +
                    restoreCandidates.Count
                );

                if (GUI.Button(
                    new Rect(
                        card.x + 230f,
                        restoreY,
                        36f,
                        28f
                    ),
                    ">"))
                {
                    CycleTraditionalRestoreCandidate(1);
                }

                if (GUI.Button(
                    new Rect(
                        card.x + 276f,
                        restoreY,
                        102f,
                        28f
                    ),
                    "RETURN 1W"))
                {
                    TraditionalRestoreCurrentModel(false);
                }

                if (GUI.Button(
                    new Rect(
                        card.x + 386f,
                        restoreY,
                        112f,
                        28f
                    ),
                    "RETURN FULL"))
                {
                    TraditionalRestoreCurrentModel(true);
                }
            }
        }
    }
}
