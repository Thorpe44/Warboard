WARBOARD v44 TEXT ENCODING HOTFIX

Fixes text such as:

  â€¢  ->  •
  â€”  ->  —
  â€“  ->  –
  â†’  ->  →
  â‰¥  ->  ≥
  â€¦  ->  …

CAUSE
-----
The original v44 PowerShell installer used Windows PowerShell's default
text-file decoding when reading existing C# files. Existing UTF-8 punctuation
was therefore decoded incorrectly and then saved back as UTF-8.

THIS HOTFIX
-----------
- Repairs the known mojibake in every source file touched by v44.
- Writes the repaired files explicitly as UTF-8.
- Creates timestamped backups.
- Verifies that the broken sequences are gone before reporting success.

INSTALL
-------
Run FIX_WARBOARD_V44_ENCODING.bat from the Warboard project root
(or a folder directly inside it).

Wait for:
  SUCCESS - v44 ENCODING HOTFIX VERIFIED

Then return to Unity and allow it to recompile.
