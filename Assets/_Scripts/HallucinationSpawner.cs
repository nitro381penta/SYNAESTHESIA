using System.Collections.Generic;
using UnityEngine;

public class HallucinationSpawner : MonoBehaviour
{
    public AudioSampler sampler;
    public Camera mainCam;
    public Sprite[] images;

    [Header("Pool")]
    public int poolSize = 40;
    public Material additiveSpriteMat;

    [Header("Placement")]
    public float minRadius = 4f;
    public float maxRadius = 8f;
    public Vector2 yRange = new Vector2(-0.5f, 2.5f);

    [Header("Timing")]
    public float spawnPerBeat = 2f;     // average sprites per beat
    public Vector2 lifeRange = new Vector2(2.5f, 5.5f);
    public Vector2 scaleRange = new Vector2(0.6f, 1.8f);

    class Item { public Transform t; public SpriteRenderer r; public float t0, life; public float baseScale; }

    readonly List<Item> _pool = new();
    float _lastBeat;

    void Awake()
    {
        if (!mainCam) mainCam = Camera.main;
        for (int i=0;i<poolSize;i++)
        {
            var go = new GameObject("Hallucination");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = additiveSpriteMat;
            sr.enabled = false;
            _pool.Add(new Item{ t=go.transform, r=sr, t0=-999, life=0 });
        }
    }

    void Update()
    {
        if (sampler == null) return;

        // spawn on beat
        if (sampler.Beat && Time.time - _lastBeat > 0.08f)
        {
            _lastBeat = Time.time;
            int count = Mathf.RoundToInt(spawnPerBeat);
            for (int i=0;i<count;i++) ActivateOne();
            if (Random.value < (spawnPerBeat - count)) ActivateOne();
        }

        // animate / billboard / fade
        foreach (var it in _pool)
        {
            if (it.life <= 0) continue;
            float t = (Time.time - it.t0) / it.life;
            if (t >= 1f) { it.r.enabled = false; it.life = 0; continue; }

            if (mainCam)
            {
                var fwd = (it.t.position - mainCam.transform.position).normalized;
                it.t.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }

            float pulse = Mathf.Sin(t * Mathf.PI) * 0.3f + 0.7f;
            it.t.localScale = Vector3.one * (it.baseScale * pulse);

            var c = it.r.color;
            c.a = Mathf.SmoothStep(0f, 1f, Mathf.Min(t*3f, 1f)) * Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((t-0.6f)/0.4f));
            it.r.color = c;

            // slow drift
            it.t.position += new Vector3(0f, Time.deltaTime * 0.15f, 0f);
        }
    }

    void ActivateOne()
    {
        var it = _pool.Find(x => x.life <= 0);
        if (it == null || images == null || images.Length == 0) return;

        float ang = Random.value * Mathf.PI * 2f;
        float rad = Random.Range(minRadius, maxRadius);
        Vector3 pos = new Vector3(Mathf.Cos(ang)*rad, Random.Range(yRange.x, yRange.y), Mathf.Sin(ang)*rad);
        it.t.position = transform.TransformPoint(pos);

        it.t0 = Time.time;
        it.life = Random.Range(lifeRange.x, lifeRange.y);
        it.baseScale = Random.Range(scaleRange.x, scaleRange.y);
        it.r.sprite = images[Random.Range(0, images.Length)];
        var c = Color.HSVToRGB(Random.value, 1f, 1f); c.a = 0f; it.r.color = c;
        it.r.enabled = true;
    }
}
