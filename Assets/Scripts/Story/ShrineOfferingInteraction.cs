using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Script xử lý tương tác Dâng Vật Phẩm (Gore / Nội Tạng) lên Đền Thờ (Siramori Shrine):
/// 1. Kế thừa IInteractable -> Tự động nhận diện tâm ngắm và hiện icon Tương Tác.
/// 2. TH1 (Chưa có Gore/Nội Tạng): Hiện thoại nhân vật nhận xét đền thờ còn thiếu vật hiến tế.
/// 3. TH2 (Đã có Gore trong túi): 
///    - Fade đen màn hình mượt mà (Cinema Fade), khóa di chuyển, phát âm thanh dâng tế.
///    - Bật Active cục Gore được setup sẵn trong đền thờ này (shrineGoreObject).
///    - Trừ 1 vật phẩm Gore trong Inventory của người chơi.
///    - Fade sáng trở lại, hiện thoại sau khi dâng tế.
///    - Đếm số lượng đền đã hoàn thành (Ví dụ: 1/2, 2/2) -> Khi đủ tất cả đền sẽ kích hoạt sự kiện onAllShrinesCompleted!
/// 4. TH3 (Đã đặt rồi): Báo đền thờ này đã được dâng tế.
/// </summary>
public class ShrineOfferingInteraction : MonoBehaviour, IInteractable
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

    [Header("1. Yêu Cầu Vật Phẩm (Required Offering Item)")]
    [Tooltip("Tên vật phẩm cần có trong túi đồ để dâng lên đền (Mặc định: 'Nội Tạng' hoặc 'Gore')")]
    public string requiredItemName = "Nội Tạng";

    [Tooltip("Các tên phụ có thể chấp nhận (phân cách bởi dấu phẩy, ví dụ: Gore, Core, Noi Tang, Organ)")]
    public string alternateItemNames = "Gore,Core,Organ,Noi Tang,noi tang,gore,core";

    [Tooltip("Có xóa vật phẩm khỏi túi đồ sau khi dâng tế không? (Mặc định: BẬT)")]
    public bool consumeItem = true;

    [Header("2. Vật Thể Gore Hiển Thị Tại Đền (Gore in Shrine)")]
    [Tooltip("Kéo GameObject cục Gore được bạn đặt sẵn bên trong ngôi đền này vào đây (Ban đầu script sẽ tự tắt, khi dâng đồ sẽ bật lên)")]
    public GameObject shrineGoreObject;

    [Header("3. Cấu Hình Fade Màn Hình (Cinema Fade)")]
    [Tooltip("Kéo UI Fade Image (hoặc để trống để code tự động tìm Canvas tạo màn che)")]
    public Image fadeImage;
    public Color fadeColor = Color.black;
    [Tooltip("Thời gian màn hình tối dần (giây)")]
    public float fadeInDuration = 0.6f;
    [Tooltip("Thời gian giữ màn hình đen trong lúc đặt đồ tế (giây)")]
    public float blackScreenHoldDuration = 1.6f;
    [Tooltip("Thời gian màn hình sáng lại (giây)")]
    public float fadeOutDuration = 0.6f;

    [Header("4. Âm Thanh (Audio Sfx)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh đặt vật hiến tế / rùng rợn")]
    public AudioClip placeSound;
    [Tooltip("Âm thanh chạy chữ phụ đề")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("5. Thoại Khi CHƯA Có Vật Phẩm (No Offering Dialogue)")]
    public DialogueLine[] noItemDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Một ngôi đền thờ nhỏ bị phong ấn... Bên trong dường như còn thiếu một vật hiến tế bằng máu thịt.",
            englishDialogue = "A small sealed shrine... It seems to be missing a flesh sacrifice.",
            holdDuration = 4.0f
        }
    };

    [Header("6. Cấu Hình Phụ Đề (Subtitle Text UI)")]
    [Tooltip("Kéo TextMeshPro Subtitle Text trên Canvas vào đây (Để trống sẽ tự tìm)")]
    public TextMeshProUGUI subtitleTextUI;
    public bool useTypewriter = true;
    public float typewriterSpeed = 0.035f;

    [Header("7. Sự Kiện & Quản Lý Đa Đền Thờ (Multi-Shrine Events)")]
    [Tooltip("Sự kiện kích hoạt khi đặt vật tế vào chính ngôi đền này")]
    public UnityEvent onThisShrineOffered;

    [Tooltip("Sự kiện kích hoạt khi TẤT CẢ các đền thờ trong Scene đã được dâng tế đầy đủ (VD: Mở cửa, mở xích...)")]
    public UnityEvent onAllShrinesCompleted;

    [Header("8. Hóa Giải Vùng Cấm / Quái (Pacify Zones & Uma on All Shrines Completed)")]
    [Tooltip("Tự động tìm và hóa giải toàn bộ ForbiddenDangerZone & Uma trong Scene khi cúng đủ 2 đền thờ (Mặc định: BẬT)")]
    public bool autoPacifyZonesAndUma = true;

    [Tooltip("Kéo cụ thể các ForbiddenDangerZone cần tắt (nếu để trống và bật autoPacify thì script tự tìm)")]
    public ForbiddenDangerZone[] dangerZonesToDeactivate;

    // --- Quản lý đếm số đền hoàn thành (Static) ---
    public static int totalOfferingsPlaced = 0;
    public static int totalShrinesCount = 0;

    // --- Private State ---
    private bool hasBeenOffered = false;
    private bool isInteracting = false;
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

        // Đảm bảo ban đầu cục Gore trong đền chưa được bật
        if (shrineGoreObject != null)
        {
            shrineGoreObject.SetActive(false);
        }
    }

    void Start()
    {
        EnsureAudioSource();
        AutoFindDialogueSound();
        FindSubtitleUI();
        EnsureFadeImage();

        // Đếm tổng số đền thờ có trong Scene
        ShrineOfferingInteraction[] allShrines = Object.FindObjectsByType<ShrineOfferingInteraction>(FindObjectsSortMode.None);
        totalShrinesCount = (allShrines != null) ? allShrines.Length : 0;
        totalOfferingsPlaced = 0; // Reset đếm khi bắt đầu scene
    }

    void Update()
    {
        if (!isInteracting) return;

        // Tránh ăn nhầm click tương tác ban đầu trong 0.25s
        if (Time.unscaledTime - interactStartTime < 0.25f) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                // BẤM LẦN 1 KHI ĐANG GÕ -> HIỆN FULL CHỮ NGAY
                skipRequested = true;
            }
            else if (isWaitingForNextLine)
            {
                // BẤM LẦN 2 KHI CHỮ ĐÃ ĐẦY ĐỦ -> QUA CÂU TIẾP THEO
                skipWaitRequested = true;
            }
        }
    }

    // ==================== TƯƠNG TÁC (IInteractable) ====================

    public void Interact()
    {
        if (isInteracting || SmartInteractionDialogue.isAnyDialoguePlaying)
        {
            return;
        }

        // TH3: Đền này đã được dâng vật tế rồi -> Không làm gì cả
        if (hasBeenOffered)
        {
            return;
        }

        // Kiểm tra xem người chơi có vật phẩm trong người không
        string matchedItemName = FindMatchingGoreItemInInventory();
        bool hasGore = !string.IsNullOrEmpty(matchedItemName);

        if (hasGore)
        {
            // TH2: ĐÃ CÓ GORE -> BẮT ĐẦU FADE ĐEN, ĐẶT ĐỒ TẾ VÀ TRỪ TRONG TÚI
            StartCoroutine(OfferingSequenceRoutine(matchedItemName));
        }
        else
        {
            // TH1: CHƯA CÓ GORE -> HIỆN THOẠI NHẬN XÉT
            StartCoroutine(PlaySimpleDialogueSequence(noItemDialogues));
        }
    }

    /// <summary>
    /// Tìm xem trong túi đồ có món đồ Gore / Nội Tạng nào khớp không
    /// </summary>
    private string FindMatchingGoreItemInInventory()
    {
        if (InventoryManager.Instance == null) return null;

        // 1. Kiểm tra tên chính
        if (!string.IsNullOrEmpty(requiredItemName) && InventoryManager.Instance.HasItem(requiredItemName))
        {
            return requiredItemName;
        }

        // 2. Kiểm tra các tên phụ (alternateItemNames)
        if (!string.IsNullOrEmpty(alternateItemNames))
        {
            string[] alternates = alternateItemNames.Split(',');
            foreach (string alt in alternates)
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

    // ==================== SEQUENCE DÂNG VẬT PHẨM (CINEMA FADE) ====================

    private IEnumerator OfferingSequenceRoutine(string matchedItemName)
    {
        isInteracting = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        MovePl playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = true;
            playerMovePl.SetMovementState(false);
        }

        // 1. FADE ĐEN MÀN HÌNH MƯỢT MÀ
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

        // 2. PHÁT ÂM THANH ĐẶT VẬT HIẾN TẾ
        if (placeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(placeSound, soundVolume);
        }

        // 3. BẬT CỤC GORE SETUP SẴN TRONG ĐỀN & TRỪ KHỎI TÚI ĐỒ
        if (shrineGoreObject != null)
        {
            shrineGoreObject.SetActive(true);
        }

        if (consumeItem && InventoryManager.Instance != null && !string.IsNullOrEmpty(matchedItemName))
        {
            InventoryManager.Instance.RemoveItem(matchedItemName);
            Debug.Log($"[ShrineOffering] ⛩️ Đã trừ '{matchedItemName}' khỏi Inventory của người chơi!");
        }

        hasBeenOffered = true;
        totalOfferingsPlaced++;
        Debug.Log($"[ShrineOffering] ⛩️ Đã dâng tế đền thờ! Tiến độ: {totalOfferingsPlaced}/{totalShrinesCount}");

        onThisShrineOffered?.Invoke();

        // Kiểm tra nếu tất cả các đền trong Scene đã được dâng tế đầy đủ
        if (totalOfferingsPlaced >= totalShrinesCount && totalShrinesCount > 0)
        {
            Debug.Log("[ShrineOffering] 🌟 TẤT CẢ CÁC ĐỀN THỜ ĐÃ ĐƯỢC HOÀN THÀNH! Hóa giải toàn bộ Danger Zone & Uma trong Scene.");
            onAllShrinesCompleted?.Invoke();

            if (autoPacifyZonesAndUma)
            {
                // 1. Tắt các zone được gán thủ công nếu có
                if (dangerZonesToDeactivate != null && dangerZonesToDeactivate.Length > 0)
                {
                    foreach (var z in dangerZonesToDeactivate)
                    {
                        if (z != null) z.DeactivateZone();
                    }
                }
                else
                {
                    // Tự động tìm tất cả ForbiddenDangerZone trong Scene để hóa giải
                    ForbiddenDangerZone[] allZones = Object.FindObjectsByType<ForbiddenDangerZone>(FindObjectsSortMode.None);
                    if (allZones != null)
                    {
                        foreach (var z in allZones)
                        {
                            if (z != null) z.DeactivateZone();
                        }
                    }
                }

                // 2. Tự động tìm tất cả Uma trong Scene để đưa về trạng thái hiền hòa
                UmaPatrolAI[] allUmas = Object.FindObjectsByType<UmaPatrolAI>(FindObjectsSortMode.None);
                if (allUmas != null)
                {
                    foreach (var uma in allUmas)
                    {
                        if (uma != null) uma.PacifyUma();
                    }
                }
            }
        }

        // Giữ màn hình đen một khoảng ngắn
        yield return new WaitForSeconds(blackScreenHoldDuration);

        // 4. FADE SÁNG TRỞ LẠI
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

        ClearSubtitleUI();

        // 5. MỞ LẠI QUYỀN ĐIỀU KHIỂN CHO PLAYER
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = false;
            playerMovePl.SetMovementState(true);
        }

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isInteracting = false;
    }

    // ==================== SEQUENCE HIỆN THOẠI ĐƠN GIẢN ====================

    private IEnumerator PlaySimpleDialogueSequence(DialogueLine[] dialogues)
    {
        if (dialogues == null || dialogues.Length == 0) yield break;

        isInteracting = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;
        interactStartTime = Time.unscaledTime;
        isTyping = false;
        skipRequested = false;
        skipWaitRequested = false;

        // Chờ 1 frame để tiêu thụ click chuột ban đầu
        yield return null;

        foreach (DialogueLine line in dialogues)
        {
            if (line != null)
            {
                yield return StartCoroutine(PlaySingleLineRoutine(line));
            }
        }

        ClearSubtitleUI();

        // Cooldown 0.5s để chống spam click đè thoại
        yield return new WaitForSeconds(0.5f);

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isInteracting = false;
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

        // Phát âm thanh gõ chữ looping trong suốt quá trình chạy chữ
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

        // Tắt âm thanh gõ chữ ngay khi hoàn thành câu
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
        {
            audioSource.Stop();
        }

        isTyping = false;
        skipRequested = false;

        // Bật con trỏ nhấp nháy trong lúc chờ người chơi đọc
        if (subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

        // Chờ đọc xong hoặc bấm click lần 2 để qua nhanh
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
            subtitleTextUI.gameObject.SetActive(false);
            if (subtitleTextUI.transform.parent != null) subtitleTextUI.transform.parent.gameObject.SetActive(false);
        }
    }

    // ==================== HELPER SETUP ====================

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

        Canvas targetCanvas = Object.FindFirstObjectByType<Canvas>();
        if (targetCanvas != null)
        {
            GameObject fadeObj = new GameObject("DynamicFadeImage");
            fadeObj.transform.SetParent(targetCanvas.transform, false);
            fadeImage = fadeObj.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            RectTransform rt = fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            fadeObj.SetActive(false);
        }
    }
}
