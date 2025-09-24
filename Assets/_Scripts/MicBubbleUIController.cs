using UnityEngine;

[DisallowMultipleComponent]
public class MicBubbleUIController : MonoBehaviour
{
    [Header("Refs")]
    public MicrophoneRecorder microphoneRecorder;

    [Header("Behaviour")]
    public bool startMicOnBubbleTap = true;
    public bool stopMicOnHome = true;

    const string TAG = "[MIC-BUBBLE]";

    void Awake()
    {
        if (!microphoneRecorder)
            microphoneRecorder = FindFirstObjectByType<MicrophoneRecorder>();
        Debug.Log($"{TAG} Awake() mic={(microphoneRecorder ? "OK" : "null")}");
    }

    public void OnMicBubbleTapped()
    {
        Debug.Log($"{TAG} OnMicBubbleTapped()");
        UIManagerXR.Instance?.ShowMicInputUI();

        if (startMicOnBubbleTap && microphoneRecorder && !microphoneRecorder.IsRecording)
        {
            Debug.Log($"{TAG} StartRecording()");
            microphoneRecorder.StartRecording();
        }
    }

    public void OnOpenMicSettings()
    {
        Debug.Log($"{TAG} OnOpenMicSettings()");
        UIManagerXR.Instance?.ShowMicSettings();
    }

    public void OnBackFromSettings()
    {
        Debug.Log($"{TAG} OnBackFromSettings()");
        UIManagerXR.Instance?.BackFromMicSettings();
    }

    public void OnHome()
    {
        Debug.Log($"{TAG} OnHome()");
        if (stopMicOnHome && microphoneRecorder && microphoneRecorder.IsRecording)
        {
            Debug.Log($"{TAG} StopRecording() due to Home");
            microphoneRecorder.StopRecording();
        }
        UIManagerXR.Instance?.GoHome();
    }
}
