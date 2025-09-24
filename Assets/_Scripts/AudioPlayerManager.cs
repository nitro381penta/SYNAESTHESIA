using UnityEngine;
using System.Collections.Generic;

public class AudioPlayerManager : MonoBehaviour
{
    [Header("Audio Playlist")]
    public List<AudioClip> playlist;
    public AudioSource audioSource;

    [Header("Visualizer")]
    public VisualizerManager.VisualizerMode currentVisualizer = VisualizerManager.VisualizerMode.None;

    private int currentTrackIndex = 0;

    void Start()
    {
        if (playlist.Count > 0 && audioSource != null)
        {
            PlayTrack(currentTrackIndex);
        }
    }

    public void PlayTrack(int index)
    {
        if (index < 0 || index >= playlist.Count || audioSource == null) return;

        currentTrackIndex = index;
        audioSource.clip = playlist[currentTrackIndex];
        audioSource.spatialBlend = 1f; // 3D spatial audio
        audioSource.Play();
    }

    public void Play()
    {
        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    public void Pause()
    {
        if (audioSource.isPlaying)
            audioSource.Pause();
    }

    public void Stop()
    {
        audioSource.Stop();
    }

    public void NextTrack()
    {
        currentTrackIndex = (currentTrackIndex + 1) % playlist.Count;
        PlayTrack(currentTrackIndex);
    }

    public void SetVisualizerMode(VisualizerManager.VisualizerMode mode)
    {
        currentVisualizer = mode;
        VisualizerManager.Instance.SetMode(mode);
    }
}
