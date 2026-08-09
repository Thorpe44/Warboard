using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Aeldari faction-level runtime controller.
///
/// v37 makes this the authority for:
/// - the selected/locked Aeldari detachment
/// - the loaded detachment controller
/// - detachment-granted temporary keywords/state
/// - the base Battle Focus token pool
///
/// Detachment identity comes from imported roster metadata when available.
/// If the roster does not expose a single unambiguous detachment, the
/// pre-game selector must confirm it before deployment can begin.
///
/// The existing AeldariRulesSystem remains a legacy rules implementation
/// behind the faction/detachment controllers while individual rule bodies
/// continue to migrate outward.
/// </summary>
public sealed class AeldariGameController :
    FactionGameControllerBase,
    IFactionPreGameController
{
private static readonly Dictionary<
        AeldariDetachment,
        string
    > DetachmentNames =
        new Dictionary<
            AeldariDetachment,
            string>
        {
            {
                AeldariDetachment.Warhost,
                "Warhost"
            },
            {
                AeldariDetachment.WindriderHost,
                "Windrider Host"
            },
            {
                AeldariDetachment.SpiritConclave,
                "Spirit Conclave"
            },
            {
                AeldariDetachment.GuardianBattlehost,
                "Guardian Battlehost"
            },
            {
                AeldariDetachment.GhostsOfTheWebway,
                "Ghosts of the Webway"
            },
            {
                AeldariDetachment.DevotedOfYnnead,
                "Devoted of Ynnead"
            },
            {
                AeldariDetachment.SeerCouncil,
                "Seer Council"
            },
            {
                AeldariDetachment.AspectHost,
                "Aspect Host"
            },
            {
                AeldariDetachment.ArmouredWarhost,
                "Armoured Warhost"
            },
            {
                AeldariDetachment.FatefulPerformance,
                "Fateful Performance"
            },
            {
                AeldariDetachment.PathOfTheOutcast,
                "Path of the Outcast"
            },
            {
                AeldariDetachment.TwilightFlickers,
                "Twilight Flickers"
            },
            {
                AeldariDetachment.SerpentsBrood,
                "Serpent's Brood"
            },
            {
                AeldariDetachment.EldritchRaiders,
                "Eldritch Raiders"
            },
            {
                AeldariDetachment.CorsairCoterie,
                "Corsair Coterie"
            }
        };

    private AeldariRulesSystem rules;

    private IAeldariDetachmentController
        detachmentController;

    private AeldariDetachment lockedDetachment;
    private bool detachmentLocked;
    private string detachmentLockSource = "";

    private AeldariDetachment suggestedDetachment =
        AeldariDetachment.Warhost;

    private RosterImportMetadata rosterMetadata;
    private int rosterMetadataRevision = -1;
    private string rosterProbeStatus = "";

    private string selectionError = "";

    private readonly AeldariBattleFocusController
        battleFocus =
            new AeldariBattleFocusController();

    private readonly HashSet<SquadController>
        ynnariGrantedByDetachment =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        battlelineGrantedByDetachment =
            new HashSet<SquadController>();

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

    public bool DetachmentLocked
    {
        get { return detachmentLocked; }
    }

    public AeldariDetachment LockedDetachment
    {
        get
        {
            return detachmentLocked
                ? lockedDetachment
                : suggestedDetachment;
        }
    }

    public string DetachmentName
    {
        get
        {
            return DisplayNameFor(
                LockedDetachment);
        }
    }

    public string DetachmentLockSource
    {
        get { return detachmentLockSource; }
    }

    public string RosterProbeStatus
    {
        get { return rosterProbeStatus; }
    }

    public string SelectionError
    {
        get { return selectionError; }
    }

    public int BattleFocusTokens
    {
        get { return battleFocus.Tokens; }
    }

    public IAeldariDetachmentController
        ActiveDetachmentController
    {
        get { return detachmentController; }
    }

    public override void Initialize(
        GameController game,
        string factionId)
    {
        base.Initialize(
            game,
            factionId);

        EnsureRulesBinding();

        battleFocus.Initialize(
            game,
            factionId);
    }

public override void RefreshArmy(
    IReadOnlyList<SquadController> units)
{
    base.RefreshArmy(units);

    EnsureRulesBinding();
    PruneTemporaryKeywordGrants();

    ResolveRosterDetachmentMetadata();
    SynchronizeDetachmentState();
}

public override void OnGameEvent(
        GameEventContext context)
    {
        if (context == null)
            return;

        EnsureRulesBinding();

        // Battle-round and phase timing are global core events. Every Aeldari
        // controller must receive them, regardless of which faction currently
        // has the active turn.
        battleFocus.HandleGameEvent(
            context,
            UsesBattleFocus());

        switch (context.Type)
        {
            case GameEventType.BattleStarted:
            case GameEventType.BattleRoundStarted:
            case GameEventType.TurnStarted:
            case GameEventType.PhaseStarted:
                SynchronizeDetachmentState();
                break;
        }

        if (!EventConcernsFaction(
                context) &&
            context.Type !=
                GameEventType.BattleRoundStarted &&
            context.Type !=
                GameEventType.BattleRoundEnded &&
            context.Type !=
                GameEventType.PhaseEnded)
        {
            return;
        }

        if (context.Type ==
                GameEventType.UnitSetUp)
        {
            SynchronizeDetachmentState();
        }

        if (detachmentController != null)
        {
            detachmentController.OnGameEvent(
                context);
        }
    }

public bool ShouldShowDetachmentSelection()
{
    if (detachmentLocked ||
        army.Count == 0 ||
        Game == null)
    {
        return false;
    }

    ResolveRosterDetachmentMetadata();

    return Game.PreGameReady;
}

    public bool IsReadyForDeployment
    {
        get
        {
            return
                army.Count == 0 ||
                detachmentLocked;
        }
    }

    public string DeploymentBlockReason
    {
        get
        {
            if (IsReadyForDeployment)
                return "";

            return
                FactionId +
                " Aeldari detachment has not been confirmed.";
        }
    }

    public AeldariDetachment SuggestedDetachment
    {
        get { return suggestedDetachment; }
    }

    public AeldariDetachment[] AvailableDetachments()
    {
        return
            (AeldariDetachment[])
            Enum.GetValues(
                typeof(AeldariDetachment));
    }

    public string GetDetachmentDisplayName(
        AeldariDetachment detachment)
    {
        return DisplayNameFor(
            detachment);
    }

public bool TryLockDetachment(
    AeldariDetachment detachment,
    string source)
{
    EnsureRulesBinding();

    if (detachmentLocked)
    {
        if (lockedDetachment ==
                detachment)
        {
            return true;
        }

        selectionError =
            "Detachment is already locked for this battle.";

        return false;
    }

    if (Game != null &&
        Game.DeploymentStarted)
    {
        selectionError =
            "Detachment must be confirmed before deployment begins.";

        return false;
    }

    if (rules == null ||
        !rules.IsAeldariFaction(
            FactionId))
    {
        selectionError =
            "Aeldari rules have not finished loading yet.";

        return false;
    }

    string validation;

    if (!ValidateDetachment(
            detachment,
            out validation))
    {
        selectionError =
            validation;

        return false;
    }

    lockedDetachment =
        detachment;

    detachmentLocked = true;

    detachmentLockSource =
        string.IsNullOrWhiteSpace(
            source)
        ? "Pre-game roster"
        : source;

    selectionError = "";

    rules.SetDetachment(
        FactionId,
        lockedDetachment);

    LoadDetachmentController();
    SynchronizeDetachmentState();

    return true;
}

public bool UsesDevotedOfYnnead()
{
    return
        detachmentLocked &&
        lockedDetachment ==
            AeldariDetachment
                .DevotedOfYnnead;
}

    public bool UsesBattleFocus()
    {
        return army.Any(
            unit =>
                unit != null &&
                (unit.HasIntrinsicKeyword(
                     "asuryani") ||
                 FactionRuleSystem
                     .UnitOrLeaderHasRule(
                         unit,
                         "Battle Focus")));
    }

public void StartBattleRound(
        int round)
    {
        battleFocus.StartBattleRound(
            round,
            UsesBattleFocus());
    }

public bool SpendBattleFocus(
        int amount,
        string manoeuvre = "")
    {
        string failureReason;

        bool spent =
            battleFocus.Spend(
                amount,
                manoeuvre,
                out failureReason);

        if (!spent &&
            !string.IsNullOrWhiteSpace(
                failureReason))
        {
            selectionError =
                failureReason;
        }

        return spent;
    }

public void EndBattleRound()
    {
        battleFocus.EndBattleRound();
    }

    public static string DisplayNameFor(
        AeldariDetachment detachment)
    {
        string value;

        return DetachmentNames.TryGetValue(
            detachment,
            out value)
            ? value
            : detachment.ToString();
    }

private void ResolveRosterDetachmentMetadata()
{
    RosterImportMetadata current =
        RosterImportMetadataStore.Get(
            FactionId);

    if (current != null &&
        !current.MatchesArmy(
            army))
    {
        current = null;
    }

    int revision =
        current != null
        ? current.Revision
        : -1;

    if (revision !=
            rosterMetadataRevision)
    {
        if (Game == null ||
            !Game.DeploymentStarted)
        {
            ResetDetachmentForRosterChange();
        }

        rosterMetadata =
            current;

        rosterMetadataRevision =
            revision;
    }

    if (detachmentLocked)
        return;

    if (rosterMetadata == null)
    {
        rosterProbeStatus =
            "No matching imported detachment metadata was found for this army. Select the roster's detachment once before deployment.";

        return;
    }

    AeldariDetachment detected;
    string detectionMessage;

    RosterDetachmentResolution resolution =
        TryResolveRosterDetachment(
            rosterMetadata,
            out detected,
            out detectionMessage);

    rosterProbeStatus =
        detectionMessage;

    if (resolution !=
            RosterDetachmentResolution.Detected)
    {
        return;
    }

    if (TryLockDetachment(
            detected,
            "YellowScribe / New Recruit roster"))
    {
        rosterProbeStatus =
            "Detachment read from imported roster: " +
            DisplayNameFor(
                detected) +
            ".";
    }
}

private enum RosterDetachmentResolution
{
    Missing,
    Detected,
    Ambiguous
}

private RosterDetachmentResolution
    TryResolveRosterDetachment(
        RosterImportMetadata metadata,
        out AeldariDetachment detachment,
        out string message)
{
    detachment =
        AeldariDetachment.Warhost;

    message =
        "The imported roster did not expose one unambiguous Aeldari detachment. Select it once before deployment.";

    if (metadata == null)
        return RosterDetachmentResolution.Missing;

    HashSet<AeldariDetachment> strong =
        new HashSet<AeldariDetachment>();

    foreach (string value
        in metadata.ExplicitDetachmentValues ??
           new string[0])
    {
        AeldariDetachment candidate;

        if (TryMatchDetachmentText(
                value,
                true,
                out candidate))
        {
            strong.Add(
                candidate);
        }
    }

    if (strong.Count == 1)
    {
        detachment =
            strong.First();

        message =
            "Detected from explicit roster detachment metadata: " +
            DisplayNameFor(
                detachment) +
            ".";

        return RosterDetachmentResolution.Detected;
    }

    if (strong.Count > 1)
    {
        message =
            "The imported roster exposes multiple detachment values, so Warboard will not guess. Select the roster's detachment once.";

        return RosterDetachmentResolution.Ambiguous;
    }

    // YellowScribe's 8-character code stores the transformed unit payload,
    // not the roster-level configuration selections. In particular, the
    // upstream parser only carries top-level selections of type unit/model
    // into armyData, so a New Recruit/BattleScribe "Detachment Choice"
    // selection is normally absent by the time Warboard receives the code.
    //
    // Do not scan arbitrary unit/rule/category names for detachment names:
    // that creates false positives and can report an "ambiguous" detachment
    // even though YellowScribe simply did not preserve the choice.
    message =
        "YellowScribe did not preserve a roster-level Aeldari detachment choice in this code. Select the detachment once before deployment.";

    return RosterDetachmentResolution.Missing;
}

private void ResetDetachmentForRosterChange()
{
    if (detachmentLocked)
    {
        detachmentLocked = false;
        detachmentLockSource = "";
        detachmentController = null;
    }

    selectionError = "";
    suggestedDetachment =
        AeldariDetachment.Warhost;

    ClearTemporaryDetachmentState();
}




    private bool TryMatchDetachmentText(
        string value,
        bool allowContainedName,
        out AeldariDetachment detachment)
    {
        detachment =
            AeldariDetachment.Warhost;

        string normalized =
            NormalizeDetachmentText(
                value);

        if (string.IsNullOrWhiteSpace(
                normalized))
        {
            return false;
        }

        foreach (
            KeyValuePair<
                AeldariDetachment,
                string
            > pair
            in DetachmentNames
                .OrderByDescending(
                    item =>
                        item.Value.Length))
        {
            string wanted =
                NormalizeDetachmentText(
                    pair.Value);

            if (normalized == wanted ||
                (allowContainedName &&
                 normalized.Contains(
                     wanted)))
            {
                detachment =
                    pair.Key;

                return true;
            }
        }

        return false;
    }

    private static string NormalizeDetachmentText(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "";
        }

        char[] characters =
            value
                .ToLowerInvariant()
                .Replace('’', '\'')
                .Where(
                    c =>
                        char.IsLetterOrDigit(c) ||
                        char.IsWhiteSpace(c) ||
                        c == '\'')
                .ToArray();

        return string.Join(
            " ",
            new string(characters)
                .Split(
                    new[]
                    {
                        ' ',
                        '\t',
                        '\r',
                        '\n'
                    },
                    StringSplitOptions
                        .RemoveEmptyEntries));
    }

    private bool ValidateDetachment(
        AeldariDetachment detachment,
        out string message)
    {
        message = "";

        if (detachment ==
                AeldariDetachment
                    .DevotedOfYnnead)
        {
            bool hasRequiredEpicHero =
                army.Any(
                    unit =>
                        unit != null &&
                        (NameContains(
                             unit,
                             "Yvraine") ||
                         NameContains(
                             unit,
                             "Yncarne")));

            if (!hasRequiredEpicHero)
            {
                message =
                    "Devoted of Ynnead requires Yvraine and/or the Yncarne in the army.";

                return false;
            }
        }

        return true;
    }

    private void LoadDetachmentController()
    {
        if (!detachmentLocked)
            return;

        if (detachmentController != null &&
            detachmentController.Detachment ==
                lockedDetachment)
        {
            return;
        }

        detachmentController =
            AeldariDetachmentControllerFactory
                .Create(
                    lockedDetachment);

        if (detachmentController != null)
        {
            detachmentController.Initialize(
                this);
        }
    }

