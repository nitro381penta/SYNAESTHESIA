using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SoundPlayerUIManager : MonoBehaviour
{
    [Header("References")]
    public GameObject instructionCanvas;
    public GameObject settingsPanel;
    public GameObject soundPlayerCanvas;
    public AudioSource ambientAudio;

    [Header("XR Buttons")]
    public XRSimpleInteractable homeButton;
    public XRSimpleInteractable settingsButton;
    public XRSimpleInteractable playButton;
    public XRSimpleInteractable pauseButton;
    public XRSimpleInteractable forwardButton;
    public XRSimpleInteractable rewindButton;
    public XRSimpleInteractable previousButton;
    public XRSimpleInteractable nextButton;
    public XRSimpleInteractable loopButton;
    public XRSimpleInteractable muteButton;

    private AudioSource currentBubbleAudio;
    private int loopState = 0; // 0 = no loop, 1 = playlist loop, 2 = current track loop

    void Start()
    {
        homeButton.selectEntered.AddListener(_ => OnHome());
        settingsButton.selectEntered.AddListener(_ => OpenSettings());

        playButton.selectEntered.AddListener(_ => currentBubbleAudio?.Play());
        pauseButton.selectEntered.AddListener(_ => currentBubbleAudio?.Pause());
        forwardButton.selectEntered.AddListener(_ => { if (currentBubbleAudio) currentBubbleAudio.time += 5f; });
        rewindButton.selectEntered.AddListener(_ => { if (currentBubbleAudio) currentBubbleAudio.time = Mathf.Max(0f, currentBubbleAudio.time - 5f); });

        previousButton.selectEntered.AddListener(_ => AudioPlaylistManager.Instance?.Previous());
        nextButton.selectEntered.AddListener(_ => AudioPlaylistManager.Instance?.Next());

        loopButton.selectEntered.AddListener(_ => CycleLoopMode());
        muteButton.selectEntered.AddListener(_ => { if (currentBubbleAudio) currentBubbleAudio.mute = !currentBubbleAudio.mute; });
    }

    public void SetCurrentAudioSource(AudioSource source)
    {
        currentBubbleAudio = source;
    }

    private void CycleLoopMode()
    {
        loopState = (loopState + 1) % 3;

        switch (loopState)
        {
            case 0:
                currentBubbleAudio.loop = false;
                AudioPlaylistManager.Instance?.SetLoop(false);
                break;
            case 1:
                currentBubbleAudio.loop = false;
                AudioPlaylistManager.Instance?.SetLoop(true);
                break;
            case 2:
                currentBubbleAudio.loop = true;
                break;
        }
    }

    private void OnHome()
    {
        instructionCanvas?.SetActive(true);
        soundPlayerCanvas?.SetActive(false);
        settingsPanel?.SetActive(false);

        BubbleManager.Instance?.ShowAllBubbles();
        ambientAudio?.Play();
    }

    private void OpenSettings()
    {
        settingsPanel?.SetActive(true);
        soundPlayerCanvas?.SetActive(false);
    }
}
