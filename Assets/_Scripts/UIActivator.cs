using UnityEngine;
using UnityEngine.InputSystem;

public class UIActivator : MonoBehaviour
{
    [Header("UI Canvases")]
    public GameObject soundPlayerCanvas;
    public GameObject microphoneInputCanvas;

    public Transform handAnchorRight; 


    [Header("Input")]
    public InputActionReference toggleAction;

    private GameObject currentUI;

    private void OnEnable()
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.performed += OnTogglePressed;
            toggleAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (toggleAction != null && toggleAction.action != null)
        {
            toggleAction.action.performed -= OnTogglePressed;
            toggleAction.action.Disable();
        }
    }

    private void OnTogglePressed(InputAction.CallbackContext context)
    {
        if (currentUI != null)
        {
            bool isActive = currentUI.activeSelf;
            currentUI.SetActive(!isActive);
            Debug.Log("Toggled UI: " + currentUI.name + " - Active: " + !isActive);
        }
    }

    // Call from BubbleTrigger.cs when a bubble is tapped
    public void SetCurrentUI(GameObject ui)
    {
        if (currentUI != null && currentUI != ui)
            currentUI.SetActive(false);

        currentUI = ui;
        currentUI?.SetActive(true);
    }
}
