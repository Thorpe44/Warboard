using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Stable bridge used by post-v45 faction modules. Because GameController is
/// already a partial class, this exposes existing private core operations
/// without teaching Core about Tyranids, Orks or Space Marines individually.
/// </summary>
public partial class GameController
{
    public SquadController StandardSelectedSquad
    {
        get { return selectedSquad; }
    }

    public ModelToken StandardSelectedModel
    {
        get { return selectedModel; }
    }

    public string StandardBattleSizeName
    {
        get { return battleSizeName; }
    }

    public IReadOnlyList<ObjectiveController>
        StandardObjectives
    {
        get { return objectives; }
    }

    public IReadOnlyList<string>
        StandardFactionIds
    {
        get { return factions; }
    }

    public float StandardDistance(
        SquadController first,
        SquadController second)
    {
        if (first == null ||
            second == null)
        {
            return float.MaxValue;
        }

        return JoinedDistance(
            first.JoinedActionController(),
            second.JoinedActionController()
        );
    }

    public List<SquadController>
        StandardEnemyUnits(
            string faction)
    {
        return squads
            .Where(
                unit =>
                    unit != null &&
                    unit.IsAlive &&
                    !unit.IsAttachedLeader &&
                    !string.Equals(
                        unit.FactionId,
                        faction,
                        StringComparison.OrdinalIgnoreCase))
            .Select(
                unit =>
                    unit.JoinedActionController())
            .Distinct()
            .ToList();
    }

    public void StandardOpenRuleChoice(
        string title,
        string description,
        IEnumerable<RuleChoiceOption> options)
    {
        OpenRuleChoice(
            title,
            description,
            options
        );
    }

    public void StandardSetStatus(
        string text)
    {
        status = text ?? "";
    }

    public void StandardBeginSpecialMove(
        SquadController unit,
        float maximumDistance,
        string label,
        Action completed)
    {
        BeginSpecialMove(
            unit,
            maximumDistance,
            label,
            completed
        );
    }

    public void StandardBeginSpecialShoot(
        SquadController unit,
        SquadController forcedTarget,
        string label,
        Action completed)
    {
        BeginSpecialShoot(
            unit,
            forcedTarget,
            label,
            completed
        );
    }

