using UnityEngine;

public enum TerrainTrait
{
    Blocking,
    Cover,
    Traversable
}

public class TerrainFeature : MonoBehaviour
{
    public TerrainTrait Trait
    {
        get;
        private set;
    }

    public string MissionTerrainId
    {
        get;
        private set;
    } = "";

    public bool IsMissionObjectiveArea
    {
        get;
        private set;
    }

    public string OperationMarkerOwner
    {
        get;
        private set;
    } = "";

    public string TrappedByFaction
    {
        get;
        private set;
    } = "";

    public int TrappedRound
    {
        get;
        private set;
    }

    public void Initialize(
        TerrainTrait trait)
    {
        Initialize(
            trait,
            "",
            false
        );
    }

    public void Initialize(
        TerrainTrait trait,
        string missionTerrainId,
        bool isMissionObjectiveArea)
    {
        Trait = trait;

        MissionTerrainId =
            missionTerrainId ?? "";

        IsMissionObjectiveArea =
            isMissionObjectiveArea;
    }

    public bool BlocksLineOfSight
    {
        get
        {
            return
                Trait ==
                TerrainTrait.Blocking;
        }
    }

    public bool GrantsCover
    {
        get
        {
            return
                Trait ==
                TerrainTrait.Cover;
        }
    }

    public bool BlocksMovement
    {
        get
        {
            return
                Trait !=
                TerrainTrait.Traversable;
        }
    }

    public void SetOperationMarker(
        string faction)
    {
        OperationMarkerOwner =
            faction ?? "";
    }

    public void ClearOperationMarker()
    {
        OperationMarkerOwner = "";
    }

    public bool HasOperationMarker(
        string faction = null)
    {
        if (string.IsNullOrWhiteSpace(
                OperationMarkerOwner))
        {
            return false;
        }

        return
            string.IsNullOrWhiteSpace(
                faction) ||
            OperationMarkerOwner ==
                faction;
    }

    public void Trap(
        string faction,
        int round)
    {
        TrappedByFaction =
            faction ?? "";

        TrappedRound =
            Mathf.Max(
                0,
                round
            );
    }

    public bool WasTrappedBy(
        string faction,
        int round)
    {
        return
            !string.IsNullOrWhiteSpace(
                faction) &&
            TrappedByFaction ==
                faction &&
            TrappedRound ==
                round;
    }
}
