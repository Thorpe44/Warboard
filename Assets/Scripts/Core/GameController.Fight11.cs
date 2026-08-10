using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// WARBOARD_V40_FIGHT_PHASE_COMPLIANCE
//
// 11e Fight phase state machine. The physical GameController split is retained;
// this partial owns the edition-specific sequencing while the existing model-
// level attack and combat-movement tools remain reusable.
public partial class GameController
{
    private enum Fight11Step
    {
        None,
        PileIn,
        Fight,
        Consolidate,
        Complete
    }

    private enum Fight11ConsolidationMode
    {
        None,
        Ongoing,
        Engaging,
        Objective
    }

    private Fight11Step fight11Step = Fight11Step.None;
    private Fight11ConsolidationMode fight11ConsolidationMode =
        Fight11ConsolidationMode.None;

    private string fight11StepFaction = "";

    private readonly HashSet<SquadController> fight11PileInCompleted =
        new HashSet<SquadController>();

    private readonly HashSet<SquadController> fight11ConsolidationCompleted =
        new HashSet<SquadController>();

    private readonly HashSet<SquadController> fight11EngagedAtFightStepStart =
        new HashSet<SquadController>();

    private readonly HashSet<SquadController> fight11UnengagedAtFightStepStart =
        new HashSet<SquadController>();

    private readonly HashSet<SquadController> fight11EverEligibleToFight =
        new HashSet<SquadController>();

    private readonly List<SquadController> fight11MoveTargets =
        new List<SquadController>();

    private ObjectiveController fight11ConsolidationObjective;

    private readonly Dictionary<ModelToken, List<SquadController>>
        fight11ModelStartEngagements =
            new Dictionary<ModelToken, List<SquadController>>();

    private readonly Dictionary<ModelToken, float>
        fight11ModelStartTargetDistances =
            new Dictionary<ModelToken, float>();

    private readonly HashSet<ModelToken> fight11BaseContactLockedModels =
        new HashSet<ModelToken>();

    private bool fight11OverrunInProgress;
    private bool fight11CurrentFightWasOverrun;

    private readonly List<SquadController> fight11ForcedConsolidationFights =
        new List<SquadController>();

    private string fight11ConsolidationResumeFaction = "";
    private bool fight11ForcedFightActive;

    private void Fight11BeginPileInStep()
    {
        ClearFightActivationState();

        fight11Step = Fight11Step.PileIn;
        fight11StepFaction = activeFaction;
        fight11ConsolidationMode = Fight11ConsolidationMode.None;
        fight11ConsolidationObjective = null;
        fight11OverrunInProgress = false;
        fight11CurrentFightWasOverrun = false;
        fight11ForcedFightActive = false;
        fight11ConsolidationResumeFaction = "";

        fight11PileInCompleted.Clear();
        fight11ConsolidationCompleted.Clear();
        fight11EngagedAtFightStepStart.Clear();
        fight11UnengagedAtFightStepStart.Clear();
        fight11EverEligibleToFight.Clear();
        fight11ForcedConsolidationFights.Clear();

        // Fight selections are phase-local for both players. Reset this state
        // for every joined unit, not just the player whose turn it is.
        foreach (SquadController raw in squads)
        {
            if (raw == null || raw.IsAttachedLeader)
                continue;

            SquadController unit = raw.JoinedActionController();
            unit.HasFought = false;
            unit.KatahChoiceMadeThisFight = false;

            if (unit.AttachedLeader != null)
                unit.AttachedLeader.HasFought = false;
        }

        fightSequenceActive = true;
        fightPriorityStep = FightPriorityStep.None;
        fightSelectionFaction = "";

        AppendBattleLog(
            "FIGHT",
            "12.02 Pile In",
            DisplayFactionName(activeFaction) +
            " resolves all optional pile-in moves first, then the opponent."
        );

        Fight11SkipEmptyPileInSides();
    }

    private void Fight11SkipEmptyPileInSides()
    {
        while (fight11Step == Fight11Step.PileIn &&
               !Fight11SideHasAvailablePileIn(fight11StepFaction))
        {
            string opponent = OtherFaction(fight11StepFaction);

            if (fight11StepFaction == activeFaction &&
                opponent != fight11StepFaction)
            {
                fight11StepFaction = opponent;
                continue;
            }

            Fight11BeginFightStep();
            return;
        }

        if (fight11Step == Fight11Step.PileIn)
        {
            status =
                "PILE IN STEP  -  " +
                DisplayFactionName(fight11StepFaction) +
                " resolves optional pile-ins. Select a unit and an eligible enemy, or click DONE SIDE PILE-IN.";
        }
    }

    private bool Fight11SideHasAvailablePileIn(string faction)
    {
        return squads.Any(
            unit =>
                Fight11CanPileInUnit(unit, faction)
        );
    }

    private bool Fight11CanPileInUnit(
        SquadController unit,
        string faction)
    {
        if (unit == null ||
            unit.IsAttachedLeader ||
            !unit.IsAlive ||
            !unit.IsOnBattlefield ||
            unit.FactionId != faction)
        {
            return false;
        }

        SquadController actionUnit = unit.JoinedActionController();

        if (fight11PileInCompleted.Contains(actionUnit))
            return false;

        if (IsEngaged(actionUnit))
            return true;

        if (!actionUnit.MadeChargeMove)
            return false;

        return Fight11EnemiesWithin(actionUnit, 5f).Count > 0;
    }

    private void Fight11FinishCurrentPileInSide()
    {
        if (fight11Step != Fight11Step.PileIn ||
            fightActivationStage != FightActivationStage.None)
        {
            return;
        }

        string opponent = OtherFaction(fight11StepFaction);

        if (fight11StepFaction == activeFaction &&
            opponent != fight11StepFaction)
        {
            fight11StepFaction = opponent;
            Fight11SkipEmptyPileInSides();
            return;
        }

        Fight11BeginFightStep();
    }

    private void Fight11BeginFightStep()
    {
        ClearFightActivationState();

        fight11Step = Fight11Step.Fight;
        fight11StepFaction = "";
        fight11EngagedAtFightStepStart.Clear();
        fight11UnengagedAtFightStepStart.Clear();
        fight11EverEligibleToFight.Clear();

        foreach (SquadController raw in squads)
        {
            if (raw == null ||
                raw.IsAttachedLeader ||
                !raw.IsAlive ||
                !raw.IsOnBattlefield)
            {
                continue;
            }

            SquadController unit = raw.JoinedActionController();

            if (IsEngaged(unit))
                fight11EngagedAtFightStepStart.Add(unit);
            else
                fight11UnengagedAtFightStepStart.Add(unit);
        }

        fightPriorityStep = FightPriorityStep.FightsFirst;
        fightSelectionFaction = activeFaction;
        fightSequenceActive = true;

        Fight11RefreshEverEligible();
        Fight11ResolveFightSelector(activeFaction);

        AppendBattleLog(
            "FIGHT",
            "12.04 Fight Step",
            "Fights First begins with " +
            DisplayFactionName(activeFaction) +
            "; players alternate selections."
        );

        if (fight11Step == Fight11Step.Fight)
        {
            status = Fight11FightPriorityText();
        }
    }

    private bool Fight11IsEligibleToFightNow(SquadController unit)
    {
        if (unit == null ||
            unit.IsAttachedLeader ||
            !unit.IsAlive ||
            !unit.IsOnBattlefield ||
            unit.HasFought)
        {
            return false;
        }

        SquadController actionUnit = unit.JoinedActionController();

        return
            IsEngaged(actionUnit) ||
            fight11EngagedAtFightStepStart.Contains(actionUnit) ||
            actionUnit.MadeChargeMove;
    }

    private void Fight11RefreshEverEligible()
    {
        foreach (SquadController unit in squads)
        {
            if (Fight11IsEligibleToFightNow(unit))
            {
                fight11EverEligibleToFight.Add(
                    unit.JoinedActionController()
                );
            }
        }
    }

