using UnityEngine;

[DisallowMultipleComponent]
public class CanvasDebugDropIn : MonoBehaviour
{
    [Header("Force-show distance (meters)")]
    public float showDistance = 2.0f;

    // Right-click the component header -> "Dump Canvas State"
    [ContextMenu("Dump Canvas State")]
    public void Dump()
    {
        var go     = gameObject;
        var canvas = GetComponentInChildren<Canvas>(true);
        var cg     = GetComponentInChildren<CanvasGroup>(true);
        var cam    = Camera.main;

        Debug.Log($"[CanvasDebug] GO='{go.name}' activeSelf={go.activeSelf}, activeInHierarchy={go.activeInHierarchy}", this);

        if (canvas)
        {
            string camName = (canvas.worldCamera != null) ? canvas.worldCamera.name : "<null>";
            Debug.Log(
                $"[CanvasDebug] Canvas enabled={canvas.enabled}, renderMode={canvas.renderMode}, " +
                $"sortingLayer='{canvas.sortingLayerName}', sortingOrder={canvas.sortingOrder}, " +
                $"planeDistance={canvas.planeDistance}, worldCamera={camName}",
                this
            );
        }
        else
        {
            Debug.LogWarning("[CanvasDebug] No Canvas found under this object.", this);
        }

        if (cg)
        {
            Debug.Log($"[CanvasDebug] CanvasGroup alpha={cg.alpha}, interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}", this);
        }

        if (cam)
        {
            bool culled = (cam.cullingMask & (1 << gameObject.layer)) == 0;
            Debug.Log($"[CanvasDebug] Camera='{cam.name}', layer='{LayerMask.LayerToName(gameObject.layer)}', culledByCamera={culled}", this);
        }
        else
        {
            Debug.LogWarning("[CanvasDebug] No Camera.main in scene.", this);
        }

        Debug.Log($"[CanvasDebug] pos={transform.position}, rot={transform.rotation.eulerAngles}, scale={transform.lossyScale}", this);
    }

    // Right-click -> "Force Show In Front Of HMD (2m)"
    [ContextMenu("Force Show In Front Of HMD (2m)")]
    public void ForceShow()
    {
        var go     = gameObject;
        var canvas = GetComponentInChildren<Canvas>(true);
        var cg     = GetComponentInChildren<CanvasGroup>(true);
        var cam    = Camera.main;

        go.SetActive(true);

        if (canvas)
        {
            canvas.renderMode        = RenderMode.WorldSpace; // keep world-space (as you prefer)
            canvas.overrideSorting   = true;
            canvas.sortingOrder      = 500;                   // on top
            if (canvas.worldCamera == null && cam != null)
                canvas.worldCamera = cam;                     // important for raycasts in world-space
        }

        if (cg)
        {
            cg.alpha        = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        if (cam)
        {
            Vector3 pos = cam.transform.position + cam.transform.forward * showDistance + Vector3.up * 0.05f;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            transform.localScale = Vector3.one * 0.15f;
        }

        if (cam && ((cam.cullingMask & (1 << gameObject.layer)) == 0))
        {
            Debug.LogWarning(
                $"[CanvasDebug] Layer '{LayerMask.LayerToName(gameObject.layer)}' is NOT in Camera.main culling mask. " +
                $"The canvas will be invisible until you enable that layer on the camera.", this);
        }

        Debug.Log("[CanvasDebug] ForceShow applied.", this);
    }
}
