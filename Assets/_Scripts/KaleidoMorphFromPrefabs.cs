using System.Collections.Generic;
using UnityEngine;
public class KaleidoMorphFromPrefabs : MonoBehaviour, IAudioReactive
{
    [Header("Prefabs (drag your 4 kaleidoscope prefabs here)")]
    public List<GameObject> prefabs = new();   // Prefab assets

    [Header("Emission")]
    public float baseRate = 4f;          // idle particles
    public float rateMul  = 180f;        // high energy leading to extra particles

    [Header("Motion / Look")]
    public float rigSpinBase = 10f;      // deg/s
    public float rigSpinMul  = 80f;      // extra with energy
    public float orbitYMul   = 10f;      // velocityOverLifetime.orbitalY
    public Vector2 shapeRadiusRange = new Vector2(4f, 14f); // min..max by energy

    [Header("Morphing")]
    public float crossfadeSeconds = 1.0f;
    public int beatsBetweenMorphMin = 8;  // morph cadence
    public int beatsBetweenMorphMax = 16;

    [Header("Beat")]
    public int   beatBurst     = 30;     // extra particles on beat
    public float beatRateBoost = 1.2f;   // short boost to emission
    public float beatDecay     = 3f;     // /s

    // --- internals
    Transform _rig;
    int _curIndex = -1;                 // current prefab
    int _nextIndex = -1;
    ParticleSystem _curPS;
    ParticleSystem _nextPS;
    float _cross;                        // crossfade
    float _beatPulse;                    // envelope
    int _beatsToNext;

    void Awake()
    {
        _rig = transform;
    }

    public void Activate()
    {
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning("KaleidoMorphFromPrefabs: assign prefabs.");
            return;
        }
        if (_curPS == null)
        {
            _curIndex = 0;
            _curPS = Spawn(prefabs[_curIndex]);
            Prep(_curPS);
            _curPS.Play();
        }
        ResetBeatCountdown();
        _beatPulse = 0f;
    }

    public void Deactivate()
    {
        Kill(ref _curPS);
        Kill(ref _nextPS);
        _curIndex = -1; _nextIndex = -1; _cross = 0f; _beatPulse = 0f;
    }

    public void React(float[] spectrum, float[] waveform, bool beat, float level)
    {
        if (_curPS == null || spectrum == null || spectrum.Length == 0) return;

        int mid = Mathf.FloorToInt(spectrum.Length * 0.25f);
        int hi  = Mathf.FloorToInt(spectrum.Length * 0.55f);
        float e = Avg(spectrum, mid, hi) * 0.6f + Avg(spectrum, hi, spectrum.Length) * 0.9f + level * 0.5f;
        e = Mathf.Clamp01(e * 3f);

        _beatPulse = Mathf.Max(0f, _beatPulse - beatDecay * Time.deltaTime);
        if (beat)
        {
            _beatPulse = Mathf.Min(1f, _beatPulse + 1f);
            _beatsToNext--;
            if (beatBurst > 0) _curPS.Emit(Mathf.RoundToInt(beatBurst * Mathf.Clamp01(level + e)));
        }

        // spin the parent
        float spin = rigSpinBase + e * rigSpinMul;
        _rig.Rotate(0f, spin * Time.deltaTime, 0f, Space.Self);

        // drive current
        DrivePS(_curPS, (1f - _cross), e, level);
        if (_nextPS) DrivePS(_nextPS, _cross, e, level);

        // schedule morphs
        if (_nextPS == null && _beatsToNext <= 0 && prefabs.Count > 1)
        {
            _nextIndex = PickNextIndex();
            _nextPS = Spawn(prefabs[_nextIndex]);
            Prep(_nextPS);
            _nextPS.Play();
            _cross = 0f; // start crossfade
        }

        // crossfade
        if (_nextPS)
        {
            _cross = Mathf.MoveTowards(_cross, 1f, Time.deltaTime / Mathf.Max(0.05f, crossfadeSeconds));
            if (_cross >= 1f)
            {
                // swap
                Kill(ref _curPS);
                _curPS = _nextPS; _curIndex = _nextIndex;
                _nextPS = null; _nextIndex = -1; _cross = 0f;
                ResetBeatCountdown();
            }
        }
    }

    // helpers
    ParticleSystem Spawn(GameObject prefab)
    {
        if (!prefab) return null;
        var go = Instantiate(prefab, _rig);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        return go.GetComponentInChildren<ParticleSystem>();
    }

    void Kill(ref ParticleSystem ps)
    {
        if (!ps) return;
        var go = ps.gameObject;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Destroy(go);
        ps = null;
    }

    void Prep(ParticleSystem ps)
    {
        if (!ps) return;

        var main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(8000, main.maxParticles);
        if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            main.startLifetime = Mathf.Clamp(main.startLifetime.constant, 0.6f, 6f);
        if (main.startSpeed.mode == ParticleSystemCurveMode.Constant)
            main.startSpeed = Mathf.Clamp(main.startSpeed.constant, 0.2f, 6f);
        if (main.startSize.mode == ParticleSystemCurveMode.Constant)
            main.startSize = Mathf.Clamp(main.startSize.constant, 0.03f, 0.6f);

        var em = ps.emission; em.enabled = true;

        var shape = ps.shape; shape.enabled = true;
        shape.radius = Mathf.Clamp(shape.radius, 0.01f, 20f);

        var vol = ps.velocityOverLifetime; vol.enabled = true;
        vol.orbitalX = 0f; vol.orbitalZ = 0f;

        var trails = ps.trails; trails.enabled = false;
        var coll = ps.collision; coll.enabled = false;
    }

    void DrivePS(ParticleSystem ps, float weight, float e, float level)
    {
        if (!ps) return;

        // Emission
        float boost = 1f + _beatPulse * beatRateBoost;
        float rate  = (baseRate + e * rateMul) * Mathf.Max(0f, weight) * boost;
        var em = ps.emission; em.rateOverTime = rate;

        // Orbit swirl
        var vol = ps.velocityOverLifetime;
        vol.orbitalY = e * orbitYMul;

        // Radius morph
        var shape = ps.shape;
        float r = Mathf.Lerp(shapeRadiusRange.x, shapeRadiusRange.y, e);
        shape.radius = Mathf.Clamp(r, 0.01f, 30f);
    }

    int PickNextIndex()
    {
        if (prefabs.Count <= 1) return _curIndex >= 0 ? _curIndex : 0;
        int idx; int safety = 10;
        do { idx = Random.Range(0, prefabs.Count); } while (idx == _curIndex && --safety > 0);
        return idx;
    }

    void ResetBeatCountdown()
    {
        _beatsToNext = Random.Range(beatsBetweenMorphMin, beatsBetweenMorphMax + 1);
    }

    static float Avg(float[] a, int i0, int i1)
    {
        i0 = Mathf.Clamp(i0, 0, a.Length); i1 = Mathf.Clamp(i1, 0, a.Length);
        int n = Mathf.Max(1, i1 - i0); float s = 0f; for (int i = i0; i < i1; i++) s += a[i]; return s / n;
    }
}