    private List<SquadController> Fight11EligibleFightUnits(
        string faction,
        bool fightsFirstOnly)
    {
        List<SquadController> core11Forced =
            Core11ForcedFightSelection(faction, fightsFirstOnly);
        if (core11Forced != null)
            return core11Forced;


        Fight11RefreshEverEligible();

        return squads
            .Where(
                unit =>
                    unit != null &&
                    !unit.IsAttachedLeader &&
                    unit.FactionId == faction &&
                    Fight11IsEligibleToFightNow(unit) &&
                    (!fightsFirstOnly || UnitHasFightsFirst(unit))
            )
            .Select(unit => unit.JoinedActionController())
            .Distinct()
            .ToList();
    }

    private bool Fight11AnyEligibleFightUnits()
    {
        Fight11RefreshEverEligible();

        return squads.Any(
            unit => Fight11IsEligibleToFightNow(unit)
        );
    }

    private bool Fight11AnyEligibleFightsFirst()
    {
        return squads.Any(
            unit =>
                Fight11IsEligibleToFightNow(unit) &&
                UnitHasFightsFirst(unit)
        );
    }

    private void Fight11ResolveFightSelector(string preferredFaction)
    {
        if (fight11Step != Fight11Step.Fight ||
            !fightSequenceActive)
        {
            return;
        }

        Fight11RefreshEverEligible();

        if (fightPriorityStep == FightPriorityStep.FightsFirst)
        {
            if (Fight11AnyEligibleFightsFirst())
            {
                if (Fight11EligibleFightUnits(preferredFaction, true).Count > 0)
                {
                    fightSelectionFaction = preferredFaction;
                    return;
                }

                string other = OtherFaction(preferredFaction);

                if (Fight11EligibleFightUnits(other, true).Count > 0)
                {
                    fightSelectionFaction = other;
                    return;
                }
            }
            else
            {
                // 12.04: the player who could not select a Fights First unit
                // and moved the sequence to Remaining selects next.
                fightPriorityStep = FightPriorityStep.Remaining;
            }
        }

        if (!Fight11AnyEligibleFightUnits())
        {
            Fight11BeginConsolidateStep();
            return;
        }

        if (Fight11EligibleFightUnits(preferredFaction, false).Count > 0)
        {
            fightSelectionFaction = preferredFaction;
            return;
        }

        string fallback = OtherFaction(preferredFaction);

        if (Fight11EligibleFightUnits(fallback, false).Count > 0)
        {
            fightSelectionFaction = fallback;
            return;
        }

        Fight11BeginConsolidateStep();
    }

    private void Fight11AdvanceFightPriority(SquadController unitThatFought)
    {
        if (fight11ForcedFightActive)
        {
            Fight11ContinueForcedConsolidationFights();
            return;
        }

        if (fight11Step != Fight11Step.Fight ||
            !fightSequenceActive ||
            unitThatFought == null)
        {
            return;
        }

        string next = OtherFaction(unitThatFought.FactionId);

        // 12.04: if a Fights First unit becomes newly eligible during the
        // Remaining sequence, return to the Fights First sequence.
        if (fightPriorityStep == FightPriorityStep.Remaining &&
            Fight11AnyEligibleFightsFirst())
        {
            fightPriorityStep = FightPriorityStep.FightsFirst;
        }

        Fight11ResolveFightSelector(next);

        if (fight11Step == Fight11Step.Fight)
            status = Fight11FightPriorityText();
    }

    private string Fight11FightPriorityText()
    {
        if (fight11Step == Fight11Step.PileIn)
        {
            return
                "Pile In: " +
                DisplayFactionName(fight11StepFaction) +
                " resolves optional moves";
        }

        if (fight11Step == Fight11Step.Consolidate)
        {
            return
                "Consolidate: " +
                DisplayFactionName(fight11StepFaction) +
                " resolves optional moves";
        }

        if (fight11Step == Fight11Step.Complete)
            return "Fight combat steps complete";

        if (fight11Step != Fight11Step.Fight ||
            !fightSequenceActive)
        {
            return "Fight sequence waiting";
        }

        return
            "Fight priority: " +
            (fightPriorityStep == FightPriorityStep.FightsFirst
                ? "FIGHTS FIRST"
                : "REMAINING") +
            "  -  " +
            DisplayFactionName(fightSelectionFaction) +
            " selects";
    }

    private void Fight11TryFight(
        SquadController attacker,
        SquadController target)
    {
        if (attacker != null && target != null &&
            !CoreRules11Aircraft.CanFightTarget(attacker, target))
        {
            status = "AIRCRAFT melee can only interact with FLYING units/models.";
            return;
        }


        if (attacker == null || target == null)
            return;

        attacker = attacker.JoinedActionController();
        target = target.JoinedActionController();

        if (fightActivationStage != FightActivationStage.None)
        {
            if (fightActivationUnit == null ||
                attacker != fightActivationUnit)
            {
                status = "Finish the current Fight-phase operation first.";
                return;
            }

            if (fightActivationStage == FightActivationStage.Attacks)
            {
                if (selectedModel == null ||
                    !ModelBelongsToFightActivation(selectedModel))
                {
                    status =
                        "MELEE: click the exact fighting model, choose its melee weapon, then click an enemy target.";
                    return;
                }

                TryFightModelAttack(selectedModel, target);
                return;
            }

            status =
                fightActivationStage == FightActivationStage.PileIn
                ? "Finish the current pile-in move before selecting another unit."
                : "Finish the current consolidation move before selecting another unit.";
            return;
        }

        if (fight11Step == Fight11Step.PileIn)
        {
            Fight11TryBeginPileIn(attacker, target, false);
            return;
        }

        if (fight11Step == Fight11Step.Fight)
        {
            Fight11TrySelectUnitToFight(attacker, target, false);
            return;
        }

        if (fight11Step == Fight11Step.Consolidate)
        {
            Fight11TryBeginConsolidation(attacker, target);
            return;
        }

        status =
            fight11Step == Fight11Step.Complete
            ? "All Fight phase combat steps are complete."
            : "Resolve the start-of-Fight rules first.";
    }

    private void Fight11TryBeginPileIn(
        SquadController attacker,
        SquadController clickedTarget,
        bool overrun)
    {
        if (attacker == null || clickedTarget == null)
            return;

        attacker = attacker.JoinedActionController();
        clickedTarget = clickedTarget.JoinedActionController();

        if (!overrun)
        {
            if (fight11Step != Fight11Step.PileIn ||
                attacker.FactionId != fight11StepFaction ||
                !Fight11CanPileInUnit(attacker, fight11StepFaction))
            {
                status = "That unit cannot make a pile-in at this point.";
                return;
            }
        }

        List<SquadController> targets;

        if (IsEngaged(attacker))
        {
            targets = Fight11EngagedEnemies(attacker);
        }
        else
        {
            List<SquadController> candidates = Fight11EnemiesWithin(attacker, 5f);

            if (!candidates.Contains(clickedTarget))
            {
                status = "That enemy is not within 5″ for this pile-in.";
                return;
            }

            if (candidates.Count > 1)
            {
                Fight11OpenTargetSubsetChoice(
                    attacker,
                    candidates,
                    "PILE-IN TARGETS",
                    selected => Fight11StartPileInMove(attacker, selected, overrun)
                );
                return;
            }

            targets = new List<SquadController> { clickedTarget };
        }

        Fight11StartPileInMove(attacker, targets, overrun);
    }

