using UnityEngine;
using System.Collections;
using TMPro;

public class CorpseDialogueInteractable : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "";
        [TextArea(2, 4)]
        public string englishDialogue = "";
        public float holdDuration = 3.2f;
    }

    [Header("0. Điều Kiện Xuất Hiện (Spawn Condition)")]
    [Tooltip("Chỉ hiện cái xác sau khi người chơi đã nhặt Máy Quay (Camcorder)")]
    public bool requireCamcorderPickup = true;

    [Tooltip("Các GameObject phụ kèm theo cần ẩn/hiện cùng cái xác (ví dụ: bệ đá, vết máu...)")]
    public GameObject[] additionalObjectsToToggle;

    [Header("1. Danh Sách Câu Thoại Khi Kiểm Tra Xác (Corpse Dialogues)")]
    public DialogueLine[] inspectDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Cái quái gì đây...",
            englishDialogue = "What the hell is this...",
            holdDuration = 3.5f
        },
        new DialogueLine
        {
            vietnameseDialogue = "Đạo cụ này trông thật quá...",
            englishDialogue = "This prop looks way too real...",
            holdDuration = 3.2f
        }
    };

    [Header("2. Cài Đặt Tương Tác (Settings)")]
    [Tooltip("Chỉ cho phép tương tác 1 lần duy nhất hay có thể xem lại nhiều lần")]
    public bool onlyInteractOnce = false;

    [Tooltip("Khóa di chuyển và xoay camera trong lúc phát thoại")]
    public bool lockMovementDuringDialogue = false;

    [Header("3. Âm Thanh Rùng Rợn / Cảnh Báo (Tùy Chọn)")]
    [Tooltip("Âm thanh hiệu ứng kinh dị rùng rợn khi tương tác với cái xác")]
    public AudioClip eerieCueSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

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

    private AudioSource audioSource;
    private bool isInteracting = false;
    private bool hasInteracted = false;
    private bool isVisible = true;

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

        // Tự động thêm BoxCollider nếu chưa có để nhận diện tia nhìn chuột
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

        // Kiểm tra điều kiện nhặt máy quay lúc ban đầu
        if (requireCamcorderPickup)
        {
            bool isPickedUp = CamcorderUI.HasPickedUpCamera;
            SetCorpseVisibility(isPickedUp);
        }
        else
        {
            SetCorpseVisibility(true);
        }
    }

    void Update()
    {
        // Tự động hiện cái xác lên ngay khi người chơi vừa nhặt máy quay
        if (requireCamcorderPickup && !isVisible && CamcorderUI.HasPickedUpCamera)
        {
            SetCorpseVisibility(true);
            Debug.Log("[CorpseDialogueInteractable] 👻 Đã nhặt Camcorder! Cái xác dưới tượng Phật chính thức xuất hiện!");
        }

        if (!isInteracting) return;

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
    // QUẢN LÝ ẨN / HIỆN CÁI XÁC
    // =========================================================================
    public void SetCorpseVisibility(bool visible)
    {
        isVisible = visible;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null) r.enabled = visible;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            if (c != null) c.enabled = visible;
        }

        if (additionalObjectsToToggle != null)
        {
            foreach (var obj in additionalObjectsToToggle)
            {
                if (obj != null) obj.SetActive(visible);
            }
        }
    }

    // =========================================================================
    // IINTERACTABLE: TƯƠNG TÁC KHI CLICK CHUỘT TRÁI
    // =========================================================================
    public void Interact()
    {
        if (!isVisible) return;
        if (isInteracting) return;
        if (onlyInteractOnce && hasInteracted) return;

        StartCoroutine(InspectCorpseRoutine());
    }

    IEnumerator InspectCorpseRoutine()
    {
        isInteracting = true;
        hasInteracted = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        MovePl playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (lockMovementDuringDialogue && playerMovePl != null)
        {
            playerMovePl.isCameraLocked = true;
            playerMovePl.SetMovementState(false);
        }

        // Phát âm thanh rùng rợn nếu có
        if (eerieCueSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(eerieCueSound, soundVolume);
        }

        // Chờ 1 frame tránh click tương tác ban đầu
        yield return null;

        // Phát chuỗi câu thoại kiểm tra xác
        if (inspectDialogues != null && inspectDialogues.Length > 0)
        {
            foreach (DialogueLine line in inspectDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(line));
                }
            }
        }

        // Mở khóa lại nhân vật
        if (lockMovementDuringDialogue && playerMovePl != null)
        {
            playerMovePl.isCameraLocked = false;
            playerMovePl.SetMovementState(true);
        }

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isInteracting = false;

        // Kích hoạt sự kiện tiếp theo (nếu có)
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
