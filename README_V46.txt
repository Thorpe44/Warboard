WARBOARD v46e TRADITIONAL BATTLE-SHOCK HOTFIX
================================================
v46d successfully patched every source stage before the Traditional manual
Battle-shock method, then failed because that ONE method still used its own
byte-for-byte `$method.Contains($old)` / `.Replace($old,$new)` logic instead of
the robust installer patch engine.

v46e removes that final special-case matcher. `BeginNextTraditionalBattleShock()`
is still located structurally by method braces, but its internal Synapse change
is now applied with the same unique whitespace/line-ending tolerant
`Replace-Exact` helper used by the rest of v46.

The installer then verifies the WARBOARD_V46_TRADITIONAL_SYNAPSE marker exists
before it can continue.

The actual Orks/Tyranids/Space Marines rules payload is unchanged.
Your screenshot showed "Rollback complete", so the previous failed attempt did
not leave the project half-applied.


WARBOARD v46d PATCH ENGINE HOTFIX
=================================
v46c successfully found the locally-formatted shooting anchor, but exposed a
PowerShell return bug inside the flexible matching function.

The bug was:

    return
        $Text.Substring(...)

In Windows PowerShell, the newline terminates `return`, so the function
returned $null and the next source operation failed with:

    You cannot call a method on a null-valued expression.

v46d fixes that by constructing the result first and returning it explicitly.

It also adds:
- a flexible-anchor self-test BEFORE real project patching;
- a regex-replacement self-test BEFORE real project patching;
- a hard Patch-File guard that aborts immediately if any patcher returns null;
- static checks for the previous PowerShell syntax/compatibility failures.

The actual v46 Orks/Tyranids/Space Marines rules payload is unchanged.

If the previous attempt reported "Rollback complete", your baseline was
restored and you can extract v46d over the same project root and rerun the BAT.


WARBOARD v46c ROBUST ANCHOR HOTFIX
==================================
v46b reached the actual source-patching stage but failed because one local
GameController.Combat.cs formatting block did not exactly match the installer
anchor.

v46c changes the installer globally:

- exact anchors are still preferred;
- if exact matching fails, the installer retries while ignoring indentation,
  spaces, tabs and CR/LF differences;
- the flexible match MUST be unique;
- if zero or multiple locations match, installation stops and rolls back
  rather than guessing.

This specifically addresses:
    Could not find anchor for:
    standard faction shoot-after-Fall-Back eligibility

The actual v46 faction/rules payload is unchanged.


WARBOARD v46b WINDOWS POWERSHELL HOTFIX
=======================================
The v46a installer fixed the original missing { } block, but Windows
PowerShell 5.1 then exposed a second installer-only compatibility issue:
several expressions used C#-style member chaining on the following line,
for example:

    $Path.Substring(...)
        .TrimStart(...)

Those installer expressions have now all been rewritten as valid Windows
PowerShell 5.1 expressions.

The actual v46 faction/rules payload is unchanged.

If v46a failed with:
    The term '.TrimStart' is not recognized...
and reported rollback complete, extract this package over the same Warboard
project root and run INSTALL_WARBOARD_V46.bat again.


WARBOARD v46a INSTALLER HOTFIX
================================
This repack fixes a PowerShell parse error in the original v46 installer:

    if (-not (Test-Path $Path))
        throw ...

PowerShell requires a statement block for this if. It is now:

    if (-not (Test-Path $Path)) {
        throw ...
    }

The faction/rules payload itself is unchanged from v46.

If the original v46 attempt failed with:
    Missing statement block after if ( condition ).
then no v46 source installation took place because PowerShell failed while
parsing the script. Extract this package over the same Warboard root and run
INSTALL_WARBOARD_V46.bat again.


WARBOARD v46
DEEP AUDIT + ORKS + TYRANIDS + SPACE MARINES (NO SUPPLEMENTS)

BASELINE
========
Built for the current WARBOARD v45.7 project after the 10-Aug-2026 EOD cleanup.

INSTALL
=======
1. Keep Unity closed if possible.
2. Extract this ZIP directly over the MAIN Warboard project folder.
3. Run:
      INSTALL_WARBOARD_V46.bat
4. The installer validates all three data packs and reports success/failure.
5. Return to Unity and let it import/compile.

If the installer fails before completion, it automatically restores every
baseline source file it changed from:
    Library\WarboardBackups\V46ThreeFactions

NEW FACTIONS
============
- Orks
- Tyranids
- Space Marines — base ADEPTUS ASTARTES pack only

Space Marine supplements are intentionally NOT implemented in this release.
Armies containing Black Templars, Blood Angels, Dark Angels, Deathwatch or
Space Wolves supplement identities are blocked by the new faction controller
rather than silently running incomplete rules. The base Faction Pack rule that
an army cannot contain units from more than one Chapter is also validated from
the imported faction keywords before deployment.

SOURCE SCOPE
============
Faction Pack 11e v1.1, July 2026, from the three supplied faction files.
Crusade and Boarding Actions are excluded.

CONTENT COUNTS
==============
Orks:
  13 matched-play detachments
  44 enhancements
  66 stratagems

Tyranids:
  10 detachments
  34 enhancements
  51 stratagems

Space Marines:
  16 detachments
  59 enhancements
  81 stratagems

TOTAL:
  39 detachments
  137 enhancements
  198 stratagems

ARCHITECTURE
============
v46 adds a shared post-v45 faction extension layer instead of hard-coding each
new faction throughout Core.

New files:
  Assets\Scripts\Core\GameController.StandardFactionApi.cs
  Assets\Scripts\Core\WarboardFactionExtensionHub.cs
  Assets\Scripts\Factions\Standard11\StandardFactionPack11.cs
  Assets\Scripts\Factions\Standard11\StandardFactionGameController.cs
  Assets\Scripts\Factions\Standard11\StandardFactionSetupUI.cs
  Assets\Resources\FactionPacks11\orks.json
  Assets\Resources\FactionPacks11\tyranids.json
  Assets\Resources\FactionPacks11\space_marines.json

DEEP AUDIT
==========
After installation:
  Docs\WARBOARD_V46_DEEP_AUDIT.md

RULE RESOLUTION POLICY
======================
Deterministic mechanics with an exact existing Core hook are automated.

Rules that require data Warboard cannot currently represent exactly — such as
arbitrary marker placement, unknown Enhancement bearers, per-individual-hit
Precision or certain unusual reactive placements — are NOT guessed. Their
source-derived card remains available in the faction UI and the player resolves the
choice/rule explicitly.

Traditional mode keeps physical/manual resolution.
XCOM mode automates supported deterministic mechanics but still stops for
meaningful player choices. A source card that is not explicitly automated is
logged for manual/source-card resolution; Warboard does not pretend the effect
was applied internally.

NEW RECRUIT / YELLOWSCRIBE
===========================
Datasheets and loadouts remain live/imported. v46 does not hard-code army
datasheets.

The main STRATAGEMS menu recognises the three v46 faction controllers, previews
the selected Detachment cards and opens the full faction card panel for
SPEND + LOG actions.

The faction setup UI can read the existing pasted New Recruit text manifest to
auto-detect detachments. If no manifest is available, it offers manual
detachment selection and validates Detachment Points before deployment.

MODELS / VISUALS
================
This release adds faction rules/content, not new miniature model packs. Orks,
Tyranids and Space Marines use the existing Warboard model resolver/fallbacks
until dedicated model assets are supplied.

FINAL VALIDATION
================
This package was statically source-checked and faction JSON was parsed/count
validated before packaging.

This environment cannot launch the Unity Editor. Unity's compile after install
is the final required validation step.
