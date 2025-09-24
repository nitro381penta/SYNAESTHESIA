using UnityEngine;

[DisallowMultipleComponent]
public class WavesVisualizer : MonoBehaviour, IAudioReactive
{
    [Header("Rings")]
    [Range(1, 64)] public int ringCount = 18;
    [Range(64, 2048)] public int pointsPerRing = 512;
    public float baseRadius = 7f;
    public float ringSpacing = 1.0f;

    [Header("Amplitude & Motion")]
    public float waveformAmp = 2.0f;
    public float spectrumAmp = 1.0f;
    public float rotationDegPerSec = 9f;
    public float yHeight = 0f;
    public float yWaveAmp = 0.25f;
    public float lineWidth = 0.06f;

    [Header("Band Ranges (as 0..1 of spectrum length)")]
    [Range(0f, 1f)] public float lowEnd = 0.02f;
    [Range(0f, 1f)] public float highEnd = 0.65f;

    [Header("Noise (fractal)")]
    public float noiseAmp = 0.8f;
    public float noiseFrequency = 0.45f;
    public int   noiseOctaves = 3;
    public float noiseGain = 0.55f;
    public float noiseScroll = 0.25f;

    [Header("Smoothing")]
    public float ampSmooth = 0.12f;
    public float radiusSmooth = 0.06f;

    [Header("Beat Pulse")]
    public float beatPulseAdd = 0.7f;
    public float beatPulseDecay = 1.6f;
    public float beatWidthMul = 0.9f;

    [Header("Look")]
    public Material lineMaterial;
    public Gradient colorOverRings;
    public bool useAngleRainbow = true;
    public Gradient colorOverAngle;

    LineRenderer[] _lr;
    Vector3[][] _positions;
    float[] _radiusBase;
    float _phase;
    float _pulse;
    float _energySmoothed;
    bool  _primed;

