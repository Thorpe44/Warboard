using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WarboardAttackDieStage47
{
    Hit,
    Wound,
    Save,
    Damage
}

public sealed class WarboardAttackDieState47
{
    public int Sequence;
    public WarboardAttackDieStage47 Stage;
    public SquadController Attacker;
    public SquadController Target;
    public ModelToken SourceModel;
    public WeaponData Weapon;
    public AttackMode Mode;
    public int Roll;
    public bool Success;
    public bool Critical;
    public bool Rerolled;
    public bool Precision;
    public bool Lethal;
    public bool Devastating;
    public int SustainedExtraHits;
}

/// <summary>
/// Keeps per-die provenance instead of collapsing every special rule into one
/// volley-wide bool. Faction/reaction code can now distinguish ordinary hits,
/// Critical Hits, Critical Wounds and rerolled dice.
/// </summary>
public static class WarboardAttackDieLedger47
{
    private static readonly List<WarboardAttackDieState47> recent =
        new List<WarboardAttackDieState47>();

    private static int nextSequence;

    public static IReadOnlyList<WarboardAttackDieState47> Recent
    {
        get { return recent.ToArray(); }
    }

    public static WarboardAttackDieState47 RecordHit(
        GameController game,
        SquadController attacker,
        SquadController target,
        ModelToken sourceModel,
        WeaponData weapon,
        AttackMode mode,
        int roll,
        bool success,
        bool critical,
        bool rerolled,
        bool precision,
        bool lethal,
        int sustainedExtraHits)
    {
        WarboardAttackDieState47 value =
            new WarboardAttackDieState47
            {
                Sequence = ++nextSequence,
                Stage = WarboardAttackDieStage47.Hit,
                Attacker = Normalize(attacker),
                Target = Normalize(target),
                SourceModel = sourceModel,
                Weapon = weapon,
                Mode = mode,
                Roll = roll,
                Success = success,
                Critical = critical,
                Rerolled = rerolled,
                Precision = precision,
                Lethal = lethal,
                SustainedExtraHits =
                    Mathf.Max(0, sustainedExtraHits)
            };

        Add(value);
        return value;
    }

    public static WarboardAttackDieState47 RecordWound(
        GameController game,
        SquadController attacker,
        SquadController target,
        ModelToken sourceModel,
        WeaponData weapon,
        AttackMode mode,
        int roll,
        bool success,
        bool critical,
        bool rerolled,
        bool precision,
        bool devastating)
    {
        WarboardAttackDieState47 value =
            new WarboardAttackDieState47
            {
                Sequence = ++nextSequence,
                Stage = WarboardAttackDieStage47.Wound,
                Attacker = Normalize(attacker),
                Target = Normalize(target),
                SourceModel = sourceModel,
                Weapon = weapon,
                Mode = mode,
                Roll = roll,
                Success = success,
                Critical = critical,
                Rerolled = rerolled,
                Precision = precision,
                Devastating = devastating
            };

        Add(value);
        return value;
    }

    public static void ClearAttackStage(
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        WarboardAttackDieStage47 stage)
    {
        SquadController source = Normalize(attacker);
        SquadController destination = Normalize(target);

        recent.RemoveAll(
            value =>
                value != null &&
                value.Attacker == source &&
                value.Target == destination &&
                value.Weapon == weapon &&
                value.Stage == stage
        );
    }

    public static void EmitStageEvents(
        GameController game,
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        WarboardAttackDieStage47 stage)
    {
        foreach (WarboardAttackDieState47 value
            in ForAttack(
                    attacker,
                    target,
                    weapon)
                .Where(item =>
                    item != null &&
                    item.Stage == stage)
                .OrderBy(item => item.Sequence)
                .ToArray())
        {
            if (stage ==
                WarboardAttackDieStage47.Hit)
            {
                WarboardRuleEventBus47.RaiseHit(
                    game,
                    attacker,
                    target,
                    value.SourceModel,
                    weapon,
                    value.Mode,
                    value.Roll,
                    value.Success,
                    value.Critical,
                    value.Rerolled
                );
            }
            else if (stage ==
                WarboardAttackDieStage47.Wound)
            {
                WarboardRuleEventBus47.RaiseWound(
                    game,
                    attacker,
                    target,
                    value.SourceModel,
                    weapon,
                    value.Mode,
                    value.Roll,
                    value.Success,
                    value.Critical,
                    value.Rerolled
                );
            }
        }
    }

