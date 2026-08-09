# Warboard v41 — Core Rules Completion

v41 is the single final 11e core-rules completion pass. It is intended for the current v40 split-GameController project.

## Install

1. Extract this ZIP over the Warboard project root and replace files when asked.
2. Return to Unity and let the first compile finish.
3. The v41 editor migration writes the direct runtime files and patches the owning split GameController/Squad/attack files.
4. Unity will compile a second time.
5. The visible header must read `WARBOARD v41`.

Do not restart Unity between the two compiles. If Unity does not refresh automatically, use **Assets → Refresh** once.

## What v41 completes

- 11e terrain categories and movement/visibility integration, Benefit of Cover, Hidden/detection, Gone to Ground approximation, and Plunging Fire.
- Transport state, capacity/restriction parsing, Embark, Rapid/Tactical/Combat Disembark, Emergency Disembark, and Dedicated Transport formation handling.
- Take to the Skies for FLY units and AIRCRAFT deployment/movement/charge/fight lifecycle rules.
- Core Stratagem completion around the existing Command Re-roll and Fire Overwatch systems: Epic Challenge, Insane Bravery, Explosives, Crushing Impact, Rapid Ingress, Smokescreen, Heroic Intervention, and Counteroffensive.
- Muster validation for battle-size point limits, enhancement limits represented by roster metadata, Warlord presence, datasheet copy limits, BATTLELINE/DEDICATED TRANSPORT doubling, and EPIC HERO limits.
- Final integration audit against the 25 core-rule sections.

## Architecture

The installer is an idempotent editor-time migration only. The resulting systems are direct runtime code (`CoreRules11Completion.cs` and `GameController.CoreCompletion11.cs`) plus direct edits to the owning classes. It remains in `Assets/Editor` so a failed/partial migration can be re-run safely; once v41 is installed it becomes a no-op. No runtime bridge or reflection shim is installed.

Backups of changed source files are written to `Library/WarboardBackups/V41/`. The migration report is written to `Library/WarboardV41CoreRulesCompletionReport.txt`.

## Source-data boundary

Rules that depend on faction/datasheet-specific metadata can only be enforced when that metadata exists in the imported roster/datasheet text. v41 deliberately does not invent missing transport capacities, Upgrade classifications, or faction restrictions.
