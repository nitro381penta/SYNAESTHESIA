using UnityEngine;

public class PsychedelicPostController : MonoBehaviour
{
    public Material mat;
    public AudioSampler sampler;

    [Header("Response")]
    public float energyMul = 1.0f;
    public float beatBoost = 1.0f;
    public float beatDecay = 3.0f;

    float _pulse;

    void Update()
    {
        if (!mat || sampler == null || sampler.Spectrum == null) return;

        int mid = Mathf.FloorToInt(sampler.Spectrum.Length * 0.25f);
        int hi  = Mathf.FloorToInt(sampler.Spectrum.Length * 0.55f);
        float e = Avg(sampler.Spectrum, mid, hi)*0.6f + Avg(sampler.Spectrum, hi, sampler.Spectrum.Length)*0.9f;
        e += sampler.Level*0.5f;
        e *= energyMul;

        // beat pulse
        _pulse = Mathf.Max(0f, _pulse - beatDecay*Time.deltaTime);
        if (sampler.Beat) _pulse = Mathf.Min(1f, _pulse + beatBoost);

        mat.SetFloat("_AudioEnergy", Mathf.Clamp(e, 0f, 4f));
        mat.SetFloat("_BeatPulse",   Mathf.Clamp01(_pulse));
    }

    float Avg(float[] a, int i0, int i1)
    {
        i0 = Mathf.Clamp(i0, 0, a.Length); i1 = Mathf.Clamp(i1, 0, a.Length);
        int n = Mathf.Max(1, i1 - i0); float s = 0f; for (int i=i0;i<i1;i++) s += a[i]; return s / n;
    }
}
