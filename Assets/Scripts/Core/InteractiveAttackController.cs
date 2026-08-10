using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum InteractiveAttackStage
{
    RollHits,
    ReviewHits,
    RollWounds,
    ReviewWounds,
    RollSaves,
    ReviewSaves,
    RollDamage,
    ReviewDamage,
    ApplyDamage,
    WeaponComplete,
    AttackComplete
}

public class InteractiveWeaponVolley
{
    public WeaponData weapon;
    public List<WeaponAttackSelection> selections =
        new List<WeaponAttackSelection>();

    public int attacks;
    public int skill;
    public int woundTarget;
    public int criticalWoundThreshold;
    public int saveTarget;

    public int effectiveStrength;
    public int effectiveAp;
    public bool saveUsesInvulnerable;

    public int hitRollModifier;
    public int woundRollModifier;
    public int minimumUnmodifiedHit;
    public bool cannotRerollHits;
    public bool automaticHitRerolls;
    public bool automaticWoundRerolls;
    public string ruleSummary;

    public bool torrent;
    public bool lethalHits;
    public int sustainedHits;
    public bool twinLinked;
    public bool devastating;
    public bool precision;
    public int meltaBonus;

    public List<int> hitRolls =
        new List<int>();

    public List<int> woundRolls =
        new List<int>();

    public List<int> saveRolls =
        new List<int>();

    public List<int> damageValues =
        new List<int>();

    public int hits;
    public int lethalAutoWounds;
    public int normalWounds;
    public int devastatingWounds;
    public int failedSaves;

    public bool hitCommandRerollUsed;
    public bool woundCommandRerollUsed;
    public bool saveCommandRerollUsed;
    public bool damageCommandRerollUsed;

    public int woundsLost;
    public int modelsKilled;
}

public class InteractiveCasualtyChoice
{
    public ModelToken automaticModel;
    public int previousWounds;
    public Vector3 previousPosition;

    public string Label
    {
        get
        {
            if (automaticModel == null)
                return "Casualty";

            return
                automaticModel.Squad.DisplayName +
                "  -  " +
                automaticModel.RoleName;
        }
    }
}

public class InteractiveAttackController
{
    private readonly GameController game;
    private readonly SquadController attacker;
    private readonly SquadController target;
    private readonly AttackMode mode;

    private readonly List<InteractiveWeaponVolley> volleys =
        new List<InteractiveWeaponVolley>();

    private int volleyIndex;
    private InteractiveAttackStage stage;

    private int totalWoundsLost;
    private int totalModelsKilled;

    private readonly List<ModelToken> destroyedModels =
        new List<ModelToken>();

    private readonly List<InteractiveCasualtyChoice>
        pendingCasualties =
            new List<InteractiveCasualtyChoice>();

    private string lastActionText = "";
    private bool factionDefensiveReactionResolved;

    public InteractiveAttackController(
        GameController gameValue,
        SquadController attackerValue,
        SquadController targetValue,
        List<WeaponAttackSelection> selections,
        AttackMode modeValue)
    {
        game = gameValue;
        attacker = attackerValue;
        target = targetValue;
        mode = modeValue;

        BuildVolleys(
            selections ??
            new List<WeaponAttackSelection>()
        );

        stage =
            volleys.Count > 0
            ? InteractiveAttackStage.RollHits
            : InteractiveAttackStage.AttackComplete;
    }

    public SquadController Attacker
    {
        get { return attacker; }
    }

    public SquadController Target
    {
        get { return target; }
    }

    public AttackMode Mode
    {
        get { return mode; }
    }

    public InteractiveAttackStage Stage
    {
        get { return stage; }
    }

    public bool IsComplete
    {
        get
        {
            return stage ==
                InteractiveAttackStage.AttackComplete;
        }
    }

    public int TotalWoundsLost
    {
        get { return totalWoundsLost; }
    }

    public int TotalModelsKilled
    {
        get { return totalModelsKilled; }
    }

    public int TotalHits
    {
        get { return volleys.Sum(volley => volley.hits); }
    }

    public IReadOnlyList<ModelToken> DestroyedModels
    {
        get { return destroyedModels; }
    }

    public bool HasPendingCasualtyChoices
    {
        get { return pendingCasualties.Count > 0; }
    }

    public InteractiveCasualtyChoice
        CurrentPendingCasualty
    {
        get
        {
            return pendingCasualties.Count > 0
                ? pendingCasualties[0]
                : null;
        }
    }

    public int PendingCasualtyCount
    {
        get { return pendingCasualties.Count; }
    }

    public int VolleyNumber
    {
        get { return volleyIndex + 1; }
    }

    public int VolleyCount
    {
        get { return volleys.Count; }
    }

    public InteractiveWeaponVolley CurrentVolley
    {
        get
        {
            if (volleyIndex < 0 ||
                volleyIndex >= volleys.Count)
            {
                return null;
            }

            return volleys[
                volleyIndex
            ];
        }
    }

    public string StageTitle
    {
        get
        {
            switch (stage)
            {
                case InteractiveAttackStage.RollHits:
                    return "HIT ROLL";

                case InteractiveAttackStage.ReviewHits:
                    return "HIT RESULTS";

                case InteractiveAttackStage.RollWounds:
                    return "WOUND ROLL";

                case InteractiveAttackStage.ReviewWounds:
                    return "WOUND RESULTS";

                case InteractiveAttackStage.RollSaves:
                    return "SAVE ROLL";

                case InteractiveAttackStage.ReviewSaves:
                    return "SAVE RESULTS";

                case InteractiveAttackStage.RollDamage:
                    return "DAMAGE ROLL";

                case InteractiveAttackStage.ReviewDamage:
                    return "DAMAGE RESULTS";

                case InteractiveAttackStage.ApplyDamage:
                    return "APPLY DAMAGE";

                case InteractiveAttackStage.WeaponComplete:
                    return "WEAPON COMPLETE";

                case InteractiveAttackStage.AttackComplete:
                    return "ATTACK COMPLETE";
            }

            return "ATTACK";
        }
    }

    public string RequirementText
    {
        get
        {
            InteractiveWeaponVolley volley =
                CurrentVolley;

            if (volley == null)
                return "";

            switch (stage)
            {
                case InteractiveAttackStage.RollHits:
                case InteractiveAttackStage.ReviewHits:
                    if (volley.torrent)
                        return "Torrent: automatically hits.";

                    return
                        "Need " +
                        volley.skill +
                        "+ to hit" +
                        (volley.hitRollModifier != 0
                            ? " | Hit modifier " +
                              (volley.hitRollModifier > 0
                                ? "+"
                                : "") +
                              volley.hitRollModifier
                            : "") +
                        (volley.minimumUnmodifiedHit > 0
                            ? " | unmodified " +
                              volley.minimumUnmodifiedHit +
                              "+ required"
                            : "") +
                        " | " +
                        volley.attacks +
                        " attack dice" +
                        (string.IsNullOrWhiteSpace(
                            volley.ruleSummary)
                            ? ""
                            : "\n" +
                              volley.ruleSummary);

                case InteractiveAttackStage.RollWounds:
                case InteractiveAttackStage.ReviewWounds:
                    return
                        "Need " +
                        volley.woundTarget +
                        "+ to wound" +
                        (volley.woundRollModifier != 0
                            ? " | Wound modifier " +
                              (volley.woundRollModifier > 0
                                ? "+"
                                : "") +
                              volley.woundRollModifier
                            : "") +
                        (volley.criticalWoundThreshold < 6
                            ? " | Critical Wound on unmodified " +
                              volley.criticalWoundThreshold +
                              "+"
                            : "");

                case InteractiveAttackStage.RollSaves:
                case InteractiveAttackStage.ReviewSaves:
                    return
                        volley.normalWounds +
                        " normal wound(s) | target save " +
                        volley.saveTarget +
                        "+" +
                        (volley.saveUsesInvulnerable
                            ? " (Invulnerable)"
                            : " (Armour)");

                case InteractiveAttackStage.RollDamage:
                case InteractiveAttackStage.ReviewDamage:
                case InteractiveAttackStage.ApplyDamage:
                    return
                        volley.failedSaves +
                        " failed save(s) + " +
                        volley.devastatingWounds +
                        " Devastating Wound(s)";

                case InteractiveAttackStage.WeaponComplete:
                    return
                        volley.woundsLost +
                        " wound(s) lost | " +
                        volley.modelsKilled +
                        " model(s) killed";
            }

            return "";
        }
    }

