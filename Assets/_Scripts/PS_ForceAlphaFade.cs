using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class PS_ForceAlphaFade : MonoBehaviour
{
    public ParticleSystem ps;

    [Range(0f,1f)] public float a0 = 0f;
    [Range(0f,1f)] public float a1 = 1f;
    [Range(0f,1f)] public float aMid = 0.8f;
    [Range(0f,1f)] public float aEnd = 0f;
    [Range(0f,0.2f)] public float t1 = 0.07f;
    [Range(0f,1f)]   public float tMid = 0.60f;

    void OnEnable()  => Apply();
    void OnValidate()=> Apply();

    void Apply()
    {
        if (!ps) ps = GetComponent<ParticleSystem>();
        if (!ps) return;

        var col = ps.colorOverLifetime;
        col.enabled = true;

        var g = new Gradient();
        
        var ck = new GradientColorKey[]
        {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f)
        };
        var ak = new GradientAlphaKey[]
        {
            new GradientAlphaKey(a0,   0f),
            new GradientAlphaKey(a1,   t1),
            new GradientAlphaKey(aMid, tMid),
            new GradientAlphaKey(aEnd, 1f)
        };
        g.SetKeys(ck, ak);

        col.color = new ParticleSystem.MinMaxGradient(g);
    }
}
