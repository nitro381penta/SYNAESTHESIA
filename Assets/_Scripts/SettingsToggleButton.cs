using UnityEngine;

public class SettingsToggleButton : MonoBehaviour
{
    public GameObject targetPanelToHide;
    public GameObject settingsPanel;

    public void ToggleSettingsUI()
    {
        if (targetPanelToHide != null)
            targetPanelToHide.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }
}