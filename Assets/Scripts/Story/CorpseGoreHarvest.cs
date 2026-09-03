using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Script xử lý tương tác đặc biệt với Cái Xác (Corpse Gore Harvest):
/// 1. Kế thừa IInteractable -> Tự động nhận diện tâm ngắm và hiện icon Bàn Tay (Hand).
/// 2. TH1 (Chưa có Dao): Hiện thoại người chơi nhận xét cần vật sắc nhọn để mổ xác.
/// 3. TH2 (Đã có Dao): 
///    - Hiện câu thoại áy náy: "Xin lỗi..."
///    - Fade đen màn hình mượt mà (Smooth Cinema Fade), khóa di chuyển, phát âm thanh mổ rạch thịt.
///    - Trao vật phẩm "Gore" (Nội Tạng) vào Inventory (có 3D Preview trong ô Slot).
///    - Fade sáng trở lại và hiện câu thoại sau khi lấy đồ.
/// 4. TH3 (Đã lấy rồi): Báo thi thể không còn gì để kiểm tra.
/// </summary>
public class CorpseGoreHarvest : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 3)]
        public string vietnameseDialogue = "";
        [TextArea(2, 3)]
        public string englishDialogue = "";
        public float holdDuration = 3.5f;
    }

    [Header("1. Yêu Cầu Vật Phẩm (Required Item)")]
    [Tooltip("Tên vật phẩm cần có trong túi đồ để mổ xác (Mặc định: 'Dao')")]
    public string requiredKnifeName = "Dao";
    [Tooltip("Có xóa con dao sau khi mổ xác không? (Bỏ tích = Vẫn giữ lại dao trong túi)")]
    public bool consumeKnife = false;

    [Header("2. Phần Thưởng Vật Phẩm Nhận Được (Harvest Reward)")]
    [Tooltip("Tên vật phẩm nhận được sau khi mổ xác")]
    public string rewardGoreItemName = "Nội Tạng";
    [Tooltip("Kéo Prefab 3D 'Gore' vào đây để hiển thị xoay trong ô Inventory")]
    public GameObject gore3DPrefab;
    [Tooltip("Hình icon 2D (nếu có, không có thì Inventory dùng 3D Preview)")]
    public Sprite goreIconSprite;

    [Header("3. Cấu Hình Fade Màn Hình (Cinema Fade)")]
    [Tooltip("Kéo UI Fade Image (hoặc để trống để code tự động tìm Canvas tạo màn che)")]
    public Image fadeImage;
    public Color fadeColor = Color.black;
    [Tooltip("Thời gian màn hình tối dần (giây)")]
    public float fadeInDuration = 0.8f;
    [Tooltip("Thời gian giữ màn hình đen trong lúc mổ xác (giây)")]
    public float blackScreenHoldDuration = 2.5f;
    [Tooltip("Thời gian màn hình sáng lại (giây)")]
    public float fadeOutDuration = 0.8f;

    [Header("4. Âm Thanh (Audio Sfx)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh mổ rạch thịt / gore rùng rợn trong lúc màn hình đen")]
    public AudioClip cutFleshSound;
    [Tooltip("Âm thanh chạy chữ (Tự động tìm nếu để trống)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("5. Thoại Khi CHƯA Có Dao (No Knife Dialogue)")]
    public DialogueLine[] noKnifeDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Một thi thể không đầu... Trông có vẻ bên trong ổ bụng có thứ gì đó, nhưng mình cần một vật sắc nhọn để rạch ra.",
            englishDialogue = "A headless corpse... Looks like there's something inside the abdomen, but I need something sharp to cut it open.",
            holdDuration = 4.0f
        }
    };

    [Header("6. Thoại TRƯỚC Khi Mổ Xác (Pre-Cut Dialogue)")]
    public DialogueLine[] preCutDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Xin lỗi...",
            englishDialogue = "I'm sorry...",
            holdDuration = 2.5f
        }
    };

    [Header("7. Thoại SAU Khi Mổ Xác Xong (Post-Harvest Dialogue)")]
    public DialogueLine[] postHarvestDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Đã lấy được nội tạng... Thật kinh tởm, nhưng có thể đây là thứ cần thiết để giải ấn.",
            englishDialogue = "Obtained the organ... Disgusting, but this might be needed for the seal.",
            holdDuration = 4.0f
        }
    };

    [Header("8. Thoại Khi Đã Lấy Xong Rồi (Already Harvested)")]
    public DialogueLine[] alreadyHarvestedDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Không còn gì để kiểm tra ở thi thể này nữa...",
            englishDialogue = "Nothing else left to inspect from this body...",
            holdDuration = 3.0f
        }
    };

    [Header("8.1. Thoại Khi Túi Đồ ĐẦY (5/5 Ô) Không Thể Chứa Gore")]
    public DialogueLine[] fullInventoryDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Túi đồ của mình đã đầy rồi... Mình cần giải phóng bớt chỗ trống trước khi lấy thứ này ra.",
            englishDialogue = "My inventory is full... I need to make some space before taking this out.",
            holdDuration = 3.8f
        }
    };

    [Header("9. Cấu Hình Phụ Đề (Subtitle Text UI)")]
    [Tooltip("Kéo TextMeshPro Subtitle Text trên Canvas vào đây (Để trống sẽ tự tìm)")]
    public TextMeshProUGUI subtitleTextUI;
    public bool useTypewriter = true;
    public float typewriterSpeed = 0.035f;

    [Header("10. Sự Kiện Mở Rộng (Events - Tùy Chọn)")]
    public UnityEvent onHarvestStart;
    public UnityEvent onHarvestCompleted;

    [Header("11. Kích Hoạt Vùng Quái (Trigger Stranger Zone - Tùy Chọn)")]
    [Tooltip("Kéo GameObject ZoneStranger (hoặc để trống để script tự động tìm) để kích hoạt minigame sau khi mổ xác")]
    public StrangerMinigameManager strangerZoneManager;
    [Tooltip("Tự động kích hoạt Stranger Zone ngay sau khi lấy được nội tạng")]
    public bool activateStrangerZoneOnHarvest = true;

    // --- Static & Public State ---
    public static bool IsGoreHarvested { get; set; } = false;
    public bool HasHarvested => hasHarvested;

    // --- Private State ---
    private bool isInteracting = false;
    private bool hasHarvested = false;
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private bool skipWaitRequested = false;
    private float interactStartTime = 0f;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;

    void Awake()
    {
        EnsureAudioSource();
        AutoFindDialogueSound();
        FindSubtitleUI();
        EnsureFadeImage();
    }

    void Start()
    {
        EnsureAudioSource();
        AutoFindDialogueSound();
        FindSubtitleUI();
        EnsureFadeImage();
    }

    void EnsureAudioSource()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D Sound
            audioSource.playOnAwake = false;
        }
    }

    void AutoFindDialogueSound()
    {
        if (dialogueSound != null) return;

        SmartInteractionDialogue smart = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
        if (smart != null && smart.dialogueSound != null)
        {
            dialogueSound = smart.dialogueSound;
            return;
        }

        ReadablePaper readable = Object.FindFirstObjectByType<ReadablePaper>(FindObjectsInactive.Include);
        if (readable != null && readable.dialogueSound != null)
        {
            dialogueSound = readable.dialogueSound;
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>("Sound/8-bit-wavering-text-scroll");
        if (clip != null) dialogueSound = clip;
    }

    void FindSubtitleUI()
    {
        if (subtitleTextUI != null) return;

        GameObject subContainer = GameObject.Find("Subtitle");
        if (subContainer != null)
        {
            subtitleTextUI = subContainer.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (subtitleTextUI == null)
        {
            GameObject subObj = GameObject.Find("Subtitle Text") ?? GameObject.Find("SubtitleText");
            if (subObj != null) subtitleTextUI = subObj.GetComponent<TextMeshProUGUI>();
        }
    }

    void EnsureFadeImage()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
            return;
        }

        // Tìm UI FadeScreen có sẵn trong scene
        GameObject existingFade = GameObject.Find("Ladder_FadeScreen") ?? GameObject.Find("FadeScreen");
        if (existingFade != null)
        {
            fadeImage = existingFade.GetComponent<Image>();
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
                fadeImage.raycastTarget = false;
                return;
            }
        }

        // Tự tạo màn chắn đen trên Canvas nếu chưa có
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject newFadeObj = new GameObject("Corpse_FadeScreen");
            newFadeObj.transform.SetParent(canvas.transform, false);
            newFadeObj.transform.SetAsLastSibling();

            fadeImage = newFadeObj.AddComponent<Image>();
            Color c = fadeColor;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;

            RectTransform rt = fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    void Update()
    {
        if (!isInteracting) return;

        // Bỏ qua click trong 0.25s đầu tiên để tránh xung đột chuột click tương tác ban đầu
        if (Time.unscaledTime - interactStartTime < 0.25f) return;

        // Bấm chuột trái hoặc Space để tua nhanh phụ đề
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                // BẤM LẦN 1: Hiện đầy đủ chữ ngay lập tức
                skipRequested = true;
            }
            else if (isWaitingForNextLine)
            {
                // BẤM LẦN 2: Chuyển câu tiếp theo hoặc kết thúc
                skipWaitRequested = true;
            }
        }
    }

    /// <summary>
    /// Được gọi tự động khi Player nhìn vào cái xác và bấm Chuột Trái
    /// </summary>
    public void Interact()
    {
        if (isInteracting) return;

        // Kiểm tra xem trong Inventory đã có dao chưa và túi đồ có bị đầy không
        bool hasKnife = false;
        bool isInvFull = false;

        if (InventoryManager.Instance != null)
        {
            hasKnife = InventoryManager.Instance.HasItem(requiredKnifeName) 
                    || InventoryManager.Instance.HasItem("Dao") 
                    || InventoryManager.Instance.HasItem("Knife")
                    || InventoryManager.Instance.HasItem("Sw");

            isInvFull = InventoryManager.Instance.IsInventoryFull();
        }

        if (hasHarvested)
        {
            StartCoroutine(PlaySimpleDialogueSequence(alreadyHarvestedDialogues));
        }
        else if (!hasKnife)
        {
            StartCoroutine(PlaySimpleDialogueSequence(noKnifeDialogues));
        }
        else if (isInvFull)
        {
            Debug.Log("[CorpseGoreHarvest] ⚠️ Túi đồ đã đầy (5/5 ô)! Không thể mổ xác lấy Gore.");
            StartCoroutine(PlaySimpleDialogueSequence(fullInventoryDialogues));
        }
        else
        {
            StartCoroutine(HarvestGoreSequenceRoutine());
        }
    }

    private IEnumerator PlaySimpleDialogueSequence(DialogueLine[] lines)
    {
        isInteracting = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;
        interactStartTime = Time.unscaledTime;
        isTyping = false;
        skipRequested = false;
        skipWaitRequested = false;

        // Chờ 1 frame để tiêu thụ click chuột ban đầu
        yield return null;

        if (lines != null && lines.Length > 0)
        {
            foreach (DialogueLine line in lines)
            {
                if (line != null) yield return StartCoroutine(PlaySingleLineRoutine(line));
            }
        }

        ClearSubtitleUI();

        // Cooldown 0.5s chống spam click
        yield return new WaitForSeconds(0.5f);

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isInteracting = false;
    }

    private IEnumerator HarvestGoreSequenceRoutine()
    {
        isInteracting = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;
        onHarvestStart?.Invoke();

        MovePl playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = true;
            playerMovePl.SetMovementState(false);
        }

        // 1. THOẠI TRƯỚC KHI MỔ XÁC ("Xin lỗi...")
        if (preCutDialogues != null && preCutDialogues.Length > 0)
        {
            foreach (DialogueLine line in preCutDialogues)
            {
                if (line != null) yield return StartCoroutine(PlaySingleLineRoutine(line));
            }
        }

        ClearSubtitleUI();

        // 2. FADE ĐEN MÀN HÌNH MƯỢT MÀ
        EnsureFadeImage();
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
                Color c = fadeColor;
                c.a = alpha;
                fadeImage.color = c;
                yield return null;
            }
            Color finalC = fadeColor;
            finalC.a = 1f;
            fadeImage.color = finalC;
        }

        // 3. PHÁT ÂM THANH MỔ RẠCH THỊT TRONG LÚC ĐEN MÀN HÌNH
        if (cutFleshSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cutFleshSound, soundVolume);
        }

        // Giữ màn hình đen một lúc
        yield return new WaitForSeconds(blackScreenHoldDuration);

        // 4. TRAO VẬT PHẨM "GORE" VÀO INVENTORY
        if (InventoryManager.Instance != null)
        {
            GameObject sourceGore = (gore3DPrefab != null) ? gore3DPrefab : gameObject;
            InventoryManager.Instance.AddConsumableItem(rewardGoreItemName, sourceGore, goreIconSprite);

            if (consumeKnife)
            {
                InventoryManager.Instance.RemoveItem(requiredKnifeName);
            }
        }

        hasHarvested = true;
        IsGoreHarvested = true;

        // 5. FADE SÁNG TRỞ LẠI
        if (fadeImage != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.SmoothStep(1f, 0f, elapsed / fadeOutDuration);
                Color c = fadeColor;
                c.a = alpha;
                fadeImage.color = c;
                yield return null;
            }
            Color clearC = fadeColor;
            clearC.a = 0f;
            fadeImage.color = clearC;
            fadeImage.gameObject.SetActive(false);
        }

        // 6. THOẠI SAU KHI MỔ XÁC
        if (postHarvestDialogues != null && postHarvestDialogues.Length > 0)
        {
            interactStartTime = Time.unscaledTime;
            isTyping = false;
            skipRequested = false;
            skipWaitRequested = false;
            yield return null;

            foreach (DialogueLine line in postHarvestDialogues)
            {
                if (line != null) yield return StartCoroutine(PlaySingleLineRoutine(line));
            }
        }

        ClearSubtitleUI();

        // Cooldown 0.5s chống spam click
        yield return new WaitForSeconds(0.5f);

        // 7. MỞ LẠI QUYỀN ĐIỀU KHIỂN CHO PLAYER
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = false;
            playerMovePl.SetMovementState(true);
        }

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isInteracting = false;

        onHarvestCompleted?.Invoke();
        Debug.Log("[CorpseGoreHarvest] 🩸 Đã mổ xác thành công và nhận vật phẩm Gore vào túi đồ!");

        // Kích hoạt mở khóa Zone Minigame Stranger ngay sau khi mổ xác xong
        if (activateStrangerZoneOnHarvest)
        {
            if (strangerZoneManager == null)
            {
                strangerZoneManager = Object.FindFirstObjectByType<StrangerMinigameManager>();
            }

            if (strangerZoneManager != null)
            {
                strangerZoneManager.UnlockAndActivateZone();
            }
        }
    }

    private IEnumerator PlaySingleLineRoutine(DialogueLine line)
    {
        if (line == null) yield break;

        string lang = SettingsManager.currentLanguage;
        currentFullText = (lang == "VI") ? line.vietnameseDialogue : line.englishDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.vietnameseDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.englishDialogue;

        if (string.IsNullOrEmpty(currentFullText)) yield break;

        FindSubtitleUI();
        if (subtitleTextUI != null)
        {
            if (subtitleTextUI.transform.parent != null) subtitleTextUI.transform.parent.gameObject.SetActive(true);
            subtitleTextUI.gameObject.SetActive(true);
            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;
            subtitleTextUI.text = "";
        }

        isTyping = true;
        skipRequested = false;

        // Âm thanh chạy chữ
        if (dialogueSound == null) AutoFindDialogueSound();
        if (dialogueSound != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.clip = dialogueSound;
            audioSource.volume = soundVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        // Hiệu ứng gõ từng chữ
        if (useTypewriter && subtitleTextUI != null)
        {
            for (int i = 1; i <= currentFullText.Length; i++)
            {
                if (skipRequested)
                {
                    subtitleTextUI.text = currentFullText;
                    break;
                }

                string typed = currentFullText.Substring(0, i) + "_";
                subtitleTextUI.text = typed;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            if (!skipRequested)
            {
                subtitleTextUI.text = currentFullText;
            }
        }
        else if (subtitleTextUI != null)
        {
            subtitleTextUI.text = currentFullText;
        }

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
        {
            audioSource.Stop();
        }

        isTyping = false;
        skipRequested = false;

        // Bật con trỏ nhấp nháy trong lúc chờ đọc
        if (subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

        // Chờ người chơi đọc hoặc click lần 2 để qua
        isWaitingForNextLine = true;
        skipWaitRequested = false;
        float waitTimer = 0f;
        float holdTime = (line.holdDuration > 0f) ? line.holdDuration : 3.5f;

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

    private void ClearSubtitleUI()
    {
        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            if (subtitleTextUI.transform.parent != null && subtitleTextUI.transform.parent.name == "Subtitle")
            {
                subtitleTextUI.transform.parent.gameObject.SetActive(false);
            }
        }
    }
}
