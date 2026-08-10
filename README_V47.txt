WARBOARD v47a POWERSHELL PARSE HOTFIX
=====================================
The original v47 package contained two Windows PowerShell parser errors in
installer-only exception messages:

    throw "Could not locate C# method for $Label: $Signature"

PowerShell interprets `$Label:` as an invalid scoped/drive-style variable
reference. Both occurrences are now written using explicit string
concatenation instead.

The v47 C# rules-engine payload itself is unchanged.

IMPORTANT
=========
The screenshot showed a ParserError before the installer began executing.
That means the failed v47 attempt did not modify the Warboard project.

Install by extracting this package over the main Warboard project folder and
running:

    INSTALL_WARBOARD_V47.bat


WARBOARD v47 - RULES ENGINE EXPANSION
=====================================

PREREQUISITE
------------
A working v46 installation (the Orks / Tyranids / base Space Marines release).

INSTALL
-------
1. Preferably close Unity.
2. Extract this ZIP directly over the MAIN Warboard project folder.
3. Run:
      INSTALL_WARBOARD_V47.bat
4. Wait for "WARBOARD v47 INSTALLED."
5. Return to/reopen Unity and let it compile/import.

DO NOT extract into a separate subfolder beside Warboard.

MAIN CHANGES
------------
- generic persistent faction target/flag/objective state with expiry scopes
- physical arbitrary faction marker system
- Tyranid 40 mm Tunnel Markers and Tunnel Network automation
- explicit Enhancement bearer assignment; deployment blocks if taken Enhancements are unassigned
- machine-readable bearer restriction validation when the source card safely supports it
- bearer-aware passive Enhancement hooks
- per-Critical-Hit / per-Critical-Wound attack provenance
- mixed Precision allocation in both automatic and interactive combat paths
- Hive Predators Precision only on Critical Hits
- generic Hidden/detection-range state hooked into Core11 visibility
- Subversion detection and exact start-of-opponent-Movement Cloaked Position reaction
- Bastion scanned/pinned/suppressed/Heresy Undone state chain
- generic special reposition/endpoint validation engine
- generic datasheet/Stratagem choice state
- rich rules event bus for future reactive rules

IMPORTANT
---------
v47 is deliberately conservative. If a source rule still requires information
or route/timing geometry Warboard cannot prove, it remains on the exact card /
manual resolution path rather than being silently approximated.

FULL ENGINE NOTES
-----------------
Docs\WARBOARD_V47_RULES_ENGINE_EXPANSION.md

BACKUPS
-------
Library\WarboardBackups\V47RulesEngine

VERSION
-------
The installer updates WarboardBuildInfo.CurrentVersion to v47.
