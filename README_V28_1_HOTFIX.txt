WARBOARD v28.1 — MISSION SETUP UI HOTFIX

Fixes the v28 screen where the game displayed:
  "Select both Force Dispositions..."
but no Mission Setup controls appeared.

ROOT CAUSE
The Mission Setup and Battle Complete GUI branches were accidentally inserted
into Update() rather than OnGUI(). Update() also returned immediately while
missionSetupMode was active, making those drawing calls unreachable.

FIX
- Removes GUI drawing from Update().
- Draws Mission Setup from OnGUI() where Unity IMGUI requires it.
- Draws Battle Complete from OnGUI() as intended.
- No mission rules, model packs, rosters or saved project folders are changed.

INSTALL
1. Close Unity.
2. Extract over the Warboard project root.
3. Replace GameController.cs.
4. Reopen Unity.
5. Load both rosters and click Mission Setup.

You should now see the full Chapter Approved mission setup panel with both
Force Dispositions, resolved Primary Missions, secondary modes, layout slot,
attacker role and BEGIN DEPLOYMENT button.
