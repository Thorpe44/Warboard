# Warboard v39 - 11e Core Rules Audit + Compliance Pass

Visible build marker: `WARBOARD v39`

This version freezes the architecture and begins the rule-by-rule 11e compliance pass against the June 2026 Core Rules.

## Corrections installed by v39

- Coherency uses 2" horizontal / 5" vertical to at least one model and 9" horizontal / 5" vertical to every other model.
- Engagement uses 2" horizontal / 5" vertical, measured base-to-base.
- Objective range uses 3" horizontal / 5" vertical.
- Objective control is resolved before other end-of-phase/end-of-turn rules and mission scoring.
- Untouched units are selected to Remain Stationary when the Move Units step closes, without emitting move-start/end triggers.
- Mission actions now pass through the generic 11e action eligibility gate.
- The automatic attack resolver now applies Benefit of Cover by worsening BS rather than improving saves.
- The automatic attack resolver now applies Feel No Pain consistently to normal, Devastating and Hazardous damage.

## Audit

See `CORE_RULES_AUDIT_V39.md` for all 25 Core Rules sections. It explicitly marks what is implemented, partial, or missing.

The largest remaining universal-rule blocks after this pass are the 11e Fight-phase step ordering, Transports, full Terrain/visibility categories, remaining Core Stratagems, Flying/Aircraft and generic muster-army legality.

## Install

1. Extract over the Warboard project and replace files.
2. Unity compiles v39.
3. The one-time v39 migration patches the existing local split source files.
4. It validates the changes, writes `Library/WarboardV39CoreRulesReport.txt`, then deletes itself.
5. Unity compiles a second time.
6. Header should read `WARBOARD v39`.

Backups are written under `Library/WarboardBackups/V39/`.
