using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SoundBubbleUIController : MonoBehaviour
{
    [Header("Central Player")]
    public BubbleAudioPlayer bubbleAudioPlayer;

    [Header("XR Buttons")]
    public XRSimpleInteractable playButton;
    public XRSimpleInteractable pauseButton;
    public XRSimpleInteractable forwardButton;
    public XRSimpleInteractable rewindButton;
    public XRSimpleInteractable previousButton;
    public XRSimpleInteractable nextButton;
    public XRSimpleInteractable loopButton;
    public XRSimpleInteractable muteButton;
    public XRSimpleInteractable settingsButton;  
    public XRSimpleInteractable homeButton;

    void Start()
    {
        if (!bubbleAudioPlayer) Debug.LogError("SoundBubbleUIController: bubbleAudioPlayer not assigned.");

        playButton?.selectEntered.AddListener(_ => bubbleAudioPlayer?.Resume());
        pauseButton?.selectEntered.AddListener(_ => bubbleAudioPlayer?.Pause());
        forwardButton?.selectEntered.AddListener(_ => bubbleAudioPlayer?.FastForward());
        rewindButton?.selectEntered.AddListener(_ => bubbleAudioPlayer?.Rewind());
        previousButton?.selectEntered.AddListener(_ => bubbleAudioPlayer?.PreviousTrack());
        nextButton?.selectEntered.AddListener(_ => bubbleAudioPlayer?.NextTrack());
        loopButton?.selectEntered.AddListener(_ => bubbleAudioPlayer?.SetLoop(!bubbleAudioPlayer.audioSource.loop));
        muteButton?.selectEntered.AddListener(_ => ToggleMuteAndMaybeResume());
        settingsButton?.selectEntered.AddListener(_ => UIManagerXR.Instance?.ShowSettingsForSound()); 
        homeButton?.selectEntered.AddListener(_ => UIManagerXR.Instance?.GoHome());
    }

    private void ToggleMuteAndMaybeResume()
    {
        if (!bubbleAudioPlayer || !bubbleAudioPlayer.audioSource) return;
        var src = bubbleAudioPlayer.audioSource;
        bool newMuted = !src.mute;
        src.mute = newMuted;

        if (!newMuted && !src.isPlaying)
        {
            if (src.clip != null) src.Play();
            else bubbleAudioPlayer.Play();
        }
    }
}
