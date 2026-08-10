using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WARBOARD v45 presentation layer.
/// Keeps rules/colliders unchanged while replacing prototype rendering with
/// textured materials, dressed terrain silhouettes and a coherent IMGUI skin.
/// </summary>
public static class WarboardV45Presentation
{
    private static bool guiReady;
    private static Texture2D panelTexture;
    private static Texture2D buttonTexture;
    private static Texture2D buttonHoverTexture;
    private static Texture2D buttonActiveTexture;
    private static Texture2D primaryTexture;
    private static Texture2D primaryHoverTexture;
    private static Texture2D fieldTexture;

    private static GUIStyle toolbarButtonStyle;
    private static GUIStyle primaryButtonStyle;
    private static GUIStyle headerStyle;
    private static GUIStyle subHeaderStyle;
    private static GUIStyle phasePillStyle;
    private static GUIStyle selectedTitleStyle;
    private static GUIStyle selectedBodyStyle;

    private static Material boardMaterial;
    private static Material concreteMaterial;
    private static Material metalMaterial;
    private static Material rubbleMaterial;
    private static Material objectiveMaterial;
    private static Material worldPanelMaterial;

    public static GUIStyle ToolbarButtonStyle
    {
        get { EnsureGuiAssets(); return toolbarButtonStyle; }
    }

    public static GUIStyle PrimaryButtonStyle
    {
        get { EnsureGuiAssets(); return primaryButtonStyle; }
    }

    public static GUIStyle HeaderStyle
    {
        get { EnsureGuiAssets(); return headerStyle; }
    }

    public static GUIStyle SubHeaderStyle
    {
        get { EnsureGuiAssets(); return subHeaderStyle; }
    }

    public static GUIStyle PhasePillStyle
    {
        get { EnsureGuiAssets(); return phasePillStyle; }
    }

    public static GUIStyle SelectedTitleStyle
    {
        get { EnsureGuiAssets(); return selectedTitleStyle; }
    }

    public static GUIStyle SelectedBodyStyle
    {
        get { EnsureGuiAssets(); return selectedBodyStyle; }
    }

    public static void ApplyGuiTheme()
    {
        EnsureGuiAssets();

        GUI.contentColor = new Color(0.93f, 0.95f, 0.97f, 1f);
        GUI.backgroundColor = Color.white;

        GUI.skin.label.normal.textColor =
            new Color(0.90f, 0.92f, 0.95f, 1f);

        GUI.skin.label.fontSize = 12;

        GUI.skin.box.normal.background = panelTexture;
        GUI.skin.box.normal.textColor =
            new Color(0.91f, 0.93f, 0.96f, 1f);

        GUI.skin.button.normal.background = buttonTexture;
        GUI.skin.button.hover.background = buttonHoverTexture;
        GUI.skin.button.active.background = buttonActiveTexture;
        GUI.skin.button.focused.background = buttonHoverTexture;
        GUI.skin.button.normal.textColor = Color.white;
        GUI.skin.button.hover.textColor = Color.white;
        GUI.skin.button.active.textColor = Color.white;
        GUI.skin.button.fontSize = 11;
        GUI.skin.button.fontStyle = FontStyle.Bold;
        GUI.skin.button.padding = new RectOffset(10, 10, 5, 5);

        GUI.skin.textField.normal.background = fieldTexture;
        GUI.skin.textField.focused.background = buttonHoverTexture;
        GUI.skin.textField.normal.textColor = Color.white;
        GUI.skin.textField.focused.textColor = Color.white;
        GUI.skin.textField.padding = new RectOffset(9, 9, 5, 5);

        GUI.skin.scrollView.normal.background = panelTexture;
    }

    public static void DrawPanel(
        Rect rect,
        Color accent,
        bool strong = false)
    {
        EnsureGuiAssets();

        Color old = GUI.color;
        GUI.color = strong
            ? new Color(1f, 1f, 1f, 0.98f)
            : new Color(1f, 1f, 1f, 0.92f);

        GUI.DrawTexture(
            rect,
            panelTexture,
            ScaleMode.StretchToFill,
            true
        );

        GUI.color = accent;
        GUI.DrawTexture(
            new Rect(
                rect.x,
                rect.y,
                4f,
                rect.height
            ),
            Texture2D.whiteTexture
        );

        GUI.color = new Color(
            accent.r,
            accent.g,
            accent.b,
            0.55f
        );

        GUI.DrawTexture(
            new Rect(
                rect.x + 4f,
                rect.y,
                Mathf.Max(0f, rect.width - 4f),
                2f
            ),
            Texture2D.whiteTexture
        );

        GUI.color = old;
    }

