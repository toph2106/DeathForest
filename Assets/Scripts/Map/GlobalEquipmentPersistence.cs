using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý đồng bộ và bảo lưu toàn bộ trang bị & dữ liệu xuyên suốt các Map (Map 01 -> 02 -> 03 -> 04 -> 05):
/// 1. Tự động khôi phục toàn bộ vật phẩm trong túi đồ (Inventory) kèm Model 3D xoay trên Hotbar.
/// 2. Tự động khôi phục % Pin thực tế của Đèn Pin (Flashlight).
/// 3. Tự động khôi phục giao diện Máy Quay (Camcorder UI) và kính ngắm REC.
/// </summary>
public class GlobalEquipmentPersistence : MonoBehaviour
{
    public static GlobalEquipmentPersistence Instance { get; private set; }

    [Header("1. Đèn Pin (Flashlight)")]
    [Tooltip("Tự động khôi phục đèn pin khi vào Scene mới")]
    public bool autoRestoreFlashlight = true;

    [Header("2. Máy Quay (Camcorder UI)")]
    [Tooltip("Tự động khôi phục giao diện máy quay khi vào Scene mới")]
    public bool autoRestoreCamcorder = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("[GlobalEquipmentPersistence]");
            obj.AddComponent<GlobalEquipmentPersistence>();
            DontDestroyOnLoad(obj);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(SyncEquipmentRoutine());
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        StartCoroutine(SyncEquipmentRoutine());
    }

    private IEnumerator SyncEquipmentRoutine()
    {
        yield return new WaitForSeconds(0.12f);

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene == "MainMenu") yield break;

        // Nếu ở Map01, Map02 hoặc Map05: Tuyệt đối không tự động nạp đồ đạc, đèn pin hay máy quay của Map03/Map04!
        // Người chơi bắt đầu nguyên bản (Map 05 vừa tỉnh dậy sau ác mộng, túi đồ sạch sẽ, không có thiết bị).
        if (currentScene == "Map01" || currentScene == "Map02" || currentScene == "Map05")
        {
            GameSaveManager.isResettingData = false;
            Debug.Log($"[GlobalEquipmentPersistence] 🎮 Bắt đầu {currentScene}: Chơi nguyên bản (không nạp đồ đạc hay thiết bị từ Map khác sang).");
            yield break;
        }

        GameSaveManager.isResettingData = false;

        // 1. KHÔI PHỤC ĐÈN PIN & % PIN
        if (autoRestoreFlashlight)
        {
            bool shouldHaveFlash = (PlayerPrefs.GetInt("Global_Has_Flashlight", 0) == 1) || (currentScene == "Map03" || currentScene == "Map04");
            if (shouldHaveFlash)
            {
                FlashlightToggle ft = FlashlightToggle.Instance ?? Object.FindFirstObjectByType<FlashlightToggle>(FindObjectsInactive.Include);
                if (ft != null)
                {
                    ft.gameObject.SetActive(true);
                    ft.hasFlashlight = true;
                    ft.RestoreFlashlightState();
                    ft.UpdateUI();
                    Debug.Log($"[GlobalEquipmentPersistence] 🔦 Đã đồng bộ Đèn Pin (% Pin: {ft.currentBattery:F1}%) cho {currentScene}");
                }
            }
        }

        // 2. KHÔI PHỤC MÁY QUAY CAMCORDER
        if (autoRestoreCamcorder)
        {
            bool shouldHaveCam = (PlayerPrefs.GetInt("Global_Has_Camera", 0) == 1) || (currentScene == "Map03" || currentScene == "Map04");
            if (shouldHaveCam)
            {
                CamcorderUI.MarkCameraPickedUp();

                CamcorderUI[] camUIs = Object.FindObjectsByType<CamcorderUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in camUIs)
                {
                    c.gameObject.SetActive(true);
                    Transform p = c.transform.parent;
                    while (p != null)
                    {
                        p.gameObject.SetActive(true);
                        p = p.parent;
                    }
                }

                SimpleCameraOverlay overlay = Object.FindFirstObjectByType<SimpleCameraOverlay>(FindObjectsInactive.Include);
                if (overlay != null)
                {
                    overlay.TurnOnCameraView();
                }
                Debug.Log($"[GlobalEquipmentPersistence] 📹 Đã kích hoạt Giao diện Máy Quay Camcorder cho {currentScene}");
            }
        }

        // 3. CẬP NHẬT GIAO DIỆN HOTBAR INVENTORY
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RestoreInventoryState();
            Debug.Log($"[GlobalEquipmentPersistence] 🎒 Đã đồng bộ {InventoryManager.Instance.CurrentCapacity} ô túi đồ ({InventoryManager.Instance.GetItemCount()} món) cho {currentScene}");
        }
    }
}
