using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Trình điều khiển Cắt Cảnh Mở Màn Map 03 (Intro Sequence)
/// Quản lý mở màn Fade In từ màn hình đen, khôi phục đèn pin/camcorder,
/// và phụ đề thoại mở đầu cho người chơi.
/// </summary>
public class Map03IntroSequence : MonoBehaviour
{
    public static Map03IntroSequence Instance { get; private set; }
    public static bool isCutsceneRunning { get; private set; } = false;

    [Header("1. Tham Chiếu Player & UI Phụ Đề (References)")]
    [Tooltip("Kéo GameObject Main (chứa MovePl) vào đây (Tự tìm nếu để trống)")]
    public MovePl playerMain;

    [Tooltip("Kéo TextMeshProUGUI (Subtitle Text) vào đây (Tự tìm nếu để trống)")]
    public TextMeshProUGUI subtitleTextUI;

    [Tooltip("Kéo FadePanel (Image đen) vào đây (Tự tìm nếu để trống)")]
    public Image fadeScreenImage;

    [Header("2. Mở Màn & Khoảng Lặng (Phase 1 - Fade & Silence)")]
    [Tooltip("Thời gian giữ đen ngắn lúc nạp map trước khi mở sáng (giây - Mặc định: 0.5s)")]
    public float initialBlackDuration = 0.5f;

    [Tooltip("Thời gian màn hình từ đen xì sáng dần lên rõ cảnh (giây - Mặc định: 1.5s)")]
    public float fadeInDuration = 1.5f;

