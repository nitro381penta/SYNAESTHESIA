using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BubbleAudioPlayer : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Playback")]
    public bool loopTrack = false;
    public bool autoAdvance = true;     // move to next track

    private List<AudioClip> playlist = new();
    private int currentTrackIndex = 0;

    // runtime flags
    private bool _userPaused = false;
    private bool _userStopped = false;
    private bool _wasPlaying = false;

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loopTrack;
        audioSource.spatialBlend = 0f; // 2D
    }

    void Update()
    {
        bool nowPlaying = audioSource && audioSource.isPlaying;
        if (!_userStopped && !_userPaused && _wasPlaying && !nowPlaying)
        {
            if (autoAdvance && playlist.Count > 0 && !loopTrack)
                NextTrack();
        }
        _wasPlaying = nowPlaying;
        _userStopped = false;
    }

    public void SetPlaylist(List<AudioClip> clips)
    {
        playlist = clips != null ? new List<AudioClip>(clips) : new List<AudioClip>();
        currentTrackIndex = 0;
        if (playlist.Count == 0) audioSource.clip = null;
    }

    public void SetPlaylistAndPlayShuffled(List<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0) return;

        playlist = new List<AudioClip>(clips);
        currentTrackIndex = Random.Range(0, playlist.Count);

        var clip = playlist[currentTrackIndex];
        if (!clip) return;

        ForceAudible();
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.time = 0f;
        _userPaused = false;
        _userStopped = false;
        audioSource.Play();
    }

    public void Play()
    {
        if (playlist == null || playlist.Count == 0) return;
        var clip = playlist[currentTrackIndex];
        if (!clip) return;

        ForceAudible();
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.time = 0f;
        _userPaused = false; _userStopped = false;
        audioSource.Play();
    }

    public void Resume(){ if (audioSource.clip && !audioSource.isPlaying) { _userPaused = false; _userStopped = false; audioSource.Play(); } }
    public void Pause(){ if (audioSource.isPlaying) { _userPaused = true; audioSource.Pause(); } }
    public void Stop(){ if (audioSource.isPlaying) { _userStopped = true; audioSource.Stop(); } }
    public void NextTrack(){ if (playlist == null || playlist.Count == 0) return; currentTrackIndex = (currentTrackIndex + 1) % playlist.Count; Play(); }
    public void PreviousTrack(){ if (playlist == null || playlist.Count == 0) return; currentTrackIndex = (currentTrackIndex - 1 + playlist.Count) % playlist.Count; Play(); }
    public void Rewind(){ if (audioSource.clip) audioSource.time = 0f; }
    public void FastForward(){ if (audioSource.clip) audioSource.time = Mathf.Min(audioSource.clip.length, audioSource.time + 5f); }
    public void SetLoop(bool loopCurrentTrack){ loopTrack = loopCurrentTrack; audioSource.loop = loopCurrentTrack; }
    public void SetMute(bool muted){ audioSource.mute = muted; }
    public void ClearPlaylist(){ playlist.Clear(); currentTrackIndex = 0; audioSource.clip = null; _userPaused = _userStopped = _wasPlaying = false; }

    private void ForceAudible()
    {
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        audioSource.spatialBlend = 0f;
        audioSource.mute = false;
        if (audioSource.volume <= 0f) audioSource.volume = 1f;
    }
}
