using System;
using UnityEngine;

public static class WarboardVisualTheme
{
    private static bool guiTexturesReady;
    private static Texture2D buttonNormal;
    private static Texture2D buttonHover;
    private static Texture2D buttonActive;
    private static Texture2D buttonDisabled;
    private static Texture2D textFieldBackground;
    private static Texture2D selectionBackground;
    private static Texture2D boardTexture;

    private static readonly Color Text =
        new Color(0.91f, 0.93f, 0.96f, 1f);

    private static readonly Color MutedText =
        new Color(0.66f, 0.70f, 0.76f, 1f);

    public static void ApplyGUITheme()
    {
        EnsureGuiTextures();

        GUISkin skin = GUI.skin;
        if (skin == null)
            return;

        skin.label.normal.textColor = Text;
        skin.label.fontSize = 12;
        skin.label.padding =
            new RectOffset(3, 3, 2, 2);

        ConfigureButton(skin.button);

        if (skin.textField != null)
        {
            skin.textField.normal.background =
                textFieldBackground;
            skin.textField.focused.background =
                selectionBackground;
            skin.textField.hover.background =
                selectionBackground;

            skin.textField.normal.textColor = Text;
            skin.textField.focused.textColor = Color.white;
            skin.textField.hover.textColor = Color.white;
            skin.textField.fontSize = 12;
            skin.textField.padding =
                new RectOffset(8, 8, 5, 5);
            skin.textField.border =
                new RectOffset(6, 6, 6, 6);
        }

        if (skin.textArea != null)
        {
            skin.textArea.normal.background =
                textFieldBackground;
            skin.textArea.focused.background =
                selectionBackground;
            skin.textArea.normal.textColor = Text;
            skin.textArea.focused.textColor = Color.white;
            skin.textArea.padding =
                new RectOffset(8, 8, 6, 6);
            skin.textArea.border =
                new RectOffset(6, 6, 6, 6);
        }

        if (skin.toggle != null)
        {
            skin.toggle.normal.textColor = Text;
            skin.toggle.hover.textColor = Color.white;
            skin.toggle.onNormal.textColor = Color.white;
            skin.toggle.onHover.textColor = Color.white;
        }
    }

    private static void ConfigureButton(
        GUIStyle style)
    {
        if (style == null)
            return;

        style.normal.background = buttonNormal;
        style.hover.background = buttonHover;
        style.active.background = buttonActive;
        style.focused.background = buttonHover;

        style.onNormal.background = buttonActive;
        style.onHover.background = buttonHover;
        style.onActive.background = buttonActive;
        style.onFocused.background = buttonHover;

        style.normal.textColor = Text;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.focused.textColor = Color.white;

        style.onNormal.textColor = Color.white;
        style.onHover.textColor = Color.white;
        style.onActive.textColor = Color.white;
        style.onFocused.textColor = Color.white;

        style.fontSize = 12;
        style.fontStyle = FontStyle.Normal;
        style.alignment = TextAnchor.MiddleCenter;

        style.padding =
            new RectOffset(9, 9, 5, 5);

        style.margin =
            new RectOffset(2, 2, 2, 2);

        style.border =
            new RectOffset(6, 6, 6, 6);
    }

