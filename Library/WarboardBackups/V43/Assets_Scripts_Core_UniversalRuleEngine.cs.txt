using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class UniversalAttackRuleState
{
    public int skillModifier;
    public int hitRollModifier;
    public int woundRollModifier;

    public bool benefitOfCover;
    public bool ignoresCover;
    public bool indirect;
    public bool cannotRerollHits;

    // 0 means no special unmodified minimum.
    public int minimumUnmodifiedHit;

    public readonly List<string> notes =
        new List<string>();

    public string Summary()
    {
        return string.Join(
            " | ",
            notes.ToArray()
        );
    }
}

public interface IUniversalAttackRuleHook
{
    string Id { get; }

    void Apply(
        GameController game,
        SquadController attacker,
        SquadController target,
        ModelToken shooter,
        WeaponData weapon,
        AttackMode mode,
        UniversalAttackRuleState state);
}

public static class UniversalRuleRegistry
{
    private static readonly
        List<IUniversalAttackRuleHook> Hooks =
            new List<IUniversalAttackRuleHook>();

    static UniversalRuleRegistry()
    {
        Register(new HeavyRuleHook());
        Register(new CoverRuleHook());
        Register(new IndirectFireRuleHook());
        Register(new LanceRuleHook());
        Register(new BigGunsRuleHook());
    }

    public static void Register(
        IUniversalAttackRuleHook hook)
    {
        if (hook == null)
            return;

        Hooks.RemoveAll(
            existing =>
                string.Equals(
                    existing.Id,
                    hook.Id,
                    StringComparison.OrdinalIgnoreCase
                )
        );

        Hooks.Add(hook);
    }

    public static UniversalAttackRuleState
        BuildAttackState(
            GameController game,
            SquadController attacker,
            SquadController target,
            ModelToken shooter,
            WeaponData weapon,
            AttackMode mode)
    {
        UniversalAttackRuleState state =
            new UniversalAttackRuleState();

        foreach (IUniversalAttackRuleHook hook
            in Hooks)
        {
            hook.Apply(
                game,
                attacker,
                target,
                shooter,
                weapon,
                mode,
                state
            );
        }

        if (game != null)
        {
            game.ApplyFactionAttackRules(
                attacker,
                target,
                weapon,
                mode,
                state
            );
        }

        if (attacker != null &&
            attacker
                .JoinedActionController()
                .SnapShootingActive)
        {
            state.minimumUnmodifiedHit = 6;
            state.cannotRerollHits = true;
            state.notes.Add(
                "Snap Shooting: only unmodified 6s hit; no Hit re-rolls"
            );
        }

        state.hitRollModifier =
            Mathf.Clamp(
                state.hitRollModifier,
                -1,
                1
            );

        state.woundRollModifier =
            Mathf.Clamp(
                state.woundRollModifier,
                -1,
                1
            );

        return state;
    }

