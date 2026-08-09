using System;
using System.Collections.Generic;

/// <summary>
/// Edition 11 Battle Focus implementation from Aeldari Faction Pack v1.1.
/// </summary>
public sealed class AeldariBattleFocusController
{
    private GameController game;
    private string factionId = "";
    private int tokens;
    private int activeRound = -1;

    private readonly HashSet<string> manoeuvresUsedThisPhase =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<SquadController> unitsUsedThisPhase =
        new HashSet<SquadController>();

    public int Tokens { get { return tokens; } }
    public int ActiveRound { get { return activeRound; } }

    public void Initialize(GameController owner, string ownerFactionId)
    {
        game = owner;
        factionId = ownerFactionId ?? "";
        tokens = 0;
        activeRound = -1;
        manoeuvresUsedThisPhase.Clear();
        unitsUsedThisPhase.Clear();
    }

    public void HandleGameEvent(GameEventContext context, bool battleFocusEligible)
    {
        if (context == null) return;
        switch (context.Type)
        {
            case GameEventType.BattleRoundStarted:
                StartBattleRound(context.Amount > 0 ? context.Amount : game != null ? game.BattleRound : 0, battleFocusEligible);
                break;
            case GameEventType.BattleRoundEnded:
                EndBattleRound();
                break;
            case GameEventType.PhaseEnded:
                EndPhase();
                break;
        }
    }

    public void StartBattleRound(int round, bool battleFocusEligible)
    {
        if (activeRound == round && round > 0) return;
        activeRound = round;
        manoeuvresUsedThisPhase.Clear();
        unitsUsedThisPhase.Clear();
        tokens = battleFocusEligible ? BaseTokensForBattleSize() : 0;
    }

    public void AddTokens(int amount)
    {
        tokens += Math.Max(0, amount);
    }

    public bool Spend(int amount, string manoeuvre, SquadController unit, out string failureReason)
    {
        failureReason = "";
        EnsureCurrentRound();

        SquadController actionUnit = unit != null ? unit.JoinedActionController() : null;
        string canonical = CanonicalManoeuvre(manoeuvre);
        bool swift = string.Equals(canonical, "SWIFT AS THE WIND", StringComparison.OrdinalIgnoreCase);
        bool fade = string.Equals(canonical, "FADE BACK", StringComparison.OrdinalIgnoreCase);
        bool adrenalFreeFade = fade && actionUnit != null &&
            AeldariFactionPack11.UnitHasEnhancement(actionUnit, "Adrenal Infusions");

        // A unit can perform only one Agile Manoeuvre per phase.
        if (actionUnit != null &&
            (unitsUsedThisPhase.Contains(actionUnit) || actionUnit.AgileManoeuvreUsedThisPhase))
        {
            failureReason = actionUnit.DisplayName + " has already performed an Agile Manoeuvre this phase.";
            return false;
        }

        // Unless otherwise stated, each named manoeuvre is once per phase.
        // Swift is explicitly repeatable for different units. Adrenal Infusions
        // makes its unit's Fade Back independent of the global Fade Back limit.
        if (!string.IsNullOrWhiteSpace(canonical) && !swift && !adrenalFreeFade &&
            manoeuvresUsedThisPhase.Contains(canonical))
        {
            failureReason = canonical + " has already been triggered this phase.";
            return false;
        }

        int actualCost = adrenalFreeFade ? 0 : Math.Max(0, amount);
        if (tokens < actualCost)
        {
            failureReason = "Not enough Battle Focus tokens.";
            return false;
        }

        tokens -= actualCost;

        if (actionUnit != null)
        {
            unitsUsedThisPhase.Add(actionUnit);
            actionUnit.AgileManoeuvreUsedThisPhase = true;
        }

        if (!string.IsNullOrWhiteSpace(canonical) && !swift && !adrenalFreeFade)
            manoeuvresUsedThisPhase.Add(canonical);

        // Pirate Prince: each time a token is spent for Prince Yriel's unit,
        // on 3+ regain one token. Traditional mode still uses Warboard's die
        // logger so the refund is visible and deterministic.
        if (actualCost > 0 && actionUnit != null &&
            AeldariFactionPack11.UnitHasEnhancement(actionUnit, "Pirate Prince"))
        {
            if (game != null && !game.IsXcomMode)
            {
                // Traditional mode must never silently roll the physical die
                // on the player's behalf. The GameController opens the normal
                // tabletop result prompt and refunds the token on a marked 3+.
                game.Aeldari11ResolvePiratePrinceRefund(actionUnit);
            }
            else
            {
                int roll = DiceRoller.RollD6("Pirate Prince Battle Focus refund");
                if (roll >= 3) tokens += 1;
            }
        }

        return true;
    }

    // Compatibility overload for older call sites while v42 migration updates
    // GameController to pass the exact unit.
    public bool Spend(int amount, string manoeuvre, out string failureReason)
    {
        return Spend(amount, manoeuvre, null, out failureReason);
    }

    public void EndPhase()
    {
        manoeuvresUsedThisPhase.Clear();
        unitsUsedThisPhase.Clear();
    }

    public void EndBattleRound()
    {
        tokens = 0;
        manoeuvresUsedThisPhase.Clear();
        unitsUsedThisPhase.Clear();
    }

    private void EnsureCurrentRound()
    {
        if (game == null) return;
        int round = game.BattleRound;
        if (round <= 0 || round == activeRound) return;
        StartBattleRound(round, true);
    }

    private int BaseTokensForBattleSize()
    {
        string battleSize = game != null ? game.BattleSizeName : "";
        if (string.Equals(battleSize, "Incursion", StringComparison.OrdinalIgnoreCase)) return 2;
        if (string.Equals(battleSize, "Strike Force", StringComparison.OrdinalIgnoreCase)) return 4;
        if (string.Equals(battleSize, "Onslaught", StringComparison.OrdinalIgnoreCase)) return 6;
        int points = game != null ? game.BattlePoints : 2000;
        if (points <= 1000) return 2;
        if (points <= 2000) return 4;
        return 6;
    }

    private static string CanonicalManoeuvre(string manoeuvre)
    {
        if (string.IsNullOrWhiteSpace(manoeuvre)) return "";
        string value = manoeuvre.ToUpperInvariant();
        if (value.Contains("SWIFT AS THE WIND")) return "SWIFT AS THE WIND";
        if (value.Contains("FLITTING SHADOWS")) return "FLITTING SHADOWS";
        if (value.Contains("STAR ENGINES")) return "STAR ENGINES";
        if (value.Contains("SUDDEN STRIKE")) return "SUDDEN STRIKE";
        if (value.Contains("OPPORTUNITY SEIZED")) return "OPPORTUNITY SEIZED";
        if (value.Contains("FADE BACK")) return "FADE BACK";
        return value.Trim();
    }
}