private void EnsureRulesBinding()
{
    if (Game == null)
        return;

    if (rules == null)
    {
        rules =
            Game.AeldariRules;
    }

    if (rules == null)
        return;

    // v37.1: roster import notifies faction controllers immediately. Ensure
    // the backing AeldariRulesSystem knows the newly loaded armies before
    // detachment validation/locking is attempted.
    if (!rules.IsAeldariFaction(
            FactionId))
    {
        rules.Configure(
            Game.AllSquads != null
                ? Game.AllSquads.ToList()
                : new List<SquadController>(),
            Game.FactionIds != null
                ? Game.FactionIds.ToList()
                : new List<string>());
    }
}

private void SynchronizeDetachmentState()
{
    if (rules == null ||
        !rules.IsAeldariFaction(
            FactionId))
    {
        return;
    }

    if (!detachmentLocked)
    {
        ClearTemporaryDetachmentState();
        return;
    }

    AeldariDetachment detachment =
        lockedDetachment;

    rules.SetDetachment(
        FactionId,
        detachment);

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

    LoadDetachmentController();
}

private void ClearTemporaryDetachmentState()
{
    foreach (SquadController unit
        in ynnariGrantedByDetachment
            .ToArray())
    {
        SetTemporaryFactionKeyword(
            unit,
            "YNNARI",
            false,
            ynnariGrantedByDetachment);
    }

    foreach (SquadController unit
        in battlelineGrantedByDetachment
            .ToArray())
    {
        SetTemporaryFactionKeyword(
            unit,
            "BATTLELINE",
            false,
            battlelineGrantedByDetachment);
    }

    foreach (SquadController unit
        in army)
    {
        if (unit != null)
        {
            unit.AeldariObjectiveControlOverride =
                0;
        }
    }

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

        SetTemporaryFactionKeyword(
            unit,
            "YNNARI",
            shouldHave,
            ynnariGrantedByDetachment);
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

        if (windrider ||
            wraithBattleline ||
            troupe)
        {
            SetTemporaryFactionKeyword(
                unit,
                "BATTLELINE",
                granted,
                battlelineGrantedByDetachment);
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

private static void SetTemporaryFactionKeyword(
        SquadController unit,
        string keyword,
        bool enabled,
        HashSet<SquadController> grants)
    {
        if (unit == null ||
            unit.SourceData == null ||
            grants == null ||
            string.IsNullOrWhiteSpace(
                keyword))
        {
            return;
        }

        if (enabled)
        {
            if (grants.Contains(unit))
                return;

            // Never claim ownership of a keyword imported on the roster.
            if (unit.HasIntrinsicKeyword(
                    keyword))
            {
                return;
            }

            unit.AddFactionKeyword(
                keyword);

            grants.Add(
                unit);

            return;
        }

        if (!grants.Remove(unit))
            return;

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


    internal void RefreshDetachmentState()
    {
        EnsureRulesBinding();
        SynchronizeDetachmentState();
    }

    private void PruneTemporaryKeywordGrants()
    {
        HashSet<SquadController> current =
            new HashSet<SquadController>(
                army.Where(
                    unit =>
                        unit != null));

        ynnariGrantedByDetachment.RemoveWhere(
            unit =>
                unit == null ||
                !current.Contains(unit));

        battlelineGrantedByDetachment.RemoveWhere(
            unit =>
                unit == null ||
                !current.Contains(unit));
    }

}
