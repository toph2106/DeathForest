using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }
    public static bool isPaused = false;

    [Header("1. Các Panel Giao Diện (UI Panels)")]
    [Tooltip("Kéo PauseGame (Panel chứa 4 nút Resume, Settings, Title, Quit) vào đây")]
    public GameObject pauseMenuPanel;

    [Tooltip("Kéo Background (Tấm nền tối mờ đằng sau) vào đây")]
    public GameObject backgroundDim;

    [Tooltip("Kéo SettingsPanel vào đây (nếu muốn mở Cài đặt trong Pause Menu)")]
    public GameObject settingsPanel;

    [Tooltip("Kéo FadePanel (Image đen) vào đây để chuyển cảnh mượt")]
    public Image fadePanel;

    [Header("2. Global Volume (Hiệu ứng khi Pause)")]
    [Tooltip("Kéo Global Volume trong Pause vào đây để bật nhói màu khi Pause")]
    public Volume pauseVolume;

    [Header("3. Ẩn UI Khác Khi Pause (Camcorder, Inventory, HUD...)")]
    [Tooltip("Kéo các UI trong game (như Camcorder HUD, Thanh đồ Inventory, Tâm ngắm) vào đây để tự động ẩn khi Pause và hiện lại khi Resume")]
    public GameObject[] uiToHideOnPause;

    [Header("4. Hiệu Ứng Sáng Dần Khi Mới Vào Map (Scene Fade-In)")]
    [Tooltip("Màn hình tự động đen ngòm rồi mờ sáng dần khi vừa nạp Map thành công")]
    public bool fadeInOnSceneStart = true;
    [Tooltip("Khoảng thời gian giữ đen ngòm lúc vừa nạp Map (giây) - Chỉnh chậm cinematic")]
    public float initialBlackPause = 1.0f;
    [Tooltip("Tốc độ từ đen mở sáng dần ra (giây) - Mặc định: 3.5s cho mở mượt sâu hơn")]
    public float fadeInDuration = 3.5f;

    [Header("5. Chuyển Cảnh (Transition)")]
    [Tooltip("Thời gian mờ đen khi quay về MainMenu (giây)")]
    public float fadeDuration = 1.0f;
    [Tooltip("Tên Scene MainMenu (Mặc định: MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    private CanvasGroup pauseCanvasGroup;
    private CanvasGroup settingsCanvasGroup;
    private CanvasGroup dimCanvasGroup;
    private RectTransform pauseRect;
    private RectTransform settingsRect;

    private Vector2 pauseOriginalPos;
    private Vector2 settingsOriginalPos;
    private bool isTransitioning = false;
    private List<GameObject> previouslyActiveUI = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupComponents();
    }

    void SetupComponents()
    {
        if (pauseMenuPanel != null)
        {
            pauseCanvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
            if (pauseCanvasGroup == null) pauseCanvasGroup = pauseMenuPanel.AddComponent<CanvasGroup>();
            pauseRect = pauseMenuPanel.GetComponent<RectTransform>();
            if (pauseRect != null) pauseOriginalPos = pauseRect.anchoredPosition;
        }

        if (settingsPanel != null)
        {
            settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (settingsCanvasGroup == null) settingsCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
            settingsRect = settingsPanel.GetComponent<RectTransform>();
            if (settingsRect != null) settingsOriginalPos = settingsRect.anchoredPosition;
        }

        if (backgroundDim != null)
        {
            dimCanvasGroup = backgroundDim.GetComponent<CanvasGroup>();
            if (dimCanvasGroup == null) dimCanvasGroup = backgroundDim.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        HideAllImmediate();

        // Ép phát tín hiệu đồng bộ ngôn ngữ đã lưu ngay từ frame đầu tiên của Map
        SettingsManager.RefreshLanguageState();

        if (fadeInOnSceneStart)
        {
            StartCoroutine(FadeInSceneSequence());
        }
    }

    IEnumerator FadeInSceneSequence()
    {
        if (fadePanel != null)
        {
            isTransitioning = true; // Vô hiệu hóa nút bấm & phím ESC khi đang mờ cảnh
            fadePanel.transform.SetAsLastSibling();
            fadePanel.gameObject.SetActive(true);
            fadePanel.color = new Color(0, 0, 0, 1f); // Bắt đầu bằng màn đen 100%
            fadePanel.raycastTarget = true; // Chặn toàn bộ cú click chuột của người chơi

            yield return new WaitForSecondsRealtime(initialBlackPause);

            // Mở mờ sáng dần ra với Ease.InCubic
            fadePanel.DOFade(0f, fadeInDuration).SetEase(Ease.InCubic).SetUpdate(true).OnComplete(() =>
            {
                fadePanel.raycastTarget = false;
                fadePanel.gameObject.SetActive(false);
                isTransitioning = false; // Mở lại cho phép tương tác bình thường
            });
        }
    }

    void Update()
    {
        // Bắt phím ESC hoặc P để Pause / Resume game
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (isTransitioning) return;

            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (isTransitioning) return;

        isPaused = true;
        Time.timeScale = 0f; // Tạm dừng thời gian

        // Mở con trỏ chuột cho người chơi thao tác UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ẩn danh sách các UI ingame (Camcorder, Inventory...)
        previouslyActiveUI.Clear();
        if (uiToHideOnPause != null)
        {
            foreach (var ui in uiToHideOnPause)
            {
                if (ui != null && ui.activeSelf)
                {
                    previouslyActiveUI.Add(ui);
                    ui.SetActive(false);
                }
            }
        }

        // Bật Volume Pause
        if (pauseVolume != null)
        {
            pauseVolume.priority = 10;
            pauseVolume.gameObject.SetActive(true);
        }

        // Bật Background Dim mờ dần
        if (backgroundDim != null)
        {
            backgroundDim.SetActive(true);
            if (dimCanvasGroup != null)
            {
                dimCanvasGroup.alpha = 0f;
                dimCanvasGroup.DOFade(0.75f, 0.2f).SetUpdate(true);
            }
        }

        // Bật Pause Menu trôi lên mượt mà
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            if (pauseCanvasGroup != null) pauseCanvasGroup.alpha = 0f;
            if (pauseRect != null) pauseRect.anchoredPosition = pauseOriginalPos + new Vector2(0, -30f);

            if (pauseCanvasGroup != null) pauseCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
            if (pauseRect != null) pauseRect.DOAnchorPos(pauseOriginalPos, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        // KHÔI PHỤC THỜI GIAN VÀ CON TRỎ CHUỘT NGAY LẬP TỨC
        isPaused = false;
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Hiện lại các UI ingame trước đó
        if (previouslyActiveUI != null)
        {
            foreach (var ui in previouslyActiveUI)
            {
                if (ui != null) ui.SetActive(true);
            }
            previouslyActiveUI.Clear();
        }

        StartCoroutine(AnimateResume());
    }

    IEnumerator AnimateResume()
    {
        isTransitioning = true;

        if (pauseCanvasGroup != null) pauseCanvasGroup.DOFade(0f, 0.18f).SetUpdate(true);
        if (dimCanvasGroup != null) dimCanvasGroup.DOFade(0f, 0.18f).SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.18f);

        HideAllImmediate();

        isTransitioning = false;
    }

    public void OpenSettings()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isTransitioning) return;
        StartCoroutine(AnimateOpenSettings());
    }

    public void CloseSettings()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isTransitioning) return;
        StartCoroutine(AnimateCloseSettings());
    }

    IEnumerator AnimateOpenSettings()
    {
        isTransitioning = true;

        if (pauseCanvasGroup != null) pauseCanvasGroup.DOFade(0f, 0.15f).SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.15f);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            if (settingsCanvasGroup != null) settingsCanvasGroup.alpha = 0f;
            if (settingsRect != null) settingsRect.anchoredPosition = settingsOriginalPos + new Vector2(0, -30f);

            if (settingsCanvasGroup != null) settingsCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
            if (settingsRect != null) settingsRect.DOAnchorPos(settingsOriginalPos, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.25f);

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        isTransitioning = false;
    }

    IEnumerator AnimateCloseSettings()
    {
        isTransitioning = true;

        if (settingsCanvasGroup != null) settingsCanvasGroup.DOFade(0f, 0.15f).SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.15f);

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            if (pauseCanvasGroup != null) pauseCanvasGroup.alpha = 0f;
            if (pauseRect != null) pauseRect.anchoredPosition = pauseOriginalPos + new Vector2(0, -30f);

            if (pauseCanvasGroup != null) pauseCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
            if (pauseRect != null) pauseRect.DOAnchorPos(pauseOriginalPos, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.25f);

        if (settingsPanel != null) settingsPanel.SetActive(false);
        isTransitioning = false;
    }

    public void ReturnToTitle()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeAndLoadTitle());
    }

    IEnumerator FadeAndLoadTitle()
    {
        isTransitioning = true;
        Time.timeScale = 1f;

        // RESET TOÀN BỘ TRẠNG THÁI MÁY QUAY VÀ CẮT CẢNH KHI VỀ TITLE
        CameraObjectPickup.isComputerCutsceneFinished = false;
        InWorldComputerCutscene.isUsingComputer = false;

        // Reset bộ đếm và ẩn hoàn toàn CamcorderUI
        CamcorderUI.ResetTimer();

        SimpleCameraOverlay overlay = FindFirstObjectByType<SimpleCameraOverlay>();
        if (overlay != null)
        {
            overlay.ResetCameraView();
        }

        if (fadePanel != null)
        {
            fadePanel.transform.SetAsLastSibling();
            fadePanel.gameObject.SetActive(true);
            fadePanel.color = new Color(0, 0, 0, 0);
            fadePanel.raycastTarget = true;

            fadePanel.DOFade(1f, fadeDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(fadeDuration);
        }

        isPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[PauseMenuManager] Quit Game!");
        Application.Quit();
    }

    void HideAllImmediate()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (backgroundDim != null) backgroundDim.SetActive(false);
        if (pauseVolume != null) pauseVolume.gameObject.SetActive(false);
        if (fadePanel != null) fadePanel.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        isPaused = false;
    }
}
