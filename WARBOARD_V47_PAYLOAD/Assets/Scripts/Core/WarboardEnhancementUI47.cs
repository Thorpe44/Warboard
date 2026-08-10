using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Explicit bearer assignment UI. It never guesses an Enhancement bearer.
/// Manifest Enhancements are registered automatically; manual registration is
/// available for rosters whose text export does not preserve Enhancement data.
/// </summary>
[DefaultExecutionOrder(-31920)]
public sealed class WarboardEnhancementUI47 : MonoBehaviour
{
    private static WarboardEnhancementUI47 instance;

    private StandardFactionGameController controller;
    private bool show;
    private Vector2 scroll;
    private WarboardEnhancementAssignment47 assigning;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object.FindAnyObjectByType<
                WarboardEnhancementUI47>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "Warboard Enhancement Bearers v47"
            );

        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<WarboardEnhancementUI47>();
    }

    private void Awake()
    {
        instance = this;
    }

    public static void Open(
        StandardFactionGameController value)
    {
        if (instance == null || value == null)
            return;

        WarboardEnhancementRegistry47.SyncFromController(
            value
        );

        instance.controller = value;
        instance.assigning = null;
        instance.scroll = Vector2.zero;
        instance.show = true;
    }

    private void OnGUI()
    {
        if (!show ||
            controller == null ||
            controller.Pack == null)
        {
            return;
        }

        if (assigning != null)
        {
            DrawBearerPicker();
            return;
        }

        DrawEnhancementPanel();
    }

    private void DrawEnhancementPanel()
    {
        float width =
            Mathf.Min(760f, Screen.width - 40f);

        float height =
            Mathf.Min(690f, Screen.height - 70f);

        Rect panel =
            new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height
            );

        GUI.Box(panel, "");

        GUIStyle title =
            new GUIStyle(GUI.skin.label);
        title.fontSize = 20;
        title.fontStyle = FontStyle.Bold;

        GUIStyle small =
            new GUIStyle(GUI.skin.label);
        small.wordWrap = true;
        small.fontSize = 12;

        GUI.Label(
            new Rect(
                panel.x + 16f,
                panel.y + 12f,
                panel.width - 72f,
                28f
            ),
            controller.DisplayName +
            " - ENHANCEMENT BEARERS",
            title
        );

        if (GUI.Button(
            new Rect(
                panel.x + panel.width - 48f,
                panel.y + 10f,
                32f,
                28f
            ),
            "X"))
        {
            show = false;
            return;
        }

        GUI.Label(
            new Rect(
                panel.x + 16f,
                panel.y + 45f,
                panel.width - 32f,
                48f
            ),
            "Warboard never guesses which unit carries an Enhancement. " +
            "Roster Enhancements are listed automatically when available; " +
            "otherwise register the one actually taken, then assign its bearer.",
            small
        );

        Rect outer =
            new Rect(
                panel.x + 14f,
                panel.y + 96f,
                panel.width - 28f,
                panel.height - 112f
            );

        List<StandardFactionEnhancement11> available =
            AvailableEnhancements();

        float contentHeight =
            Mathf.Max(
                outer.height,
                30f + available.Count * 92f
            );

        scroll = GUI.BeginScrollView(
            outer,
            scroll,
            new Rect(
                0f,
                0f,
                outer.width - 20f,
                contentHeight
            )
        );

        float y = 4f;

        if (available.Count == 0)
        {
            GUI.Label(
                new Rect(4f, y, outer.width - 30f, 48f),
                "No Enhancements are present in the selected detachment(s).",
                small
            );
        }

        foreach (StandardFactionEnhancement11 enhancement
            in available)
        {
            StandardFactionDetachment11 detachment =
                FindDetachmentFor(enhancement);

            WarboardEnhancementAssignment47 assignment =
                WarboardEnhancementRegistry47.Find(
                    controller.FactionId,
                    enhancement.name
                );

            GUI.Box(
                new Rect(
                    2f,
                    y,
                    outer.width - 28f,
                    82f
                ),
                ""
            );

            GUI.Label(
                new Rect(12f, y + 7f, 330f, 22f),
                enhancement.name +
                " - " +
                enhancement.points +
                " pts"
            );

            string status =
                assignment == null
                ? "NOT IN ROSTER / NOT REGISTERED"
                : assignment.Bearer != null
                    ? "BEARER: " +
                      assignment.Bearer.DisplayName
                    : "REGISTERED - BEARER REQUIRED";

            GUI.Label(
                new Rect(12f, y + 31f, 410f, 22f),
                status
            );

            if (assignment == null)
            {
                if (GUI.Button(
                    new Rect(
                        outer.width - 162f,
                        y + 8f,
                        130f,
                        28f
                    ),
                    "REGISTER TAKEN"))
                {
                    assignment =
                        WarboardEnhancementRegistry47
                            .RegisterManual(
                                controller,
                                detachment,
                                enhancement
                            );
                }
            }
            else
            {
                if (GUI.Button(
                    new Rect(
                        outer.width - 162f,
                        y + 8f,
                        130f,
                        28f
                    ),
                    assignment.Bearer == null
                    ? "ASSIGN BEARER"
                    : "CHANGE BEARER"))
                {
                    assigning = assignment;
                }

                if (!assignment.FromRosterManifest &&
                    GUI.Button(
                        new Rect(
                            outer.width - 162f,
                            y + 42f,
                            130f,
                            26f
                        ),
                        "REMOVE"))
                {
                    WarboardEnhancementRegistry47.RemoveManual(
                        controller.FactionId,
                        assignment.EnhancementName
                    );
                }
            }

            y += 90f;
        }

        GUI.EndScrollView();
    }

    private void DrawBearerPicker()
    {
        float width =
            Mathf.Min(660f, Screen.width - 40f);

        float height =
            Mathf.Min(640f, Screen.height - 60f);

        Rect panel =
            new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height
            );

        GUI.Box(panel, "");

        GUIStyle title =
            new GUIStyle(GUI.skin.label);
        title.fontSize = 19;
        title.fontStyle = FontStyle.Bold;

        GUI.Label(
            new Rect(
                panel.x + 16f,
                panel.y + 12f,
                panel.width - 32f,
                30f
            ),
            "ASSIGN: " +
            assigning.EnhancementName,
            title
        );

        GUI.Label(
            new Rect(
                panel.x + 16f,
                panel.y + 44f,
                panel.width - 32f,
                40f
            ),
            "Choose the actual bearer from the imported roster. " +
            "When the source card states a machine-readable MODEL/UNIT ONLY restriction, Warboard enforces it; otherwise it does not invent one."
        );

        List<SquadController> candidates =
            controller.ArmyUnits
                .Where(
                    unit =>
                    {
                        if (unit == null)
                            return false;

                        string reason;

                        return WarboardEnhancementRegistry47
                            .IsEligibleBearer(
                                assigning,
                                unit,
                                out reason
                            );
                    })
                .OrderByDescending(
                    unit => unit.HasKeyword("CHARACTER"))
                .ThenBy(unit => unit.DisplayName)
                .ToList();

        Rect outer =
            new Rect(
                panel.x + 14f,
                panel.y + 88f,
                panel.width - 28f,
                panel.height - 142f
            );

        scroll = GUI.BeginScrollView(
            outer,
            scroll,
            new Rect(
                0f,
                0f,
                outer.width - 20f,
                Mathf.Max(
                    outer.height,
                    candidates.Count * 42f + 12f
                )
            )
        );

        float y = 4f;

        foreach (SquadController unit
            in candidates)
        {
            string flags =
                (unit.HasKeyword("CHARACTER")
                    ? "CHARACTER  "
                    : "") +
                (unit.HasKeyword("MONSTER")
                    ? "MONSTER  "
                    : "") +
                (unit.HasKeyword("VEHICLE")
                    ? "VEHICLE"
                    : "");

            if (GUI.Button(
                new Rect(
                    4f,
                    y,
                    outer.width - 32f,
                    36f
                ),
                unit.DisplayName +
                (string.IsNullOrWhiteSpace(flags)
                    ? ""
                    : "    [" + flags.Trim() + "]")))
            {
                WarboardEnhancementRegistry47.AssignBearer(
                    assigning,
                    unit
                );

                assigning = null;
                scroll = Vector2.zero;
                return;
            }

            y += 42f;
        }

        GUI.EndScrollView();

        if (GUI.Button(
            new Rect(
                panel.x + 16f,
                panel.y + panel.height - 42f,
                120f,
                28f
            ),
            "BACK"))
        {
            assigning = null;
            scroll = Vector2.zero;
        }

        if (GUI.Button(
            new Rect(
                panel.x + 146f,
                panel.y + panel.height - 42f,
                150f,
                28f
            ),
            "CLEAR BEARER"))
        {
            WarboardEnhancementRegistry47.AssignBearer(
                assigning,
                null
            );

            assigning = null;
            scroll = Vector2.zero;
        }
    }

    private List<StandardFactionEnhancement11>
        AvailableEnhancements()
    {
        List<StandardFactionEnhancement11> result =
            new List<StandardFactionEnhancement11>();

        foreach (string name
            in controller.SelectedDetachments)
        {
            StandardFactionDetachment11 detachment =
                controller.GetDetachment(name);

            if (detachment == null ||
                detachment.enhancements == null)
            {
                continue;
            }

            result.AddRange(
                detachment.enhancements
                    .Where(value => value != null)
            );
        }

        return result;
    }

    private StandardFactionDetachment11
        FindDetachmentFor(
            StandardFactionEnhancement11 enhancement)
    {
        foreach (string name
            in controller.SelectedDetachments)
        {
            StandardFactionDetachment11 detachment =
                controller.GetDetachment(name);

            if (detachment == null ||
                detachment.enhancements == null)
            {
                continue;
            }

            if (detachment.enhancements.Contains(
                    enhancement))
            {
                return detachment;
            }
        }

        return null;
    }
}
