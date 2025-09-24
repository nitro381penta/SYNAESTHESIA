using UnityEngine;
using System.Collections;

public class GlowOnPlayer : MonoBehaviour
{
    public Renderer rend;
    public Color glowColor = Color.cyan;
    public float glowTime = 0.5f;

    private Material mat;
    private Color originalColor;

    void Start()
    {
        mat = rend.material;
        originalColor = mat.GetColor("_EmissionColor");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            StartCoroutine(Glow());
        }
    }

    IEnumerator Glow()
    {
        mat.SetColor("_EmissionColor", glowColor);
        yield return new WaitForSeconds(glowTime);
        mat.SetColor("_EmissionColor", originalColor);
    }
}