    public string LastActionText
    {
        get { return lastActionText; }
    }

    public IReadOnlyList<int> CurrentDice
    {
        get
        {
            InteractiveWeaponVolley volley =
                CurrentVolley;

            if (volley == null)
                return new int[0];

            if (stage ==
                    InteractiveAttackStage.ReviewHits)
            {
                return volley.hitRolls;
            }

            if (stage ==
                    InteractiveAttackStage.ReviewWounds)
            {
                return volley.woundRolls;
            }

            if (stage ==
                    InteractiveAttackStage.ReviewSaves)
            {
                return volley.saveRolls;
            }

            if (stage ==
                    InteractiveAttackStage.ReviewDamage ||
                stage ==
                    InteractiveAttackStage.ApplyDamage)
            {
                return volley.damageValues;
            }

            return new int[0];
        }
    }

    public bool CanCommandReroll
    {
        get
        {
            if (CurrentVolley == null)
                return false;

            if (GetRerollFaction() == null)
                return false;

            if (game.GetCommandPoints(
                    GetRerollFaction()) < 1)
            {
                return false;
            }

            SquadController unit =
                stage == InteractiveAttackStage.ReviewSaves
                ? target
                : attacker;

            if (unit != null &&
                (unit.JoinedActionController()
                    .IsBattleShocked ||
                 unit.JoinedActionController()
                    .TargetedByFactionStratagemThisPhase))
            {
                return false;
            }

            return FindRerollableDieIndex() >= 0;
        }
    }

    public bool IsReviewStage
    {
        get
        {
            return
                stage ==
                    InteractiveAttackStage.ReviewHits ||
                stage ==
                    InteractiveAttackStage.ReviewWounds ||
                stage ==
                    InteractiveAttackStage.ReviewSaves ||
                stage ==
                    InteractiveAttackStage.ReviewDamage;
        }
    }

    public bool IsRollStage
    {
        get
        {
            return
                stage ==
                    InteractiveAttackStage.RollHits ||
                stage ==
                    InteractiveAttackStage.RollWounds ||
                stage ==
                    InteractiveAttackStage.RollSaves ||
                stage ==
                    InteractiveAttackStage.RollDamage;
        }
    }

    public bool HasMeaningfulDecision
    {
        get
        {
            return
                CanUsePartingTheVeil ||
                CanUseMacabreResilience ||
                (IsReviewStage &&
                 CanCommandReroll);
        }
    }

    public bool CanUsePartingTheVeil
    {
        get
        {
            return
                !factionDefensiveReactionResolved &&
                stage ==
                    InteractiveAttackStage.RollHits &&
                mode == AttackMode.Melee &&
                game != null &&
                game.CanUsePartingTheVeil(
                    attacker,
                    target
                );
        }
    }

    public bool UsePartingTheVeil()
    {
        if (!CanUsePartingTheVeil)
            return false;

        if (!game.UsePartingTheVeil(
                target))
        {
            return false;
        }

        factionDefensiveReactionResolved =
            true;

        lastActionText =
            "Parting the Veil active: eligible destroyed models will fight after this attack.";

        return true;
    }

    public bool CanUseMacabreResilience
    {
        get
        {
            return
                !factionDefensiveReactionResolved &&
                stage ==
                    InteractiveAttackStage.RollHits &&
                game != null &&
                game.CanUseMacabreResilience(
                    attacker,
                    target
                );
        }
    }

    public bool UseMacabreResilience()
    {
        if (!CanUseMacabreResilience)
            return false;

        if (!game.UseMacabreResilience(
            target))
        {
            return false;
        }

        factionDefensiveReactionResolved =
            true;

        foreach (
            InteractiveWeaponVolley volley
            in volleys)
        {
            volley.woundRollModifier =
                Mathf.Max(
                    -1,
                    volley.woundRollModifier - 1
                );

            if (string.IsNullOrWhiteSpace(
                    volley.ruleSummary))
            {
                volley.ruleSummary =
                    "Macabre Resilience: -1 Wound";
            }
            else if (volley.ruleSummary.IndexOf(
                "Macabre Resilience",
                StringComparison.OrdinalIgnoreCase
            ) < 0)
            {
                volley.ruleSummary +=
                    " | Macabre Resilience: -1 Wound";
            }
        }

        lastActionText =
            "Macabre Resilience used: attacks are -1 to Wound until the end of the phase.";

        return true;
    }

    public string CommandRerollButtonText
    {
        get
        {
            if (!CanCommandReroll)
                return "Command Re-roll unavailable";

            return
                "Command Re-roll one die (1 CP)";
        }
    }

    public void RollCurrentStage()
    {
        if (IsComplete ||
            CurrentVolley == null)
        {
            return;
        }

        switch (stage)
        {
            case InteractiveAttackStage.RollHits:
                RollHits();
                break;

            case InteractiveAttackStage.RollWounds:
                RollWounds();
                break;

            case InteractiveAttackStage.RollSaves:
                RollSaves();
                break;

            case InteractiveAttackStage.RollDamage:
                RollDamage();
                break;

            case InteractiveAttackStage.ApplyDamage:
                ApplyDamage();
                break;
        }
    }

    public void Continue()
    {
        if (CurrentVolley == null)
        {
            stage =
                InteractiveAttackStage.AttackComplete;

            return;
        }

        switch (stage)
        {
            case InteractiveAttackStage.ReviewHits:
                stage =
                    InteractiveAttackStage.RollWounds;
                break;

            case InteractiveAttackStage.ReviewWounds:
                if (CurrentVolley.normalWounds > 0)
                {
                    PrepareSaveTarget();
                    stage =
                        InteractiveAttackStage.RollSaves;
                }
                else
                {
                    stage =
                        InteractiveAttackStage.RollDamage;
                }
                break;

            case InteractiveAttackStage.ReviewSaves:
                stage =
                    InteractiveAttackStage.RollDamage;
                break;

            case InteractiveAttackStage.ReviewDamage:
                stage =
                    InteractiveAttackStage.ApplyDamage;
                break;

            case InteractiveAttackStage.WeaponComplete:
                AdvanceToNextVolley();
                break;
        }
    }

    public void RollAndAdvanceIfNoDecision()
    {
        if (!IsRollStage)
            return;

        RollCurrentStage();

        if (IsReviewStage &&
            !HasMeaningfulDecision)
        {
            Continue();
        }
    }

    public bool FastResolveUntilDecision()
    {
        int guard = 0;

        while (!IsComplete &&
               guard < 256)
        {
            guard++;

            if (HasMeaningfulDecision)
            {
                lastActionText =
                    "Fast Resolve paused: a Command Re-roll decision is available.";

                return false;
            }

            if (IsRollStage)
            {
                RollCurrentStage();
                continue;
            }

            if (IsReviewStage)
            {
                Continue();
                continue;
            }

            if (stage ==
                InteractiveAttackStage.ApplyDamage)
            {
                RollCurrentStage();
                continue;
            }

            if (stage ==
                InteractiveAttackStage.WeaponComplete)
            {
                Continue();
                continue;
            }

            if (stage ==
                InteractiveAttackStage.AttackComplete)
            {
                break;
            }

            break;
        }

        return IsComplete;
    }

    public bool DeclineDecisionAndFastResolve()
    {
        if (CanUsePartingTheVeil ||
            CanUseMacabreResilience)
        {
            factionDefensiveReactionResolved =
                true;

            lastActionText =
                "Defensive faction reaction declined.";
        }
        else if (HasMeaningfulDecision)
        {
            Continue();
        }

        return FastResolveUntilDecision();
    }

