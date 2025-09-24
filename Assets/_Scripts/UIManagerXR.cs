using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIManagerXR : MonoBehaviour
{
    public static UIManagerXR Instance { get; private set; }

    [Header("Canvases (root GameObjects)")]
    public GameObject soundPlayerCanvas;
    public GameObject microphoneInputCanvas;
    public GameObject settingsPanel_Sound;
    public GameObject settingsPanel_Mic;
    public GameObject instructionCanvas;

    [Header("Central Player (required)")]
    public BubbleAudioPlayer soundPlayer;

    [Header("Visualizer wiring (optional)")]
    public AudioSampler sampler;
    public bool wireSamplerToCentral = false;

    [Header("Startup (optional)")]
    public bool playMusicOnStart = false;
    public List<AudioClip> startupPlaylist;
    public bool showSoundUIOnStart = true;

    [Header("Debug / Helpers")]
    public bool verboseLogs = true;
    public bool forceWorldSpace = true;
    public int  sortingOrderOnShow = 600;
    public float popupDistance = 2.0f;

    GameObject _returnPanel;
    public bool IsCentralPlaying { get; private set; }

    const string TAG = "[UIXR]";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (verboseLogs) Debug.Log($"{TAG} Awake()");
    }

    void Start()
    {
        AssignSamplerToCentral();

        if (!playMusicOnStart) { ResetUI(); return; }

        if (startupPlaylist != null && startupPlaylist.Count > 0)
        {
            if (showSoundUIOnStart) ShowSoundPlayerUI(); else ResetUI();
            PlayPlaylist(startupPlaylist);
            return;
        }

        if (soundPlayer && soundPlayer.audioSource && soundPlayer.audioSource.clip)
        {
            if (showSoundUIOnStart) ShowSoundPlayerUI(); else ResetUI();
            var single = new List<AudioClip> { soundPlayer.audioSource.clip };
            SetPlaylistAndPlayNow(single);
            return;
        }

        ResetUI();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log($"{TAG} DEBUG: M pressed → ShowMicInputUI()");
            ShowMicInputUI();
        }
    }