    private static void EnsureGuiAssets()
    {
        if (guiReady)
            return;

        panelTexture = RoundedTexture(
            new Color(0.030f, 0.036f, 0.048f, 0.96f),
            new Color(0.15f, 0.18f, 0.22f, 0.95f),
            7
        );

        buttonTexture = RoundedTexture(
            new Color(0.095f, 0.11f, 0.14f, 0.98f),
            new Color(0.22f, 0.25f, 0.30f, 0.90f),
            6
        );

        buttonHoverTexture = RoundedTexture(
            new Color(0.145f, 0.17f, 0.21f, 1f),
            new Color(0.42f, 0.48f, 0.56f, 1f),
            6
        );

        buttonActiveTexture = RoundedTexture(
            new Color(0.07f, 0.22f, 0.29f, 1f),
            new Color(0.30f, 0.72f, 0.88f, 1f),
            6
        );

        primaryTexture = RoundedTexture(
            new Color(0.12f, 0.36f, 0.46f, 1f),
            new Color(0.36f, 0.83f, 0.92f, 1f),
            7
        );

        primaryHoverTexture = RoundedTexture(
            new Color(0.16f, 0.47f, 0.58f, 1f),
            new Color(0.52f, 0.92f, 0.98f, 1f),
            7
        );

        fieldTexture = RoundedTexture(
            new Color(0.045f, 0.052f, 0.066f, 1f),
            new Color(0.17f, 0.20f, 0.24f, 1f),
            5
        );

        toolbarButtonStyle = new GUIStyle(GUI.skin.button);
        toolbarButtonStyle.normal.background = buttonTexture;
        toolbarButtonStyle.hover.background = buttonHoverTexture;
        toolbarButtonStyle.active.background = buttonActiveTexture;
        toolbarButtonStyle.normal.textColor = Color.white;
        toolbarButtonStyle.hover.textColor = Color.white;
        toolbarButtonStyle.active.textColor = Color.white;
        toolbarButtonStyle.fontStyle = FontStyle.Bold;
        toolbarButtonStyle.fontSize = 11;

        primaryButtonStyle = new GUIStyle(toolbarButtonStyle);
        primaryButtonStyle.normal.background = primaryTexture;
        primaryButtonStyle.hover.background = primaryHoverTexture;
        primaryButtonStyle.active.background = primaryHoverTexture;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 17;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.UpperLeft;
        headerStyle.normal.textColor = Color.white;

        subHeaderStyle = new GUIStyle(GUI.skin.label);
        subHeaderStyle.fontSize = 10;
        subHeaderStyle.alignment = TextAnchor.UpperLeft;
        subHeaderStyle.normal.textColor =
            new Color(0.62f, 0.68f, 0.74f, 1f);

        phasePillStyle = new GUIStyle(GUI.skin.label);
        phasePillStyle.fontSize = 12;
        phasePillStyle.fontStyle = FontStyle.Bold;
        phasePillStyle.alignment = TextAnchor.MiddleCenter;
        phasePillStyle.normal.background = buttonActiveTexture;
        phasePillStyle.normal.textColor =
            new Color(0.94f, 0.98f, 1f, 1f);
        phasePillStyle.padding = new RectOffset(12, 12, 4, 4);

        selectedTitleStyle = new GUIStyle(GUI.skin.label);
        selectedTitleStyle.fontSize = 17;
        selectedTitleStyle.fontStyle = FontStyle.Bold;
        selectedTitleStyle.normal.textColor = Color.white;

        selectedBodyStyle = new GUIStyle(GUI.skin.label);
        selectedBodyStyle.fontSize = 11;
        selectedBodyStyle.normal.textColor =
            new Color(0.76f, 0.81f, 0.86f, 1f);

        guiReady = true;
    }

