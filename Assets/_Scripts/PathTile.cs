using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PathTile : MonoBehaviour
{
    [Header("Tile Settings")]
    public Material normalMaterial;
    public Material glowMaterial;
    public float glowDuration = 2f;
    public AnimationCurve glowCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Renderer tileRenderer;
    private Coroutine glowCoroutine;
    public bool IsActivated { get; private set; } = false;

    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        if (normalMaterial != null)
            tileRenderer.material = normalMaterial;
    }

    public void ActivateTile()
    {
        if (IsActivated) return;
        IsActivated = true;

        if (glowCoroutine != null)
            StopCoroutine(glowCoroutine);

        glowCoroutine = StartCoroutine(GlowEffect());
    }

    public void DeactivateTile()
    {
        if (!IsActivated) return;
        IsActivated = false;

        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
            glowCoroutine = null;
        }

        if (normalMaterial != null)
            tileRenderer.material = normalMaterial;
    }

    private System.Collections.IEnumerator GlowEffect()
    {
        float elapsed = 0f;

        while (elapsed < glowDuration)
        {
            elapsed += Time.deltaTime;
            float t = glowCurve.Evaluate(elapsed / glowDuration);

            if (normalMaterial != null && glowMaterial != null)
            {
                Material mat = new Material(normalMaterial);

                if (glowMaterial.HasProperty("_EmissionColor"))
                {
                    Color emission = glowMaterial.GetColor("_EmissionColor");
                    mat.SetColor("_EmissionColor", emission * t);
                    mat.EnableKeyword("_EMISSION");
                }

                if (glowMaterial.HasProperty("_Color"))
                {
                    Color glowColor = glowMaterial.color;
                    Color baseColor = normalMaterial.color;
                    mat.color = Color.Lerp(baseColor, glowColor, t);
                }

                tileRenderer.material = mat;
            }

            yield return null;
        }

        if (glowMaterial != null)
            tileRenderer.material = glowMaterial;
    }
}
