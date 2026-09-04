using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Quản lý chuyển cảnh Game Over Điện Ảnh (Cinematic Death Screen):
/// 1. Fade Out màn hình sang Đen (Dedicated Canvas che 100% toàn màn hình).
/// 2. Khi đen hoàn toàn: Tắt quái in-camera, BẬT ảnh tử nạn (EndG / Image).
/// 3. Fade In mở dần màn hình để hé lộ bức ảnh tử nạn End.
/// 4. Chờ người chơi nhấp chuột hoặc bấm phím bất kỳ (Khóa phím bấm nhầm / giữ phím lúc chạy).
/// 5. Fade Out sang Đen lần 2 để kết thúc và chuyển mượt về MainMenu.
/// </summary>
public class GameOverJumpscareManager : MonoBehaviour
{
    private static GameOverJumpscareManager _instance;
    public static GameOverJumpscareManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<GameOverJumpscareManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameOverJumpscareManager");
                    _instance = go.AddComponent<GameOverJumpscareManager>();
                }
            }
            return _instance;
        }
    }

    [Header("1. UI Tử Nạn & Màn Hình Fade")]
    [Tooltip("Kéo GameObject 'EndG' hoặc 'Image' (trong Canvas UI > EndG) vào đây")]
    public GameObject endScreenObject;

    [Tooltip("Kéo Sprite 'End' (trong Assets/UI/End) vào đây làm ảnh tử nạn dự phòng")]
    public Sprite endScreenSprite;

    [Tooltip("Kéo FadePanel vào đây (Nếu để trống code tự tạo Canvas Fade 100% riêng biệt)")]
    public Image fadePanel;

    [Header("2. Thời Gian Đóng Mở Fade (Cinematic Timings)")]
    [Tooltip("Thời gian màn hình tối đen dần sau jumpscare (giây)")]
    public float fadeOutToBlackDuration = 1.2f;

    [Tooltip("Thời gian mở màn hình hé lộ bức ảnh End (giây)")]
    public float fadeInToDeathScreenDuration = 1.0f;

    [Tooltip("Thời gian tối đen lại sau khi bấm phím trước khi load Menu (giây)")]
    public float fadeOutToMenuDuration = 0.8f;

    [Tooltip("Tên Scene Menu chính (Mặc định: MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    [Header("3. Âm Thanh Tử Nạn (Tùy Chọn)")]
    [Tooltip("Âm thanh u ám lúc hiện ảnh chết chóc")]
    public AudioClip deathAmbienceSound;

    private bool isGameOverTriggered = false;
    private AudioSource audioSource;
    private Canvas dedicatedFadeCanvas;

    void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        EnsureDedicatedFadeCanvas();
        EnsureUIReferences();
    }

    void Start()
    {
        EnsureDedicatedFadeCanvas();
        EnsureUIReferences();
        if (endScreenObject != null)
        {
            endScreenObject.SetActive(false);
        }
    }

    public void EnsureDedicatedFadeCanvas()
    {
        if (dedicatedFadeCanvas == null)
        {
            GameObject canvasObj = GameObject.Find("DedicatedGameOverFadeCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("DedicatedGameOverFadeCanvas");
            }
            dedicatedFadeCanvas = canvasObj.GetComponent<Canvas>();
            if (dedicatedFadeCanvas == null) dedicatedFadeCanvas = canvasObj.AddComponent<Canvas>();
            dedicatedFadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dedicatedFadeCanvas.overrideSorting = true;
            dedicatedFadeCanvas.sortingOrder = 999999; // Lớp cao nhất tuyệt đối

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GraphicRaycaster gr = canvasObj.GetComponent<GraphicRaycaster>();
            if (gr == null) gr = canvasObj.AddComponent<GraphicRaycaster>();

            Transform existingPanel = canvasObj.transform.Find("DedicatedFadePanel");
            if (existingPanel != null)
            {
                fadePanel = existingPanel.GetComponent<Image>();
            }
            else
            {
                GameObject panelObj = new GameObject("DedicatedFadePanel");
                panelObj.transform.SetParent(canvasObj.transform, false);
                fadePanel = panelObj.AddComponent<Image>();
                fadePanel.color = new Color(0f, 0f, 0f, 0f);
                fadePanel.raycastTarget = false;

                RectTransform rt = fadePanel.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }

    public void EnsureUIReferences()
    {
        // 1. Tìm GameObject EndG trong Scene nếu chưa được gán
        if (endScreenObject == null)
        {
            GameObject[] allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjs)
            {
                if (obj != null && (obj.name == "EndG" || obj.name == "EndScreen" || obj.name == "GameOverScreen"))
                {
                    endScreenObject = obj;
                    break;
                }
            }
        }

        // 2. Tìm Sprite End nếu chưa có
        if (endScreenSprite == null)
        {
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in allSprites)
            {
                if (s != null && s.name == "End")
                {
                    endScreenSprite = s;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// API: Kích hoạt chuỗi Game Over toàn diện (cho cả Yoshie, Chó, Stranger, Uma, Gari...)
    /// </summary>
    public void TriggerGameOverDeathScreen(GameObject inCamMonster = null, float customFadeOutTime = -1f, string customMenuScene = null, GameObject customEndScreen = null, Sprite customEndSprite = null)
    {
        if (isGameOverTriggered) return;
        isGameOverTriggered = true;

        if (customEndScreen != null) endScreenObject = customEndScreen;
        if (customEndSprite != null) endScreenSprite = customEndSprite;

        float fOutTime = (customFadeOutTime > 0f) ? customFadeOutTime : fadeOutToBlackDuration;
        string mScene = !string.IsNullOrEmpty(customMenuScene) ? customMenuScene : mainMenuSceneName;

        StartCoroutine(PlayGameOverDeathScreenRoutine(inCamMonster, fOutTime, fadeInToDeathScreenDuration, fadeOutToMenuDuration, mScene));
    }

    public void TriggerGameOver(float customFadeDuration = -1f)
    {
        TriggerGameOverDeathScreen(null, customFadeDuration);
    }

    public static void MuteAllUnrelatedAudioAndHideUI(AudioSource audioToKeep = null)
    {
        AudioSource[] allAudio = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        if (allAudio != null)
        {
            foreach (var a in allAudio)
            {
                if (a == null) continue;
                if (audioToKeep != null && a == audioToKeep) continue;
                if (_instance != null && a == _instance.audioSource) continue;

                try
                {
                    a.Stop();
                }
                catch { }
            }
        }

        string[] uiNamesToDisable = new string[] 
        { 
            "ItemUI", "InventoryPanel", "Inventory", "Camcorder", "CameraOverlayCanvas", 
            "Interact", "Crosshair", "Subtitle", "SubtitleText", "ReadNote", 
            "Note", "Pause", "Tutorial", "StaminaUI", "HealthUI"
        };

        foreach (string name in uiNamesToDisable)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                if (_instance != null && _instance.fadePanel != null)
                {
                    if (obj == _instance.fadePanel.gameObject || _instance.fadePanel.transform.IsChildOf(obj.transform))
                    {
                        continue;
                    }
                }
                if (_instance != null && _instance.endScreenObject != null)
                {
                    if (obj == _instance.endScreenObject || _instance.endScreenObject.transform.IsChildOf(obj.transform))
                    {
                        continue;
                    }
                }
                obj.SetActive(false);
            }
        }
    }

    public IEnumerator PlayGameOverDeathScreenRoutine(GameObject inCamMonsterToHide, float fadeToBlackTime, float fadeInEndTime, float fadeOutToMenuTime, string menuScene)
    {
        EnsureDedicatedFadeCanvas();
        EnsureUIReferences();
        MuteAllUnrelatedAudioAndHideUI(audioSource);

        MovePl player = Object.FindFirstObjectByType<MovePl>();
        if (player != null)
        {
            player.SetMovementState(false);
            player.isCameraLocked = true;
        }

        // Đảm bảo FadePanel active và render ở layer cao nhất
        if (dedicatedFadeCanvas != null) dedicatedFadeCanvas.gameObject.SetActive(true);
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            fadePanel.color = new Color(0f, 0f, 0f, 0f);
        }

        // ==================== GIAI ĐOẠN 1: FADE OUT SANG ĐEN (0 -> 1) ====================
        float elapsed = 0f;
        while (elapsed < fadeToBlackTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeToBlackTime);
            if (fadePanel != null)
            {
                fadePanel.color = new Color(0f, 0f, 0f, t);
            }
            yield return null;
        }

        if (fadePanel != null) fadePanel.color = Color.black;

        // TẮT QUÁI IN-CAMERA (khi màn hình đã đen 100%)
        if (inCamMonsterToHide != null)
        {
            inCamMonsterToHide.SetActive(false);
        }

        // BẬT BỨC ẢNH TỬ NẠN EndG
        ShowDeathScreenImage();

        // Phát âm thanh tử nạn nếu có
        if (deathAmbienceSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathAmbienceSound);
        }

        // ==================== GIAI ĐOẠN 2: FADE IN MỞ MÀN HÌNH (1 -> 0) HÉ LỘ ẢNH EndG ====================
        elapsed = 0f;
        while (elapsed < fadeInEndTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInEndTime);
            if (fadePanel != null)
            {
                fadePanel.color = new Color(0f, 0f, 0f, 1f - t);
            }
            yield return null;
        }

        if (fadePanel != null) fadePanel.color = new Color(0f, 0f, 0f, 0f);

        // MỞ KHÓA CHUỘT
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ==================== GIAI ĐOẠN 3: ĐỢI NGƯỜI CHƠI NHẤP CHUỘT VÀO BẤT KỲ CHỖ NÀO TRÊN MÀN HÌNH ====================
        // 1. Xả sạch nút chuột nếu đang bị đè
        while (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            yield return null;
        }

        // 2. Chờ nhịp ngắn 0.3s để người chơi ổn định thao tác
        yield return new WaitForSeconds(0.3f);

        // 3. Chờ cú NHẤP CHUỘT bất kỳ vào màn hình (Chuột trái, Chuột phải hoặc Chuột giữa)
        while (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
        {
            yield return null;
        }

        // ==================== GIAI ĐOẠN 4: FADE OUT ĐÓNG MÀN HÌNH ĐEN LẦN 2 (0 -> 1) ====================
        elapsed = 0f;
        while (elapsed < fadeOutToMenuTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutToMenuTime);
            if (fadePanel != null)
            {
                fadePanel.color = new Color(0f, 0f, 0f, t);
            }
            yield return null;
        }

        if (fadePanel != null) fadePanel.color = Color.black;

        // CHUYỂN MƯỢT VỀ MENU CHÍNH
        Time.timeScale = 1f;
        GameSaveManager.ResetAllGameplayRuntimeData();

        string sceneToLoad = !string.IsNullOrEmpty(menuScene) ? menuScene : mainMenuSceneName;
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync(sceneToLoad);
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void ShowDeathScreenImage()
    {
        bool showedEndG = false;

        if (endScreenObject != null)
        {
            // Bật toàn bộ cha mẹ của EndG (Canvas UI)
            Transform cur = endScreenObject.transform;
            while (cur != null)
            {
                cur.gameObject.SetActive(true);
                Canvas c = cur.GetComponent<Canvas>();
                if (c != null)
                {
                    c.enabled = true;
                    c.overrideSorting = true;
                    c.sortingOrder = 999990; // Dưới FadePanel (999999) nhưng trên toàn bộ HUD khác
                }
                cur = cur.parent;
            }

            endScreenObject.SetActive(true);

            // Bật toàn bộ children & Image trong EndG
            foreach (Transform child in endScreenObject.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.SetActive(true);
            }

            foreach (var img in endScreenObject.GetComponentsInChildren<Image>(true))
            {
                img.gameObject.SetActive(true);
                img.enabled = true;
                img.color = Color.white;
            }

            showedEndG = true;
        }

        // Nếu EndG chưa có hoặc không hiển thị, tạo 1 Canvas hiển thị Sprite End trực tiếp
        if (!showedEndG || endScreenSprite != null)
        {
            GameObject dynCanvas = GameObject.Find("DynamicDeathScreenCanvas");
            if (dynCanvas == null)
            {
                dynCanvas = new GameObject("DynamicDeathScreenCanvas");
                Canvas dCanvas = dynCanvas.AddComponent<Canvas>();
                dCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                dCanvas.overrideSorting = true;
                dCanvas.sortingOrder = 999990; // Nằm dưới FadePanel (999999)

                CanvasScaler scaler = dynCanvas.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                GameObject imgObj = new GameObject("DeathImage");
                imgObj.transform.SetParent(dynCanvas.transform, false);
                Image dImg = imgObj.AddComponent<Image>();

                if (endScreenSprite != null)
                {
                    dImg.sprite = endScreenSprite;
                }
                else
                {
                    Sprite s = Resources.Load<Sprite>("UI/End");
                    if (s != null) dImg.sprite = s;
                }

                dImg.color = Color.white;
                RectTransform rt = dImg.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            dynCanvas.SetActive(true);
        }
    }
}
