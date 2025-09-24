using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class FullscreenQuad : MonoBehaviour
{
    public Camera cam;
    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        float z = Mathf.Abs(transform.localPosition.z);
        float h = 2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * z;
        float w = h * cam.aspect;
        transform.localScale = new Vector3(w, h, 1f);
    }
}
