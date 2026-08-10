using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AttackMode
{
    Ranged,
    Melee
}

public class WeaponAttackSelection
{
    public ModelToken model;
    public WeaponData weapon;

    public WeaponAttackSelection(
        ModelToken modelValue,
        WeaponData weaponValue)
    {
        model = modelValue;
        weapon = weaponValue;
    }
}

public struct DamageResult
{
    public int woundsLost;
    public int modelsKilled;

    public DamageResult(
        int woundsLostValue,
        int modelsKilledValue)
    {
        woundsLost = woundsLostValue;
        modelsKilled = modelsKilledValue;
    }
}

public struct AttackResult
{
    public string text;

    public int attacks;
    public int hits;
    public int wounds;
    public int failedSaves;
    public int woundsLost;
    public int modelsKilled;

    public AttackResult(
        string textValue,
        int attacksValue,
        int hitsValue,
        int woundsValue,
        int failedSavesValue,
        int woundsLostValue,
        int modelsKilledValue)
    {
        text = textValue;
        attacks = attacksValue;
        hits = hitsValue;
        wounds = woundsValue;
        failedSaves = failedSavesValue;
        woundsLost = woundsLostValue;
        modelsKilled = modelsKilledValue;
    }
}

public static class RulesEngine
{
    public static AttackResult ResolveWeaponAttacks(
        GameController game,
        SquadController attacker,
        SquadController target,
        List<WeaponAttackSelection> selections,
        AttackMode mode)
    {
        int totalAttacks = 0;
        int totalHits = 0;
        int totalWounds = 0;
        int totalFailedSaves = 0;
        int totalWoundsLost = 0;
        int totalModelsKilled = 0;

        int targetModelsAtSelection =
            Mathf.Max(
                1,
                target.JoinedLivingModels
            );

        Dictionary<string, WeaponReport> reports =
            new Dictionary<string, WeaponReport>();

        foreach (WeaponAttackSelection selection
            in selections)
        {
            if (selection == null)
                continue;

            ModelToken model =
                selection.model;

            WeaponData weapon =
                selection.weapon;

            if (model == null ||
                !model.IsAlive ||
                weapon == null)
            {
                continue;
            }

            WeaponReport report;

            if (!reports.TryGetValue(
                weapon.displayName,
                out report))
            {
                report =
                    new WeaponReport(
                        weapon.displayName
                    );

                reports[
                    weapon.displayName
                ] = report;
            }

            float targetDistance =
                DistanceFromModelToUnit(
                    model,
                    target
                );

            bool halfRange =
                mode == AttackMode.Ranged &&
                weapon.range > 0f &&
                targetDistance <=
                    (weapon.range +
                     NecronsFactionPack11.RangeModifier(
                         attacker, weapon, mode)) * 0.5f +
                    0.001f;

            int attacks =
                Mathf.Max(
                    0,
                    RollCharacteristic(
                        weapon.attacksExpression,
                        weapon.attacksPerModel
                    )
                );

            attacks +=
                CustodesFactionPack11.AdditionalAttacks(
                    game, attacker, model, weapon, mode, target);

                        attacks +=
                NecronsFactionPack11.AdditionalAttacks(
                    game, attacker, model, weapon, mode, target);

            // WARBOARD_V46_RULES_STANDARD_ATTACKS
            attacks +=
                WarboardFactionExtensionHub
                    .AdditionalAttacks(
                        attacker,
                        weapon,
                        mode
                    );

attacks +=
                AeldariFactionPack11.AdditionalAttacks(
                    attacker, model, weapon, mode);

            int rapidFire =
                WeaponRuleParser.GetValue(
                    weapon,
                    "rapid_fire",
                    0
                );

            rapidFire +=
                CustodesFactionPack11.AdditionalRapidFire(
                    attacker, weapon, mode);

                        rapidFire +=
                NecronsFactionPack11.AdditionalRapidFire(
                    attacker, weapon, mode);

rapidFire +=
                AeldariFactionPack11.AdditionalRapidFire(
                    attacker, weapon, mode);

            if (halfRange &&
                rapidFire > 0)
            {
                attacks +=
                    rapidFire;

                report.rapidFireDice +=
                    rapidFire;
            }

            if (WeaponRuleParser.Has(
                weapon,
                "blast"))
            {
                int blastValue =
                    Mathf.Max(
                        1,
                        WeaponRuleParser.GetValue(
                            weapon,
                            "blast",
                            1
                        )
                    );

                int blastDice =
                    (targetModelsAtSelection / 5) *
                    blastValue;

                attacks +=
                    blastDice;

                report.blastDice +=
                    blastDice;
            }

            SquadController attackOwner =
                model.Squad != null
                ? model.Squad
                : attacker;

            int skill =
                mode == AttackMode.Ranged
                ? attackOwner.GetRangedSkill(
                    target,
                    weapon
                  )
                : attackOwner.GetMeleeSkill(
                    target,
                    weapon
                  );

            // v39 11e Benefit of Cover: cover worsens the attacking BS
            // characteristic by 1; it does not improve the target's save.
            bool v39BenefitOfCover =
                mode == AttackMode.Ranged &&
                game != null &&
                (game.TargetUnitHasCoverFromShooter(
                    model,
                    target
                 ) ||
                 UniversalRuleRegistry.UnitHasRule(
                    target.JoinedActionController(),
                    "stealth"
                 ));

            bool v39IgnoresCover =
                WeaponRuleParser.Has(
                    weapon,
                    "ignores_cover"
                );

            if (v39BenefitOfCover &&
                !v39IgnoresCover)
            {
                skill =
                    Mathf.Min(
                        7,
                        skill + 1
                    );
            }

            // v41 / 22.05 Plunging Fire improves BS by 1. It does not
            // apply to attacks made by or targeting AIRCRAFT.
            if (mode == AttackMode.Ranged &&
                game != null &&
                game.Core11PlungingFireApplies(
                    model,
                    target
                ))
            {
                skill = Mathf.Max(2, skill - 1);
            }

                        UniversalAttackRuleState aeldari11UniversalState =
                UniversalRuleRegistry.BuildAttackState(
                    game, attacker, target, model, weapon, mode);

bool torrent =
                HasKeyword(
                    weapon,
                    "torrent"
                );

            bool lethalHits =
                HasKeyword(
                    weapon,
                    "lethal_hits"
                );

            lethalHits = lethalHits ||
                CustodesFactionPack11.GrantsLethalHits(attacker, mode);

            lethalHits =
                lethalHits ||
                AeldariFactionPack11.GrantsLethalHits(
                    attacker, mode);

                        lethalHits = lethalHits ||
                NecronsFactionPack11.GrantsLethalHits(
                    attacker, mode);

            // WARBOARD_V46_RULES_STANDARD_LETHAL
            lethalHits =
                lethalHits ||
                WarboardFactionExtensionHub
                    .GrantsLethalHits(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

int sustainedHits =
                WeaponRuleParser.GetValue(
                    weapon,
                    "sustained_hits",
                    HasKeyword(
                        weapon,
                        "sustained_hits_1"
                    )
                    ? 1
                    : 0
                );

            sustainedHits = Mathf.Max(
                sustainedHits,
                CustodesFactionPack11.MinimumSustainedHits(
                    attacker, weapon, mode));

            sustainedHits =
                Mathf.Max(
                    sustainedHits,
                    AeldariFactionPack11.MinimumSustainedHits(
                        attacker, weapon, mode));

                        sustainedHits = Mathf.Max(
                sustainedHits,
                NecronsFactionPack11.MinimumSustainedHits(
                    attacker, weapon, mode));

            // WARBOARD_V46_RULES_STANDARD_SUSTAINED
            sustainedHits =
                Mathf.Max(
                    sustainedHits,
                    WarboardFactionExtensionHub
                        .MinimumSustainedHits(
                            attacker,
                            target,
                            weapon,
                            mode
                        )
                );

bool twinLinked =
                HasKeyword(
                    weapon,
                    "twin_linked"
                );

            bool devastating =
                WeaponRuleParser.Has(
                    weapon,
                    "devastating_wounds"
                );

            devastating =
                devastating ||
                AeldariFactionPack11.GrantsDevastatingWounds(
                    attacker, weapon, mode);

                        devastating = devastating ||
                NecronsFactionPack11.GrantsDevastatingWounds(
                    attacker, weapon, mode);

            // WARBOARD_V46_RULES_STANDARD_DEVASTATING
            devastating =
                devastating ||
                WarboardFactionExtensionHub
                    .GrantsDevastatingWounds(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

bool precision =
                WeaponRuleParser.Has(
                    weapon,
                    "precision"
                ) ||
                (game != null &&
                 game.Core11HasEpicChallenge(model));

            precision = precision ||
                CustodesFactionPack11.GrantsPrecision(
                    attacker, weapon, mode);

            precision =
                precision ||
                AeldariFactionPack11.GrantsPrecision(
                    attacker, weapon, mode);

                        precision = precision ||
                NecronsFactionPack11.GrantsPrecision(
                    attacker, weapon, mode);

            // WARBOARD_V46_RULES_STANDARD_PRECISION
            precision =
                precision ||
                WarboardFactionExtensionHub
                    .GrantsPrecision(
                        attacker,
                        target,
                        weapon,
                        mode
                    );

int melta =
                halfRange
                ? Mathf.Max(
                    0,
                    WeaponRuleParser.GetValue(
                        weapon,
                        "melta",
                        0
                    )
                  )
                : 0;

            int hits = 0;
            int lethalAutoWounds = 0;

            for (int i = 0;
                 i < attacks;
                 i++)
            {
                if (torrent)
                {
                    hits++;
                    continue;
                }

                int hitRoll =
                    DiceRoller.RollD6(
                        "Hit: " +
                        weapon.displayName
                    );

                bool custodesHitSuccess =
                    AeldariFactionPack11.AutomaticHitSucceeds(
                        hitRoll, skill, aeldari11UniversalState);
                if (!aeldari11UniversalState.cannotRerollHits &&
                    CustodesFactionPack11.AutomaticRerollHit(
                        game, attacker, hitRoll, custodesHitSuccess, mode))
                {
                    hitRoll = DiceRoller.RollD6(
                        "Custodes Hit re-roll: " + weapon.displayName);
                }

                                bool necronsHitSuccess =
                    AeldariFactionPack11.AutomaticHitSucceeds(
                        hitRoll, skill, aeldari11UniversalState);
                if (!aeldari11UniversalState.cannotRerollHits &&
                    NecronsFactionPack11.AutomaticRerollHit(
                        game, attacker, target, hitRoll,
                        necronsHitSuccess, mode))
                {
                    hitRoll = DiceRoller.RollD6(
                        "Necrons Hit re-roll: " + weapon.displayName);
                }

if (!aeldari11UniversalState.cannotRerollHits &&
                    AeldariFactionPack11.AutomaticRerollHit(
                        attacker, hitRoll, skill, aeldari11UniversalState))
                {
                    hitRoll = DiceRoller.RollD6(
                        "Aeldari Hit re-roll: " + weapon.displayName);
                }

                // WARBOARD_V46_RULES_STANDARD_HIT_REROLLS
                if (!aeldari11UniversalState.cannotRerollHits)
                {
                    bool standardHitSuccess =
                        AeldariFactionPack11
                            .AutomaticHitSucceeds(
                                hitRoll,
                                skill,
                                aeldari11UniversalState
                            );

                    bool rerollStandardHit =
                        WarboardFactionExtensionHub
                            .RerollAllHits(
                                game,
                                attacker,
                                target,
                                weapon,
                                mode
                            )
                        ? !standardHitSuccess
                        : WarboardFactionExtensionHub
                            .RerollHitOnes(
                                game,
                                attacker,
                                target,
                                weapon,
                                mode
                            ) &&
                          hitRoll == 1;

                    if (rerollStandardHit)
                    {
                        hitRoll =
                            DiceRoller.RollD6(
                                "Faction Hit re-roll: " +
                                weapon.displayName
                            );
                    }
                }

                if (!AeldariFactionPack11.AutomaticHitSucceeds(
                        hitRoll, skill, aeldari11UniversalState))
                {
                    int rerolledHit;

                    if (game != null &&
                        game.TryConsumeArmedCommandReroll(
                            attacker.FactionId,
                            CommandRerollStage.Hit,
                            hitRoll,
                            skill,
                            attackOwner,
                            target,
                            out rerolledHit))
                    {
                        hitRoll =
                            rerolledHit;
                    }
                }

                GameEventBus.Raise(
                    new GameEventContext
                    {
                        Type =
                            GameEventType.HitRolled,
                        Game = game,
                        ActingFaction =
                            attacker.FactionId,
                        Phase =
                            game != null
                            ? game.CurrentPhase
                            : GameController.Phase.Shoot,
                        Source =
                            attackOwner,
                        Target = target,
                        AttackMode = mode,
                        RollTotal =
                            hitRoll,
                        IsReroll = false
                    }
                );

                if (!AeldariFactionPack11.AutomaticHitSucceeds(
                        hitRoll, skill, aeldari11UniversalState))
                    continue;

                hits++;

                if (NecronsFactionPack11.IsCriticalHit(
                        attacker, hitRoll, true))
                {
                    if (lethalHits)
                        lethalAutoWounds++;

                    if (sustainedHits > 0)
                    {
                        hits +=
                            sustainedHits;

                        report.sustainedExtraHits +=
                            sustainedHits;
                    }
                }
            }

            int normalWounds =
                lethalAutoWounds;

            int devastatingWounds = 0;

            int normalWoundRolls =
                Mathf.Max(
                    0,
                    hits -
                    lethalAutoWounds
                );

                        // WARBOARD_V46_RULES_STANDARD_STRENGTH
            int effectiveStrength =
                weapon.strength +
                WarboardFactionExtensionHub
                    .StrengthModifier(
                        game,
                        attacker,
                        target,
                        weapon,
                        mode
                    );

            int woundTarget =
                WoundRollNeeded(
                    effectiveStrength,
                    target.Toughness
                );

            int criticalThreshold =
                WeaponRuleParser
                    .GetCriticalWoundThreshold(
                        weapon,
                        target
                    );

            criticalThreshold =
                AeldariFactionPack11.CriticalWoundThreshold(
                    attacker, target, weapon, criticalThreshold);

            for (int i = 0;
                 i < normalWoundRolls;
                 i++)
            {
                int woundRoll =
                    DiceRoller.RollD6(
                        "Wound: " +
                        weapon.displayName
                    );

                bool success =
                    AeldariFactionPack11.AutomaticWoundSucceeds(
                        woundRoll, woundTarget, criticalThreshold,
                        aeldari11UniversalState.woundRollModifier);

                bool alreadyRerolled =
                    false;

                if (NecronsFactionPack11.AutomaticRerollWound(
                        game, attacker, target, woundRoll, success, mode))
                {
                    woundRoll = DiceRoller.RollD6(
                        "Necrons Wound re-roll: " + weapon.displayName);
                    success = AeldariFactionPack11.AutomaticWoundSucceeds(
                        woundRoll, woundTarget, criticalThreshold,
                        aeldari11UniversalState.woundRollModifier);
                    alreadyRerolled = true;
                }

                if (CustodesFactionPack11.AutomaticRerollWound(
                        attacker, target, woundRoll, success, mode))
                {
                    woundRoll = DiceRoller.RollD6(
                        "Custodes Wound re-roll: " + weapon.displayName);
                    success = AeldariFactionPack11.AutomaticWoundSucceeds(
                        woundRoll, woundTarget, criticalThreshold,
                        aeldari11UniversalState.woundRollModifier);
                    alreadyRerolled = true;
                }

                if (AeldariFactionPack11.AutomaticRerollWound(
                        attacker, woundRoll, success, mode))
                {
                    woundRoll = DiceRoller.RollD6(
                        "Aeldari Wound re-roll: " + weapon.displayName);
                    success = AeldariFactionPack11.AutomaticWoundSucceeds(
                        woundRoll, woundTarget, criticalThreshold,
                        aeldari11UniversalState.woundRollModifier);
                    alreadyRerolled = true;
                }

                                // WARBOARD_V46_RULES_STANDARD_WOUND_REROLLS
                if (!alreadyRerolled)
                {
                    bool standardReroll =
                        WarboardFactionExtensionHub
                            .RerollAllWounds(
                                game,
                                attacker,
                                target,
                                weapon,
                                mode
                            )
                        ? !success
                        : WarboardFactionExtensionHub
                            .RerollWoundOnes(
                                game,
                                attacker,
                                target,
                                weapon,
                                mode
                            ) &&
                          woundRoll == 1;

                    if (standardReroll)
                    {
                        woundRoll =
                            DiceRoller.RollD6(
                                "Faction Wound re-roll: " +
                                weapon.displayName
                            );

                        success =
                            AeldariFactionPack11
                                .AutomaticWoundSucceeds(
                                    woundRoll,
                                    woundTarget,
                                    criticalThreshold,
                                    aeldari11UniversalState
                                        .woundRollModifier
                                );

                        alreadyRerolled = true;
                    }
                }

                if (!success &&
                    twinLinked)
                {
                    woundRoll =
                        DiceRoller.RollD6(
                            "Twin-linked: " +
                            weapon.displayName
                        );

                    alreadyRerolled = true;

                    success =
                        AeldariFactionPack11.AutomaticWoundSucceeds(
                            woundRoll, woundTarget, criticalThreshold,
                            aeldari11UniversalState.woundRollModifier);
                }

                if (!success &&
                    !alreadyRerolled)
                {
                    int rerolledWound;

                    if (game != null &&
                        game.TryConsumeArmedCommandReroll(
                            attacker.FactionId,
                            CommandRerollStage.Wound,
                            woundRoll,
                            woundTarget,
                            attackOwner,
                            target,
                            out rerolledWound))
                    {
                        woundRoll =
                            rerolledWound;

                        alreadyRerolled = true;

                        success =
                        AeldariFactionPack11.AutomaticWoundSucceeds(
                            woundRoll, woundTarget, criticalThreshold,
                            aeldari11UniversalState.woundRollModifier);
                    }
                }

                bool critical =
                    woundRoll >=
                    criticalThreshold;

                if (critical)
                    success = true;

                GameEventBus.Raise(
                    new GameEventContext
                    {
                        Type =
                            GameEventType.WoundRolled,
                        Game = game,
                        ActingFaction =
                            attacker.FactionId,
                        Phase =
                            game != null
                            ? game.CurrentPhase
                            : GameController.Phase.Shoot,
                        Source =
                            attackOwner,
                        Target = target,
                        AttackMode = mode,
                        RollTotal =
                            woundRoll,
                        IsReroll =
                            alreadyRerolled
                    }
                );

                if (!success)
                    continue;

                if (critical &&
                    devastating)
                {
                    devastatingWounds++;
                }
                else
                {
                    normalWounds++;
                }
            }

                        int necronsApModifier =
                NecronsFactionPack11.ApModifier(
                    game, attacker, target, model, weapon, mode);

            // WARBOARD_V46_RULES_STANDARD_AP
            int standardApModifier =
                WarboardFactionExtensionHub
                    .ApModifier(
                        game,
                        attacker,
                        target,
                        weapon,
                        mode
                    );

int failedSaves = 0;
            int woundsLost = 0;
            int modelsKilled = 0;
            int coverSaves = 0;

            for (int i = 0;
                 i < normalWounds;
                 i++)
            {
                ModelToken allocated =
                    GetAllocationModel(
                        game,
                        model,
                        target,
                        precision
                    );

                if (allocated == null)
                    break;

                SquadController saveOwner =
                    allocated.Squad;

                int saveTarget =
                    Mathf.Clamp(
                        saveOwner.GetSave(
                            attackOwner
                                                ) -
                        weapon.ap +
                        necronsApModifier -
                        standardApModifier,
                        2,
                        7
                    );

                int saveRoll =
                    DiceRoller.RollD6(
                        "Save: " +
                        allocated.Squad.DisplayName
                    );

                if (saveRoll <
                    saveTarget)
                {
                    failedSaves++;

                    bool wasAlive =
                        allocated.IsAlive;

                    int rolledDamage =
                        Mathf.Max(
                            0,
                            RollCharacteristic(
                                weapon.damageExpression,
                                weapon.damage
                            ) +
                            melta
                        );

                    rolledDamage =
                        CustodesFactionPack11.ModifyIncomingDamage(
                            allocated, attacker, weapon, rolledDamage);

                    rolledDamage =
                        NecronsFactionPack11.ModifyIncomingDamage(
                            allocated.Squad, rolledDamage);

                    int lost =
                        allocated.ApplyDamage(
                            UniversalRuleRegistry.ApplyFeelNoPain(
                                allocated.Squad,
                                rolledDamage,
                                weapon.displayName
                            )
                        );

                    woundsLost += lost;

                    if (wasAlive &&
                        !allocated.IsAlive)
                    {
                        modelsKilled++;
                    }
                }
            }

            // 11e Devastating Wounds: resolve normal damage first, then each
            // critical wound inflicts mortal wounds equal to D. Excess mortal
            // wounds from each individual critical wound cannot spill into a
            // second model.
            for (int i = 0;
                 i < devastatingWounds;
                 i++)
            {
                ModelToken allocated =
                    GetAllocationModel(
                        game,
                        model,
                        target,
                        precision
                    );

                if (allocated == null)
                    break;

                bool wasAlive =
                    allocated.IsAlive;

                int mortalDamage =
                    Mathf.Max(
                        0,
                        RollCharacteristic(
                            weapon.damageExpression,
                            weapon.damage
                        ) +
                        melta
                    );

                mortalDamage =
                    CustodesFactionPack11.ModifyIncomingDamage(
                        allocated, attacker, weapon, mortalDamage, false);

                mortalDamage =
                    NecronsFactionPack11.ModifyIncomingDamage(
                        allocated.Squad, mortalDamage);

                int lost =
                    allocated.ApplyDamage(
                        UniversalRuleRegistry.ApplyFeelNoPain(
                            allocated.Squad,
                            mortalDamage,
                            "Devastating Wounds: " +
                            weapon.displayName
                        )
                    );

                woundsLost += lost;

                report.devastatingCriticals++;

                if (wasAlive &&
                    !allocated.IsAlive)
                {
                    modelsKilled++;
                }
            }

            target.RefreshVisuals();

            if (target.AttachedLeader != null)
                target.AttachedLeader.RefreshVisuals();

            int weaponWounds =
                normalWounds +
                devastatingWounds;

            totalAttacks += attacks;
            totalHits += hits;
            totalWounds += weaponWounds;
            totalFailedSaves +=
                failedSaves;
            totalWoundsLost +=
                woundsLost;
            totalModelsKilled +=
                modelsKilled;

            report.RecordModel(model);
            report.weaponInstances++;
            report.attacks += attacks;
            report.hits += hits;
            report.wounds +=
                weaponWounds;
            report.failedSaves +=
                failedSaves;
            report.woundsLost +=
                woundsLost;
            report.modelsKilled +=
                modelsKilled;
            report.coverSaves +=
                coverSaves;

            if (precision &&
                target.AttachedLeader != null)
            {
                report.usedPrecision = true;
            }

            if (melta > 0)
                report.meltaBonus = melta;
        }

        int hazardTests = 0;
        int hazardFailures = 0;
        int hazardMortalWounds = 0;

        foreach (WeaponAttackSelection selection
            in selections)
        {
            if (selection == null ||
                selection.weapon == null ||
                !WeaponRuleParser.Has(
                    selection.weapon,
                    "hazardous"))
            {
                continue;
            }

            hazardTests++;

            int hazardRoll =
                DiceRoller.RollD6(
                    "Hazardous"
                );

            if (hazardRoll > 2)
                continue;

            hazardFailures++;

            int mortalWounds =
                attacker.HasKeyword(
                    "monster") ||
                attacker.HasKeyword(
                    "vehicle")
                ? 3
                : 1;

            ModelToken hazardTarget =
                attacker
                    .GetAutomaticAllocationModel();

            if (hazardTarget == null &&
                attacker.AttachedLeader != null)
            {
                hazardTarget =
                    attacker.AttachedLeader
                        .GetAutomaticAllocationModel();
            }

            if (hazardTarget == null)
                continue;

            hazardMortalWounds +=
                hazardTarget.ApplyDamage(
                    UniversalRuleRegistry.ApplyFeelNoPain(
                        hazardTarget.Squad,
                        mortalWounds,
                        "Hazardous"
                    )
                );
        }

        attacker.RefreshVisuals();

        if (attacker.AttachedLeader != null)
            attacker.AttachedLeader.RefreshVisuals();

        string action =
            mode == AttackMode.Ranged
            ? "shoots"
            : "fights";

        string detail =
            string.Join(
                " | ",
                reports.Values
                    .Select(
                        report =>
                            report.ToText()
                    )
                    .ToArray()
            );

        string text =
            attacker.DisplayName +
            " " +
            action +
            ": " +
            detail;

        if (reports.Count == 0)
        {
            text =
                attacker.DisplayName +
                " has no eligible " +
                (mode == AttackMode.Ranged
                    ? "weapons."
                    : "melee weapons.");
        }

        if (hazardTests > 0)
        {
            text +=
                " | HAZARDOUS: " +
                hazardTests +
                " test(s), " +
                hazardFailures +
                " failed, " +
                hazardMortalWounds +
                " MW suffered.";
        }

        bool joinedTargetDestroyed =
            !target.IsAlive &&
            (target.AttachedLeader == null ||
             !target.AttachedLeader.IsAlive);

        if (joinedTargetDestroyed)
        {
            text +=
                " Target unit destroyed.";
        }

        return new AttackResult(
            text,
            totalAttacks,
            totalHits,
            totalWounds,
            totalFailedSaves,
            totalWoundsLost,
            totalModelsKilled
        );
    }

    private static float DistanceFromModelToUnit(
        ModelToken model,
        SquadController target)
    {
        if (model == null ||
            target == null)
        {
            return 999f;
        }

        float best =
            float.MaxValue;

        foreach (ModelToken targetModel
            in target.JoinedLivingModelTokens())
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

    private static ModelToken GetAllocationModel(
        GameController game,
        ModelToken shooter,
        SquadController target,
        bool precision)
    {
        if (target == null)
            return null;

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
                            game == null ||
                            game.ModelCanSeeModel(
                                shooter,
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

    // Compatibility wrapper for any older call sites.
    public static AttackResult ResolveModelAttacks(
        GameController game,
        SquadController attacker,
        SquadController target,
        List<ModelToken> attackingModels,
        AttackMode mode)
    {
        List<WeaponAttackSelection> selections =
            new List<WeaponAttackSelection>();

        foreach (ModelToken model
            in attackingModels)
        {
            if (model == null)
                continue;

            WeaponData weapon =
                mode == AttackMode.Ranged
                ? model.RangedWeapon
                : model.MeleeWeapon;

            if (weapon != null)
            {
                selections.Add(
                    new WeaponAttackSelection(
                        model,
                        weapon
                    )
                );
            }
        }

        return ResolveWeaponAttacks(
            game,
            attacker,
            target,
            selections,
            mode
        );
    }

    public static int WoundRollNeeded(
        int strength,
        int toughness)
    {
        if (strength >=
            toughness * 2)
        {
            return 2;
        }

        if (strength >
            toughness)
        {
            return 3;
        }

        if (strength ==
            toughness)
        {
            return 4;
        }

        if (strength * 2 <=
            toughness)
        {
            return 6;
        }

        return 5;
    }

    public static float AverageCharacteristic(
        string expression,
        int fallback)
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

        int diceCount = 1;

        if (dIndex > 0)
        {
            int parsedCount;

            if (int.TryParse(
                value.Substring(
                    0,
                    dIndex
                ),
                out parsedCount))
            {
                diceCount =
                    Mathf.Max(
                        1,
                        parsedCount
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
                    (dIndex + 1)
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

        float result =
            diceCount *
            (sides + 1) *
            0.5f;

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
            0f,
            result
        );
    }

    public static int RollCharacteristic(
        string expression,
        int fallback)
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

        int diceCount = 1;

        if (dIndex > 0)
        {
            int parsedCount;

            if (int.TryParse(
                value.Substring(
                    0,
                    dIndex
                ),
                out parsedCount))
            {
                diceCount =
                    Mathf.Max(
                        1,
                        parsedCount
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
                    (dIndex + 1)
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

        int result = 0;

        for (int die = 0;
             die < diceCount;
             die++)
        {
            result +=
                DiceRoller.RollExpressionDie(
                    sides,
                    "Characteristic D" +
                    sides
                );
        }

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

    public static bool HasKeyword(
        WeaponData weapon,
        string keyword)
    {
        return WeaponRuleParser.Has(
            weapon,
            keyword
        );
    }

    private class WeaponReport
    {
        public string name;
        public int weaponInstances;
        public int attacks;
        public int hits;
        public int wounds;
        public int failedSaves;
        public int woundsLost;
        public int modelsKilled;
        public int coverSaves;

        public int rapidFireDice;
        public int blastDice;
        public int sustainedExtraHits;
        public int devastatingCriticals;
        public int meltaBonus;
        public bool usedPrecision;

        private readonly HashSet<ModelToken> models =
            new HashSet<ModelToken>();

        public WeaponReport(string value)
        {
            name = value;
        }

        public void RecordModel(
            ModelToken model)
        {
            if (model != null)
                models.Add(model);
        }

        public string ToText()
        {
            string cover =
                coverSaves > 0
                ? ", " +
                  coverSaves +
                  " cover save(s)"
                : "";

            string copies =
                weaponInstances >
                models.Count
                ? ", " +
                  weaponInstances +
                  " weapons"
                : "";

            List<string> ruleNotes =
                new List<string>();

            if (rapidFireDice > 0)
                ruleNotes.Add(
                    "Rapid Fire +" +
                    rapidFireDice +
                    "A"
                );

            if (blastDice > 0)
                ruleNotes.Add(
                    "Blast +" +
                    blastDice +
                    "A"
                );

            if (sustainedExtraHits > 0)
                ruleNotes.Add(
                    "Sustained +" +
                    sustainedExtraHits +
                    "H"
                );

            if (devastatingCriticals > 0)
                ruleNotes.Add(
                    devastatingCriticals +
                    " Dev Wound(s)"
                );

            if (meltaBonus > 0)
                ruleNotes.Add(
                    "Melta +" +
                    meltaBonus +
                    "D"
                );

            if (usedPrecision)
                ruleNotes.Add(
                    "Precision"
                );

            string rules =
                ruleNotes.Count > 0
                ? " [" +
                  string.Join(
                    ", ",
                    ruleNotes.ToArray()
                  ) +
                  "]"
                : "";

            return
                models.Count +
                " model(s) " +
                name +
                copies +
                ": " +
                attacks +
                "A, " +
                hits +
                "H, " +
                wounds +
                "W, " +
                failedSaves +
                " failed Sv, " +
                woundsLost +
                " dmg" +
                cover +
                rules;
        }
    }
}
