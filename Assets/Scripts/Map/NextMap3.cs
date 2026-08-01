using UnityEngine;
using UnityEngine.SceneManagement;

public class NextMap3 : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. TỰ ĐỘNG LƯU TIẾN TRÌNH: Mở khóa Map 03 khi qua màn Map 02
            GameSaveManager.UnlockLevel(3);

            // 2. Chuyển sang Map 03
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadSceneAsync("Map03");
            }
            else
            {
                SceneManager.LoadScene("Map03");
            }
        }
    }
}