using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WarboardSpecialPlacementKind47
{
    Translation,
    Reposition,
    ReactiveMove,
    SurgeMove
}

public sealed class WarboardSpecialPlacementRequest47
{
    public SquadController Unit;
    public string Label = "SPECIAL MOVE";
    public WarboardSpecialPlacementKind47 Kind =
        WarboardSpecialPlacementKind47.Translation;
    public float MaximumDistance;
    public float MinimumEnemyDistance;
    public WarboardRuleMarker47 MustFinishWithinMarker;
    public float MarkerDistance;
    public SquadController MustFinishCloserToUnit;
    public ObjectiveController MustFinishCloserToObjective;
    public bool RequireUnengagedEnd;
    public bool IgnorePathObstructions;
    public Action Completed;
    public Action Cancelled;
}

/// <summary>
/// Generic endpoint-validated special movement/reposition engine. Unlike a
/// normal move it can represent reactions, teleports and rules that explicitly
/// allow models to pass through intervening terrain/models while still enforcing
/// legal final placement, board bounds, enemy-distance and closer-to constraints.
/// </summary>
[DefaultExecutionOrder(-31760)]
public sealed class WarboardSpecialPlacement47 : MonoBehaviour
{
    public static WarboardSpecialPlacement47 Instance
    {
        get;
        private set;
    }

