using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Quản lý Chế Độ Bất Tử / Demo Mode (Phím M):
/// - Bấm phím 'M' để Bật/Tắt chế độ Bất Tử.
/// - Khi Bật M: Chữ REC chuyển sang Màu Vàng Bạch Kim (Platinum Gold) cực sang và đẹp!
/// - Khi Tắt M: Chữ REC trở lại Màu Trắng mặc định ban đầu.
/// - Khi Bị Quái/Chó tóm: Vẫn Jumpscare đầy đủ 2s để Demo, sau 2s con chó biến mất và cho bạn đi tiếp, không bị Game Over về Menu!
/// </summary>
public class GodModeManager : MonoBehaviour
{
    public static GodModeManager Instance { get; private set; }
    public static bool IsGodModeActive = false;

    [Header("1. Phím Tắt (Shortcut Key)")]
    [Tooltip("Phím tắt để bật/tắt chế độ bất tử (Mặc định: Phím M)")]
    public KeyCode toggleKey = KeyCode.M;

    [Header("2. Kéo Thả UI Chữ REC")]
    [Tooltip("Kéo GameObject chữ REC (hoặc cụm REC) vào đây. Nếu để trống code tự quét tìm trong Scene!")]
    public GameObject recTarget;

    [Tooltip("(Tùy chọn) Kéo trực tiếp component TextMeshProUGUI chữ REC vào đây")]
    public TMP_Text recTMPText;

    [Tooltip("(Tùy chọn) Kéo trực tiếp component Text chữ REC vào đây")]
    public Text recLegacyText;

    [Header("3. Cấu Hình Màu Sắc (Color Palette)")]
    [Tooltip("Màu Vàng Bạch Kim (Platinum Gold) tuyệt đẹp khi BẬT GodMode")]
    public Color platinumGoldColor = new Color(1.0f, 0.88f, 0.40f, 1.0f); // #FFE066

    [Tooltip("Màu Trắng nguyên bản khi TẮT GodMode")]
    public Color normalColor = Color.white;

    [Tooltip("Thời gian chuyển đổi màu mượt mà (giây)")]
    public float colorTransitionDuration = 0.25f;

    private Coroutine colorRoutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("[GodModeManager]");
            go.AddComponent<GodModeManager>();
        }
    }

    void Start()
    {
        FindRecTargetIfNull();
        ApplyColorInstant(IsGodModeActive ? platinumGoldColor : normalColor);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleGodMode();
        }
    }

    public void ToggleGodMode()
    {
        IsGodModeActive = !IsGodModeActive;

        FindRecTargetIfNull();

        Color targetCol = IsGodModeActive ? platinumGoldColor : normalColor;

        if (colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(SmoothColorRoutine(targetCol));

        if (IsGodModeActive)
        {
            Debug.Log("<color=#FFE066><b>[GodMode] 👑 BẬT BẤT TỬ (Phím M) -> REC chuyển sang Màu Vàng Bạch Kim!</b></color>");
        }
        else
        {
            Debug.Log("<color=white><b>[GodMode] ❌ TẮT BẤT TỬ (Phím M) -> REC trở lại Màu Trắng bình thường!</b></color>");
        }
    }

    private void FindRecTargetIfNull()
    {
        if (recTMPText != null || recLegacyText != null || recTarget != null) return;

        // 1. Tìm qua CamcorderUIAnimation
        CamcorderUIAnimation animUI = Object.FindFirstObjectByType<CamcorderUIAnimation>();
        if (animUI != null && animUI.recText != null)
        {
            recTMPText = animUI.recText;
            return;
        }

        // 2. Tìm qua CamcorderUI
        CamcorderUI camUI = Object.FindFirstObjectByType<CamcorderUI>();
        if (camUI != null && camUI.recTimeText != null)
        {
            recTMPText = camUI.recTimeText;
            return;
        }

        // 3. Tìm GameObject tên 'REC', 'RecText', 'Rec' trong Scene
        GameObject[] allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjs)
        {
            if (obj != null && obj.scene.isLoaded)
            {
                string lower = obj.name.ToLower();
                if (lower == "rec" || lower == "rectext" || lower == "rec_text")
                {
                    recTarget = obj;
                    recTMPText = obj.GetComponent<TMP_Text>() ?? obj.GetComponentInChildren<TMP_Text>();
                    recLegacyText = obj.GetComponent<Text>() ?? obj.GetComponentInChildren<Text>();
                    return;
                }
            }
        }
    }

    private void ApplyColorInstant(Color col)
    {
        if (recTMPText != null) recTMPText.color = col;
        if (recLegacyText != null) recLegacyText.color = col;

        if (recTarget != null)
        {
            Graphic[] graphics = recTarget.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                if (g != null) g.color = col;
            }
        }
    }

    private IEnumerator SmoothColorRoutine(Color targetColor)
    {
        Color startCol = normalColor;
        if (recTMPText != null) startCol = recTMPText.color;
        else if (recLegacyText != null) startCol = recLegacyText.color;
        else if (recTarget != null)
        {
            Graphic g = recTarget.GetComponentInChildren<Graphic>();
            if (g != null) startCol = g.color;
        }

        float elapsed = 0f;
        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / colorTransitionDuration);
            Color current = Color.Lerp(startCol, targetColor, t);
            ApplyColorInstant(current);
            yield return null;
        }

        ApplyColorInstant(targetColor);
        colorRoutine = null;
    }
}
