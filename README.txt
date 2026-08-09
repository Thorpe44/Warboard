WARBOARD ROSTER ABILITY WARNING FIX V2

WHY V2 EXISTS
-------------
The first installer relied on exact whitespace/text matching. The Unity log after
running it still showed:

  AbilityRegistry:Create(string)
  SquadController:Initialize(...)

That proves Unity was still compiling the unpatched path.

V2:
- Automatically locates the real Warboard project.
- Uses regex/pattern matching rather than exact formatting.
- Creates timestamped backups.
- Re-reads both files after editing.
- Refuses to report SUCCESS unless:
    * AbilityRegistry.TryCreate exists
    * SquadController uses TryCreate(id)
    * the old AbilityRegistry.Create(id) call is gone from SquadController
- Writes ABILITY_WARNING_FIX_V2_INSTALLED.txt into the actual project root.

INSTALL
-------
1. Extract BOTH .bat and .ps1.
2. Put them in the Warboard project root if possible.
3. Run FIX_ROSTER_ABILITY_WARNINGS_V2.bat.
4. Do not close the window until it says:
       SUCCESS - PATCH VERIFIED ON DISK
5. Return to Unity and allow compilation.
6. Reload the roster.

If the BAT says FAILED, take a screenshot of the BAT window and send it to ChatGPT.
