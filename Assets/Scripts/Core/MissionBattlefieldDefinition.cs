using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum MissionDeploymentArchetype
{
    TippingPoint,
    SweepingEngagement,
    CrucibleOfBattle,
    SearchAndDestroy,
    HammerAndAnvil,
    DawnOfWar
}

public class MissionDeploymentZone
{
    public readonly List<Vector2> Polygon =
        new List<Vector2>();

    public bool HasCircularExclusion;
    public Vector2 ExclusionCentre =
        Vector2.zero;
    public float ExclusionRadius;

    public MissionDeploymentZone(
        params Vector2[] vertices)
    {
        if (vertices != null)
        {
            Polygon.AddRange(
                vertices
            );
        }
    }

    public bool ContainsPoint(
        Vector2 point)
    {
        if (Polygon.Count < 3)
            return false;

        for (int edgeIndex = 0;
             edgeIndex < Polygon.Count;
             edgeIndex++)
        {
            Vector2 edgeA =
                Polygon[edgeIndex];

            Vector2 edgeB =
                Polygon[
                    (edgeIndex + 1) %
                    Polygon.Count
                ];

            if (DistancePointToSegment(
                    point,
                    edgeA,
                    edgeB) <= 0.001f)
            {
                if (HasCircularExclusion &&
                    Vector2.Distance(
                        point,
                        ExclusionCentre
                    ) <
                    ExclusionRadius)
                {
                    return false;
                }

                return true;
            }
        }

        bool inside = false;

        int j =
            Polygon.Count - 1;

        for (int i = 0;
             i < Polygon.Count;
             i++)
        {
            Vector2 pi =
                Polygon[i];

            Vector2 pj =
                Polygon[j];

            bool crosses =
                ((pi.y > point.y) !=
                 (pj.y > point.y)) &&
                (point.x <
                 (pj.x - pi.x) *
                 (point.y - pi.y) /
                 (pj.y - pi.y) +
                 pi.x);

            if (crosses)
                inside = !inside;

            j = i;
        }

        if (!inside)
            return false;

        if (HasCircularExclusion &&
            Vector2.Distance(
                point,
                ExclusionCentre
            ) <
            ExclusionRadius)
        {
            return false;
        }

        return true;
    }

    public bool ContainsBase(
        Vector3 world,
        float radius)
    {
        Vector2 centre =
            new Vector2(
                world.x,
                world.z
            );

        if (!ContainsPoint(centre))
            return false;

        float sampleRadius =
            Mathf.Max(
                0f,
                radius
            );

        if (sampleRadius <= 0.001f)
            return true;

        const int samples = 20;

        for (int i = 0;
             i < samples;
             i++)
        {
            float angle =
                i /
                (float)samples *
                Mathf.PI *
                2f;

            Vector2 edge =
                centre +
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) *
                sampleRadius;

            if (!ContainsPoint(edge))
                return false;
        }

