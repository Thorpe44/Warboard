using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Shared Edition 11 controller for the post-v45 data-driven faction packs.
/// It owns detachment locking and the small amount of state that cannot live in
/// stateless attack hooks (Waaagh!, Oath target, Hyper-adaptation, doctrines).
/// </summary>
public sealed partial class StandardFactionGameController :
    FactionGameControllerBase,
    IFactionPreGameController
{
    private string packId = "";
    private StandardFactionPack11Data pack;

    private readonly List<string>
        selectedDetachments =
            new List<string>();

    private string rosterProbeStatus = "";
    private string selectionError = "";
    private bool detachmentLocked;
    private bool unsupportedSupplement;
    private string chapterValidationError = "";

    private int commandSerial;

    private bool waaaghUsed;
    private bool waaaghActive;
    private int waaaghCommandSerial = -1;
    private bool bullySecondWaaaghUsed;
    private bool currentWaaaghBullyOnly;

    private bool shadowInTheWarpUsed;

    private SquadController oathTarget;
    private bool oathSelectionRequired;

    private SquadController preyTarget;
    private bool preySelectionRequired;

    private ObjectiveController lootObjective;
    private bool lootSelectionRequired;

    private string hyperAdaptation = "";
    private bool hyperAdaptationRequired;

    private string combatDoctrine = "";
    private readonly HashSet<string>
        combatDoctrinesUsed =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

    private string psychicDiscipline = "";
    private bool psychicDisciplineRequired;

    private string synapticImperative = "";
    private readonly HashSet<string>
        synapticImperativesUsed =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<SquadController>
        rapidDropUnits =
            new HashSet<SquadController>();

    private bool rapidDropLocked;

    private readonly HashSet<SquadController>
        disembarkedThisTurn =
            new HashSet<SquadController>();

    private bool extremisUsed;
    private bool extremisActive;
    private int extremisCommandSerial = -1;

    public StandardFactionPack11Data Pack
    {
        get { return pack; }
    }

    public string PackId
    {
        get { return packId; }
    }

    public IReadOnlyList<SquadController> ArmyUnits
    {
        get { return army.ToArray(); }
    }

    public override string DisplayName
    {
        get
        {
            return pack != null &&
                   !string.IsNullOrWhiteSpace(
                       pack.displayName)
                ? pack.displayName
                : "Standard Faction";
        }
    }

    public bool DetachmentLocked
    {
        get { return detachmentLocked; }
    }

    public string SelectionError
    {
        get { return selectionError; }
    }

    public string RosterProbeStatus
    {
        get { return rosterProbeStatus; }
    }

    public IReadOnlyList<string>
        SelectedDetachments
    {
        get
        {
            return selectedDetachments
                .ToArray();
        }
    }

    public int DetachmentPointsSpent
    {
        get
        {
            if (pack == null ||
                pack.detachments == null)
            {
                return 0;
            }

            HashSet<string> selected =
                new HashSet<string>(
                    selectedDetachments
                        .Select(
                            StandardFactionPack11
                                .Normalize),
                    StringComparer.OrdinalIgnoreCase
                );

            return pack.detachments
                .Where(
                    value =>
                        value != null &&
                        selected.Contains(
                            StandardFactionPack11
                                .Normalize(
                                    value.name)))
                .Sum(value => value.dp);
        }
    }

    public int DetachmentPointLimit
    {
        get
        {
            if (Game == null)
                return -1;

            if (string.Equals(
                    Game.StandardBattleSizeName,
                    "Incursion",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (string.Equals(
                    Game.StandardBattleSizeName,
                    "Strike Force",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            return -1;
        }
    }

    public bool IsReadyForDeployment
    {
        get
        {
            if (unsupportedSupplement ||
                !string.IsNullOrWhiteSpace(
                    chapterValidationError))
            {
                return false;
            }

            if (!detachmentLocked)
                return false;

            // v47: never silently ignore a taken Enhancement because New
            // Recruit/YellowScribe did not preserve its bearer relationship.
            // Any Enhancement found in the roster must be assigned to an
            // actual unit before deployment can begin.
            if (WarboardEnhancementRegistry47
                .ForFaction(FactionId)
                .Any(assignment =>
                    assignment != null &&
                    assignment.Bearer == null))
            {
                return false;
            }

            if (HasDetachment(
                    "ORBITAL ASSAULT FORCE"))
            {
                return
                    rapidDropLocked &&
                    rapidDropUnits.Count ==
                    RequiredRapidDropUnits();
            }

            return true;
        }
    }

    public string DeploymentBlockReason
    {
        get
        {
            if (unsupportedSupplement)
            {
                return
                    "Space Marines supplement content was detected. v46 installs the base Space Marines faction pack only; remove Blood Angels, Dark Angels, Black Templars, Deathwatch or Space Wolves supplement units/rules for this battle.";
            }

            if (!string.IsNullOrWhiteSpace(
                    chapterValidationError))
            {
                return chapterValidationError;
            }

            if (!detachmentLocked)
            {
                return
                    DisplayName +
                    " detachment selection is not locked.";
            }

            int unassignedEnhancements =
                WarboardEnhancementRegistry47
                    .ForFaction(FactionId)
                    .Count(assignment =>
                        assignment != null &&
                        assignment.Bearer == null);

            if (unassignedEnhancements > 0)
            {
                return
                    unassignedEnhancements +
                    " Enhancement" +
                    (unassignedEnhancements == 1
                        ? " is"
                        : "s are") +
                    " missing a bearer. Open the faction rules panel, choose ENHANCEMENTS and assign each taken Enhancement before deployment.";
            }

            if (HasDetachment(
                    "ORBITAL ASSAULT FORCE") &&
                (!rapidDropLocked ||
                 rapidDropUnits.Count !=
                    RequiredRapidDropUnits()))
            {
                return
                    "Orbital Assault Force must select exactly " +
                    RequiredRapidDropUnits() +
                    " non-TITANIC unit(s) to gain Deep Strike before deployment.";
            }

            return "";
        }
    }

    public bool WaaaghUsed
    {
        get { return waaaghUsed; }
    }

    public bool WaaaghActive
    {
        get
        {
            return
                waaaghActive &&
                waaaghCommandSerial ==
                    commandSerial;
        }
    }

    public bool ShadowInTheWarpUsed
    {
        get { return shadowInTheWarpUsed; }
    }

    public bool CanUseShadowInTheWarp
    {
        get
        {
            return
                packId == "tyranids" &&
                !shadowInTheWarpUsed &&
                army.Any(
                    unit =>
                        unit != null &&
                        unit.IsAlive &&
                        unit.IsOnBattlefield &&
                        unit.HasIntrinsicKeyword(
                            "TYRANIDS"));
        }
    }

    public SquadController OathTarget
    {
        get { return oathTarget; }
    }

    public bool OathSelectionRequired
    {
        get { return oathSelectionRequired; }
    }

    public SquadController PreyTarget
    {
        get { return preyTarget; }
    }

    public bool PreySelectionRequired
    {
        get { return preySelectionRequired; }
    }

    public ObjectiveController LootObjective
    {
        get { return lootObjective; }
    }

    public bool LootSelectionRequired
    {
        get { return lootSelectionRequired; }
    }

    public string HyperAdaptation
    {
        get { return hyperAdaptation; }
    }

    public bool HyperAdaptationRequired
    {
        get { return hyperAdaptationRequired; }
    }

    public string CombatDoctrine
    {
        get { return combatDoctrine; }
    }

    public IReadOnlyCollection<string>
        CombatDoctrinesUsed
    {
        get { return combatDoctrinesUsed; }
    }

    public string PsychicDiscipline
    {
        get { return psychicDiscipline; }
    }

    public bool PsychicDisciplineRequired
    {
        get { return psychicDisciplineRequired; }
    }

    public string SynapticImperative
    {
        get { return synapticImperative; }
    }

    public IReadOnlyCollection<string>
        SynapticImperativesUsed
    {
        get { return synapticImperativesUsed; }
    }

    public bool ExtremisUsed
    {
        get { return extremisUsed; }
    }

    public bool ExtremisActive
    {
        get
        {
            return
                extremisActive &&
                extremisCommandSerial ==
                    commandSerial;
        }
    }

    public bool DisembarkedThisTurn(
        SquadController unit)
    {
        if (unit == null)
            return false;

        return disembarkedThisTurn.Contains(
            unit.JoinedActionController());
    }

    public override void RefreshArmy(
        IReadOnlyList<SquadController> units)
    {
        base.RefreshArmy(units);

        string detected =
            WarboardFactionExtensionHub
                .DetectPackId(army);

        if (!string.Equals(
                detected,
                packId,
                StringComparison.OrdinalIgnoreCase))
        {
            packId = detected ?? "";
            pack =
                StandardFactionPack11.Get(
                    packId
                );

            selectedDetachments.Clear();
            detachmentLocked = false;
            selectionError = "";
            rosterProbeStatus = "";
            rapidDropUnits.Clear();
            rapidDropLocked = false;
        }

        unsupportedSupplement =
            packId == "space_marines" &&
            army.Any(
                unit =>
                    unit != null &&
                    (unit.HasIntrinsicKeyword(
                        "black templars") ||
                     unit.HasIntrinsicKeyword(
                        "blood angels") ||
                     unit.HasIntrinsicKeyword(
                        "dark angels") ||
                     unit.HasIntrinsicKeyword(
                        "deathwatch") ||
                     unit.HasIntrinsicKeyword(
                        "space wolves")));

        chapterValidationError =
            ValidateSpaceMarineChapterMix();

        ProbeRosterManifest();
        ApplyStaticKeywords();

        WarboardEnhancementRegistry47.SyncFromController(
            this
        );

        V47RefreshArmy();
    }

    private string ValidateSpaceMarineChapterMix()
    {
        if (packId != "space_marines")
            return "";

        HashSet<string> chapters =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (SquadController unit
            in army)
        {
            if (unit == null ||
                unit.SourceData == null ||
                !unit.HasIntrinsicKeyword(
                    "adeptus astartes"))
            {
                continue;
            }

            foreach (string factionKeyword
                in unit.SourceData
                    .factionKeywords ??
                   new string[0])
            {
                if (string.IsNullOrWhiteSpace(
                        factionKeyword) ||
                    string.Equals(
                        factionKeyword,
                        "ADEPTUS ASTARTES",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        factionKeyword,
                        "IMPERIUM",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                chapters.Add(
                    factionKeyword.Trim()
                );
            }
        }

        if (chapters.Count <= 1)
            return "";

        return
            "Space Marines Chapter restriction: this army contains units from more than one Chapter (" +
            string.Join(
                ", ",
                chapters
                    .OrderBy(
                        value => value)
                    .ToArray()) +
            "). The supplied base Space Marines Faction Pack does not permit mixing Chapters.";
    }

    private void ProbeRosterManifest()
    {
        if (detachmentLocked ||
            pack == null)
        {
            return;
        }

        WarboardRosterManifest manifest =
            RosterTextManifestStore.Get(
                FactionId
            );

        if (manifest == null)
        {
            rosterProbeStatus =
                "No New Recruit manifest detected. Select detachments manually or paste the roster text.";
            return;
        }

        List<string> found =
            new List<string>();

        foreach (string text
            in manifest.Detachments ??
               new List<string>())
        {
            StandardFactionDetachment11
                detachment =
                    StandardFactionPack11
                        .FindDetachment(
                            pack,
                            text
                        );

            if (detachment != null &&
                !found.Any(
                    value =>
                        string.Equals(
                            value,
                            detachment.name,
                            StringComparison
                                .OrdinalIgnoreCase)))
            {
                found.Add(
                    detachment.name
                );
            }
        }

        if (found.Count == 0)
        {
            rosterProbeStatus =
                "New Recruit roster parsed, but no installed " +
                DisplayName +
                " detachment name was recognised. Manual selection remains available.";
            return;
        }

        string validation;

        if (!TryValidateDetachmentSelection(
                found,
                out validation))
        {
            rosterProbeStatus =
                "New Recruit detachments were found but are not legal for this battle size: " +
                validation;
            return;
        }

        TryLockDetachments(
            found,
            "New Recruit roster"
        );

        rosterProbeStatus =
            "New Recruit auto-detected: " +
            string.Join(
                " + ",
                selectedDetachments.ToArray()
            );
    }

    public IEnumerable<
        StandardFactionDetachment11
    > AvailableDetachments()
    {
        return
            pack != null &&
            pack.detachments != null
            ? pack.detachments
                .Where(
                    value =>
                        value != null)
            : Enumerable.Empty<
                StandardFactionDetachment11>();
    }

    public bool HasDetachment(
        string name)
    {
        string wanted =
            StandardFactionPack11
                .Normalize(name);

        return selectedDetachments
            .Any(
                value =>
                    StandardFactionPack11
                        .Normalize(value) ==
                    wanted
            );
    }

    public StandardFactionDetachment11
        GetDetachment(
            string name)
    {
        if (pack == null ||
            pack.detachments == null)
        {
            return null;
        }

        string wanted =
            StandardFactionPack11
                .Normalize(name);

        return pack.detachments
            .FirstOrDefault(
                value =>
                    value != null &&
                    StandardFactionPack11
                        .Normalize(
                            value.name) ==
                    wanted
            );
    }

    public bool TryValidateDetachmentSelection(
        IEnumerable<string> selection,
        out string reason)
    {
        reason = "";

        if (pack == null ||
            pack.detachments == null)
        {
            reason =
                "Faction pack data is not loaded.";
            return false;
        }

        List<string> selected =
            (selection ??
                Enumerable.Empty<string>())
            .Where(
                value =>
                    !string.IsNullOrWhiteSpace(
                        value))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selected.Count == 0)
        {
            reason =
                "Select at least one Detachment.";
            return false;
        }

        List<StandardFactionDetachment11>
            definitions =
                selected
                    .Select(
                        value =>
                            StandardFactionPack11
                                .FindDetachment(
                                    pack,
                                    value))
                    .ToList();

        if (definitions.Any(
                value => value == null))
        {
            reason =
                "One or more selected Detachments are not part of the installed faction pack.";
            return false;
        }

        int total =
            definitions.Sum(
                value => value.dp);

        int limit =
            DetachmentPointLimit;

        bool incursionThreePointException =
            string.Equals(
                Game.StandardBattleSizeName,
                "Incursion",
                StringComparison.OrdinalIgnoreCase) &&
            definitions.Count == 1 &&
            definitions[0].dp == 3;

        if (limit > 0 &&
            total > limit &&
            !incursionThreePointException)
        {
            reason =
                total +
                "DP selected; " +
                Game.StandardBattleSizeName +
                " normally allows " +
                limit +
                "DP. Incursion permits a single 3DP Detachment as the exception.";
            return false;
        }

        Dictionary<string, int> tagCounts =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        foreach (
            StandardFactionDetachment11 definition
            in definitions)
        {
            foreach (string tag
                in definition.tags ??
                   new string[0])
            {
                if (string.IsNullOrWhiteSpace(
                        tag))
                {
                    continue;
                }

                int count;

                tagCounts.TryGetValue(
                    tag,
                    out count);

                tagCounts[tag] =
                    count + 1;
            }
        }

        string conflict =
            tagCounts
                .Where(
                    pair =>
                        pair.Value > 1)
                .Select(
                    pair => pair.Key)
                .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(
                conflict))
        {
            reason =
                "Only one Detachment with the " +
                conflict +
                " tag can be selected.";
            return false;
        }

        return true;
    }

    public bool TryLockDetachments(
        IEnumerable<string> selection,
        string source)
    {
        string reason;

        if (!TryValidateDetachmentSelection(
                selection,
                out reason))
        {
            selectionError = reason;
            return false;
        }

        selectedDetachments.Clear();

        foreach (string name
            in selection)
        {
            StandardFactionDetachment11
                definition =
                    StandardFactionPack11
                        .FindDetachment(
                            pack,
                            name
                        );

            if (definition != null &&
                !selectedDetachments.Contains(
                    definition.name))
            {
                selectedDetachments.Add(
                    definition.name
                );
            }
        }

        detachmentLocked = true;
        selectionError = "";
        rapidDropUnits.Clear();
        rapidDropLocked =
            !HasDetachment(
                "ORBITAL ASSAULT FORCE");

        ApplyStaticKeywords();

        if (Game != null)
        {
            Game.StandardLog(
                "FACTION",
                DisplayName +
                " detachments locked",
                string.Join(
                    " + ",
                    selectedDetachments
                        .ToArray()) +
                " (" +
                DetachmentPointsSpent +
                "DP) via " +
                source +
                "."
            );
        }

        return true;
    }

    public bool TryUnlockBeforeDeployment()
    {
        detachmentLocked = false;
        selectedDetachments.Clear();
        rapidDropUnits.Clear();
        rapidDropLocked = false;
        selectionError = "";
        return true;
    }

    public int RequiredRapidDropUnits()
    {
        if (Game == null)
            return 0;

        if (string.Equals(
                Game.StandardBattleSizeName,
                "Incursion",
                StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(
                Game.StandardBattleSizeName,
                "Strike Force",
                StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 4;
    }

    public IEnumerable<SquadController>
        RapidDropCandidates()
    {
        return army
            .Where(
                unit =>
                    unit != null &&
                    !unit.IsAttachedLeader &&
                    !unit.HasKeyword(
                        "TITANIC"));
    }

    public bool RapidDropContains(
        SquadController unit)
    {
        return
            unit != null &&
            rapidDropUnits.Contains(
                unit.JoinedActionController());
    }

    public void ToggleRapidDropUnit(
        SquadController unit)
    {
        if (unit == null ||
            rapidDropLocked)
        {
            return;
        }

        unit =
            unit.JoinedActionController();

        if (rapidDropUnits.Contains(unit))
        {
            rapidDropUnits.Remove(unit);
            return;
        }

        if (rapidDropUnits.Count >=
            RequiredRapidDropUnits())
        {
            return;
        }

        rapidDropUnits.Add(unit);
    }

    public bool LockRapidDropUnits()
    {
        if (!HasDetachment(
                "ORBITAL ASSAULT FORCE"))
        {
            rapidDropLocked = true;
            return true;
        }

        if (rapidDropUnits.Count !=
            RequiredRapidDropUnits())
        {
            return false;
        }

        foreach (SquadController unit
            in rapidDropUnits)
        {
            if (unit != null)
                unit.TemporaryDeepStrike = true;
        }

        rapidDropLocked = true;

        if (Game != null)
        {
            Game.StandardLog(
                "SPACE MARINES",
                "Rapid-drop Deployment",
                string.Join(
                    ", ",
                    rapidDropUnits
                        .Where(
                            value =>
                                value != null)
                        .Select(
                            value =>
                                value.DisplayName)
                        .ToArray()) +
                " gain Deep Strike."
            );
        }

        return true;
    }

    public override void OnGameEvent(
        GameEventContext context)
    {
        if (context == null ||
            Game == null ||
            pack == null)
        {
            return;
        }

        V47HandleCoreGameEvent(context);

        if (context.Type ==
                GameEventType.TurnStarted)
        {
            disembarkedThisTurn.Clear();
        }

        if (context.Type ==
                GameEventType.UnitDisembarked &&
            context.Source != null &&
            string.Equals(
                context.Source.FactionId,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            disembarkedThisTurn.Add(
                context.Source
                    .JoinedActionController());
        }

        if (context.Type ==
                GameEventType.BattleRoundStarted)
        {
            if (packId == "tyranids" &&
                HasDetachment(
                    "INVASION FLEET") &&
                string.IsNullOrWhiteSpace(
                    hyperAdaptation))
            {
                hyperAdaptationRequired = true;
            }

            if (packId == "space_marines" &&
                HasDetachment(
                    "LIBRARIUS CONCLAVE"))
            {
                psychicDiscipline = "";
                psychicDisciplineRequired = true;
            }

            if (packId == "tyranids" &&
                HasDetachment(
                    "SYNAPTIC NEXUS"))
            {
                synapticImperative = "";
            }
        }

        if (context.Type ==
                GameEventType.PhaseStarted &&
            context.Phase ==
                GameController.Phase.Command &&
            string.Equals(
                context.ActingFaction,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            commandSerial++;

            if (waaaghActive &&
                waaaghCommandSerial !=
                    commandSerial)
            {
                waaaghActive = false;
            }

            if (extremisActive &&
                extremisCommandSerial !=
                    commandSerial)
            {
                extremisActive = false;
            }

            if (packId ==
                "space_marines")
            {
                oathTarget = null;
                oathSelectionRequired =
                    Game.StandardEnemyUnits(
                        FactionId).Count > 0;

                combatDoctrine = "";
            }

            if (packId == "orks" &&
                HasDetachment(
                    "DA BIG HUNT"))
            {
                preyTarget = null;

                preySelectionRequired =
                    Game.StandardEnemyUnits(
                        FactionId)
                    .Any(
                        unit =>
                            unit.HasKeyword(
                                "MONSTER") ||
                            unit.HasKeyword(
                                "VEHICLE") ||
                            unit.HasKeyword(
                                "CHARACTER"));
            }

            if (packId == "orks" &&
                HasDetachment(
                    "FREEBOOTER KREW"))
            {
                lootObjective = null;
                lootSelectionRequired =
                    Game.StandardObjectives !=
                    null &&
                    Game.StandardObjectives.Count >
                    0;
            }
        }
    }

    public void ActivateWaaagh(
        bool bullyOnly)
    {
        if (packId != "orks" ||
            Game == null ||
            Game.CurrentPhase !=
                GameController.Phase.Command ||
            !string.Equals(
                Game.ActiveFactionId,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (bullyOnly)
        {
            bool warbossPresent =
                army.Any(
                    unit =>
                        unit != null &&
                        unit.IsAlive &&
                        unit.HasKeyword("WARBOSS") &&
                        (unit.IsOnBattlefield ||
                         unit.IsEmbarked));

            if (!HasDetachment(
                    "BULLY BOYZ") ||
                !waaaghUsed ||
                bullySecondWaaaghUsed ||
                waaaghActive ||
                !warbossPresent)
            {
                return;
            }

            bullySecondWaaaghUsed = true;
        }
        else
        {
            if (waaaghUsed)
                return;

            waaaghUsed = true;
        }

        waaaghActive = true;
        currentWaaaghBullyOnly =
            bullyOnly;
        waaaghCommandSerial =
            commandSerial;

        Game.StandardLog(
            "ORKS",
            bullyOnly
                ? "DA BOSS IS WATCHIN' - WAAAGH!"
                : "WAAAGH!",
            bullyOnly
                ? "Second Waaagh! called for WARBOSS, NOBZ and MEGANOBZ units."
                : "Waaagh! active until the start of this army's next Command phase."
        );
    }

    public bool UnitBenefitsFromWaaagh(
        SquadController unit)
    {
        if (!WaaaghActive ||
            unit == null ||
            !unit.HasIntrinsicKeyword(
                "ORKS") ||
            !string.Equals(
                unit.FactionId,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (currentWaaaghBullyOnly)
        {
            return
                unit.HasKeyword("WARBOSS") ||
                unit.HasKeyword("NOBZ") ||
                unit.HasKeyword("MEGANOBZ");
        }

        return true;
    }

    public void MarkShadowInTheWarpUsed()
    {
        shadowInTheWarpUsed = true;
    }

    public void SetOathTarget(
        SquadController unit)
    {
        if (unit == null)
            return;

        oathTarget =
            unit.JoinedActionController();

        oathSelectionRequired = false;

        WarboardRuleStateStore47.SetUnitTarget(
            "OATH_OF_MOMENT",
            FactionId,
            null,
            oathTarget,
            WarboardRuleScope47.OwnerNextTurn,
            "Oath of Moment"
        );

        Game.StandardLog(
            "SPACE MARINES",
            "Oath of Moment",
            oathTarget.DisplayName +
            " is the Oath of Moment target until the start of the next Space Marines Command phase."
        );
    }

    public void SetPreyTarget(
        SquadController unit)
    {
        if (unit == null)
            return;

        preyTarget =
            unit.JoinedActionController();

        preySelectionRequired = false;

        WarboardRuleStateStore47.SetUnitTarget(
            "ORK_PREY",
            FactionId,
            null,
            preyTarget,
            WarboardRuleScope47.OwnerNextTurn,
            "Da Hunt Is On"
        );

        Game.StandardLog(
            "ORKS",
            "Da Hunt Is On",
            preyTarget.DisplayName +
            " is the Prey until the start of the next Orks Command phase."
        );
    }

    public void SetLootObjective(
        ObjectiveController objective)
    {
        if (objective == null)
            return;

        lootObjective = objective;
        lootSelectionRequired = false;

        WarboardRuleStateStore47.SetObjectiveTarget(
            "ORK_LOOT_OBJECTIVE",
            FactionId,
            objective,
            WarboardRuleScope47.OwnerNextTurn,
            "Here Be Loot"
        );

        Game.StandardLog(
            "ORKS",
            "Here Be Loot",
            "Selected loot objective: " +
            objective.name +
            "."
        );
    }

    public void SelectHyperAdaptation(
        string value)
    {
        hyperAdaptation =
            value ?? "";

        hyperAdaptationRequired =
            string.IsNullOrWhiteSpace(
                hyperAdaptation);

        if (!hyperAdaptationRequired)
        {
            Game.StandardLog(
                "TYRANIDS",
                "Hyper-adaptations",
                hyperAdaptation +
                " selected for the battle."
            );
        }
    }

    public bool SelectCombatDoctrine(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value) ||
            combatDoctrinesUsed.Contains(
                value))
        {
            return false;
        }

        combatDoctrine = value;
        combatDoctrinesUsed.Add(value);

        Game.StandardLog(
            "SPACE MARINES",
            "Combat Doctrine",
            value +
            " active until the start of the next Space Marines Command phase."
        );

        return true;
    }

    public void SelectPsychicDiscipline(
        string value)
    {
        psychicDiscipline =
            value ?? "";

        psychicDisciplineRequired =
            string.IsNullOrWhiteSpace(
                psychicDiscipline);

        if (!psychicDisciplineRequired)
        {
            Game.StandardLog(
                "SPACE MARINES",
                "Psychic Discipline",
                psychicDiscipline +
                " active until the end of the battle round."
            );
        }
    }

    public bool SelectSynapticImperative(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value) ||
            synapticImperativesUsed
                .Contains(value))
        {
            return false;
        }

        synapticImperative = value;
        synapticImperativesUsed.Add(value);

        Game.StandardLog(
            "TYRANIDS",
            "Synaptic Imperative",
            value +
            " active until the end of the battle round."
        );

        return true;
    }

    public bool ActivateExtremisLevelThreat()
    {
        if (packId != "space_marines" ||
            !HasDetachment(
                "1ST COMPANY TASK FORCE") ||
            extremisUsed ||
            Game == null ||
            Game.CurrentPhase !=
                GameController.Phase.Command ||
            !string.Equals(
                Game.ActiveFactionId,
                FactionId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        extremisUsed = true;
        extremisActive = true;
        extremisCommandSerial =
            commandSerial;

        Game.StandardLog(
            "SPACE MARINES",
            "Extremis-level Threat",
            "Until the start of the next Space Marines Command phase, attacks made by models with Oath of Moment that target the Oath target can re-roll Wound rolls."
        );

        return true;
    }

    public bool ArmyHasSupplementKeyword()
    {
        return unsupportedSupplement;
    }

    private void ApplyStaticKeywords()
    {
        if (Game == null ||
            pack == null ||
            !detachmentLocked)
        {
            return;
        }

        if (packId == "orks")
        {
            if (HasDetachment(
                    "DREAD MOB"))
            {
                foreach (SquadController unit
                    in army.Where(
                        value =>
                            value != null &&
                            value.HasKeyword(
                                "GRETCHIN")))
                {
                    Game.StandardAddKeyword(
                        unit,
                        "BATTLELINE"
                    );
                }
            }

            if (HasDetachment(
                    "ROLLIN' DEFF"))
            {
                foreach (SquadController unit
                    in army.Where(
                        value =>
                            value != null &&
                            (NameContains(
                                value,
                                "BATTLEWAGON") ||
                             NameContains(
                                value,
                                "HUNTA RIG") ||
                             NameContains(
                                value,
                                "KILL RIG"))))
                {
                    Game.StandardAddKeyword(
                        unit,
                        "WAGON"
                    );
                }
            }

            if (HasDetachment(
                    "TAKTIKAL BRIGADE"))
            {
                foreach (SquadController unit
                    in army.Where(
                        value =>
                            value != null &&
                            NameContains(
                                value,
                                "STORMBOYZ")))
                {
                    Game.StandardAddKeyword(
                        unit,
                        "BATTLELINE"
                    );
                }
            }
        }

        if (packId == "tyranids")
        {
            if (HasDetachment(
                    "AMBUSH PREDATORS"))
            {
                foreach (SquadController unit
                    in army.Where(
                        value =>
                            value != null &&
                            (NameContains(
                                value,
                                "DEATHLEAPER") ||
                             NameContains(
                                value,
                                "LICTOR") ||
                             NameContains(
                                value,
                                "NEUROLICTOR"))))
                {
                    unit.TemporaryDeepStrike =
                        true;
                }
            }

            if (HasDetachment(
                    "WARRIOR BIOFORM ONSLAUGHT"))
            {
                foreach (SquadController unit
                    in army.Where(
                        value =>
                            value != null &&
                            (NameContains(
                                value,
                                "TYRANID WARRIORS WITH RANGED") ||
                             NameContains(
                                value,
                                "TYRANID WARRIORS WITH MELEE"))))
                {
                    Game.StandardAddKeyword(
                        unit,
                        "TYRANID WARRIORS"
                    );

                    Game.StandardAddKeyword(
                        unit,
                        "BATTLELINE"
                    );
                }
            }
        }

        if (packId ==
            "space_marines")
        {
            if (HasDetachment(
                    "HEADHUNTER TASK FORCE"))
            {
                foreach (SquadController unit
                    in army.Where(
                        value =>
                            value != null &&
                            value.HasKeyword(
                                "VEHICLE") &&
                            !value.HasKeyword(
                                "FORTIFICATION") &&
                            !value.HasKeyword(
                                "DROP POD") &&
                            !value.HasKeyword(
                                "WALKER") &&
                            !value.HasKeyword(
                                "FLY")))
                {
                    Game.StandardAddKeyword(
                        unit,
                        "TANK ACE"
                    );
                }
            }

            if (HasDetachment(
                    "FULGURIS TASK FORCE"))
            {
                foreach (SquadController unit
                    in army.Where(
                        value =>
                            value != null &&
                            (NameContains(
                                value,
                                "LAND SPEEDER") ||
                             NameContains(
                                value,
                                "STORM SPEEDER HAILSTRIKE") ||
                             NameContains(
                                value,
                                "STORM SPEEDER HAMMERSTRIKE") ||
                             NameContains(
                                value,
                                "STORM SPEEDER THUNDERSTRIKE"))))
                {
                    Game.StandardAddKeyword(
                        unit,
                        "SPEEDER"
                    );
                }
            }
        }
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
}