    private void Fight11StartPileInMove(
        SquadController attacker,
        List<SquadController> targets,
        bool overrun)
    {
        if (attacker == null || targets == null || targets.Count == 0)
            return;

        fightActivationUnit = attacker.JoinedActionController();
        fightActivationInitialTarget = targets[0].JoinedActionController();
        fightActivationStage = FightActivationStage.PileIn;
        fight11OverrunInProgress = overrun;
        fight11ConsolidationMode = Fight11ConsolidationMode.None;

        Fight11SetMoveTargets(targets);
        CaptureFightStageStartPositions();
        Fight11CaptureMoveConstraints(true);

        selectedSquad = fightActivationUnit;
        selectedModel = JoinedModels(fightActivationUnit)
            .FirstOrDefault(model => model != null && model.IsAlive);

        if (selectedModel != null)
            SelectModelForAction(fightActivationUnit, selectedModel);

        status =
            (overrun ? "OVERRUN PILE-IN: " : "PILE-IN: ") +
            fightActivationUnit.DisplayName +
            ". Move models up to " +
            FightStageMoveLimit().ToString("0.#") +
            "″, then click DONE PILE-IN.";

        AppendBattleLog(
            "FIGHT",
            overrun ? "12.06 Overrun pile-in" : "12.03 Pile-in",
            fightActivationUnit.DisplayName +
            " targets " +
            string.Join(", ", fight11MoveTargets.Select(t => t.DisplayName).ToArray())
        );

        RefreshMoveRing();
    }

    private void Fight11CompletePileIn()
    {
        if (fightActivationStage != FightActivationStage.PileIn ||
            fightActivationUnit == null)
        {
            return;
        }

        if (!fightActivationUnit.IsCoherent())
        {
            status = "Cannot finish pile-in: the unit is out of coherency.";
            return;
        }

        if (!AllModelsInsideBoard(fightActivationUnit) ||
            !AllModelsHaveLegalPlacement(fightActivationUnit))
        {
            status = "Cannot finish pile-in: one or more models are illegally placed.";
            return;
        }

        if (!IsEngaged(fightActivationUnit))
        {
            status = "Cannot finish pile-in: the unit must end the move engaged.";
            return;
        }

        string engagementReason;
        if (!Fight11PreservesRequiredStartEngagements(out engagementReason))
        {
            status = engagementReason;
            return;
        }

        SquadController completed = fightActivationUnit;
        bool wasOverrun = fight11OverrunInProgress;

        fightStageStartPositions.Clear();
        fightStageMovedModels.Clear();
        Fight11ClearMoveConstraints();

        if (wasOverrun)
        {
            fight11OverrunInProgress = false;
            Fight11BeginAttacksForSelectedUnit(completed);
            return;
        }

        fight11PileInCompleted.Add(completed);
        ClearFightActivationState();
        Fight11ClearMoveConstraints();

        status =
            completed.DisplayName +
            " completed its pile-in. " +
            DisplayFactionName(fight11StepFaction) +
            " continues the Pile In step.";

        Fight11SkipEmptyPileInSides();
    }

    private void Fight11TrySelectUnitToFight(
        SquadController attacker,
        SquadController clickedTarget,
        bool forcedByConsolidation)
    {
        if (attacker == null)
            return;

        attacker = attacker.JoinedActionController();

        if (!forcedByConsolidation)
        {
            if (fight11Step != Fight11Step.Fight ||
                !fightSequenceActive ||
                attacker.FactionId != fightSelectionFaction ||
                !Fight11IsEligibleToFightNow(attacker))
            {
                status = "That unit is not the next eligible unit to fight.";
                return;
            }

            if (fightPriorityStep == FightPriorityStep.FightsFirst &&
                !UnitHasFightsFirst(attacker))
            {
                status = "Fights First units must be resolved before remaining combats.";
                return;
            }
        }

        if (clickedTarget == null ||
            !clickedTarget.IsAlive ||
            !clickedTarget.IsOnBattlefield ||
            clickedTarget.FactionId == attacker.FactionId)
        {
            status = "Select an enemy unit for this fight.";
            return;
        }

        clickedTarget = clickedTarget.JoinedActionController();

        if (IsEngaged(attacker))
        {
            if (!UnitsAreEngaged(attacker, clickedTarget))
            {
                status = "For an engaged unit, select an enemy unit it is currently engaged with.";
                return;
            }
        }
        else if (Fight11UnitDistance(attacker, clickedTarget) > 5.001f)
        {
            status = "That enemy is too far away for an Overrun pile-in.";
            return;
        }

        Fight11OfferPreFightChoices(
            attacker,
            clickedTarget,
            forcedByConsolidation,
            () => Fight11SelectFightTypeAndBegin(attacker, clickedTarget, forcedByConsolidation)
        );
    }

    private void Fight11OfferPreFightChoices(
        SquadController attacker,
        SquadController target,
        bool forced,
        Action after)
    {
        if (FactionRuleSystem.UnitOrLeaderHasRule(attacker, "Martial Ka'tah") &&
            !attacker.KatahChoiceMadeThisFight)
        {
            OpenRuleChoice(
                "MARTIAL KA'TAH",
                attacker.DisplayName + " is selected to fight. Choose one Ka'tah stance.",
                new[]
                {
                    new RuleChoiceOption(
                        "Dacatarai  -  Sustained Hits 1",
                        () =>
                        {
                            CloseRuleChoice();
                            attacker.KatahChoiceMadeThisFight = true;
                            attacker.KatahSustainedActive = true;
                            attacker.KatahLethalActive = false;
                            Fight11OfferPreFightChoices(attacker, target, forced, after);
                        }),
                    new RuleChoiceOption(
                        "Rendax  -  Lethal Hits",
                        () =>
                        {
                            CloseRuleChoice();
                            attacker.KatahChoiceMadeThisFight = true;
                            attacker.KatahSustainedActive = false;
                            attacker.KatahLethalActive = true;
                            Fight11OfferPreFightChoices(attacker, target, forced, after);
                        })
                }
            );
            return;
        }

        if (CanUseBattleFocusManoeuvre(attacker))
        {
            OpenRuleChoice(
                "BATTLE FOCUS  -  SUDDEN STRIKE",
                attacker.DisplayName +
                " is selected to fight. Spend 1 Battle Focus token so any Overrun pile-in and later consolidation can be up to 6″?",
                new[]
                {
                    new RuleChoiceOption(
                        "Use Sudden Strike (1 BF)",
                        () =>
                        {
                            CloseRuleChoice();
                            if (SpendBattleFocusFor(attacker, "SUDDEN STRIKE"))
                                attacker.SuddenStrikeActive = true;
                            if (after != null) after();
                        }),
                    new RuleChoiceOption(
                        "Fight normally",
                        () =>
                        {
                            CloseRuleChoice();
                            if (after != null) after();
                        })
                }
            );
            return;
        }

        if (after != null)
            after();
    }

    private void Fight11SelectFightTypeAndBegin(
        SquadController attacker,
        SquadController clickedTarget,
        bool forced)
    {
        bool normal = IsEngaged(attacker);
        bool overrun = Fight11CanMakeOverrun(attacker);

        if (normal && overrun)
        {
            OpenRuleChoice(
                "SELECT FIGHT TYPE  -  " + attacker.DisplayName,
                "This unit is eligible to make either a Normal Fight or an Overrun Fight.",
                new[]
                {
                    new RuleChoiceOption(
                        "NORMAL FIGHT",
                        () =>
                        {
                            CloseRuleChoice();
                            Fight11BeginNormalFight(attacker, clickedTarget, forced);
                        }),
                    new RuleChoiceOption(
                        "OVERRUN FIGHT",
                        () =>
                        {
                            CloseRuleChoice();
                            Fight11TryBeginPileIn(attacker, clickedTarget, true);
                        })
                }
            );
            return;
        }

        if (normal)
        {
            Fight11BeginNormalFight(attacker, clickedTarget, forced);
            return;
        }

        if (overrun)
        {
            Fight11TryBeginPileIn(attacker, clickedTarget, true);
            return;
        }

        status =
            attacker.DisplayName +
            " is eligible to be selected but has no legal Normal/Overrun fight against that target.";
    }

    private bool Fight11CanMakeOverrun(SquadController unit)
    {
        if (unit == null)
            return false;

        unit = unit.JoinedActionController();

        if (!IsEngaged(unit))
            return Fight11EnemiesWithin(unit, 5f).Count > 0;

        return fight11UnengagedAtFightStepStart.Contains(unit);
    }

