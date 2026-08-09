WARBOARD v31.4.4 — MANUAL CASUALTY UI NULLREF HOTFIX

FIXED
NullReferenceException:
    GameController.DrawContextActionBar()
    GameController.cs around line 25170

WHEN IT HAPPENED
In Traditional WOUND EDIT:
1. A selected model had 1 wound remaining.
2. Player clicked -1.
3. TraditionalAdjustSelectedWounds correctly converted that into model removal.
4. ConfirmTraditionalModelRemoval correctly set selectedModel = null.
5. The SAME OnGUI frame continued executing.
6. DrawContextActionBar then tried to read:
       selectedModel.CurrentWounds
   for the +1 button state.
7. That produced the NullReferenceException.

WHY GAMEPLAY APPEARED FINE
The model had already been removed and game state updated successfully before
the UI exception occurred. Only the remainder of that immediate-mode GUI frame
failed.

FIX
- After -1 WOUND, DrawContextActionBar now immediately exits if the selected
  model was removed / selection was invalidated.
- REMOVE immediately ends the current action-bar GUI pass after processing.
- REMOVE ANYWAY does the same.
- GUI.enabled is restored before exiting.

No wound, casualty, coherency or attack rules were changed.

CARRIED FORWARD
- Alt-hold movement ruler
- polyhedral 3D dice tray
- Traditional manual state controls
- XCOM automatic resolution
- Battle Log / visual states
- attached-unit deployment fix

INSTALL
1. Close Unity.
2. Extract over the current Warboard project.
3. Replace Assets\Scripts\Core\GameController.cs.
4. Reopen Unity.
5. In Traditional WOUND EDIT, take a 1W model from 1 -> 0 with the -1 button.

Expected:
model is removed normally with no Console exception.

Static C# source checks passed.
