# Warboard v43 — Full Adeptus Custodes Faction Rules

Visible build marker: `WARBOARD v43`

This release implements the standard Edition 11 Adeptus Custodes Faction Pack
v1.1 (July 2026) as one complete faction gameplay pass.

Crusade and Boarding Actions are not included because they are separate game
modes rather than the normal faction rules used by Warboard battles.

## Included

- Martial Ka'tah:
  - Dacatarai — melee Sustained Hits 1.
  - Rendax — melee Lethal Hits.
- 9 standard detachments.
- 45 standard faction Stratagem entries.
- 30 standard Enhancements.
- Detachment Point validation and multi-detachment stacking.
- ARMOURY and LIONS mutual-exclusion tags.
- New Recruit roster-text detachment detection plus manual fallback.
- Custodes faction setup/lock UI.
- Solar Spearhead selection of up to two WALKER models to gain CHARACTER.
- Selected-detachment Stratagem cards in the normal Stratagem UI.

## Detachments

- Talons of the Emperor — 3DP
- Shield Host — 2DP
- Null Maiden Vigil — 2DP
- Auric Champions — 2DP
- Solar Spearhead — 2DP — ARMOURY
- Lions of the Emperor — 2DP — LIONS
- Might of the Moritoi — 1DP — ARMOURY
- Silent Hunters — 1DP
- Tharanatoi Hammerblow — 1DP — LIONS

The existing Warboard Detachment Point allowance is enforced, including the
Incursion exception that permits one 3DP detachment.

## Gameplay integration

The faction rules hook directly into Warboard's existing core systems for
attack modifiers, critical hits, Lethal/Sustained/Precision/Lance, Strength,
AP, Damage, Attacks, Rapid Fire, Blast, re-rolls, Feel No Pain, incoming
Damage, movement, Advance/Charge modifiers, Fall Back/Advance permissions,
reserves/ingress, Objective Control, Battle-shock, detection range, dynamic
keywords/core abilities, Stratagem costs and reactive timings.

Where the faction pack requires a real player choice, exact model placement,
a selected enemy, an arbitrary once-per-battle ability choice, or another
result Warboard cannot infer safely, the rule is surfaced through the existing
choice/Traditional manual flow rather than guessed.

## Install

1. Extract over the existing Warboard v42 project.
2. Replace files.
3. Let Unity compile.
4. The one-time v43 migration integrates Custodes into the current split core.
5. It validates the catalogue and installed gameplay hooks.
6. It writes `Library/WarboardV43CustodesFactionRulesReport.txt`.
7. It deletes itself and Unity compiles again.

Backups are stored under `Library/WarboardBackups/V43/`.

Afterward the visible header should read `WARBOARD v43`.