    public bool UseCommandReroll()
    {
        if (!CanCommandReroll)
            return false;

        string faction =
            GetRerollFaction();

        SquadController rerollUnit =
            stage == InteractiveAttackStage.ReviewSaves
            ? target
            : attacker;

        if (!game.SpendStratagemCPForUnit(
                rerollUnit,
                1,
                "Command Re-roll"))
        {
            return false;
        }

        int index =
            FindRerollableDieIndex();

        if (index < 0)
            return false;

        InteractiveWeaponVolley volley =
            CurrentVolley;

        switch (stage)
        {
            case InteractiveAttackStage.ReviewHits:
            {
                int old =
                    volley.hitRolls[index];

                int value =
                    DiceRoller.RollD6(
                        "Command Re-roll Hit: " +
                        volley.weapon.displayName
                    );

                volley.hitRolls[index] =
                    value;

                volley.hitCommandRerollUsed =
                    true;

                RecalculateHitResults();

                lastActionText =
                    "Command Re-roll hit: " +
                    old +
                    "  ->  " +
                    value;

                break;
            }

            case InteractiveAttackStage.ReviewWounds:
            {
                int old =
                    volley.woundRolls[index];

                int value =
                    DiceRoller.RollD6(
                        "Command Re-roll Wound: " +
                        volley.weapon.displayName
                    );

                volley.woundRolls[index] =
                    value;

                volley.woundCommandRerollUsed =
                    true;

                RecalculateWoundResults();

                lastActionText =
                    "Command Re-roll wound: " +
                    old +
                    "  ->  " +
                    value;

                break;
            }

            case InteractiveAttackStage.ReviewSaves:
            {
                int old =
                    volley.saveRolls[index];

                int value =
                    DiceRoller.RollD6(
                        "Command Re-roll Save: " +
                        target.DisplayName
                    );

                volley.saveRolls[index] =
                    value;

                volley.saveCommandRerollUsed =
                    true;

                RecalculateSaveResults();

                lastActionText =
                    "Command Re-roll save: " +
                    old +
                    "  ->  " +
                    value;

                break;
            }

            case InteractiveAttackStage.ReviewDamage:
            {
                int old =
                    volley.damageValues[index];

                int value =
                    RollDamageCharacteristic(
                        volley.weapon,
                        "Command Re-roll Damage: " +
                        volley.weapon.displayName
                    ) +
                    volley.meltaBonus;

                volley.damageValues[index] =
                    value;

                volley.damageCommandRerollUsed =
                    true;

                lastActionText =
                    "Command Re-roll damage: " +
                    old +
                    "  ->  " +
                    value;

                break;
            }

            default:
                return false;
        }

        return true;
    }

    public void MarkOneShotWeaponsUsed()
    {
        if (mode != AttackMode.Ranged)
            return;

        foreach (InteractiveWeaponVolley volley
            in volleys)
        {
            foreach (WeaponAttackSelection selection
                in volley.selections)
            {
                if (selection == null ||
                    selection.model == null ||
                    selection.weapon == null)
                {
                    continue;
                }

                selection.model.MarkWeaponUsed(
                    selection.weapon
                );
            }
        }
    }

    public void MarkRangedWeaponsFiredThisTurn()
    {
        if (mode != AttackMode.Ranged)
            return;

        foreach (InteractiveWeaponVolley volley
            in volleys)
        {
            foreach (WeaponAttackSelection selection
                in volley.selections)
            {
                if (selection == null ||
                    selection.model == null ||
                    selection.weapon == null)
                {
                    continue;
                }

                selection.model
                    .MarkRangedWeaponFiredThisTurn(
                        selection.weapon
                    );
            }
        }
    }

    public ModelToken ConfirmNextCasualty(
        ModelToken chosenModel)
    {
        if (pendingCasualties.Count == 0)
            return null;

        InteractiveCasualtyChoice choice =
            pendingCasualties[0];

        pendingCasualties.RemoveAt(0);

        ModelToken automatic =
            choice.automaticModel;

        ModelToken casualty =
            chosenModel != null &&
            chosenModel.IsAlive
            ? chosenModel
            : automatic;

        if (automatic != null &&
            casualty != automatic)
        {
            automatic.Revive(
                Mathf.Max(
                    1,
                    choice.previousWounds
                ),
                choice.previousPosition
            );

            destroyedModels.Remove(
                automatic
            );

            if (casualty.IsAlive)
            {
                casualty.ApplyDamage(
                    casualty.CurrentWounds
                );
            }

            if (!destroyedModels.Contains(
                    casualty))
            {
                destroyedModels.Add(
                    casualty
                );
            }
        }

        if (casualty != null)
        {
            game.RecordModelDestroyed(
                casualty,
                attacker
            );

            game.ResolveDeadlyDemise(
                casualty
            );
        }

        target.RefreshVisuals();

        if (target.AttachedLeader != null)
            target.AttachedLeader.RefreshVisuals();

        return casualty;
    }

    public string SummaryText()
    {
        return
            attacker.DisplayName +
            " resolved " +
            volleys.Count +
            " weapon profile(s): " +
            totalWoundsLost +
            " total wound(s) lost, " +
            totalModelsKilled +
            " model(s) killed.";
    }

    private string RollList(
        List<int> values)
    {
        if (values == null ||
            values.Count == 0)
        {
            return " - ";
        }

        return
            "[" +
            string.Join(
                ", ",
                values
                    .Select(
                        value =>
                            value.ToString()
                    )
                    .ToArray()
            ) +
            "]";
    }

    public string AuditText()
    {
        List<string> lines =
            new List<string>();

        lines.Add(
            attacker.DisplayName +
            "  ->  " +
            target.DisplayName +
            "  |  " +
            mode.ToString().ToUpper()
        );

        foreach (InteractiveWeaponVolley volley
            in volleys)
        {
            if (volley == null ||
                volley.weapon == null)
            {
                continue;
            }

            string rules =
                string.IsNullOrWhiteSpace(
                    volley.ruleSummary)
                ? ""
                : "\nRules: " +
                  volley.ruleSummary;

            string hitTarget =
                volley.torrent
                ? "auto-hit"
                : volley.skill +
                  "+" +
                  (volley.hitRollModifier == 0
                    ? ""
                    : " (" +
                      (volley.hitRollModifier > 0
                        ? "+"
                        : "") +
                      volley.hitRollModifier +
                      " modifier)");

            string saveType =
                volley.saveUsesInvulnerable
                ? "Invulnerable"
                : "Armour";

            lines.Add(
                "\n" +
                volley.weapon.displayName +
                "\nATTACKS: " +
                volley.attacks +
                "\nHIT: " +
                hitTarget +
                "  |  rolls " +
                RollList(
                    volley.hitRolls
                ) +
                "  ->  " +
                volley.hits +
                " hit(s)" +
                "\nWOUND: S" +
                volley.effectiveStrength +
                " vs T" +
                target.Toughness +
                "  ->  " +
                volley.woundTarget +
                "+" +
                (volley.woundRollModifier == 0
                    ? ""
                    : " (" +
                      (volley.woundRollModifier > 0
                        ? "+"
                        : "") +
                      volley.woundRollModifier +
                      " modifier)") +
                "  |  rolls " +
                RollList(
                    volley.woundRolls
                ) +
                "  ->  " +
                (volley.normalWounds +
                 volley.devastatingWounds) +
                " wound(s)" +
                (volley.devastatingWounds > 0
                    ? " (" +
                      volley.devastatingWounds +
                      " devastating)"
                    : "") +
                "\nSAVE: " +
                saveType +
                " " +
                volley.saveTarget +
                "+  |  AP " +
                volley.effectiveAp +
                "  |  rolls " +
                RollList(
                    volley.saveRolls
                ) +
                "  ->  " +
                volley.failedSaves +
                " failed" +
                "\nDAMAGE: " +
                RollList(
                    volley.damageValues
                ) +
                "  ->  " +
                volley.woundsLost +
                " wound(s), " +
                volley.modelsKilled +
                " model(s) destroyed" +
                rules
            );
        }

        lines.Add(
            "\nTOTAL: " +
            totalWoundsLost +
            " wound(s) lost  |  " +
            totalModelsKilled +
            " model(s) destroyed"
        );

        return string.Join(
            "\n",
            lines.ToArray()
        );
    }