        return true;
    }

    public float DistanceToZone(
        Vector3 world)
    {
        Vector2 point =
            new Vector2(
                world.x,
                world.z
            );

        if (ContainsPoint(point))
            return 0f;

        float best =
            float.MaxValue;

        for (int i = 0;
             i < Polygon.Count;
             i++)
        {
            Vector2 a =
                Polygon[i];

            Vector2 b =
                Polygon[
                    (i + 1) %
                    Polygon.Count
                ];

            best =
                Mathf.Min(
                    best,
                    DistancePointToSegment(
                        point,
                        a,
                        b
                    )
                );
        }

        if (HasCircularExclusion &&
            PointInPolygon(point) &&
            Vector2.Distance(
                point,
                ExclusionCentre
            ) <
            ExclusionRadius)
        {
            best =
                Mathf.Min(
                    best,
                    ExclusionRadius -
                    Vector2.Distance(
                        point,
                        ExclusionCentre
                    )
                );
        }

        return
            best == float.MaxValue
            ? 999f
            : best;
    }

    private bool PointInPolygon(
        Vector2 point)
    {
        if (Polygon.Count < 3)
            return false;

        bool inside = false;

        int j =
            Polygon.Count - 1;

        for (int i = 0;
             i < Polygon.Count;
             i++)
        {
            Vector2 pi =
                Polygon[i];

            Vector2 pj =
                Polygon[j];

            bool crosses =
                ((pi.y > point.y) !=
                 (pj.y > point.y)) &&
                (point.x <
                 (pj.x - pi.x) *
                 (point.y - pi.y) /
                 (pj.y - pi.y) +
                 pi.x);

            if (crosses)
                inside = !inside;

            j = i;
        }

        return inside;
    }

    private static float DistancePointToSegment(
        Vector2 point,
        Vector2 a,
        Vector2 b)
    {
        Vector2 ab =
            b - a;

        float lengthSq =
            ab.sqrMagnitude;

        if (lengthSq <= 0.00001f)
        {
            return
                Vector2.Distance(
                    point,
                    a
                );
        }

        float t =
            Vector2.Dot(
                point - a,
                ab
            ) /
            lengthSq;

        t =
            Mathf.Clamp01(t);

        Vector2 closest =
            a +
            ab * t;

        return
            Vector2.Distance(
                point,
                closest
            );
    }
}

public class MissionObjectiveSpec
{
    public Vector3 Position;
    public MissionObjectiveRole Role;

    public MissionObjectiveSpec(
        float x,
        float z,
        MissionObjectiveRole role)
    {
        Position =
            new Vector3(
                x,
                0f,
                z
            );

        Role = role;
    }
}

public class MissionTerrainSpec
{
    public string Id;
    public Vector3 Position;
    public Vector3 Size;
    public TerrainTrait Trait;
    public bool IsObjectiveArea;

    public MissionTerrainSpec(
        string id,
        float x,
        float z,
        float width,
        float depth,
        TerrainTrait trait,
        bool objectiveArea = false)
    {
        Id = id;

        Position =
            new Vector3(
                x,
                1f,
                z
            );

        Size =
            new Vector3(
                width,
                2f,
                depth
            );

        Trait = trait;
        IsObjectiveArea =
            objectiveArea;
    }
}

public class MissionBattlefieldDefinition
{
    public string DisplayName = "";
    public MissionDeploymentArchetype Archetype;

    public MissionDeploymentZone AttackerZone;
    public MissionDeploymentZone DefenderZone;

    public readonly List<MissionObjectiveSpec>
        Objectives =
            new List<MissionObjectiveSpec>();

    public readonly List<MissionTerrainSpec>
        Terrain =
            new List<MissionTerrainSpec>();

    public string LayoutLabel = "A";

    public MissionDeploymentZone ZoneForRole(
        bool attacker)
    {
        return attacker
            ? AttackerZone
            : DefenderZone;
    }
}

public static class MissionBattlefieldRegistry
{
    public static MissionBattlefieldDefinition Build(
        ForceDisposition first,
        ForceDisposition second,
        int layoutIndex,
        float boardWidth,
        float boardDepth)
    {
        MissionDeploymentArchetype archetype =
            ResolveArchetype(
                first,
                second
            );

        MissionBattlefieldDefinition definition =
            new MissionBattlefieldDefinition();

        definition.Archetype =
            archetype;

        definition.DisplayName =
            DisplayName(
                archetype
            );

        definition.LayoutLabel =
            layoutIndex == 2
            ? "B"
            : layoutIndex == 3
                ? "C"
                : "A";

        BuildZones(
            definition,
            boardWidth,
            boardDepth
        );

        BuildObjectives(
            definition,
            layoutIndex
        );

        BuildTerrain(
            definition,
            layoutIndex
        );

        return definition;
    }

    public static string DisplayName(
        MissionDeploymentArchetype archetype)
    {
        switch (archetype)
        {
            case MissionDeploymentArchetype.TippingPoint:
                return "Tipping Point";

            case MissionDeploymentArchetype.SweepingEngagement:
                return "Sweeping Engagement";

            case MissionDeploymentArchetype.CrucibleOfBattle:
                return "Crucible of Battle";

            case MissionDeploymentArchetype.SearchAndDestroy:
                return "Search and Destroy";

            case MissionDeploymentArchetype.HammerAndAnvil:
                return "Hammer and Anvil";

            default:
                return "Dawn of War";
        }
    }

