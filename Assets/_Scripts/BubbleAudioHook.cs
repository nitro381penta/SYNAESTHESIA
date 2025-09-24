using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BubbleAudioHook : MonoBehaviour
{
    public AudioSampler sampler; 
    private AudioSource _src;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        if (!sampler) sampler = FindFirstObjectByType<AudioSampler>();
        if (!sampler) Debug.LogWarning("BubbleAudioHook: No AudioSampler found in scene.");
    }

    void OnEnable()  { if (sampler) sampler.RegisterSource(_src); }
    void OnDisable() { if (sampler) sampler.UnregisterSource(_src); }

    public void OnBubbleTappedPlay()
    {
        if (!sampler) return;
        sampler.SetManualSource(_src); 
        
    }

    // play + route in one call
    public void PlayClipAndRoute(AudioClip clip, bool loop = true, float volume = 1f)
    {
        _src.clip = clip; _src.loop = loop; _src.volume = volume;
        _src.Play();
        OnBubbleTappedPlay();
    }
}
