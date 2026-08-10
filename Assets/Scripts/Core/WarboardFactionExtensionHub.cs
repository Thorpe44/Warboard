using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Single extension point for faction mechanics added after v45.
///
/// Existing Aeldari/Custodes/Necrons code is left intact. New factions flow
/// through this hub so future packs do not need their names copied into every
/// combat and movement subsystem.
/// </summary>
public static class WarboardFactionExtensionHub
{
    public static IFactionGameController
        TryCreateController(
            IReadOnlyList<SquadController> army)
    {
        string id =
            DetectPackId(army);

        return string.IsNullOrWhiteSpace(id)
            ? null
            : new StandardFactionGameController();
    }

    public static string DetectPackId(
        IEnumerable<SquadController> army)
    {
        if (army == null)
            return "";

        List<SquadController> units =
            army
                .Where(
                    value =>
                        value != null)
                .ToList();

        if (units.Any(
                unit =>
                    unit.HasIntrinsicKeyword(
                        "tyranids")))
        {
            return "tyranids";
        }

        if (units.Any(
                unit =>
                    unit.HasIntrinsicKeyword(
                        "orks")))
        {
            return "orks";
        }

        if (units.Any(
                unit =>
                    unit.HasIntrinsicKeyword(
                        "adeptus astartes")))
        {
            return "space_marines";
        }

        return "";
    }

    public static StandardFactionGameController
        ControllerFor(
            string factionId)
    {
        if (string.IsNullOrWhiteSpace(
                factionId) ||
            FactionControllerHost.Instance ==
                null)
        {
            return null;
        }

        return
            FactionControllerHost.Instance
                .Get(factionId)
            as StandardFactionGameController;
    }

    public static StandardFactionGameController
        ControllerFor(
            SquadController unit)
    {
        return unit == null
            ? null
            : ControllerFor(
                unit.FactionId);
    }

