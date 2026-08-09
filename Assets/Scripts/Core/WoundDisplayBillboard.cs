using UnityEngine;

public class WoundDisplayBillboard : MonoBehaviour
{
    private Camera cachedCamera;

    private void LateUpdate()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera == null)
            return;

        // Match camera orientation so the text remains readable while the
        // board camera pans/zooms.
        transform.rotation =
            cachedCamera.transform.rotation;
    }
}
