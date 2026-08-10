using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Small v47 bridge for generic marker/placement/enhancement systems. It keeps
/// the new reusable engines out of GameController's private implementation.
/// </summary>
public partial class GameController
{
    public void StandardCloseRuleChoice()
    {
        CloseRuleChoice();
    }

    public bool StandardAllModelsInsideBoard(
        SquadController unit)
    {
        return unit != null &&
            AllModelsInsideBoard(
                unit.JoinedActionController());
    }

    public bool StandardAllModelsHaveLegalPlacement(
        SquadController unit)
    {
        return unit != null &&
            AllModelsHaveLegalPlacement(
                unit.JoinedActionController());
    }

    public bool StandardIsEngaged(
        SquadController unit)
    {
        return unit != null &&
            IsEngaged(
                unit.JoinedActionController());
    }

    public bool StandardUnitWithinControlledObjective(
        SquadController unit,
        string factionId)
    {
        if (unit == null)
            return false;

        return objectives.Any(
            objective =>
                objective != null &&
                objective.UnitWithinRange(
                    unit.JoinedActionController()) &&
                string.Equals(
                    objective.Controller(squads),
                    factionId,
                    StringComparison.OrdinalIgnoreCase)
        );
    }

    public bool StandardUnitWithinObjective(
        SquadController unit,
        ObjectiveController objective)
    {
        return unit != null &&
            objective != null &&
            objective.UnitWithinRange(
                unit.JoinedActionController());
    }

    public string StandardObjectiveController(
        ObjectiveController objective)
    {
        return objective != null
            ? objective.Controller(squads)
            : null;
    }

    public ObjectiveController StandardNearestObjective(
        SquadController unit)
    {
        if (unit == null)
            return null;

        Vector3 centre =
            unit.JoinedActionController()
                .CurrentCentre();

        return objectives
            .Where(value => value != null)
            .OrderBy(
                value =>
                    Vector2.Distance(
                        new Vector2(
                            centre.x,
                            centre.z),
                        new Vector2(
                            value.transform.position.x,
                            value.transform.position.z)))
            .FirstOrDefault();
    }

    public bool StandardUnitIsHidden(
        SquadController unit)
    {
        if (unit == null)
            return false;

        return unit
            .JoinedActionController()
            .JoinedLivingModelTokens()
            .Any(model =>
                model != null &&
                Core11ModelIsHidden(model));
    }

    public bool StandardUnitVisibleToUnit(
        SquadController observer,
        SquadController target)
    {
        if (observer == null ||
            target == null)
        {
            return false;
        }

        return observer
            .JoinedActionController()
            .JoinedLivingModelTokens()
            .Any(
                model =>
                    model != null &&
                    ModelCanSeeUnit(
                        model,
                        target.JoinedActionController()));
    }

    public int StandardApplyMortalWounds(
        SquadController target,
        int amount,
        string label)
    {
        if (target == null ||
            amount <= 0)
        {
            return 0;
        }

        SquadController action =
            target.JoinedActionController();

        int remaining = amount;
        int lost = 0;

        while (remaining > 0 &&
               action.IsAlive)
        {
            ModelToken model =
                action.GetAutomaticAllocationModel();

            if (model == null &&
                action.AttachedLeader != null)
            {
                model =
                    action.AttachedLeader
                        .GetAutomaticAllocationModel();
            }

            if (model == null)
                break;

            int one =
                Mathf.Min(
                    remaining,
                    model.CurrentWounds);

            int afterFnp =
                UniversalRuleRegistry.ApplyFeelNoPain(
                    model.Squad,
                    one,
                    label ?? "Mortal wounds");

            int applied =
                model.ApplyDamage(afterFnp);

            lost += applied;
            remaining -= one;
        }

        action.RefreshVisuals();

        if (action.AttachedLeader != null)
            action.AttachedLeader.RefreshVisuals();

        return lost;
    }
}
