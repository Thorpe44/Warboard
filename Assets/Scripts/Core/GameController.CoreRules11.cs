using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameController
{
    /// <summary>
    /// 11e requires objective control to be determined before any other
    /// end-of-phase/end-of-turn rules. Controller() is live rather than stored,
    /// but resolving secured control and refreshing here fixes the ordering of
    /// stateful objective effects before later rules/scoring execute.
    /// </summary>
    private void ResolveCoreObjectiveControlTiming()
    {
        foreach (ObjectiveController objective
            in objectives)
        {
            if (objective == null)
                continue;

            objective.ResolveSecuredControlAtEndOfPhase(
                squads
            );

            objective.RefreshStatus(
                squads
            );
        }
    }

    /// <summary>
    /// Units that did nothing in the Move Units step are resolved as Remain
    /// Stationary when the player closes the Movement phase. This deliberately
    /// does not emit MoveStarted/MoveEnded: Remain Stationary is a move type but
    /// does not trigger rules that occur when a move starts or ends.
    /// </summary>
    private void ResolveImplicitRemainStationarySelections()
    {
        foreach (SquadController unit
            in squads)
        {
            if (unit == null ||
                unit.IsAttachedLeader ||
                !unit.IsAlive ||
                unit.FactionId != activeFaction)
            {
                continue;
            }

            // A battlefield move, reserve ingress/setup, Advance or Fall Back
            // already represents that unit being selected in this Move Units
            // step. Units left untouched are selected to Remain Stationary.
            if (unit.HasMoved ||
                unit.HasAdvanced ||
                unit.HasFallenBack ||
                unit.WasSetUpThisTurn)
            {
                continue;
            }

            GameEventBus.Raise(
                new GameEventContext
                {
                    Type =
                        GameEventType.UnitSelectedToMove,
                    Game = this,
                    ActingFaction =
                        unit.FactionId,
                    Phase = phase,
                    Source = unit,
                    Note =
                        "Remain Stationary"
                }
            );
        }
    }
}
