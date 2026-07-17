using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorExit : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        SceneManager.LoadScene("Map02");
    }
}