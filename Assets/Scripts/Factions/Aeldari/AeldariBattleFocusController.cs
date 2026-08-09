using System;
using System.Collections.Generic;

/// <summary>
/// Owns the Aeldari Battle Focus resource and Agile Manoeuvre phase limits.
///
/// The faction controller decides whether the army is eligible for Battle
/// Focus. This class owns only the resource/timing state so it no longer lives
/// in GameController or FactionRuleSystem.
/// </summary>
public sealed class AeldariBattleFocusController
{
    private GameController game;
    private string factionId = "";

    private int tokens;
    private int activeRound = -1;

    private readonly HashSet<string>
        manoeuvresUsedThisPhase =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

    public int Tokens
    {
        get { return tokens; }
    }

    public int ActiveRound
    {
        get { return activeRound; }
    }

    public void Initialize(
        GameController owner,
        string ownerFactionId)
    {
        game = owner;
        factionId = ownerFactionId ?? "";
        tokens = 0;
        activeRound = -1;
        manoeuvresUsedThisPhase.Clear();
    }

    public void HandleGameEvent(
        GameEventContext context,
        bool battleFocusEligible)
    {
        if (context == null)
            return;

        switch (context.Type)
        {
            case GameEventType.BattleRoundStarted:
                StartBattleRound(
                    context.Amount > 0
                    ? context.Amount
                    : game != null
                        ? game.BattleRound
                        : 0,
                    battleFocusEligible);
                break;

            case GameEventType.BattleRoundEnded:
                EndBattleRound();
                break;

            case GameEventType.PhaseEnded:
                EndPhase();
                break;
        }
    }

    public void StartBattleRound(
        int round,
        bool battleFocusEligible)
    {
        if (activeRound == round &&
            round > 0)
        {
            return;
        }

        activeRound = round;
        manoeuvresUsedThisPhase.Clear();

        if (!battleFocusEligible)
        {
            tokens = 0;
            return;
        }

        tokens =
            BaseTokensForBattleSize();
    }

    public bool Spend(
        int amount,
        string manoeuvre,
        out string failureReason)
    {
        failureReason = "";

        if (amount <= 0)
            return true;

        EnsureCurrentRound();

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
            manoeuvresUsedThisPhase.Contains(
                canonical))
        {
            failureReason =
                canonical +
                " has already been triggered this phase.";

            return false;
        }

        if (tokens < amount)
        {
            failureReason =
                "Not enough Battle Focus tokens.";

            return false;
        }

        tokens -= amount;

        if (!string.IsNullOrWhiteSpace(
                canonical) &&
            !repeatable)
        {
            manoeuvresUsedThisPhase.Add(
                canonical);
        }

        return true;
    }

    public void EndPhase()
    {
        manoeuvresUsedThisPhase.Clear();
    }

    public void EndBattleRound()
    {
        tokens = 0;
        manoeuvresUsedThisPhase.Clear();
    }

    private void EnsureCurrentRound()
    {
        if (game == null)
            return;

        int round =
            game.BattleRound;

        if (round <= 0 ||
            round == activeRound)
        {
            return;
        }

        StartBattleRound(
            round,
            true);
    }

    private int BaseTokensForBattleSize()
    {
        string battleSize =
            game != null
            ? game.BattleSizeName
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
            game != null
            ? game.BattlePoints
            : 2000;

        if (points <= 1000)
            return 2;

        if (points <= 2000)
            return 4;

        return 6;
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
}
