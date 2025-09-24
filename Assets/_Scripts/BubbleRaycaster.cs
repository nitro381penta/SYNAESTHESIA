using UnityEngine;
using UnityEngine.InputSystem;

public class BubbleRaycaster : MonoBehaviour
{
    public float maxDistance = 5f;
    public LayerMask bubbleLayer;
    public InputActionReference tapAction;

    private void OnEnable()
    {
        if (tapAction?.action != null)
        {
            tapAction.action.performed += OnTapPerformed;
            tapAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (tapAction?.action != null)
        {
            tapAction.action.performed -= OnTapPerformed;
            tapAction.action.Disable();
        }
    }

    private void OnTapPerformed(InputAction.CallbackContext context)
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, bubbleLayer))
        {
            Debug.Log("Raycast hit: " + hit.collider.name);

            var bubble = hit.collider.GetComponent<BubbleTrigger>();
            if (bubble != null)
            {
                bubble.OnBubbleTapped();
            }
            else
            {
                Debug.LogWarning("No BubbleTrigger found on hit object.");
            }
        }
        else
        {
            Debug.Log("Raycast did not hit any bubble.");
        }
    }
}
