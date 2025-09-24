using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class MicrophoneRecorder : MonoBehaviour
{
    [Header("Device")]
    public string microphoneDevice;
    public bool autoMatchSampleRate = true;
    public int sampleRate = 44100;

    [Header("Buffer / Latency")]
    public bool loopRecording = true;
    [Range(1, 30)] public int recordLengthSec = 10;

    [Header("Monitoring")]
    public bool monitorAudio = false;
    [Range(0f, 1f)] public float monitorVolume = 0.0f;

    [Header("Integration")]
    public AudioSampler sampler;
    public bool wireSamplerWhileActive = false; 

    [Header("Meter")]
    [Range(64, 4096)] public int meterSamples = 1024;

    public float LevelRMS  { get; private set; }
    public float LevelPeak { get; private set; }
    public bool  IsRecording { get; private set; }

    AudioSource _src;
    const string TAG = "[MIC]";

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = true;
        _src.spatialBlend = 0f;
        _src.ignoreListenerVolume = true;
        _src.ignoreListenerPause  = true;

        if (!sampler) sampler = FindFirstObjectByType<AudioSampler>();
        ApplyMonitorState();
    }

    public void StartRecording()
    {
        if (IsRecording) return;
        if (Microphone.devices.Length == 0) { Debug.LogWarning($"{TAG} No microphone device found."); return; }

        if (string.IsNullOrEmpty(microphoneDevice)) microphoneDevice = Microphone.devices[0];

        int sr = sampleRate;
        if (autoMatchSampleRate)
        {
            Microphone.GetDeviceCaps(microphoneDevice, out var min, out var max);
            sr = (max > 0) ? max : AudioSettings.outputSampleRate;
        }
        sampleRate = sr;

        _src.Stop();
        _src.clip = Microphone.Start(microphoneDevice, loopRecording, recordLengthSec, sampleRate);
        _src.loop = true;
        StartCoroutine(WaitAndPlay());
    }

    IEnumerator WaitAndPlay()
    {
        float t = 0f;
        while (Microphone.GetPosition(microphoneDevice) <= 0 && t < 3f)
        {
            t += Time.unscaledDeltaTime; yield return null;
        }

        if (!_src.clip) { Debug.LogWarning($"{TAG} WaitAndPlay() got no clip."); yield break; }

        _src.Play();
        IsRecording = true;
        ApplyMonitorState();

        if (wireSamplerWhileActive && sampler)
        {
            // Intentionally avoid rewiring;
            // sampler.SetManualSource(_src);
        }
    }

    public void StopRecording()
    {
        if (!IsRecording) return;

        Microphone.End(microphoneDevice);
        _src.Stop();
        IsRecording = false;

        if (wireSamplerWhileActive && sampler)
        {
            // sampler.sourceMode = AudioSampler.SourceMode.MixAudioListener;
        }
    }

    void OnDisable()
    {
        if (IsRecording) StopRecording();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (data == null || data.Length == 0) { LevelRMS = LevelPeak = 0f; return; }

        double sum = 0.0; float peak = 0f;
        int step = Mathf.Max(1, data.Length / Mathf.Min(data.Length, meterSamples));

        for (int i = 0; i < data.Length; i += step)
        {
            float v = Mathf.Abs(data[i]);
            sum += v * v;
            if (v > peak) peak = v;
        }

        LevelRMS  = Mathf.Sqrt((float)(sum / (data.Length / step)));
        LevelPeak = peak;
    }

    public void SetMonitor(bool on, float volume = 0f)
    {
        monitorAudio = on;
        monitorVolume = Mathf.Clamp01(volume);
        ApplyMonitorState();
    }

    void ApplyMonitorState()
    {
        if (!_src) return;
        if (monitorAudio) { _src.mute = false; _src.volume = monitorVolume; }
        else { _src.mute = false; _src.volume = 0f; } // keep graph alive
    }
}