    private void BuildVolleys(
        List<WeaponAttackSelection> selections)
    {
        int targetModels =
            Mathf.Max(
                1,
                target.JoinedLivingModels
            );

        IEnumerable<IGrouping<string, WeaponAttackSelection>>
            groups =
                selections
                    .Where(
                        selection =>
                            selection != null &&
                            selection.model != null &&
                            selection.weapon != null
                    )
                    .GroupBy(
                        selection =>
                            (selection.weapon.id ??
                             selection.weapon.displayName) +
                            "|" +
                            EffectiveSkill(
                                selection
                            ) +
                            "|" +
                            (selection.model.Squad != null
                                ? selection.model.Squad.UnitId
                                : "unit") +
                            "|" +
                            (game.ModelCanSeeUnit(
                                selection.model,
                                target
                             )
                                ? "direct"
                                : "indirect")
                    );

        foreach (
            IGrouping<string, WeaponAttackSelection> group
            in groups)
        {
            WeaponAttackSelection first =
                group.First();

            WeaponData weapon =
                first.weapon;

            SquadController modelOwner =
                first.model.Squad != null
                ? first.model.Squad
                : attacker;

            InteractiveWeaponVolley volley =
                new InteractiveWeaponVolley();

            volley.weapon = weapon;
            volley.selections =
                group.ToList();

            volley.skill =
                EffectiveSkill(
                    first
                );

            UniversalAttackRuleState universal =
                UniversalRuleRegistry
                    .BuildAttackState(
                        game,
                        attacker,
                        target,
                        first.model,
                        weapon,
                        mode
                    );

            volley.skill =
                Mathf.Clamp(
                    volley.skill +
                    universal.skillModifier,
                    2,
                    6
                );

            if (mode == AttackMode.Ranged &&
                game != null &&
                game.Core11PlungingFireApplies(
                    first.model,
                    target
                ))
            {
                volley.skill = Mathf.Max(2, volley.skill - 1);
            }

            volley.hitRollModifier =
                universal.hitRollModifier;

            volley.woundRollModifier =
                universal.woundRollModifier;

            volley.minimumUnmodifiedHit =
                universal.minimumUnmodifiedHit;

            volley.cannotRerollHits =
                universal.cannotRerollHits;

            volley.ruleSummary =
                universal.Summary();

            volley.torrent =
                RulesEngine.HasKeyword(
                    weapon,
                    "torrent"
                );

            volley.lethalHits =
                RulesEngine.HasKeyword(
                    weapon,
                    "lethal_hits"
                ) ||
                (mode == AttackMode.Ranged &&
                 attacker
                    .JoinedActionController()
                    .FactionSoulsightActive) ||
                (mode == AttackMode.Melee &&
                 attacker
                    .JoinedActionController()
                    .KatahLethalActive) ||
                (game != null &&
                 game.AeldariGrantsLethalHits(
                    attacker,
                    mode
                 ));

            volley.lethalHits =
                volley.lethalHits ||
                CustodesFactionPack11.GrantsLethalHits(
                    attacker, mode);

                        volley.lethalHits =
                volley.lethalHits ||
                NecronsFactionPack11.GrantsLethalHits(
                    attacker, mode);

            // WARBOARD_V46_INTERACTIVE_STANDARD_LETHAL
            volley.lethalHits =
                volley.lethalHits ||
                WarboardFactionExtensionHub
                    .GrantsLethalHits(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

            volley.sustainedHits =
                WeaponRuleParser.GetValue(
                    weapon,
                    "sustained_hits",
                    RulesEngine.HasKeyword(
                        weapon,
                        "sustained_hits_1"
                    )
                    ? 1
                    : 0
                );

            if (mode == AttackMode.Melee &&
                attacker
                    .JoinedActionController()
                    .KatahSustainedActive)
            {
                volley.sustainedHits =
                    Mathf.Max(
                        1,
                        volley.sustainedHits
                    );
            }

            if (game != null)
            {
                volley.sustainedHits =
                    Mathf.Max(
                        volley.sustainedHits,
                        game.AeldariMinimumSustainedHits(
                            attacker,
                            weapon,
                            mode
                        )
                    );
            }

            if (attacker
                    .JoinedActionController()
                    .SpiritMarkTarget ==
                target.JoinedActionController())
            {
                volley.sustainedHits =
                    Mathf.Max(
                        1,
                        volley.sustainedHits
                    );
            }

                        volley.sustainedHits =
                Mathf.Max(
                    volley.sustainedHits,
                    CustodesFactionPack11.MinimumSustainedHits(
                        attacker, weapon, mode));

            volley.sustainedHits =
                Mathf.Max(
                    volley.sustainedHits,
                    NecronsFactionPack11.MinimumSustainedHits(
                        attacker, weapon, mode));

            // WARBOARD_V46_INTERACTIVE_STANDARD_SUSTAINED
            volley.sustainedHits =
                Mathf.Max(
                    volley.sustainedHits,
                    WarboardFactionExtensionHub
                        .MinimumSustainedHits(
                            attacker,
                            target,
                            weapon,
                            mode
                        )
                );

volley.twinLinked =
                RulesEngine.HasKeyword(
                    weapon,
                    "twin_linked"
                );

            volley.devastating =
                WeaponRuleParser.Has(
                    weapon,
                    "devastating_wounds"
                ) ||
                (mode == AttackMode.Ranged &&
                 weapon.displayName != null &&
                 weapon.displayName.IndexOf(
                    "Eldritch Storm",
                    StringComparison.OrdinalIgnoreCase
                 ) >= 0 &&
                 UniversalRuleRegistry.UnitHasRule(
                    modelOwner,
                    "Gaze of Ynnead"
                 )) ||
                (game != null &&
                 game.AeldariGrantsDevastatingWounds(
                    attacker,
                    mode
                 ));

                        volley.devastating =
                volley.devastating ||
                NecronsFactionPack11.GrantsDevastatingWounds(
                    attacker, weapon, mode);

            // WARBOARD_V46_INTERACTIVE_STANDARD_DEVASTATING
            volley.devastating =
                volley.devastating ||
                WarboardFactionExtensionHub
                    .GrantsDevastatingWounds(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

volley.precision =
                WeaponRuleParser.Has(
                    weapon,
                    "precision"
                );

            volley.precision =
                volley.precision ||
                CustodesFactionPack11.GrantsPrecision(
                    attacker, weapon, mode);

            volley.precision =
                volley.precision ||
                AeldariFactionPack11.GrantsPrecision(
                    attacker, weapon, mode);
            volley.precision = volley.precision ||
                (game != null &&
                 game.Core11HasEpicChallenge(first.model));

            volley.effectiveStrength =
                weapon.strength +
                AeldariFactionPack11.StrengthModifier(
                    attacker, weapon, mode);

            volley.effectiveStrength +=
                CustodesFactionPack11.StrengthModifier(
                    attacker, first.model, weapon, mode);

                        volley.effectiveStrength +=
                NecronsFactionPack11.StrengthModifier(
                    attacker, first.model, weapon, mode);

            // WARBOARD_V46_INTERACTIVE_STANDARD_STRENGTH
            volley.effectiveStrength +=
                WarboardFactionExtensionHub
                    .StrengthModifier(
                        game,
                        attacker,
                        target,
                        weapon,
                        mode
                    );

volley.effectiveAp =
                weapon.ap;

            if (mode == AttackMode.Melee &&
                attacker
                    .JoinedActionController()
                    .FactionHungryVoidActive)
            {
                volley.effectiveStrength += 1;

                if (attacker.AttachedLeader != null &&
                    attacker.AttachedLeader.IsAlive &&
                    attacker.AttachedLeader.HasKeyword(
                        "necrons") &&
                    attacker.AttachedLeader.HasKeyword(
                        "character"))
                {
                    volley.effectiveAp -= 1;
                }
            }

            if (game != null)
            {
                volley.effectiveAp +=
                    game.AeldariApModifier(
                        attacker,
                        target,
                        weapon,
                        mode
                    );
            }

                        volley.effectiveAp +=
                CustodesFactionPack11.ApModifier(
                    attacker, target, first.model, weapon, mode);

            volley.precision =
                volley.precision ||
                NecronsFactionPack11.GrantsPrecision(
                    attacker, weapon, mode);

            // WARBOARD_V46_INTERACTIVE_STANDARD_PRECISION
            volley.precision =
                volley.precision ||
                WarboardFactionExtensionHub
                    .GrantsPrecision(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

            volley.effectiveAp +=
                NecronsFactionPack11.ApModifier(
                    game, attacker, target, first.model, weapon, mode);

            // WARBOARD_V46_INTERACTIVE_STANDARD_AP
            volley.effectiveAp +=
                WarboardFactionExtensionHub
                    .ApModifier(
                        game,
                        attacker,
                        target,
                        weapon,
                        mode
                    );

volley.woundTarget =
                RulesEngine.WoundRollNeeded(
                    volley.effectiveStrength,
                    target.Toughness
                );

            volley.criticalWoundThreshold =
                WeaponRuleParser
                    .GetCriticalWoundThreshold(
                        weapon,
                        target
                    );

            volley.criticalWoundThreshold =
                AeldariFactionPack11.CriticalWoundThreshold(
                    attacker, target, weapon,
                    volley.criticalWoundThreshold);

            volley.criticalWoundThreshold =
                NecronsFactionPack11.CriticalWoundThreshold(
                    attacker, target, weapon, mode,
                    volley.criticalWoundThreshold);

            int attacks = 0;

            foreach (
                WeaponAttackSelection selection
                in volley.selections)
            {
                int oneModelAttacks =
                    Mathf.Max(
                        0,
                        RulesEngine.RollCharacteristic(
                            weapon.attacksExpression,
                            weapon.attacksPerModel
                        )
                    );

                oneModelAttacks +=
                    CustodesFactionPack11.AdditionalAttacks(
                        game, attacker, selection.model,
                        weapon, mode, target);

                                oneModelAttacks +=
                    NecronsFactionPack11.AdditionalAttacks(
                        game, attacker, selection.model,
                        weapon, mode, target);

                // WARBOARD_V46_INTERACTIVE_STANDARD_ATTACKS
                oneModelAttacks +=
                    WarboardFactionExtensionHub
                        .AdditionalAttacks(
                            attacker,
                            weapon,
                            mode
                        );

oneModelAttacks +=
                    AeldariFactionPack11.AdditionalAttacks(
                        attacker, selection.model, weapon, mode);

                float distance =
                    DistanceFromModelToUnit(
                        selection.model,
                        target
                    );

                bool halfRange =
                    mode == AttackMode.Ranged &&
                    weapon.range > 0f &&
                    distance <=
                        (weapon.range +
                         NecronsFactionPack11.RangeModifier(
                             attacker, weapon, mode)) * 0.5f +
                        0.001f;

                int rapid =
                    WeaponRuleParser.GetValue(
                        weapon,
                        "rapid_fire",
                        0
                    );

                rapid +=
                    CustodesFactionPack11.AdditionalRapidFire(
                        attacker, weapon, mode);

                                rapid +=
                    NecronsFactionPack11.AdditionalRapidFire(
                        attacker, weapon, mode);

rapid +=
                    AeldariFactionPack11.AdditionalRapidFire(
                        attacker, weapon, mode);

                if (halfRange)
                    oneModelAttacks += rapid;

                attacks +=
                    oneModelAttacks;

                if (halfRange)
                {
                    volley.meltaBonus =
                        Mathf.Max(
                            volley.meltaBonus,
                            WeaponRuleParser.GetValue(
                                weapon,
                                "melta",
                                0
                            )
                        );

                    if (mode == AttackMode.Ranged &&
                        UniversalRuleRegistry.UnitHasRule(
                            selection.model.Squad,
                            "Bladestorm"
                        ))
                    {
                        volley.sustainedHits =
                            Mathf.Max(
                                1,
                                volley.sustainedHits
                            );
                    }
                }
            }

            if (WeaponRuleParser.Has(
                weapon,
                "blast"))
            {
                int blast =
                    Mathf.Max(
                        1,
                        WeaponRuleParser.GetValue(
                            weapon,
                            "blast",
                            1
                        )
                    );

                attacks +=
                    (targetModels / 5) *
                    blast *
                    volley.selections.Count;
            }

            if (mode == AttackMode.Melee &&
                WeaponRuleParser.Has(
                    weapon,
                    "cleave"))
            {
                int cleave =
                    Mathf.Max(
                        1,
                        WeaponRuleParser.GetValue(
                            weapon,
                            "cleave",
                            1
                        )
                    );

                attacks +=
                    (targetModels / 5) *
                    cleave *
                    volley.selections.Count;
            }

            volley.attacks =
                Mathf.Max(
                    0,
                    attacks
                );

            volleys.Add(volley);
        }
    }

    private int EffectiveSkill(
        WeaponAttackSelection selection)
    {
        SquadController owner =
            selection.model.Squad != null
            ? selection.model.Squad
            : attacker;

        return
            mode == AttackMode.Ranged
            ? owner.GetRangedSkill(
                target,
                selection.weapon
              )
            : owner.GetMeleeSkill(
                target,
                selection.weapon
              );
    }

    private void RollHits()
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        volley.hitRolls.Clear();

        if (volley.torrent)
        {
            volley.hits =
                volley.attacks;

            volley.lethalAutoWounds = 0;

            lastActionText =
                "Torrent: " +
                volley.attacks +
                " automatic hit(s).";

            stage =
                InteractiveAttackStage.ReviewHits;

            return;
        }

        DiceRollRecord record =
            DiceRoller.RollDice(
                Mathf.Max(
                    1,
                    volley.attacks
                ),
                6,
                "Hit roll: " +
                volley.weapon.displayName
            );

        volley.hitRolls.AddRange(
            record.Results.Take(
                volley.attacks
            )
        );

        bool withinHalfRange =
            volley.selections.Count > 0 &&
            volley.weapon.range > 0f &&
            volley.selections.Any(
                selection =>
                    DistanceFromModelToUnit(
                        selection.model,
                        target
                    ) <=
                    (volley.weapon.range +
                     NecronsFactionPack11.RangeModifier(
                         attacker, volley.weapon, mode)) * 0.5f +
                    0.001f
            );

        bool conquering =
            mode == AttackMode.Ranged &&
            withinHalfRange &&
            attacker
                .JoinedActionController()
                .FactionConqueringTyrantActive;

        bool ledNecron =
            attacker.AttachedLeader != null &&
            attacker.AttachedLeader.IsAlive &&
            attacker.AttachedLeader.HasKeyword(
                "necrons") &&
            attacker.AttachedLeader.HasKeyword(
                "character");

        SquadController actionUnit =
            attacker.JoinedActionController();

                // WARBOARD_V46_INTERACTIVE_STANDARD_HIT_REROLLS
        bool rerollAll =
            (mode == AttackMode.Melee &&
             actionUnit
                .FactionEmissariesRerollAll) ||
            actionUnit.AeldariRerollAllHits ||
            (conquering && ledNecron) ||
            WarboardFactionExtensionHub
                .RerollAllHits(
                    game,
                    attacker,
                    target,
                    volley.weapon,
                    mode
                );

        bool rerollOnes =
            (mode == AttackMode.Melee &&
             actionUnit
                .FactionEmissariesRerollOnes) ||
            actionUnit.AeldariRerollHitOnes ||
            (conquering && !ledNecron) ||
            WarboardFactionExtensionHub
                .RerollHitOnes(
                    game,
                    attacker,
                    target,
                    volley.weapon,
                    mode
                );

        volley.automaticHitRerolls =
            rerollAll || rerollOnes;

        if (rerollAll ||
            rerollOnes)
        {
            for (int i = 0;
                 i < volley.hitRolls.Count;
                 i++)
            {
                int roll =
                    volley.hitRolls[i];

                bool failed =
                    roll == 1 ||
                    (volley.minimumUnmodifiedHit > 0 &&
                     roll <
                        volley.minimumUnmodifiedHit) ||
                    (roll != 6 &&
                     roll +
                        volley.hitRollModifier <
                        volley.skill);

                bool shouldReroll =
                    rerollAll
                    ? failed
                    : roll == 1;

                if (!shouldReroll)
                    continue;

                volley.hitRolls[i] =
                    DiceRoller.RollD6(
                        "Automatic Hit re-roll: " +
                        volley.weapon.displayName
                    );
            }
        }

        if (!volley.cannotRerollHits)
        {
            bool custodesRerolled = false;
            for (int i = 0; i < volley.hitRolls.Count; i++)
            {
                int roll = volley.hitRolls[i];
                bool success = roll != 1 &&
                    (roll == 6 ||
                     roll + volley.hitRollModifier >= volley.skill);
                if (!CustodesFactionPack11.AutomaticRerollHit(
                        game, attacker, roll, success, mode))
                    continue;
                volley.hitRolls[i] = DiceRoller.RollD6(
                    "Custodes Hit re-roll: " + volley.weapon.displayName);
                custodesRerolled = true;
            }
            if (custodesRerolled)
                volley.automaticHitRerolls = true;
        }

        if (!volley.cannotRerollHits)
        {
            bool necronsRerolled = false;
            for (int i = 0; i < volley.hitRolls.Count; i++)
            {
                int roll = volley.hitRolls[i];
                bool success = roll != 1 &&
                    (roll == 6 ||
                     roll + volley.hitRollModifier >= volley.skill);
                if (!NecronsFactionPack11.AutomaticRerollHit(
                        game, attacker, target, roll, success, mode))
                    continue;
                volley.hitRolls[i] = DiceRoller.RollD6(
                    "Necrons Hit re-roll: " + volley.weapon.displayName);
                necronsRerolled = true;
            }
            if (necronsRerolled)
                volley.automaticHitRerolls = true;
        }

        RecalculateHitResults();

        lastActionText =
            volley.hits +
            " hit(s) from " +
            volley.attacks +
            " attack(s).";

        stage =
            InteractiveAttackStage.ReviewHits;
    }

    private void RecalculateHitResults()
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        int hits = 0;
        int lethal = 0;

        foreach (int roll
            in volley.hitRolls)
        {
            if (roll == 1)
                continue;

            if (volley.minimumUnmodifiedHit > 0 &&
                roll <
                    volley.minimumUnmodifiedHit)
            {
                continue;
            }

            int modified =
                roll +
                volley.hitRollModifier;

            bool success =
                roll == 6 ||
                modified >=
                    volley.skill;

            if (!success)
                continue;

            hits++;

            if (CustodesFactionPack11.IsCriticalHit(
                    attacker, roll, success) ||
                NecronsFactionPack11.IsCriticalHit(
                    attacker, roll, success))
            {
                if (volley.lethalHits)
                    lethal++;

                if (volley.sustainedHits > 0)
                {
                    hits +=
                        volley.sustainedHits;
                }
            }
        }

        volley.hits = hits;
        volley.lethalAutoWounds =
            lethal;
    }

    private void RollWounds()
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        volley.woundRolls.Clear();

        int woundDice =
            Mathf.Max(
                0,
                volley.hits -
                volley.lethalAutoWounds
            );

        if (woundDice == 0)
        {
            volley.normalWounds =
                volley.lethalAutoWounds;

            volley.devastatingWounds = 0;

            lastActionText =
                "No wound dice required; " +
                volley.lethalAutoWounds +
                " Lethal Hit wound(s) proceed.";

            stage =
                InteractiveAttackStage.ReviewWounds;

            return;
        }

        DiceRollRecord record =
            DiceRoller.RollDice(
                woundDice,
                6,
                "Wound roll: " +
                volley.weapon.displayName
            );

        volley.woundRolls.AddRange(
            record.Results
        );

        if (volley.twinLinked)
        {
            for (int i = 0;
                 i < volley.woundRolls.Count;
                 i++)
            {
                int roll =
                    volley.woundRolls[i];

                bool success =
                    roll != 1 &&
                    (roll >=
                        volley.woundTarget ||
                     roll >=
                        volley.criticalWoundThreshold);

                if (success)
                    continue;

                volley.woundRolls[i] =
                    DiceRoller.RollD6(
                        "Twin-linked wound re-roll: " +
                        volley.weapon.displayName
                    );
            }

            volley.automaticWoundRerolls = true;
        }

        SquadController modelOwner =
            volley.selections.Count > 0
            ? volley.selections[0].model.Squad
            : attacker;

        bool morbidMight =
            mode == AttackMode.Melee &&
            UniversalRuleRegistry.UnitHasRule(
                modelOwner,
                "Morbid Might"
            );

        bool immortals =
            modelOwner != null &&
            modelOwner.DisplayName.IndexOf(
                "Immortal",
                StringComparison.OrdinalIgnoreCase
            ) >= 0 &&
            UniversalRuleRegistry.UnitHasRule(
                modelOwner,
                "Implacable Eradication"
            );

        bool heraldOfYnnead =
            game != null &&
            game.HasHeraldOfYnneadReroll(
                modelOwner,
                target
            );

        bool targetOnObjective =
            immortals &&
            game.UnitWithinAnyObjective(
                target
            );

        SquadController woundActionUnit =
            attacker.JoinedActionController();

        bool aeldariRerollAll =
            !volley.twinLinked &&
            woundActionUnit
                .AeldariRerollAllWounds;

        bool aeldariRerollOnes =
            !volley.twinLinked &&
            woundActionUnit
                .AeldariRerollWoundOnes;

        if (morbidMight ||
            immortals ||
            heraldOfYnnead ||
            aeldariRerollAll ||
            aeldariRerollOnes)
        {
            for (int i = 0;
                 i < volley.woundRolls.Count;
                 i++)
            {
                int roll =
                    volley.woundRolls[i];

                bool critical =
                    roll >=
                        volley.criticalWoundThreshold;

                bool success =
                    roll != 1 &&
                    (critical ||
                     roll == 6 ||
                     roll +
                        volley.woundRollModifier >=
                        volley.woundTarget);

                bool reroll =
                    morbidMight ||
                    targetOnObjective ||
                    aeldariRerollAll
                    ? !success
                    : roll == 1;

                if (!reroll)
                    continue;

                volley.woundRolls[i] =
                    DiceRoller.RollD6(
                        "Automatic Wound re-roll: " +
                        volley.weapon.displayName
                    );
            }

            volley.automaticWoundRerolls = true;
        }

        bool custodesWoundRerolled = false;
        for (int i = 0; i < volley.woundRolls.Count; i++)
        {
            int roll = volley.woundRolls[i];
            bool critical = roll >= volley.criticalWoundThreshold;
            bool success = roll != 1 &&
                (critical || roll == 6 ||
                 roll + volley.woundRollModifier >= volley.woundTarget);
            if (!CustodesFactionPack11.AutomaticRerollWound(
                    attacker, target, roll, success, mode))
                continue;
            volley.woundRolls[i] = DiceRoller.RollD6(
                "Custodes Wound re-roll: " + volley.weapon.displayName);
            custodesWoundRerolled = true;
        }
        if (custodesWoundRerolled)
            volley.automaticWoundRerolls = true;

        bool necronsWoundRerolled = false;
        for (int i = 0; i < volley.woundRolls.Count; i++)
        {
            int roll = volley.woundRolls[i];
            bool critical = roll >= volley.criticalWoundThreshold;
            bool success = roll != 1 &&
                (critical || roll == 6 ||
                 roll + volley.woundRollModifier >= volley.woundTarget);
            if (!NecronsFactionPack11.AutomaticRerollWound(
                    game, attacker, target, roll, success, mode))
                continue;
            volley.woundRolls[i] = DiceRoller.RollD6(
                "Necrons Wound re-roll: " + volley.weapon.displayName);
            necronsWoundRerolled = true;
        }
        if (necronsWoundRerolled)
            volley.automaticWoundRerolls = true;

        // WARBOARD_V46_INTERACTIVE_STANDARD_WOUND_REROLLS
        bool standardWoundRerolled = false;

        if (!volley.automaticWoundRerolls)
        {
        for (int i = 0;
             i < volley.woundRolls.Count;
             i++)
        {
            int roll =
                volley.woundRolls[i];

            bool critical =
                roll >=
                volley.criticalWoundThreshold;

            bool success =
                roll != 1 &&
                (critical ||
                 roll == 6 ||
                 roll +
                    volley.woundRollModifier >=
                    volley.woundTarget);

            bool shouldReroll =
                WarboardFactionExtensionHub
                    .RerollAllWounds(
                        game,
                        attacker,
                        target,
                        volley.weapon,
                        mode
                    )
                ? !success
                : WarboardFactionExtensionHub
                    .RerollWoundOnes(
                        game,
                        attacker,
                        target,
                        volley.weapon,
                        mode
                    ) &&
                  roll == 1;

            if (!shouldReroll)
                continue;

            volley.woundRolls[i] =
                DiceRoller.RollD6(
                    "Faction Wound re-roll: " +
                    volley.weapon.displayName
                );

            standardWoundRerolled =
                true;
        }
        }

        if (standardWoundRerolled)
        {
            volley.automaticWoundRerolls =
                true;
        }

        RecalculateWoundResults();

        lastActionText =
            (volley.normalWounds +
             volley.devastatingWounds) +
            " successful wound(s).";

        stage =
            InteractiveAttackStage.ReviewWounds;
    }