    private static MissionDeploymentArchetype
        ResolveArchetype(
            ForceDisposition a,
            ForceDisposition b)
    {
        string key =
            PairKey(
                a,
                b
            );

        switch (key)
        {
            case "TakeAndHold|TakeAndHold":
            case "Reconnaissance|TakeAndHold":
            case "Disruption|Reconnaissance":
                return
                    MissionDeploymentArchetype
                        .TippingPoint;

            case "PurgeTheFoe|TakeAndHold":
            case "Disruption|TakeAndHold":
            case "PriorityAssets|PriorityAssets":
            case "Disruption|PriorityAssets":
                return
                    MissionDeploymentArchetype
                        .SweepingEngagement;

            case "PriorityAssets|TakeAndHold":
            case "PriorityAssets|Reconnaissance":
            case "Disruption|Disruption":
                return
                    MissionDeploymentArchetype
                        .CrucibleOfBattle;

            case "PurgeTheFoe|PurgeTheFoe":
            case "Disruption|PurgeTheFoe":
                return
                    MissionDeploymentArchetype
                        .SearchAndDestroy;

            case "PurgeTheFoe|Reconnaissance":
                return
                    MissionDeploymentArchetype
                        .HammerAndAnvil;

            case "PriorityAssets|PurgeTheFoe":
                return
                    MissionDeploymentArchetype
                        .DawnOfWar;

            // Reconnaissance mirror is the one current GDM layout image that
            // is not reliably retrievable in the build environment. Keep it
            // on a conventional long-axis mission map rather than inventing a
            // bespoke geometry. The registry isolates this one mapping so it
            // can be replaced without touching deployment rules.
            case "Reconnaissance|Reconnaissance":
                return
                    MissionDeploymentArchetype
                        .HammerAndAnvil;
        }

        return
            MissionDeploymentArchetype
                .TippingPoint;
    }

    private static string PairKey(
        ForceDisposition a,
        ForceDisposition b)
    {
        string first =
            a.ToString();

        string second =
            b.ToString();

        return
            string.CompareOrdinal(
                first,
                second
            ) <= 0
            ? first + "|" + second
            : second + "|" + first;
    }

