using UnityEngine;

public class SettingsToggleUI : MonoBehaviour
{
    public GameObject soundPlayerCanvas;
    public GameObject microphoneInputCanvas;
    public GameObject settingsPanel;

    private bool showingSettings = false;

    public void ToggleSettings()
    {
        showingSettings = !showingSettings;

        settingsPanel.SetActive(showingSettings);

        if (soundPlayerCanvas != null && soundPlayerCanvas.activeSelf)
            soundPlayerCanvas.SetActive(!showingSettings);

        if (microphoneInputCanvas != null && microphoneInputCanvas.activeSelf)
            microphoneInputCanvas.SetActive(!showingSettings);
    }
}
