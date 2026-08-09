WARBOARD - ROSTER ABILITY WARNING FIX

WHAT THIS FIXES
---------------
YellowScribe/New Recruit imports every datasheet ability name into UnitData.abilities.
SquadController was attempting to instantiate every imported rule through the old
AbilityRegistry, causing "Unknown ability id" warnings for modern rules.

The modern engine already retains those rule names in SourceData. Universal rules
and faction-pack systems can continue reading them there.

This patch:
1. Adds AbilityRegistry.TryCreate(), which quietly checks whether a legacy ability
   object is actually registered.
2. Changes SquadController so it only instantiates registered legacy abilities.
3. Leaves all imported ability names/data intact.
4. Keeps AbilityRegistry.Create() warning behaviour for any genuine direct caller.
5. Creates backups before editing.

INSTALL
-------
1. Extract both FIX_ROSTER_ABILITY_WARNINGS files into the ROOT Warboard folder
   (the folder containing Assets, Packages and ProjectSettings).
2. Double-click FIX_ROSTER_ABILITY_WARNINGS.bat.
3. Return to Unity and wait for compilation.
4. Reload the roster.
5. The repeated "Unknown ability id" warnings from roster loading should be gone.

This is deliberately NOT a fake no-op registration of the 33 rule names. Doing that
would make the console quiet while falsely implying the old AbilityRegistry was
implementing those rules.
