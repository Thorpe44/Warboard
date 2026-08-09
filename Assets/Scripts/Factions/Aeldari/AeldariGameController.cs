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

        RefreshSuggestedDetachment();
    }

public override void RefreshArmy(
        IReadOnlyList<SquadController> units)
    {
        base.RefreshArmy(units);

        EnsureRulesBinding();
        PruneTemporaryKeywordGrants();

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

        BeginRosterProbeWhenPossible();

        if (!ReadyForPreGameSelection())
            return false;

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
            Game.AeldariRules;
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

private bool ReadyForPreGameSelection()
    {
        return
            Game != null &&
            Game.PreGameReady;
    }

private string ResolveYellowScribeCode()
    {
        return
            Game != null
            ? Game.GetRosterCode(
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
