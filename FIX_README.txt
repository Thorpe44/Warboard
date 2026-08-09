WARBOARD UNITY 6 v3 FIX

This build corrects the package manifest.

The previous "clean" build removed every built-in Unity module from the manifest.
That caused the compiler errors you saw:
- RaycastHit / Physics -> UnityEngine.PhysicsModule missing
- GUI -> UnityEngine.IMGUIModule missing

This build explicitly includes the built-in modules used by the prototype:
- com.unity.modules.physics
- com.unity.modules.imgui
- com.unity.modules.inputlegacy
- com.unity.modules.jsonserialize

HOW TO OPEN
1. Extract this ZIP into a completely new folder.
2. Confirm the folder directly contains Assets, Packages, ProjectSettings.
3. Add that folder through Unity Hub.
4. Open with Unity 6000.5.7f1.
5. If Unity offers Safe Mode on FIRST OPEN, enter it and allow package resolution to finish.
6. The RaycastHit/Physics/GUI errors should then disappear.
7. Exit Safe Mode and press Play.

Do not copy an old Library folder into this project.
