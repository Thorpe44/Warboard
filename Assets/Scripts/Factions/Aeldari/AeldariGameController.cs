using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Aeldari faction-level runtime controller.
///
/// v34 makes this the authority for:
/// - the selected/locked Aeldari detachment
/// - the loaded detachment controller
/// - detachment-granted temporary keywords/state
/// - the base Battle Focus token pool
///
/// The existing AeldariRulesSystem remains the current rules implementation
/// during migration, but GameController no longer needs to own new Aeldari
/// architecture.
/// </summary>
public sealed class AeldariGameController :
    FactionGameControllerBase
{
    private const string YellowScribeEndpoint =
        "https://yellowscribe.link/get_army_by_id?id=";

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

    private bool rosterProbeStarted;
    private bool rosterProbeFinished;
    private string rosterProbeStatus = "";

    private string selectionError = "";

    private AeldariDetachment lastAppliedDetachment;
    private bool hasLastAppliedDetachment;

    private int battleFocusTokens;
    private int battleFocusRound = -1;

    private readonly HashSet<string>
        agileManoeuvresUsedThisPhase =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

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
        get
        {
            EnsureBattleFocusRound();
            return battleFocusTokens;
        }
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
        RefreshSuggestedDetachment();
    }

    public override void RefreshArmy(
        IReadOnlyList<SquadController> units)
    {
        base.RefreshArmy(units);

        EnsureRulesBinding();

        if (!detachmentLocked)
        {
            RefreshSuggestedDetachment();
            BeginRosterProbeWhenPossible();
        }

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
                StartBattleRound(
                    Game != null
                    ? Game.CurrentRoundNumber
                    : context.Amount);
                SynchronizeDetachmentState();
                break;

            case GameEventType.BattleRoundEnded:
                battleFocusTokens = 0;
                agileManoeuvresUsedThisPhase.Clear();
                break;

            case GameEventType.PhaseEnded:
                agileManoeuvresUsedThisPhase.Clear();
                break;

            case GameEventType.TurnStarted:
            case GameEventType.PhaseStarted:
            case GameEventType.UnitSetUp:
                SynchronizeDetachmentState();
                break;
        }

        if (detachmentController != null)
        {
            detachmentController.OnGameEvent(
                context);
        }
    }

    public override void Tick()
    {
        ObserveCoreTiming();

        EnsureRulesBinding();

        if (rules == null)
            return;

        BeginRosterProbeWhenPossible();

        if (detachmentLocked)
        {
            AeldariDetachment current =
                rules.GetDetachment(
                    FactionId);

            // The old v32 "NEXT AELDARI DETACHMENT" control may still be
            // rendered by GameController during migration. Once v34 locks
            // the roster detachment, any attempt to cycle it is immediately
            // rejected and the roster's locked value is restored.
            if (current !=
                lockedDetachment)
            {
                rules.SetDetachment(
                    FactionId,
                    lockedDetachment);

                selectionError =
                    "Detachment is locked for this battle.";
            }
        }

        AeldariDetachment effective =
            detachmentLocked
            ? lockedDetachment
            : rules.GetDetachment(
                FactionId);

        if (!hasLastAppliedDetachment ||
            effective !=
                lastAppliedDetachment)
        {
            SynchronizeDetachmentState();
        }

        if (detachmentController != null)
        {
            detachmentController.Tick();
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

        if (!ReadyForPreGameSelection())
            return false;

        // Give the YellowScribe probe a chance to resolve the roster
        // automatically. The fallback selector only appears once that probe
        // is complete or if no roster code is available.
        return rosterProbeFinished ||
            string.IsNullOrWhiteSpace(
                ResolveYellowScribeCode());
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
            detachmentLocked
            ? lockedDetachment ==
                AeldariDetachment
                    .DevotedOfYnnead
            : rules != null &&
              rules.DetachmentIs(
                  FactionId,
                  AeldariDetachment
                      .DevotedOfYnnead);
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
        if (!UsesBattleFocus())
        {
            battleFocusTokens = 0;
            battleFocusRound = round;
            return;
        }

        if (battleFocusRound == round &&
            round > 0)
        {
            return;
        }

        battleFocusRound = round;

        battleFocusTokens =
            BaseBattleFocusForCurrentSize();

        agileManoeuvresUsedThisPhase.Clear();
    }

    public bool SpendBattleFocus(
        int amount,
        string manoeuvre = "")
    {
        if (amount <= 0)
            return true;

        EnsureBattleFocusRound();

        string canonical =
            CanonicalManoeuvre(
                manoeuvre);

        bool repeatable =
            string.Equals(
                canonical,
                "SWIFT AS THE WIND",
                StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(
                canonical) &&
            !repeatable &&
            agileManoeuvresUsedThisPhase.Contains(
                canonical))
        {
            selectionError =
                canonical +
                " has already been triggered this phase.";

            return false;
        }

        if (battleFocusTokens < amount)
            return false;

        battleFocusTokens -= amount;

        if (!string.IsNullOrWhiteSpace(
                canonical) &&
            !repeatable)
        {
            agileManoeuvresUsedThisPhase.Add(
                canonical);
        }

        return true;
    }

    public void EndBattleRound()
    {
        battleFocusTokens = 0;
        agileManoeuvresUsedThisPhase.Clear();
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

    private void EnsureBattleFocusRound()
    {
        if (Game == null)
            return;

        int round =
            Game.CurrentRoundNumber;

        if (round > 0 &&
            round != battleFocusRound)
        {
            StartBattleRound(
                round);
        }
    }

    private int BaseBattleFocusForCurrentSize()
    {
        string battleSize =
            Game != null
            ? Game.CoreBattleSizeName
            : "";

        if (string.Equals(
                battleSize,
                "Incursion",
                StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(
                battleSize,
                "Strike Force",
                StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (string.Equals(
                battleSize,
                "Onslaught",
                StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        int points =
            Game != null
            ? Game.CoreBattlePoints
            : 2000;

        if (points <= 1000)
            return 2;

        if (points <= 2000)
            return 4;

        return 6;
    }

    private void RefreshSuggestedDetachment()
    {
        if (rules == null ||
            !rules.IsAeldariFaction(
                FactionId))
        {
            return;
        }

        suggestedDetachment =
            rules.GetDetachment(
                FactionId);
    }

    private void BeginRosterProbeWhenPossible()
    {
        if (detachmentLocked ||
            rosterProbeStarted ||
            Game == null ||
            army.Count == 0)
        {
            return;
        }

        string code =
            ResolveYellowScribeCode();

        if (string.IsNullOrWhiteSpace(
                code))
        {
            if (ReadyForPreGameSelection())
            {
                rosterProbeFinished = true;
                rosterProbeStatus =
                    "No YellowScribe roster code is available; pre-game detachment selection is required.";
            }

            return;
        }

        rosterProbeStarted = true;
        rosterProbeStatus =
            "Reading detachment from imported roster...";

        Game.StartCoroutine(
            ProbeRosterDetachment(
                code));
    }

    private IEnumerator ProbeRosterDetachment(
        string code)
    {
        string url =
            YellowScribeEndpoint +
            UnityWebRequest.EscapeURL(
                code);

        using (UnityWebRequest request =
            UnityWebRequest.Get(url))
        {
            yield return
                request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                rosterProbeFinished = true;

                rosterProbeStatus =
                    "Roster loaded, but its detachment metadata could not be read automatically.";

                yield break;
            }

            AeldariDetachment detected;

            if (TryFindDetachmentInPayload(
                    request.downloadHandler.text,
                    out detected))
            {
                rosterProbeFinished = true;

                if (TryLockDetachment(
                        detected,
                        "YellowScribe / New Recruit roster"))
                {
                    rosterProbeStatus =
                        "Detachment read from roster: " +
                        DisplayNameFor(
                            detected) +
                        ".";
                }

                yield break;
            }

            rosterProbeFinished = true;

            rosterProbeStatus =
                "The imported roster did not expose a single Aeldari detachment value. Select it once before deployment.";
        }
    }

    private bool TryFindDetachmentInPayload(
        string json,
        out AeldariDetachment detachment)
    {
        detachment =
            AeldariDetachment.Warhost;

        if (string.IsNullOrWhiteSpace(
                json))
        {
            return false;
        }

        object root =
            MiniJson.Deserialize(
                json);

        HashSet<AeldariDetachment>
            explicitCandidates =
                new HashSet<AeldariDetachment>();

        HashSet<AeldariDetachment>
            exactCandidates =
                new HashSet<AeldariDetachment>();

        CollectDetachmentCandidates(
            root,
            "",
            explicitCandidates,
            exactCandidates);

        if (explicitCandidates.Count == 1)
        {
            detachment =
                explicitCandidates.First();

            return true;
        }

        if (explicitCandidates.Count > 1)
            return false;

        if (exactCandidates.Count == 1)
        {
            detachment =
                exactCandidates.First();

            return true;
        }

        return false;
    }

    private void CollectDetachmentCandidates(
        object node,
        string keyHint,
        HashSet<AeldariDetachment>
            explicitCandidates,
        HashSet<AeldariDetachment>
            exactCandidates)
    {
        if (node == null)
            return;

        string text =
            node as string;

        if (text != null)
        {
            AeldariDetachment match;

            bool explicitField =
                !string.IsNullOrWhiteSpace(
                    keyHint) &&
                keyHint.IndexOf(
                    "detachment",
                    StringComparison.OrdinalIgnoreCase) >= 0;

            if (TryMatchDetachmentText(
                    text,
                    explicitField,
                    out match))
            {
                if (explicitField)
                {
                    explicitCandidates.Add(
                        match);
                }
                else
                {
                    exactCandidates.Add(
                        match);
                }
            }

            return;
        }

        Dictionary<string, object> map =
            node as
                Dictionary<string, object>;

        if (map != null)
        {
            foreach (
                KeyValuePair<string, object>
                    pair
                in map)
            {
                CollectDetachmentCandidates(
                    pair.Value,
                    pair.Key,
                    explicitCandidates,
                    exactCandidates);
            }

            return;
        }

        IList list =
            node as IList;

        if (list != null)
        {
            foreach (object item in list)
            {
                CollectDetachmentCandidates(
                    item,
                    keyHint,
                    explicitCandidates,
                    exactCandidates);
            }
        }
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
        if (rules != null ||
            Game == null)
        {
            return;
        }

        rules =
            Game.CoreAeldariRules;
    }

    private void SynchronizeDetachmentState()
    {
        if (rules == null ||
            !rules.IsAeldariFaction(
                FactionId))
        {
            return;
        }

        AeldariDetachment detachment =
            detachmentLocked
            ? lockedDetachment
            : rules.GetDetachment(
                FactionId);

        if (detachmentLocked)
        {
            rules.SetDetachment(
                FactionId,
                detachment);
        }

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

        rules.ApplyDetachmentKeywords(
            FactionId,
            army);

        lastAppliedDetachment =
            detachment;

        hasLastAppliedDetachment =
            true;

        if (detachmentLocked)
        {
            LoadDetachmentController();
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

    private bool ReadyForPreGameSelection()
    {
        return
            Game != null &&
            Game.CorePreGameReady;
    }

    private string ResolveYellowScribeCode()
    {
        return
            Game != null
            ? Game.CoreYellowCodeForFaction(
                FactionId)
            : "";
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

    private static string CanonicalManoeuvre(
        string manoeuvre)
    {
        if (string.IsNullOrWhiteSpace(
                manoeuvre))
        {
            return "";
        }

        string value =
            manoeuvre.ToUpperInvariant();

        if (value.Contains(
                "SWIFT AS THE WIND"))
        {
            return "SWIFT AS THE WIND";
        }

        if (value.Contains(
                "FLITTING SHADOWS"))
        {
            return "FLITTING SHADOWS";
        }

        if (value.Contains(
                "STAR ENGINES"))
        {
            return "STAR ENGINES";
        }

        if (value.Contains(
                "SUDDEN STRIKE"))
        {
            return "SUDDEN STRIKE";
        }

        if (value.Contains(
                "OPPORTUNITY SEIZED"))
        {
            return "OPPORTUNITY SEIZED";
        }

        if (value.Contains(
                "FADE BACK"))
        {
            return "FADE BACK";
        }

        return value.Trim();
    }

    private GameController.Phase observedPhase;
    private bool hasObservedPhase;
    private int observedRound = -1;

    private void ObserveCoreTiming()
    {
        if (Game == null)
            return;

        GameController.Phase currentPhase =
            Game.CurrentPhase;

        if (!hasObservedPhase)
        {
            observedPhase = currentPhase;
            hasObservedPhase = true;
        }
        else if (observedPhase != currentPhase)
        {
            agileManoeuvresUsedThisPhase.Clear();
            observedPhase = currentPhase;
        }

        int currentRound =
            Game.CurrentRoundNumber;

        if (currentRound > 0 &&
            currentRound != observedRound)
        {
            observedRound = currentRound;
            StartBattleRound(
                currentRound);
        }
    }
}
