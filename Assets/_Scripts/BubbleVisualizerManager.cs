using UnityEngine;

public class BubbleVisualizerManager : MonoBehaviour
{
    public enum VisualizerMode { None, Sparkles, Fireworks, Waves, Butterfly, Psychedelic }
    public VisualizerMode mode = VisualizerMode.None;

    public void SetMode(VisualizerMode newMode)
    {
        mode = newMode;
    }

    public void ActivateVisualizer(Vector3 position)
    {
        Debug.Log($"Visualizer for bubble at {position} activated with mode {mode}");
    }
}
