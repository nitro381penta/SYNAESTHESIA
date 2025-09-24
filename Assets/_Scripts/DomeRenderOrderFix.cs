using UnityEngine;

[ExecuteAlways]
public class DomeRenderOrderFix : MonoBehaviour
{
    public Renderer domeRenderer;
    [Tooltip("Render before UI (3000). 2950 is a safe default.")]
    public int renderQueue = 2950;

    void OnEnable()   => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        if (!domeRenderer) domeRenderer = GetComponent<Renderer>();
        if (!domeRenderer) return;

        foreach (var m in domeRenderer.sharedMaterials)
        {
            if (!m) continue;
            m.renderQueue = renderQueue;
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        }
    }
}
