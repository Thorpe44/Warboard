using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Transient Edition 11 Adeptus Custodes faction-rule state.
/// </summary>
public static class CustodesFactionPack11Runtime
{
    private static readonly Dictionary<SquadController, HashSet<string>>
        phaseFlags =
            new Dictionary<SquadController, HashSet<string>>();

    private static readonly Dictionary<SquadController, HashSet<string>>
        turnFlags =
            new Dictionary<SquadController, HashSet<string>>();

    private static readonly Dictionary<SquadController, HashSet<string>>
        roundFlags =
            new Dictionary<SquadController, HashSet<string>>();

    private static readonly HashSet<string> usedBattle =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> usedTurn =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> usedRound =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, CustodesGameController>
        controllers =
            new Dictionary<string, CustodesGameController>(
                StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string>
        martialMastery =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<SquadController, string>
        katahByUnit =
            new Dictionary<SquadController, string>();

    private static readonly Dictionary<string, SquadController>
        assemblageTarget =
            new Dictionary<string, SquadController>(
                StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<SquadController> nulled =
        new HashSet<SquadController>();

    private static readonly Dictionary<SquadController, string>
        prosecutedByFaction =
            new Dictionary<SquadController, string>();

    private static readonly Dictionary<SquadController, SquadController>
        shieldOfHonourRedirect =
            new Dictionary<SquadController, SquadController>();

    public static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string normalized =
            WeaponRuleParser.NormalizeRuleName(value);

        return normalized == null
            ? ""
            : normalized.ToLowerInvariant();
    }

    public static void Register(
        CustodesGameController controller)
    {
        if (controller == null ||
            string.IsNullOrWhiteSpace(
                controller.FactionId))
        {
            return;
        }

        controllers[controller.FactionId] =
            controller;
    }

    public static CustodesGameController Controller(
        string faction)
    {
        CustodesGameController result;

        return
            !string.IsNullOrWhiteSpace(faction) &&
            controllers.TryGetValue(
                faction,
                out result)
            ? result
            : null;
    }

    public static bool HasFlag(
        SquadController unit,
        string flag)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(flag))
        {
            return false;
        }

        unit = unit.JoinedActionController();
        string key = NormalizeKey(flag);

