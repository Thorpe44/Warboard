using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class WarboardDatasheetChoiceOption47
{
    public string Id = "";
    public string Label = "";
    public string Note = "";

    public WarboardDatasheetChoiceOption47(
        string id,
        string label,
        string note = "")
    {
        Id = id ?? "";
        Label = label ?? id ?? "Choice";
        Note = note ?? "";
    }
}

/// <summary>
/// Generic player-choice surface for datasheet/faction rules. The selected
/// option is persisted through WarboardRuleStateStore47 with the requested
/// scope, so once-per-phase/turn/battle rules no longer need bespoke booleans.
/// </summary>
public static class WarboardDatasheetChoice47
{
    public static bool HasChoice(
        string id,
        string factionId,
        SquadController unit = null)
    {
        WarboardRuleState47 state =
            WarboardRuleStateStore47.GetLatest(
                id,
                factionId
            );

        if (state == null)
            return false;

        if (unit == null)
            return true;

        return state.SourceUnit ==
            unit.JoinedActionController();
    }

    public static string ChoiceValue(
        string id,
        string factionId,
        SquadController unit = null)
    {
        IEnumerable<WarboardRuleState47> states =
            WarboardRuleStateStore47.GetAll(
                id,
                factionId
            );

        SquadController action =
            unit != null
            ? unit.JoinedActionController()
            : null;

        WarboardRuleState47 state =
            states.LastOrDefault(
                value =>
                    unit == null ||
                    value.SourceUnit == action
            );

        return state != null
            ? state.StringValue ?? ""
            : "";
    }

    public static void Request(
        GameController game,
        string title,
        string description,
        string stateId,
        string factionId,
        SquadController source,
        WarboardRuleScope47 scope,
        IEnumerable<WarboardDatasheetChoiceOption47> options,
        Action<string> completion = null)
    {
        if (game == null ||
            string.IsNullOrWhiteSpace(stateId) ||
            options == null)
        {
            return;
        }

        List<WarboardDatasheetChoiceOption47> available =
            options
                .Where(value => value != null)
                .ToList();

        if (available.Count == 0)
            return;

        List<RuleChoiceOption> ui =
            new List<RuleChoiceOption>();

        foreach (WarboardDatasheetChoiceOption47 option
            in available)
        {
            WarboardDatasheetChoiceOption47 captured =
                option;

            ui.Add(
                new RuleChoiceOption(
                    captured.Label,
                    () =>
                    {
                        game.StandardCloseRuleChoice();

                        WarboardRuleStateStore47.SetSourceValue(
                            stateId,
                            factionId,
                            source,
                            captured.Id,
                            0,
                            0f,
                            scope,
                            captured.Note
                        );

                        WarboardRuleEventBus47.Raise(
                            new WarboardRuleEvent47
                            {
                                Type =
                                    WarboardRuleEventType47
                                        .DatasheetChoiceMade,
                                Game = game,
                                ActingFaction =
                                    factionId ?? "",
                                Source =
                                    source != null
                                    ? source.JoinedActionController()
                                    : null,
                                RuleId = stateId ?? "",
                                StringValue = captured.Id ?? "",
                                Note = captured.Note ?? ""
                            }
                        );

                        game.StandardLog(
                            "RULE CHOICE",
                            title,
                            captured.Label +
                            (string.IsNullOrWhiteSpace(
                                captured.Note)
                                ? ""
                                : " - " + captured.Note)
                        );

                        if (completion != null)
                            completion(captured.Id);
                    }
                )
            );
        }

        game.StandardOpenRuleChoice(
            title,
            description,
            ui.ToArray()
        );
    }
}
