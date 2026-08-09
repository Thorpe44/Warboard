WARBOARD v31.4.1 — POLYHEDRAL 3D DICE TRAY HOTFIX

BASELINE
Apply over v31.4.

THE TRAY IS NO LONGER D6-ONLY.

AVAILABLE TYPES
- D3
- D4
- D6
- D8
- D10
- D12
- D20

PHYSICAL DICE
The tray now generates separate runtime 3D geometry:
- D4 tetrahedron
- D6 cube
- D8 octahedron
- D10 ten-faced bipyramid
- D12 dodecahedron
- D20 icosahedron

D3 uses a physical cube marked 1/1, 2/2, 3/3. This matches the normal
tabletop D3 convention of deriving D3 outcomes from a six-sided die while
still exposing a dedicated D3 control.

MIXED POOLS
Each die type has its own count.

Example:
    2D6 + 1D8 + 1D20

Select a die type and use -5 / -1 / +1 / +5 to edit only that type.
The entire configured pool is capped at 40 physical dice.

ROLL POOL throws every configured die simultaneously.

MANUAL REROLLS
Click any individual physical die to select it.
REROLL SELECTED rerolls exactly those dice, regardless of type.
Warboard still does not decide why a reroll is legal.

TRANSPARENCY
Settled display / Battle Log groups results by die type, e.g.:
    2D6 -> [5, 2] | 1D8 -> [7] | 1D20 -> [14]

This remains an audit aid only. Traditional mode does not interpret those
numbers as successes/failures.

INTEGRATION
Existing Traditional prompts still default to D6 because 40K's core random
mechanics are D6-based.
A new SetRequestedDicePool(sides, count) API is included so individual
abilities can request another die type later without changing the free-form
tray design.

INSTALL
1. Close Unity.
2. Extract over Documents\Warboard.
3. Replace GameController.cs and TraditionalDiceTray3D.cs.
4. Reopen Unity and allow compilation.
5. Open 3D DICE and test a mixed pool such as:
       2D6 + 1D8 + 1D20
6. Select one die after it settles and use REROLL SELECTED.

Static C# lexical / brace / integration checks passed.
Unity physics/rendering still requires local runtime confirmation.
