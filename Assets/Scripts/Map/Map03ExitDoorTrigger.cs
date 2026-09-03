using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý Cửa / Cổng dịch chuyển (NextScene) chuyển từ Map 03 sang Scene 04 (Map04):
/// 1. Tự động kích hoạt khi người chơi CHẠM VÀO TRIGGER (không cần nút bấm tương tác).
/// 2. Điều kiện qua cửa: Người chơi bắt buộc phải sở hữu ĐỦ 6 BỘ PHẬN + 2 GORE:
///    - "Bua" (Lá Bùa) x1
///    - "ArmsL" (Tay Trái) x1
///    - "ArmsR" (Tay Phải) x1
///    - "FeetL" / "FeelL" (Chân Trái) x1
///    - "FeetR" / "FeelR" (Chân Phải) x1
///    - "Head" (Đầu) x1
///    - "Gore" (Nội tạng / Thịt) x2
/// 3. Nếu trong túi đồ chứa thêm các đồ khác (Dao, Pin, Nước...) thì vẫn cho phép qua bình thường.
/// 4. Nếu THIẾU bất kỳ món nào trong danh sách trên:
///    - Ngăn không cho qua Scene mới.
///    - Tự động phát lời thoại kèm hiệu ứng gõ chữ phụ đề Subtitle (hỗ trợ song ngữ Tiếng Việt & Tiếng Anh theo Cài Đặt).
///    - Xử lý âm thanh chạy chữ mượt mà: Phát loop liền mạch trong lúc chữ chạy, gõ xong tự ngắt sạch sẽ không bị đè âm!
///    - Hỗ trợ SKIP THOẠI chuẩn: Bấm Chuột Trái hoặc Space để hiện chữ ngay lập tức hoặc bỏ qua chờ đọc.
/// 5. Nếu ĐỦ TẤT CẢ VẬT PHẨM:
///    - Khóa di chuyển người chơi -> Fade đen màn hình mượt mà -> Load sang Scene Map04!
/// </summary>
public class Map03ExitDoorTrigger : MonoBehaviour
{
    [System.Serializable]
    public class RequiredItemEntry
    {
        [Tooltip("Tên chính của vật phẩm trong Inventory")]
        public string itemName = "Bua";

        [Tooltip("Các tên gọi tương đương (cách nhau bởi dấu phẩy) phòng khi đặt tên khác")]
        public string alternateNames = "Bua,Bùa,Talisman,bua,bùa";

        [Tooltip("Số lượng bắt buộc cần có trong túi (Mặc định: 1)")]
        [Min(1)]
        public int requiredAmount = 1;

        public RequiredItemEntry(string name, string alternates, int amount = 1)
        {
            itemName = name;
            alternateNames = alternates;
            requiredAmount = Mathf.Max(1, amount);
        }