    public static IEnumerable<WarboardAttackDieState47> ForAttack(
        SquadController attacker,
        SquadController target,
        WeaponData weapon)
    {
        SquadController source = Normalize(attacker);
        SquadController destination = Normalize(target);

        return recent
            .Where(
                value =>
                    value != null &&
                    value.Attacker == source &&
                    value.Target == destination &&
                    value.Weapon == weapon)
            .ToArray();
    }

    public static void Clear()
    {
        recent.Clear();
    }

    private static void Add(
        WarboardAttackDieState47 value)
    {
        recent.Add(value);

        while (recent.Count > 1024)
            recent.RemoveAt(0);
    }

    private static SquadController Normalize(
        SquadController unit)
    {
        return unit != null
            ? unit.JoinedActionController()
            : null;
    }
}

public sealed class WarboardAttackAugment47
{
    public ModelToken Shooter;
    public int AdditionalAttacks;
    public int StrengthModifier;
    public int ApModifier;
    public int HitModifier;
    public int WoundModifier;
    public bool LethalHits;
    public int SustainedHits;
    public bool DevastatingWounds;
    public bool Precision;
    public bool PrecisionOnCriticalHit;
    public bool IgnoresCover;
    public bool RerollHitOnes;
    public bool RerollAllHits;
    public bool RerollWoundOnes;
    public bool RerollAllWounds;
    public readonly List<string> Notes =
        new List<string>();
}

/// <summary>
/// Shared v47 attack modifier interpreter for explicitly assigned Enhancement
/// bearers and persistent target states. It is intentionally conservative:
/// only deterministic wording/patterns are automated; ambiguous conditional
/// prose remains visible in the source card rather than being guessed.
/// </summary>
public static class WarboardV47FactionRules
{
    private static SquadController lastAttacker;
    private static SquadController lastTarget;
    private static WeaponData lastWeapon;
    private static AttackMode lastMode;
    private static WarboardAttackAugment47 lastAugment;

    public static WarboardAttackAugment47 BuildAttackAugment(
        GameController game,
        SquadController attacker,
        SquadController target,
        ModelToken shooter,
        WeaponData weapon,
        AttackMode mode)
    {
        WarboardAttackAugment47 result =
            new WarboardAttackAugment47
            {
                Shooter = shooter
            };

        foreach (WarboardEnhancementAssignment47 assignment
            in WarboardEnhancementRegistry47
                .ApplicableToAttack(
                    attacker,
                    shooter))
        {
            ApplyEnhancementAttackRule(
                assignment,
                attacker,
                target,
                shooter,
                weapon,
                mode,
                result
            );
        }

        ApplyPersistentTargetStates(
            attacker,
            target,
            result
        );

        StandardFactionGameController controller =
            WarboardFactionExtensionHub.ControllerFor(
                attacker
            );

        if (controller != null &&
            controller.PackId == "tyranids" &&
            controller.HasDetachment(
                "INVASION FLEET") &&
            string.Equals(
                controller.HyperAdaptation,
                "HIVE PREDATORS",
                StringComparison.OrdinalIgnoreCase) &&
            target != null &&
            target.HasKeyword("CHARACTER"))
        {
            result.PrecisionOnCriticalHit = true;
            result.Notes.Add(
                "Hive Predators: Precision on Critical Hits only"
            );
        }

        lastAttacker = Normalize(attacker);
        lastTarget = Normalize(target);
        lastWeapon = weapon;
        lastMode = mode;
        lastAugment = result;

        return result;
    }

    public static WarboardAttackAugment47 CurrentAugment(
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        if (lastAugment == null ||
            lastAttacker != Normalize(attacker) ||
            lastTarget != Normalize(target) ||
            lastWeapon != weapon ||
            lastMode != mode)
        {
            return new WarboardAttackAugment47();
        }

        return lastAugment;
    }

