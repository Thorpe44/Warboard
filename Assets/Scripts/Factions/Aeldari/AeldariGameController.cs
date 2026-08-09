using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Faction-level controller for Aeldari.
///
/// v33 is a migration release: the existing AeldariRulesSystem remains the
/// gameplay implementation, while this controller becomes the faction-level
/// authority and owns detachment-derived state synchronisation.
///
/// Future Aeldari rules should enter through this controller rather than
/// adding new Aeldari branches to GameController.
/// </summary>
public sealed class AeldariGameController :
    FactionGameControllerBase
{
    private AeldariRulesSystem rules;

    private AeldariDetachment lastDetachment;
    private bool hasLastDetachment;

    public override string DisplayName
    {
        get { return "Aeldari"; }
    }

    public AeldariRulesSystem Rules
    {
        get
        {
            EnsureRulesBinding();
            return rules;
        }
    }

    public override void Initialize(
        GameController game,
        string factionId)
    {
        base.Initialize(
            game,
            factionId);

        EnsureRulesBinding();
    }

    public override void RefreshArmy(
        IReadOnlyList<SquadController> units)
    {
        base.RefreshArmy(units);

        EnsureRulesBinding();
        SynchronizeDetachmentState();
    }

    public override void OnGameEvent(
        GameEventContext context)
    {
        if (!EventConcernsFaction(
                context))
        {
            return;
        }

        EnsureRulesBinding();

        switch (context.Type)
        {
            case GameEventType.BattleRoundStarted:
            case GameEventType.TurnStarted:
            case GameEventType.PhaseStarted:
            case GameEventType.UnitSetUp:
                SynchronizeDetachmentState();
                break;
        }
    }

    public override void Tick()
    {
        EnsureRulesBinding();

        if (rules == null)
            return;

        AeldariDetachment current =
            rules.GetDetachment(
                FactionId);

        if (!hasLastDetachment ||
            current != lastDetachment)
        {
            SynchronizeDetachmentState();
        }
    }

    private void EnsureRulesBinding()
    {
        if (rules != null ||
            Game == null)
        {
            return;
        }

        FieldInfo field =
            typeof(GameController)
                .GetField(
                    "aeldariRules",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

        if (field == null)
            return;

        rules =
            field.GetValue(Game)
            as AeldariRulesSystem;
    }

    /// <summary>
    /// Ensures that keywords/state granted by a detachment do not leak into
    /// another detachment when the player cycles selections.
    ///
    /// The old v32 path only added keywords. v33 makes the selected Aeldari
    /// detachment the authority for those temporary grants.
    /// </summary>
    private void SynchronizeDetachmentState()
    {
        if (rules == null ||
            !rules.IsAeldariFaction(
                FactionId))
        {
            return;
        }

        AeldariDetachment detachment =
            rules.GetDetachment(
                FactionId);

        foreach (SquadController unit
            in army)
        {
            if (unit == null ||
                !string.Equals(
                    unit.FactionId,
                    FactionId,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SynchronizeYnnariKeyword(
                unit,
                detachment);

            SynchronizeBattlelineKeyword(
                unit,
                detachment);

            SynchronizeObjectiveControl(
                unit,
                detachment);
        }

        // Keep the existing v32 implementation responsible for adding all
        // grants that belong to the currently selected detachment.
        rules.ApplyDetachmentKeywords(
            FactionId,
            army);

        lastDetachment = detachment;
        hasLastDetachment = true;
    }

    private void SynchronizeYnnariKeyword(
        SquadController unit,
        AeldariDetachment detachment)
    {
        bool asuryani =
            unit.HasIntrinsicKeyword(
                "asuryani");

        bool epicHero =
            unit.HasIntrinsicKeyword(
                "epic hero");

        if (!asuryani ||
            epicHero)
        {
            return;
        }

        bool shouldHave =
            detachment ==
                AeldariDetachment
                    .DevotedOfYnnead;

        SetAddedFactionKeyword(
            unit,
            "YNNARI",
            shouldHave);
    }

    private void SynchronizeBattlelineKeyword(
        SquadController unit,
        AeldariDetachment detachment)
    {
        bool windrider =
            unit.HasIntrinsicKeyword(
                "windriders") ||
            NameContains(
                unit,
                "Windrider");

        bool wraithBattleline =
            unit.HasIntrinsicKeyword(
                "wraithblades") ||
            unit.HasIntrinsicKeyword(
                "wraithguard") ||
            NameContains(
                unit,
                "Wraithblade") ||
            NameContains(
                unit,
                "Wraithguard");

        bool troupe =
            unit.HasIntrinsicKeyword(
                "troupe") ||
            NameContains(
                unit,
                "Troupe");

        bool granted =
            (detachment ==
                AeldariDetachment
                    .WindriderHost &&
             windrider) ||
            (detachment ==
                AeldariDetachment
                    .SpiritConclave &&
             wraithBattleline) ||
            ((detachment ==
                 AeldariDetachment
                     .GhostsOfTheWebway ||
              detachment ==
                 AeldariDetachment
                     .SerpentsBrood) &&
             troupe);

        // Only the units above receive BATTLELINE from the detachment
        // architecture. Other units retain whatever their imported
        // datasheet already says.
        if (windrider ||
            wraithBattleline ||
            troupe)
        {
            SetAddedFactionKeyword(
                unit,
                "BATTLELINE",
                granted);
        }
    }

    private void SynchronizeObjectiveControl(
        SquadController unit,
        AeldariDetachment detachment)
    {
        bool troupe =
            unit.HasIntrinsicKeyword(
                "troupe") ||
            NameContains(
                unit,
                "Troupe");

        bool troupeOcTwo =
            troupe &&
            (detachment ==
                AeldariDetachment
                    .GhostsOfTheWebway ||
             detachment ==
                AeldariDetachment
                    .SerpentsBrood);

        unit.AeldariObjectiveControlOverride =
            troupeOcTwo
            ? 2
            : 0;
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

    private static void SetAddedFactionKeyword(
        SquadController unit,
        string keyword,
        bool enabled)
    {
        if (unit == null ||
            unit.SourceData == null ||
            string.IsNullOrWhiteSpace(
                keyword))
        {
            return;
        }

        if (enabled)
        {
            unit.AddFactionKeyword(
                keyword);

            return;
        }

        // Detachment-granted keywords in v32 were written into
        // SourceData.factionKeywords. Remove the temporary grant from that
        // collection when its detachment is no longer selected.
        List<string> values =
            new List<string>(
                unit.SourceData
                    .factionKeywords ??
                new string[0]);

        values.RemoveAll(
            value =>
                string.Equals(
                    value,
                    keyword,
                    StringComparison.OrdinalIgnoreCase));

        unit.SourceData.factionKeywords =
            values.ToArray();
    }
}
