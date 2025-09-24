using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HoverScaler : MonoBehaviour
{
    private Vector3 originalScale;
    public float hoverScaleFactor = 1.1f;
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;

    private void Start()
    {
        originalScale = transform.localScale;
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
    }

    private void OnDestroy()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        transform.localScale = originalScale * hoverScaleFactor;
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        transform.localScale = originalScale;
    }
}
