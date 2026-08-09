# Warboard v39.1 — Core Rules Migration Compile Hotfix

Fixes the Unity compiler errors in:

`Assets/Editor/WarboardV39CoreRulesCompliance.cs`

The v39 migration contains a verbatim C# string used to inject the automatic
Benefit of Cover correction into `RulesEngine.cs`. Two string literals inside
that generated code (`"stealth"` and `"ignores_cover"`) were not escaped for
the surrounding verbatim string, so the Editor migration itself could not
compile.

v39.1 escapes those literals correctly. No rule behaviour is otherwise changed.

Visible header: `WARBOARD v39.1`

Install over v39 and replace files. Unity should compile the migration, run it,
then perform the second compile as originally intended.
