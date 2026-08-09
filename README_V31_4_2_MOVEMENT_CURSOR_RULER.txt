WARBOARD v31.4.2 — LIVE MOVEMENT CURSOR RULER

BASELINE
Apply over v31.4.1.

When movement is active, Warboard now draws a live world-space line from the
selected model or formation anchor to the mouse position and places a distance
readout beside the cursor.

SINGLE MODEL
Initial move:
    4.2″ / 7.0″

After the model has already moved:
    2.1″ segment • 5.8/7.0″ total

The total uses the existing per-model turn-start tracking.

WHOLE SQUAD / ATTACHED UNIT
Shows live formation translation distance from the unit centre.

SPECIAL MOVEMENT
Special/faction moves using BeginSpecialMove get the same ruler, including the
movement limit and rule name.

CHARGE PHASE
A selected model also provides a live charge ruler to the cursor with the
12″ maximum pre-roll guide.

COLOUR
- cyan/blue: within the movement constraint
- red: over allowance, off-board, or illegal selected-model placement
- special moves use purple while legal
- charge measuring uses red/orange while legal

The existing movement/range circles remain; this ruler is additive.

INSTALL
1. Close Unity.
2. Extract over Documents\Warboard.
3. Replace Assets\Scripts\Core\GameController.cs.
4. Reopen Unity.
5. Select a model in the Movement phase and move the cursor around the board.

Static C# lexical / brace / regression checks passed.
Unity runtime rendering still requires local confirmation.