    private static void BuildZones(
        MissionBattlefieldDefinition definition,
        float boardWidth,
        float boardDepth)
    {
        float halfX =
            boardWidth * 0.5f;

        float halfZ =
            boardDepth * 0.5f;

        float tippingDepth =
            Mathf.Min(
                15f,
                halfX * 0.45f
            );

        float hammerDepth =
            Mathf.Min(
                18f,
                halfX * 0.45f
            );

        float sweepingDepth =
            Mathf.Min(
                11f,
                halfZ * 0.45f
            );

        float dawnDepth =
            Mathf.Min(
                12f,
                halfZ * 0.45f
            );

        switch (definition.Archetype)
        {
            case MissionDeploymentArchetype.TippingPoint:
                // Top/bottom edge deployment with a broad central no-man's
                // land, matching the GDM Tipping Point orientation.
                definition.AttackerZone =
                    Rectangle(
                        halfX -
                            tippingDepth,
                        halfX,
                        -halfZ,
                        halfZ
                    );

                definition.DefenderZone =
                    Rectangle(
                        -halfX,
                        -halfX +
                            tippingDepth,
                        -halfZ,
                        halfZ
                    );
                break;

            case MissionDeploymentArchetype.HammerAndAnvil:
                definition.AttackerZone =
                    Rectangle(
                        halfX -
                            hammerDepth,
                        halfX,
                        -halfZ,
                        halfZ
                    );

                definition.DefenderZone =
                    Rectangle(
                        -halfX,
                        -halfX +
                            hammerDepth,
                        -halfZ,
                        halfZ
                    );
                break;

            case MissionDeploymentArchetype.SweepingEngagement:
                definition.AttackerZone =
                    Rectangle(
                        -halfX,
                        halfX,
                        -halfZ,
                        -halfZ +
                            sweepingDepth
                    );

                definition.DefenderZone =
                    Rectangle(
                        -halfX,
                        halfX,
                        halfZ -
                            sweepingDepth,
                        halfZ
                    );
                break;

            case MissionDeploymentArchetype.DawnOfWar:
                definition.AttackerZone =
                    Rectangle(
                        -halfX,
                        halfX,
                        -halfZ,
                        -halfZ +
                            dawnDepth
                    );

                definition.DefenderZone =
                    Rectangle(
                        -halfX,
                        halfX,
                        halfZ -
                            dawnDepth,
                        halfZ
                    );
                break;

            case MissionDeploymentArchetype.SearchAndDestroy:
                definition.AttackerZone =
                    new MissionDeploymentZone(
                        new Vector2(
                            0f,
                            -halfZ
                        ),
                        new Vector2(
                            halfX,
                            -halfZ
                        ),
                        new Vector2(
                            halfX,
                            0f
                        ),
                        new Vector2(
                            0f,
                            0f
                        )
                    );

                definition.DefenderZone =
                    new MissionDeploymentZone(
                        new Vector2(
                            -halfX,
                            0f
                        ),
                        new Vector2(
                            0f,
                            0f
                        ),
                        new Vector2(
                            0f,
                            halfZ
                        ),
                        new Vector2(
                            -halfX,
                            halfZ
                        )
                    );

                definition.AttackerZone
                    .HasCircularExclusion = true;

                definition.AttackerZone
                    .ExclusionRadius = 9f;

                definition.DefenderZone
                    .HasCircularExclusion = true;

                definition.DefenderZone
                    .ExclusionRadius = 9f;
                break;

            case MissionDeploymentArchetype.CrucibleOfBattle:
                // Opposing triangular corners separated by a diagonal
                // no-man's-land band.
                definition.AttackerZone =
                    new MissionDeploymentZone(
                        new Vector2(
                            halfX,
                            -halfZ
                        ),
                        new Vector2(
                            halfX,
                            halfZ
                        ),
                        new Vector2(
                            0f,
                            -halfZ
                        )
                    );

                definition.DefenderZone =
                    new MissionDeploymentZone(
                        new Vector2(
                            -halfX,
                            -halfZ
                        ),
                        new Vector2(
                            0f,
                            halfZ
                        ),
                        new Vector2(
                            -halfX,
                            halfZ
                        )
                    );
                break;
        }
    }

    private static MissionDeploymentZone Rectangle(
        float minX,
        float maxX,
        float minZ,
        float maxZ)
    {
        return
            new MissionDeploymentZone(
                new Vector2(
                    minX,
                    minZ
                ),
                new Vector2(
                    maxX,
                    minZ
                ),
                new Vector2(
                    maxX,
                    maxZ
                ),
                new Vector2(
                    minX,
                    maxZ
                )
            );
    }

