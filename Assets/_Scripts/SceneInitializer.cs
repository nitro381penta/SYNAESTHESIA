using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    void Start()
    {
        Debug.Log("SceneInitializer running...");

        // Reset UI state
        UIManagerXR.Instance?.ResetUI();

        // Show all bubbles
        BubbleManager.Instance?.ShowAllBubbles();
    }
}
