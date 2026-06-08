using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorExit : MonoBehaviour, IInteractable
{
    public string nextSceneName = "Map02";

    public void Interact()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}