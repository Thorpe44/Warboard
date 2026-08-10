using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class WarboardRuleMarker47
{
    public string Id = "";
    public string Type = "";
    public string Label = "";
    public string FactionId = "";
    public Vector3 Position;
    public float RulesRadius;
    public WarboardRuleScope47 Scope;
    public int CreatedRound;
    public int CreatedTurnSerial;
    public GameController.Phase CreatedPhase;
    public SquadController SourceUnit;
    public GameObject Visual;
}

public sealed class WarboardMarkerPlacementRequest47
{
    public string Type = "marker";
    public string Label = "RULE MARKER";
    public string FactionId = "";
    public float VisualDiameter = 1.57f;
    public float RulesRadius;
    public float MinimumEnemyDistance;
    public SquadController SourceUnit;
    public float MaximumDistanceFromSource;
    public WarboardRuleMarker47 ReferenceMarker;
    public float MaximumDistanceFromReference;
    public WarboardRuleScope47 Scope =
        WarboardRuleScope47.Battle;
    public Color Color =
        new Color(0.25f, 0.85f, 1f, 1f);
    public Action<WarboardRuleMarker47> Completed;
    public Action Cancelled;
}

/// <summary>
/// Generic physical faction-marker runtime. Rules request placement with
/// distance constraints; the player clicks the battlefield; Warboard validates,
/// creates, tracks, queries and removes the marker.
/// </summary>
[DefaultExecutionOrder(-31780)]
public sealed class WarboardMarkerSystem47 : MonoBehaviour
{
    public static WarboardMarkerSystem47 Instance
    {
        get;
        private set;
    }

    private readonly List<WarboardRuleMarker47> markers =
        new List<WarboardRuleMarker47>();

    private WarboardMarkerPlacementRequest47 pending;
    private string placementError = "";
    private int nextId = 1;

    public IReadOnlyList<WarboardRuleMarker47> Markers
    {
        get { return markers.ToArray(); }
    }

