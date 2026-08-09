# Warboard v39 - Warhammer 40,000 11e Core Rules Audit

Source of truth: **Warhammer 40,000 Core Rules, Edition 11, June 2026**.

Status key:
- **Implemented** - Warboard has a direct rules implementation suitable for ordinary play.
- **Partial** - important parts exist, but one or more 11e requirements are still absent or simplified.
- **Missing** - no general-purpose implementation was found in the current rules engine.
- **Data/UI only** - represented in state or UI, but not fully enforced as gameplay rules.

This audit deliberately separates the stability of Warboard's architecture from completeness of the 11e rules. The architecture is frozen; remaining work belongs inside the existing core/faction systems.

## 01 Core Concepts - PARTIAL -> improved in v39

Implemented before v39:
- units/models and friendly/enemy ownership
- D6/2D6-style rolls
- leadership and Battle-shock state
- Battle-shocked OC suppression, Stratagem restrictions and mission-action interaction

Corrected in v39:
- coherency now uses base-to-base **2" horizontal / 5" vertical** to at least one model and **9" horizontal / 5" vertical** to every other model
- engagement now uses base-to-base **2" horizontal / 5" vertical**

Remaining:
- end-of-turn coherency model removal still needs a player-choice flow; Warboard continues to prevent illegal Movement-phase endings, but casualties later in the turn can still require a dedicated removal prompt
- a few rare sequencing/"treated as" dice concepts are handled ad hoc rather than by one generic resolver

## 02 Datasheets - PARTIAL

Implemented:
- M, T, Sv, InSv, W, Ld, OC
- weapons and weapon characteristics
- faction/other keywords
- abilities and datasheet-rule text import
- wargear/loadout representation

Remaining:
- characteristic-modifier ordering/precedence is distributed across systems rather than one canonical modifier pipeline
- some model-specific mixed-profile cases remain approximate because YellowScribe/New Recruit source data can be incomplete

## 03 Moving - PARTIAL -> improved in v39

Implemented:
- Normal, Advance and Fall Back movement framework
- board bounds, collisions, movement distance and coherency checks
- reserves/setup movement
- direct move events

Corrected in v39:
- when the Movement phase is closed, untouched active-player units are resolved as being selected to **Remain Stationary**
- Remain Stationary emits the unit-selection timing but deliberately does **not** emit MoveStarted/MoveEnded, matching 11e
- coherency/engagement geometry now has the 5" vertical component

Remaining:
- full 11e Desperate Escape mode (per-model hazard rolls + Battle-shock interaction) needs a dedicated Traditional/XCOM choice flow
- full vertical terrain traversal is still simplified
- generic FLY / Take to the Skies movement is not complete
- Super-heavy Walker movement options are not complete

## 04 Making Attacks - PARTIAL

Implemented:
- weapon selection and separate model loadouts
- ranged/melee target selection
- multi-weapon attack resolution
- target visibility/range checks
- Precision allocation support

Remaining:
- the 11e "identical attacks" grouping/target declaration sequence is represented functionally but not as a fully explicit declaration UI
- split melee target declaration remains less expressive than the tabletop sequence

## 05 Attack Sequence - IMPLEMENTED for common attacks, with exceptions

Implemented:
- Hit, Wound, Save, Damage sequence
- Strength vs Toughness wound table
- invulnerable saves
- command rerolls in the interactive pipeline
- critical hits/wounds and attack modifiers
- interactive/manual dice flow

Corrected in v39:
- automatic attack resolver now treats **Benefit of Cover as worsening BS by 1**, not as improving armour saves
- automatic attack resolver now applies Feel No Pain to normal, Devastating and Hazardous damage consistently with the interactive resolver

Remaining:
- some rare simultaneous-damage/allocation edge cases still need dedicated test cases

## 06 Other Concepts - PARTIAL

Implemented:
- visibility framework
- mortal wounds
- Hazardous/Hazard rolls
- Deadly Demise support

Remaining:
- visibility is constrained by Warboard's simplified terrain model
- mortal-wound player allocation is mostly automatic rather than always exposing the controlling player's model choice

## 07 Battle Round - IMPLEMENTED

Implemented:
- battle rounds and player turns
- start/end round and turn events
- five-round normal battle structure

