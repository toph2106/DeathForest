using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CamcorderUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo cái Text 00:00:00 ở trên cùng vào đây")]
    public TMP_Text recTimeText;

    [Tooltip("Kéo cái Text AM 00:00 ở dưới cùng vào đây")]
    public TMP_Text clockText;

    [Header("Battery UI (Ngay dưới chữ REC)")]
    [Tooltip("Kéo TextMeshProUGUI hiển thị % Pin ở ngay dưới chữ REC vào đây")]
    public TMP_Text batteryText;

    [Header("Clock Settings")]
    [Tooltip("Giờ bắt đầu đếm (Theo định dạng 24h. 0 = 12h đêm, 13 = 1h chiều)")]
    public int startHour = 0;

    [Tooltip("Phút bắt đầu đếm")]
    public int startMinute = 0;

    // Singleton để giữ bộ đếm sống sót qua các Scene
    public static CamcorderUI Instance { get; private set; }

    // Lưu thời gian đã trôi qua (STATIC để không bị mất khi chuyển scene)
    private static float savedTimer = -1f;
    public static bool HasPickedUpCamera { get; private set; } = false;

    // Sự kiện khi bộ đếm đạt mốc 10 giây
    public static event System.Action OnTimerReached10s;
    private bool hasTriggered10s = false;

    // Bộ đếm thời gian từ lúc Object được Active
    private float activeTimer = 0f;
    public float CurrentActiveTime => activeTimer;

    void Awake()
    {
        // Nếu đã có 1 bản CamcorderUI tồn tại rồi -> Xóa bản mới đi
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Tách ra khỏi GameObject cha (nếu có) để trở thành Root GameObject trước khi gọi DontDestroyOnLoad
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // CHƯA NHẶT MÁY QUAY -> ÉP ẨN GIAO DIỆN MÁY QUAY MẶC ĐỊNH KHI VỪA MỚI VÀO MAP 01
        if (!HasPickedUpCamera)
        {
            gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // CHỈ XÓA VÀ RESET UI MÁY QUAY KHI VỀ MAINMENU
        if (scene.name == "MainMenu")
        {
            ResetTimer();
        }
    }

    void OnEnable()
    {
        // Nếu đã có thời gian cũ được lưu -> Khôi phục lại, KHÔNG reset về 0
        if (savedTimer >= 0f)
        {
            activeTimer = savedTimer;
        }
        else
        {
            activeTimer = 0f;
        }
    }

    void OnDisable()
    {
        // Trước khi bị tắt hoặc chuyển scene -> Lưu lại thời gian hiện tại
        savedTimer = activeTimer;
    }

    void Update()
    {
        // Thời gian trôi qua mỗi frame (Thời gian thực)
        activeTimer += Time.deltaTime;

        // ============================================
        // 1. CẬP NHẬT THỜI GIAN QUAY (00:00:00)
        // ============================================
        if (recTimeText != null)
        {
            int recHours = Mathf.FloorToInt(activeTimer / 3600f);
            int recMinutes = Mathf.FloorToInt((activeTimer % 3600f) / 60f);
            int recSeconds = Mathf.FloorToInt(activeTimer % 60f);

            recTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", recHours, recMinutes, recSeconds);
        }

        // ============================================
        // 2. CẬP NHẬT ĐỒNG HỒ TRONG GAME (AM 00:00)
        // ============================================
        if (clockText != null)
        {
            // Tính tổng số giây kể từ thời điểm startHour:startMinute
            float totalSeconds = (startHour * 3600) + (startMinute * 60) + activeTimer;

            int clockHours24 = Mathf.FloorToInt(totalSeconds / 3600f) % 24;
            int clockMinutes = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);

            // Xác định AM hay PM
            string amPm = clockHours24 < 12 ? "AM" : "PM";

            // Ép về hệ 12 giờ để hiển thị giống ảnh của bạn (00 đến 11)
            int clockHours12 = clockHours24 % 12;

            clockText.text = string.Format("{0} {1:00}:{2:00}", amPm, clockHours12, clockMinutes);
        }

        // ============================================
        // 3. CẬP NHẬT % PIN ĐÈN PIN (ĐỔI MÀU TRẮNG -> VÀNG -> ĐỎ NHÁY CẢNH BÁO)
        // ============================================
        if (batteryText != null && FlashlightToggle.Instance != null)
        {
            float maxBat = FlashlightToggle.Instance.maxBattery;
            float curBat = FlashlightToggle.Instance.currentBattery;
            float pctRatio = (maxBat > 0) ? (curBat / maxBat) : 0f;
            int pct = Mathf.CeilToInt(pctRatio * 100f);

            batteryText.text = pct + "%";

            // ĐỔI MÀU CHỮ THEO ĐỨNG PHONG CÁCH MÁY QUAY CAMCORDER RETRO
            if (pctRatio > 0.5f)
            {
                batteryText.color = Color.white;
            }
            else if (pctRatio > 0.2f)
            {
                batteryText.color = new Color(1f, 0.82f, 0.2f, 1f);
            }
            else
            {
                if (curBat <= 0f)
                {
                    batteryText.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                }
                else
                {
                    float alpha = (Mathf.Sin(Time.time * 6f) > 0f) ? 1f : 0.35f;
                    batteryText.color = new Color(1f, 0.2f, 0.2f, alpha);
                }
            }
        }
    }

    public static void MarkCameraPickedUp()
    {
        HasPickedUpCamera = true;
    }

    public static void ResetPickedUpCameraState()
    {
        HasPickedUpCamera = false;
        savedTimer = -1f;
        if (Instance != null)
        {
            Instance.hasTriggered10s = false;
            Instance.activeTimer = 0f;
            Instance.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gọi hàm này khi về MainMenu để tiêu hủy bản cũ hoàn toàn, cho phép chơi lượt mới tạo bản UI máy quay mới
    /// </summary>
    public static void ResetTimer()
    {
        savedTimer = -1f;
        HasPickedUpCamera = false;
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }
}