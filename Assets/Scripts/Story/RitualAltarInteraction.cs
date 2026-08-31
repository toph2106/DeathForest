using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Script quản lý chuỗi nghi lễ trên bệ đá (Map 04):
/// 
/// [GIAI ĐOẠN 1]: Dâng 5 bộ phận thi thể
/// - Kiểm tra người chơi có đủ 5 món bộ phận không.
/// - Thiếu -> Hiện thoại nhắc nhở cần tìm đủ 5 bộ phận.
/// - Đủ -> Fade đen màn hình, xóa 5 món khỏi Inventory, fade sáng lại và hiện thoại nhắc cần dán lá bùa.
/// 
/// [GIAI ĐOẠN 2]: Dán lá bùa trắng lên xác
/// - Chưa có lá bùa trong túi -> Hiện thoại nhắc cần có lá bùa.
/// - Đã có lá bùa -> Fade đen màn hình + phát âm thanh dán bùa, trừ lá bùa trong túi, bật hiện lá bùa trắng trên xác,
///   fade sáng lại và hiện thoại nhắc bước tiếp theo (kích hoạt phong ấn máu).
/// 
/// [GIAI ĐOẠN 3]: Kích hoạt phong ấn máu (Đổi sang Material bùa đỏ có chữ)
/// - Tương tác lần nữa -> Fade đen màn hình + phát âm thanh ma pháp, đổi Material lá bùa sang Material bùa đỏ (Material.004),
///   fade sáng lại, hiện câu thoại cuối cùng: "Cuối cùng cũng xong, mình mong nó hoạt động" và kích hoạt onRitualFullyCompleted!
/// </summary>
public class RitualAltarInteraction : MonoBehaviour, IInteractable
{
    public static RitualAltarInteraction Instance { get; private set; }

