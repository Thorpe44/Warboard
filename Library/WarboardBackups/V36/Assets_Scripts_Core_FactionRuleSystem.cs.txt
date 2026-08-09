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

            // v33:
            // Do not mutate Aeldari keywords here. Devoted of Ynnead is a
            // detachment choice, so AeldariGameController/AeldariRulesSystem
            // is now the authority for Servants of the Whispering God.
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
                AeldariGameController aeldari =
                    FactionControllerRuntime
                        .GetAeldari(
                            pair.Key);

                if (aeldari != null)
                {
                    aeldari.StartBattleRound(
                        round);

                    battleFocusTokens[pair.Key] =
                        aeldari.BattleFocusTokens;
                }
                else
                {
                    battleFocusTokens[pair.Key] =
                        BattleFocusTokensForBattlefield();
                }
            }

            lethalSurgeUsedThisTurn[pair.Key] = false;
        }
    }

    private static int BattleFocusTokensForBattlefield()
    {
        // 11e Aeldari Battle Focus:
        // Incursion 2, Strike Force 4, Onslaught 6.
        //
        // Warboard already selects battlefield dimensions before army
        // loading. 44x30 / 44x60 / 44x90 are the standard footprints, so
        // depth gives us a backwards-compatible size signal without adding
        // another GameController dependency in this migration release.
        float battlefieldDepth =
            GameController.BoardDepth;

        if (battlefieldDepth <= 30.01f)
            return 2;

        if (battlefieldDepth <= 60.01f)
            return 4;

        return 6;
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
        AeldariGameController aeldari =
            FactionControllerRuntime
                .GetAeldari(
                    faction);

        if (aeldari != null &&
            aeldari.UsesBattleFocus())
        {
            return aeldari
                .BattleFocusTokens;
        }

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

        AeldariGameController aeldari =
            FactionControllerRuntime
                .GetAeldari(
                    faction);

        if (aeldari != null &&
            aeldari.UsesBattleFocus())
        {
            // Compatibility only. v36's real Agile Manoeuvre path passes the
            // manoeuvre name directly to AeldariGameController.
            return aeldari
                .SpendBattleFocus(
                    amount);
        }

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

        // v33:
        // Native Battle Focus remains generic. Spirit Guides is explicitly a
        // Spirit Conclave detachment rule and is handled by
        // AeldariRulesSystem.GrantsBattleFocusFromSpiritGuides().
        return UnitOrLeaderHasRule(
            unit,
            "Battle Focus");
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

        // Psychic Guidance is a datasheet ability rather than the
        // Spirit Conclave detachment's Spirit Guides aura, so it remains
        // here for compatibility.
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
        AeldariGameController aeldari =
            FactionControllerRuntime
                .GetAeldari(
                    faction);

        if (aeldari != null)
        {
            return aeldari
                .UsesDevotedOfYnnead();
        }

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

        bool aeldari =
            army.Any(
                squad =>
                    squad != null &&
                    (squad.HasKeyword(
                         "aeldari") ||
                     squad.HasKeyword(
                         "asuryani") ||
                     squad.HasKeyword(
                         "ynnari") ||
                     squad.HasKeyword(
                         "harlequins") ||
                     squad.HasKeyword(
                         "anhrathe"))
            );

        bool battleFocus =
            army.Any(
                squad =>
                    UniversalRuleRegistry.UnitHasRule(
                        squad,
                        "Battle Focus") ||
                    squad.HasKeyword(
                        "asuryani")
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

        if (aeldari)
        {
            return new FactionRuleProfile
            {
                GameFactionId = faction,
                ArmyName = "Aeldari",
                ArmyRuleName =
                    "Battle Focus",
                DetachmentName =
                    "Faction controller",
                IsYnnari = false,
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

}
