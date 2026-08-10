# WARBOARD v46 — Deep Audit + Three-Faction Integration

Audit date: 10 August 2026  
Target baseline: WARBOARD v45.7  
Faction sources: Orks Faction Pack 11e v1.1 (July 2026), Tyranids Faction Pack 11e v1.1 (July 2026), Space Marines Faction Pack 11e v1.1 (July 2026; base faction only / no supplements).

## Scope

This pass statically audited the current Warboard source paths that govern roster import, faction detection, movement, reserves, Battle-shock, shooting, interactive attacks, the legacy automatic attack resolver, charge resolution, objectives, transports, faction events and the existing faction-controller layer.

The supplied faction packs were used as the authority for the new Orks, Tyranids and Space Marines content. Crusade and Boarding Actions sections were deliberately excluded.

This is a static source/rules integration audit. Unity is not available in this environment, so the package is syntax/structure checked but still requires Unity to perform the final compile and gameplay smoke test.

No separate Edition 11 Core Rules source was supplied for this pass. Core-rule findings therefore test the current implementation for internal consistency, event/eligibility gaps and interactions exposed by the three supplied faction packs; they are not an independent line-by-line certification of the entire Core Rules publication.

## High-priority audit findings

### 1. The faction-controller layer was only partly modular
`IFactionGameController` and `FactionControllerHost` are a good extension surface, but `FactionGameControllerFactory` still explicitly knew only Aeldari, Necrons and Custodes.

**v46 action:** new factions register through `WarboardFactionExtensionHub`. Core no longer needs a fresh faction-specific branch for each controller. The new shared hooks also require the unit's intrinsic faction keyword, so allied units in the same player army do not accidentally inherit Orks/Tyranids/Space Marine army rules.

### 2. Faction rules are duplicated across two attack pipelines
Warboard currently contains both `RulesEngine` and `InteractiveAttackController`. Existing Aeldari/Custodes/Necron mechanics are repeated across both. That creates a real risk that a faction works in one resolution path but not the other.

**v46 action:** the three new factions use one shared extension hub and both attack paths call the same shared mechanics.

### 3. The legacy `FactionRuleSystem` remains faction-specific debt
`FactionRuleSystem` contains explicit Necron/Aeldari/Custodes profile logic and several older compatibility fields.

**v46 action:** no Orks/Tyranids/Space Marines state was added to this class. New faction state lives in `StandardFactionGameController`.

### 4. `SquadController` contains accumulated faction-specific state
There are many Aeldari/Necron/Custodes presentation/combat fields on the core unit object.

**v46 action:** no new Ork/Tyranid/Space Marine fields are added to `SquadController`. New state stays in the faction controller.

### 5. The event bus is strong enough to build on
The core already defines battle, turn, phase, movement, attack, charge, destruction, objective and transport event types. The audit found that the `UnitDisembarked` event existed in the enum but was not actually raised by the current source.

**v46 action:** the shared faction controller consumes the event bus and v46 publishes disembarkation so rules such as Blitz Brigade can key off the real action.

### 6. Battle-shock correctly excludes off-board units in the current core
The previous “Battle-shock in Reserves” bug is not present in the audited v45.7 command-phase path: current code filters to living on-battlefield units.

**v46 action:** preserve that behavior and extend the dice-count path so Tyranid Synapse can use 3D6 without reintroducing reserve tests.

### 7. New Recruit / YellowScribe is the correct authority for datasheets
The importer retains unit keywords, faction keywords, ability text and per-model weapon profiles.

**v46 action:** the new faction modules operate on imported live datasheet content rather than embedding unit datasheets.

### 8. The text roster manifest does not identify enhancement bearers
The generic manifest stores enhancement names, but not a reliable enhancement-to-character assignment.

**v46 action:** enhancement cards are loaded from the supplied faction pack text. Mechanics that require a specific bearer remain explicit player/manual rules unless the imported game state can identify the bearer safely. Warboard does not guess.

### 9. Several 11e faction effects require information the core does not yet represent per attack/model
Examples include Tyranid Hive Predators granting Precision only on Critical Hits, Space Marine per-target detection states, arbitrary tunnel-marker placement, some “choose one model/unit” effects, and complex reactive placement.

**v46 action:** deterministic effects are automated. Effects that cannot be represented exactly are surfaced through the faction rules/choice layer with source-derived rule text rather than being silently approximated.

### 10. Two existing eligibility inconsistencies were found during the audit
`SelectedUnitCanCharge()` only consulted the Aeldari charge-after-Advance/Fall-Back path, while the later charge resolver also knew about Necron and Custodes exceptions. This could make a legal unit appear unable to charge before the resolver was even reached.

The indirect-fire shooting branch also required `!HasAdvanced` before consulting faction Advance-and-shoot permissions, making the later permission checks ineffective in that branch.

**v46 action:** both paths are normalised and the new shared faction permissions are included at the same time.

## New shared module

`StandardFactionGameController` is the stateful rules controller for post-v45 standard faction packs.

The main Warboard Stratagem menu now recognises these shared faction controllers, previews the selected Detachment cards and links into the full scrollable faction card panel for CP spend/logging.

`WarboardFactionExtensionHub` is the common Core-facing hook for:
- faction detection/controller creation
- attack modifiers
- granted core weapon abilities
- Hit/Wound rerolls
- Strength/AP
- Sustained/Lethal/Devastating/Precision
- invulnerable-save overrides
- movement/Advance/Charge permissions
- fixed Advance results
- Charge modifiers/rerolls
- Battle-shock dice count
- Synapse range checks
- future detection modifiers

