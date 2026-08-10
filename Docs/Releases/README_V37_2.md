# Warboard v37.2 — YellowScribe Detachment Detection Fix

## What this fixes

v37 could report that a YellowScribe roster's Aeldari detachment was
"ambiguous".

That was a false inference.

YellowScribe's 8-character army code stores its transformed army payload.
Its upstream 10e/11e parser only adds top-level roster selections whose type is
`unit` or `model` to the stored army. Roster-level configuration selections,
such as New Recruit/BattleScribe `Detachment Choice`, are normally not carried
into the code.

Warboard was then scanning arbitrary structural names inside the transformed
unit data and could accidentally match more than one supported detachment.

## v37.2 behaviour

- Only direct string values on explicit detachment-like JSON keys are trusted
  for automatic detachment locking.
- Warboard no longer infers a detachment from arbitrary unit/rule/category
  names in a YellowScribe payload.
- When the YellowScribe code does not contain detachment metadata, the setup UI
  says that the roster-level choice was not preserved and asks for the
  detachment once.
- Deployment remains blocked until that one-time choice is confirmed.
- Once deployment begins, the choice remains locked.
- No polling, reflection, or runtime bridge is added.

## Important limitation

With a normal YellowScribe 8-character code alone, automatic detachment
selection cannot be guaranteed because YellowScribe has already discarded the
roster-level configuration selection before Warboard receives the payload.

A future direct New Recruit/.rosz import path can preserve that information and
auto-lock the detachment.

## Version

Visible in-game header: `WARBOARD v37.2`
