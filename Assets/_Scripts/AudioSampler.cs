using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class AudioSampler : MonoBehaviour
{
    public enum SourceMode { ManualLastTapped, AutoLoudest, MixAudioListener }

    [Header("Source Selection")]
    public SourceMode sourceMode = SourceMode.ManualLastTapped;
    public AudioSource Source; 

    [Header("Capture")]
    [Range(256, 8192)] public int fftSize = 512;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;

    [Header("Gain & Beat")]
    [Range(0.1f, 4f)] public float spectrumGain = 1.3f;
    public int   fluxHistory = 43;
    public float fluxThresholdMul = 1.5f;

    // Public outputs
    public float[] Spectrum { get; private set; }
    public float[] Waveform { get; private set; }
    public bool    Beat     { get; private set; }
    public float   Level    { get; private set; }

    // Internals
    private float[] _prevSpectrum;
    private Queue<float> _flux;
    private readonly List<AudioSource> _registry = new();
    private float[] _probeBuf;

    // —— API for bubbles ——
    public void RegisterSource(AudioSource s)
    {
        if (s && !_registry.Contains(s)) _registry.Add(s);
    }
    public void UnregisterSource(AudioSource s)
    {
        if (!s) return;
        _registry.Remove(s);
        if (Source == s) Source = null;
    }
    public void SetManualSource(AudioSource s)
    {
        Source = s;
        sourceMode = SourceMode.ManualLastTapped;
        if (s) RegisterSource(s);
    }

    void Awake()
    {
        Spectrum      = new float[fftSize];
        Waveform      = new float[fftSize];
        _prevSpectrum = new float[fftSize];
        _probeBuf     = new float[Mathf.Min(fftSize, 512)]; // quick RMS probe
        _flux         = new Queue<float>(fluxHistory);
    }

    void Update()
    {
        // 1) Resolve which source to sample
        AudioSource src = ResolveSource();

        // 2) Capture spectrum + waveform
        if (src != null)
        {
            src.GetSpectrumData(Spectrum, 0, fftWindow);
            src.GetOutputData (Waveform,  0);
        }
        else
        {
            AudioListener.GetSpectrumData(Spectrum, 0, fftWindow);
            AudioListener.GetOutputData (Waveform,  0);
        }

        for (int i = 0; i < Spectrum.Length; i++)
            Spectrum[i] *= spectrumGain;

        // 3) Level (RMS)
        float sumSq = 0f;
        for (int i = 0; i < Waveform.Length; i++) { float v = Waveform[i]; sumSq += v * v; }
        Level = Mathf.Sqrt(sumSq / Mathf.Max(1, Waveform.Length));

        // 4) Spectral flux beat
        float flux = 0f;
        int n = Mathf.Min(fftSize, Spectrum.Length);
        for (int i = 0; i < n; i++)
        {
            float v = Spectrum[i] - _prevSpectrum[i];
            if (v > 0f) flux += v;
            _prevSpectrum[i] = Spectrum[i];
        }
        if (_flux.Count >= fluxHistory) _flux.Dequeue();
        _flux.Enqueue(flux);

        float avg = 0f; foreach (var f in _flux) avg += f;
        avg /= Mathf.Max(1, _flux.Count);
        Beat = flux > avg * fluxThresholdMul;
    }

    AudioSource ResolveSource()
    {
        switch (sourceMode)
        {
            case SourceMode.MixAudioListener:
                return null; 
            case SourceMode.ManualLastTapped:
                return Source ? Source : null;
            case SourceMode.AutoLoudest:
                return PickLoudest();
            default:
                return Source;
        }
    }

    AudioSource PickLoudest()
    {
        AudioSource best = null;
        float bestRms = 0f;

        for (int i = _registry.Count - 1; i >= 0; i--)
        {
            var s = _registry[i];
            if (!s) { _registry.RemoveAt(i); continue; }
            if (!s.enabled || !s.gameObject.activeInHierarchy) continue;
            if (!s.isPlaying || s.volume <= 0f) continue;

            // Quick probe RMS
            s.GetOutputData(_probeBuf, 0);
            float sum = 0f;
            for (int j = 0; j < _probeBuf.Length; j++) { float v = _probeBuf[j]; sum += v * v; }
            float rms = Mathf.Sqrt(sum / _probeBuf.Length);

            if (rms > bestRms) { bestRms = rms; best = s; }
        }
        return best; 
    }
}
