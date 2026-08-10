using UnityEngine;

/// <summary>
/// WARBOARD v45.7a tabletop environment.
/// Adds a large wood-style table underneath the battlefield,
/// reserve/destroyed trays, scoreboard area and world dice tray.
/// </summary>
public class WarboardV45EnvironmentTable :
    MonoBehaviour
{
    private bool built;

    private void Start()
    {
        TryBuild();
    }

    private void Update()
    {
        if (!built)
            TryBuild();
    }

    private void TryBuild()
    {
        GameObject board =
            GameObject.Find("Board");

        if (board == null)
            return;

        GameObject existing =
            GameObject.Find(
                "Warboard Wood Table"
            );

        if (existing != null)
        {
            built = true;
            return;
        }

        float boardWidth =
            board.transform.localScale.x;

        float boardDepth =
            board.transform.localScale.z;

        float width =
            boardWidth + 34f;

        float depth =
            boardDepth + 22f;

        GameObject root =
            new GameObject(
                "Warboard Wood Table"
            );

        Material woodA =
            CreateMaterial(
                new Color(
                    0.40f,
                    0.245f,
                    0.125f,
                    1f
                ),
                0.08f,
                0.24f
            );

        Material woodB =
            CreateMaterial(
                new Color(
                    0.47f,
                    0.30f,
                    0.16f,
                    1f
                ),
                0.08f,
                0.22f
            );

        Material darkWood =
            CreateMaterial(
                new Color(
                    0.20f,
                    0.105f,
                    0.050f,
                    1f
                ),
                0.10f,
                0.20f
            );

        float tableY = -0.70f;
        float centreZ = -2.8f;

        AddBox(
            root.transform,
            "Tabletop Base",
            new Vector3(
                0f,
                tableY,
                centreZ
            ),
            new Vector3(
                width,
                0.46f,
                depth
            ),
            woodA
        );

        // Long plank strips so the surface reads as wood from the game camera.
        int plankCount = 18;

        float plankWidth =
            width / plankCount;

        for (int i = 0;
             i < plankCount;
             i++)
        {
            float x =
                -width * 0.5f +
                plankWidth * 0.5f +
                plankWidth * i;

            AddBox(
                root.transform,
                "Wood Plank " + i,
                new Vector3(
                    x,
                    tableY + 0.245f,
                    centreZ
                ),
                new Vector3(
                    plankWidth - 0.045f,
                    0.035f,
                    depth - 0.22f
                ),
                i % 2 == 0
                ? woodA
                : woodB
            );
        }

        // Dark wooden outer rim.
        const float rim = 0.34f;
        const float rimHeight = 0.26f;

        AddBox(
            root.transform,
            "Front Rim",
            new Vector3(
                0f,
                tableY + 0.25f,
                centreZ -
                    depth * 0.5f +
                    rim * 0.5f
            ),
            new Vector3(
                width,
                rimHeight,
                rim
            ),
            darkWood
        );

        AddBox(
            root.transform,
            "Rear Rim",
            new Vector3(
                0f,
                tableY + 0.25f,
                centreZ +
                    depth * 0.5f -
                    rim * 0.5f
            ),
            new Vector3(
                width,
                rimHeight,
                rim
            ),
            darkWood
        );

        AddBox(
            root.transform,
            "Left Rim",
            new Vector3(
                -width * 0.5f +
                    rim * 0.5f,
                tableY + 0.25f,
                centreZ
            ),
            new Vector3(
                rim,
                rimHeight,
                depth
            ),
            darkWood
        );

        AddBox(
            root.transform,
            "Right Rim",
            new Vector3(
                width * 0.5f -
                    rim * 0.5f,
                tableY + 0.25f,
                centreZ
            ),
            new Vector3(
                rim,
                rimHeight,
                depth
            ),
            darkWood
        );

        built = true;
    }

    private static GameObject AddBox(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
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

        go.transform.position =
            position;

        go.transform.localScale =
            scale;

        Collider collider =
            go.GetComponent<Collider>();

        if (collider != null)
            Object.Destroy(collider);

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

        return material;
    }
}