    private void Fight11BeginNormalFight(
        SquadController attacker,
        SquadController target,
        bool forced)
    {
        if (attacker == null || target == null)
            return;

        attacker = attacker.JoinedActionController();
        target = target.JoinedActionController();

        fightActivationUnit = attacker;
        fightActivationInitialTarget = target;
        fightActivationStage = FightActivationStage.Attacks;
        fight11CurrentFightWasOverrun = false;
        fightModelsResolvedThisActivation.Clear();
        fightPreparedAttackModel = null;
        fightPreparedMeleeWeapon = null;

        fight11EverEligibleToFight.Add(attacker);
        fight11ForcedFightActive = forced || fight11ForcedFightActive;

        selectedSquad = attacker;
        selectedModel = null;
        ClearFightModelFocus();

        GameEventBus.Raise(
            new GameEventContext
            {
                Type = GameEventType.UnitSelectedToFight,
                Game = this,
                ActingFaction = attacker.FactionId,
                Phase = phase,
                Source = attacker,
                Target = target,
                Note = forced ? "Consolidation-forced fight" : "11e Fight step"
            }
        );

        AppendBattleLog(
            "FIGHT",
            forced ? "Forced fight" : "12.05 Normal Fight",
            attacker.DisplayName + " selected to fight " + target.DisplayName + "."
        );

        status =
            "MELEE ATTACKS: " + attacker.DisplayName +
            ". Click a fighting model, choose its melee weapon/profile, then click an engaged enemy target.";

        RefreshMoveRing();
    }

    private void Fight11BeginAttacksForSelectedUnit(SquadController attacker)
    {
        if (attacker == null)
            return;

        attacker = attacker.JoinedActionController();

        SquadController target = Fight11EngagedEnemies(attacker).FirstOrDefault();

        if (target == null)
        {
            Fight11MarkSelectedUnitUnableToFight(attacker);
            return;
        }

        fightActivationUnit = attacker;
        fightActivationInitialTarget = target;
        fightActivationStage = FightActivationStage.Attacks;
        fight11CurrentFightWasOverrun = true;
        fightModelsResolvedThisActivation.Clear();
        fightPreparedAttackModel = null;
        fightPreparedMeleeWeapon = null;

        selectedSquad = attacker;
        selectedModel = null;
        ClearFightModelFocus();

        GameEventBus.Raise(
            new GameEventContext
            {
                Type = GameEventType.UnitSelectedToFight,
                Game = this,
                ActingFaction = attacker.FactionId,
                Phase = phase,
                Source = attacker,
                Target = target,
                Note = "12.06 Overrun Fight"
            }
        );

        status =
            "OVERRUN MELEE ATTACKS: " + attacker.DisplayName +
            " completed its extra pile-in. Resolve melee attacks now.";

        RefreshMoveRing();
    }

    private void Fight11CompleteFightModelAttack(ModelToken model)
    {
        if (model != null)
            fightModelsResolvedThisActivation.Add(model);

        fightPreparedAttackModel = null;
        fightPreparedMeleeWeapon = null;

        if (fightActivationStage != FightActivationStage.Attacks)
            return;

        ClearFightModelFocus();
        selectedModel = null;

        int remaining = FightPotentialAttackModels()
            .Count(candidate => !fightModelsResolvedThisActivation.Contains(candidate));

        if (remaining <= 0)
        {
            Fight11FinishSelectedFightAttacks();
            return;
        }

        status =
            "MELEE: model resolved. " + remaining +
            " fighting model(s) remain. Select the next model, or click DONE ATTACKS.";

        RefreshMoveRing();
    }

    private void Fight11SkipSelectedFightModel()
    {
        if (fightActivationStage != FightActivationStage.Attacks ||
            !ModelBelongsToFightActivation(selectedModel))
        {
            return;
        }

        string role = selectedModel.RoleName;
        fightModelsResolvedThisActivation.Add(selectedModel);
        selectedModel = null;
        fightPreparedAttackModel = null;
        fightPreparedMeleeWeapon = null;
        ClearFightModelFocus();

        int remaining = FightPotentialAttackModels()
            .Count(candidate => !fightModelsResolvedThisActivation.Contains(candidate));

        if (remaining <= 0)
        {
            Fight11FinishSelectedFightAttacks();
            return;
        }

        status = role + " marked DONE. " + remaining + " model(s) remain.";
        RefreshMoveRing();
    }

    private void Fight11CompleteFightAttacks()
    {
        if (fightActivationStage != FightActivationStage.Attacks)
            return;

        foreach (ModelToken model in FightPotentialAttackModels())
            fightModelsResolvedThisActivation.Add(model);

        Fight11FinishSelectedFightAttacks();
    }

    private void Fight11FinishSelectedFightAttacks()
    {
        if (fightActivationUnit == null)
        {
            ClearFightActivationState();
            return;
        }

        SquadController completed = fightActivationUnit.JoinedActionController();

        completed.HasFought = true;
        if (completed.AttachedLeader != null)
            completed.AttachedLeader.HasFought = true;

        completed.KatahSustainedActive = false;
        completed.KatahLethalActive = false;

        GameEventBus.Raise(
            new GameEventContext
            {
                Type = GameEventType.UnitFinishedFighting,
                Game = this,
                ActingFaction = completed.FactionId,
                Phase = phase,
                Source = completed,
                Target = fightActivationInitialTarget,
                Note = fight11ForcedFightActive
                    ? "Consolidation-forced fight complete"
                    : "11e Fight step complete"
            }
        );

        AppendBattleLog(
            "FIGHT",
            completed.DisplayName,
            "Attacks complete. Consolidation will be resolved later in the phase-wide Consolidate step."
        );

        if (fight11ForcedFightActive && fight11CurrentFightWasOverrun)
        {
            foreach (SquadController newlyEligible in Fight11EngagedEnemies(completed))
            {
                if (newlyEligible != null &&
                    newlyEligible.IsAlive &&
                    !newlyEligible.HasFought &&
                    !fight11ForcedConsolidationFights.Contains(newlyEligible))
                {
                    fight11ForcedConsolidationFights.Add(newlyEligible);
                    fight11EverEligibleToFight.Add(newlyEligible);
                }
            }
        }

        fight11CurrentFightWasOverrun = false;
        ClearFightActivationState();
        Fight11ClearMoveConstraints();

        if (fight11ForcedFightActive)
        {
            Fight11ContinueForcedConsolidationFights();
            return;
        }

        if (Core11CounteroffensiveDecisionIsPending(completed))
            return;

        Fight11AdvanceFightPriority(completed);
    }

    private void Fight11MarkSelectedUnitUnableToFight(SquadController unit)
    {
        if (unit == null)
            return;

        unit = unit.JoinedActionController();

        if (!Fight11IsEligibleToFightNow(unit))
            return;

        unit.HasFought = true;
        if (unit.AttachedLeader != null)
            unit.AttachedLeader.HasFought = true;

        fight11EverEligibleToFight.Add(unit);

        AppendBattleLog(
            "FIGHT",
            unit.DisplayName,
            "Selected as eligible to fight, but no legal fight could be resolved."
        );

        if (fight11ForcedFightActive)
        {
            Fight11ContinueForcedConsolidationFights();
            return;
        }

        Fight11AdvanceFightPriority(unit);
    }

    private bool Fight11SelectedUnitCanBeMarkedUnable()
    {
        if (fight11Step != Fight11Step.Fight ||
            selectedSquad == null)
        {
            return false;
        }

        SquadController unit = selectedSquad.JoinedActionController();

        if (unit.FactionId != fightSelectionFaction ||
            !Fight11IsEligibleToFightNow(unit))
        {
            return false;
        }

        if (IsEngaged(unit))
            return false;

        return Fight11EnemiesWithin(unit, 5f).Count == 0;
    }

