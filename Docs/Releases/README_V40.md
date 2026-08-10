# Warboard v40 — 11e Fight Phase Compliance

Visible header: `WARBOARD v40`

v40 replaces the old per-unit `pile in -> attacks -> consolidate` activation
with the Edition 11 phase-wide Fight sequence:

1. Start of Fight phase
2. Pile In
3. Fight
4. Consolidate
5. End of Fight phase

## Pile In step

- The player whose turn it is resolves all optional pile-ins first.
- The opponent then resolves all optional pile-ins.
- Each unit can pile in at most once in the step.
- Eligible units are engaged units and units that made a charge move.
- Engaged units select every enemy they are engaged with.
- Unengaged eligible units can select one or more enemies within 5".
- Base-contact models are locked.
- Moved models must finish closer to the closest selected target.
- The unit must end engaged and preserve required start-of-move engagements.

## Fight step

- Fights First begins with the player whose turn it is.
- Players alternate selecting eligible Fights First units.
- The sequence then moves to Remaining combats using the correct hand-off.
- If a Fights First unit becomes newly eligible during Remaining combats,
  priority returns to Fights First.
- Eligibility includes units currently engaged, units engaged at the start of
  the Fight step, and units that made a charge move this turn.
- Normal Fight and Overrun Fight are separate fight types.
- Overrun Fight performs its additional pile-in before attacks.
- Charging units count as Fights First.
- Fight completion no longer starts consolidation immediately.

## Consolidate step

- The player whose turn it is resolves all optional consolidations first.
- The opponent then resolves theirs.
- A unit can consolidate if it was eligible to fight this phase.
- Ongoing, Engaging and Objective Consolidation modes are represented.
- Engaging Consolidation can select one or more enemy targets.
- Objective Consolidation can select among eligible objectives.
- `New Foes To Face` is represented: newly engaged enemy units that have not
  fought are selected to fight one at a time before consolidation resumes.

## UI changes

The Fight phase action bar now changes by step:

- `DONE SIDE PILE-IN`
- Fight priority / Fights First / Remaining
- `NO LEGAL FIGHT` when an eligible unit cannot resolve a fight
- `OBJECTIVE CONSOLIDATE`
- `DONE SIDE CONSOLIDATE`
- `FIGHT COMBAT STEPS COMPLETE`

`NEXT PHASE` is blocked until all three combat steps are complete.

## Compatibility

The existing model-by-model combat movement and melee attack tools are reused.
The one-time Editor migration redirects the old Fight methods to the new v40
state machine, validates the result, writes a report, then deletes itself.

Backups:
`Library/WarboardBackups/V40/`

Report:
`Library/WarboardV40FightPhaseReport.txt`

## Install

Extract over the Warboard project and replace files.

Unity should compile once, run the v40 migration, then compile a second time.
Afterward the header should read `WARBOARD v40`.
