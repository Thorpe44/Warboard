using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// WARBOARD v45.4 physical off-board trays.
///
/// Important:
/// Reserve/dead gameplay models remain in their authoritative game states.
/// These trays display non-interactive visual copies of the real miniature
/// visuals, so the presentation cannot accidentally affect rules, selection,
/// movement, LOS or resurrection state.
/// </summary>
public class WarboardV45PhysicalSideTrays :
    MonoBehaviour
{
    private sealed class Tray
    {
        public string Name;
        public string Role;
        public string Faction;
        public Vector3 Centre;
        public float Width;
        public float Depth;
        public Transform Root;
        public Transform ContentRoot;
        public TextMesh Label;
        public Color Accent;
        public string Signature = "";
    }

    private GameController game;

    private readonly List<Tray> trays =
        new List<Tray>();

    private static FieldInfo squadModelsField;
    private static FieldInfo modelVisualRootField;
    private static FieldInfo modelProxyRendererField;

    private bool built;

    private void Awake()
    {
        game =
            GameController.Current;

        if (game == null)
        {
            game =
                UnityEngine.Object
                    .FindAnyObjectByType<
                        GameController
                    >();
        }

        CacheReflection();
    }

    private void Start()
    {
        EnsureLegacyScoreboardActive();
        TryBuild();
    }

    private void Update()
    {
        if (game == null)
        {
            game =
                GameController.Current;

            if (game == null)
            {
                game =
                    UnityEngine.Object
                        .FindAnyObjectByType<
                            GameController
                        >();
            }
        }

        if (!built)
            TryBuild();

        EnsureLegacyScoreboardActive();
    }

    private void LateUpdate()
    {
        if (!built ||
            game == null)
        {
            return;
        }

        RefreshTrayOwnership();
        RefreshTrayVisuals();
    }

    private static void CacheReflection()
    {
        if (squadModelsField == null)
        {
            squadModelsField =
                typeof(SquadController)
                    .GetField(
                        "models",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic
                    );
        }

        if (modelVisualRootField == null)
        {
            modelVisualRootField =
                typeof(ModelToken)
                    .GetField(
                        "visualRoot",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic
                    );
        }

        if (modelProxyRendererField == null)
        {
            modelProxyRendererField =
                typeof(ModelToken)
                    .GetField(
                        "proxyRenderer",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic
                    );
        }
    }

    private void EnsureLegacyScoreboardActive()
    {
        BattlefieldWorldUI[] worldUis =
            UnityEngine.Object
                .FindObjectsByType<
                    BattlefieldWorldUI
                >(FindObjectsInactive.Include,
                  FindObjectsSortMode.None);

        foreach (BattlefieldWorldUI ui
            in worldUis)
        {
            if (ui != null &&
                !ui.gameObject.activeSelf)
            {
                ui.gameObject.SetActive(true);
            }
        }
    }

    private void TryBuild()
    {
        GameObject board =
            GameObject.Find("Board");

        if (board == null)
            return;

        DestroyOldTrayObject(
            "Left Reserves"
        );

        DestroyOldTrayObject(
            "Left Destroyed"
        );

        DestroyOldTrayObject(
            "Right Reserves"
        );

        DestroyOldTrayObject(
            "Right Destroyed"
        );

        float boardWidth =
            board.transform.localScale.x;

        float boardDepth =
            board.transform.localScale.z;

        const float trayWidth = 11.0f;
        const float trayDepth = 7.2f;

        float sideX =
            boardWidth * 0.5f +
            trayWidth * 0.5f +
            0.75f;

        float upperZ =
            boardDepth * 0.23f;

        float lowerZ =
            -boardDepth * 0.23f;

        trays.Clear();

        trays.Add(
            CreateTray(
                "Left Reserves",
                "RESERVES",
                new Vector3(
                    -sideX,
                    0f,
                    upperZ
                ),
                trayWidth,
                trayDepth,
                new Color(
                    0.78f,
                    0.26f,
                    0.88f,
                    1f
                )
            )
        );

        trays.Add(
            CreateTray(
                "Left Destroyed",
                "DESTROYED",
                new Vector3(
                    -sideX,
                    0f,
                    lowerZ
                ),
                trayWidth,
                trayDepth,
                new Color(
                    0.78f,
                    0.26f,
                    0.88f,
                    1f
                )
            )
        );

        trays.Add(
            CreateTray(
                "Right Reserves",
                "RESERVES",
                new Vector3(
                    sideX,
                    0f,
                    upperZ
                ),
                trayWidth,
                trayDepth,
                new Color(
                    0.24f,
                    0.88f,
                    0.55f,
                    1f
                )
            )
        );

        trays.Add(
            CreateTray(
                "Right Destroyed",
                "DESTROYED",
                new Vector3(
                    sideX,
                    0f,
                    lowerZ
                ),
                trayWidth,
                trayDepth,
                new Color(
                    0.24f,
                    0.88f,
                    0.55f,
                    1f
                )
            )
        );

        built = true;

        RefreshTrayOwnership();
        RefreshTrayVisuals();
    }

    private static void DestroyOldTrayObject(
        string name)
    {
        GameObject existing =
            GameObject.Find(name);

        if (existing != null)
        {
            UnityEngine.Object.Destroy(
                existing
            );
        }
    }

    private Tray CreateTray(
        string name,
        string role,
        Vector3 centre,
        float width,
        float depth,
        Color accent)
    {
        GameObject rootObject =
            new GameObject(name);

        rootObject.transform.position =
            centre;

        Tray tray =
            new Tray
            {
                Name = name,
                Role = role,
                Centre = centre,
                Width = width,
                Depth = depth,
                Root =
                    rootObject.transform,
                Accent = accent
            };

        Material shell =
            CreateMaterial(
                Color.Lerp(
                    new Color(
                        0.10f,
                        0.115f,
                        0.14f,
                        1f
                    ),
                    accent,
                    0.10f
                ),
                0.46f,
                0.28f
            );

        Material floor =
            CreateMaterial(
                new Color(
                    0.13f,
                    0.145f,
                    0.16f,
                    1f
                ),
                0.34f,
                0.18f
            );

        AddBox(
            tray.Root,
            "Floor",
            new Vector3(
                0f,
                -0.18f,
                0f
            ),
            new Vector3(
                width,
                0.18f,
                depth
            ),
            floor
        );

        AddBox(
            tray.Root,
            "Rear Wall",
            new Vector3(
                0f,
                0.38f,
                depth * 0.5f -
                    0.12f
            ),
            new Vector3(
                width,
                0.95f,
                0.20f
            ),
            shell
        );

        AddBox(
            tray.Root,
            "Front Lip",
            new Vector3(
                0f,
                0.08f,
                -depth * 0.5f +
                    0.12f
            ),
            new Vector3(
                width,
                0.32f,
                0.20f
            ),
            shell
        );

        AddBox(
            tray.Root,
            "Left Wall",
            new Vector3(
                -width * 0.5f +
                    0.12f,
                0.22f,
                0f
            ),
            new Vector3(
                0.20f,
                0.52f,
                depth
            ),
            shell
        );

        AddBox(
            tray.Root,
            "Right Wall",
            new Vector3(
                width * 0.5f -
                    0.12f,
                0.22f,
                0f
            ),
            new Vector3(
                0.20f,
                0.52f,
                depth
            ),
            shell
        );

        AddBox(
            tray.Root,
            "Accent Strip",
            new Vector3(
                0f,
                0.62f,
                depth * 0.5f -
                    0.03f
            ),
            new Vector3(
                width - 0.25f,
                0.07f,
                0.06f
            ),
            CreateMaterial(
                accent,
                0.10f,
                0.50f,
                true
            )
        );

        GameObject contentObject =
            new GameObject(
                "Display Models"
            );

        contentObject.transform
            .SetParent(
                tray.Root,
                false
            );

        tray.ContentRoot =
            contentObject.transform;

        GameObject labelObject =
            new GameObject("Label");

        labelObject.transform.SetParent(
            tray.Root,
            false
        );

        labelObject.transform.localPosition =
            new Vector3(
                0f,
                1.20f,
                depth * 0.5f -
                    0.06f
            );

        tray.Label =
            labelObject
                .AddComponent<TextMesh>();

        tray.Label.anchor =
            TextAnchor.MiddleCenter;

        tray.Label.alignment =
            TextAlignment.Center;

        tray.Label.fontSize = 54;
        tray.Label.characterSize =
            0.085f;

        tray.Label.color =
            new Color(
                0.95f,
                0.97f,
                0.99f,
                1f
            );

        labelObject.AddComponent<
            WoundDisplayBillboard
        >();

        return tray;
    }

    private void RefreshTrayOwnership()
    {
        List<string> factionOrder =
            FactionOrder();

        for (int i = 0;
             i < trays.Count;
             i++)
        {
            Tray tray =
                trays[i];

            int factionIndex =
                i < 2
                ? 0
                : 1;

            tray.Faction =
                factionOrder.Count >
                    factionIndex
                ? factionOrder[
                    factionIndex]
                : "";

            if (tray.Label != null)
            {
                string owner =
                    string.IsNullOrWhiteSpace(
                        tray.Faction
                    )
                    ? ""
                    : "\n" +
                      DisplayFactionName(
                          tray.Faction
                      );

                tray.Label.text =
                    tray.Role +
                    owner;
            }
        }
    }

    private List<string> FactionOrder()
    {
        if (game == null ||
            game.AllSquads == null)
        {
            return
                new List<string>();
        }

        return game.AllSquads
            .Where(
                squad =>
                    squad != null &&
                    !string.IsNullOrWhiteSpace(
                        squad.FactionId
                    )
            )
            .Select(
                squad =>
                    squad.FactionId
            )
            .Distinct(
                StringComparer
                    .OrdinalIgnoreCase
            )
            .Take(2)
            .ToList();
    }

    private string DisplayFactionName(
        string faction)
    {
        if (string.IsNullOrWhiteSpace(
                faction))
        {
            return "";
        }

        return faction;
    }

    private void RefreshTrayVisuals()
    {
        foreach (Tray tray
            in trays)
        {
            List<ModelToken> wanted =
                tray.Role == "RESERVES"
                ? ReserveModels(
                    tray.Faction)
                : DestroyedModels(
                    tray.Faction);

            string signature =
                BuildSignature(
                    wanted
                );

            if (signature ==
                tray.Signature)
            {
                continue;
            }

            tray.Signature =
                signature;

            RebuildTrayVisuals(
                tray,
                wanted
            );
        }
    }

    private List<ModelToken> ReserveModels(
        string faction)
    {
        List<ModelToken> result =
            new List<ModelToken>();

        if (game == null ||
            string.IsNullOrWhiteSpace(
                faction))
        {
            return result;
        }

        foreach (SquadController squad
            in game.AllSquads)
        {
            if (squad == null ||
                !string.Equals(
                    squad.FactionId,
                    faction,
                    StringComparison
                        .OrdinalIgnoreCase) ||
                squad.BattlefieldState !=
                    SquadBattlefieldState
                        .Reserves)
            {
                continue;
            }

            foreach (ModelToken model
                in AllModels(squad))
            {
                if (model != null &&
                    model.IsAlive)
                {
                    result.Add(model);
                }
            }
        }

        return result;
    }

    private List<ModelToken> DestroyedModels(
        string faction)
    {
        List<ModelToken> result =
            new List<ModelToken>();

        if (game == null ||
            string.IsNullOrWhiteSpace(
                faction))
        {
            return result;
        }

        foreach (SquadController squad
            in game.AllSquads)
        {
            if (squad == null ||
                !string.Equals(
                    squad.FactionId,
                    faction,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (ModelToken model
                in AllModels(squad))
            {
                if (model != null &&
                    !model.IsAlive)
                {
                    result.Add(model);
                }
            }
        }

        return result;
    }

    private static List<ModelToken> AllModels(
        SquadController squad)
    {
        CacheReflection();

        if (squad == null ||
            squadModelsField == null)
        {
            return
                new List<ModelToken>();
        }

        object raw =
            squadModelsField.GetValue(
                squad
            );

        List<ModelToken> list =
            raw as List<ModelToken>;

        return
            list != null
            ? list
            : new List<ModelToken>();
    }

    private static string BuildSignature(
        List<ModelToken> models)
    {
        if (models == null ||
            models.Count == 0)
        {
            return "EMPTY";
        }

        return string.Join(
            "|",
            models
                .Where(
                    model =>
                        model != null
                )
                .Select(
                    model =>
                        model
                            .GetEntityId()
                            .ToString()
                )
                .OrderBy(
                    value => value
                )
                .ToArray()
        );
    }

    private void RebuildTrayVisuals(
        Tray tray,
        List<ModelToken> models)
    {
        if (tray == null ||
            tray.ContentRoot == null)
        {
            return;
        }

        for (int childIndex =
                 tray.ContentRoot
                     .childCount -
                 1;
             childIndex >= 0;
             childIndex--)
        {
            Transform child =
                tray.ContentRoot
                    .GetChild(
                        childIndex
                    );

            if (child != null)
            {
                UnityEngine.Object
                    .Destroy(
                        child.gameObject
                    );
            }
        }

        if (models == null ||
            models.Count == 0)
        {
            return;
        }

        float left =
            -tray.Width * 0.5f +
            0.70f;

        float right =
            tray.Width * 0.5f -
            0.70f;

        float front =
            -tray.Depth * 0.5f +
            0.75f;

        float back =
            tray.Depth * 0.5f -
            0.85f;

        float cursorX = left;
        float cursorZ = back;
        float rowDepth = 0f;

        float crowdScale =
            models.Count > 18
            ? 0.68f
            : models.Count > 12
                ? 0.80f
                : 1f;

        foreach (ModelToken model
            in models)
        {
            if (model == null)
                continue;

            float diameter =
                Mathf.Clamp(
                    model
                        .BaseRadiusInches *
                        2f +
                    0.40f,
                    0.95f,
                    3.8f
                ) *
                crowdScale;

            if (cursorX +
                    diameter >
                right)
            {
                cursorX = left;
                cursorZ -=
                    Mathf.Max(
                        rowDepth,
                        1.25f
                    );

                rowDepth = 0f;
            }

            if (cursorZ -
                    diameter <
                front)
            {
                crowdScale *= 0.82f;
                diameter *= 0.82f;
            }

            float placeX =
                cursorX +
                diameter * 0.5f;

            float placeZ =
                cursorZ -
                diameter * 0.5f;

            GameObject clone =
                CreateVisualClone(
                    model,
                    tray.ContentRoot,
                    crowdScale
                );

            if (clone != null)
            {
                clone.transform
                    .localPosition =
                    new Vector3(
                        placeX,
                        0.02f,
                        placeZ
                    );

                clone.transform
                    .localRotation =
                    Quaternion.Euler(
                        0f,
                        tray.Centre.x <
                            0f
                        ? 18f
                        : -18f,
                        0f
                    );
            }

            cursorX +=
                diameter +
                0.18f;

            rowDepth =
                Mathf.Max(
                    rowDepth,
                    diameter +
                    0.18f
                );
        }
    }

    private static GameObject
        CreateVisualClone(
            ModelToken model,
            Transform parent,
            float scale)
    {
        CacheReflection();

        if (model == null ||
            parent == null)
        {
            return null;
        }

        GameObject sourceVisual =
            null;

        if (modelVisualRootField != null)
        {
            sourceVisual =
                modelVisualRootField
                    .GetValue(model)
                as GameObject;
        }

        GameObject clone;

        if (sourceVisual != null)
        {
            clone =
                UnityEngine.Object
                    .Instantiate(
                        sourceVisual,
                        parent
                    );

            clone.name =
                "Tray Visual - " +
                model.RoleName;

            clone.SetActive(true);

            Renderer[] renderers =
                clone
                    .GetComponentsInChildren<
                        Renderer
                    >(true);

            foreach (Renderer renderer
                in renderers)
            {
                if (renderer == null)
                    continue;

                bool selection =
                    renderer.gameObject
                        .name
                        .IndexOf(
                            "Selection",
                            StringComparison
                                .OrdinalIgnoreCase
                        ) >= 0;

                renderer.enabled =
                    !selection;
            }

            Collider[] colliders =
                clone
                    .GetComponentsInChildren<
                        Collider
                    >(true);

            foreach (Collider collider
                in colliders)
            {
                if (collider != null)
                {
                    UnityEngine.Object
                        .Destroy(
                            collider
                        );
                }
            }

            clone.transform.localScale =
                Vector3.one * scale;

            return clone;
        }

        clone =
            GameObject.CreatePrimitive(
                PrimitiveType.Capsule
            );

        clone.name =
            "Tray Proxy - " +
            model.RoleName;

        clone.transform.SetParent(
            parent,
            false
        );

        clone.transform.localScale =
            model.transform.localScale *
            scale;

        Collider fallbackCollider =
            clone.GetComponent<Collider>();

        if (fallbackCollider != null)
        {
            UnityEngine.Object.Destroy(
                fallbackCollider
            );
        }

        Renderer sourceRenderer = null;

        if (modelProxyRendererField != null)
        {
            sourceRenderer =
                modelProxyRendererField
                    .GetValue(model)
                as Renderer;
        }

        Renderer targetRenderer =
            clone.GetComponent<Renderer>();

        if (sourceRenderer != null &&
            targetRenderer != null &&
            sourceRenderer.sharedMaterial !=
                null)
        {
            targetRenderer.sharedMaterial =
                sourceRenderer
                    .sharedMaterial;
        }

        clone.transform.localPosition =
            new Vector3(
                0f,
                0.65f,
                0f
            );

        return clone;
    }

    private static GameObject AddBox(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject go =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        go.name = name;

        go.transform.SetParent(
            parent,
            false
        );

        go.transform.localPosition =
            localPosition;

        go.transform.localScale =
            localScale;

        Collider col =
            go.GetComponent<Collider>();

        if (col != null)
        {
            UnityEngine.Object.Destroy(
                col
            );
        }

        Renderer renderer =
            go.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial =
                material;
        }

        return go;
    }

    private static Material CreateMaterial(
        Color color,
        float metallic,
        float smoothness,
        bool emission = false)
    {
        Shader shader =
            Shader.Find("Standard");

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                );
        }

        if (shader == null)
        {
            shader =
                Shader.Find("Diffuse");
        }

        Material material =
            new Material(shader);

        material.color = color;

        if (material.HasProperty(
                "_Metallic"))
        {
            material.SetFloat(
                "_Metallic",
                metallic
            );
        }

        if (material.HasProperty(
                "_Glossiness"))
        {
            material.SetFloat(
                "_Glossiness",
                smoothness
            );
        }

        if (material.HasProperty(
                "_Smoothness"))
        {
            material.SetFloat(
                "_Smoothness",
                smoothness
            );
        }

        if (emission &&
            material.HasProperty(
                "_EmissionColor"))
        {
            material.EnableKeyword(
                "_EMISSION"
            );

            material.SetColor(
                "_EmissionColor",
                color * 0.70f
            );
        }

        return material;
    }
}
