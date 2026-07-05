using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public GameObject menuPanel;
    public GameObject settingsPanel;

   
    public void PlayGame()
    {
        SceneManager.LoadScene("Map01");
    }

    
    public void OpenSettings()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

   
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

   
    public void QuitGame()
    {
        Application.Quit();
    }
}