    private void Fight11BeginConsolidateStep()
    {
        ClearFightActivationState();
        Fight11ClearMoveConstraints();

        fight11Step = Fight11Step.Consolidate;
        fight11StepFaction = activeFaction;
        fightSequenceActive = true;
        fightPriorityStep = FightPriorityStep.None;
        fightSelectionFaction = "";

        Fight11RefreshEverEligible();

        AppendBattleLog(
            "FIGHT",
            "12.07 Consolidate",
            DisplayFactionName(activeFaction) +
            " resolves all optional consolidations first, then the opponent."
        );

        Fight11SkipEmptyConsolidationSides();
    }

    private void Fight11SkipEmptyConsolidationSides()
    {
        while (fight11Step == Fight11Step.Consolidate &&
               !Fight11SideHasAvailableConsolidation(fight11StepFaction))
        {
            string opponent = OtherFaction(fight11StepFaction);

            if (fight11StepFaction == activeFaction &&
                opponent != fight11StepFaction)
            {
                fight11StepFaction = opponent;
                continue;
            }

            Fight11CompleteFightCombatSteps();
            return;
        }

        if (fight11Step == Fight11Step.Consolidate)
        {
            status =
                "CONSOLIDATE STEP  -  " +
                DisplayFactionName(fight11StepFaction) +
                " resolves optional consolidations. Select an eligible unit and target, or click DONE SIDE CONSOLIDATE.";
        }
    }

    private bool Fight11SideHasAvailableConsolidation(string faction)
    {
        return fight11EverEligibleToFight.Any(
            unit => Fight11CanConsolidateUnit(unit, faction)
        );
    }

    private bool Fight11CanConsolidateUnit(
        SquadController unit,
        string faction)
    {
        if (unit == null ||
            !unit.IsAlive ||
            !unit.IsOnBattlefield ||
            unit.IsAttachedLeader ||
            unit.FactionId != faction ||
            !fight11EverEligibleToFight.Contains(unit.JoinedActionController()) ||
            fight11ConsolidationCompleted.Contains(unit.JoinedActionController()))
        {
            return false;
        }

        unit = unit.JoinedActionController();

        if (IsEngaged(unit))
            return true;

        if (Fight11EnemiesWithin(unit, 3f).Count > 0)
            return true;

        return Fight11ObjectivesWithin(unit, 3f).Count > 0;
    }

    private void Fight11FinishCurrentConsolidationSide()
    {
        if (fight11Step != Fight11Step.Consolidate ||
            fightActivationStage != FightActivationStage.None ||
            fight11ForcedFightActive)
        {
            return;
        }

        string opponent = OtherFaction(fight11StepFaction);

        if (fight11StepFaction == activeFaction &&
            opponent != fight11StepFaction)
        {
            fight11StepFaction = opponent;
            Fight11SkipEmptyConsolidationSides();
            return;
        }

        Fight11CompleteFightCombatSteps();
    }

    private void Fight11CompleteFightCombatSteps()
    {
        ClearFightActivationState();
        Fight11ClearMoveConstraints();

        fight11Step = Fight11Step.Complete;
        fight11StepFaction = "";
        fightSequenceActive = false;
        fightPriorityStep = FightPriorityStep.None;
        fightSelectionFaction = "";

        status =
            "Fight phase combat steps complete. End-of-Fight-phase rules can now resolve.";

        AppendBattleLog(
            "FIGHT",
            "12.09 End of Fight phase",
            "Pile In, Fight and Consolidate steps are complete."
        );
    }

    private void Fight11TryBeginConsolidation(
        SquadController unit,
        SquadController clickedEnemy)
    {
        if (fight11Step != Fight11Step.Consolidate ||
            fight11ForcedFightActive ||
            unit == null)
        {
            return;
        }

        unit = unit.JoinedActionController();

        if (!Fight11CanConsolidateUnit(unit, fight11StepFaction))
        {
            status = "That unit is not eligible to consolidate now.";
            return;
        }

        if (IsEngaged(unit))
        {
            Fight11StartConsolidationMove(
                unit,
                Fight11ConsolidationMode.Ongoing,
                Fight11EngagedEnemies(unit),
                null
            );
            return;
        }

        List<SquadController> nearbyEnemies = Fight11EnemiesWithin(unit, 3f);

        if (nearbyEnemies.Count > 0)
        {
            if (clickedEnemy == null ||
                !nearbyEnemies.Contains(clickedEnemy.JoinedActionController()))
            {
                status = "Select one of the enemy units within 3″ as an Engaging Consolidation target.";
                return;
            }

            Fight11OpenTargetSubsetChoice(
                unit,
                nearbyEnemies,
                "ENGAGING CONSOLIDATION TARGETS",
                selected =>
                    Fight11StartConsolidationMove(
                        unit,
                        Fight11ConsolidationMode.Engaging,
                        selected,
                        null
                    )
            );
            return;
        }

        status =
            "No enemy is within 3″. Use OBJECTIVE CONSOLIDATE if an objective is available.";
    }

    private void Fight11BeginObjectiveConsolidationForSelected()
    {
        if (fight11Step != Fight11Step.Consolidate ||
            fight11ForcedFightActive ||
            selectedSquad == null)
        {
            return;
        }

        SquadController unit = selectedSquad.JoinedActionController();

        if (!Fight11CanConsolidateUnit(unit, fight11StepFaction) ||
            IsEngaged(unit) ||
            Fight11EnemiesWithin(unit, 3f).Count > 0)
        {
            status = "That unit cannot use Objective Consolidation right now.";
            return;
        }

        List<ObjectiveController> objectivesInRange = Fight11ObjectivesWithin(unit, 3f);

        if (objectivesInRange.Count == 0)
        {
            status = "No objective is within 3″ of that unit.";
            return;
        }

        if (objectivesInRange.Count == 1)
        {
            Fight11StartConsolidationMove(
                unit,
                Fight11ConsolidationMode.Objective,
                new List<SquadController>(),
                objectivesInRange[0]
            );
            return;
        }

        List<RuleChoiceOption> options = new List<RuleChoiceOption>();

        for (int i = 0; i < objectivesInRange.Count; i++)
        {
            ObjectiveController captured = objectivesInRange[i];
            int displayIndex = objectives.IndexOf(captured) + 1;

            options.Add(
                new RuleChoiceOption(
                    "Objective " + displayIndex,
                    () =>
                    {
                        CloseRuleChoice();
                        Fight11StartConsolidationMove(
                            unit,
                            Fight11ConsolidationMode.Objective,
                            new List<SquadController>(),
                            captured
                        );
                    })
            );
        }

        OpenRuleChoice(
            "OBJECTIVE CONSOLIDATION",
            "Select the objective this unit will consolidate toward.",
            options.ToArray()
        );
    }

    private bool Fight11CanObjectiveConsolidateSelected()
    {
        if (fight11Step != Fight11Step.Consolidate ||
            fight11ForcedFightActive ||
            selectedSquad == null)
        {
            return false;
        }

        SquadController unit = selectedSquad.JoinedActionController();

        return
            Fight11CanConsolidateUnit(unit, fight11StepFaction) &&
            !IsEngaged(unit) &&
            Fight11EnemiesWithin(unit, 3f).Count == 0 &&
            Fight11ObjectivesWithin(unit, 3f).Count > 0;
    }

    private void Fight11StartConsolidationMove(
        SquadController unit,
        Fight11ConsolidationMode mode,
        List<SquadController> targets,
        ObjectiveController objective)
    {
        fightActivationUnit = unit.JoinedActionController();
        fightActivationInitialTarget =
            targets != null && targets.Count > 0
            ? targets[0].JoinedActionController()
            : null;
        fightActivationStage = FightActivationStage.Consolidate;
        fight11ConsolidationMode = mode;
        fight11ConsolidationObjective = objective;
        fight11OverrunInProgress = false;

        Fight11SetMoveTargets(targets);
        CaptureFightStageStartPositions();
        Fight11CaptureMoveConstraints(mode == Fight11ConsolidationMode.Ongoing);

        selectedSquad = fightActivationUnit;
        selectedModel = JoinedModels(fightActivationUnit)
            .FirstOrDefault(model => model != null && model.IsAlive);

        if (selectedModel != null)
            SelectModelForAction(fightActivationUnit, selectedModel);

        status =
            mode.ToString().ToUpperInvariant() +
            " CONSOLIDATION: " +
            fightActivationUnit.DisplayName +
            ". Move models up to " +
            FightStageMoveLimit().ToString("0.#") +
            "″, then click DONE CONSOLIDATE.";

        RefreshMoveRing();
    }