    public bool StandardUnitWithinTerrain(
        SquadController unit)
    {
        if (unit == null)
            return false;

        foreach (ModelToken model
            in unit
                .JoinedActionController()
                .JoinedLivingModelTokens())
        {
            if (model == null)
                continue;

            foreach (TerrainFeature terrain
                in CoreRules11Terrain.AllTerrain())
            {
                if (terrain != null &&
                    CoreRules11Terrain
                        .ModelInsideTerrainArea(
                            model,
                            terrain))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public int StandardEffectiveLeadership(
        SquadController unit)
    {
        return unit == null
            ? 7
            : EffectiveLeadership(
                unit.JoinedActionController()
            );
    }

    public void StandardResolveBattleShock(
        SquadController unit,
        int rollModifier,
        int diceCount,
        string label,
        Action completed = null)
    {
        if (unit == null ||
            !unit.IsAlive ||
            !unit.IsOnBattlefield)
        {
            if (completed != null)
                completed();

            return;
        }

        unit =
            unit.JoinedActionController();

        diceCount =
            Mathf.Clamp(
                diceCount,
                2,
                3
            );

        int leadership =
            StandardEffectiveLeadership(
                unit
            );

        if (IsXcomMode)
        {
            DiceRollRecord roll =
                DiceRoller.RollDice(
                    diceCount,
                    6,
                    label +
                    ": " +
                    unit.DisplayName
                );

            int total =
                roll.Total +
                rollModifier;

            bool passed =
                total >=
                leadership;

            unit.SetBattleShocked(
                !passed,
                total
            );

            AppendBattleLog(
                "BATTLE-SHOCK",
                label,
                unit.DisplayName +
                " rolled " +
                roll.Total +
                (rollModifier == 0
                    ? ""
                    : (rollModifier > 0
                        ? " +" +
                          rollModifier
                        : " " +
                          rollModifier)) +
                " = " +
                total +
                " vs Leadership " +
                leadership +
                "+: " +
                (passed
                    ? "PASS"
                    : "FAIL - BATTLE-SHOCKED")
            );

            RefreshObjectiveDisplays();

            if (completed != null)
                completed();

            return;
        }

        OpenTraditionalDicePrompt(
            diceCount
        );

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        options.Add(
            new RuleChoiceOption(
                "PASS",
                () =>
                {
                    CloseRuleChoice();

                    unit.SetBattleShocked(
                        false,
                        0
                    );

                    AppendBattleLog(
                        "BATTLE-SHOCK",
                        label,
                        unit.DisplayName +
                        " was marked PASS by the player after resolving " +
                        diceCount +
                        "D6" +
                        (rollModifier == 0
                            ? ""
                            : " with modifier " +
                              rollModifier) +
                        "."
                    );

                    RefreshObjectiveDisplays();

                    if (completed != null)
                        completed();
                }
            )
        );

        options.Add(
            new RuleChoiceOption(
                "FAIL - BATTLE-SHOCKED",
                () =>
                {
                    CloseRuleChoice();

                    unit.SetBattleShocked(
                        true,
                        0
                    );

                    AppendBattleLog(
                        "BATTLE-SHOCK",
                        label,
                        unit.DisplayName +
                        " was marked FAIL after resolving " +
                        diceCount +
                        "D6" +
                        (rollModifier == 0
                            ? ""
                            : " with modifier " +
                              rollModifier) +
                        "."
                    );

                    RefreshObjectiveDisplays();

                    if (completed != null)
                        completed();
                }
            )
        );

        OpenRuleChoice(
            label,
            unit.DisplayName +
            ": resolve " +
            diceCount +
            "D6 manually" +
            (rollModifier == 0
                ? ""
                : " and apply " +
                  rollModifier +
                  " to the test") +
            ". Leadership " +
            leadership +
            "+. This faction-rule test deliberately does not offer Insane Bravery unless its source rule permits it.",
            options
        );
    }

    public void StandardAddKeyword(
        SquadController unit,
        string keyword)
    {
        if (unit == null ||
            unit.SourceData == null ||
            string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        string[] current =
            unit.SourceData.keywords ??
            new string[0];

        if (current.Any(
                value =>
                    string.Equals(
                        value,
                        keyword,
                        StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        unit.SourceData.keywords =
            current
                .Concat(
                    new[] { keyword })
                .ToArray();
    }

    public void StandardLog(
        string category,
        string title,
        string detail)
    {
        AppendBattleLog(
            category,
            title,
            detail
        );
    }
    private void DrawStandardFactionStratagemCards(
        float left,
        float right,
        float y,
        float cardWidth)
    {
        StandardFactionGameController controller =
            WarboardFactionExtensionHub
                .ControllerFor(
                    activeFaction
                );

        if (controller == null ||
            controller.Pack == null)
        {
            return;
        }

        List<StandardFactionStratagem11> cards =
            StandardFactionPack11
                .StratagemsFor(
                    controller.Pack,
                    controller.SelectedDetachments
                )
                .ToList();

        int shown =
            Mathf.Min(
                8,
                cards.Count
            );

        for (int index = 0;
             index < shown;
             index++)
        {
            StandardFactionStratagem11 card =
                cards[index];

            bool rightColumn =
                (index % 2) == 1;

            int row =
                index / 2;

            Rect rect =
                new Rect(
                    rightColumn
                    ? right
                    : left,
                    y + row * 54f,
                    cardWidth,
                    42f
                );

            DrawStratagemInfoCard(
                rect,
                (card.name ?? "STRATAGEM") +
                "  -  " +
                card.cost +
                " CP",
                card.FullRule
            );
        }

        float buttonY =
            y +
            Mathf.CeilToInt(
                shown / 2f
            ) *
            54f;

        string label =
            cards.Count > shown
            ? "OPEN ALL " +
              cards.Count +
              " FACTION STRATAGEMS / SPEND + LOG"
            : "OPEN FACTION STRATAGEMS / SPEND + LOG";

        if (GUI.Button(
            new Rect(
                left,
                buttonY,
                cardWidth * 2f + 16f,
                38f
            ),
            label))
        {
            showStratagemMenu = false;

            StandardFactionSetupUI
                .OpenFactionRules(
                    activeFaction
                );
        }
    }

    private bool StandardOfferFactionChargeReroll(
        SquadController attacker,
        SquadController target,
        int roll,
        bool wasRerolled)
    {
        if (attacker == null ||
            target == null ||
            wasRerolled ||
            !WarboardFactionExtensionHub
                .CanRerollCharge(
                    this,
                    attacker,
                    target))
        {
            return false;
        }

        // Traditional mode intentionally leaves all optional rerolls to the
        // players around the physical dice tray.
        if (!IsXcomMode)
            return false;

        int original = roll;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        options.Add(
            new RuleChoiceOption(
                "Re-roll the Charge roll",
                () =>
                {
                    CloseRuleChoice();

                    int reroll =
                        DiceRoller.Roll2D6(
                            "Faction Charge re-roll: " +
                            attacker.DisplayName
                        );

                    ResolveChargeRoll(
                        attacker,
                        target,
                        reroll,
                        true,
                        original
                    );
                }
            )
        );

        options.Add(
            new RuleChoiceOption(
                "Keep " + original,
                () =>
                {
                    CloseRuleChoice();

                    ResolveChargeRoll(
                        attacker,
                        target,
                        original,
                        true,
                        original
                    );
                }
            )
        );

        OpenRuleChoice(
            "OPTIONAL CHARGE RE-ROLL",
            attacker.DisplayName +
            " can re-roll this Charge roll (" +
            original +
            "). Choose before Warboard resolves the Charge move.",
            options
        );

        return true;
    }

    private bool StandardOfferPostAttackReaction(
        InteractiveAttackController attack,
        Action completed)
    {
        if (attack == null ||
            attack.Mode != AttackMode.Ranged ||
            attack.Target == null ||
            attack.TotalModelsKilled <= 0)
        {
            return false;
        }

        SquadController target =
            attack.Target
                .JoinedActionController();

        StandardFactionGameController
            controller =
                WarboardFactionExtensionHub
                    .ControllerFor(target);

        if (controller == null ||
            controller.PackId != "tyranids" ||
            !controller.HasDetachment(
                "UNENDING SWARM") ||
            !target.HasKeyword(
                "ENDLESS MULTITUDE") ||
            !target.IsAlive ||
            !target.IsOnBattlefield)
        {
            return false;
        }

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        options.Add(
            new RuleChoiceOption(
                "Make the Surge move",
                () =>
                {
                    CloseRuleChoice();

                    if (!IsXcomMode)
                    {
                        OpenTraditionalNumericPrompt(
                            "INSURMOUNTABLE ODDS",
                            target.DisplayName +
                            " can make a Surge move of up to D6 inches. Roll the D6 manually; enter the maximum distance rolled, then move any distance up to it (including 0).",
                            1,
                            6,
                            1,
                            1,
                            value =>
                                BeginSurgeMove(
                                    target,
                                    value,
                                    "Insurmountable Odds",
                                    completed
                                ),
                            completed
                        );

                        return;
                    }

                    int distance =
                        DiceRoller.RollD6(
                            "Insurmountable Odds: " +
                            target.DisplayName
                        );

                    BeginSurgeMove(
                        target,
                        distance,
                        "Insurmountable Odds",
                        completed
                    );
                }
            )
        );

        options.Add(
            new RuleChoiceOption(
                "Do not Surge",
                () =>
                {
                    CloseRuleChoice();

                    if (completed != null)
                        completed();
                }
            )
        );

        OpenRuleChoice(
            "UNENDING SWARM - INSURMOUNTABLE ODDS",
            target.DisplayName +
            " lost one or more models to the enemy unit's ranged attacks. It can make a Surge move of up to D6 inches.",
            options
        );

        return true;
    }

}
