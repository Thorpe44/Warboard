using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Edition 11 Necrons faction controller.
/// New Recruit roster text is authoritative for detachment configuration;
/// YellowScribe remains the datasheet/profile source.
/// </summary>
public sealed class NecronGameController :
    FactionGameControllerBase,
    IFactionPreGameController
{
    private readonly List<NecronDetachment>
        lockedDetachments =
            new List<NecronDetachment>();

    private readonly List<INecronDetachmentController>
        detachmentControllers =
            new List<INecronDetachmentController>();

    private bool detachmentLocked;
    private string detachmentLockSource = "";
    private string selectionError = "";
    private string rosterProbeStatus = "";

    private WarboardRosterManifest rosterManifest;
    private int rosterManifestRevision = -1;

    public override string DisplayName
    {
        get { return "Necrons"; }
    }

    public GameController OwnerGame
    {
        get { return Game; }
    }

    public IReadOnlyList<SquadController> ArmyUnits
    {
        get { return army.ToArray(); }
    }

    public IReadOnlyList<NecronDetachment> LockedDetachments
    {
        get { return lockedDetachments.ToArray(); }
    }

    public IReadOnlyList<INecronDetachmentController>
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
            return NecronDetachmentRuntime.TotalCost(
                lockedDetachments);
        }
    }

    public int DetachmentPointLimit
    {
        get
        {
            return NecronDetachmentRuntime
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
                        .Select(NecronDetachmentRuntime.Name)
                        .ToArray());
        }
    }

    public override void Initialize(
        GameController game,
        string factionId)
    {
        base.Initialize(game, factionId);
        NecronsFactionPack11Runtime.Register(this);
    }

    public override void RefreshArmy(
        IReadOnlyList<SquadController> units)
    {
        base.RefreshArmy(units);

        ResolveRosterDetachmentMetadata();
        NecronsFactionPack11Runtime.Register(this);
    }

    public override void OnGameEvent(
        GameEventContext context)
    {
        if (context == null)
            return;

        NecronsFactionPack11Runtime.HandleFactionEvent(
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

        foreach (INecronDetachmentController controller
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
                " Necrons detachment configuration has not been confirmed.";
        }
    }

    public NecronDetachment[] AvailableDetachments()
    {
        return
            (NecronDetachment[])
            Enum.GetValues(
                typeof(NecronDetachment));
    }

    public string GetDetachmentDisplayName(
        NecronDetachment detachment)
    {
        return NecronDetachmentRuntime.Name(detachment);
    }

    public int GetDetachmentPointCost(
        NecronDetachment detachment)
    {
        return NecronDetachmentRuntime.Cost(detachment);
    }

    public bool HasDetachment(
        NecronDetachment detachment)
    {
        return
            detachmentLocked &&
            lockedDetachments.Contains(detachment);
    }

    public bool TryValidateDetachmentSelection(
        IEnumerable<NecronDetachment> detachments,
        out string message)
    {
        return ValidateDetachmentSet(
            detachments != null
                ? detachments.ToList()
                : new List<NecronDetachment>(),
            out message);
    }

    public bool TryLockDetachments(
        IEnumerable<NecronDetachment> detachments,
        string source)
    {
        List<NecronDetachment> requested =
            detachments != null
            ? detachments.Distinct().ToList()
            : new List<NecronDetachment>();

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

        NecronDetachmentRuntime.SetSelected(
            FactionId,
            lockedDetachments);

        LoadDetachmentControllers();
        NecronsFactionPack11Runtime.Register(this);

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

    private bool ValidateDetachmentSet(
        List<NecronDetachment> requested,
        out string message)
    {
        message = "";

        if (requested == null ||
            requested.Count == 0)
        {
            message =
                "Select at least one Necrons detachment.";
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
                NecronDetachmentRuntime.IsDynasty) > 1)
        {
            message =
                "Only one DYNASTY detachment can be selected.";
            return false;
        }

        if (requested.Count(
                NecronDetachmentRuntime.IsHypercrypt) > 1)
        {
            message =
                "Only one HYPERCRYPT detachment can be selected.";
            return false;
        }

        int spent =
            NecronDetachmentRuntime.TotalCost(
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
                "Necron",
                StringComparison.OrdinalIgnoreCase) < 0)
        {
            message =
                "The pasted roster does not identify itself as Necrons.";
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
                "Paste the New Recruit roster text to load Necrons detachments automatically, or select them manually.";
            return;
        }

        List<NecronDetachment> parsed =
            new List<NecronDetachment>();

        List<string> unknown =
            new List<string>();

        foreach (string label
            in rosterManifest.Detachments)
        {
            NecronDetachment detachment;

            if (NecronDetachmentRuntime.TryParse(
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
                "Unsupported Necrons detachment name(s): " +
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
                NecronDetachmentRuntime.TotalCost(
                    parsed);

            int limit = DetachmentPointLimit;

            rosterProbeStatus =
                "Roster text locked: " +
                string.Join(
                    " + ",
                    parsed
                        .Select(
                            NecronDetachmentRuntime.Name)
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

        foreach (NecronDetachment detachment
            in lockedDetachments)
        {
            INecronDetachmentController controller =
                NecronDetachmentControllerFactory.Create(
                    detachment);

            if (controller == null)
                continue;

            controller.Initialize(this);
            detachmentControllers.Add(controller);
        }
    }

    private void ResetForRosterChange()
    {
        lockedDetachments.Clear();
        detachmentControllers.Clear();
        detachmentLocked = false;
        detachmentLockSource = "";
        selectionError = "";

        NecronDetachmentRuntime.Clear(
            FactionId);
    }
}
