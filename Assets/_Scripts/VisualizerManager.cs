using UnityEngine;

public class VisualizerManager : MonoBehaviour
{
    public static VisualizerManager Instance { get; private set; }

    public enum VisualizerMode { None, Sparkles, Fireworks, Waves, Butterfly, Psychedelic }

    [Header("Visualizer Roots (assign in Inspector)")]
    public GameObject sparklesRoot;
    public GameObject fireworksRoot;
    public GameObject wavesRoot;
    public GameObject butterflyRoot;   // renamed from mandala
    public GameObject psychedelicRoot;

    VisualizerMode _mode;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SetMode(VisualizerMode.None);
    }

    public void SetMode(VisualizerMode newMode)
    {
        _mode = newMode;
        EnableOnly(newMode);

        var mgr = FindFirstObjectByType<AudioReactiveManager_Listener>();
        if (mgr) mgr.RefreshReactives();
    }

    public void SetModeByName(string name)
    {
        if (string.IsNullOrEmpty(name)) { SetMode(VisualizerMode.None); return; }
        string n = name.Trim().ToLowerInvariant();
        switch (n)
        {
            case "none":        SetMode(VisualizerMode.None); break;
            case "sparkles":    SetMode(VisualizerMode.Sparkles); break;
            case "fireworks":   SetMode(VisualizerMode.Fireworks); break;
            case "waves":       SetMode(VisualizerMode.Waves); break;
            case "butterfly":   SetMode(VisualizerMode.Butterfly); break;
            case "psychedelic": SetMode(VisualizerMode.Psychedelic); break;
            case "mandala":     SetMode(VisualizerMode.Butterfly); break; 
            default:
                Debug.LogWarning($"[VisualizerManager] Unknown mode '{name}', switching to None.");
                SetMode(VisualizerMode.None);
                break;
        }
    }

    public void StopAllVisualizers() => EnableOnly(VisualizerMode.None);

    void EnableOnly(VisualizerMode m)
    {
        Safe(sparklesRoot,    m == VisualizerMode.Sparkles);
        Safe(fireworksRoot,   m == VisualizerMode.Fireworks);
        Safe(wavesRoot,       m == VisualizerMode.Waves);
        Safe(butterflyRoot,   m == VisualizerMode.Butterfly);
        Safe(psychedelicRoot, m == VisualizerMode.Psychedelic);
    }

    static void Safe(GameObject go, bool on)
    {
        if (!go) return;
        if (go.activeSelf != on) go.SetActive(on);
    }
}
