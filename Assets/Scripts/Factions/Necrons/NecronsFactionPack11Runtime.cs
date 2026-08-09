using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Transient Edition 11 Necrons faction-rule state.
/// </summary>
public static class NecronsFactionPack11Runtime
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

    private static readonly Dictionary<string, NecronGameController>
        controllers =
            new Dictionary<string, NecronGameController>(
                StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, SquadController>
        worthyFoes =
            new Dictionary<string, SquadController>(
                StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<SquadController, HashSet<string>>
        augmentations =
            new Dictionary<SquadController, HashSet<string>>();

    private static readonly Dictionary<string, HashSet<SquadController>>
        cursedTargets =
            new Dictionary<string, HashSet<SquadController>>(
                StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, HashSet<SquadController>>
        animusTargets =
            new Dictionary<string, HashSet<SquadController>>(
                StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<SquadController>
        distortionExtended =
            new HashSet<SquadController>();

    private static readonly HashSet<SquadController>
        pinned =
            new HashSet<SquadController>();

    private static readonly HashSet<SquadController>
        reanimatedRevenants =
            new HashSet<SquadController>();

    private static readonly Dictionary<string, bool>
        coldFervourEmpowered =
            new Dictionary<string, bool>(
                StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, bool>
        yourTimeIsNigh =
            new Dictionary<string, bool>(
                StringComparer.OrdinalIgnoreCase);

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
        NecronGameController controller)
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

    public static IEnumerable<NecronGameController> AllControllers()
    {
        return controllers.Values
            .Where(value => value != null)
            .ToArray();
    }

    public static NecronGameController Controller(
        string faction)
    {
        NecronGameController result;

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

        List<SquadController> clearAugment =
            augmentations.Keys
                .Where(unit =>
                    unit == null ||
                    string.Equals(
                        unit.FactionId,
                        faction,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        foreach (SquadController unit in clearAugment)
            augmentations.Remove(unit);

        distortionExtended.RemoveWhere(
            unit =>
                unit == null ||
                string.Equals(
                    unit.FactionId,
                    faction,
                    StringComparison.OrdinalIgnoreCase));
    }

    public static void ClearTurn(string faction)
    {
        ClearStore(turnFlags, faction);

        usedTurn.RemoveWhere(
            key =>
                key.StartsWith(
                    (faction ?? "") + "|",
                    StringComparison.OrdinalIgnoreCase));

        coldFervourEmpowered[faction ?? ""] =
            false;

        pinned.RemoveWhere(
            unit =>
                unit == null ||
                string.Equals(
                    unit.FactionId,
                    faction,
                    StringComparison.OrdinalIgnoreCase));
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

    public static bool HasUsedThisRound(
        string faction,
        string id)
    {
        return usedRound.Contains(
            (faction ?? "") + "|" +
            NormalizeKey(id));
    }

    public static void SetWorthyFoe(
        string faction,
        SquadController target)
    {
        if (target == null)
            worthyFoes.Remove(faction ?? "");
        else
            worthyFoes[faction ?? ""] =
                target.JoinedActionController();
    }

    public static SquadController WorthyFoe(
        string faction)
    {
        SquadController result;

        return worthyFoes.TryGetValue(
            faction ?? "",
            out result)
            ? result
            : null;
    }

    public static void SetAugmentation(
        SquadController unit,
        string value,
        bool clearExisting = true)
    {
        if (unit == null)
            return;

        unit = unit.JoinedActionController();

        HashSet<string> values;

        if (!augmentations.TryGetValue(
                unit,
                out values) ||
            values == null)
        {
            values =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            augmentations[unit] =
                values;
        }

        if (clearExisting)
            values.Clear();

        values.Add(
            NormalizeKey(value));
    }

    public static bool HasAugmentation(
        SquadController unit,
        string value)
    {
        if (unit == null)
            return false;

        HashSet<string> values;

        return
            augmentations.TryGetValue(
                unit.JoinedActionController(),
                out values) &&
            values != null &&
            values.Contains(
                NormalizeKey(value));
    }

    public static bool HasAnyAugmentation(
        SquadController unit)
    {
        if (unit == null)
            return false;

        HashSet<string> values;

        return
            augmentations.TryGetValue(
                unit.JoinedActionController(),
                out values) &&
            values != null &&
            values.Count > 0;
    }

    private static HashSet<SquadController> TargetSet(
        Dictionary<string, HashSet<SquadController>> store,
        string faction)
    {
        HashSet<SquadController> set;

        if (!store.TryGetValue(
                faction ?? "",
                out set) ||
            set == null)
        {
            set =
                new HashSet<SquadController>();

            store[faction ?? ""] = set;
        }

        return set;
    }

    public static void MarkCursedTarget(
        string faction,
        SquadController target)
    {
        if (target != null)
            TargetSet(cursedTargets, faction)
                .Add(target.JoinedActionController());
    }

    public static bool IsCursedTarget(
        string faction,
        SquadController target)
    {
        if (target == null)
            return false;

        return TargetSet(
                cursedTargets,
                faction)
            .Contains(
                target.JoinedActionController());
    }

    public static void MarkAnimusTarget(
        string faction,
        SquadController target)
    {
        if (target != null)
            TargetSet(animusTargets, faction)
                .Add(target.JoinedActionController());
    }

    public static bool IsAnimusTarget(
        string faction,
        SquadController target)
    {
        if (target == null)
            return false;

        return TargetSet(
                animusTargets,
                faction)
            .Contains(
                target.JoinedActionController());
    }

    public static void SetDistortionExtended(
        SquadController unit,
        bool value)
    {
        if (unit == null)
            return;

        unit = unit.JoinedActionController();

        if (value)
            distortionExtended.Add(unit);
        else
            distortionExtended.Remove(unit);
    }

    public static bool DistortionExtended(
        SquadController unit)
    {
        return
            unit != null &&
            distortionExtended.Contains(
                unit.JoinedActionController());
    }

    public static void SetPinned(
        SquadController unit,
        bool value)
    {
        if (unit == null)
            return;

        unit = unit.JoinedActionController();

        if (value)
            pinned.Add(unit);
        else
            pinned.Remove(unit);
    }

    public static bool IsPinned(
        SquadController unit)
    {
        return
            unit != null &&
            pinned.Contains(
                unit.JoinedActionController());
    }

    public static void SetColdFervourEmpowered(
        string faction,
        bool value)
    {
        coldFervourEmpowered[
            faction ?? ""] = value;
    }

    public static bool ColdFervourEmpowered(
        string faction)
    {
        bool value;

        return
            coldFervourEmpowered.TryGetValue(
                faction ?? "",
                out value) &&
            value;
    }

    public static void SetYourTimeIsNigh(
        string faction,
        bool value)
    {
        yourTimeIsNigh[
            faction ?? ""] = value;
    }

    public static bool YourTimeIsNigh(
        string faction)
    {
        bool value;

        return
            yourTimeIsNigh.TryGetValue(
                faction ?? "",
                out value) &&
            value;
    }

    public static bool MarkRevenantUsed(
        SquadController unit)
    {
        return
            unit != null &&
            reanimatedRevenants.Add(
                unit.JoinedActionController());
    }

    public static void HandleFactionEvent(
        NecronGameController controller,
        GameEventContext context)
    {
        if (controller == null ||
            context == null ||
            controller.OwnerGame == null)
        {
            return;
        }

        Register(controller);

        switch (context.Type)
        {
            case GameEventType.BattleStarted:
                controller.OwnerGame
                    .Necrons11OnBattleStarted(
                        controller);
                break;

            case GameEventType.BattleRoundStarted:
                BeginRound(controller.FactionId);

                controller.OwnerGame
                    .Necrons11OnBattleRoundStarted(
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
                    .Necrons11OnTurnStarted(
                        controller,
                        context);
                break;

            case GameEventType.PhaseStarted:
                ClearPhase(
                    controller.FactionId);

                controller.OwnerGame
                    .Necrons11OnPhaseStarted(
                        controller,
                        context);
                break;
        }

        controller.OwnerGame
            .Necrons11OfferEventRules(
                controller,
                context);
    }
}
