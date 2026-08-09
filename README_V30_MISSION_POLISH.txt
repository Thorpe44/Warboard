WARBOARD v30 — MISSION POLISH + VERIFIED SECONDARY AUTOMATION

BASELINE
Apply over the working v29.1 project. The confirmed v28.3 attached-Leader
deployment fix remains intact.

UNITY 6.5 CLEANUP
- Removed the unused battlefieldBuilt field.
- Replaced the deprecated FindObjectsByType(... FindObjectsSortMode.None)
  calls with the current no-sort overloads.
- This targets the CS0618 / CS0414 warning noise seen in the v29 Console.

MISSION SETUP PREVIEW
- Mission Setup now contains an actual mini battlefield preview.
- It renders the currently selected mission terrain framework and objective
  positions before deployment.
- Cycling Layout A/B/C updates the preview immediately.
- The resolved deployment archetype remains visible in the same setup screen.

VERIFIED DEFENDER SECONDARY AUTOMATION
The following Chapter Approved 2026-27 Defender secondary cards now resolve
automatically from actual Warboard game state:

TACTICAL
- Behind Enemy Lines
- Secure No Man's Land
- Engage on All Fronts
- Centre Ground
- No Prisoners
- Assassination
- Bring it Down

FIXED
- Engage on All Fronts
- Assassination
- Bring it Down

Implemented tracked state includes:
- real-base wholly-within opponent deployment-zone checks
- whole-unit table-quarter presence outside 6" of battlefield centre
- objective control in No Man's Land
- unit proximity to battlefield centre
- units destroyed this turn
- CHARACTER models destroyed this turn
- CHARACTER models with Wounds 4+ destroyed this turn
- models with Wounds 10+ destroyed this turn
- whether all enemy CHARACTER models have been destroyed

Automatic scoring still goes through Warboard's existing:
- 15 VP secondary cap per battle round
- 45 VP secondary cap per game
- 20 VP cap per Fixed secondary card

If a Tactical card scores automatically, it is removed from the hand.

ATTACKER SECONDARIES
Attacker-card automatic scoring remains MANUAL in v30.
The website exposes Attacker and Defender secondary decks as separate card
sets; v30 only automates card sides whose current 2026-27 Defender wording was
verified during this pass rather than assuming both sides are identical.

MISSION / TERRAIN STATUS
The mission battlefield/deployment architecture from v29 remains active:
- data-driven deployment archetypes
- polygon deployment zones
- real-base wholly-within validation
- objective actions
- mission-state terrain/objectives

The A/B/C terrain footprints remain functional framework layouts. They are not
claimed to be exact measured reproductions of every current Battlemaster
recommended terrain card yet. That is still a dedicated data-ingestion pass.

INSTALL
1. Close Unity.
2. Extract this ZIP over Documents\Warboard.
3. Replace the three script files.
4. Reopen Unity and allow compilation.
5. Confirm the previous CS0618 / battlefieldBuilt warnings are gone.
6. Open Mission Setup and cycle A/B/C to test the battlefield preview.
7. Play a Defender Tactical-secondary turn and test one of the verified
   automatic cards above.

STATIC VALIDATION
C# lexical / brace / regression-token checks passed.
Unity Editor compilation must still be confirmed locally.
