WARBOARD v45.6a - ROBUST INSTALLER

This replaces the failed original v45.6 installer.

WHY THE FIRST INSTALLER FAILED
==============================
It used a regex that expected DrawTopCommandBar() to have a particular body
format. v45.5 had already changed that method, so the regex failed.

WHAT v45.6a CHANGES
===================
The new installer finds C# methods by:
1. locating the method signature
2. finding its opening brace
3. scanning balanced braces while ignoring strings and comments

That makes it independent of the previous method formatting.

The intended v45.6 changes remain:
- balanced top HUD around the centred round/faction/phase pill
- live VP / Primary / Secondary / CP below it
- selected-unit card below the top-left HUD
- Wound Edit / Restore Edit restored inside that card
- existing real Rigidbody dice tray moved into battlefield world space
- Traditional mode shows the physical tray
- XCOM mode hides it
- giant RenderTexture dice popup replaced by compact controls
- world dice can be clicked to select them for rerolls
- bottom-left version watermark

INSTALL
=======
1. Extract this ZIP directly over the MAIN Warboard folder.
2. Run:
   INSTALL_WARBOARD_V45_6A_ROBUST.bat
3. Return to Unity after it reports success.

It is safe to use after the failed first v45.6 attempt.

BACKUPS
=======
Library\WarboardBackups\V45_6aWorldDice
