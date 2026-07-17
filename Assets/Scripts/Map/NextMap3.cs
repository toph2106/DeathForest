using UnityEngine;
using UnityEngine.SceneManagement;

public class NextMap3 : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem thực thể "other" chạm vào khối này có phải là Player không
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene("Map03");
        }
    }
}