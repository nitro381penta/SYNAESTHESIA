using UnityEngine;

public class BubbleGlowOnHover : MonoBehaviour
{
    public Color glowColor = Color.cyan;
    private Color originalColor;
    private Material bubbleMaterial;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        bubbleMaterial = rend.material;

        originalColor = bubbleMaterial.GetColor("_Color");

    }

    public void OnHoverEnter()
    {
        bubbleMaterial.SetColor("_Color", glowColor);
    }

    public void OnHoverExit()
    {
        bubbleMaterial.SetColor("_Color", originalColor);
    }
}
