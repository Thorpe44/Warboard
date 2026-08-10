WARBOARD v45.1 - CHARACTER LITERAL COMPILE FIX

FIXES
=====
The v45.1 text-cleanup pass accidentally changed Unicode dash character
literals inside parser code into multi-character C# literals.

Unity errors:
- WeaponRuleParser.cs line 361: CS1012
- WeaponRuleParser.cs line 362: CS1012
- YellowScribeImporter.cs line 1362: CS1012
- YellowScribeImporter.cs line 1363: CS1012

This patch restores those parser characters using ASCII-safe C# escapes:

    '\u2011'
    '\u2013'
    '\u2014'

These are parser normalization characters, not visible UI separators.

INSTALL
=======
1. Extract over the main Warboard project folder.
2. Run:
   FIX_WARBOARD_V45_1_CHAR_LITERALS.bat
3. Return to Unity and let it compile.

The BAT remains open on success or failure.
