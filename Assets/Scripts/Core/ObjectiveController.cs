using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectiveController : MonoBehaviour
{
    public const float ControlRadius = 3f;

    private TextMesh statusText;
    private string securedByFaction;
    private Renderer markerRenderer;

    private readonly HashSet<string>
        missionStates =
            new HashSet<string>();

    public MissionObjectiveRole MissionRole
    {
        get;
        private set;
    } =
        MissionObjectiveRole.Neutral;

    public void Initialize(
        Vector3 position)
    {
        Initialize(
            position,
            MissionObjectiveRole.Neutral
        );
    }

    public void Initialize(
        Vector3 position,
        MissionObjectiveRole role)
    {
        transform.position = position;
        MissionRole = role;

        SphereCollider missionClickCollider =
            gameObject.AddComponent<
                SphereCollider
            >();

        missionClickCollider.radius = 1.15f;
        missionClickCollider.center =
            new Vector3(
                0f,
                0.35f,
                0f
            );
        missionClickCollider.isTrigger = true;

        GameObject marker =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        marker.name = "ObjectiveMarker";
        marker.transform.SetParent(
            transform,
            false
        );
        marker.transform.localPosition =
            new Vector3(0f, 0.03f, 0f);
        marker.transform.localScale =
            new Vector3(
                ControlRadius * 2f,
                0.04f,
                ControlRadius * 2f
            );

        Collider col =
            marker.GetComponent<Collider>();

        if (col != null)
            Object.Destroy(col);

        GameController.SetObjectColor(
            marker,
            new Color(
                0.75f,
                0.65f,
                0.12f
            )
        );

        markerRenderer =
            marker.GetComponent<Renderer>();

        WarboardV45Presentation.StyleObjectiveMarker(
            marker,
            transform,
            role
        );

        CreateStatusText();
    }

    public Dictionary<string, int>
        ObjectiveControlTotals(
            List<SquadController> squads)
    {
        Dictionary<string, int> totals =
            new Dictionary<string, int>();

        foreach (SquadController squad
            in squads)
        {
            if (squad == null ||
                !squad.IsAlive ||
                !squad.IsOnBattlefield ||
                squad.IsAttachedLeader)
            {
                continue;
            }

            int oc =
                squad.TotalObjectiveControlWithin(
                    transform.position,
                    ControlRadius
                );

            if (oc <= 0)
                continue;

            if (!totals.ContainsKey(
                squad.FactionId))
            {
                totals[squad.FactionId] = 0;
            }

            totals[squad.FactionId] += oc;
        }

        return totals;
    }

    public bool UnitWithinRange(
        SquadController squad)
    {
        if (squad == null ||
            !squad.IsAlive ||
            !squad.IsOnBattlefield)
        {
            return false;
        }

        return squad
            .JoinedLivingModelTokens()
            .Any(
                model =>
                    CoreRules11Geometry
                        .ModelWithinObjective(
                            model,
                            transform.position,
                            ControlRadius
                        )
            );
    }

    private string MissionStateKey(
        string faction,
        string state)
    {
        return
            (faction ?? "") +
            "|" +
            (state ?? "");
    }

    public bool HasMissionState(
        string faction,
        string state)
    {
        return missionStates.Contains(
            MissionStateKey(
                faction,
                state
            )
        );
    }

    public void SetMissionState(
        string faction,
        string state,
        bool value = true)
    {
        string key =
            MissionStateKey(
                faction,
                state
            );

        if (value)
            missionStates.Add(key);
        else
            missionStates.Remove(key);
    }

    public int MissionStateCount()
    {
        return missionStates.Count;
    }

    public void SecureFor(string faction)
    {
        securedByFaction =
            faction;
    }

    public string SecuredByFaction
    {
        get { return securedByFaction; }
    }

    public void ResolveSecuredControlAtEndOfPhase(
        List<SquadController> squads)
    {
        if (string.IsNullOrWhiteSpace(
                securedByFaction))
        {
            return;
        }

        Dictionary<string, int> totals =
            ObjectiveControlTotals(
                squads
            );

        int owner = 0;
        totals.TryGetValue(
            securedByFaction,
            out owner
        );

        foreach (
            KeyValuePair<string, int> pair
            in totals)
        {
            if (pair.Key !=
                    securedByFaction &&
                pair.Value > owner)
            {
                securedByFaction = null;
                return;
            }
        }
    }

    public string Controller(
        List<SquadController> squads)
    {
        Dictionary<string, int> totals =
            ObjectiveControlTotals(
                squads
            );

        string bestFaction = null;
        int bestOC = 0;
        bool tie = false;

        foreach (
            KeyValuePair<string, int> pair
            in totals)
        {
            if (pair.Value > bestOC)
            {
                bestFaction = pair.Key;
                bestOC = pair.Value;
                tie = false;
            }
            else if (pair.Value == bestOC &&
                     pair.Value > 0)
            {
                tie = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(
                securedByFaction))
        {
            int ownerOC = 0;
            totals.TryGetValue(
                securedByFaction,
                out ownerOC
            );

            bool opponentGreater =
                totals.Any(
                    pair =>
                        pair.Key !=
                            securedByFaction &&
                        pair.Value > ownerOC
                );

            if (!opponentGreater)
                return securedByFaction;
        }

        return tie
            ? null
            : bestFaction;
    }

    public void RefreshStatus(
        List<SquadController> squads)
    {
        if (statusText == null)
            return;

        Dictionary<string, int> totals =
            ObjectiveControlTotals(squads);

        string controller =
            Controller(squads);

        RefreshMarkerColor(
            totals,
            controller
        );

        if (totals.Count == 0 &&
            string.IsNullOrWhiteSpace(
                securedByFaction))
        {
            statusText.text =
                ObjectiveRoleLabel() +
                "\nUncontrolled" +
                MissionStateLabel();
            return;
        }

        string values =
            string.Join(
                " / ",
                totals.Select(
                    pair =>
                        pair.Key +
                        " " +
                        pair.Value +
                        " OC"
                ).ToArray()
            );

        statusText.text =
            ObjectiveRoleLabel() +
            "\n" +
            (controller == null
                ? "Contested"
                : controller) +
            "\n" +
            values +
            MissionStateLabel();
    }

    private void RefreshMarkerColor(
        Dictionary<string, int> totals,
        string controller)
    {
        if (markerRenderer == null)
            return;

        bool physicallyContested =
            totals != null &&
            totals.Count > 0 &&
            string.IsNullOrWhiteSpace(
                controller
            );

        Color color;

        if (!string.IsNullOrWhiteSpace(
                controller))
        {
            color =
                GameController.FactionColor(
                    controller
                );

            if (statusText != null)
            {
                statusText.color =
                    Color.Lerp(
                        color,
                        Color.white,
                        0.30f
                    );
            }
        }
        else if (physicallyContested)
        {
            float pulse =
                0.5f +
                0.5f *
                Mathf.Sin(
                    Time.time *
                    5f
                );

            color =
                Color.Lerp(
                    new Color(
                        0.72f,
                        0.22f,
                        0.18f,
                        1f
                    ),
                    new Color(
                        1f,
                        0.78f,
                        0.22f,
                        1f
                    ),
                    pulse
                );

            if (statusText != null)
                statusText.color =
                    Color.white;
        }
        else
        {
            color =
                new Color(
                    0.62f,
                    0.62f,
                    0.66f,
                    1f
                );

            if (statusText != null)
                statusText.color =
                    Color.white;
        }

        markerRenderer.material.color =
            color;

        if (markerRenderer.material
            .HasProperty(
                "_EmissionColor"))
        {
            markerRenderer.material
                .EnableKeyword(
                    "_EMISSION"
                );

            markerRenderer.material
                .SetColor(
                    "_EmissionColor",
                    color *
                    1.35f
                );
        }
    }

    private string MissionStateLabel()
    {
        if (missionStates.Count == 0)
            return "";

        List<string> tags =
            new List<string>();

        foreach (string key
            in missionStates)
        {
            string[] parts =
                key.Split('|');

            string state =
                parts.Length > 1
                ? parts[1]
                : key;

            if (state == "decoyed")
                tags.Add("DECOY");
            else if (state == "triangulated")
                tags.Add("TRIANGULATED");
            else if (state == "intel")
                tags.Add("INTEL");
            else if (state == "secured_asset")
                tags.Add("SECURED");
            else if (state == "consecrated")
                tags.Add("CONSECRATED");
        }

        return
            tags.Count == 0
            ? ""
            : "\\n" +
              string.Join(
                  ",",
                  tags.Distinct()
                      .ToArray()
              );
    }

    private string ObjectiveRoleLabel()
    {
        switch (MissionRole)
        {
            case MissionObjectiveRole.PlayerOneHome:
                return "HOME P1";
            case MissionObjectiveRole.PlayerTwoHome:
                return "HOME P2";
            case MissionObjectiveRole.Central:
                return "CENTRAL";
            case MissionObjectiveRole.Expansion:
                return "EXPANSION";
            default:
                return "OBJ";
        }
    }

    private void CreateStatusText()
    {
        GameObject textObject =
            new GameObject(
                "Objective Status"
            );

        textObject.transform.SetParent(
            transform,
            false
        );

        textObject.transform.localPosition =
            new Vector3(
                0f,
                0.35f,
                0f
            );

        statusText =
            textObject.AddComponent<TextMesh>();

        Font font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        if (font != null)
        {
            statusText.font = font;

            MeshRenderer renderer =
                textObject.GetComponent<MeshRenderer>();

            if (renderer != null)
                renderer.sharedMaterial =
                    font.material;
        }

        statusText.anchor =
            TextAnchor.MiddleCenter;
        statusText.alignment =
            TextAlignment.Center;
        statusText.fontSize = 40;
        statusText.characterSize = 0.045f;
        statusText.color = Color.white;

        textObject.AddComponent<
            WoundDisplayBillboard
        >();
    }
}
