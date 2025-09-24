using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class KaleidoCloner : MonoBehaviour
{
    public Transform source;
    [Range(2, 24)] public int slices = 6;
    public bool includeSource = true;
    public bool rebuildOnEnable = true;

    readonly List<Transform> _clones = new();

    void OnEnable()
    {
        if (rebuildOnEnable) Rebuild();
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        Rebuild();
    }

    public void Rebuild()
    {
        // Clear previous clones
        for (int i = _clones.Count - 1; i >= 0; --i)
        {
            if (_clones[i]) {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(_clones[i].gameObject);
                else Destroy(_clones[i].gameObject);
#else
                Destroy(_clones[i].gameObject);
#endif
            }
        }
        _clones.Clear();

        if (!source || slices < 2) return;

        float step = 360f / slices;
        int copies = includeSource ? slices - 1 : slices;

        for (int i = 0; i < copies; i++)
        {
            var go = Instantiate(source.gameObject, source.parent);
            go.name = $"{source.name}_Clone_{i}";
            var t = go.transform;

            t.localPosition = source.localPosition;
            t.localScale    = source.localScale;

            // If we keep the source, start from the next slice; else start at 0
            float angle = step * (includeSource ? (i + 1) : i);
            t.localRotation = Quaternion.Euler(0f, angle, 0f);

            _clones.Add(t);
        }

        if (source) source.gameObject.SetActive(includeSource);
    }
}