    private void Fight11CompleteConsolidation()
    {
        if (fightActivationStage != FightActivationStage.Consolidate ||
            fightActivationUnit == null)
        {
            return;
        }

        SquadController completed = fightActivationUnit.JoinedActionController();

        if (completed.IsAlive && !completed.IsCoherent())
        {
            status = "Cannot finish consolidation: the unit is out of coherency.";
            return;
        }

        if (completed.IsAlive &&
            (!AllModelsInsideBoard(completed) ||
             !AllModelsHaveLegalPlacement(completed)))
        {
            status = "Cannot finish consolidation: one or more models are illegally placed.";
            return;
        }

        string reason;

        if (fight11ConsolidationMode == Fight11ConsolidationMode.Ongoing)
        {
            if (!Fight11PreservesRequiredStartEngagements(out reason))
            {
                status = reason;
                return;
            }
        }
        else if (fight11ConsolidationMode == Fight11ConsolidationMode.Engaging)
        {
            foreach (SquadController target in fight11MoveTargets)
            {
                if (!UnitsAreEngaged(completed, target))
                {
                    status =
                        "Cannot finish Engaging Consolidation: the unit must be engaged with every selected enemy target.";
                    return;
                }
            }
        }
        else if (fight11ConsolidationMode == Fight11ConsolidationMode.Objective)
        {
            if (IsEngaged(completed) ||
                fight11ConsolidationObjective == null ||
                !fight11ConsolidationObjective.UnitWithinRange(completed))
            {
                status =
                    "Cannot finish Objective Consolidation: the unit must be unengaged and within range of the selected objective.";
                return;
            }
        }

        List<SquadController> newlyEngagedForced = new List<SquadController>();

        if (fight11ConsolidationMode == Fight11ConsolidationMode.Engaging)
        {
            newlyEngagedForced = fight11MoveTargets
                .Where(
                    enemy =>
                        enemy != null &&
                        enemy.IsAlive &&
                        !enemy.HasFought &&
                        !fight11ModelStartEngagements.Values.Any(list => list.Contains(enemy))
                )
                .Select(enemy => enemy.JoinedActionController())
                .Distinct()
                .ToList();
        }

        fight11ConsolidationCompleted.Add(completed);

        AppendBattleLog(
            "FIGHT",
            "12.08 Consolidation",
            completed.DisplayName + " completed " +
            fight11ConsolidationMode + " Consolidation."
        );

        ClearFightActivationState();
        Fight11ClearMoveConstraints();
        fight11ConsolidationMode = Fight11ConsolidationMode.None;
        fight11ConsolidationObjective = null;

        if (newlyEngagedForced.Count > 0)
        {
            fight11ForcedConsolidationFights.Clear();
            fight11ForcedConsolidationFights.AddRange(newlyEngagedForced);
            fight11ConsolidationResumeFaction = fight11StepFaction;
            Fight11ContinueForcedConsolidationFights();
            return;
        }

        Fight11SkipEmptyConsolidationSides();
    }

    private void Fight11ContinueForcedConsolidationFights()
    {
        ClearFightActivationState();
        fight11ForcedFightActive = false;

        fight11ForcedConsolidationFights.RemoveAll(
            unit => unit == null || !unit.IsAlive || unit.HasFought
        );

        if (fight11ForcedConsolidationFights.Count == 0)
        {
            fight11StepFaction = fight11ConsolidationResumeFaction;
            fight11ConsolidationResumeFaction = "";
            status =
                "Consolidation-triggered fights complete. Resume " +
                DisplayFactionName(fight11StepFaction) +
                " consolidations.";
            Fight11SkipEmptyConsolidationSides();
            return;
        }

        if (fight11ForcedConsolidationFights.Count == 1)
        {
            Fight11StartForcedConsolidationFight(
                fight11ForcedConsolidationFights[0]
            );
            return;
        }

        List<RuleChoiceOption> options = new List<RuleChoiceOption>();

        foreach (SquadController unit in fight11ForcedConsolidationFights.ToList())
        {
            SquadController captured = unit;
            options.Add(
                new RuleChoiceOption(
                    captured.DisplayName,
                    () =>
                    {
                        CloseRuleChoice();
                        Fight11StartForcedConsolidationFight(captured);
                    })
            );
        }

        OpenRuleChoice(
            "NEW FOES TO FACE",
            "Engaging Consolidation contacted enemy units that have not fought. Their controller selects each one to fight, one at a time.",
            options.ToArray()
        );
    }

    private void Fight11StartForcedConsolidationFight(SquadController unit)
    {
        if (unit == null)
        {
            Fight11ContinueForcedConsolidationFights();
            return;
        }

        unit = unit.JoinedActionController();
        fight11ForcedConsolidationFights.Remove(unit);
        fight11EverEligibleToFight.Add(unit);
        fight11ForcedFightActive = true;

        List<SquadController> engaged = Fight11EngagedEnemies(unit);
        SquadController target = engaged.FirstOrDefault();

        if (target == null)
        {
            Fight11MarkSelectedUnitUnableToFight(unit);
            return;
        }

        Fight11TrySelectUnitToFight(unit, target, true);
    }

    private bool Fight11FightStageDestinationLegal(
        ModelToken model,
        Vector3 destination,
        out string reason)
    {
        reason = "";

        if (!ModelBelongsToFightActivation(model) ||
            (fightActivationStage != FightActivationStage.PileIn &&
             fightActivationStage != FightActivationStage.Consolidate))
        {
            reason = "No Fight-phase combat move is active for that model.";
            return false;
        }

        Vector3 start;
        if (!fightStageStartPositions.TryGetValue(model, out start))
            start = model.transform.position;

        destination.y = model.transform.position.y;

        float moveDistance = HorizontalDistance(start, destination);
        float limit = FightStageMoveLimit();

        if (moveDistance > limit + 0.001f)
        {
            reason = "That move exceeds the " + limit.ToString("0.#") + "″ allowance.";
            return false;
        }

        if (fight11BaseContactLockedModels.Contains(model) &&
            moveDistance > 0.001f)
        {
            reason = "A model in base contact with an enemy cannot move in this mode.";
            return false;
        }

        if (!InsideBoard(destination))
        {
            reason = "That model would leave the battlefield.";
            return false;
        }

        if (!CanPlaceModel(model, destination))
        {
            reason = "That model cannot legally occupy that position.";
            return false;
        }

        if (!CombatMovePathIsClear(model, model.transform.position, destination))
        {
            reason = "The combat move path is blocked.";
            return false;
        }

        if (moveDistance <= 0.001f)
            return true;

        if (fightActivationStage == FightActivationStage.Consolidate &&
            fight11ConsolidationMode == Fight11ConsolidationMode.Objective)
        {
            if (fight11ConsolidationObjective == null)
            {
                reason = "No objective is selected for this consolidation.";
                return false;
            }

            float startDistance = Fight11ModelToObjectiveDistance(model, start, fight11ConsolidationObjective);
            float endDistance = Fight11ModelToObjectiveDistance(model, destination, fight11ConsolidationObjective);

            if (endDistance >= startDistance - 0.001f &&
                endDistance > ObjectiveController.ControlRadius + 0.001f)
            {
                reason = "A model making Objective Consolidation must finish closer to the selected objective (or within range).";
                return false;
            }

            return true;
        }

        float startTargetDistance;
        if (!fight11ModelStartTargetDistances.TryGetValue(model, out startTargetDistance))
            startTargetDistance = Fight11DistanceToClosestSelectedTarget(model, start);

        float endTargetDistance = Fight11DistanceToClosestSelectedTarget(model, destination);

        if (endTargetDistance >= startTargetDistance - 0.001f)
        {
            reason =
                fightActivationStage == FightActivationStage.PileIn
                ? "A moved pile-in model must finish closer to its closest selected pile-in target."
                : "A moved consolidating model must finish closer to its closest selected enemy target.";
            return false;
        }

        return true;
    }

