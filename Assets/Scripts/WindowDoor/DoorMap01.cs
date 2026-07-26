using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorExit : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        // Dùng SceneLoader để chuyển scene mượt mà (không giật)
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync("Map02");
        }
        else
        {
            // Fallback nếu chưa setup SceneLoader
            SceneManager.LoadScene("Map02");
        }
    }
}