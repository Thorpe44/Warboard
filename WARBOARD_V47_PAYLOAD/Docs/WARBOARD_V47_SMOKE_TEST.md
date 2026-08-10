# WARBOARD v47 — Smoke Test

Run this after Unity compiles v47. The purpose is to prove the new generic systems before a full battle.

## 1. Baseline regression

- Load one previously working v46 match.
- Confirm deployment, normal movement, shooting, charge and phase advance still work.
- Confirm Aeldari/Custodes/Necrons still load if used; v47 does not replace their faction controllers.

## 2. Enhancement bearer gate

With an Orks/Tyranids/Space Marines roster that contains an Enhancement:

- open the faction rules panel -> `ENHANCEMENTS`;
- verify the taken Enhancement appears as requiring a bearer;
- verify deployment is blocked while it remains unassigned;
- assign its legal bearer;
- verify deployment can proceed once other pre-game requirements are satisfied.

## 3. Tyranids — Tunnel Marker

Use `SUBTERRANEAN ASSAULT` and a BURROWER arriving from Reserves:

- confirm a 40 mm Tunnel Marker placement prompt appears;
- illegal points >1" from the BURROWER or <=3" from an enemy are rejected;
- legal placement creates a physical marker;
- move a non-AIRCRAFT enemy unit to end within 3" and confirm the marker disappears.

If two Tunnel Markers exist, select an eligible Tyranid unit wholly within 9" of one and use `TUNNEL NETWORK` from the faction Stratagem panel. Confirm the destination must be another marker and the final unit position must be wholly within 9" and >6" from enemies.

## 4. Tyranids — Hive Predators Precision

Use Invasion Fleet and select `HIVE PREDATORS`, then attack a CHARACTER target.

- ordinary successful Hits should not globally turn the volley into Precision;
- Critical Hits should retain Precision provenance;
- in the staged attack resolver, mixed Precision/non-Precision saves should use the corresponding allocation rules.

## 5. Space Marines — Bastion state chain

Use `BASTION TASK FORCE`:

- attack with a BATTLELINE unit and score at least one hit;
- confirm the target becomes auspex scanned;
- confirm later attacks into that target receive the detachment Hit re-roll interaction;
- test `CODEX DISCIPLINE`;
- test `GUIDED DISRUPTION` against a non-MONSTER/non-VEHICLE and confirm -2 Move / -2 Charge persists until the start of your next turn;
- test `SHOCK BOMBARDMENT` and confirm -1 Hit persists for the correct duration;
- test `HERESY UNDONE`: after Advance/Fall Back, only scanned targets should be accepted.

Traditional mode should ask whether the manually resolved BATTLELINE attack actually scored a hit before creating the scanned state.

## 6. Space Marines — Subversion detection

Use `SUBVERSION ASSETS`:

- in your Shooting phase, select a PHOBOS/Scout source and choose `DETECT TARGET`;
- only visible enemy units within 12" should be offered;
- a detected hidden target should gain +3" detection range;
- at the start of your opponent's Movement phase, verify the `CLOAKED POSITION` reaction window appears for eligible unengaged PHOBOS/Scout units;
- accepting it should spend CP and apply -3" detection range through the live Hidden visibility calculation.

## 7. Attack-regression check

Test both XCOM/automatic and staged interactive attacks with:

- a normal non-Precision weapon;
- a normal Precision weapon;
- a Critical-Hit-only Precision effect;
- Lethal Hits;
- Devastating Wounds;
- a Command Re-roll on hit/wound/save.

Confirm the attack reaches completion and casualty selection still works.

## If Unity reports a compile error

Do not make manual edits first. Capture the first red compiler error and its file/line. The v47 installer keeps the previous files in:

`Library\WarboardBackups\V47RulesEngine`
