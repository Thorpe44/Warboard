# Warboard v44 — Full Necrons Faction Rules

Visible build marker: `WARBOARD v44`

This release implements the standard matched-play content from Necrons Faction
Pack 11e v1.1 (July 2026) as one faction pass. Crusade and Boarding Actions are
not mixed into normal Warboard battles.

## Army rule

- Reanimation Protocols at the end of the controlling player's Command phase.
- D3 wounds are reanimated using Warboard's existing heal/return-model engine.
- Reanimation modifiers from faction rules and upgrades are applied by the
  v44 faction runtime.

## Detachments

- Awakened Dynasty — 3DP — DYNASTY
- Annihilation Legion — 2DP
- Canoptek Court — 3DP
- Obeisance Phalanx — 2DP
- Hypercrypt Legion — 2DP — HYPERCRYPT
- Starshatter Arsenal — 3DP
- Cryptek Conclave — 2DP
- Cursed Legion — 2DP
- Pantheon of Woe — 2DP
- Hand of the Dynasty — 1DP — DYNASTY
- Skyshroud Spearhead — 1DP
- The Phaeron's Armoury — 1DP — HYPERCRYPT

The normal Detachment Point allowance is enforced, including the Incursion
single-3DP exception. DYNASTY cannot be combined with another DYNASTY
Detachment and HYPERCRYPT cannot be combined with another HYPERCRYPT
Detachment.

## Faction content

- 12 Detachment rules
- 63 standard matched-play Necrons Stratagems
- 42 Enhancements / Upgrades / Necrodermal Bindings
- New Recruit pasted-roster detachment detection
- Manual multi-detachment fallback
- Selected-detachment Stratagem cards in the normal Stratagem UI

## Gameplay integration

v44 integrates deterministic faction effects into the existing core systems,
including attack modifiers, critical hits/wounds, re-rolls, Strength/AP,
Sustained/Lethal/Devastating effects, Rapid Fire, movement, fixed Advance,
Advance/Fall Back permissions, charge modifiers/re-rolls, Objective Control,
Deep Strike/Infiltrators/Scouts/Stealth, range changes, Reanimation modifiers,
incoming Damage changes, detection range, Power Matrix, Cosmic Distortion,
Worthy Foes, Cold Fervour and selected reactive Stratagem timings.

Rules that require an arbitrary player choice, exact model placement, a
specific enemy selection or other information Warboard cannot infer safely are
shown through the existing Traditional/rule-choice flow with their faction-pack
WHEN/TARGET/EFFECT text instead of being guessed.

## Install

1. Extract over the working Warboard v43.4 project and choose Replace files.
2. Let Unity compile.
3. The one-time v44 installer adds the Necrons hooks to the frozen core.
4. It validates 12 Detachments / 63 Stratagems / 42 Enhancements-Bindings.
5. It writes `Library/WarboardV44NecronsFactionRulesReport.txt`.
6. It removes itself and Unity compiles once more.

Core files changed by the one-time installer are backed up under:
`Library/WarboardBackups/V44/`
