using UnityEngine;


public class RayDebugDisabler : MonoBehaviour
{
    public bool disableAllLineVisualsAtStart = true;
    void Start()
    {
        if (!disableAllLineVisualsAtStart) return;
        foreach (var lv in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>(FindObjectsSortMode.None))
        {
            lv.enabled = false;
            Debug.Log("Disabled line visual on: " + lv.gameObject.name);
        }
        
    }
}
