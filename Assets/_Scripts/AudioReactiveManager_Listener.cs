#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine;
using System.Collections.Generic;

public class AudioReactiveManager_Listener : MonoBehaviour
{
    [Header("Analysis")]
    [Range(64, 8192)] public int spectrumSize = 512;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;

    [Header("Beat Detection")]
    [Range(1.1f, 3f)] public float beatThresholdFactor = 1.5f;
    [Range(0.05f, 0.5f)] public float beatCooldown = 0.20f;
    [Range(0.005f, 0.5f)] public float lowBandFraction = 1f / 32f;

    [Header("Microphone")]
    public bool startMicOnAwake = true;
    [Tooltip("Not zero so the listener 'hears' the mic for analysis.")]
    [Range(0f, 0.2f)] public float micAudibleVolume = 0.05f;
    [Range(1, 30)] public int micBufferSeconds = 10;

    float[] spectrum, waveform;
    float lowEnergyAvg, lastBeatTime;
    AudioSource micSrc;

    readonly List<IAudioReactive> reactives = new();

    bool _askedPermission;
    bool _micStarted;

    void Awake()
    {
        spectrum = new float[spectrumSize];
        waveform = new float[spectrumSize];

        // collect current reactives
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            if (mb is IAudioReactive r && mb.isActiveAndEnabled) reactives.Add(r);

        // Android mic permission prompt (Quest)
        #if UNITY_ANDROID
        if (startMicOnAwake && !_askedPermission)
        {
            _askedPermission = true;
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                Permission.RequestUserPermission(Permission.Microphone);
            else
                TryStartMic();
        }
        #else
        if (startMicOnAwake) TryStartMic();
        #endif
    }

    void Update()
    {
        // Late permission grant handling on Android
        #if UNITY_ANDROID
        if (startMicOnAwake && !_micStarted && _askedPermission &&
            Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            TryStartMic();
        }
        #endif

        // keep buffers in sync if changed in inspector
        if (spectrum.Length != spectrumSize || waveform.Length != spectrumSize)
        {
            spectrum = new float[spectrumSize];
            waveform = new float[spectrumSize];
        }

        // Mixed output (music + mic) from the listener
        AudioListener.GetOutputData(waveform, 0);
        AudioListener.GetSpectrumData(spectrum, 0, fftWindow);

        // Level (RMS)
        float sum = 0f; for (int i = 0; i < waveform.Length; i++) { float v = waveform[i]; sum += v * v; }
        float level = Mathf.Sqrt(sum / Mathf.Max(1, waveform.Length));

        // Simple onset on low band
        int lowCount = Mathf.Clamp(Mathf.RoundToInt(spectrum.Length * lowBandFraction), 1, spectrum.Length);
        float lowEnergy = 0f; for (int i = 0; i < lowCount; i++) lowEnergy += spectrum[i];
        lowEnergyAvg = Mathf.Lerp(lowEnergyAvg, lowEnergy, 0.10f);

        bool beat = false;
        if (lowEnergy > lowEnergyAvg * beatThresholdFactor && (Time.time - lastBeatTime) > beatCooldown)
        {
            beat = true; lastBeatTime = Time.time;
        }

        // Drive active reactives
        for (int i = reactives.Count - 1; i >= 0; i--)
        {
            var mb = reactives[i] as MonoBehaviour;
            if (!mb) { reactives.RemoveAt(i); continue; }
            if (!mb.isActiveAndEnabled) continue;
            reactives[i].React(spectrum, waveform, beat, level);
        }
    }

    public void RefreshReactives()
    {
        reactives.Clear();
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            if (mb is IAudioReactive r && mb.isActiveAndEnabled) reactives.Add(r);
    }

    void TryStartMic()
    {
        if (_micStarted) return;
        if (Microphone.devices.Length == 0) { Debug.LogWarning("[AR] No microphone found."); return; }

        if (!micSrc) micSrc = gameObject.AddComponent<AudioSource>();
        micSrc.loop = true;
        micSrc.playOnAwake = false;
        micSrc.spatialBlend = 0f;
        micSrc.ignoreListenerPause = true;
        micSrc.ignoreListenerVolume = true;

        int sr = AudioSettings.outputSampleRate;
        micSrc.clip = Microphone.Start(null, true, Mathf.Max(1, micBufferSeconds), sr);
        StartCoroutine(WaitThenPlay());
    }

    System.Collections.IEnumerator WaitThenPlay()
    {
        float t = 0f;
        while (Microphone.GetPosition(null) <= 0 && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
        if (!micSrc.clip) { Debug.LogWarning("[AR] Mic clip missing."); yield break; }

        micSrc.volume = micAudibleVolume; // small but non-zero
        micSrc.mute = false;
        micSrc.Play();
        _micStarted = true;
        Debug.Log("[AR] Microphone streaming to listener.");
    }
}
