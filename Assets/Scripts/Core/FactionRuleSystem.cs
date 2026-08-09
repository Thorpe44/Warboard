using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FactionRuleProfile
{
    public string GameFactionId;
    public string ArmyName;
    public string ArmyRuleName;
    public string DetachmentName;

    public bool IsNecrons;
    public bool IsYnnari;
    public bool IsCustodes;
    public bool UsesBattleFocus;
}

public class FactionRuleSystem
{
    private readonly GameController game;

    private readonly Dictionary<string, FactionRuleProfile> profiles =
        new Dictionary<string, FactionRuleProfile>(
            StringComparer.OrdinalIgnoreCase
        );

    private readonly Dictionary<string, int> battleFocusTokens =
        new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase
        );

    private readonly Dictionary<string, bool> lethalSurgeUsedThisTurn =
        new Dictionary<string, bool>(
            StringComparer.OrdinalIgnoreCase
        );

    public FactionRuleSystem(GameController gameValue)
    {
        game = gameValue;
    }

    public void Configure(
        List<SquadController> squads,
        List<string> factions)
    {
        profiles.Clear();
        battleFocusTokens.Clear();
        lethalSurgeUsedThisTurn.Clear();

        foreach (string faction in factions)
        {
            List<SquadController> army =
                squads
                    .Where(
                        squad =>
                            squad != null &&
                            string.Equals(
                                squad.FactionId,
                                faction,
                                StringComparison.OrdinalIgnoreCase
                            )
                    )
                    .ToList();

            FactionRuleProfile profile =
                DetectProfile(
                    faction,
                    army
                );

            profiles[faction] = profile;
            battleFocusTokens[faction] = 0;
            lethalSurgeUsedThisTurn[faction] = false;

            if (profile.IsYnnari)
            {
                ApplyServantsOfTheWhisperingGod(
                    army
                );
            }
        }
    }

    public FactionRuleProfile GetProfile(string faction)
    {
        FactionRuleProfile profile;

        return profiles.TryGetValue(
            faction,
            out profile)
            ? profile
            : null;
    }

    public string RuleSummary(string faction)
    {
        FactionRuleProfile profile =
            GetProfile(faction);

        if (profile == null)
            return "Faction rules: generic";

        string text =
            profile.ArmyName +
            " | " +
            profile.ArmyRuleName;

        if (!string.IsNullOrWhiteSpace(
            profile.DetachmentName))
        {
            text +=
                " | " +
                profile.DetachmentName;
        }

        if (profile.UsesBattleFocus)
        {
            text +=
                " | Battle Focus " +
                GetBattleFocusTokens(faction);
        }

        return text;
    }

    public void StartBattleRound(
        int round,
        List<SquadController> squads)
    {
        foreach (KeyValuePair<string, FactionRuleProfile> pair
            in profiles)
        {
            if (pair.Value.UsesBattleFocus)
            {
                // Warboard's current board/mission is Strike Force scale.
                battleFocusTokens[pair.Key] = 4;
            }

            lethalSurgeUsedThisTurn[pair.Key] = false;
        }
    }

    public void StartTurn(string faction)
    {
        List<string> keys =
            lethalSurgeUsedThisTurn.Keys
                .ToList();

        foreach (string key in keys)
            lethalSurgeUsedThisTurn[key] = false;
    }

    public int GetBattleFocusTokens(string faction)
    {
        int value;

        return battleFocusTokens.TryGetValue(
            faction,
            out value)
            ? value
            : 0;
    }

    public bool SpendBattleFocus(
        string faction,
        int amount = 1)
    {
        if (amount <= 0)
            return true;

        int current =
            GetBattleFocusTokens(
                faction
            );

        if (current < amount)
            return false;

        battleFocusTokens[faction] =
            current - amount;

        return true;
    }

    public bool CanUseLethalSurge(string faction)
    {
        bool used;

        return
            IsYnnari(faction) &&
            (!lethalSurgeUsedThisTurn.TryGetValue(
                 faction,
                 out used) ||
             !used);
    }

    public void MarkLethalSurgeUsed(string faction)
    {
        lethalSurgeUsedThisTurn[faction] = true;
    }

    public bool UnitHasBattleFocus(
        SquadController squad,
        List<SquadController> allSquads)
    {
        if (squad == null ||
            !squad.IsOnBattlefield ||
            !squad.IsAlive)
        {
            return false;
        }

        SquadController unit =
            squad.JoinedActionController();

        if (UnitOrLeaderHasRule(
            unit,
            "Battle Focus"))
        {
            return true;
        }

        if (!unit.HasKeyword(
            "wraith construct"))
        {
            return false;
        }

        return allSquads.Any(
            psyker =>
                psyker != null &&
                psyker.IsAlive &&
                psyker.IsOnBattlefield &&
                psyker.FactionId ==
                    unit.FactionId &&
                psyker.HasKeyword("aeldari") &&
                psyker.HasKeyword("psyker") &&
                game.JoinedDistancePublic(
                    psyker,
                    unit
                ) <= 12.001f
        );
    }

    public string EndCommandPhase(
        string faction,
        List<SquadController> squads)
    {
        FactionRuleProfile profile =
            GetProfile(faction);

        if (profile == null ||
            !profile.IsNecrons)
        {
            return "";
        }

        List<string> results =
            new List<string>();

        foreach (SquadController squad in squads)
        {
            if (squad == null ||
                squad.IsAttachedLeader ||
                !squad.IsOnBattlefield ||
                !squad.IsAlive ||
                squad.LivingModels <= 0 ||
                !string.Equals(
                    squad.FactionId,
                    faction,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!UnitOrLeaderHasRule(
                squad,
                "Reanimation Protocols"))
            {
                continue;
            }

            if (!squad.HasAnyLostWoundsOrModels())
                continue;

            int reanimation =
                game.RollTabletopD3(
                    "Reanimation Protocols: " +
                    squad.DisplayName
                );

            int restored =
                game.ReanimateUnit(
                    squad,
                    reanimation
                );

            results.Add(
                squad.DisplayName +
                " D3=" +
                reanimation +
                ", restored " +
                restored
            );
        }

        if (results.Count == 0)
        {
            return
                "Reanimation Protocols: no eligible damaged units.";
        }

        return
            "Reanimation Protocols — " +
            string.Join(
                " | ",
                results.ToArray()
            );
    }

    public void ApplyAttackModifiers(
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode,
        UniversalAttackRuleState state)
    {
        if (attacker == null ||
            state == null)
        {
            return;
        }

        SquadController actionAttacker =
            attacker.JoinedActionController();

        FactionRuleProfile profile =
            GetProfile(
                actionAttacker.FactionId
            );

        if (profile != null &&
            profile.IsNecrons &&
            actionAttacker.AttachedLeader != null &&
            actionAttacker.AttachedLeader.IsAlive &&
            actionAttacker.AttachedLeader.HasKeyword(
                "necrons") &&
            actionAttacker.AttachedLeader.HasKeyword(
                "character"))
        {
            state.hitRollModifier += 1;
            state.notes.Add(
                "Command Protocols: +1 Hit"
            );
        }

        if (profile != null &&
            profile.IsNecrons &&
            game.FriendlyEnhancementAuraWithin(
                actionAttacker,
                "Phasal Subjugator",
                6f,
                true))
        {
            state.hitRollModifier += 1;
            state.notes.Add(
                "Phasal Subjugator: +1 Hit"
            );
        }

        if (actionAttacker.HasKeyword(
                "wraith construct") &&
            game.FriendlyKeywordWithin(
                actionAttacker,
                "aeldari",
                "psyker",
                12f))
        {
            state.hitRollModifier += 1;
            state.notes.Add(
                "Psychic Guidance: +1 Hit"
            );
        }

        if (target != null &&
            target
                .JoinedActionController()
                .FactionMacabreResilienceActive)
        {
            state.woundRollModifier -= 1;
            state.notes.Add(
                "Macabre Resilience: -1 Wound"
            );
        }
    }

    public bool IsYnnari(string faction)
    {
        FactionRuleProfile profile =
            GetProfile(faction);

        return profile != null &&
            profile.IsYnnari;
    }

    public bool IsNecrons(string faction)
    {
        FactionRuleProfile profile =
            GetProfile(faction);

        return profile != null &&
            profile.IsNecrons;
    }

    public bool UsesBattleFocus(string faction)
    {
        FactionRuleProfile profile =
            GetProfile(faction);

        return profile != null &&
            profile.UsesBattleFocus;
    }

    public static bool UnitOrLeaderHasRule(
        SquadController squad,
        string ruleName)
    {
        if (squad == null)
            return false;

        SquadController unit =
            squad.JoinedActionController();

        if (UniversalRuleRegistry.UnitHasRule(
                unit,
                ruleName))
        {
            return true;
        }

        return
            unit.AttachedLeader != null &&
            UniversalRuleRegistry.UnitHasRule(
                unit.AttachedLeader,
                ruleName);
    }

    private FactionRuleProfile DetectProfile(
        string faction,
        List<SquadController> army)
    {
        bool necrons =
            army.Any(
                squad =>
                    squad.HasKeyword(
                        "necrons")
            );

        bool custodes =
            army.Any(
                squad =>
                    squad.HasKeyword(
                        "adeptus custodes")
            );

        bool ynnari =
            army.Any(
                squad =>
                    squad.HasKeyword(
                        "ynnari") ||
                    squad.DisplayName.IndexOf(
                        "Ynnari",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0 ||
                    squad.DisplayName.IndexOf(
                        "Yvraine",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0 ||
                    squad.DisplayName.IndexOf(
                        "Yncarne",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
            );

        bool battleFocus =
            army.Any(
                squad =>
                    UniversalRuleRegistry.UnitHasRule(
                        squad,
                        "Battle Focus") ||
                    squad.HasKeyword("asuryani") ||
                    squad.HasKeyword("aeldari")
            );

        if (necrons)
        {
            return new FactionRuleProfile
            {
                GameFactionId = faction,
                ArmyName = "Necrons",
                ArmyRuleName =
                    "Reanimation Protocols",
                DetachmentName =
                    "Awakened Dynasty — Command Protocols",
                IsNecrons = true
            };
        }

        if (ynnari)
        {
            return new FactionRuleProfile
            {
                GameFactionId = faction,
                ArmyName = "Aeldari / Ynnari",
                ArmyRuleName =
                    "Battle Focus",
                DetachmentName =
                    "Devoted of Ynnead — Strength from Death",
                IsYnnari = true,
                UsesBattleFocus = battleFocus
            };
        }

        if (custodes)
        {
            return new FactionRuleProfile
            {
                GameFactionId = faction,
                ArmyName = "Adeptus Custodes",
                ArmyRuleName =
                    "Martial Ka'tah",
                DetachmentName =
                    "Faction framework ready",
                IsCustodes = true
            };
        }

        return new FactionRuleProfile
        {
            GameFactionId = faction,
            ArmyName = faction,
            ArmyRuleName = "Generic Core",
            DetachmentName = "",
            UsesBattleFocus = battleFocus
        };
    }

    private void ApplyServantsOfTheWhisperingGod(
        List<SquadController> army)
    {
        foreach (SquadController squad in army)
        {
            if (squad == null)
                continue;

            bool asuryani =
                squad.HasKeyword(
                    "asuryani") ||
                squad.HasKeyword(
                    "aeldari");

            bool epicHero =
                squad.HasKeyword(
                    "epic hero");

            if (asuryani &&
                !epicHero)
            {
                squad.AddFactionKeyword(
                    "YNNARI"
                );
            }
        }
    }
}
