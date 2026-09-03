using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleCameraOverlay : MonoBehaviour
{
    public static SimpleCameraOverlay Instance { get; private set; }

    [Header("UI Reference")]
    public GameObject cameraOverlayCanvas; // Kéo Canvas kính ngắm vào đây

    public bool HasCamera { get; private set; } = false;

    void Awake()
    {
        Instance = this;
    }

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
            TurnOnCameraView();
        }
        else
        {
            ResetCameraView();
        }
    }

    public void TurnOnCameraView()
    {
        HasCamera = true;
        PlayerPrefs.SetInt("Global_Has_Camera", 1);
        PlayerPrefs.Save();

        if (cameraOverlayCanvas == null)
        {
            GameObject found = GameObject.Find("CameraOverlayCanvas");
            if (found != null) cameraOverlayCanvas = found;
        }

        if (cameraOverlayCanvas != null)
        {
            cameraOverlayCanvas.SetActive(true);
            Transform p = cameraOverlayCanvas.transform.parent;
            while (p != null)
            {
                p.gameObject.SetActive(true);
                p = p.parent;
            }
            Transform[] children = cameraOverlayCanvas.GetComponentsInChildren<Transform>(true);
            foreach (var c in children) if (c != null) c.gameObject.SetActive(true);
        }
    }

    public void ResetCameraView()
    {
        HasCamera = false;
        PlayerPrefs.SetInt("Global_Has_Camera", 0);
        PlayerPrefs.Save();
        if (cameraOverlayCanvas != null)
        {
            cameraOverlayCanvas.SetActive(false);
        }
    }
}
