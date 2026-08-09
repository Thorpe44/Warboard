using UnityEngine;

// v35.1 bootstrap only.
//
// The v34 CoreEventBridge cannot be allowed to block compilation because the
// v35 structural refactor must compile before it can delete that bridge.
// Once WarboardV35GameControllerRefactor runs, this file is removed entirely.
public sealed class CoreEventBridge : MonoBehaviour
{
}
