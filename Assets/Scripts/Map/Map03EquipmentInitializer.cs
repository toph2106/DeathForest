using UnityEngine;
using System.Collections;

public class Map03EquipmentInitializer : MonoBehaviour
{
    public static Map03EquipmentInitializer Instance { get; private set; }

    [Header("1. Cấu Hình Đèn Pin (Flashlight)")]
    [Tooltip("Tự động cấp đèn pin cho Player khi vừa vào Map 03 nếu chưa có")]
    public bool autoEquipFlashlight = true;

    [Tooltip("Tự động bật sáng đèn pin sau khi màn hình sáng xong")]
    public bool turnFlashlightOnAfterIntro = true;

    [Header("2. Cấu Hình Máy Quay (Camcorder UI)")]
    [Tooltip("Tự động đánh dấu đã có máy quay để hiện UI REC khi màn hình sáng")]
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
        StartCoroutine(InitializeEquipmentAfterIntroRoutine());
    }

    IEnumerator InitializeEquipmentAfterIntroRoutine()
    {
        // 1. CHỜ CHO CẮT CẢNH MỞ MÀN MAP 03 CHẠY XONG VÀ MÀN HÌNH ĐÃ SÁNG HOÀN TOÀN
        while (Map03IntroSequence.isCutsceneRunning)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // 2. KHÔI PHỤC ĐÈN PIN (GIỮ NGUYÊN % PIN TỪ MAP 02 SANG)
        if (autoEquipFlashlight)
        {
            FlashlightToggle ft = FlashlightToggle.Instance ?? Object.FindFirstObjectByType<FlashlightToggle>(FindObjectsInactive.Include);
            if (ft != null)
            {
                ft.gameObject.SetActive(true);
                ft.hasFlashlight = true;

                // Chỉ bật đèn nếu đèn còn pin
                if (turnFlashlightOnAfterIntro && ft.currentBattery > 0f)
                {
                    ft.SetFlashlightState(true, false);
                }
                Debug.Log($"[Map03EquipmentInitializer] 🔦 Đã khôi phục Đèn Pin (Số pin thực tế từ Map 02: {ft.currentBattery:F1}%)");
            }
        }

        // 3. KHÔI PHỤC GIAO DIỆN MÁY QUAY CAMCORDER
        if (autoEquipCamcorder)
        {
            CamcorderUI.MarkCameraPickedUp();

            GameObject camObj = GameObject.Find("Camcorder");
            if (camObj != null) camObj.SetActive(true);

            CamcorderUI[] camUIs = Object.FindObjectsByType<CamcorderUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in camUIs)
            {
                c.gameObject.SetActive(true);
            }

            Debug.Log("[Map03EquipmentInitializer] 📹 Đã kích hoạt Giao diện Máy Quay Camcorder sau khi hết Fade!");
        }
    }
}
