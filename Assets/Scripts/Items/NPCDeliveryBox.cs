using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NPCDeliveryBox : MonoBehaviour, IInteractable
{
    [Header("1. Chữ Nhắc Tương Tác (Prompt UI)")]
    public string englishPrompt = "Take Package";
    public string vietnamesePrompt = "Nhận thùng hàng";

    [Header("2. Thùng Hàng Trên Tay Người Chơi (Player Hand Box)")]
    [Tooltip("Kéo Object thùng hàng setup sẵn trên tay Player/trước camera vào đây để tự động bật active khi màn hình đen")]
    public GameObject playerHandBox;

    [Header("3. Mô Hình Thùng Hàng Trên Tay NPC Cần Ẩn")]
    [Tooltip("Để trống sẽ tự lấy chính Object này! Đồ trên tay NPC sẽ ẩn khi màn hình đen")]
    public GameObject boxModelToHide;

    [Header("4. Cửa Cần Tự Động Đóng Lại")]
    [Tooltip("Kéo Object Cửa (chứa script DoorExit / DoorMap01) vào đây để tự động đóng cửa lại khi màn hình đen")]
    public DoorExit doorToClose;

    [Header("5. Cấu Hình Thời Gian Màn Hình Đen (Fade Transition)")]
    [Tooltip("Thời gian giữ màn hình bình thường trước khi đen (mặc định: 0.5s)")]
    public float delayBeforeFade = 0.5f;

    [Tooltip("Thời gian màn hình đen dần (mặc định: 2.0s)")]
    public float fadeDuration = 2.0f;

    [Tooltip("Thời gian giữ màn hình đen hoàn toàn SAU KHI CỬA ĐÓNG để mở sáng lại (mặc định: 1.0s)")]
    public float holdBlackDuration = 1.0f;

    [Tooltip("Thời gian màn hình từ đen mở sáng trở lại bình thường (mặc định: 2.0s - Ngang với thời gian tối đen)")]
    public float fadeInDuration = 2.0f;

    [Header("6. UI Màn Hình Đen (Fade Image - Tùy chọn)")]
    [Tooltip("Kéo UI Image màu đen phủ kín màn hình vào đây. Nếu ĐỂ TRỐNG code sẽ TỰ ĐỘNG TẠO 1 màn hình đen chuẩn!")]
    public Image fadeScreenImage;

    [Header("7. Tự Động Thêm Vào Túi Đồ (Tùy chọn)")]
    public bool addToInventory = true;
    public string questItemName = "Thùng Hàng";

    [Header("8. NPC Cần Ẩn Đi Sau Khi Nhận Hàng")]
    [Tooltip("Kéo GameObject NPC Johnson vào đây để tự động tắt active khi màn hình đen")]
    public GameObject npcToDisableAfterDelivery;

    [Header("9. Đàn Gián Xuất Hiện Sau Khi NPC Biến Mất")]
    [Tooltip("Kéo GameObject Cockroach (chứa 3 con gián) vào đây. Mặc định sẽ bị ẩn lúc đầu và tự động bật Active khi NPC biến mất!")]
    public GameObject cockroachesToEnableAfterNPC;

    [Header("10. Thoại Sau Khi Nhận Hàng & Đóng Cửa (Hàng Vẫn Trên Tay)")]
    [Tooltip("Danh sách câu thoại phát ngay khi màn hình mở sáng lại và cửa đã đóng")]
    public SmartInteractionDialogue.DialogueLine[] postDeliveryDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Thùng hàng gì mà nặng thế này...",
            englishDialogue = "What's in this package that makes it so heavy...",
            holdDuration = 2.5f
        },
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Đem vào phòng rồi đặt xuống chiếu xem bên trong có gì nào.",
            englishDialogue = "Let's take it into the room and place it on the mat to see what's inside.",
            holdDuration = 3.0f
        }
    };

    [Header("10.1. Âm Thanh Thoại Gõ Chữ (Dialogue SFX)")]
    [Tooltip("Gói âm thanh lồng tiếng / gõ chữ khi hiện phụ đề (5s blip)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    private Collider boxCollider;
    private InteractPrompt interactPrompt;
    private bool isUnlocked = false;
    private bool isProcessingDelivery = false;
    private FadeCoroutineRunner coroutineRunner;

    void Awake()
    {
        boxCollider = GetComponent<Collider>();
        interactPrompt = GetComponent<InteractPrompt>();
        if (boxModelToHide == null) boxModelToHide = gameObject;
    }

    void Start()
    {
        // Tự tạo hoặc cấu hình InteractPrompt để hiển thị chữ [F] Nhận thùng hàng
        if (interactPrompt == null)
        {
            interactPrompt = gameObject.AddComponent<InteractPrompt>();
        }
        interactPrompt.englishPrompt = englishPrompt;
        interactPrompt.vietnamesePrompt = vietnamesePrompt;

        // Đảm bảo thùng hàng trên tay player ban đầu bị ẩn
        if (playerHandBox != null && playerHandBox != boxModelToHide && !isProcessingDelivery)
        {
            playerHandBox.SetActive(false);
        }

        if (doorToClose == null)
        {
            doorToClose = Object.FindFirstObjectByType<DoorExit>();
        }

        // TỰ ĐỘNG TÌM VÀ ẨN ĐÀN GIÁN LÚC ĐẦU GAME KHI NPC CÒN ĐANG ĐỨNG ĐÓ
        if (cockroachesToEnableAfterNPC == null)
        {
            GameObject roachParent = GameObject.Find("Cockroach");
            if (roachParent != null) cockroachesToEnableAfterNPC = roachParent;
        }

        if (cockroachesToEnableAfterNPC != null && !isProcessingDelivery)
        {
            cockroachesToEnableAfterNPC.SetActive(false);
            Debug.Log("[NPCDeliveryBox] 🪳 Đã ẩn đàn gián lúc đầu game, sẽ hiện lên sau khi NPC giao hàng xong!");
        }

        // MẶC ĐỊNH KHÓA TƯƠNG TÁC THÙNG HÀNG LÚC MỚI VÀO GAME
        LockInteraction();
    }

    public void LockInteraction()
    {
        isUnlocked = false;
        if (boxCollider != null) boxCollider.enabled = false;
        this.enabled = false;
    }

    public void UnlockInteraction()
    {
        isUnlocked = true;
        if (boxCollider != null) boxCollider.enabled = true;
        this.enabled = true;
    }

    public void Interact()
    {
        if (!isUnlocked || isProcessingDelivery) return;

        EnsureFadeImageExists();
        Debug.Log("[NPCDeliveryBox] 📦 Bấm [F] nhận thùng hàng! Khởi động chuỗi hiệu ứng delay 2s & chuyển cảnh màn hình...");

        if (coroutineRunner != null)
        {
            coroutineRunner.StartCoroutine(DeliverySequenceRoutine());
        }
        else
        {
            StartCoroutine(DeliverySequenceRoutine());
        }
    }

    IEnumerator DeliverySequenceRoutine()
    {
        isProcessingDelivery = true;
        LockInteraction();

        // ẨN NGAY LẬP TỨC GIAO DIỆN [F] ĐỂ KHÔNG BỊ LỘ TRÊN MÀN HÌNH ĐEN
        InteractPro interactPro = Object.FindFirstObjectByType<InteractPro>();
        if (interactPro != null && interactPro.interactionUI != null)
        {
            interactPro.interactionUI.SetActive(false);
        }
        GameObject pressF = GameObject.Find("PressF");
        if (pressF != null) pressF.SetActive(false);

        // KHÓA GÓC NHÌN CAMERA CHUỘT & DI CHUYỂN CỦA PLAYER LÚC BẤM NHẬN HÀNG
        MovePl playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = true;
            playerMovePl.SetMovementState(false);
        }

        // 1. Chờ 0.5s đầu tiên (màn hình bình thường)
        if (delayBeforeFade > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFade);
        }

        // 2. Đảm bảo UI màn hình đen tồn tại
        EnsureFadeImageExists();

        // 3. Fade đen dần trong 1.5s (Tổng delay đúng 2.0s)
        float elapsed = 0f;
        Color color = fadeScreenImage.color;
        color.a = 0f;
        fadeScreenImage.color = color;
        fadeScreenImage.gameObject.SetActive(true);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeScreenImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeScreenImage.color = color;

        // 4. THỰC HIỆN CÁC THAO TÁC KHI MÀN HÌNH ĐÃ ĐEN HOÀN TOÀN (2.0s):

        // a) Ẩn thùng hàng trên tay NPC
        if (boxModelToHide != null)
        {
            if (boxModelToHide == gameObject)
            {
                MeshRenderer mr = GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>();
                foreach (var r in childRenderers) if (r != null) r.enabled = false;
            }
            else
            {
                boxModelToHide.SetActive(false);
            }
        }

        // b) TẮT ACTIVE CON NPC JOHNSON
        if (npcToDisableAfterDelivery != null)
        {
            npcToDisableAfterDelivery.SetActive(false);
        }
        else
        {
            NPCDialogueCutscene npc = Object.FindFirstObjectByType<NPCDialogueCutscene>();
            if (npc != null) npc.gameObject.SetActive(false);
        }

        // e) BẬT ACTIVE ĐÀN GIÁN (COCKROACH) XUẤT HIỆN Ở NGOÀI CỬA KHI NPC ĐÃ BIẾN MẤT
        if (cockroachesToEnableAfterNPC != null)
        {
            cockroachesToEnableAfterNPC.SetActive(true);
            Debug.Log("[NPCDeliveryBox] 🪳 NPC Johnson đã biến mất! Đã bật Active đàn gián (Cockroach) sẵn sàng ở ngoài cửa!");
        }

        // c) Hiện thùng hàng trên tay Player (nếu đã kéo vào Inspector)
        if (playerHandBox != null && playerHandBox != boxModelToHide)
        {
            playerHandBox.SetActive(true);

            // TẮT ĐỔ BÓNG TRÊN TAY PLAYER
            MeshRenderer[] renderers = playerHandBox.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer mr in renderers)
            {
                if (mr != null)
                {
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        // d) Đóng cửa lại & phát âm thanh đóng cửa
        if (doorToClose == null) doorToClose = Object.FindFirstObjectByType<DoorExit>();
        if (doorToClose != null)
        {
            doorToClose.CloseDoor(false);

            // ĐỜI CỬA TRƯỢT ĐÓNG HOÀN TOÀN TRONG MÀN HÌNH ĐEN TRƯỚC KHI SÁNG LẠI
            float waitTimer = 0f;
            float maxWait = 4.0f;
            while (!doorToClose.IsFullyClosed() && waitTimer < maxWait)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            WindowAmbienceController win = Object.FindFirstObjectByType<WindowAmbienceController>();
            if (win != null) win.StartVolumeFade(0.35f);
        }

        // e) Cộng đồ vào inventory nếu bật
        if (addToInventory)
        {
            InventoryManager inv = Object.FindFirstObjectByType<InventoryManager>();
            if (inv != null)
            {
                inv.AddQuestItem(questItemName);
            }
        }

        // Giữ màn hình đen thêm đúng 1s SAU KHI CỬA ĐÃ ĐÓNG HOÀN TOÀN rồi mới mở sáng lại
        float holdAfterClose = Mathf.Max(holdBlackDuration, 1.0f);
        yield return new WaitForSeconds(holdAfterClose);

        // 5. Fade màn hình mở sáng trở lại (Fade Out màn hình đen / Fade In trò chơi)
        // MỞ LẠI CAMERA CHUỘT & DI CHUYỂN NGAY KHI BẮT ĐẦU SÁNG TRỞ LẠI
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = false;
            playerMovePl.SetMovementState(true);
            playerMovePl.SyncRotationWithCurrentCamera();
        }

        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(1f - (elapsed / fadeInDuration));
            fadeScreenImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeScreenImage.color = color;
        fadeScreenImage.gameObject.SetActive(false);

        isProcessingDelivery = false;
        Debug.Log("[NPCDeliveryBox] ✅ Đã hoàn thành quá trình giao nhận hàng & đóng cửa, NPC đã ẩn!");

        // KÍCH HOẠT THOẠI SAU KHI NHẬN HÀNG & ĐÓNG CỬA (HÀNG VẪN TRÊN TAY)
        if (postDeliveryDialogues != null && postDeliveryDialogues.Length > 0 && coroutineRunner != null)
        {
            coroutineRunner.StartCoroutine(PlayDeliveryDialoguesRoutine());
        }
    }

    IEnumerator PlayDeliveryDialoguesRoutine()
    {
        if (postDeliveryDialogues == null || postDeliveryDialogues.Length == 0) yield break;

        TMPro.TextMeshProUGUI subtitleTextUI = null;
        BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
        if (bed != null && bed.subtitleTextUI != null) subtitleTextUI = bed.subtitleTextUI;
        if (subtitleTextUI == null)
        {
            GameIntroManager intro = Object.FindFirstObjectByType<GameIntroManager>(FindObjectsInactive.Include);
            if (intro != null && intro.subtitleTextUI != null) subtitleTextUI = intro.subtitleTextUI;
        }
        if (subtitleTextUI == null)
        {
            GameObject subObj = GameObject.Find("SubtitleText");
            if (subObj == null) subObj = GameObject.Find("Subtitle");
            if (subObj == null) subObj = GameObject.Find("DialogueText");
            if (subObj != null) subtitleTextUI = subObj.GetComponent<TMPro.TextMeshProUGUI>();
        }

        if (subtitleTextUI == null) yield break;

        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        AudioSource aSource = null;
        if (coroutineRunner != null)
        {
            aSource = coroutineRunner.GetComponent<AudioSource>();
            if (aSource == null) aSource = coroutineRunner.gameObject.AddComponent<AudioSource>();
            aSource.spatialBlend = 0f;
            aSource.playOnAwake = false;
        }

        if (subtitleTextUI.transform.parent != null) subtitleTextUI.transform.parent.gameObject.SetActive(true);
        subtitleTextUI.gameObject.SetActive(true);

        // Chờ 1 frame để click tương tác ban đầu trôi qua
        yield return null;

        for (int i = 0; i < postDeliveryDialogues.Length; i++)
        {
            var line = postDeliveryDialogues[i];
            if (line == null) continue;

            string fullText = (SettingsManager.currentLanguage == "VI") ? line.vietnameseDialogue : line.englishDialogue;
            if (string.IsNullOrEmpty(fullText)) fullText = line.vietnameseDialogue;
            if (string.IsNullOrEmpty(fullText)) fullText = line.englishDialogue;
            if (string.IsNullOrEmpty(fullText)) continue;

            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;

            if (dialogueSound != null && aSource != null)
            {
                aSource.clip = dialogueSound;
                aSource.volume = dialogueVolume;
                aSource.loop = true;
                aSource.time = 0f;
                aSource.Play();
            }

            float lineStartTime = Time.time;
            bool skip = false;
            subtitleTextUI.text = "";
            for (int c = 1; c <= fullText.Length; c++)
            {
                if (Time.time - lineStartTime > 0.2f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                {
                    skip = true;
                    subtitleTextUI.text = fullText;
                    break;
                }
                subtitleTextUI.text = fullText.Substring(0, c) + "_";
                yield return new WaitForSeconds(0.03f);
            }

            if (aSource != null && aSource.isPlaying) aSource.Stop();
            subtitleTextUI.text = fullText;

            float timer = 0f;
            float hold = (line.holdDuration > 0f) ? line.holdDuration : 2.5f;
            bool blink = true;
            float blinkTimer = 0f;
            while (timer < hold)
            {
                if (timer > 0.2f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))) break;
                timer += Time.deltaTime;
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= 0.4f)
                {
                    blinkTimer = 0f;
                    blink = !blink;
                    subtitleTextUI.text = fullText + (blink ? " _" : "  ");
                }
                yield return null;
            }

            float fadeElapsed = 0f;
            while (fadeElapsed < 0.2f)
            {
                fadeElapsed += Time.deltaTime;
                sc.a = Mathf.Lerp(1f, 0f, fadeElapsed / 0.2f);
                subtitleTextUI.color = sc;
                yield return null;
            }
        }

        subtitleTextUI.text = "";
        Color finalColor = subtitleTextUI.color;
        finalColor.a = 1f;
        subtitleTextUI.color = finalColor;

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
    }

    void EnsureFadeImageExists()
    {
        if (fadeScreenImage != null) return;

        if (coroutineRunner == null)
        {
            GameObject runnerObj = new GameObject("DeliveryFadeRunner");
            coroutineRunner = runnerObj.AddComponent<FadeCoroutineRunner>();
            DontDestroyOnLoad(runnerObj);
        }

        // Luôn tạo Canvas Overlay độc lập ở tầng hiển thị cao nhất (sortingOrder 9999)
        // để tuyệt đối không bị đè bởi bất kỳ UI / Menu nào khác
        GameObject canvasObj = new GameObject("DeliveryFadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);

        GameObject imageObj = new GameObject("DeliveryFadePanel");
        imageObj.transform.SetParent(canvasObj.transform, false);

        fadeScreenImage = imageObj.AddComponent<Image>();
        fadeScreenImage.color = new Color(0f, 0f, 0f, 0f);
        fadeScreenImage.raycastTarget = false;

        RectTransform rect = fadeScreenImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        imageObj.SetActive(false);
    }

    public void ShowPrompt()
    {
        if (!isUnlocked || isProcessingDelivery) return;
        if (interactPrompt != null) interactPrompt.ShowPrompt();
    }

    public void HidePrompt()
    {
        if (interactPrompt != null) interactPrompt.HidePrompt();
    }
}
