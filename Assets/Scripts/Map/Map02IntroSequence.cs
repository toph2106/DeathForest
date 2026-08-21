using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Map02IntroSequence : MonoBehaviour
{
    public static Map02IntroSequence Instance { get; private set; }
    public static bool isWakeUpCutsceneRunning { get; private set; } = false;

    [Header("1. Tham Chiếu Player Main & Vị Trí (Transforms)")]
    [Tooltip("Kéo GameObject Main (chứa MovePl) vào đây")]
    public MovePl playerMain;

    [Tooltip("Kéo PointSpawn (Object vị trí & góc nằm ngửa nhìn trời) vào đây")]
    public Transform pointSpawn;

    [Tooltip("Vị trí đứng của Player sau khi thức dậy (Tùy chọn. Nếu để trống sẽ LẤY ĐÚNG VỊ TRÍ GỐC CỦA MAIN TRONG MAP 02!)")]
    public Transform playerStandTransform;

    [Header("2. Cấu Hình Tọa Độ Mắt Đứng Của Main (Local Position)")]
    [Tooltip("Độ cao mắt đứng của Main Camera trong Main (Mặc định: 2.5 như bạn đã setup trong Main Camera)")]
    public float standingCameraLocalY = 2.5f;

    [Header("3. Tham Chiếu UI Phụ Đề (Subtitle UI)")]
    [Tooltip("Kéo TextMeshProUGUI (Subtitle Text) vào đây")]
    public TextMeshProUGUI subtitleTextUI;

    [Header("4. Cấu Hình Gõ Chữ Typewriter & Con Trỏ (Đồng Bộ 100% Map 01)")]
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.03f;
    public float holdTimePerLine = 3.2f;
    public bool showBlinkingCursor = true;
    public bool useFadeEffect = true;
    public float fadeDuration = 0.2f;

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "";
        [TextArea(2, 4)]
        public string englishDialogue = "";
        public float holdDuration = 3.2f;
    }

    [Header("5. Đợt Thoại 1: Mơ Hồ Tỉnh Dậy (Nhìn Bầu Trời)")]
    [Tooltip("Câu đầu tiên (Ugh) sẽ chạy ngay lúc đang Fade sáng dần lên. Các câu tiếp theo sẽ chạy sau khi Fade xong!")]
    public DialogueLine[] firstWakeUpDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Ugh",
            englishDialogue = "...",
            holdDuration = 3.0f
        },
        new DialogueLine
        {
            vietnameseDialogue = "Trời đêm nay đẹp nhỉ",
            englishDialogue = "...",
            holdDuration = 3.0f
        }
    };

    [Header("6. Ngồi Dậy 45 Độ & Đợt Thoại 2 (Sitting Up 45° Settings)")]
    [Tooltip("Thời gian camera xoay về 45 độ và nâng cao dần lên như đang ngồi dậy (giây - Mặc định: 2.5s)")]
    public float sitUpDuration = 2.5f;

    [Tooltip("Góc ngửa của camera lúc ngồi dậy (độ - Mặc định: -45 độ nhìn chéo lên tán cây)")]
    public float sitUpPitchAngle = -45.0f;

    public DialogueLine[] sittingUpDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Tf sao mình ở đâu đây",
            englishDialogue = "...",
            holdDuration = 3.5f
        }
    };

    [Header("7. Đứng Dậy Bay Về Đúng Tọa Độ (0, 2.5, 0) & Đợt Thoại 3")]
    [Tooltip("Thời gian camera bay mượt mà về đúng vị trí mắt đứng chuẩn (0, 2.5, 0) trong Player (giây - Mặc định: 2.0s)")]
    public float standUpDuration = 2.0f;

    public DialogueLine[] standingUpDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Khoan đã... cái gì thế kia?! Một... một cái xác sao?!!",
            englishDialogue = "Wait... what is that over there?! A... a corpse?!!",
            holdDuration = 3.5f
        }
    };

    [Header("8. Hiệu Ứng Sáng Dần Lên & Ngó Nghiêng")]
    [Tooltip("Thời gian màn hình từ đen xì sáng dần lên mượt mà (giây - Mặc định: 2.5s, không chớp nháy)")]
    public float fadeInDuration = 2.5f;

    [Tooltip("Biên độ ngó nghiêng lơ mơ sang 2 bên lúc nhìn lên trời (độ - Mặc định: 7.5 độ)")]
    public float groggySwayAngle = 7.5f;

    [Header("9. Âm Thanh (Audio Settings)")]
    [Tooltip("Tiếng thở dốc / choáng váng lúc vừa mở mắt")]
    public AudioClip breathWakeUpAudio;
    [Tooltip("Tiếng sột soạt quần áo/lá khô khi ngồi dậy")]
    public AudioClip rustlingGroundAudio;
    [Tooltip("Âm thanh gõ chữ phụ đề thoại (5s blip)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("10. Kích Hoạt Trigger Khác (Nếu Có)")]
    public GameObject truckTriggerObject;

    private CharacterController characterController;
    private Transform cameraTransform;

    private Vector3 targetPlayerWorldPos;
    private Quaternion targetPlayerWorldRot;

    private AudioSource audioSource;
    private Image fadeScreenImage;
    private Coroutine swayCoroutine;

    // --- BIẾN ĐIỀU KHIỂN CLICK CHUỘT QUA THOẠI NHANH (GIỐNG MAP 01) ---
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[Map02IntroSequence] ⚠️ Phát hiện script trùng trên GameObject '{gameObject.name}'. Tự động xóa bản thừa!");
            Destroy(this);
            return;
        }
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        // 1. TÌM PLAYER MAIN VÀ CAMERA
        if (playerMain == null)
        {
            playerMain = Object.FindFirstObjectByType<MovePl>();
        }

        if (playerMain != null)
        {
            characterController = playerMain.GetComponent<CharacterController>();
            cameraTransform = (playerMain.cameraTransform != null) ? playerMain.cameraTransform : (Camera.main != null ? Camera.main.transform : null);

            // Lấy chính xác tọa độ đứng gốc của Main trong Map 02
            if (playerStandTransform != null)
            {
                targetPlayerWorldPos = playerStandTransform.position;
                targetPlayerWorldRot = playerStandTransform.rotation;
            }
            else
            {
                targetPlayerWorldPos = playerMain.transform.position;
                targetPlayerWorldRot = playerMain.transform.rotation;
            }

            if (cameraTransform != null && cameraTransform.localPosition.y > 0.1f)
            {
                standingCameraLocalY = cameraTransform.localPosition.y;
            }
            if (standingCameraLocalY <= 0.1f) standingCameraLocalY = 2.5f;

            // ĐỒNG BỘ NGAY CHIỀU CAO CAMERA CHO MOVEPL TRÁNH BỊ LERP NHẦM SANG SỐ ÂM
            playerMain.SetStandingCamY(standingCameraLocalY);
        }

        if (pointSpawn == null)
        {
            GameObject pObj = GameObject.Find("PointSpawn") ?? GameObject.Find("SpawnNM");
            if (pObj != null) pointSpawn = pObj.transform;
        }

        if (subtitleTextUI == null)
        {
            subtitleTextUI = FindSubtitleTextUI();
        }

        // ẨN TOÀN BỘ UI MÁY QUAY, REC, ĐÈN PIN & TÂM NGẮM
        HideAllCamUIAndEquipment();

        if (truckTriggerObject != null)
        {
            truckTriggerObject.SetActive(true);
        }

        // BẮT ĐẦU CHUỖI CẮT CẢNH MỞ MÀN
        StartCoroutine(ExecuteMap02WakeUpRoutine());
    }

    void Update()
    {
        if (!isWakeUpCutsceneRunning) return;

        // Bấm chuột trái (Mouse 0) hoặc Space để qua thoại nhanh giống Map 01
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // BẤM LẦN 1 KHI ĐANG GÕ ➔ HIỆN TOÀN BỘ CHỮ NGAY LẬP TỨC
                isTyping = false;
                if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;
            }
            else if (isWaitingForNextLine)
            {
                // BẤM LẦN 2 KHI CHỮ ĐÃ ĐẦY ĐỦ ➔ SANG CÂU THOẠI TIẾP THEO NGAY LẬP TỨC
                skipRequested = true;
            }
        }
    }

    void HideAllCamUIAndEquipment()
    {
        // 1. Tắt đèn pin
        FlashlightToggle flashlight = Object.FindFirstObjectByType<FlashlightToggle>();
        if (flashlight != null)
        {
            flashlight.SetFlashlightState(false, false);
            flashlight.UnequipFlashlight();
        }

        // 2. Tắt toàn bộ UI máy quay Camcorder / REC / Pin (Kể cả DontDestroyOnLoad)
        CamcorderUI.ResetPickedUpCameraState();
        InventoryManager.ResetInventoryData();
        Map02CamcorderManager.hasTriggeredBlackout = false;
        if (CamcorderUI.Instance != null)
        {
            CamcorderUI.Instance.gameObject.SetActive(false);
        }

        CamcorderUI[] camUIs = Object.FindObjectsByType<CamcorderUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in camUIs)
        {
            c.gameObject.SetActive(false);
        }

        // CHỈ TẮT ROOT UI NẰM DƯỚI CANVAS (VD: GameObject Camcorder, CameraOverlay)
        // TUYỆT ĐỐI KHÔNG TẮT CÁC CON BÊN TRONG ĐỂ KHI NHẶT MÁY QUAY KHÔNG BỊ MẤT CHỮ REC!
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in allCanvases)
        {
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                Transform t = canvas.transform.GetChild(i);
                if (t == null) continue;
                string n = t.gameObject.name.ToLower();
                if (n.Contains("camcorder") || n.Contains("cameraoverlay") || n.Contains("pressf") || n.Contains("interact"))
                {
                    t.gameObject.SetActive(false);
                }
            }
        }

        SimpleCameraOverlay overlay = Camera.main != null ? Camera.main.GetComponent<SimpleCameraOverlay>() : Object.FindFirstObjectByType<SimpleCameraOverlay>();
        if (overlay != null) overlay.ResetCameraView();

        // 3. Tắt tâm ngắm chấm tròn & bàn tay lúc đang cutscene
        InteractPro interactPro = Object.FindFirstObjectByType<InteractPro>();
        if (interactPro != null)
        {
            if (interactPro.dotObject != null) interactPro.dotObject.SetActive(false);
            if (interactPro.handObject != null) interactPro.handObject.SetActive(false);
            if (interactPro.interactionUI != null) interactPro.interactionUI.SetActive(false);
        }
    }

    IEnumerator ExecuteMap02WakeUpRoutine()
    {
        isWakeUpCutsceneRunning = true;
        Debug.Log("[Map02IntroSequence] 🌲 BẮT ĐẦU CẮT CẢNH TỈNH DẬY MAP 02!");

        // 1. KHÓA HOÀN TOÀN DI CHUYỂN & GÓC QUAY CHUỘT
        if (playerMain != null)
        {
            playerMain.SetMovementState(false);
            playerMain.isCameraLocked = true;
        }

        // 2. ĐẶT MAIN Ở VỊ TRÍ ĐỨNG CHUẨN CỦA NÓ TRONG MAP 02
        if (playerMain != null)
        {
            if (characterController != null) characterController.enabled = false;
            playerMain.transform.position = targetPlayerWorldPos;
            playerMain.transform.rotation = targetPlayerWorldRot;
            if (characterController != null) characterController.enabled = true;
        }

        // 3. ĐẶT CAMERA TẠI ĐÚNG TỌA ĐỘ VÀ GÓC NẰM NGỬA NHÌN TRỜI CỦA POINTSPAWN
        Vector3 lyingWorldPos = (pointSpawn != null) ? pointSpawn.position : (targetPlayerWorldPos + new Vector3(0f, 0.25f, 0f));
        Quaternion lyingWorldRot = (pointSpawn != null) ? pointSpawn.rotation : Quaternion.Euler(-90f, targetPlayerWorldRot.eulerAngles.y, 0f);

        if (cameraTransform != null)
        {
            cameraTransform.position = lyingWorldPos;
            cameraTransform.rotation = lyingWorldRot;
        }

        EnsureFadeScreenImage();
        if (subtitleTextUI == null) subtitleTextUI = FindSubtitleTextUI();

        // MÀN HÌNH BAN ĐẦU ĐEN KỊT 100%
        if (fadeScreenImage != null)
        {
            fadeScreenImage.gameObject.SetActive(true);
            fadeScreenImage.color = Color.black;
        }

        // Chờ 0.8s trong bóng tối
        yield return new WaitForSeconds(0.8f);

        // =========================================================================
        // GIAI ĐOẠN 1: MÀN HÌNH SÁNG DẦN LÊN + KÍCH HOẠT THOẠI 1 (UGH)
        // =========================================================================
        if (breathWakeUpAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(breathWakeUpAudio, soundVolume);
        }

        // Chạy câu thoại 1 (Ugh) NGAY KHI BẮT ĐẦU FADE SÁNG DẦN LÊN
        Coroutine firstLineCoroutine = null;
        if (firstWakeUpDialogues != null && firstWakeUpDialogues.Length > 0 && firstWakeUpDialogues[0] != null)
        {
            firstLineCoroutine = StartCoroutine(PlaySingleDialogueLineRoutine(firstWakeUpDialogues[0]));
        }

        // FADE IN TỪ TỪ ÊM ÁI (1.0 -> 0.0)
        yield return StartCoroutine(FadeScreenInRoutine(fadeInDuration));

        // Chờ câu thoại đầu tiên hoàn tất (nếu người chơi chưa click qua)
        if (firstLineCoroutine != null)
        {
            yield return firstLineCoroutine;
        }

        // =========================================================================
        // SAU KHI FADE XONG: NGÓ NGHIÊNG NHÌN TRỜI & CHẠY TIẾP CÁC CÂU THOẠI CÒN LẠI
        // =========================================================================
        swayCoroutine = StartCoroutine(GroggyLookAroundRoutine(lyingWorldRot));

        // Chạy tiếp từ câu thứ 2 trở đi trong Đợt 1 (Ví dụ: "Trời đêm nay đẹp nhỉ")
        if (firstWakeUpDialogues != null && firstWakeUpDialogues.Length > 1)
        {
            for (int i = 1; i < firstWakeUpDialogues.Length; i++)
            {
                if (firstWakeUpDialogues[i] != null)
                {
                    yield return StartCoroutine(PlaySingleDialogueLineRoutine(firstWakeUpDialogues[i]));
                }
            }
        }

        // DỪNG NGÓ NGHIÊNG ĐỂ BẮT ĐẦU NGỒI DẬY
        if (swayCoroutine != null)
        {
            StopCoroutine(swayCoroutine);
            swayCoroutine = null;
        }

        // =========================================================================
        // GIAI ĐOẠN 2: XOAY CAMERA VỀ 45 ĐỘ VÀ NÂNG CAO LÊN (KIỂU ĐANG NGỒI DẬY)
        // =========================================================================
        Debug.Log("[Map02IntroSequence] 🧘 Đang xoay cam về 45 độ và ngồi dậy dần dần...");
        if (rustlingGroundAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(rustlingGroundAudio, soundVolume);
        }

        // Vị trí ngồi dậy: Ở giữa điểm nằm và vị trí mắt đứng chuẩn
        Vector3 finalStandEyeWorldPos = (playerMain != null) ? playerMain.transform.TransformPoint(new Vector3(0f, standingCameraLocalY, 0f)) : (targetPlayerWorldPos + new Vector3(0f, standingCameraLocalY, 0f));
        Vector3 sittingWorldPos = Vector3.Lerp(lyingWorldPos, finalStandEyeWorldPos, 0.5f);
        sittingWorldPos.y = targetPlayerWorldPos.y + (standingCameraLocalY * 0.5f);

        // Góc xoay ngồi dậy: 45 độ nhìn chéo lên theo hướng mặt người chơi
        Quaternion sittingWorldRot = Quaternion.Euler(sitUpPitchAngle, targetPlayerWorldRot.eulerAngles.y, 0f);

        if (cameraTransform != null)
        {
            Vector3 fromPos = cameraTransform.position;
            Quaternion fromRot = cameraTransform.rotation;

            float sitElapsed = 0f;
            while (sitElapsed < sitUpDuration)
            {
                sitElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(sitElapsed / sitUpDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                float wobbleY = Mathf.Sin(t * Mathf.PI * 2f) * 0.01f;

                cameraTransform.position = Vector3.Lerp(fromPos, sittingWorldPos, smoothT) + new Vector3(0f, wobbleY, 0f);
                cameraTransform.rotation = Quaternion.Slerp(fromRot, sittingWorldRot, smoothT);
                yield return null;
            }
            cameraTransform.position = sittingWorldPos;
            cameraTransform.rotation = sittingWorldRot;
        }

        // PHÁT ĐỢT THOẠI 2: ĐANG NGỒI DẬY (Ví dụ: "Tf sao mình ở đâu đây")
        if (sittingUpDialogues != null && sittingUpDialogues.Length > 0)
        {
            foreach (var line in sittingUpDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleDialogueLineRoutine(line));
                }
            }
        }

        // =========================================================================
        // GIAI ĐOẠN 3: ĐỨNG DẬY & BAY CAMERA NHẸ NHÀNG VỀ ĐÚNG (0, 2.5, 0) TRONG MAIN
        // =========================================================================
        Debug.Log($"[Map02IntroSequence] 🚶 Đứng dậy và bay Camera về đúng (0, {standingCameraLocalY}, 0) trong Main...");

        // Tọa độ và góc quay mắt đứng chuẩn 100% trong Main
        if (playerMain != null)
        {
            finalStandEyeWorldPos = playerMain.transform.TransformPoint(new Vector3(0f, standingCameraLocalY, 0f));
        }
        Quaternion finalStandEyeWorldRot = Quaternion.Euler(0f, targetPlayerWorldRot.eulerAngles.y, 0f);

        if (cameraTransform != null)
        {
            Vector3 fromPos = cameraTransform.position;
            Quaternion fromRot = cameraTransform.rotation;

            float standElapsed = 0f;
            while (standElapsed < standUpDuration)
            {
                standElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(standElapsed / standUpDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                cameraTransform.position = Vector3.Lerp(fromPos, finalStandEyeWorldPos, smoothT);
                cameraTransform.rotation = Quaternion.Slerp(fromRot, finalStandEyeWorldRot, smoothT);
                yield return null;
            }

            // GẮN CHÍNH XÁC VỀ LOCAL TRANSFORM (0, 2.5, 0) VÀ ROTATION GỐC
            cameraTransform.localPosition = new Vector3(0f, standingCameraLocalY, 0f);
            cameraTransform.localRotation = Quaternion.identity;
        }

        // PHÁT ĐỢT THOẠI 3: LÚC ĐỨNG DẬY XONG
        if (standingUpDialogues != null && standingUpDialogues.Length > 0)
        {
            foreach (var line in standingUpDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleDialogueLineRoutine(line));
                }
            }
        }

        // =========================================================================
        // GIAI ĐOẠN 4: TRẢ LẠI TOÀN BỘ QUYỀN ĐIỀU KHIỂN & DI CHUYỂN TỰ DO
        // =========================================================================
        if (playerMain != null)
        {
            playerMain.transform.position = targetPlayerWorldPos;
            playerMain.transform.rotation = targetPlayerWorldRot;

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = new Vector3(0f, standingCameraLocalY, 0f);
                cameraTransform.localRotation = Quaternion.identity;
            }

            // Đảm bảo MovePl luôn giữ đúng độ cao mắt đứng chuẩn (2.5)
            playerMain.SetStandingCamY(standingCameraLocalY);

            // Reset góc quay xRotation về 0 (nhìn thẳng ngang)
            playerMain.SyncRotationWithCurrentCamera();

            playerMain.isCameraLocked = false;
            playerMain.SetMovementState(true);
            playerMain.LockCursor();

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }

        // Bật lại tâm ngắm chấm tròn
        InteractPro interactPro = Object.FindFirstObjectByType<InteractPro>();
        if (interactPro != null && interactPro.dotObject != null)
        {
            interactPro.dotObject.SetActive(true);
        }

        isWakeUpCutsceneRunning = false;
        Debug.Log("[Map02IntroSequence] 🎮 CẮT CẢNH HOÀN TẤT! ĐÃ TRẢ DI CHUYỂN TỰ DO & CAMERA CHUẨN!");
    }

    IEnumerator FadeScreenInRoutine(float duration)
    {
        if (fadeScreenImage != null)
        {
            float fadeElapsed = 0f;
            while (fadeElapsed < duration)
            {
                fadeElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(fadeElapsed / duration);
                float a = Mathf.SmoothStep(1.0f, 0.0f, t);
                fadeScreenImage.color = new Color(0f, 0f, 0f, a);
                yield return null;
            }
            fadeScreenImage.color = new Color(0f, 0f, 0f, 0f);
            fadeScreenImage.gameObject.SetActive(false);
        }
    }

    IEnumerator GroggyLookAroundRoutine(Quaternion baseRot)
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;
            float yaw = Mathf.Sin(timer * 1.3f) * groggySwayAngle;
            float roll = Mathf.Cos(timer * 1.0f) * (groggySwayAngle * 0.35f);

            if (cameraTransform != null)
            {
                cameraTransform.rotation = baseRot * Quaternion.Euler(0f, yaw, roll);
            }
            yield return null;
        }
    }

    // =========================================================================
    // HÀM CHẠY 1 CÂU THOẠI ĐƠN LẺ CHUẨN GAMEINTRO MANAGER (MAP 01)
    // =========================================================================
    IEnumerator PlaySingleDialogueLineRoutine(DialogueLine line)
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

        // BẬT ÂM THANH THOẠI TRONG SUỐT LÚC ĐANG GÕ CHỮ
        if (dialogueSound != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.clip = dialogueSound;
            audioSource.volume = soundVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        // GÕ CHỮ TỪNG KÝ TỰ BẰNG SUBSTRING
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

        // DỪNG ÂM THANH MƯỢT MÀ KHI ĐÃ GÕ XONG HOẶC SKIP
        if (audioSource != null && audioSource.isPlaying)
        {
            StartCoroutine(FadeAudioOutRoutine(audioSource, 0.08f));
        }

        // BẬT CON TRỎ NHẤP NHÁY '_' TRONG LÚC CHỜ ĐỌC
        if (showBlinkingCursor && subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

        // CHỜ ĐỌC XONG HOẶC CHỜ BẤM CHUỘT QUA CÂU TIẾP THEO
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

        // Hiệu ứng Fade Out mờ dần khi chuyển câu thoại
        if (useFadeEffect && subtitleTextUI != null)
        {
            yield return StartCoroutine(FadeTextOutRoutine(subtitleTextUI, fadeDuration));
        }

        // Tắt chữ phụ đề sau khi câu thoại kết thúc
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

    void EnsureFadeScreenImage()
    {
        if (fadeScreenImage != null) return;

        Image[] allImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var img in allImages)
        {
            if (img.gameObject.name.ToLower().Contains("fade") || img.gameObject.name.ToLower().Contains("black"))
            {
                fadeScreenImage = img;
                break;
            }
        }

        if (fadeScreenImage == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canObj = new GameObject("IntroCanvas");
                canvas = canObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                canObj.AddComponent<CanvasScaler>();
                canObj.AddComponent<GraphicRaycaster>();
            }

            GameObject fObj = new GameObject("FadeScreen");
            fObj.transform.SetParent(canvas.transform, false);
            fadeScreenImage = fObj.AddComponent<Image>();
            fadeScreenImage.color = Color.black;

            RectTransform rt = fadeScreenImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    TextMeshProUGUI FindSubtitleTextUI()
    {
        if (subtitleTextUI != null) return subtitleTextUI;

        SmartInteractionDialogue sid = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
        if (sid != null && sid.subtitleTextUI != null) return sid.subtitleTextUI;

        TextMeshProUGUI[] tmps = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in tmps)
        {
            if (tmp.gameObject.name.ToLower().Contains("sub")) return tmp;
        }

        return null;
    }
}
