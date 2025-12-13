using UnityEngine;

public class SettingManager : MonoBehaviour
{
    [SerializeField] private GameObject SettingPanel;
    [SerializeField] private GameObject exitPanel;
    
    public void ToggleSettingsPanel()
    {
        if (SettingPanel != null)
        {
           SettingPanel.SetActive(!SettingPanel.activeSelf);
        }
    }
    public void CloseSettingsPanel()
    {
        if (SettingPanel != null)
        {
            SettingPanel.SetActive(false);
        }
    }
    // Exit Confirmation Panel
    public void OpenExitPanel()
    {
        if (exitPanel != null)
        {
            exitPanel.SetActive(true);
        }
    }

    public void CloseExitPanel()
    {
        if (exitPanel != null)
        {
            exitPanel.SetActive(false);
        }
    }

    public void ConfirmExit()
    {
        Debug.Log("Exiting Game...");

#if UNITY_EDITOR
        // Stop play mode if running in Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quit application in build
        Application.Quit();
#endif
    }
}

