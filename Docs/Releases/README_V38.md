# Warboard v38 — Pasted Rosters + 11e Multi-Detachment Architecture

Visible build marker: `WARBOARD v38`

## What v38 changes

v38 corrects Warboard's Aeldari roster architecture for Warhammer 40,000 11e.

The army no longer has one globally active detachment. A faction controller can
now own multiple simultaneously selected detachment controllers.

Example:

AeldariGameController
- Devoted of Ynnead controller (2DP)
- Armoured Warhost controller (1DP)

Both controllers receive the same core game events and both detachment rule
sets are considered active.

## New Recruit text is configuration authority

The Aeldari pre-game window now contains a large `NEW RECRUIT ROSTER TEXT`
field.

Paste the normal text export, for example:

- FACTION KEYWORD: Xenos - Aeldari
- DETACHMENT: Devoted of Ynnead (Strength From Death)
- FORCE DISPOSITION: Priority Assets
- TOTAL ARMY POINTS: 1085pts
- WARLORD: Char1: Yvraine
- ENHANCEMENT:
- NUMBER OF UNITS: 8

Press `APPLY PASTED ROSTER`.

Warboard parses and stores:
- faction keyword
- one or more detachments
- force disposition
- total army points
- warlord
- enhancements
- declared unit count
- unit names visible in the text export

The pasted text controls roster configuration. YellowScribe remains responsible
for detailed unit characteristics, datasheets and weapon profiles.

## Multiple detachments and DP

Supported Aeldari detachment costs:

- Warhost — 3DP
- Windrider Host — 2DP
- Spirit Conclave — 2DP
- Guardian Battlehost — 2DP
- Ghosts of the Webway — 2DP
- Devoted of Ynnead — 2DP
- Seer Council — 2DP
- Aspect Host — 3DP
- Armoured Warhost — 1DP
- Fateful Performance — 1DP
- Path of the Outcast — 1DP
- Twilight Flickers — 1DP
- Serpent's Brood — 2DP
- Eldritch Raiders — 2DP
- Corsair Coterie — 2DP

Warboard validates:
- no duplicate detachment
- Incursion: 2DP
- Strike Force: 3DP
- Incursion's special rule allowing one 3DP detachment as the only detachment
- no more than one ACROBATIC detachment
- pasted roster points do not exceed the selected battle points
- Devoted of Ynnead has Yvraine and/or the Yncarne
- when pasted roster metadata is present, Devoted has Yvraine or the Yncarne
  recorded as WARLORD

For custom/unknown battle sizes Warboard does not invent a DP allowance.

## ACROBATIC restriction

These are tagged ACROBATIC:
- Ghosts of the Webway
- Fateful Performance
- Twilight Flickers
- Serpent's Brood

v38 prevents selecting more than one of them.

## Manual fallback

The manual selector is now multi-select.

Every detachment button displays its DP cost and the window shows current
`spent / available DP`.

This remains a fallback when pasted roster configuration is unavailable or
needs overriding.

## Locked roster badge

After confirmation the badge shows:
- all active detachments
- DP spent / DP limit
- force disposition when supplied
- locked state

Before deployment, `EDIT` unlocks the configuration and returns to the setup
window. After deployment begins, the selected detachment set is immutable.

## Compatibility migration

`AeldariRulesSystem.cs` still contains rule bodies written before Warboard
supported multiple detachments.

The one-time editor migration redirects its public detachment checks,
stratagem list, enhancement list and summaries to `AeldariDetachmentRuntime`.

Backup:
`Library/WarboardBackups/V38/AeldariRulesSystem_PreV38.cs.txt`

Migration report:
`Library/WarboardV38MultiDetachmentReport.txt`

After a successful migration the editor migration script deletes itself.

## Expected Unity sequence

1. Extract this ZIP over the Warboard project and replace files.
2. Unity compiles v38.
3. The one-time v38 compatibility migration updates AeldariRulesSystem.
4. The migration deletes itself.
5. Unity compiles again.
6. Header reads `WARBOARD v38`.

## Smoke test

1. Load the YellowScribe roster as normal.
2. Paste the New Recruit text export into the Aeldari setup window.
3. Click `APPLY PASTED ROSTER`.
4. A one-detachment Devoted roster should lock as Devoted of Ynnead.
5. On Strike Force it should show `2/3DP`.
6. A valid 2DP + 1DP pair should load both controllers.
7. Selecting two ACROBATIC detachments should be rejected.
8. Exceeding the battle-size DP allowance should be rejected.
9. Deployment must remain blocked until configuration is valid and locked.
10. Start the game and confirm there are no red Console errors.

Force Disposition is parsed and retained in v38, but its faction-specific
gameplay effects are not invented here; those will be implemented when their
authoritative rule definitions are added.
