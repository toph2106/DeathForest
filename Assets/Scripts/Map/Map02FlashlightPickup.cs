using UnityEngine;
using System.Collections;
using TMPro;

public class Map02FlashlightPickup : MonoBehaviour, IInteractable
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

    [Header("1. Âm Thanh Khi Nhặt Đèn Pin (Pickup SFX)")]
    [Tooltip("Tiếng lách cách bật công tắc đèn pin")]
    public AudioClip flashlightClickSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("2. Thoại Khi Nhặt Đèn Pin (Pickup Dialogues)")]
    public DialogueLine[] pickupDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Một cây đèn pin... May quá nó vẫn còn dùng được.",
            englishDialogue = "A flashlight... Luckily it still works.",
            holdDuration = 3.2f
        }
    };

    [Header("3. Quản Lý Đèn Ngôi Đền Vụt Tắt (Tùy Chọn)")]
    [Tooltip("Kéo các Point Light của ngôi đền vào đây nếu muốn đèn đền nhấp nháy tắt sau khi nhặt đèn pin")]
    public Light[] shrinePointLights;
    public float delayBeforeFlicker = 3.0f;
    public float flickerDuration = 2.2f;
    public AudioClip lightFlickerAudio;
    public AudioClip lightBlackoutAudio;

    [Header("4. Cấu Hình Gõ Chữ Typewriter (Subtitles)")]
    public TextMeshProUGUI subtitleTextUI;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.03f;
    public float holdTimePerLine = 3.2f;
    public bool showBlinkingCursor = true;
    public bool useFadeEffect = true;
    public float fadeDuration = 0.2f;
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    [Header("5. Kích Hoạt Sự Kiện / Quái Vật Tiếp Theo (Tùy Chọn)")]
    public GameObject nextTriggerToActivate;

    [Header("6. Mở Khóa Tờ Giấy Sau Khi Nhặt Đèn Pin (Paper Unlock)")]
    [Tooltip("Kéo GameObject Tờ Giấy (Note) vào đây. Collider của tờ giấy sẽ tự động tắt lúc đầu để không che mất đèn pin, và chỉ bật lên sau khi đã nhặt đèn pin và thoại xong.")]
    public GameObject notePaperObject;
    [Tooltip("Thời gian chờ (giây) sau khi phát xong thoại nhặt đèn pin mới bật tương tác cho tờ giấy")]
    public float delayBeforeUnlockPaper = 2.0f;

    [Header("7. Cấu Hình % Pin Ngẫu Nhiên Khi Nhặt (Random Battery)")]
    [Tooltip("Bật chế độ ngẫu nhiên % pin còn lại khi vừa nhặt đèn")]
    public bool useRandomBattery = true;
    [Range(0, 100)] public int minBatteryPercent = 70;
    [Range(0, 100)] public int maxBatteryPercent = 100;

    private AudioSource audioSource;
    private bool isPickedUp = false;
    private bool isEquipping = false;

    // Biến hỗ trợ skip thoại
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

        // Tự động thêm BoxCollider nếu chưa có
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                boxCol.center = transform.InverseTransformPoint(mr.bounds.center);
                boxCol.size = mr.bounds.size;
            }
        }
    }

    void Start()
    {
        if (subtitleTextUI == null)
        {
            subtitleTextUI = FindSubtitleTextUI();
        }

        // Đảm bảo model 3D cây đèn pin trên bàn luôn hiển thị ban đầu
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) r.enabled = true;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.enabled = true;

        // Tự động tắt Collider của tờ giấy lúc đầu để người chơi không bị click nhầm vào giấy
        SetPaperCollidersState(false);
    }

    private void SetPaperCollidersState(bool enabledState)
    {
        if (notePaperObject == null) return;

        Collider[] paperCols = notePaperObject.GetComponentsInChildren<Collider>(true);
        foreach (var c in paperCols)
        {
            if (c != null) c.enabled = enabledState;
        }
    }

    void Update()
    {
        if (!isEquipping) return;

        // Bấm Chuột Trái hoặc Space để qua nhanh thoại
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
    // IINTERACTABLE: TƯƠNG TÁC KHI CLICK CHUỘT TRÁI
    // =========================================================================
    public void Interact()
    {
        if (isPickedUp || isEquipping) return;
        StartCoroutine(EquipFlashlightRoutine());
    }

    IEnumerator EquipFlashlightRoutine()
    {
        isEquipping = true;
        isPickedUp = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;
        Debug.Log("[Map02FlashlightPickup] 🔦 Người chơi nhặt cây đèn pin trên bàn cúng!");

        // 1. Ẩn toàn bộ hình ảnh 3D cây đèn pin trên bàn
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = false;

        // 2. Tính toán % Pin khởi đầu (Random từ 70% đến 100%)
        float startBattery = useRandomBattery ? Random.Range(minBatteryPercent, maxBatteryPercent + 1) : 100f;

        // 3. TRANG BỊ VÀ BẬT SÁNG ĐÈN PIN TRÊN TAY NGƯỜI CHƠI
        FlashlightToggle ft = FlashlightToggle.Instance ?? Object.FindFirstObjectByType<FlashlightToggle>(FindObjectsInactive.Include);
        if (ft != null)
        {
            ft.gameObject.SetActive(true);
            ft.EquipFlashlight(false, startBattery);
            Debug.Log($"[Map02FlashlightPickup] 💡 Đã trang bị đèn pin cho người chơi với {startBattery}% pin!");
        }

        // 4. Phát tiếng bật đèn pin chuẩn 1 lần duy nhất tự nhiên
        if (flashlightClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(flashlightClickSound, soundVolume);
        }

        yield return null; // Chờ 1 frame click ban đầu

        // 5. PHÁT CHUỖI CÂU THOẠI KHI NHẶT ĐÈN PIN
        if (pickupDialogues != null && pickupDialogues.Length > 0)
        {
            foreach (DialogueLine line in pickupDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(line));
                }
            }
        }

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isEquipping = false;

        // 6. KIỂM TRA ĐIỀU KIỆN TẮT ĐÈN ĐỀN (BẮT BUỘC PHẢI NHẶT ĐỦ CẢ 2 MỚI TẮT)
        if (Map02CamcorderManager.Instance != null)
        {
            Map02CamcorderManager.Instance.CheckAndTriggerShrineBlackout();
        }
        else if (shrinePointLights != null && shrinePointLights.Length > 0)
        {
            StartCoroutine(ShrineLightFlickerAndBlackoutRoutine());
        }

        // Kích hoạt sự kiện tiếp theo (nếu có)
        if (nextTriggerToActivate != null)
        {
            nextTriggerToActivate.SetActive(true);
        }

        // 7. CHỜ THOẠI XONG RỒI DELAY 2 GIÂY MỚI MỞ KHÓA TRIGGER CHO TỜ GIẤY (NOTE)
        if (delayBeforeUnlockPaper > 0f)
        {
            yield return new WaitForSeconds(delayBeforeUnlockPaper);
        }
        SetPaperCollidersState(true);
        Debug.Log("[Map02FlashlightPickup] 📜 Đã kết thúc thoại nhặt đèn + chờ 2s ➔ Mở khóa tương tác cho tờ giấy (Note)!");
    }

    IEnumerator ShrineLightFlickerAndBlackoutRoutine()
    {
        if (delayBeforeFlicker > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFlicker);
        }

        if (lightFlickerAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(lightFlickerAudio, soundVolume);
        }

        float elapsed = 0f;
        bool lightState = true;
        while (elapsed < flickerDuration)
        {
            float randomInterval = Random.Range(0.04f, 0.12f);
            elapsed += randomInterval;

            lightState = !lightState;
            SetShrineLightsState(lightState);

            yield return new WaitForSeconds(randomInterval);
        }

        SetShrineLightsState(false);

        if (lightBlackoutAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(lightBlackoutAudio, soundVolume);
        }
    }

    void SetShrineLightsState(bool active)
    {
        if (shrinePointLights == null || shrinePointLights.Length == 0) return;
        foreach (var l in shrinePointLights)
        {
            if (l != null) l.enabled = active;
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

        if (dialogueSound != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.clip = dialogueSound;
            audioSource.volume = dialogueVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

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

        if (showBlinkingCursor && subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

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
            yield return StartCoroutine(FadeTextOutRoutine(subtitleTextUI, fadeDuration));
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
}