    void Awake()
    {
        if (colorOverRings == null || colorOverRings.colorKeys.Length == 0)
        {
            colorOverRings = new Gradient();
            colorOverRings.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.2f,0.9f,1f), 0f),
                    new GradientColorKey(new Color(0.9f,0.5f,1f), 1f)
                },
                new[] { new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) }
            );
        }
        if (colorOverAngle == null || colorOverAngle.colorKeys.Length == 0)
        {
            colorOverAngle = new Gradient();
            colorOverAngle.SetKeys(
                new[] {
                    new GradientColorKey(Color.cyan, 0f),
                    new GradientColorKey(Color.blue, 0.2f),
                    new GradientColorKey(new Color(0.8f,0.2f,1f), 0.4f),
                    new GradientColorKey(Color.magenta, 0.6f),
                    new GradientColorKey(Color.yellow, 0.8f),
                    new GradientColorKey(Color.cyan, 1f),
                },
                new[] { new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) }
            );
        }
        EnsureRings();
    }

    void EnsureRings()
    {
        if (_lr != null && _lr.Length == ringCount)
        {
            for (int r = 0; r < ringCount; r++)
                if (_lr[r]) _lr[r].positionCount = pointsPerRing;
            return;
        }

        if (_lr != null)
            for (int i = 0; i < _lr.Length; i++)
                if (_lr[i]) DestroyImmediate(_lr[i].gameObject);

        _lr = new LineRenderer[ringCount];
        _positions = new Vector3[ringCount][];
        _radiusBase = new float[ringCount];

        for (int r = 0; r < ringCount; r++)
        {
            var go = new GameObject($"WaveRing_{r}");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.allowOcclusionWhenDynamic = false;
            lr.positionCount = pointsPerRing;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.textureMode = LineTextureMode.Stretch;

            if (lineMaterial) lr.material = lineMaterial;

            var width = new AnimationCurve();
            width.AddKey(0f, lineWidth);
            width.AddKey(1f, lineWidth);
            lr.widthCurve = width;

            var ringT = ringCount <= 1 ? 0f : (float)r / (ringCount - 1);
            var ringColor = colorOverRings.Evaluate(ringT);
            lr.colorGradient = new Gradient
            {
                colorKeys = new[] {
                    new GradientColorKey(ringColor, 0f),
                    new GradientColorKey(ringColor, 1f)
                },
                alphaKeys = new[] { new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) }
            };

            lr.enabled = false;

            _lr[r] = lr;
            _positions[r] = new Vector3[pointsPerRing];
            _radiusBase[r] = baseRadius + r * ringSpacing;
        }

        _primed = false;
    }

    public void Activate()
    {
        EnsureRings();
        _pulse = 0f;
        _primed = false;
    }

    public void Deactivate()
    {
        if (_lr == null) return;
        foreach (var lr in _lr) if (lr) lr.enabled = false;
    }

    public void React(float[] spectrum, float[] waveform, bool beat, float level)
    {
        if (_lr == null || _lr.Length == 0) return;

        _phase += rotationDegPerSec * Mathf.Deg2Rad * Time.deltaTime;

        // energies
        float specEnergy = 0f;
        if (spectrum != null && spectrum.Length > 0)
        {
            int i0 = Mathf.Clamp(Mathf.FloorToInt(lowEnd * spectrum.Length), 0, spectrum.Length-1);
            int i1 = Mathf.Clamp(Mathf.FloorToInt(highEnd * spectrum.Length), i0, spectrum.Length-1);
            for (int i = i0; i <= i1; i++) specEnergy += spectrum[i];
            specEnergy /= Mathf.Max(1, (i1 - i0 + 1));
        }

        float wfEnergy = 0f;
        if (waveform != null && waveform.Length > 0)
        {
            double sum = 0;
            for (int i = 0; i < waveform.Length; i++) { double v = waveform[i]; sum += v*v; }
            wfEnergy = Mathf.Sqrt((float)(sum / Mathf.Max(1, waveform.Length)));
        }

        float rawEnergy = Mathf.Clamp01(level + 0.6f * specEnergy + 0.4f * wfEnergy);
        _energySmoothed = Mathf.Lerp(_energySmoothed, rawEnergy,
            1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, ampSmooth)));

        _pulse = Mathf.Max(0f, _pulse - beatPulseDecay * Time.deltaTime);
        if (beat) _pulse = Mathf.Min(1f, _pulse + beatPulseAdd);

        float tNow = Time.time;
        for (int r = 0; r < ringCount; r++)
        {
            float rBase = _radiusBase[r];
            float ringPhase = _phase + r * 0.35f;
            float amp = waveformAmp * _energySmoothed + spectrumAmp * specEnergy + _pulse * (1f - 0.05f*r);

            var lr = _lr[r];
            var wc = lr.widthCurve;
            wc.MoveKey(0, new Keyframe(0f, lineWidth * (1f + beatWidthMul * _pulse)));
            wc.MoveKey(1, new Keyframe(1f, lineWidth * (1f + beatWidthMul * _pulse)));
            lr.widthCurve = wc;

            for (int i = 0; i < pointsPerRing; i++)
            {
                float t = i / (float)pointsPerRing;
                float ang = t * Mathf.PI * 2f + ringPhase;

                float wfSample = 0f;
                if (waveform != null && waveform.Length > 0)
                    wfSample = waveform[Mathf.Clamp(Mathf.FloorToInt(t * waveform.Length), 0, waveform.Length-1)];

                float nx = Mathf.Cos(ang) * noiseFrequency;
                float ny = Mathf.Sin(ang) * noiseFrequency;
                float n = fBm(nx + tNow * noiseScroll, ny + tNow * noiseScroll, noiseOctaves, noiseGain);
                n = (n * 2f - 1f);

                float targetRadius = rBase + amp * wfSample + noiseAmp * n;

                float prevRadius = _positions[r][i] == Vector3.zero
                    ? rBase
                    : new Vector2(_positions[r][i].x - transform.position.x,
                                  _positions[r][i].z - transform.position.z).magnitude;

                float smoothedRadius = Mathf.Lerp(prevRadius, targetRadius,
                    1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, radiusSmooth)));

                float y = yHeight + (yWaveAmp > 0f ? Mathf.Sin(ang * 2f + tNow * 0.7f + r * 0.5f) * yWaveAmp : 0f);

                Vector3 pos = transform.position +
                              new Vector3(Mathf.Cos(ang) * smoothedRadius, y, Mathf.Sin(ang) * smoothedRadius);
                _positions[r][i] = pos;
            }

            lr.SetPositions(_positions[r]);

            if (lineMaterial && lr.material != null)
            {
                var ringT = ringCount <= 1 ? 0f : (float)r / (ringCount - 1);
                var ringCol = colorOverRings.Evaluate(ringT);
                if (useAngleRainbow)
                {
                    float h, s, v; Color.RGBToHSV(ringCol, out h, out s, out v);
                    h = Mathf.Repeat(h + (_phase * 0.02f), 1f);
                    ringCol = Color.HSVToRGB(h, s, v);
                }
                lr.material.SetColor("_BaseColor", ringCol);
            }
        }

        // after the first frame of valid positions, show lines
        if (!_primed)
        {
            _primed = true;
            foreach (var lr in _lr) if (lr) lr.enabled = true;
        }
    }

    float fBm(float x, float y, int oct, float gain)
    {
        float amp = 1f;
        float sum = 0f;
        float freq = 1f;
        for (int i = 0; i < Mathf.Max(1, oct); i++)
        {
            sum += amp * Mathf.PerlinNoise(x * freq, y * freq);
            amp *= gain;
            freq *= 2f;
        }
        float norm = (1f - Mathf.Pow(gain, Mathf.Max(1, oct))) / (1f - gain + 1e-6f);
        return sum / Mathf.Max(1e-6f, norm);
    }
}
