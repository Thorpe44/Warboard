# Warboard v42 — Full Aeldari Faction Rules

Visible build marker: `WARBOARD v42`

This is the full Edition 11 Aeldari faction-rules pass against Aeldari Faction
Pack v1.1 (July 2026).

## Included

- Battle Focus and all six Agile Manoeuvres.
- All 15 Aeldari detachments.
- Multi-detachment rule stacking through the existing DP system.
- All 15 detachment rules.
- All 78 faction Stratagems with CP cost, WHEN, TARGET, EFFECT and restrictions.
- All 52 Enhancements with points and rule text.
- Selected-detachment Stratagem cards are usable in the normal Warboard
  Stratagem UI.
- Reactive Stratagems open automatically when the core event stream provides
  the exact timing window; their cards remain manually usable as a Traditional
  fallback for player-declared sub-step timings.
- Deterministic attack, movement, objective, reserve, keyword and defensive
  effects are integrated into the existing core systems.
- Rules requiring a genuine player choice, physical dice result, model
  placement, Aspect Shrine-token choice or other information Warboard cannot
  infer are surfaced at their correct rule window using the existing
  Traditional/manual tools rather than guessed.

## Battle Focus

The Edition 11 token pool is:
- Incursion: 2
- Strike Force: 4
- Onslaught: 6

Warhost adds 1 token each battle round, improves Swift as the Wind by another
1", and adds 1 to Agile Manoeuvre D6 results.

The same unit cannot perform two Agile Manoeuvres in one phase. The same named
manoeuvre is once per phase unless the faction pack says otherwise. Swift as
the Wind remains repeatable for different units. Unspent tokens are lost at the
end of the battle round.

## Detachments

- Warhost
- Windrider Host
- Spirit Conclave
- Guardian Battlehost
- Ghosts of the Webway
- Devoted of Ynnead
- Seer Council
- Aspect Host
- Armoured Warhost
- Fateful Performance
- Path of the Outcast
- Twilight Flickers
- Serpent's Brood
- Eldritch Raiders
- Corsair Coterie

The v38 multi-detachment selection/DP layer remains the authority for which
rules are active.

## Traditional mode

Warboard does not replace a player-required tabletop choice with a hidden
automatic guess.

Examples include:
- selecting one of several legal enemy targets;
- choosing Aspect Shrine-token options;
- physical D6/D3 results;
- exact revive/return-model placement;
- rules that alter a datasheet choice such as Cruel Amusement.

When necessary, Warboard records the CP/rule state and opens the existing
manual rule/dice flow with the exact faction-pack rule.

## Install

1. Extract this ZIP over the Warboard project and replace files.
2. Unity compiles once.
3. The one-time v42 source migration integrates the faction pack into the
   current v41 split core source.
4. The migration validates 78 Stratagems / 52 Enhancements and the key runtime
   hooks.
5. It writes `Library/WarboardV42AeldariFactionRulesReport.txt`.
6. The migration deletes itself.
7. Unity compiles a second time.

Backups of files modified by the migration are written to:
`Library/WarboardBackups/V42/`
