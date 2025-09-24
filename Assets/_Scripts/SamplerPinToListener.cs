using UnityEngine;

public class SamplerPinToListener : MonoBehaviour
{
    public AudioSampler sampler;

    void Awake()
    {
        if (!sampler) sampler = FindFirstObjectByType<AudioSampler>();
        if (!sampler) { Debug.LogWarning("[SamplerPinToListener] No AudioSampler found."); return; }

        sampler.sourceMode = AudioSampler.SourceMode.MixAudioListener;
        Debug.Log("[SamplerPinToListener] AudioSampler pinned to MixAudioListener.");
    }
}