    private static void EnsureGuiTextures()
    {
        if (guiTexturesReady)
            return;

        guiTexturesReady = true;

        buttonNormal =
            MakePanelTexture(
                new Color(0.105f, 0.120f, 0.145f, 0.98f),
                new Color(0.22f, 0.25f, 0.30f, 1f)
            );

        buttonHover =
            MakePanelTexture(
                new Color(0.15f, 0.18f, 0.22f, 1f),
                new Color(0.42f, 0.48f, 0.56f, 1f)
            );

        buttonActive =
            MakePanelTexture(
                new Color(0.17f, 0.22f, 0.27f, 1f),
                new Color(0.58f, 0.68f, 0.78f, 1f)
            );

        buttonDisabled =
            MakePanelTexture(
                new Color(0.07f, 0.075f, 0.085f, 0.9f),
                new Color(0.13f, 0.14f, 0.16f, 0.9f)
            );

        textFieldBackground =
            MakePanelTexture(
                new Color(0.035f, 0.043f, 0.055f, 1f),
                new Color(0.18f, 0.20f, 0.24f, 1f)
            );

        selectionBackground =
            MakePanelTexture(
                new Color(0.055f, 0.075f, 0.095f, 1f),
                new Color(0.34f, 0.44f, 0.54f, 1f)
            );

        // Disabled state has to be assigned after the texture exists.
        if (GUI.skin != null &&
            GUI.skin.button != null)
        {
            GUI.skin.button.normal.background =
                buttonNormal;
        }
    }

    private static Texture2D MakePanelTexture(
        Color fill,
        Color edge)
    {
        const int size = 16;

        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );

        texture.name =
            "Warboard UI Surface";

        texture.wrapMode =
            TextureWrapMode.Clamp;

        texture.filterMode =
            FilterMode.Bilinear;

        Color[] pixels =
            new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border =
                    x == 0 ||
                    y == 0 ||
                    x == size - 1 ||
                    y == size - 1;

                float vertical =
                    y / (float)(size - 1);

                Color shaded =
                    Color.Lerp(
                        fill * 0.88f,
                        fill * 1.08f,
                        vertical
                    );

                shaded.a = fill.a;