    public enum RitualStage
    {
        Stage1_NeedBodyParts = 0,        // Cần 5 bộ phận thi thể
        Stage2_NeedTalisman = 1,         // Đã đặt 5 bộ phận, cần dán lá bùa trắng
        Stage3_NeedActivateBloodSeal = 2,// Đã dán bùa trắng, cần tương tác để kích hoạt phong ấn máu
        Completed = 3                    // Đã hoàn tất toàn bộ nghi thức
    }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 3)]
        public string vietnameseDialogue = "";
        [TextArea(2, 3)]
        public string englishDialogue = "";
        public float holdDuration = 3.5f;
    }

    [Header("=== TRẠNG THÁI HIỆN TẠI ===")]
    public RitualStage currentStage = RitualStage.Stage1_NeedBodyParts;

    [Header("=== GIAI ĐOẠN 1: CẤU HÌNH 5 BỘ PHẬN THI THỂ ===")]
    [Tooltip("Danh sách tên 5 món đồ bộ phận cần có trong túi đồ (Không phân biệt hoa thường)")]
    public string[] requiredBodyPartNames = new string[]
    {
        "Head",
        "ArmsL",
        "ArmsR",
        "FeetL",
        "FeetR"
    };

    [Tooltip("Thoại khi CHƯA ĐỦ 5 bộ phận thi thể")]
    public DialogueLine[] missingBodyPartsDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Một bệ đá hiến tế cổ xưa... Dường như cần phải thu thập đủ 5 bộ phận thi thể bị nguyền rủa để đặt lên đây.",
            englishDialogue = "An ancient sacrificial altar... It seems necessary to gather all 5 cursed body parts to place here.",
            holdDuration = 4.0f
        }
    };

    [Tooltip("Thoại SAU KHI ĐẶT ĐỦ 5 bộ phận thành công (nhắc dán bùa)")]
    public DialogueLine[] placedBodyPartsSuccessDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Các bộ phận thi thể đã được xếp lên bệ đá... Bây giờ cần một lá bùa để dán lên phong ấn tà thuật này.",
            englishDialogue = "The body parts have been placed on the altar... Now a talisman is needed to seal this dark ritual.",
            holdDuration = 4.5f
        }
    };

    [Header("=== GIAI ĐOẠN 2: CẤU HÌNH DÁN LÁ BÙA TRẮNG ===")]
    [Tooltip("Tên vật phẩm lá bùa trong Inventory")]
    public string requiredTalismanName = "Bua";
    [Tooltip("Các tên phụ của bùa (cách nhau bởi dấu phẩy)")]
    public string alternateTalismanNames = "Bùa,Talisman,Charm,Paper,bua,bùa,talisman,charm,paper";
    [Tooltip("Kéo GameObject lá bùa được đặt sẵn trên xác vào đây (Ban đầu tự ẩn, khi dán bùa sẽ tự hiện lên)")]
    public GameObject placedTalismanOnCorpseObject;
    [Tooltip("Có xóa/tiêu hao lá bùa sau khi dán không? (Mặc định: BẬT)")]
    public bool consumeTalismanOnUse = true;

    [Tooltip("Thoại khi CHƯA CÓ LÁ BÙA TRONG TÚI")]
    public DialogueLine[] missingTalismanDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Các bộ phận thi thể đã được xếp lên bệ đá... Mình cần một lá bùa để dán lên phong ấn tà thuật.",
            englishDialogue = "The body parts are on the altar... I need a talisman to seal this dark magic.",
            holdDuration = 4.0f
        }
    };

    [Tooltip("Thoại SAU KHI DÁN LÁ BÙA TRẮNG THÀNH CÔNG (nhắc kích hoạt)")]
    public DialogueLine[] talismanPlacedSuccessDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Lá bùa trắng đã được đặt lên thi thể... Bây giờ hãy chạm vào để kích hoạt phong ấn máu.",
            englishDialogue = "The talisman paper has been placed... Now touch it to activate the blood seal.",
            holdDuration = 4.5f
        }
    };

    [Tooltip("Âm thanh dán bùa")]
    public AudioClip talismanPlaceSound;

    [Header("=== GIAI ĐOẠN 3: KÍCH HOẠT PHONG ẤN MÁU & ĐỔI MATERIAL ===")]
    [Tooltip("Kéo Material bùa đỏ có chữ vẽ máu (Material.004) vào đây để tự động đổi màu bùa sau khi kích hoạt")]
    public Material activatedTalismanMaterial;

    [Tooltip("Hoặc kéo GameObject lá bùa đỏ (nếu bạn dùng 2 GameObject riêng biệt thay vì đổi Material)")]
    public GameObject activatedTalismanObject;

    [Tooltip("Thoại SAU KHI KÍCH HOẠT PHONG ẤN MÁU THÀNH CÔNG (Câu thoại kết thúc)")]
    public DialogueLine[] ritualCompletedDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Cuối cùng cũng xong, mình mong nó hoạt động.",
            englishDialogue = "Finally it's done, I hope it works.",
            holdDuration = 4.5f
        }
    };

    [Tooltip("Âm thanh kích hoạt phong ấn máu / ma pháp")]
    public AudioClip bloodSealActivateSound;

    [Header("=== GIAI ĐOẠN ĐÃ HOÀN TẤT ===")]
    [Tooltip("Thoại khi nghi lễ đã hoàn tất từ trước mà người chơi bấm vào lại")]
    public DialogueLine[] alreadyCompletedDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Nghi lễ hiến tế trên bệ đá đã hoàn tất.",
            englishDialogue = "The sacrificial ritual on the altar is already complete.",
            holdDuration = 3.0f
        }
    };

    [Header("=== GIAI ĐOẠN 4: CHOÁNG VÁNG NGẤT XỈU (FAINT) ===")]
    [Tooltip("Tự động ngất xỉu và kích hoạt chuỗi tỉnh dậy sau khi hoàn tất bùa (Mặc định: BẬT)")]
    public bool triggerFaintAfterRitual = true;
    [Tooltip("Thời gian chờ sau khi hết câu thoại bùa trước khi bắt đầu ngất xỉu (Mặc định: 3 giây)")]
    public float delayBeforeFaint = 3.0f;

    [Tooltip("Thoại lúc bắt đầu cảm thấy choáng váng")]
    public DialogueLine[] faintDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Chuyện gì thế...",
            englishDialogue = "What is happening...",
            holdDuration = 2.5f
        }
    };

    [Tooltip("Thời gian camera lắc lư choáng váng (giây)")]
    public float dizzyDuration = 2.5f;
    [Tooltip("Cường độ lắc lư")]
    public float dizzyShakeIntensity = 0.4f;
    [Tooltip("Thời gian đổ gục xuống đất (giây)")]
    public float collapseDuration = 1.2f;

    [Tooltip("Âm thanh tim đập / choáng váng")]
    public AudioClip faintHeartbeatAudio;
    [Tooltip("Âm thanh ngã gục / bodyfall")]
    public AudioClip faintCollapseAudio;
    [Range(0f, 1f)] public float faintAudioVolume = 0.9f;

    [Header("=== GIAI ĐOẠN 5: CẮT CẢNH TỈNH DẬY (WAKE-UP SEQUENCE) ===")]
    [Tooltip("Kéo PointSpawn (Object vị trí & góc nằm ngửa nhìn trời) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM PointSpawn trong Scene!")]
    public Transform pointSpawn;
    [Tooltip("Vị trí đứng của Player sau khi thức dậy. Để trống sẽ tự dùng vị trí gốc của Player!")]
    public Transform playerStandTransform;
    [Tooltip("Độ cao mắt đứng của Main Camera trong Player (Mặc định: 2.5)")]
    public float standingCameraLocalY = 2.5f;
    [Tooltip("Thời gian màn hình từ đen xì sáng dần lên mượt mà (giây - Mặc định: 2.5s)")]
    public float wakeUpFadeInDuration = 2.5f;
    [Tooltip("Biên độ ngó nghiêng lơ mơ sang 2 bên lúc nhìn lên trời (độ - Mặc định: 7.5 độ)")]
    public float groggySwayAngle = 7.5f;

    [Header("--- Đợt Thoại 1: Nằm Ngửa Nhìn Bầu Trời ---")]
    [Tooltip("Câu đầu tiên sẽ chạy ngay lúc đang Fade sáng dần lên. Các câu tiếp theo sẽ chạy sau khi Fade xong!")]
    public DialogueLine[] firstWakeUpDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Ưm...",
            englishDialogue = "Ugh...",
            holdDuration = 3.0f
        },
        new DialogueLine
        {
            vietnameseDialogue = "Trời đêm nay...",
            englishDialogue = "The night sky...",
            holdDuration = 3.0f
        }
    };

    [Header("--- Đợt Thoại 2: Ngồi Dậy 45 Độ ---")]
    [Tooltip("Thời gian camera xoay về 45 độ và nâng cao dần lên như đang ngồi dậy (giây - Mặc định: 2.5s)")]
    public float sitUpDuration = 2.5f;
    [Tooltip("Góc ngửa của camera lúc ngồi dậy (độ - Mặc định: -45 độ nhìn chéo lên tán cây)")]
    public float sitUpPitchAngle = -45.0f;
    public DialogueLine[] sittingUpDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Mình... vừa ngất đi sao?",
            englishDialogue = "Did I... just faint?",
            holdDuration = 3.5f
        }
    };

    [Header("--- Đợt Thoại 3: Đứng Dậy Nhìn Xung Quanh ---")]
    [Tooltip("Thời gian camera bay mượt mà về đúng vị trí mắt đứng chuẩn (0, 2.5, 0) trong Player (giây - Mặc định: 2.0s)")]
    public float standUpDuration = 2.0f;
    public DialogueLine[] standingUpDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Phù... may mà tỉnh lại được. Phải kiểm tra xem xung quanh thế nào.",
            englishDialogue = "Phew... luckily I woke up. I need to check around.",
            holdDuration = 3.5f
        }
    };

    [Header("--- Âm Thanh Tỉnh Dậy ---")]
    [Tooltip("Tiếng thở dốc / choáng váng lúc vừa mở mắt")]
    public AudioClip breathWakeUpAudio;
    [Tooltip("Tiếng sột soạt quần áo/lá khô khi ngồi dậy")]
    public AudioClip rustlingGroundAudio;


    [Header("=== CẤU HÌNH FADE MÀN HÌNH (CINEMA FADE) ===")]
    public Image fadeImage;
    public Color fadeColor = Color.black;
    public float fadeInDuration = 0.6f;
    public float holdBlackDuration = 1.6f;
    public float fadeOutDuration = 0.6f;

    [Header("=== CẤU HÌNH PHỤ ĐỀ TYPEWRITER ===")]
    public TextMeshProUGUI subtitleTextUI;
    public bool useTypewriter = true;
    public float typewriterSpeed = 0.03f;
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("=== SỰ KIỆN UNITY EVENTS ===")]
    public UnityEvent onStage1BodyPartsPlaced;
    public UnityEvent onStage2TalismanPlaced;
    public UnityEvent onRitualFullyCompleted;
    public UnityEvent onFaintCompleted;

    // --- Private Fields ---
    private AudioSource audioSource;
    private bool isInteracting = false;
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private bool skipWaitRequested = false;
    private float interactStartTime = 0f;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;

    void EnsureCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            Debug.Log($"[RitualAltar] 📦 Tự động thêm BoxCollider cho '{gameObject.name}'!");
        }
    }

    void Awake()
    {
        Instance = this;
        EnsureCollider();
        EnsureAudioSource();
        AutoFindDialogueSound();
        FindSubtitleUI();
        EnsureFadeImage();

        InitializeTalismanObjectsState();
    }

    void Start()
    {
        EnsureCollider();
        EnsureAudioSource();
        AutoFindDialogueSound();
        FindSubtitleUI();
        EnsureFadeImage();

        InitializeTalismanObjectsState();
    }

    private void InitializeTalismanObjectsState()
    {
        if (currentStage < RitualStage.Stage3_NeedActivateBloodSeal)
        {
            if (placedTalismanOnCorpseObject != null)
            {
                placedTalismanOnCorpseObject.SetActive(false);
            }
            if (activatedTalismanObject != null && activatedTalismanObject != placedTalismanOnCorpseObject)
            {
                activatedTalismanObject.SetActive(false);
            }
        }
        else if (currentStage == RitualStage.Stage3_NeedActivateBloodSeal)
        {
            if (placedTalismanOnCorpseObject != null)
            {
                placedTalismanOnCorpseObject.SetActive(true);
            }
            if (activatedTalismanObject != null && activatedTalismanObject != placedTalismanOnCorpseObject)
            {
                activatedTalismanObject.SetActive(false);
            }
        }
        else if (currentStage == RitualStage.Completed)
        {
            if (placedTalismanOnCorpseObject != null)
            {
                placedTalismanOnCorpseObject.SetActive(true);
                if (activatedTalismanMaterial != null)
                {
                    ApplyActivatedMaterial(placedTalismanOnCorpseObject);
                }
            }

            if (activatedTalismanObject != null && activatedTalismanObject != placedTalismanOnCorpseObject)
            {
                if (placedTalismanOnCorpseObject != null) placedTalismanOnCorpseObject.SetActive(false);
                activatedTalismanObject.SetActive(true);
                if (activatedTalismanMaterial != null)
                {
                    ApplyActivatedMaterial(activatedTalismanObject);
                }
            }
        }
    }

    void Update()
    {
        if (!isInteracting) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                // BẤM LẦN 1 KHI ĐANG GÕ: Hiện toàn bộ chữ câu này ngay lập tức
                skipRequested = true;
            }
            else if (isWaitingForNextLine)
            {
                // BẤM LẦN 2 KHI CHỮ ĐÃ ĐẦY ĐỦ: Chuyển câu tiếp theo hoặc kết thúc
                skipWaitRequested = true;
            }
        }
    }

    // =========================================================================
    // TƯƠNG TÁC CHÍNH (IInteractable)
    // =========================================================================

    public void Interact()
    {
        if (isInteracting || SmartInteractionDialogue.isAnyDialoguePlaying)
        {
            return;
        }

        Debug.Log($"[RitualAltar] ⚡ Player click tương tác vào Bệ Đá! Stage: {currentStage}");

        switch (currentStage)
        {
            case RitualStage.Stage1_NeedBodyParts:
                HandleStage1Interaction();
                break;

            case RitualStage.Stage2_NeedTalisman:
                HandleStage2Interaction();
                break;

            case RitualStage.Stage3_NeedActivateBloodSeal:
                HandleStage3Interaction();
                break;

            case RitualStage.Completed:
                StartCoroutine(PlaySimpleDialogueSequence(alreadyCompletedDialogues));
                break;
        }
    }

    // =========================================================================
    // GIAI ĐOẠN 1: ĐẶT 5 BỘ PHẬN THI THỂ
    // =========================================================================

    private void HandleStage1Interaction()
    {
        bool hasAll5Parts = CheckHasAllBodyParts();

        if (hasAll5Parts)
        {
            Debug.Log("[RitualAltar] 🌟 ĐÃ CÓ ĐỦ 5 BỘ PHẬN! Bắt đầu nghi thức Stage 1...");
            StartCoroutine(Stage1SequenceRoutine());
        }
        else
        {
            Debug.Log("[RitualAltar] ⚠️ Chưa đủ 5 bộ phận. Hiện thoại nhắc nhở...");
            StartCoroutine(PlaySimpleDialogueSequence(missingBodyPartsDialogues));
        }
    }

    private bool CheckHasAllBodyParts()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[RitualAltar] ⚠️ Không tìm thấy InventoryManager.Instance!");
            return false;
        }

        if (requiredBodyPartNames == null || requiredBodyPartNames.Length == 0) return true;

        bool allFound = true;
        foreach (string partName in requiredBodyPartNames)
        {
            if (string.IsNullOrEmpty(partName)) continue;
            string trimmedName = partName.Trim();
            bool found = InventoryManager.Instance.HasItem(trimmedName);
            Debug.Log($"[RitualAltar] 🔍 Kiểm tra bộ phận '{trimmedName}': {(found ? "✅ ĐÃ CÓ" : "❌ THIẾU")}");
            if (!found)
            {
                allFound = false;
            }
        }
        return allFound;
    }

    private void ConsumeAllBodyParts()
    {
        if (InventoryManager.Instance == null || requiredBodyPartNames == null) return;

        foreach (string partName in requiredBodyPartNames)
        {
            if (string.IsNullOrEmpty(partName)) continue;
            string trimmedName = partName.Trim();
            InventoryManager.Instance.RemoveItem(trimmedName);
            Debug.Log($"[RitualAltar] 🩸 Đã tiêu hao bộ phận: '{trimmedName}'");
        }
    }

    private IEnumerator Stage1SequenceRoutine()
    {
        isInteracting = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        MovePl playerMovePl = LockPlayer();

        // 1. Fade đen màn hình
        yield return StartCoroutine(FadeScreenRoutine(0f, 1f, fadeInDuration));

        // 2. Tiêu hao 5 bộ phận trong lúc màn hình đen
        ConsumeAllBodyParts();

        yield return new WaitForSeconds(holdBlackDuration);

        // 3. Fade sáng trở lại
        yield return StartCoroutine(FadeScreenRoutine(1f, 0f, fadeOutDuration));

        // 4. Chuyển sang giai đoạn 2 (Cần lá bùa)
        currentStage = RitualStage.Stage2_NeedTalisman;
        onStage1BodyPartsPlaced?.Invoke();

        interactStartTime = Time.unscaledTime;
        isTyping = false;
        skipRequested = false;
        skipWaitRequested = false;
        yield return null;

        // 5. Hiện thoại nhắc dán bùa
        yield return StartCoroutine(PlayDialogueListRoutine(placedBodyPartsSuccessDialogues));

        // Cooldown 0.5s chống spam click
        yield return new WaitForSeconds(0.5f);

        UnlockPlayer(playerMovePl);
        isInteracting = false;
        SmartInteractionDialogue.isAnyDialoguePlaying = false;
    }

    // =========================================================================
    // GIAI ĐOẠN 2: KIỂM TRA LÁ BÙA & DÁN LÁ BÙA TRẮNG
    // =========================================================================

    private void HandleStage2Interaction()
    {
        string matchedTalisman = FindItemInInventory(requiredTalismanName, alternateTalismanNames);
        bool hasTalisman = !string.IsNullOrEmpty(matchedTalisman);

        if (hasTalisman)
        {
            StartCoroutine(Stage2SequenceRoutine(matchedTalisman));
        }
        else
        {
            // Chưa có lá bùa trong người -> Nhắc nhở tìm bùa
            StartCoroutine(PlaySimpleDialogueSequence(missingTalismanDialogues));
        }
    }

    private IEnumerator Stage2SequenceRoutine(string matchedTalismanName)
    {
        isInteracting = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        MovePl playerMovePl = LockPlayer();

        // 1. Fade đen màn hình
        yield return StartCoroutine(FadeScreenRoutine(0f, 1f, fadeInDuration));

        // 2. Phát âm thanh dán bùa
        if (talismanPlaceSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(talismanPlaceSound, soundVolume);
        }

        // 3. Tiêu hao lá bùa sau khi dùng
        if (consumeTalismanOnUse && InventoryManager.Instance != null && !string.IsNullOrEmpty(matchedTalismanName))
        {
            InventoryManager.Instance.RemoveItem(matchedTalismanName);
            Debug.Log($"[RitualAltar] 📜 Đã tiêu hao lá bùa: '{matchedTalismanName}'");
        }

        // 4. Bật Active lá bùa dán trên xác
        if (placedTalismanOnCorpseObject != null)
        {
            placedTalismanOnCorpseObject.SetActive(true);
            Debug.Log("[RitualAltar] 📜 Đã dán lá bùa trắng lên xác!");
        }

        yield return new WaitForSeconds(holdBlackDuration);

        // 5. Fade sáng trở lại
        yield return StartCoroutine(FadeScreenRoutine(1f, 0f, fadeOutDuration));

        // 6. Chuyển sang giai đoạn 3 (Cần kích hoạt phong ấn máu)
        currentStage = RitualStage.Stage3_NeedActivateBloodSeal;
        onStage2TalismanPlaced?.Invoke();

        interactStartTime = Time.unscaledTime;
        isTyping = false;
        skipRequested = false;
        skipWaitRequested = false;
        yield return null;

        // 7. Hiện thoại sau khi dán bùa trắng
        yield return StartCoroutine(PlayDialogueListRoutine(talismanPlacedSuccessDialogues));

        // Cooldown 0.5s chống spam click
        yield return new WaitForSeconds(0.5f);

        UnlockPlayer(playerMovePl);
        isInteracting = false;
        SmartInteractionDialogue.isAnyDialoguePlaying = false;
    }

    // =========================================================================
    // GIAI ĐOẠN 3: KÍCH HOẠT PHONG ẤN MÁU & ĐỔI MATERIAL
    // =========================================================================

    private void HandleStage3Interaction()
    {
        StartCoroutine(Stage3SequenceRoutine());
    }

    private IEnumerator Stage3SequenceRoutine()
    {
        isInteracting = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        MovePl playerMovePl = LockPlayer();

        // 1. Fade đen màn hình
        yield return StartCoroutine(FadeScreenRoutine(0f, 1f, fadeInDuration));

        // 2. Phát âm thanh kích hoạt ma pháp
        if (bloodSealActivateSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bloodSealActivateSound, soundVolume);
        }

        // 3. Đổi Material lá bùa sang Material bùa đỏ (Material.004)
        if (placedTalismanOnCorpseObject != null && activatedTalismanMaterial != null)
        {
            ApplyActivatedMaterial(placedTalismanOnCorpseObject);
            Debug.Log("[RitualAltar] 🩸 Đã đổi Material lá bùa sang Material máu đỏ!");
        }

        // Hoặc nếu người chơi dùng 2 GameObject riêng biệt khác nhau
        if (activatedTalismanObject != null && activatedTalismanObject != placedTalismanOnCorpseObject)
        {
            if (placedTalismanOnCorpseObject != null) placedTalismanOnCorpseObject.SetActive(false);
            activatedTalismanObject.SetActive(true);
            if (activatedTalismanMaterial != null) ApplyActivatedMaterial(activatedTalismanObject);
            Debug.Log("[RitualAltar] 🩸 Đã kích hoạt GameObject bùa đỏ riêng biệt!");
        }

        yield return new WaitForSeconds(holdBlackDuration);

        // 4. Fade sáng trở lại
        yield return StartCoroutine(FadeScreenRoutine(1f, 0f, fadeOutDuration));

        // 5. Đánh dấu hoàn tất toàn bộ nghi lễ
        currentStage = RitualStage.Completed;
        onRitualFullyCompleted?.Invoke();
        Debug.Log("[RitualAltar] 🌟 TOÀN BỘ NGHI LỄ ĐÃ HOÀN TẤT 100%!");

        interactStartTime = Time.unscaledTime;
        isTyping = false;
        skipRequested = false;
        skipWaitRequested = false;
        yield return null;

        // 6. Hiện câu thoại kết thúc ("Cuối cùng cũng xong, mình mong nó hoạt động.")
        yield return StartCoroutine(PlayDialogueListRoutine(ritualCompletedDialogues));

        // Cooldown 0.5s chống spam click
        yield return new WaitForSeconds(0.5f);

        UnlockPlayer(playerMovePl);
        isInteracting = false;
        SmartInteractionDialogue.isAnyDialoguePlaying = false;

        // 7. BẮT ĐẦU CHUỖI CHOÁNG VÁNG NGẤT XỈU VÀ TỈNH DẬY
        if (triggerFaintAfterRitual)
        {
            StartCoroutine(FaintAndWakeUpRoutine());
        }
    }

    private IEnumerator FaintAndWakeUpRoutine()
    {
        Debug.Log($"[RitualAltar] ⏱️ Chờ {delayBeforeFaint}s sau câu thoại bùa trước khi bắt đầu ngất xỉu...");
        yield return new WaitForSeconds(delayBeforeFaint);

        Debug.Log("[RitualAltar] 😵 BẮT ĐẦU HIỆU ỨNG CHOÁNG VÁNG NGẤT XỈU VÀ TỈNH DẬY!");

        isInteracting = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        MovePl player = Object.FindFirstObjectByType<MovePl>();
        Transform camTrans = (player != null && player.cameraTransform != null) ? player.cameraTransform : (Camera.main != null ? Camera.main.transform : null);

        // Khóa di chuyển và góc nhìn của người chơi
        if (player != null)
        {
            player.SetMovementState(false);
            player.isCameraLocked = true;
        }

        // Tự động tìm âm thanh bodyfall nếu chưa có
        if (faintCollapseAudio == null)
        {
            CockroachNightmareWakeUp cockroach = Object.FindFirstObjectByType<CockroachNightmareWakeUp>(FindObjectsInactive.Include);
            if (cockroach != null && cockroach.faintCollapseAudio != null)
            {
                faintCollapseAudio = cockroach.faintCollapseAudio;
            }
        }

        // PHÁT CÂU THOẠI: "Chuyện gì thế..."
        if (faintDialogues != null && faintDialogues.Length > 0)
        {
            StartCoroutine(PlayDialogueListRoutine(faintDialogues));
        }

        // Phát âm thanh tim đập / choáng váng nếu có
        if (faintHeartbeatAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(faintHeartbeatAudio, faintAudioVolume);
        }

        EnsureFadeImage();

        // GIAI ĐOẠN 1: CAMERA LẮC LƯ CHOÁNG VÁNG (DIZZY SWAY & BLUR BLINK)
        if (camTrans != null)
        {
            Vector3 startLocalPos = camTrans.localPosition;
            Quaternion startLocalRot = camTrans.localRotation;

            float dizzyElapsed = 0f;
            while (dizzyElapsed < dizzyDuration)
            {
                dizzyElapsed += Time.deltaTime;
                float t = dizzyElapsed;

                // Lắc lư chao đảo nhẹ nhàng, êm ái tự nhiên
                float pitch = Mathf.Sin(t * 1.8f) * 2.0f * dizzyShakeIntensity;
                float yaw = Mathf.Cos(t * 1.4f) * 2.5f * dizzyShakeIntensity;
                float roll = Mathf.Sin(t * 2.0f) * 3.5f * dizzyShakeIntensity;

                camTrans.localRotation = startLocalRot * Quaternion.Euler(pitch, yaw, roll);

                // Thị lực mờ dần nhấp nháy đen nhẹ
                if (fadeImage != null)
                {
                    fadeImage.gameObject.SetActive(true);
                    float blinkAlpha = Mathf.PingPong(t * 1.5f, 0.45f);
                    fadeImage.color = new Color(0f, 0f, 0f, blinkAlpha);
                }

                yield return null;
            }

            // GIAI ĐOẠN 2: ĐỔ SẬP NGÃ GỤC XUỐNG SÀN (COLLAPSE TO FLOOR) + FADE ĐEN DẦN
            Vector3 preCollapsePos = camTrans.localPosition;
            Quaternion preCollapseRot = camTrans.localRotation;
            Vector3 floorCollapsePos = new Vector3(preCollapsePos.x, 0.2f, preCollapsePos.z);
            Quaternion floorCollapseRot = preCollapseRot * Quaternion.Euler(15f, 25f, 75f); // Áp má xuống sàn

            float collapseElapsed = 0f;
            while (collapseElapsed < collapseDuration)
            {
                collapseElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(collapseElapsed / collapseDuration);
                float easeInT = t * t; // Gia tốc rơi nhanh dần

                camTrans.localPosition = Vector3.Lerp(preCollapsePos, floorCollapsePos, easeInT);
                camTrans.localRotation = Quaternion.Slerp(preCollapseRot, floorCollapseRot, easeInT);

                if (fadeImage != null)
                {
                    fadeImage.gameObject.SetActive(true);
                    fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.3f, 1f, t));
                }

                yield return null;
            }
        }

        // GIAI ĐOẠN 3: MÀN HÌNH ĐEN XÌ HOÀN TOÀN (100% BLACKOUT)
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = Color.black;
        }

        // PHÁT TIẾNG "BỊCH" NẶNG TRỊCH NGAY KHI MÀN HÌNH VỪA ĐEN XÌ HOÀN TOÀN!
        if (faintCollapseAudio != null)
        {
            Vector3 soundPos = (Camera.main != null) ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(faintCollapseAudio, soundPos, faintAudioVolume);
            Debug.Log($"[RitualAltar] 🔊 ĐÃ PHÁT ÂM THANH BỊCH/NGÃ GỤC: {faintCollapseAudio.name} (Âm lượng: {faintAudioVolume})");
        }

        // Chờ 1.5s hồi hộp trong bóng tối sau tiếng bịch
        yield return new WaitForSeconds(1.5f);

        onFaintCompleted?.Invoke();

        // =====================================================================
        // GIAI ĐOẠN 5: CẮT CẢNH TỈNH DẬY ĐỒNG BỘ 100% INTRO
        // =====================================================================
        Debug.Log("[RitualAltar] 🌅 BẮT ĐẦU CẮT CẢNH TỈNH DẬY (WAKE-UP SEQUENCE)!");

        AutoFindIntroAssets();

        // 1. Xác định vị trí đứng đích (targetPlayerWorldPos & targetPlayerWorldRot)
        Vector3 targetPlayerWorldPos;
        Quaternion targetPlayerWorldRot;

        if (playerStandTransform != null)
        {
            targetPlayerWorldPos = playerStandTransform.position;
            targetPlayerWorldRot = playerStandTransform.rotation;
        }
        else if (player != null)
        {
            targetPlayerWorldPos = player.transform.position;
            targetPlayerWorldRot = player.transform.rotation;
        }
        else
        {
            targetPlayerWorldPos = transform.position;
            targetPlayerWorldRot = Quaternion.identity;
        }

        // 2. Dịch chuyển Player đến vị trí đứng đích
        CharacterController cc = (player != null) ? player.GetComponent<CharacterController>() : null;
        if (player != null)
        {
            if (cc != null) cc.enabled = false;
            player.transform.position = targetPlayerWorldPos;
            player.transform.rotation = targetPlayerWorldRot;
            if (cc != null) cc.enabled = true;
        }

        // 3. Đặt Camera tại vị trí & góc nằm ngửa nhìn trời (PointSpawn)
        Vector3 lyingWorldPos = (pointSpawn != null) ? pointSpawn.position : (targetPlayerWorldPos + new Vector3(0f, 0.25f, 0f));
        Quaternion lyingWorldRot = (pointSpawn != null) ? pointSpawn.rotation : Quaternion.Euler(-90f, targetPlayerWorldRot.eulerAngles.y, 0f);

        if (camTrans != null)
        {
            camTrans.position = lyingWorldPos;
            camTrans.rotation = lyingWorldRot;
        }

        // Màn hình đen kịt ban đầu
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = Color.black;
        }

        // Chờ 0.8s trong bóng tối
        yield return new WaitForSeconds(0.8f);

        // --- ĐỢT 1: MÀN HÌNH SÁNG DẦN LÊN + THỞ DỐC + THOẠI 1 (NHÌN TRỜI) ---
        if (breathWakeUpAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(breathWakeUpAudio, soundVolume);
        }

        // Chạy câu thoại đầu tiên của Đợt 1 ngay lúc đang Fade sáng
        Coroutine firstLineCoroutine = null;
        if (firstWakeUpDialogues != null && firstWakeUpDialogues.Length > 0 && firstWakeUpDialogues[0] != null)
        {
            firstLineCoroutine = StartCoroutine(PlaySingleLineRoutine(firstWakeUpDialogues[0]));
        }

        // Fade sáng từ từ (1.0 -> 0.0)
        yield return StartCoroutine(FadeScreenRoutine(1f, 0f, wakeUpFadeInDuration));

        // Chờ câu thoại đầu tiên kết thúc nếu người chơi chưa click qua
        if (firstLineCoroutine != null)
        {
            yield return firstLineCoroutine;
        }

        // Bắt đầu ngó nghiêng lơ mơ nhìn trời
        Coroutine swayRoutine = StartCoroutine(GroggyLookAroundRoutine(camTrans, lyingWorldRot));

        // Chạy tiếp các câu thoại còn lại của Đợt 1 (nếu có)
        if (firstWakeUpDialogues != null && firstWakeUpDialogues.Length > 1)
        {
            for (int i = 1; i < firstWakeUpDialogues.Length; i++)
            {
                if (firstWakeUpDialogues[i] != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(firstWakeUpDialogues[i]));
                }
            }
        }

        // Dừng ngó nghiêng để bắt đầu ngồi dậy
        if (swayRoutine != null)
        {
            StopCoroutine(swayRoutine);
            swayRoutine = null;
        }

        // --- ĐỢT 2: NGỒI DẬY 45 ĐỘ & ĐỢT THOẠI 2 ---
        Debug.Log("[RitualAltar] 🧘 Đang xoay camera về 45 độ và ngồi dậy dần dần...");
        if (rustlingGroundAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(rustlingGroundAudio, soundVolume);
        }

        Vector3 finalStandEyeWorldPos = (player != null) ? player.transform.TransformPoint(new Vector3(0f, standingCameraLocalY, 0f)) : (targetPlayerWorldPos + new Vector3(0f, standingCameraLocalY, 0f));
        Vector3 sittingWorldPos = Vector3.Lerp(lyingWorldPos, finalStandEyeWorldPos, 0.5f);
        sittingWorldPos.y = targetPlayerWorldPos.y + (standingCameraLocalY * 0.5f);

        Quaternion sittingWorldRot = Quaternion.Euler(sitUpPitchAngle, targetPlayerWorldRot.eulerAngles.y, 0f);

        if (camTrans != null)
        {
            Vector3 fromPos = camTrans.position;
            Quaternion fromRot = camTrans.rotation;

            float sitElapsed = 0f;
            while (sitElapsed < sitUpDuration)
            {
                sitElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(sitElapsed / sitUpDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                float wobbleY = Mathf.Sin(t * Mathf.PI * 2f) * 0.01f;

                camTrans.position = Vector3.Lerp(fromPos, sittingWorldPos, smoothT) + new Vector3(0f, wobbleY, 0f);
                camTrans.rotation = Quaternion.Slerp(fromRot, sittingWorldRot, smoothT);
                yield return null;
            }
            camTrans.position = sittingWorldPos;
            camTrans.rotation = sittingWorldRot;
        }

        // Phát đợt thoại 2 (lúc đang ngồi dậy)
        if (sittingUpDialogues != null && sittingUpDialogues.Length > 0)
        {
            foreach (var line in sittingUpDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(line));
                }
            }
        }

        // --- ĐỢT 3: ĐỨNG DẬY VỀ ĐÚNG CHIỀU CAO MẮT (0, 2.5, 0) & ĐỢT THOẠI 3 ---
        Debug.Log($"[RitualAltar] 🚶 Đứng dậy và bay Camera về đúng (0, {standingCameraLocalY}, 0) trong Main...");

        if (player != null)
        {
            finalStandEyeWorldPos = player.transform.TransformPoint(new Vector3(0f, standingCameraLocalY, 0f));
        }
        Quaternion finalStandEyeWorldRot = Quaternion.Euler(0f, targetPlayerWorldRot.eulerAngles.y, 0f);

        if (camTrans != null)
        {
            Vector3 fromPos = camTrans.position;
            Quaternion fromRot = camTrans.rotation;

            float standElapsed = 0f;
            while (standElapsed < standUpDuration)
            {
                standElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(standElapsed / standUpDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                camTrans.position = Vector3.Lerp(fromPos, finalStandEyeWorldPos, smoothT);
                camTrans.rotation = Quaternion.Slerp(fromRot, finalStandEyeWorldRot, smoothT);
                yield return null;
            }

            // Gắn chính xác về local transform (0, standingCameraLocalY, 0)
            camTrans.localPosition = new Vector3(0f, standingCameraLocalY, 0f);
            camTrans.localRotation = Quaternion.identity;
        }

        // Phát đợt thoại 3 (lúc đứng dậy)
        if (standingUpDialogues != null && standingUpDialogues.Length > 0)
        {
            foreach (var line in standingUpDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(line));
                }
            }
        }

        // --- GIAI ĐOẠN 6: TRẢ LẠI TOÀN BỘ QUYỀN ĐIỀU KHIỂN ---
        if (player != null)
        {
            player.transform.position = targetPlayerWorldPos;
            player.transform.rotation = targetPlayerWorldRot;

            if (camTrans != null)
            {
                camTrans.localPosition = new Vector3(0f, standingCameraLocalY, 0f);
                camTrans.localRotation = Quaternion.identity;
            }

            player.SetStandingCamY(standingCameraLocalY);
            player.SyncRotationWithCurrentCamera();

            player.isCameraLocked = false;
            player.SetMovementState(true);
            player.LockCursor();

            if (cc != null) cc.enabled = true;
        }

        // Bật lại tâm ngắm chấm tròn
        InteractPro interactPro = Object.FindFirstObjectByType<InteractPro>();
        if (interactPro != null && interactPro.dotObject != null)
        {
            interactPro.dotObject.SetActive(true);
        }

        isInteracting = false;
        SmartInteractionDialogue.isAnyDialoguePlaying = false;

        Debug.Log("[RitualAltar] 🎮 CẮT CẢNH HOÀN TẤT! ĐÃ TRẢ DI CHUYỂN TỰ DO & CAMERA CHUẨN!");
    }

    private IEnumerator GroggyLookAroundRoutine(Transform camTrans, Quaternion baseRot)
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;
            float yaw = Mathf.Sin(timer * 1.3f) * groggySwayAngle;
            float roll = Mathf.Cos(timer * 1.0f) * (groggySwayAngle * 0.35f);

            if (camTrans != null)
            {
                camTrans.rotation = baseRot * Quaternion.Euler(0f, yaw, roll);
            }
            yield return null;
        }
    }

    private void AutoFindIntroAssets()
    {
        if (pointSpawn == null)
        {
            GameObject pObj = GameObject.Find("PointSpawn") ?? GameObject.Find("SpawnNM");
            if (pObj != null) pointSpawn = pObj.transform;
        }

        if (breathWakeUpAudio == null)
        {
            Map02IntroSequence intro02 = Object.FindFirstObjectByType<Map02IntroSequence>(FindObjectsInactive.Include);
            if (intro02 != null && intro02.breathWakeUpAudio != null) breathWakeUpAudio = intro02.breathWakeUpAudio;
        }

        if (rustlingGroundAudio == null)
        {
            Map02IntroSequence intro02 = Object.FindFirstObjectByType<Map02IntroSequence>(FindObjectsInactive.Include);
            if (intro02 != null && intro02.rustlingGroundAudio != null) rustlingGroundAudio = intro02.rustlingGroundAudio;
        }
    }

    private void ApplyActivatedMaterial(GameObject targetObj)
    {
        if (targetObj == null || activatedTalismanMaterial == null) return;

        Renderer[] renderers = targetObj.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            foreach (Renderer rend in renderers)
            {
                if (rend != null)
                {
                    int matCount = (rend.sharedMaterials != null && rend.sharedMaterials.Length > 0) ? rend.sharedMaterials.Length : 1;
                    Material[] newMats = new Material[matCount];
                    for (int i = 0; i < matCount; i++)
                    {
                        newMats[i] = activatedTalismanMaterial;
                    }
                    rend.materials = newMats;
                    Debug.Log($"[RitualAltar] 🎨 Đã áp dụng Material '{activatedTalismanMaterial.name}' lên '{rend.gameObject.name}'!");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[RitualAltar] ⚠️ Không tìm thấy Renderer nào trên '{targetObj.name}' để đổi Material!");
        }
    }

    // =========================================================================
    // HELPER INVENTORY FINDER
    // =========================================================================

    private string FindItemInInventory(string mainName, string alternateNames)
    {
        if (InventoryManager.Instance == null) return null;

        if (!string.IsNullOrEmpty(mainName) && InventoryManager.Instance.HasItem(mainName))
        {
            return mainName;
        }

        if (!string.IsNullOrEmpty(alternateNames))
        {
            string[] alts = alternateNames.Split(',');
            foreach (string alt in alts)
            {
                string trimmed = alt.Trim();
                if (!string.IsNullOrEmpty(trimmed) && InventoryManager.Instance.HasItem(trimmed))
                {
                    return trimmed;
                }
            }
        }

        return null;
    }

    // =========================================================================
    // HELPER CINEMA FADE & DIALOGUE TYPEWRITER
    // =========================================================================

    private MovePl LockPlayer()
    {
        MovePl p = Object.FindFirstObjectByType<MovePl>();
        if (p != null)
        {
            p.isCameraLocked = true;
            p.SetMovementState(false);
        }
        return p;
    }

    private void UnlockPlayer(MovePl p)
    {
        if (p != null)
        {
            p.isCameraLocked = false;
            p.SetMovementState(true);
        }
    }

    private IEnumerator FadeScreenRoutine(float fromAlpha, float toAlpha, float duration)
    {
        EnsureFadeImage();
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = (duration > 0f) ? Mathf.Clamp01(elapsed / duration) : 1f;
            float alpha = Mathf.SmoothStep(fromAlpha, toAlpha, t);
            Color c = fadeColor;
            c.a = alpha;
            fadeImage.color = c;
            yield return null;
        }

        Color finalC = fadeColor;
        finalC.a = toAlpha;
        fadeImage.color = finalC;

        if (toAlpha <= 0.001f)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator PlaySimpleDialogueSequence(DialogueLine[] dialogues)
    {
        if (dialogues == null || dialogues.Length == 0) yield break;

        isInteracting = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;
        interactStartTime = Time.unscaledTime;
        isTyping = false;
        skipRequested = false;
        skipWaitRequested = false;

        // Chờ 1 frame để lượt click chuột tương tác ban đầu kết thúc
        yield return null;

        yield return StartCoroutine(PlayDialogueListRoutine(dialogues));

        ClearSubtitleUI();

        // Cooldown 0.5s chống spam click
        yield return new WaitForSeconds(0.5f);

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isInteracting = false;
    }

    private IEnumerator PlayDialogueListRoutine(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;

        foreach (DialogueLine line in lines)
        {
            if (line != null) yield return StartCoroutine(PlaySingleLineRoutine(line));
        }

        ClearSubtitleUI();
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

        // Phát âm thanh gõ chữ looping
        if (dialogueSound != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.clip = dialogueSound;
            audioSource.volume = soundVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

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

        // Chờ 1 frame để lượt click bỏ qua gõ chữ không ăn nhầm vào lượt click chuyển câu
        yield return null;

        // Bật con trỏ nhấp nháy trong lúc chờ người chơi đọc
        if (subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

        // Chờ người chơi đọc hoặc click lần 2 để qua nhanh
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
        skipWaitRequested = false;

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        ClearSubtitleUI();
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
            subtitleTextUI.gameObject.SetActive(false);
            if (subtitleTextUI.transform.parent != null) subtitleTextUI.transform.parent.gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // AUTO FIND HELPER
    // =========================================================================

    void EnsureAudioSource()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
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
        if (fadeImage != null) return;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            Transform fadeT = c.transform.Find("FadeImage") ?? c.transform.Find("FadePanel") ?? c.transform.Find("BlackScreen");
            if (fadeT != null)
            {
                fadeImage = fadeT.GetComponent<Image>();
                if (fadeImage != null) return;
            }
        }

        CorpseGoreHarvest corpse = Object.FindFirstObjectByType<CorpseGoreHarvest>(FindObjectsInactive.Include);
        if (corpse != null && corpse.fadeImage != null)
        {
            fadeImage = corpse.fadeImage;
            return;
        }
    }
}
