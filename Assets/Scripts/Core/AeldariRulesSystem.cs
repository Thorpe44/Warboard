using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AeldariDetachment
{
    Warhost,
    WindriderHost,
    SpiritConclave,
    GuardianBattlehost,
    GhostsOfTheWebway,
    DevotedOfYnnead,
    SeerCouncil,
    AspectHost,
    ArmouredWarhost,
    FatefulPerformance,
    PathOfTheOutcast,
    TwilightFlickers,
    SerpentsBrood,
    EldritchRaiders,
    CorsairCoterie
}

public class AeldariStratagemDefinition
{
    public string Name;
    public int Cost;
    public string Summary;

    public AeldariStratagemDefinition(
        string name,
        int cost,
        string summary)
    {
        Name = name;
        Cost = cost;
        Summary = summary;
    }
}

public class AeldariDetachmentDefinition
{
    public AeldariDetachment Id;
    public string DisplayName;
    public string RuleName;
    public string RuleSummary;
    public string[] Enhancements;
    public AeldariStratagemDefinition[] Stratagems;
}

public class AeldariRulesSystem
{
    // WARBOARD_V38_MULTI_DETACHMENT

    // WARBOARD_V37_ROSTER_DRIVEN_DETACHMENT
    private readonly GameController game;

    private readonly Dictionary<string, AeldariDetachment>
        detachmentByFaction =
            new Dictionary<string, AeldariDetachment>();

    private readonly Dictionary<string, int>
        bonusBattleFocusByFaction =
            new Dictionary<string, int>();

    private readonly HashSet<string>
        aeldariFactions =
            new HashSet<string>();


    private static readonly Dictionary<AeldariDetachment, AeldariDetachmentDefinition>
        Definitions = BuildDefinitions();

    public AeldariRulesSystem(
        GameController owner)
    {
        game = owner;
    }

    private static AeldariStratagemDefinition S(
        string name,
        int cost,
        string summary)
    {
        return new AeldariStratagemDefinition(
            name,
            cost,
            summary
        );
    }

    private static AeldariDetachmentDefinition D(
        AeldariDetachment id,
        string display,
        string rule,
        string ruleSummary,
        string[] enhancements,
        params AeldariStratagemDefinition[] stratagems)
    {
        return new AeldariDetachmentDefinition
        {
            Id = id,
            DisplayName = display,
            RuleName = rule,
            RuleSummary = ruleSummary,
            Enhancements = enhancements ?? new string[0],
            Stratagems = stratagems ?? new AeldariStratagemDefinition[0]
        };
    }

