WARBOARD v45.3a - TRAY INJECTION FIX

This fixes the failed final step from v45.3:
"Could not find the BuildWorld battlefieldWorldUI.Initialize anchor in GameController.Core.cs."

WHAT IT DOES
============
- Uses a broader regex to find battlefieldWorldUI.Initialize(...)
  regardless of formatting.
- Injects the WarboardV45PhysicalSideTrays runtime immediately after it.
- Re-includes WarboardV45PhysicalSideTrays.cs so the patch is self-contained.

IMPORTANT
=========
This is a follow-up patch.
Your earlier successful v45.3 UI layout changes remain in place.
This patch only fixes the failed core-runtime injection step.

INSTALL
=======
1. Extract this ZIP over the main Warboard project folder.
2. Run:
   INSTALL_WARBOARD_V45_3A_TRAY_INJECTION_FIX.bat
3. Return to Unity and let it compile.
