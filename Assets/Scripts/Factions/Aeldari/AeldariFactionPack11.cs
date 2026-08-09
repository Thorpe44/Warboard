using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class AeldariStratagem11
{
    public AeldariDetachment Detachment;
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
            string text = "WHEN: " + When + "\nTARGET: " + Target + "\nEFFECT: " + Effect;
            if (!string.IsNullOrWhiteSpace(Restrictions)) text += "\nRESTRICTIONS: " + Restrictions;
            return text;
        }
    }
}

public sealed class AeldariEnhancement11
{
    public AeldariDetachment Detachment;
    public string Name = "";
    public int Points;
    public string Rule = "";
    public int SourcePage;
}

public sealed class AeldariDetachmentRule11
{
    public AeldariDetachment Detachment;
    public string Name = "";
    public string Rule = "";
}

public static class AeldariFactionPack11
{
    // WARBOARD_V42_FULL_AELDARI_FACTION_RULES

    public const string Version = "Aeldari Faction Pack 11e v1.1 July 2026";
    private static readonly List<AeldariStratagem11> stratagems = new List<AeldariStratagem11>();
    private static readonly List<AeldariEnhancement11> enhancements = new List<AeldariEnhancement11>();
    private static readonly Dictionary<AeldariDetachment, AeldariDetachmentRule11> rules = new Dictionary<AeldariDetachment, AeldariDetachmentRule11>();

