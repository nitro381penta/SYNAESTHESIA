using UnityEngine;

public class PsychedelicVisualizer : MonoBehaviour, IAudioReactive
{
    [Header("Particle Systems")]
    public ParticleSystem psCore;
    public ParticleSystem psStreaks;
    public ParticleSystem psRipples;

    [Header("Emission")]
    public float baseRate = 14f;
    public float rateMul  = 420f;

    [Header("Hue / Color Cycling")]
    public float hueSpeed = 0.18f;
    public float hueSpeedMul = 1.4f;     // extra with energy
    [Range(0f,1f)] public float saturation = 1f;
    [Range(0f,1f)] public float value = 1f;
    public float rainbowUpdateHz = 20f;

    [Header("Noise / Swirl (Core + Streaks)")]
    public float noiseBase = 0.2f;
    public float noiseMul  = 2.0f;
    public float noiseFreqBase = 0.25f;
    public float noiseFreqMul  = 1.2f;
    public float swirlOrbitMul = 7f;

    [Header("Beat Bursts")]
    public int   burstCountCore    = 70;
    public float burstStreakFactor = 0.5f;   // trails get fewer than core
    public int   burstRipples      = 18;     // ripple particles per beat
    public float minBeatInterval   = 0.12f;

    [Header("Beat Scale Pulse")]
    public float scalePulseAmount  = 0.25f;
    public float scalePulseDecay   = 3.5f;   // /s

    [Header("Band Focus")]
    [Range(0f,1f)] public float midStart = 0.25f;
    [Range(0f,1f)] public float hiStart  = 0.55f;

    [Header("Post FX (Dot Mosaic + Hairlines)")]
    [Tooltip("Material you assigned to the full-screen quad (Shader Graph SG_PsyDots).")]
    public Material dotMosaicMat;
    [Range(0,1)] public float postIntensityOn  = 1f;
    [Range(0,1)] public float postIntensityOff = 0f;
    public bool animateDotAndLineScale = true;
    public Vector2 dotScaleRange  = new Vector2(36, 96);
    public Vector2 lineScaleRange = new Vector2(90, 220);

    // internals
    float _hue, _lastBeat = -999f, _pulse, _rainbowTimer;
    Gradient _rainbow = new Gradient();

    // lifecycle
    public void Activate()
    {
        Prep(psCore,   trails:false);
        Prep(psStreaks,trails:true);
        PrepRipple(psRipples);

        if (psCore    && !psCore.isPlaying)    psCore.Play();
        if (psStreaks && !psStreaks.isPlaying) psStreaks.Play();
        if (psRipples && !psRipples.isPlaying) psRipples.Play();

        _lastBeat = -999f; _pulse = 0f; _rainbowTimer = 0f;
        BakeRainbow(0f);

        if (dotMosaicMat) dotMosaicMat.SetFloat("_Intensity", postIntensityOn);
    }

    public void Deactivate()
    {
        if (psCore)    psCore.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (psStreaks) psStreaks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (psRipples) psRipples.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        transform.localScale = Vector3.one;

        if (dotMosaicMat) dotMosaicMat.SetFloat("_Intensity", postIntensityOff);
    }