    private static Texture2D RoundedTexture(
        Color fill,
        Color border,
        int radius)
    {
        const int size = 32;

        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx =
                    x < radius
                    ? radius - x
                    : x >= size - radius
                        ? x - (size - radius - 1)
                        : 0f;

                float dy =
                    y < radius
                    ? radius - y
                    : y >= size - radius
                        ? y - (size - radius - 1)
                        : 0f;

                bool outside =
                    dx > 0f &&
                    dy > 0f &&
                    dx * dx + dy * dy >
                    radius * radius;

                if (outside)
                {
                    pixels[y * size + x] =
                        new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                bool edge =
                    x <= 1 ||
                    y <= 1 ||
                    x >= size - 2 ||
                    y >= size - 2;

                pixels[y * size + x] =
                    edge
                    ? border
                    : fill;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    public static void StyleBoard(GameObject board)
    {
        if (board == null)
            return;

        Renderer renderer =
            board.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial =
                BoardMaterial();

            if (renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.mainTextureScale =
                    new Vector2(
                        Mathf.Max(2f, board.transform.localScale.x / 7f),
                        Mathf.Max(2f, board.transform.localScale.z / 7f)
                    );
            }
        }

        if (board.transform.Find("V45_BoardTrim") != null)
            return;

        GameObject trimRoot =
            new GameObject("V45_BoardTrim");

        trimRoot.transform.SetParent(
            board.transform,
            false
        );

        AddVisualCube(
            trimRoot.transform,
            "North Trim",
            new Vector3(0f, 0.54f, 0.495f),
            new Vector3(1.0f, 0.10f, 0.012f),
            MetalMaterial(),
            Quaternion.identity
        );

        AddVisualCube(
            trimRoot.transform,
            "South Trim",
            new Vector3(0f, 0.54f, -0.495f),
            new Vector3(1.0f, 0.10f, 0.012f),
            MetalMaterial(),
            Quaternion.identity
        );

        AddVisualCube(
            trimRoot.transform,
            "East Trim",
            new Vector3(0.495f, 0.54f, 0f),
            new Vector3(0.012f, 0.10f, 1.0f),
            MetalMaterial(),
            Quaternion.identity
        );

        AddVisualCube(
            trimRoot.transform,
            "West Trim",
            new Vector3(-0.495f, 0.54f, 0f),
            new Vector3(0.012f, 0.10f, 1.0f),
            MetalMaterial(),
            Quaternion.identity
        );
    }

    public static void StyleTerrain(
        GameObject terrain,
        TerrainTrait trait,
        string id,
        Vector3 sourceSize)
    {
        if (terrain == null)
            return;

        Transform existing =
            terrain.transform.Find("V45_Visuals");

        if (existing != null)
            return;

        Renderer baseRenderer =
            terrain.GetComponent<Renderer>();

        if (baseRenderer != null)
            baseRenderer.enabled = false;

        GameObject root =
            new GameObject("V45_Visuals");

        root.transform.SetParent(
            terrain.transform,
            false
        );

        if (trait == TerrainTrait.Blocking)
        {
            BuildRuin(root.transform, id);
        }
        else if (trait == TerrainTrait.Cover)
        {
            BuildBarricades(root.transform, id);
        }
        else
        {
            BuildRubble(root.transform, id);
        }
    }

    private static void BuildRuin(
        Transform root,
        string id)
    {
        Material concrete = ConcreteMaterial();
        Material metal = MetalMaterial();

        AddVisualCube(
            root,
            "Rear Ruin Wall",
            new Vector3(0f, 0.02f, 0.405f),
            new Vector3(0.94f, 0.86f, 0.13f),
            concrete,
            Quaternion.identity
        );

        AddVisualCube(
            root,
            "Side Ruin Wall",
            new Vector3(-0.405f, -0.06f, 0f),
            new Vector3(0.13f, 0.74f, 0.72f),
            concrete,
            Quaternion.identity
        );

        AddVisualCube(
            root,
            "Broken Pillar",
            new Vector3(0.25f, -0.13f, -0.18f),
            new Vector3(0.16f, 0.58f, 0.17f),
            concrete,
            Quaternion.Euler(0f, 8f, 4f)
        );

        AddVisualCube(
            root,
            "Upper Brace",
            new Vector3(-0.08f, 0.29f, 0.30f),
            new Vector3(0.52f, 0.10f, 0.09f),
            metal,
            Quaternion.Euler(0f, -5f, 0f)
        );

        AddVisualCube(
            root,
            "Rubble A",
            new Vector3(0.14f, -0.40f, 0.06f),
            new Vector3(0.30f, 0.12f, 0.22f),
            RubbleMaterial(),
            Quaternion.Euler(0f, 27f, 6f)
        );

        AddVisualCube(
            root,
            "Rubble B",
            new Vector3(-0.09f, -0.42f, -0.16f),
            new Vector3(0.22f, 0.10f, 0.18f),
            RubbleMaterial(),
            Quaternion.Euler(0f, -18f, -5f)
        );
    }

    private static void BuildBarricades(
        Transform root,
        string id)
    {
        Material metal = MetalMaterial();
        Material rubble = RubbleMaterial();

        AddVisualCube(
            root,
            "Barricade A",
            new Vector3(-0.23f, -0.31f, 0.12f),
            new Vector3(0.42f, 0.25f, 0.13f),
            metal,
            Quaternion.Euler(0f, 8f, 0f)
        );

        AddVisualCube(
            root,
            "Barricade B",
            new Vector3(0.24f, -0.31f, -0.09f),
            new Vector3(0.42f, 0.25f, 0.13f),
            metal,
            Quaternion.Euler(0f, -10f, 0f)
        );

        AddVisualCube(
            root,
            "Crate A",
            new Vector3(-0.12f, -0.30f, -0.27f),
            new Vector3(0.20f, 0.28f, 0.22f),
            metal,
            Quaternion.Euler(0f, 18f, 0f)
        );

        AddVisualCube(
            root,
            "Debris",
            new Vector3(0.14f, -0.42f, 0.23f),
            new Vector3(0.34f, 0.10f, 0.22f),
            rubble,
            Quaternion.Euler(0f, 23f, 5f)
        );
    }

    private static void BuildRubble(
        Transform root,
        string id)
    {
        Material rubble = RubbleMaterial();
        Material metal = MetalMaterial();

        AddVisualCube(
            root,
            "Rubble Slab A",
            new Vector3(-0.22f, -0.39f, 0.05f),
            new Vector3(0.38f, 0.11f, 0.28f),
            rubble,
            Quaternion.Euler(2f, 18f, 6f)
        );

        AddVisualCube(
            root,
            "Rubble Slab B",
            new Vector3(0.19f, -0.40f, -0.12f),
            new Vector3(0.34f, 0.09f, 0.25f),
            rubble,
            Quaternion.Euler(-2f, -22f, -5f)
        );

        AddVisualCube(
            root,
            "Pipe",
            new Vector3(0.05f, -0.33f, 0.27f),
            new Vector3(0.45f, 0.09f, 0.09f),
            metal,
            Quaternion.Euler(0f, 31f, 0f)
        );
    }

    public static void StyleWorldPanel(
        GameObject root,
        Renderer background,
        TextMesh text,
        float width,
        float height,
        Color accent)
    {
        if (root == null)
            return;

        if (background != null)
        {
            background.sharedMaterial =
                WorldPanelMaterial();
        }

        if (text != null)
        {
            text.color =
                new Color(0.91f, 0.94f, 0.96f, 1f);

            text.fontSize = 45;
        }

        if (root.transform.Find("V45_Frame") != null)
            return;

        GameObject frame =
            new GameObject("V45_Frame");

        frame.transform.SetParent(
            root.transform,
            false
        );

        Material metal =
            new Material(MetalMaterial());

        metal.color =
            Color.Lerp(
                new Color(0.13f, 0.15f, 0.17f, 1f),
                accent,
                0.18f
            );

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        AddWorldCube(
            frame.transform,
            "Top Frame",
            new Vector3(0f, halfH, -0.13f),
            new Vector3(width + 0.16f, 0.15f, 0.12f),
            metal
        );

        AddWorldCube(
            frame.transform,
            "Bottom Frame",
            new Vector3(0f, -halfH, -0.13f),
            new Vector3(width + 0.16f, 0.15f, 0.12f),
            metal
        );

        AddWorldCube(
            frame.transform,
            "Left Frame",
            new Vector3(-halfW, 0f, -0.13f),
            new Vector3(0.15f, height, 0.12f),
            metal
        );

        AddWorldCube(
            frame.transform,
            "Right Frame",
            new Vector3(halfW, 0f, -0.13f),
            new Vector3(0.15f, height, 0.12f),
            metal
        );
    }

    public static void StyleObjectiveMarker(
        GameObject marker,
        Transform objectiveRoot,
        MissionObjectiveRole role)
    {
        if (marker == null ||
            objectiveRoot == null ||
            objectiveRoot.Find("V45_ObjectiveNode") != null)
        {
            return;
        }

        Renderer source =
            marker.GetComponent<Renderer>();

        if (source != null)
        {
            source.sharedMaterial =
                new Material(ObjectiveMaterial());

            source.sharedMaterial.color =
                new Color(0.62f, 0.68f, 0.70f, 1f);
        }

        GameObject nodeRoot =
            new GameObject("V45_ObjectiveNode");

        nodeRoot.transform.SetParent(
            objectiveRoot,
            false
        );

        Material accent =
            new Material(ObjectiveMaterial());

        List<Renderer> accents =
            new List<Renderer>();

        GameObject centre =
            AddWorldCylinder(
                nodeRoot.transform,
                "Objective Core",
                new Vector3(0f, 0.13f, 0f),
                new Vector3(0.78f, 0.12f, 0.78f),
                accent
            );

        accents.Add(
            centre.GetComponent<Renderer>()
        );

        GameObject pillar =
            AddWorldCylinder(
                nodeRoot.transform,
                "Objective Beacon",
                new Vector3(0f, 0.30f, 0f),
                new Vector3(0.23f, 0.27f, 0.23f),
                accent
            );

        accents.Add(
            pillar.GetComponent<Renderer>()
        );

        const int segments = 16;

        for (int i = 0; i < segments; i++)
        {
            float angle =
                i / (float)segments *
                Mathf.PI * 2f;

            float radius =
                ObjectiveController.ControlRadius *
                0.86f;

            Vector3 pos =
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.07f,
                    Mathf.Sin(angle) * radius
                );

            GameObject tick =
                AddWorldCube(
                    nodeRoot.transform,
                    "Objective Tick " + i,
                    pos,
                    new Vector3(0.38f, 0.07f, 0.14f),
                    accent
                );

            tick.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    -angle * Mathf.Rad2Deg,
                    0f
                );

            accents.Add(
                tick.GetComponent<Renderer>()
            );
        }

