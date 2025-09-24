using UnityEngine;
public class KaleidoComfort : MonoBehaviour, IAudioReactive
{
    [Header("Prefab (drag kaleidoscope prefab)")]
    public GameObject prefab;

    [Header("Emission")]
    public float baseRate = 3f;             // idle particles
    public float rateMul  = 140f;           // high energy leading to extra particles

    [Header("Comfort Motion (very small)")]
    [Tooltip("World-space orbital swirl amount driven by energy")]
    public float orbitYMul = 6f;
    [Tooltip("Shape radius will move inside this range by energy")]
    public Vector2 shapeRadiusRange = new Vector2(3.5f, 9f);
    [Tooltip("Breathing scale around 1.0 (keep tiny)")]
    public float breathAmplitude = 0.05f;   // ±5%
    public float breathLerp = 4f;           // responsiveness

    [Header("Beat")]
    public int   beatBurst     = 20;
    public float beatRateBoost = 1.15f;     // subtle
    public float beatDecay     = 3f;        // /s

    // internals
    Transform _rig;
    ParticleSystem _ps;
    float _beatPulse;
    float _breathTarget = 1f;
    float _breath = 1f;

    void Awake()
    {
        _rig = transform;
    }

    public void Activate()
    {
        if (!_ps && prefab)
        {
            var go = Instantiate(prefab, _rig);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;
            _ps = go.GetComponentInChildren<ParticleSystem>();
            Prep(_ps);
            _ps.Play();
        }
        _beatPulse = 0f;
        _breath = _breathTarget = 1f;
    }

    public void Deactivate()
    {
        if (_ps)
        {
            var go = _ps.gameObject;
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(go);
            _ps = null;
        }
        _rig.localScale = Vector3.one;
    }

    public void React(float[] spectrum, float[] waveform, bool beat, float level)
    {
        if (!_ps || spectrum == null || spectrum.Length == 0) return;

        // energy
        int mid = Mathf.FloorToInt(spectrum.Length * 0.25f);
        int hi  = Mathf.FloorToInt(spectrum.Length * 0.55f);
        float e = Avg(spectrum, mid, hi) * 0.6f + Avg(spectrum, hi, spectrum.Length) * 0.9f + level * 0.5f;
        e = Mathf.Clamp01(e * 3f);

        // beat envelope
        _beatPulse = Mathf.Max(0f, _beatPulse - beatDecay * Time.deltaTime);
        if (beat)
        {
            _beatPulse = Mathf.Min(1f, _beatPulse + 1f);
            if (beatBurst > 0) _ps.Emit(Mathf.RoundToInt(beatBurst * Mathf.Clamp01(level + e)));
        }

        // emission
        float boost = 1f + _beatPulse * beatRateBoost;
        var em = _ps.emission;
        em.rateOverTime = (baseRate + e * rateMul) * boost;

        // world-space swirl
        var vol = _ps.velocityOverLifetime;
        vol.enabled = true;
        vol.orbitalX = 0f; vol.orbitalZ = 0f;
        vol.orbitalY = e * orbitYMul;

        // radius
        var shape = _ps.shape;
        float r = Mathf.Lerp(shapeRadiusRange.x, shapeRadiusRange.y, e);
        shape.radius = Mathf.Clamp(r, 0.01f, 30f);

        // breath scale
        _breathTarget = 1f + breathAmplitude * (e * 0.7f + _beatPulse * 0.3f);
        _breath = Mathf.Lerp(_breath, _breathTarget, 1f - Mathf.Exp(-breathLerp * Time.deltaTime));
        _rig.localScale = new Vector3(_breath, _breath, _breath);
    }

    void Prep(ParticleSystem ps)
    {
        if (!ps) return;
        var main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(6000, main.maxParticles);

        if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            main.startLifetime = Mathf.Clamp(main.startLifetime.constant, 0.6f, 6f);
        if (main.startSpeed.mode == ParticleSystemCurveMode.Constant)
            main.startSpeed = Mathf.Clamp(main.startSpeed.constant, 0.2f, 4f);
        if (main.startSize.mode == ParticleSystemCurveMode.Constant)
            main.startSize = Mathf.Clamp(main.startSize.constant, 0.03f, 0.5f);

        var trails = ps.trails; trails.enabled = false;
        var coll   = ps.collision; coll.enabled = false;
    }

    static float Avg(float[] a, int i0, int i1)
    {
        i0 = Mathf.Clamp(i0, 0, a.Length);
        i1 = Mathf.Clamp(i1, 0, a.Length);
        int n = Mathf.Max(1, i1 - i0);
        float s = 0f; for (int i = i0; i < i1; i++) s += a[i];
        return s / n;
    }
}
