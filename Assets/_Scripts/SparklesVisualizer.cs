using UnityEngine;

[DisallowMultipleComponent]
public class SparklesVisualizer : MonoBehaviour, IAudioReactive
{
    [Header("Sparkles (main)")]
    public ParticleSystem psInScene;
    public ParticleSystem psPrefab;
    private ParticleSystem _ps;

    [Header("Sparkles Tuning")]
    public float baseRate = 5f;
    public float rateMul  = 220f;
    public float orbitMul = 6f;
    [Range(0f, 1f)] public float highBandStart = 0.56f;
    [Tooltip("Larger = smoother, less flicker.")]
    public float rateSmooth = 0.18f;

    [Header("Smoke (optional)")]
    public bool enableSmoke = true;
    public ParticleSystem smokeInScene;
    public ParticleSystem smokePrefab;
    private ParticleSystem _smoke;

    [Header("Smoke Tuning")]
    public float smokeBaseRate = 8f;
    public float smokeRateMul  = 140f;
    public float smokeBaseSize = 0.7f;
    public float smokeSizeMul  = 1.2f;
    [Range(0f,1f)] public float smokeAlphaBase = 0.18f;
    [Range(0f,1f)] public float smokeAlphaMul  = 0.35f;
    public float smokeOrbitMul = 1.8f;
    public float smokeSmooth = 0.28f;

    float _sparkRateSmoothed;
    float _smokeRateSmoothed;
    float _smokeSizeSmoothed;
    float _smokeAlphaSmoothed;

    void Awake()
    {
        EnsureSparkles();
        ConfigurePS(_ps);

        if (enableSmoke)
        {
            EnsureSmoke();
            ConfigureSmokePS(_smoke);
        }
    }

    // Auto-start/stop when VisualizerManager toggles this root
    void OnEnable()  { Activate();  }
    void OnDisable() { Deactivate(); }

    void EnsureSparkles()
    {
        if (_ps) return;
        if (psInScene) _ps = psInScene;
        else if (psPrefab) _ps = Instantiate(psPrefab, transform);
        else Debug.LogError("[SparklesVisualizer] Assign a ParticleSystem (psInScene or psPrefab).");
    }

    void EnsureSmoke()
    {
        if (_smoke) return;
        if (smokeInScene) _smoke = smokeInScene;
        else if (smokePrefab) _smoke = Instantiate(smokePrefab, transform);
        else Debug.LogWarning("[SparklesVisualizer] enableSmoke is true but no smoke PS assigned.");
    }

    void ConfigurePS(ParticleSystem ps)
    {
        if (!ps) return;
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        var em = ps.emission; em.enabled = true;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.orbitalX = 0f; vol.orbitalY = 0f; vol.orbitalZ = 0f;
    }

    void ConfigureSmokePS(ParticleSystem ps)
    {
        if (!ps) return;
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.scalingMode = ParticleSystemScalingMode.Shape;

        var em = ps.emission; em.enabled = true;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.orbitalX = 0f; vol.orbitalY = 0f; vol.orbitalZ = 0f;
    }

    public void Activate()
    {
        if (_ps && !_ps.isPlaying) _ps.Play();
        if (enableSmoke && _smoke && !_smoke.isPlaying) _smoke.Play();
        // reset smoothing to avoid first-frame spikes
        _sparkRateSmoothed = 0f;
        _smokeRateSmoothed = smokeBaseRate;
        _smokeSizeSmoothed = smokeBaseSize;
        _smokeAlphaSmoothed = smokeAlphaBase;
    }

    public void Deactivate()
    {
        if (_ps) _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_smoke) _smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void React(float[] spectrum, float[] waveform, bool beat, float level)
    {
        if (_ps == null || spectrum == null || spectrum.Length == 0) return;

        // Energy = high band + overall level
        int start = Mathf.Clamp(Mathf.FloorToInt(spectrum.Length * highBandStart), 0, spectrum.Length - 1);
        float sum = 0f;
        for (int i = start; i < spectrum.Length; i++) sum += spectrum[i];
        float hi = sum / Mathf.Max(1, spectrum.Length - start);
        float energy = Mathf.Clamp01(level + hi);

        // Sparkles emission
        float targetSparkRate = baseRate + energy * rateMul;
        _sparkRateSmoothed = SmoothWithSlew(_sparkRateSmoothed, targetSparkRate, rateSmooth, 800f);
        var em = _ps.emission; em.rateOverTime = _sparkRateSmoothed;

        // Gentle orbital swirl
        var vol = _ps.velocityOverLifetime;
        vol.enabled = true;
        vol.orbitalY = energy * orbitMul;

        // Small size bump on beat
        if (beat)
        {
            var main = _ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f + energy * 0.3f);
        }

        // Smoke
        if (enableSmoke && _smoke)
        {
            float targetSmokeRate = smokeBaseRate + energy * smokeRateMul;
            _smokeRateSmoothed = SmoothWithSlew(_smokeRateSmoothed, targetSmokeRate, smokeSmooth, 400f);
            var smEm = _smoke.emission; smEm.rateOverTime = _smokeRateSmoothed;

            float targetSize = smokeBaseSize + energy * smokeSizeMul;
            _smokeSizeSmoothed = Mathf.Lerp(_smokeSizeSmoothed, targetSize, 1f - Mathf.Exp(-Time.deltaTime / smokeSmooth));
            var smMain = _smoke.main; smMain.startSize = _smokeSizeSmoothed;

            float targetAlpha = Mathf.Clamp01(smokeAlphaBase + energy * smokeAlphaMul);
            _smokeAlphaSmoothed = Mathf.Lerp(_smokeAlphaSmoothed, targetAlpha, 1f - Mathf.Exp(-Time.deltaTime / (smokeSmooth * 1.2f)));

            // Apply alpha on startColor (supports single color or gradient)
            var col = smMain.startColor;
            Color baseC = col.colorMax; // works for both MinMaxGradient types
            baseC.a = _smokeAlphaSmoothed;
            smMain.startColor = new ParticleSystem.MinMaxGradient(baseC);

            var smVol = _smoke.velocityOverLifetime;
            smVol.enabled = true;
            smVol.orbitalY = energy * smokeOrbitMul;

            if (beat)
            {
                float bump = Mathf.Min(1f, _smokeAlphaSmoothed + 0.08f);
                baseC.a = bump;
                smMain.startColor = new ParticleSystem.MinMaxGradient(baseC);
            }
        }
    }

    float SmoothWithSlew(float current, float target, float smooth, float maxChangePerSec)
    {
        float ema = Mathf.Lerp(current, target, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, smooth)));
        return Mathf.MoveTowards(current, ema, maxChangePerSec * Time.deltaTime);
    }
}