    private static void BuildObjectives(
        MissionBattlefieldDefinition definition,
        int layoutIndex)
    {
        definition.Objectives.Clear();

        float lateral =
            layoutIndex == 2
            ? 11f
            : layoutIndex == 3
                ? 14f
                : 12f;

        float homeOffset =
            layoutIndex == 2
            ? 5f
            : layoutIndex == 3
                ? -5f
                : 0f;

        if (definition.Archetype ==
            MissionDeploymentArchetype.SweepingEngagement ||
            definition.Archetype ==
            MissionDeploymentArchetype.DawnOfWar)
        {
            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    homeOffset,
                    -16f,
                    MissionObjectiveRole
                        .PlayerOneHome
                )
            );

            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    -homeOffset,
                    16f,
                    MissionObjectiveRole
                        .PlayerTwoHome
                )
            );

            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    -5f,
                    -2f,
                    MissionObjectiveRole.Central
                )
            );

            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    5f,
                    2f,
                    MissionObjectiveRole.Central
                )
            );

            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    lateral,
                    -4f,
                    MissionObjectiveRole.Expansion
                )
            );

            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    -lateral,
                    4f,
                    MissionObjectiveRole.Expansion
                )
            );

            return;
        }

        if (definition.Archetype ==
            MissionDeploymentArchetype.SearchAndDestroy ||
            definition.Archetype ==
            MissionDeploymentArchetype.CrucibleOfBattle)
        {
            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    20f,
                    -10f,
                    MissionObjectiveRole
                        .PlayerOneHome
                )
            );

            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    -20f,
                    10f,
                    MissionObjectiveRole
                        .PlayerTwoHome
                )
            );

            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    3f,
                    -3f,
                    MissionObjectiveRole.Central
                )
            );

            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    -3f,
                    3f,
                    MissionObjectiveRole.Central
                )
            );

            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    8f,
                    11f,
                    MissionObjectiveRole.Expansion
                )
            );

            definition.Objectives.Add(
                new MissionObjectiveSpec(
                    -8f,
                    -11f,
                    MissionObjectiveRole.Expansion
                )
            );

            return;
        }

        definition.Objectives.Add(
            new MissionObjectiveSpec(
                20f,
                homeOffset,
                MissionObjectiveRole
                    .PlayerOneHome
            )
        );

        definition.Objectives.Add(
            new MissionObjectiveSpec(
                -20f,
                -homeOffset,
                MissionObjectiveRole
                    .PlayerTwoHome
            )
        );

        definition.Objectives.Add(
            new MissionObjectiveSpec(
                3f,
                -4f,
                MissionObjectiveRole.Central
            )
        );

        definition.Objectives.Add(
            new MissionObjectiveSpec(
                -3f,
                4f,
                MissionObjectiveRole.Central
            )
        );

        definition.Objectives.Add(
            new MissionObjectiveSpec(
                8f,
                lateral,
                MissionObjectiveRole.Expansion
            )
        );

        definition.Objectives.Add(
            new MissionObjectiveSpec(
                -8f,
                -lateral,
                MissionObjectiveRole.Expansion
            )
        );
    }

    private static void BuildTerrain(
        MissionBattlefieldDefinition definition,
        int layoutIndex)
    {
        definition.Terrain.Clear();

        float shift =
            layoutIndex == 2
            ? 2.5f
            : layoutIndex == 3
                ? -2.5f
                : 0f;

        float flip =
            layoutIndex == 3
            ? -1f
            : 1f;

        AddTerrainPair(
            definition,
            "A",
            17f,
            -9f + shift,
            7f,
            4f,
            TerrainTrait.Blocking,
            false
        );

        AddTerrainPair(
            definition,
            "B",
            8f,
            10f * flip,
            6f,
            4f,
            TerrainTrait.Cover,
            true
        );

        AddTerrainPair(
            definition,
            "C",
            2f,
            -13f * flip,
            7f,
            3f,
            TerrainTrait.Blocking,
            false
        );

        AddTerrainPair(
            definition,
            "D",
            11f,
            4f * flip,
            4f,
            6f,
            TerrainTrait.Cover,
            false
        );

        AddTerrainPair(
            definition,
            "E",
            4f,
            4f + shift,
            5f,
            5f,
            TerrainTrait.Traversable,
            true
        );

        AddTerrainPair(
            definition,
            "F",
            22f,
            12f * flip,
            5f,
            3f,
            TerrainTrait.Blocking,
            false
        );
    }

    private static void AddTerrainPair(
        MissionBattlefieldDefinition definition,
        string id,
        float x,
        float z,
        float width,
        float depth,
        TerrainTrait trait,
        bool objectiveArea)
    {
        definition.Terrain.Add(
            new MissionTerrainSpec(
                id + "1",
                x,
                z,
                width,
                depth,
                trait,
                objectiveArea
            )
        );

        definition.Terrain.Add(
            new MissionTerrainSpec(
                id + "2",
                -x,
                -z,
                width,
                depth,
                trait,
                objectiveArea
            )
        );
    }
}
