using UnityEngine;
using UnityEngine.SceneManagement;

public class NextMap3 : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem thực thể "other" chạm vào khối này có phải là Player không
        if (other.CompareTag("Player"))
        {
            // Dùng SceneLoader để chuyển scene mượt mà (không giật)
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadSceneAsync("Map03");
            }
            else
            {
                // Fallback nếu chưa setup SceneLoader
                SceneManager.LoadScene("Map03");
            }
        }
    }
}