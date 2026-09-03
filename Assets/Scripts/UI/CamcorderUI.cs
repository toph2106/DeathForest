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

    // Singleton
    public static CamcorderUI Instance { get; private set; }

    // Lưu thời gian đã trôi qua
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
        Instance = this;
        AutoFindUIReferences();

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu" || sceneName == "Map01" || sceneName == "Map02")
        {
            HasPickedUpCamera = false;
            PlayerPrefs.SetInt("Global_Has_Camera", 0);
            savedTimer = -1f;
            gameObject.SetActive(false);
            return;
        }

        if (sceneName == "Map03" || sceneName == "Map04" || sceneName == "Map05")
        {
            HasPickedUpCamera = true;
            PlayerPrefs.SetInt("Global_Has_Camera", 1);
            PlayerPrefs.Save();
            gameObject.SetActive(true);
        }
        else
        {
            HasPickedUpCamera = (PlayerPrefs.GetInt("Global_Has_Camera", 0) == 1);
            gameObject.SetActive(HasPickedUpCamera);
        }
    }

    void Start()
    {
        AutoFindUIReferences();

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MainMenu" || sceneName == "Map01" || sceneName == "Map02")
        {
            if (!HasPickedUpCamera)
            {
                gameObject.SetActive(false);
            }
        }
        else if (sceneName == "Map03" || sceneName == "Map04" || sceneName == "Map05")
        {
            HasPickedUpCamera = true;
            gameObject.SetActive(true);
        }
    }

    public void AutoFindUIReferences()
    {
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps)
        {
            string n = t.gameObject.name.ToLower();
            if (recTimeText == null && (n.Contains("time") || n.Contains("rec") || n.Contains("00:00:00") || n.Contains("timer")))
            {
                recTimeText = t;
            }
            else if (clockText == null && (n.Contains("clock") || n.Contains("am") || n.Contains("pm") || n.Contains("date") || n.Contains("day")))
            {
                clockText = t;
            }
            else if (batteryText == null && (n.Contains("battery") || n.Contains("pin") || n.Contains("%")))
            {
                batteryText = t;
            }
        }
    }

    void OnEnable()
    {
        Instance = this;
        AutoFindUIReferences();

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
        savedTimer = activeTimer;
    }

    void Update()
    {
        bool hasCam = HasPickedUpCamera;
        bool hasFlash = (FlashlightToggle.Instance != null && FlashlightToggle.Instance.hasFlashlight);

        // A. Quản lý hiển thị % Pin (Chỉ hiện khi có đèn pin)
        if (batteryText != null && batteryText.gameObject.activeSelf != hasFlash)
        {
            batteryText.gameObject.SetActive(hasFlash);
        }

        // B. Quản lý hiển thị các thành phần máy quay
        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text txt in allTexts)
        {
            if (txt == batteryText) continue;
            if (txt.gameObject.activeSelf != hasCam)
            {
                txt.gameObject.SetActive(hasCam);
            }
        }

        UnityEngine.UI.Image[] allImages = GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (UnityEngine.UI.Image img in allImages)
        {
            if (batteryText != null && img.transform.IsChildOf(batteryText.transform)) continue;
            if (img.gameObject.activeSelf != hasCam)
            {
                img.gameObject.SetActive(hasCam);
            }
        }

        // C. Cập nhật bộ đếm và đồng hồ nếu có máy quay
        if (hasCam)
        {
            activeTimer += Time.deltaTime;

            if (!hasTriggered10s && activeTimer >= 10f)
            {
                hasTriggered10s = true;
                OnTimerReached10s?.Invoke();
            }

            if (recTimeText != null)
            {
                int recHours = Mathf.FloorToInt(activeTimer / 3600f);
                int recMinutes = Mathf.FloorToInt((activeTimer % 3600f) / 60f);
                int recSeconds = Mathf.FloorToInt(activeTimer % 60f);
                recTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", recHours, recMinutes, recSeconds);
            }

            if (clockText != null)
            {
                float totalSeconds = (startHour * 3600) + (startMinute * 60) + activeTimer;
                int clockHours24 = Mathf.FloorToInt(totalSeconds / 3600f) % 24;
                int clockMinutes = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
                string amPm = clockHours24 < 12 ? "AM" : "PM";
                int clockHours12 = clockHours24 % 12;
                clockText.text = string.Format("{0} {1:00}:{2:00}", amPm, clockHours12, clockMinutes);
            }
        }

        // D. Cập nhật % pin
        if (hasFlash && batteryText != null && FlashlightToggle.Instance != null)
        {
            float maxBat = FlashlightToggle.Instance.maxBattery;
            float curBat = FlashlightToggle.Instance.currentBattery;
            float pctRatio = (maxBat > 0) ? (curBat / maxBat) : 0f;
            int pct = Mathf.CeilToInt(pctRatio * 100f);

            batteryText.text = pct + "%";

            if (pctRatio > 0.3f)
            {
                batteryText.color = Color.white;
            }
            else if (pctRatio > 0.05f)
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
        PlayerPrefs.SetInt("Global_Has_Camera", 1);
        PlayerPrefs.Save();

        // 1. Kích hoạt toàn bộ CamcorderUI component
        CamcorderUI[] camUIs = Object.FindObjectsByType<CamcorderUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in camUIs)
        {
            if (c == null) continue;
            c.gameObject.SetActive(true);
            Transform p = c.transform.parent;
            while (p != null)
            {
                p.gameObject.SetActive(true);
                p = p.parent;
            }

            Transform[] allChildren = c.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child != null) child.gameObject.SetActive(true);
            }

            c.AutoFindUIReferences();
        }

        // 2. Kích hoạt toàn bộ GameObject có tên Camcorder hoặc CameraOverlay dưới Canvas
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in allCanvases)
        {
            if (canvas == null) continue;
            canvas.gameObject.SetActive(true);

            Transform[] allTransforms = canvas.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                string n = t.gameObject.name.ToLower();
                if (n.Contains("camcorder") || n.Contains("cameraoverlay") || n.Contains("camcanvas") || n.Contains("rec"))
                {
                    t.gameObject.SetActive(true);
                    Transform p = t.parent;
                    while (p != null)
                    {
                        p.gameObject.SetActive(true);
                        p = p.parent;
                    }

                    Transform[] children = t.GetComponentsInChildren<Transform>(true);
                    foreach (var child in children)
                    {
                        if (child != null) child.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    public static void ResetPickedUpCameraState()
    {
        HasPickedUpCamera = false;
        savedTimer = -1f;
        PlayerPrefs.SetInt("Global_Has_Camera", 0);
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.hasTriggered10s = false;
            Instance.activeTimer = 0f;
            Instance.gameObject.SetActive(false);
        }
    }

    public static void ResetTimer()
    {
        savedTimer = -1f;
        HasPickedUpCamera = false;
        PlayerPrefs.SetInt("Global_Has_Camera", 0);
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.hasTriggered10s = false;
            Instance.activeTimer = 0f;
            Instance.gameObject.SetActive(false);
        }
    }
}
