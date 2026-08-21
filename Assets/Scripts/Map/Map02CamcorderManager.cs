using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Map02CamcorderManager : MonoBehaviour, IInteractable
{
    public static Map02CamcorderManager Instance { get; private set; }

    [Header("1. UI Màn Hình Máy Quay (Camera UI Canvas)")]
    [Tooltip("Kéo GameObject Camcorder trong Canvas vào đây (Nếu để trống script sẽ tự tìm!)")]
    public GameObject camcorderUICanvasObject;

    [Header("2. Âm Thanh Khi Nhặt Máy Quay (Pickup SFX)")]
    [Tooltip("Tiếng lật mở màn hình máy quay (Cạch cạch)")]
    public AudioClip camcorderClickSound;

    [Tooltip("Tiếng bíp khởi động máy quay (Beep)")]
    public AudioClip camcorderBeepSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.8f;

    [Header("3. Thoại Khi Nhặt Máy Quay (Pickup Dialogues)")]
    public DialogueLine[] pickupDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Nó còn nhiều pin không vậy",
            englishDialogue = "Does it still have battery?",
            holdDuration = 2.5f
        },
        new DialogueLine
        {
            vietnameseDialogue = "May quá vẫn còn",
            englishDialogue = "Luckily it still works",
            holdDuration = 2.5f
        }
    };

    [Header("4. Quản Lý Đèn Ngôi Đền & Sự Kiện Vụt Tắt (Shrine Point Light)")]
    [Tooltip("Kéo các Point Light / Nguồn sáng trong ngôi đền vào đây")]
    public Light[] shrinePointLights;

    [Tooltip("Thời gian chờ sau khi chạy xong thoại nhặt máy quay để đèn bắt đầu nhấp nháy (giây - Mặc định: 3.0s)")]
    public float delayBeforeFlicker = 3.0f;

    [Tooltip("Thời gian đèn nhấp nháy dồn dập trước khi vụt tắt hoàn toàn (giây - Mặc định: 2.2s)")]
    public float flickerDuration = 2.2f;

    [Tooltip("Âm thanh đèn chập chờn / rùng rợn khi nhấp nháy (Tùy chọn)")]
    public AudioClip lightFlickerAudio;

    [Tooltip("Âm thanh khi đèn vụt tắt hẳn / tiếng động kinh dị trong đền (Tùy chọn)")]
    public AudioClip lightBlackoutAudio;

    [Header("5. Cấu Hình Gõ Chữ Typewriter (Đồng Bộ Chuẩn)")]
    public TextMeshProUGUI subtitleTextUI;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.03f;
    public float holdTimePerLine = 3.2f;
    public bool showBlinkingCursor = true;
    public bool useFadeEffect = true;
    public float fadeDuration = 0.2f;
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    [Header("6. Kích Hoạt Sự Kiện / Trigger Tiếp Theo (Tùy Chọn)")]
    public GameObject nextTriggerToActivate;

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "";
        [TextArea(2, 4)]
        public string englishDialogue = "";
        public float holdDuration = 2.5f;
    }

    private SimpleCameraOverlay overlayManager;
    private AudioSource audioSource;
    private bool isPickedUp = false;
    private bool isEquipping = false;

    // Biến hỗ trợ Click Chuột Trái / Space Skip Thoại
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D âm thanh rõ nét
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        // 1. Tắt UI máy quay ban đầu (chưa nhặt)
        if (camcorderUICanvasObject == null)
        {
            GameObject camUI = GameObject.Find("Camcorder");
            if (camUI != null) camcorderUICanvasObject = camUI;
        }
        if (camcorderUICanvasObject != null) camcorderUICanvasObject.SetActive(false);

        if (Camera.main != null)
        {
            overlayManager = Camera.main.GetComponent<SimpleCameraOverlay>();
        }

        if (subtitleTextUI == null)
        {
            subtitleTextUI = FindSubtitleTextUI();
        }

        // Đảm bảo mô hình 3D chiếc máy quay và Collider trong đền LUÔN HIỆN HỮU khi bắt đầu Map 02
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) r.enabled = true;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.enabled = true;

        // Đảm bảo đèn trong ngôi đền sáng sẵn từ đầu
        if (shrinePointLights != null)
        {
            foreach (var l in shrinePointLights)
            {
                if (l != null)
                {
                    l.gameObject.SetActive(true);
                    l.enabled = true;
                }
            }
        }
    }

    void Update()
    {
        // Bấm chuột trái hoặc Space để qua thoại nhanh
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
    // NGƯỜI CHƠI TƯƠNG TÁC NHẶT MÁY QUAY (CLICK CHUỘT TRÁI)
    // =========================================================================
    public void Interact()
    {
        Pickup();
    }

    public void Pickup()
    {
        if (isPickedUp || isEquipping) return;
        StartCoroutine(EquipCamcorderRoutine());
    }

    IEnumerator EquipCamcorderRoutine()
    {
        isEquipping = true;
        isPickedUp = true;
        Debug.Log("[Map02CamcorderManager] 📹 Người chơi tương tác nhặt máy quay trong ngôi đền!");

        // 1. Ẩn toàn bộ hình ảnh 3D chiếc máy quay trên bục đền
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = false;

        if (subtitleTextUI == null) subtitleTextUI = FindSubtitleTextUI();

        // Chờ 1 frame để click tương tác ban đầu trôi qua
        yield return null;

        // 2. BƯỚC 1: PHÁT NGAY CÂU THOẠI 1 ("Nó còn nhiều pin không vậy")
        if (pickupDialogues != null && pickupDialogues.Length > 0 && pickupDialogues[0] != null)
        {
            yield return StartCoroutine(PlaySingleLineRoutine(pickupDialogues[0]));
        }

        // 3. BƯỚC 2: PHÁT TIẾNG "CẠCH CẠCH" MỞ NẮP MÀN HÌNH MÁY QUAY
        if (camcorderClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(camcorderClickSound, soundVolume);
            yield return new WaitForSeconds(camcorderClickSound.length);
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        // 4. BƯỚC 3: PHÁT TIẾNG "BÍP" KHỞI ĐỘNG MÁY QUAY
        if (camcorderBeepSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(camcorderBeepSound, soundVolume);
            yield return new WaitForSeconds(0.2f);
        }

        // 5. BƯỚC 4: KÍCH HOẠT GIAO DIỆN KÍNH NGẮM VÀ UI CAMCORDER + ĐÈN PIN PLAYER
        CamcorderUI.MarkCameraPickedUp();

        if (CamcorderUI.Instance != null)
        {
            CamcorderUI.Instance.gameObject.SetActive(true);
            Transform[] children = CamcorderUI.Instance.GetComponentsInChildren<Transform>(true);
            foreach (var c in children) if (c != null) c.gameObject.SetActive(true);
        }

        if (camcorderUICanvasObject != null)
        {
            camcorderUICanvasObject.SetActive(true);
            Transform[] children = camcorderUICanvasObject.GetComponentsInChildren<Transform>(true);
            foreach (var c in children) if (c != null) c.gameObject.SetActive(true);
        }

        if (overlayManager != null)
        {
            overlayManager.TurnOnCameraView();
        }

        // 6. BƯỚC 5: PHÁT CÁC CÂU THOẠI TIẾP THEO (NẾU CÓ)
        if (pickupDialogues != null && pickupDialogues.Length > 1)
        {
            for (int i = 1; i < pickupDialogues.Length; i++)
            {
                if (pickupDialogues[i] != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(pickupDialogues[i]));
                }
            }
        }

        isEquipping = false;

        // 7. BƯỚC 6: KIỂM TRA ĐIỀU KIỆN TẮT ĐÈN ĐỀN (BẮT BUỘC PHẢI NHẶT ĐỦ CẢ 2 MỚI TẮT)
        CheckAndTriggerShrineBlackout();
    }

    public static bool hasTriggeredBlackout = false;

    public void CheckAndTriggerShrineBlackout()
    {
        if (hasTriggeredBlackout) return;

        bool hasCam = CamcorderUI.HasPickedUpCamera;
        bool hasFlash = (FlashlightToggle.Instance != null && FlashlightToggle.Instance.hasFlashlight);

        if (hasCam && hasFlash)
        {
            hasTriggeredBlackout = true;
            Debug.Log("[Map02CamcorderManager] ⛩️ Đã nhặt ĐỦ CẢ 2 (Máy quay + Đèn pin)! Bắt đầu đếm ngược 3s hiệu ứng đèn đền vụt tắt...");
            StartCoroutine(ShrineLightFlickerAndBlackoutRoutine());
        }
        else
        {
            Debug.Log($"[Map02CamcorderManager] ⛩️ Đèn đền vẫn sáng (Đã có Cam: {hasCam}, Đèn pin: {hasFlash}). Cần nhặt nốt món còn lại mới bắt đầu đếm ngược tắt!");
        }
    }

    // =========================================================================
    // SỰ KIỆN KINH DỊ: ĐÈN POINT LIGHT TRONG ĐỀN NHẤP NHÁY RỒI VỤT TẮT HOÀN TOÀN
    // =========================================================================
    IEnumerator ShrineLightFlickerAndBlackoutRoutine()
    {
        // Chờ 3 giây sau khi thoại kết thúc
        if (delayBeforeFlicker > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFlicker);
        }

        Debug.Log("[Map02CamcorderManager] 👻 Đèn Point Light trong ngôi đền bắt đầu nhấp nháy dồn dập...");

        if (lightFlickerAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(lightFlickerAudio, soundVolume);
        }

        // Nhấp nháy dồn dập liên tục trong khoảng 2.2 giây
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

        // VỤT TẮT HOÀN TOÀN!
        SetShrineLightsState(false);
        Debug.Log("[Map02CamcorderManager] 🌑 Đèn trong ngôi đền đã vụt tắt hoàn toàn!");

        if (lightBlackoutAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(lightBlackoutAudio, soundVolume);
        }

        // Kích hoạt sự kiện / Trigger tiếp theo (nếu có)
        if (nextTriggerToActivate != null)
        {
            nextTriggerToActivate.SetActive(true);
        }
    }

    void SetShrineLightsState(bool active)
    {
        if (shrinePointLights == null || shrinePointLights.Length == 0) return;

        foreach (var l in shrinePointLights)
        {
            if (l != null)
            {
                l.enabled = active;
            }
        }
    }

    // =========================================================================
    // ENGINE PHỤ ĐỀ GÕ CHỮ TYPEWRITER CHUẨN
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

        // Chờ đọc hoặc skip
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