    private void Fight11CaptureMoveConstraints(bool lockBaseContactModels)
    {
        fight11ModelStartEngagements.Clear();
        fight11ModelStartTargetDistances.Clear();
        fight11BaseContactLockedModels.Clear();

        if (fightActivationUnit == null)
            return;

        foreach (ModelToken model in JoinedModels(fightActivationUnit))
        {
            if (model == null || !model.IsAlive)
                continue;

            List<SquadController> startsEngaged = squads
                .Where(
                    enemy =>
                        enemy != null &&
                        enemy.IsAlive &&
                        enemy.IsOnBattlefield &&
                        !enemy.IsAttachedLeader &&
                        enemy.FactionId != fightActivationUnit.FactionId &&
                        Fight11ModelEngagedWithUnit(model, enemy)
                )
                .Select(enemy => enemy.JoinedActionController())
                .Distinct()
                .ToList();

            fight11ModelStartEngagements[model] = startsEngaged;
            fight11ModelStartTargetDistances[model] =
                Fight11DistanceToClosestSelectedTarget(model, model.transform.position);

            if (lockBaseContactModels && Fight11ModelInBaseContactWithEnemy(model))
                fight11BaseContactLockedModels.Add(model);
        }
    }

    private bool Fight11PreservesRequiredStartEngagements(out string reason)
    {
        reason = "";

        foreach (KeyValuePair<ModelToken, List<SquadController>> pair
            in fight11ModelStartEngagements)
        {
            ModelToken model = pair.Key;
            if (model == null || !model.IsAlive)
                continue;

            foreach (SquadController enemy in pair.Value)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                if (!Fight11ModelEngagedWithUnit(model, enemy))
                {
                    reason =
                        model.RoleName +
                        " started this move engaged with " +
                        enemy.DisplayName +
                        " and must still be engaged with that unit afterwards.";
                    return false;
                }
            }
        }