    // Main audio callback
    public void React(float[] spectrum, float[] waveform, bool beat, float level)
    {
        if (spectrum == null || spectrum.Length == 0) return;

        // energy (mids + highs + overall)
        int midIdx = Mathf.FloorToInt(spectrum.Length * midStart);
        int hiIdx  = Mathf.FloorToInt(spectrum.Length * hiStart);

        float mid = AvgRange(spectrum, midIdx, hiIdx);
        float hi  = AvgRange(spectrum, hiIdx, spectrum.Length);
        float energy = (hi*0.9f + mid*0.6f + level*0.7f);

        // pulse & beat gating
        _pulse = Mathf.Max(0f, _pulse - scalePulseDecay * Time.deltaTime);
        bool canBurst = (Time.time - _lastBeat > minBeatInterval);

        if (beat && canBurst)
        {
            _pulse = Mathf.Min(1f, _pulse + 1f);

            // burst counts scale lightly with energy so that quieter beats are smaller
            int cCore   = Mathf.RoundToInt(burstCountCore * Mathf.Clamp01(energy * 2f + 0.2f));
            int cStreak = Mathf.RoundToInt(cCore * Mathf.Clamp01(burstStreakFactor));

            if (psCore)    psCore.Emit(cCore);
            if (psStreaks) psStreaks.Emit(cStreak);
            if (psRipples) psRipples.Emit(Mathf.RoundToInt(burstRipples * Mathf.Clamp01(0.5f + energy)));

            _lastBeat = Time.time;
        }

        float s = 1f + _pulse * scalePulseAmount;
        transform.localScale = new Vector3(s, s, s);

        // continuous emission
        float targetRate = baseRate + energy * rateMul;
        SetRate(psCore,    targetRate);
        SetRate(psStreaks, targetRate * 0.6f);

        // noise & swirl
        float nzStr = noiseBase + energy * noiseMul;
        float nzFrq = noiseFreqBase + energy * noiseFreqMul;
        float swirl = (energy + level) * swirlOrbitMul;
        ApplyNoiseAndSwirl(psCore,    nzStr, nzFrq, swirl);
        ApplyNoiseAndSwirl(psStreaks, nzStr, nzFrq, swirl * 1.2f);

        //  hue / rainbow
        float hueSpd = hueSpeed + energy * hueSpeedMul;   // cycles/sec
        _hue = Mathf.Repeat(_hue + hueSpd * Time.deltaTime, 1f);

        _rainbowTimer += Time.deltaTime;
        if (_rainbowTimer >= (1f / Mathf.Max(1f, rainbowUpdateHz)))
        {
            _rainbowTimer = 0f;
            BakeRainbow(_hue);
            ApplyGradient(psCore,    _rainbow);
            ApplyGradient(psStreaks, _rainbow);
            ApplyGradient(psRipples, _rainbow);
        }

        // post shader parameters
        if (dotMosaicMat)
        {
            dotMosaicMat.SetFloat("_AudioEnergy", Mathf.Clamp(energy, 0f, 4f));
            dotMosaicMat.SetFloat("_BeatPulse",   Mathf.Clamp01(_pulse));

            if (animateDotAndLineScale)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(energy*0.9f));
                float dot  = Mathf.Lerp(dotScaleRange.x,  dotScaleRange.y,  t);
                float line = Mathf.Lerp(lineScaleRange.x, lineScaleRange.y, t);
                dotMosaicMat.SetFloat("_DotScale",  dot);
                dotMosaicMat.SetFloat("_LineScale", line);
            }
        }
    }

    // helpers
    void Prep(ParticleSystem ps, bool trails)
    {
        if (!ps) return;
        var main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(20000, main.maxParticles);

        // sensible minimums
        if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            main.startLifetime = Mathf.Max(2.5f, main.startLifetime.constant);
        if (main.startSpeed.mode == ParticleSystemCurveMode.Constant)
            main.startSpeed = Mathf.Max(0.35f, main.startSpeed.constant);
        if (main.startSize.mode == ParticleSystemCurveMode.Constant)
            main.startSize = Mathf.Max(0.05f, main.startSize.constant);
        if (main.startRotation.mode == ParticleSystemCurveMode.Constant)
            main.startRotation = Random.Range(0f, Mathf.PI * 2f);

        var em = ps.emission; em.enabled = true;

        var shape = ps.shape; shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(3f, shape.radius);
        shape.randomDirectionAmount = 0.5f;

        var nz = ps.noise; nz.enabled = true;
        nz.quality = ParticleSystemNoiseQuality.Medium;
        nz.separateAxes = false;

        var vol = ps.velocityOverLifetime; vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.World;
        vol.orbitalX = 0f; vol.orbitalZ = 0f; // we animate Y

        var col = ps.colorOverLifetime; col.enabled = true;

        var t = ps.trails;
        t.enabled = trails;
        if (trails)
        {
            t.inheritParticleColor = true;
            t.ratio = 1f;
            t.lifetime = 1f;
            t.minVertexDistance = 0.08f;
            t.dieWithParticles = true;
        }

        var r = ps.GetComponent<ParticleSystemRenderer>();
        if (r) r.renderMode = ParticleSystemRenderMode.Billboard;
    }

    void PrepRipple(ParticleSystem ps)
    {
        if (!ps) return;
        var main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(6000, main.maxParticles);

        if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            main.startLifetime = Mathf.Max(2.5f, main.startLifetime.constant);
        if (main.startSpeed.mode == ParticleSystemCurveMode.Constant)
            main.startSpeed = Mathf.Max(0.0f, main.startSpeed.constant);
        if (main.startSize.mode == ParticleSystemCurveMode.Constant)
            main.startSize = Mathf.Max(0.05f, main.startSize.constant);

        // no extra noise/swirl by default
        var nz = ps.noise; nz.enabled = false;

        var vol = ps.velocityOverLifetime; vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.World;
        vol.orbitalX = 0f; vol.orbitalY = 0f; vol.orbitalZ = 0f;

        var col = ps.colorOverLifetime; col.enabled = true; // script re-bakes gradient

        var em = ps.emission; em.enabled = true;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        if (r) r.renderMode = ParticleSystemRenderMode.Billboard;
    }

    void SetRate(ParticleSystem ps, float rate)
    {
        if (!ps) return;
        var em = ps.emission;
        em.rateOverTime = rate;
    }

    void ApplyNoiseAndSwirl(ParticleSystem ps, float strength, float freq, float orbitalY)
    {
        if (!ps) return;
        var nz = ps.noise; nz.enabled = true;
        nz.strength = strength;
        nz.frequency = freq;
        nz.scrollSpeed = freq * 0.35f;

        var vol = ps.velocityOverLifetime; vol.enabled = true;
        vol.orbitalY = orbitalY;
    }

    void BakeRainbow(float shift)
    {
        Color H(float h) => Color.HSVToRGB(Mathf.Repeat(h + shift, 1f), saturation, value);
        var keys = new GradientColorKey[] {
            new GradientColorKey(H(0f/6f), 0f),
            new GradientColorKey(H(1f/6f), 0.17f),
            new GradientColorKey(H(2f/6f), 0.33f),
            new GradientColorKey(H(3f/6f), 0.50f),
            new GradientColorKey(H(4f/6f), 0.67f),
            new GradientColorKey(H(5f/6f), 0.83f),
            new GradientColorKey(H(6f/6f), 1f),
        };
        var a = new GradientAlphaKey[] { new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) };
        _rainbow.SetKeys(keys, a);
    }

    void ApplyGradient(ParticleSystem ps, Gradient g)
    {
        if (!ps) return;
        var col = ps.colorOverLifetime; col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    float AvgRange(float[] a, int from, int to)
    {
        from = Mathf.Clamp(from, 0, a.Length);
        to   = Mathf.Clamp(to,   0, a.Length);
        int n = Mathf.Max(1, to - from);
        float s = 0f; for (int i = from; i < to; i++) s += a[i];
        return s / n;
    }
}
