using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleCameraOverlay : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject cameraOverlayCanvas; // Kéo Canvas kính ngắm vào đây

    public bool HasCamera { get; private set; } = false;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Nếu quay về MainMenu hoặc nạp lại Scene -> Tự động ẩn UI Camcorder
        if (scene.name == "MainMenu")
        {
            ResetCameraView();
        }
    }

    void Start()
    {
        // Mặc định lúc mới vào game chưa nhặt máy quay thì ẩn đi
        if (cameraOverlayCanvas != null) cameraOverlayCanvas.SetActive(false);
    }

    // Hàm này sẽ được gọi khi người chơi bấm F nhặt máy quay
    public void TurnOnCameraView()
    {
        HasCamera = true;
        if (cameraOverlayCanvas != null)
        {
            cameraOverlayCanvas.SetActive(true); // Bật hiệu ứng kính ngắm lên
        }
    }

    public void ResetCameraView()
    {
        HasCamera = false;
        if (cameraOverlayCanvas != null)
        {
            cameraOverlayCanvas.SetActive(false);
        }
    }
}