        return true;
    }

    private void Fight11SetMoveTargets(IEnumerable<SquadController> targets)
    {
        fight11MoveTargets.Clear();

        if (targets == null)
            return;

        fight11MoveTargets.AddRange(
            targets
                .Where(target => target != null)
                .Select(target => target.JoinedActionController())
                .Distinct()
        );
    }

    private void Fight11ClearMoveConstraints()
    {
        fight11MoveTargets.Clear();
        fight11ModelStartEngagements.Clear();
        fight11ModelStartTargetDistances.Clear();
        fight11BaseContactLockedModels.Clear();
        fight11ConsolidationObjective = null;
    }

    private List<SquadController> Fight11EngagedEnemies(SquadController unit)
    {
        if (unit == null)
            return new List<SquadController>();

        unit = unit.JoinedActionController();

        return squads
            .Where(
                enemy =>
                    enemy != null &&
                    enemy.IsAlive &&
                    enemy.IsOnBattlefield &&
                    !enemy.IsAttachedLeader &&
                    enemy.FactionId != unit.FactionId &&
                    CoreRules11Aircraft.CanFightTarget(unit, enemy) &&
                    UnitsAreEngaged(unit, enemy)
            )
            .Select(enemy => enemy.JoinedActionController())
            .Distinct()
            .ToList();
    }

    private List<SquadController> Fight11EnemiesWithin(
        SquadController unit,
        float inches)
    {
        if (unit == null)
            return new List<SquadController>();

        unit = unit.JoinedActionController();

        return squads
            .Where(
                enemy =>
                    enemy != null &&
                    enemy.IsAlive &&
                    enemy.IsOnBattlefield &&
                    !enemy.IsAttachedLeader &&
                    enemy.FactionId != unit.FactionId &&
                    CoreRules11Aircraft.CanFightTarget(unit, enemy) &&
                    Fight11UnitDistance(unit, enemy) <= inches + 0.001f
            )
            .Select(enemy => enemy.JoinedActionController())
            .Distinct()
            .ToList();
    }

    private List<ObjectiveController> Fight11ObjectivesWithin(
        SquadController unit,
        float inches)
    {
        if (unit == null)
            return new List<ObjectiveController>();

        return objectives
            .Where(
                objective =>
                    objective != null &&
                    Fight11UnitToObjectiveDistance(unit, objective) <= inches + 0.001f
            )
            .ToList();
    }

    private float Fight11UnitDistance(
        SquadController a,
        SquadController b)
    {
        if (a == null || b == null)
            return float.MaxValue;

        float best = float.MaxValue;

        foreach (ModelToken ma in JoinedModels(a))
        {
            if (ma == null || !ma.IsAlive)
                continue;

            foreach (ModelToken mb in JoinedModels(b))
            {
                if (mb == null || !mb.IsAlive)
                    continue;

                float horizontal = CoreRules11Geometry.HorizontalBaseGap(ma, mb);
                float vertical = CoreRules11Geometry.VerticalBaseGap(ma, mb);
                best = Mathf.Min(best, Mathf.Sqrt(horizontal * horizontal + vertical * vertical));
            }
        }

        return best;
    }

    private float Fight11UnitToObjectiveDistance(
        SquadController unit,
        ObjectiveController objective)
    {
        if (unit == null || objective == null)
            return float.MaxValue;

        float best = float.MaxValue;

        foreach (ModelToken model in JoinedModels(unit))
        {
            if (model == null || !model.IsAlive)
                continue;

            best = Mathf.Min(
                best,
                Fight11ModelToObjectiveDistance(model, model.transform.position, objective)
            );
        }

        return best;
    }

    private float Fight11ModelToObjectiveDistance(
        ModelToken model,
        Vector3 position,
        ObjectiveController objective)
    {
        Vector2 a = new Vector2(position.x, position.z);
        Vector2 b = new Vector2(objective.transform.position.x, objective.transform.position.z);
        float horizontal = Mathf.Max(0f, Vector2.Distance(a, b) - model.BaseRadiusInches);
        float vertical = Mathf.Abs(
            (position.y - CoreRules11Geometry.TokenBasePlaneOffset) -
            objective.transform.position.y
        );
        return Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
    }

    private bool Fight11ModelInBaseContactWithEnemy(ModelToken model)
    {
        if (model == null)
            return false;

        return squads.Any(
            enemy =>
                enemy != null &&
                enemy.IsAlive &&
                enemy.IsOnBattlefield &&
                enemy.FactionId != model.Squad.FactionId &&
                JoinedModels(enemy).Any(
                    other =>
                        other != null &&
                        other.IsAlive &&
                        CoreRules11Geometry.HorizontalBaseGap(model, other) <= 0.03f &&
                        CoreRules11Geometry.VerticalBaseGap(model, other) <= 0.10f
                )
        );
    }

    private bool Fight11ModelEngagedWithUnit(
        ModelToken model,
        SquadController enemy)
    {
        if (model == null || enemy == null)
            return false;

        return JoinedModels(enemy).Any(
            other =>
                other != null &&
                other.IsAlive &&
                CoreRules11Geometry.ModelsEngaged(model, other)
        );
    }

    private float Fight11DistanceToClosestSelectedTarget(
        ModelToken model,
        Vector3 position)
    {
        if (model == null || fight11MoveTargets.Count == 0)
            return float.MaxValue;

        float best = float.MaxValue;

        foreach (SquadController target in fight11MoveTargets)
        {
            if (target == null)
                continue;

            foreach (ModelToken enemy in JoinedModels(target))
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                Vector2 a = new Vector2(position.x, position.z);
                Vector2 b = new Vector2(enemy.transform.position.x, enemy.transform.position.z);
                float horizontal = Mathf.Max(
                    0f,
                    Vector2.Distance(a, b) - model.BaseRadiusInches - enemy.BaseRadiusInches
                );
                float vertical = Mathf.Abs(
                    (position.y - CoreRules11Geometry.TokenBasePlaneOffset) -
                    CoreRules11Geometry.ModelBasePlaneY(enemy)
                );
                float distance = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
                best = Mathf.Min(best, distance);
            }
        }

        return best;
    }

    private void Fight11OpenTargetSubsetChoice(
        SquadController mover,
        List<SquadController> candidates,
        string title,
        Action<List<SquadController>> selected)
    {
        candidates = candidates
            .Where(unit => unit != null)
            .Select(unit => unit.JoinedActionController())
            .Distinct()
            .ToList();

        if (candidates.Count <= 1)
        {
            if (selected != null)
                selected(candidates);
            return;
        }

        List<RuleChoiceOption> options = new List<RuleChoiceOption>();

        if (candidates.Count <= 4)
        {
            int combinations = (1 << candidates.Count);

            for (int mask = 1; mask < combinations; mask++)
            {
                List<SquadController> subset = new List<SquadController>();
                for (int i = 0; i < candidates.Count; i++)
                {
                    if ((mask & (1 << i)) != 0)
                        subset.Add(candidates[i]);
                }

                List<SquadController> captured = subset.ToList();
                options.Add(
                    new RuleChoiceOption(
                        string.Join(" + ", captured.Select(u => u.DisplayName).ToArray()),
                        () =>
                        {
                            CloseRuleChoice();
                            if (selected != null) selected(captured);
                        })
                );
            }
        }
        else
        {
            foreach (SquadController candidate in candidates)
            {
                SquadController capturedUnit = candidate;
                options.Add(
                    new RuleChoiceOption(
                        capturedUnit.DisplayName,
                        () =>
                        {
                            CloseRuleChoice();
                            if (selected != null)
                                selected(new List<SquadController> { capturedUnit });
                        })
                );
            }

            List<SquadController> all = candidates.ToList();
            options.Add(
                new RuleChoiceOption(
                    "ALL ELIGIBLE TARGETS",
                    () =>
                    {
                        CloseRuleChoice();
                        if (selected != null) selected(all);
                    })
            );
        }

        OpenRuleChoice(
            title,
            mover.DisplayName + ": select one or more legal targets.",
            options.ToArray()
        );
    }

    private bool Fight11CanLeaveFightPhase(out string reason)
    {
        reason = "";

        if (phase != Phase.Fight)
            return true;

        if (fight11Step == Fight11Step.Complete)
            return true;

        if (fight11Step == Fight11Step.None)
        {
            reason = "Resolve the start-of-Fight-phase rules before ending the phase.";
            return false;
        }

        reason =
            fight11Step == Fight11Step.PileIn
            ? "Complete both players' Pile In step first."
            : fight11Step == Fight11Step.Fight
                ? "All eligible units must be selected to fight before the Fight step ends."
                : "Complete both players' Consolidate step first.";

        return false;
    }

    private void DrawFight11ContextControls(Rect bar, ref float x)
    {
        if (fightActivationUnit != null &&
            fightActivationStage == FightActivationStage.PileIn)
        {
            GUI.Label(
                new Rect(x, bar.y + 10f, 190f, 24f),
                fight11OverrunInProgress ? "OVERRUN PILE-IN" : "PILE-IN"
            );
            x += 195f;

            if (GUI.Button(new Rect(x, bar.y + 6f, 128f, 30f), "DONE PILE-IN"))
                Fight11CompletePileIn();

            x += 135f;
            GUI.Label(
                new Rect(x, bar.y + 10f, bar.width - (x - bar.x) - 12f, 24f),
                "Select model  ->  click board  |  hold ALT to measure"
            );
            return;
        }

        if (fightActivationUnit != null &&
            fightActivationStage == FightActivationStage.Attacks)
        {
            int total = FightPotentialAttackModels().Count;
            int resolved = fightModelsResolvedThisActivation.Count(model => model != null);

            GUI.Label(new Rect(x, bar.y + 10f, 155f, 24f), "MELEE  " + resolved + "/" + total);
            x += 160f;

            if (selectedModel != null &&
                ModelBelongsToFightActivation(selectedModel) &&
                !fightModelsResolvedThisActivation.Contains(selectedModel))
            {
                if (GUI.Button(new Rect(x, bar.y + 6f, 108f, 30f), "DONE MODEL"))
                    Fight11SkipSelectedFightModel();
                x += 115f;
            }

            if (GUI.Button(new Rect(x, bar.y + 6f, 118f, 30f), "DONE ATTACKS"))
                Fight11CompleteFightAttacks();

            x += 125f;
            GUI.Label(
                new Rect(x, bar.y + 10f, bar.width - (x - bar.x) - 12f, 24f),
                "Model  ->  weapon  ->  enemy"
            );
            return;
        }

        if (fightActivationUnit != null &&
            fightActivationStage == FightActivationStage.Consolidate)
        {
            GUI.Label(
                new Rect(x, bar.y + 10f, 190f, 24f),
                fight11ConsolidationMode.ToString().ToUpperInvariant() + " CONSOLIDATE"
            );
            x += 195f;

            if (GUI.Button(new Rect(x, bar.y + 6f, 158f, 30f), "DONE CONSOLIDATE"))
                Fight11CompleteConsolidation();

            x += 165f;
            GUI.Label(
                new Rect(x, bar.y + 10f, bar.width - (x - bar.x) - 12f, 24f),
                "Select model  ->  click board  |  hold ALT to measure"
            );
            return;
        }

        if (fight11Step == Fight11Step.PileIn)
        {
            GUI.Label(
                new Rect(x, bar.y + 10f, 240f, 24f),
                "PILE IN  -  " + DisplayFactionName(fight11StepFaction)
            );
            x += 245f;

            if (GUI.Button(new Rect(x, bar.y + 6f, 160f, 30f), "DONE SIDE PILE-IN"))
                Fight11FinishCurrentPileInSide();

            x += 167f;
            GUI.Label(
                new Rect(x, bar.y + 10f, bar.width - (x - bar.x) - 12f, 24f),
                "Select unit  ->  enemy to pile in  |  optional"
            );
            return;
        }

        if (fight11Step == Fight11Step.Fight)
        {
            GUI.Label(
                new Rect(x, bar.y + 10f, 420f, 24f),
                Fight11FightPriorityText() + "  |  select unit/model  ->  enemy"
            );
            x += 425f;

            if (Fight11SelectedUnitCanBeMarkedUnable())
            {
                if (GUI.Button(new Rect(x, bar.y + 6f, 150f, 30f), "NO LEGAL FIGHT"))
                    Fight11MarkSelectedUnitUnableToFight(selectedSquad.JoinedActionController());
            }
            return;
        }

        if (fight11Step == Fight11Step.Consolidate)
        {
            GUI.Label(
                new Rect(x, bar.y + 10f, 260f, 24f),
                "CONSOLIDATE  -  " + DisplayFactionName(fight11StepFaction)
            );
            x += 265f;

            if (Fight11CanObjectiveConsolidateSelected())
            {
                if (GUI.Button(new Rect(x, bar.y + 6f, 170f, 30f), "OBJECTIVE CONSOLIDATE"))
                    Fight11BeginObjectiveConsolidationForSelected();
                x += 177f;
            }

            if (GUI.Button(new Rect(x, bar.y + 6f, 190f, 30f), "DONE SIDE CONSOLIDATE"))
                Fight11FinishCurrentConsolidationSide();
            return;
        }

        GUI.Label(
            new Rect(x, bar.y + 10f, bar.width - (x - bar.x) - 12f, 24f),
            fight11Step == Fight11Step.Complete
                ? "FIGHT COMBAT STEPS COMPLETE"
                : "Resolve start-of-Fight-phase rules"
        );
    }
}
