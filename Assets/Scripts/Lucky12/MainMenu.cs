using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public string mainMenuSceneName = "loggedin1";
    // This method will be called when the button is clicked
    public void OnButtonClick()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }
    public void ClosePanel()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}