    public static bool IsPack(
        SquadController unit,
        string packId)
    {
        StandardFactionGameController
            controller =
                ControllerFor(unit);

        return
            controller != null &&
            string.Equals(
                controller.PackId,
                packId,
                StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasDetachment(
        SquadController unit,
        string detachment)
    {
        StandardFactionGameController
            controller =
                ControllerFor(unit);

        return
            controller != null &&
            controller.HasDetachment(
                detachment);
    }

    public static void ApplyAttackModifiers(
        GameController game,
        SquadController attacker,
        SquadController target,
        ModelToken shooter,
        WeaponData weapon,
        AttackMode mode,
        UniversalAttackRuleState state)
    {
        if (game == null ||
            attacker == null ||
            target == null ||
            state == null)
        {
            return;
        }

        StandardFactionGameController
            controller =
                ControllerFor(attacker);

        if (controller == null)
            return;

        if (controller.PackId ==
            "tyranids" &&
            IsTyranids(attacker))
        {
            if (controller.HasDetachment(
                    "CRUSHER STAMPEDE") &&
                attacker.HasKeyword(
                    "MONSTER"))
            {
                if (attacker
                    .HasAnyLostWoundsOrModels())
                {
                    state.hitRollModifier += 1;
                    state.notes.Add(
                        "Crusher Stampede: +1 Hit below Starting Strength"
                    );
                }

                if (attacker
                    .IsAtOrBelowHalfStrength())
                {
                    state.woundRollModifier += 1;
                    state.notes.Add(
                        "Crusher Stampede: +1 Wound below Half-strength"
                    );
                }
            }

            if (controller.HasDetachment(
                    "SYNAPTIC NEXUS") &&
                IsWithinSynapse(
                    game,
                    attacker) &&
                string.Equals(
                    controller.SynapticImperative,
                    "GOADED TO SLAUGHTER",
                    StringComparison.OrdinalIgnoreCase) &&
                mode ==
                    AttackMode.Melee)
            {
                state.hitRollModifier += 1;
                state.notes.Add(
                    "Goaded to Slaughter: +1 Hit"
                );
            }
        }

        if (controller.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                attacker))
        {
            if (controller.OathTarget !=
                    null &&
                target.JoinedActionController() ==
                    controller.OathTarget
                        .JoinedActionController() &&
                !controller
                    .ArmyHasSupplementKeyword())
            {
                state.woundRollModifier += 1;
                state.notes.Add(
                    "Oath of Moment: +1 Wound"
                );
            }

            if (controller.HasDetachment(
                    "ANVIL SIEGE FORCE") &&
                mode ==
                    AttackMode.Ranged &&
                weapon != null &&
                WeaponRuleParser.Has(
                    weapon,
                    "heavy") &&
                !attacker.WasSetUpThisTurn &&
                attacker
                    .MaxDistanceMovedThisTurn() <=
                    0.001f)
            {
                state.woundRollModifier += 1;
                state.notes.Add(
                    "Shield of the Imperium: stationary Heavy weapon +1 Wound"
                );
            }

            if (controller.HasDetachment(
                    "VANGUARD SPEARHEAD") &&
                mode ==
                    AttackMode.Ranged &&
                StandardDistance(
                    game,
                    attacker,
                    target) >
                    12.001f)
            {
                if (!state.benefitOfCover)
                {
                    state.benefitOfCover =
                        true;

                    if (!state.ignoresCover)
                        state.skillModifier += 1;
                }

                state.notes.Add(
                    "Shadow Masters: Benefit of Cover beyond 12 inches"
                );
            }

            if (controller.HasDetachment(
                    "LIBRARIUS CONCLAVE") &&
                attacker.HasKeyword(
                    "PSYKER") &&
                string.Equals(
                    controller.PsychicDiscipline,
                    "TELEPATHY DISCIPLINE",
                    StringComparison.OrdinalIgnoreCase))
            {
                state.skillModifier = 0;
                state.hitRollModifier = 0;
                state.notes.Add(
                    "Telepathy Discipline: ignore BS/WS/Hit modifiers"
                );
            }
        }
    }

    public static bool GrantsCoreAbility(
        SquadController unit,
        string ruleName)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(
                ruleName))
        {
            return false;
        }

        string wanted =
            WeaponRuleParser.NormalizeRuleName(
                ruleName);

        if (wanted ==
            WeaponRuleParser.NormalizeRuleName(
                "deep strike"))
        {
            return unit.TemporaryDeepStrike;
        }

        if (wanted ==
            WeaponRuleParser.NormalizeRuleName(
                "assault"))
        {
            return
                GrantsAssault(
                    unit,
                    null,
                    AttackMode.Ranged);
        }

        if (wanted ==
            WeaponRuleParser.NormalizeRuleName(
                "heavy"))
        {
            return
                HasDetachment(
                    unit,
                    "ANVIL SIEGE FORCE");
        }