    public static bool UnitHasRule(
        SquadController squad,
        string ruleName)
    {
        if (squad == null ||
            string.IsNullOrWhiteSpace(
                ruleName))
        {
            return false;
        }

        if (AeldariFactionPack11.GrantsCoreAbility(
                squad, ruleName))
        {
            return true;
        }

        UnitData data =
            squad.SourceData;

        if (data == null)
            return false;

        string wanted =
            WeaponRuleParser.NormalizeRuleName(
                ruleName
            );

        IEnumerable<string> names =
            (data.abilities ??
                new string[0])
            .Concat(
                (data.datasheetRules ??
                    new DatasheetRuleData[0])
                .Where(rule => rule != null)
                .Select(rule => rule.name)
            )
            .Concat(
                data.keywords ??
                    new string[0]
            );

        foreach (string name in names)
        {
            string normalized =
                WeaponRuleParser.NormalizeRuleName(
                    name
                );

            if (normalized == wanted ||
                normalized.StartsWith(
                    wanted + "_",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return true;
            }
        }

        return UnitRuleText(
            squad
        ).IndexOf(
            ruleName,
            StringComparison.OrdinalIgnoreCase
        ) >= 0;
    }

    public static int GetFeelNoPain(
        SquadController squad)
    {
        string text =
            UnitRuleText(squad);

        Match match =
            Regex.Match(
                text,
                @"feel\s*no\s*pain\s*(\d)\+",
                RegexOptions.IgnoreCase
            );

        int value;

        if (match.Success &&
            int.TryParse(
                match.Groups[1].Value,
                out value))
        {
            return Mathf.Clamp(
                value,
                2,
                6
            );
        }

        return 0;
    }

    public static string GetDeadlyDemiseExpression(
        SquadController squad)
    {
        string text =
            UnitRuleText(squad);

        Match match =
            Regex.Match(
                text,
                @"deadly\s*demise\s+((?:\d+)?d\d+(?:[+-]\d+)?|\d+)",
                RegexOptions.IgnoreCase
            );

        return match.Success
            ? match.Groups[1].Value
            : "";
    }

    public static string UnitRuleText(
        SquadController squad)
    {
        if (squad == null ||
            squad.SourceData == null)
        {
            return "";
        }

        UnitData data =
            squad.SourceData;

        List<string> pieces =
            new List<string>();

        if (data.abilities != null)
            pieces.AddRange(data.abilities);

        if (data.datasheetRules != null)
        {
            foreach (DatasheetRuleData rule
                in data.datasheetRules)
            {
                if (rule == null)
                    continue;

                pieces.Add(
                    (rule.name ?? "") +
                    " " +
                    (rule.text ?? "")
                );
            }
        }

        return string.Join(
            " ",
            pieces.ToArray()
        );
    }

    public static int ApplyFeelNoPain(
        SquadController squad,
        int incomingWounds,
        string label)
    {
        incomingWounds =
            Mathf.Max(
                0,
                incomingWounds
            );

        int fnp =
            GetFeelNoPain(squad);

        if (incomingWounds <= 0 ||
            fnp <= 0)
        {
            return incomingWounds;
        }

        DiceRollRecord record =
            DiceRoller.RollDice(
                incomingWounds,
                6,
                "Feel No Pain " +
                fnp +
                "+: " +
                label
            );

        int ignored =
            record.Results.Count(
                roll =>
                    roll >= fnp
            );

        return Mathf.Max(
            0,
            incomingWounds -
            ignored
        );
    }

    private class HeavyRuleHook :
        IUniversalAttackRuleHook
    {
        public string Id
        {
            get { return "heavy"; }
        }

        public void Apply(
            GameController game,
            SquadController attacker,
            SquadController target,
            ModelToken shooter,
            WeaponData weapon,
            AttackMode mode,
            UniversalAttackRuleState state)
        {
            if (mode != AttackMode.Ranged ||
                !WeaponRuleParser.Has(
                    weapon,
                    "heavy") ||
                game == null ||
                attacker == null)
            {
                return;
            }

            bool eligible =
                !game.IsUnitEngagedPublic(
                    attacker
                ) &&
                !attacker.WasSetUpThisTurn &&
                attacker.MaxDistanceMovedThisTurn() <=
                    3.001f;

            if (!eligible)
                return;

            state.hitRollModifier += 1;
            state.notes.Add(
                "Heavy: +1 Hit"
            );
        }
    }

    private class CoverRuleHook :
        IUniversalAttackRuleHook
    {
        public string Id
        {
            get { return "cover"; }
        }

        public void Apply(
            GameController game,
            SquadController attacker,
            SquadController target,
            ModelToken shooter,
            WeaponData weapon,
            AttackMode mode,
            UniversalAttackRuleState state)
        {
            if (mode != AttackMode.Ranged ||
                game == null ||
                target == null ||
                shooter == null)
            {
                return;
            }

            state.ignoresCover =
                WeaponRuleParser.Has(
                    weapon,
                    "ignores_cover"
                ) ||
                (attacker != null &&
                 attacker
                    .JoinedActionController()
                    .FactionSoulsightActive);

            SquadController actionTarget =
                target.JoinedActionController();

            bool stealth =
                UnitHasRule(
                    actionTarget,
                    "stealth"
                ) ||
                (actionTarget.AttachedLeader != null &&
                 UnitHasRule(
                    actionTarget.AttachedLeader,
                    "Nether-Realm Casket"
                 ));

            bool terrainCover =
                (game.TargetUnitHasCoverFromShooter(shooter, target) ||
                 game.Aeldari11HasRuneOfMistsCover(target, shooter));

            state.benefitOfCover =
                stealth ||
                terrainCover;

            if (state.benefitOfCover &&
                !state.ignoresCover)
            {
                // 11e Benefit of Cover worsens BS by 1.
                state.skillModifier += 1;

                state.notes.Add(
                    stealth
                    ? "Stealth/Cover: BS worsened by 1"
                    : "Cover: BS worsened by 1"
                );
            }
            else if (state.benefitOfCover &&
                     state.ignoresCover)
            {
                state.notes.Add(
                    "Ignores Cover"
                );
            }
        }
    }

    private class IndirectFireRuleHook :
        IUniversalAttackRuleHook
    {
        public string Id
        {
            get { return "indirect_fire"; }
        }

        public void Apply(
            GameController game,
            SquadController attacker,
            SquadController target,
            ModelToken shooter,
            WeaponData weapon,
            AttackMode mode,
            UniversalAttackRuleState state)
        {
            if (mode != AttackMode.Ranged ||
                game == null ||
                shooter == null ||
                attacker == null ||
                target == null ||
                !WeaponRuleParser.Has(
                    weapon,
                    "indirect_fire"))
            {
                return;
            }

            bool directVisible =
                game.ModelCanSeeUnit(
                    shooter,
                    target
                );

            if (directVisible)
                return;

            state.indirect = true;
            state.cannotRerollHits = true;

            bool stationary =
                !attacker.WasSetUpThisTurn &&
                attacker.MaxDistanceMovedThisTurn() <=
                    0.001f;

            bool spotted =
                game.FriendlyUnitCanSeeTarget(
                    attacker.FactionId,
                    target
                );

            state.minimumUnmodifiedHit =
                stationary &&
                spotted
                ? 4
                : 6;

            if (!state.ignoresCover)
            {
                // Indirect always grants Benefit of Cover.
                if (!state.benefitOfCover)
                {
                    state.benefitOfCover =
                        true;

                    state.skillModifier += 1;
                }
            }

            state.notes.Add(
                state.minimumUnmodifiedHit == 4
                ? "Indirect: unmodified 1-3 fail; no Hit rerolls"
                : "Indirect: unmodified 1-5 fail; no Hit rerolls"
            );
        }
    }

    private class LanceRuleHook :
        IUniversalAttackRuleHook
    {
        public string Id
        {
            get { return "lance"; }
        }

        public void Apply(
            GameController game,
            SquadController attacker,
            SquadController target,
            ModelToken shooter,
            WeaponData weapon,
            AttackMode mode,
            UniversalAttackRuleState state)
        {
            if (!WeaponRuleParser.Has(
                    weapon,
                    "lance") ||
                attacker == null ||
                !attacker
                    .JoinedActionController()
                    .MadeChargeMove)
            {
                return;
            }

            state.woundRollModifier += 1;
            state.notes.Add(
                "Lance: +1 Wound"
            );
        }
    }

    private class BigGunsRuleHook :
        IUniversalAttackRuleHook
    {
        public string Id
        {
            get { return "big_guns"; }
        }

        public void Apply(
            GameController game,
            SquadController attacker,
            SquadController target,
            ModelToken shooter,
            WeaponData weapon,
            AttackMode mode,
            UniversalAttackRuleState state)
        {
            if (mode != AttackMode.Ranged ||
                game == null ||
                attacker == null ||
                target == null)
            {
                return;
            }

            bool attackerBig =
                attacker.HasKeyword("monster") ||
                attacker.HasKeyword("vehicle");

            bool targetBig =
                target.HasKeyword("monster") ||
                target.HasKeyword("vehicle");

            bool attackerEngaged =
                game.IsUnitEngagedPublic(
                    attacker
                );

            bool targetEngaged =
                game.IsUnitEngagedPublic(
                    target
                );

            bool together =
                game.UnitsAreEngaged(
                    attacker,
                    target
                );

            bool closeQuarters =
                WeaponRuleParser.Has(
                    weapon,
                    "pistol"
                ) ||
                WeaponRuleParser.Has(
                    weapon,
                    "close_quarters"
                );

            if (attackerBig &&
                attackerEngaged &&
                !(closeQuarters &&
                  together))
            {
                state.hitRollModifier -= 1;
                state.notes.Add(
                    "Monster/Vehicle engaged: -1 Hit"
                );
            }

            if (targetBig &&
                targetEngaged &&
                !together)
            {
                state.hitRollModifier -= 1;
                state.notes.Add(
                    "Shooting engaged Monster/Vehicle: -1 Hit"
                );
            }
        }
    }
}
