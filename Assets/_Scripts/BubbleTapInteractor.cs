using UnityEngine;
using UnityEngine.InputSystem;

public class BubbleTapInteractor : MonoBehaviour
{
    public float rayLength = 2f;
    public LayerMask bubbleLayer;
    public InputActionReference tapAction; // XR controller trigger input

    private void OnEnable()
    {
        if (tapAction?.action != null)
        {
            tapAction.action.performed += OnTap;
            tapAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (tapAction?.action != null)
        {
            tapAction.action.performed -= OnTap;
            tapAction.action.Disable();
        }
    }

    private void OnTap(InputAction.CallbackContext context)
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayLength, bubbleLayer))
        {
            Debug.Log("Bubble hit by raycast: " + hit.collider.name);

            var bubble = hit.collider.GetComponent<BubbleTrigger>();
            if (bubble != null)
            {
                bubble.OnBubbleTapped();
            }
            else
            {
                Debug.LogWarning("No BubbleTrigger component found on hit object.");
            }
        }
        else
        {
            Debug.Log("Raycast did not hit any bubble.");
        }
    }
}
