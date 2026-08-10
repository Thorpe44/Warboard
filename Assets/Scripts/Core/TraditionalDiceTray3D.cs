using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TraditionalDiceMarker : MonoBehaviour
{
    public int Id;
    public int Sides;
    public Renderer Renderer;
    public Rigidbody Body;
    public bool Selected;
    public Vector3[] FaceNormals;
    public int[] FaceValues;

    private Color baseColor =
        new Color(
            0.92f,
            0.92f,
            0.94f,
            1f
        );

    public void Configure(
        int sides,
        Vector3[] normals,
        int[] values,
        Color color)
    {
        Sides = sides;
        FaceNormals = normals;
        FaceValues = values;
        baseColor = color;

        if (Renderer != null)
            Renderer.material.color = baseColor;
    }

    public void SetSelected(
        bool selected)
    {
        Selected = selected;

        if (Renderer == null)
            return;

        Renderer.material.color =
            selected
            ? new Color(
                1.00f,
                0.78f,
                0.22f,
                1f
              )
            : baseColor;
    }

    public int TopValue()
    {
        if (FaceNormals == null ||
            FaceValues == null ||
            FaceNormals.Length == 0 ||
            FaceValues.Length !=
                FaceNormals.Length)
        {
            return 1;
        }

        int bestValue =
            FaceValues[0];

        float bestDot = -2f;

        for (int i = 0;
             i < FaceNormals.Length;
             i++)
        {
            float dot =
                Vector3.Dot(
                    transform.TransformDirection(
                        FaceNormals[i]
                    ),
                    Vector3.up
                );

            if (dot > bestDot)
            {
                bestDot = dot;
                bestValue =
                    FaceValues[i];
            }
        }

        return bestValue;
    }
}

public class TraditionalDiceTray3D : MonoBehaviour
{
    private class DieGeometry
    {
        public Mesh Mesh;
        public Vector3[] FaceNormals;
        public Vector3[] FaceCenters;
        public int[] FaceValues;
    }

    private const int DiceLayer = 30;
    private const int MaxDice = 40;

    private static readonly int[] SupportedSides =
    {
        3,
        4,
        6,
        8,
        10,
        12,
        20
    };

    private Vector3 trayOrigin =
        new Vector3(
            4000f,
            -5000f,
            4000f
        );

    private readonly List<TraditionalDiceMarker>
        dice =
            new List<TraditionalDiceMarker>();

    private readonly Dictionary<int, int>
        requestedPool =
            new Dictionary<int, int>();

    private readonly Dictionary<int, DieGeometry>
        geometryCache =
            new Dictionary<int, DieGeometry>();

    private GameController game;
    private Camera trayCamera;
    private RenderTexture renderTexture;
    private GameObject trayRoot;
    private PhysicsMaterial dicePhysics;
    private bool worldSpaceMode;

    private int selectedSides = 6;
    private int nextDieId = 1;
    private bool rollInProgress;
    private bool rollLogged;
    private float settledSince = -1f;
    private string settledText = "Tray empty";

    public void Initialize(
        GameController owner)
    {
        game = owner;

        EnsurePoolInitialized();
        EnsureBuilt();
    }

    public void SetWorldSpaceMode(
        bool enabled)
    {
        EnsureBuilt();

        worldSpaceMode = enabled;

        if (trayRoot == null)
            return;

        if (!enabled)
        {
            trayRoot.SetActive(false);
            return;
        }

        GameObject board =
            GameObject.Find("Board");

        if (board == null)
            return;

        float boardDepth =
            board.transform.localScale.z;

        trayOrigin =
            new Vector3(
                board.transform.position.x,
                0.055f,
                board.transform.position.z -
                    boardDepth * 0.5f -
                    4.25f
            );

        trayRoot.SetActive(true);

        trayRoot.transform.position =
            trayOrigin;

        trayRoot.transform.localScale =
            new Vector3(
                1.55f,
                1.0f,
                0.58f
            );

        Camera main =
            Camera.main;

        if (main != null)
        {
            main.cullingMask |=
                1 << DiceLayer;
        }
    }



    private void EnsurePoolInitialized()
    {
        foreach (int sides
            in SupportedSides)
        {
            if (!requestedPool.ContainsKey(
                    sides))
            {
                requestedPool[
                    sides] = 0;
            }
        }

        if (RequestedPoolTotal() == 0)
        {
            requestedPool[6] = 6;
        }
    }

    public void SetRequestedDiceCount(
        int count)
    {
        EnsurePoolInitialized();

        foreach (int sides
            in SupportedSides)
        {
            requestedPool[sides] = 0;
        }

        selectedSides = 6;

        requestedPool[6] =
            Mathf.Clamp(
                count,
                1,
                MaxDice
            );
    }