    public static WarboardAttackAugment47 CurrentAugment(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        if (lastAugment == null ||
            lastAttacker != Normalize(attacker) ||
            lastWeapon != weapon ||
            lastMode != mode)
        {
            return new WarboardAttackAugment47();
        }

        return lastAugment;
    }

    public static bool CanAttackTarget(
        SquadController attacker,
        SquadController target,
        AttackMode mode,
        out string reason)
    {
        reason = "";

        if (attacker == null || target == null)
            return true;

        if (WarboardRuleStateStore47.HasUnitTarget(
                "HERESY_UNDONE",
                attacker.FactionId,
                attacker) &&
            !WarboardRuleStateStore47.HasUnitTarget(
                "BASTION_AUSPEX_SCANNED",
                attacker.FactionId,
                target))
        {
            reason =
                "Heresy Undone: every target of those attacks/that charge must be an auspex scanned unit.";
            return false;
        }

        return true;
    }

    public static int SaveOverride(
        SquadController unit,
        int existing)
    {
        int result = existing;

        foreach (WarboardEnhancementAssignment47 assignment
            in WarboardEnhancementRegistry47
                .ApplicableToUnit(unit))
        {
            string text =
                WarboardEnhancementRegistry47
                    .NormalizeText(
                        assignment.RuleText);

            // Explicit, unconditional bearer Save characteristic changes.
            if (text.Contains(
                    "THE BEARER HAS A SAVE CHARACTERISTIC OF 2+"))
            {
                if (assignment.Bearer == unit ||
                    assignment.Bearer ==
                        unit.JoinedActionController())
                {
                    result = Mathf.Min(result, 2);
                }
            }
        }

        return result;
    }

    public static int BattleShockedObjectiveControl(
        SquadController unit,
        ModelToken model)
    {
        if (unit == null ||
            model == null ||
            !unit.JoinedActionController()
                .IsBattleShocked)
        {
            return -1;
        }

        foreach (WarboardEnhancementAssignment47 assignment
            in WarboardEnhancementRegistry47
                .ApplicableToUnit(unit))
        {
            if (assignment == null ||
                assignment.Bearer == null ||
                !assignment.Bearer.IsAttachedLeader)
            {
                continue;
            }

            string name =
                StandardFactionPack11.Normalize(
                    assignment.EnhancementName);

            if (name !=
                StandardFactionPack11.Normalize(
                    "STOIC DEFENDER"))
            {
                continue;
            }

            return Mathf.Max(
                0,
                Mathf.CeilToInt(
                    model.ObjectiveControl / 2f));
        }

        return -1;
    }

    public static int ModifyObjectiveControl(
        SquadController unit,
        ModelToken model,
        int existing)
    {
        int result = existing;

        foreach (WarboardEnhancementAssignment47 assignment
            in WarboardEnhancementRegistry47
                .ApplicableToUnit(unit))
        {
            string name =
                StandardFactionPack11.Normalize(
                    assignment.EnhancementName);

            string text =
                WarboardEnhancementRegistry47
                    .NormalizeText(
                        assignment.RuleText);

            if (name ==
                StandardFactionPack11.Normalize(
                    "RITES OF WAR") &&
                text.Contains(
                    "IMPROVE THE OBJECTIVE CONTROL CHARACTERISTIC") &&
                model != null &&
                model.Squad ==
                    assignment.Bearer)
            {
                // The passive clause improves the bearer only. The separate
                // once-per-battle unit-wide activation is not silently assumed.
                result += 1;
            }

            if (name ==
                StandardFactionPack11.Normalize(
                    "OMINOUS PRESENCE"))
            {
                // Source wording is an additive OC improvement for the bearer.
                if (model != null &&
                    model.Squad ==
                        assignment.Bearer)
                {
                    result += 3;
                }
            }
        }

        // Bastion scanned/suppressed state is target state, not OC.
        return Mathf.Max(0, result);
    }

