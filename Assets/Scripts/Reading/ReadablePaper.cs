using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ReadablePaper : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "";
        [TextArea(2, 4)]
        public string englishDialogue = "";
        public float holdDuration = 3.0f;
    }

    [Header("1. Cấu Hình Fade Màn Hình (Screen Fade Settings)")]
    [Tooltip("Kéo Fade Image đen vào đây (Nếu để trống script sẽ tự tìm từ Canvas/Fade)")]
    public Image fadeScreenImage;
    public float delayBeforeFade = 0.1f;
    public float fadeOutDuration = 0.8f;
    public float holdBlackDuration = 0.5f;
    public float fadeInDuration = 0.8f;

    [Header("2. Âm Thanh Lật Giấy Khi Đọc (Paper Audio)")]
    [Tooltip("Âm thanh tiếng sột soạt lật mở giấy")]
    public AudioClip paperRustleSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("3. Danh Sách Câu Thoại Tóm Tắt Sau Khi Đọc (Summary Dialogues)")]
    public DialogueLine[] summaryDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Nội dung mấy tờ giấy này nói về những nghi lễ hiến tế cổ xưa trong khu đền...",
            englishDialogue = "These papers describe ancient sacrificial rituals performed in the shrine...",
            holdDuration = 3.5f
        },
        new DialogueLine
        {
            vietnameseDialogue = "Hình như có thứ gì đó vô cùng tồi tệ đã thức giấc trong khu rừng này...",
            englishDialogue = "It seems something truly horrific has awakened in this forest...",
            holdDuration = 3.5f
        }
    };

    [Header("4. Cấu Hình Gõ Chữ Typewriter (Phụ Đề Điện Ảnh)")]
    public TextMeshProUGUI subtitleTextUI;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.03f;
    public float holdTimePerLine = 3.2f;
    public bool showBlinkingCursor = true;
    public bool useFadeEffect = true;
    public float textFadeDuration = 0.2f;
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    [Header("5. Kích Hoạt Sự Kiện Tiếp Theo (Tùy Chọn)")]
    public GameObject nextTriggerToActivate;

    public static bool HasReadPaper
    {
        get { return PlayerPrefs.GetInt("HasReadPaper_Map02", 0) == 1; }
        set
        {
            PlayerPrefs.SetInt("HasReadPaper_Map02", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private AudioSource audioSource;
    private bool isReading = false;
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        HasReadPaper = false;

        if (fadeScreenImage == null)
        {
            EnsureFadeImageExists();
        }

        if (subtitleTextUI == null)
        {
            subtitleTextUI = FindSubtitleTextUI();
        }
    }

    void Update()
    {
        if (!isReading) return;

        // Bấm Chuột Trái hoặc Space để qua nhanh câu thoại
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                isTyping = false;
                if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;
            }
            else if (isWaitingForNextLine)
            {
                skipRequested = true;
            }
        }
    }

    // =========================================================================
    // NGƯỜI CHƠI TƯƠNG TÁC ĐỌC GIẤY (CLICK CHUỘT TRÁI)
    // =========================================================================
    public void Interact()
    {
        if (isReading) return;
        StartCoroutine(ReadPaperSequenceRoutine());
    }

    IEnumerator ReadPaperSequenceRoutine()
    {
        isReading = true;
        HasReadPaper = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        // 1. KHÓA DI CHUYỂN & GÓC NHÌN CHUỘT CỦA PLAYER
        MovePl playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = true;
            playerMovePl.SetMovementState(false);
        }

        // Chờ ngắn trước khi fade
        if (delayBeforeFade > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFade);
        }

        EnsureFadeImageExists();

        // 2. FADE MÀN HÌNH TỐI DẦN SANG ĐEN
        if (fadeScreenImage != null)
        {
            fadeScreenImage.gameObject.SetActive(true);
            float elapsed = 0f;
            Color color = fadeScreenImage.color;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Clamp01(elapsed / fadeOutDuration);
                fadeScreenImage.color = color;
                yield return null;
            }
            color.a = 1f;
            fadeScreenImage.color = color;
        }

        // 3. TRONG BÓNG TỐI: PHÁT ÂM THANH LẬT MỞ GIẤY ĐỌC BÀI
        if (paperRustleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(paperRustleSound, soundVolume);
        }

        // Giữ màn hình đen để tạo cảm giác vừa đọc xong
        if (holdBlackDuration > 0f)
        {
            yield return new WaitForSeconds(holdBlackDuration);
        }

        // 4. FADE MÀN HÌNH SÁNG TRỞ LẠI BÌNH THƯỜNG
        if (fadeScreenImage != null)
        {
            float elapsed = 0f;
            Color color = fadeScreenImage.color;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                color.a = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
                fadeScreenImage.color = color;
                yield return null;
            }
            color.a = 0f;
            fadeScreenImage.color = color;
            fadeScreenImage.gameObject.SetActive(false);
        }

        // 5. PHÁT CHUỖI CÂU THOẠI TÓM TẮT NỘI DUNG TỜ GIẤY
        if (summaryDialogues != null && summaryDialogues.Length > 0)
        {
            foreach (DialogueLine line in summaryDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(line));
                }
            }
        }

        // 6. MỞ KHÓA LẠI PLAYER VÀ TỰ DO DI CHUYỂN
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = false;
            playerMovePl.SetMovementState(true);
        }

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isReading = false;

        // Kích hoạt sự kiện / Trigger tiếp theo (nếu có)
        if (nextTriggerToActivate != null)
        {
            nextTriggerToActivate.SetActive(true);
        }
    }

    // =========================================================================
    // ENGINE GÕ CHỮ TYPEWRITER CHO PHỤ ĐỀ
    // =========================================================================
    IEnumerator PlaySingleLineRoutine(DialogueLine line)
    {
        if (line == null) yield break;

        if (subtitleTextUI == null) subtitleTextUI = FindSubtitleTextUI();

        string lang = SettingsManager.currentLanguage;
        currentFullText = (lang == "VI") ? line.vietnameseDialogue : line.englishDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.vietnameseDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.englishDialogue;

        if (string.IsNullOrEmpty(currentFullText)) yield break;

        if (subtitleTextUI != null)
        {
            if (subtitleTextUI.transform.parent != null && !subtitleTextUI.transform.parent.gameObject.activeSelf)
            {
                subtitleTextUI.transform.parent.gameObject.SetActive(true);
            }
            subtitleTextUI.gameObject.SetActive(true);

            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;
        }

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        skipRequested = false;

        // Âm thanh gõ chữ
        if (dialogueSound != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.clip = dialogueSound;
            audioSource.volume = dialogueVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        // Gõ chữ Substring
        if (useTypewriterEffect && subtitleTextUI != null)
        {
            isTyping = true;
            subtitleTextUI.text = "";

            for (int i = 0; i <= currentFullText.Length; i++)
            {
                if (!isTyping || skipRequested) break;
                string typed = currentFullText.Substring(0, i);
                if (showBlinkingCursor) typed += "_";
                subtitleTextUI.text = typed;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            subtitleTextUI.text = currentFullText;
            isTyping = false;
        }
        else if (subtitleTextUI != null)
        {
            subtitleTextUI.text = currentFullText;
        }

        if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;

        if (audioSource != null && audioSource.isPlaying)
        {
            StartCoroutine(FadeAudioOutRoutine(audioSource, 0.08f));
        }

        // Con trỏ nhấp nháy
        if (showBlinkingCursor && subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

        // Chờ người chơi đọc hoặc click skip
        isWaitingForNextLine = true;
        float holdTime = (line.holdDuration > 0f) ? line.holdDuration : holdTimePerLine;
        float waitTimer = 0f;
        while (waitTimer < holdTime && !skipRequested)
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

        if (useFadeEffect && subtitleTextUI != null)
        {
            yield return StartCoroutine(FadeTextOutRoutine(subtitleTextUI, textFadeDuration));
        }

        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;
            subtitleTextUI.gameObject.SetActive(false);
        }

        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }

    IEnumerator BlinkCursorRoutine(TextMeshProUGUI txt, string baseText)
    {
        bool showUnderscore = true;
        while (true)
        {
            if (txt != null)
            {
                txt.text = baseText + (showUnderscore ? " _" : "  ");
            }
            showUnderscore = !showUnderscore;
            yield return new WaitForSeconds(0.4f);
        }
    }

    IEnumerator FadeTextOutRoutine(TextMeshProUGUI txt, float duration)
    {
        if (txt == null) yield break;
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

    IEnumerator FadeAudioOutRoutine(AudioSource src, float duration)
    {
        if (src == null || !src.isPlaying) yield break;
        float startVol = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        src.Stop();
        src.volume = startVol;
    }

    void EnsureFadeImageExists()
    {
        if (fadeScreenImage != null) return;

        Image[] allImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var img in allImages)
        {
            if (img == null) continue;
            string n = img.gameObject.name.ToLower();
            if (n.Contains("fade") || n.Contains("black"))
            {
                fadeScreenImage = img;
                return;
            }
        }

        GameObject fadeObj = GameObject.Find("FadeScreen") ?? GameObject.Find("BlackScreen") ?? GameObject.Find("FadeImage") ?? GameObject.Find("Fade");
        if (fadeObj != null)
        {
            fadeScreenImage = fadeObj.GetComponent<Image>();
            if (fadeScreenImage != null) return;
        }

        // Tự động tạo nếu chưa có
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject fObj = new GameObject("FadeScreen");
            fObj.transform.SetParent(canvas.transform, false);
            fObj.transform.SetAsLastSibling();

            fadeScreenImage = fObj.AddComponent<Image>();
            fadeScreenImage.color = new Color(0f, 0f, 0f, 0f);

            RectTransform rt = fadeScreenImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            fObj.SetActive(false);
        }
    }

    TextMeshProUGUI FindSubtitleTextUI()
    {
        if (Map02IntroSequence.Instance != null && Map02IntroSequence.Instance.subtitleTextUI != null)
            return Map02IntroSequence.Instance.subtitleTextUI;

        SmartInteractionDialogue sid = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
        if (sid != null && sid.subtitleTextUI != null) return sid.subtitleTextUI;

        GameObject subObj = GameObject.Find("SubtitlesText") ?? GameObject.Find("Subtitle Text") ?? GameObject.Find("SubtitleText") ?? GameObject.Find("Subtitle") ?? GameObject.Find("DialogueText");
        if (subObj != null) return subObj.GetComponent<TextMeshProUGUI>();

        TextMeshProUGUI[] tmps = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in tmps)
        {
            if (tmp.gameObject.name.ToLower().Contains("sub")) return tmp;
        }

        return null;
    }

    public void ShowPrompt() { }
    public void HidePrompt() { }
}