        return
            Contains(phaseFlags, unit, key) ||
            Contains(turnFlags, unit, key) ||
            Contains(roundFlags, unit, key);
    }

    public static void SetPhaseFlag(
        SquadController unit,
        string flag,
        bool value = true)
    {
        Set(phaseFlags, unit, flag, value);
    }

    public static void SetTurnFlag(
        SquadController unit,
        string flag,
        bool value = true)
    {
        Set(turnFlags, unit, flag, value);
    }

    public static void SetRoundFlag(
        SquadController unit,
        string flag,
        bool value = true)
    {
        Set(roundFlags, unit, flag, value);
    }

    private static bool Contains(
        Dictionary<SquadController, HashSet<string>> store,
        SquadController unit,
        string key)
    {
        HashSet<string> values;

        return
            store.TryGetValue(unit, out values) &&
            values != null &&
            values.Contains(key);
    }

    private static void Set(
        Dictionary<SquadController, HashSet<string>> store,
        SquadController unit,
        string flag,
        bool value)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(flag))
        {
            return;
        }

        unit = unit.JoinedActionController();

        HashSet<string> values;

        if (!store.TryGetValue(unit, out values) ||
            values == null)
        {
            values =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            store[unit] = values;
        }

        string key = NormalizeKey(flag);

        if (value)
            values.Add(key);
        else
            values.Remove(key);
    }

    private static void ClearStore(
        Dictionary<SquadController, HashSet<string>> store,
        string faction)
    {
        List<SquadController> remove =
            store.Keys
                .Where(unit =>
                    unit == null ||
                    string.Equals(
                        unit.FactionId,
                        faction,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        foreach (SquadController unit in remove)
            store.Remove(unit);
    }

    public static void ClearPhase(string faction)
    {
        ClearStore(phaseFlags, faction);
        shieldOfHonourRedirect.Clear();
    }

    public static void ClearTurn(string faction)
    {
        ClearStore(turnFlags, faction);

        usedTurn.RemoveWhere(
            key =>
                key.StartsWith(
                    (faction ?? "") + "|",
                    StringComparison.OrdinalIgnoreCase));

        List<SquadController> prosecuted =
            prosecutedByFaction
                .Where(pair =>
                    string.Equals(
                        pair.Value,
                        faction,
                        StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key)
                .ToList();

        foreach (SquadController target in prosecuted)
            prosecutedByFaction.Remove(target);
    }

    public static void BeginRound(string faction)
    {
        ClearStore(roundFlags, faction);

        usedRound.RemoveWhere(
            key =>
                key.StartsWith(
                    (faction ?? "") + "|",
                    StringComparison.OrdinalIgnoreCase));
    }

    public static bool MarkOncePerBattle(
        string faction,
        string id)
    {
        return usedBattle.Add(
            (faction ?? "") + "|" +
            NormalizeKey(id));
    }

    public static bool HasUsedThisBattle(
        string faction,
        string id)
    {
        return usedBattle.Contains(
            (faction ?? "") + "|" +
            NormalizeKey(id));
    }

    public static bool MarkOncePerTurn(
        string faction,
        string id)
    {
        return usedTurn.Add(
            (faction ?? "") + "|" +
            NormalizeKey(id));
    }

    public static bool HasUsedThisTurn(
        string faction,
        string id)
    {
        return usedTurn.Contains(
            (faction ?? "") + "|" +
            NormalizeKey(id));
    }

    public static bool MarkOncePerRound(
        string faction,
        string id)
    {
        return usedRound.Add(
            (faction ?? "") + "|" +
            NormalizeKey(id));
    }

    public static void SetMartialMastery(
        string faction,
        string mode)
    {
        martialMastery[faction ?? ""] =
            NormalizeKey(mode);
    }

    public static string MartialMastery(
        string faction)
    {
        string value;

        return martialMastery.TryGetValue(
            faction ?? "",
            out value)
            ? value
            : "";
    }

    public static void SetKatah(
        SquadController unit,
        string stance)
    {
        if (unit == null)
            return;

        katahByUnit[
            unit.JoinedActionController()] =
                NormalizeKey(stance);
    }

    public static string Katah(
        SquadController unit)
    {
        if (unit == null)
            return "";

        string value;

        return katahByUnit.TryGetValue(
            unit.JoinedActionController(),
            out value)
            ? value
            : "";
    }

    public static void ClearKatah(
        SquadController unit)
    {
        if (unit != null)
        {
            katahByUnit.Remove(
                unit.JoinedActionController());
        }
    }

    public static void SetAssemblageTarget(
        string faction,
        SquadController target)
    {
        if (target == null)
            assemblageTarget.Remove(faction ?? "");
        else
            assemblageTarget[faction ?? ""] =
                target.JoinedActionController();
    }

    public static SquadController AssemblageTarget(
        string faction)
    {
        SquadController result;

        return assemblageTarget.TryGetValue(
            faction ?? "",
            out result)
            ? result
            : null;
    }

    public static void SetNulled(
        SquadController target,
        bool value = true)
    {
        if (target == null)
            return;

        target = target.JoinedActionController();

        if (value)
            nulled.Add(target);
        else
            nulled.Remove(target);
    }

    public static bool IsNulled(
        SquadController target)
    {
        return
            target != null &&
            nulled.Contains(
                target.JoinedActionController());
    }

    public static void SetProsecuted(
        SquadController target,
        string faction)
    {
        if (target == null)
            return;

        prosecutedByFaction[
            target.JoinedActionController()] =
                faction ?? "";
    }

    public static bool IsProsecutedBy(
        SquadController target,
        string faction)
    {
        if (target == null)
            return false;

        string value;

        return
            prosecutedByFaction.TryGetValue(
                target.JoinedActionController(),
                out value) &&
            string.Equals(
                value,
                faction,
                StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsProsecuted(
        SquadController target)
    {
        return
            target != null &&
            prosecutedByFaction.ContainsKey(
                target.JoinedActionController());
    }

    public static void SetShieldOfHonourRedirect(
        SquadController anathema,
        SquadController custodian)
    {
        if (anathema == null)
            return;

        if (custodian == null)
            shieldOfHonourRedirect.Remove(
                anathema.JoinedActionController());
        else
            shieldOfHonourRedirect[
                anathema.JoinedActionController()] =
                    custodian.JoinedActionController();
    }

    public static SquadController ShieldOfHonourRedirect(
        SquadController target)
    {
        if (target == null)
            return null;

        SquadController result;

        return
            shieldOfHonourRedirect.TryGetValue(
                target.JoinedActionController(),
                out result)
            ? result
            : null;
    }

    public static void HandleFactionEvent(
        CustodesGameController controller,
        GameEventContext context)
    {
        if (controller == null ||
            context == null)
        {
            return;
        }

        Register(controller);

        switch (context.Type)
        {
            case GameEventType.BattleStarted:
                controller.OwnerGame
                    .Custodes11OnBattleStarted(
                        controller);
                break;

            case GameEventType.BattleRoundStarted:
                BeginRound(controller.FactionId);

                controller.OwnerGame
                    .Custodes11OnBattleRoundStarted(
                        controller);
                break;

            case GameEventType.TurnStarted:
                if (string.Equals(
                        context.ActingFaction,
                        controller.FactionId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ClearTurn(
                        controller.FactionId);
                }

                controller.OwnerGame
                    .Custodes11OnTurnStarted(
                        controller,
                        context);
                break;

            case GameEventType.PhaseStarted:
                ClearPhase(
                    controller.FactionId);

                controller.OwnerGame
                    .Custodes11OnPhaseStarted(
                        controller,
                        context);
                break;

            case GameEventType.UnitSelectedToFight:
                if (context.Source != null &&
                    string.Equals(
                        context.Source.FactionId,
                        controller.FactionId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    controller.OwnerGame
                        .Custodes11OfferKatah(
                            context.Source);
                }
                break;

            case GameEventType.UnitFinishedFighting:
                if (context.Source != null)
                    ClearKatah(context.Source);
                break;
        }

        controller.OwnerGame
            .Custodes11OfferEventRules(
                controller,
                context);
    }
}