## 08 Command Phase - IMPLEMENTED for core sequence

Implemented:
- Command phase
- both players gain 1 Core CP each Command phase
- Battle-shock step for currently shocked / half-strength units
- command ability window and phase events

Remaining:
- generic simultaneous command-ability sequencing UI can be improved as more factions are implemented

## 09 Movement Phase - PARTIAL -> improved in v39

Implemented:
- phase sequencing
- battlefield and reserve unit handling
- Normal/Advance/Fall Back
- setup/ingress framework

Corrected in v39:
- every untouched unit is selected to Remain Stationary as the Move Units step closes

Remaining:
- Desperate Escape modes/tests
- full Disembark/Ingress mode parity
- generic Transport movement integration

## 10 Shooting Phase - PARTIAL

Implemented:
- Normal shooting
- Assault restrictions
- Close-quarters/Pistol handling
- Monster/Vehicle engaged shooting modifiers
- Indirect Fire
- Snap Shooting / Fire Overwatch flow
- shooting eligibility and attack resolution

Corrected in v39:
- automatic-mode cover handling now matches 11e BS degradation

Remaining:
- some mixed Close-Quarters weapon declaration edge cases need tests
- action lockout after shooting is mission-system dependent rather than one universal action state

## 11 Charge Phase - PARTIAL

Implemented:
- charge eligibility
- 2D6 charge roll
- target/range checks
- charge movement and direct events
- Advance/Fall Back restrictions with faction overrides

Remaining:
- full multi-target 11e charge declaration/movement constraints need additional tests
- Heroic Intervention core Stratagem is not yet implemented

## 12 Fight Phase - PARTIAL - major remaining core item

Implemented:
- Fights First and remaining-fighter priority framework
- pile-in movement
- melee attacks
- consolidation movement
- charge/Fights First state

Major 11e gap:
- Warboard's current fight activation still derives from the older per-activation "pile in -> attack -> consolidate" shape. 11e resolves a phase-wide **Pile In step**, then **Fight step**, then **Consolidate step**. This needs a dedicated gameplay refactor inside `GameController.Fight.cs`.
- Counteroffensive 2CP is not yet implemented.

This is the highest-priority core-rules item after v39.

## 13 Terrain - PARTIAL - major remaining core item

Implemented:
- blocking/cover/traversable world features
- basic line-of-sight blocking
- cover queries

Corrected in v39:
- Benefit of Cover is treated as BS degradation in the automatic attack resolver, consistent with 11e

Remaining:
- 11e Exposed / Light / Dense categories
- Hidden and default 15" detection range
- Obscuring
- Solid
- Gone to Ground interactions
- full vertical movement and terrain-surface legality

## 14 Objectives - IMPLEMENTED for common battlefield objectives -> improved in v39

Implemented:
- OC totals
- contested objectives
- secured objectives
- mission roles and scoring integration

Corrected in v39:
- objective range now enforces **3" horizontal / 5" vertical**
- secured objective/control state is resolved before other end-of-phase and end-of-turn rules/scoring

## 15 Stratagems - PARTIAL - major remaining core item

Implemented:
- CP resource and spending
- Battle-shock target restriction
- once-per-phase tracking infrastructure
- Command Re-roll in attack/charge flows
- Fire Overwatch / Snap Shooting flow

Missing or incomplete core Stratagems:
- Epic Challenge
- Insane Bravery
- Explosives
- Crushing Impact
- Rapid Ingress as a Stratagem choice/timing flow
- Smokescreen
- Heroic Intervention
- Counteroffensive

Command Re-roll also needs all listed 11e trigger types consolidated through one generic trigger system rather than several specialised paths.

## 16 Actions - PARTIAL -> improved in v39

Implemented:
- mission action definitions/state
- action starts/completion and mission scoring

Corrected in v39:
- generic action gate now rejects units that are off battlefield, AIRCRAFT/FORTIFICATION, Battle-shocked, have no model with OC 1+, are engaged unless TITANIC, Advanced/Fell Back, or already started an action this turn

Remaining:
- universal action failure on every non-pile-in/non-consolidation move or leaving battlefield needs central event-driven enforcement
- universal post-start shooting/charge restrictions should move out of mission-specific paths

## 17 Monsters and Vehicles - PARTIAL

