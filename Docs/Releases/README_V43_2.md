# Warboard v43.2 — Custodes Migration Parser Hotfix

The compile stage is now passing. The remaining error is the one-time v43
migration rejecting a valid method in `UniversalRuleEngine.cs`:

`BuildAttackState`

That method is formatted with its return type and method name on separate
lines. The migration's source parser only recognised declarations where both
were on the same line, so it reported "Method not found" even though the
method exists.

v43.2 makes the method finder multiline-safe.

The previous failed migration had already applied several earlier Custodes
patches before reaching `UniversalRuleEngine.cs`. Re-running is safe: the
installer's edits are idempotent and skip hooks already present.

Install directly over the current failed v43.1 state and replace files. Unity
should re-run the migration, finish the remaining hooks, validate them, remove
the temporary shim/installer and compile again.

Visible header after completion: `WARBOARD v43.2`.
