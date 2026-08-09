using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Edition 11 Adeptus Custodes faction controller.
/// New Recruit roster text is authoritative for detachment configuration;
/// YellowScribe remains the datasheet/profile source.
/// </summary>
public sealed class CustodesGameController :
    FactionGameControllerBase,
    IFactionPreGameController
{
    private readonly List<CustodesDetachment>
        lockedDetachments =
            new List<CustodesDetachment>();

    private readonly List<ICustodesDetachmentController>
        detachmentControllers =
            new List<ICustodesDetachmentController>();

    private readonly HashSet<SquadController>
        battlelineGrantedByDetachment =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        characterGrantedBySolar =
            new HashSet<SquadController>();

    private readonly HashSet<SquadController>
        selectedSolarWalkers =
            new HashSet<SquadController>();

    private bool detachmentLocked;
    private string detachmentLockSource = "";
    private string selectionError = "";
    private string rosterProbeStatus = "";

    private WarboardRosterManifest rosterManifest;
    private int rosterManifestRevision = -1;

    private bool solarWalkerChoiceConfirmed;

    public override string DisplayName
    {
        get { return "Adeptus Custodes"; }
    }

    public GameController OwnerGame
    {
        get { return Game; }
    }

    public IReadOnlyList<SquadController> ArmyUnits
    {
        get { return army.ToArray(); }
    }

    public IReadOnlyList<CustodesDetachment> LockedDetachments
    {
        get { return lockedDetachments.ToArray(); }
    }

    public IReadOnlyList<ICustodesDetachmentController>
        ActiveDetachmentControllers
    {
        get { return detachmentControllers.ToArray(); }
    }

    public bool DetachmentLocked
    {
        get { return detachmentLocked; }
    }

    public string DetachmentLockSource
    {
        get { return detachmentLockSource; }
    }

    public string SelectionError
    {
        get { return selectionError; }
    }

    public string RosterProbeStatus
    {
        get { return rosterProbeStatus; }
    }

    public WarboardRosterManifest RosterManifest
    {
        get { return rosterManifest; }
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

    public int DetachmentPointsSpent
    {
        get
        {
            return CustodesDetachmentRuntime.TotalCost(
                lockedDetachments);
        }
    }

    public int DetachmentPointLimit
    {
        get
        {
            return CustodesDetachmentRuntime
                .DetachmentPointLimit(
                    Game != null
                    ? Game.BattleSizeName
                    : "");
        }
    }

    public string DetachmentName
    {
        get
        {
            return lockedDetachments.Count == 0
                ? "Not selected"
                : string.Join(
                    " + ",
                    lockedDetachments
                        .Select(CustodesDetachmentRuntime.Name)
                        .ToArray());
        }
    }

    public bool RequiresSolarWalkerChoice
    {
        get
        {
            return
                detachmentLocked &&
                HasDetachment(
                    CustodesDetachment.SolarSpearhead) &&
                EligibleSolarWalkers().Count > 0 &&
                !solarWalkerChoiceConfirmed;
        }
    }

    public bool SolarWalkerChoiceConfirmed
    {
        get { return solarWalkerChoiceConfirmed; }
    }

    public override void Initialize(
        GameController game,
        string factionId)
    {
        base.Initialize(game, factionId);
        CustodesFactionPack11Runtime.Register(this);
    }

    public override void RefreshArmy(
        IReadOnlyList<SquadController> units)
    {
        base.RefreshArmy(units);

        PruneTemporaryKeywordGrants();
        ResolveRosterDetachmentMetadata();
        SynchronizePersistentState();
        CustodesFactionPack11Runtime.Register(this);
    }

    public override void OnGameEvent(
        GameEventContext context)
    {
        if (context == null)
            return;

        CustodesFactionPack11Runtime.HandleFactionEvent(
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

        foreach (ICustodesDetachmentController controller
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
                (detachmentLocked &&
                 !RequiresSolarWalkerChoice);
        }
    }

    public string DeploymentBlockReason
    {
        get
        {
            if (IsReadyForDeployment)
                return "";

            if (!detachmentLocked)
            {
                return
                    FactionId +
                    " Adeptus Custodes detachment configuration has not been confirmed.";
            }

            if (RequiresSolarWalkerChoice)
            {
                return
                    FactionId +
                    " Solar Spearhead must confirm which (if any) up to two WALKER models gain CHARACTER before deployment.";
            }

            return
                FactionId +
                " Adeptus Custodes pre-game setup is incomplete.";
        }
    }

    public CustodesDetachment[] AvailableDetachments()
    {
        return
            (CustodesDetachment[])
            Enum.GetValues(
                typeof(CustodesDetachment));
    }

    public string GetDetachmentDisplayName(
        CustodesDetachment detachment)
    {
        return CustodesDetachmentRuntime.Name(detachment);
    }

    public int GetDetachmentPointCost(
        CustodesDetachment detachment)
    {
        return CustodesDetachmentRuntime.Cost(detachment);
    }

    public bool HasDetachment(
        CustodesDetachment detachment)
    {
        return
            detachmentLocked &&
            lockedDetachments.Contains(detachment);
    }

    public bool TryValidateDetachmentSelection(
        IEnumerable<CustodesDetachment> detachments,
        out string message)
    {
        return ValidateDetachmentSet(
            detachments != null
                ? detachments.ToList()
                : new List<CustodesDetachment>(),
            out message);
    }

    public bool TryLockDetachments(
        IEnumerable<CustodesDetachment> detachments,
        string source)
    {
        List<CustodesDetachment> requested =
            detachments != null
            ? detachments.Distinct().ToList()
            : new List<CustodesDetachment>();

        if (detachmentLocked)
        {
            if (lockedDetachments.SequenceEqual(requested))
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

        CustodesDetachmentRuntime.SetSelected(
            FactionId,
            lockedDetachments);

        LoadDetachmentControllers();

        if (!HasDetachment(
                CustodesDetachment.SolarSpearhead))
        {
            solarWalkerChoiceConfirmed = true;
            selectedSolarWalkers.Clear();
        }
        else
        {
            solarWalkerChoiceConfirmed =
                EligibleSolarWalkers().Count == 0;
        }

        SynchronizePersistentState();
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

        ResetForRosterChange();
        return true;
    }

    public List<SquadController> EligibleSolarWalkers()
    {
        return army
            .Where(unit =>
                unit != null &&
                !unit.IsAttachedLeader &&
                unit.HasIntrinsicKeyword(
                    "adeptus custodes") &&
                unit.HasIntrinsicKeyword(
                    "walker"))
            .ToList();
    }

    public bool IsSolarWalkerSelected(
        SquadController unit)
    {
        return
            unit != null &&
            selectedSolarWalkers.Contains(unit);
    }

    public bool ToggleSolarWalker(
        SquadController unit)
    {
        if (unit == null ||
            !EligibleSolarWalkers().Contains(unit) ||
            Game == null ||
            Game.DeploymentStarted)
        {
            return false;
        }

        if (selectedSolarWalkers.Contains(unit))
        {
            selectedSolarWalkers.Remove(unit);
            return true;
        }

        if (selectedSolarWalkers.Count >= 2)
        {
            selectionError =
                "Solar Spearhead can make at most two ADEPTUS CUSTODES WALKER models CHARACTER.";
            return false;
        }

        selectedSolarWalkers.Add(unit);
        selectionError = "";
        return true;
    }

    public bool ConfirmSolarWalkerSelection()
    {
        if (!HasDetachment(
                CustodesDetachment.SolarSpearhead))
        {
            solarWalkerChoiceConfirmed = true;
            return true;
        }

        if (Game != null &&
            Game.DeploymentStarted)
        {
            selectionError =
                "Solar Spearhead WALKER CHARACTER choices must be confirmed before deployment.";
            return false;
        }

        if (selectedSolarWalkers.Count > 2)
        {
            selectionError =
                "Select no more than two WALKER units.";
            return false;
        }

        solarWalkerChoiceConfirmed = true;
        SynchronizePersistentState();
        selectionError = "";
        return true;
    }

    private bool ValidateDetachmentSet(
        List<CustodesDetachment> requested,
        out string message)
    {
        message = "";

        if (requested == null ||
            requested.Count == 0)
        {
            message =
                "Select at least one Adeptus Custodes detachment.";
            return false;
        }

        if (requested.Distinct().Count() !=
            requested.Count)
        {
            message =
                "The same detachment cannot be selected twice.";
            return false;
        }

        if (requested.Count(
                CustodesDetachmentRuntime.IsArmoury) > 1)
        {
            message =
                "Only one ARMOURY detachment can be selected.";
            return false;
        }

        if (requested.Count(
                CustodesDetachmentRuntime.IsLions) > 1)
        {
            message =
                "Only one LIONS detachment can be selected.";
            return false;
        }

        int spent =
            CustodesDetachmentRuntime.TotalCost(
                requested);

        int limit = DetachmentPointLimit;

        bool incursionSingleThree =
            Game != null &&
            string.Equals(
                Game.BattleSizeName,
                "Incursion",
                StringComparison.OrdinalIgnoreCase) &&
            requested.Count == 1 &&
            spent == 3;

        if (limit > 0 &&
            spent > limit &&
            !incursionSingleThree)
        {
            message =
                "Selected detachments cost " +
                spent +
                " DP, but this battle allows " +
                limit +
                " DP.";
            return false;
        }

        if (rosterManifest != null &&
            !string.IsNullOrWhiteSpace(
                rosterManifest.FactionKeyword) &&
            rosterManifest.FactionKeyword.IndexOf(
                "Adeptus Custodes",
                StringComparison.OrdinalIgnoreCase) < 0 &&
            rosterManifest.FactionKeyword.IndexOf(
                "Custodes",
                StringComparison.OrdinalIgnoreCase) < 0)
        {
            message =
                "The pasted roster does not identify itself as Adeptus Custodes.";
            return false;
        }

        if (Game != null &&
            rosterManifest != null &&
            rosterManifest.TotalArmyPoints > 0 &&
            Game.BattlePoints > 0 &&
            rosterManifest.TotalArmyPoints >
                Game.BattlePoints)
        {
            message =
                "Pasted roster is " +
                rosterManifest.TotalArmyPoints +
                " points, above the " +
                Game.BattlePoints +
                " point battle limit.";
            return false;
        }

        return true;
    }

    private void ResolveRosterDetachmentMetadata()
    {
        WarboardRosterManifest manifest =
            RosterTextManifestStore.Get(
                FactionId);

        int revision =
            manifest != null
            ? manifest.Revision
            : -1;

        if (revision != rosterManifestRevision)
        {
            if (Game == null ||
                !Game.DeploymentStarted)
            {
                ResetForRosterChange();
            }

            rosterManifestRevision = revision;
        }

        rosterManifest = manifest;

        if (detachmentLocked)
            return;

        if (rosterManifest == null)
        {
            rosterProbeStatus =
                "Paste the New Recruit roster text to load Custodes detachments automatically, or select them manually.";
            return;
        }

        List<CustodesDetachment> parsed =
            new List<CustodesDetachment>();

        List<string> unknown =
            new List<string>();

        foreach (string label
            in rosterManifest.Detachments)
        {
            CustodesDetachment detachment;

            if (CustodesDetachmentRuntime.TryParse(
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
                "Unsupported Custodes detachment name(s): " +
                string.Join(
                    ", ",
                    unknown.ToArray()) +
                ".";
            return;
        }

        if (parsed.Count == 0)
        {
            rosterProbeStatus =
                "Roster text was parsed, but it did not contain a supported DETACHMENT line.";
            return;
        }

        if (TryLockDetachments(
                parsed,
                "Pasted New Recruit roster"))
        {
            int spent =
                CustodesDetachmentRuntime.TotalCost(
                    parsed);

            int limit = DetachmentPointLimit;

            rosterProbeStatus =
                "Roster text locked: " +
                string.Join(
                    " + ",
                    parsed
                        .Select(
                            CustodesDetachmentRuntime.Name)
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
    }

    private void LoadDetachmentControllers()
    {
        detachmentControllers.Clear();

        foreach (CustodesDetachment detachment
            in lockedDetachments)
        {
            ICustodesDetachmentController controller =
                CustodesDetachmentControllerFactory.Create(
                    detachment);

            if (controller == null)
                continue;

            controller.Initialize(this);
            detachmentControllers.Add(controller);
        }
    }

    private void SynchronizePersistentState()
    {
        if (!detachmentLocked)
            return;

        foreach (SquadController unit
            in army)
        {
            if (unit == null)
                continue;

            bool prosecutors =
                unit.HasIntrinsicKeyword(
                    "prosecutors") ||
                (!string.IsNullOrWhiteSpace(
                     unit.DisplayName) &&
                 unit.DisplayName.IndexOf(
                     "Prosecutors",
                     StringComparison.OrdinalIgnoreCase) >= 0);

            SetTemporaryFactionKeyword(
                unit,
                "BATTLELINE",
                HasDetachment(
                    CustodesDetachment.NullMaidenVigil) &&
                prosecutors,
                battlelineGrantedByDetachment);

            SetTemporaryFactionKeyword(
                unit,
                "CHARACTER",
                HasDetachment(
                    CustodesDetachment.SolarSpearhead) &&
                solarWalkerChoiceConfirmed &&
                selectedSolarWalkers.Contains(unit),
                characterGrantedBySolar);
        }
    }

    private void ResetForRosterChange()
    {
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
            in characterGrantedBySolar
                .ToArray())
        {
            SetTemporaryFactionKeyword(
                unit,
                "CHARACTER",
                false,
                characterGrantedBySolar);
        }

        lockedDetachments.Clear();
        detachmentControllers.Clear();
        selectedSolarWalkers.Clear();
        detachmentLocked = false;
        detachmentLockSource = "";
        selectionError = "";
        solarWalkerChoiceConfirmed = false;

        CustodesDetachmentRuntime.Clear(
            FactionId);
    }

    private void PruneTemporaryKeywordGrants()
    {
        HashSet<SquadController> current =
            new HashSet<SquadController>(
                army.Where(unit => unit != null));

        battlelineGrantedByDetachment.RemoveWhere(
            unit =>
                unit == null ||
                !current.Contains(unit));

        characterGrantedBySolar.RemoveWhere(
            unit =>
                unit == null ||
                !current.Contains(unit));

        selectedSolarWalkers.RemoveWhere(
            unit =>
                unit == null ||
                !current.Contains(unit));
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
            string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        if (enabled)
        {
            if (grants.Contains(unit) ||
                unit.HasIntrinsicKeyword(keyword))
            {
                return;
            }

            unit.AddFactionKeyword(keyword);
            grants.Add(unit);
            return;
        }

        if (!grants.Remove(unit))
            return;

        List<string> values =
            new List<string>(
                unit.SourceData.factionKeywords ??
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
