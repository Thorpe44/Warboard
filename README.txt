WARBOARD SAFE PROJECT CLEANUP

Removes only the temporary files created during the Custodes/ability/v44 work:

- one-off BAT/PS1 installers and repair scripts
- v44 marker text files
- timestamped backup copies made by those scripts
- downloaded Warboard patch ZIPs if they were copied into the project root
- stale WarboardVisualTheme.cs.meta if the rolled-back source file is gone

DOES NOT REMOVE

- Assets
- Packages
- ProjectSettings
- Custodes model assets
- ModelPool / TexturePool
- ModelIndex.json
- faction rules
- roster importer
- live Unity .meta files
- actual Warboard source files

Run CLEAN_WARBOARD_FOLDER.bat from the Warboard project root.
