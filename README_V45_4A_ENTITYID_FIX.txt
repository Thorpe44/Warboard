WARBOARD v45.4a - UNITY 6.5 ENTITY ID COMPILE FIX

FIXES
=====
Assets\Scripts\Core\WarboardV45PhysicalSideTrays.cs
CS0619:
'Object.GetInstanceID()' is obsolete: 'Use GetEntityId instead.'

The tray system only uses this ID to detect when its visual contents need to be
rebuilt, so GetEntityId() is the correct Unity 6.5 replacement.

INSTALL
=======
1. Extract over the main Warboard project folder.
2. Run:
   FIX_WARBOARD_V45_4A_ENTITYID.bat
3. Return to Unity and let it compile.

The remaining CS0618 messages shown in the Console are warnings only.
