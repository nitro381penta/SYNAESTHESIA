using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
public class MicrophoneInputUIManager : MonoBehaviour
{
    [Header("Refs")]
    public MicrophoneRecorder microphoneRecorder;

    [Header("UI")]
    public GameObject microphoneInputCanvas; // reference only
    public TMP_Text recordingText;

    [Header("XR Buttons")]
    public XRSimpleInteractable micButton;
    public XRSimpleInteractable muteButton;
    public XRSimpleInteractable homeButton;
    public XRSimpleInteractable settingsButton;

    bool isMuted = false;
    const string TAG = "[MIC-UI]";

    void Awake()
    {
        if (!microphoneRecorder)
            microphoneRecorder = FindFirstObjectByType<MicrophoneRecorder>();
        if (recordingText) recordingText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        micButton?.selectEntered.AddListener(OnMicButton);
        muteButton?.selectEntered.AddListener(OnMuteButton);
        homeButton?.selectEntered.AddListener(OnHomeButton);
        settingsButton?.selectEntered.AddListener(OnSettingsButton);
    }
    void OnDisable()
    {
        micButton?.selectEntered.RemoveListener(OnMicButton);
        muteButton?.selectEntered.RemoveListener(OnMuteButton);
        homeButton?.selectEntered.RemoveListener(OnHomeButton);
        settingsButton?.selectEntered.RemoveListener(OnSettingsButton);
    }

    void Update()
    {
        bool rec = microphoneRecorder && microphoneRecorder.IsRecording;
        if (recordingText && recordingText.gameObject.activeSelf != rec)
            recordingText.gameObject.SetActive(rec);
    }

    void OnMicButton(SelectEnterEventArgs _)
    {
        if (!microphoneRecorder) return;

        if (microphoneRecorder.IsRecording) microphoneRecorder.StopRecording();
        else microphoneRecorder.StartRecording();

        // Ensure some visualizer is active for mic
        VisualizerManager.Instance?.SetMode(VisualizerManager.VisualizerMode.Fireworks);
    }

    void OnMuteButton(SelectEnterEventArgs _)
    {
        isMuted = !isMuted;
        foreach (var s in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            s.mute = isMuted;
    }

    void OnHomeButton(SelectEnterEventArgs _)
    {
        if (microphoneRecorder && microphoneRecorder.IsRecording) microphoneRecorder.StopRecording();
        UIManagerXR.Instance?.GoHome();
    }

    void OnSettingsButton(SelectEnterEventArgs _)
    {
        UIManagerXR.Instance?.ToggleMicSettings();
    }
}
