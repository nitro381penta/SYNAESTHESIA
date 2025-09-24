using UnityEngine;

public class FireworksVisualizer : MonoBehaviour, IAudioReactive
{
    [Header("References")]
    public ParticleSystem launcher;

    [Header("Launch Area")]
    public float launchRadius = 6f;
    public Vector2 launchHeightRange = new Vector2(0.0f, 0.4f);

    [Header("Rocket Motion")]
    public Vector2 launchSpeed = new Vector2(8f, 14f);
    public float lateralJitter = 1.4f;
    public Vector2 rocketLifetime = new Vector2(1.3f, 2.0f);
    public float rocketGravity = 0.8f;

    [Header("Beat → Rockets")]
    public int rocketsOnSoftBeat = 1;
    public int rocketsOnHardBeat = 3;
    public float minBeatInterval = 0.10f;

    public float idleRate = 2f;

    [Header("Energy Response")]
    public float energySmoothTau = 0.06f;
    public bool impulseOnRise = true;
    [Range(0.005f, 0.2f)] public float impulseThreshold = 0.035f;
    public float impulseCooldown = 0.08f;

    [Header("Color (audio-reactive)")]
    [Range(0f, 1f)] public float hueFromSpectrum = 1f;
    [Range(0f, 0.25f)] public float hueJitter = 0.06f;
    public float hueSmooth = 0.22f;
    public Vector2 satRange = new Vector2(0.7f, 1f);
    public Vector2 valRange = new Vector2(0.8f, 1.2f);
    public Gradient fallbackGradient;

    float _lastBeatTime = -999f;
    float _lastImpulseTime = -999f;
    float _energySmoothed;
    float _energyPrev;
    float _hueSmoothed;

    void Awake()
    {
        if (fallbackGradient == null || fallbackGradient.colorKeys.Length == 0)
        {
            fallbackGradient = new Gradient();
            fallbackGradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f,0.55f,0.1f), 0f),
                    new GradientColorKey(new Color(1f,0.2f,0.2f), 0.25f),
                    new GradientColorKey(new Color(0.3f,0.7f,1f), 0.5f),
                    new GradientColorKey(new Color(0.8f,0.5f,1f), 0.75f),
                    new GradientColorKey(new Color(1f,1f,0.35f), 1f)
                },
                new[] { new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) }
            );
        }
    }

    public void Activate()
    {
        if (!launcher) { Debug.LogError("[FireworksVisualizer] Assign 'launcher'."); return; }
        ConfigureLauncher();
        if (!launcher.isPlaying) launcher.Play();
        _lastBeatTime = _lastImpulseTime = -999f;
        _energySmoothed = _energyPrev = 0f;
    }

    public void Deactivate()
    {
        if (launcher) launcher.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void React(float[] spectrum, float[] waveform, bool beat, float level)
    {
        if (!launcher) return;

        float hi = 0f;
        if (spectrum != null && spectrum.Length > 0)
        {
            int start = Mathf.FloorToInt(spectrum.Length * 0.55f);
            for (int i = start; i < spectrum.Length; i++) hi += spectrum[i];
            hi /= Mathf.Max(1, spectrum.Length - start);
        }
        float energy = Mathf.Clamp01(level + hi);

        float tau = Mathf.Max(0.008f, energySmoothTau);
        _energySmoothed = Mathf.Lerp(_energySmoothed, energy, 1f - Mathf.Exp(-Time.deltaTime / tau));
        float rise = Mathf.Max(0f, _energySmoothed - _energyPrev);
        _energyPrev = _energySmoothed;

        float targetHue = _hueSmoothed;
        if (spectrum != null && spectrum.Length > 0 && hueFromSpectrum > 0f)
        {
            double num = 0, den = 0;
            for (int i = 0; i < spectrum.Length; i++) { double a = spectrum[i]; num += a * i; den += a; }
            double centroid = (den > 1e-6) ? (num / den) : 0.0;
            targetHue = (float)(centroid / (spectrum.Length - 1));
        }
        _hueSmoothed = SmoothHue(_hueSmoothed, targetHue, hueSmooth);

        var em = launcher.emission;
        em.rateOverTime = idleRate * Mathf.SmoothStep(0f, 1f, _energySmoothed);

        bool canBeat = (Time.time - _lastBeatTime) > minBeatInterval;
        if (beat && canBeat)
        {
            int rockets = Mathf.RoundToInt(Mathf.Lerp(rocketsOnSoftBeat, rocketsOnHardBeat, _energySmoothed));
            rockets = Mathf.Max(1, rockets);
            for (int i = 0; i < rockets; i++) EmitRocket(_energySmoothed);
            _lastBeatTime = Time.time;
        }
        else if (impulseOnRise && rise >= impulseThreshold && (Time.time - _lastImpulseTime) > impulseCooldown)
        {
            EmitRocket(_energySmoothed);
            _lastImpulseTime = Time.time;
        }
    }

    void EmitRocket(float energy)
    {
        var t = transform;
        Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(0.3f * launchRadius, launchRadius);
        Vector3 spawn = new Vector3(t.position.x + ring.x,
                                    t.position.y + Random.Range(launchHeightRange.x, launchHeightRange.y),
                                    t.position.z + ring.y);

        float speed = Random.Range(launchSpeed.x, launchSpeed.y);
        Vector3 lateral = new Vector3(Random.Range(-lateralJitter, lateralJitter), 0f, Random.Range(-lateralJitter, lateralJitter));
        Vector3 vel = Vector3.up * speed + lateral;
        float life = Random.Range(rocketLifetime.x, rocketLifetime.y);

        float hue = Mathf.Repeat(_hueSmoothed + RandomRangeSigned(hueJitter), 1f);

        if (hueFromSpectrum < 1f)
        {
            Color g = fallbackGradient.Evaluate(Random.value);
            Color h = Color.HSVToRGB(hue, 1f, 1f);
            Color mixed = Color.Lerp(g, h, hueFromSpectrum);
            Color.RGBToHSV(mixed, out hue, out _, out _);
        }

        float sat = Mathf.Lerp(satRange.x, satRange.y, energy);
        float val = Mathf.Lerp(valRange.x, valRange.y, energy);
        Color c = Color.HSVToRGB(hue, Mathf.Clamp01(sat), Mathf.Clamp01(val));
        c.a = 1f;

        var ep = new ParticleSystem.EmitParams
        {
            position = spawn,
            velocity = vel,
            startLifetime = life,
            startColor = c,
            startSize = 0.08f
        };

        launcher.Emit(ep, 1);
    }

    void ConfigureLauncher()
    {
        var main = launcher.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = rocketGravity;
        main.startSpeed = 0f;
        main.startLifetime = rocketLifetime.x;

        var em = launcher.emission; em.enabled = true;

        var tr = launcher.trails; tr.enabled = true; tr.ratio = 1f; tr.lifetime = 0.25f;

        var noise = launcher.noise;
        noise.enabled = true; noise.strength = 0.3f; noise.frequency = 0.25f; noise.scrollSpeed = 0.4f;
    }

    static float RandomRangeSigned(float r) => (float)((Random.value * 2.0 - 1.0) * r);

    static float SmoothHue(float current, float target, float tau)
    {
        if (tau <= 0f) return target;
        float diff = Mathf.DeltaAngle(current * 360f, target * 360f) / 360f;
        float next = current + diff * (1f - Mathf.Exp(-Time.deltaTime / tau));
        return Mathf.Repeat(next, 1f);
    }
}