    public void SetRequestedDicePool(
        int sides,
        int count)
    {
        EnsurePoolInitialized();

        if (!requestedPool.ContainsKey(
                sides))
        {
            sides = 6;
        }

        foreach (int key
            in SupportedSides)
        {
            requestedPool[key] = 0;
        }

        selectedSides = sides;

        requestedPool[sides] =
            Mathf.Clamp(
                count,
                1,
                MaxDice
            );
    }

    public int RequestedDiceCount
    {
        get
        {
            EnsurePoolInitialized();

            return requestedPool[
                selectedSides
            ];
        }
    }

    private int RequestedPoolTotal()
    {
        return requestedPool.Values.Sum();
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (trayRoot != null)
            Destroy(trayRoot);

        foreach (DieGeometry geometry
            in geometryCache.Values)
        {
            if (geometry != null &&
                geometry.Mesh != null)
            {
                Destroy(
                    geometry.Mesh
                );
            }
        }

        geometryCache.Clear();
    }

    private Shader PreferredShader()
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
                    "Sprites/Default"
                );
        }

        return shader;
    }

    private Material NewMaterial(
        Color color)
    {
        Shader shader =
            PreferredShader();

        Material material =
            shader != null
            ? new Material(shader)
            : null;

        if (material != null)
            material.color = color;

        return material;
    }

    private void EnsureBuilt()
    {
        if (trayRoot != null)
            return;

        EnsurePoolInitialized();

        trayRoot =
            new GameObject(
                "Traditional 3D Polyhedral Dice Tray"
            );

        trayRoot.transform.position =
            trayOrigin;

        SetLayerRecursive(
            trayRoot,
            DiceLayer
        );

        renderTexture =
            new RenderTexture(
                760,
                440,
                24,
                RenderTextureFormat.ARGB32
            );

        renderTexture.name =
            "Warboard Traditional Polyhedral Dice Tray";

        renderTexture.Create();

        GameObject cameraObject =
            new GameObject(
                "Dice Tray Camera"
            );

        cameraObject.transform.SetParent(
            trayRoot.transform,
            false
        );

        trayCamera =
            cameraObject.AddComponent<Camera>();

        trayCamera.clearFlags =
            CameraClearFlags.SolidColor;

        trayCamera.backgroundColor =
            new Color(
                0.025f,
                0.03f,
                0.04f,
                1f
            );

        trayCamera.fieldOfView = 47f;
        trayCamera.nearClipPlane = 0.1f;
        trayCamera.farClipPlane = 80f;
        trayCamera.cullingMask =
            1 << DiceLayer;

        trayCamera.targetTexture =
            renderTexture;

        cameraObject.transform.position =
            trayOrigin +
            new Vector3(
                0f,
                10.8f,
                -12.6f
            );

        cameraObject.transform.LookAt(
            trayOrigin +
            new Vector3(
                0f,
                0.65f,
                0f
            )
        );

        GameObject lightObject =
            new GameObject(
                "Dice Tray Light"
            );

        lightObject.transform.SetParent(
            trayRoot.transform,
            false
        );

        Light light =
            lightObject.AddComponent<Light>();

        light.type = LightType.Directional;
        light.intensity = 1.7f;
        light.cullingMask =
            1 << DiceLayer;

        lightObject.transform.rotation =
            Quaternion.Euler(
                46f,
                -28f,
                0f
            );

        Material trayMaterial =
            NewMaterial(
                new Color(
                    0.12f,
                    0.15f,
                    0.17f,
                    1f
                )
            );

        CreateTrayPart(
            "Floor",
            new Vector3(
                0f,
                -0.35f,
                0f
            ),
            new Vector3(
                13.2f,
                0.5f,
                7.8f
            ),
            trayMaterial
        );

        CreateTrayPart(
            "Left Wall",
            new Vector3(
                -6.70f,
                0.55f,
                0f
            ),
            new Vector3(
                0.4f,
                2.1f,
                8.2f
            ),
            trayMaterial
        );

        CreateTrayPart(
            "Right Wall",
            new Vector3(
                6.70f,
                0.55f,
                0f
            ),
            new Vector3(
                0.4f,
                2.1f,
                8.2f
            ),
            trayMaterial
        );

        CreateTrayPart(
            "Near Wall",
            new Vector3(
                0f,
                0.55f,
                -3.95f
            ),
            new Vector3(
                13.5f,
                2.1f,
                0.4f
            ),
            trayMaterial
        );

        CreateTrayPart(
            "Far Wall",
            new Vector3(
                0f,
                0.55f,
                3.95f
            ),
            new Vector3(
                13.5f,
                2.1f,
                0.4f
            ),
            trayMaterial
        );

        dicePhysics =
            new PhysicsMaterial(
                "Traditional Dice Physics"
            );

        dicePhysics.dynamicFriction = 0.55f;
        dicePhysics.staticFriction = 0.60f;
        dicePhysics.bounciness = 0.22f;
        dicePhysics.frictionCombine =
            PhysicsMaterialCombine.Average;
        dicePhysics.bounceCombine =
            PhysicsMaterialCombine.Average;
    }

    private void CreateTrayPart(
        string label,
        Vector3 localPosition,
        Vector3 scale,
        Material material)
    {
        GameObject part =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        part.name = label;
        part.transform.SetParent(
            trayRoot.transform,
            false
        );
        part.transform.localPosition =
            localPosition;
        part.transform.localScale = scale;

        SetLayerRecursive(
            part,
            DiceLayer
        );

        Renderer renderer =
            part.GetComponent<Renderer>();

        if (renderer != null &&
            material != null)
        {
            renderer.material =
                new Material(material);
        }
    }

    private void SetLayerRecursive(
        GameObject value,
        int layer)
    {
        if (value == null)
            return;

        value.layer = layer;

        foreach (Transform child
            in value.transform)
        {
            SetLayerRecursive(
                child.gameObject,
                layer
            );
        }
    }

    private void Update()
    {
        if (worldSpaceMode)
            HandleWorldDiceClick();

        if (!rollInProgress ||
            dice.Count == 0)
        {
            return;
        }

        bool allSlow =
            dice.All(
                die =>
                    die != null &&
                    die.Body != null &&
                    (die.Body.IsSleeping() ||
                     (die.Body.linearVelocity.sqrMagnitude <
                          0.018f &&
                      die.Body.angularVelocity.sqrMagnitude <
                          0.035f))
            );

        if (!allSlow)
        {
            settledSince = -1f;
            return;
        }

        if (settledSince < 0f)
        {
            settledSince =
                Time.unscaledTime;
            return;
        }

        if (Time.unscaledTime -
            settledSince < 0.45f)
        {
            return;
        }

        rollInProgress = false;
        RefreshSettledText();

        if (!rollLogged &&
            game != null)
        {
            rollLogged = true;

            game.AppendBattleLog(
                "DICE",
                "Traditional free dice roll",
                settledText
            );
        }
    }


    private void HandleWorldDiceClick()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Camera main =
            Camera.main;

        if (main == null)
            return;

        Ray ray =
            main.ScreenPointToRay(
                Input.mousePosition
            );

        RaycastHit hit;

        if (!Physics.Raycast(
                ray,
                out hit,
                250f,
                1 << DiceLayer))
        {
            return;
        }

        TraditionalDiceMarker marker =
            hit.collider
                .GetComponentInParent<
                    TraditionalDiceMarker
                >();

        if (marker != null)
        {
            marker.SetSelected(
                !marker.Selected
            );
        }
    }
    private void RefreshSettledText()
    {
        if (dice.Count == 0)
        {
            settledText =
                "Tray empty";
            return;
        }

        List<string> groups =
            new List<string>();

        foreach (IGrouping<int, TraditionalDiceMarker>
            group
            in dice
                .Where(
                    die => die != null
                )
                .OrderBy(
                    die => die.Id
                )
                .GroupBy(
                    die => die.Sides
                )
                .OrderBy(
                    group => group.Key
                ))
        {
            int[] results =
                group
                    .OrderBy(
                        die => die.Id
                    )
                    .Select(
                        die => die.TopValue()
                    )
                    .ToArray();

            groups.Add(
                results.Length +
                "D" +
                group.Key +
                "  ->  [" +
                string.Join(
                    ", ",
                    results
                        .Select(
                            value =>
                                value.ToString()
                        )
                        .ToArray()
                ) +
                "]"
            );
        }

        settledText =
            string.Join(
                "  |  ",
                groups.ToArray()
            );
    }

    private void ClearDice()
    {
        foreach (TraditionalDiceMarker die
            in dice)
        {
            if (die != null)
                Destroy(die.gameObject);
        }

        dice.Clear();
        rollInProgress = false;
        rollLogged = false;
        settledSince = -1f;
        settledText = "Tray empty";
    }

    private string PoolSummary()
    {
        List<string> parts =
            new List<string>();

        foreach (int sides
            in SupportedSides)
        {
            int count =
                requestedPool[
                    sides
                ];

            if (count <= 0)
                continue;

            parts.Add(
                count +
                "D" +
                sides
            );
        }

        return parts.Count == 0
            ? "No dice"
            : string.Join(
                " + ",
                parts.ToArray()
            );
    }

    private void RollAll()
    {
        EnsureBuilt();
        EnsurePoolInitialized();

        int total =
            RequestedPoolTotal();

        if (total <= 0)
        {
            settledText =
                "Add at least one die to the pool.";
            return;
        }

        ClearDice();

        int index = 0;

        foreach (int sides
            in SupportedSides)
        {
            int count =
                requestedPool[
                    sides
                ];

            for (int i = 0;
                 i < count;
                 i++)
            {
                SpawnDie(
                    sides,
                    index++
                );
            }
        }

        rollInProgress = true;
        rollLogged = false;
        settledSince = -1f;
        settledText =
            PoolSummary() +
            " rolling...";
    }

    private Color DieColor(
        int sides)
    {
        return new Color(
            0.92f,
            0.92f,
            0.94f,
            1f
        );
    }

    private void SpawnDie(
        int sides,
        int index)
    {
        DieGeometry geometry =
            GetGeometry(
                sides
            );

        if (geometry == null ||
            geometry.Mesh == null)
        {
            return;
        }

        GameObject dieObject =
            new GameObject(
                "D" +
                sides +
                " #" +
                nextDieId
            );

        dieObject.transform.SetParent(
            trayRoot.transform,
            true
        );

        dieObject.transform.position =
            trayOrigin +
            new Vector3(
                Random.Range(
                    -5.0f,
                    5.0f
                ),
                Random.Range(
                    4.4f,
                    7.8f
                ) +
                index * 0.018f,
                Random.Range(
                    -2.5f,
                    2.5f
                )
            );

        dieObject.transform.rotation =
            Random.rotation;

        dieObject.transform.localScale =
            Vector3.one;

        SetLayerRecursive(
            dieObject,
            DiceLayer
        );

        MeshFilter filter =
            dieObject.AddComponent<MeshFilter>();

        filter.sharedMesh =
            geometry.Mesh;

        MeshRenderer renderer =
            dieObject.AddComponent<MeshRenderer>();

        Material material =
            NewMaterial(
                DieColor(
                    sides
                )
            );

        if (material != null)
            renderer.material = material;

        MeshCollider collider =
            dieObject.AddComponent<MeshCollider>();

        collider.sharedMesh =
            geometry.Mesh;

        collider.convex = true;
        collider.material =
            dicePhysics;

        Rigidbody body =
            dieObject.AddComponent<Rigidbody>();

        body.mass =
            Mathf.Lerp(
                0.85f,
                1.20f,
                Mathf.InverseLerp(
                    4f,
                    20f,
                    sides
                )
            );

        body.linearDamping = 0.18f;
        body.angularDamping = 0.16f;
        body.interpolation =
            RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        body.linearVelocity =
            new Vector3(
                Random.Range(
                    -2.8f,
                    2.8f
                ),
                Random.Range(
                    -0.5f,
                    0.5f
                ),
                Random.Range(
                    -2.4f,
                    2.4f
                )
            );

        body.angularVelocity =
            Random.onUnitSphere *
            Random.Range(
                10f,
                18f
            );

        TraditionalDiceMarker marker =
            dieObject.AddComponent<
                TraditionalDiceMarker
            >();

        marker.Id = nextDieId++;
        marker.Renderer = renderer;
        marker.Body = body;

        marker.Configure(
            sides,
            geometry.FaceNormals,
            geometry.FaceValues,
            DieColor(
                sides
            )
        );

        CreateFaceLabels(
            dieObject.transform,
            sides,
            geometry
        );

        dice.Add(marker);
    }

    private DieGeometry GetGeometry(
        int sides)
    {
        DieGeometry cached;

        if (geometryCache.TryGetValue(
                sides,
                out cached))
        {
            return cached;
        }

        DieGeometry geometry;

        switch (sides)
        {
            case 3:
                geometry =
                    BuildCubeGeometry(
                        true
                    );
                break;

            case 4:
                geometry =
                    BuildTetrahedronGeometry();
                break;

            case 8:
                geometry =
                    BuildOctahedronGeometry();
                break;

            case 10:
                geometry =
                    BuildD10Geometry();
                break;

            case 12:
                geometry =
                    BuildDodecahedronGeometry();
                break;

            case 20:
                geometry =
                    BuildIcosahedronGeometry();
                break;

            case 6:
            default:
                geometry =
                    BuildCubeGeometry(
                        false
                    );
                break;
        }

        geometryCache[
            sides] =
            geometry;

        return geometry;
    }

    private DieGeometry NewGeometry(
        string name,
        List<Vector3[]> faces,
        int[] values)
    {
        List<Vector3> vertices =
            new List<Vector3>();

        List<int> triangles =
            new List<int>();

        List<Vector3> normals =
            new List<Vector3>();

        List<Vector3> centers =
            new List<Vector3>();

        for (int faceIndex = 0;
             faceIndex < faces.Count;
             faceIndex++)
        {
            Vector3[] polygon =
                faces[
                    faceIndex
                ];

            if (polygon == null ||
                polygon.Length < 3)
            {
                continue;
            }

            Vector3 center =
                Vector3.zero;

            foreach (Vector3 vertex
                in polygon)
            {
                center += vertex;
            }

            center /=
                polygon.Length;

            Vector3 normal =
                Vector3.Cross(
                    polygon[1] -
                    polygon[0],
                    polygon[2] -
                    polygon[0]
                ).normalized;

            if (Vector3.Dot(
                    normal,
                    center) < 0f)
            {
                System.Array.Reverse(
                    polygon
                );

                normal =
                    Vector3.Cross(
                        polygon[1] -
                        polygon[0],
                        polygon[2] -
                        polygon[0]
                    ).normalized;
            }

            int start =
                vertices.Count;

            vertices.AddRange(
                polygon
            );

            for (int i = 1;
                 i < polygon.Length - 1;
                 i++)
            {
                triangles.Add(
                    start
                );
                triangles.Add(
                    start + i
                );
                triangles.Add(
                    start + i + 1
                );
            }

            normals.Add(
                normal
            );
            centers.Add(
                center
            );
        }

        Mesh mesh =
            new Mesh();

        mesh.name =
            name;

        mesh.SetVertices(
            vertices
        );

        mesh.SetTriangles(
            triangles,
            0
        );

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return new DieGeometry
        {
            Mesh = mesh,
            FaceNormals =
                normals.ToArray(),
            FaceCenters =
                centers.ToArray(),
            FaceValues =
                values
        };
    }

    private DieGeometry BuildCubeGeometry(
        bool d3)
    {
        float r = 0.60f;

        Vector3 p000 =
            new Vector3(
                -r,
                -r,
                -r
            );

        Vector3 p001 =
            new Vector3(
                -r,
                -r,
                r
            );

        Vector3 p010 =
            new Vector3(
                -r,
                r,
                -r
            );

        Vector3 p011 =
            new Vector3(
                -r,
                r,
                r
            );

        Vector3 p100 =
            new Vector3(
                r,
                -r,
                -r
            );

        Vector3 p101 =
            new Vector3(
                r,
                -r,
                r
            );

        Vector3 p110 =
            new Vector3(
                r,
                r,
                -r
            );

        Vector3 p111 =
            new Vector3(
                r,
                r,
                r
            );

        List<Vector3[]> faces =
            new List<Vector3[]>
            {
                new[]
                {
                    p010,
                    p110,
                    p111,
                    p011
                },
                new[]
                {
                    p001,
                    p101,
                    p100,
                    p000
                },
                new[]
                {
                    p011,
                    p111,
                    p101,
                    p001
                },
                new[]
                {
                    p100,
                    p110,
                    p010,
                    p000
                },
                new[]
                {
                    p110,
                    p100,
                    p101,
                    p111
                },
                new[]
                {
                    p000,
                    p010,
                    p011,
                    p001
                }
            };

        int[] values =
            d3
            ? new[]
              {
                  1,
                  1,
                  2,
                  2,
                  3,
                  3
              }
            : new[]
              {
                  1,
                  6,
                  2,
                  5,
                  3,
                  4
              };

        return NewGeometry(
            d3
            ? "Warboard D3"
            : "Warboard D6",
            faces,
            values
        );
    }

    private DieGeometry BuildTetrahedronGeometry()
    {
        Vector3[] vertices =
        {
            new Vector3(
                1f,
                1f,
                1f
            ).normalized * 0.82f,

            new Vector3(
                -1f,
                -1f,
                1f
            ).normalized * 0.82f,

            new Vector3(
                -1f,
                1f,
                -1f
            ).normalized * 0.82f,

            new Vector3(
                1f,
                -1f,
                -1f
            ).normalized * 0.82f
        };

        List<Vector3[]> faces =
            new List<Vector3[]>
            {
                new[]
                {
                    vertices[0],
                    vertices[2],
                    vertices[1]
                },
                new[]
                {
                    vertices[0],
                    vertices[1],
                    vertices[3]
                },
                new[]
                {
                    vertices[0],
                    vertices[3],
                    vertices[2]
                },
                new[]
                {
                    vertices[1],
                    vertices[2],
                    vertices[3]
                }
            };

        return NewGeometry(
            "Warboard D4",
            faces,
            new[]
            {
                1,
                2,
                3,
                4
            }
        );
    }

    private DieGeometry BuildOctahedronGeometry()
    {
        float r = 0.82f;

        Vector3 up =
            Vector3.up * r;
        Vector3 down =
            Vector3.down * r;
        Vector3 right =
            Vector3.right * r;
        Vector3 left =
            Vector3.left * r;
        Vector3 forward =
            Vector3.forward * r;
        Vector3 back =
            Vector3.back * r;

        List<Vector3[]> faces =
            new List<Vector3[]>
            {
                new[] { up, forward, right },
                new[] { up, left, forward },
                new[] { up, back, left },
                new[] { up, right, back },
                new[] { down, right, forward },
                new[] { down, forward, left },
                new[] { down, left, back },
                new[] { down, back, right }
            };

        return NewGeometry(
            "Warboard D8",
            faces,
            new[]
            {
                1, 2, 3, 4,
                5, 6, 7, 8
            }
        );
    }

    private DieGeometry BuildD10Geometry()
    {
        List<Vector3[]> faces =
            new List<Vector3[]>();

        Vector3 top =
            new Vector3(
                0f,
                0.92f,
                0f
            );

        Vector3 bottom =
            new Vector3(
                0f,
                -0.92f,
                0f
            );

        Vector3[] ring =
            new Vector3[5];

        for (int i = 0;
             i < 5;
             i++)
        {
            float angle =
                Mathf.Deg2Rad *
                (90f +
                 i * 72f);

            ring[i] =
                new Vector3(
                    Mathf.Cos(angle) *
                        0.78f,
                    0f,
                    Mathf.Sin(angle) *
                        0.78f
                );
        }

        for (int i = 0;
             i < 5;
             i++)
        {
            int next =
                (i + 1) %
                5;

            faces.Add(
                new[]
                {
                    top,
                    ring[i],
                    ring[next]
                }
            );
        }

        for (int i = 0;
             i < 5;
             i++)
        {
            int next =
                (i + 1) %
                5;

            faces.Add(
                new[]
                {
                    bottom,
                    ring[next],
                    ring[i]
                }
            );
        }

        return NewGeometry(
            "Warboard D10",
            faces,
            Enumerable
                .Range(
                    1,
                    10
                )
                .ToArray()
        );
    }

    private Vector3[] IcosahedronVertices(
        float radius)
    {
        float phi =
            (1f +
             Mathf.Sqrt(5f)) /
            2f;

        Vector3[] raw =
        {
            new Vector3(-1f, phi, 0f),
            new Vector3(1f, phi, 0f),
            new Vector3(-1f, -phi, 0f),
            new Vector3(1f, -phi, 0f),
            new Vector3(0f, -1f, phi),
            new Vector3(0f, 1f, phi),
            new Vector3(0f, -1f, -phi),
            new Vector3(0f, 1f, -phi),
            new Vector3(phi, 0f, -1f),
            new Vector3(phi, 0f, 1f),
            new Vector3(-phi, 0f, -1f),
            new Vector3(-phi, 0f, 1f)
        };

        for (int i = 0;
             i < raw.Length;
             i++)
        {
            raw[i] =
                raw[i].normalized *
                radius;
        }

        return raw;
    }

    private int[][] IcosahedronFaces()
    {
        return new[]
        {
            new[] { 0, 11, 5 },
            new[] { 0, 5, 1 },
            new[] { 0, 1, 7 },
            new[] { 0, 7, 10 },
            new[] { 0, 10, 11 },
            new[] { 1, 5, 9 },
            new[] { 5, 11, 4 },
            new[] { 11, 10, 2 },
            new[] { 10, 7, 6 },
            new[] { 7, 1, 8 },
            new[] { 3, 9, 4 },
            new[] { 3, 4, 2 },
            new[] { 3, 2, 6 },
            new[] { 3, 6, 8 },
            new[] { 3, 8, 9 },
            new[] { 4, 9, 5 },
            new[] { 2, 4, 11 },
            new[] { 6, 2, 10 },
            new[] { 8, 6, 7 },
            new[] { 9, 8, 1 }
        };
    }

    private DieGeometry BuildIcosahedronGeometry()
    {
        Vector3[] vertices =
            IcosahedronVertices(
                0.84f
            );

        int[][] indices =
            IcosahedronFaces();

        List<Vector3[]> faces =
            new List<Vector3[]>();

        foreach (int[] face
            in indices)
        {
            faces.Add(
                new[]
                {
                    vertices[
                        face[0]
                    ],
                    vertices[
                        face[1]
                    ],
                    vertices[
                        face[2]
                    ]
                }
            );
        }

        return NewGeometry(
            "Warboard D20",
            faces,
            Enumerable
                .Range(
                    1,
                    20
                )
                .ToArray()
        );
    }

    private DieGeometry BuildDodecahedronGeometry()
    {
        Vector3[] icoVertices =
            IcosahedronVertices(
                1f
            );

        int[][] icoFaces =
            IcosahedronFaces();

        Vector3[] dualVertices =
            new Vector3[
                icoFaces.Length
            ];

        for (int faceIndex = 0;
             faceIndex <
                 icoFaces.Length;
             faceIndex++)
        {
            int[] face =
                icoFaces[
                    faceIndex
                ];

            Vector3 center =
                (icoVertices[
                     face[0]
                 ] +
                 icoVertices[
                     face[1]
                 ] +
                 icoVertices[
                     face[2]
                 ]) /
                3f;

            dualVertices[
                faceIndex
            ] =
                center.normalized *
                0.82f;
        }

        List<Vector3[]> faces =
            new List<Vector3[]>();

        for (int vertexIndex = 0;
             vertexIndex <
                 icoVertices.Length;
             vertexIndex++)
        {
            Vector3 axis =
                icoVertices[
                    vertexIndex
                ].normalized;

            List<int> adjacentFaces =
                new List<int>();

            for (int faceIndex = 0;
                 faceIndex <
                     icoFaces.Length;
                 faceIndex++)
            {
                if (icoFaces[
                        faceIndex]
                    .Contains(
                        vertexIndex))
                {
                    adjacentFaces.Add(
                        faceIndex
                    );
                }
            }

            Vector3 tangent =
                Mathf.Abs(
                    Vector3.Dot(
                        axis,
                        Vector3.up
                    )) < 0.90f
                ? Vector3.Cross(
                    axis,
                    Vector3.up
                  ).normalized
                : Vector3.Cross(
                    axis,
                    Vector3.right
                  ).normalized;

            Vector3 bitangent =
                Vector3.Cross(
                    axis,
                    tangent
                ).normalized;

            adjacentFaces =
                adjacentFaces
                    .OrderBy(
                        faceIndex =>
                        {
                            Vector3 value =
                                dualVertices[
                                    faceIndex
                                ];

                            Vector3 projected =
                                value -
                                axis *
                                Vector3.Dot(
                                    value,
                                    axis
                                );

                            return Mathf.Atan2(
                                Vector3.Dot(
                                    projected,
                                    bitangent
                                ),
                                Vector3.Dot(
                                    projected,
                                    tangent
                                )
                            );
                        }
                    )
                    .ToList();

            faces.Add(
                adjacentFaces
                    .Select(
                        faceIndex =>
                            dualVertices[
                                faceIndex
                            ]
                    )
                    .ToArray()
            );
        }

        return NewGeometry(
            "Warboard D12",
            faces,
            Enumerable
                .Range(
                    1,
                    12
                )
                .ToArray()
        );
    }

    private void CreateFaceLabels(
        Transform die,
        int sides,
        DieGeometry geometry)
    {
        if (geometry == null ||
            geometry.FaceNormals == null ||
            geometry.FaceCenters == null ||
            geometry.FaceValues == null)
        {
            return;
        }

        for (int i = 0;
             i < geometry.FaceNormals.Length;
             i++)
        {
            CreateFaceLabel(
                die,
                geometry.FaceValues[i],
                geometry.FaceCenters[i],
                geometry.FaceNormals[i],
                sides
            );
        }
    }

    private void CreateFaceLabel(
        Transform die,
        int value,
        Vector3 center,
        Vector3 normal,
        int sides)
    {
        GameObject face =
            new GameObject(
                "D" +
                sides +
                " Face " +
                value
            );

        face.transform.SetParent(
            die,
            false
        );

        face.transform.localPosition =
            center +
            normal *
            0.026f;

        Vector3 up =
            Mathf.Abs(
                Vector3.Dot(
                    normal,
                    Vector3.up
                )) < 0.90f
            ? Vector3.up
            : Vector3.forward;

        face.transform.localRotation =
            Quaternion.LookRotation(
                normal,
                up
            );

        TextMesh text =
            face.AddComponent<TextMesh>();

        Font font =
            Resources
                .GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf"
                );

        if (font != null)
        {
            text.font = font;

            MeshRenderer textRenderer =
                face.GetComponent<MeshRenderer>();

            if (textRenderer != null)
            {
                textRenderer.sharedMaterial =
                    font.material;
            }
        }

        text.text =
            value.ToString();

        text.anchor =
            TextAnchor.MiddleCenter;
        text.alignment =
            TextAlignment.Center;
        text.fontSize = 56;

        text.characterSize =
            sides >= 12
            ? 0.047f
            : sides >= 8
                ? 0.055f
                : 0.065f;

        text.color = Color.black;
        text.fontStyle =
            FontStyle.Bold;

        SetLayerRecursive(
            face,
            DiceLayer
        );
    }

    private void RerollSelected()
    {
        List<TraditionalDiceMarker> selected =
            dice
                .Where(
                    die =>
                        die != null &&
                        die.Selected
                )
                .ToList();

        if (selected.Count == 0)
            return;

        foreach (TraditionalDiceMarker die
            in selected)
        {
            die.SetSelected(false);

            die.transform.position =
                trayOrigin +
                new Vector3(
                    Random.Range(
                        -4.8f,
                        4.8f
                    ),
                    Random.Range(
                        4.6f,
                        7.5f
                    ),
                    Random.Range(
                        -2.4f,
                        2.4f
                    )
                );

            die.transform.rotation =
                Random.rotation;

            die.Body.linearVelocity =
                new Vector3(
                    Random.Range(
                        -2.8f,
                        2.8f
                    ),
                    Random.Range(
                        -0.4f,
                        0.6f
                    ),
                    Random.Range(
                        -2.2f,
                        2.2f
                    )
                );

            die.Body.angularVelocity =
                Random.onUnitSphere *
                Random.Range(
                    10f,
                    18f
                );

            die.Body.WakeUp();
        }

        rollInProgress = true;
        rollLogged = false;
        settledSince = -1f;
        settledText =
            selected.Count +
            " selected dice rerolling...";
    }

    private void HandleTrayClick(
        Rect textureRect)
    {
        Event current =
            Event.current;

        if (current == null ||
            current.type !=
                EventType.MouseDown ||
            current.button != 0 ||
            !textureRect.Contains(
                current.mousePosition))
        {
            return;
        }

        Vector2 local =
            current.mousePosition -
            textureRect.position;

        float u =
            Mathf.Clamp01(
                local.x /
                textureRect.width
            );

        float v =
            Mathf.Clamp01(
                1f -
                local.y /
                textureRect.height
            );

        Ray ray =
            trayCamera.ViewportPointToRay(
                new Vector3(
                    u,
                    v,
                    0f
                )
            );

        RaycastHit hit;

        if (Physics.Raycast(
                ray,
                out hit,
                100f,
                1 << DiceLayer))
        {
            TraditionalDiceMarker marker =
                hit.collider
                    .GetComponentInParent<
                        TraditionalDiceMarker
                    >();

            if (marker != null)
            {
                marker.SetSelected(
                    !marker.Selected
                );

                current.Use();
            }
        }
    }

    private void AdjustSelectedPool(
        int delta)
    {
        EnsurePoolInitialized();

        int current =
            requestedPool[
                selectedSides
            ];

        if (delta > 0)
        {
            int room =
                MaxDice -
                RequestedPoolTotal();

            delta =
                Mathf.Min(
                    delta,
                    room
                );
        }

        requestedPool[
            selectedSides] =
            Mathf.Clamp(
                current + delta,
                0,
                MaxDice
            );
    }

    public void DrawGUI()
    {
        EnsureBuilt();
        EnsurePoolInitialized();

        float width =
            Mathf.Min(
                650f,
                Screen.width - 28f
            );

        const float height = 176f;

        Rect panel =
            new Rect(
                Screen.width -
                    width -
                    14f,
                Screen.height -
                    height -
                    14f,
                width,
                height
            );

        GUI.Box(panel, "");

        GUIStyle heading =
            new GUIStyle(
                GUI.skin.label
            );

        heading.fontSize = 15;
        heading.fontStyle =
            FontStyle.Bold;

        GUI.Label(
            new Rect(
                panel.x + 12f,
                panel.y + 8f,
                panel.width - 24f,
                22f
            ),
            "WORLD DICE TRAY CONTROLS",
            heading
        );

        float typeY =
            panel.y + 36f;

        float typeWidth =
            (panel.width -
             24f -
             6f * 6f) /
            7f;

        float typeX =
            panel.x + 12f;

        foreach (int sides
            in SupportedSides)
        {
            Color old = GUI.color;

            if (sides == selectedSides)
            {
                GUI.color =
                    new Color(
                        0.76f,
                        0.90f,
                        1f,
                        1f
                    );
            }

            if (GUI.Button(
                new Rect(
                    typeX,
                    typeY,
                    typeWidth,
                    34f
                ),
                "D" +
                sides +
                " " +
                requestedPool[sides]))
            {
                selectedSides = sides;
            }

            GUI.color = old;

            typeX +=
                typeWidth + 6f;
        }

        float y =
            panel.y + 78f;

        if (GUI.Button(
            new Rect(
                panel.x + 12f,
                y,
                40f,
                28f
            ),
            "-5"))
        {
            AdjustSelectedPool(-5);
        }

        if (GUI.Button(
            new Rect(
                panel.x + 56f,
                y,
                40f,
                28f
            ),
            "-1"))
        {
            AdjustSelectedPool(-1);
        }

        GUI.Label(
            new Rect(
                panel.x + 104f,
                y + 4f,
                108f,
                22f
            ),
            "D" +
            selectedSides +
            ": " +
            requestedPool[selectedSides]
        );

        if (GUI.Button(
            new Rect(
                panel.x + 212f,
                y,
                40f,
                28f
            ),
            "+1"))
        {
            AdjustSelectedPool(1);
        }

        if (GUI.Button(
            new Rect(
                panel.x + 256f,
                y,
                40f,
                28f
            ),
            "+5"))
        {
            AdjustSelectedPool(5);
        }

        GUI.enabled =
            RequestedPoolTotal() > 0;

        if (GUI.Button(
            new Rect(
                panel.x + 306f,
                y,
                106f,
                28f
            ),
            "ROLL POOL"))
        {
            RollAll();
        }

        GUI.enabled = true;

        int selectedCount =
            dice.Count(
                die =>
                    die != null &&
                    die.Selected
            );

        GUI.enabled =
            selectedCount > 0;

        if (GUI.Button(
            new Rect(
                panel.x + 418f,
                y,
                122f,
                28f
            ),
            "REROLL SELECTED"))
        {
            RerollSelected();
        }

        GUI.enabled = true;

        if (GUI.Button(
            new Rect(
                panel.x +
                    panel.width -
                    100f,
                y,
                88f,
                28f
            ),
            "CLEAR"))
        {
            ClearDice();
        }

        GUI.Label(
            new Rect(
                panel.x + 12f,
                panel.y + 116f,
                panel.width - 24f,
                22f
            ),
            "Pool: " +
            PoolSummary() +
            " | " +
            settledText +
            (selectedCount > 0
                ? " | " +
                  selectedCount +
                  " selected"
                : "")
        );

        GUI.Label(
            new Rect(
                panel.x + 12f,
                panel.y + 140f,
                panel.width - 24f,
                22f
            ),
            "Dice are physical objects below the battlefield. Click a die there to select it for reroll."
        );
    }

}