The new packs are data-driven JSON in `Assets/Resources/FactionPacks11`.

## Orks coverage

Faction rule:
- WAAAGH! — once per battle; charge after Advance, +1 melee Strength/Attacks and 5++ while active.
- Bully Boyz second Waaagh! is separately tracked and restricted to WARBOSS/NOBZ/MEGANOBZ.

13 matched-play detachments are included:
War Horde, Da Big Hunt, Kult of Speed, Dread Mob, Green Tide, Bully Boyz, Freebooter Krew, Speedwaaagh!, Blitz Brigade, More Dakka!, Rollin’ Deff, Taktikal Brigade, Equatorial Hordes.

All 44 enhancements and 66 stratagem cards from the supplied pack are included in the faction browser. Deterministic detachment mechanics are automated where Core has an exact hook; choice-heavy/placement-heavy effects use source-card/manual resolution.

## Tyranids coverage

Faction rules:
- SYNAPSE — units within 6" of a friendly SYNAPSE model use 3D6 for Battle-shock and gain +1 melee Strength.
- SHADOW IN THE WARP — once per battle, all enemy battlefield units test Battle-shock; -1 if within 6" of the Tyranid player’s SYNAPSE; defending Tyranids in Synapse still use 3D6. The special test does not offer Insane Bravery.

10 matched-play detachments are included:
Invasion Fleet, Crusher Stampede, Unending Swarm, Assimilation Swarm, Vanguard Onslaught, Synaptic Nexus, Subterranean Assault, Ambush Predators, Talons of the Norn Queen, Warrior Bioform Onslaught.

All 34 enhancements and 51 stratagem cards from the supplied pack are included.

## Space Marines coverage

Base ADEPTUS ASTARTES only. Supplement armies are intentionally blocked rather than silently treated as base Space Marines. The base pack's “one Chapter only” army restriction is also validated from imported faction keywords before deployment.

Faction rule:
- OATH OF MOMENT — enemy target selected at the start of the Space Marine Command phase; Hit rerolls against that target, plus the no-supplement Codex wound bonus.

16 matched-play detachments are included:
Gladius Task Force, Anvil Siege Force, Ironstorm Spearhead, Firestorm Assault Force, Stormlance Task Force, Vanguard Spearhead, 1st Company Task Force, Bastion Task Force, Orbital Assault Force, Ceramite Sentinels, Armoured Speartip, Headhunter Task Force, Vengeful Hosts, Fulguris Task Force, Librarius Conclave, Subversion Assets.

All 59 enhancements and 81 stratagem cards from the supplied no-supplements pack are included.

Gladius Combat Doctrines, Librarius Psychic Disciplines, Oath target selection, Orbital Assault Rapid-drop unit selection and other persistent faction choices have explicit UI/state. Mandatory start-of-round/Command choices are also phase-gated so keyboard/toolbar phase advancement cannot silently skip Oath, Prey, Loot, Hyper-adaptation or Psychic Discipline selections.

## Known deliberate manual/source-card interactions

The following categories are not guessed:
- arbitrary marker placement (e.g. tunnel markers)
- rules requiring a specific enhancement bearer when the roster manifest does not identify that bearer
- per-individual-critical-hit Precision where the current volley model stores Precision at pool level
- rules requiring a target-selection state the core does not currently retain (certain detection/scan mechanics)
- complicated “move through X but not Y / place as close as possible” effects without a matching legal-placement solver
- reactive Stratagems whose full timing/target state is not represented by a current event
- datasheet-specific choices whose exact option state is not present in imported data

Those rules remain present as source-derived cards in the faction UI and Battle Log workflow so Traditional play remains exact and XCOM play never invents an effect.

## Validation performed

- faction JSON parsed successfully
- no Unicode replacement-character corruption in generated faction data
- expected matched-play counts validated:
  - Orks: 13 detachments / 44 enhancements / 66 stratagems
  - Tyranids: 10 / 34 / 51
  - Space Marines: 16 / 59 / 81
- generated C# files passed lexical brace/string/comment structural checks
- shared faction effects are guarded by intrinsic faction keyword so allied units do not inherit the army rule by player-slot association
- mandatory faction choices have an explicit phase-advance gate
- installer is idempotent and keeps backups under `Library/WarboardBackups/V46ThreeFactions`
- no v45 presentation/table/dice/tray source is intentionally removed

## Required post-install smoke test

Unity must perform the final compile. Then run one minimal setup per new faction and verify:
1. YellowScribe army loads.
2. The new faction setup modal appears.
3. New Recruit pasted roster auto-detects the detachment, or manual selection locks correctly.
4. Deployment begins only after required pre-game faction choices are complete.
5. One full Command → Move → Shoot → Charge → Fight cycle works.
6. Traditional and XCOM modes both reach the same legal game state for deterministic effects.
7. Battle Log records faction-rule choices and manually resolved source-card effects.


## Asset/model note

This v46 package is a rules/controller/data integration. No Orks, Tyranids or Space Marines miniature model pack was supplied in the three rule PDFs, so v46 does not fabricate faction model assets. Imported armies continue to use Warboard's existing model-visual registry/resolver/fallback behavior until dedicated model packs are added.

## Automation boundary

A source-card being present in the faction browser does not mean every effect on that card is silently simulated in XCOM mode. `SPEND + LOG` records the CP spend and exact rule text; deterministic mechanics explicitly listed in the relevant controller/hub are applied automatically. Choice/placement/timing mechanics without a safe Core representation remain manual/source-card resolution. This is intentional: the audit preferred an explicit manual rule over an incorrect automatic approximation.