#endif

    public void HideAllCanvases()
    {
        if (verboseLogs) Debug.Log($"{TAG} HideAllCanvases()");
        SafeSetActive(soundPlayerCanvas, false);
        SafeSetActive(microphoneInputCanvas, false);
        SafeSetActive(settingsPanel_Sound, false);
        SafeSetActive(settingsPanel_Mic, false);
        SafeSetActive(instructionCanvas, false);
    }

    public void ResetUI()
    {
        if (verboseLogs) Debug.Log($"{TAG} ResetUI()");
        HideAllCanvases();
        SafeSetActive(instructionCanvas, true);
        _returnPanel = null;
    }

    public void ShowSoundPlayerUI()
    {
        if (verboseLogs) Debug.Log($"{TAG} ShowSoundPlayerUI()");
        HideAllCanvases();
        StartCoroutine(ForceShowAfterFrame(soundPlayerCanvas, "ShowSoundPlayerUI"));
        _returnPanel = soundPlayerCanvas;
        AssignSamplerToCentral();
    }

    public void ShowMicInputUI()
    {
        if (verboseLogs) Debug.Log($"{TAG} ShowMicInputUI()");
        HideAllCanvases();
        SafeSetActive(microphoneInputCanvas, true); // mic canvas follows camera by its own script
        _returnPanel = microphoneInputCanvas;
        EnsureCanvasSetup(microphoneInputCanvas);
    }

    public void ShowSettingsForSound()
    {
        if (!settingsPanel_Sound) { Debug.LogError($"{TAG} settingsPanel_Sound not assigned."); return; }
        if (verboseLogs) Debug.Log($"{TAG} ShowSettingsForSound()");
        _returnPanel = soundPlayerCanvas;

        SafeSetActive(soundPlayerCanvas, false);
        SafeSetActive(microphoneInputCanvas, false);
        SafeSetActive(settingsPanel_Mic, false);

        StartCoroutine(ForceShowAfterFrame(settingsPanel_Sound, "ShowSettingsForSound"));
    }

    public void ShowSettingsForMic()
    {
        if (!settingsPanel_Mic) { Debug.LogError($"{TAG} settingsPanel_Mic not assigned."); return; }
        if (verboseLogs) Debug.Log($"{TAG} ShowSettingsForMic()");
        _returnPanel = microphoneInputCanvas;

        SafeSetActive(soundPlayerCanvas, false);
        SafeSetActive(microphoneInputCanvas, false);

        StartCoroutine(ForceShowAfterFrame(settingsPanel_Mic, "ShowSettingsForMic"));
    }

    public void ToggleMicSettings()
    {
        if (!settingsPanel_Mic) return;
        if (settingsPanel_Mic.activeSelf) BackFromMicSettings();
        else ShowMicSettings();
    }

    public void ReturnFromSettings()
    {
        if (verboseLogs) Debug.Log($"{TAG} ReturnFromSettings()");
        SafeSetActive(settingsPanel_Sound, false);
        SafeSetActive(settingsPanel_Mic, false);

        if (_returnPanel)
        {
            StartCoroutine(ForceShowAfterFrame(_returnPanel, "ReturnFromSettings"));
            if (_returnPanel == soundPlayerCanvas) AssignSamplerToCentral();
        }
        else ResetUI();
    }

    public void ShowMicSettings() => ShowSettingsForMic();
    public void BackFromMicSettings() => ReturnFromSettings();
    public void ShowSoundSettings() => ShowSettingsForSound();

    public void PlayPlaylist(List<AudioClip> clips)
    {
        if (!soundPlayer) { Debug.LogError($"{TAG} assign 'soundPlayer' in the inspector."); return; }
        if (clips == null || clips.Count == 0) { Debug.LogWarning($"{TAG} playlist is empty."); return; }

        ShowSoundPlayerUI();
        SetPlaylistAndPlayNow(clips);
    }

    public void GoHome()
    {
        if (verboseLogs) Debug.Log($"{TAG} GoHome()");
        if (soundPlayer != null)
        {
            soundPlayer.Stop();
            soundPlayer.ClearPlaylist();
            if (soundPlayer.audioSource) soundPlayer.audioSource.mute = false;
        }
        IsCentralPlaying = false;

        foreach (var b in FindAll<BubbleTrigger>())
            b.ResetBubble();

        BubbleManager.Instance?.ShowAllBubbles();
        VisualizerManager.Instance?.StopAllVisualizers();
        DarkDomeController.TryExitGlobal();

        ResetUI();
    }

    void AssignSamplerToCentral()
    {
        if (!wireSamplerToCentral) return;
        if (sampler && soundPlayer && soundPlayer.audioSource)
        {
            if (verboseLogs) Debug.Log($"{TAG} Sampler → CentralPlayer");
            sampler.SetManualSource(soundPlayer.audioSource);
        }
    }

    void SetPlaylistAndPlayNow(List<AudioClip> clips)
    {
        var shuffled = new List<AudioClip>(clips);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        if (soundPlayer.audioSource)
        {
            soundPlayer.audioSource.spatialBlend = 0f;
            soundPlayer.audioSource.mute = false;
            soundPlayer.audioSource.volume = 1f;
        }

        soundPlayer.SetPlaylist(shuffled);
        soundPlayer.Play();
        IsCentralPlaying = true;

        VisualizerManager.Instance?.SetMode(VisualizerManager.VisualizerMode.Sparkles);
        AssignSamplerToCentral();
    }

    IEnumerator ForceShowAfterFrame(GameObject root, string reason)
    {
        yield return null;
        ForceCanvasVisible(root, reason);
    }

    void ForceCanvasVisible(GameObject root, string reason)
    {
        if (!root) { Debug.LogWarning($"{TAG} ForceCanvasVisible(null) reason={reason}"); return; }
        if (verboseLogs) Debug.Log($"{TAG} ForceCanvasVisible('{root.name}') reason={reason}");

        root.SetActive(true);
        EnsureCanvasSetup(root);

        var cam = Camera.main;
        if (cam && root != microphoneInputCanvas)
        {
            Vector3 pos = cam.transform.position + cam.transform.forward * popupDistance + Vector3.up * 0.05f;
            root.transform.position = pos;
            root.transform.rotation = Quaternion.LookRotation(root.transform.position - cam.transform.position);
        }

        if (cam && ((cam.cullingMask & (1 << root.layer)) == 0))
            Debug.LogWarning($"{TAG} '{root.name}' layer '{LayerMask.LayerToName(root.layer)}' is NOT in Camera.main culling mask.");
    }

    void EnsureCanvasSetup(GameObject root)
    {
        var canvas = root ? root.GetComponentInChildren<Canvas>(true) : null;
        if (canvas && forceWorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrderOnShow;
        }
        var cg = root ? root.GetComponentInChildren<CanvasGroup>(true) : null;
        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
    }

    static void SafeSetActive(GameObject go, bool on)
    {
        if (!go) return;
        if (go.activeSelf != on) go.SetActive(on);
    }

    static T[] FindAll<T>() where T : Object
    {
#if UNITY_2022_2_OR_NEWER
        return FindObjectsByType<T>(FindObjectsSortMode.None);
#else
        return FindObjectsOfType<T>(true);
#endif
    }
}
