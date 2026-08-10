# Warboard v38.1 — Multi-Detachment Migration Hotfix

Fixes the Console error:

`v38 validation failed: multi-detachment runtime references were not installed.`

The v38 migration had already generated the correct runtime calls, but its own
validation looked for `AeldariDetachmentRuntime.GetSelected` on one line. The
generated code deliberately formats that call across two lines, so the validator
incorrectly rejected it before writing the migrated file.

v38.1 validates the actual generated call shape and places the migration marker
in the `AeldariRulesSystem` class body. No gameplay behaviour is otherwise
changed.

Visible header: `WARBOARD v38.1`

Install over v38 and replace files. Unity should run the migration again, then
perform the normal second compile.
