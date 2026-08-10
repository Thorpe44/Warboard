# Warboard v43.1 — Custodes Compile Hotfix

This fixes the three compile errors shown after installing v43.

## Fixed

1. `CustodesFactionPack11.cs`
   - Replaced the two Unity-obsolete `GetInstanceID()` calls with
     `GetEntityId()`.

2. `GameController.CustodesFaction11.cs`
   - v43 correctly intended to add
     `ModelToken.ApplyFactionMaxWoundsModifier(int)` for the Auric Mantle
     Enhancement, but that method was being added by the Editor migration
     *after* Unity needed to compile the caller.
   - v43.1 adds a temporary first-compile extension shim.
   - The existing v43 migration then installs the real ModelToken instance
     method and removes the shim automatically.

3. Visible build marker
   - `WARBOARD v43.1`

## Install

Extract directly over the failed v43 install and choose Replace files.

Unity should now get through the first compile, run the v43 Custodes migration,
install the real ModelToken wounds modifier, and compile again.

The two `CoreRules11Completion.cs` CS0618 messages in the screenshot are
warnings, not blockers.