        public bool Matches(string itemInBag)
        {
            if (string.IsNullOrEmpty(itemInBag)) return false;
            string cleanBag = itemInBag.Trim().ToLower();

            if (cleanBag == itemName.Trim().ToLower()) return true;

            if (!string.IsNullOrEmpty(alternateNames))
            {
                string[] splits = alternateNames.Split(',');
                foreach (var s in splits)
                {
                    if (cleanBag == s.Trim().ToLower()) return true;
                }
            }
            return false;
        }
    }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "Có vẻ như mình vẫn còn thiếu đồ gì đó...";
        [TextArea(2, 4)]
        public string englishDialogue = "Looks like I'm still missing something...";
        public float holdDuration = 3.0f;
    }

    [Header("1. Cấu Hình Scene Đích (Target Scene)")]
    [Tooltip("Tên Scene tiếp theo cần chuyển đến (Mặc định: Map04)")]
    public string targetSceneName = "Map04";

    [Header("2. Danh Sách Các Vật Phẩm Bắt Buộc Phải Có (Kèm Số Lượng)")]
    public List<RequiredItemEntry> requiredItems = new List<RequiredItemEntry>()
    {
        new RequiredItemEntry("Bua", "Bùa,Talisman,Paper,bua,bùa", 1),
        new RequiredItemEntry("ArmsL", "armsl,Arms_L,ArmL,tay trai,tay_trai", 1),
        new RequiredItemEntry("ArmsR", "armsr,Arms_R,ArmR,tay phai,tay_phai", 1),
        new RequiredItemEntry("FeetL", "FeelL,feetl,feell,Feet_L,chan trai,chan_trai", 1),
        new RequiredItemEntry("FeetR", "FeelR,feetr,feelr,Feet_R,chan phai,chan_phai", 1),
        new RequiredItemEntry("Head", "head,Dau,Đầu,đầu,dau", 1),
        new RequiredItemEntry("Gore", "gore,NoiTang,Nội Tạng,thit,Thịt", 2)
    };

    [Header("3. Lời Thoại Khi Chưa Đủ Đồ (Hỗ Trợ Tiếng Việt & Tiếng Anh)")]
    public DialogueLine[] missingItemsDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Có vẻ như mình vẫn còn thiếu đồ gì đó...",
            englishDialogue = "Looks like I'm still missing something...",
            holdDuration = 3.0f
        }
    };

    [Header("4. Cấu Hình Phụ Đề & Gõ Chữ (Subtitles)")]
    public TextMeshProUGUI subtitleTextUI;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.035f;
    public bool useFadeEffect = true;
    public float fadeDuration = 0.25f;
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;
    public float dialogueCooldown = 3.5f; // Chống lặp thoại liên tục khi đứng trong trigger

    [Header("5. Hiệu Ứng Chuyển Scene (Transition)")]
    [Tooltip("Âm thanh bước qua cửa hầm / gió rít khi chuyển Scene")]
    public AudioClip passDoorSound;
    [Range(0f, 1f)] public float passSoundVolume = 0.9f;
    public float sceneFadeOutDuration = 1.0f;

    // --- Private Variables ---
    private AudioSource audioSource;
    private bool isTransitioning = false;
    private bool isDialoguePlaying = false;
    private float lastDialogueTime = -999f;
    private Coroutine dialogueCoroutine;

    // Skip thoại (Chuột Trái / Space)
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private bool skipWaitRequested = false;
    private string currentFullText = "";

    void Awake()
    {
        EnsureDefaultRequirements();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        // Đảm bảo BoxCollider là isTrigger
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.isTrigger = true;
        }
    }

    void OnValidate()
    {
        EnsureDefaultRequirements();
    }

    /// <summary>
    /// Tự động sửa chữa và cập nhật danh sách 6 bộ phận + 2 Gore nếu Inspector bị lưu đè cấu hình cũ
    /// </summary>
    public void EnsureDefaultRequirements()
    {
        if (requiredItems == null) requiredItems = new List<RequiredItemEntry>();

        bool hasGore = false;
        foreach (var req in requiredItems)
        {
            if (req.itemName.Trim().ToLower() == "gore")
            {
                hasGore = true;
                if (req.requiredAmount < 2) req.requiredAmount = 2;
                if (string.IsNullOrEmpty(req.alternateNames)) req.alternateNames = "gore,NoiTang,Nội Tạng,thit,Thịt";
            }
            else
            {
                if (req.requiredAmount <= 0) req.requiredAmount = 1;
            }
        }

        if (!hasGore)
        {
            requiredItems.Add(new RequiredItemEntry("Gore", "gore,NoiTang,Nội Tạng,thit,Thịt", 2));
        }

        // Đảm bảo các bộ phận cơ bản không bị thiếu
        EnsureItemExists("Bua", "Bùa,Talisman,Paper,bua,bùa", 1);
        EnsureItemExists("ArmsL", "armsl,Arms_L,ArmL,tay trai,tay_trai", 1);
        EnsureItemExists("ArmsR", "armsr,Arms_R,ArmR,tay phai,tay_phai", 1);
        EnsureItemExists("FeetL", "FeelL,feetl,feell,Feet_L,chan trai,chan_trai", 1);
        EnsureItemExists("FeetR", "FeelR,feetr,feelr,Feet_R,chan phai,chan_phai", 1);
        EnsureItemExists("Head", "head,Dau,Đầu,đầu,dau", 1);
    }

    private void EnsureItemExists(string name, string alternates, int amount)
    {
        foreach (var r in requiredItems)
        {
            if (r.itemName.Trim().ToLower() == name.Trim().ToLower())
            {
                if (r.requiredAmount <= 0) r.requiredAmount = amount;
                return;
            }
        }
        requiredItems.Add(new RequiredItemEntry(name, alternates, amount));
    }

    void Start()
    {
        FindSubtitleUI();
    }

    void Update()
    {
        if (!isDialoguePlaying) return;

        // Bấm Chuột Trái hoặc Phím Space để skip nhanh thoại
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
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

    private void FindSubtitleUI()
    {
        if (subtitleTextUI != null) return;

        // 1. Tìm trong BedSleepCutscene
        BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
        if (bed != null && bed.subtitleTextUI != null)
        {
            subtitleTextUI = bed.subtitleTextUI;
            return;
        }

        // 2. Tìm trong CorpseDialogueInteractable
        CorpseDialogueInteractable corpse = Object.FindFirstObjectByType<CorpseDialogueInteractable>(FindObjectsInactive.Include);
        if (corpse != null && corpse.subtitleTextUI != null)
        {
            subtitleTextUI = corpse.subtitleTextUI;
            return;
        }

        // 3. Quét tất cả TextMeshProUGUI
        TextMeshProUGUI[] tmps = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in tmps)
        {
            string n = t.name.ToLower();
            if (n.Contains("subtitle") || n.Contains("sub") || n.Contains("dialogue") || n.Contains("thoai"))
            {
                subtitleTextUI = t;
                return;
            }
        }
    }

    // =========================================================================
    // TRIGGER CHECK: CHẠM VÀO CỔNG LÀ TỰ ĐỘNG XỬ LÝ NGAY
    // =========================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning) return;

        if (IsPlayer(other))
        {
            TryPassGate();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isTransitioning || isDialoguePlaying) return;

        if (IsPlayer(other))
        {
            // Nếu người chơi vẫn đứng trong trigger và hết cooldown -> Kiểm tra lại
            if (Time.time - lastDialogueTime >= dialogueCooldown)
            {
                TryPassGate();
            }
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        if (other.GetComponentInParent<MovePl>() != null) return true;
        if (other.transform.root.GetComponentInChildren<MovePl>() != null) return true;
        return false;
    }

    // =========================================================================
    // KIỂM TRA ĐIỀU KIỆN ĐỦ CÁC VẬT PHẨM VÀ ĐỦ SỐ LƯỢNG (VÍ DỤ GORE X2)
    // =========================================================================

    public bool CheckHasAllRequiredItems(out List<string> missingList)
    {
        missingList = new List<string>();

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[Map03ExitDoorTrigger] ⚠️ Không tìm thấy InventoryManager.Instance trong Scene!");
            missingList.Add("InventoryManager missing");
            return false;
        }

        string[] heldItems = InventoryManager.Instance.heldItems;
        List<string> validBag = new List<string>();
        if (heldItems != null)
        {
            foreach (var it in heldItems)
            {
                if (!string.IsNullOrEmpty(it)) validBag.Add(it);
            }
        }

        // Mảng đánh dấu các slot đã tính để không đếm trùng slot
        bool[] slotUsed = new bool[heldItems != null ? heldItems.Length : 0];

        foreach (var req in requiredItems)
        {
            int targetAmount = Mathf.Max(1, req.requiredAmount);
            int foundCount = 0;

            if (heldItems != null)
            {
                for (int i = 0; i < heldItems.Length; i++)
                {
                    if (slotUsed[i]) continue;
                    if (string.IsNullOrEmpty(heldItems[i])) continue;

                    if (req.Matches(heldItems[i]))
                    {
                        slotUsed[i] = true;
                        foundCount++;
                        if (foundCount >= targetAmount) break;
                    }
                }
            }

            if (foundCount < targetAmount)
            {
                int missingCount = targetAmount - foundCount;
                missingList.Add($"{req.itemName} (cần {targetAmount}, thiếu {missingCount})");
            }
        }

        return (missingList.Count == 0);
    }

    private void TryPassGate()
    {
        if (isTransitioning) return;

        List<string> missing;
        bool hasAllItems = CheckHasAllRequiredItems(out missing);

        if (hasAllItems)
        {
            Debug.Log("<color=green><b>[Map03ExitDoorTrigger] 🚪 ĐÃ CÓ ĐỦ TẤT CẢ VẬT PHẨM (BAO GỒM 2 GORE)! Đang chuyển sang Scene: " + targetSceneName + "...</b></color>");
            StartCoroutine(ProceedToNextSceneRoutine());
        }
        else
        {
            // In danh sách đồ trong túi ra Console để dễ kiểm tra
            List<string> bagItems = new List<string>();
            if (InventoryManager.Instance != null && InventoryManager.Instance.heldItems != null)
            {
                foreach (var s in InventoryManager.Instance.heldItems) if (!string.IsNullOrEmpty(s)) bagItems.Add(s);
            }
            Debug.Log($"<color=yellow><b>[Map03ExitDoorTrigger] ⚠️ Chưa đủ đồ! Trong túi có: [{string.Join(", ", bagItems)}]. Còn thiếu ({missing.Count} mục): {string.Join(", ", missing)}. Tự động hiện thoại.</b></color>");

            if (!isDialoguePlaying && Time.time - lastDialogueTime >= dialogueCooldown)
            {
                lastDialogueTime = Time.time;
                PlayMissingItemsDialogue();
            }
        }
    }

    // =========================================================================
    // CHUYỂN SCENE MƯỢT MÀ (FADE TO BLACK & LOAD MAP04)
    // =========================================================================

    private IEnumerator ProceedToNextSceneRoutine()
    {
        isTransitioning = true;

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }

        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            subtitleTextUI.gameObject.SetActive(false);
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // 1. Khóa di chuyển của Player
        MovePl playerMove = Object.FindFirstObjectByType<MovePl>();
        if (playerMove != null)
        {
            playerMove.isCameraLocked = true;
            playerMove.SetMovementState(false);
            playerMove.enabled = false;
        }

        // 2. Phát âm thanh bước qua cửa
        if (passDoorSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(passDoorSound, passSoundVolume);
        }

        // 3. Fade màn hình ra màu đen
        yield return StartCoroutine(FadeScreenToBlack(sceneFadeOutDuration));

        // 4. LƯU DỮ LIỆU INVENTORY, ĐÈN PIN VÀ MÁY QUAY VÀO JSON TRƯỚC KHI TẢI MAP MỚI
        GameSaveManager.UnlockLevel(4);
        GameSaveManager.SetCurrentLevel(4);
        InventoryManager.hasUnlockedBackpack = true;
        PlayerPrefs.SetInt("Global_Has_Backpack", 1);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SaveInventoryData();
            Debug.Log($"[Map03ExitDoorTrigger] 🎒 Đã lưu {InventoryManager.Instance.GetItemCount()} món đồ trước khi sang {targetSceneName}!");
        }
        if (FlashlightToggle.Instance != null)
        {
            FlashlightToggle.Instance.SaveFlashlightData();
            Debug.Log($"[Map03ExitDoorTrigger] 🔦 Đã lưu % Pin: {FlashlightToggle.Instance.currentBattery:F1}% trước khi sang {targetSceneName}!");
        }
        CamcorderUI.MarkCameraPickedUp();
        PlayerPrefs.Save();
        GameSaveManager.SaveGame();

        // 5. Gọi SceneLoader (nếu có) hoặc SceneManager
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private IEnumerator FadeScreenToBlack(float duration)
    {
        GameObject canvasObj = new GameObject("NextSceneFadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 999999;

        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject imgObj = new GameObject("BlackOverlay");
        imgObj.transform.SetParent(canvasObj.transform, false);

        UnityEngine.UI.Image blackImg = imgObj.AddComponent<UnityEngine.UI.Image>();
        blackImg.color = new Color(0f, 0f, 0f, 0f);
        blackImg.raycastTarget = false;

        RectTransform rt = blackImg.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            blackImg.color = new Color(0f, 0f, 0f, t);
            yield return null;
        }

        blackImg.color = Color.black;
    }

    // =========================================================================
    // PHÁT THOẠI SUBTITLE KHI THIẾU ĐỒ (HỖ TRỢ ĐỔI NGÔN NGỮ VI / EN THEO SETTINGS)
    // =========================================================================

    private void PlayMissingItemsDialogue()
    {
        if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
        dialogueCoroutine = StartCoroutine(DialogueSequenceRoutine());
    }

    private IEnumerator DialogueSequenceRoutine()
    {
        isDialoguePlaying = true;
        FindSubtitleUI();

        if (subtitleTextUI != null)
        {
            subtitleTextUI.gameObject.SetActive(true);
            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;
            subtitleTextUI.text = "";
        }

        foreach (var line in missingItemsDialogues)
        {
            if (line == null) continue;

            // ĐỌC NGÔN NGỮ TỪ SETTINGS MANAGER (VIỆT NAM / ENGLISH)
            string lang = SettingsManager.currentLanguage;
            string textToShow = (lang == "VI") ? line.vietnameseDialogue : line.englishDialogue;
            if (string.IsNullOrEmpty(textToShow)) textToShow = line.vietnameseDialogue;
            if (string.IsNullOrEmpty(textToShow)) textToShow = line.englishDialogue;
            if (string.IsNullOrEmpty(textToShow)) continue;

            currentFullText = textToShow;
            isTyping = true;
            skipRequested = false;
            isWaitingForNextLine = false;
            skipWaitRequested = false;

            // 1. BẬT ÂM THANH CHẠY CHỮ (LOOP LIỀN MẠCH)
            if (dialogueSound != null && audioSource != null)
            {
                audioSource.spatialBlend = 0f;
                audioSource.clip = dialogueSound;
                audioSource.volume = dialogueVolume;
                audioSource.loop = true;
                audioSource.time = 0f;
                audioSource.Play();
            }

            // 2. GÕ CHỮ TYPEWRITER
            if (useTypewriterEffect && subtitleTextUI != null)
            {
                subtitleTextUI.text = "";
                for (int i = 0; i < textToShow.Length; i++)
                {
                    if (skipRequested)
                    {
                        subtitleTextUI.text = textToShow;
                        break;
                    }

                    subtitleTextUI.text = textToShow.Substring(0, i + 1);
                    yield return new WaitForSeconds(typewriterSpeed);
                }
            }
            else if (subtitleTextUI != null)
            {
                subtitleTextUI.text = textToShow;
            }

            isTyping = false;
            if (subtitleTextUI != null) subtitleTextUI.text = textToShow;

            // 3. DỪNG NGAY ÂM THANH CHẠY CHỮ KHI GÕ XONG HOẶC KHI BẤM SKIP
            if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
            {
                audioSource.Stop();
            }

            // 4. GIỮ THỜI GIAN ĐỂ NGƯỜI CHƠI ĐỌC THOẠI (CHO PHÉP BẤM SKIP ĐỂ QUA NHANH)
            isWaitingForNextLine = true;
            skipWaitRequested = false;
            float waitTimer = 0f;
            float targetHoldTime = (line.holdDuration > 0.1f) ? line.holdDuration : 3.0f;

            while (waitTimer < targetHoldTime && !skipWaitRequested)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }

            isWaitingForNextLine = false;

            // 5. FADE OUT MỜ DẦN KHI KẾT THÚC CÂU (NẾU KHÔNG SKIP)
            if (useFadeEffect && subtitleTextUI != null && !skipWaitRequested)
            {
                Color origColor = subtitleTextUI.color;
                float fadeTimer = 0f;
                while (fadeTimer < fadeDuration)
                {
                    fadeTimer += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeDuration);
                    subtitleTextUI.color = new Color(origColor.r, origColor.g, origColor.b, alpha);
                    yield return null;
                }
                subtitleTextUI.color = origColor;
            }

            if (subtitleTextUI != null)
            {
                subtitleTextUI.text = "";
            }
        }

        // TẮT HOÀN TOÀN ÂM THANH & SUBTITLE KHI HOÀN TẤT
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
        {
            audioSource.Stop();
        }

        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            subtitleTextUI.gameObject.SetActive(false);
        }

        isDialoguePlaying = false;
        dialogueCoroutine = null;
    }

    void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
        {
            audioSource.Stop();
        }
        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            subtitleTextUI.gameObject.SetActive(false);
        }
        isDialoguePlaying = false;
    }
}
