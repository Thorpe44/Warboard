# Warboard v33 Header Mini Patch

This patch adds an authoritative visible build marker.

The top Warboard header is redrawn with:

`WARBOARD v33`

The current resolution mode and battle size are preserved in the displayed
header.

From this release onward, every Warboard ZIP should update
`WarboardBuildInfo.CurrentVersion` so the running Unity build can be verified
immediately from the game screen.

No gameplay rules are changed by this mini patch.
