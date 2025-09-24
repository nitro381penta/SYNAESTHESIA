using UnityEngine;

[DisallowMultipleComponent]
public class ButterflyVisualizer : MonoBehaviour, IAudioReactive
{
    [Header("Build")]
    [Tooltip("If true, petals are spawned from prefab on Activate.")]
    public bool autoBuild = true;
    public ParticleSystem petalPrefab;
    [Range(3, 64)] public int petalCount = 24;
    public float ringRadius = 6f;
    public float yHeight = 0f;

    [Header("Response")]
    public float baseRate = 2f;
    public float rateMul = 180f;
    public float petalOrbitMul = 6f;
    public float ringRotateSpeed = 10f;

    [Header("Beat")]
    public int beatBurst = 30;
    public float beatRateBoost = 1.2f;
    public float beatDecay = 3f;

    [Header("Color")]
    public Gradient ringColors;
    public float hueRotateDegPerSec = 12f;

    [Header("Optional: manual petals (if autoBuild=false)")]
    public ParticleSystem[] petals;

    Transform _ring;
    float _beatPulse;
    float _hueOffset;

    void Awake()
    {
        _ring = new GameObject("ButterflyRing").transform;
        _ring.SetParent(transform, false);
        _ring.localPosition = new Vector3(0f, yHeight, 0f);

        if (ringColors == null || ringColors.colorKeys.Length == 0)
        {
            ringColors = new Gradient();
            ringColors.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f,0.5f,0.1f), 0f),
                    new GradientColorKey(new Color(1f,0.2f,0.2f), 0.25f),
                    new GradientColorKey(new Color(0.3f,0.7f,1f), 0.5f),
                    new GradientColorKey(new Color(0.8f,0.5f,1f), 0.75f),
                    new GradientColorKey(new Color(1f,1f,0.35f), 1f)
                },
                new[] { new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) }
            );
        }
    }

    // Auto-start/stop when VisualizerManager toggles this root
    void OnEnable()  { Activate();  }
    void OnDisable() { Deactivate(); }

    public void Activate()
    {
        if (autoBuild)
        {
            ClearChildren(_ring);
            if (!petalPrefab) { Debug.LogError("[ButterflyVisualizer] Assign petalPrefab."); return; }

            petals = new ParticleSystem[petalCount];
            float step = 360f / Mathf.Max(1, petalCount);

            for (int i = 0; i < petalCount; i++)
            {
                float angle = step * i;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 pos = dir * ringRadius;

                var ps = Instantiate(petalPrefab, _ring);
                ps.transform.localPosition = pos;
                ps.transform.forward = dir;
                ps.gameObject.name = "Petal_" + i;

                PrepPetal(ps);
                ColorizePetal(ps, IndexToColor(i));

                if (!ps.isPlaying) ps.Play();
                petals[i] = ps;
            }
        }
        else
        {
            if (petals == null || petals.Length == 0) { Debug.LogError("[ButterflyVisualizer] Assign 'petals' when autoBuild=false."); return; }
            foreach (var ps in petals) { if (!ps) continue; PrepPetal(ps); if (!ps.isPlaying) ps.Play(); }
        }

        _beatPulse = 0f;
        _hueOffset = 0f;
        _ring.localPosition = new Vector3(0f, yHeight, 0f);
    }

    public void Deactivate()
    {
        if (petals == null) return;
        foreach (var ps in petals)
            if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void React(float[] spectrum, float[] waveform, bool beat, float level)
    {
        if (petals == null || petals.Length == 0 || spectrum == null || spectrum.Length == 0) return;

        // Ring posture
        _ring.Rotate(Vector3.up, ringRotateSpeed * Time.deltaTime, Space.Self);
        _ring.localPosition = new Vector3(0f, yHeight, 0f);
        _hueOffset = Mathf.Repeat(_hueOffset + (hueRotateDegPerSec / 360f) * Time.deltaTime, 1f);

        // Beat pulse envelope
        _beatPulse = Mathf.Max(0f, _beatPulse - beatDecay * Time.deltaTime);
        if (beat) _beatPulse = Mathf.Min(1f, _beatPulse + 1f);

        // Split spectrum into equal bands across petals
        int bins = Mathf.Max(1, petals.Length);
        int bandSize = Mathf.Max(1, spectrum.Length / bins);

        for (int i = 0; i < petals.Length; i++)
        {
            var ps = petals[i];
            if (!ps) continue;

            int start = i * bandSize;
            int end   = Mathf.Min(spectrum.Length, start + bandSize);

            float sum = 0f;
            for (int b = start; b < end; b++) sum += spectrum[b];
            float energy = sum / Mathf.Max(1, (end - start));

            // Emission with beat boost
            var em = ps.emission;
            float boost = 1f + _beatPulse * beatRateBoost;
            em.rateOverTime = baseRate + (energy + level) * rateMul * boost;

            // Orbital swirl per petal
            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.orbitalY = (energy + level) * petalOrbitMul;

            // extra particles on beat
            if (beat && beatBurst > 0)
                ps.Emit(Mathf.RoundToInt(beatBurst * Mathf.Clamp01(level + energy)));

            // Slow color drift around ring
            ColorizePetal(ps, IndexToColor(i));
        }
    }

    // helpers

    void PrepPetal(ParticleSystem ps)
    {
        var main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = Mathf.Max(0.2f, main.startSpeed.constant);
        main.startLifetime = Mathf.Max(0.8f, main.startLifetime.constant);
        main.startSize = Mathf.Max(0.03f, main.startSize.constant);

        var em = ps.emission; em.enabled = true;

        var shape = ps.shape; shape.enabled = true;
        if (shape.shapeType == ParticleSystemShapeType.Cone || shape.shapeType == ParticleSystemShapeType.ConeVolume)
        {
            shape.radius = Mathf.Max(0.01f, shape.radius);
            shape.angle = Mathf.Clamp(shape.angle, 0f, 12f);
        }

        var vol = ps.velocityOverLifetime; vol.enabled = true;
        vol.orbitalX = 0f; vol.orbitalZ = 0f;
    }

    void ColorizePetal(ParticleSystem ps, Color c)
    {
        var main = ps.main;
        c.a = 1f;
        main.startColor = c; // additive/transparent material
    }

    Color IndexToColor(int i)
    {
        float t = (petalCount <= 1) ? 0f : (float)i / (petalCount - 1);
        t = Mathf.Repeat(t + _hueOffset, 1f);
        return ringColors.Evaluate(t);
    }

    void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }
}
