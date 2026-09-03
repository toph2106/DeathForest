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
        // Nếu quay về MainMenu hoặc vào Map01/Map02 -> Tự động ẩn UI Camcorder
        if (scene.name == "MainMenu" || scene.name == "Map01" || scene.name == "Map02")
        {
            ResetCameraView();
        }
    }

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu" || sceneName == "Map01" || sceneName == "Map02")
        {
            ResetCameraView();
            return;
        }

        bool hasCam = HasCamera || CamcorderUI.HasPickedUpCamera || (PlayerPrefs.GetInt("Global_Has_Camera", 0) == 1);
        if (hasCam)
        {
            HasCamera = true;
            if (cameraOverlayCanvas != null) cameraOverlayCanvas.SetActive(true);
        }
        else
        {
            // Mặc định lúc mới vào game chưa nhặt máy quay thì ẩn đi
            if (cameraOverlayCanvas != null) cameraOverlayCanvas.SetActive(false);
        }
    }

    // Hàm này sẽ được gọi khi người chơi bấm F nhặt máy quay
    public void TurnOnCameraView()
    {
        HasCamera = true;
        PlayerPrefs.SetInt("Global_Has_Camera", 1);
        PlayerPrefs.Save();
        if (cameraOverlayCanvas != null)
        {
            cameraOverlayCanvas.SetActive(true); // Bật hiệu ứng kính ngắm lên
        }
    }

    public void ResetCameraView()
    {
        HasCamera = false;
        PlayerPrefs.DeleteKey("Global_Has_Camera");
        PlayerPrefs.Save();
        if (cameraOverlayCanvas != null)
        {
            cameraOverlayCanvas.SetActive(false);
        }
    }
}