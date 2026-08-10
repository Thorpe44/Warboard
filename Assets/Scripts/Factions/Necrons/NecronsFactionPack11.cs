// WARBOARD_V44_FULL_NECRONS_FACTION_RULES
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class NecronStratagem11
{
    public NecronDetachment Detachment;
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

public sealed class NecronEnhancement11
{
    public NecronDetachment Detachment;
    public string Name = "";
    public int Points;
    public string Rule = "";
    public int SourcePage;
}

public sealed class NecronDetachmentRule11
{
    public NecronDetachment Detachment;
    public string Name = "";
    public string Rule = "";
    public int SourcePage;
}

/// <summary>
/// Necrons Faction Pack, Edition 11, v1.1 July 2026.
/// Standard battle faction rules only. Crusade and Boarding Actions are not
/// part of Warboard's normal matched-play faction layer.
/// </summary>
public static class NecronsFactionPack11
{
    public const string Version =
        "Necrons Faction Pack 11e v1.1 July 2026";

    private static readonly List<NecronStratagem11>
        stratagems =
            new List<NecronStratagem11>();

    private static readonly List<NecronEnhancement11>
        enhancements =
            new List<NecronEnhancement11>();

    private static readonly Dictionary<
        NecronDetachment,
        NecronDetachmentRule11
    > rules =
        new Dictionary<
            NecronDetachment,
            NecronDetachmentRule11>();

    static NecronsFactionPack11()
    {
        rules[NecronDetachment.AwakenedDynasty] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "Command Protocols",
                Rule = "While a NECRONS CHARACTER model is leading this unit, each time a model in this unit makes an attack, add 1 to the Hit roll.",
                SourcePage = 4
            };
        rules[NecronDetachment.AnnihilationLegion] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "Annihilation Protocol",
                Rule = "Each time a DESTROYER CULT or FLAYED ONES unit from your army declares a charge, you can re-roll the Charge roll. If one or more targets of that charge are Below Half-strength, add 1 to the Charge roll as well. Each time a DESTROYER CULT unit from your army makes a ranged attack that targets the closest eligible target, add 1 to the Armour Penetration characteristic of that attack.",
                SourcePage = 7
            };
        rules[NecronDetachment.CanoptekCourt] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "Power Matrix",
                Rule = "Your deployment zone is always within your army’s Power Matrix. At the start of any phase, if you control at least half of the objective markers within No Man’s Land, until the end of that phase No Man’s Land is within your army’s Power Matrix. At the start of any phase, if you control at least half of the objective markers within your opponent’s deployment zone, until the end of that phase your opponent’s deployment zone is within your army’s Power Matrix. Each time a model in a CRYPTEK or CANOPTEK unit makes an attack, re-roll a Hit roll of 1. If such a unit is wholly within the Power Matrix, you can re-roll the Hit roll instead.",
                SourcePage = 10
            };
        rules[NecronDetachment.ObeisancePhalanx] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "Worthy Foes",
                Rule = "In your Command phase, select one enemy unit. Until the start of your next Command phase, each time a NOBLE, LYCHGUARD or TRIARCH unit from your army makes an attack that targets that unit, add 1 to the Wound roll.",
                SourcePage = 12
            };
        rules[NecronDetachment.HypercryptLegion] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "Hyperphasing",
                Rule = "At the end of your opponent’s turn, select a number of unengaged NECRONS units and place them into Strategic Reserves. The maximum is 1 in Incursion, 2 in Strike Force and 3 in Onslaught.",
                SourcePage = 16
            };
        rules[NecronDetachment.StarshatterArsenal] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "Relentless Onslaught",
                Rule = "Each time a NECRONS model (excluding MONSTER models) makes an attack that targets a unit within range of one or more objective markers, add 1 to the Hit roll. Ranged weapons equipped by NECRONS VEHICLE and NECRONS MOUNTED models (excluding TITANIC models) have the [ASSAULT] ability.",
                SourcePage = 19
            };
        rules[NecronDetachment.CryptekConclave] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "Technosorcerous Augmentations",
                Rule = "Ranged weapons equipped by CRYPTEK models have [ASSAULT]. In your Shooting phase, each time a CRYPTEK unit is selected to shoot, select one of [ANTI-INFANTRY 3+], [ANTI-MOUNTED 4+], [ASSAULT], [HEAVY] or [IGNORES COVER] for that unit’s ranged weapons until the end of the phase.",
                SourcePage = 22
            };
        rules[NecronDetachment.CursedLegion] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "Cold Fervour",
                Rule = "Add 2 to the Strength characteristic of weapons equipped by DESTROYER CULT models. The first time each turn a DESTROYER CULT unit’s attacks destroy a unit or cause it to become Below Half-strength, after that unit has finished its attacks, until the end of the turn add 2 to the Strength characteristic of weapons equipped by friendly NECRONS models (excluding DESTROYER CULT, MONSTER and TITANIC models).",
                SourcePage = 25
            };
        rules[NecronDetachment.PantheonOfWoe] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "Cosmic Distortion",
                Rule = "NECRONS MONSTER units gain Distortion Fields (Aura): while an enemy unit is within 6\", it is unravelling; each time an attack targets an unravelling unit, improve that attack’s Armour Penetration by 1. At the start of each phase, each NECRONS MONSTER unit can suffer 3 mortal wounds to increase its aura to 9\" until the end of the phase. Each NECRONS MONSTER must take its relevant Necrodermal Binding and pay its listed points cost.",
                SourcePage = 28
            };
        rules[NecronDetachment.HandOfTheDynasty] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.HandOfTheDynasty,
                Name = "Hypermotility Protocols",
                Rule = "Friendly IMMORTALS and NECRON WARRIORS units’ ranged attacks have [ASSAULT]. When one of those units is selected to make an Advance move, that move does not prevent that unit from being eligible to start an Action. This Detachment has the DYNASTY tag.",
                SourcePage = 31
            };
        rules[NecronDetachment.SkyshroudSpearhead] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.SkyshroudSpearhead,
                Name = "Transdimensional Deployment",
                Rule = "Friendly TOMB BLADES units have Deep Strike. When a friendly TOMB BLADES unit is selected to shoot, if it made an ingress move this turn, its ranged attacks have +1 to Hit rolls.",
                SourcePage = 33
            };
        rules[NecronDetachment.ThePhaeronsArmoury] =
            new NecronDetachmentRule11
            {
                Detachment = NecronDetachment.ThePhaeronsArmoury,
                Name = "Empowered Engines",
                Rule = "Friendly NECRONS TITANIC FLY units have +6\" Move. This Detachment has the HYPERCRYPT tag.",
                SourcePage = 35
            };
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "PROTOCOL OF THE ETERNAL REVENANT",
                Cost = 1,
                Category = "AWAKENED DYNASTY – EPIC DEED STRATAGEM",
                When = "Any phase.",
                Target = "One NECRONS INFANTRY CHARACTER model from your army that was just destroyed. You can use this Stratagem on that model even though it was just destroyed.",
                Effect = "At the end of the phase, set up the destroyed model on the battlefield, unengaged and as close as possible to where it was destroyed. That model is not part of an attached unit and its unit has a starting strength of 1. That model has half of its starting number of wounds remaining.",
                Restrictions = "Each model can only be targeted with this Stratagem once per battle.",
                SourcePage = 5
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "PROTOCOL OF THE UNDYING LEGIONS",
                Cost = 1,
                Category = "AWAKENED DYNASTY – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has resolved its attacks.",
                Target = "One NECRONS unit from your army that had one or more of its models destroyed as a result of the attacking unit’s attacks.",
                Effect = "Your unit activates its Reanimation Protocols and reanimates D3 wounds (or D3+1 wounds if a NECRONS CHARACTER is leading your unit].",
                Restrictions = "",
                SourcePage = 5
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "PROTOCOL OF THE HUNGRY VOID",
                Cost = 1,
                Category = "AWAKENED DYNASTY – BATTLE TACTIC STRATAGEM",
                When = "Fight phase.",
                Target = "One NECRONS unit from your army that has not been selected to fight this phase.",
                Effect = "Until the end of the phase, add 1 to the Strength characteristic of melee weapons equipped by models in your unit. In addition, If a NECRONS CHARACTER is leading your unit, until the end of the phase, improve the Armour Penetration characteristic of melee weapons equipped by models in your unit by 1. (this is not cumulative with any other modifiers that improve Armour Penetration].",
                Restrictions = "",
                SourcePage = 5
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "PROTOCOL OF THE SUDDEN STORM",
                Cost = 1,
                Category = "AWAKENED DYNASTY – STRATEGIC PLOY STRATAGEM",
                When = "Your Movement phase.",
                Target = "One NECRONS unit from your army.",
                Effect = "Until the end of the turn, ranged weapons equipped by models in your unit have the [ASSAULT] ability. In addition, if a NECRONS CHARACTER is leading your unit, until the end of the phase, you can re-roll Advance rolls made for your unit.",
                Restrictions = "",
                SourcePage = 6
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "PROTOCOL OF THE CONQUERING TYRANT",
                Cost = 1,
                Category = "AWAKENED DYNASTY – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase.",
                Target = "One NECRONS unit from your army that has not been selected to shoot this phase.",
                Effect = "Until the end of the phase, each time a model in your unit makes an attack that targets a unit within half range, re-roll a Hit roll of 1. If a NECRONS CHARACTER is leading your unit, until the end of the phase, you can re-roll the Hit roll for that attack instead.",
                Restrictions = "",
                SourcePage = 6
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "PROTOCOL OF THE VENGEFUL STARS",
                Cost = 2,
                Category = "AWAKENED DYNASTY – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Shooting phase, just after an enemy unit destroys a NECRONS unit from your army.",
                Target = "One NECRONS CHARACTER unit from your army that was within 6\" of that NECRONS unit when it was destroyed.",
                Effect = "A�er the attacking unit has resolved its attacks, your unit can shoot as if it were your Shooting phase, but it must target only that enemy unit when doing so, and can only do so if that enemy unit is an eligible target. ANNIHILATION LEGION 2DP",
                Restrictions = "",
                SourcePage = 6
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "MASKS OF DEATH",
                Cost = 1,
                Category = "ANNIHILATION LEGION – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
                Target = "One DESTROYER CULT or FLAYED ONES unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, each time an attack targets your unit, subtract 1 from the Hit roll.",
                Restrictions = "",
                SourcePage = 8
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "THE SPOOR OF FRAILTY",
                Cost = 1,
                Category = "ANNIHILATION LEGION – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase or the Fight phase.",
                Target = "One DESTROYER CULT or FLAYED ONES unit from your army that has not been selected to shoot or fight this phase.",
                Effect = "Until the end of the phase, each time a model from your unit makes an attack that targets a unit below Starting Strength, add 1 to the Hit roll. If the target is Below Half-strength, add 1 to the Wound roll as well.",
                Restrictions = "",
                SourcePage = 8
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "MURDEROUS REANIMATION",
                Cost = 1,
                Category = "ANNIHILATION LEGION – BATTLE TACTIC STRATAGEM",
                When = "Fight phase.",
                Target = "One DESTROYER CULT or FLAYED ONES unit from your army that has just destroyed an enemy unit, or just caused an enemy unit that was not Below Half-strength to become Below Half-strength.",
                Effect = "Your unit’s Reanimation Protocols activate.",
                Restrictions = "",
                SourcePage = 8
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "PITILESS HUNTERS",
                Cost = 1,
                Category = "ANNIHILATION LEGION – BATTLE TACTIC STRATAGEM",
                When = "Fight phase.",
                Target = "One DESTROYER CULT or FLAYED ONES unit from your army that has not been selected to fight this phase.",
                Effect = "Until the end of the phase, each time a model in your unit makes a Pile-in or Consolidation move, it can move up to 6\" instead of up to 3\".",
                Restrictions = "",
                SourcePage = 9
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "BLOOD-FUELLED CRUELTY",
                Cost = 1,
                Category = "ANNIHILATION LEGION – BATTLE TACTIC STRATAGEM",
                When = "Your opponent’s Movement phase, just after an enemy unit ends a Fall Back move.",
                Target = "One DESTROYER CULT or FLAYED ONES unit from your army that started the phase within Engagement Range of that enemy unit.",
                Effect = "Roll one D6: on a 2-5, that enemy unit suﬀers D3 mortal wounds; on a 6, that enemy unit suﬀers 3 mortal wounds. Your unit can then make a Normal move, but must end that move as close as possible to that enemy unit.",
                Restrictions = "",
                SourcePage = 9
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "INSANITY’S IRE",
                Cost = 1,
                Category = "ANNIHILATION LEGION – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Shooting phase, when an enemy unit that targeted a friendly unengaged DESTROYER CULT/FLAYED ONES unit this phase has shot.",
                Target = "That DESTROYER CULT/FLAYED ONES unit.",
                Effect = "Your unit can make a surge move of up to D6\". CANOPTEK COURT 3DP",
                Restrictions = "",
                SourcePage = 9
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "CURSE OF THE CRYPTEK",
                Cost = 1,
                Category = "CANOPTEK COURT – BATTLE TACTIC STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has shot or fought.",
                Target = "One CRYPTEK model from your army that was destroyed by one of the attacking unit’s attacks. You can use this Stratagem on that model even though it was just destroyed.",
                Effect = "Until the end of the battle, each time a friendly CANOPTEK model makes an attack that targets the attacking unit, add 1 to the Hit roll and add 1 to the Wound roll.",
                Restrictions = "",
                SourcePage = 11
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "CYNOSURE OF ERADICATION",
                Cost = 2,
                Category = "CANOPTEK COURT – BATTLE TACTIC STRATAGEM",
                When = "The start of your Shooting phase or the start of the Fight phase.",
                Target = "One CRYPTEK or CANOPTEK unit from your army that is wholly within your army’s Power Matrix.",
                Effect = "Until the end of the phase, weapons equipped by CRYPTEK or CANOPTEK models in your unit have the [DEVASTATING WOUNDS] ability.",
                Restrictions = "",
                SourcePage = 11
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "SOLAR PULSE",
                Cost = 1,
                Category = "CANOPTEK COURT – STRATEGIC PLOY STRATAGEM",
                When = "Start of your Shooting phase.",
                Target = "One CRYPTEK model from your army.",
                Effect = "Select one objective marker within 18\" of your CRYPTEK model. Until the end of the phase, weapons equipped by friendly NECRONS models have the [IGNORES COVER] ability while targeting units within range of that objective marker.",
                Restrictions = "",
                SourcePage = 11
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "REACTIVE SUBROUTINES",
                Cost = 1,
                Category = "CANOPTEK COURT – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Movement phase, just after an enemy unit ends a Normal, Advance or Fall Back move.",
                Target = "One CANOPTEK unit from your army that is within 8\" of that enemy unit.",
                Effect = "Your unit can make a Normal move of up to 6\".",
                Restrictions = "",
                SourcePage = 11
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "COUNTERTEMPORAL SHIFT",
                Cost = 1,
                Category = "CANOPTEK COURT – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Shooting phase, just after an enemy unit has selected its targets.",
                Target = "One CANOPTEK unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, your unit can only be selected as the target of a ranged attack if the attacking model is within 18\".",
                Restrictions = "",
                SourcePage = 12
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "SUBOPTIMAL FACADE",
                Cost = 1,
                Category = "CANOPTEK COURT – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Charge phase, just after an enemy unit has declared a charge.",
                Target = "One CANOPTEK unit from your army that was selected as a target of that charge and is wholly within your army’s Power Matrix.",
                Effect = "Your unit’s Reanimation Protocols activate. OBEISANCE PHALANX 2DP",
                Restrictions = "",
                SourcePage = 12
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "YOUR TIME IS NIGH",
                Cost = 1,
                Category = "OBEISANCE PHALANX – EPIC DEED STRATAGEM",
                When = "Any phase, just after your opponent’s WARLORD is destroyed.",
                Target = "Your NECRONS WARLORD.",
                Effect = "Until the end of the battle, each time an enemy unit takes a Ba�le-shock or Leadership test, subtract 1 from the result.",
                Restrictions = "",
                SourcePage = 14
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "ENSLAVED ARTIFICE",
                Cost = 1,
                Category = "OBEISANCE PHALANX – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase or the Fight phase.",
                Target = "One NECRONS unit from your army (excluding TITANIC units) that has not been selected to shoot or fight this phase.",
                Effect = "Until the end of the phase, each time a model in your unit makes an attack, an unmodified Hit roll of 5+ scores a Critical Hit.",
                Restrictions = "",
                SourcePage = 14
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "NANOASSEMBLY PROTOCOLS",
                Cost = 1,
                Category = "OBEISANCE PHALANX – BATTLE TACTIC STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
                Target = "One NECRONS VEHICLE unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, each time an attack is allocated to a model in your unit, subtract 1 from the Damage characteristic of that attack.",
                Restrictions = "",
                SourcePage = 14
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "SENTINELS OF ETERNITY",
                Cost = 1,
                Category = "OBEISANCE PHALANX – EPIC DEED STRATAGEM",
                When = "Fight phase, just after an enemy unit has selected its targets.",
                Target = "One LYCHGUARD or TRIARCH PRAETORIANS unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, each time a model in your unit is destroyed, if that model has not fought this phase, roll one D6: on a 4+, do not remove it from play. The destroyed model can fight after the attacking model’s unit has finished making attacks, and is then removed from play.",
                Restrictions = "",
                SourcePage = 15
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "SUFFER NO RIVAL",
                Cost = 1,
                Category = "OBEISANCE PHALANX – BATTLE TACTIC STRATAGEM",
                When = "Fight phase.",
                Target = "One LYCHGUARD or TRIARCH unit from your army that has not been selected to fight this phase.",
                Effect = "Until the end of the phase, melee weapons equipped by models in your unit have the [PRECISION] ability.",
                Restrictions = "",
                SourcePage = 15
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "TERRITORIAL OBSESSION",
                Cost = 1,
                Category = "OBEISANCE PHALANX – STRATEGIC PLOY STRATAGEM",
                When = "Your Command phase.",
                Target = "One LYCHGUARD or TRIARCH unit from your army.",
                Effect = "Until the start of your next Command phase, add 1 to the Objective Control characteristic of models in your unit. If your unit has the VEHICLE keyword, add 3 to the Objective Control characteristic instead. UNIQUE: HYPERCRYPT HYPERCRYPT LEGION 2DP",
                Restrictions = "",
                SourcePage = 15
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "HYPERPHASIC RECALL",
                Cost = 2,
                Category = "HYPERCRYPT LEGION – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has shot or fought.",
                Target = "One NECRONS INFANTRY unit from your army that had one or more of its models destroyed as a result of the attacking unit’s attacks and one friendly MONOLITH model.",
                Effect = "Remove your INFANTRY unit from the battlefield and then set it back up anywhere on the battlefield that is wholly within 6\" of your MONOLITH model and not within Engagement Range of one or more enemy units.",
                Restrictions = "",
                SourcePage = 17
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "QUANTUM DEFLECTION",
                Cost = 1,
                Category = "HYPERCRYPT LEGION – WARGEAR STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
                Target = "One NECRONS VEHICLE unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, models in your unit have a 4+ invulnerable save.",
                Restrictions = "",
                SourcePage = 17
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "REANIMATION CRYPTS",
                Cost = 1,
                Category = "HYPERCRYPT LEGION – STRATEGIC PLOY STRATAGEM",
                When = "Your Command phase.",
                Target = "Your NECRONS WARLORD.",
                Effect = "For each of your NECRONS units in Reserves, that Reserves unit’s Reanimation Protocols activate.",
                Restrictions = "",
                SourcePage = 17
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "COSMIC PRECISION",
                Cost = 1,
                Category = "HYPERCRYPT LEGION – STRATEGIC PLOY STRATAGEM",
                When = "Your Movement phase.",
                Target = "One NECRONS unit from your army (excluding MONSTER units) that is arriving using the Deep Strike or Hyperphasing abilities this phase.",
                Effect = "Your unit can be set up anywhere on the battlefield that is more than 6\" horizontally away from all enemy models.",
                Restrictions = "A unit targeted with this Stratagem is not eligible to declare a charge in the same turn.",
                SourcePage = 18
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "DIMENSIONAL CORRIDOR",
                Cost = 2,
                Category = "HYPERCRYPT LEGION – STRATEGIC PLOY STRATAGEM",
                When = "Your Charge phase.",
                Target = "One NECRONS unit from your army that was set up on the battlefield this turn using the Eternity Gate ability of a MONOLITH model that started the turn on the battlefield.",
                Effect = "Your unit is eligible to charge this phase.",
                Restrictions = "",
                SourcePage = 18
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "ENTROPIC DAMPING",
                Cost = 1,
                Category = "HYPERCRYPT LEGION – WARGEAR STRATAGEM",
                When = "Your opponent’s Shooting phase, just after an enemy unit has selected its targets.",
                Target = "One TITANIC model from your army that was selected as the target of one or more of the attacking unit’s attacks and is within 18\" of the attacking unit.",
                Effect = "Until the end of the phase, weapons equipped by models in the attacking unit have the [HAZARDOUS] ability.",
                Restrictions = "",
                SourcePage = 18
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "MERCILESS RECLAMATION",
                Cost = 2,
                Category = "STARSHATTER ARSENAL – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase or the Fight phase.",
                Target = "One NECRONS unit (excluding MONSTER and TITANIC units) from your army that has not been selected to shoot or fight this phase.",
                Effect = "Until the end of the phase, each time a model in your unit makes an attack, if the target of that attack is within range of one or more objective markers, add 1 to the Wound roll.",
                Restrictions = "",
                SourcePage = 20
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "UNYIELDING FORMS",
                Cost = 2,
                Category = "STARSHATTER ARSENAL – BATTLE TACTIC STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
                Target = "One NECRONS VEHICLE or NECRONS MOUNTED unit (excluding TITANIC units) from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, each time an attack targets a model in your unit, if the Strength characteristic of that attack is greater than the Toughness characteristic of that unit, subtract 1 from the Wound roll.",
                Restrictions = "",
                SourcePage = 20
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "CHRONOSHIFT",
                Cost = 1,
                Category = "STARSHATTER ARSENAL – STRATEGIC PLOY STRATAGEM",
                When = "Your Movement phase.",
                Target = "One NECRONS VEHICLE or NECRONS MOUNTED unit (excluding TITANIC units) from your army that has not been selected to move this phase.",
                Effect = "Until the end of the phase, if your unit Advances, do not make an Advance roll for it. Instead, until the end of the phase, add 6\" to the Move characteristic of models in your unit.",
                Restrictions = "",
                SourcePage = 20
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "DIMENSIONAL TUNNEL",
                Cost = 1,
                Category = "STARSHATTER ARSENAL – STRATEGIC PLOY STRATAGEM",
                When = "Your Movement phase.",
                Target = "One NECRONS VEHICLE or NECRONS MOUNTED unit (excluding TITANIC units) from your army.",
                Effect = "Until the end of the phase, models in your unit can move horizontally through models and terrain features.",
                Restrictions = "",
                SourcePage = 21
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "ENDLESS SERVITUDE",
                Cost = 1,
                Category = "STARSHATTER ARSENAL – STRATEGIC PLOY STRATAGEM",
                When = "End of your Fight phase.",
                Target = "One NECRONS unit (excluding MONSTER and TITANIC units) from your army that is within range of one or more objective markers you control.",
                Effect = "Your unit’s Reanimation Protocols activate.",
                Restrictions = "",
                SourcePage = 21
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "REACTIVE REPOSITION",
                Cost = 1,
                Category = "STARSHATTER ARSENAL – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Shooting phase, just after an enemy unit has shot.",
                Target = "One NECRONS unit from your army (excluding MONSTER and TITANIC units) that was the target of one or more of the attacking unit’s attacks.",
                Effect = "Your unit can make a Normal move of up to D6\".",
                Restrictions = "",
                SourcePage = 21
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "MOLECULAR TARGETING",
                Cost = 1,
                Category = "CRYPTEK CONCLAVE – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase or the Fight phase.",
                Target = "One NECRONS unit from your army that has not been selected to shoot or fight this phase.",
                Effect = "Until the end of the phase, each time a model in your unit makes an attack, you can ignore any or all modifiers to the following: that attack’s Ballistic Skill or Weapon Skill characteristic; the Hit roll. If your unit has the CRYPTEK keyword, you can also ignore any or all modifiers to the Wound roll.",
                Restrictions = "",
                SourcePage = 23
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "MICROSCARAB SWARM",
                Cost = 1,
                Category = "CRYPTEK CONCLAVE – WARGEAR STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
                Target = "One CRYPTEK INFANTRY unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "If your unit has the NECRON WARRIORS keyword, until the end of the phase, models in your unit have a 5+ invulnerable save. If your unit has the IMMORTALS keyword, until the end of the phase, models in your unit have a 4+ invulnerable save.",
                Restrictions = "",
                SourcePage = 23
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "ANIMUS CURSE",
                Cost = 1,
                Category = "CRYPTEK CONCLAVE – WARGEAR STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has shot or fought.",
                Target = "One CRYPTEK model from your army that was destroyed by one of the attacking unit’s attacks. You can use this Stratagem on that model even though it was just destroyed.",
                Effect = "Until the end of the battle, each time a friendly NECRONS model makes an attack that targets the attacking unit, you can re-roll the Hit roll.",
                Restrictions = "",
                SourcePage = 23
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "SYNERGISTIC EMPOWERMENT",
                Cost = 1,
                Category = "CRYPTEK CONCLAVE – STRATEGIC PLOY STRATAGEM",
                When = "Start of your Shooting phase.",
                Target = "One CRYPTEK unit from your army.",
                Effect = "Select one friendly NECRONS model (excluding MONSTERS and VEHICLES) within 12\" of a CRYPTEK model in your unit. Until the end of the phase, that friendly NECRONS model has the CRYPTEK keyword.",
                Restrictions = "",
                SourcePage = 24
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "UNTAPPED POWER",
                Cost = 1,
                Category = "CRYPTEK CONCLAVE – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase.",
                Target = "One CRYPTEK unit from your army that has not been selected to shoot this phase.",
                Effect = "Until the end of the phase, each time your unit is selected to shoot, when selecting an ability for the Technosorcerous Augmentations Detachment Rule, you can select one additional ability from those available.",
                Restrictions = "",
                SourcePage = 24
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "POTENTIALITY SYPHON",
                Cost = 1,
                Category = "CRYPTEK CONCLAVE – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Command phase.",
                Target = "One NECRONS unit from your army within range of one or more objective markers.",
                Effect = "Your unit’s Reanimation Protocols activate. If it is a CRYPTEK unit, it reanimates an additional 1 wound.",
                Restrictions = "",
                SourcePage = 24
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "METHODICAL MURDER",
                Cost = 1,
                Category = "CURSED LEGION – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase or the Fight phase.",
                Target = "One NECRONS unit (excluding MONSTERS and VEHICLES) from your army that has not been selected to shoot or fight this phase.",
                Effect = "Until the end of the phase, weapons equipped by models in your unit have the [SUSTAINED HITS 1] ability.",
                Restrictions = "",
                SourcePage = 26
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "IMAGE OF DEATH",
                Cost = 1,
                Category = "CURSED LEGION – BATTLE TACTIC STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, just after an enemy unit has selected its targets.",
                Target = "One DESTROYER CULT unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, each time an attack targets your unit, subtract 1 from the Hit roll.",
                Restrictions = "",
                SourcePage = 26
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "MORTIS PROTOCOLS",
                Cost = 1,
                Category = "CURSED LEGION – STRATEGIC PLOY STRATAGEM",
                When = "Your Shooting phase or the Fight phase, just after the first time a DESTROYER CULT unit from your army destroys an enemy unit this turn.",
                Target = "One friendly NECRONS unit (excluding MONSTERS and VEHICLES) within 9\" of that DESTROYER CULT unit.",
                Effect = "The friendly unit’s Reanimation Protocols activate.",
                Restrictions = "",
                SourcePage = 26
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "DRIVEN TO BUTCHERY",
                Cost = 1,
                Category = "CURSED LEGION – STRATEGIC PLOY STRATAGEM",
                When = "Your Shooting phase or your Charge phase.",
                Target = "One DESTROYER CULT unit from your army.",
                Effect = "Until the end of the turn, your unit is eligible to shoot and declare a charge in a turn in which it Advanced.",
                Restrictions = "You can only use this Stratagem once per turn.",
                SourcePage = 27
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "SPREADING MADNESS",
                Cost = 1,
                Category = "CURSED LEGION – BATTLE TACTIC STRATAGEM",
                When = "Your Charge phase.",
                Target = "One NECRONS unit (excluding MONSTERS and VEHICLES) from your army that has not declared a charge this phase.",
                Effect = "Until the end of the phase, each time your unit declares a charge, if one or more targets of that charge are within Engagement Range of one or more friendly units, add 2 to the Charge roll.",
                Restrictions = "",
                SourcePage = 27
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "UNNATURAL AGGRESSION",
                Cost = 2,
                Category = "CURSED LEGION – STRATEGIC PLOY STRATAGEM",
                When = "End of your opponent’s Charge phase.",
                Target = "One NECRONS unit (excluding MONSTERS and VEHICLES) from your army that is within 6\" of one or more enemy units and would be eligible to declare a charge against one or more of those enemy units if it were your Charge phase.",
                Effect = "Your unit now declares a charge that only targets one or more of those enemy units, and you resolve that charge. Note that even if this charge is successful, your unit does not receive any Charge bonus this turn.",
                Restrictions = "",
                SourcePage = 27
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "DISHARMONISATION CASCADE",
                Cost = 1,
                Category = "PANTHEON OF WOE – EPIC DEED STRATAGEM",
                When = "Any phase, just after a NECRONS MONSTER model from your army is destroyed, before making its Deadly Demise roll.",
                Target = "That NECRONS MONSTER model. You can use this Stratagem on that model even though it was just destroyed.",
                Effect = "Until the end of the phase, your model’s Deadly Demise ability inflicts mortal wounds on a D6 roll of 3+ instead of on a 6.",
                Restrictions = "",
                SourcePage = 29
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "MOLECULAR EROSION",
                Cost = 1,
                Category = "PANTHEON OF WOE – STRATEGIC PLOY STRATAGEM",
                When = "Command phase.",
                Target = "One NECRONS MONSTER unit from your army.",
                Effect = "Select one unravelling enemy unit visible to your unit. That enemy unit must take a Ba�le-shock test. When doing so, subtract 1 from the result. If that test is failed, that enemy unit suﬀers D3+1 mortal wounds.",
                Restrictions = "You can only use this Stratagem once per battle round.",
                SourcePage = 29
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "MASS TRANSMOGRIFICATION",
                Cost = 1,
                Category = "PANTHEON OF WOE – EPIC DEED STRATAGEM",
                When = "Your Shooting phase or the Fight phase, just after a NECRONS MONSTER unit from your army destroys an enemy unit.",
                Target = "One friendly NECRONS unit (excluding MONSTERS) within 6\" of that MONSTER unit.",
                Effect = "If that enemy unit was unravelling at the start of the phase, your friendly unit’s Reanimation Protocols activate.",
                Restrictions = "You can only use this Stratagem once per turn.",
                SourcePage = 29
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "ENTROPHASIC AURA TARGETING",
                Cost = 1,
                Category = "PANTHEON OF WOE – BATTLE TACTIC STRATAGEM",
                When = "Your Shooting phase or the Fight phase.",
                Target = "One NECRONS unit (excluding MONSTERS) from your army that has not been selected to shoot or fight this phase.",
                Effect = "Until the end of the phase, each time a model in your unit makes an attack that targets an enemy unit, re-roll a Hit roll of 1. If the target of that attack is unravelling, re-roll a Wound roll of 1 as well.",
                Restrictions = "",
                SourcePage = 30
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "CHRONODISTORTION",
                Cost = 1,
                Category = "PANTHEON OF WOE – BATTLE TACTIC STRATAGEM",
                When = "Fight phase, just after an enemy unit has selected its targets.",
                Target = "One NECRONS unit from your army that was selected as the target of one or more of the attacking unit’s attacks.",
                Effect = "Until the end of the phase, each time a model in your unit is destroyed, if that model has not fought this phase, roll one D6, adding 1 if the attacking unit is unravelling: on a 4+, do not remove the destroyed model from play; it can fight after the attacking unit has finished making its attacks, and is then removed from play.",
                Restrictions = "",
                SourcePage = 30
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "PHASE MELDING",
                Cost = 1,
                Category = "PANTHEON OF WOE – STRATEGIC PLOY STRATAGEM",
                When = "Your opponent’s Movement phase, when an unravelling enemy unit is selected to Fall Back.",
                Target = "One NECRONS unit from your army that is within Engagement Range of that enemy unit.",
                Effect = "When that enemy unit Falls Back, all models in that enemy unit must take a Desperate Escape test. When doing so, if that enemy unit is Ba�le-shocked, subtract 1 from each of those tests. UNIQUE: DYNASTY HAND OF THE DYNASTY 1DP PHALANXES OF NECRON SOLDIERY RECLAIM THE BATTLEFIELD FOR THEIR DYNASTY",
                Restrictions = "",
                SourcePage = 30
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.HandOfTheDynasty,
                Name = "DOMINANCE PROTOCOLS",
                Cost = 1,
                Category = "HAND OF THE DYNASTY STRATAGEM",
                When = "Command phase.",
                Target = "One friendly IMMORTALS unit.",
                Effect = "Your unit has +1 OC until the end of the turn.",
                Restrictions = "",
                SourcePage = 32
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.HandOfTheDynasty,
                Name = "WILL OF THE CONQUEROR",
                Cost = 1,
                Category = "HAND OF THE DYNASTY STRATAGEM",
                When = "End of your Movement phase.",
                Target = "One friendly IMMORTALS/NECRON WARRIORS unit.",
                Effect = "Select one objective your unit is controlling. That objective is secured.",
                Restrictions = "",
                SourcePage = 32
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.HandOfTheDynasty,
                Name = "NANOSATURATION",
                Cost = 1,
                Category = "HAND OF THE DYNASTY STRATAGEM",
                When = "Your opponent’s Shooting phase, when an enemy unit that targeted a friendly IMMORTALS/NECRON WARRIORS unit has shot.",
                Target = "That IMMORTALS/NECRON WARRIORS unit.",
                Effect = "Your unit shoots using snap shooting, but while doing so your unit can only target that enemy unit. SKYSHROUD SPEARHEAD 1DP LIKE A PLAGUE FROM ANCIENT MYTH, MURDEROUS GRAV-SKIMMING NECRON SWARMS FILL THE SKIES",
                Restrictions = "",
                SourcePage = 32
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.SkyshroudSpearhead,
                Name = "OMNILOCKED STRAFING",
                Cost = 1,
                Category = "SKYSHROUD SPEARHEAD STRATAGEM",
                When = "Your Movement phase, when a friendly NECRONS MOUNTED unit is selected to make a fall-back move.",
                Target = "That NECRONS MOUNTED unit.",
                Effect = "That move does not prevent your unit from being eligible to shoot.",
                Restrictions = "",
                SourcePage = 34
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.SkyshroudSpearhead,
                Name = "SWIFT AS DEATH",
                Cost = 1,
                Category = "SKYSHROUD SPEARHEAD STRATAGEM",
                When = "Your opponent’s Movement phase, when an enemy unit ends a move within 8\" of a friendly unengaged NECRONS MOUNTED unit.",
                Target = "That NECRONS MOUNTED unit.",
                Effect = "Your unit can make a normal move of up to D3+3\".",
                Restrictions = "",
                SourcePage = 34
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.SkyshroudSpearhead,
                Name = "EVASIVE PROTOCOLS",
                Cost = 1,
                Category = "SKYSHROUD SPEARHEAD STRATAGEM",
                When = "Your opponent’s Shooting phase, when an enemy unit targets a friendly NECRONS MOUNTED unit.",
                Target = "That NECRONS MOUNTED unit.",
                Effect = "Ranged attacks that target your unit with a S greater than your unit’s T have -1 to wound rolls. UNIQUE: HYPERCRYPT THE PHAERON'S ARMOURY 1DP WAR ENGINES ARE UNLEASHED FROM WITHIN THE TOMB WORLD’S ROYAL ARMOURY TO CRUSH THE FOE",
                Restrictions = "",
                SourcePage = 34
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.ThePhaeronsArmoury,
                Name = "SUBSURFACE QUANTUMWEAVE",
                Cost = 1,
                Category = "THE PHAERON'S ARMOURY STRATAGEM",
                When = "Your opponent’s Shooting phase or the Fight phase, when an enemy unit targets a friendly NECRONS TITANIC FLY unit.",
                Target = "That NECRONS TITANIC FLY unit.",
                Effect = "A�acks that target your unit have -1 AP until that enemy unit has attacked.",
                Restrictions = "",
                SourcePage = 36
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.ThePhaeronsArmoury,
                Name = "PARTICLE PULSE",
                Cost = 1,
                Category = "THE PHAERON'S ARMOURY STRATAGEM",
                When = "Start of your Shooting phase.",
                Target = "One friendly NECRONS TITANIC FLY unit.",
                Effect = "Select one visible enemy unit within 12\" of your unit. That enemy unit has +3\" detection range.",
                Restrictions = "",
                SourcePage = 36
            });
        stratagems.Add(
            new NecronStratagem11
            {
                Detachment = NecronDetachment.ThePhaeronsArmoury,
                Name = "COSMIC STORM",
                Cost = 1,
                Category = "THE PHAERON'S ARMOURY STRATAGEM",
                When = "Your Shooting phase, when a friendly OBELISK/TESSERACT VAULT unit is selected to shoot.",
                Target = "That OBELISK/TESSERACT VAULT unit.",
                Effect = "Your unit’s Tesla Sphere weapons have +1 AP. CRUSADE RULES In this section you’ll find additional rules for playing Crusade battles that are bespoke to NECRONS units.",
                Restrictions = "",
                SourcePage = 36
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "VEIL OF DARKNESS",
                Points = 20,
                Rule = "With this device the bearer can twist space and time about them, enfolding them in a swirling darkness. When it fades, they have vanished, rematerialising elsewhere through a miracle of arcane science. NECRONS model only. (Once per battle, per army) At the end of your opponent’s turn, if this unit is unengaged, you can use this ability. If you do: ▪ Place this unit in strategic reserves. ▪ This unit has Deep Strike until the start of your next Shooting phase. ▪ This unit must make an ingress move in your next Movement phase (including in your first turn).",
                SourcePage = 4
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "NETHER-REALM CASKET",
                Points = 20,
                Rule = "Clouds of hyper-dense particles billow from this small artefact, to obscure and shield the bearer from the foe. NECRONS model only. While the bearer is leading a unit, models in that unit have the Stealth ability.",
                SourcePage = 4
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "PHASAL SUBJUGATOR (AURA)",
                Points = 35,
                Rule = "This engraved sigil-circuitry transforms the fierce will of the bearer into a surging lash across every phasal state. NECRONS model only. While a friendly NECRONS unit (excluding CHARACTER units) is within 6\" of the bearer, each time a model in that unit makes an attack, add 1 to the Hit roll.",
                SourcePage = 4
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.AwakenedDynasty,
                Name = "ENAEGIC DERMAL BOND",
                Points = 30,
                Rule = "The bearers living metal mantle is bonded to mirror versions of itself across many dimensional thresholds. NECRONS model only. The bearer has the Feel No Pain 4+ ability.",
                SourcePage = 4
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "ETERNAL MADNESS",
                Points = 20,
                Rule = "This Necrons sanity suﬀered during the Great Sleep. Now they are driven by a wrathful zeal, one which has seeped through the carrier waves of their commandments and into their followers. NECRONS model only. In the Fight phase, each time a model in the bearer’s unit is destroyed, if that model has not fought this phase, roll one D6: on a 4+, do not remove the destroyed model from play; it can fight after the attacking models unit has finished making its attacks, and is then removed from play.",
                SourcePage = 7
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "INGRAINED SUPERIORITY",
                Points = 5,
                Rule = "An immortal destroyer, this war leaders every victim is etched irrevocably into their cognitive engrams. Every weakness they ever overcame is recalled, frailties they can exploit on each new battlefield. NECRONS model only. Each time a model in the bearer’s unit makes an attack, on a Critical Wound, improve the Armour Penetration characteristic of that attack by 1.",
                SourcePage = 7
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "SOULLESS REAPER",
                Points = 15,
                Rule = "This deathly killer exudes a soul-sapping presence, the promise of lifes’ end so explicit in their chilling gaze that few can muster the strength of will to evade it. DESTROYER CULT model only. Each time an enemy unit within Engagement Range of the bearer’s unit is selected to Fall Back, roll one D6: on a 3+, that unit cannot Fall Back this phase and must Remain Stationary.",
                SourcePage = 7
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.AnnihilationLegion,
                Name = "ELDRITCH NIGHTMARE",
                Points = 10,
                Rule = "Atavistic fears are summoned from the pits of nightmare and thrust into the minds of all foes near this metal-skinned horror. DESTROYER CULT model only. At the start of the Fight phase, each enemy unit within Engagement Range of the bearer must take a Ba�leshock test.",
                SourcePage = 7
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "DIMENSIONAL SANCTUM",
                Points = 20,
                Rule = "This Cryptek has had a personal dimensional pocket reality crafted for them, from which they can emerge into battle at will. CRYPTEK model only. Models in the bearer’s unit have the Infiltrators ability.",
                SourcePage = 10
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "HYPERPHASIC FULCRUM",
                Points = 15,
                Rule = "With this small orb, its surface shimmering in fractal hues and shifting in multidimensional facets, a Cryptek can bypass the ancient security protocols placed upon the areas dormant machineries. Esoteric data and unusual strands of energy can be accessed, the orb acting as a skeleton key to greater power. CRYPTEK model only. While the bearer is leading a unit, if that unit is wholly within your army’s Power Matrix, each time a model in that unit makes an attack, re-roll a Wound roll of 1.",
                SourcePage = 10
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "AUTODIVINATOR",
                Points = 15,
                Rule = "This cryptogeometric puzzle box harbours a chronoscarab. The tiny construct creeps forward in time along temporal webs before hauling back a captured net of future moments. Its master can scry these wriggling futures to predict their foe’s strategies, using the knowledge to develop new counter-tactics to ambush the attackers. CRYPTEK model only. Each time your opponent gains a CP as the result of an ability, roll one D6: on a 2+, you also gain 1CP.",
                SourcePage = 10
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CanoptekCourt,
                Name = "METALODERMAL TESLA WEAVE",
                Points = 10,
                Rule = "This microsilicate weave generates a cyclical electrostatic overload that, providing its user triggers it in time, sends arcing lightning leaping out to roast onrushing attackers. CRYPTEK model only. Once per phase, when an enemy unit selects the bearer’s unit as a target of a charge, roll one D6: on a 2-5, that enemy unit suﬀers D3 mortal wounds; on a 6, that enemy unit suﬀers 3 mortal wounds.",
                SourcePage = 10
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "HONOURABLE COMBATANT",
                Points = 10,
                Rule = "A strict adherent to the ancient codes, this noble reserves their greatest displays of power to execute the enemy's leaders. Such champions are brought low in ostentatious manner, the inspiration or strategy they once provided severed at a stroke. OVERLORD model only. Each time the bearer’s unit destroys an enemy CHARACTER unit, your opponent loses 1CP if they have any.",
                SourcePage = 13
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "UNFLINCHING WILL",
                Points = 20,
                Rule = "With sheer will this noble drives their warriors into battle against the most malevolent foes. Not from pretenders to nobility, nor marauding beasts, nor titanic machineries will this noble turn; they have faced greater horrors during their long existence. OVERLORD model only. The bearer’s melee weapons have the [PRECISION] and [ANTI-INFANTRY 5+] abilities.",
                SourcePage = 13
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "WARRIOR NOBLE",
                Points = 15,
                Rule = "This dynastic scion is an expert in the arts of martial excellence, and their living metal form has been enhanced by their Crypteks. With eﬀortless arrogance, they and their guardians parry or contemptuously swat aside the strikes of those they see as their lessers. OVERLORD model only. Each time a melee attack targets the bearer’s unit, subtract 1 from the Hit roll.",
                SourcePage = 13
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.ObeisancePhalanx,
                Name = "ETERNAL CONQUEROR",
                Points = 25,
                Rule = "This insatiable subjugator sees all before them as theirs for the taking by ancient right of conquest. They wield their legion like an iron gauntlet, using it to seize the battlefield in a demonstration of their power and strength, daring others to try taking it from them. OVERLORD model only. Each time a model in the bearer’s unit makes an attack that targets an enemy unit within range of an objective marker, you can re-roll the Hit roll.",
                SourcePage = 13
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "DIMENSIONAL OVERSEER",
                Points = 25,
                Rule = "With a mind as labyrinthine as the multi-dimensional tomb itself, this commander directs their servants in complex forays through the tomb’s hyperspatial architecture, catching the witless enemy off guard. NECRONS model only. While the bearer is on the battlefield or in Strategic Reserves, add one to the number of units from your army that you can select for the Hyperphasing rule.",
                SourcePage = 16
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "ARISEN TYRANT",
                Points = 25,
                Rule = "This ancient being will suﬀer no threats to their rule, stepping imperiously from twisting nightmare dimensions to annihilate any who dare to oppose them. NECRONS model only. Each time a model in the bearer’s unit makes an attack, re-roll a Hit roll of 1. If the bearer’s unit was set up on the battlefield this turn, you can re-roll the Hit roll instead.",
                SourcePage = 16
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "HYPERSPATIAL TRANSFER NODE",
                Points = 15,
                Rule = "Wound with filaments of temporal circuitry, this amulet enables the bearer to accelerate themselves and their closest guardians in stu�ering bursts of compressed time. NECRONS model only. Each time the bearer’s unit Advances, do not make an Advance roll for it. Instead, until the end of the phase, add 6\" to the Move characteristic of models in the bearer’s unit.",
                SourcePage = 16
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.HypercryptLegion,
                Name = "OSTEOCLAVE FULCRUM",
                Points = 20,
                Rule = "This hypermaterial key unlocks portals between transpatial dimensions embedded into a tomb’s architecture. Through such limitless doorways, the Necrons can pursue their quarry wherever they hide. NECRONS model only. Models in the bearer’s unit have the Deep Strike ability.",
                SourcePage = 16
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "DREAD MAJESTY (AURA)",
                Points = 30,
                Rule = "When this noble unleashes the might of their cosmic armoury, their followers are le� in no doubt as to the importance of the battle at hand. If they do not strive to live up to the lethal effectiveness of the dynasty’s war engines, their Overlord’s wrath will be terrible. OVERLORD or CATACOMB COMMAND BARGE model only. While a friendly NECRONS unit (excluding MONSTER and TITANIC units) is within 6\" of the bearer, each time a model in that unit makes an attack, re-roll a Hit roll of 1 and re-roll a Wound roll of 1.",
                SourcePage = 19
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "MINIATURISED NEBULOSCOPE",
                Points = 15,
                Rule = "Feeding vampirically on datastreams from the dynasty’s war engines, this device enables its owner to track enemies through multiple dimensions, leaving them no hiding place. NECRONS model only. Ranged weapons equipped by models in the bearer’s unit have the [IGNORES COVER] ability.",
                SourcePage = 19
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "DEMANDING LEADER",
                Points = 10,
                Rule = "This Necron noble is a master of rapid warfare, commanding armoured columns with great precision. NECRONS model only. In your Command phase, select one friendly NECRONS VEHICLE or NECRONS MOUNTED unit (excluding TITANIC units) within 6\" of the bearer. Until the start of your next Command phase, that unit is eligible to shoot in a turn in which it Fell Back.",
                SourcePage = 19
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.StarshatterArsenal,
                Name = "CHRONO-IMPEDANCE FIELDS",
                Points = 25,
                Rule = "When activated, this device wreathes dynastic craft in a time-dilation field that reduces the force of incoming blows and shots. NECRONS model only. In your Command phase, select one friendly NECRONS VEHICLE or NECRONS MOUNTED unit (excluding TITANIC units) within 6\" of the bearer. Until the start of your next Command phase, each time an attack is allocated to a model in that unit, subtract 1 from the Damage characteristic of that attack.",
                SourcePage = 19
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "QUANTUM ABACUS",
                Points = 15,
                Rule = "This cloud-like familiar of fractal computational electrons possesses a cogitational intellect that, when fed raw data, produces inspired strategic guidance. NECRONS model only. Each time you select the bearer’s unit as the target of a Stratagem, roll one D6, adding 1 if it is within range of one or more objectives: on a 4+, you gain 1CP.",
                SourcePage = 22
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "ATOMIC DISINTEGRATORS",
                Points = 10,
                Rule = "Energy lenses project in a web from a central node borne by the Cryptek, coalescing about Necron weapon systems and focusing their power still further. CRYPTEK model only. In your Shooting phase, each time the bearer’s unit is selected to shoot, when selecting an ability for the Technosorcerous Augmentations Detachment rule, you can also select from the following abilities: [ANTI-MONSTER 5+], [ANTI-VEHICLE 5+].",
                SourcePage = 22
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "GAUNTLET OF COMPRESSION",
                Points = 20,
                Rule = "Clinging to the bearer’s hand like a glove woven from shadow, this strange device folds space-time with a single gesture, momentarily compressing the relative distance between Necron weapons and their targets. NECRONS model only. Add 6\" to the Range characteristic of ranged weapons equipped by models in the bearer’s unit.",
                SourcePage = 22
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CryptekConclave,
                Name = "GRAVITIC BOLAS",
                Points = 15,
                Rule = "Projected from the bearer’s staff as a secondary energistic emission, these solid-state electroshackles bind and trammel their victims in crackling fe�ers. CRYPTEK model only. In your Shooting phase, after the bearer has shot, select one enemy unit hit by one or more of those attacks (excluding TITANIC units); until the start of your next turn, that enemy unit is pinned. While a unit is pinned, subtract 2 from that unit’s Move characteristic and subtract 2 from Charge rolls made for that unit.",
                SourcePage = 22
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "DESTROYER ANKH",
                Points = 20,
                Rule = "Though it resembles a typical ankh, this chest piece fills its host with an insatiable need to exterminate the foe. CATACOMB COMMAND BARGE or OVERLORD model only. The bearer has the DESTROYER CULT keyword. Add 2\" to the Move characteristic of models in the bearer’s unit and add 2 to the A�acks characteristic of melee weapons equipped by the bearer.",
                SourcePage = 25
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "MURDERMIND",
                Points = 15,
                Rule = "SUPPORT: SKORPEKH DESTROYERS, LOKHUST DESTROYERS, OPHYDIAN DESTROYERS, LOKHUST HEAVY DESTROYERS Consumed by the Destroyer madness, this Cryptek’s powerful intellect is turned entirely toward killing. CRYPTEK model only. The bearer has the DESTROYER CULT keyword. Add 3\" to the Move characteristic of the bearer.",
                SourcePage = 25
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "MARK OF THE NEKROSOR",
                Points = 20,
                Rule = "The malevolent madness of Ammentar itself burns in the minds of this warrior and their comrades. DESTROYER CULT model only. Each time a model in the bearer’s unit makes an attack, add 1 to the Hit roll.",
                SourcePage = 25
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.CursedLegion,
                Name = "CURSED CIRCLET",
                Points = 25,
                Rule = "This band of living metal sinks into the bearer’s brow and floods their neural cortex with a murderous urge. DESTROYER CULT model only. In your opponent’s Shooting phase, when an enemy unit has shot, if a model in this unit was destroyed by those attacks, this unit can make a surge move of up to D6\".",
                SourcePage = 25
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "SINGULARITY MATRIX",
                Points = 45,
                Rule = "This eldritch device fe�ers and directs the Deceiver’s powers within a vortex that devours lesser wits entirely. C’TAN SHARD OF THE DECEIVER model only. This model has the following ability: Lord of Deceit (Aura): Each time your opponent targets a unit from their army with a Stratagem, if that unit is within 12\" of this model, increase the cost of that use of that Stratagem by 1CP.",
                SourcePage = 28
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "QUANTUM GOAD",
                Points = 45,
                Rule = "The energies of this binding latch onto enemy targets and shunt the shard into alignment with them. C’TAN SHARD OF THE NIGHTBRINGER model only. This model is eligible to declare a charge in a turn in which it Advanced.",
                SourcePage = 28
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "ANIMUS DAMPER",
                Points = 35,
                Rule = "This device bleeds off the Void Dragon shard’s energies and earths them violently through nearby machines. C’TAN SHARD OF THE VOID DRAGON model only. Once per turn, at the start of your opponent’s Shooting phase, select one enemy VEHICLE unit visible to the bearer. That unit must take a Leadership test. Until the end of the phase, each time a model in that unit makes an attack, subtract 1 from the Hit roll and, if that Leadership test was failed, subtract 1 from the Wound roll as well.",
                SourcePage = 28
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.PantheonOfWoe,
                Name = "RELETAVISTIC TETHER",
                Points = 40,
                Rule = "This binding uses atomic resonance magnetism to compel the shard into the midst of the foe. TRANSCENDENT C’TAN model only. In your turn, when this unit makes an ingress/advance move using its Transdimensional Displacement ability, this unit can end that move more than 6\" horizontally from all enemy units (instead of more than 8\"). When this unit ends that move within 8\" of an enemy unit, this unit is not eligible to declare a charge until the end of the turn.",
                SourcePage = 28
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.HandOfTheDynasty,
                Name = "ENLIVENED SENTINELS UPGRADE",
                Points = 20,
                Rule = "Goaded into battle by overriding imperatives, these Warriors march relentlessly out to enact the will of their masters. NECRON WARRIORS unit only. This unit has Scouts 5\"",
                SourcePage = 31
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.HandOfTheDynasty,
                Name = "TOOLS OF DOMINION UPGRADE",
                Points = 15,
                Rule = "Nobles are not above expending additional resources to improve the armaments of their more valuable soldiery. IMMORTALS unit only. This unit’s ranged attacks have [RAPID FIRE 1].",
                SourcePage = 31
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.SkyshroudSpearhead,
                Name = "RECURSIVE REANIMATION UPGRADE",
                Points = 5,
                Rule = "Ancillary reclamators capture the molecular mafter of these Tomb Blades even as it is blasted apart, drawing it in a swirling comet trail behind them and working it back into their physical forms. TOMB BLADES unit only. When this unit activates its Reanimation Protocols, +1 to the roll.",
                SourcePage = 33
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.SkyshroudSpearhead,
                Name = "DEEPENING MADNESS UPGRADE",
                Points = 20,
                Rule = "Like some echo of the Nekrosor’s own murder-madness, a coldly eﬀicient kill-frenzy grips these Lokhust Destroyers so that – even as they streak into battle – they lay down one ferocious volley of fire after another. DESTROYER CULT MOUNTED unit only. This unit’s ranged attacks have [ASSAULT].",
                SourcePage = 33
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.ThePhaeronsArmoury,
                Name = "RELOCATIONAL OPTIMISER",
                Points = 25,
                Rule = "Subtle transplanar optics allow this war leader to peer through the energy veil of a Monolith’s eternity gate and dissect the defences and weaknesses of the enemies beyond it. This ensures their emergence from the portal is as grandly bloody and spectacular as possible. NECRONS model only. When this unit is selected to shoot, if this unit was set up using a Monolith’s Eternity Gate ability this turn, this unit’s ranged attacks have: ▪ [LETHAL HITS]. ▪ Or: [SUSTAINED HITS 1].",
                SourcePage = 35
            });
        enhancements.Add(
            new NecronEnhancement11
            {
                Detachment = NecronDetachment.ThePhaeronsArmoury,
                Name = "MORTALITY SHROUD (AURA) UPGRADE",
                Points = 10,
                Rule = "Projecting a subtly tailored weave of spiritual entropy fields, antiphotons and infrasonic oppression waves, this war engine projects a sense of ominous dread and impending death like a shadow before it. OBELISK unit only. In your opponent’s Ba�le-shock step, if an enemy unit within 8\" of this unit is below starting strength, that enemy unit makes a battle-shock roll.",
                SourcePage = 35
            });

    }

    public static IReadOnlyList<NecronStratagem11>
        StratagemsFor(string faction)
    {
        HashSet<NecronDetachment> selected =
            new HashSet<NecronDetachment>(
                NecronDetachmentRuntime
                    .GetSelected(faction));

        return stratagems
            .Where(rule =>
                selected.Contains(
                    rule.Detachment))
            .ToArray();
    }

    public static IReadOnlyList<NecronEnhancement11>
        EnhancementsFor(string faction)
    {
        HashSet<NecronDetachment> selected =
            new HashSet<NecronDetachment>(
                NecronDetachmentRuntime
                    .GetSelected(faction));

        return enhancements
            .Where(rule =>
                selected.Contains(
                    rule.Detachment))
            .ToArray();
    }

    public static NecronDetachmentRule11
        DetachmentRule(
            NecronDetachment detachment)
    {
        NecronDetachmentRule11 result;

        return rules.TryGetValue(
            detachment,
            out result)
            ? result
            : null;
    }

    public static bool Has(
        string faction,
        NecronDetachment detachment)
    {
        return NecronDetachmentRuntime.Has(
            faction,
            detachment);
    }

    public static bool IsNecrons(
        SquadController unit)
    {
        return
            unit != null &&
            unit.HasIntrinsicKeyword(
                "necrons");
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

        if (UnitHasEnhancementDirect(
                unit,
                name))
        {
            return true;
        }

        SquadController action =
            unit.JoinedActionController();

        return
            action.AttachedLeader != null &&
            UnitHasEnhancementDirect(
                action.AttachedLeader,
                name);
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

        if (string.IsNullOrWhiteSpace(raw) ||
            string.IsNullOrWhiteSpace(wanted) ||
            string.IsNullOrWhiteSpace(unitName))
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
                unitIndex - 160);

        int length =
            Mathf.Min(
                raw.Length - start,
                620);

        string window =
            raw.Substring(
                start,
                length);

        return
            window.IndexOf(
                wanted,
                StringComparison.OrdinalIgnoreCase) >= 0;
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

    private static bool FriendlyEnhancementAuraNear(
        GameController game,
        SquadController unit,
        string enhancement,
        float range,
        bool excludeCharacterTarget)
    {
        if (game == null ||
            unit == null)
        {
            return false;
        }

        SquadController action =
            unit.JoinedActionController();

        if (excludeCharacterTarget &&
            action.HasKeyword("character"))
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
                    action.FactionId,
                    StringComparison.OrdinalIgnoreCase) &&
                UnitHasEnhancementDirect(
                    source,
                    enhancement) &&
                game.JoinedDistancePublic(
                    source.JoinedActionController(),
                    action) <=
                    range + 0.001f);
    }

    public static bool IsWhollyWithinPowerMatrix(
        GameController game,
        SquadController unit)
    {
        if (game == null ||
            unit == null ||
            !Has(
                unit.FactionId,
                NecronDetachment.CanoptekCourt))
        {
            return false;
        }

        SquadController action =
            unit.JoinedActionController();

        if (game.MissionUnitWhollyWithinDeploymentZone(
                action,
                action.FactionId))
        {
            return true;
        }

        if (game.Necrons11ControlsHalfNoMansLandObjectives(
                action.FactionId) &&
            game.Necrons11UnitWhollyInNoMansLand(
                action))
        {
            return true;
        }

        if (game.Necrons11ControlsHalfOpponentZoneObjectives(
                action.FactionId) &&
            game.MissionUnitWhollyWithinOpponentDeploymentZone(
                action,
                action.FactionId))
        {
            return true;
        }

        return false;
    }

    public static bool IsUnravelling(
        GameController game,
        SquadController target)
    {
        if (game == null ||
            target == null)
        {
            return false;
        }

        SquadController actionTarget =
            target.JoinedActionController();

        foreach (NecronGameController controller
            in NecronsFactionPack11Runtime
                .AllControllers())
        {
            if (controller == null ||
                !controller.HasDetachment(
                    NecronDetachment.PantheonOfWoe))
            {
                continue;
            }

            foreach (SquadController source
                in controller.ArmyUnits)
            {
                if (source == null ||
                    !source.IsAlive ||
                    !source.IsOnBattlefield ||
                    !source.HasKeyword("monster"))
                {
                    continue;
                }

                float range =
                    NecronsFactionPack11Runtime
                        .DistortionExtended(source)
                    ? 9f
                    : 6f;

                if (game.JoinedDistancePublic(
                        source.JoinedActionController(),
                        actionTarget) <=
                    range + 0.001f)
                {
                    return true;
                }
            }
        }

        return false;
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

        SquadController actionAttacker =
            attacker != null
            ? attacker.JoinedActionController()
            : null;

        SquadController actionTarget =
            target != null
            ? target.JoinedActionController()
            : null;

        if (actionAttacker != null &&
            IsNecrons(actionAttacker))
        {
            string faction =
                actionAttacker.FactionId;

            if (GrantsIgnoresCover(
                    actionAttacker,
                    mode) &&
                !state.ignoresCover)
            {
                // Cover hooks run before faction modifiers. If a Necron rule
                // grants IGNORES COVER, undo the BS penalty already applied
                // by Benefit of Cover and mark the attack accordingly.
                if (state.benefitOfCover &&
                    state.skillModifier > 0)
                {
                    state.skillModifier -= 1;
                }

                state.ignoresCover = true;
                state.notes.Add(
                    "Necrons: Ignores Cover");
            }

            if (Has(
                    faction,
                    NecronDetachment.AwakenedDynasty) &&
                actionAttacker.AttachedLeader != null &&
                actionAttacker.AttachedLeader.IsAlive &&
                actionAttacker.AttachedLeader.HasKeyword("necrons") &&
                actionAttacker.AttachedLeader.HasKeyword("character"))
            {
                state.hitRollModifier += 1;
                state.notes.Add(
                    "Command Protocols: +1 Hit");
            }

            if (Has(
                    faction,
                    NecronDetachment.AwakenedDynasty) &&
                FriendlyEnhancementAuraNear(
                    game,
                    actionAttacker,
                    "PHASAL SUBJUGATOR",
                    6f,
                    true))
            {
                state.hitRollModifier += 1;
                state.notes.Add(
                    "Phasal Subjugator: +1 Hit");
            }

            if (Has(
                    faction,
                    NecronDetachment.ObeisancePhalanx) &&
                actionTarget != null &&
                NecronsFactionPack11Runtime
                    .WorthyFoe(faction) ==
                    actionTarget &&
                (actionAttacker.HasKeyword("noble") ||
                 actionAttacker.HasKeyword("lychguard") ||
                 actionAttacker.HasKeyword("triarch")))
            {
                state.woundRollModifier += 1;
                state.notes.Add(
                    "Worthy Foes: +1 Wound");
            }

            if (Has(
                    faction,
                    NecronDetachment.StarshatterArsenal) &&
                !actionAttacker.HasKeyword("monster") &&
                actionTarget != null &&
                game != null &&
                game.UnitWithinAnyObjective(
                    actionTarget))
            {
                state.hitRollModifier += 1;
                state.notes.Add(
                    "Relentless Onslaught: +1 Hit");
            }

            if (Has(
                    faction,
                    NecronDetachment.SkyshroudSpearhead) &&
                NameOrKeyword(
                    actionAttacker,
                    "Tomb Blades") &&
                actionAttacker.WasSetUpThisTurn)
            {
                state.hitRollModifier += 1;
                state.notes.Add(
                    "Transdimensional Deployment: +1 Hit after ingress");
            }

            if (UnitHasEnhancement(
                    actionAttacker,
                    "MARK OF THE NEKROSOR"))
            {
                state.hitRollModifier += 1;
                state.notes.Add(
                    "Mark of the Nekrosor: +1 Hit");
            }

            if (mode == AttackMode.Ranged &&
                NecronsFactionPack11Runtime
                    .HasAugmentation(
                        actionAttacker,
                        "heavy") &&
                game != null &&
                !game.IsUnitEngagedPublic(
                    actionAttacker) &&
                !actionAttacker.WasSetUpThisTurn &&
                actionAttacker.MaxDistanceMovedThisTurn() <=
                    3.001f)
            {
                state.hitRollModifier += 1;
                state.notes.Add(
                    "Technosorcerous Augmentations — Heavy: +1 Hit");
            }

            if (NecronsFactionPack11Runtime
                .IsCursedTarget(
                    faction,
                    actionTarget))
            {
                state.hitRollModifier += 1;
                state.woundRollModifier += 1;
                state.notes.Add(
                    "Curse of the Cryptek: +1 Hit/+1 Wound");
            }

            if (NecronsFactionPack11Runtime
                .HasFlag(
                    actionAttacker,
                    "spoor_of_frailty") &&
                actionTarget != null &&
                IsBelowStartingStrength(
                    actionTarget))
            {
                state.hitRollModifier += 1;

                if (actionTarget
                    .IsAtOrBelowHalfStrength())
                {
                    state.woundRollModifier += 1;
                }
            }

            if (NecronsFactionPack11Runtime
                .HasFlag(
                    actionAttacker,
                    "merciless_reclamation") &&
                actionTarget != null &&
                game != null &&
                game.UnitWithinAnyObjective(
                    actionTarget))
            {
                state.woundRollModifier += 1;
            }

            if (NecronsFactionPack11Runtime
                .HasFlag(
                    actionAttacker,
                    "molecular_targeting"))
            {
                if (state.skillModifier > 0)
                    state.skillModifier = 0;

                if (state.hitRollModifier < 0)
                    state.hitRollModifier = 0;

                if (actionAttacker.HasKeyword("cryptek") &&
                    state.woundRollModifier < 0)
                {
                    state.woundRollModifier = 0;
                }
            }
        }

        if (actionTarget != null &&
            IsNecrons(actionTarget))
        {
            if (NecronsFactionPack11Runtime
                .HasFlag(
                    actionTarget,
                    "masks_of_death") ||
                NecronsFactionPack11Runtime
                    .HasFlag(
                        actionTarget,
                        "image_of_death"))
            {
                state.hitRollModifier -= 1;
            }

            if (NecronsFactionPack11Runtime
                .HasFlag(
                    actionTarget,
                    "unyielding_forms") &&
                weapon != null &&
                weapon.strength >
                    actionTarget.Toughness)
            {
                state.woundRollModifier -= 1;
            }

            if (mode == AttackMode.Melee &&
                UnitHasEnhancement(
                    actionTarget,
                    "WARRIOR NOBLE"))
            {
                state.hitRollModifier -= 1;
            }

            if (mode == AttackMode.Ranged &&
                NecronsFactionPack11Runtime
                    .HasFlag(
                        actionTarget,
                        "evasive_protocols") &&
                weapon != null &&
                weapon.strength >
                    actionTarget.Toughness)
            {
                state.woundRollModifier -= 1;
            }
        }
    }

    public static int MinimumSustainedHits(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        if (attacker == null)
            return 0;

        int value = 0;

        if (NecronsFactionPack11Runtime
            .HasFlag(
                attacker,
                "methodical_murder"))
        {
            value = 1;
        }

        if (NecronsFactionPack11Runtime
            .HasFlag(
                attacker,
                "relocational_sustained"))
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
        return
            attacker != null &&
            NecronsFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "relocational_lethal");
    }

    public static bool GrantsPrecision(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        if (attacker == null ||
            mode != AttackMode.Melee)
        {
            return false;
        }

        return
            UnitHasEnhancement(
                attacker,
                "UNFLINCHING WILL") ||
            NecronsFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "suffer_no_rival");
    }

    public static bool GrantsLance(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return false;
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
                "MINIATURISED NEBULOSCOPE") ||
            NecronsFactionPack11Runtime
                .HasAugmentation(
                    attacker,
                    "ignores cover") ||
            NecronsFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "solar_pulse");
    }

    public static bool GrantsDevastatingWounds(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return
            attacker != null &&
            NecronsFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "cynosure_of_eradication") &&
            (attacker.HasKeyword("cryptek") ||
             attacker.HasKeyword("canoptek"));
    }

    public static bool GrantsHazardous(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return
            attacker != null &&
            NecronsFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "entropic_damping_hazardous");
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

        if (mode == AttackMode.Melee &&
            NecronsFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "hungry_void"))
        {
            result += 1;
        }

        if (Has(
                attacker.FactionId,
                NecronDetachment.CursedLegion))
        {
            if (attacker.HasKeyword(
                    "destroyer cult"))
            {
                result += 2;
            }
            else if (!attacker.HasKeyword("monster") &&
                     !attacker.HasKeyword("titanic") &&
                     NecronsFactionPack11Runtime
                        .ColdFervourEmpowered(
                            attacker.FactionId))
            {
                result += 2;
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
        return 0;
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
            model == null ||
            model.Squad == null ||
            mode != AttackMode.Melee)
        {
            return 0;
        }

        return
            UnitHasEnhancementDirect(
                model.Squad,
                "DESTROYER ANKH")
            ? 2
            : 0;
    }

    public static int AdditionalRapidFire(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        if (attacker == null ||
            mode != AttackMode.Ranged)
        {
            return 0;
        }

        return
            UnitHasEnhancement(
                attacker,
                "TOOLS OF DOMINION UPGRADE")
            ? 1
            : 0;
    }

    public static int AdditionalBlast(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return 0;
    }

    public static bool GrantsBlast(
        SquadController attacker,
        WeaponData weapon,
        AttackMode mode)
    {
        return false;
    }

    public static int ApModifier(
        GameController game,
        SquadController attacker,
        SquadController target,
        ModelToken model,
        WeaponData weapon,
        AttackMode mode)
    {
        int result = 0;

        if (attacker != null &&
            IsNecrons(attacker))
        {
            string faction =
                attacker.FactionId;

            if (mode == AttackMode.Melee &&
                NecronsFactionPack11Runtime
                    .HasFlag(
                        attacker,
                        "hungry_void") &&
                attacker.AttachedLeader != null &&
                attacker.AttachedLeader.IsAlive &&
                attacker.AttachedLeader.HasKeyword("necrons") &&
                attacker.AttachedLeader.HasKeyword("character"))
            {
                result += 1;
            }

            if (mode == AttackMode.Ranged &&
                Has(
                    faction,
                    NecronDetachment.AnnihilationLegion) &&
                attacker.HasKeyword("destroyer cult") &&
                target != null &&
                game != null &&
                game.Necrons11IsClosestEnemyUnit(
                    attacker,
                    target))
            {
                result += 1;
            }

            if (target != null &&
                IsUnravelling(
                    game,
                    target))
            {
                result += 1;
            }

            if (NecronsFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "cosmic_storm") &&
                weapon != null &&
                !string.IsNullOrWhiteSpace(
                    weapon.displayName) &&
                weapon.displayName.IndexOf(
                    "Tesla Sphere",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result += 1;
            }
        }

        if (target != null &&
            NecronsFactionPack11Runtime
                .HasFlag(
                    target,
                    "subsurface_quantumweave"))
        {
            result -= 1;
        }

        return result;
    }

    public static int ToughnessModifier(
        SquadController target)
    {
        return 0;
    }

    public static float MoveModifier(
        SquadController unit)
    {
        if (unit == null)
            return 0f;

        float result = 0f;

        if (Has(
                unit.FactionId,
                NecronDetachment
                    .ThePhaeronsArmoury) &&
            unit.HasKeyword("necrons") &&
            unit.HasKeyword("titanic") &&
            unit.HasKeyword("fly"))
        {
            result += 6f;
        }

        if (UnitHasEnhancement(
                unit,
                "DESTROYER ANKH"))
        {
            result += 2f;
        }

        if (NecronsFactionPack11Runtime
            .IsPinned(unit))
        {
            result -= 2f;
        }

        return result;
    }

    public static int AdvanceRollModifier(
        SquadController unit)
    {
        return 0;
    }

    public static int ChargeRollModifier(
        GameController game,
        SquadController attacker,
        SquadController target)
    {
        if (attacker == null)
            return 0;

        int result = 0;

        if (Has(
                attacker.FactionId,
                NecronDetachment.AnnihilationLegion) &&
            (attacker.HasKeyword("destroyer cult") ||
             NameOrKeyword(attacker, "Flayed Ones")) &&
            target != null &&
            target.IsAtOrBelowHalfStrength())
        {
            result += 1;
        }

        if (NecronsFactionPack11Runtime
            .IsPinned(attacker))
        {
            result -= 2;
        }

        if (NecronsFactionPack11Runtime
            .HasFlag(
                attacker,
                "spreading_madness") &&
            target != null &&
            game != null &&
            game.Necrons11EnemyEngagedByFriendly(
                attacker,
                target))
        {
            result += 2;
        }

        return result;
    }

    public static bool CanRerollCharge(
        SquadController unit)
    {
        return
            unit != null &&
            Has(
                unit.FactionId,
                NecronDetachment.AnnihilationLegion) &&
            (unit.HasKeyword("destroyer cult") ||
             NameOrKeyword(unit, "Flayed Ones"));
    }

    public static bool CanShootAfterFallBack(
        SquadController unit)
    {
        return
            unit != null &&
            (NecronsFactionPack11Runtime
                .HasFlag(
                    unit,
                    "shoot_after_fallback") ||
             NecronsFactionPack11Runtime
                .HasFlag(
                    unit,
                    "demanding_leader"));
    }

    public static bool CanChargeAfterFallBack(
        SquadController unit)
    {
        return false;
    }

    public static bool CanShootAfterAdvance(
        SquadController unit)
    {
        if (unit == null)
            return false;

        if (NecronsFactionPack11Runtime
            .HasFlag(
                unit,
                "shoot_after_advance"))
        {
            return true;
        }

        if (Has(
                unit.FactionId,
                NecronDetachment.StarshatterArsenal) &&
            (unit.HasKeyword("vehicle") ||
             unit.HasKeyword("mounted")) &&
            !unit.HasKeyword("titanic"))
        {
            return true;
        }

        if (Has(
                unit.FactionId,
                NecronDetachment.HandOfTheDynasty) &&
            (NameOrKeyword(unit, "Immortals") ||
             NameOrKeyword(unit, "Necron Warriors")))
        {
            return true;
        }

        if (Has(
                unit.FactionId,
                NecronDetachment.SkyshroudSpearhead) &&
            UnitHasEnhancement(
                unit,
                "DEEPENING MADNESS UPGRADE"))
        {
            return true;
        }

        if (Has(
                unit.FactionId,
                NecronDetachment.CryptekConclave) &&
            unit.HasKeyword("cryptek"))
        {
            return true;
        }

        return false;
    }

    public static bool CanChargeAfterAdvance(
        SquadController unit)
    {
        if (unit == null)
            return false;

        return
            NecronsFactionPack11Runtime
                .HasFlag(
                    unit,
                    "charge_after_advance") ||
            UnitHasEnhancement(
                unit,
                "QUANTUM GOAD");
    }

    public static bool CanStartActionAfterAdvance(
        SquadController unit)
    {
        return
            unit != null &&
            Has(
                unit.FactionId,
                NecronDetachment.HandOfTheDynasty) &&
            (NameOrKeyword(unit, "Immortals") ||
             NameOrKeyword(unit, "Necron Warriors"));
    }

    public static int ModifyObjectiveControl(
        SquadController unit,
        ModelToken model,
        int current)
    {
        if (unit == null)
            return current;

        if (NecronsFactionPack11Runtime
            .HasFlag(
                unit,
                "territorial_obsession"))
        {
            return current +
                (unit.HasKeyword("vehicle")
                    ? 3
                    : 1);
        }

        return current;
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

        if (wanted.Contains("stealth"))
        {
            return
                UnitHasEnhancement(
                    unit,
                    "NETHER-REALM CASKET");
        }

        if (wanted.Contains("infiltrator"))
        {
            return
                UnitHasEnhancement(
                    unit,
                    "DIMENSIONAL SANCTUM");
        }

        if (wanted.Contains("deep strike"))
        {
            return
                UnitHasEnhancement(
                    unit,
                    "OSTEOCLAVE FULCRUM") ||
                (Has(
                     unit.FactionId,
                     NecronDetachment
                        .SkyshroudSpearhead) &&
                 NameOrKeyword(
                     unit,
                     "Tomb Blades"));
        }

        if (wanted.Contains("scout"))
        {
            return
                UnitHasEnhancement(
                    unit,
                    "ENLIVENED SENTINELS UPGRADE");
        }

        return false;
    }

    public static bool GrantsKeyword(
        SquadController unit,
        string keyword)
    {
        if (unit == null ||
            string.IsNullOrWhiteSpace(keyword))
        {
            return false;
        }

        string wanted =
            Normalize(keyword);

        if (wanted == "destroyer cult")
        {
            return
                UnitHasEnhancement(
                    unit,
                    "DESTROYER ANKH") ||
                UnitHasEnhancement(
                    unit,
                    "MURDERMIND");
        }

        return false;
    }

    public static bool AutomaticRerollHit(
        GameController game,
        SquadController attacker,
        SquadController target,
        int roll,
        bool success,
        AttackMode mode)
    {
        if (attacker == null)
            return false;

        string faction =
            attacker.FactionId;

        if (NecronsFactionPack11Runtime
            .HasFlag(
                attacker,
                "reroll_all_hits"))
        {
            return !success;
        }

        if (NecronsFactionPack11Runtime
            .HasFlag(
                attacker,
                "reroll_hit_ones"))
        {
            return roll == 1;
        }

        if (Has(
                faction,
                NecronDetachment.CanoptekCourt) &&
            (attacker.HasKeyword("cryptek") ||
             attacker.HasKeyword("canoptek")))
        {
            return
                IsWhollyWithinPowerMatrix(
                    game,
                    attacker)
                ? !success
                : roll == 1;
        }

        if (UnitHasEnhancement(
                attacker,
                "ARISEN TYRANT"))
        {
            return
                attacker.WasSetUpThisTurn
                ? !success
                : roll == 1;
        }

        if (UnitHasEnhancement(
                attacker,
                "ETERNAL CONQUEROR") &&
            target != null &&
            game != null &&
            game.UnitWithinAnyObjective(
                target))
        {
            return !success;
        }

        if (FriendlyEnhancementAuraNear(
                game,
                attacker,
                "DREAD MAJESTY",
                6f,
                false) &&
            !attacker.HasKeyword("monster") &&
            !attacker.HasKeyword("titanic"))
        {
            if (roll == 1)
                return true;
        }

        if (NecronsFactionPack11Runtime
            .IsAnimusTarget(
                faction,
                target))
        {
            return !success;
        }

        if (NecronsFactionPack11Runtime
            .HasFlag(
                attacker,
                "conquering_tyrant_full"))
        {
            return !success;
        }

        if (NecronsFactionPack11Runtime
            .HasFlag(
                attacker,
                "conquering_tyrant_ones") ||
            NecronsFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "entrophasic_aura_targeting"))
        {
            return roll == 1;
        }

        return false;
    }

    public static bool AutomaticRerollWound(
        GameController game,
        SquadController attacker,
        SquadController target,
        int roll,
        bool success,
        AttackMode mode)
    {
        if (attacker == null)
            return false;

        if (NecronsFactionPack11Runtime
            .HasFlag(
                attacker,
                "reroll_all_wounds"))
        {
            return !success;
        }

        if (NecronsFactionPack11Runtime
            .HasFlag(
                attacker,
                "reroll_wound_ones"))
        {
            return roll == 1;
        }

        if (Has(
                attacker.FactionId,
                NecronDetachment.CanoptekCourt) &&
            UnitHasEnhancement(
                attacker,
                "HYPERPHASIC FULCRUM") &&
            IsWhollyWithinPowerMatrix(
                game,
                attacker))
        {
            return roll == 1;
        }

        if (FriendlyEnhancementAuraNear(
                game,
                attacker,
                "DREAD MAJESTY",
                6f,
                false) &&
            !attacker.HasKeyword("monster") &&
            !attacker.HasKeyword("titanic"))
        {
            if (roll == 1)
                return true;
        }

        if (NecronsFactionPack11Runtime
            .HasFlag(
                attacker,
                "entrophasic_aura_targeting") &&
            IsUnravelling(
                game,
                target))
        {
            return roll == 1;
        }

        return false;
    }

    public static int CriticalWoundThreshold(
        SquadController attacker,
        SquadController target,
        WeaponData weapon,
        AttackMode mode,
        int current)
    {
        if (attacker == null ||
            target == null ||
            mode != AttackMode.Ranged)
        {
            return current;
        }

        int result = current;

        if (NecronsFactionPack11Runtime
            .HasAugmentation(
                attacker,
                "anti infantry 3") &&
            target.HasKeyword("infantry"))
        {
            result = Mathf.Min(result, 3);
        }

        if (NecronsFactionPack11Runtime
            .HasAugmentation(
                attacker,
                "anti mounted 4") &&
            target.HasKeyword("mounted"))
        {
            result = Mathf.Min(result, 4);
        }

        if (NecronsFactionPack11Runtime
            .HasAugmentation(
                attacker,
                "anti monster 5") &&
            target.HasKeyword("monster"))
        {
            result = Mathf.Min(result, 5);
        }

        if (NecronsFactionPack11Runtime
            .HasAugmentation(
                attacker,
                "anti vehicle 5") &&
            target.HasKeyword("vehicle"))
        {
            result = Mathf.Min(result, 5);
        }

        return result;
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
            roll >= 5 &&
            NecronsFactionPack11Runtime
                .HasFlag(
                    attacker,
                    "enslaved_artifice");
    }

    public static int ConditionalFeelNoPain(
        SquadController unit,
        string label,
        int current)
    {
        if (unit == null)
            return current;

        if (UnitHasEnhancement(
                unit,
                "ENAEGIC DERMAL BOND"))
        {
            return BetterFeelNoPain(
                current,
                4);
        }

        return current;
    }

    private static int BetterFeelNoPain(
        int current,
        int candidate)
    {
        if (candidate <= 0)
            return current;

        if (current <= 0)
            return candidate;

        return Mathf.Min(
            current,
            candidate);
    }

    public static int ModifyIncomingDamage(
        SquadController target,
        int damage)
    {
        if (target == null ||
            damage <= 0)
        {
            return damage;
        }

        if (NecronsFactionPack11Runtime
            .HasFlag(
                target,
                "nanoassembly_protocols") ||
            NecronsFactionPack11Runtime
                .HasFlag(
                    target,
                    "chrono_impedance"))
        {
            return Mathf.Max(
                1,
                damage - 1);
        }

        return damage;
    }

    public static int ModifyLeadership(
        SquadController unit,
        int current)
    {
        return current;
    }

    public static float DetectionRangeBonus(
        SquadController target)
    {
        return
            target != null &&
            NecronsFactionPack11Runtime
                .HasFlag(
                    target,
                    "particle_pulse_target")
            ? 3f
            : 0f;
    }

    public static int ModifyStratagemCost(
        SquadController target,
        string label,
        int current)
    {
        if (target == null)
            return current;

        foreach (NecronGameController controller
            in NecronsFactionPack11Runtime
                .AllControllers())
        {
            if (controller == null ||
                controller.OwnerGame == null ||
                !controller.HasDetachment(
                    NecronDetachment.PantheonOfWoe) ||
                string.Equals(
                    controller.FactionId,
                    target.FactionId,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (SquadController source
                in controller.ArmyUnits)
            {
                if (source == null ||
                    !source.IsAlive ||
                    !source.IsOnBattlefield ||
                    !NameOrKeyword(
                        source,
                        "C'tan Shard of the Deceiver") ||
                    !UnitHasEnhancement(
                        source,
                        "SINGULARITY MATRIX"))
                {
                    continue;
                }

                if (controller.OwnerGame
                    .JoinedDistancePublic(
                        source,
                        target) <= 12.001f)
                {
                    return current + 1;
                }
            }
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

        if (NecronsFactionPack11Runtime
            .HasFlag(
                target,
                "countertemporal_shift") &&
            game.JoinedDistancePublic(
                attacker,
                target) >
                18.001f)
        {
            reason =
                "Countertemporal Shift: this CANOPTEK unit can only be selected as a ranged target within 18 inches.";
            return false;
        }

        return true;
    }

    public static int ModifyReanimationRoll(
        SquadController unit,
        int rolled)
    {
        if (unit == null)
            return rolled;

        if (UnitHasEnhancement(
                unit,
                "RECURSIVE REANIMATION UPGRADE"))
        {
            rolled += 1;
        }

        return Mathf.Max(
            0,
            rolled);
    }

    public static int FixedAdvanceResult(
        SquadController unit)
    {
        if (unit == null)
            return 0;

        if (NecronsFactionPack11Runtime
            .HasFlag(
                unit,
                "advance_fixed_6") ||
            UnitHasEnhancement(
                unit,
                "HYPERSPATIAL TRANSFER NODE"))
        {
            return 6;
        }

        return 0;
    }

    public static bool CanRerollAdvance(
        SquadController unit)
    {
        return
            unit != null &&
            NecronsFactionPack11Runtime
                .HasFlag(
                    unit,
                    "reroll_advance");
    }

    public static float RangeModifier(
        SquadController unit,
        WeaponData weapon,
        AttackMode mode)
    {
        return
            unit != null &&
            mode == AttackMode.Ranged &&
            UnitHasEnhancement(
                unit,
                "GAUNTLET OF COMPRESSION")
            ? 6f
            : 0f;
    }
}