        WarboardV45ObjectivePulse pulse =
            objectiveRoot.gameObject
                .AddComponent<
                    WarboardV45ObjectivePulse
                >();

        pulse.Source = source;
        pulse.Accents =
            accents.ToArray();
    }

    private static GameObject AddVisualCube(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        Quaternion rotation)
    {
        GameObject go =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        go.transform.localRotation = rotation;

        Collider col =
            go.GetComponent<Collider>();

        if (col != null)
            UnityEngine.Object.Destroy(col);

        Renderer renderer =
            go.GetComponent<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;

        return go;
    }

    private static GameObject AddWorldCube(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        return AddVisualCube(
            parent,
            name,
            localPosition,
            localScale,
            material,
            Quaternion.identity
        );
    }

    private static GameObject AddWorldCylinder(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject go =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        Collider col =
            go.GetComponent<Collider>();

        if (col != null)
            UnityEngine.Object.Destroy(col);

        Renderer renderer =
            go.GetComponent<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;

        return go;
    }

    private static Material BoardMaterial()
    {
        if (boardMaterial == null)
        {
            boardMaterial =
                CreateMaterial(
                    "Warboard v45 Battlemat",
                    LoadTexture("board_mat"),
                    new Color(0.78f, 0.82f, 0.79f, 1f),
                    0.05f,
                    0.22f
                );
        }

        return boardMaterial;
    }

    private static Material ConcreteMaterial()
    {
        if (concreteMaterial == null)
        {
            concreteMaterial =
                CreateMaterial(
                    "Warboard v45 Concrete",
                    LoadTexture("concrete"),
                    new Color(0.84f, 0.84f, 0.81f, 1f),
                    0.02f,
                    0.18f
                );
        }

        return concreteMaterial;
    }

    private static Material MetalMaterial()
    {
        if (metalMaterial == null)
        {
            metalMaterial =
                CreateMaterial(
                    "Warboard v45 Industrial Metal",
                    LoadTexture("industrial_metal"),
                    new Color(0.78f, 0.82f, 0.83f, 1f),
                    0.58f,
                    0.36f
                );
        }

        return metalMaterial;
    }

    private static Material RubbleMaterial()
    {
        if (rubbleMaterial == null)
        {
            rubbleMaterial =
                CreateMaterial(
                    "Warboard v45 Rubble",
                    LoadTexture("rubble"),
                    new Color(0.88f, 0.83f, 0.73f, 1f),
                    0.01f,
                    0.14f
                );
        }

        return rubbleMaterial;
    }

    private static Material ObjectiveMaterial()
    {
        if (objectiveMaterial == null)
        {
            objectiveMaterial =
                CreateMaterial(
                    "Warboard v45 Objective",
                    LoadTexture("objective_node"),
                    new Color(0.78f, 0.88f, 0.90f, 1f),
                    0.34f,
                    0.55f
                );

            if (objectiveMaterial.HasProperty(
                    "_EmissionColor"))
            {
                objectiveMaterial.EnableKeyword(
                    "_EMISSION"
                );

                objectiveMaterial.SetColor(
                    "_EmissionColor",
                    new Color(
                        0.12f,
                        0.28f,
                        0.32f,
                        1f
                    )
                );
            }
        }

        return objectiveMaterial;
    }

    private static Material WorldPanelMaterial()
    {
        if (worldPanelMaterial == null)
        {
            worldPanelMaterial =
                CreateMaterial(
                    "Warboard v45 World Panel",
                    LoadTexture("industrial_metal"),
                    new Color(0.13f, 0.15f, 0.18f, 1f),
                    0.42f,
                    0.32f
                );
        }

        return worldPanelMaterial;
    }

    private static Texture2D LoadTexture(
        string name)
    {
        Texture2D texture =
            Resources.Load<Texture2D>(
                "WarboardV45/Textures/" +
                name
            );

        if (texture != null)
        {
            texture.wrapMode =
                TextureWrapMode.Repeat;

            texture.filterMode =
                FilterMode.Bilinear;

            return texture;
        }

        Texture2D fallback =
            new Texture2D(4, 4);

        fallback.wrapMode =
            TextureWrapMode.Repeat;

        Color[] pixels =
            new Color[16];

        for (int i = 0; i < pixels.Length; i++)
        {
            float value =
                0.50f +
                ((i % 3) - 1) *
                0.04f;

            pixels[i] =
                new Color(
                    value,
                    value,
                    value,
                    1f
                );
        }

        fallback.SetPixels(pixels);
        fallback.Apply();
        return fallback;
    }

    private static Material CreateMaterial(
        string name,
        Texture2D texture,
        Color tint,
        float metallic,
        float smoothness)
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
            shader = Shader.Find("Diffuse");

        Material material =
            new Material(shader);

        material.name = name;
        material.mainTexture = texture;
        material.color = tint;

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat(
                "_Metallic",
                metallic
            );
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat(
                "_Glossiness",
                smoothness
            );
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat(
                "_Smoothness",
                smoothness
            );
        }

        material.mainTextureScale =
            new Vector2(2.5f, 2.5f);

        return material;
    }
}

public sealed class WarboardV45ObjectivePulse :
    MonoBehaviour
{
    public Renderer Source;
    public Renderer[] Accents =
        new Renderer[0];

    private void LateUpdate()
    {
        if (Source == null)
            return;

        Color sourceColor =
            Source.material.color;

        float pulse =
            0.78f +
            0.22f *
            Mathf.Sin(
                Time.time * 2.8f
            );

        Color accentColor =
            Color.Lerp(
                sourceColor,
                Color.white,
                0.12f
            ) * pulse;

        foreach (Renderer renderer
            in Accents)
        {
            if (renderer == null)
                continue;

            Material material =
                renderer.sharedMaterial;

            if (material == null)
                continue;

            material.color =
                accentColor;

            if (material.HasProperty(
                    "_EmissionColor"))
            {
                material.EnableKeyword(
                    "_EMISSION"
                );

                material.SetColor(
                    "_EmissionColor",
                    accentColor * 0.85f
                );
            }
        }
    }
}
