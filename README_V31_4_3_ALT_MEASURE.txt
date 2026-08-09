WARBOARD v31.4.3 — ALT-TO-MEASURE HOTFIX

BASELINE
Apply over v31.4.2.

CHANGE
The live movement cursor ruler is no longer permanently visible.

HOLD:
    Left Alt
or
    Right Alt

while moving / measuring to show the live line and cursor distance.

Release Alt:
    ruler disappears immediately.

UNCHANGED
- movement-radius circles remain visible normally
- single-model movement measurement
- cumulative turn-start movement readout
- whole-squad formation measurement
- special/faction movement measurement
- charge-phase ruler
- red illegal-distance feedback

WHY
This keeps the battlefield visually clean during normal interaction but makes
precise tabletop-style measuring available instantly when the player wants it.

INSTALL
1. Close Unity.
2. Extract over Documents\Warboard.
3. Replace Assets\Scripts\Core\GameController.cs.
4. Reopen Unity.
5. Select a movable model.
6. Hold ALT and move the cursor around the battlefield.

Static C# checks passed.
Unity runtime still requires local confirmation.
