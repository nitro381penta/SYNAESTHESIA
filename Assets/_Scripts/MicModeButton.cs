using UnityEngine;

public class MicModeButton : MonoBehaviour
{
    public MicrophoneRecorder recorder;

    public void ToggleMic()
    {
        if (!recorder) recorder = FindFirstObjectByType<MicrophoneRecorder>();
        if (!recorder) return;

        if (recorder.IsRecording) recorder.StopRecording();
        else recorder.StartRecording();
    }
}