    static AeldariFactionPack11()
    {
        rules[AeldariDetachment.Warhost] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.Warhost, Name = "Martial Grace", Rule = "At the start of the battle round, receive 1 additional Battle Focus token. Swift as the Wind adds an additional 1\" Move. Add 1 to D6 results for Agile Manoeuvres." };
        rules[AeldariDetachment.WindriderHost] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.WindriderHost, Name = "Ride the Wind", Rule = "ASURYANI MOUNTED and VYPER units can use Reserves as Strategic Reserves and count the battle round as one higher for arrival. At the end of the opponent turn, eligible MOUNTED/VYPER units can return to Strategic Reserves. WINDRIDERS gain BATTLELINE." };
        rules[AeldariDetachment.SpiritConclave] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.SpiritConclave, Name = "Shepherds of the Dead", Rule = "Enemy units gain Vengeful Dead tokens when they destroy ASURYANI PSYKER models. WRAITH CONSTRUCT attacks against tokened units gain +1 Hit and +1 Wound. Spirit Guides grant Battle Focus to nearby WRAITHBLADES, WRAITHGUARD and WRAITHLORD. WRAITHBLADES/WRAITHGUARD gain BATTLELINE." };
        rules[AeldariDetachment.GuardianBattlehost] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.GuardianBattlehost, Name = "Defend at All Costs", Rule = "DIRE AVENGER, GUARDIAN, SUPPORT WEAPON and WAR WALKER attacks gain +1 Hit when the attacker or target is within range of an objective marker." };
        rules[AeldariDetachment.GhostsOfTheWebway] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.GhostsOfTheWebway, Name = "Acrobatic Onslaught", Rule = "HARLEQUINS models can move through enemy models while making Charge moves. TROUPE units gain BATTLELINE and TROUPE models have OC 2." };
        rules[AeldariDetachment.DevotedOfYnnead] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.DevotedOfYnnead, Name = "Strength from Death", Rule = "Lethal Intent, Lethal Surge and Lethal Reprisal apply to YNNARI units. ASURYANI non-EPIC HEROES gain YNNARI. Army must include Yvraine and/or the Yncarne, with one as WARLORD." };
        rules[AeldariDetachment.SeerCouncil] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.SeerCouncil, Name = "Strands of Fate", Rule = "At the start of the first battle round generate Fate dice based on battle size. Matching Fate dice can be discarded to reduce the CP cost of the six Seer Council Stratagems by 1CP." };
        rules[AeldariDetachment.AspectHost] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.AspectHost, Name = "Path of the Warrior", Rule = "Each time an ASPECT WARRIORS or AVATAR OF KHAINE unit is selected to shoot or fight, choose re-roll Hit rolls of 1 or re-roll Wound rolls of 1 until end of phase." };
        rules[AeldariDetachment.ArmouredWarhost] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.ArmouredWarhost, Name = "Skilled Crews", Rule = "Friendly AELDARI VEHICLE units’ ranged attacks have ASSAULT." };
        rules[AeldariDetachment.FatefulPerformance] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.FatefulPerformance, Name = "Acrobatic Onslaught", Rule = "Friendly HARLEQUINS units can move through enemy models while making Charge moves. ACROBATIC." };
        rules[AeldariDetachment.PathOfTheOutcast] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.PathOfTheOutcast, Name = "Far-Reaching Doom", Rule = "When a friendly RANGERS/SHROUD RUNNERS unit is selected to shoot, enemy units have +6\" detection range until that unit has shot." };
        rules[AeldariDetachment.TwilightFlickers] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.TwilightFlickers, Name = "Dance of Distortion", Rule = "Friendly HARLEQUINS units have Stealth. ACROBATIC." };
        rules[AeldariDetachment.SerpentsBrood] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.SerpentsBrood, Name = "Boons of the Brood", Rule = "HARLEQUINS MOUNTED and VEHICLE models’ weapons have SUSTAINED HITS 1. HARLEQUINS units gain SUSTAINED HITS 1 until end of turn after disembarking. TROUPE units gain BATTLELINE and OC 2." };
        rules[AeldariDetachment.EldritchRaiders] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.EldritchRaiders, Name = "Yriel’s Own", Rule = "AELDARI units can declare a charge after Advancing. ANHRATHE, RANGERS and SHROUD RUNNERS can re-roll Advance rolls." };
        rules[AeldariDetachment.CorsairCoterie] = new AeldariDetachmentRule11 { Detachment = AeldariDetachment.CorsairCoterie, Name = "Relentless Raiders", Rule = "Controlled objectives punish enemy units ending moves within range on a 2+ for D3 mortal wounds. ANHRATHE Void Thieves secure controlled objectives until opponent Level of Control exceeds yours at end of a phase." };
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.Warhost,
            Name = "Lightning-Fast Reactions",
            Cost = 1,
            Category = "WARHOST – BATTLE TACTIC STRATAGEM",
            When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
            Target = "One ASURYANI unit from your army (excluding WRAITH CONSTRUCT units) that was selected as the target of one or more of the attacking unit’s attacks.",
            Effect = "Until the end of the phase, each time an attack targets your unit, subtract 1 from the Hit roll.",
            Restrictions = "",
            SourcePage = 8
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.Warhost,
            Name = "Skyborne Sanctuary",
            Cost = 1,
            Category = "WARHOST – STRATEGIC PLOY STRATAGEM",
            When = "End of the Fight phase.",
            Target = "One unengaged ASURYANI unit from your army that was eligible to fight this phase and one friendly TRANSPORT it is able to embark within.",
            Effect = "If your ASURYANI unit is wholly within 6” of that TRANSPORT, it can embark within it.",
            Restrictions = "",
            SourcePage = 8
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.Warhost,
            Name = "Feigned Retreat",
            Cost = 1,
            Category = "WARHOST – STRATEGIC PLOY STRATAGEM",
            When = "Your Movement phase, just after an ASURYANI unit from your army Falls Back.",
            Target = "That ASURYANI unit.",
            Effect = "Until the end of the turn, your unit is eligible to shoot and declare a charge in a turn in which it Fell Back.",
            Restrictions = "",
            SourcePage = 8
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.Warhost,
            Name = "Blitzing Firepower",
            Cost = 1,
            Category = "WARHOST – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase.",
            Target = "One ASURYANI unit from your army that has not been selected to shoot this phase.",
            Effect = "Until the end of the phase, ranged weapons equipped by models in your unit have the [SUSTAINED HITS 1] ability while targeting an enemy unit within 12\". If such a weapon already has that ability, until the end of the phase, each time an attack is made with that weapon, an unmodified Hit roll of 5+ scores a Critical Hit.",
            Restrictions = "",
            SourcePage = 8
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.Warhost,
            Name = "Fire And Fade",
            Cost = 1,
            Category = "WARHOST – STRATEGIC PLOY STRATAGEM",
            When = "Your Shooting phase, just after an ASURYANI INFANTRY unit from your army (excluding AIRCRAFT, ASURMEN and WRAITH CONSTRUCT units) has shot.",
            Target = "That ASURYANI unit.",
            Effect = "Your unit can make a Normal move of up to D6+1\".",
            Restrictions = "Until the end of the turn, your unit is not eligible to declare a charge or embark within a TRANSPORT.",
            SourcePage = 9
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.Warhost,
            Name = "Webway Tunnel",
            Cost = 1,
            Category = "WARHOST – STRATEGIC PLOY STRATAGEM",
            When = "End of your opponent’s Fight phase.",
            Target = "One ASURYANI INFANTRY unit from your army that is wholly within 9\" of one or more battlefield edges.",
            Effect = "If your unit is not within Engagement Range of one or more enemy units, remove it from the battlefield and place it into Strategic Reserves.",
            Restrictions = "",
            SourcePage = 9
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.WindriderHost,
            Name = "Death From On High",
            Cost = 1,
            Category = "WINDRIDER HOST – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase or the Fight phase.",
            Target = "One ASURYANI MOUNTED or VYPER unit from your army that was set upon the battlefield from Reserves this turn and has not been selected to shoot or fight this phase.",
            Effect = "Until the end of the phase, each time a model in your unit makes an attack, you can re-roll the Wound roll.",
            Restrictions = "",
            SourcePage = 11
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.WindriderHost,
            Name = "Overflight",
            Cost = 1,
            Category = "WINDRIDER HOST – STRATEGIC PLOY STRATAGEM",
            When = "End of your Shooting phase or the end of the Fight phase.",
            Target = "One ASURYANI MOUNTED unit from your army that destroyed one or more enemy units this phase.",
            Effect = "Your unit can make a Normal move of up to 7\".",
            Restrictions = "",
            SourcePage = 11
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.WindriderHost,
            Name = "Wind Of Blades",
            Cost = 1,
            Category = "WINDRIDER HOST – STRATEGIC PLOY STRATAGEM",
            When = "Your Movement phase.",
            Target = "One ASURYANI MOUNTED or VYPER unit from your army that has not been selected to move this phase.",
            Effect = "Until the end of the turn, your unit is eligible to shoot and declare a charge in a turn in which it Advanced or Fell Back.",
            Restrictions = "",
            SourcePage = 11
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.WindriderHost,
            Name = "Daring Riders",
            Cost = 1,
            Category = "WINDRIDER HOST – STRATEGIC PLOY STRATAGEM",
            When = "The Reinforcements step of your Movement phase.",
            Target = "One ASURYANI MOUNTED or VYPER unit from your army in Reserves.",
            Effect = "Until the end of the phase, when setting up your unit on the battlefield from Reserves, it can be set up anywhere on the battlefield that is more than 6\" horizontally away from all enemy units. When doing so, if your unit is set up within 8\" horizontally of one or more enemy units, until the end of the turn, it is not eligible to declare a charge.",
            Restrictions = "",
            SourcePage = 12
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.WindriderHost,
            Name = "Focused Firepower",
            Cost = 1,
            Category = "WINDRIDER HOST – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase.",
            Target = "One ASURYANI MOUNTED or VYPER unit from your army that has not been selected to shoot this phase.",
            Effect = "Until the end of the phase, each time a model in your unit makes an attack, improve the Armour Penetration characteristic of that attack by 1.",
            Restrictions = "",
            SourcePage = 12
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.WindriderHost,
            Name = "Spiralling Evasion",
            Cost = 1,
            Category = "WINDRIDER HOST – BATTLE TACTIC STRATAGEM",
            When = "Your opponent’s Shooting phase, just after an enemy unit has selected its targets.",
            Target = "One ASURYANI MOUNTED or VYPER unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
            Effect = "Until the end of the phase, models in your unit have a 4+ invulnerable save.",
            Restrictions = "",
            SourcePage = 12
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SpiritConclave,
            Name = "Seer’S Eye",
            Cost = 1,
            Category = "SPIRIT CONCLAVE – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase or the Fight phase.",
            Target = "One AELDARI PSYKER model from your army and one friendly WRAITH CONSTRUCT unit within 12\" of it that has not been selected to shoot or fight this phase.",
            Effect = "Select one enemy unit visible to your PSYKER model. Until the end of the phase, each time a model in your WRAITH CONSTRUCT unit makes an attack that targets that enemy unit, you can ignore any or all modifiers to the Armour Penetration and/or Damage characteristics of that attack.",
            Restrictions = "",
            SourcePage = 14
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SpiritConclave,
            Name = "Wraithbone Armour",
            Cost = 1,
            Category = "SPIRIT CONCLAVE – BATTLE TACTIC STRATAGEM",
            When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
            Target = "One WRAITH CONSTRUCT unit from your army (excluding TITANIC units] that was selected as the target of one or more of the attacking unit’s attacks.",
            Effect = "Until the end of the phase, each time an attack is allocated to a model in your unit, subtract 1 from the Damage characteristic of that attack.",
            Restrictions = "",
            SourcePage = 14
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SpiritConclave,
            Name = "Blades From Beyond",
            Cost = 1,
            Category = "SPIRIT CONCLAVE – BATTLE TACTIC STRATAGEM",
            When = "Fight phase.",
            Target = "One WRAITHBLADES, WRAITHLORD or WRAITHKNIGHT unit from your army that has not been selected to fight this phase.",
            Effect = "Until the end of the phase, melee weapons equipped by models in your unit have the [DEVASTATING WOUNDS] ability.",
            Restrictions = "",
            SourcePage = 14
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SpiritConclave,
            Name = "Soul Bridge",
            Cost = 1,
            Category = "SPIRIT CONCLAVE – STRATEGIC PLOY STRATAGEM",
            When = "Your Command phase.",
            Target = "One WRAITHBLADES, WRAITHGUARD or WRAITHLORD unit from your army and one ASURYANI PSYKER model from your army.",
            Effect = "Until the start of your next Command phase, your WRAITHBLADES, WRAITHGUARD or WRAITHLORD unit is considered to be within 12\" of your PSYKER model for the purposes of the Psychic Guidance and Spirit Guides abilities.",
            Restrictions = "",
            SourcePage = 15
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SpiritConclave,
            Name = "Spirit Token",
            Cost = 1,
            Category = "SPIRIT CONCLAVE – STRATEGIC PLOY STRATAGEM",
            When = "Start of your Movement phase.",
            Target = "One WRAITHBLADES or WRAITHGUARD unit from your army.",
            Effect = "Select one objective marker you control that your unit is within range of. That objective marker remains under your control until your opponent’s Level of Control over that objective marker is greater than yours at the end of a phase.",
            Restrictions = "",
            SourcePage = 15
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SpiritConclave,
            Name = "Crushing Strides",
            Cost = 1,
            Category = "SPIRIT CONCLAVE – BATTLE TACTIC STRATAGEM",
            When = "Your Charge phase, just after a WRAITHBLADES, WRAITHLORD or WRAITHKNIGHT unit from your army ends a Charge move.",
            Target = "That WRAITHBLADES, WRAITHLORD or WRAITHKNIGHT unit.",
            Effect = "Select one enemy unit within Engagement Range of your unit and roll one D6 for each WRAITHBLADES model in your unit, or roll four D6 if your unit has the WRAITHLORD keyword, or roll six D6 if your unit has the WRAITHKNIGHT keyword: for each 3+, that enemy unit suffers 1 mortal wound.",
            Restrictions = "",
            SourcePage = 15
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GuardianBattlehost,
            Name = "Warding Salvoes",
            Cost = 1,
            Category = "GUARDIAN BATTLEHOST – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase or the Fight phase.",
            Target = "One DIRE AVENGERS or GUARDIANS unit from your army that has not been selected to shoot or fight this phase.",
            Effect = "Until the end of the phase, each time a model in your unit makes an attack that targets an enemy unit within range of one or more objective markers, you can re-roll the Wound roll.",
            Restrictions = "",
            SourcePage = 17
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GuardianBattlehost,
            Name = "Shield Nodes",
            Cost = 1,
            Category = "GUARDIAN BATTLEHOST – BATTLE TACTIC STRATAGEM",
            When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
            Target = "One DIRE AVENGERS or GUARDIANS unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
            Effect = "If your unit is within range of one or more objective markers, until the end of the phase, each time an attack targets your unit, subtract 1 from the Wound roll.",
            Restrictions = "",
            SourcePage = 17
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GuardianBattlehost,
            Name = "Vaul’S Vengeance",
            Cost = 1,
            Category = "GUARDIAN BATTLEHOST – BATTLE TACTIC STRATAGEM",
            When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit destroys a DIRE AVENGERS or GUARDIANS unit from your army.",
            Target = "One WAR WALKERS unit from your army.",
            Effect = "A�er that enemy unit has finished making its attacks, your unit can shoot as if it were your Shooting phase, but when resolving those attacks, it can only target that enemy unit (and only if it is an eligible target).",
            Restrictions = "You can only use this Stratagem once per battle round.",
            SourcePage = 17
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GuardianBattlehost,
            Name = "Time To Strike",
            Cost = 1,
            Category = "GUARDIAN BATTLEHOST – STRATEGIC PLOY STRATAGEM",
            When = "Your Movement phase.",
            Target = "One STORM GUARDIANS unit from your army that has not been selected to move this phase.",
            Effect = "Until the end of the phase, each time your unit Advances, do not make an Advance roll. Instead, until the end of the phase, add 6\" to the Move characteristic of models in your unit. Until the end of the turn, your unit is eligible to shoot and declare a charge in a turn in which it Advanced.",
            Restrictions = "",
            SourcePage = 18
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GuardianBattlehost,
            Name = "Blades Of Asuryan",
            Cost = 1,
            Category = "GUARDIAN BATTLEHOST – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase.",
            Target = "One DIRE AVENGERS or GUARDIANS unit from your army that has not been selected to shoot this phase.",
            Effect = "Until the end of the phase, ranged weapons equipped by models in your unit have the [PISTOL] ability.",
            Restrictions = "",
            SourcePage = 18
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GuardianBattlehost,
            Name = "Cost Of Victory",
            Cost = 1,
            Category = "GUARDIAN BATTLEHOST – STRATEGIC PLOY STRATAGEM",
            When = "End of your opponent’s Fight phase.",
            Target = "One GUARDIANS unit from your army.",
            Effect = "If your unit is not within Engagement Range of one or more enemy units, remove it from the battlefield and place it into Strategic Reserves. When doing so, return every destroyed GUARDIANS model to your unit. UNIQUE: ACROBATIC",
            Restrictions = "",
            SourcePage = 18
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GhostsOfTheWebway,
            Name = "Staged Death",
            Cost = 1,
            Category = "GHOSTS OF THE WEBWAY – STRATEGIC PLOY STRATAGEM",
            When = "Any phase.",
            Target = "One HARLEQUINS CHARACTER model from your army that was just destroyed. You can use this Stratagem on that model even though it was just destroyed.",
            Effect = "At the end of the phase, set your model back up on the battlefield as close as possible to where it was destroyed and not within Engagement Range of any enemy units, with half of its starting number of wounds remaining.",
            Restrictions = "Each model can only be targeted with this Stratagem once per battle.",
            SourcePage = 20
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GhostsOfTheWebway,
            Name = "Heroes’ Fall",
            Cost = 1,
            Category = "GHOSTS OF THE WEBWAY – STRATEGIC PLOY STRATAGEM",
            When = "Fight phase, just after an enemy unit has selected its targets.",
            Target = "One HARLEQUINS unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
            Effect = "Until the end of the phase, each time a model in your unit is destroyed, if that model has not fought this phase, roll one D6. On a 4+, do not remove the destroyed model from play; it can fight after the attacking unit has finished making its attacks, and is then removed from play.",
            Restrictions = "",
            SourcePage = 20
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GhostsOfTheWebway,
            Name = "Mocking Flight",
            Cost = 1,
            Category = "GHOSTS OF THE WEBWAY – STRATEGIC PLOY STRATAGEM",
            When = "Your Movement phase, just after a HARLEQUINS unit from your army Falls Back.",
            Target = "That HARLEQUINS unit.",
            Effect = "Until the end of the turn, your unit is eligible to shoot and declare a charge in a turn in which it Fell Back.",
            Restrictions = "",
            SourcePage = 20
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GhostsOfTheWebway,
            Name = "Tricksters’ Retort",
            Cost = 1,
            Category = "GHOSTS OF THE WEBWAY – STRATEGIC PLOY STRATAGEM",
            When = "Your opponent’s Movement phase, just after an enemy unit ends a Normal, Advance or Fall Back move.",
            Target = "One TROUPE unit from your army that is within 8\" of that enemy unit.",
            Effect = "Your unit can make a Normal move of up to 6\".",
            Restrictions = "",
            SourcePage = 20
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GhostsOfTheWebway,
            Name = "Bloody Dance",
            Cost = 1,
            Category = "GHOSTS OF THE WEBWAY – STRATEGIC PLOY STRATAGEM",
            When = "End of your opponent’s Charge phase.",
            Target = "One HARLEQUINS INFANTRY or HARLEQUINS MOUNTED unit from your army that is within 6\" of one or more enemy units and would be eligible to declare a charge against one or more of those enemy units if it were your Charge phase.",
            Effect = "Your unit now declares a charge that only targets one or more of those enemy units, and you resolve that charge.",
            Restrictions = "Note that even if this charge is successful, your unit does not receive any Charge bonus this turn.",
            SourcePage = 21
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.GhostsOfTheWebway,
            Name = "Exit The Stage",
            Cost = 1,
            Category = "GHOSTS OF THE WEBWAY – STRATEGIC PLOY STRATAGEM",
            When = "End of your opponent’s Fight phase.",
            Target = "One HARLEQUINS unit from your army that is not within Engagement Range of one or more enemy units.",
            Effect = "Remove your unit from the battlefield and place it into Strategic Reserves.",
            Restrictions = "",
            SourcePage = 21
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.DevotedOfYnnead,
            Name = "Pall Of Dread",
            Cost = 1,
            Category = "DEVOTED OF YNNEAD – STRATEGIC PLOY STRATAGEM",
            When = "Any phase.",
            Target = "One YNNARI unit from your army that was just destroyed while it was within range of one or more objective markers you controlled at the end of the previous phase. You can use this Stratagem on that unit even though it was just destroyed.",
            Effect = "Select one of those objective markers. That objective marker remains under your control until your opponent’s Level of Control over that objective marker is greater than yours at the end of a phase.",
            Restrictions = "",
            SourcePage = 24
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.DevotedOfYnnead,
            Name = "Macabre Resilience",
            Cost = 1,
            Category = "DEVOTED OF YNNEAD – BATTLE TACTIC STRATAGEM",
            When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
            Target = "One YNNARI INFANTRY or YNNARI MOUNTED unit from your army (excluding WRAITH CONSTRUCT units) that was selected as the target of one or more of the attacking unit’s attacks.",
            Effect = "Until the end of the phase, each time an attack targets your unit, subtract 1 from the Wound roll.",
            Restrictions = "",
            SourcePage = 24
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.DevotedOfYnnead,
            Name = "Emissaries Of Ynnead",
            Cost = 1,
            Category = "DEVOTED OF YNNEAD – BATTLE TACTIC STRATAGEM",
            When = "Fight phase, just after a YNNARI INFANTRY unit from your army has selected its targets.",
            Target = "That YNNARI INFANTRY unit.",
            Effect = "Until the end of the phase, each time a model in your unit makes an attack, re-roll a Hit roll of 1. If your unit is below its Starting Strength, you can re-roll the Hit roll instead.",
            Restrictions = "",
            SourcePage = 24
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.DevotedOfYnnead,
            Name = "Parting The Veil",
            Cost = 2,
            Category = "DEVOTED OF YNNEAD – STRATEGIC PLOY STRATAGEM",
            When = "Fight phase, just after an enemy unit has selected its targets.",
            Target = "One YNNARI unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
            Effect = "Until the end of the phase, each time a model in your unit is destroyed, if that model has not fought this phase, do not remove it from play. The destroyed model can fight after the attacking unit has finished making its attacks, and is then removed from play.",
            Restrictions = "",
            SourcePage = 25
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.DevotedOfYnnead,
            Name = "Soulsight",
            Cost = 1,
            Category = "DEVOTED OF YNNEAD – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase.",
            Target = "One YNNARI unit from your army that has not been selected to shoot this phase.",
            Effect = "Until the end of the phase, ranged weapons equipped by models in your unit have the [LETHAL HITS] and [IGNORES COVER] abilities.",
            Restrictions = "",
            SourcePage = 25
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.DevotedOfYnnead,
            Name = "Death Answers Death",
            Cost = 1,
            Category = "DEVOTED OF YNNEAD – STRATEGIC PLOY STRATAGEM",
            When = "End of your opponent’s Shooting phase.",
            Target = "One YNNARI unit from your army (excluding WRAITH CONSTRUCT units), if one or more models in that unit were destroyed this phase.",
            Effect = "Your unit can shoot as if it were your Shooting phase.",
            Restrictions = "",
            SourcePage = 25
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SeerCouncil,
            Name = "Presentiment Of Dread",
            Cost = 1,
            Category = "SEER COUNCIL – STRATEGIC PLOY STRATAGEM",
            When = "Command phase.",
            Target = "One ASURYANI PSYKER model from your army.",
            Effect = "Select one enemy unit within 18\" of and visible to your model. That enemy unit must take a Ba�le-shock test, subtracting 1 from that test.",
            Restrictions = "",
            SourcePage = 28
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SeerCouncil,
            Name = "Forewarned",
            Cost = 1,
            Category = "SEER COUNCIL – STRATEGIC PLOY STRATAGEM",
            When = "Fight phase, just after an enemy unit has selected its targets.",
            Target = "One ASURYANI INFANTRY unit from your army (excluding WRAITH CONSTRUCT units) that was selected as the target of one or more of the attacking unit’s attacks and is within 9\" of one or more friendly ASURYANI PSYKER models.",
            Effect = "Until the end of the phase, each time an attack targets your unit, subtract 1 from the Hit roll and subtract 1 from the Wound roll.",
            Restrictions = "",
            SourcePage = 28
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SeerCouncil,
            Name = "Unshrouded Truth",
            Cost = 1,
            Category = "SEER COUNCIL – STRATEGIC PLOY STRATAGEM",
            When = "Your Movement phase.",
            Target = "One ASURYANI INFANTRY unit from your army (excluding WRAITH CONSTRUCT units) that has not been selected to move this phase, was not set up on the battlefield this phase, and is within 9\" of one or more friendly ASURYANI PSYKER models.",
            Effect = " ▪ Place your unit in strategic reserves. ▪ Your unit has Deep Strike. ▪ Your unit must make an ingress move this phase.",
            Restrictions = "Until the end of the phase, your unit is not eligible to be selected to move.",
            SourcePage = 28
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SeerCouncil,
            Name = "Fate Inescapable",
            Cost = 1,
            Category = "SEER COUNCIL – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase.",
            Target = "One ASURYANI INFANTRY unit from your army (excluding WRAITH CONSTRUCT units) that has not been selected to shoot this phase and is within 9\" of one or more friendly ASURYANI PSYKER models.",
            Effect = "Until the end of the phase, ranged weapons equipped by models in your unit have the [IGNORES COVER] ability and each time a model in your unit makes an attack, on a Critical Wound, improve the Armour Penetration characteristic of that attack by 1.",
            Restrictions = "",
            SourcePage = 29
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SeerCouncil,
            Name = "Isha’S Fury",
            Cost = 1,
            Category = "SEER COUNCIL – EPIC DEED STRATAGEM",
            When = "Your opponent’s Movement phase, just after an enemy unit ends a Normal, Advance or Fall Back move.",
            Target = "One ASURYANI PSYKER model from your army within 9\" of that enemy unit.",
            Effect = "Roll six D6: for each 3+, that enemy unit suffers 1 mortal wound.",
            Restrictions = "",
            SourcePage = 29
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SeerCouncil,
            Name = "Psychic Shield",
            Cost = 1,
            Category = "SEER COUNCIL – STRATEGIC PLOY STRATAGEM",
            When = "Your opponent’s Shooting phase, just after an enemy unit has selected its targets.",
            Target = "One ASURYANI INFANTRY unit from your army (excluding WRAITH CONSTRUCT units) that was selected as the target of one or more of the attacking unit’s attacks and is within 9\" of one or more friendly ASURYANI PSYKER models.",
            Effect = "Until the end of the phase, your unit can only be selected as the target of a ranged attack if the attacking model is within 18\".",
            Restrictions = "",
            SourcePage = 29
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.AspectHost,
            Name = "Warrior Focus",
            Cost = 1,
            Category = "ASPECT HOST – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase or the Fight phase.",
            Target = "One ASPECT WARRIORS or AVATAR OF KHAINE unit from your army that has not been selected to shoot or fight this phase.",
            Effect = "Until the end of the phase, each time a model in your unit makes an attack, you can ignore any or all modifiers to that attack’s Ballistic Skill, Weapon skill, Strength, Armour Penetration and Damage characteristics and/or any or all modifiers to the Hit roll.",
            Restrictions = "",
            SourcePage = 31
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.AspectHost,
            Name = "To Their Final Breath",
            Cost = 1,
            Category = "ASPECT HOST – STRATEGIC PLOY STRATAGEM",
            When = "Fight phase, just after an enemy unit has selected its targets.",
            Target = "One ASPECT WARRIORS or AVATAR OF KHAINE unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
            Effect = "Each time you use this Stratagem, you can remove one Aspect Shrine token your unit has (see datasheets). Then, until the end of the phase, each time a model in your unit is destroyed, if that model has not fought this phase, roll one D6, adding 1 to the result if you removed an Aspect Shrine token during this usage of this Stratagem. On a 4+, do not remove the destroyed model from play; it can fight after the attacking unit has finished making its attacks, and is then removed from play.",
            Restrictions = "",
            SourcePage = 31
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.AspectHost,
            Name = "Skyborne Sanctuary",
            Cost = 1,
            Category = "ASPECT HOST – STRATEGIC PLOY STRATAGEM",
            When = "End of the Fight phase.",
            Target = "One unengaged ASURYANI unit from your army that was eligible to fight this phase and one friendly TRANSPORT it is able to embark within.",
            Effect = "If your ASURYANI unit is wholly within 6” of that TRANSPORT, it can embark within it.",
            Restrictions = "",
            SourcePage = 31
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.AspectHost,
            Name = "Doom Inescapable",
            Cost = 1,
            Category = "ASPECT HOST – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase.",
            Target = "One AVATAR OF KHAINE model from your army that has not been selected to shoot this phase.",
            Effect = "Until the end of the phase, your model’s Wailing Doom ranged weapon has a Range characteristic of 18\" and a Damage characteristic of 8.",
            Restrictions = "",
            SourcePage = 32
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.AspectHost,
            Name = "Preternatural Precision",
            Cost = 1,
            Category = "ASPECT HOST – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase.",
            Target = "One ASPECT WARRIORS unit from your army that has not been selected to shoot this phase.",
            Effect = "Each time you use this Stratagem, you can remove one Aspect Shrine token your unit has (see datasheets). Then, select one of the following abilities, or select two of the following abilities if you removed an Aspect Shrine token during this usage of this Stratagem: [IGNORES COVER], [LETHAL HITS], [SUSTAINED HITS 1]. Until the end of the phase, ranged weapons equipped by models in your unit have the selected abilities.",
            Restrictions = "",
            SourcePage = 32
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.AspectHost,
            Name = "Khaine’S Vengeance",
            Cost = 1,
            Category = "ASPECT HOST – STRATEGIC PLOY STRATAGEM",
            When = "Your opponent’s Movement phase, just after an enemy unit (excluding MONSTERS and VEHICLES) is selected to Fall Back.",
            Target = "One ASPECT WARRIORS or AVATAR OF KHAINE unit from your army that is within Engagement Range of that enemy unit.",
            Effect = "All models in that enemy unit must take a Desperate Escape test. When doing so, if that enemy unit is Ba�le- shocked, subtract 1 from each of those tests.",
            Restrictions = "",
            SourcePage = 32
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.ArmouredWarhost,
            Name = "Layered Wards",
            Cost = 1,
            Category = "ARMOURED WARHOST STRATAGEM",
            When = "Any phase, when a friendly AELDARI VEHICLE unit suffers a mortal wound.",
            Target = "That AELDARI VEHICLE unit.",
            Effect = "Your unit has Feel No Pain 5+ against mortal wounds.",
            Restrictions = "",
            SourcePage = 34
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.ArmouredWarhost,
            Name = "Soulsight",
            Cost = 1,
            Category = "ARMOURED WARHOST STRATAGEM",
            When = "Your Shooting phase, when a friendly AELDARI VEHICLE unit is selected to shoot.",
            Target = "That AELDARI VEHICLE unit.",
            Effect = "Your unit’s attacks can re-roll: ▪ One hit roll. ▪ One wound roll. ▪ One damage roll.",
            Restrictions = "",
            SourcePage = 34
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.ArmouredWarhost,
            Name = "Vectored Engines",
            Cost = 1,
            Category = "ARMOURED WARHOST STRATAGEM",
            When = "Your Movement phase, when a friendly AELDARI VEHICLE unit makes a fall-back move.",
            Target = "That AELDARI VEHICLE unit.",
            Effect = "That move does not prevent your unit from being eligible to shoot. UNIQUE: ACROBATIC",
            Restrictions = "",
            SourcePage = 34
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.FatefulPerformance,
            Name = "Heroes’ Fall",
            Cost = 1,
            Category = "FATEFUL PERFORMANCE STRATAGEM",
            When = "Fight phase, when an enemy unit targets a friendly HARLEQUINS unit.",
            Target = "That HARLEQUINS unit.",
            Effect = "When a model in your unit is destroyed, if your unit has not been selected to fight this phase, roll one D6: ▪ On a 4+, do not remove that model from the battlefield. When your unit has fought, or at the end of the phase (whichever comes first), that model is removed from the battlefield.",
            Restrictions = "",
            SourcePage = 36
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.FatefulPerformance,
            Name = "Exit The Stage",
            Cost = 1,
            Category = "FATEFUL PERFORMANCE STRATAGEM",
            When = "End of your opponent’s Fight phase.",
            Target = "One friendly unengaged HARLEQUINS unit.",
            Effect = "Place your unit in strategic reserves",
            Restrictions = "",
            SourcePage = 36
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.FatefulPerformance,
            Name = "Deceptive Feint",
            Cost = 1,
            Category = "FATEFUL PERFORMANCE STRATAGEM",
            When = "Your opponent’s Movement phase, when an enemy unit ends a move within 8\" of a friendly unengaged HARLEQUINS INFANTRY unit.",
            Target = "That HARLEQUINS INFANTRY unit.",
            Effect = "Your unit can make a normal move of up to D3+3\".",
            Restrictions = "",
            SourcePage = 36
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.PathOfTheOutcast,
            Name = "Eldritch Suppression",
            Cost = 1,
            Category = "PATH OF THE OUTCAST STRATAGEM",
            When = "Your Shooting phase, when a friendly RANGERS/SHROUD RUNNERS unit has shot.",
            Target = "That RANGERS/SHROUD RUNNERS unit.",
            Effect = "Select one enemy unit hit by those ranged attacks. That enemy unit makes a battle-shock roll, with -1 to that battle-shock roll if a model in that enemy unit was destroyed by those attacks.",
            Restrictions = "",
            SourcePage = 38
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.PathOfTheOutcast,
            Name = "Casting Back The Veil",
            Cost = 1,
            Category = "PATH OF THE OUTCAST STRATAGEM",
            When = "Your Shooting phase, when a friendly RANGERS/SHROUD RUNNERS unit has shot.",
            Target = "That RANGERS/SHROUD RUNNERS unit.",
            Effect = "Select one enemy unit hit by those ranged attacks. That enemy unit has +6\" detection range.",
            Restrictions = "",
            SourcePage = 38
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.PathOfTheOutcast,
            Name = "Nomads Of The Hidden Way",
            Cost = 1,
            Category = "PATH OF THE OUTCAST STRATAGEM",
            When = "Your Shooting phase, when a friendly RANGERS/SHROUD RUNNERS unit has shot.",
            Target = "That RANGERS/SHROUD RUNNERS unit.",
            Effect = " ▪ Your unit can make a normal move of up to D6\". ▪ Your unit is not eligible to declare a charge or embark within a TRANSPORT until the end of the turn. UNIQUE: ACROBATIC",
            Restrictions = "",
            SourcePage = 38
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.TwilightFlickers,
            Name = "Presaged Rehearsal",
            Cost = 1,
            Category = "TWILIGHT FLICKERS STRATAGEM",
            When = "Fight phase, when a friendly TROUPE unit is selected to fight.",
            Target = "That TROUPE unit.",
            Effect = "Your unit’s melee attacks have [LANCE].",
            Restrictions = "",
            SourcePage = 40
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.TwilightFlickers,
            Name = "Captivating Performance",
            Cost = 1,
            Category = "TWILIGHT FLICKERS STRATAGEM",
            When = "End of your Movement phase.",
            Target = "One friendly TROUPE unit.",
            Effect = "Select one objective your unit is controlling. That objective is secured.",
            Restrictions = "",
            SourcePage = 40
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.TwilightFlickers,
            Name = "Phantasmal Mirage",
            Cost = 1,
            Category = "TWILIGHT FLICKERS STRATAGEM",
            When = "Your Shooting phase, when a friendly HARLEQUINS VEHICLE unit has shot.",
            Target = "That HARLEQUINS VEHICLE unit.",
            Effect = " ▪ Your unit can make a normal move of up to D6\". ▪ Your unit is not eligible to declare a charge until the end of the turn. UNIQUE: ACROBATIC",
            Restrictions = "",
            SourcePage = 40
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SerpentsBrood,
            Name = "Fangs Of The Brood",
            Cost = 1,
            Category = "SERPENT’S BROOD STRATAGEM",
            When = "Start of the Fight phase.",
            Target = "One TROUPE unit from your army.",
            Effect = "Until the end of the phase, when using your unit’s Dance of Death ability, you can select three of the abilities for your unit to gain, instead of one.",
            Restrictions = "",
            SourcePage = 42
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SerpentsBrood,
            Name = "Venomous Wrath",
            Cost = 1,
            Category = "SERPENT’S BROOD STRATAGEM",
            When = "Your Shooting phase.",
            Target = "One HARLEQUINS VEHICLE unit from your army that has not been selected to shoot this phase.",
            Effect = "A�er your unit has shot, if it is not within Engagement Range of one or more enemy units, it can make a Normal move of up to 6\". Until the end of the turn, your unit is not eligible to declare a charge.",
            Restrictions = "",
            SourcePage = 42
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SerpentsBrood,
            Name = "Striking Stride",
            Cost = 1,
            Category = "SERPENT’S BROOD STRATAGEM",
            When = "Your Charge phase.",
            Target = "One HARLEQUINS unit from your army.",
            Effect = "Until the end of the phase, your unit is eligible to declare a charge in a turn in which it Advanced.",
            Restrictions = "",
            SourcePage = 42
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SerpentsBrood,
            Name = "Weavers’ Coils",
            Cost = 1,
            Category = "SERPENT’S BROOD STRATAGEM",
            When = "End of your Fight phase.",
            Target = "One HARLEQUINS MOUNTED unit from your army that was eligible to fight this phase.",
            Effect = "If your unit is not within Engagement Range of one or more enemy units, it can make a Normal move. Otherwise, your unit can make a Fall Back move of up to 6\".",
            Restrictions = "",
            SourcePage = 42
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SerpentsBrood,
            Name = "Weaving Stride",
            Cost = 1,
            Category = "SERPENT’S BROOD STRATAGEM",
            When = "Your opponent’s Movement phase, just after an enemy unit ends a Normal, Advance or Fall Back move.",
            Target = "One HARLEQUINS INFANTRY unit from your army that is within 8\" of that enemy unit.",
            Effect = "Your unit can make a Normal move of up to 6\".",
            Restrictions = "",
            SourcePage = 43
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.SerpentsBrood,
            Name = "Skyward Lunge",
            Cost = 1,
            Category = "SERPENT’S BROOD STRATAGEM",
            When = "End of your opponent’s Fight phase.",
            Target = "One HARLEQUINS VEHICLE or HARLEQUINS MOUNTED unit from your army.",
            Effect = "If your unit is not within Engagement Range of one or more enemy units, you can remove it from the battlefield and place it into Strategic Reserves.",
            Restrictions = "",
            SourcePage = 43
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.EldritchRaiders,
            Name = "Raiders’ Spoils",
            Cost = 1,
            Category = "ELDRITCH RAIDERS – STRATEGIC PLOY STRATAGEM",
            When = "Command phase.",
            Target = "One ANHRATHE unit from your army that is within Engagement Range of one or more enemy units.",
            Effect = "Until the start of the next Command phase, add 1 to the Objective Control characteristic of models in your unit.",
            Restrictions = "",
            SourcePage = 45
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.EldritchRaiders,
            Name = "Ruthless Killers",
            Cost = 1,
            Category = "ELDRITCH RAIDERS – STRATEGIC PLOY STRATAGEM",
            When = "Your Shooting phase or the Fight phase.",
            Target = "One CORSAIR VOIDSCARRED unit from your army that has not been selected to shoot or Fight this phase.",
            Effect = "Until the end of the phase, each time a model in your unit makes an attack, add 1 to the Damage characteristic of that attack.",
            Restrictions = "",
            SourcePage = 45
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.EldritchRaiders,
            Name = "Yriel’S Example",
            Cost = 1,
            Category = "ELDRITCH RAIDERS – EPIC DEED STRATAGEM",
            When = "Fight phase, just after an enemy unit has selected its targets.",
            Target = "One AELDARI INFANTRY unit from your army (excluding WRAITH CONSTRUCT units) that was selected as the target of one or more of the attacking unit’s attacks.",
            Effect = "Until the end of the phase, models in your unit have the Feel No Pain 5+ ability.",
            Restrictions = "",
            SourcePage = 45
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.EldritchRaiders,
            Name = "No Prey Too Big",
            Cost = 1,
            Category = "ELDRITCH RAIDERS – BATTLE TACTIC STRATAGEM",
            When = "Your Shooting phase.",
            Target = "One ANHRATHE, RANGERS or SHROUD RUNNERS unit from your army that has not been selected to shoot this phase.",
            Effect = "Until the end of the phase, each time a model in your unit makes an attack, if the Strength characteristic of that attack is less than the highest Toughness characteristic of models in the target unit, add 1 to the Wound roll.",
            Restrictions = "",
            SourcePage = 45
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.EldritchRaiders,
            Name = "Impeding Fire",
            Cost = 1,
            Category = "ELDRITCH RAIDERS – WARGEAR STRATAGEM",
            When = "Start of your opponent’s Charge phase.",
            Target = "One RANGERS, SHROUD RUNNERS or STARFANG unit from your army.",
            Effect = "Select one enemy unit (excluding TITANIC units) visible to and within 36\" of your unit. Until the end of the phase, each time that enemy unit declares a charge, subtract 2 from the Charge roll (this is not cumulative with any other negative modifiers to that Charge roll).",
            Restrictions = "",
            SourcePage = 46
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.EldritchRaiders,
            Name = "Withdraw And Reinforce",
            Cost = 1,
            Category = "ELDRITCH RAIDERS – STRATEGIC PLOY STRATAGEM",
            When = "End of your opponent’s Fight phase.",
            Target = "One ANHRATHE unit from your army that is not within Engagement Range of one or more enemy units.",
            Effect = "Remove your unit from the battlefield and place it into Strategic Reserves. If that unit is below Starting Strength, return all destroyed models (excluding CHARACTER models) to that unit.",
            Restrictions = "",
            SourcePage = 46
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.CorsairCoterie,
            Name = "Pirates’ Due",
            Cost = 1,
            Category = "CORSAIR COTERIE – BATTLE TACTIC STRATAGEM",
            When = "The Fight phase.",
            Target = "One AELDARI unit from your army that has not been selected to fight this phase.",
            Effect = "Until the end of the phase, each time a model in your unit makes an attack, re-roll a Wound roll of 1. If your unit has the ANHRATHE keyword, then until the end of the phase, each time a model in your unit makes an attack that targets an enemy unit within range of an objective marker, you can re-roll the Wound roll instead.",
            Restrictions = "",
            SourcePage = 48
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.CorsairCoterie,
            Name = "Lethal Ruse",
            Cost = 1,
            Category = "CORSAIR COTERIE – STRATEGIC PLOY STRATAGEM",
            When = "Your Movement phase, just after an AELDARI unit from your army Falls Back.",
            Target = "That AELDARI unit.",
            Effect = "Until the end of the turn, your unit is eligible to declare a charge in a turn in which it Fell Back. If it is an ANHRATHE unit, also select one enemy unit your unit was within Engagement Range of at the start of the phase, and roll six D6: for each 4+, that enemy unit suffers 1 mortal wound.",
            Restrictions = "",
            SourcePage = 48
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.CorsairCoterie,
            Name = "Outcast Ambush",
            Cost = 1,
            Category = "CORSAIR COTERIE – STRATEGIC PLOY STRATAGEM",
            When = "Your Shooting phase.",
            Target = "One RANGERS or SHROUD RUNNERS unit from your army that has not been selected to shoot this phase.",
            Effect = "Until the end of the phase, ranged weapons equipped by models in your unit have the [IGNORES COVER] and [RAPID FIRE 1] abilities, and until the end of the phase, improve the Armour Penetration characteristic of those weapons by 1.",
            Restrictions = "",
            SourcePage = 48
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.CorsairCoterie,
            Name = "Into The Breach",
            Cost = 1,
            Category = "CORSAIR COTERIE – STRATEGIC PLOY STRATAGEM",
            When = "Your Shooting phase, just after an ANHRATHE unit from your army destroyed one or more enemy units.",
            Target = "That ANHRATHE unit.",
            Effect = "A�er your unit has resolved all of its shooting attacks, it can make a Normal move of up to D6+1\".",
            Restrictions = "",
            SourcePage = 48
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.CorsairCoterie,
            Name = "Cloak And Shadow",
            Cost = 1,
            Category = "CORSAIR COTERIE – STRATEGIC PLOY STRATAGEM",
            When = "Your opponent’s Shooting phase, just after an enemy unit has selected its targets.",
            Target = "One AELDARI INFANTRY unit from your army that is within range of an objective marker that you control and that was selected as the target of one or more of the attacking unit’s attacks.",
            Effect = "Until the end of the phase, models in your unit have the Stealth ability and your unit can only be selected as the target of a ranged attack if the attacking model is within 18\".",
            Restrictions = "",
            SourcePage = 49
        });
        stratagems.Add(new AeldariStratagem11
        {
            Detachment = AeldariDetachment.CorsairCoterie,
            Name = "Vengeful Sorrow",
            Cost = 1,
            Category = "CORSAIR COTERIE – STRATEGIC PLOY STRATAGEM",
            When = "Your opponent’s Shooting phase, just after an enemy unit has shot.",
            Target = "One AELDARI INFANTRY unit from your army, if one or more models in that unit were destroyed as a result of those attacks, and if that AELDARI unit is neither Ba�le-shocked nor within Engagement Range of one or more enemy units.",
            Effect = "Your unit can make a surge move of up to D6+1\".",
            Restrictions = "",
            SourcePage = 49
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.Warhost,
            Name = "Phoenix Gem",
            Points = 35,
            Rule = "Aeldari myth tells how Isha once drew down the heat of a hundred stars into a glittering gem to save Asuryan. The Phoenix Gem is the only surviving fragment of this ancient stone and retains the power to return life to the fallen. ASURYANI model only. The first time the bearer is destroyed, remove it from play, then, at the end of the phase, roll one D6: on a 2+, set the bearer back up on the battlefield as close as possible to where it was destroyed and not within Engagement Range of one or more enemy units, with its full wounds remaining.",
            SourcePage = 7
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.Warhost,
            Name = "Timeless Strategist",
            Points = 15,
            Rule = "This ancient Aeldari war leader has commanded armies for the entire lifetimes of the younger mortal species. Their mastery of the swift, decisive and reactive strategy is second to none. ASURYANI model only. At the start of the battle round, if the bearer is on the battlefield (or any TRANSPORT it is embarked within is on the battlefield), you receive 1 additional Ba�le Focus token.",
            SourcePage = 7
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.Warhost,
            Name = "Gift Of Foresight",
            Points = 15,
            Rule = "It is far easier to avoid fatal battlefield errors if one has already foreseen when they will occur and how to prevent them. ASURYANI model only. Once per battle round, you can target the bearer’s unit with the Command Re-roll Stratagem for 0CP.",
            SourcePage = 7
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.Warhost,
            Name = "Psychic Destroyer",
            Points = 30,
            Rule = "This psyker has refined the destructive potential of their mental abilities, honing them to a fine and frighteningly lethal point. ASURYANI PSYKER model only. Add 1 to the Damage characteristic of ranged Psychic weapons equipped by the bearer.",
            SourcePage = 7
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.WindriderHost,
            Name = "Firstdrawn Blade",
            Points = 10,
            Rule = "Those who lead the Windrider Hosts do so from the front, embodying the first of the Swords of Vaul drawn in wrath. ASURYANI MOUNTED model only. Models in the bearer’s unit have the Scouts 9\" ability.",
            SourcePage = 10
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.WindriderHost,
            Name = "Mirage Field",
            Points = 25,
            Rule = "This field generator surrounds its bearer with contradictory sensor ghosts and split-second illusions that make them incredibly challenging to target, particularly when moving at speed. ASURYANI MOUNTED model only. Each time an attack targets the bearer’s unit, subtract 1 from the Hit roll.",
            SourcePage = 10
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.WindriderHost,
            Name = "Seersight Strike",
            Points = 15,
            Rule = "This warrior has mastered the art of augmenting their mounted attack runs with lashing blades of focused psychic power that punch through the heaviest armour and monstrous hide to vital targets deep within. ASURYANI MOUNTED PSYKER model only. Psychic weapons equipped by the bearer have the [ANTI-MONSTER 2+] and [ANTI-VEHICLE 2+] abilities.",
            SourcePage = 10
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.WindriderHost,
            Name = "Echoes Of Ulthanesh",
            Points = 20,
            Rule = "Riding out to battle upon their skimming steed, this charismatic commander is reminiscent of the great Aeldari hero come again. The mere sight of them swooping down upon their foes inspires their warriors to remarkable feats of heroism in their turn. ASURYANI MOUNTED model only. In your Command phase, roll one D6, adding 1 to the result if the bearer is not within your deployment zone, and adding an additional 1 to the result if the bearer is within your opponent’s deployment zone: on a 5+, you gain 1CP.",
            SourcePage = 10
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SpiritConclave,
            Name = "Light Of Clarity",
            Points = 30,
            Rule = "The mind of this Spiritseer shines with psychic illumination, burning away the veils that cloud the senses of the dead. SPIRITSEER model only. In your Command phase, select one friendly WRAITH CONSTRUCT unit within 12\" of the bearer. Until the start of your next Command phase, add 1 to the Objective Control characteristic of INFANTRY models in that unit and add 3 to the Objective Control characteristic of MONSTER models in that unit.",
            SourcePage = 13
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SpiritConclave,
            Name = "Stave Of Kurnous",
            Points = 15,
            Rule = "Wound about with finely inscribed myths of Kurnous, the Hunter, this wraithbone staﬀ imbues ghost warriors with an echo of the slain god’s keen-eyed skill. SPIRITSEER model only. In your Command phase, select one friendly WRAITH CONSTRUCT unit within 12\" of the bearer (excluding TITANIC units). Until the start of your next Command phase, each time a model in that unit makes an attack, on a Critical Wound, that attack has the [PRECISION] ability.",
            SourcePage = 13
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SpiritConclave,
            Name = "Rune Of Mists",
            Points = 10,
            Rule = "This rare psychic rune binds occluding psychic energies about nearby wraith constructs to baﬄe the foe’s senses. SPIRITSEER model only. In your Command phase, select one friendly WRAITH CONSTRUCT unit within 12\" of the bearer. Until the start of your next Command phase, each time a ranged attack targets that unit, unless the attacking model is within 18\", models in that unit have the Benefit of Cover against that attack.",
            SourcePage = 13
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SpiritConclave,
            Name = "Higher Duty",
            Points = 25,
            Rule = "Knowing their crucial role in guiding the ghost warriors, this seer chooses duty over personal glory. SPIRITSEER model only. In your opponent’s Movement phase, if an enemy unit ends a move within 8\" of this unit, if this unit is not within Engagement Range of one or more enemy units, this unit can make a Normal move of up to 6\".",
            SourcePage = 13
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.GuardianBattlehost,
            Name = "Craftworld’S Champion",
            Points = 25,
            Rule = "Appointed as the mastermind behind the defence of an entire craftworld, this warrior will hold vital ground at all costs. ASURYANI model only. The bearer has an Objective Control characteristic of 5.",
            SourcePage = 16
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.GuardianBattlehost,
            Name = "Ethereal Pathway",
            Points = 30,
            Rule = "Knowing secret paths through the Webway, the bearer can direct warriors to outmanoeuvre the foe. ASURYANI model only. In the Deploy Armies step, select up to two GUARDIANS units from your army. Models in the selected units have the Infiltrators ability.",
            SourcePage = 16
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.GuardianBattlehost,
            Name = "Protector Of The Paths",
            Points = 20,
            Rule = "This warrior’s knowledge of the home ground on which they fight allows them to expertly position their forces to bracket the routes of the enemy’s approach with fire. ASURYANI model only. While the bearer is leading a DIRE AVENGERS or GUARDIANS unit, once per battle round, you can target the bearer’s unit with the Fire Overwatch Stratagem for 0CP, and while resolving that Stratagem, hits are scored on unmodified Hit rolls of 5+, or unmodified Hit rolls of 4+ instead if the bearer’s unit is within range of an objective marker you control.",
            SourcePage = 16
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.GuardianBattlehost,
            Name = "Breath Of Vaul",
            Points = 10,
            Rule = "An ancient relic of Aeldari technology, this device enhances the lethality of those weapons said to channel the killing heal ofVaul’s blazing forges. ASURYANI model only. While the bearer is leading a STORM GUARDIANS unit, each time you roll to determine the number of attacks made with a flamer equipped by a model in that unit, you can re-roll the result, and each time you make a Damage roll for a model equipped with a fusion gun in that unit, you can re-roll the result.",
            SourcePage = 16
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.GhostsOfTheWebway,
            Name = "Cegorach’S Coil",
            Points = 25,
            Rule = "This vicious monofilament lariat unspools from its wielder’s gauntlet in a heartbeat, lashing through the foe before retracting as they tumble into bloody chunks. TROUPE MASTER model only. Each time the bearer’s unit ends a Charge move, select one enemy unit within Engagement Range of the bearer’s unit, then roll one D6 for each model in the bearer’s unit that is within Engagement Range of that enemy unit: for each 4+, that enemy unit suffers 1 mortal wound (to a maximum of 6 mortal wounds).",
            SourcePage = 19
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.GhostsOfTheWebway,
            Name = "Mask Of Secrets",
            Points = 15,
            Rule = "So terrifying and yet so captivating are the shi�ing aspects of this Harlequin’s psychoreactive mask that they can hypnotise foes or stop their hearts with fear. HARLEQUINS model only. Each time an enemy unit (excluding MONSTERS and VEHICLES) within Engagement Range of the bearer’s unit Falls Back, all models in that enemy unit must take a Desperate Escape test. When doing so, if that enemy unit is Ba�le-shocked, subtract 1 from each of those tests.",
            SourcePage = 19
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.GhostsOfTheWebway,
            Name = "Murder’S Jest",
            Points = 20,
            Rule = "This malevolent shrieker cannon catalyses its victims’ fear impulses into exaggerated bioelectric shocks, forcing them to scare themselves to death. DEATH JESTER model only. Each time the bearer makes an attack that targets a unit that is Below Half-strength, each successful Hit roll scores a Critical Hit.",
            SourcePage = 19
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.GhostsOfTheWebway,
            Name = "Mistweave",
            Points = 15,
            Rule = "Employing their psychic powers of illusion and obfuscation, this Shadowseer can cause their comrades to vanish from the foe’s perceptions. SHADOWSEER model only. While the bearer is leading a unit, models in that unit have the Infiltrators ability.",
            SourcePage = 19
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.DevotedOfYnnead,
            Name = "Gaze Of Ynnead",
            Points = 15,
            Rule = "This psychic executioner projects the extinguishing will of the Aeldari death god into the minds of the fc reducing them to ashen grey husks in moments. FARSEER model only. The bearer’s Eldritch Storm weapon has the [DEVASTATING WOUNDS] ability.",
            SourcePage = 23
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.DevotedOfYnnead,
            Name = "Storm Of Whispers",
            Points = 10,
            Rule = "As though this Warlock were a conduit to the realm of the unquiet dead, they are surrounded by an endless susurrus that chills the foe with terror. WARLOCK model only. In your Shooting phase, after the bearer has shot, select one enemy unit hit by one or more of those attacks. That unit must take a Ba�le-shock test.",
            SourcePage = 23
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.DevotedOfYnnead,
            Name = "Borrowed Vigour",
            Points = 10,
            Rule = "This cruel warrior steals a portion of animus from each vanquished foe, keeping a li�le of Ynnead’s due to empower themselves and slay more foes in his name. ARCHON model only. Add 2 to the A�acks characteristic of the bearer’s melee weapons.",
            SourcePage = 23
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.DevotedOfYnnead,
            Name = "Morbid Might",
            Points = 15,
            Rule = "Driven to new heights of cold fury and icy strength by the death energies flowing through their sinews, this arena champion fights with supernatural vigour. SUCCUBUS model only. Each time the bearer makes a melee attack, you can re-roll the Wound roll.",
            SourcePage = 23
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SeerCouncil,
            Name = "Lucid Eye",
            Points = 30,
            Rule = "This helm houses a psychocrystalline weave that aids the wearer in si�ing clarity and truth from the myriad ghosts of unrealised futures. ASURYANI PSYKER model only. In your Command phase, you can add 1 to or subtract 1 from the value of one Fate dice in your Fate dice pool.",
            SourcePage = 27
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SeerCouncil,
            Name = "Runes Of Warding",
            Points = 25,
            Rule = "These precisely cra�ed runic wards hold harmful empyric energies and predatory entities at hay, protecting the bearer’s mind and soul. ASURYANI PSYKER model only. Models in the bearer’s unit have the Feel No Pain 4+ ability against mortal wounds, Psychic A�acks and Critical Wounds caused by attacks with the [DEVASTATING WOUNDS] ability.",
            SourcePage = 27
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SeerCouncil,
            Name = "Stone Of Eldritch Fury",
            Points = 15,
            Rule = "This ancient gem was recovered from the crone world of Lleghaine and is said to resonate with the rage of that dead world’s ghosts. It acts as a magnifying lens for destructive psychic powers. ASURYANI PSYKER model only. Add 12\" to the Range characteristic of ranged Psychic weapons equipped by the bearer.",
            SourcePage = 27
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SeerCouncil,
            Name = "Torc Of Morai-Heg",
            Points = 20,
            Rule = "Waves of malevolent energies roll from this article of warrior jewellery. Though they leave Aeldari systems and minds untouched, they cloud the enemy’s thoughts with doubt, lace their communications with mournful wails and scafter confusing sensor ghosts across their instruments. ASURYANI PSYKER model only. Once per turn, when your opponent targets a unit from their army within 12\" of the bearer with a Stratagem, the bearer can use this Enhancement. If it does, increase the CP cost of that usage of that Stratagem by 1CP.",
            SourcePage = 27
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.AspectHost,
            Name = "Aspect Of Murder",
            Points = 15,
            Rule = "Khaine is a murderous deity, and many of his aspects channel this element of his nature to some degree. Combining these teachings renders this warrior a truly fearsome assassin. AUTARCH or AUTARCH WAYLEAPER model only. Add 1 to the Damage characteristic of melee weapons equipped by the bearer, and those weapons have the [PRECISION] ability.",
            SourcePage = 30
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.AspectHost,
            Name = "Mantle Of Wisdom",
            Points = 20,
            Rule = "This ritual relic marks the bearer out as one who has walked the Path of Command for ages untold and whose understanding of all things martial borders on the preternatural. AUTARCH or AUTARCH WAYLEAPER model only. While the bearer is leading an ASPECT WARRIORS unit, each time that unit is selected to shoot or fight, until the end of the phase, models in that unit gain both of the abilities from the Path of the Warrior Detachment rule.",
            SourcePage = 30
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.AspectHost,
            Name = "Shimmerstone",
            Points = 10,
            Rule = "This simple and elegant gem conceals complex technology based on Dire Avenger shimmershields, its protective aegis extending across the bearer and those who fight at their side. AUTARCH or AUTARCH WAYLEAPER model only. While the bearer is leading an ASPECT WARRIORS unit, each time a ranged attack targets that unit, subtract 1 from the Wound roll.",
            SourcePage = 30
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.AspectHost,
            Name = "Strategic Savant",
            Points = 10,
            Rule = "A commander who knows with absolute certainty which strategic goals must be achieved, this warrior can appraise the battlefield at a glance. AUTARCH or AUTARCH WAYLEAPER model only. While the bearer is leading an ASPECT WARRIORS unit, add 1 to the Objective Control characteristic of models in that unit.",
            SourcePage = 30
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.ArmouredWarhost,
            Name = "Spirit Stone Of Raelyth",
            Points = 20,
            Rule = "This spirit stone contains the essence of Bonesinger Raelyth. Those Asuryani psykers who bear this item to battle can draw upon the fallen artisan’s talents. AELDARI PSYKER model only. ▪ While this model is within 3\" of a friendly AELDARI VEHICLE unit, this model has Lone Operative. ▪ In your Movement phase, at the start or end of this unit’s move, you can select one friendly AELDARI VEHICLE model within 3\" of this model. That VEHICLE model heals D3 wounds.",
            SourcePage = 33
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.ArmouredWarhost,
            Name = "Guiding Presence",
            Points = 25,
            Rule = "This seer is closely a�uned to their craftworld’s vehicles. Communicating with the souls inhabiting hull-mounted spirit stones can sharpen the tactical awareness of spirits and crew. AELDARI PSYKER model only. At the start of your Shooting phase, select one visible friendly AELDARI VEHICLE unit within 6\" of this model. That VEHICLE unit’s ranged attacks have +1 to hit rolls.",
            SourcePage = 33
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.FatefulPerformance,
            Name = "A Foot In The Future",
            Points = 15,
            Rule = "Flowing like starlight across the field of battle, this warrior-artiste leads their chorus in a dance whose speed peaks as they surge into the foe. TROUPE MASTER model only. This unit can re-roll charge rolls",
            SourcePage = 35
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.FatefulPerformance,
            Name = "Mistweave",
            Points = 20,
            Rule = "Employing their psychic powers of illusion and obfuscation, this Shadowseer can cause their comrades to vanish from the foe’s perceptions. SHADOWSEER model only. This unit has Infiltrators.",
            SourcePage = 35
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.PathOfTheOutcast,
            Name = "Camouflaged Snipers Upgrade",
            Points = 10,
            Rule = "Aeldari Rangers rely upon fieldcra� and marksmanship to defeat their foes. They conceal themselves so expertly that even the act of firing upon the enemy does not reveal their precise location. RANGERS unit only. This unit’s ranged attacks do not prevent this unit from being hidden.",
            SourcePage = 37
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.PathOfTheOutcast,
            Name = "Assassins' Eye Upgrade",
            Points = 15,
            Rule = "Having trodden the Path of the Outcast for so long that they risk becoming trapped upon it, these snipers have honed their talents until they can pinpoint eye lenses, armour seals and other weaknesses to fell the toughest foes. RANGERS/SHROUD RUNNERS unit only. This unit’s ranged attacks that target a CHARACTER unit have +1 AP.",
            SourcePage = 37
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.TwilightFlickers,
            Name = "Shadowfall Masks Upgrade",
            Points = 15,
            Rule = "These nightmarish masks torment the enemy with a psychoresponsive bombardment laced through the murk generated by the wearers’ holofields. The confusing distortion leaves enemies vulnerable to the blades that suddenly spear from the terrifying gloom. TROUPE unit only. This unit has Fights First.",
            SourcePage = 39
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.TwilightFlickers,
            Name = "Prelude Performer",
            Points = 20,
            Rule = "With breathtakingly subtle yet rapid movements, this master of the opening act leads the Harlequins in a deadly dance that can catch their foes off guard. HARLEQUINS model only. This unit has Scouts 6\"",
            SourcePage = 39
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SerpentsBrood,
            Name = "Key Of Ghosts",
            Points = 20,
            Rule = "This mystic wraithbone implement allows the bearer to slip onto the stage from the Webway even before the curtain’s rise, beginning their performance in full and furious flow. HARLEQUINS model only (excluding SOLITAIRE models). Models in the bearer’s unit have the Scouts 6\" ability.",
            SourcePage = 41
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SerpentsBrood,
            Name = "Weavers’ Wail",
            Points = 20,
            Rule = "A cruel weapon more often kept locked away, this ill-omened implement is said to resonate with the Cosmic Serpent’s own dismay at the suffering of its brood during the Fall. TROUPE MASTER model only. Add 3 to the Strength and add 1 to the A�acks characteristics of the bearer’s melee weapons.",
            SourcePage = 41
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SerpentsBrood,
            Name = "Fanged Leer",
            Points = 10,
            Rule = "This cruel mask is worn when performing the Serpent’s Brood, and lends its wearer a supernatural degree of venom and spite. DEATH JESTER model only. When using the bearer’s Cruel Amusement ability, you can select two of the abilities for its shrieker cannon to gain, instead of one.",
            SourcePage = 41
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.SerpentsBrood,
            Name = "Shedskin Raiment",
            Points = 25,
            Rule = "This glittering cloak projects a grand illusion that falls away as its wearer sheds it like a discarded serpent’s hide, revealing a still-more dismaying reality beneath. SHADOWSEER model only. A�er both players have deployed their armies, select up to three HARLEQUINS units from your army and redeploy them. When doing so, you can set those units up in Strategic Reserves, regardless of how many units are already in Strategic Reserves.",
            SourcePage = 41
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.EldritchRaiders,
            Name = "Pirate Prince",
            Points = 15,
            Rule = "Yriel’s speed, both of thought and action, ensures that he remains one step ahead of his opponents at all times. PRINCE YRIEL unit only. Each time you spend a Ba�le Focus token to enable this unit to perform an Agile Manoeuvre, roll one D6: on a 3+, you gain 1 Ba�le Focus token.",
            SourcePage = 44
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.EldritchRaiders,
            Name = "Alacritous Assault",
            Points = 20,
            Rule = "The key to any raid is the shock of the opening strike. Anhrathe warriors strike at breakneck speed, power swords and boarding hooks finding gaps in enemy armour and inflicting devastating wounds. ANHRATHE unit only. Melee weapons equipped by models in this unit have the [LANCE] ability.",
            SourcePage = 44
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.EldritchRaiders,
            Name = "Exotic Munitions",
            Points = 15,
            Rule = "In their travels through the void, these Anhrathe warriors have collected a bounty of esoteric ammunition. The most lethal of these munitions are toxic or acidic enough to fell monstrous foes or to burn through armour and servo-motors with frightening rapidity. ANHRATHE unit only. Ranged weapons equipped by models in this unit have the [ANTI-MONSTER 5+] and [ANTI-VEHICLE 5+] abilities.",
            SourcePage = 44
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.EldritchRaiders,
            Name = "Adrenal Infusions",
            Points = 20,
            Rule = "Amongst the hauls taken by Aeldari Corsairs are many stimulants and elixirs, the most powerful of which enhance the already impressive grace and agility of the Aeldari physiology. ANHRATHE INFANTRY unit only. This unit can perform the Fade Back Agile Manoeuvre without spending a Ba�le Focus token to do so. It can do so even if other units have done so in the same phase, and doing so does not prevent other units from performing the same Agile Manoeuvre in the same phase.",
            SourcePage = 44
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.CorsairCoterie,
            Name = "Infamy (Aura)",
            Points = 25,
            Rule = "These infamous raiders are rightly feared, and use their reputation to their advantage with easily identifiable armour and insignia. ANHRATHE unit only. While an enemy unit is within 3\" of this unit, subtract 1 from the Objective Control characteristic of models in that unit (to a minimum of 1).",
            SourcePage = 47
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.CorsairCoterie,
            Name = "Webway Pathstone",
            Points = 25,
            Rule = "This smooth token contains esoteric knowledge of local Webway spurs. When activated by psychic impulse, it projects a mental map of these routes into the minds of the bearer, enabling them to locate hidden gates, bypass their foes, and seize the treasures they seek. ANHRATHE unit only. Models in this unit have the Deep Strike ability. In addition, once per battle, at the end of your opponent’s turn, if this unit is not within Engagement Range of one or more enemy units, it can use this ability. If it does, remove this unit from the battlefield and place it into Strategic Reserves.",
            SourcePage = 47
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.CorsairCoterie,
            Name = "Archraider",
            Points = 35,
            Rule = "A master of the lightning assault, this commander appears prescient in their ability to confound the foe. ANHRATHE CHARACTER unit only. At the start of the battle, select one CHARACTER model in this unit. That model has the following ability: Lord of Deceit (Aura): Once per turn, when your opponent targets a unit from their army within 12\" of this model with a stratagem, you can use this ability. If you do, increase the CP cost of that use of that stratagem by 1CP.",
            SourcePage = 47
        });
        enhancements.Add(new AeldariEnhancement11
        {
            Detachment = AeldariDetachment.CorsairCoterie,
            Name = "Voidstone",
            Points = 15,
            Rule = "Seized from an alien tomb, this obsidian artefact seems to absorb light itself. It offers the bearer and their unit some measure of protection against even the strongest attacks. ANHRATHE INFANTRY unit only. Models in this unit have a 5+ invulnerable save.",
            SourcePage = 47
        });
    }

    public static IReadOnlyList<AeldariStratagem11> StratagemsFor(string faction)
    {
        HashSet<AeldariDetachment> selected = new HashSet<AeldariDetachment>(AeldariDetachmentRuntime.GetSelected(faction));
        return stratagems.Where(rule => selected.Contains(rule.Detachment)).ToArray();
    }
    public static IReadOnlyList<AeldariEnhancement11> EnhancementsFor(string faction)
    {
        HashSet<AeldariDetachment> selected = new HashSet<AeldariDetachment>(AeldariDetachmentRuntime.GetSelected(faction));
        return enhancements.Where(rule => selected.Contains(rule.Detachment)).ToArray();
    }
    public static AeldariDetachmentRule11 DetachmentRule(AeldariDetachment detachment)
    {
        AeldariDetachmentRule11 value; return rules.TryGetValue(detachment, out value) ? value : null;
    }
    public static AeldariStratagem11 FindStratagem(string faction, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return StratagemsFor(faction).FirstOrDefault(rule => string.Equals(rule.Name, name, StringComparison.OrdinalIgnoreCase));
    }
    public static bool Has(string faction, AeldariDetachment detachment) { return AeldariDetachmentRuntime.Has(faction, detachment); }

    public static bool UnitHasEnhancement(SquadController unit, string name)
    {
        if (unit == null || string.IsNullOrWhiteSpace(name)) return false;
        if (FactionRuleSystem.UnitOrLeaderHasRule(unit, name)) return true;
        WarboardRosterManifest manifest = RosterTextManifestStore.Get(unit.FactionId);
        if (manifest == null || string.IsNullOrWhiteSpace(manifest.RawText)) return false;
        string raw = Normalize(manifest.RawText);
        string wanted = Normalize(name);
        if (raw.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0) return false;
        string unitName = Normalize(unit.DisplayName ?? "");
        // Do not treat an enhancement that merely appears somewhere in the
        // roster header as though every unit in the army carries it. YellowScribe
        // or the unit's local New Recruit text must identify the bearer. If the
        // export does not identify a bearer, Warboard surfaces the enhancement
        // rule as a roster rule rather than silently applying it to all units.
        if (string.IsNullOrWhiteSpace(unitName)) return false;
        int unitIndex = raw.IndexOf(unitName, StringComparison.OrdinalIgnoreCase);
        if (unitIndex < 0) return false;
        int start = Mathf.Max(0, unitIndex - 120);
        int length = Mathf.Min(raw.Length - start, 420);
        string window = raw.Substring(start, length);
        return window.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;
    }
    public static int CountEnhancement(GameController game, string faction, string name)
    {
        if (game == null) return 0;
        return game.AllSquads.Where(unit => unit != null && string.Equals(unit.FactionId, faction, StringComparison.OrdinalIgnoreCase) && UnitHasEnhancement(unit, name)).Select(unit => unit.JoinedActionController()).Distinct().Count();
    }
    public static bool FactionHasEnhancement(GameController game, string faction, string name) { return CountEnhancement(game, faction, name) > 0; }

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return WeaponRuleParser.NormalizeRuleName(value).Replace("_", " ");
    }

    public static bool NameOrKeyword(SquadController unit, string value)
    {
        if (unit == null || string.IsNullOrWhiteSpace(value)) return false;
        if (unit.HasKeyword(value)) return true;
        return !string.IsNullOrWhiteSpace(unit.DisplayName) && unit.DisplayName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static void ApplyAttackModifiers(GameController game, SquadController attacker, SquadController target, WeaponData weapon, AttackMode mode, UniversalAttackRuleState state)
    {
        if (state == null) return;
        if (attacker != null)
        {
            string faction = attacker.FactionId;
            if (Has(faction, AeldariDetachment.GuardianBattlehost) && (NameOrKeyword(attacker,"dire avenger") || NameOrKeyword(attacker,"guardian") || NameOrKeyword(attacker,"support weapon") || NameOrKeyword(attacker,"war walker")) && game != null && (game.UnitWithinAnyObjective(attacker) || (target != null && game.UnitWithinAnyObjective(target)))) state.hitRollModifier += 1;
            if (Has(faction, AeldariDetachment.SpiritConclave) && attacker.HasKeyword("wraith construct") && target != null && target.AeldariVengefulDeadTokens > 0) { state.hitRollModifier += 1; state.woundRollModifier += 1; }
            state.hitRollModifier += attacker.JoinedActionController().AeldariOffensiveHitModifier;
            state.woundRollModifier += attacker.JoinedActionController().AeldariOffensiveWoundModifier;
            if (AeldariFactionPack11Runtime.HasFlag(attacker, "guiding_presence")) state.hitRollModifier += 1;
            if (AeldariFactionPack11Runtime.HasFlag(attacker, "no_prey_too_big") && target != null && weapon != null && weapon.strength < target.Toughness) state.woundRollModifier += 1;
            if (mode == AttackMode.Melee && attacker.JoinedActionController().MadeChargeMove && (UnitHasEnhancement(attacker, "Alacritous Assault") || AeldariFactionPack11Runtime.HasFlag(attacker, "presaged_rehearsal"))) state.woundRollModifier += 1;
        }
        if (target != null)
        {
            string faction = target.FactionId;
            if (Has(faction, AeldariDetachment.TwilightFlickers) && target.HasKeyword("harlequins")) state.hitRollModifier -= 1;
            if (UnitHasEnhancement(target, "Mirage Field")) state.hitRollModifier -= 1;
            if (mode == AttackMode.Ranged && UnitHasEnhancement(target, "Shimmerstone")) state.woundRollModifier -= 1;
            state.hitRollModifier += target.JoinedActionController().AeldariDefensiveHitModifier;
            state.woundRollModifier += target.JoinedActionController().AeldariDefensiveWoundModifier;
            if (AeldariFactionPack11Runtime.HasFlag(target, "cloak_and_shadow")) state.hitRollModifier -= 1;
            if (AeldariFactionPack11Runtime.HasFlag(target, "forewarned")) { state.hitRollModifier -= 1; state.woundRollModifier -= 1; }
        }
    }

    public static int MinimumSustainedHits(SquadController attacker, WeaponData weapon, AttackMode mode)
    {
        if (attacker == null) return 0;
        int value = attacker.JoinedActionController().AeldariSustainedHits;
        if (Has(attacker.FactionId, AeldariDetachment.SerpentsBrood) && attacker.HasKeyword("harlequins") && (attacker.HasKeyword("mounted") || attacker.HasKeyword("vehicle"))) value = Mathf.Max(value,1);
        if (AeldariFactionPack11Runtime.HasFlag(attacker,"serpent_disembark_sustained")) value = Mathf.Max(value,1);
        return value;
    }
    public static bool GrantsLethalHits(SquadController attacker, AttackMode mode) { return attacker != null && attacker.JoinedActionController().AeldariLethalHits; }
    public static bool GrantsDevastatingWounds(SquadController attacker, WeaponData weapon, AttackMode mode)
    {
        if (attacker == null) return false;
        if (attacker.JoinedActionController().AeldariDevastatingWounds) return true;
        if (mode == AttackMode.Ranged && UnitHasEnhancement(attacker,"Gaze of Ynnead") && weapon != null && weapon.displayName != null && weapon.displayName.IndexOf("Eldritch Storm",StringComparison.OrdinalIgnoreCase)>=0) return true;
        return false;
    }
    public static bool GrantsIgnoresCover(SquadController attacker, AttackMode mode) { return attacker != null && attacker.JoinedActionController().AeldariIgnoresCover; }
    public static int ApModifier(SquadController attacker, SquadController target, WeaponData weapon, AttackMode mode)
    {
        if (attacker == null) return 0; int result = attacker.JoinedActionController().AeldariApModifier;
        if (mode == AttackMode.Ranged && UnitHasEnhancement(attacker,"Assassins' Eye Upgrade") && target != null && target.HasKeyword("character")) result -= 1;
        if (mode == AttackMode.Ranged && AeldariFactionPack11Runtime.HasFlag(attacker,"outcast_ambush")) result -= 1;
        return result;
    }
    public static int DamageModifier(SquadController attacker, WeaponData weapon, AttackMode mode)
    {
        if (attacker == null || weapon == null) return 0; int result = attacker.JoinedActionController().AeldariDamageModifier;
        if (mode == AttackMode.Ranged && WeaponRuleParser.Has(weapon,"psychic") && UnitHasEnhancement(attacker,"Psychic Destroyer")) result += 1;
        if (mode == AttackMode.Melee && UnitHasEnhancement(attacker,"Aspect of Murder")) result += 1;
        if (AeldariFactionPack11Runtime.HasFlag(attacker,"ruthless_killers")) result += 1;
        if (AeldariFactionPack11Runtime.HasFlag(attacker,"doom_inescapable") && weapon.displayName != null && weapon.displayName.IndexOf("Wailing Doom",StringComparison.OrdinalIgnoreCase)>=0) result += Mathf.Max(0, 8 - weapon.damage);
        return result;
    }
    public static int StrengthModifier(SquadController attacker, WeaponData weapon, AttackMode mode)
    {
        if (attacker == null || weapon == null) return 0;
        if (mode == AttackMode.Melee && UnitHasEnhancement(attacker,"Weavers’ Wail")) return 3;
        return 0;
    }
    public static int AdditionalAttacks(SquadController attacker, ModelToken model, WeaponData weapon, AttackMode mode)
    {
        if (attacker == null || weapon == null) return 0; int value=0;
        if (mode == AttackMode.Melee && UnitHasEnhancement(attacker,"Borrowed Vigour")) value += 2;
        if (mode == AttackMode.Melee && UnitHasEnhancement(attacker,"Weavers’ Wail")) value += 1;
        return value;
    }
    public static int AdditionalRapidFire(SquadController attacker, WeaponData weapon, AttackMode mode)
    {
        return attacker != null && mode == AttackMode.Ranged && AeldariFactionPack11Runtime.HasFlag(attacker,"outcast_ambush") ? 1 : 0;
    }
    public static bool GrantsPrecision(SquadController attacker, WeaponData weapon, AttackMode mode)
    {
        return attacker != null && ((mode == AttackMode.Melee && UnitHasEnhancement(attacker,"Aspect of Murder")) || AeldariFactionPack11Runtime.HasFlag(attacker,"stave_of_kurnous"));
    }
    public static int CriticalWoundThreshold(SquadController attacker, SquadController target, WeaponData weapon, int current)
    {
        int value=current; if (attacker==null || target==null || weapon==null) return value;
        bool psychic = WeaponRuleParser.Has(weapon,"psychic");
        if (psychic && UnitHasEnhancement(attacker,"Seersight Strike") && (target.HasKeyword("monster") || target.HasKeyword("vehicle"))) value = Mathf.Min(value,2);
        if (UnitHasEnhancement(attacker,"Exotic Munitions") && (target.HasKeyword("monster") || target.HasKeyword("vehicle"))) value = Mathf.Min(value,5);
        return value;
    }
    public static bool IsCriticalHit(SquadController attacker, SquadController target, WeaponData weapon, int roll, bool successful)
    {
        if (!successful) return false;
        if (roll >= 6) return true;
        if (attacker != null && AeldariFactionPack11Runtime.HasFlag(attacker,"blitzing_firepower_crit5") && roll >= 5) return true;
        if (attacker != null && UnitHasEnhancement(attacker,"Murder’s Jest") && target != null && target.IsAtOrBelowHalfStrength()) return true;
        return false;
    }
    public static int InvulnerableOverride(SquadController unit)
    {
        if (unit == null) return 0; int value=unit.JoinedActionController().AeldariInvulnerableOverride;
        if (UnitHasEnhancement(unit,"Voidstone")) value = value>0 ? Mathf.Min(value,5) : 5;
        if (AeldariFactionPack11Runtime.HasFlag(unit,"spirit_token")) value = value>0 ? Mathf.Min(value,4) : 4;
        return value;
    }
    public static float RangedRangeModifier(SquadController unit, WeaponData weapon)
    {
        if (unit == null || weapon == null) return 0f; float value=0f;
        if (WeaponRuleParser.Has(weapon,"psychic") && UnitHasEnhancement(unit,"Stone of Eldritch Fury")) value += 12f;
        if (AeldariFactionPack11Runtime.HasFlag(unit,"doom_inescapable") && weapon.displayName != null && weapon.displayName.IndexOf("Wailing Doom",StringComparison.OrdinalIgnoreCase)>=0) value += Mathf.Max(0f,18f-weapon.range);
        return value;
    }
    public static bool CanMoveThroughEnemyModelsWhenCharging(SquadController unit)
    {
        if (unit == null || !unit.HasKeyword("harlequins")) return false;
        return Has(unit.FactionId,AeldariDetachment.GhostsOfTheWebway) || Has(unit.FactionId,AeldariDetachment.FatefulPerformance);
    }
    public static bool CanRerollAdvance(SquadController unit)
    {
        return unit != null && Has(unit.FactionId,AeldariDetachment.EldritchRaiders) && (unit.HasKeyword("anhrathe") || NameOrKeyword(unit,"rangers") || NameOrKeyword(unit,"shroud runners"));
    }
    public static bool CanChargeAfterAdvance(SquadController unit)
    {
        return unit != null && (unit.AeldariCanChargeAfterAdvance || Has(unit.FactionId,AeldariDetachment.EldritchRaiders));
    }
    public static bool CanShootAfterFallBack(SquadController unit) { return unit != null && (unit.AeldariCanShootAfterFallBack || unit.AeldariVectoredEnginesActive); }
    public static bool CanChargeAfterFallBack(SquadController unit) { return unit != null && unit.AeldariCanChargeAfterFallBack; }
    public static bool VehicleRangedHasAssault(SquadController unit) { return unit != null && unit.HasKeyword("vehicle") && Has(unit.FactionId,AeldariDetachment.ArmouredWarhost); }
    public static bool HasRange18Protection(SquadController unit) { return unit != null && (unit.AeldariRange18Protection || AeldariFactionPack11Runtime.HasFlag(unit,"cloak_and_shadow")); }
    public static int ModifyObjectiveControl(SquadController unit, ModelToken model, int current)
    {
        if (unit == null || model == null) return current; int value=current;
        if (UnitHasEnhancement(unit,"Craftworld’s Champion")) value=Mathf.Max(value,5);
        if (UnitHasEnhancement(unit,"Strategic Savant") && NameOrKeyword(unit,"aspect warriors")) value += 1;
        if (AeldariFactionPack11Runtime.HasFlag(unit,"light_of_clarity")) value += unit.HasKeyword("monster") ? 3 : 1;
        if (AeldariFactionPack11Runtime.HasFlag(unit,"raiders_spoils")) value += 1;
        value = Mathf.Max(1, value - AeldariFactionPack11Runtime.InfamyPenalty(unit));
        return value;
    }
    public static bool GrantsCoreAbility(SquadController unit, string ruleName)
    {
        if (unit == null || string.IsNullOrWhiteSpace(ruleName)) return false;
        string wanted = Normalize(ruleName);
        if (wanted.Contains("infiltrators"))
            return UnitHasEnhancement(unit,"Mistweave") || AeldariFactionPack11Runtime.HasFlag(unit,"ethereal_pathway");
        if (wanted.Contains("fights first") || wanted.Contains("fights_first"))
            return UnitHasEnhancement(unit,"Shadowfall Masks Upgrade");
        if (wanted.Contains("deep strike") || wanted.Contains("deep_strike"))
            return UnitHasEnhancement(unit,"Webway Pathstone");
        if (wanted.Contains("stealth"))
            return UnitHasPermanentStealth(unit) || AeldariFactionPack11Runtime.HasFlag(unit,"cloak_and_shadow");
        if (wanted.Contains("scouts"))
            return UnitHasEnhancement(unit,"Firstdrawn Blade") ||
                   UnitHasEnhancement(unit,"Prelude Performer") ||
                   UnitHasEnhancement(unit,"Key Of Ghosts");
        return false;
    }

    public static bool UnitHasPermanentStealth(SquadController unit) { return unit != null && Has(unit.FactionId,AeldariDetachment.TwilightFlickers) && unit.HasKeyword("harlequins"); }

    public static bool AutomaticHitSucceeds(
        int roll,
        int skill,
        UniversalAttackRuleState state)
    {
        if (roll <= 1) return false;
        if (state != null &&
            state.minimumUnmodifiedHit > 0 &&
            roll < state.minimumUnmodifiedHit)
        {
            return false;
        }

        int modifier =
            state != null
            ? state.hitRollModifier
            : 0;

        return roll >= 6 ||
            roll + modifier >= skill;
    }

    public static bool AutomaticWoundSucceeds(
        int roll,
        int target,
        int criticalThreshold,
        int modifier)
    {
        if (roll <= 1) return false;
        if (roll >= criticalThreshold) return true;
        if (roll >= 6) return true;
        return roll + modifier >= target;
    }

    public static bool AutomaticRerollHit(
        SquadController attacker,
        int roll,
        int skill,
        UniversalAttackRuleState state)
    {
        if (attacker == null) return false;
        SquadController unit = attacker.JoinedActionController();
        bool success = AutomaticHitSucceeds(roll, skill, state);
        if (unit.AeldariRerollAllHits) return !success;
        if (unit.AeldariRerollHitOnes) return roll == 1;
        return false;
    }

    public static bool AutomaticRerollWound(
        SquadController attacker,
        int roll,
        bool success,
        AttackMode mode)
    {
        if (attacker == null) return false;
        SquadController unit = attacker.JoinedActionController();
        if (mode == AttackMode.Melee && UnitHasEnhancement(attacker, "Morbid Might"))
            return !success;
        if (unit.AeldariRerollAllWounds) return !success;
        if (unit.AeldariRerollWoundOnes) return roll == 1;
        return false;
    }
}