    private WarboardSpecialPlacementRequest47 pending;
    private Dictionary<ModelToken, Vector3> startPositions;
    private Vector3 startCentre;
    private float startTargetDistance;
    private string error = "";

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object.FindAnyObjectByType<
                WarboardSpecialPlacement47>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "Warboard Special Placement v47"
            );

        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<WarboardSpecialPlacement47>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Begin(
        WarboardSpecialPlacementRequest47 request)
    {
        if (request == null ||
            request.Unit == null)
        {
            return;
        }

        pending = request;
        pending.Unit =
            request.Unit.JoinedActionController();

        startPositions =
            CaptureJoinedPositions(
                pending.Unit);

        startCentre =
            JoinedCentre(
                pending.Unit);

        startTargetDistance =
            ConstraintDistance(
                pending,
                startCentre);

        error = "";

        GameController game =
            GameController.Current;

        if (game != null)
        {
            game.StandardSetStatus(
                pending.Label +
                ": click a legal destination. ESC cancels."
            );
        }
    }

    public void Cancel()
    {
        if (pending == null)
            return;

        Action cancelled =
            pending.Cancelled;

        pending = null;
        startPositions = null;
        error = "";

        if (cancelled != null)
            cancelled();
    }

    private void Update()
    {
        if (pending == null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
            return;
        }

        if (Input.GetMouseButtonDown(0))
            TryPlaceFromCursor();
    }

    private void TryPlaceFromCursor()
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            error = "No game camera.";
            return;
        }

        Ray ray =
            camera.ScreenPointToRay(
                Input.mousePosition);

        Plane plane =
            new Plane(
                Vector3.up,
                new Vector3(0f, 0.05f, 0f));

        float enter;

        if (!plane.Raycast(ray, out enter))
        {
            error = "Click the battlefield.";
            return;
        }

        Vector3 destination =
            ray.GetPoint(enter);

        destination.y = startCentre.y;

        Vector3 delta =
            destination - startCentre;

        delta.y = 0f;

        if (pending.MaximumDistance > 0f &&
            delta.magnitude >
                pending.MaximumDistance + 0.001f)
        {
            error =
                "Maximum distance is " +
                pending.MaximumDistance.ToString("0.#") +
                " inches.";
            return;
        }

        ApplyDelta(
            pending.Unit,
            startPositions,
            delta);

        Physics.SyncTransforms();

        string reason;

        if (!ValidateEnd(
                pending,
                out reason))
        {
            Restore(startPositions);
            Physics.SyncTransforms();
            error = reason;

            GameController game =
                GameController.Current;

            if (game != null)
                game.StandardSetStatus(reason);

            return;
        }

        WarboardSpecialPlacementRequest47 completedRequest =
            pending;

        pending = null;
        startPositions = null;
        error = "";

        GameController owner =
            GameController.Current;

        if (owner != null)
        {
            owner.StandardLog(
                "SPECIAL MOVE",
                completedRequest.Label,
                completedRequest.Unit.DisplayName +
                " moved/repositioned " +
                delta.magnitude.ToString("0.0") +
                " inches using the v47 special-placement engine."
            );
        }

        WarboardMarkerSystem47 markerSystem =
            WarboardMarkerSystem47.Instance;

        if (markerSystem != null)
        {
            markerSystem.HandleUnitEndedMove(
                completedRequest.Unit
            );
        }

        if (completedRequest.Completed != null)
            completedRequest.Completed();
    }

    private bool ValidateEnd(
        WarboardSpecialPlacementRequest47 request,
        out string reason)
    {
        reason = "";

        GameController game =
            GameController.Current;

        if (game == null)
        {
            reason = "Game controller unavailable.";
            return false;
        }

        SquadController unit =
            request.Unit.JoinedActionController();

        if (!game.StandardAllModelsInsideBoard(
                unit))
        {
            reason =
                "Every model must finish wholly on the battlefield.";
            return false;
        }

        if (!unit.IsCoherent())
        {
            reason =
                "The unit must finish in coherency.";
            return false;
        }

        if (!game.StandardAllModelsHaveLegalPlacement(
                unit))
        {
            reason =
                "One or more models overlap a model or blocking terrain at the destination.";
            return false;
        }

        if (request.RequireUnengagedEnd &&
            game.StandardIsEngaged(unit))
        {
            reason =
                "This rule requires the unit to finish outside Engagement Range.";
            return false;
        }

        if (request.MinimumEnemyDistance > 0f)
        {
            float nearest =
                game.StandardEnemyUnits(
                        unit.FactionId)
                    .Where(
                        enemy =>
                            enemy != null &&
                            enemy.IsAlive &&
                            enemy.IsOnBattlefield)
                    .Select(
                        enemy =>
                            game.StandardDistance(
                                unit,
                                enemy))
                    .DefaultIfEmpty(
                        float.MaxValue)
                    .Min();

            if (nearest <=
                request.MinimumEnemyDistance +
                0.001f)
            {
                reason =
                    "The unit must finish more than " +
                    request.MinimumEnemyDistance
                        .ToString("0.#") +
                    " inches from all enemy units.";
                return false;
            }
        }

        if (request.MustFinishWithinMarker != null &&
            request.MarkerDistance > 0f)
        {
            WarboardMarkerSystem47 markers =
                WarboardMarkerSystem47.Instance;

            if (markers == null ||
                !markers.UnitWhollyWithin(
                    unit,
                    request.MustFinishWithinMarker,
                    request.MarkerDistance))
            {
                reason =
                    "The unit must finish wholly within " +
                    request.MarkerDistance.ToString("0.#") +
                    " inches of " +
                    request.MustFinishWithinMarker.Label +
                    ".";
                return false;
            }
        }

        if (request.MustFinishCloserToUnit != null ||
            request.MustFinishCloserToObjective != null)
        {
            float after =
                ConstraintDistance(
                    request,
                    JoinedCentre(unit));

            if (after >=
                startTargetDistance - 0.001f)
            {
                reason =
                    "The unit must finish closer to the required target.";
                return false;
            }
        }

        return true;
    }

    private static Dictionary<ModelToken, Vector3>
        CaptureJoinedPositions(
            SquadController unit)
    {
        Dictionary<ModelToken, Vector3> result =
            new Dictionary<ModelToken, Vector3>();

        if (unit == null)
            return result;

        foreach (ModelToken model
            in unit.JoinedLivingModelTokens())
        {
            if (model != null)
                result[model] =
                    model.transform.position;
        }

        return result;
    }

    private static void ApplyDelta(
        SquadController unit,
        Dictionary<ModelToken, Vector3> positions,
        Vector3 delta)
    {
        if (positions == null)
            return;

        foreach (KeyValuePair<ModelToken, Vector3> pair
            in positions)
        {
            if (pair.Key != null &&
                pair.Key.IsAlive)
            {
                pair.Key.transform.position =
                    pair.Value + delta;
            }
        }

        if (unit != null)
        {
            unit.RefreshVisuals();

            if (unit.AttachedLeader != null)
                unit.AttachedLeader.RefreshVisuals();
        }
    }

    private static void Restore(
        Dictionary<ModelToken, Vector3> positions)
    {
        if (positions == null)
            return;

        foreach (KeyValuePair<ModelToken, Vector3> pair
            in positions)
        {
            if (pair.Key != null)
                pair.Key.transform.position =
                    pair.Value;
        }
    }

    private static Vector3 JoinedCentre(
        SquadController unit)
    {
        if (unit == null)
            return Vector3.zero;

        List<ModelToken> models =
            unit.JoinedLivingModelTokens()
                .Where(value => value != null)
                .ToList();

        if (models.Count == 0)
            return unit.transform.position;

        Vector3 sum = Vector3.zero;

        foreach (ModelToken model in models)
            sum += model.transform.position;

        return sum / models.Count;
    }

    private static float ConstraintDistance(
        WarboardSpecialPlacementRequest47 request,
        Vector3 point)
    {
        if (request.MustFinishCloserToUnit != null)
        {
            return WarboardMarkerSystem47
                .PointToUnitDistance(
                    point,
                    request.MustFinishCloserToUnit);
        }

        if (request.MustFinishCloserToObjective != null)
        {
            Vector2 a =
                new Vector2(point.x, point.z);

            Vector3 target =
                request.MustFinishCloserToObjective
                    .transform.position;

            Vector2 b =
                new Vector2(target.x, target.z);

            return Vector2.Distance(a, b);
        }

        return 0f;
    }

    private void OnGUI()
    {
        if (pending == null)
            return;

        int oldDepth = GUI.depth;
        GUI.depth = -30900;

        Rect box =
            new Rect(
                (Screen.width - 620f) * 0.5f,
                Screen.height - 170f,
                620f,
                72f);

        GUI.Box(box, "");

        GUI.Label(
            new Rect(
                box.x + 12f,
                box.y + 7f,
                box.width - 100f,
                25f),
            pending.Label +
            " - CLICK DESTINATION");

        GUI.Label(
            new Rect(
                box.x + 12f,
                box.y + 34f,
                box.width - 100f,
                24f),
            string.IsNullOrWhiteSpace(error)
            ? "Endpoint legality, coherency and supplied rule constraints are validated. ESC cancels."
            : error);

        if (GUI.Button(
            new Rect(
                box.x + box.width - 82f,
                box.y + 18f,
                70f,
                32f),
            "CANCEL"))
        {
            Cancel();
        }

        GUI.depth = oldDepth;
    }
}
