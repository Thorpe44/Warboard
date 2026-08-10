WARBOARD v45.1 - UI TEXT ENCODING HOTFIX (FIXED)

This replaces the broken first v45.1 installer.

INSTALL
=======
1. Extract this ZIP directly into the main Warboard project folder.
2. Run:
   INSTALL_WARBOARD_V45_1_ASCII_HOTFIX.bat
3. The window will now STAY OPEN whether it succeeds or fails.
4. Return to Unity after it reports HOTFIX FINISHED.

FIX
===
Removes Unicode separators and common mojibake versions from the Core C# UI
strings, replacing them with plain ASCII:
  bullet       -> |
  arrow        -> ->
  long dashes  -> -
  multiply     -> x

It also updates the visible build marker from v45 to v45.1.

Backups are written under:
Library\WarboardBackups\V45_1AsciiHotfix
