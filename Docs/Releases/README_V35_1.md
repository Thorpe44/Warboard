# Warboard v35.1 — Refactor Bootstrap Fix

This is a bootstrap hotfix for v35.

## What happened

The v34 `CoreEventBridge.cs` and `AeldariSetupUI.cs` contain unqualified
`Object` references while importing both `System` and `UnityEngine`.

In the Unity/C# version used by this project, that makes `Object` ambiguous
between `System.Object` and `UnityEngine.Object`.

Those compiler errors occur before Unity can compile the v35 Editor refactor
tool, so the v35 refactor never gets a chance to run and remove the bridge.

## What this patch does

- Replaces `CoreEventBridge.cs` with a tiny compile-safe bootstrap stub.
- Qualifies the `UnityEngine.Object` usages in `AeldariSetupUI.cs`.
- Re-includes the v35 GameController refactor tool.
- Updates the visible version marker to `WARBOARD v35.1`.
- Uses CRLF line endings for the C# files in this patch.

## Expected sequence

After extraction and `Assets -> Refresh`:

1. Unity compiles the bootstrap-safe files.
2. `WarboardV35GameControllerRefactor` runs.
3. It splits `GameController.cs` into focused partial files.
4. It removes `CoreEventBridge.cs` completely.
5. It cleans the faction-controller access paths.
6. Unity refreshes and compiles a second time.

The important check is the Console after the second compile.