    public bool PlacementPending
    {
        get { return pending != null; }
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object.FindAnyObjectByType<
                WarboardMarkerSystem47>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "Warboard Rule Markers v47"
            );

        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<WarboardMarkerSystem47>();
    }

    private void Awake()
    {
        Instance = this;
        GameEventBus.Raised += HandleCoreEvent;
        WarboardRuleEventBus47.Raised += HandleRuleEvent;
    }

    private void OnDestroy()
    {
        GameEventBus.Raised -= HandleCoreEvent;
        WarboardRuleEventBus47.Raised -= HandleRuleEvent;

        if (Instance == this)
            Instance = null;
    }

    public void BeginPlacement(
        WarboardMarkerPlacementRequest47 request)
    {
        if (request == null)
            return;

        pending = request;
        placementError = "";

        GameController game =
            GameController.Current;

        if (game != null)
        {
            game.StandardSetStatus(
                "PLACE " +
                (request.Label ?? "MARKER") +
                ": click a legal point on the battlefield. ESC cancels."
            );
        }
    }

    public void CancelPlacement()
    {
        if (pending == null)
            return;

        Action cancel = pending.Cancelled;
        pending = null;
        placementError = "";

        if (cancel != null)
            cancel();
    }

    public IEnumerable<WarboardRuleMarker47> ForFaction(
        string factionId,
        string type = "")
    {
        return markers
            .Where(
                marker =>
                    marker != null &&
                    string.Equals(
                        marker.FactionId,
                        factionId,
                        StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(type) ||
                     string.Equals(
                        marker.Type,
                        type,
                        StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    public bool UnitWhollyWithin(
        SquadController unit,
        WarboardRuleMarker47 marker,
        float distance)
    {
        if (unit == null || marker == null)
            return false;

        return unit
            .JoinedActionController()
            .JoinedLivingModelTokens()
            .All(
                model =>
                    model != null &&
                    PointToModelBaseDistance(
                        marker.Position,
                        model) <=
                    distance + 0.001f
            );
    }

    public bool UnitWithinAny(
        SquadController unit,
        string factionId,
        string type,
        float distance,
        bool wholly)
    {
        if (unit == null)
            return false;

        foreach (WarboardRuleMarker47 marker
            in ForFaction(factionId, type))
        {
            if (wholly)
            {
                if (UnitWhollyWithin(
                        unit,
                        marker,
                        distance))
                {
                    return true;
                }
            }
            else if (unit
                .JoinedActionController()
                .JoinedLivingModelTokens()
                .Any(
                    model =>
                        model != null &&
                        PointToModelBaseDistance(
                            marker.Position,
                            model) <=
                        distance + 0.001f))
            {
                return true;
            }
        }

        return false;
    }

    public void RemoveMarker(
        WarboardRuleMarker47 marker,
        string reason = "")
    {
        if (marker == null ||
            !markers.Remove(marker))
        {
            return;
        }

        if (marker.Visual != null)
            Destroy(marker.Visual);

        WarboardRuleEventBus47.Raise(
            new WarboardRuleEvent47
            {
                Type = WarboardRuleEventType47.MarkerRemoved,
                Game = GameController.Current,
                ActingFaction = marker.FactionId,
                Source = marker.SourceUnit,
                Marker = marker,
                Note = reason ?? ""
            }
        );
    }

    private void Update()
    {
        if (pending != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceFromCursor();
            }
        }

        RemoveExpiredMarkers();
    }

    private void TryPlaceFromCursor()
    {
        if (pending == null)
            return;

        Camera camera = Camera.main;

        if (camera == null)
        {
            placementError = "No game camera.";
            return;
        }

        Ray ray =
            camera.ScreenPointToRay(
                Input.mousePosition
            );

        Plane plane =
            new Plane(
                Vector3.up,
                new Vector3(0f, 0.03f, 0f)
            );

        float enter;

        if (!plane.Raycast(ray, out enter))
        {
            placementError =
                "Click the battlefield surface.";
            return;
        }

        Vector3 point = ray.GetPoint(enter);
        point.y = 0.035f;

        string reason;

        if (!LegalPlacement(
                pending,
                point,
                out reason))
        {
            placementError = reason;

            GameController game =
                GameController.Current;

            if (game != null)
                game.StandardSetStatus(reason);

            return;
        }

        WarboardMarkerPlacementRequest47 request =
            pending;

        pending = null;
        placementError = "";

        WarboardRuleMarker47 marker =
            CreateMarker(
                request,
                point
            );

        markers.Add(marker);

        GameController owner =
            GameController.Current;

        if (owner != null)
        {
            owner.StandardLog(
                "RULE MARKER",
                marker.Label,
                "Placed at (" +
                marker.Position.x.ToString("0.0") +
                ", " +
                marker.Position.z.ToString("0.0") +
                ")."
            );
        }

        WarboardRuleEventBus47.Raise(
            new WarboardRuleEvent47
            {
                Type = WarboardRuleEventType47.MarkerPlaced,
                Game = owner,
                ActingFaction = marker.FactionId,
                Source = marker.SourceUnit,
                Marker = marker,
                Note = marker.Label
            }
        );

        if (request.Completed != null)
            request.Completed(marker);
    }

    private bool LegalPlacement(
        WarboardMarkerPlacementRequest47 request,
        Vector3 point,
        out string reason)
    {
        reason = "";

        float halfX =
            GameController.BoardWidth * 0.5f;

        float halfZ =
            GameController.BoardDepth * 0.5f;

        float radius =
            Mathf.Max(
                0.1f,
                request.VisualDiameter * 0.5f
            );

        if (Mathf.Abs(point.x) + radius >
                halfX + 0.001f ||
            Mathf.Abs(point.z) + radius >
                halfZ + 0.001f)
        {
            reason =
                "Marker must be wholly on the battlefield.";
            return false;
        }

        if (request.SourceUnit != null &&
            request.MaximumDistanceFromSource > 0f)
        {
            float distance =
                PointToUnitDistance(
                    point,
                    request.SourceUnit
                );

            if (distance >
                request.MaximumDistanceFromSource +
                0.001f)
            {
                reason =
                    "Marker must be within " +
                    request.MaximumDistanceFromSource
                        .ToString("0.#") +
                    " inches of " +
                    request.SourceUnit.DisplayName +
                    ".";
                return false;
            }
        }

        if (request.ReferenceMarker != null &&
            request.MaximumDistanceFromReference > 0f)
        {
            float distance =
                HorizontalDistance(
                    point,
                    request.ReferenceMarker.Position
                );

            if (distance >
                request.MaximumDistanceFromReference +
                0.001f)
            {
                reason =
                    "Marker must be within " +
                    request.MaximumDistanceFromReference
                        .ToString("0.#") +
                    " inches of " +
                    request.ReferenceMarker.Label +
                    ".";
                return false;
            }
        }

        if (request.MinimumEnemyDistance > 0f)
        {
            GameController game =
                GameController.Current;

            if (game != null)
            {
                float nearest =
                    game.StandardEnemyUnits(
                            request.FactionId)
                        .Where(
                            unit =>
                                unit != null &&
                                unit.IsAlive &&
                                unit.IsOnBattlefield)
                        .Select(
                            unit =>
                                PointToUnitDistance(
                                    point,
                                    unit))
                        .DefaultIfEmpty(
                            float.MaxValue)
                        .Min();

                if (nearest <=
                    request.MinimumEnemyDistance +
                    0.001f)
                {
                    reason =
                        "Marker must be more than " +
                        request.MinimumEnemyDistance
                            .ToString("0.#") +
                        " inches from all enemy units.";
                    return false;
                }
            }
        }

        return true;
    }

    private WarboardRuleMarker47 CreateMarker(
        WarboardMarkerPlacementRequest47 request,
        Vector3 point)
    {
        WarboardRuleMarker47 marker =
            new WarboardRuleMarker47
            {
                Id = "v47_marker_" +
                    nextId++,
                Type = request.Type ?? "marker",
                Label = request.Label ?? "RULE MARKER",
                FactionId = request.FactionId ?? "",
                Position = point,
                RulesRadius = request.RulesRadius,
                Scope = request.Scope,
                CreatedRound =
                    GameController.Current != null
                    ? GameController.Current.BattleRound
                    : 0,
                CreatedTurnSerial =
                    WarboardRuleStateRuntime47.CurrentTurnSerial,
                CreatedPhase =
                    GameController.Current != null
                    ? GameController.Current.CurrentPhase
                    : GameController.Phase.Command,
                SourceUnit = request.SourceUnit != null
                    ? request.SourceUnit.JoinedActionController()
                    : null
            };

        GameObject root =
            new GameObject(
                marker.Label +
                " [" +
                marker.Id +
                "]"
            );

        root.transform.position = point;

        GameObject disc =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        disc.name = "Marker Disc";
        disc.transform.SetParent(
            root.transform,
            false
        );

        disc.transform.localPosition =
            Vector3.zero;

        disc.transform.localScale =
            new Vector3(
                request.VisualDiameter,
                0.035f,
                request.VisualDiameter
            );

        Collider collider =
            disc.GetComponent<Collider>();

        if (collider != null)
            Destroy(collider);

        Renderer renderer =
            disc.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material material =
                new Material(
                    Shader.Find("Standard") ??
                    Shader.Find("Diffuse")
                );

            material.color = request.Color;
            renderer.sharedMaterial = material;
        }

        GameObject labelObject =
            new GameObject("Marker Label");

        labelObject.transform.SetParent(
            root.transform,
            false
        );

        labelObject.transform.localPosition =
            new Vector3(
                0f,
                0.18f,
                0f
            );

        labelObject.transform.rotation =
            Quaternion.Euler(
                90f,
                0f,
                0f
            );

        TextMesh text =
            labelObject.AddComponent<TextMesh>();

        text.text = marker.Label;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.16f;
        text.fontSize = 34;
        text.color = Color.white;

        marker.Visual = root;

        return marker;
    }

    private void RemoveExpiredMarkers()
    {
        // Scope expiry is intentionally synchronized with the central state
        // store. Battle markers survive until removed by their own rule.
        // Phase/Turn/Round markers are uncommon but supported for future packs.
        GameController game = GameController.Current;

        if (game == null)
            return;

        List<WarboardRuleMarker47> expired =
            markers
                .Where(
                    marker =>
                        marker == null ||
                        marker.Visual == null)
                .ToList();

        foreach (WarboardRuleMarker47 marker in expired)
            RemoveMarker(marker, "Marker visual expired.");
    }

    private void HandleRuleEvent(
        WarboardRuleEvent47 context)
    {
        if (context == null ||
            context.Type !=
                WarboardRuleEventType47.CoreEvent ||
            context.CoreContext == null ||
            context.CoreContext.Type !=
                GameEventType.MoveEnded ||
            context.Source == null)
        {
            return;
        }

        HandleUnitEndedMove(
            context.Source
        );
    }

    public void HandleUnitEndedMove(
        SquadController unit)
    {
        if (unit == null)
            return;

        List<WarboardRuleMarker47> remove =
            new List<WarboardRuleMarker47>();

        foreach (WarboardRuleMarker47 marker
            in markers)
        {
            if (marker == null ||
                !string.Equals(
                    marker.Type,
                    "TYRANID_TUNNEL",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    marker.FactionId,
                    unit.FactionId,
                    StringComparison.OrdinalIgnoreCase) ||
                unit.HasKeyword("AIRCRAFT"))
            {
                continue;
            }

            if (PointToUnitDistance(
                    marker.Position,
                    unit) <= 3.001f)
            {
                remove.Add(marker);
            }
        }

        foreach (WarboardRuleMarker47 marker
            in remove)
        {
            RemoveMarker(
                marker,
                "Enemy non-AIRCRAFT model ended a move within 3 inches."
            );
        }
    }

    private void RemoveClosedTunnelMarkersLegacy()
    {
        GameController game =
            GameController.Current;

        if (game == null)
            return;

        List<WarboardRuleMarker47> tunnels =
            markers
                .Where(
                    marker =>
                        marker != null &&
                        string.Equals(
                            marker.Type,
                            "TYRANID_TUNNEL",
                            StringComparison.OrdinalIgnoreCase))
                .ToList();

        foreach (WarboardRuleMarker47 marker
            in tunnels)
        {
            bool enemyWithinThree =
                game.StandardEnemyUnits(
                        marker.FactionId)
                    .Where(
                        unit =>
                            unit != null &&
                            unit.IsAlive &&
                            unit.IsOnBattlefield &&
                            !unit.HasKeyword("AIRCRAFT"))
                    .Any(
                        unit =>
                            PointToUnitDistance(
                                marker.Position,
                                unit) <=
                            3.001f);

            if (enemyWithinThree)
            {
                RemoveMarker(
                    marker,
                    "Enemy model ended within 3 inches of a Tunnel Marker."
                );
            }
        }
    }

    private void HandleCoreEvent(
        GameEventContext context)
    {
        if (context == null)
            return;

        if (context.Type == GameEventType.BattleStarted)
        {
            foreach (WarboardRuleMarker47 marker
                in markers.ToArray())
            {
                RemoveMarker(marker, "New battle.");
            }

            return;
        }

        WarboardRuleScope47? expiredScope = null;

        if (context.Type == GameEventType.PhaseEnded)
            expiredScope = WarboardRuleScope47.Phase;
        else if (context.Type == GameEventType.TurnEnded)
            expiredScope = WarboardRuleScope47.Turn;
        else if (context.Type == GameEventType.BattleRoundEnded)
            expiredScope = WarboardRuleScope47.Round;

        if (expiredScope.HasValue)
        {
            foreach (WarboardRuleMarker47 marker
                in markers
                    .Where(value =>
                        value != null &&
                        value.Scope ==
                            expiredScope.Value)
                    .ToArray())
            {
                RemoveMarker(
                    marker,
                    expiredScope.Value +
                    " marker expired.");
            }
        }
    }

    private void OnGUI()
    {
        if (pending == null)
            return;

        int oldDepth = GUI.depth;
        GUI.depth = -31000;

        Rect box =
            new Rect(
                (Screen.width - 560f) * 0.5f,
                Screen.height - 94f,
                560f,
                70f
            );

        GUI.Box(box, "");

        GUI.Label(
            new Rect(
                box.x + 12f,
                box.y + 8f,
                box.width - 100f,
                24f
            ),
            "PLACE " +
            (pending.Label ?? "RULE MARKER") +
            " - CLICK BATTLEFIELD"
        );

        GUI.Label(
            new Rect(
                box.x + 12f,
                box.y + 34f,
                box.width - 100f,
                24f
            ),
            string.IsNullOrWhiteSpace(
                placementError)
            ? "ESC cancels. Warboard validates all supplied placement constraints."
            : placementError
        );

        if (GUI.Button(
            new Rect(
                box.x + box.width - 82f,
                box.y + 18f,
                70f,
                32f
            ),
            "CANCEL"))
        {
            CancelPlacement();
        }

        GUI.depth = oldDepth;
    }

    public static float PointToUnitDistance(
        Vector3 point,
        SquadController unit)
    {
        if (unit == null)
            return float.MaxValue;

        return unit
            .JoinedActionController()
            .JoinedLivingModelTokens()
            .Where(model => model != null)
            .Select(
                model =>
                    PointToModelBaseDistance(
                        point,
                        model))
            .DefaultIfEmpty(
                float.MaxValue)
            .Min();
    }

    public static float PointToModelBaseDistance(
        Vector3 point,
        ModelToken model)
    {
        if (model == null)
            return float.MaxValue;

        Vector2 a =
            new Vector2(point.x, point.z);

        Vector2 b =
            new Vector2(
                model.transform.position.x,
                model.transform.position.z
            );

        return Mathf.Max(
            0f,
            Vector2.Distance(a, b) -
            Mathf.Max(
                0f,
                model.BaseRadiusInches)
        );
    }

    private static float HorizontalDistance(
        Vector3 first,
        Vector3 second)
    {
        return Vector2.Distance(
            new Vector2(first.x, first.z),
            new Vector2(second.x, second.z)
        );
    }
}
