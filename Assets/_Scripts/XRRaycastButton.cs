using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class XRRaycastButton : MonoBehaviour
{
    public UnityEvent onClick;

    private void OnEnable()
    {
        var interactable = gameObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            interactable.interactionLayers = InteractionLayerMask.GetMask("Default");
        }
    }

    public void TriggerClick()
    {
        onClick.Invoke();
    }
}
