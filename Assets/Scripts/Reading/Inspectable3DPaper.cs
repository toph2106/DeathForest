using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Script đọc tài liệu / tờ giấy 3D chuyên nghiệp chuẩn AAA:
/// 1. Click tương tác (IInteractable) vào tờ giấy 3D dưới sàn/nệm.
/// 2. Tờ giấy dưới sàn tạm ẩn đi, ReadNoteP lướt nhẹ từ dưới lên (Smooth Pickup Animation) kèm phóng to và xoay góc đọc tự nhiên.
/// 3. Lớp nền đen mờ Dark_Backdrop tối dần mềm mại (Smooth Fade).
/// 4. Đèn pin VẪN BẬT BÌNH THƯỜNG.
/// 5. Đọc không bị tự tắt (Click 1 hiện hết chữ, Click 2 qua câu hoặc đóng).
/// 6. HỖ TRỢ 2 SLOT TEXT RIÊNG BIỆT:
///    - Slot 1 (Document Subtitle Text): Dùng riêng để hiển thị nội dung trên tờ giấy/nhật ký.
///    - Slot 2 (Player Thought Subtitle Text): Dùng riêng cho câu thoại suy nghĩ của Player ở đáy màn hình sau khi đọc xong!
/// </summary>
public class Inspectable3DPaper : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "";
        [TextArea(2, 4)]
        public string englishDialogue = "";
        [Tooltip("Thời gian tự động qua câu (chỉ áp dụng cho phần thoại suy nghĩ sau khi đọc)")]
        public float holdDuration = 3.5f;
    }

    [Header("1. Đối Tượng Giấy 3D Trước Camera (3D Note Display)")]
    [Tooltip("Kéo GameObject ReadNoteP dưới Camera vào đây")]
    public GameObject holdingPaperModel;

    [Header("2. Tờ Giấy Dưới Sàn/Nệm (Ground Note)")]
    [Tooltip("Tờ giấy dưới sàn cần ẩn đi khi đọc (Để trống sẽ tự động dùng chính đối tượng này)")]
    public GameObject groundNoteObject;

    [Header("3. Hoạt Ảnh Cầm / Hạ Giấy Mượt Mà (Cinematic Animations)")]
    [Tooltip("Bật hoạt ảnh lướt nhẹ từ dưới lên khi nhặt và hạ xuống khi gấp giấy")]
    public bool useSmoothPickupAnimation = true;
    [Tooltip("Thời gian lướt lên khi nhặt giấy (giây - Mặc định: 0.35s)")]
    public float pickupDuration = 0.35f;
    [Tooltip("Thời gian hạ xuống khi cất giấy (giây - Mặc định: 0.25s)")]
    public float putdownDuration = 0.25f;

    [Header("4. Giao Diện UI & Làm Mờ Nền (UI Backdrop)")]
    [Tooltip("Kéo Panel_ReadDocument trên Canvas vào đây")]
    public GameObject readDocumentPanel;

    [Tooltip("Kéo Image Dark_Backdrop vào đây để làm mờ tối không gian đằng sau")]
    public Image darkBackdropImage;

    [Tooltip("Độ mờ tối của nền Dark_Backdrop (0.7 = 70% mờ đen)")]
    [Range(0f, 1f)] public float backdropAlpha = 0.75f;

    [Header("5. Chống Chói Sáng Tờ Giấy (Unlit Shader)")]
    [Tooltip("Tự động chuyển Shader của tờ giấy trên tay sang Unlit để tờ giấy không bị ánh sáng đèn pin làm chói lóa")]
    public bool makePaperUnlit = true;

    [Header("6. Âm Thanh (Audio)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh sột soạt lật mở giấy")]
    public AudioClip paperRustleSound;
    [Tooltip("Âm thanh chạy chữ lách cách (dialogueSound / 8-bit-wavering-text-scroll) - Tự động tìm nếu để trống")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("7. Nội Dung Văn Bản Trên Giấy (Document Content)")]
    public DialogueLine[] documentDialogueLines = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Khu đền Siramori này từng là nơi phong ấn linh hồn oán hận...",
            englishDialogue = "This Siramori shrine was once a place of sealing vengeful spirits..."
        },
        new DialogueLine
        {
            vietnameseDialogue = "Muốn mở được lồng thờ, cần phải tìm đủ 2 chiếc tay quay cơ khí...",
            englishDialogue = "To open the altars, two mechanical crank handles must be found..."
        }
    };

    [Header("8. Thoại Suy Nghĩ Sau Khi Đọc Xong (Post-Read Thoughts)")]
    [Tooltip("Bật câu thoại suy nghĩ của nhân vật sau khi gấp tờ giấy lại")]
    public bool enablePostReadThoughts = true;
    [Tooltip("Khóa di chuyển của Player trong lúc đang suy nghĩ")]
    public bool lockMovementDuringThoughts = false;
    public DialogueLine[] postReadThoughts = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "2 chiếc tay quay cơ khí... Có lẽ chúng nằm rải rác đâu đó trong khu nhà bỏ hoang hoặc xác máy bay.",
            englishDialogue = "Two mechanical crank handles... They might be scattered somewhere in the abandoned houses or plane wreckage.",
            holdDuration = 4.0f
        }
    };

    [Header("9. Cấu Hình 2 Slot Text Riêng Biệt (Text UI Slots)")]
    [Tooltip("Slot 1 (Chữ Nhật Ký): Kéo TextMeshPro trên Panel_ReadDocument vào đây")]
    public TextMeshProUGUI documentSubtitleTextUI;

    [Tooltip("Slot 2 (Thoại Suy Nghĩ): Kéo TextMeshPro phụ đề ở đáy màn hình (UI -> Subtitle -> Subtitle Text) vào đây")]
    public TextMeshProUGUI playerThoughtSubtitleUI;

    [Header("10. Cài Đặt Gõ Chữ (Typewriter Settings)")]
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.035f;
    public bool showBlinkingCursor = true;

    [Header("11. Kích Hoạt Quái Vật Ngoài Cửa Sổ (Window Stalker)")]
    [Tooltip("Kéo con quái death_forest_-_stalker vào đây (Nó sẽ tự động bật dậy khi đọc giấy DÙ BẠN CÓ TẮT DẤU TÍCH LÚC ĐẦU)")]
    public GameObject windowStalkerToActivate;

    [Header("12. Sự Kiện Mở Rộng (Events - Tùy Chọn)")]
    public GameObject nextTriggerToActivate;
    public UnityEvent onReadStart;
    public UnityEvent onReadCompleted;

    // --- Private State ---
    private bool isReading = false;
    private bool isTyping = false;
    private bool isWaitingForNext = false;
    private bool skipRequested = false;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;
    private TextMeshProUGUI activeCurrentTextUI;

    // Lưu lại Transform gốc của ReadNoteP để làm animation
    private Vector3 origLocalPos = Vector3.zero;
    private Quaternion origLocalRot = Quaternion.identity;
    private Vector3 origLocalScale = Vector3.one;
    private bool hasCachedOrigTransform = false;

    void Awake()
    {
        if (groundNoteObject == null) groundNoteObject = this.gameObject;

        CacheOriginalTransform();
        EnsureAudioSource();
        AutoFindDialogueSound();
        FindTextUISlots();

        // Đảm bảo trạng thái ban đầu: Ẩn ReadNoteP và ẩn Panel UI
        if (holdingPaperModel != null) holdingPaperModel.SetActive(false);
        if (readDocumentPanel != null) readDocumentPanel.SetActive(false);
    }

    void Start()
    {
        CacheOriginalTransform();
        EnsureAudioSource();
        AutoFindDialogueSound();
        FindTextUISlots();
    }

    void CacheOriginalTransform()
    {
        if (hasCachedOrigTransform || holdingPaperModel == null) return;
        origLocalPos = holdingPaperModel.transform.localPosition;
        origLocalRot = holdingPaperModel.transform.localRotation;
        origLocalScale = holdingPaperModel.transform.localScale;
        hasCachedOrigTransform = true;
    }

    void FindTextUISlots()
    {
        // 1. Tự tìm Slot 1 (Document Subtitle Text trong Panel)
        if (documentSubtitleTextUI == null && readDocumentPanel != null)
        {
            documentSubtitleTextUI = readDocumentPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // 2. Tự tìm Slot 2 (Player Thought Subtitle Text ở cụm Subtitle chung)
        if (playerThoughtSubtitleUI == null)
        {
            GameObject subContainer = GameObject.Find("Subtitle");
            if (subContainer != null)
            {
                playerThoughtSubtitleUI = subContainer.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (playerThoughtSubtitleUI == null)
            {
                GameObject subObj = GameObject.Find("Subtitle Text") ?? GameObject.Find("SubtitleText");
                if (subObj != null && (documentSubtitleTextUI == null || subObj != documentSubtitleTextUI.gameObject))
                {
                    playerThoughtSubtitleUI = subObj.GetComponent<TextMeshProUGUI>();
                }
            }
        }
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

    void SetupUnlitMaterials()
    {
        if (!makePaperUnlit || holdingPaperModel == null) return;

        Renderer[] renderers = holdingPaperModel.GetComponentsInChildren<Renderer>(true);
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");

        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.receiveShadows = false;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (unlitShader != null)
            {
                foreach (var mat in r.materials)
                {
                    if (mat != null && mat.shader != unlitShader)
                    {
                        Texture mainTex = mat.mainTexture;
                        if (mainTex == null && mat.HasProperty("_BaseMap")) mainTex = mat.GetTexture("_BaseMap");
                        Color mainCol = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;

                        mat.shader = unlitShader;
                        if (mainTex != null)
                        {
                            mat.mainTexture = mainTex;
                            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                        }
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mainCol);
                    }
                }
            }
        }
    }

    void AutoFindDialogueSound()
    {
        if (dialogueSound != null) return;

        // 1. Tìm từ SmartInteractionDialogue
        SmartInteractionDialogue smart = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
        if (smart != null && smart.dialogueSound != null)
        {
            dialogueSound = smart.dialogueSound;
            return;
        }

        // 2. Tìm từ ReadablePaper
        ReadablePaper readable = Object.FindFirstObjectByType<ReadablePaper>(FindObjectsInactive.Include);
        if (readable != null && readable.dialogueSound != null)
        {
            dialogueSound = readable.dialogueSound;
            return;
        }

        // 3. Tìm từ Map03IntroSequence
        Map03IntroSequence intro = Object.FindFirstObjectByType<Map03IntroSequence>(FindObjectsInactive.Include);
        if (intro != null && intro.dialogueBlipSound != null)
        {
            dialogueSound = intro.dialogueBlipSound;
            return;
        }

        // 4. Tìm từ Resources/Sound nếu có
        AudioClip clip = Resources.Load<AudioClip>("Sound/8-bit-wavering-text-scroll");
        if (clip != null) dialogueSound = clip;
    }

    private void SetGroundNoteVisible(bool visible)
    {
        if (groundNoteObject == null) return;

        Renderer[] renderers = groundNoteObject.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null) r.enabled = visible;
        }

        Collider[] colliders = groundNoteObject.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            if (c != null) c.enabled = visible;
        }
    }

    void Update()
    {
        if (!isReading) return;

        // Bấm Chuột Trái, Space hoặc phím E
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                // LẦN BẤM 1 (KHI CHỮ ĐANG GÕ): Hiện ngay toàn bộ văn bản câu hiện tại + dừng âm thanh gõ
                isTyping = false;
                if (activeCurrentTextUI != null) activeCurrentTextUI.text = currentFullText;
                if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
                {
                    audioSource.Stop();
                }
            }
            else if (isWaitingForNext)
            {
                // LẦN BẤM 2 (KHI CHỮ ĐÃ HIỆN ĐỦ): Chuyển câu tiếp theo HOẶC ĐÓNG TÀI LIỆU (OUT)!
                skipRequested = true;
            }
        }
    }

    /// <summary>
    /// Được gọi tự động khi người chơi click Chuột Trái vào tờ giấy 3D trên bàn
    /// </summary>
    public void Interact()
    {
        if (isReading) return;
        StartCoroutine(ReadDocumentSequenceRoutine());
    }

    private IEnumerator ReadDocumentSequenceRoutine()
    {
        isReading = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;
        onReadStart?.Invoke();

        CacheOriginalTransform();
        FindTextUISlots();

        // 1. TẠM KHÓA DI CHUYỂN & GÓC NHÌN CHUỘT
        MovePl playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = true;
            playerMovePl.SetMovementState(false);
        }

        // 2. ẨN TỜ GIẤY DƯỚI NỆM / SÀN
        SetGroundNoteVisible(false);

        // 3. PHÁT ÂM THANH LẬT MỞ GIẤY
        if (paperRustleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(paperRustleSound, soundVolume);
        }

        // 3.1. KÍCH HOẠT QUÁI VẬT NGOÀI CỬA SỔ (NẾU CÓ)
        if (windowStalkerToActivate != null)
        {
            windowStalkerToActivate.SetActive(true);
        }
        else
        {
            WindowStalkerEvent stalkerEvent = Object.FindFirstObjectByType<WindowStalkerEvent>(FindObjectsInactive.Include);
            if (stalkerEvent != null) stalkerEvent.gameObject.SetActive(true);
        }

        // 4. BẬT ACTIVE TOÀN BỘ CÂY CON CỦA ReadNoteP & SETUP UNLIT
        if (holdingPaperModel != null)
        {
            holdingPaperModel.SetActive(true);

            Transform[] allChildren = holdingPaperModel.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child != null) child.gameObject.SetActive(true);
            }

            Renderer[] allRenderers = holdingPaperModel.GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRenderers)
            {
                if (r != null) r.enabled = true;
            }

            SetupUnlitMaterials();
        }

        // 5. BẬT PANEL UI
        if (readDocumentPanel != null)
        {
            readDocumentPanel.SetActive(true);
        }

        // 6. HOẠT ẢNH LƯỚT LÊN MƯỢT MÀ (SMOOTH PICKUP ANIMATION)
        if (useSmoothPickupAnimation && holdingPaperModel != null)
        {
            yield return StartCoroutine(AnimatePickupRoutine());
        }
        else
        {
            if (darkBackdropImage != null)
            {
                darkBackdropImage.gameObject.SetActive(true);
                Color c = darkBackdropImage.color;
                c.a = backdropAlpha;
                darkBackdropImage.color = c;
                darkBackdropImage.raycastTarget = false;
            }
        }

        // 7. CHẠY TỪNG DÒNG VĂN BẢN TRÊN GIẤY (DÙNG SLOT 1: documentSubtitleTextUI)
        if (documentDialogueLines != null && documentDialogueLines.Length > 0)
        {
            foreach (DialogueLine line in documentDialogueLines)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(line, documentSubtitleTextUI, false));
                }
            }
        }

        // 8. HOẠT ẢNH HẠ GIẤY XUỐNG MƯỢT MÀ (SMOOTH PUTDOWN ANIMATION)
        if (useSmoothPickupAnimation && holdingPaperModel != null)
        {
            yield return StartCoroutine(AnimatePutdownRoutine());
        }

        // 9. TẮT ReadNoteP VÀ HIỆN LẠI GIẤY DƯỚI SÀN
        if (holdingPaperModel != null)
        {
            holdingPaperModel.SetActive(false);
            holdingPaperModel.transform.localPosition = origLocalPos;
            holdingPaperModel.transform.localRotation = origLocalRot;
            holdingPaperModel.transform.localScale = origLocalScale;
        }

        SetGroundNoteVisible(true);

        // 10. TẮT DARK BACKDROP VÀ XÓA TEXT SLOT 1
        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        if (darkBackdropImage != null)
        {
            darkBackdropImage.gameObject.SetActive(false);
        }

        if (readDocumentPanel != null)
        {
            readDocumentPanel.SetActive(false);
        }

        if (documentSubtitleTextUI != null)
        {
            documentSubtitleTextUI.text = "";
        }

        // 11. PHÁT TIẾP THOẠI SUY NGHĨ CỦA PLAYER (DÙNG SLOT 2: playerThoughtSubtitleUI)
        if (enablePostReadThoughts && postReadThoughts != null && postReadThoughts.Length > 0)
        {
            // Mở khóa di chuyển nếu không bắt buộc đứng yên lúc nghĩ
            if (!lockMovementDuringThoughts && playerMovePl != null)
            {
                playerMovePl.isCameraLocked = false;
                playerMovePl.SetMovementState(true);
            }

            foreach (DialogueLine thoughtLine in postReadThoughts)
            {
                if (thoughtLine != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(thoughtLine, playerThoughtSubtitleUI, true));
                }
            }
        }

        // 12. MỞ LẠI TOÀN BỘ ĐIỀU KHIỂN SAU KHI SUY NGHĨ XONG
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = false;
            playerMovePl.SetMovementState(true);
        }

        if (playerThoughtSubtitleUI != null)
        {
            playerThoughtSubtitleUI.text = "";
            if (playerThoughtSubtitleUI.transform.parent != null && playerThoughtSubtitleUI.transform.parent.name == "Subtitle")
            {
                playerThoughtSubtitleUI.transform.parent.gameObject.SetActive(false);
            }
        }

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isReading = false;

        // Kích hoạt sự kiện / Trigger tiếp theo (nếu có)
        if (nextTriggerToActivate != null)
        {
            nextTriggerToActivate.SetActive(true);
        }

        onReadCompleted?.Invoke();
        Debug.Log("[Inspectable3DPaper] 📄 Đã hoàn tất đọc tài liệu và chuỗi suy nghĩ!");
    }

    private IEnumerator AnimatePickupRoutine()
    {
        Vector3 startPos = origLocalPos - Vector3.up * 0.18f + Vector3.forward * 0.04f;
        Quaternion startRot = origLocalRot * Quaternion.Euler(18f, 0f, 8f);
        Vector3 startScale = origLocalScale * 0.72f;

        holdingPaperModel.transform.localPosition = startPos;
        holdingPaperModel.transform.localRotation = startRot;
        holdingPaperModel.transform.localScale = startScale;

        if (darkBackdropImage != null)
        {
            darkBackdropImage.gameObject.SetActive(true);
            Color c = darkBackdropImage.color;
            c.a = 0f;
            darkBackdropImage.color = c;
            darkBackdropImage.raycastTarget = false;
        }

        float elapsed = 0f;
        while (elapsed < pickupDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / pickupDuration);

            holdingPaperModel.transform.localPosition = Vector3.Lerp(startPos, origLocalPos, t);
            holdingPaperModel.transform.localRotation = Quaternion.Slerp(startRot, origLocalRot, t);
            holdingPaperModel.transform.localScale = Vector3.Lerp(startScale, origLocalScale, t);

            if (darkBackdropImage != null)
            {
                Color c = darkBackdropImage.color;
                c.a = Mathf.Lerp(0f, backdropAlpha, t);
                darkBackdropImage.color = c;
            }

            yield return null;
        }

        holdingPaperModel.transform.localPosition = origLocalPos;
        holdingPaperModel.transform.localRotation = origLocalRot;
        holdingPaperModel.transform.localScale = origLocalScale;
    }

    private IEnumerator AnimatePutdownRoutine()
    {
        Vector3 endPos = origLocalPos - Vector3.up * 0.2f + Vector3.forward * 0.04f;
        Quaternion endRot = origLocalRot * Quaternion.Euler(18f, 0f, 8f);
        Vector3 endScale = origLocalScale * 0.72f;

        float elapsed = 0f;
        while (elapsed < putdownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / putdownDuration);

            holdingPaperModel.transform.localPosition = Vector3.Lerp(origLocalPos, endPos, t);
            holdingPaperModel.transform.localRotation = Quaternion.Slerp(origLocalRot, endRot, t);
            holdingPaperModel.transform.localScale = Vector3.Lerp(origLocalScale, endScale, t);

            if (darkBackdropImage != null)
            {
                Color c = darkBackdropImage.color;
                c.a = Mathf.Lerp(backdropAlpha, 0f, t);
                darkBackdropImage.color = c;
            }

            yield return null;
        }
    }

    private IEnumerator PlaySingleLineRoutine(DialogueLine line, TextMeshProUGUI targetTextUI, bool isPostThought)
    {
        if (line == null) yield break;

        // Lấy ngôn ngữ hiện tại của game (VI hoặc EN)
        string lang = SettingsManager.currentLanguage;
        currentFullText = (lang == "VI") ? line.vietnameseDialogue : line.englishDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.vietnameseDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.englishDialogue;

        if (string.IsNullOrEmpty(currentFullText)) yield break;

        activeCurrentTextUI = targetTextUI;
        if (activeCurrentTextUI != null)
        {
            if (activeCurrentTextUI.transform.parent != null)
            {
                activeCurrentTextUI.transform.parent.gameObject.SetActive(true);
            }
            activeCurrentTextUI.gameObject.SetActive(true);
            Color sc = activeCurrentTextUI.color;
            sc.a = 1f;
            activeCurrentTextUI.color = sc;
            activeCurrentTextUI.text = "";
        }

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        skipRequested = false;

        // Âm thanh chạy chữ dialogueSound (8-bit-wavering-text-scroll)
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

        // Hiệu ứng Typewriter gõ từng chữ
        if (useTypewriterEffect && activeCurrentTextUI != null)
        {
            isTyping = true;

            for (int i = 0; i <= currentFullText.Length; i++)
            {
                if (!isTyping || skipRequested) break;
                string typed = currentFullText.Substring(0, i);
                if (showBlinkingCursor) typed += "_";
                activeCurrentTextUI.text = typed;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            activeCurrentTextUI.text = currentFullText;
            isTyping = false;
        }
        else if (activeCurrentTextUI != null)
        {
            activeCurrentTextUI.text = currentFullText;
        }

        if (activeCurrentTextUI != null) activeCurrentTextUI.text = currentFullText;

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
        {
            StartCoroutine(FadeAudioOutRoutine(audioSource, 0.08f));
        }

        // Con trỏ nhấp nháy sau khi gõ xong câu
        if (showBlinkingCursor && activeCurrentTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(activeCurrentTextUI, currentFullText));
        }

        // XỬ LÝ CHỜ ĐỌC:
        if (!isPostThought)
        {
            // Với nội dung trên giấy: KHÔNG TỰ TẮT, đợi người chơi click
            isWaitingForNext = true;
            skipRequested = false;
            yield return new WaitForSeconds(0.12f);

            while (!skipRequested)
            {
                yield return null;
            }

            isWaitingForNext = false;
            skipRequested = false;
        }
        else
        {
            // Với suy nghĩ của Player sau khi đọc: Tự qua sau holdDuration hoặc click để qua nhanh
            isWaitingForNext = true;
            skipRequested = false;
            float holdTime = (line.holdDuration > 0f) ? line.holdDuration : 3.5f;
            float timer = 0f;

            while (timer < holdTime && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            isWaitingForNext = false;
            skipRequested = false;
        }

        // DỪNG CON TRỎ NHẤP NHÁY NGAY KHI KẾT THÚC CÂU NÀY
        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }
        if (activeCurrentTextUI != null)
        {
            activeCurrentTextUI.text = "";
        }
    }

    private IEnumerator FadeAudioOutRoutine(AudioSource aSource, float duration)
    {
        if (aSource == null) yield break;
        float startVol = aSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            aSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        aSource.Stop();
        aSource.volume = soundVolume;
    }

    private IEnumerator BlinkCursorRoutine(TextMeshProUGUI textUI, string baseText)
    {
        bool showCursor = true;
        while (true)
        {
            if (textUI != null)
            {
                textUI.text = showCursor ? (baseText + "_") : baseText;
            }
            showCursor = !showCursor;
            yield return new WaitForSeconds(0.45f);
        }
    }
}
