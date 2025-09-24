using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BubbleManager : MonoBehaviour
{
    public static BubbleManager Instance { get; private set; }

    private readonly List<BubbleTrigger> _bubbles = new List<BubbleTrigger>();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        _bubbles.Clear();
    }

    // Registration (called from BubbleTrigger.Awake)
    public void RegisterBubble(GameObject go)
    {
        if (!go) return;
        var bt = go.GetComponent<BubbleTrigger>();
        if (bt && !_bubbles.Contains(bt)) _bubbles.Add(bt);
    }

    public void UnregisterBubble(GameObject go)
    {
        if (!go) return;
        var bt = go.GetComponent<BubbleTrigger>();
        if (bt) _bubbles.Remove(bt);
    }

    // Core visibility control
    public void HideAllExcept(GameObject keep)
    {
        foreach (var b in _bubbles)
        {
            if (!b) continue;
            if (keep && b.gameObject == keep) continue;
            HideShellOnly(b.gameObject);
        }
        BubbleTrigger.Ambient_PauseAll(); // central ambient autopilot
    }

    public void ShowAll()
    {
        foreach (var b in _bubbles)
        {
            if (!b) continue;
            ShowShell(b.gameObject);
        }
        BubbleTrigger.Ambient_Resume(); // central ambient autopilot
    }

    // Compatibility aliases
    public void ShowAllBubbles() => ShowAll();

    public void HideAllBubblesExcept(GameObject keep) => HideAllExcept(keep);

    public void HideAllBubbles()
    {
        foreach (var b in _bubbles)
        {
            if (!b) continue;
            HideShellOnly(b.gameObject);
        }
        BubbleTrigger.Ambient_PauseAll();
    }

    // Helpers (mirror BubbleTrigger’s implementation)
    static void HideShellOnly(GameObject root)
    {
        if (!root) return;

        var rs = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++) rs[i].enabled = false;

        var cs = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < cs.Length; i++) cs[i].enabled = false;

        var ps = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < ps.Length; i++)
            ps[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);

        var cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
    }

    static void ShowShell(GameObject root)
    {
        if (!root) return;

        var rs = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++) rs[i].enabled = true;

        var cs = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < cs.Length; i++) cs[i].enabled = true;

        var ps = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < ps.Length; i++) ps[i].Play();

        var cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = true;
    }

    public void PauseAllAmbients()  => BubbleTrigger.Ambient_PauseAll();
    public void ResumeAmbients()    => BubbleTrigger.Ambient_Resume();
}
