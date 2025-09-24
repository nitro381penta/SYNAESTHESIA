using UnityEngine;

public class GroundProbe : MonoBehaviour
{
    public LayerMask groundLayers = ~0;
    public float probeRadius = 0.2f;
    public float probeDistance = 0.3f;

    void Update()
    {
        var origin = transform.position + Vector3.up * 0.1f;
        if (Physics.SphereCast(origin, probeRadius, Vector3.down, out var hit, probeDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"[GroundProbe] Grounded on '{hit.collider.name}' (layer={LayerMask.LayerToName(hit.collider.gameObject.layer)})");
        }
        else
        {
            Debug.Log("[GroundProbe] NOT grounded");
        }
    }
}
