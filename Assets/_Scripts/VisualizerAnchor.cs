using UnityEngine;

public class VisualizerAnchor : MonoBehaviour
{
    [Tooltip("Leave empty to auto-use Camera.main")]
    public Transform target;

    [Tooltip("Meters per second the anchor can catch up; 0 = snap")]
    public float followSpeed = 8f;

    [Tooltip("Keep same Y as target? If false, use fixedYOffset.")]
    public bool matchTargetY = false;

    [Tooltip("World Y for the anchor if not matching target Y.")]
    public float fixedY = 0f;

    [Tooltip("Extra vertical offset applied after Y selection.")]
    public float yOffset = 0f;

    [Tooltip("Follow only horizontally (ignore target rotation; keep visuals level).")]
    public bool horizontalOnly = true;

    void LateUpdate()
    {
        var t = target ? target : (Camera.main ? Camera.main.transform : null);
        if (!t) return;

        // desired position
        Vector3 desired = t.position;
        if (!matchTargetY) desired.y = fixedY;
        desired.y += yOffset;

        // smooth follow
        if (followSpeed <= 0f) transform.position = desired;
        else transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSpeed);

        // keep visuals level & not tied to head rotation
        if (horizontalOnly) transform.rotation = Quaternion.identity;
    }
}