    [Tooltip("Khoảng lặng sau khi fade sáng trước khi vào game (giây - Mặc định: 1.0s)")]
    public float silenceDurationAfterFade = 1.0f;

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "Khu rừng này... u ám quá.";
        [TextArea(2, 4)]
        public string englishDialogue = "This forest... is so eerie.";
        public float holdDuration = 2.0f;
    }

    [Header("3. Cấu Hình Phụ Đề Thoại Player (Phase 2 - Subtitle)")]
    public DialogueLine[] introDialogues = new DialogueLine[0];

    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.035f;
    public bool showBlinkingCursor = true;
    public bool useFadeEffect = true;
    public float fadeTextDuration = 0.2f;

    [Tooltip("Âm thanh gõ chữ thoại (Loop trong lúc gõ - Để trống nếu không dùng)")]
    public AudioClip dialogueBlipSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    // --- Private Fields ---
    private AudioSource audioSource;
    private CharacterController characterController;

    // --- Biến điều khiển Click chuột qua thoại nhanh ---
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        if (audioSource.isPlaying) audioSource.Stop();

        // 1. Tìm và bật đen màn hình ngay từ Awake (không lộ hình ảnh ban đầu)
        EnsureFadeScreenImage();
        if (fadeScreenImage != null)
        {
            EnsureParentsActive(fadeScreenImage);
            fadeScreenImage.gameObject.SetActive(true);
            fadeScreenImage.color = Color.black;
            fadeScreenImage.raycastTarget = true;
        }

        // 2. Khóa chuột ngay từ đầu
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. Ẩn toàn bộ UI máy quay & tâm ngắm
        HideAllCamUIAndEquipment();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerMain == null) playerMain = Object.FindFirstObjectByType<MovePl>();
        if (playerMain != null)
        {
            characterController = playerMain.GetComponent<CharacterController>();
            playerMain.LockCursor();
        }

        if (subtitleTextUI == null) subtitleTextUI = FindSubtitleTextUI();
        if (fadeScreenImage == null) EnsureFadeScreenImage();

        HideAllCamUIAndEquipment();

        StartCoroutine(IntroSequenceRoutine());
    }

    void Update()
    {
        if (!isCutsceneRunning) return;

        // Bấm chuột trái, Space hoặc E để qua thoại nhanh
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                isTyping = false;
                if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;
                if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueBlipSound)
                {
                    audioSource.Stop();
                }
            }
            else if (isWaitingForNextLine)
            {
                skipRequested = true;
            }
        }
    }

    void HideAllCamUIAndEquipment()
    {
        FlashlightToggle flashlight = Object.FindFirstObjectByType<FlashlightToggle>(FindObjectsInactive.Include);
        if (flashlight != null)
        {
            flashlight.hasFlashlight = true;
            flashlight.SetFlashlightState(false, false);
        }

        CamcorderUI[] camUIs = Object.FindObjectsByType<CamcorderUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in camUIs)
        {
            c.gameObject.SetActive(false);
        }

        InteractPro interactPro = Object.FindFirstObjectByType<InteractPro>(FindObjectsInactive.Include);
        if (interactPro != null)
        {
            if (interactPro.dotObject != null) interactPro.dotObject.SetActive(false);
            if (interactPro.handObject != null) interactPro.handObject.SetActive(false);
            if (interactPro.interactionUI != null) interactPro.interactionUI.SetActive(false);
        }
    }

    IEnumerator IntroSequenceRoutine()
    {
        isCutsceneRunning = true;
        Debug.Log("[Map03IntroSequence] 🌲 BẮT ĐẦU MỞ MÀN MAP 03!");

        // 1. Khóa di chuyển & Camera lúc mở màn
        if (playerMain != null)
        {
            playerMain.isCameraLocked = true;
            playerMain.SetMovementState(false);
            playerMain.enabled = false;
        }
        if (characterController != null) characterController.enabled = false;

        PauseMenuManager.SetInGameHUDActive(false);

        EnsureFadeScreenImage();
        if (fadeScreenImage != null)
        {
            EnsureParentsActive(fadeScreenImage);
            fadeScreenImage.gameObject.SetActive(true);
            fadeScreenImage.color = Color.black;
            fadeScreenImage.raycastTarget = true;
        }

        yield return new WaitForSeconds(initialBlackDuration);

        // 2. Màn hình từ đen xì sáng dần lên rõ cảnh
        yield return StartCoroutine(FadeScreenInRoutine(fadeInDuration));

        // 3. Mở khóa Player & Trả lại UI
        PauseMenuManager.SetInGameHUDActive(true);

        if (playerMain != null)
        {
            playerMain.enabled = true;
            playerMain.isCameraLocked = false;
            playerMain.SetMovementState(true);
            playerMain.LockCursor();
        }
        if (characterController != null) characterController.enabled = true;

        InteractPro interactPro = Object.FindFirstObjectByType<InteractPro>();
        if (interactPro != null && interactPro.dotObject != null)
        {
            interactPro.dotObject.SetActive(true);
        }

        // Khôi phục Đèn pin & Camcorder
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

        FlashlightToggle ft = FlashlightToggle.Instance ?? Object.FindFirstObjectByType<FlashlightToggle>(FindObjectsInactive.Include);
        if (ft != null)
        {
            ft.gameObject.SetActive(true);
            ft.hasFlashlight = true;
            ft.RestoreFlashlightState();
            ft.UpdateUI();
            if (ft.currentBattery > 0f)
            {
                ft.SetFlashlightState(true, false);
            }
        }

        if (silenceDurationAfterFade > 0f)
        {
            yield return new WaitForSeconds(silenceDurationAfterFade);
        }

        // 4. Phát thoại mở đầu (nếu có)
        if (introDialogues != null && introDialogues.Length > 0)
        {
            foreach (var line in introDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleDialogueLineRoutine(line));
                }
            }
        }

        isCutsceneRunning = false;
        Debug.Log("[Map03IntroSequence] 🎮 CẮT CẢNH MAP 03 HOÀN TẤT!");
    }

    IEnumerator FadeScreenInRoutine(float duration)
    {
        if (fadeScreenImage != null && duration > 0f)
        {
            float fadeElapsed = 0f;
            while (fadeElapsed < duration)
            {
                fadeElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(fadeElapsed / duration);
                float a = Mathf.SmoothStep(1.0f, 0.0f, t);
                fadeScreenImage.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }
            fadeScreenImage.color = new Color(0f, 0f, 0f, 0f);
            fadeScreenImage.raycastTarget = false;
            fadeScreenImage.gameObject.SetActive(false);
        }
    }

    IEnumerator PlaySingleDialogueLineRoutine(DialogueLine line)
    {
        if (line == null) yield break;

        if (subtitleTextUI == null) subtitleTextUI = FindSubtitleTextUI();
        if (subtitleTextUI == null) yield break;

        EnsureParentsActive(subtitleTextUI);
        subtitleTextUI.gameObject.SetActive(true);
        Color sc = subtitleTextUI.color;
        sc.a = 1f;
        subtitleTextUI.color = sc;

        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        string lang = SettingsManager.currentLanguage;
        currentFullText = (lang == "VI") ? line.vietnameseDialogue : line.englishDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.vietnameseDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.englishDialogue;
        if (string.IsNullOrEmpty(currentFullText)) yield break;

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        skipRequested = false;

        if (dialogueBlipSound != null && audioSource != null)
        {
            audioSource.clip = dialogueBlipSound;
            audioSource.volume = dialogueVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        if (useTypewriterEffect)
        {
            isTyping = true;
            subtitleTextUI.text = "";

            for (int i = 0; i <= currentFullText.Length; i++)
            {
                if (!isTyping) break;
                string typed = currentFullText.Substring(0, i);
                if (showBlinkingCursor) typed += " _";
                subtitleTextUI.text = typed;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            subtitleTextUI.text = currentFullText;
            isTyping = false;
        }
        else
        {
            subtitleTextUI.text = currentFullText;
        }

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueBlipSound)
        {
            audioSource.Stop();
        }

        if (showBlinkingCursor && subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

        isWaitingForNextLine = true;
        float waitTimer = 0f;
        while (waitTimer < line.holdDuration && !skipRequested)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }
        isWaitingForNextLine = false;

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        if (useFadeEffect)
        {
            yield return StartCoroutine(FadeTextOutRoutine(subtitleTextUI, fadeTextDuration));
        }
        else
        {
            subtitleTextUI.text = "";
        }

        subtitleTextUI.gameObject.SetActive(false);
        SmartInteractionDialogue.isAnyDialoguePlaying = false;
    }

    IEnumerator BlinkCursorRoutine(TextMeshProUGUI txt, string baseText)
    {
        bool show = true;
        while (true)
        {
            if (txt != null)
            {
                txt.text = baseText + (show ? " _" : "");
            }
            show = !show;
            yield return new WaitForSeconds(0.4f);
        }
    }

    IEnumerator FadeTextOutRoutine(TextMeshProUGUI txt, float duration)
    {
        if (txt == null || duration <= 0f) yield break;
        float elapsed = 0f;
        Color c = txt.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            txt.color = c;
            yield return null;
        }
        c.a = 0f;
        txt.color = c;
    }

    private TextMeshProUGUI FindSubtitleTextUI()
    {
        GameObject subObj = GameObject.Find("SubtitleText") ?? GameObject.Find("Subtitle_Text");
        if (subObj != null)
        {
            TextMeshProUGUI tmp = subObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }

        TextMeshProUGUI[] tmps = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in tmps)
        {
            if (t.name.ToLower().Contains("sub") || t.name.ToLower().Contains("dialogue") || t.name.ToLower().Contains("thoai"))
            {
                return t;
            }
        }
        return null;
    }

    private void EnsureFadeScreenImage()
    {
        if (fadeScreenImage != null) return;
        GameObject panelObj = GameObject.Find("FadePanel") ?? GameObject.Find("BlackScreenPanel");
        if (panelObj != null)
        {
            fadeScreenImage = panelObj.GetComponent<Image>();
            if (fadeScreenImage != null) return;
        }

        Image[] imgs = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var img in imgs)
        {
            if (img.name.ToLower().Contains("fade") || img.name.ToLower().Contains("black"))
            {
                fadeScreenImage = img;
                return;
            }
        }
    }

    private void EnsureParentsActive(Component comp)
    {
        if (comp == null) return;
        Transform curr = comp.transform.parent;
        while (curr != null)
        {
            if (!curr.gameObject.activeSelf)
            {
                curr.gameObject.SetActive(true);
            }
            curr = curr.parent;
        }
    }
}
