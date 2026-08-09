using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 11e geometry helpers. Core distances are measured base-to-base where a
/// model has a base, with independent horizontal/vertical requirements for
/// coherency, engagement and objective range.
/// </summary>
public static class CoreRules11Geometry
{
    public const float CoherencyNeighbourHorizontal = 2f;
    public const float CoherencyAllHorizontal = 9f;
    public const float CoherencyVertical = 5f;
    public const float EngagementHorizontal = 2f;
    public const float EngagementVertical = 5f;
    public const float ObjectiveVertical = 5f;

    // Warboard gameplay tokens are centred 0.65" above the surface their base
    // sits on. Keep the geometry helper in one place so elevated terrain can
    // later replace this proxy convention without touching rules code.
    public const float TokenBasePlaneOffset = 0.65f;

    public static float ModelBasePlaneY(
        ModelToken model)
    {
        if (model == null)
            return 0f;

        return
            model.transform.position.y -
            TokenBasePlaneOffset;
    }

    public static float HorizontalBaseGap(
        ModelToken a,
        ModelToken b)
    {
        if (a == null || b == null)
            return float.MaxValue;

        Vector2 pa =
            new Vector2(
                a.transform.position.x,
                a.transform.position.z
            );

        Vector2 pb =
            new Vector2(
                b.transform.position.x,
                b.transform.position.z
            );

        return Mathf.Max(
            0f,
            Vector2.Distance(pa, pb) -
            Mathf.Max(0f, a.BaseRadiusInches) -
            Mathf.Max(0f, b.BaseRadiusInches)
        );
    }

    public static float VerticalBaseGap(
        ModelToken a,
        ModelToken b)
    {
        if (a == null || b == null)
            return float.MaxValue;

        return Mathf.Abs(
            ModelBasePlaneY(a) -
            ModelBasePlaneY(b)
        );
    }

    public static bool WithinCoherencyNeighbour(
        ModelToken a,
        ModelToken b)
    {
        return
            HorizontalBaseGap(a, b) <=
                CoherencyNeighbourHorizontal + 0.001f &&
            VerticalBaseGap(a, b) <=
                CoherencyVertical + 0.001f;
    }

    public static bool WithinCoherencyAll(
        ModelToken a,
        ModelToken b)
    {
        return
            HorizontalBaseGap(a, b) <=
                CoherencyAllHorizontal + 0.001f &&
            VerticalBaseGap(a, b) <=
                CoherencyVertical + 0.001f;
    }

    public static bool ModelsEngaged(
        ModelToken a,
        ModelToken b)
    {
        return
            HorizontalBaseGap(a, b) <=
                EngagementHorizontal + 0.001f &&
            VerticalBaseGap(a, b) <=
                EngagementVertical + 0.001f;
    }

    public static bool UnitsEngaged(
        SquadController a,
        SquadController b)
    {
        if (a == null || b == null ||
            !a.IsAlive || !b.IsAlive ||
            !a.IsOnBattlefield ||
            !b.IsOnBattlefield)
        {
            return false;
        }

        IReadOnlyList<ModelToken> modelsA =
            a.JoinedLivingModelTokens();

        IReadOnlyList<ModelToken> modelsB =
            b.JoinedLivingModelTokens();

        foreach (ModelToken modelA in modelsA)
        {
            foreach (ModelToken modelB in modelsB)
            {
                if (ModelsEngaged(
                        modelA,
                        modelB))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool ModelWithinObjective(
        ModelToken model,
        Vector3 objectivePosition,
        float horizontalRange)
    {
        if (model == null || !model.IsAlive)
            return false;

        Vector2 modelPoint =
            new Vector2(
                model.transform.position.x,
                model.transform.position.z
            );

        Vector2 objectivePoint =
            new Vector2(
                objectivePosition.x,
                objectivePosition.z
            );

        float horizontalGap =
            Mathf.Max(
                0f,
                Vector2.Distance(
                    modelPoint,
                    objectivePoint
                ) -
                Mathf.Max(
                    0f,
                    model.BaseRadiusInches
                )
            );

        float verticalGap =
            Mathf.Abs(
                ModelBasePlaneY(model) -
                objectivePosition.y
            );

        return
            horizontalGap <=
                horizontalRange + 0.001f &&
            verticalGap <=
                ObjectiveVertical + 0.001f;
    }
}

/// <summary>
/// Generic 11e action eligibility shared by mission actions and future faction
/// actions. Mission-specific conditions remain in MissionSystem.
/// </summary>
public static class CoreRules11Actions
{
    public static bool CanStart(
        GameController game,
        SquadController unit,
        out string reason)
    {
        reason = "";

        if (unit == null ||
            !unit.IsAlive ||
            !unit.IsOnBattlefield)
        {
            reason =
                "That unit is not on the battlefield.";
            return false;
        }

        SquadController actionUnit =
            unit.JoinedActionController();

        if (actionUnit.HasKeyword("AIRCRAFT") ||
            actionUnit.HasKeyword("FORTIFICATION"))
        {
            reason =
                "AIRCRAFT and FORTIFICATION units cannot start actions.";
            return false;
        }

        if (actionUnit.IsBattleShocked)
        {
            reason =
                "Battle-shocked units cannot start actions.";
            return false;
        }

        bool hasPositiveOc =
            actionUnit
                .JoinedLivingModelTokens()
                .Any(
                    model =>
                        model != null &&
                        model.IsAlive &&
                        model.ObjectiveControl > 0
                );

        if (!hasPositiveOc)
        {
            reason =
                "A unit with no model of OC 1+ cannot start an action.";
            return false;
        }

        if (game != null &&
            game.IsUnitEngagedPublic(
                actionUnit) &&
            !actionUnit.HasKeyword("TITANIC"))
        {
            reason =
                "An engaged unit cannot start an action unless it is TITANIC.";
            return false;
        }

        if (actionUnit.HasAdvanced ||
            actionUnit.HasFallenBack)
        {
            reason =
                "A unit that Advanced or Fell Back this turn cannot start an action.";
            return false;
        }

        if (actionUnit.StartedMissionActionThisTurn)
        {
            reason =
                "That unit has already started an action this turn.";
            return false;
        }

        return true;
    }
}