                pixels[
                    y * size + x
                ] =
                    border
                    ? edge
                    : shaded;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        return texture;
    }

    public static void StyleCamera(
        Camera camera)
    {
        if (camera == null)
            return;

        camera.backgroundColor =
            new Color(
                0.018f,
                0.022f,
                0.030f,
                1f
            );

        camera.allowHDR = true;
        camera.allowMSAA = true;
        camera.fieldOfView = 43f;
        camera.nearClipPlane = 0.08f;
        camera.farClipPlane = 500f;
    }

    public static void StyleLight(
        Light key)
    {
        if (key == null)
            return;

        RenderSettings.ambientLight =
            new Color(
                0.28f,
                0.30f,
                0.34f,
                1f
            );

        key.color =
            new Color(
                1.0f,
                0.94f,
                0.86f,
                1f
            );

        key.intensity = 1.18f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.72f;
        key.bounceIntensity = 0.82f;

        GameObject existing =
            GameObject.Find(
                "Warboard Rim Light"
            );

        if (existing == null)
        {
            GameObject rimObject =
                new GameObject(
                    "Warboard Rim Light"
                );

            Light rim =
                rimObject.AddComponent<Light>();

            rim.type = LightType.Directional;
            rim.intensity = 0.34f;
            rim.color =
                new Color(
                    0.58f,
                    0.72f,
                    1.0f,
                    1f
                );

            rim.shadows =
                LightShadows.None;

            rim.transform.rotation =
                Quaternion.Euler(
                    35f,
                    145f,
                    0f
                );
        }
    }

    public static void StyleBoard(
        GameObject board)
    {
        if (board == null)
            return;

        Renderer renderer =
            board.GetComponent<Renderer>();

        if (renderer == null)
            return;

        if (boardTexture == null)
            boardTexture =
                CreateBattleMatTexture();

        Material material =
            CreateLitMaterial(
                new Color(
                    0.24f,
                    0.27f,
                    0.255f,
                    1f
                )
            );

        material.name =
            "Warboard Battle Mat";

        material.mainTexture =
            boardTexture;

        material.mainTextureScale =
            new Vector2(
                Mathf.Max(
                    2f,
                    board.transform
                        .localScale.x /
                    8f
                ),
                Mathf.Max(
                    2f,
                    board.transform
                        .localScale.z /
                    8f
                )
            );

        SetSmoothness(
            material,
            0.15f
        );

        renderer.sharedMaterial =
            material;

        AddBoardEdge(
            board,
            "North",
            new Vector3(
                0f,
                0.55f,
                0.497f
            ),
            new Vector3(
                1.006f,
                0.18f,
                0.010f
            )
        );

        AddBoardEdge(
            board,
            "South",
            new Vector3(
                0f,
                0.55f,
                -0.497f
            ),
            new Vector3(
                1.006f,
                0.18f,
                0.010f
            )
        );

        AddBoardEdge(
            board,
            "East",
            new Vector3(
                0.497f,
                0.55f,
                0f
            ),
            new Vector3(
                0.008f,
                0.18f,
                1.0f
            )
        );

        AddBoardEdge(
            board,
            "West",
            new Vector3(
                -0.497f,
                0.55f,
                0f
            ),
            new Vector3(
                0.008f,
                0.18f,
                1.0f
            )
        );
    }

    private static Texture2D
        CreateBattleMatTexture()
    {
        const int size = 128;

        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGB24,
                false
            );

        texture.name =
            "Warboard Procedural Battle Mat";

        texture.wrapMode =
            TextureWrapMode.Repeat;

        texture.filterMode =
            FilterMode.Bilinear;

        System.Random random =
            new System.Random(4400);

        Color[] pixels =
            new Color[size * size];

        for (int y = 0;
             y < size;
             y++)
        {
            for (int x = 0;
                 x < size;
                 x++)
            {
                float noise =
                    ((float)random.NextDouble() -
                     0.5f) *
                    0.065f;

                float broad =
                    Mathf.Sin(
                        x * 0.17f +
                        y * 0.11f
                    ) *
                    0.018f;

                Color color =
                    new Color(
                        0.245f +
                            noise +
                            broad,
                        0.270f +
                            noise +
                            broad,
                        0.255f +
                            noise *
                            0.8f,
                        1f
                    );

                bool seam =
                    (x % 32 == 0) ||
                    (y % 32 == 0);

                if (seam)
                {
                    color *= 0.92f;
                    color.a = 1f;
                }

                pixels[
                    y * size + x
                ] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        return texture;
    }

    private static void AddBoardEdge(
        GameObject board,
        string suffix,
        Vector3 localPosition,
        Vector3 localScale)
    {
        if (board.transform.Find(
                "Board Trim " +
                suffix) != null)
        {
            return;
        }

        GameObject edge =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        edge.name =
            "Board Trim " +
            suffix;

        edge.transform.SetParent(
            board.transform,
            false
        );

        edge.transform.localPosition =
            localPosition;

        edge.transform.localScale =
            localScale;

        Collider collider =
            edge.GetComponent<Collider>();

        if (collider != null)
            UnityEngine.Object.Destroy(
                collider
            );

        Renderer renderer =
            edge.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material material =
                CreateLitMaterial(
                    new Color(
                        0.045f,
                        0.050f,
                        0.060f,
                        1f
                    )
                );

            SetSmoothness(
                material,
                0.28f
            );

            renderer.sharedMaterial =
                material;
        }
    }

    public static void StyleTerrain(
        GameObject terrain,
        TerrainTrait trait,
        Color sourceColor)
    {
        if (terrain == null)
            return;

        Color baseColor;

        switch (trait)
        {
            case TerrainTrait.Blocking:
                baseColor =
                    new Color(
                        0.25f,
                        0.28f,
                        0.32f,
                        1f
                    );
                break;

            case TerrainTrait.Cover:
                baseColor =
                    new Color(
                        0.39f,
                        0.36f,
                        0.29f,
                        1f
                    );
                break;

            default:
                baseColor =
                    new Color(
                        0.25f,
                        0.34f,
                        0.29f,
                        1f
                    );
                break;
        }

        // Preserve a little of the mission layout's original differentiation.
        baseColor =
            Color.Lerp(
                baseColor,
                sourceColor,
                0.18f
            );

        baseColor.a = 1f;

        Renderer renderer =
            terrain.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material material =
                CreateLitMaterial(
                    baseColor
                );

            material.name =
                "Warboard Terrain " +
                trait;

            SetSmoothness(
                material,
                0.18f
            );

            renderer.sharedMaterial =
                material;
        }

        AddTerrainDetail(
            terrain,
            "Top Cap",
            new Vector3(
                0f,
                0.515f,
                0f
            ),
            new Vector3(
                1.02f,
                0.045f,
                1.02f
            ),
            Color.Lerp(
                baseColor,
                Color.white,
                0.10f
            )
        );

        AddTerrainDetail(
            terrain,
            "Front Trim",
            new Vector3(
                0f,
                0f,
                -0.505f
            ),
            new Vector3(
                0.92f,
                0.84f,
                0.028f
            ),
            baseColor * 0.72f
        );

        AddTerrainDetail(
            terrain,
            "Left Trim",
            new Vector3(
                -0.505f,
                0f,
                0f
            ),
            new Vector3(
                0.028f,
                0.84f,
                0.92f
            ),
            baseColor * 0.68f
        );

        if (trait ==
            TerrainTrait.Blocking)
        {
            AddTerrainDetail(
                terrain,
                "Buttress A",
                new Vector3(
                    0.39f,
                    -0.15f,
                    0.39f
                ),
                new Vector3(
                    0.12f,
                    0.65f,
                    0.12f
                ),
                baseColor * 0.62f
            );

            AddTerrainDetail(
                terrain,
                "Buttress B",
                new Vector3(
                    -0.39f,
                    -0.15f,
                    -0.39f
                ),
                new Vector3(
                    0.12f,
                    0.65f,
                    0.12f
                ),
                baseColor * 0.62f
            );
        }
    }

    private static void AddTerrainDetail(
        GameObject parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Color color)
    {
        if (parent.transform.Find(name) != null)
            return;

        GameObject detail =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        detail.name = name;

        detail.transform.SetParent(
            parent.transform,
            false
        );

        detail.transform.localPosition =
            localPosition;

        detail.transform.localScale =
            localScale;

        Collider collider =
            detail.GetComponent<Collider>();

        if (collider != null)
            UnityEngine.Object.Destroy(
                collider
            );

        Renderer renderer =
            detail.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material material =
                CreateLitMaterial(
                    color
                );

            SetSmoothness(
                material,
                0.22f
            );

            renderer.sharedMaterial =
                material;
        }
    }

    public static void StyleObjective(
        Transform objectiveRoot,
        GameObject core,
        Renderer coreRenderer,
        TextMesh text,
        float radius)
    {
        if (objectiveRoot == null)
            return;

        if (core != null)
        {
            core.transform.localPosition =
                new Vector3(
                    0f,
                    0.055f,
                    0f
                );

            core.transform.localScale =
                new Vector3(
                    0.72f,
                    0.022f,
                    0.72f
                );
        }

        if (coreRenderer != null)
        {
            Material material =
                CreateUnlitMaterial(
                    new Color(
                        0.88f,
                        0.72f,
                        0.22f,
                        0.92f
                    )
                );

            material.name =
                "Warboard Objective Core";

            coreRenderer.sharedMaterial =
                material;
        }

        Transform existing =
            objectiveRoot.Find(
                "Objective Control Ring"
            );

        if (existing == null)
        {
            GameObject ringObject =
                new GameObject(
                    "Objective Control Ring"
                );

            ringObject.transform.SetParent(
                objectiveRoot,
                false
            );

            ringObject.transform.localPosition =
                new Vector3(
                    0f,
                    0.065f,
                    0f
                );

            LineRenderer line =
                ringObject.AddComponent<
                    LineRenderer
                >();

            line.loop = true;
            line.useWorldSpace = false;
            line.positionCount = 72;
            line.widthMultiplier = 0.085f;

            Shader shader =
                Shader.Find(
                    "Sprites/Default"
                );

            if (shader != null)
            {
                line.sharedMaterial =
                    new Material(shader);
            }

            Color ringColor =
                new Color(
                    0.84f,
                    0.72f,
                    0.32f,
                    0.78f
                );

            line.startColor = ringColor;
            line.endColor = ringColor;

            for (int i = 0;
                 i < line.positionCount;
                 i++)
            {
                float angle =
                    i /
                    (float)line.positionCount *
                    Mathf.PI *
                    2f;

                line.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(angle) *
                            radius,
                        0f,
                        Mathf.Sin(angle) *
                            radius
                    )
                );
            }
        }

        if (text != null)
        {
            text.fontSize = 38;
            text.characterSize = 0.032f;
            text.color =
                new Color(
                    0.92f,
                    0.94f,
                    0.97f,
                    0.92f
                );

            text.transform.localPosition =
                new Vector3(
                    0f,
                    0.24f,
                    0f
                );
        }
    }

    public static void StyleWorldPanel(
        Renderer renderer,
        Color color)
    {
        if (renderer == null)
            return;

        bool accent =
            renderer.gameObject.name
                .IndexOf(
                    "Accent",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0;

        Material material =
            renderer.sharedMaterial;

        if (material == null ||
            material.name == null ||
            !material.name.StartsWith(
                "Warboard World Panel",
                StringComparison.Ordinal))
        {
            material =
                CreateLitMaterial(
                    color
                );

            material.name =
                accent
                ? "Warboard World Panel Accent"
                : "Warboard World Panel Background";

            renderer.sharedMaterial =
                material;
        }

        Color finalColor =
            accent
            ? Color.Lerp(
                color,
                Color.white,
                0.08f
              )
            : Color.Lerp(
                new Color(
                    0.022f,
                    0.027f,
                    0.038f,
                    1f
                ),
                color,
                0.10f
              );

        finalColor.a = 1f;

        SetMaterialColor(
            material,
            finalColor
        );

        SetSmoothness(
            material,
            accent ? 0.42f : 0.22f
        );

        if (accent &&
            material.HasProperty(
                "_EmissionColor"))
        {
            material.EnableKeyword(
                "_EMISSION"
            );

            material.SetColor(
                "_EmissionColor",
                finalColor * 1.5f
            );
        }
    }

    private static Material
        CreateLitMaterial(
            Color color)
    {
        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Lit"
            );

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Standard"
                );
        }

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Legacy Shaders/Diffuse"
                );
        }

        Material material =
            new Material(shader);

        SetMaterialColor(
            material,
            color
        );

        if (material.HasProperty(
                "_Metallic"))
        {
            material.SetFloat(
                "_Metallic",
                0f
            );
        }

        return material;
    }

    private static Material
        CreateUnlitMaterial(
            Color color)
    {
        Shader shader =
            Shader.Find(
                "Sprites/Default"
            );

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );
        }

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Unlit/Color"
                );
        }

        if (shader == null)
        {
            return
                CreateLitMaterial(
                    color
                );
        }

        Material material =
            new Material(shader);

        SetMaterialColor(
            material,
            color
        );

        return material;
    }

    private static void SetMaterialColor(
        Material material,
        Color color)
    {
        if (material == null)
            return;

        material.color = color;

        if (material.HasProperty(
                "_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                color
            );
        }

        if (material.HasProperty(
                "_Color"))
        {
            material.SetColor(
                "_Color",
                color
            );
        }
    }

    private static void SetSmoothness(
        Material material,
        float value)
    {
        if (material == null)
            return;

        if (material.HasProperty(
                "_Smoothness"))
        {
            material.SetFloat(
                "_Smoothness",
                value
            );
        }

        if (material.HasProperty(
                "_Glossiness"))
        {
            material.SetFloat(
                "_Glossiness",
                value
            );
        }
    }
}