Implemented:
- Monster/Vehicle keywords
- engaged shooting / Big Guns-style hit penalties
- weapon restrictions such as Blast while engaged

Remaining:
- complete move-through-model rules
- Frame handling
- full Super-heavy Walker rules

## 18 Transports - MISSING / skeletal

Remaining:
- transport capacity enforcement
- Dedicated Transport prebattle passenger requirement
- embark
- Rapid/Tactical/Combat disembark modes
- Emergency Disembark
- embarked-unit eligibility/state
- Firing Deck integration

## 19 Attached Units - PARTIAL

Implemented:
- Leader/bodyguard attachment
- bodyguard protection/allocation concepts
- combined action unit and keyword/rule handling

Remaining:
- separate **Support** attachment slot (11e permits one Leader and one Support unless stated otherwise)
- full union-keyword/model-keyword distinctions for every rule
- all attachment destruction/separation edge cases

## 20 Strategic Reserves - PARTIAL

Implemented:
- reserve state
- round-gated arrival
- edge setup and >8" enemy exclusion
- opponent deployment-zone restriction before round 3
- round-3 destruction of unarrived non-repositioned reserves
- repositioned reserve state

Remaining:
- generic prebattle **50% army-points** reserve cap needs per-unit points integrated into the authoritative roster manifest
- final-turn reserve destruction and transport-passenger exceptions need a dedicated rules test pass

## 21 Flying and Surging - PARTIAL

Implemented:
- special/surge movement infrastructure used by faction rules

Remaining:
- generic Surge move eligibility and target-priority enforcement
- Take to the Skies decision
- -2" maximum movement while flying (Hover exception)
- move-through-model/terrain flying rules

## 22 Other Rules and Abilities - PARTIAL

Implemented:
- aura/datasheet/faction rule infrastructure
- Psychic and Wargear text can be imported
- faction-controller event system

Remaining:
- generic aura boundary and persistence tests
- Plunging Fire
- some ability duration/return-from-reserves edge cases

## 23 Aircraft - MISSING / skeletal

Remaining:
- mandatory strategic reserves before battle
- ingress-only movement
- return to reserves at end opponent turn
- movement through Aircraft
- Aircraft pile-in/consolidation/surge target exceptions
- charging/fighting restrictions and FLY-only melee interactions

## 24 Core Abilities - PARTIAL, many common abilities implemented

Implemented or substantially implemented:
- Anti
- Assault
- Blast
- Close-Quarters / Pistol
- Deadly Demise
- Deep Strike
- Devastating Wounds
- Feel No Pain
- Fights First
- Hazardous
- Heavy
- Ignores Cover
- Indirect Fire
- Lance
- Lethal Hits
- Melta
- Precision
- Rapid Fire
- Stealth
- Sustained Hits
- Torrent
- Twin-linked

Partial/missing:
- Cleave
- Extra Attacks
- Firing Deck
- Hover
- Infiltrators deployment
- Leader + Support full implementation
- Lone Operative
- One Shot full enforcement audit
- Scouts/prebattle Scout move
- Super-heavy Walker
- Psychic is largely a keyword/category rather than a generic mechanical ability

## 25 Muster Armies - PARTIAL

Implemented:
- battle size/points selection
- roster import
- faction selection through roster data
- Aeldari multi-detachment DP system from v38
- Warlord/config metadata via pasted New Recruit roster

Remaining generic core validation:
- unit-copy limits (Incursion 2 / Strike Force 3, doubled for Battleline/Dedicated Transport)
- Epic Hero one-copy rule
- enhancement limits and duplicate-enhancement enforcement for every faction
- generic detachment restrictions beyond Aeldari
- prebattle strategic-reserve 50% points check
- Support attachment selection

# v39 conclusion

v39 fixes several rule-level correctness issues without reopening Warboard's architecture. It does **not** claim that all 91 pages are now fully automated.

The largest remaining universal-rule implementation blocks are:
1. 11e Fight-phase step ordering
2. Transports
3. full 11e Terrain/visibility categories
4. remaining Core Stratagems
5. Flying/Aircraft
6. generic Muster-army legality and Support attachments

Those can now be implemented as ordinary rule modules inside the frozen architecture rather than by restructuring GameController again.
