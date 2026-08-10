WARBOARD v44.2 - NECRON MIGRATION HOTFIX

PURPOSE
-------
Fixes:
[Warboard v44] Necrons faction-rule migration failed.
System.ArgumentOutOfRangeException
Parameter name: startIndex

The v44 one-time installer uses source-code insertion anchors. If one anchor is
missing or formatting has moved, String.Insert receives -1 and aborts the whole
migration. This hotfix routes those insertions through a guarded helper so the
installer can continue instead of crashing.

INSTALL
-------
1. Extract this ZIP directly into the MAIN Warboard project folder.
   The folder should contain Assets, Packages and ProjectSettings.

2. Double-click:
   FIX_WARBOARD_V44_2_NECRON_MIGRATION.bat

3. Return to Unity and let it compile.
   The v44 Necron migration should automatically re-run.

A backup is made of:
Assets\Editor\WarboardV44NecronsFactionRules.cs

If a moved source anchor is encountered, Unity will show a YELLOW warning naming
the insertion that was skipped rather than crashing the migration. If that
happens, send that warning so the specific hook can be corrected.
