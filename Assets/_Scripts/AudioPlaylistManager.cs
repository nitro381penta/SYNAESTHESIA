using UnityEngine;

public class AudioPlaylistManager : MonoBehaviour
{
    public static AudioPlaylistManager Instance;

    [SerializeField] private BubbleAudioPlayer bubbleAudioPlayer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLoop(bool shouldLoop)
    {
        if (bubbleAudioPlayer != null)
        {
            bubbleAudioPlayer.SetLoop(shouldLoop);
            Debug.Log("SetLoop called with: " + shouldLoop);
        }
        else
        {
            Debug.LogWarning("BubbleAudioPlayer is not assigned in AudioPlaylistManager.");
        }
    }

    public void Next()
    {
        if (bubbleAudioPlayer != null)
        {
            bubbleAudioPlayer.NextTrack();
            Debug.Log("Next track triggered.");
        }
        else
        {
            Debug.LogWarning("BubbleAudioPlayer is not assigned in AudioPlaylistManager.");
        }
    }

    public void Previous()
    {
        if (bubbleAudioPlayer != null)
        {
            bubbleAudioPlayer.PreviousTrack();
            Debug.Log("Previous track triggered.");
        }
        else
        {
            Debug.LogWarning("BubbleAudioPlayer is not assigned in AudioPlaylistManager.");
        }
    }

    public void Reset()
    {
        if (bubbleAudioPlayer != null)
        {
            bubbleAudioPlayer.Stop();
            bubbleAudioPlayer.ClearPlaylist();
            Debug.Log("AudioPlaylistManager: Reset performed.");
        }
        else
        {
            Debug.LogWarning("Cannot reset: bubbleAudioPlayer is null.");
        }
    }
}
