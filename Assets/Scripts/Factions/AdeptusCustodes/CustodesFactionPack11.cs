// WARBOARD_V43_FULL_ADEPTUS_CUSTODES_FACTION_RULES
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class CustodesStratagem11
{
    public CustodesDetachment Detachment;
    public string Name = "";
    public int Cost;
    public string Category = "";
    public string When = "";
    public string Target = "";
    public string Effect = "";
    public string Restrictions = "";
    public int SourcePage;

    public string FullRule
    {
        get
        {
            string text =
                "WHEN: " + When +
                "\nTARGET: " + Target +
                "\nEFFECT: " + Effect;

            if (!string.IsNullOrWhiteSpace(
                    Restrictions))
            {
                text +=
                    "\nRESTRICTIONS: " +
                    Restrictions;
            }

            return text;
        }
    }
}

public sealed class CustodesEnhancement11
{
    public CustodesDetachment Detachment;
    public string Name = "";
    public int Points;
    public string Rule = "";
    public int SourcePage;
}

public sealed class CustodesDetachmentRule11
{
    public CustodesDetachment Detachment;
    public string Name = "";
    public string Rule = "";
    public int SourcePage;
}

/// <summary>
/// Adeptus Custodes Faction Pack, Edition 11, v1.1 July 2026.
/// Standard battle faction rules only: Army Rule, Detachments,
/// Enhancements and Stratagems. Crusade and Boarding Actions are not part of
/// the normal Warboard matched-play faction layer.
/// </summary>
public static class CustodesFactionPack11
{
    public const string Version =
        "Adeptus Custodes Faction Pack 11e v1.1 July 2026";

    private static readonly List<CustodesStratagem11>
        stratagems =
            new List<CustodesStratagem11>();

    private static readonly List<CustodesEnhancement11>
        enhancements =
            new List<CustodesEnhancement11>();

    private static readonly Dictionary<
        CustodesDetachment,
        CustodesDetachmentRule11
    > rules =
        new Dictionary<
            CustodesDetachment,
            CustodesDetachmentRule11>();