    public static float MoveModifier(
        SquadController unit)
    {
        float value = 0f;

        foreach (WarboardEnhancementAssignment47 assignment
            in WarboardEnhancementRegistry47
                .ApplicableToUnit(unit))
        {
            string name =
                StandardFactionPack11.Normalize(
                    assignment.EnhancementName);

            if (name ==
                StandardFactionPack11.Normalize(
                    "RELENTLESS HUNGER"))
            {
                value += 2f;
            }
        }

        if (WarboardRuleStateStore47.HasUnitFlag(
                "BASTION_PINNED",
                unit))
        {
            value -= 2f;
        }

        return value;
    }

    public static int ChargeRollModifier(
        SquadController unit)
    {
        return WarboardRuleStateStore47.HasUnitFlag(
                "BASTION_PINNED",
                unit)
            ? -2
            : 0;
    }

    public static bool GrantsKeyword(
        SquadController unit,
        string keyword)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(keyword))
        {
            return false;
        }

        string wanted =
            StandardFactionPack11.Normalize(
                keyword
            );

        foreach (WarboardEnhancementAssignment47 assignment
            in WarboardEnhancementRegistry47
                .ApplicableToUnit(unit))
        {
            if (assignment.Bearer == null)
                continue;

            string text =
                WarboardEnhancementRegistry47
                    .NormalizeText(
                        assignment.RuleText
                    );

            string bearerPrefix =
                "THE BEARER GAINS THE ";

            int index =
                text.IndexOf(
                    bearerPrefix,
                    StringComparison.OrdinalIgnoreCase
                );

            if (index < 0)
                continue;

            int start =
                index + bearerPrefix.Length;

            int end =
                text.IndexOf(
                    " KEYWORD",
                    start,
                    StringComparison.OrdinalIgnoreCase
                );

            if (end <= start)
                continue;

            string granted =
                text.Substring(
                    start,
                    end - start
                ).Trim();

            if (StandardFactionPack11.Normalize(
                    granted) == wanted)
            {
                return true;
            }
        }

        return false;
    }

    public static bool GrantsCoreAbility(
        SquadController unit,
        string ruleName)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(ruleName))
        {
            return false;
        }

        string wanted =
            StandardFactionPack11.Normalize(
                ruleName);

        foreach (WarboardEnhancementAssignment47 assignment
            in WarboardEnhancementRegistry47
                .ApplicableToUnit(unit))
        {
            string text =
                WarboardEnhancementRegistry47
                    .NormalizeText(
                        assignment.RuleText);

            bool bearerIsThisUnit =
                assignment.Bearer == unit;

            if (wanted ==
                    StandardFactionPack11.Normalize(
                        "STEALTH"))
            {
                bool unitWide =
                    text.Contains(
                        "THIS UNIT HAS STEALTH") ||
                    text.Contains(
                        "MODELS IN THE BEARER'S UNIT HAVE THE STEALTH") ||
                    text.Contains(
                        "MODELS IN THE BEARERS UNIT HAVE THE STEALTH");

                bool bearerOnly =
                    text.Contains(
                        "THE BEARER HAS THE STEALTH") ||
                    text.Contains(
                        "THE BEARER HAS STEALTH") ||
                    text.Contains(
                        "THIS MODEL HAS") &&
                        text.Contains("STEALTH");

                if (unitWide ||
                    (bearerOnly &&
                     bearerIsThisUnit))
                {
                    return true;
                }
            }

            if (wanted ==
                    StandardFactionPack11.Normalize(
                        "LONE OPERATIVE"))
            {
                bool bearerOnly =
                    text.Contains(
                        "THE BEARER HAS") ||
                    text.Contains(
                        "THIS MODEL HAS");

                if (text.Contains(
                        "LONE OPERATIVE") &&
                    (!bearerOnly ||
                     bearerIsThisUnit))
                {
                    return true;
                }
            }

            if (wanted ==
                    StandardFactionPack11.Normalize(
                        "INFILTRATORS") &&
                text.Contains(
                    "INFILTRATORS") &&
                (text.Contains("THIS MODEL HAS") ||
                 text.Contains("THE BEARER HAS")) &&
                bearerIsThisUnit)
            {
                return true;
            }
        }

        return false;
    }

    public static int ConditionalFeelNoPain(
        SquadController unit,
        string label,
        int existing)
    {
        return WarboardEnhancementRegistry47
            .ParsedFeelNoPain(
                unit,
                existing,
                label);
    }

    public static bool IsCriticalHit(
        SquadController attacker,
        int roll,
        bool success)
    {
        if (!success)
            return false;

        if (CustodesFactionPack11.IsCriticalHit(
                attacker,
                roll,
                success) ||
            NecronsFactionPack11.IsCriticalHit(
                attacker,
                roll,
                success))
        {
            return true;
        }

        return roll == 6;
    }

    private static void ApplyEnhancementAttackRule(
        WarboardEnhancementAssignment47 assignment,
        SquadController attacker,
        SquadController target,
        ModelToken shooter,
        WeaponData weapon,
        AttackMode mode,
        WarboardAttackAugment47 result)
    {
        if (assignment == null ||
            result == null)
        {
            return;
        }

        string name =
            StandardFactionPack11.Normalize(
                assignment.EnhancementName);

        string text =
            WarboardEnhancementRegistry47
                .NormalizeText(
                    assignment.RuleText);

        bool bearerModel =
            shooter != null &&
            shooter.Squad ==
                assignment.Bearer;

        bool unitContainsBearer =
            assignment.Bearer != null &&
            attacker != null &&
            assignment.Bearer
                .JoinedActionController() ==
                attacker.JoinedActionController();

        if (!unitContainsBearer)
            return;

        // Deterministic named rules from the supplied v46 faction packs.
        if (name == StandardFactionPack11.Normalize(
                "EYE OF THE PRIMARCH") &&
            mode == AttackMode.Ranged &&
            (bearerModel ||
             (shooter != null &&
              shooter.Squad != null &&
              shooter.Squad.HasKeyword(
                  "BATTLELINE"))))
        {
            result.Precision = true;
            result.Notes.Add(
                "Eye of the Primarch: Precision");
        }

        if (name == StandardFactionPack11.Normalize(
                "RAPTORIAL COGITATOR CORE") &&
            mode == AttackMode.Ranged)
        {
            result.IgnoresCover = true;
            result.Notes.Add(
                "Raptorial Cogitator Core: Ignores Cover");
        }

        if (name == StandardFactionPack11.Normalize(
                "FUSILLADE") &&
            mode == AttackMode.Ranged)
        {
            result.LethalHits = true;
            result.Notes.Add(
                "Fusillade: Lethal Hits");

            StandardFactionGameController controller =
                WarboardFactionExtensionHub.ControllerFor(
                    attacker);

            if (controller != null &&
                string.Equals(
                    controller.PsychicDiscipline,
                    "PYROMANCY DISCIPLINE",
                    StringComparison.OrdinalIgnoreCase))
            {
                result.SustainedHits =
                    Mathf.Max(
                        1,
                        result.SustainedHits);

                result.Notes.Add(
                    "Fusillade + Pyromancy: Sustained Hits 1");
            }
        }

        if (name == StandardFactionPack11.Normalize(
                "THE IMPERIUM'S SWORD") &&
            mode == AttackMode.Melee &&
            bearerModel)
        {
            result.AdditionalAttacks += 1;
            result.Notes.Add(
                "The Imperium's Sword: +1 Attack");
        }

        if (name == StandardFactionPack11.Normalize(
                "POWER OF THE HIVE MIND") &&
            bearerModel &&
            weapon != null &&
            WeaponRuleParser.Has(
                weapon,
                "psychic"))
        {
            result.StrengthModifier += 1;
            result.ApModifier -= 1;
            result.Notes.Add(
                "Power of the Hive Mind: +1 Strength, AP improved by 1");
        }

        if (name == StandardFactionPack11.Normalize(
                "CHAMELEONIC"))
        {
            // Defensive Stealth is exposed through GrantsCoreAbility.
        }

        if (name == StandardFactionPack11.Normalize(
                "DEATH IN THE DARK UPGRADE") &&
            target != null &&
            GameController.Current != null &&
            GameController.Current.StandardUnitIsHidden(
                target))
        {
            result.HitModifier += 1;
            result.Notes.Add(
                "Death in the Dark: +1 Hit against hidden target");
        }

        // Conservative generic ability parsing. Only direct, unqualified
        // grant wording is accepted here.
        bool hasConditionalPrefix =
            text.Contains(" IF ") ||
            text.Contains(" WHEN ") ||
            text.Contains(" UNTIL ") ||
            text.Contains(" AFTER ");

        if (!hasConditionalPrefix)
        {
            if (text.Contains("[LETHAL HITS]"))
                result.LethalHits = true;

            if (text.Contains("[DEVASTATING WOUNDS]"))
                result.DevastatingWounds = true;

            if (text.Contains("[PRECISION]"))
                result.Precision = true;

            if (text.Contains("[IGNORES COVER]"))
                result.IgnoresCover = true;

            if (text.Contains("[SUSTAINED HITS 1]"))
                result.SustainedHits =
                    Mathf.Max(
                        1,
                        result.SustainedHits);
        }

        // The per-die system supports future wording where only Critical Hits
        // gain Precision. This parser deliberately requires the two concepts
        // to appear in the same sentence-like clause.
        if ((text.Contains("CRITICAL HIT") ||
             text.Contains("CRITICAL HITS")) &&
            text.Contains("PRECISION"))
        {
            result.PrecisionOnCriticalHit = true;
            result.Precision = false;
            result.Notes.Add(
                "Precision applies only to Critical Hits");
        }
    }

    private static void ApplyPersistentTargetStates(
        SquadController attacker,
        SquadController target,
        WarboardAttackAugment47 result)
    {
        if (attacker == null ||
            target == null ||
            result == null)
        {
            return;
        }

        string faction = attacker.FactionId;

        if (WarboardRuleStateStore47.HasUnitTarget(
                "BASTION_AUSPEX_SCANNED",
                faction,
                target))
        {
            result.RerollHitOnes = true;
            result.Notes.Add(
                "Auspex scanned: re-roll Hit rolls of 1");
        }

        if (WarboardRuleStateStore47.HasUnitTarget(
                "CODEX_DISCIPLINE",
                faction,
                attacker))
        {
            result.RerollHitOnes = true;

            if (WarboardRuleStateStore47.HasUnitTarget(
                    "BASTION_AUSPEX_SCANNED",
                    faction,
                    target))
            {
                result.RerollWoundOnes = true;
            }
        }

        string lightChoice =
            WarboardDatasheetChoice47.ChoiceValue(
                "LIGHT_OF_VENGEANCE",
                faction,
                attacker
            );

        bool lightEligible =
            !string.IsNullOrWhiteSpace(lightChoice) &&
            (attacker.HasKeyword("BATTLELINE") ||
             WarboardRuleStateStore47.HasUnitTarget(
                "BASTION_AUSPEX_SCANNED",
                faction,
                target));

        if (lightEligible &&
            string.Equals(
                lightChoice,
                "LETHAL_HITS",
                StringComparison.OrdinalIgnoreCase))
        {
            result.LethalHits = true;
            result.Notes.Add(
                "Light of Vengeance: Lethal Hits");
        }

        if (lightEligible &&
            string.Equals(
                lightChoice,
                "SUSTAINED_HITS_1",
                StringComparison.OrdinalIgnoreCase))
        {
            result.SustainedHits =
                Mathf.Max(1, result.SustainedHits);
            result.Notes.Add(
                "Light of Vengeance: Sustained Hits 1");
        }

        if (WarboardRuleStateStore47.HasUnitFlag(
                "BASTION_SUPPRESSED",
                attacker))
        {
            result.HitModifier -= 1;
            result.Notes.Add(
                "Suppressed: -1 Hit");
        }
    }

    private static SquadController Normalize(
        SquadController unit)
    {
        return unit != null
            ? unit.JoinedActionController()
            : null;
    }
}
