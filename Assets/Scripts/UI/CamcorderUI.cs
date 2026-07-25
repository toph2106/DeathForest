using UnityEngine;
using TMPro;

public class CamcorderUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo cái Text 00:00:00 ở trên cùng vào đây")]
    public TMP_Text recTimeText; 
    
    [Tooltip("Kéo cái Text AM 00:00 ở dưới cùng vào đây")]
    public TMP_Text clockText;

    [Header("Clock Settings")]
    [Tooltip("Giờ bắt đầu đếm (Theo định dạng 24h. 0 = 12h đêm, 13 = 1h chiều)")]
    public int startHour = 0;
    
    [Tooltip("Phút bắt đầu đếm")]
    public int startMinute = 0;

    // Singleton để giữ bộ đếm sống sót qua các Scene
    public static CamcorderUI Instance { get; private set; }

    // Lưu thời gian đã trôi qua (STATIC để không bị mất khi chuyển scene)
    private static float savedTimer = -1f;
    private static bool hasPickedUpCamera = false;

    // Bộ đếm thời gian từ lúc Object được Active
    private float activeTimer = 0f;

    void Awake()
    {
        // Nếu đã có 1 bản CamcorderUI tồn tại rồi -> Xóa bản mới đi
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Giữ Canvas này sống sót khi chuyển Scene
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
    }

    /// <summary>
    /// Gọi hàm này từ bất kỳ đâu để reset toàn bộ (ví dụ khi bắt đầu game mới)
    /// </summary>
    public static void ResetTimer()
    {
        savedTimer = -1f;
        hasPickedUpCamera = false;
    }
}