        return false;
    }

    public static int
        ConditionalFeelNoPain(
            SquadController unit,
            string label,
            int existing)
    {
        // Army/detachment FNPs in these three faction packs are mainly
        // enhancement-specific. Bearer mapping comes from roster manifests,
        // not unit text, so ambiguous enhancement FNPs deliberately remain in
        // the exact-rule/manual layer instead of being guessed.
        return existing;
    }

    public static bool CanShootAfterAdvance(
        SquadController unit)
    {
        StandardFactionGameController c =
            ControllerFor(unit);

        if (c == null || unit == null)
            return false;

        if (c.PackId == "orks" &&
            IsOrks(unit))
        {
            if (c.HasDetachment(
                    "KULT OF SPEED") &&
                unit.HasKeyword(
                    "SPEED FREEKS"))
            {
                return true;
            }

            if (c.HasDetachment(
                    "MORE DAKKA!") &&
                unit.HasKeyword(
                    "INFANTRY"))
            {
                return true;
            }
        }

        if (c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                unit))
        {
            if (c.HasDetachment(
                    "FIRESTORM ASSAULT FORCE"))
            {
                return true;
            }

            if (c.HasDetachment(
                    "GLADIUS TASK FORCE") &&
                string.Equals(
                    c.CombatDoctrine,
                    "DEVASTATOR DOCTRINE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (c.HasDetachment(
                    "BASTION TASK FORCE") &&
                unit.HasKeyword(
                    "BATTLELINE"))
            {
                return true;
            }
        }

        return false;
    }

    public static bool CanShootAfterFallBack(
        SquadController unit)
    {
        StandardFactionGameController c =
            ControllerFor(unit);

        if (c == null || unit == null)
            return false;

        if (c.PackId == "orks" &&
            c.HasDetachment(
                "KULT OF SPEED") &&
            unit.HasKeyword(
                "SPEED FREEKS"))
        {
            return true;
        }

        if (c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                unit))
        {
            if (c.HasDetachment(
                    "GLADIUS TASK FORCE") &&
                string.Equals(
                    c.CombatDoctrine,
                    "TACTICAL DOCTRINE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (c.HasDetachment(
                    "BASTION TASK FORCE") &&
                unit.HasKeyword(
                    "BATTLELINE"))
            {
                return true;
            }
        }

        return false;
    }

    public static bool CanChargeAfterAdvance(
        SquadController unit)
    {
        StandardFactionGameController c =
            ControllerFor(unit);

        if (c == null || unit == null)
            return false;

        if (c.PackId == "orks" &&
            IsOrks(unit))
        {
            if (c.UnitBenefitsFromWaaagh(
                    unit))
            {
                return true;
            }

            if (c.HasDetachment(
                    "KULT OF SPEED") &&
                unit.HasKeyword(
                    "SPEED FREEKS"))
            {
                return true;
            }
        }

        if (c.PackId ==
            "tyranids" &&
            IsTyranids(unit) &&
            c.HasDetachment(
                "VANGUARD ONSLAUGHT") &&
            unit.HasKeyword(
                "VANGUARD INVADER"))
        {
            return true;
        }

        if (c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                unit))
        {
            if (c.HasDetachment(
                    "STORMLANCE TASK FORCE"))
            {
                return true;
            }

            if (c.HasDetachment(
                    "GLADIUS TASK FORCE") &&
                string.Equals(
                    c.CombatDoctrine,
                    "ASSAULT DOCTRINE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (c.HasDetachment(
                    "BASTION TASK FORCE") &&
                unit.HasKeyword(
                    "BATTLELINE"))
            {
                return true;
            }
        }

        return false;
    }

    public static bool CanChargeAfterFallBack(
        SquadController unit)
    {
        StandardFactionGameController c =
            ControllerFor(unit);

        if (c == null || unit == null)
            return false;

        if (c.PackId == "orks" &&
            IsOrks(unit) &&
            c.HasDetachment(
                "KULT OF SPEED") &&
            unit.HasKeyword(
                "SPEED FREEKS"))
        {
            return true;
        }

        if (c.PackId == "tyranids" &&
            IsTyranids(unit) &&
            c.HasDetachment(
                "VANGUARD ONSLAUGHT"))
        {
            return true;
        }

        if (c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                unit))
        {
            if (c.HasDetachment(
                    "STORMLANCE TASK FORCE"))
            {
                return true;
            }

            if (c.HasDetachment(
                    "GLADIUS TASK FORCE") &&
                string.Equals(
                    c.CombatDoctrine,
                    "TACTICAL DOCTRINE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (c.HasDetachment(
                    "BASTION TASK FORCE") &&
                unit.HasKeyword(
                    "BATTLELINE"))
            {
                return true;
            }
        }

        return false;
    }

    public static bool GrantsAssault(
        SquadController unit,
        WeaponData weapon,
        AttackMode mode)
    {
        if (unit == null ||
            mode != AttackMode.Ranged)
        {
            return false;
        }

        StandardFactionGameController c =
            ControllerFor(unit);

        if (c == null)
            return false;

        if (c.PackId == "orks" &&
            IsOrks(unit) &&
            c.HasDetachment(
                "MORE DAKKA!") &&
            unit.HasKeyword(
                "INFANTRY"))
        {
            return true;
        }

        if (c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                unit) &&
            c.HasDetachment(
                "FIRESTORM ASSAULT FORCE"))
        {
            return true;
        }

        return false;
    }

    public static bool GrantsHeavy(
        SquadController unit,
        WeaponData weapon,
        AttackMode mode)
    {
        return
            unit != null &&
            IsAdeptusAstartes(
                unit) &&
            mode == AttackMode.Ranged &&
            HasDetachment(
                unit,
                "ANVIL SIEGE FORCE");
    }

    public static int AdditionalAttacks(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        if (c == null ||
            attacker == null ||
            mode != AttackMode.Melee)
        {
            return 0;
        }

        return
            c.PackId == "orks" &&
            IsOrks(attacker) &&
            c.UnitBenefitsFromWaaagh(
                attacker)
            ? 1
            : 0;
    }

    public static int StrengthModifier(
        GameController game,
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        int modifier = 0;

        if (c != null &&
            c.PackId == "orks" &&
            IsOrks(attacker) &&
            mode == AttackMode.Melee &&
            c.UnitBenefitsFromWaaagh(
                attacker))
        {
            modifier += 1;
        }

        if (c != null &&
            c.PackId == "tyranids" &&
            IsTyranids(attacker) &&
            mode == AttackMode.Melee &&
            IsWithinSynapse(
                game,
                attacker))
        {
            modifier += 1;
        }

        if (c != null &&
            c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                attacker) &&
            c.HasDetachment(
                "FIRESTORM ASSAULT FORCE") &&
            mode ==
                AttackMode.Ranged &&
            StandardDistance(
                game,
                attacker,
                target) <=
                12.001f)
        {
            modifier += 1;
        }

        StandardFactionGameController targetC =
            ControllerFor(target);

        if (targetC != null &&
            targetC.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                target) &&
            targetC.HasDetachment(
                "LIBRARIUS CONCLAVE") &&
            target != null &&
            target.HasKeyword(
                "PSYKER") &&
            string.Equals(
                targetC.PsychicDiscipline,
                "TELEKINESIS DISCIPLINE",
                StringComparison.OrdinalIgnoreCase) &&
            mode ==
                AttackMode.Ranged)
        {
            modifier -= 1;
        }

        return modifier;
    }

    public static int ApModifier(
        GameController game,
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        if (c == null)
            return 0;

        int modifier = 0;

        if (c.PackId == "orks" &&
            IsOrks(attacker) &&
            c.HasDetachment(
                "DA BIG HUNT") &&
            c.PreyTarget != null &&
            target != null &&
            target.JoinedActionController() ==
                c.PreyTarget
                    .JoinedActionController() &&
            attacker.HasKeyword(
                "BEAST SNAGGA"))
        {
            modifier -= 1;
        }

        if (c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                attacker) &&
            c.HasDetachment(
                "LIBRARIUS CONCLAVE") &&
            attacker.HasKeyword(
                "PSYKER") &&
            string.Equals(
                c.PsychicDiscipline,
                "PYROMANCY DISCIPLINE",
                StringComparison.OrdinalIgnoreCase) &&
            mode ==
                AttackMode.Ranged &&
            StandardDistance(
                game,
                attacker,
                target) <=
                12.001f)
        {
            modifier -= 1;
        }

        return modifier;
    }

    public static int MinimumSustainedHits(
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        if (c == null)
            return 0;

        if (c.PackId == "orks" &&
            IsOrks(attacker))
        {
            if (c.HasDetachment(
                    "WAR HORDE") &&
                mode ==
                    AttackMode.Melee)
            {
                return 1;
            }

            if (c.HasDetachment(
                    "MORE DAKKA!") &&
                mode ==
                    AttackMode.Ranged &&
                attacker.HasKeyword(
                    "INFANTRY") &&
                c.WaaaghActive)
            {
                return 1;
            }

            if (c.HasDetachment(
                    "FREEBOOTER KREW") &&
                c.LootObjective != null &&
                (attacker.HasKeyword("INFANTRY") ||
                 attacker.HasKeyword("MOUNTED") ||
                 attacker.HasKeyword("WALKER")) &&
                (c.LootObjective
                    .UnitWithinRange(
                        attacker
                            .JoinedActionController()) ||
                 (target != null &&
                  c.LootObjective
                    .UnitWithinRange(
                        target
                            .JoinedActionController()))))
            {
                return 1;
            }
        }

        if (c.PackId ==
                "tyranids" &&
            IsTyranids(attacker) &&
            c.HasDetachment(
                "INVASION FLEET") &&
            string.Equals(
                c.HyperAdaptation,
                "SWARMING INSTINCTS",
                StringComparison.OrdinalIgnoreCase) &&
            target != null &&
            (target.HasKeyword(
                 "INFANTRY") ||
             target.HasKeyword(
                 "SWARM")))
        {
            return 1;
        }

        return 0;
    }

    public static bool GrantsLethalHits(
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        return
            c != null &&
            c.PackId ==
                "tyranids" &&
            IsTyranids(attacker) &&
            c.HasDetachment(
                "INVASION FLEET") &&
            string.Equals(
                c.HyperAdaptation,
                "HYPER-AGGRESSION",
                StringComparison.OrdinalIgnoreCase) &&
            target != null &&
            (target.HasKeyword(
                 "MONSTER") ||
             target.HasKeyword(
                 "VEHICLE"));
    }

    public static bool GrantsDevastatingWounds(
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        return false;
    }

    public static bool GrantsPrecision(
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        // Hive Predators grants Precision only to attacks that scored a
        // Critical Hit. Current combat state stores Precision per attack pool,
        // not per individual hit, so Warboard deliberately leaves this exact
        // interaction in the choice/manual layer instead of over-applying it.
        return false;
    }

    public static bool RerollHitOnes(
        GameController game,
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        if (c == null)
            return false;

        if (c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                attacker))
        {
            if (c.HasDetachment(
                    "LIBRARIUS CONCLAVE") &&
                attacker.HasKeyword(
                    "PSYKER") &&
                string.Equals(
                    c.PsychicDiscipline,
                    "DIVINATION DISCIPLINE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (c.HasDetachment(
                    "CERAMITE SENTINELS") &&
                game != null &&
                game.StandardUnitWithinTerrain(
                    attacker))
            {
                return true;
            }

            if (c.HasDetachment(
                    "VENGEFUL HOSTS") &&
                attacker.HasKeyword(
                    "FLY") &&
                attacker.HasKeyword(
                    "INFANTRY") &&
                attacker.MadeChargeMove)
            {
                return true;
            }
        }

        if (c.PackId ==
            "tyranids" &&
            IsTyranids(attacker))
        {
            if (c.HasDetachment(
                    "SUBTERRANEAN ASSAULT"))
            {
                return true;
            }

            if (c.HasDetachment(
                    "AMBUSH PREDATORS") &&
                (NameContains(
                    attacker,
                    "LICTOR") ||
                 NameContains(
                    attacker,
                    "NEUROLICTOR")) &&
                target != null &&
                target.HasKeyword(
                    "CHARACTER"))
            {
                return true;
            }
        }

        return false;
    }

    public static bool RerollAllHits(
        GameController game,
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        return
            c != null &&
            c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                attacker) &&
            c.OathTarget != null &&
            target != null &&
            target.JoinedActionController() ==
                c.OathTarget
                    .JoinedActionController();
    }

    public static bool RerollWoundOnes(
        GameController game,
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        if (c == null)
            return false;

        if (c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(attacker) &&
            c.HasDetachment(
                "LIBRARIUS CONCLAVE") &&
            attacker.HasKeyword(
                "PSYKER") &&
            string.Equals(
                c.PsychicDiscipline,
                "DIVINATION DISCIPLINE",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(attacker) &&
            c.HasDetachment(
                "CERAMITE SENTINELS") &&
            game != null &&
            game.StandardUnitWithinTerrain(
                attacker))
        {
            return true;
        }

        if (c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(attacker) &&
            c.HasDetachment(
                "ORBITAL ASSAULT FORCE") &&
            attacker.WasSetUpThisTurn)
        {
            return true;
        }

        return false;
    }

    public static bool RerollAllWounds(
        GameController game,
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        return
            c != null &&
            c.PackId ==
                "space_marines" &&
            IsAdeptusAstartes(
                attacker) &&
            c.ExtremisActive &&
            c.OathTarget != null &&
            target != null &&
            target.JoinedActionController() ==
                c.OathTarget
                    .JoinedActionController();
    }

    public static int InvulnerableOverride(
        GameController game,
        SquadController target)
    {
        StandardFactionGameController c =
            ControllerFor(target);

        if (c == null ||
            target == null)
        {
            return 0;
        }

        int result = 0;

        if (c.PackId == "orks" &&
            IsOrks(target))
        {
            if (c.UnitBenefitsFromWaaagh(
                    target))
            {
                result = 5;
            }

            if (c.HasDetachment(
                    "GREEN TIDE") &&
                NameContains(
                    target,
                    "BOYZ"))
            {
                int greenTide =
                    target.JoinedLivingModels >= 10
                    ? 5
                    : 6;

                result =
                    BestSave(
                        result,
                        greenTide);
            }
        }

        if (c.PackId ==
            "tyranids" &&
            IsTyranids(target))
        {
            if (c.HasDetachment(
                    "SYNAPTIC NEXUS") &&
                string.Equals(
                    c.SynapticImperative,
                    "SYNAPTIC AUGMENTATION",
                    StringComparison.OrdinalIgnoreCase) &&
                IsWithinSynapse(
                    game,
                    target))
            {
                result =
                    BestSave(
                        result,
                        5);
            }

            if (c.HasDetachment(
                    "WARRIOR BIOFORM ONSLAUGHT") &&
                (NameContains(
                    target,
                    "TYRANID WARRIORS") ||
                 NameContains(
                    target,
                    "TYRANID PRIME WITH LASH WHIP") ||
                 NameContains(
                    target,
                    "WINGED TYRANID PRIME")))
            {
                result =
                    BestSave(
                        result,
                        5);
            }
        }

        return result;
    }

    public static int ChargeRollModifier(
        GameController game,
        SquadController attacker,
        SquadController target)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        if (c == null)
            return 0;

        if (c.PackId ==
                "tyranids" &&
            IsTyranids(attacker) &&
            c.HasDetachment(
                "SYNAPTIC NEXUS") &&
            string.Equals(
                c.SynapticImperative,
                "SURGING VITALITY",
                StringComparison.OrdinalIgnoreCase) &&
            IsWithinSynapse(
                game,
                attacker))
        {
            return 1;
        }

        return 0;
    }

    public static int AdvanceRollModifier(
        GameController game,
        SquadController unit)
    {
        StandardFactionGameController c =
            ControllerFor(unit);

        if (c == null)
            return 0;

        if (c.PackId ==
                "tyranids" &&
            IsTyranids(unit) &&
            c.HasDetachment(
                "SYNAPTIC NEXUS") &&
            string.Equals(
                c.SynapticImperative,
                "SURGING VITALITY",
                StringComparison.OrdinalIgnoreCase) &&
            IsWithinSynapse(
                game,
                unit))
        {
            return 1;
        }

        return 0;
    }

    public static bool CanRerollCharge(
        GameController game,
        SquadController attacker,
        SquadController target)
    {
        StandardFactionGameController c =
            ControllerFor(attacker);

        if (c == null)
            return false;

        if (c.PackId == "orks" &&
            IsOrks(attacker) &&
            c.HasDetachment(
                "ROLLIN' DEFF") &&
            attacker.HasKeyword(
                "WAGON"))
        {
            return true;
        }

        if (c.PackId == "orks" &&
            IsOrks(attacker) &&
            c.HasDetachment(
                "DA BIG HUNT") &&
            attacker.HasKeyword(
                "BEAST SNAGGA") &&
            c.PreyTarget != null &&
            target != null &&
            target.JoinedActionController() ==
                c.PreyTarget
                    .JoinedActionController() &&
            StandardDistance(
                game,
                attacker,
                c.PreyTarget) <=
                12.001f)
        {
            // The source permits a broader multi-target charge so long as the
            // final move engages the Prey. Warboard's current charge solver
            // tracks one selected target, so automatic mode offers this
            // reroll only when the selected target is the Prey. Traditional
            // mode can resolve the broader source case manually.
            return true;
        }

        if (c.PackId == "orks" &&
            IsOrks(attacker) &&
            c.HasDetachment(
                "BLITZ BRIGADE") &&
            c.DisembarkedThisTurn(
                attacker))
        {
            return true;
        }

        return false;
    }

    public static int FixedAdvanceResult(
        SquadController unit)
    {
        StandardFactionGameController c =
            ControllerFor(unit);

        if (c != null &&
            c.PackId == "orks" &&
            IsOrks(unit) &&
            c.HasDetachment(
                "ROLLIN' DEFF") &&
            unit != null &&
            unit.HasKeyword(
                "WAGON"))
        {
            return 6;
        }

        if (c != null &&
            c.PackId ==
                "space_marines" &&
            unit != null &&
            IsAdeptusAstartes(
                unit) &&
            c.HasDetachment(
                "HEADHUNTER TASK FORCE") &&
            unit.HasKeyword(
                "TANK ACE"))
        {
            return 6;
        }

        return 0;
    }

    public static float MoveModifier(
        GameController game,
        SquadController unit)
    {
        StandardFactionGameController c =
            ControllerFor(unit);

        if (c != null &&
            c.PackId ==
                "space_marines" &&
            unit != null &&
            IsAdeptusAstartes(
                unit) &&
            c.HasDetachment(
                "LIBRARIUS CONCLAVE") &&
            unit.HasKeyword(
                "PSYKER") &&
            string.Equals(
                c.PsychicDiscipline,
                "BIOMANCY DISCIPLINE",
                StringComparison.OrdinalIgnoreCase))
        {
            return 2f;
        }

        return 0f;
    }

    public static float DetectionRangeModifier(
        SquadController target)
    {
        // Subversion Assets and Fulguris contain selected-target detection
        // changes. Those require target state/choice and are presented in the
        // faction-rule UI until the core visibility API gains a first-class
        // per-target detection-status component.
        return 0f;
    }

    public static bool CanAdvancePhase(
        GameController game,
        out string reason)
    {
        reason = "";

        if (game == null ||
            game.BattleRound <= 0)
        {
            return true;
        }

        // Start-of-battle-round choices must be made before either player's
        // turn continues, not merely before the owning faction's turn.
        // Check every loaded standard faction controller first.
        foreach (string factionId
            in game.FactionIds)
        {
            StandardFactionGameController roundController =
                ControllerFor(factionId);

            if (roundController == null)
                continue;

            if (roundController.HyperAdaptationRequired)
            {
                reason =
                    "Select the Invasion Fleet Hyper-adaptation before continuing the battle round.";
                return false;
            }

            if (roundController.PsychicDisciplineRequired)
            {
                reason =
                    "Select the Librarius Conclave Psychic Discipline before continuing the battle round.";
                return false;
            }
        }

        StandardFactionGameController c =
            ControllerFor(
                game.ActiveFactionId
            );

        if (c == null)
            return true;

        if (game.CurrentPhase ==
                GameController.Phase.Command)
        {
            if (c.OathSelectionRequired)
            {
                reason =
                    "Select the Space Marines Oath of Moment target before leaving the Command phase.";
                return false;
            }

            if (c.PreySelectionRequired)
            {
                reason =
                    "Select the Da Big Hunt Prey before leaving the Orks Command phase.";
                return false;
            }

            if (c.LootSelectionRequired)
            {
                reason =
                    "Select the Freebooter Krew loot objective before leaving the Orks Command phase.";
                return false;
            }
        }

        return true;
    }

    public static int BattleShockDice(
        GameController game,
        SquadController unit)
    {
        StandardFactionGameController c =
            ControllerFor(unit);

        if (c != null &&
            c.PackId == "tyranids" &&
            IsTyranids(unit) &&
            IsWithinSynapse(
                game,
                unit))
        {
            return 3;
        }

        return 2;
    }

    public static bool IsWithinSynapse(
        GameController game,
        SquadController unit)
    {
        if (game == null ||
            unit == null)
        {
            return false;
        }

        StandardFactionGameController c =
            ControllerFor(unit);

        if (c == null ||
            c.PackId !=
                "tyranids" ||
            !IsTyranids(unit))
        {
            return false;
        }

        return game.AllSquads
            .Where(
                other =>
                    other != null &&
                    other.IsAlive &&
                    other.IsOnBattlefield &&
                    !other.IsAttachedLeader &&
                    string.Equals(
                        other.FactionId,
                        unit.FactionId,
                        StringComparison.OrdinalIgnoreCase) &&
                    IsTyranids(other) &&
                    other.HasKeyword(
                        "SYNAPSE"))
            .Any(
                synapse =>
                    game.StandardDistance(
                        synapse,
                        unit) <=
                    6.001f
            );
    }

    private static float StandardDistance(
        GameController game,
        SquadController first,
        SquadController second)
    {
        if (game == null)
            return float.MaxValue;

        return game.StandardDistance(
            first,
            second
        );
    }

    private static bool IsOrks(
        SquadController unit)
    {
        return
            unit != null &&
            unit.HasIntrinsicKeyword(
                "ORKS");
    }

    private static bool IsTyranids(
        SquadController unit)
    {
        return
            unit != null &&
            unit.HasIntrinsicKeyword(
                "TYRANIDS");
    }

    public static void FinalizeAttackState(
        SquadController attacker,
        UniversalAttackRuleState state)
    {
        if (attacker == null ||
            state == null)
        {
            return;
        }

        StandardFactionGameController c =
            ControllerFor(attacker);

        if (c != null &&
            c.PackId == "space_marines" &&
            IsAdeptusAstartes(attacker) &&
            c.HasDetachment(
                "LIBRARIUS CONCLAVE") &&
            attacker.HasKeyword(
                "PSYKER") &&
            string.Equals(
                c.PsychicDiscipline,
                "TELEPATHY DISCIPLINE",
                StringComparison.OrdinalIgnoreCase))
        {
            // Telepathy says this unit can ignore modifiers to BS, WS and
            // Hit rolls. Run this after every attack-hook provider so a later
            // defensive modifier cannot reintroduce one.
            state.skillModifier = 0;
            state.hitRollModifier = 0;
        }
    }

    private static bool IsAdeptusAstartes(
        SquadController unit)
    {
        return
            unit != null &&
            unit.HasIntrinsicKeyword(
                "ADEPTUS ASTARTES");
    }

    private static bool NameContains(
        SquadController unit,
        string text)
    {
        return
            unit != null &&
            !string.IsNullOrWhiteSpace(
                unit.DisplayName) &&
            unit.DisplayName.IndexOf(
                text,
                StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int BestSave(
        int first,
        int second)
    {
        if (first <= 0)
            return second;

        if (second <= 0)
            return first;

        return Mathf.Min(
            first,
            second
        );
    }
}