    private static Dictionary<AeldariDetachment, AeldariDetachmentDefinition>
        BuildDefinitions()
    {
        Dictionary<AeldariDetachment, AeldariDetachmentDefinition> result =
            new Dictionary<AeldariDetachment, AeldariDetachmentDefinition>();

        result[AeldariDetachment.Warhost] = D(
            AeldariDetachment.Warhost,
            "Warhost",
            "Martial Grace",
            "+1 Battle Focus token each round; Swift as the Wind gains another +1 Move; Agile Manoeuvre D6 results gain +1.",
            new[] { "Phoenix Gem", "Timeless Strategist", "Gift of Foresight", "Psychic Destroyer" },
            S("Lightning-fast Reactions", 1, "Reactive: eligible Asuryani target is -1 to Hit for the phase."),
            S("Skyborne Sanctuary", 1, "End Fight: eligible unengaged Asuryani unit can embark in a nearby Transport."),
            S("Feigned Retreat", 1, "After Fall Back: unit can shoot and charge this turn."),
            S("Blitzing Firepower", 1, "Shooting: ranged weapons gain Sustained Hits 1 within 12; existing Sustained can crit on 5+."),
            S("Fire and Fade", 1, "After shooting: eligible infantry makes a D6+1 Normal move and cannot charge/embark this turn."),
            S("Webway Tunnel", 1, "End enemy Fight: eligible infantry near a board edge returns to Strategic Reserves.")
        );

        result[AeldariDetachment.WindriderHost] = D(
            AeldariDetachment.WindriderHost,
            "Windrider Host",
            "Ride the Wind",
            "Mounted/Vyper reserve flexibility, early Strategic Reserve arrival, end-enemy-turn extraction; Windriders gain Battleline.",
            new[] { "Firstdrawn Blade", "Mirage Field", "Seersight Strike", "Echoes of Ulthanesh" },
            S("Death from on High", 1, "Mounted/Vyper arriving from Reserves can re-roll Wound rolls this phase."),
            S("Overflight", 1, "End Shooting/Fight: qualifying Mounted unit that destroyed an enemy makes a Normal move up to 7."),
            S("Wind of Blades", 1, "Mounted/Vyper can shoot and charge after Advance or Fall Back this turn."),
            S("Daring Riders", 1, "Reserve setup can be >6 from enemies; setup within 8 prevents charging this turn."),
            S("Focused Firepower", 1, "Mounted/Vyper improves AP by 1 for the phase."),
            S("Spiralling Evasion", 1, "Reactive: Mounted/Vyper models gain a 4+ invulnerable save for the phase.")
        );

        result[AeldariDetachment.SpiritConclave] = D(
            AeldariDetachment.SpiritConclave,
            "Spirit Conclave",
            "Shepherds of the Dead",
            "Enemy units that destroy Asuryani Psykers gain Vengeful Dead; Wraith Constructs gain +1 Hit/+1 Wound into them. Nearby Spirit Guides grant Battle Focus to Wraithblades/Wraithguard/Wraithlords.",
            new[] { "Light of Clarity", "Stave of Kurnous", "Rune of Mists", "Higher Duty" },
            S("Seer's Eye", 1, "Psyker-guided Wraith Construct can ignore AP/Damage modifiers against a selected visible enemy."),
            S("Wraithbone Armour", 1, "Reactive: non-Titanic Wraith Construct reduces incoming Damage by 1."),
            S("Blades from Beyond", 1, "Fight: Wraithblades/Wraithlord/Wraithknight melee gains Devastating Wounds."),
            S("Soul Bridge", 1, "Command: Wraith unit counts as within 12 of selected Psyker for guidance rules."),
            S("Spirit Token", 1, "Start Move: secure an objective controlled by Wraithblades/Wraithguard."),
            S("Crushing Strides", 1, "After charge: roll model-dependent D6 pool; each 3+ inflicts 1 mortal wound.")
        );

        result[AeldariDetachment.GuardianBattlehost] = D(
            AeldariDetachment.GuardianBattlehost,
            "Guardian Battlehost",
            "Defend at All Costs",
            "Dire Avengers, Guardians, Support Weapons and War Walkers gain +1 Hit when attacker or target is on an objective.",
            new[] { "Craftworld's Champion", "Ethereal Pathway", "Protector of the Paths", "Breath of Vaul" },
            S("Warding Salvoes", 1, "Dire Avengers/Guardians re-roll Wound rolls against enemies on objectives."),
            S("Shield Nodes", 1, "Reactive: eligible unit on an objective is -1 to be Wounded for the phase."),
            S("Vaul's Vengeance", 1, "After a Guardian/Dire Avenger unit is destroyed, a War Walker can shoot the killer."),
            S("Time to Strike", 1, "Storm Guardians Advance 6 instead of rolling and can shoot/charge after advancing."),
            S("Blades of Asuryan", 1, "Dire Avengers/Guardians ranged weapons gain Pistol for the phase."),
            S("Cost of Victory", 1, "End enemy Fight: Guardians return to Strategic Reserves and restore destroyed Guardian models.")
        );

        result[AeldariDetachment.GhostsOfTheWebway] = D(
            AeldariDetachment.GhostsOfTheWebway,
            "Ghosts of the Webway",
            "Acrobatic Onslaught",
            "Harlequins can move through enemy models while making Charge moves; Troupes gain Battleline and OC 2.",
            new[] { "Cegorach's Coil", "Mask of Secrets", "Murder's Jest", "Mistweave" },
            S("Staged Death", 1, "Destroyed Harlequin Character returns at end of phase with half wounds, once per model."),
            S("Heroes' Fall", 1, "Reactive fight-on-death on 4+ for Harlequin models that have not fought."),
            S("Mocking Flight", 1, "Harlequins can shoot and charge after Falling Back this turn."),
            S("Tricksters' Retort", 1, "Reactive: nearby Troupe makes a Normal move up to 6 after an enemy move."),
            S("Bloody Dance", 1, "End enemy Charge: eligible Harlequin unit declares an out-of-turn charge without Charge bonus."),
            S("Exit the Stage", 1, "End enemy Fight: unengaged Harlequin unit returns to Strategic Reserves.")
        );

        result[AeldariDetachment.DevotedOfYnnead] = D(
            AeldariDetachment.DevotedOfYnnead,
            "Devoted of Ynnead",
            "Strength from Death",
            "Lethal Intent, Lethal Surge and Lethal Reprisal; Asuryani (except Epic Heroes) gain Ynnari; Yvraine and/or the Yncarne required.",
            new[] { "Gaze of Ynnead", "Storm of Whispers", "Borrowed Vigour", "Morbid Might" },
            S("Pall of Dread", 1, "Destroyed Ynnari unit can keep a previously controlled objective secured."),
            S("Macabre Resilience", 1, "Reactive: eligible Ynnari Infantry/Mounted target is -1 to be Wounded."),
            S("Emissaries of Ynnead", 1, "Fight: Ynnari Infantry re-rolls Hit rolls of 1, or all failed Hits if below Starting Strength."),
            S("Parting the Veil", 2, "Reactive: destroyed models that have not fought can fight after the attacker finishes."),
            S("Soulsight", 1, "Shooting: ranged weapons gain Lethal Hits and Ignores Cover."),
            S("Death Answers Death", 1, "End enemy Shooting: eligible Ynnari unit that lost models can shoot.")
        );

        result[AeldariDetachment.SeerCouncil] = D(
            AeldariDetachment.SeerCouncil,
            "Seer Council",
            "Strands of Fate",
            "Generate Fate dice at battle start; matching Fate die values can reduce the CP cost of the six Seer Council Stratagems by 1.",
            new[] { "Lucid Eye", "Runes of Warding", "Stone of Eldritch Fury", "Torc of Morai-Heg" },
            S("Presentiment of Dread", 1, "Command: visible enemy within 18 takes Battle-shock at -1."),
            S("Forewarned", 1, "Reactive Fight: eligible Asuryani Infantry near a Psyker is -1 to Hit and -1 to Wound."),
            S("Unshrouded Truth", 1, "Move: eligible infantry near a Psyker enters Strategic Reserves, gains Deep Strike and must ingress."),
            S("Fate Inescapable", 1, "Shooting: Ignores Cover; Critical Wounds improve AP by 1."),
            S("Isha's Fury", 1, "Reactive Movement: roll six D6; each 3+ inflicts 1 mortal wound."),
            S("Psychic Shield", 1, "Reactive Shooting: eligible infantry near a Psyker cannot be targeted from beyond 18.")
        );

        result[AeldariDetachment.AspectHost] = D(
            AeldariDetachment.AspectHost,
            "Aspect Host",
            "Path of the Warrior",
            "When an Aspect Warriors/Avatar unit shoots or fights, choose re-roll Hit rolls of 1 or re-roll Wound rolls of 1 for the phase.",
            new[] { "Aspect of Murder", "Mantle of Wisdom", "Shimmerstone", "Strategic Savant" },
            S("Warrior Focus", 1, "Ignore selected attack characteristic/Hit modifiers for the phase."),
            S("To Their Final Breath", 1, "Reactive fight-on-death on 4+, optionally boosted by an Aspect Shrine token."),
            S("Skyborne Sanctuary", 1, "End Fight: eligible Asuryani unit can embark in a nearby Transport."),
            S("Doom Inescapable", 1, "Avatar Wailing Doom ranged profile becomes Range 18, Damage 8 for the phase."),
            S("Preternatural Precision", 1, "Aspect ranged weapons gain selected Ignores Cover/Lethal Hits/Sustained Hits abilities."),
            S("Khaine's Vengeance", 1, "Enemy Infantry attempting Fall Back must take Desperate Escape tests.")
        );

        result[AeldariDetachment.ArmouredWarhost] = D(
            AeldariDetachment.ArmouredWarhost,
            "Armoured Warhost",
            "Skilled Crews",
            "Friendly Aeldari Vehicle ranged attacks have Assault.",
            new[] { "Spirit Stone of Raelyth", "Guiding Presence" },
            S("Layered Wards", 1, "Vehicle gains Feel No Pain 5+ against mortal wounds."),
            S("Soulsight", 1, "Vehicle attacks can re-roll one Hit, one Wound and one Damage roll."),
            S("Vectored Engines", 1, "Vehicle remains eligible to shoot after Falling Back.")
        );

        result[AeldariDetachment.FatefulPerformance] = D(
            AeldariDetachment.FatefulPerformance,
            "Fateful Performance",
            "Acrobatic Onslaught",
            "Harlequins can move through enemy models when charging; this detachment carries the Acrobatic tag.",
            new[] { "A Foot in the Future", "Mistweave" },
            S("Heroes' Fall", 1, "Reactive Harlequin fight-on-death on 4+."),
            S("Exit the Stage", 1, "End enemy Fight: unengaged Harlequin unit enters Strategic Reserves."),
            S("Deceptive Feint", 1, "Reactive: unengaged Harlequin Infantry makes a D3+3 Normal move after enemy approaches within 8.")
        );

        result[AeldariDetachment.PathOfTheOutcast] = D(
            AeldariDetachment.PathOfTheOutcast,
            "Path of the Outcast",
            "Far-Reaching Doom",
            "When Rangers/Shroud Runners shoot, enemy units have +6 detection range until that unit has shot.",
            new[] { "Camouflaged Snipers Upgrade", "Assassins' Eye Upgrade" },
            S("Eldritch Suppression", 1, "After shooting: a hit enemy takes Battle-shock, with -1 if a model was destroyed."),
            S("Casting Back the Veil", 1, "After shooting: selected hit enemy has +6 detection range."),
            S("Nomads of the Hidden Way", 1, "After shooting: Rangers/Shroud Runners make a D6 Normal move; cannot charge/embark.")
        );

        result[AeldariDetachment.TwilightFlickers] = D(
            AeldariDetachment.TwilightFlickers,
            "Twilight Flickers",
            "Dance of Distortion",
            "Friendly Harlequins units have Stealth; this detachment carries the Acrobatic tag.",
            new[] { "Shadowfall Masks Upgrade", "Prelude Performer" },
            S("Presaged Rehearsal", 1, "Fight: Troupe melee attacks gain Lance."),
            S("Captivating Performance", 1, "End Move: secure an objective controlled by a Troupe."),
            S("Phantasmal Mirage", 1, "After Harlequin Vehicle shoots: D6 Normal move; cannot charge this turn.")
        );

        result[AeldariDetachment.SerpentsBrood] = D(
            AeldariDetachment.SerpentsBrood,
            "Serpent's Brood",
            "Boons of the Brood",
            "Harlequin Mounted/Vehicle weapons have Sustained Hits 1; Harlequin units gain Sustained Hits 1 after disembarking for the turn.",
            new[] { "Key of Ghosts", "Weavers' Wail", "Fanged Leer", "Shedskin Raiment" },
            S("Fangs of the Brood", 1, "Troupe Dance of Death can select three abilities instead of one."),
            S("Venomous Wrath", 1, "After Harlequin Vehicle shoots: Normal move up to 6; cannot charge."),
            S("Striking Stride", 1, "Harlequin unit can charge after Advancing this turn."),
            S("Weavers' Coils", 1, "End Fight: Mounted unit makes Normal move, or Fall Back up to 6 if engaged."),
            S("Weaving Stride", 1, "Reactive: nearby Harlequin Infantry makes Normal move up to 6 after enemy move."),
            S("Skyward Lunge", 1, "End enemy Fight: unengaged Harlequin Vehicle/Mounted unit enters Strategic Reserves.")
        );

        result[AeldariDetachment.EldritchRaiders] = D(
            AeldariDetachment.EldritchRaiders,
            "Eldritch Raiders",
            "Yriel's Own",
            "Aeldari units can charge after Advancing. Anhrathe/Rangers/Shroud Runners can re-roll Advance rolls.",
            new[] { "Pirate Prince", "Alacritous Assault", "Exotic Munitions", "Adrenal Infusions" },
            S("Raiders' Spoils", 1, "Command: engaged Anhrathe models gain +1 OC until next Command."),
            S("Ruthless Killers", 1, "Corsair Voidscarred attacks gain +1 Damage for the phase."),
            S("Yriel's Example", 1, "Reactive Fight: Aeldari Infantry gains Feel No Pain 5+ for the phase."),
            S("No Prey Too Big", 1, "Eligible units gain +1 to Wound when Strength is below target Toughness."),
            S("Impeding Fire", 1, "Start enemy Charge: visible enemy within 36 suffers -2 to Charge rolls."),
            S("Withdraw and Reinforce", 1, "End enemy Fight: Anhrathe enters Strategic Reserves and restores non-Character models.")
        );

        result[AeldariDetachment.CorsairCoterie] = D(
            AeldariDetachment.CorsairCoterie,
            "Corsair Coterie",
            "Relentless Raiders",
            "Enemy units ending moves on your controlled objectives can suffer D3 mortal wounds on 2+; Anhrathe can secure controlled objectives.",
            new[] { "Infamy", "Webway Pathstone", "Archraider", "Voidstone" },
            S("Pirates' Due", 1, "Fight: re-roll Wound rolls of 1; Anhrathe into units on objectives can re-roll Wounds."),
            S("Lethal Ruse", 1, "After Fall Back: can charge; Anhrathe can inflict mortal wounds on the former engagement target."),
            S("Outcast Ambush", 1, "Rangers/Shroud Runners gain Ignores Cover, Rapid Fire 1 and +1 AP for the phase."),
            S("Into the Breach", 1, "After Anhrathe destroys an enemy by shooting: D6+1 Normal move."),
            S("Cloak and Shadow", 1, "Reactive: objective-holding Aeldari Infantry gains Stealth and cannot be targeted from beyond 18."),
            S("Vengeful Sorrow", 1, "After suffering Shooting casualties: eligible Aeldari Infantry makes a D6+1 Surge move.")
        );

        return result;
    }

