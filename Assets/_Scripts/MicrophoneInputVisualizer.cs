using UnityEngine;
using UnityEngine.UI;

public class MicrophoneInputVisualizer : MonoBehaviour
{
    [Header("Sources")]
    public AudioSampler sampler;                
    public MicrophoneRecorder recorder;         
    [Header("UI")]
    public Image radial;                        
    public Image[] ticks;                       

    [Header("Look")]
    [Range(0.01f, 10f)] public float gain = 2.0f;
    [Range(0f, 0.2f)]  public float noiseFloor = 0.02f;  
    [Range(0.01f, 0.5f)] public float smooth = 0.12f;    // smooth
    public Gradient colorByLevel;                       // gradient

    float _v; 

    void Awake()
    {
        if (!sampler) sampler = FindFirstObjectByType<AudioSampler>();
        if (!recorder) recorder = FindFirstObjectByType<MicrophoneRecorder>();
    }

    void Update()
    {
        float src = 0f;

        if (sampler) src = sampler.Level;                           
        else if (recorder) src = Mathf.Max(recorder.LevelRMS, recorder.LevelPeak * 0.7f);

        float target = Mathf.Clamp01(Mathf.Max(0f, src - noiseFloor) * gain);

        // exponentially smoothing
        float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, smooth));
        _v = Mathf.Lerp(_v, target, k);

        // Radial
        if (radial)
        {
            radial.fillAmount = _v;
            if (colorByLevel != null && colorByLevel.colorKeys.Length > 0)
                radial.color = colorByLevel.Evaluate(_v);
        }

        // Ticks
        if (ticks != null && ticks.Length > 0)
        {
            int lit = Mathf.RoundToInt(_v * ticks.Length);
            for (int i = 0; i < ticks.Length; i++)
            {
                if (!ticks[i]) continue;
                bool on = i < lit;
                ticks[i].enabled = true;
                var c = ticks[i].color; c.a = on ? 1f : 0.15f; ticks[i].color = c;
                ticks[i].transform.localScale = Vector3.one * (on ? 1.08f : 1f);
            }
        }
    }
}
