using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Quản lý tương tác Dâng Vật Phẩm (Gore / Nội Tạng) lên 2 Đền Thờ (Siramori Shrine) trong Map 04:
/// 1. TƯƠNG TÁC GỌN GÀNG, KHÔNG THOẠI THỪA:
///    - Fade đen mượt mà (0.5s) -> Phát âm thanh đặt tế phẩm -> Bật hiện Gore trong đền -> Trừ 1 Gore khỏi túi -> Fade sáng lại (0.5s).
///    - Không bắt người chơi phải chờ đọc thoại rườm rà.
/// 2. ĐẢM BẢO GORE LUÔN HIỆN RÕ RÀNG TRONG ĐỀN:
///    - Tự động kích hoạt toàn bộ MeshRenderer & GameObject con (0_0_0) của cục Gore.
///    - Khóa script nhặt đồ và collider trên Gore để người chơi không nhặt lại.
/// 3. HÓA GIẢI WMAN KHI ĐỦ 2 ĐỀN:
///    - Tự động tính đúng 2 đền (1/2, 2/2).
///    - Khi đủ 2/2 đền -> Tự động hóa giải WmanBehavior (Wman không bao giờ săn đuổi nữa) và vô hiệu hóa ForbiddenDangerZone.
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

    [Tooltip("Các tên phụ có thể chấp nhận (phân cách bởi dấu phẩy)")]
    public string alternateItemNames = "Gore,Core,Organ,Noi Tang,noi tang,gore,core,xac,ruot,thit,flesh";

    [Tooltip("Có xóa vật phẩm khỏi túi đồ sau khi dâng tế không? (Mặc định: BẬT)")]
    public bool consumeItem = true;

    [Header("2. Vật Thể Gore Hiển Thị Tại Đền (Gore in Shrine)")]
    [Tooltip("Kéo GameObject cục Gore được setup bên trong ngôi đền này vào đây (Ban đầu tự tắt, khi dâng đồ sẽ bật lên)")]
    public GameObject shrineGoreObject;

    [Header("3. Cấu Hình Tương Tác & Cooldown")]
    [Tooltip("Thời gian hồi chiêu chống spam click tương tác (giây - Mặc định: 0.4s)")]
    public float interactCooldown = 0.4f;

    [Header("4. Cấu Hình Phụ Đề / Thoại")]
    [Tooltip("Bật nếu muốn hiện phụ đề suy nghĩ khi chưa có đồ tế. Tắt nếu không muốn hiện thoại thừa (Mặc định: TẮT)")]
    public bool enableDialogues = false;

    [Header("5. Cấu Hình Fade Màn Hình (Cinema Fade)")]
    [Tooltip("Kéo UI Fade Image (hoặc để trống để code tự động tìm Canvas tạo màn che)")]
    public Image fadeImage;
    public Color fadeColor = Color.black;
    [Tooltip("Thời gian màn hình tối dần (giây)")]
    public float fadeInDuration = 0.5f;
    [Tooltip("Thời gian giữ màn hình đen trong lúc đặt đồ tế (giây)")]
    public float blackScreenHoldDuration = 1.0f;
    [Tooltip("Thời gian màn hình sáng lại (giây)")]
    public float fadeOutDuration = 0.5f;

    [Header("6. Âm Thanh (Audio SFX)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh đặt vật hiến tế / rùng rợn")]
    public AudioClip placeSound;
    [Tooltip("Âm thanh chạy chữ phụ đề")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("7. Thoại Nhắc Nhở Khi CHƯA Có Vật Phẩm (Chỉ hiện nếu enableDialogues = true)")]
    public DialogueLine[] noItemDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Một ngôi đền thờ bị phong ấn... Cần một vật hiến tế bằng máu thịt.",
            englishDialogue = "A sealed shrine... It requires a flesh sacrifice.",
            holdDuration = 2.5f
        }
    };

    [Header("8. Cấu Hình Phụ Đề (Subtitle Text UI)")]
    public TextMeshProUGUI subtitleTextUI;
    public bool useTypewriter = true;
    public float typewriterSpeed = 0.035f;

    [Header("9. Sự Kiện & Quản Lý Đa Đền Thờ (Multi-Shrine Events)")]
    [Tooltip("Sự kiện kích hoạt khi đặt vật tế vào chính ngôi đền này")]
    public UnityEvent onThisShrineOffered;

    [Tooltip("Sự kiện kích hoạt khi TẤT CẢ các đền thờ trong Scene đã được dâng tế đầy đủ")]
    public UnityEvent onAllShrinesCompleted;

    [Header("10. Hóa Giải Vùng Cấm / Wman / Uma (Pacify Zones & Wman on All Shrines Completed)")]
    [Tooltip("Tự động tìm và hóa giải toàn bộ ForbiddenDangerZone, Wman & Uma trong Scene khi cúng đủ 2 đền thờ (Mặc định: BẬT)")]
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
    private float lastInteractTimestamp = -10f;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;

    void Awake()
    {
        EnsureCollider();
        EnsureAudioSource();
        AutoFindDialogueSound();
        FindSubtitleUI();
        EnsureFadeImage();
        AutoBindGoreObject();

        // Đảm bảo ban đầu cục Gore trong đền luôn được ẩn
        if (shrineGoreObject != null)
        {
            shrineGoreObject.SetActive(false);
            DisableGorePickupScripts(shrineGoreObject);
        }
    }

    void Start()
    {
        EnsureCollider();
        EnsureAudioSource();
        AutoFindDialogueSound();
        FindSubtitleUI();
        EnsureFadeImage();
        AutoBindGoreObject();

        // Đếm chính xác tổng số đền thờ trong Scene
        ShrineOfferingInteraction[] allShrines = Object.FindObjectsByType<ShrineOfferingInteraction>(FindObjectsSortMode.None);
        totalShrinesCount = (allShrines != null) ? allShrines.Length : 2;
        totalOfferingsPlaced = 0; // Reset đếm khi bắt đầu scene
    }

    void Update()
    {
        if (!isInteracting) return;

        if (Time.unscaledTime - interactStartTime < 0.25f) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                skipRequested = true;
            }
            else if (isWaitingForNextLine)
            {
                skipWaitRequested = true;
            }
        }
    }

    private void AutoBindGoreObject()
    {
        if (shrineGoreObject != null) return;

        // Tự động tìm cục Gore gần đền thờ nhất trong bán kính 4m
        GameObject[] allGores = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        float closestDist = float.MaxValue;
        GameObject bestGore = null;

        foreach (var g in allGores)
        {
            if (g != null && (g.name.Equals("Gore") || g.name.StartsWith("Gore (") || g.name.Equals("Gore (1)")))
            {
                if (g.transform.parent != null && g.transform.parent.name == "Gore")
                {
                    float d = Vector3.Distance(transform.position, g.transform.position);
                    if (d < 4.0f && d < closestDist)
                    {
                        closestDist = d;
                        bestGore = g;
                    }
                }
            }
        }

        if (bestGore != null)
        {
            shrineGoreObject = bestGore;
            Debug.Log($"[ShrineOffering] ⛩️ Tự động liên kết '{gameObject.name}' với cục Gore: '{bestGore.name}'");
        }
    }

    private void DisableGorePickupScripts(GameObject gore)
    {
        if (gore == null) return;

        InteractableItem[] goreItems = gore.GetComponentsInChildren<InteractableItem>(true);
        foreach (var item in goreItems)
        {
            if (item != null) item.enabled = false;
        }

        Collider[] goreCols = gore.GetComponentsInChildren<Collider>(true);
        foreach (var col in goreCols)
        {
            if (col != null) col.enabled = false;
        }
    }

    // ==================== TƯƠNG TÁC (IInteractable) ====================

    public void Interact()
    {
        // 1. KIỂM TRA COOLDOWN CHỐNG SPAM CLICK
        if (Time.time - lastInteractTimestamp < interactCooldown)
        {
            return;
        }
        lastInteractTimestamp = Time.time;

        if (isInteracting || SmartInteractionDialogue.isAnyDialoguePlaying)
        {
            return;
        }

        // 2. ĐỀN NÀY ĐÃ ĐƯỢC DÂNG VẬT TẾ RỒI
        if (hasBeenOffered)
        {
            return;
        }

        // 3. KIỂM TRA XEM NGƯỜI CHƠI CÓ GORE TRONG TÚI KHÔNG
        string matchedItemName = FindMatchingGoreItemInInventory();
        bool hasGore = !string.IsNullOrEmpty(matchedItemName);

        if (hasGore)
        {
            // TH2: ĐÃ CÓ GORE -> BẮT ĐẦU FADE ĐEN, HIỆN GORE VÀ TRỪ TRONG TÚI
            StartCoroutine(OfferingSequenceRoutine(matchedItemName));
        }
        else
        {
            // TH1: CHƯA CÓ GORE
            if (enableDialogues)
            {
                StartCoroutine(PlaySimpleDialogueSequence(noItemDialogues));
            }
        }
    }

    public string FindMatchingGoreItemInInventory()
    {
        if (InventoryManager.Instance == null) return null;

        if (!string.IsNullOrEmpty(requiredItemName) && InventoryManager.Instance.HasItem(requiredItemName))
        {
            return requiredItemName;
        }

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

        // 3. BẬT HIỆN CỤC GORE TRONG ĐỀN & ĐẢM BẢO 100% HIỂN THỊ MESH RENDERER
        if (shrineGoreObject == null) AutoBindGoreObject();

        if (shrineGoreObject != null)
        {
            shrineGoreObject.SetActive(true);

            // Bật toàn bộ GameObject con (ví dụ 0_0_0)
            Transform[] allChildren = shrineGoreObject.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                if (t != null) t.gameObject.SetActive(true);
            }

            // Bật toàn bộ MeshRenderer
            Renderer[] allRends = shrineGoreObject.GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRends)
            {
                if (r != null) r.enabled = true;
            }

            // Vô hiệu hóa script nhặt đồ và collider trên Gore
            DisableGorePickupScripts(shrineGoreObject);
        }

        // 4. TRỪ GORE TRONG TÚI ĐỒ
        if (consumeItem && InventoryManager.Instance != null && !string.IsNullOrEmpty(matchedItemName))
        {
            InventoryManager.Instance.RemoveItem(matchedItemName);
            Debug.Log($"[ShrineOffering] ⛩️ Đã trừ '{matchedItemName}' khỏi Inventory của người chơi!");
        }

        hasBeenOffered = true;
        totalOfferingsPlaced++;
        Debug.Log($"[ShrineOffering] ⛩️ Đã dâng tế đền thờ! Tiến độ: {totalOfferingsPlaced}/{totalShrinesCount}");

        onThisShrineOffered?.Invoke();

        bool isAllCompleted = (totalOfferingsPlaced >= totalShrinesCount && totalShrinesCount > 0);

        // 5. NẾU ĐÃ ĐỦ TẤT CẢ CÁC ĐỀN -> HÓA GIẢI WMAN & DANGER ZONE
        if (isAllCompleted)
        {
            Debug.Log("<color=green><b>[ShrineOffering] 🌟 ĐÃ DÂNG ĐỦ 2 ĐỀN THỜ! Hóa giải toàn bộ Danger Zone & Wman trong Map.</b></color>");
            onAllShrinesCompleted?.Invoke();

            if (autoPacifyZonesAndUma)
            {
                if (dangerZonesToDeactivate != null && dangerZonesToDeactivate.Length > 0)
                {
                    foreach (var z in dangerZonesToDeactivate)
                    {
                        if (z != null) z.DeactivateZone();
                    }
                }
                else
                {
                    ForbiddenDangerZone[] allZones = Object.FindObjectsByType<ForbiddenDangerZone>(FindObjectsSortMode.None);
                    if (allZones != null)
                    {
                        foreach (var z in allZones)
                        {
                            if (z != null) z.DeactivateZone();
                        }
                    }
                }

                WmanBehavior[] allWmans = Object.FindObjectsByType<WmanBehavior>(FindObjectsSortMode.None);
                if (allWmans != null)
                {
                    foreach (var wman in allWmans)
                    {
                        if (wman != null) wman.PacifyWman();
                    }
                }

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

        // Giữ màn hình đen ngắn
        yield return new WaitForSeconds(blackScreenHoldDuration);

        // 6. FADE SÁNG TRỞ LẠI
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

        // 7. MỞ LẠI QUYỀN ĐIỀU KHIỂN CHO PLAYER NGAY LẬP TỨC (KHÔNG THOẠI THỪA)
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = false;
            playerMovePl.SetMovementState(true);
        }

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isInteracting = false;
    }

    // ==================== SEQUENCE HIỆN THOẠI (NẾU BẬT) ====================

    private IEnumerator PlaySimpleDialogueSequence(DialogueLine[] dialogues)
    {
        if (dialogues == null || dialogues.Length == 0) yield break;

        isInteracting = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;
        interactStartTime = Time.unscaledTime;
        isTyping = false;
        skipRequested = false;
        skipWaitRequested = false;

        yield return null;

        foreach (DialogueLine line in dialogues)
        {
            if (line != null)
            {
                yield return StartCoroutine(PlaySingleLineRoutine(line));
            }
        }

        ClearSubtitleUI();
        yield return new WaitForSeconds(0.4f);

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

        if (subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

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

    void EnsureCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.4f, 0f);
            box.size = new Vector3(1.5f, 1.6f, 1.5f);
        }
        else if (col is BoxCollider box)
        {
            if (box.size.x < 0.8f || box.size.y < 0.8f || box.size.z < 0.8f)
            {
                box.size = new Vector3(Mathf.Max(box.size.x, 1.2f), Mathf.Max(box.size.y, 1.4f), Mathf.Max(box.size.z, 1.2f));
            }
        }
    }

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
