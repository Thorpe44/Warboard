WARBOARD v44.3 - PROJECT CLEANUP

Extract this ZIP directly into the Warboard project root and run:

    CLEAN_WARBOARD_V44_3.bat

WHAT IT DOES
------------
- Adds a proper Unity .gitignore.
- Keeps Assets, Packages and ProjectSettings.
- Keeps the current v44 README, v44 patch manifest and build report.
- Moves older release README/manifest files into Docs/Releases.
- Moves rules audits into Docs/Audits.
- Archives old FIX / INSTALL / ROLLBACK / CLEAN scripts locally under:
      Library/WarboardCleanupArchive/V44_3
- Archives temporary .bak / .before_* source files instead of destroying them.
- Removes the one-time Necron migration installer only if its successful-install
  marker is present.
- Stops Git tracking Library, Logs, UserSettings, Temp, obj and .vs WITHOUT
  deleting those folders from your computer.

AFTER RUNNING
-------------
Open GitHub Desktop. The cleanup will appear as file changes/deletions.
Commit and push those changes.

NOTE
----
This cleans the current branch/tree. Because Library was previously committed,
old Git history will still contain that historical data until the repository
history itself is purged/repacked. That is a separate optional cleanup.
