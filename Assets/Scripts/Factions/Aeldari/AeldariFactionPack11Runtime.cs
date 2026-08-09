using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Runtime state for the complete 11e Aeldari faction pack.
/// The faction controller owns timing; this class owns transient rule state.
/// </summary>
public static class AeldariFactionPack11Runtime
{
    private static readonly Dictionary<SquadController, HashSet<string>> phaseFlags =
        new Dictionary<SquadController, HashSet<string>>();
    private static readonly Dictionary<SquadController, HashSet<string>> turnFlags =
        new Dictionary<SquadController, HashSet<string>>();
    private static readonly Dictionary<SquadController, HashSet<string>> commandFlags =
        new Dictionary<SquadController, HashSet<string>>();
    private static readonly Dictionary<string, List<int>> fateDice =
        new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> usedRound =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> usedTurn =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> usedBattle =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, AeldariGameController> controllers =
        new Dictionary<string, AeldariGameController>(StringComparer.OrdinalIgnoreCase);

    public static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string normalized = WeaponRuleParser.NormalizeRuleName(value);
        return normalized == null ? "" : normalized.ToLowerInvariant();
    }

    public static void Register(AeldariGameController controller)
    {
        if (controller == null || string.IsNullOrWhiteSpace(controller.FactionId)) return;
        controllers[controller.FactionId] = controller;
    }

    public static void SynchronizePersistent(AeldariGameController controller)
    {
        if (controller == null) return;
        Register(controller);
        foreach (SquadController unit in controller.ArmyUnits)
        {
            if (unit == null) continue;

            // Webway Pathstone grants Deep Strike for the battle.
            if (AeldariFactionPack11.UnitHasEnhancement(unit, "Webway Pathstone"))
                unit.TemporaryDeepStrike = true;

            // Shadowfall Masks grants Fights First. Re-applied each phase because
            // SquadController clears temporary faction effects at phase boundaries.
            if (AeldariFactionPack11.UnitHasEnhancement(unit, "Shadowfall Masks Upgrade"))
                unit.TemporaryFightsFirst = true;
        }
    }

    public static bool HasFlag(SquadController unit, string flag)
    {
        if (unit == null || string.IsNullOrWhiteSpace(flag)) return false;
        unit = unit.JoinedActionController();
        string key = NormalizeKey(flag);
        return Contains(phaseFlags, unit, key) ||
               Contains(turnFlags, unit, key) ||
               Contains(commandFlags, unit, key);
    }

    public static void SetPhaseFlag(SquadController unit, string flag, bool value = true)
    {
        Set(phaseFlags, unit, flag, value);
    }

    public static void SetTurnFlag(SquadController unit, string flag, bool value = true)
    {
        Set(turnFlags, unit, flag, value);
    }

    public static void SetCommandFlag(SquadController unit, string flag, bool value = true)
    {
        Set(commandFlags, unit, flag, value);
    }

    private static bool Contains(Dictionary<SquadController, HashSet<string>> store, SquadController unit, string key)
    {
        HashSet<string> values;
        return store.TryGetValue(unit, out values) && values.Contains(key);
    }

    private static void Set(Dictionary<SquadController, HashSet<string>> store, SquadController unit, string flag, bool value)
    {
        if (unit == null || string.IsNullOrWhiteSpace(flag)) return;
        unit = unit.JoinedActionController();
        HashSet<string> values;
        if (!store.TryGetValue(unit, out values))
        {
            values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            store[unit] = values;
        }
        string key = NormalizeKey(flag);
        if (value) values.Add(key); else values.Remove(key);
    }

    public static void ClearPhase(string faction)
    {
        ClearStoreForFaction(phaseFlags, faction);
    }

    public static void ClearTurn(string faction)
    {
        ClearStoreForFaction(turnFlags, faction);
        usedTurn.RemoveWhere(key => key.StartsWith((faction ?? "") + "|", StringComparison.OrdinalIgnoreCase));
    }

    public static void ClearCommandCycle(string faction)
    {
        ClearStoreForFaction(commandFlags, faction);
    }

    private static void ClearStoreForFaction(Dictionary<SquadController, HashSet<string>> store, string faction)
    {
        List<SquadController> remove = store.Keys
            .Where(unit => unit == null || string.Equals(unit.FactionId, faction, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (SquadController unit in remove) store.Remove(unit);
    }

    public static void BeginRound(string faction, int round)
    {
        usedRound.RemoveWhere(key => key.StartsWith((faction ?? "") + "|", StringComparison.OrdinalIgnoreCase));
    }

    public static bool MarkOncePerRound(string faction, string id)
    {
        return usedRound.Add((faction ?? "") + "|" + NormalizeKey(id));
    }

    public static bool HasUsedThisRound(string faction, string id)
    {
        return usedRound.Contains((faction ?? "") + "|" + NormalizeKey(id));
    }

    public static bool MarkOncePerTurn(string faction, string id)
    {
        return usedTurn.Add((faction ?? "") + "|" + NormalizeKey(id));
    }

    public static bool HasUsedThisTurn(string faction, string id)
    {
        return usedTurn.Contains((faction ?? "") + "|" + NormalizeKey(id));
    }

    public static bool MarkOncePerBattle(string faction, string id)
    {
        return usedBattle.Add((faction ?? "") + "|" + NormalizeKey(id));
    }

    public static bool HasUsedThisBattle(string faction, string id)
    {
        return usedBattle.Contains((faction ?? "") + "|" + NormalizeKey(id));
    }

    public static IReadOnlyList<int> FateDice(string faction)
    {
        List<int> values;
        if (!fateDice.TryGetValue(faction ?? "", out values)) return new int[0];
        return values.ToArray();
    }

    public static void SetFateDice(string faction, IEnumerable<int> values)
    {
        fateDice[faction ?? ""] = values == null
            ? new List<int>()
            : values.Select(value => Mathf.Clamp(value, 1, 6)).ToList();
    }

    public static bool HasFateDie(string faction, int value)
    {
        List<int> values;
        return fateDice.TryGetValue(faction ?? "", out values) && values.Contains(value);
    }

    public static bool SpendFateDie(string faction, int value)
    {
        List<int> values;
        if (!fateDice.TryGetValue(faction ?? "", out values)) return false;
        int index = values.IndexOf(value);
        if (index < 0) return false;
        values.RemoveAt(index);
        return true;
    }

    public static bool AdjustFateDie(string faction, int index, int delta)
    {
        List<int> values;
        if (!fateDice.TryGetValue(faction ?? "", out values) || index < 0 || index >= values.Count) return false;
        values[index] = Mathf.Clamp(values[index] + delta, 1, 6);
        return true;
    }

    public static int RequiredFateValue(string stratagemName)
    {
        string key = NormalizeKey(stratagemName);
        if (key.Contains("presentiment_of_dread")) return 1;
        if (key.Contains("forewarned")) return 2;
        if (key.Contains("unshrouded_truth")) return 3;
        if (key.Contains("fate_inescapable")) return 4;
        if (key.Contains("isha_s_fury")) return 5;
        if (key.Contains("psychic_shield")) return 6;
        return 0;
    }

    public static int FateDiceCountForBattle(GameController game)
    {
        if (game == null) return 6;
        string size = game.BattleSizeName ?? "";
        if (string.Equals(size, "Incursion", StringComparison.OrdinalIgnoreCase)) return 3;
        if (string.Equals(size, "Strike Force", StringComparison.OrdinalIgnoreCase)) return 6;
        if (string.Equals(size, "Onslaught", StringComparison.OrdinalIgnoreCase)) return 9;
        return game.BattlePoints <= 1000 ? 3 : game.BattlePoints <= 2000 ? 6 : 9;
    }

    public static int InfamyPenalty(SquadController target)
    {
        if (target == null) return 0;
        foreach (KeyValuePair<string, AeldariGameController> pair in controllers)
        {
            AeldariGameController controller = pair.Value;
            if (controller == null || !controller.HasDetachment(AeldariDetachment.CorsairCoterie)) continue;
            if (string.Equals(target.FactionId, controller.FactionId, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (SquadController source in controller.ArmyUnits)
            {
                if (source == null || !source.IsAlive || !source.IsOnBattlefield) continue;
                if (!AeldariFactionPack11.UnitHasEnhancement(source, "Infamy (Aura)")) continue;
                if (controller.OwnerGame != null && controller.OwnerGame.JoinedDistancePublic(source, target) <= 3.001f)
                    return 1;
            }
        }
        return 0;
    }

    public static void HandleFactionEvent(AeldariGameController controller, GameEventContext context)
    {
        if (controller == null || context == null) return;
        Register(controller);
        SynchronizePersistent(controller);

        switch (context.Type)
        {
            case GameEventType.BattleStarted:
                controller.OwnerGame.Aeldari11OnBattleStarted(controller);
                break;

            case GameEventType.BattleRoundStarted:
                BeginRound(controller.FactionId, context.Amount > 0 ? context.Amount : controller.OwnerGame.BattleRound);
                controller.OwnerGame.Aeldari11OnBattleRoundStarted(controller);
                break;

            case GameEventType.PhaseStarted:
                ClearPhase(controller.FactionId);
                if (context.Phase == GameController.Phase.Command &&
                    string.Equals(context.ActingFaction, controller.FactionId, StringComparison.OrdinalIgnoreCase))
                {
                    ClearCommandCycle(controller.FactionId);
                }
                SynchronizePersistent(controller);
                controller.OwnerGame.Aeldari11OnPhaseStarted(controller, context);
                break;

            case GameEventType.PhaseEnded:
                controller.OwnerGame.Aeldari11OnPhaseEnded(controller, context);
                ClearPhase(controller.FactionId);
                break;

            case GameEventType.TurnEnded:
                controller.OwnerGame.Aeldari11OnTurnEnded(controller, context);
                ClearTurn(controller.FactionId);
                break;

            case GameEventType.UnitDisembarked:
                if (context.Source != null &&
                    string.Equals(context.Source.FactionId, controller.FactionId, StringComparison.OrdinalIgnoreCase) &&
                    controller.HasDetachment(AeldariDetachment.SerpentsBrood) &&
                    context.Source.HasKeyword("harlequins"))
                {
                    SetTurnFlag(context.Source, "serpent_disembark_sustained");
                }
                controller.OwnerGame.Aeldari11OfferEventRules(controller, context);
                break;

            default:
                controller.OwnerGame.Aeldari11OfferEventRules(controller, context);
                break;
        }
    }
}
