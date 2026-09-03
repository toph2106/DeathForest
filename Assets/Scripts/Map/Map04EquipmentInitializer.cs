using UnityEngine;
using System.Collections;

/// <summary>
/// Script khởi tạo và khôi phục toàn bộ trang bị & dữ liệu cho người chơi khi vào Map 04:
/// 1. Tự động đồng bộ và hiển thị đầy đủ các món đồ mang theo từ Map 03 (6 bộ phận thi thể + 2 Gore + Pin...).
/// 2. Khôi phục chính xác % Pin Đèn Pin từ Map 03 chuyển sang.
/// 3. Khôi phục giao diện Máy Quay Camcorder UI (chữ REC và % Pin).
/// </summary>
public class Map04EquipmentInitializer : MonoBehaviour
{
    public static Map04EquipmentInitializer Instance { get; private set; }

    [Header("1. Cấu Hình Đèn Pin (Flashlight)")]
    [Tooltip("Tự động cấp đèn pin cho Player khi vừa vào Map 04 nếu chưa có")]
    public bool autoEquipFlashlight = true;

    [Tooltip("Tự động bật sáng đèn pin khi vào Map 04 (nếu còn pin)")]
    public bool turnFlashlightOn = true;

    [Header("2. Cấu Hình Máy Quay (Camcorder UI)")]
    [Tooltip("Tự động đánh dấu đã có máy quay để hiện UI REC và % Pin")]
    public bool autoEquipCamcorder = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(InitializeEquipmentRoutine());
    }

    IEnumerator InitializeEquipmentRoutine()
    {
        yield return new WaitForSeconds(0.15f);

        // 1. KHÔI PHỤC TÚI ĐỒ (INVENTORY)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RestoreInventoryState();
            Debug.Log($"[Map04EquipmentInitializer] 🎒 Đã khôi phục Hotbar Inventory ({InventoryManager.Instance.CurrentCapacity} ô, {InventoryManager.Instance.GetItemCount()} món) trong Map 04!");
        }

        // 2. KHÔI PHỤC ĐÈN PIN (GIỮ NGUYÊN % PIN TỪ MAP 03 SANG)
        if (autoEquipFlashlight)
        {
            FlashlightToggle ft = FlashlightToggle.Instance ?? Object.FindFirstObjectByType<FlashlightToggle>(FindObjectsInactive.Include);
            if (ft != null)
            {
                ft.gameObject.SetActive(true);
                ft.hasFlashlight = true;
                ft.RestoreFlashlightState();

                if (turnFlashlightOn && ft.currentBattery > 0f)
                {
                    ft.SetFlashlightState(true, false);
                }
                ft.UpdateUI();
                Debug.Log($"[Map04EquipmentInitializer] 🔦 Đã khôi phục Đèn Pin (Số pin thực tế từ Map 03: {ft.currentBattery:F1}%)");
            }
        }

        // 3. KHÔI PHỤC GIAO DIỆN MÁY QUAY CAMCORDER & % PIN
        if (autoEquipCamcorder)
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

            Debug.Log("[Map04EquipmentInitializer] 📹 Đã kích hoạt Giao diện Máy Quay Camcorder & % Pin trong Map 04!");
        }
    }
}
