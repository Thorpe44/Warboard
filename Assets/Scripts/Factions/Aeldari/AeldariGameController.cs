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
    // v38: names, DP costs and UNIQUE tags live in AeldariDetachmentRuntime.

    private AeldariRulesSystem rules;

    private readonly List<IAeldariDetachmentController>
        detachmentControllers =
            new List<IAeldariDetachmentController>();

    private readonly List<AeldariDetachment>
        lockedDetachments =
            new List<AeldariDetachment>();

    private bool detachmentLocked;
    private string detachmentLockSource = "";

    private AeldariDetachment suggestedDetachment =
        AeldariDetachment.Warhost;

    private RosterImportMetadata rosterMetadata;
    private int rosterMetadataRevision = -1;

    private WarboardRosterManifest rosterManifest;
    private int rosterManifestRevision = -1;

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

    public GameController OwnerGame
    {
        get { return Game; }
    }

    public IReadOnlyList<SquadController> ArmyUnits
    {
        get { return army.ToArray(); }
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

    public IReadOnlyList<AeldariDetachment> LockedDetachments
    {
        get { return lockedDetachments.ToArray(); }
    }

    // Compatibility accessor for code that still expects one detachment.
    public AeldariDetachment LockedDetachment
    {
        get
        {
            return lockedDetachments.Count > 0
                ? lockedDetachments[0]
                : suggestedDetachment;
        }
    }

    public string DetachmentName
    {
        get
        {
            if (lockedDetachments.Count == 0)
                return AeldariDetachmentRuntime.Name(
                    suggestedDetachment);

            return string.Join(
                " + ",
                lockedDetachments
                    .Select(
                        AeldariDetachmentRuntime.Name)
                    .ToArray());
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

    public int DetachmentPointsSpent
    {
        get
        {
            return AeldariDetachmentRuntime.TotalCost(
                lockedDetachments);
        }
    }

    public int DetachmentPointLimit
    {
        get
        {
            return AeldariDetachmentRuntime
                .DetachmentPointLimit(
                    Game != null
                    ? Game.BattleSizeName
                    : "");
        }
    }

    public string ForceDisposition
    {
        get
        {
            return rosterManifest != null
                ? rosterManifest.ForceDisposition
                : "";
        }
    }

    public WarboardRosterManifest RosterManifest
    {
        get { return rosterManifest; }
    }

    public IAeldariDetachmentController
        ActiveDetachmentController
    {
        get
        {
            return detachmentControllers
                .FirstOrDefault();
        }
    }

    public IReadOnlyList<IAeldariDetachmentController>
        ActiveDetachmentControllers
    {
        get { return detachmentControllers.ToArray(); }
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

        AeldariFactionPack11Runtime.Register(this);
    }

public override void RefreshArmy(
    IReadOnlyList<SquadController> units)
{
    base.RefreshArmy(units);

    EnsureRulesBinding();
    PruneTemporaryKeywordGrants();

    ResolveRosterDetachmentMetadata();
    SynchronizeDetachmentState();
    AeldariFactionPack11Runtime.SynchronizePersistent(this);
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

        if (context.Type ==
                GameEventType.UnitSetUp &&
            context.Source != null &&
            string.Equals(
                context.Source.FactionId,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            SynchronizeDetachmentState();
        }

        // The 11e faction pack contains reactions to opponent movement,
        // opponent Fall Back moves and opponent turn/phase endings. Route the
        // global event stream to the faction runtime and let each rule apply
        // its own WHEN/TARGET filters.
        AeldariFactionPack11Runtime.HandleFactionEvent(
            this,
            context);

        bool concernsFaction =
            EventConcernsFaction(context) ||
            context.Type == GameEventType.BattleRoundStarted ||
            context.Type == GameEventType.BattleRoundEnded ||
            context.Type == GameEventType.PhaseStarted ||
            context.Type == GameEventType.PhaseEnded ||
            context.Type == GameEventType.TurnStarted ||
            context.Type == GameEventType.TurnEnded;

        if (!concernsFaction)
            return;

        foreach (IAeldariDetachmentController controller
            in detachmentControllers.ToArray())
        {
            if (controller != null)
                controller.OnGameEvent(context);
        }
    }
public bool ShouldShowDetachmentSelection()
{
    if (army.Count == 0 ||
        Game == null)
    {
        return false;
    }

    ResolveRosterDetachmentMetadata();

    return
        !detachmentLocked &&
        Game.PreGameReady;
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
        return AeldariDetachmentRuntime.Name(
            detachment);
    }

    public int GetDetachmentPointCost(
        AeldariDetachment detachment)
    {
        return AeldariDetachmentRuntime.Cost(
            detachment);
    }


public bool TryLockDetachment(
    AeldariDetachment detachment,
    string source)
{
    return TryLockDetachments(
        new[] { detachment },
        source);
}

public bool TryLockDetachments(
    IEnumerable<AeldariDetachment> detachments,
    string source)
{
    EnsureRulesBinding();

    List<AeldariDetachment> requested =
        detachments != null
        ? detachments.ToList()
        : new List<AeldariDetachment>();

    if (detachmentLocked)
    {
        bool same =
            lockedDetachments.SequenceEqual(
                requested);

        if (same)
            return true;

        selectionError =
            "Detachments are already locked for this battle.";

        return false;
    }

    if (Game != null &&
        Game.DeploymentStarted)
    {
        selectionError =
            "Detachments must be confirmed before deployment begins.";

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

    if (!ValidateDetachmentSet(
            requested,
            out validation))
    {
        selectionError = validation;
        return false;
    }

    lockedDetachments.Clear();
    lockedDetachments.AddRange(requested);

    detachmentLocked = true;

    detachmentLockSource =
        string.IsNullOrWhiteSpace(source)
        ? "Pre-game roster"
        : source;

    selectionError = "";

    AeldariDetachmentRuntime.SetSelected(
        FactionId,
        lockedDetachments);

    // Keep the old single-detachment storage pointed at the first selected
    // detachment while v38's compatibility migration redirects rule checks
    // to AeldariDetachmentRuntime.
    rules.SetDetachment(
        FactionId,
        lockedDetachments[0]);

    LoadDetachmentControllers();
    SynchronizeDetachmentState();

    return true;
}

public bool TryUnlockBeforeDeployment()
{
    if (Game != null &&
        Game.DeploymentStarted)
    {
        selectionError =
            "Detachments cannot be changed after deployment begins.";
        return false;
    }

    ResetDetachmentForRosterChange();
    return true;
}

public bool TryValidateDetachmentSelection(
    IEnumerable<AeldariDetachment> detachments,
    out string message)
{
    return ValidateDetachmentSet(
        detachments != null
        ? detachments.ToList()
        : new List<AeldariDetachment>(),
        out message);
}

public bool HasDetachment(
    AeldariDetachment detachment)
{
    return
        detachmentLocked &&
        lockedDetachments.Contains(detachment);
}
public bool UsesDevotedOfYnnead()
{
    return HasDetachment(
        AeldariDetachment.DevotedOfYnnead);
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
        return SpendBattleFocus(
            amount,
            manoeuvre,
            null);
    }

public bool SpendBattleFocus(
        int amount,
        string manoeuvre,
        SquadController unit)
    {
        string failureReason;

        bool spent =
            battleFocus.Spend(
                amount,
                manoeuvre,
                unit,
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

public void AddBattleFocusTokens(
        int amount)
    {
        battleFocus.AddTokens(amount);
    }

public void EndBattleRound()
    {
        battleFocus.EndBattleRound();
    }
public static string DisplayNameFor(
        AeldariDetachment detachment)
    {
        return AeldariDetachmentRuntime.Name(
            detachment);
    }
private void ResolveRosterDetachmentMetadata()
{
    WarboardRosterManifest manifest =
        RosterTextManifestStore.Get(
            FactionId);

    int manifestRevision =
        manifest != null
        ? manifest.Revision
        : -1;

    if (manifestRevision !=
            rosterManifestRevision)
    {
        if (Game == null ||
            !Game.DeploymentStarted)
        {
            ResetDetachmentForRosterChange();
        }

        rosterManifest = manifest;
        rosterManifestRevision =
            manifestRevision;
    }
    else
    {
        rosterManifest = manifest;
    }

    if (detachmentLocked)
        return;

    // v38 authority: pasted New Recruit roster text. This preserves the
    // roster-level configuration that YellowScribe's compact code discards.
    if (rosterManifest != null)
    {
        List<AeldariDetachment> parsed =
            new List<AeldariDetachment>();

        List<string> unknown =
            new List<string>();

        foreach (string label
            in rosterManifest.Detachments)
        {
            AeldariDetachment detachment;

            if (AeldariDetachmentRuntime.TryParse(
                    label,
                    out detachment))
            {
                parsed.Add(detachment);
            }
            else
            {
                unknown.Add(label);
            }
        }

        if (unknown.Count > 0)
        {
            rosterProbeStatus =
                "Roster text contains unsupported Aeldari detachment name(s): " +
                string.Join(", ", unknown.ToArray()) +
                ".";
            return;
        }

        if (parsed.Count == 0)
        {
            rosterProbeStatus =
                "Roster text was parsed, but it did not contain a DETACHMENT line.";
            return;
        }

        if (TryLockDetachments(
                parsed,
                "Pasted New Recruit roster"))
        {
            int spent =
                AeldariDetachmentRuntime.TotalCost(
                    parsed);

            int limit =
                DetachmentPointLimit;

            rosterProbeStatus =
                "Roster text locked: " +
                string.Join(
                    " + ",
                    parsed
                        .Select(
                            AeldariDetachmentRuntime.Name)
                        .ToArray()) +
                " • " +
                spent +
                (limit > 0
                    ? "/" + limit
                    : "") +
                " DP" +
                (!string.IsNullOrWhiteSpace(
                    rosterManifest.ForceDisposition)
                    ? " • " +
                      rosterManifest.ForceDisposition
                    : "") +
                (!string.IsNullOrWhiteSpace(
                    rosterManifest.Warlord)
                    ? " • Warlord " +
                      rosterManifest.Warlord
                    : "") +
                ".";
        }

        return;
    }

    // Secondary fallback: explicit metadata that genuinely survived the
    // YellowScribe payload. YellowScribe normally strips roster-level
    // configuration, so this path is deliberately conservative.
    RosterImportMetadata current =
        RosterImportMetadataStore.Get(
            FactionId);

    if (current != null &&
        !current.MatchesArmy(army))
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
        rosterMetadata = current;
        rosterMetadataRevision = revision;
    }
    else
    {
        rosterMetadata = current;
    }

    if (rosterMetadata == null)
    {
        rosterProbeStatus =
            "Paste the New Recruit roster text to load detachment configuration automatically, or select detachments manually.";
        return;
    }

    AeldariDetachment detected;
    string detectionMessage;

    RosterDetachmentResolution resolution =
        TryResolveRosterDetachment(
            rosterMetadata,
            out detected,
            out detectionMessage);

    rosterProbeStatus = detectionMessage;

    if (resolution !=
            RosterDetachmentResolution.Detected)
    {
        return;
    }

    if (TryLockDetachments(
            new[] { detected },
            "YellowScribe explicit metadata"))
    {
        rosterProbeStatus =
            "Detachment read from explicit YellowScribe metadata: " +
            DisplayNameFor(detected) +
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
    detachmentLocked = false;
    detachmentLockSource = "";

    lockedDetachments.Clear();
    detachmentControllers.Clear();

    AeldariDetachmentRuntime.Clear(
        FactionId);

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
        // AeldariDetachmentRuntime owns the canonical names in v38. Its
        // parser accepts exact names and names embedded in explicit metadata
        // labels such as "Devoted of Ynnead (Strength From Death)".
        return AeldariDetachmentRuntime.TryParse(
            value,
            out detachment);
    }

private bool ValidateDetachmentSet(
        IReadOnlyList<AeldariDetachment> detachments,
        out string message)
    {
        message = "";

        if (detachments == null ||
            detachments.Count == 0)
        {
            message =
                "Select at least one Aeldari detachment.";
            return false;
        }

        if (detachments.Distinct().Count() !=
            detachments.Count)
        {
            message =
                "The same detachment cannot be selected more than once.";
            return false;
        }

        int acrobaticCount =
            detachments.Count(
                AeldariDetachmentRuntime.IsAcrobatic);

        if (acrobaticCount > 1)
        {
            message =
                "Only one ACROBATIC detachment can be selected.";
            return false;
        }

        int spent =
            AeldariDetachmentRuntime.TotalCost(
                detachments);

        int limit =
            AeldariDetachmentRuntime
                .DetachmentPointLimit(
                    Game != null
                    ? Game.BattleSizeName
                    : "");

        bool incursionThreeDpException =
            Game != null &&
            string.Equals(
                Game.BattleSizeName,
                "Incursion",
                StringComparison.OrdinalIgnoreCase) &&
            detachments.Count == 1 &&
            spent == 3;

        if (limit > 0 &&
            spent > limit &&
            !incursionThreeDpException)
        {
            message =
                "Selected detachments cost " +
                spent +
                "DP, but " +
                (Game != null
                    ? Game.BattleSizeName
                    : "this battle") +
                " allows " +
                limit +
                "DP.";

            return false;
        }

        if (rosterManifest != null &&
            rosterManifest.TotalArmyPoints > 0 &&
            Game != null &&
            Game.BattlePoints > 0 &&
            rosterManifest.TotalArmyPoints >
                Game.BattlePoints)
        {
            message =
                "Roster text is " +
                rosterManifest.TotalArmyPoints +
                "pts, exceeding the selected " +
                Game.BattlePoints +
                "pt battle size.";

            return false;
        }

        if (detachments.Contains(
                AeldariDetachment
                    .DevotedOfYnnead))
        {
            bool hasYvraine =
                army.Any(
                    unit =>
                        NameContains(
                            unit,
                            "Yvraine"));

            bool hasYncarne =
                army.Any(
                    unit =>
                        NameContains(
                            unit,
                            "Yncarne"));

            if (!hasYvraine &&
                !hasYncarne)
            {
                message =
                    "Devoted of Ynnead requires Yvraine and/or the Yncarne in the army.";
                return false;
            }

            if (rosterManifest != null)
            {
                string warlord =
                    rosterManifest.Warlord ?? "";

                bool validWarlord =
                    warlord.IndexOf(
                        "Yvraine",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    warlord.IndexOf(
                        "Yncarne",
                        StringComparison.OrdinalIgnoreCase) >= 0;

                if (!validWarlord)
                {
                    message =
                        "Devoted of Ynnead requires Yvraine or the Yncarne to be the WARLORD; the pasted roster lists '" +
                        (string.IsNullOrWhiteSpace(warlord)
                            ? "no Warlord"
                            : warlord) +
                        "'.";

                    return false;
                }
            }
        }

        if (rosterManifest != null &&
            !string.IsNullOrWhiteSpace(
                rosterManifest.FactionKeyword) &&
            rosterManifest.FactionKeyword.IndexOf(
                "Aeldari",
                StringComparison.OrdinalIgnoreCase) < 0 &&
            rosterManifest.FactionKeyword.IndexOf(
                "Asuryani",
                StringComparison.OrdinalIgnoreCase) < 0)
        {
            message =
                "The pasted roster faction does not appear to be Aeldari: " +
                rosterManifest.FactionKeyword +
                ".";
            return false;
        }

        return true;
    }
private void LoadDetachmentControllers()
    {
        if (!detachmentLocked)
        {
            detachmentControllers.Clear();
            return;
        }

        bool alreadyCorrect =
            detachmentControllers.Count ==
                lockedDetachments.Count &&
            detachmentControllers
                .Select(
                    controller =>
                        controller.Detachment)
                .SequenceEqual(
                    lockedDetachments);

        if (alreadyCorrect)
            return;

        detachmentControllers.Clear();

        foreach (AeldariDetachment detachment
            in lockedDetachments)
        {
            IAeldariDetachmentController controller =
                AeldariDetachmentControllerFactory
                    .Create(detachment);

            if (controller == null)
                continue;

            controller.Initialize(this);
            detachmentControllers.Add(controller);
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

    if (!detachmentLocked ||
        lockedDetachments.Count == 0)
    {
        AeldariDetachmentRuntime.Clear(
            FactionId);
        ClearTemporaryDetachmentState();
        return;
    }

    AeldariDetachmentRuntime.SetSelected(
        FactionId,
        lockedDetachments);

    // Legacy primary pointer; v38 compatibility migration makes
    // AeldariRulesSystem.DetachmentIs multi-aware.
    rules.SetDetachment(
        FactionId,
        lockedDetachments[0]);

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

        SynchronizeYnnariKeyword(unit);
        SynchronizeBattlelineKeyword(unit);
        SynchronizeObjectiveControl(unit);
    }

    LoadDetachmentControllers();
    AeldariFactionPack11Runtime.SynchronizePersistent(this);
    if (Game != null)
        Game.Aeldari11SynchronizeFaction(this);
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
        SquadController unit)
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
            HasDetachment(
                AeldariDetachment
                    .DevotedOfYnnead);

        SetTemporaryFactionKeyword(
            unit,
            "YNNARI",
            shouldHave,
            ynnariGrantedByDetachment);
    }
private void SynchronizeBattlelineKeyword(
        SquadController unit)
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
            (HasDetachment(
                 AeldariDetachment
                     .WindriderHost) &&
             windrider) ||
            (HasDetachment(
                 AeldariDetachment
                     .SpiritConclave) &&
             wraithBattleline) ||
            ((HasDetachment(
                  AeldariDetachment
                      .GhostsOfTheWebway) ||
              HasDetachment(
                  AeldariDetachment
                      .SerpentsBrood)) &&
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
        SquadController unit)
    {
        bool troupe =
            unit.HasIntrinsicKeyword(
                "troupe") ||
            NameContains(
                unit,
                "Troupe");

        bool troupeOcTwo =
            troupe &&
            (HasDetachment(
                 AeldariDetachment
                     .GhostsOfTheWebway) ||
             HasDetachment(
                 AeldariDetachment
                     .SerpentsBrood));

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
