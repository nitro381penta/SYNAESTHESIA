using UnityEngine;

public class UICanvasSortingFixer : MonoBehaviour
{
    public Canvas logoCanvas;
    public Canvas textCanvas;

    void Start()
    {
        // Force override sorting
        if (logoCanvas != null)
        {
            logoCanvas.overrideSorting = true;
            logoCanvas.sortingOrder = 0;
        }

        if (textCanvas != null)
        {
            textCanvas.overrideSorting = true;
            textCanvas.sortingOrder = 10;
        }
    }
}
