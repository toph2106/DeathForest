using UnityEngine;
using TMPro;
using System.Collections;

public class SmartInteractionDialogue : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue;
        [TextArea(2, 4)]
        public string englishDialogue;
        [Tooltip("Thời gian dừng lại đọc câu thoại (giây) trước khi tự chuyển câu tiếp theo. Mặc định 2.5s")]
        public float holdDuration = 2.5f;
    }

    [Header("1. Trạng Thái Khóa (Locked / Unlocked)")]
    [Tooltip("Tích chọn nếu vật thể này đang bị khóa. Khi tương tác sẽ chạy danh sách Locked Dialogues")]
    public bool isLocked = false;

    [Header("2. Danh Sách Lời Thoại Khi ĐANG KHÓA")]
    [Tooltip("Thoại khi tương tác lúc bị khóa (VD: 'Cửa đang khóa, cần tìm chìa khóa')")]
    public DialogueLine[] lockedDialogueLines;

    [Header("3. Danh Sách Lời Thoại Khi ĐÃ MỞ KHÓA")]
    [Tooltip("Thoại khi tương tác bình thường (VD: 'Mở cửa thành công')")]
    public DialogueLine[] unlockedDialogueLines;

    [Header("4. Cấu Hình Âm Thanh Thoại & Tốc Độ Gõ")]
    [Tooltip("Gói âm thanh lồng tiếng / gõ chữ khi hiện phụ đề (5s blip)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.035f;

    [Header("5. Hiệu Ứng Con Trỏ '_' & Fade Mờ Dần (Đồng Bộ Chuẩn)")]
    [Tooltip("Bật hiệu ứng con trỏ '_' nhấp nháy cuối câu thoại")]
    public bool showBlinkingCursor = true;
    [Tooltip("Bật hiệu ứng mờ dần Fade Out khi chuyển câu thoại")]
    public bool useFadeEffect = true;
    [Tooltip("Thời gian mờ dần Fade (Mặc định: 0.2 giây)")]
    public float fadeDuration = 0.2f;

    [Header("6. Cấu Hình Hành Vi Thoại")]
    [Tooltip("Chỉ chạy thoại 1 lần duy nhất trong game")]
    public bool playOnce = false;
    [Tooltip("Khóa di chuyển của người chơi khi đang đọc thoại")]
    public bool lockMovementWhileSpeaking = false;

    [Header("7. TextMeshPro UI Phụ Đề")]
    public TextMeshProUGUI subtitleTextUI;

    public static bool isAnyDialoguePlaying { get; set; } = false;

    private AudioSource audioSource;
    private bool hasPlayed = false;
    private Coroutine dialogueCoroutine;
    private Coroutine fadeAudioCoroutine;
    private Coroutine cursorBlinkCoroutine;

    // Biến điều khiển Click Skip
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private bool skipWaitRequested = false;
    private string currentFullText = "";
    private float interactStartTime = 0f;

    void Awake()
    {
        EnsureAudioSource();
    }

    void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            // Tạo AudioSource riêng biệt cho thoại để không xung đột 3D sound với các script khác
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // Luôn là âm thanh 2D rõ ràng cho phụ đề
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        FindSubtitleUI();
        EnsureAudioSource();
    }

    void FindSubtitleUI()
    {
        if (subtitleTextUI != null) return;

        // 1. Tìm từ BedSleepCutscene
        BedSleepCutscene bed = UnityEngine.Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
        if (bed != null && bed.subtitleTextUI != null)
        {
            subtitleTextUI = bed.subtitleTextUI;
            return;
        }

        // 2. Tìm từ GameIntroManager
        GameIntroManager intro = UnityEngine.Object.FindFirstObjectByType<GameIntroManager>(FindObjectsInactive.Include);
        if (intro != null && intro.subtitleTextUI != null)
        {
            subtitleTextUI = intro.subtitleTextUI;
            return;
        }

        // 3. Tìm theo tên SubtitleText / Subtitle
        GameObject subObj = GameObject.Find("SubtitleText");
        if (subObj == null) subObj = GameObject.Find("Subtitle");
        if (subObj == null) subObj = GameObject.Find("DialogueText");
        if (subObj != null) subtitleTextUI = subObj.GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (!isAnyDialoguePlaying) return;

        // Bỏ qua input trong 0.15s đầu tiên sau khi kích hoạt tương tác để tránh xung đột chuột
        if (Time.unscaledTime - interactStartTime < 0.15f) return;

        // Bấm chuột trái (Mouse 0) hoặc Space để qua thoại nhanh
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // BẤM LẦN 1 KHI ĐANG GÕ ➔ HIỆN TOÀN BỘ CHỮ NGAY LẬP TỨC
                skipRequested = true;
            }
            else if (isWaitingForNextLine)
            {
                // BẤM LẦN 2 KHI CHỮ ĐÃ ĐẦY ĐỦ ➔ QUA CÂU TIẾP THEO NGAY LẬP TỨC
                skipWaitRequested = true;
            }
        }
    }

    public void Interact()
    {
        if (isAnyDialoguePlaying) return;
        PlayDialogue();
    }

    public void PlayDialogue(System.Action onComplete = null)
    {
        if (playOnce && hasPlayed) return;
        if (isAnyDialoguePlaying) return;

        DialogueLine[] linesToPlay = isLocked ? lockedDialogueLines : unlockedDialogueLines;

        if (linesToPlay == null || linesToPlay.Length == 0)
        {
            // Không có thoại -> gọi callback hoàn tất ngay lập tức
            onComplete?.Invoke();
            return;
        }

        if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
        dialogueCoroutine = StartCoroutine(DialogueSequenceRoutine(linesToPlay, onComplete));
    }

    public void PlayCustomLines(DialogueLine[] customLines, System.Action onComplete = null)
    {
        if (customLines == null || customLines.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
        dialogueCoroutine = StartCoroutine(DialogueSequenceRoutine(customLines, onComplete));
    }

    IEnumerator DialogueSequenceRoutine(DialogueLine[] lines, System.Action onComplete)
    {
        isAnyDialoguePlaying = true;
        hasPlayed = true;
        interactStartTime = Time.unscaledTime;
        isTyping = false;
        skipRequested = false;
        skipWaitRequested = false;

        FindSubtitleUI();
        EnsureAudioSource();

        if (fadeAudioCoroutine != null)
        {
            StopCoroutine(fadeAudioCoroutine);
            fadeAudioCoroutine = null;
        }

        MovePl player = UnityEngine.Object.FindFirstObjectByType<MovePl>();
        if (lockMovementWhileSpeaking && player != null)
        {
            player.SetMovementState(false);
            player.isCameraLocked = true;
        }

        if (subtitleTextUI != null)
        {
            if (subtitleTextUI.transform.parent != null)
                subtitleTextUI.transform.parent.gameObject.SetActive(true);
            subtitleTextUI.gameObject.SetActive(true);
        }

        // Chờ 1 frame để lượt click chuột tương tác ban đầu được tiêu thụ
        yield return null;

        for (int i = 0; i < lines.Length; i++)
        {
            DialogueLine line = lines[i];
            if (line == null) continue;

            string lang = SettingsManager.currentLanguage;
            currentFullText = (lang == "VI") ? line.vietnameseDialogue : line.englishDialogue;
            if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.vietnameseDialogue;
            if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.englishDialogue;

            if (string.IsNullOrEmpty(currentFullText)) continue;

            // Khôi phục Alpha = 1 cho Text
            if (subtitleTextUI != null)
            {
                Color sc = subtitleTextUI.color;
                sc.a = 1f;
                subtitleTextUI.color = sc;
            }

            if (cursorBlinkCoroutine != null)
            {
                StopCoroutine(cursorBlinkCoroutine);
                cursorBlinkCoroutine = null;
            }

            // BẬT ÂM THANH TRONG SUỐT THỜI GIAN ĐANG GÕ CHỮ
            bool playedAudioThisLine = false;
            if (dialogueSound != null && audioSource != null)
            {
                if (fadeAudioCoroutine != null)
                {
                    StopCoroutine(fadeAudioCoroutine);
                    fadeAudioCoroutine = null;
                }
                audioSource.spatialBlend = 0f; // Luôn là 2D âm lượng đầy đủ
                audioSource.clip = dialogueSound;
                audioSource.volume = soundVolume;
                audioSource.loop = true;
                audioSource.time = 0f;
                audioSource.Play();
                playedAudioThisLine = true;
            }

            // Typewriter effect
            isTyping = true;
            skipRequested = false;

            if (useTypewriterEffect && subtitleTextUI != null)
            {
                subtitleTextUI.text = "";
                for (int c = 1; c <= currentFullText.Length; c++)
                {
                    if (skipRequested)
                    {
                        subtitleTextUI.text = currentFullText;
                        break;
                    }
                    string typed = currentFullText.Substring(0, c);
                    if (showBlinkingCursor) typed += "_";
                    subtitleTextUI.text = typed;
                    yield return new WaitForSeconds(typewriterSpeed);
                }
            }
            else if (subtitleTextUI != null)
            {
                subtitleTextUI.text = currentFullText;
            }

            isTyping = false;
            if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;

            // DỪNG ÂM THANH MƯỢT MÀ KHI ĐÃ GÕ XONG (HOẶC SKIP)
            if (playedAudioThisLine && audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
            {
                if (fadeAudioCoroutine != null) StopCoroutine(fadeAudioCoroutine);
                fadeAudioCoroutine = StartCoroutine(FadeAudioOutRoutine(audioSource, 0.08f));
            }

            // Bật con trỏ nhấp nháy '_' trong lúc chờ đọc
            if (showBlinkingCursor && subtitleTextUI != null)
            {
                if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
                cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
            }

            // Chờ đọc xong hoặc bấm click để qua nhanh
            isWaitingForNextLine = true;
            skipWaitRequested = false;
            float waitTimer = 0f;
            float holdTime = (line.holdDuration > 0f) ? line.holdDuration : 2.5f;

            while (waitTimer < holdTime && !skipWaitRequested)
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

            // Hiệu ứng Fade Out mờ dần khi chuyển câu thoại
            if (useFadeEffect && subtitleTextUI != null)
            {
                yield return StartCoroutine(FadeTextOutRoutine(subtitleTextUI, fadeDuration));
            }
        }

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        // Tắt subtitle
        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;
        }

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
        {
            audioSource.Stop();
            audioSource.volume = soundVolume;
        }

        if (lockMovementWhileSpeaking && player != null)
        {
            player.SetMovementState(true);
            player.isCameraLocked = false;
        }

        isAnyDialoguePlaying = false;
        dialogueCoroutine = null;

        onComplete?.Invoke();
    }

    public void SetLockedState(bool locked)
    {
        isLocked = locked;
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
        src.volume = soundVolume;
        fadeAudioCoroutine = null;
    }
}
