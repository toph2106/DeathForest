using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Quản lý Màn Hình Game Over Sau Jumpscare:
/// 1. Làm mờ đen toàn màn hình (Fade To Black)
/// 2. Khóa âm thanh / mở khóa chuột
/// 3. Hiện màn hình tối đen + chữ hướng dẫn "Nhấp chuột bất kỳ để quay về Menu"
/// 4. Nhận phím/chuột bất kỳ để chuyển mượt về MainMenu
/// </summary>
public class GameOverJumpscareManager : MonoBehaviour
{
    public static GameOverJumpscareManager Instance { get; private set; }

    [Header("1. UI Màn Hình Đen & Text")]
    [Tooltip("Kéo FadePanel (Image đen toàn màn hình) vào đây. Nếu để trống code tự tìm trong Scene!")]
    public Image fadePanel;

    [Tooltip("(Tùy chọn) Text hướng dẫn 'Bấm phím bất kỳ để quay về Menu'")]
    public Text promptText;

    [Tooltip("(Tùy chọn nếu dùng TextMeshPro) TextMeshProUGUI hướng dẫn")]
    public TextMeshProUGUI promptTextTMP;

    [Header("2. Cấu Hình Chuyển Cảnh")]
    [Tooltip("Thời gian màn hình mờ đen hoàn toàn (giây)")]
    public float fadeToBlackDuration = 1.2f;

    [Tooltip("Khoảng dừng tối đen trước khi cho phép bấm (giây)")]
    public float delayBeforeCanClick = 0.5f;

    [Tooltip("Tên Scene Menu chính (Mặc định: MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    [Header("3. Âm Thanh (Tùy Chọn)")]
    [Tooltip("Âm thanh u ám lúc màn hình đen")]
    public AudioClip deathAmbienceSound;

    private bool isGameOverTriggered = false;
    private bool canClickToReturn = false;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        EnsureUIReferences();
    }

    void Start()
    {
        // Ẩn Text hướng dẫn lúc đầu
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (promptTextTMP != null) promptTextTMP.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!canClickToReturn) return;

        // Lắng nghe người chơi nhấp chuột hoặc bấm bất kỳ phím nào
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            canClickToReturn = false;
            StartCoroutine(ReturnToMenuRoutine());
        }
    }

    private void EnsureUIReferences()
    {
        if (fadePanel == null)
        {
            GameObject fadeObj = GameObject.Find("FadePanel");
            if (fadeObj == null) fadeObj = GameObject.Find("FadeScreen");
            if (fadeObj == null) fadeObj = GameObject.Find("FadeImage");
            if (fadeObj == null) fadeObj = GameObject.Find("BlackScreen");

            if (fadeObj != null)
            {
                fadePanel = fadeObj.GetComponent<Image>();
            }
        }

        // Nếu trong Scene vẫn chưa có FadePanel, tự động tạo 1 Canvas che đen
        if (fadePanel == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("GameOverCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 99999;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject newPanel = new GameObject("FadePanel");
            newPanel.transform.SetParent(canvas.transform, false);
            fadePanel = newPanel.AddComponent<Image>();
            fadePanel.color = new Color(0f, 0f, 0f, 0f);

            RectTransform rt = fadePanel.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            newPanel.SetActive(false);
        }
    }

    /// <summary>
    /// API: Kích hoạt chuỗi Game Over sau khi quái vật vồ xong
    /// </summary>
    public void TriggerGameOver(float customFadeDuration = -1f)
    {
        if (isGameOverTriggered) return;
        isGameOverTriggered = true;

        float duration = (customFadeDuration > 0f) ? customFadeDuration : fadeToBlackDuration;
        StartCoroutine(GameOverSequenceRoutine(duration));
    }

    private IEnumerator GameOverSequenceRoutine(float duration)
    {
        EnsureUIReferences();

        // 1. Khóa di chuyển Player
        MovePl player = Object.FindFirstObjectByType<MovePl>();
        if (player != null)
        {
            player.SetMovementState(false);
            player.isCameraLocked = true;
        }

        // 2. Mở Panel Đen & Bắt đầu Fade từ 0 lên 1
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;

            // Đảm bảo sorting order cao nhất đè lên tất cả HUD khác
            Canvas parentCanvas = fadePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentCanvas.overrideSorting = true;
                parentCanvas.sortingOrder = 99999;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                c.a = t;
                fadePanel.color = c;

                yield return null;
            }

            c.a = 1f;
            fadePanel.color = c;
        }

        // 3. Phát âm thanh u ám lúc chết (nếu có)
        if (deathAmbienceSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathAmbienceSound);
        }

        // 4. Mở khóa chuột để người chơi tương tác
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 5. Đợi 1 nhịp ngắn
        yield return new WaitForSeconds(delayBeforeCanClick);

        // 6. Hiện dòng chữ gợi ý nhấp chuột
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "NHẤP CHUỘT BẤT KỲ ĐỂ VỀ MENU...";
        }
        if (promptTextTMP != null)
        {
            promptTextTMP.gameObject.SetActive(true);
            promptTextTMP.text = "NHẤP CHUỘT BẤT KỲ ĐỂ VỀ MENU...";
        }

        // Bật cờ cho phép bấm
        canClickToReturn = true;
        Debug.Log("[GameOverJumpscareManager] 🌑 Màn hình đen hoàn tất. Nhấp chuột hoặc phím bất kỳ để quay về Menu!");
    }

    private IEnumerator ReturnToMenuRoutine()
    {
        Debug.Log($"[GameOverJumpscareManager] 🚪 Đang chuyển về Scene: {mainMenuSceneName}...");

        // Khôi phục timeScale đề phòng pause
        Time.timeScale = 1f;

        // Nếu SceneLoader tồn tại, dùng SceneLoader để load mượt
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        yield return null;
    }
}
