WARBOARD v45.6 - WORLD DICE + SELECTED UNIT CARD

MAIN CHANGE
===========
The existing TraditionalDiceTray3D was already a real physics tray hidden at a
remote world position and viewed through a RenderTexture camera.

v45.6 moves THAT EXISTING REAL TRAY into the battlefield world instead.

TRADITIONAL MODE
================
- Physical dice tray sits directly below the battlefield.
- Same Rigidbody / polyhedral D3,D4,D6,D8,D10,D12,D20 system.
- Dice rolls are visible as actual world objects.
- Click a physical die to select it for manual reroll.
- Top DICE CTRL button opens only a small controls panel.
- The old giant camera/render-texture tray popup is removed.

XCOM MODE
=========
- Physical dice tray is hidden.
- Dice controls are omitted from the top bar.

UI CHANGES
==========
- Top buttons split on both sides of the centred ROUND | FACTION | PHASE pill.
- NEXT PHASE remains far right.
- Real current totals (VP, Primary, Secondary, CP) sit under the centre pill.
- Selected-unit card moves from bottom-left to directly below the top-left HUD.
- WOUND EDIT and RESTORE EDIT are restored inside this card.
- Wound +/-/remove and restore-model controls appear in the card when active.
- Version remains a small bottom-left watermark.

MULTIPLAYER DIRECTION
=====================
This is intentionally world-space. When networking is added later, the dice
objects/results can be synchronised as shared game-world state rather than each
client having a private dice popup.

INSTALL
=======
1. Extract over the main Warboard project folder.
2. Run:
   INSTALL_WARBOARD_V45_6_WORLD_DICE_AND_UNIT_CARD.bat
3. Return to Unity and let it compile.

BACKUPS
=======
Library\WarboardBackups\V45_6WorldDiceUnitCard
