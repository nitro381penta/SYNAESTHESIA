using UnityEngine;

public class ReturnFromSettingsButton : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject panelToReturnTo;

    public void ReturnToPrevious()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (panelToReturnTo != null)
            panelToReturnTo.SetActive(true);
    }
}