    static CustodesFactionPack11()
    {
        rules[CustodesDetachment.TalonsOfTheEmperor] =
            new CustodesDetachmentRule11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "Revered Companions",
                Rule = "ANATHEMA PSYKANA units gain Null Aegis (Aura): while an ADEPTUS CUSTODES unit is within 6\" of this unit, models in that unit have Feel No Pain 5+ against Psychic Attacks and mortal wounds. All other ADEPTUS CUSTODES units gain Deadly Unity (Aura): while an ANATHEMA PSYKANA unit is within 6\" of this unit, each time a model in that ANATHEMA PSYKANA unit makes an attack, add 1 to the Hit roll.",
                SourcePage = 3
            };
        rules[CustodesDetachment.ShieldHost] =
            new CustodesDetachmentRule11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "Martial Mastery",
                Rule = "At the start of the battle round, you can select one of two effects until the start of the next battle round: successful unmodified Hit rolls of 5+ score Critical Hits for melee attacks made by ADEPTUS CUSTODES models with Martial Ka’tah; or improve the Armour Penetration characteristic of their melee weapons by 1.",
                SourcePage = 7
            };
        rules[CustodesDetachment.NullMaidenVigil] =
            new CustodesDetachmentRule11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "Creeping Dread (Aura)",
                Rule = "In the Battle-shock step of the opponent’s Command phase, an enemy PSYKER unit or enemy unit below Starting Strength within 12\" of one or more ANATHEMA PSYKANA models must take a Battle-shock test. If that unit is Below Half-strength, subtract 1 from its Battle-shock test this phase instead. PROSECUTORS gain BATTLELINE.",
                SourcePage = 9
            };
        rules[CustodesDetachment.AuricChampions] =
            new CustodesDetachmentRule11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "Assemblage of Might",
                Rule = "At the start of your Command phase, select one unit from your opponent’s army. Until the start of your next Command phase, each time a model in an ADEPTUS CUSTODES CHARACTER unit from your army makes an attack that targets that enemy unit, add 1 to the Wound roll.",
                SourcePage = 12
            };
        rules[CustodesDetachment.SolarSpearhead] =
            new CustodesDetachmentRule11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "Auric Armour / Moritoi Ancients",
                Rule = "ADEPTUS CUSTODES VEHICLE units at Starting Strength (excluding AIRCRAFT and Battle-shocked units) have +2 OC. Below Starting Strength they re-roll Hit rolls of 1; Below Half-strength they also re-roll Wound rolls of 1. ADEPTUS CUSTODES WALKER units have +2\" Move and +1 to Advance and Charge rolls. In the Muster Armies step, select up to 2 ADEPTUS CUSTODES WALKER models to gain CHARACTER.",
                SourcePage = 16
            };
        rules[CustodesDetachment.LionsOfTheEmperor] =
            new CustodesDetachmentRule11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "Against All Odds",
                Rule = "Each time a model in an ADEPTUS CUSTODES unit from your army (excluding VEHICLES) makes an attack, if there are no other friendly units within 6\" of that unit, add 1 to the Hit roll and add 1 to the Wound roll.",
                SourcePage = 20
            };
        rules[CustodesDetachment.MightOfTheMoritoi] =
            new CustodesDetachmentRule11
            {
                Detachment = CustodesDetachment.MightOfTheMoritoi,
                Name = "March of the Honoured Dead",
                Rule = "Friendly ADEPTUS CUSTODES WALKER units have +2\" Move and +1 to Advance rolls and Charge rolls. This Detachment has the ARMOURY tag and cannot be taken with another ARMOURY Detachment.",
                SourcePage = 23
            };
        rules[CustodesDetachment.SilentHunters] =
            new CustodesDetachmentRule11
            {
                Detachment = CustodesDetachment.SilentHunters,
                Name = "Skin-Crawling Disorientation",
                Rule = "When a friendly ANATHEMA PSYKANA unit is selected to make an Advance move, that Advance does not prevent that unit from being eligible to start an Action. Friendly ANATHEMA PSYKANA units have Ceaseless Vigilance: in your Shooting phase, select one visible enemy unit within 12\"; that enemy unit is nulled and has +3\" detection range while nulled.",
                SourcePage = 25
            };
        rules[CustodesDetachment.TharanatoiHammerblow] =
            new CustodesDetachmentRule11
            {
                Detachment = CustodesDetachment.TharanatoiHammerblow,
                Name = "The Hammer Falls",
                Rule = "If a friendly ADEPTUS CUSTODES TERMINATOR unit made an ingress move this turn, that unit can re-roll Charge rolls. This Detachment has the LIONS tag and cannot be taken with another LIONS Detachment.",
                SourcePage = 27
            };
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "HUNT AS ONE",
                Cost = 1,
                Category = "TALONS OF THE EMPEROR – STRATEGIC PLOY STRATAGEM",
                When = "Start of your Movement phase.",
                Target = "Up to two ADEPTUS CUSTODES units from your army.",
                Effect = "Until the end of the turn, your units are eligible to shoot and/or declare a charge in a turn in which they Fell Back.",
                Restrictions = "You can only select two units if one (and only one) of them is an ANATHEMA PSYKANA unit and both are within 6\" of each other.",
                SourcePage = 5
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "TALONS INTERLOCKED",
                Cost = 1,
                Category = "TALONS OF THE EMPEROR – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase.",
                Target = "Up to two ADEPTUS CUSTODES INFANTRY units from your army, and one enemy unit that is an eligible target for all of those units.",
                Effect = "Until the end of the phase, your units can only target that enemy unit, but each time a model in one of your units makes a ranged attack, improve the Strength and Armour Penetration characteristics of that attack by 1.",
                Restrictions = "You can only select two units if one (and only one) of them is an ANATHEMA PSYKANA unit and both are within 6\" of each other.",
                SourcePage = 5
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "EMPYRIC SEVERANCE",
                Cost = 1,
                Category = "TALONS OF THE EMPEROR – BATTLE TACTIC STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
                Target = "One ADEPTUS CUSTODES unit from your army that was selected as the target of one or more of the attacking unit’s attacks, and one friendly ANATHEMA PSYKANA unit within 6\" of that ADEPTUS CUSTODES unit.",
                Effect = "Until the end of the phase, your unit has the Feel No Pain 4+ ability against Psychic attacks and mortal wounds.",
                Restrictions = "",
                SourcePage = 5
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "EMPEROR’S EXECUTIONERS",
                Cost = 2,
                Category = "TALONS OF THE EMPEROR – BATTLE TACTIC STRATAGEM",
                When = "Start of the Fight phase.",
                Target = "Up to two ADEPTUS CUSTODES units from your army.",
                Effect = "Until the end of the phase, each time a model in one of your units targets an enemy unit that is below its Starting Strength, add 1 to the Wound roll.",
                Restrictions = "You can only select two units if one (and only one) of them is an ANATHEMA PSYKANA unit and both are within 6\" of each other.",
                SourcePage = 6
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "TALONED PINCER",
                Cost = 1,
                Category = "TALONS OF THE EMPEROR – BATTLE TACTIC STRATAGEM",
                When = "Your opponent’s Movement phase, just after an enemy unit ends a Normal, Advance or Fall Back move.",
                Target = "Up to two ADEPTUS CUSTODES units from your army that are within 8\" of that enemy unit.",
                Effect = "Your units can make a Normal move of up to 6\".",
                Restrictions = "You cannot select units that are within Engagement Range of one or more enemy units. You can only select two units if one (and only one) of them is an ANATHEMA PSYKANA unit and both are within 6\" of each other.",
                SourcePage = 6
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "SHIELD OF HONOUR",
                Cost = 1,
                Category = "TALONS OF THE EMPEROR – EPIC DEED STRATAGEM",
                When = "Your opponent’s Shooting phase, just after an enemy unit has selected its targets.",
                Target = "One ANATHEMA PSYKANA INFANTRY unit from your army that was selected as the target of one or more of the attacking unit’s attacks, and one other friendly ADEPTUS CUSTODES INFANTRY unit (excluding ANATHEMA PSYKANA units) within 6\" of that ANATHEMA PSYKANA INFANTRY unit.",
                Effect = "Until the end of the phase, any attack that targets your ANATHEMA PSYKANA unit must instead target your other ADEPTUS CUSTODES unit (unless it is not an eligible target).",
                Restrictions = "",
                SourcePage = 6
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "ARCANE GENETIC ALCHEMY",
                Cost = 1,
                Category = "SHIELD HOST – BATTLE TACTIC STRATAGEM",
                When = "Any phase, just after a mortal wound has been allocated to an ADEPTUS CUSTODES model from your army(excluding ANATHEMA PSYKANA models).",
                Target = "That ADEPTUS CUSTODES model’s unit.",
                Effect = "Until the end of the phase, models in your unit have the Feel No Pain 4+ ability against mortal wounds.",
                Restrictions = "",
                SourcePage = 8
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "AVENGE THE FALLEN",
                Cost = 1,
                Category = "SHIELD HOST – STRATEGIC PLOY STRATAGEM",
                When = "Start of the Fight phase.",
                Target = "One ADEPTUS CUSTODES unit from your army (excluding ANATHEMA PSYKANA units) that is below its Starting Strength.",
                Effect = "Until the end of the phase, add 1 to the Attacks characteristic of melee weapons equipped by models in that unit. If your unit is Below Half-strength, until the end of the phase, add 2 to the Attacks characteristic of those melee weapons instead.",
                Restrictions = "",
                SourcePage = 8
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "UNWAVERING SENTINELS",
                Cost = 1,
                Category = "SHIELD HOST – STRATEGIC PLOY STRATAGEM",
                When = "Fight phase, just after an enemy unit has selected its targets.",
                Target = "One ADEPTUS CUSTODES INFANTRY unit from your army (excluding ANATHEMA PSYKANA units) that is within range of an objective marker you control and that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, each time a melee attack targets your unit, subtract 1 from the Hit roll.",
                Restrictions = "",
                SourcePage = 8
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "MULTIPOTENTIALITY",
                Cost = 1,
                Category = "SHIELD HOST – STRATEGIC PLOY STRATAGEM",
                When = "Your Movement phase.",
                Target = "One ADEPTUS CUSTODES unit from your army that Fell Back this phase.",
                Effect = "Until the end of your turn, that unit is eligible to shoot and declare a charge in a turn in which it Fell Back.",
                Restrictions = "",
                SourcePage = 8
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "VIGILANCE ETERNAL",
                Cost = 1,
                Category = "SHIELD HOST – STRATEGIC PLOY STRATAGEM",
                When = "Your Movement phase.",
                Target = "One ADEPTUS CUSTODES BATTLELINE unit from your army (excluding ANATHEMA PSYKANA units) within range of an objective marker you control.",
                Effect = "That objective marker remains under your control even if you have no models within range of it, until your opponent controls it at the start or end of any turn.",
                Restrictions = "",
                SourcePage = 9
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "ARCHEOTECH MUNITIONS",
                Cost = 1,
                Category = "SHIELD HOST – WARGEAR STRATAGEM",
                When = "Your Shooting phase.",
                Target = "One ADEPTUS CUSTODES unit from your army (excluding ANATHEMA PSYKANA units) that has not been selected to shoot this phase.",
                Effect = "Select either the [LETHAL HITS] or [SUSTAINED HITS 1] ability. Until the end of the phase ranged weapons equipped by models in your unit have the selected ability.",
                Restrictions = "",
                SourcePage = 9
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "DESPERATION’S PRICE",
                Cost = 1,
                Category = "NULL MAIDEN VIGIL – STRATEGIC PLOY STRATAGEM",
                When = "Any phase, just after an enemy PSYKER unit has either finished using a Psychic ability that targets a unit, or finished making Psychic Attacks.",
                Target = "One ANATHEMA PSYKANA unit from your army within 18\" of that enemy PSYKER unit.",
                Effect = "That enemy PSYKER unit must take a Leadership test If the test is passed, that PSYKER unit is Battle-shocked; if the test is failed that PSYKER unit suffers 3 mortal wounds and is Battle-shocked.",
                Restrictions = "",
                SourcePage = 11
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "WITCH HUNTERS",
                Cost = 1,
                Category = "NULL MAIDEN VIGIL – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase or the Fight phase.",
                Target = "One ANATHEMA PSYKANA unit from your army that has not been selected to shoot or fight this phase.",
                Effect = "Select either the [LETHAL HITS] or [SUSTAINED HITS 1] ability. Until the end of the phase, weapons equipped by models in your unit have the selected ability, but models in your unit can only target PSYKER units with their attacks.",
                Restrictions = "",
                SourcePage = 11
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "ANATHEMA BLADEMASTERY",
                Cost = 1,
                Category = "NULL MAIDEN VIGIL – BATTLE TACTIC STRATAGEM",
                When = "Fight phase.",
                Target = "One VIGILATORS unit from your army that has not been selected to fight this phase.",
                Effect = "Until the end of the phase, each time a model in your unit makes a melee attack, you can re-roll the Hit roll If the target of that attack is Battle-shocked or a PSYKER, you can re-roll the Wound roll as well.",
                Restrictions = "",
                SourcePage = 11
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "PSY-CHAFF VOLLEY",
                Cost = 1,
                Category = "NULL MAIDEN VIGIL – STRATEGIC PLOY STRATAGEM",
                When = "Your Shooting phase.",
                Target = "One PROSECUTORS unit from your army that has just shot.",
                Effect = "Select one enemy unit hit by one or more of those attacks. Until the start of your next turn, while your unit is on the battlefield, that enemy unit is prosecuted. While a unit is prosecuted, each time an ANATHEMA PSYKANA model makes an attack against that unit, improve the Armour Penetration characteristic of that attack by 1. While a PSYKER or Battle- shocked unit is prosecuted, each time a model in that unit makes an attack, subtract 1 from the Hit roll.",
                Restrictions = "",
                SourcePage = 12
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "PURGATION SWEEP",
                Cost = 1,
                Category = "NULL MAIDEN VIGIL – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase.",
                Target = "One WITCHSEEKERS unit from your army that has not been selected to shoot this phase.",
                Effect = "Until the end of the phase, add 1 to the Attacks characteristic of Torrent weapons equipped by models in your unit. If such a weapon targets a PSYKER or Battle-shocked unit this phase, add 2 to its Attacks characteristic instead.",
                Restrictions = "",
                SourcePage = 12
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "PSYCHIC ABOMINATIONS",
                Cost = 1,
                Category = "NULL MAIDEN VIGIL – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Shooting phase, just after an enemy unit has selected its targets.",
                Target = "One ANATHEMA PSYKANA INFANTRY unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, your unit has the Stealth ability, and Battle-shocked and PSYKER models can only select your unit as a target of a ranged attack if they are within 12\".",
                Restrictions = "",
                SourcePage = 12
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "SLAYER OF CHAMPIONS",
                Cost = 1,
                Category = "AURIC CHAMPIONS – EPIC DEED STRATAGEM",
                When = "Any phase.",
                Target = "One ADEPTUS CUSTODES CHARACTER unit from your army that has just destroyed the unit you selected at the start of your Command phase as the target of your Assemblage of Might ability.",
                Effect = "Select one enemy unit on the battlefield Until the start of your next Command phase, each time an ADEPTUS CUSTODES CHARACTER model from your army makes an attack that target that enemy unit, add 1 to the Wound roll In addition, if the destroyed unit was a CHARACTER unit, gain 1CP.",
                Restrictions = "",
                SourcePage = 14
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "SUPERHUMAN RESERVES",
                Cost = 2,
                Category = "AURIC CHAMPIONS – EPIC DEED STRATAGEM",
                When = "Any phase, just after an ADEPTUS CUSTODES WARLORD model from your army has used an ability on its datasheet or from an Enhancement that says it can only be used Once per battle.",
                Target = "That ADEPTUS CUSTODES WARLORD model.",
                Effect = "Your model can use its Once per battle’ ability one additional time during this battle (but not in the same phase).",
                Restrictions = "You cannot use this Stratagem more than once per battle.",
                SourcePage = 14
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "THE EMPEROR’S AUSPICE",
                Cost = 1,
                Category = "AURIC CHAMPIONS – EPIC DEED STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
                Target = "One ADEPTUS CUSTODES CHARACTER unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, CHARACTER models in your unit have the Feel No Pain 4+ ability.",
                Restrictions = "",
                SourcePage = 14
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "EARNING OF A NAME",
                Cost = 1,
                Category = "AURIC CHAMPIONS – EPIC DEED STRATAGEM",
                When = "Fight phase.",
                Target = "Up to two ADEPTUS CUSTODES CHARACTER units from your army that have not been selected to fight this phase.",
                Effect = "Until the end of the phase, each time a CHARACTER model in either of your units makes an attack that targets a MONSTER or VEHICLE unit, you can re-roll the Hit roll and you can re-roll the Wound roll.",
                Restrictions = "",
                SourcePage = 15
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "VIGIL UNENDING",
                Cost = 2,
                Category = "AURIC CHAMPIONS – EPIC DEED STRATAGEM",
                When = "Fight phase.",
                Target = "One ADEPTUS CUSTODES CHARACTER model from your army that was just destroyed and has not fought this phase. You can use this Stratagem on that unit even though it was just destroyed.",
                Effect = "Do not remove your destroyed model from play. The destroyed model can fight after the attacking unit has finished making attacks, and is then removed from play.",
                Restrictions = "",
                SourcePage = 15
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "SHOULDER THE MANTLE",
                Cost = 1,
                Category = "AURIC CHAMPIONS – EPIC DEED STRATAGEM",
                When = "Your Movement phase, before the Reinforcements step.",
                Target = "One ADEPTUS CUSTODES CHARACTER model from your army that is not leading a unit.",
                Effect = "Select one friendly unit (excluding Battle-shocked and Attached units) within 2\" horizontally and 5\" vertically of your model that it could lead (as described in the Leader section of its datasheet). Your model ataches to that unit as a Leader. Change that unit’s Starting Strength accordingly.",
                Restrictions = "",
                SourcePage = 15
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "FLAWLESS CONSTRUCTION",
                Cost = 1,
                Category = "SOLAR SPEARHEAD – BATTLE TACTIC STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
                Target = "One ADEPTUS CUSTODES VEHICLE unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, each time an attack targets a model in your unit, if the Strength characteristic of that attack is greater than the Toughness characteristic of your unit, subtract 1 from the Wound roll.",
                Restrictions = "",
                SourcePage = 18
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "EMPEROR’S VENGEANCE",
                Cost = 1,
                Category = "SOLAR SPEARHEAD – BATTLE TACTIC STRATAGEM",
                When = "Fight phase, just after an enemy unit has selected its targets.",
                Target = "One ADEPTUS CUSTODES unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, each time a model in your unit is destroyed, if that model has not fought this phase, roll one D6, adding 1 to the result if your unit has the WALKER keyword. On a 4+, do not remove it from play; The destroyed model can fight after the attacking unit has finished making its attacks (when doing so, it is assumed to have 1 wound remaining), and is then removed from play.",
                Restrictions = "",
                SourcePage = 18
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "WRATHFUL ADVANCE",
                Cost = 1,
                Category = "SOLAR SPEARHEAD – BATTLE TACTIC STRATAGEM",
                When = "Fight phase, just before an ADEPTUS CUSTODES unit from your army Piles In.",
                Target = "That ADEPTUS CUSTODES unit.",
                Effect = "Until the end of the phase, each time a model in your unit makes a Pile-in move, it can move up to D3+3\" instead of up to 3\".",
                Restrictions = "",
                SourcePage = 18
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "UNSTOPPABLE",
                Cost = 1,
                Category = "SOLAR SPEARHEAD – STRATEGIC PLOY STRATAGEM",
                When = "Your Movement phase or your Charge phase.",
                Target = "One ADEPTUS CUSTODES VEHICLE or ADEPTUS CUSTODES MOUNTED unit from your army.",
                Effect = "Until the end of the phase, each time a model in your unit makes a move, it can move through terrain features.",
                Restrictions = "",
                SourcePage = 19
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "RELENTLESS PERSECUTION",
                Cost = 1,
                Category = "SOLAR SPEARHEAD – STRATEGIC PLOY STRATAGEM",
                When = "Your Movement phase, just after an ADEPTUS CUSTODES VEHICLE unit from your army Advances.",
                Target = "That ADEPTUS CUSTODES VEHICLE unit.",
                Effect = "Until the end of the turn, your unit is eligible to shoot in a turn in which it Advanced. If your unit has the WALKER keyword, until the end of the turn, your unit is eligible to shoot and declare a charge in a turn in which it Advanced instead.",
                Restrictions = "",
                SourcePage = 19
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "PUNISHMENT INESCAPABLE",
                Cost = 1,
                Category = "SOLAR SPEARHEAD – STRATEGIC PLOY STRATAGEM",
                When = "Your Shooting phase.",
                Target = "One ADEPTUS CUSTODES unit from your army that has not been selected to shoot this phase.",
                Effect = "Until the end of the phase, ranged weapons equipped by models in your unit have the [IGNORES COVER] ability, and until the end of the phase, each time a model in your unit makes an attack, you can ignore any or all modifiers to that attack’s Ballistic Skill characteristic and/or any or all modifiers to the Hit roll.",
                Restrictions = "",
                SourcePage = 19
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "GILDED CHAMPION",
                Cost = 1,
                Category = "LIONS OF THE EMPEROR – STRATEGIC PLOY STRATAGEM",
                When = "Any phase, just after an ADEPTUS CUSTODES CHARACTER model from your army has used an ability on its datasheet that states it can only be used ‘once per battle’.",
                Target = "That ADEPTUS CUSTODES CHARACTER model.",
                Effect = "Your model can use that ‘once per battle’ ability one additional time during the battle (but not in the same phase).",
                Restrictions = "You cannot use this Stratagem on the same ADEPTUS CUSTODES CHARACTER model more than once per battle.",
                SourcePage = 21
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "DEFIANT TO THE LAST",
                Cost = 1,
                Category = "LIONS OF THE EMPEROR – STRATEGIC PLOY STRATAGEM",
                When = "Fight phase, just after an enemy unit has selected its targets.",
                Target = "One ADEPTUS CUSTODES unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, each time a model in your unit is destroyed, if that model has not fought this phase, roll one D6, adding 2 to the result if that model has the CHARACTER keyword. On a 4+, do not remove it from play; the destroyed model can fight after the attacking unit has finished making its attacks (when doing so, it is treated as having 1 wound remaining), and is then removed from play.",
                Restrictions = "",
                SourcePage = 21
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "PEERLESS WARRIOR",
                Cost = 1,
                Category = "LIONS OF THE EMPEROR – BATTLE TACTIC STRATAGEM",
                When = "Fight phase.",
                Target = "One ADEPTUS CUSTODES unit from your army that has not been selected to fight this phase.",
                Effect = "Until the end of the phase, melee weapons equipped by models in your unit have the [PRECISION] ability.",
                Restrictions = "",
                SourcePage = 21
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "UNLEASH THE LIONS",
                Cost = 1,
                Category = "LIONS OF THE EMPEROR – STRATEGIC PLOY STRATAGEM",
                When = "Your Command phase.",
                Target = "One ALLARUS CUSTODIANS or AQUILON CUSTODIANS unit from your army that is on the battlefield.",
                Effect = "That unit is split into separate units, each containing one model. These new units each have a Starting Strength of 1.",
                Restrictions = "",
                SourcePage = 22
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "MANOEUVRE AND FIRE",
                Cost = 1,
                Category = "LIONS OF THE EMPEROR – STRATEGIC PLOY STRATAGEM",
                When = "Your Movement phase, just after an ADEPTUS CUSTODES unit from your army Falls Back.",
                Target = "That ADEPTUS CUSTODES unit.",
                Effect = "Until the end of the turn, your unit is eligible to shoot and declare a charge in a turn in which it Fell Back.",
                Restrictions = "",
                SourcePage = 22
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "SWIFT AS THE EAGLE",
                Cost = 1,
                Category = "LIONS OF THE EMPEROR – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Shooting phase, just after an enemy unit has shot.",
                Target = "One ADEPTUS CUSTODES unit from your army (excluding VEHICLE units) that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Your unit can make a Normal move of up to D6\".",
                Restrictions = "",
                SourcePage = 22
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.MightOfTheMoritoi,
                Name = "FLAWLESS CONSTRUCTION",
                Cost = 1,
                Category = "MIGHT OF THE MORITOI STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, when an enemy unit targets a friendly ADEPTUS CUSTODES WALKER unit.",
                Target = "That ADEPTUS CUSTODES WALKER unit.",
                Effect = "Attacks that target your unit with a S greater than your unit’s T have -1 to wound rolls.",
                Restrictions = "",
                SourcePage = 24
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.MightOfTheMoritoi,
                Name = "UNSTOPPABLE ADVANCE",
                Cost = 1,
                Category = "MIGHT OF THE MORITOI STRATAGEM",
                When = "Your Movement phase, when a friendly ADEPTUS CUSTODES WALKER unit is selected to move.",
                Target = "That ADEPTUS CUSTODES WALKER unit.",
                Effect = "Your unit has MOBILE.",
                Restrictions = "",
                SourcePage = 24
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.MightOfTheMoritoi,
                Name = "PRIORITISED ERADICATION",
                Cost = 1,
                Category = "MIGHT OF THE MORITOI STRATAGEM",
                When = "Your Shooting phase, when a friendly TELEMON HEAVY DREADNOUGHT unit is selected to shoot.",
                Target = "That TELEMON HEAVY DREADNOUGHT unit.",
                Effect = "Your unit’s: ▪ Arachnus Storm Cannon weapons have [RAPID FIRE 6]. ▪ Iliastus Accelerator Culverin weapons have [RAPID FIRE 2].",
                Restrictions = "",
                SourcePage = 24
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.SilentHunters,
                Name = "DEATHSONG SCYTHES",
                Cost = 1,
                Category = "SILENT HUNTERS STRATAGEM",
                When = "Fight phase, when a friendly VIGILATORS unit is selected to fight.",
                Target = "That VIGILATORS unit.",
                Effect = "▪ Your unit’s melee attacks have [LANCE]. ▪ Your unit’s melee attacks that target a PSYKER unit have +1 A.",
                Restrictions = "",
                SourcePage = 26
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.SilentHunters,
                Name = "UMBRAL PROSECUTION",
                Cost = 1,
                Category = "SILENT HUNTERS STRATAGEM",
                When = "Your Shooting phase, when a friendly PROSECUTORS unit is selected to shoot.",
                Target = "That PROSECUTORS unit.",
                Effect = "Your unit’s Boltgun weapons have: ▪ [RAPID FIRE 2]. ▪ +1 AP.",
                Restrictions = "",
                SourcePage = 26
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.SilentHunters,
                Name = "SYNCHRONISED INFERNO",
                Cost = 1,
                Category = "SILENT HUNTERS STRATAGEM",
                When = "Your Shooting phase, when a friendly WITCHSEEKERS unit is selected to shoot.",
                Target = "That WITCHSEEKERS unit.",
                Effect = "Your unit’s [TORRENT] ranged attacks have [BLAST 1].",
                Restrictions = "",
                SourcePage = 26
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.TharanatoiHammerblow,
                Name = "HARDENED RESOLVE",
                Cost = 1,
                Category = "THARANATOI HAMMERBLOW STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, when an enemy unit targets a friendly ADEPTUS CUSTODES TERMINATORADEPTUS CUSTODES TERMINATOR unit.",
                Target = "That ADEPTUS CUSTODES TERMINATOR unit.",
                Effect = "Your unit has +1 T.",
                Restrictions = "",
                SourcePage = 28
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.TharanatoiHammerblow,
                Name = "UNLEASH THE LIONS",
                Cost = 1,
                Category = "THARANATOI HAMMERBLOW STRATAGEM",
                When = "Your Command phase.",
                Target = "One friendly ALLARUS CUSTODIANS/AQUILON CUSTODIANS unit that is on the battlefield.",
                Effect = "Your unit is split into separate units, each containing one model. These new units each have a starting strength of 1.",
                Restrictions = "",
                SourcePage = 28
            });
        stratagems.Add(
            new CustodesStratagem11
            {
                Detachment = CustodesDetachment.TharanatoiHammerblow,
                Name = "ELECTROEXORCIST SATURATION",
                Cost = 1,
                Category = "THARANATOI HAMMERBLOW STRATAGEM",
                When = "Your Shooting phase, when a friendly ADEPTUS CUSTODES TERMINATOR unit is selected to shoot.",
                Target = "That ADEPTUS CUSTODES TERMINATOR unit.",
                Effect = "Your unit’s Ballistus Grenade Launcher weapons have D3+3 A.",
                Restrictions = "",
                SourcePage = 28
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "AEGIS PROJECTOR",
                Points = 20,
                Rule = "This archeotech field projector triggers in response to autopremonitory danger input. It pulse-casts a temporary force field that can stave off even the most powerful attacks, before charging ready for another use. ADEPTUS CUSTODES model only. Once per turn, the first time a saving throw is failed for the bearer’s unit, change the Damage characteristic of that attack to 0.",
                SourcePage = 4
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "CHAMPION OF THE IMPERIUM",
                Points = 25,
                Rule = "This leader is amongst the finest martial champions in all the Emperor's realm, and their mere presence inspires their followers to remarkable eﬀorts. ADEPTUS CUSTODES model only. The range of the bearer’s Null Aegis or Deadly Unity ability is increased to 9\".",
                SourcePage = 4
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "GIFT OF TERRAN ARTIFICE",
                Points = 15,
                Rule = "This warrior wields a close-quarters weapon craed by the finest noble artisans of Terra, the workmanship of which is magnificent in its lethality. ADEPTUS CUSTODES model only. Each time the bearer makes a melee attack, add 1 to the Wound roll.",
                SourcePage = 4
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.TalonsOfTheEmperor,
                Name = "RADIANT MANTLE",
                Points = 30,
                Rule = "The golden glory of the Emperor himself glows around this magnificent warrior like Sol's own fire. Enemies are blinded by its light, forced to recoil in pain and terror. ADEPTUS CUSTODES model only. Each time an attack targets the bearer’s unit, if the attacking model is within 12\", subtract 1 from the Hit roll.",
                SourcePage = 4
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "AURIC MANTLE",
                Points = 15,
                Rule = "This auramite weave garment is draped about its wearers shoulders before they don their battle armour. It forms a final, incredibly resilient layer of protection for them should all their other defences fail. SHIELD-CAPTAIN or BLADE CHAMPION model only. Add 2 to the bearer’s Wounds characteristic.",
                SourcePage = 7
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "CASTELLAN’S MARK",
                Points = 20,
                Rule = "This finely worked pauldron is awarded to whichever living Custodian currently holds the greatest tally of victories in the Blood Games. The one who bears the Castellan 's Mark is guaranteed to be a superlative strategic genius. SHIELD-CAPTAIN model only. Afer both players have deployed their armies, you can select up to two ADEPTUS CUSTODES units from your army (excluding ANATHEMA PSYKANA units) and redeploy all of those units. When doing so, any of those units can be placed into Strategic Reserves, regardless of how many units are already in Strategic Reserves.",
                SourcePage = 7
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "FROM THE HALL OF ARMOURIES",
                Points = 20,
                Rule = "The racks of the Adeptus Custodes’ armouries yield up some of the most finely craed close-quarters weaponry borne by any soldiers of the Imperium. SHIELD-CAPTAIN model only. Add 1 to the Strength and Damage characteristics of the bearer’s melee weapons.",
                SourcePage = 7
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.ShieldHost,
                Name = "PANOPTISPEX",
                Points = 5,
                Rule = "This incredibly advanced precursor to Imperial auspicators is able to see through solid objects and into esoteric spectra, meaning no malcontent against the Throne can ever hide from its bearer’s gaze. SHIELD-CAPTAIN or BLADE CHAMPION model only. While the bearer is leading a unit, ranged weapons equipped by models in that unit have the [IGNORES COVER] ability.",
                SourcePage = 7
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "ENHANCED VOIDSHEEN CLOAK",
                Points = 10,
                Rule = "Now rare, enhanced voidsheen cloaks were worn by commanders of the Sisters of Silence during the Great Crusade. They are made from micro-vitrious mesh designed to diﬀract and absorb attacks, and include inbuilt refractor fields. ANATHEMA PSYKANA model only. Each time an attack is allocated to the bearer, subtract 1 from the Damage characteristic of that attack. If that attack was made by a PSYKER or Battle-shocked model, change the Damage characteristic of that attack to 1 instead.",
                SourcePage = 10
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "HUNTRESS’ EYE",
                Points = 15,
                Rule = "This ancient bionic acts like a miniature animus speculum, focusing the bearers null abilities into a stare that can literally terrify foes to death. ANATHEMA PSYKANA model only. In your Command phase, select one enemy unit within 12\" of the bearer. That unit must take a Battle- shock test.",
                SourcePage = 10
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "OBLIVION KNIGHT",
                Points = 25,
                Rule = "Oblivion Knights are among the most powerful and experienced of the Sisters of Silence. When one leads their sisters in the field, witches must truly beware. ANATHEMA PSYKANA model only. While the bearer is leading a unit, each time a model in that unit makes an attack, add 1 to the Hit roll. If that attack targeted an enemy PSYKER unit, add 1 to the Wound roll as well.",
                SourcePage = 10
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.NullMaidenVigil,
                Name = "RAPTOR BLADE",
                Points = 5,
                Rule = "The Raptor Blade is an ancient relic of the Null Maidens, as razor sharp now as it was on the day of its forging and ever the bane of witches. ANATHEMA PSYKANA model only. Add 1 to the Attacks, Strength and Damage characteristics of the bearers melee weapons. While the bearer is within Engagement Range of one or more enemy PSYKER units that are Battle-shocked, add 2 to the Attacks, Strength and Damage characteristics of the bearer’s melee weapons instead.",
                SourcePage = 10
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "BLADE IMPERATOR",
                Points = 25,
                Rule = "This warrior is the Emperors own wrath made manifest, the living weapon of the Master of Mankind. ADEPTUS CUSTODES model only. Each time the bearer’s unit ends a Charge move, select one enemy unit within Engagement Range of the bearer and roll one D6: on a 4+, that enemy unit suffers D3 mortal wounds. Once per battle, after the bearer s unit ends a Charge move, all enemy units within 6\" of the bearer must take a Battle-shock test.",
                SourcePage = 13
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "INSPIRATIONAL EXEMPLAR",
                Points = 10,
                Rule = "Here is a singular being even the mighty Custodians can look up to and be inspired by ADEPTUS CUSTODES model only. The bearer has a Leadership characteristic of 5+. Once per battle, at the start of any phase, you can select one friendly ADEPTUS CUSTODES unit that is Battle-shocked and within 12\" of the bearer; that unit is no longer Battle-shocked.",
                SourcePage = 13
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "MARTIAL PHILOSOPHER",
                Points = 30,
                Rule = "Few in the history of the Imperium have been so skilled in reading - and directing- the ebb and flow of battle. ADEPTUS CUSTODES model only. The bearer’s unit is eligible to shoot and/or declare a charge in a turn in which it Fell Back. Once per battle, in your opponent's Movement phase, when an enemy unit ends a Normal, Advance or Fall Back move within 8\" of the bearer, if the bearer’s unit is not within Engagement Range of one or more enemy units, it can make a Normal move of up to 6\".",
                SourcePage = 13
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.AuricChampions,
                Name = "VEILED BLADE",
                Points = 25,
                Rule = "Said to have been fashioned for dark deeds during the Horus Heresy, this blade is an icon of vengeance. ADEPTUS CUSTODES model only. Add 2 to the Attacks characteristic of the bearers melee weapons. Once per battle, at the start of any Command phase, triple the bearer’s Objective Control characteristic until the end of the turn.",
                SourcePage = 13
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "ADAMANTINE TALISMAN",
                Points = 25,
                Rule = "This amulet contains a reservoir of promethium harvested from the wreckage of one of the Adeptus Custodes’ most ancient Land Raiders. The fluid is said to possess the last lingering traces of that ancient vehicle’s bellicose machine spirit, inspiring greater might and ferocity within its bearer. ADEPTUS CUSTODES model only. Improve the Attacks, Strength and Damage characteristics of melee weapons equipped by the bearer by 1.",
                SourcePage = 17
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "AUGURY UPLINK",
                Points = 35,
                Rule = "The war engines of the Adeptus Custodes possess an array of augury equipment that combines to build an intricate picture of the unfolding conflict, enabling the bearer to tap into this accumulated data stream, detect incoming threats and angle their armour to repel the worst of enemy attacks. ADEPTUS CUSTODES model only. The bearer has the Feel No Pain 5+ ability.",
                SourcePage = 17
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "HONOURED FALLEN (AURA)",
                Points = 15,
                Rule = "The eldest warriors of the Moritoi are revered champions with centuries of battle experience, and their presence on the battlefield is inspirational. ADEPTUS CUSTODES VEHICLE model only. While a friendly ADEPTUS CUSTODES INFANTRY or ADEPTUS CUSTODES MOUNTED unit is within 6\" of the bearer, each time a model in that unit makes an attack, re-roll a Hit roll of 1.",
                SourcePage = 17
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.SolarSpearhead,
                Name = "VETERAN OF THE KATAPHRAKTOI",
                Points = 10,
                Rule = "This champion has served amongst the Kataphraktoi and is a master at coordinating swi armoured assaults. ADEPTUS CUSTODES INFANTRY or ADEPTUS CUSTODES MOUNTED model only. In your Command phase, select one ADEPTUS CUSTODES VEHICLE or ADEPTUS CUSTODES MOUNTED unit within 6\" of the bearer. Until the start of your next Command phase, that unit is eligible to shoot in a turn in which it Fell Back.",
                SourcePage = 17
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "SUPERIOR CREATION",
                Points = 25,
                Rule = "The cellular alchemy by which this heroic warrior was forged has rendered them breathtakingly resilient. ADEPTUS CUSTODES INFANTRY model only. The first time the bearer is destroyed, roll one D6 at the end of the phase. On a 2+, set the bearer back up on the battlefield, as close as possible to where it was destroyed and not within Engagement Range of one or more enemy units, with its full wounds remaining.",
                SourcePage = 20
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "PRAESIDIUS",
                Points = 25,
                Rule = "Fashioned by the Terran armourer Annah Tsvochakin in the later years of the 32nd millennium, the stunningly worked Praesidius is a singular artefact. Nestled within its golden form are a series of microshield generators and stealth emiters. Employing a modification of displacer technology, the shield generates small localised displacement bubbles at the point of impact, literally beaming bolts, bullets and the tips of blades harmlessly away from its bearer. ADEPTUS CUSTODES model only. The bearer has the Lone Operative and Stealth abilities.",
                SourcePage = 20
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "FIERCE CONQUEROR",
                Points = 15,
                Rule = "This Captain-Commander has trained extensively to face multiple foes at once, knowing that the Custodes will always be outnumbered. SHIELD-CAPTAIN model only. At the start of the Fight phase, until the end of the phase, add 2 to the Attacks characteristic of melee weapons equipped by the bearer for every 5 enemy models within 6\" of the bearer (rounding down).",
                SourcePage = 20
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.LionsOfTheEmperor,
                Name = "ADMONIMORTIS",
                Points = 30,
                Rule = "A relic of the Dread Host, this towering blade was wrought to make a bloody example of those who dare to set themselves against the might of Terra. SHIELD-CAPTAIN model only. Improve the Strength characteristic of melee weapons equipped by the bearer by 3, and improve the Armour Penetration and Damage characteristics of those weapons by 1.",
                SourcePage = 20
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.MightOfTheMoritoi,
                Name = "INTERRED EXPERTISE UPGRADE",
                Points = 25,
                Rule = "The eldest warriors of the Moritoi are revered champions with centuries of battle experience, whose strikes eﬀiciently exploit their foes’ every weakness. ADEPTUS CUSTODES WALKER unit only. This unit’s attacks can: ▪ Re-roll hit rolls of 1. ▪ Re-roll wound rolls of 1.",
                SourcePage = 23
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.MightOfTheMoritoi,
                Name = "AURAMITE SARCOPHAGUS UPGRADE",
                Points = 15,
                Rule = "These ancient sarcophagi are hardened with age and threaded with Dark Age mechanisms. When the warrior within slams their metallic form into the foe, they can crack armour, pulverise bone and wreck enemy war machines. ADEPTUS CUSTODES WALKER unit only. When you target this unit with the Crushing Impact stratagem, that use is -1 CP.",
                SourcePage = 23
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.SilentHunters,
                Name = "ENCIRCLING HUNTER",
                Points = 15,
                Rule = "Possessed of years’ experience hunting duplicitous and evasive witches, this Knight-Centura is skilled in ensuring every escape route is covered and their unknowing target reeling in horror and confusion. ANATHEMA PSYKANA model only. When both players have deployed their armies, you can redeploy up to three friendly ANATHEMA PSYKANA INFANTRY units. When doing so, you can set those units up in strategic reserves, regardless of how many units are already in strategic reserves.",
                SourcePage = 25
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.SilentHunters,
                Name = "PSYK-OUT GRENADES UPGRADE",
                Points = 10,
                Rule = "These small, artificer-wrought explosives are deadly enough to lesser foes. Yet the favoured prey of the Sisters of Silence are excruciated or stunned by the grenades’ psi-refractive particles, convulsing in a vortex of despair. ANATHEMA PSYKANA unit only. ▪ This unit has EXPLOSIVES. ▪ When you target this unit with the Explosives stratagem, if you select an enemy PSYKER unit, you can re-roll rolls to determine whether that enemy unit suffers a mortal wound.",
                SourcePage = 25
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.TharanatoiHammerblow,
                Name = "MNEMO-LOCKED SHRINE CIPHER",
                Points = 25,
                Rule = "This encrypted activation code dates back to before the Emperor’s compact with Mars. Commited to the enhanced memory of the bearer, it can be utered to cut through a teleportarium shrine’s layers of ageing protocols to deliver armoured death in the blink of an eye. ADEPTUS CUSTODES TERMINATOR model only. In your first Movement phase, this unit can make an ingress move.",
                SourcePage = 27
            });
        enhancements.Add(
            new CustodesEnhancement11
            {
                Detachment = CustodesDetachment.TharanatoiHammerblow,
                Name = "EFFICIENT AGGRESSION",
                Points = 25,
                Rule = "This heavily armoured commander constantly seeks opportunities to exploit the foe’s show of force, leading advances into the teeth of the enemy where lesser warriors would quail. ADEPTUS CUSTODES TERMINATOR model only. (Once per turn, per army) In your opponent’s Shooting phase, when an enemy unit has shot, if this unit lost a wound as a result of those attacks, this unit can make a surge move of up to D6+1\".",
                SourcePage = 27
            });
    }

    public static IReadOnlyList<CustodesStratagem11>
        StratagemsFor(string faction)
    {
        HashSet<CustodesDetachment> selected =
            new HashSet<CustodesDetachment>(
                CustodesDetachmentRuntime
                    .GetSelected(faction));

        return stratagems
            .Where(rule =>
                selected.Contains(
                    rule.Detachment))
            .ToArray();
    }

    public static IReadOnlyList<CustodesEnhancement11>
        EnhancementsFor(string faction)
    {
        HashSet<CustodesDetachment> selected =
            new HashSet<CustodesDetachment>(
                CustodesDetachmentRuntime
                    .GetSelected(faction));

        return enhancements
            .Where(rule =>
                selected.Contains(
                    rule.Detachment))
            .ToArray();
    }

    public static CustodesDetachmentRule11
        DetachmentRule(
            CustodesDetachment detachment)
    {
        CustodesDetachmentRule11 result;

        return rules.TryGetValue(
            detachment,
            out result)
            ? result
            : null;
    }

    public static bool Has(
        string faction,
        CustodesDetachment detachment)
    {
        return CustodesDetachmentRuntime.Has(
            faction,
            detachment);
    }

    public static bool IsCustodes(
        SquadController unit)
    {
        return
            unit != null &&
            unit.HasIntrinsicKeyword(
                "adeptus custodes");
    }

    public static bool NameOrKeyword(
        SquadController unit,
        string value)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (unit.HasKeyword(value))
            return true;

        return
            !string.IsNullOrWhiteSpace(
                unit.DisplayName) &&
            unit.DisplayName.IndexOf(
                value,
                StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string normalized =
            WeaponRuleParser.NormalizeRuleName(
                value);

        return normalized == null
            ? ""
            : normalized
                .Replace("_", " ")
                .ToLowerInvariant();
    }

    public static bool UnitHasEnhancement(
        SquadController unit,
        string name)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (FactionRuleSystem
            .UnitOrLeaderHasRule(
                unit,
                name))
        {
            return true;
        }

        WarboardRosterManifest manifest =
            RosterTextManifestStore.Get(
                unit.FactionId);

        if (manifest == null ||
            string.IsNullOrWhiteSpace(
                manifest.RawText))
        {
            return false;
        }

        string raw =
            Normalize(
                manifest.RawText);

        string wanted =
            Normalize(name);

        if (raw.IndexOf(
                wanted,
                StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        string unitName =
            Normalize(
                unit.DisplayName ?? "");

        if (string.IsNullOrWhiteSpace(
                unitName))
        {
            return false;
        }

        int unitIndex =
            raw.IndexOf(
                unitName,
                StringComparison.OrdinalIgnoreCase);

        if (unitIndex < 0)
            return false;

        int start =
            Mathf.Max(
                0,
                unitIndex - 140);

        int length =
            Mathf.Min(
                raw.Length - start,
                520);

        string window =
            raw.Substring(
                start,
                length);

        return
            window.IndexOf(
                wanted,
                StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool UnitHasEnhancementDirect(
        SquadController unit,
        string name)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (UniversalRuleRegistry.UnitHasRule(
                unit,
                name))
        {
            return true;
        }

        // Roster-text fallback still checks the named unit itself, but does
        // not inherit the enhancement from an attached Leader.
        WarboardRosterManifest manifest =
            RosterTextManifestStore.Get(
                unit.FactionId);

        if (manifest == null)
            return false;

        string raw =
            Normalize(
                manifest.RawText ?? "");

        string wanted =
            Normalize(name);

        string unitName =
            Normalize(
                unit.DisplayName ?? "");

        int unitIndex =
            raw.IndexOf(
                unitName,
                StringComparison.OrdinalIgnoreCase);

        if (unitIndex < 0)
            return false;

        int start =
            Mathf.Max(
                0,
                unitIndex - 140);

        int length =
            Mathf.Min(
                raw.Length - start,
                520);

        string window =
            raw.Substring(
                start,
                length);

        return
            window.IndexOf(
                wanted,
                StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool HasMartialKatah(
        SquadController unit)
    {
        return
            unit != null &&
            (UniversalRuleRegistry.UnitHasRule(
                 unit,
                 "Martial Ka'tah") ||
             UniversalRuleRegistry.UnitHasRule(
                 unit,
                 "Martial Ka’tah"));
    }

    public static bool HasOtherFriendlyWithin(
        GameController game,
        SquadController unit,
        float distance)
    {
        if (game == null ||
            unit == null)
        {
            return false;
        }

        SquadController action =
            unit.JoinedActionController();

        return game.AllSquads.Any(
            other =>
                other != null &&
                !other.IsAttachedLeader &&
                other.IsAlive &&
                other.IsOnBattlefield &&
                other.JoinedActionController() !=
                    action &&
                string.Equals(
                    other.FactionId,
                    unit.FactionId,
                    StringComparison.OrdinalIgnoreCase) &&
                game.JoinedDistancePublic(
                    action,
                    other.JoinedActionController()) <=
                    distance + 0.001f);
    }

    public static float AuraRange(
        SquadController source)
    {
        return
            UnitHasEnhancement(
                source,
                "CHAMPION OF THE IMPERIUM")
            ? 9f
            : 6f;
    }

    public static bool FriendlyAnathemaAuraNear(
        GameController game,
        SquadController unit)
    {
        if (game == null ||
            unit == null)
        {
            return false;
        }

        return game.AllSquads.Any(
            source =>
                source != null &&
                source.IsAlive &&
                source.IsOnBattlefield &&
                string.Equals(
                    source.FactionId,
                    unit.FactionId,
                    StringComparison.OrdinalIgnoreCase) &&
                source.HasKeyword(
                    "anathema psykana") &&
                game.JoinedDistancePublic(
                    source,
                    unit) <=
                    AuraRange(source) + 0.001f);
    }

    public static bool FriendlyCustodianAuraNear(
        GameController game,
        SquadController anathema)
    {
        if (game == null ||
            anathema == null)
        {
            return false;
        }

        return game.AllSquads.Any(
            source =>
                source != null &&
                source.IsAlive &&
                source.IsOnBattlefield &&
                string.Equals(
                    source.FactionId,
                    anathema.FactionId,
                    StringComparison.OrdinalIgnoreCase) &&
                source.HasKeyword(
                    "adeptus custodes") &&
                !source.HasKeyword(
                    "anathema psykana") &&
                game.JoinedDistancePublic(
                    source,
                    anathema) <=
                    AuraRange(source) + 0.001f);
    }

    public static void ApplyAttackModifiers(
        GameController game,
        SquadController attacker,
        SquadController target,
        ModelToken attackingModel,
        WeaponData weapon,
        AttackMode mode,
        UniversalAttackRuleState state)
    {
        if (state == null)
            return;

        if (attacker != null &&
            IsCustodes(attacker))
        {
            string faction =
                attacker.FactionId;

            if (GrantsIgnoresCover(attacker, mode))
                state.ignoresCover = true;

            if (Has(
                    faction,
                    CustodesDetachment
                        .TalonsOfTheEmperor) &&
                attacker.HasKeyword(
                    "anathema psykana") &&
                FriendlyCustodianAuraNear(
                    game,
                    attacker))
            {
                state.hitRollModifier += 1;
            }

            if (Has(
                    faction,
                    CustodesDetachment
                        .LionsOfTheEmperor) &&
                !attacker.HasKeyword(
                    "vehicle") &&
                !HasOtherFriendlyWithin(
                    game,
                    attacker,
                    6f))
            {
                state.hitRollModifier += 1;
                state.woundRollModifier += 1;
            }

            if (Has(
                    faction,
                    CustodesDetachment
                        .AuricChampions))
            {
                SquadController marked =
                    CustodesFactionPack11Runtime
                        .AssemblageTarget(
                            faction);

                bool characterModel =
                    attacker.HasKeyword(
                        "character");

                if (marked != null &&
                    target != null &&
                    target.JoinedActionController() ==
                        marked.JoinedActionController() &&
                    characterModel)
                {
                    state.woundRollModifier += 1;
                }
            }

            if (UnitHasEnhancement(
                    attacker,
                    "OBLIVION KNIGHT"))
            {
                state.hitRollModifier += 1;

                if (target != null &&
                    target.HasKeyword(
                        "psyker"))
                {
                    state.woundRollModifier += 1;
                }
            }

            if (mode == AttackMode.Melee &&
                attackingModel != null &&
                attackingModel.Squad != null &&
                UnitHasEnhancementDirect(
                    attackingModel.Squad,
                    "GIFT OF TERRAN ARTIFICE"))
            {
                state.woundRollModifier += 1;
            }

            if (CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "emperor_executioners") &&
                target != null &&
                IsBelowStartingStrength(
                    target))
            {
                state.woundRollModifier += 1;
            }

            if (CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "flawless_construction") &&
                target == attacker)
            {
                // Defensive effect is applied below when target is checked.
            }

            if (CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "punishment_inescapable"))
            {
                state.hitRollModifier = 0;
                state.skillModifier = 0;
            }

            if (GrantsLance(
                    attacker,
                    weapon,
                    mode) &&
                attacker.JoinedActionController()
                    .MadeChargeMove)
            {
                state.woundRollModifier += 1;
            }

            if (CustodesFactionPack11Runtime
                .IsProsecutedBy(
                    target,
                    faction))
            {
                // Psy-Chaff Volley: ANATHEMA attacks into a prosecuted unit
                // improve AP by 1; handled by ApModifier.
            }
        }

        if (target != null &&
            IsCustodes(target))
        {
            if (mode == AttackMode.Melee &&
                CustodesFactionPack11Runtime
                    .HasFlag(
                        target,
                        "unwavering_sentinels"))
            {
                state.hitRollModifier -= 1;
            }

            if (CustodesFactionPack11Runtime
                .HasFlag(
                    target,
                    "flawless_construction") &&
                weapon != null &&
                weapon.strength >
                    target.Toughness)
            {
                state.woundRollModifier -= 1;
            }

            if (CustodesFactionPack11Runtime
                .HasFlag(
                    target,
                    "hardened_resolve"))
            {
                // Toughness is handled by ToughnessModifier.
            }

            if (CustodesFactionPack11Runtime
                .HasFlag(
                    target,
                    "psychic_abominations"))
            {
                state.hitRollModifier -= 1;
            }

            if (attackingModel != null &&
                UnitHasEnhancement(
                    target,
                    "RADIANT MANTLE") &&
                ModelDistanceToUnit(
                    attackingModel,
                    target) <=
                    12.001f)
            {
                state.hitRollModifier -= 1;
            }

            if (CustodesFactionPack11Runtime
                .IsProsecutedBy(
                    target,
                    target.FactionId))
            {
                // Same-faction case never occurs; kept intentionally empty.
            }
        }

        if (attacker != null &&
            CustodesFactionPack11Runtime
                .IsProsecuted(attacker) &&
            (attacker.HasKeyword("psyker") ||
             attacker.IsBattleShocked))
        {
            state.hitRollModifier -= 1;
        }
    }

    private static float ModelDistanceToUnit(
        ModelToken model,
        SquadController unit)
    {
        if (model == null ||
            unit == null)
        {
            return 999f;
        }

        float best = 999f;

        foreach (ModelToken target
            in unit.JoinedLivingModelTokens())
        {
            if (target == null)
                continue;

            float centre =
                Vector2.Distance(
                    new Vector2(
                        model.transform.position.x,
                        model.transform.position.z),
                    new Vector2(
                        target.transform.position.x,
                        target.transform.position.z));

            float edge =
                Mathf.Max(
                    0f,
                    centre -
                    model.BaseRadiusInches -
                    target.BaseRadiusInches);

            best =
                Mathf.Min(
                    best,
                    edge);
        }

        return best;
    }

    public static bool IsBelowStartingStrength(
        SquadController unit)
    {
        if (unit == null)
            return false;

        unit = unit.JoinedActionController();

        int starting =
            unit.JoinedStartingStrength();

        List<ModelToken> living =
            unit.JoinedLivingModelTokens();

        if (starting == 1)
        {
            ModelToken only =
                living.FirstOrDefault();

            return
                only == null ||
                only.CurrentWounds <
                    only.MaxWounds;
        }

        return living.Count < starting;
    }

    public static int MinimumSustainedHits(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        if (attacker == null)
            return 0;

        int value = 0;

        if (mode == AttackMode.Melee &&
            CustodesFactionPack11Runtime
                .Katah(attacker) ==
                "dacatarai")
        {
            value = 1;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "archeotech_sustained") ||
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "witch_hunters_sustained"))
        {
            value =
                Mathf.Max(
                    value,
                    1);
        }

        return value;
    }

    public static bool GrantsLethalHits(
        SquadController attacker,
        AttackMode mode)
    {
        if (attacker == null)
            return false;

        if (mode == AttackMode.Melee &&
            CustodesFactionPack11Runtime
                .Katah(attacker) ==
                "rendax")
        {
            return true;
        }

        return
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "archeotech_lethal") ||
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "witch_hunters_lethal");
    }

    public static bool GrantsPrecision(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return
            attacker != null &&
            mode == AttackMode.Melee &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "peerless_warrior");
    }

    public static bool GrantsLance(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return
            attacker != null &&
            mode == AttackMode.Melee &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "deathsong_scythes");
    }

    public static bool GrantsIgnoresCover(
        SquadController attacker,
        AttackMode mode)
    {
        if (attacker == null ||
            mode != AttackMode.Ranged)
        {
            return false;
        }

        return
            UnitHasEnhancement(
                attacker,
                "PANOPTISPEX") ||
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "punishment_inescapable");
    }

    public static int StrengthModifier(
        SquadController attacker,
        ModelToken model,
        WeaponData weapon,
        AttackMode mode)
    {
        if (attacker == null ||
            weapon == null)
        {
            return 0;
        }

        int result = 0;

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "talons_interlocked"))
        {
            result += 1;
        }

        if (mode == AttackMode.Melee &&
            model != null &&
            model.Squad != null)
        {
            if (UnitHasEnhancementDirect(
                    model.Squad,
                    "FROM THE HALL OF ARMOURIES"))
            {
                result += 1;
            }

            if (UnitHasEnhancementDirect(
                    model.Squad,
                    "ADAMANTINE TALISMAN"))
            {
                result += 1;
            }

            if (UnitHasEnhancementDirect(
                    model.Squad,
                    "ADMONIMORTIS"))
            {
                result += 3;
            }

            if (UnitHasEnhancementDirect(
                    model.Squad,
                    "RAPTOR BLADE"))
            {
                result +=
                    RaptorBladeEmpowered(
                        model.Squad)
                    ? 2
                    : 1;
            }
        }

        return result;
    }

    public static int DamageModifier(
        SquadController attacker,
        ModelToken model,
        WeaponData weapon,
        AttackMode mode)
    {
        if (attacker == null ||
            weapon == null ||
            mode != AttackMode.Melee ||
            model == null ||
            model.Squad == null)
        {
            return 0;
        }

        int result = 0;

        if (UnitHasEnhancementDirect(
                model.Squad,
                "FROM THE HALL OF ARMOURIES"))
        {
            result += 1;
        }

        if (UnitHasEnhancementDirect(
                model.Squad,
                "ADAMANTINE TALISMAN"))
        {
            result += 1;
        }

        if (UnitHasEnhancementDirect(
                model.Squad,
                "ADMONIMORTIS"))
        {
            result += 1;
        }

        if (UnitHasEnhancementDirect(
                model.Squad,
                "RAPTOR BLADE"))
        {
            result +=
                RaptorBladeEmpowered(
                    model.Squad)
                ? 2
                : 1;
        }

        return result;
    }

    public static int AdditionalAttacks(
        GameController game,
        SquadController attacker,
        ModelToken model,
        WeaponData weapon,
        AttackMode mode,
        SquadController target)
    {
        if (attacker == null ||
            weapon == null)
        {
            return 0;
        }

        int result = 0;

        if (mode == AttackMode.Melee)
        {
            if (CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "avenge_fallen"))
            {
                result +=
                    attacker.IsAtOrBelowHalfStrength()
                    ? 2
                    : 1;
            }

            if (CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "deathsong_scythes") &&
                target != null &&
                target.HasKeyword(
                    "psyker"))
            {
                result += 1;
            }

            if (model != null &&
                model.Squad != null)
            {
                SquadController modelUnit =
                    model.Squad;

                if (UnitHasEnhancementDirect(
                        modelUnit,
                        "ADAMANTINE TALISMAN"))
                {
                    result += 1;
                }

                if (UnitHasEnhancementDirect(
                        modelUnit,
                        "VEILED BLADE"))
                {
                    result += 2;
                }

                if (UnitHasEnhancementDirect(
                        modelUnit,
                        "RAPTOR BLADE"))
                {
                    result +=
                        RaptorBladeEmpowered(
                            modelUnit)
                        ? 2
                        : 1;
                }

                if (UnitHasEnhancementDirect(
                        modelUnit,
                        "FIERCE CONQUEROR") &&
                    game != null)
                {
                    int enemies =
                        game.AllSquads
                            .Where(enemy =>
                                enemy != null &&
                                enemy.IsAlive &&
                                enemy.IsOnBattlefield &&
                                enemy.FactionId !=
                                    attacker.FactionId &&
                                game.JoinedDistancePublic(
                                    modelUnit,
                                    enemy) <= 6.001f)
                            .Sum(enemy =>
                                enemy.JoinedLivingModels);

                    result +=
                        (enemies / 5) *
                        2;
                }
            }
        }

        if (mode == AttackMode.Ranged &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "purgation_sweep") &&
            WeaponRuleParser.Has(
                weapon,
                "torrent"))
        {
            result +=
                target != null &&
                (target.HasKeyword("psyker") ||
                 target.IsBattleShocked)
                ? 2
                : 1;
        }

        if (mode == AttackMode.Ranged &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "electroexorcist_saturation") &&
            weapon.displayName != null &&
            weapon.displayName.IndexOf(
                "Ballistus Grenade Launcher",
                StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // The standard Ballistus profile is D3 A; D3+3 therefore adds 3.
            result += 3;
        }

        return result;
    }

    public static int AdditionalRapidFire(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        if (attacker == null ||
            weapon == null ||
            mode != AttackMode.Ranged)
        {
            return 0;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "umbral_prosecution") &&
            weapon.displayName != null &&
            weapon.displayName.IndexOf(
                "Boltgun",
                StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return 2;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "prioritised_eradication") &&
            weapon.displayName != null)
        {
            if (weapon.displayName.IndexOf(
                    "Arachnus Storm Cannon",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 6;
            }

            if (weapon.displayName.IndexOf(
                    "Iliastus Accelerator Culverin",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 2;
            }
        }

        return 0;
    }

    public static int AdditionalBlast(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return
            attacker != null &&
            weapon != null &&
            mode == AttackMode.Ranged &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "synchronised_inferno") &&
            WeaponRuleParser.Has(
                weapon,
                "torrent")
            ? 1
            : 0;
    }

    public static bool GrantsBlast(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return AdditionalBlast(
            attacker,
            weapon,
            mode) > 0;
    }

    public static int ApModifier(
        SquadController attacker,
        SquadController target,
        ModelToken model,
        WeaponData weapon,
        AttackMode mode)
    {
        if (attacker == null)
            return 0;

        int result = 0;

        if (mode == AttackMode.Melee &&
            HasMartialKatah(attacker) &&
            CustodesFactionPack11Runtime
                .MartialMastery(
                    attacker.FactionId) ==
                "ap")
        {
            result -= 1;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "talons_interlocked"))
        {
            result -= 1;
        }

        if (mode == AttackMode.Ranged &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "focused_fire"))
        {
            result -= 1;
        }

        if (mode == AttackMode.Ranged &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "umbral_prosecution") &&
            weapon != null &&
            weapon.displayName != null &&
            weapon.displayName.IndexOf(
                "Boltgun",
                StringComparison.OrdinalIgnoreCase) >= 0)
        {
            result -= 1;
        }

        if (mode == AttackMode.Ranged &&
            CustodesFactionPack11Runtime
                .IsProsecutedBy(
                    target,
                    attacker.FactionId) &&
            attacker.HasKeyword(
                "anathema psykana"))
        {
            result -= 1;
        }

        if (mode == AttackMode.Melee &&
            model != null &&
            model.Squad != null &&
            UnitHasEnhancementDirect(
                model.Squad,
                "ADMONIMORTIS"))
        {
            result -= 1;
        }

        return result;
    }

    public static int ToughnessModifier(
        SquadController target)
    {
        return
            target != null &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    target,
                    "hardened_resolve")
            ? 1
            : 0;
    }

    public static float MoveModifier(
        SquadController unit)
    {
        if (unit == null)
            return 0f;

        bool walker =
            unit.HasKeyword(
                "walker");

        if (!walker)
            return 0f;

        return
            Has(
                unit.FactionId,
                CustodesDetachment
                    .SolarSpearhead) ||
            Has(
                unit.FactionId,
                CustodesDetachment
                    .MightOfTheMoritoi)
            ? 2f
            : 0f;
    }

    public static int AdvanceRollModifier(
        SquadController unit)
    {
        if (unit == null ||
            !unit.HasKeyword(
                "walker"))
        {
            return 0;
        }

        return
            Has(
                unit.FactionId,
                CustodesDetachment
                    .SolarSpearhead) ||
            Has(
                unit.FactionId,
                CustodesDetachment
                    .MightOfTheMoritoi)
            ? 1
            : 0;
    }

    public static int ChargeRollModifier(
        SquadController unit)
    {
        return AdvanceRollModifier(unit);
    }

    public static bool CanRerollCharge(
        SquadController unit)
    {
        return
            unit != null &&
            Has(
                unit.FactionId,
                CustodesDetachment
                    .TharanatoiHammerblow) &&
            unit.HasKeyword(
                "terminator") &&
            unit.WasSetUpThisTurn;
    }

    public static bool CanIngressFirstMovement(
        SquadController unit)
    {
        return
            unit != null &&
            Has(
                unit.FactionId,
                CustodesDetachment
                    .TharanatoiHammerblow) &&
            unit.HasKeyword("terminator") &&
            UnitHasEnhancement(
                unit,
                "MNEMO-LOCKED SHRINE CIPHER");
    }

    public static bool CanShootAfterFallBack(
        SquadController unit)
    {
        if (unit == null)
            return false;

        return
            CustodesFactionPack11Runtime
                .HasFlag(
                    unit,
                    "shoot_after_fallback") ||
            UnitHasEnhancement(
                unit,
                "MARTIAL PHILOSOPHER") ||
            CustodesFactionPack11Runtime
                .HasFlag(
                    unit,
                    "veteran_kataphraktoi");
    }

    public static bool CanChargeAfterFallBack(
        SquadController unit)
    {
        if (unit == null)
            return false;

        return
            CustodesFactionPack11Runtime
                .HasFlag(
                    unit,
                    "charge_after_fallback") ||
            UnitHasEnhancement(
                unit,
                "MARTIAL PHILOSOPHER");
    }

    public static bool CanShootAfterAdvance(
        SquadController unit)
    {
        return
            unit != null &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    unit,
                    "shoot_after_advance");
    }

    public static bool CanChargeAfterAdvance(
        SquadController unit)
    {
        return
            unit != null &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    unit,
                    "charge_after_advance");
    }

    public static bool CanStartActionAfterAdvance(
        SquadController unit)
    {
        return
            unit != null &&
            Has(
                unit.FactionId,
                CustodesDetachment
                    .SilentHunters) &&
            unit.HasKeyword(
                "anathema psykana");
    }

    public static int ModifyObjectiveControl(
        SquadController unit,
        ModelToken model,
        int current)
    {
        if (unit == null ||
            model == null)
        {
            return current;
        }

        int value =
            current;

        if (Has(
                unit.FactionId,
                CustodesDetachment
                    .SolarSpearhead) &&
            unit.HasKeyword(
                "vehicle") &&
            !unit.HasKeyword(
                "aircraft") &&
            !unit.IsBattleShocked &&
            !IsBelowStartingStrength(
                unit))
        {
            value += 2;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                unit,
                "veiled_blade_triple_oc") &&
            model.Squad != null &&
            UnitHasEnhancementDirect(
                model.Squad,
                "VEILED BLADE"))
        {
            value *= 3;
        }

        return value;
    }

    public static bool GrantsCoreAbility(
        SquadController unit,
        string ruleName)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(
                ruleName))
        {
            return false;
        }

        string wanted =
            Normalize(ruleName);

        if (wanted.Contains(
                "lone operative"))
        {
            return UnitHasEnhancement(
                unit,
                "PRAESIDIUS");
        }

        if (wanted.Contains("stealth"))
        {
            return
                UnitHasEnhancement(
                    unit,
                    "PRAESIDIUS") ||
                CustodesFactionPack11Runtime
                    .HasFlag(
                        unit,
                        "psychic_abominations");
        }

        if (wanted.Contains("ignores cover"))
        {
            return GrantsIgnoresCover(
                unit,
                AttackMode.Ranged);
        }

        if (wanted.Contains("explosives"))
        {
            return
                Has(
                    unit.FactionId,
                    CustodesDetachment
                        .SilentHunters) &&
                UnitHasEnhancement(
                    unit,
                    "PSYK-OUT GRENADES UPGRADE");
        }

        if (wanted.Contains("mobile"))
        {
            return
                CustodesFactionPack11Runtime
                    .HasFlag(
                        unit,
                        "mobile");
        }

        return false;
    }

    public static bool AutomaticRerollHit(
        GameController game,
        SquadController attacker,
        int roll,
        bool success,
        AttackMode mode)
    {
        if (attacker == null)
            return false;

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "reroll_all_hits"))
        {
            return !success;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "reroll_hit_ones"))
        {
            return roll == 1;
        }

        if (Has(
                attacker.FactionId,
                CustodesDetachment
                    .SolarSpearhead) &&
            attacker.HasKeyword(
                "vehicle") &&
            IsBelowStartingStrength(
                attacker))
        {
            return roll == 1;
        }

        if (Has(
                attacker.FactionId,
                CustodesDetachment
                    .MightOfTheMoritoi) &&
            attacker.HasKeyword(
                "walker") &&
            UnitHasEnhancement(
                attacker,
                "INTERRED EXPERTISE UPGRADE"))
        {
            return roll == 1;
        }

        if (game != null &&
            Has(
                attacker.FactionId,
                CustodesDetachment
                    .SolarSpearhead) &&
            (attacker.HasKeyword("infantry") ||
             attacker.HasKeyword("mounted")) &&
            game.AllSquads.Any(
                source =>
                    source != null &&
                    source.IsAlive &&
                    source.IsOnBattlefield &&
                    string.Equals(
                        source.FactionId,
                        attacker.FactionId,
                        StringComparison.OrdinalIgnoreCase) &&
                    source.HasKeyword(
                        "vehicle") &&
                    UnitHasEnhancement(
                        source,
                        "HONOURED FALLEN (AURA)") &&
                    game.JoinedDistancePublic(
                        source,
                        attacker) <= 6.001f))
        {
            return roll == 1;
        }

        return false;
    }

    public static bool AutomaticRerollWound(
        SquadController attacker,
        SquadController target,
        int roll,
        bool success,
        AttackMode mode)
    {
        if (attacker == null)
            return false;

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "reroll_all_wounds"))
        {
            return !success;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "reroll_wound_ones"))
        {
            return roll == 1;
        }

        if (Has(
                attacker.FactionId,
                CustodesDetachment
                    .SolarSpearhead) &&
            attacker.HasKeyword(
                "vehicle") &&
            attacker.IsAtOrBelowHalfStrength())
        {
            return roll == 1;
        }

        if (Has(
                attacker.FactionId,
                CustodesDetachment
                    .MightOfTheMoritoi) &&
            attacker.HasKeyword(
                "walker") &&
            UnitHasEnhancement(
                attacker,
                "INTERRED EXPERTISE UPGRADE"))
        {
            return roll == 1;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                attacker,
                "anathema_blademastery") &&
            target != null &&
            (target.HasKeyword("psyker") ||
             target.IsBattleShocked))
        {
            return !success;
        }

        return false;
    }

    public static bool IsCriticalHit(
        SquadController attacker,
        int roll,
        bool successful)
    {
        if (!successful)
            return false;

        if (roll >= 6)
            return true;

        return
            attacker != null &&
            HasMartialKatah(attacker) &&
            CustodesFactionPack11Runtime
                .MartialMastery(
                    attacker.FactionId) ==
                "crit5" &&
            roll >= 5;
    }

    public static int ConditionalFeelNoPain(
        SquadController unit,
        string label,
        int current)
    {
        if (unit == null ||
            !IsCustodes(unit))
        {
            return current;
        }

        int value =
            current;

        bool mortal =
            !string.IsNullOrWhiteSpace(label) &&
            (label.IndexOf(
                 "Mortal",
                 StringComparison.OrdinalIgnoreCase) >= 0 ||
             label.IndexOf(
                 "Devastating Wounds",
                 StringComparison.OrdinalIgnoreCase) >= 0 ||
             label.IndexOf(
                 "Hazardous",
                 StringComparison.OrdinalIgnoreCase) >= 0);

        bool psychic =
            !string.IsNullOrWhiteSpace(label) &&
            label.IndexOf(
                "Psychic",
                StringComparison.OrdinalIgnoreCase) >= 0;

        if (UnitHasEnhancementDirect(
                unit,
                "AUGURY UPLINK"))
        {
            value =
                BetterFeelNoPain(
                    value,
                    5);
        }

        if (CustodesFactionPack11Runtime
                .HasFlag(
                    unit,
                    "emperors_auspice") &&
            unit.HasIntrinsicKeyword(
                "character"))
        {
            value =
                BetterFeelNoPain(
                    value,
                    4);
        }

        if ((mortal || psychic) &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    unit,
                    "empyric_severance"))
        {
            value =
                BetterFeelNoPain(
                    value,
                    4);
        }

        if (mortal &&
            CustodesFactionPack11Runtime
                .HasFlag(
                    unit,
                    "arcane_genetic_alchemy"))
        {
            value =
                BetterFeelNoPain(
                    value,
                    4);
        }

        if ((mortal || psychic) &&
            Has(
                unit.FactionId,
                CustodesDetachment
                    .TalonsOfTheEmperor))
        {
            CustodesGameController controller =
                CustodesFactionPack11Runtime
                    .Controller(
                        unit.FactionId);

            GameController game =
                controller != null
                ? controller.OwnerGame
                : null;

            if (FriendlyAnathemaAuraNear(
                    game,
                    unit))
            {
                value =
                    BetterFeelNoPain(
                        value,
                        5);
            }
        }

        return value;
    }

    private static int BetterFeelNoPain(
        int current,
        int candidate)
    {
        if (candidate <= 0)
            return current;

        return
            current <= 0
            ? candidate
            : Mathf.Min(
                current,
                candidate);
    }

    public static int ModifyIncomingDamage(
        ModelToken allocated,
        SquadController attacker,
        WeaponData weapon,
        int incoming,
        bool fromFailedSave = true)
    {
        if (allocated == null ||
            allocated.Squad == null ||
            incoming <= 0)
        {
            return incoming;
        }

        SquadController modelUnit =
            allocated.Squad;

        if (fromFailedSave &&
            UnitHasEnhancement(
                modelUnit,
                "AEGIS PROJECTOR") &&
            !CustodesFactionPack11Runtime
                .HasUsedThisTurn(
                    modelUnit.FactionId,
                    "aegis_projector|" +
                    modelUnit.JoinedActionController().GetEntityId()))
        {
            CustodesFactionPack11Runtime
                .MarkOncePerTurn(
                    modelUnit.FactionId,
                    "aegis_projector|" +
                    modelUnit.JoinedActionController().GetEntityId());

            return 0;
        }

        if (UnitHasEnhancementDirect(
                modelUnit,
                "ENHANCED VOIDSHEEN CLOAK"))
        {
            bool psyker =
                attacker != null &&
                attacker.HasKeyword(
                    "psyker");

            bool shocked =
                attacker != null &&
                attacker.IsBattleShocked;

            if (psyker || shocked)
                return 1;

            return
                Mathf.Max(
                    1,
                    incoming - 1);
        }

        return incoming;
    }

    private static bool RaptorBladeEmpowered(
        SquadController bearer)
    {
        if (bearer == null)
            return false;

        CustodesGameController controller =
            CustodesFactionPack11Runtime
                .Controller(
                    bearer.FactionId);

        GameController game =
            controller != null
            ? controller.OwnerGame
            : null;

        if (game == null)
            return false;

        return game.AllSquads.Any(
            enemy =>
                enemy != null &&
                enemy.IsAlive &&
                enemy.IsOnBattlefield &&
                enemy.FactionId !=
                    bearer.FactionId &&
                enemy.HasKeyword(
                    "psyker") &&
                enemy.IsBattleShocked &&
                game.UnitsAreEngaged(
                    bearer,
                    enemy));
    }

    public static int ModifyLeadership(
        SquadController unit,
        int current)
    {
        return
            unit != null &&
            UnitHasEnhancement(
                unit,
                "INSPIRATIONAL EXEMPLAR")
            ? Mathf.Min(
                current,
                5)
            : current;
    }

    public static float DetectionRangeBonus(
        SquadController target)
    {
        return
            CustodesFactionPack11Runtime
                .IsNulled(target)
            ? 3f
            : 0f;
    }

    public static int ModifyStratagemCost(
        SquadController target,
        string label,
        int current)
    {
        if (target == null ||
            string.IsNullOrWhiteSpace(
                label))
        {
            return current;
        }

        if (label.IndexOf(
                "Crushing Impact",
                StringComparison.OrdinalIgnoreCase) >= 0 &&
            UnitHasEnhancement(
                target,
                "AURAMITE SARCOPHAGUS UPGRADE"))
        {
            return
                Mathf.Max(
                    0,
                    current - 1);
        }

        return current;
    }

    public static bool CanBeRangedTarget(
        GameController game,
        SquadController attacker,
        SquadController target,
        out string reason)
    {
        reason = "";

        if (game == null ||
            attacker == null ||
            target == null)
        {
            return true;
        }

        if (CustodesFactionPack11Runtime
            .HasFlag(
                target,
                "psychic_abominations") &&
            (attacker.HasKeyword("psyker") ||
             attacker.IsBattleShocked) &&
            game.JoinedDistancePublic(
                attacker,
                target) >
                12.001f)
        {
            reason =
                "Psychic Abominations: PSYKER and Battle-shocked models can only target this unit with ranged attacks within 12 inches.";

            return false;
        }

        return true;
    }
}
