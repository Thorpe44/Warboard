using System;
using System.Collections.Generic;
using UnityEngine;

// Stable internal surface used by faction controllers and other subsystems.
// This replaces reflection against GameController's private fields.
public partial class GameController : MonoBehaviour
{
    internal IReadOnlyList<SquadController> CoreSquads
    {
        get { return squads; }
    }

    internal IReadOnlyList<string> CoreFactions
    {
        get { return factions; }
    }

    internal AeldariRulesSystem CoreAeldariRules
    {
        get { return aeldariRules; }
    }

    internal string CoreActiveFaction
    {
        get { return activeFaction; }
    }

    internal string CoreBattleSizeName
    {
        get { return battleSizeName; }
    }

    internal int CoreBattlePoints
    {
        get { return battlePoints; }
    }

    internal bool CorePreGameReady
    {
        get
        {
            return
                (playerOneLoaded &&
                 playerTwoLoaded) ||
                deploymentMode ||
                missionSetupMode;
        }
    }

    internal string CoreYellowCodeForFaction(
        string factionId)
    {
        if (string.IsNullOrWhiteSpace(
                factionId))
        {
            return "";
        }

        int index =
            factions.FindIndex(
                faction =>
                    string.Equals(
                        faction,
                        factionId,
                        StringComparison.OrdinalIgnoreCase));

        if (index == 0)
            return yellowCodePlayerOne ?? "";

        if (index == 1)
            return yellowCodePlayerTwo ?? "";

        return "";
    }

    internal void RaiseCoreEvent(
        GameEventType type,
        SquadController source = null,
        SquadController target = null,
        int amount = 0,
        string note = "")
    {
        GameEventBus.Raise(
            new GameEventContext
            {
                Type = type,
                Game = this,
                ActingFaction =
                    source != null
                    ? source.FactionId
                    : activeFaction,
                Phase = phase,
                Source = source,
                Target = target,
                Amount = amount,
                Note = note ?? ""
            });
    }
}
