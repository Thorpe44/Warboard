WARBOARD v28 — CHAPTER APPROVED 2026–27 MISSION ENGINE

Apply over the permanent Warboard v27.1 project.

WHAT v28 ADDS

MISSION SETUP
- After both rosters are loaded, Warboard now opens a dedicated Mission Setup.
- Each player independently selects one of the five 11th-edition Force
  Dispositions:
    Take and Hold
    Purge the Foe
    Disruption
    Reconnaissance
    Priority Assets
- The correct Primary Mission is resolved live from the full 5 x 5 matchup
  matrix (25 Primary Missions).
- Each player chooses Tactical, Fixed, or Manual secondary handling.
- A mission terrain-layout slot (1–3) and attacker/defender role can be chosen.

PRIMARY MISSION ENGINE
- The old prototype "5 VP per objective at end of turn" scoring is removed.
- All 25 primary-front scoring structures are represented, including:
  command-phase scoring, end-of-turn scoring, round-5 timing, end-of-battle
  scoring, objective-count comparisons, territory checks, destruction counts,
  objective acquisition and persistent mission state.
- Mission scoring is capped at 15 VP per battle round and 45 VP total.

MISSION OPERATIONS
Some Chapter Approved cards rely on reverse-side Objective Actions or persistent
state such as operation markers, consecrated objectives, trapped terrain,
surveillance, relic state, condemned units, decoys, etc.
v28 exposes these through the in-game MISSION panel using explicit +/- mission
counters so their scoring works without inventing eligibility rules that have
not yet been fully encoded.

SECONDARIES
- All 18 current secondary card names are in the deck.
- Tactical mode supports draw / hand / discard / manual score.
- Fixed mode exposes the four Fixed-eligible cards and two selectable slots.
- Secondary scoring is capped at 15 VP per round and 45 VP total.
- Each Fixed card is additionally capped at 20 VP.
- Full automatic secondary-condition detection is not yet implemented.

BATTLE FLOW
- 60 x 44 mission battlefield orientation.
- Six mission-role objectives: P1 home, P2 home, two central, two expansion.
- Three symmetric terrain-layout framework slots.
- Deployment follows mission setup.
- After deployment, players roll off; the winner takes first turn.
- First-player order remains consistent through all five battle rounds.
- Battle ends after both players complete round 5.
- End-of-battle primary scoring is applied and a final Primary / Secondary /
  Total score summary is shown.

IMPORTANT LIMITATION — TERRAIN / DEPLOYMENT CARDS
The mission engine and scoring layer are real, but v28 does NOT claim to contain
pixel/exact reproductions of all 45 recommended GDM/Battlemaster terrain cards
or every mission-specific deployment polygon yet.

The three v28 layout slots are functional symmetric framework layouts.
Exact measured Chapter Approved layouts/deployment shapes should be the next
data pass now that the mission engine can consume them.

FILES CHANGED
Assets/Scripts/Core/GameController.cs
Assets/Scripts/Core/MissionSystem.cs             (new)
Assets/Scripts/Core/ObjectiveController.cs

INSTALL
1. Close Unity.
2. Extract this ZIP directly over Documents\Warboard.
3. Replace existing files.
4. Reopen Unity and allow scripts to compile.
5. Choose battle size.
6. Load both rosters.
7. Open Mission Setup, select Force Dispositions / secondary modes / layout.
8. Begin deployment.
9. Use the MISSION button during play for mission state and secondaries.

No model-pack changes, Library deletion, or asset migration are required.

VALIDATION
Static C# lexical/brace checks passed in the build environment.
Unity Editor compilation cannot be run in the build environment, so the final
compile/runtime check happens when the project is opened in Unity.
