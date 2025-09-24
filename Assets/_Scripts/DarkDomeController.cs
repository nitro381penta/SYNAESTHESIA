using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DarkDomeController : MonoBehaviour
{
    public static DarkDomeController Instance { get; private set; }

    [Header("Renderer of the dome sphere (auto-found if empty)")]
    public MeshRenderer domeRenderer;
    [Tooltip("Try to auto-find a MeshRenderer in children if none is assigned.")]
    public bool autoFindRenderer = true;

    [Range(0f, 1f)] public float targetAlpha = 0.85f; // darkness in music mode
    public float fadeIn = 0.35f;
    public float fadeOut = 0.35f;

    Material _mat;
    Coroutine _co;

    // URP Unlit
    static readonly int PROP_BASE  = Shader.PropertyToID("_BaseColor");
    static readonly int PROP_COLOR = Shader.PropertyToID("_Color");

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!domeRenderer && autoFindRenderer)
            domeRenderer = GetComponentInChildren<MeshRenderer>(true);

        if (!domeRenderer)
        {
            Debug.LogWarning("[DarkDome] No MeshRenderer found/assigned.");
            return;
        }

        _mat = domeRenderer.material;
        SetAlpha(0f);
        domeRenderer.enabled = false;
    }

    public void Enter()
    {
        if (_mat == null || domeRenderer == null) return;
        domeRenderer.enabled = true;
        StartFade(targetAlpha, fadeIn);
    }

    public void Exit()
    {
        if (_mat == null || domeRenderer == null) return;
        StartFade(0f, fadeOut);
    }

    void StartFade(float to, float dur)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(FadeCR(to, Mathf.Max(0.01f, dur)));
    }

    IEnumerator FadeCR(float to, float dur)
    {
        float from = GetAlpha();
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            SetAlpha(Mathf.Lerp(from, to, k));
            yield return null;
        }
        SetAlpha(to);
        if (to <= 0f && domeRenderer) domeRenderer.enabled = false;
        _co = null;
    }

    void SetAlpha(float a)
    {
        a = Mathf.Clamp01(a);
        if (_mat == null) return;

        if (_mat.HasProperty(PROP_BASE))
        {
            var c = _mat.GetColor(PROP_BASE); c.a = a; _mat.SetColor(PROP_BASE, c);
        }
        if (_mat.HasProperty(PROP_COLOR))
        {
            var c2 = _mat.GetColor(PROP_COLOR); c2.a = a; _mat.SetColor(PROP_COLOR, c2);
        }
    }

    float GetAlpha()
    {
        if (_mat == null) return 0f;
        if (_mat.HasProperty(PROP_BASE))  return _mat.GetColor(PROP_BASE).a;
        if (_mat.HasProperty(PROP_COLOR)) return _mat.GetColor(PROP_COLOR).a;
        return 0f;
    }

    // global helpers
    public static void TryEnterGlobal()
    {
        #if UNITY_2022_2_OR_NEWER
        var inst = Instance ?? FindFirstObjectByType<DarkDomeController>(UnityEngine.FindObjectsInactive.Include);
        #else
        var inst = Instance ?? FindObjectOfType<DarkDomeController>(true);
        #endif
        inst?.Enter();
    }

    public static void TryExitGlobal()
    {
        #if UNITY_2022_2_OR_NEWER
        var inst = Instance ?? FindFirstObjectByType<DarkDomeController>(UnityEngine.FindObjectsInactive.Include);
        #else
        var inst = Instance ?? FindObjectOfType<DarkDomeController>(true);
        #endif
        inst?.Exit();
    }
}