    public void Configure(
        IList<SquadController> squads,
        IList<string> factions)
    {
        aeldariFactions.Clear();

        if (factions == null)
            return;

        foreach (string faction
            in factions)
        {
            if (string.IsNullOrWhiteSpace(
                    faction))
            {
                continue;
            }

            bool aeldari =
                squads != null &&
                squads.Any(
                    unit =>
                        unit != null &&
                        unit.FactionId == faction &&
                        IsAeldariUnit(unit)
                );

            if (!aeldari)
                continue;

            aeldariFactions.Add(
                faction
            );

            if (!detachmentByFaction.ContainsKey(
                    faction))
            {
                detachmentByFaction[faction] = AeldariDetachment.Warhost;
            }
        }
    }

    public bool IsAeldariFaction(
        string faction)
    {
        return
            !string.IsNullOrWhiteSpace(
                faction) &&
            aeldariFactions.Contains(
                faction
            );
    }

    private bool UnitNameContains(
        SquadController unit,
        string value)
    {
        return
            unit != null &&
            !string.IsNullOrWhiteSpace(
                unit.DisplayName) &&
            unit.DisplayName.IndexOf(
                value,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
    }

    public bool IsAeldariUnit(
        SquadController unit)
    {
        if (unit == null)
            return false;

        return
            unit.HasKeyword("aeldari") ||
            unit.HasKeyword("asuryani") ||
            unit.HasKeyword("ynnari") ||
            unit.HasKeyword("harlequins") ||
            unit.HasKeyword("anhrathe") ||
            unit.DisplayName.IndexOf(
                "Yvraine",
                StringComparison.OrdinalIgnoreCase
            ) >= 0 ||
            unit.DisplayName.IndexOf(
                "Yncarne",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
    }

    public AeldariDetachment GetDetachment(
        string faction)
    {
        AeldariDetachment legacy;

        if (!detachmentByFaction.TryGetValue(
                faction,
                out legacy))
        {
            legacy =
                AeldariDetachment.Warhost;
        }

        return AeldariDetachmentRuntime.Primary(
            faction,
            legacy);
    }

    public AeldariDetachmentDefinition GetDefinition(
        string faction)
    {
        return Definitions[
            GetDetachment(faction)
        ];
    }

    public void SetDetachment(
        string faction,
        AeldariDetachment detachment)
    {
        if (!IsAeldariFaction(faction))
            return;

        detachmentByFaction[
            faction] =
            detachment;
    }

    public string RuleSummary(
        string faction)
    {
        if (!IsAeldariFaction(faction))
            return "";

        AeldariDetachment[] selected =
            AeldariDetachmentRuntime
                .GetSelected(faction)
                .ToArray();

        if (selected.Length == 0)
            return "";

        return string.Join(
            " | ",
            selected
                .Select(
                    detachment =>
                    {
                        AeldariDetachmentDefinition definition =
                            Definitions[detachment];

                        return
                            definition.DisplayName +
                            "  -  " +
                            definition.RuleName +
                            ": " +
                            definition.RuleSummary;
                    })
                .ToArray());
    }

    public string DetachmentName(
        string faction)
    {
        if (!IsAeldariFaction(faction))
            return "";

        return string.Join(
            " + ",
            AeldariDetachmentRuntime
                .GetSelected(faction)
                .Select(
                    detachment =>
                        Definitions[detachment]
                            .DisplayName)
                .ToArray());
    }

    public AeldariStratagemDefinition[] Stratagems(
        string faction)
    {
        if (!IsAeldariFaction(faction))
            return new AeldariStratagemDefinition[0];

        return AeldariDetachmentRuntime
            .GetSelected(faction)
            .SelectMany(
                detachment =>
                    Definitions[detachment]
                        .Stratagems ??
                    new AeldariStratagemDefinition[0])
            .GroupBy(
                stratagem =>
                    stratagem.Name,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    public string[] Enhancements(
        string faction)
    {
        if (!IsAeldariFaction(faction))
            return new string[0];

        return AeldariDetachmentRuntime
            .GetSelected(faction)
            .SelectMany(
                detachment =>
                    Definitions[detachment]
                        .Enhancements ??
                    new string[0])
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool DetachmentIs(
        string faction,
        AeldariDetachment detachment)
    {
        return
            IsAeldariFaction(faction) &&
            AeldariDetachmentRuntime.Has(
                faction,
                detachment);
    }

    public void ApplyDetachmentKeywords(
        string faction,
        IList<SquadController> squads)
    {
        // v36: temporary keyword grants are owned by AeldariGameController so
        // imported roster keywords are never accidentally removed later.
        AeldariGameController controller =
            FactionControllerRuntime.GetAeldari(
                faction);

        if (controller != null)
        {
            controller.RefreshDetachmentState();
        }
    }

    public void StartBattleRound(
        string faction,
        IList<SquadController> squads)
    {
        if (!IsAeldariFaction(faction))
            return;

        int bonus =
            DetachmentIs(
                faction,
                AeldariDetachment.Warhost)
            ? 1
            : 0;

        if (squads != null)
        {
            bonus +=
                squads.Count(
                    unit =>
                        unit != null &&
                        unit.FactionId == faction &&
                        unit.IsAlive &&
                        FactionRuleSystem.UnitOrLeaderHasRule(
                            unit,
                            "Timeless Strategist"
                        )
                );
        }

        bonusBattleFocusByFaction[
            faction] =
            bonus;
    }

    public int BonusBattleFocus(
        string faction)
    {
        int value;

        return bonusBattleFocusByFaction
            .TryGetValue(
                faction,
                out value)
            ? value
            : 0;
    }

    public bool SpendBonusBattleFocus(
        string faction)
    {
        int value =
            BonusBattleFocus(
                faction
            );

        if (value <= 0)
            return false;

        bonusBattleFocusByFaction[
            faction] =
            value - 1;

        return true;
    }

    public int AgileD6Bonus(
        string faction)
    {
        return
            DetachmentIs(
                faction,
                AeldariDetachment.Warhost)
            ? 1
            : 0;
    }

    public float SwiftAsTheWindExtraMove(
        string faction)
    {
        return
            DetachmentIs(
                faction,
                AeldariDetachment.Warhost)
            ? 1f
            : 0f;
    }

    public bool GrantsBattleFocusFromSpiritGuides(
        SquadController unit,
        IList<SquadController> squads)
    {
        if (unit == null ||
            squads == null ||
            !DetachmentIs(
                unit.FactionId,
                AeldariDetachment.SpiritConclave))
        {
            return false;
        }

        if (!(unit.HasKeyword("wraithblades") ||
              unit.HasKeyword("wraithguard") ||
              unit.HasKeyword("wraithlord") ||
              UnitNameContains(
                  unit,
                  "Wraithblade") ||
              UnitNameContains(
                  unit,
                  "Wraithguard") ||
              UnitNameContains(
                  unit,
                  "Wraithlord")))
        {
            return false;
        }

        return squads.Any(
            psyker =>
                psyker != null &&
                psyker.IsAlive &&
                psyker.IsOnBattlefield &&
                psyker.FactionId ==
                    unit.FactionId &&
                psyker.HasKeyword("asuryani") &&
                psyker.HasKeyword("psyker") &&
                game.JoinedDistancePublic(
                    psyker,
                    unit
                ) <= 12.001f
        );
    }

    public void ApplyAttackModifiers(
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode,
        UniversalAttackRuleState state)
    {
        AeldariFactionPack11.ApplyAttackModifiers(
            game, attacker, target, weapon, mode, state);
}

    public int MinimumSustainedHits(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return AeldariFactionPack11.MinimumSustainedHits(
            attacker, weapon, mode);
}

    public bool GrantsLethalHits(
        SquadController attacker,
        AttackMode mode)
    {
        return AeldariFactionPack11.GrantsLethalHits(
            attacker, mode);
}

    public bool GrantsDevastatingWounds(
        SquadController attacker,
        AttackMode mode)
    {
        return AeldariFactionPack11.GrantsDevastatingWounds(
            attacker, null, mode);
}

    public int ApModifier(
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode)
    {
        return AeldariFactionPack11.ApModifier(
            attacker, target, weapon, mode);
}

    public int DamageModifier(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return AeldariFactionPack11.DamageModifier(
            attacker, weapon, mode);
}

    public int InvulnerableOverride(
        SquadController unit)
    {
        return AeldariFactionPack11.InvulnerableOverride(unit);
}

    public float RangedRangeModifier(
        SquadController attacker,
        WeaponData weapon)
    {
        return AeldariFactionPack11.RangedRangeModifier(
            attacker, weapon);
}

    public bool IgnoresCover(
        SquadController attacker)
    {
        return AeldariFactionPack11.GrantsIgnoresCover(
            attacker, AttackMode.Ranged);
}

    public bool IsPathOfWarriorUnit(
        SquadController unit)
    {
        return
            unit != null &&
            DetachmentIs(
                unit.FactionId,
                AeldariDetachment.AspectHost) &&
            (unit.HasKeyword("aspect warriors") ||
             unit.HasKeyword("avatar of khaine") ||
             UnitNameContains(
                 unit,
                 "Avatar of Khaine") ||
             UnitNameContains(
                 unit,
                 "Dire Avenger") ||
             UnitNameContains(
                 unit,
                 "Howling Banshee") ||
             UnitNameContains(
                 unit,
                 "Striking Scorpion") ||
             UnitNameContains(
                 unit,
                 "Fire Dragon") ||
             UnitNameContains(
                 unit,
                 "Dark Reaper") ||
             UnitNameContains(
                 unit,
                 "Swooping Hawk") ||
             UnitNameContains(
                 unit,
                 "Shining Spear") ||
             UnitNameContains(
                 unit,
                 "Warp Spider"));
    }

    public bool TreatReserveRoundAsOneHigher(
        SquadController unit)
    {
        return
            unit != null &&
            DetachmentIs(
                unit.FactionId,
                AeldariDetachment.WindriderHost
            ) &&
            (unit.HasKeyword("asuryani") &&
             (unit.HasKeyword("mounted") ||
              unit.HasKeyword("vyper") ||
              UnitNameContains(
                  unit,
                  "Vyper")));
    }

    public bool CanMoveThroughEnemyModelsWhenCharging(
        SquadController unit)
    {
        return AeldariFactionPack11.CanMoveThroughEnemyModelsWhenCharging(unit);
}

    public bool CanRerollAdvance(
        SquadController unit)
    {
        return AeldariFactionPack11.CanRerollAdvance(unit);
}

    public bool CanChargeAfterAdvance(
        SquadController unit)
    {
        return AeldariFactionPack11.CanChargeAfterAdvance(unit);
}

    public bool CanChargeAfterFallBack(
        SquadController unit)
    {
        return AeldariFactionPack11.CanChargeAfterFallBack(unit);
}

    public bool CanShootAfterFallBack(
        SquadController unit)
    {
        return AeldariFactionPack11.CanShootAfterFallBack(unit);
}

    public bool VehicleRangedHasAssault(
        SquadController unit)
    {
        return AeldariFactionPack11.VehicleRangedHasAssault(unit);
}

    public bool HasRange18Protection(
        SquadController unit)
    {
        return AeldariFactionPack11.HasRange18Protection(unit);
}
}