    private void RecalculateWoundResults()
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        int normal =
            volley.lethalAutoWounds;

        int dev = 0;

        foreach (int roll
            in volley.woundRolls)
        {
            if (roll == 1)
                continue;

            bool critical =
                roll >=
                    volley.criticalWoundThreshold;

            int modified =
                roll +
                volley.woundRollModifier;

            bool success =
                critical ||
                roll == 6 ||
                modified >=
                    volley.woundTarget;

            if (!success)
                continue;

            if (critical &&
                volley.devastating)
            {
                dev++;
            }
            else
            {
                normal++;
            }
        }

        volley.normalWounds = normal;
        volley.devastatingWounds = dev;
    }

    private void PrepareSaveTarget()
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        ModelToken allocated =
            GetAllocationModel(
                volley.precision
            );

        if (allocated == null)
        {
            volley.saveTarget = 7;
            volley.saveUsesInvulnerable = false;
            return;
        }

        SquadController owner =
            allocated.Squad;

        SquadController attackOwner =
            volley.selections.Count > 0 &&
            volley.selections[0].model != null
            ? volley.selections[0].model.Squad
            : attacker;

        int armourSave =
            Mathf.Clamp(
                owner.GetSave(
                    attackOwner
                ) -
                volley.effectiveAp,
                2,
                7
            );

        int invulnerable =
            allocated.InvulnerableSave;

        if (game != null)
        {
            int aeldariInvulnerable =
                game.AeldariInvulnerableOverride(
                    owner
                );

                        // WARBOARD_V46_INTERACTIVE_STANDARD_INVULN
            int standardInvulnerable =
                WarboardFactionExtensionHub
                    .InvulnerableOverride(
                        game,
                        owner
                    );

            if (standardInvulnerable > 0 &&
                (invulnerable <= 0 ||
                 standardInvulnerable <
                    invulnerable))
            {
                invulnerable =
                    standardInvulnerable;
            }

            if (aeldariInvulnerable > 0)
            {
                invulnerable =
                    invulnerable > 0
                    ? Mathf.Min(
                        invulnerable,
                        aeldariInvulnerable
                      )
                    : aeldariInvulnerable;
            }
        }

        if (invulnerable > 0 &&
            invulnerable <
                armourSave)
        {
            volley.saveTarget =
                invulnerable;

            volley.saveUsesInvulnerable =
                true;
        }
        else
        {
            volley.saveTarget =
                armourSave;

            volley.saveUsesInvulnerable =
                false;
        }
    }

    private void RollSaves()
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        volley.saveRolls.Clear();

        if (volley.normalWounds <= 0)
        {
            volley.failedSaves = 0;

            lastActionText =
                "No normal saves required.";

            stage =
                InteractiveAttackStage.ReviewSaves;

            return;
        }

        PrepareSaveTarget();

        DiceRollRecord record =
            DiceRoller.RollDice(
                volley.normalWounds,
                6,
                "Save roll: " +
                target.DisplayName
            );

        volley.saveRolls.AddRange(
            record.Results
        );

        RecalculateSaveResults();

        lastActionText =
            volley.failedSaves +
            " failed save(s).";

        stage =
            InteractiveAttackStage.ReviewSaves;
    }

    private void RecalculateSaveResults()
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        volley.failedSaves =
            volley.saveRolls.Count(
                roll =>
                    roll <
                    volley.saveTarget
            );
    }

    private void RollDamage()
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        volley.damageValues.Clear();

        int damageEvents =
            volley.failedSaves +
            volley.devastatingWounds;

        if (damageEvents <= 0)
        {
            lastActionText =
                "No damage rolls required.";

            stage =
                InteractiveAttackStage.ReviewDamage;

            return;
        }

        for (int i = 0;
             i < damageEvents;
             i++)
        {
            int damage =
                RollDamageCharacteristic(
                    volley.weapon,
                    "Damage: " +
                    volley.weapon.displayName
                ) +
                volley.meltaBonus +
                (game != null
                    ? game.AeldariDamageModifier(
                        attacker,
                        volley.weapon,
                        mode
                      )
                    : 0);

            damage +=
                NecronsFactionPack11.DamageModifier(
                    attacker,
                    volley.selections.Count > 0
                        ? volley.selections[0].model
                        : null,
                    volley.weapon, mode);

            volley.damageValues.Add(
                Mathf.Max(
                    0,
                    damage
                )
            );
        }

        lastActionText =
            "Rolled " +
            damageEvents +
            " damage result(s).";

        stage =
            InteractiveAttackStage.ReviewDamage;
    }

    private int RollDamageCharacteristic(
        WeaponData weapon,
        string label)
    {
        if (weapon == null)
            return 0;

        if (string.IsNullOrWhiteSpace(
            weapon.damageExpression))
        {
            return weapon.damage;
        }

        return RollExpression(
            weapon.damageExpression,
            weapon.damage,
            label
        );
    }

    private int RollExpression(
        string expression,
        int fallback,
        string label)
    {
        if (string.IsNullOrWhiteSpace(
            expression))
        {
            return fallback;
        }

        string value =
            expression
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "");

        int flat;

        if (int.TryParse(
            value,
            out flat))
        {
            return flat;
        }

        int dIndex =
            value.IndexOf('D');

        if (dIndex < 0)
            return fallback;

        int count = 1;

        if (dIndex > 0)
        {
            int parsed;

            if (int.TryParse(
                value.Substring(
                    0,
                    dIndex
                ),
                out parsed))
            {
                count =
                    Mathf.Max(
                        1,
                        parsed
                    );
            }
        }

        int modifierIndex = -1;

        for (int i = dIndex + 1;
             i < value.Length;
             i++)
        {
            if (value[i] == '+' ||
                value[i] == '-')
            {
                modifierIndex = i;
                break;
            }
        }

        string sidesText =
            modifierIndex >= 0
            ? value.Substring(
                dIndex + 1,
                modifierIndex -
                    dIndex -
                    1
              )
            : value.Substring(
                dIndex + 1
              );

        int sides;

        if (!int.TryParse(
            sidesText,
            out sides))
        {
            return fallback;
        }

        DiceRollRecord record =
            DiceRoller.RollDice(
                count,
                sides,
                label
            );

        int result =
            record.Total;

        if (modifierIndex >= 0)
        {
            int modifier;

            if (int.TryParse(
                value.Substring(
                    modifierIndex
                ),
                out modifier))
            {
                result += modifier;
            }
        }

        return Mathf.Max(
            0,
            result
        );
    }

    private void ApplyDamage()
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        volley.woundsLost = 0;
        volley.modelsKilled = 0;

        int normalEvents =
            volley.failedSaves;

        int devEvents =
            volley.devastatingWounds;

        int damageIndex = 0;

        for (int i = 0;
             i < normalEvents;
             i++)
        {
            if (damageIndex >=
                volley.damageValues.Count)
            {
                break;
            }

            ModelToken allocated =
                GetAllocationModel(
                    volley.precision
                );

            if (allocated == null)
                break;

            bool wasAlive =
                allocated.IsAlive;

            int previousWounds =
                allocated.CurrentWounds;

            Vector3 previousPosition =
                allocated.transform.position;

            int attackDamage =
                volley.damageValues[
                    damageIndex
                ];

            if (UniversalRuleRegistry.UnitHasRule(
                    allocated.Squad,
                    "Implacable Resilience"
                ))
            {
                attackDamage =
                    Mathf.Max(
                        1,
                        attackDamage - 1
                    );
            }

            attackDamage =
                CustodesFactionPack11.ModifyIncomingDamage(
                    allocated, attacker, volley.weapon, attackDamage);

            attackDamage =
                NecronsFactionPack11.ModifyIncomingDamage(
                    allocated.Squad, attackDamage);

            int incoming =
                Mathf.Min(
                    allocated.CurrentWounds,
                    attackDamage);

            int afterFnp =
                UniversalRuleRegistry
                    .ApplyFeelNoPain(
                        allocated.Squad,
                        incoming,
                        volley.weapon.displayName
                    );

            int lost =
                allocated.ApplyDamage(
                    afterFnp
                );

            volley.woundsLost +=
                lost;

            if (wasAlive &&
                !allocated.IsAlive)
            {
                volley.modelsKilled++;

                if (!destroyedModels.Contains(
                        allocated))
                {
                    destroyedModels.Add(
                        allocated
                    );
                }

                pendingCasualties.Add(
                    new InteractiveCasualtyChoice
                    {
                        automaticModel =
                            allocated,
                        previousWounds =
                            previousWounds,
                        previousPosition =
                            previousPosition
                    }
                );
            }

            damageIndex++;
        }

        // Devastating Wounds are applied after normal damage. Each individual
        // critical wound is capped to the currently allocated model.
        for (int i = 0;
             i < devEvents;
             i++)
        {
            if (damageIndex >=
                volley.damageValues.Count)
            {
                break;
            }

            ModelToken allocated =
                GetAllocationModel(
                    volley.precision
                );

            if (allocated == null)
                break;

            bool wasAlive =
                allocated.IsAlive;

            int previousWounds =
                allocated.CurrentWounds;

            Vector3 previousPosition =
                allocated.transform.position;

            int attackDamage =
                volley.damageValues[
                    damageIndex
                ];

            if (UniversalRuleRegistry.UnitHasRule(
                    allocated.Squad,
                    "Implacable Resilience"
                ))
            {
                attackDamage =
                    Mathf.Max(
                        1,
                        attackDamage - 1
                    );
            }

            attackDamage =
                CustodesFactionPack11.ModifyIncomingDamage(
                    allocated, attacker, volley.weapon, attackDamage, false);

            attackDamage =
                NecronsFactionPack11.ModifyIncomingDamage(
                    allocated.Squad, attackDamage);

            int incoming =
                Mathf.Min(
                    allocated.CurrentWounds,
                    attackDamage);

            int afterFnp =
                UniversalRuleRegistry
                    .ApplyFeelNoPain(
                        allocated.Squad,
                        incoming,
                        "Devastating Wounds: " +
                        volley.weapon.displayName
                    );

            int lost =
                allocated.ApplyDamage(
                    afterFnp
                );

            volley.woundsLost +=
                lost;

            if (wasAlive &&
                !allocated.IsAlive)
            {
                volley.modelsKilled++;

                if (!destroyedModels.Contains(
                        allocated))
                {
                    destroyedModels.Add(
                        allocated
                    );
                }

                pendingCasualties.Add(
                    new InteractiveCasualtyChoice
                    {
                        automaticModel =
                            allocated,
                        previousWounds =
                            previousWounds,
                        previousPosition =
                            previousPosition
                    }
                );
            }

            damageIndex++;
        }

        totalWoundsLost +=
            volley.woundsLost;

        totalModelsKilled +=
            volley.modelsKilled;

        target.RefreshVisuals();

        if (target.AttachedLeader != null)
            target.AttachedLeader.RefreshVisuals();

        ResolveHazardous(
            volley
        );

        lastActionText =
            "Applied " +
            volley.woundsLost +
            " wound(s); " +
            volley.modelsKilled +
            " model(s) killed.";

        stage =
            InteractiveAttackStage.WeaponComplete;
    }

    private void ResolveHazardous(
        InteractiveWeaponVolley volley)
    {
        if (!WeaponRuleParser.Has(
            volley.weapon,
            "hazardous"))
        {
            return;
        }

        foreach (
            WeaponAttackSelection selection
            in volley.selections)
        {
            int roll =
                DiceRoller.RollD6(
                    "Hazardous: " +
                    volley.weapon.displayName
                );

            if (roll > 2)
                continue;

            int mortal =
                attacker.HasKeyword(
                    "monster") ||
                attacker.HasKeyword(
                    "vehicle")
                ? 3
                : 1;

            ModelToken allocated =
                attacker
                    .GetAutomaticAllocationModel();

            if (allocated == null &&
                attacker.AttachedLeader != null)
            {
                allocated =
                    attacker.AttachedLeader
                        .GetAutomaticAllocationModel();
            }

            if (allocated != null)
            {
                allocated.ApplyDamage(
                    UniversalRuleRegistry.ApplyFeelNoPain(
                        allocated.Squad,
                        mortal,
                        "Hazardous"
                    )
                );
            }
        }

        attacker.RefreshVisuals();

        if (attacker.AttachedLeader != null)
            attacker.AttachedLeader.RefreshVisuals();
    }

    private void AdvanceToNextVolley()
    {
        if (!target.IsAlive &&
            (target.AttachedLeader == null ||
             !target.AttachedLeader.IsAlive))
        {
            stage =
                InteractiveAttackStage.AttackComplete;

            return;
        }

        volleyIndex++;

        if (volleyIndex >=
            volleys.Count)
        {
            stage =
                InteractiveAttackStage.AttackComplete;

            return;
        }

        stage =
            InteractiveAttackStage.RollHits;

        lastActionText =
            "Next weapon profile.";
    }

    private int FindRerollableDieIndex()
    {
        InteractiveWeaponVolley volley =
            CurrentVolley;

        if (volley == null)
            return -1;

        switch (stage)
        {
            case InteractiveAttackStage.ReviewHits:
                if (volley.hitCommandRerollUsed ||
                    volley.torrent ||
                    volley.cannotRerollHits ||
                    volley.automaticHitRerolls)
                {
                    return -1;
                }

                for (int i = 0;
                     i < volley.hitRolls.Count;
                     i++)
                {
                    int roll =
                        volley.hitRolls[i];

                    if (roll == 1)
                        return i;

                    if (volley.minimumUnmodifiedHit > 0 &&
                        roll <
                            volley.minimumUnmodifiedHit)
                    {
                        return i;
                    }

                    if (roll != 6 &&
                        roll +
                            volley.hitRollModifier <
                            volley.skill)
                    {
                        return i;
                    }
                }

                return -1;

            case InteractiveAttackStage.ReviewWounds:
                if (volley.woundCommandRerollUsed ||
                    volley.twinLinked ||
                    volley.automaticWoundRerolls)
                {
                    return -1;
                }

                for (int i = 0;
                     i < volley.woundRolls.Count;
                     i++)
                {
                    int value =
                        volley.woundRolls[i];

                    bool critical =
                        value >=
                            volley.criticalWoundThreshold;

                    bool success =
                        value != 1 &&
                        (critical ||
                         value == 6 ||
                         value +
                            volley.woundRollModifier >=
                            volley.woundTarget);

                    if (!success)
                        return i;
                }

                return -1;

            case InteractiveAttackStage.ReviewSaves:
                if (volley.saveCommandRerollUsed)
                    return -1;

                return LowestFailedIndex(
                    volley.saveRolls,
                    volley.saveTarget
                );

            case InteractiveAttackStage.ReviewDamage:
                if (volley.damageCommandRerollUsed ||
                    string.IsNullOrWhiteSpace(
                        volley.weapon.damageExpression))
                {
                    return -1;
                }

                if (volley.damageValues.Count == 0)
                    return -1;

                int lowestIndex = 0;

                for (int i = 1;
                     i < volley.damageValues.Count;
                     i++)
                {
                    if (volley.damageValues[i] <
                        volley.damageValues[
                            lowestIndex
                        ])
                    {
                        lowestIndex = i;
                    }
                }

                return lowestIndex;
        }

        return -1;
    }

    private int LowestFailedIndex(
        List<int> values,
        int targetNumber)
    {
        int bestIndex = -1;
        int bestValue =
            int.MaxValue;

        for (int i = 0;
             i < values.Count;
             i++)
        {
            int value =
                values[i];

            if (value >=
                targetNumber)
            {
                continue;
            }

            if (value < bestValue)
            {
                bestValue = value;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private string GetRerollFaction()
    {
        if (stage ==
            InteractiveAttackStage.ReviewSaves)
        {
            return target.FactionId;
        }

        if (stage ==
                InteractiveAttackStage.ReviewHits ||
            stage ==
                InteractiveAttackStage.ReviewWounds ||
            stage ==
                InteractiveAttackStage.ReviewDamage)
        {
            return attacker.FactionId;
        }

        return null;
    }

    private ModelToken GetAllocationModel(
        bool precision)
    {
        SquadController actionTarget =
            target.JoinedActionController();

        if (precision &&
            actionTarget.AttachedLeader != null &&
            actionTarget.AttachedLeader.IsAlive)
        {
            ModelToken character =
                actionTarget.AttachedLeader
                    .LivingModelTokens()
                    .FirstOrDefault(
                        candidate =>
                            game.ModelCanSeeModel(
                                CurrentVolley
                                    .selections[0]
                                    .model,
                                candidate
                            )
                    );

            if (character != null)
                return character;
        }

        ModelToken allocated =
            actionTarget
                .GetAutomaticAllocationModel();

        if (allocated == null &&
            actionTarget.AttachedLeader != null &&
            actionTarget.AttachedLeader.IsAlive)
        {
            allocated =
                actionTarget.AttachedLeader
                    .GetAutomaticAllocationModel();
        }

        return allocated;
    }

    private float DistanceFromModelToUnit(
        ModelToken model,
        SquadController unit)
    {
        float best =
            float.MaxValue;

        foreach (ModelToken targetModel
            in unit.JoinedLivingModelTokens())
        {
            float distance =
                Vector2.Distance(
                    new Vector2(
                        model.transform.position.x,
                        model.transform.position.z
                    ),
                    new Vector2(
                        targetModel.transform.position.x,
                        targetModel.transform.position.z
                    )
                );

            best =
                Mathf.Min(
                    best,
                    distance
                );
        }

        return best ==
            float.MaxValue
            ? 999f
            : best;
    }
}
