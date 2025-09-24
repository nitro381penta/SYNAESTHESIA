using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SettingsPanelUIManager : MonoBehaviour
{
    [Header("Visualization UI Buttons")]
    public Button noneButton;
    public Button sparklesButton;
    public Button fireworksButton;
    public Button wavesButton;
    public Button butterflyButton;
    public Button psychedelicButton;

    [Header("XR Utility Buttons")]
    public XRSimpleInteractable muteButton;
    public XRSimpleInteractable settingsBackButton; // back to previous canvas
    public XRSimpleInteractable homeButton;

    void Start()
    {
        // Visualization choices
        noneButton.onClick.AddListener(() => SetMode("None"));
        sparklesButton.onClick.AddListener(() => SetMode("Sparkles"));
        fireworksButton.onClick.AddListener(() => SetMode("Fireworks"));
        wavesButton.onClick.AddListener(() => SetMode("Waves"));
        butterflyButton.onClick.AddListener(() => SetMode("Butterfly"));
        psychedelicButton.onClick.AddListener(() => SetMode("Psychedelic"));

        // XR utilities
        muteButton?.selectEntered.AddListener(_ => ToggleCentralMute());
        settingsBackButton?.selectEntered.AddListener(_ => UIManagerXR.Instance?.ReturnFromSettings());
        homeButton?.selectEntered.AddListener(_ => UIManagerXR.Instance?.GoHome());
    }

    void SetMode(string name)
    {
        VisualizerManager.Instance?.SetModeByName(name);
    }

    void ToggleCentralMute()
    {
        var p = UIManagerXR.Instance ? UIManagerXR.Instance.soundPlayer : null;
        if (p == null || p.audioSource == null) return;
        p.audioSource.mute = !p.audioSource.mute;
    }
}
