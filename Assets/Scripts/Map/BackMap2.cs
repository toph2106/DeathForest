using UnityEngine;
using UnityEngine.SceneManagement;

public class BackMap2 : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem thực thể "other" chạm vào khối này có phải là Player không
        if (other.CompareTag("Player"))
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
}