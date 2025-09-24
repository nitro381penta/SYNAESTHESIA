using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class BubbleTrigger : MonoBehaviour
{
    [Header("Type")]
    public bool isMicrophoneBubble = false;

    [Header("Audio")]
    public List<AudioClip> audioClips;
    public AudioSource ambientLoopAudio;
    [Range(0f, 1f)] public float ambientBaseVolume = 0.45f;

    private AudioSource _src;

    [Header("UI")]
    public GameObject instructionCanvas;

    [Header("Burst FX")]
    public GameObject burstEffectPrefab;
    public float burstScale = 4f;
    public float burstAutoDestroy = 2f;

    [Header("Integration")]
    public AudioSampler sampler;
    public DarkDomeController dome;

    [Header("Shell")]
    public bool hideShellOnTap = true;

    [Header("Local Fallback (2D)")]
    public bool  useLocal2DFallback   = true;
    public float fallbackFadeOutAfter = 1.0f;
    public float fallbackFadeDuration = 0.6f;

    bool _triggered;

    static readonly List<BubbleTrigger> sAll = new();
    static BubbleTrigger sCoordinator;
    static BubbleTrigger sCurrentAmbient;
    static bool  sAmbientPaused;
    static float sXFadeTime  = 0.45f;
    static float sHysteresis = 0.75f;

    void OnEnable()
    {
        if (!sAll.Contains(this)) sAll.Add(this);
        if (!sCoordinator) sCoordinator = this;
    }
    void OnDisable()
    {
        sAll.Remove(this);
        if (sCoordinator == this) sCoordinator = sAll.Count > 0 ? sAll[0] : null;
        if (sCurrentAmbient == this) sCurrentAmbient = null;
    }

    void Awake()
    {
        BubbleManager.Instance?.RegisterBubble(gameObject);

        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.spatialBlend = 1f;
        _src.rolloffMode = AudioRolloffMode.Logarithmic;
        _src.minDistance = 1f;
        _src.maxDistance = 20f;

        if (!sampler) sampler = FindFirstObjectByType<AudioSampler>();

        if (!dome)
        {
#if UNITY_2022_2_OR_NEWER
            dome = FindFirstObjectByType<DarkDomeController>(FindObjectsInactive.Include);
#else
            dome = FindObjectOfType<DarkDomeController>(true);
#endif
        }

        if (ambientLoopAudio)
        {
            ambientLoopAudio.loop = true;
            ambientLoopAudio.playOnAwake = false;
            ambientLoopAudio.spatialBlend = 1f;
            ambientLoopAudio.volume = 0f;
        }
    }

    void Start()
    {
        instructionCanvas?.SetActive(true);
        Ambient_Resume();
        VisualizerManager.Instance?.SetMode(VisualizerManager.VisualizerMode.None);
    }

    void Update()
    {
        if (this == sCoordinator) AmbientCoordinatorTick();
    }

    public void OnBubbleTapped()
    {
        if (_triggered) return;
        _triggered = true;

        instructionCanvas?.SetActive(false);

        Ambient_PauseAll();

        bool startedLocalFallback = false;
        if (!isMicrophoneBubble && useLocal2DFallback && audioClips != null && audioClips.Count > 0)
        {
            var clip = audioClips[Random.Range(0, audioClips.Count)];
            _src.Stop();
            _src.clip = clip;
            _src.loop = true;
            _src.volume = 1f;
            _src.spatialBlend = 0f; // 2D
            _src.Play();
            startedLocalFallback = true;
        }

        if (isMicrophoneBubble)
        {
            UIManagerXR.Instance?.ShowMicInputUI();
            VisualizerManager.Instance?.SetMode(VisualizerManager.VisualizerMode.Fireworks);
        }
        else
        {
            UIManagerXR.Instance?.PlayPlaylist(audioClips);
            UIManagerXR.Instance?.ShowSoundPlayerUI();
            VisualizerManager.Instance?.SetMode(VisualizerManager.VisualizerMode.Sparkles);
        }

        SpawnBurst();
        dome?.Enter();

        BubbleManager.Instance?.HideAllExcept(gameObject);
        if (hideShellOnTap) HideShellOnly();

        if (startedLocalFallback) StartCoroutine(FadeOutLocalFallback());
    }

    public void ResetBubble()
    {
        _triggered = false;
        ShowShell();
        instructionCanvas?.SetActive(true);
        Ambient_Resume();
    }

    public void OnHomePressed()
    {
        dome?.Exit();
        BubbleManager.Instance?.ShowAll();
        UIManagerXR.Instance?.ResetUI();
        Ambient_Resume();
    }

    private void SpawnBurst()
    {
        if (!burstEffectPrefab) return;
        var pos = transform.position + Vector3.up * 0.2f;
        var rot = Camera.main ? Quaternion.LookRotation(Camera.main.transform.forward) : Quaternion.identity;
        var fx = Instantiate(burstEffectPrefab, pos, rot);
        fx.transform.localScale = Vector3.one * Mathf.Max(0.1f, burstScale);
        if (fx.TryGetComponent<ParticleSystem>(out var ps)) { ps.Play(); Destroy(fx, burstAutoDestroy); }
        else Destroy(fx, burstAutoDestroy);
    }

    void HideShellOnly()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Canvas>(true))   c.enabled = false;
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        foreach (var col in GetComponentsInChildren<Collider>(true)) col.enabled = false;
    }
    void ShowShell()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        foreach (var c in GetComponentsInChildren<Canvas>(true))   c.enabled = true;
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true)) ps.Play();
        foreach (var col in GetComponentsInChildren<Collider>(true)) col.enabled = true;
    }

    IEnumerator FadeOutLocalFallback()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, fallbackFadeOutAfter));

        float t = 0f, dur = Mathf.Max(0.05f, fallbackFadeDuration);
        float startVol = _src ? _src.volume : 0f;
        while (t < dur && _src && _src.isPlaying)
        {
            t += Time.deltaTime;
            _src.volume = Mathf.Lerp(startVol, 0f, t / dur);
            yield return null;
        }
        if (_src) _src.Stop();
        if (_src) _src.spatialBlend = 1f;
    }

    // Ambient autopilot
    public static void Ambient_PauseAll()
    {
        sAmbientPaused = true;
        if (sCurrentAmbient) sCurrentAmbient = null;
        for (int i = 0; i < sAll.Count; i++)
        {
            var b = sAll[i];
            if (b && b.ambientLoopAudio)
            {
                b.ambientLoopAudio.DOKillIfTweening();
                b.ambientLoopAudio.volume = 0f;
                if (b.ambientLoopAudio.isPlaying) b.ambientLoopAudio.Stop();
            }
        }
    }
    public static void Ambient_Resume() { sAmbientPaused = false; }

    void AmbientCoordinatorTick()
    {
        if (sAmbientPaused || sAll.Count == 0) return;
        var cam = Camera.main; if (!cam) return;

        BubbleTrigger best = null; float bestDist = float.MaxValue;
        for (int i = 0; i < sAll.Count; i++)
        {
            var b = sAll[i];
            if (!b || !b.ambientLoopAudio || !b.ambientLoopAudio.enabled) continue;
            if (b._triggered) continue;

            float d = Vector3.Distance(cam.transform.position, b.transform.position);
            if (d < bestDist) { bestDist = d; best = b; }
        }
        if (!best) return;

        if (sCurrentAmbient && sCurrentAmbient != best)
        {
            float currDist = Vector3.Distance(cam.transform.position, sCurrentAmbient.transform.position);
            if (bestDist > currDist - sHysteresis) best = sCurrentAmbient;
        }

        if (sCurrentAmbient == best) { FadeTo(best, best.ambientBaseVolume, sXFadeTime); return; }

        if (sCurrentAmbient) FadeTo(sCurrentAmbient, 0f, sXFadeTime);
        sCurrentAmbient = best;
        FadeTo(best, best.ambientBaseVolume, sXFadeTime);
    }

    void FadeTo(BubbleTrigger b, float target, float time)
    {
        if (!b || !b.ambientLoopAudio) return;
        var a = b.ambientLoopAudio;
        if (target > 0f && !a.isPlaying) a.Play();
        b.StopAllCoroutines();
        b.StartCoroutine(FadeVolumeCo(a, target, Mathf.Max(0.01f, time)));
    }
    IEnumerator FadeVolumeCo(AudioSource a, float target, float dur)
    {
        float start = a.volume; float t = 0f;
        while (t < dur && a)
        {
            t += Time.deltaTime;
            a.volume = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        if (!a) yield break;
        a.volume = target;
        if (Mathf.Approximately(target, 0f) && a.isPlaying) a.Stop();
    }
}

static class AudioSourceTweenNoop
{
    public static void DOKillIfTweening(this AudioSource _) { /* noop */ }
}
