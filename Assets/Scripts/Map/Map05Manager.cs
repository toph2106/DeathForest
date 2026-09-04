using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Script quản lý tổng thể Map 05:
/// 1. Tự động thiết lập trạng thái phòng khi vừa vào Scene (Đóng sẵn cửa sổ, hạ âm thanh thành phố, tắt sẵn đèn sang đèn ngủ, đóng cửa chính, dọn sạch túi đồ và vô hiệu hóa thiết bị).
/// 2. Kích hoạt chuỗi cắt cảnh bừng tỉnh dậy trên đệm sau cơn ác mộng bị xe tải đâm (Map 04).
/// 3. Sau khi tỉnh dậy liền có tiếng gõ cửa liên tục (Continuous Door Knocking).
/// 4. Người chơi bật đèn phòng -> Mở khóa tương tác với cửa chính.
/// 5. Người chơi tương tác với cửa -> Dừng tiếng gõ, mở cửa, màn hình đen dần và hiển thị kết thúc game (Endgame Fade & Typewriter Ending), bấm phím bất kỳ quay về Menu chính.
/// </summary>
public class Map05Manager : MonoBehaviour
{
    public static Map05Manager Instance { get; private set; }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "";
        [TextArea(2, 4)]
        public string englishDialogue = "";
        public float holdDuration = 3.5f;
    }

    [Header("=== 1. CẤU HÌNH MẶC ĐỊNH PHÒNG TRONG MAP 05 ===")]
    [Tooltip("Tự động tắt đèn chính sang chế độ đèn ngủ khi vừa vào Scene (Mặc định: BẬT)")]
    public bool autoTurnOffRoomLight = true;

    [Tooltip("Tự động đóng cửa chính phòng khi vừa vào Scene (Mặc định: BẬT)")]
    public bool autoCloseDoor = true;

    [Tooltip("Danh sách các GameObject cần TẮT sẵn khi vào Map 05 (VD: Cockroach, TWNpc, Trigger...)")]
    public GameObject[] objectsToDisableOnStart;

    [Tooltip("Danh sách các GameObject cần BẬT sẵn khi vào Map 05")]
    public GameObject[] objectsToEnableOnStart;

    [Header("=== 2. TỌA ĐỘ GIƯỜNG NỆM TỈNH DẬY ===")]
    [Tooltip("Kéo SitCameraPoint (mép đệm - vị trí ngồi dậy) vào đây. Để trống sẽ tự tìm")]
    public Transform sitCameraPoint;

    [Tooltip("Kéo PillowCameraPoint (sát gối - vị trí nằm ngửa) vào đây. Để trống sẽ tự tìm")]
    public Transform pillowCameraPoint;

    [Tooltip("Độ cao mắt đứng của Main Camera trong phòng (Mặc định: 0.8 như Map 01).")]
    public float standingCameraLocalY = 0.8f;

    [Header("=== 3. CẮT CẢNH BỪNG TỈNH DẬY ÁC MỘNG (WAKE-UP SEQUENCE) ===")]
    [Tooltip("Tự động chạy cắt cảnh bừng tỉnh dậy khi vừa load Scene (Mặc định: BẬT)")]
    public bool playWakeUpOnStart = true;

    [Tooltip("Thời gian nín thở trong bóng tối trước khi mở mắt (giây, Mặc định: 1.5s)")]
    public float darkPauseDuration = 1.5f;

    [Tooltip("Thời gian mở mắt chớp mí từ từ hé sáng (giây, Mặc định: 2.2s)")]
    public float openEyesFadeInDuration = 2.2f;

    [Tooltip("Thời gian bật ngồi dậy ở mép đệm (giây, Mặc định: 1.5s cho đằm mượt)")]
    public float startleSitUpDuration = 1.5f;

    [Tooltip("Thời gian ngồi thở dốc / hoàn hồn ở mép đệm trước khi đứng lên (giây, Mặc định: 2.0s)")]
    public float sitBreathingDuration = 2.0f;

    [Tooltip("Thời gian nâng người đứng dậy từ nệm (giây, Mặc định: 1.8s)")]
    public float standUpDuration = 1.8f;

    [Header("=== 4. ÂM THANH (AUDIO SFX) ===")]
    [Tooltip("Tiếng hít hà / thở hắt lúc vừa mở mắt bật dậy")]
    public AudioClip wakeUpGaspAudio;

    [Tooltip("Tiếng thở dốc / tim đập lúc ngồi hoàn hồn ở mép đệm")]
    public AudioClip heavyBreathingAudio;

    [Tooltip("Tiếng sột soạt đệm/quần áo khi bật ngồi dậy")]
    public AudioClip rustlingGroundAudio;

    [Tooltip("Âm thanh gõ chữ phụ đề")]
    public AudioClip dialogueSound;

    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("=== 5. THOẠI TỈNH DẬY SAU CƠN ÁC MỘNG BỊ XE TẢI ĐÂM ===")]
    public DialogueLine[] wakeUpDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Hộc... hộc... Chỉ là ác mộng thôi sao...",
            englishDialogue = "Huff... huff... Was it just a nightmare...",
            holdDuration = 3.5f
        },
        new DialogueLine
        {
            vietnameseDialogue = "Mình vừa mơ bị một chiếc xe tải đâm phải... Cảm giác chân thật đến rợn người.",
            englishDialogue = "I just dreamed of getting hit by a truck... It felt terrifyingly real.",
            holdDuration = 4.0f
        }
    };

    [Header("=== 6. THAM CHIẾU UI PHỤ ĐỀ & FADE SCREEN ===")]
    public TextMeshProUGUI subtitleTextUI;
    public Image fadeScreenImage;
    public float typewriterSpeed = 0.03f;

    [Header("=== 7. TIẾNG GÕ CỬA LIÊN TỤC (CONTINUOUS DOOR KNOCK) ===")]
    [Tooltip("Kéo ContinuousDoorKnocker vào đây (để trống sẽ tự động tìm trong Scene)")]
    public ContinuousDoorKnocker doorKnocker;

    [Tooltip("Tự động kích hoạt tiếng gõ cửa liên tục ngay sau khi tỉnh dậy (Mặc định: BẬT)")]
    public bool startKnockingAfterWakeUp = true;

    [Tooltip("Thời gian trễ (giây) trước khi bắt đầu đợt gõ cửa đầu tiên sau khi đứng dậy")]
    public float delayBeforeKnock = 0.5f;

    [Header("=== 8. MỞ KHÓA CỬA KHI BẬT ĐÈN PHÒNG ===")]
    [Tooltip("Kéo RoomLightSwitch vào đây (để trống sẽ tự động tìm)")]
    public RoomLightSwitch roomLightSwitch;

    [Tooltip("Kéo DoorExit vào đây (để trống sẽ tự động tìm)")]
    public DoorExit roomDoor;

    [Tooltip("Thoại phát khi người chơi BẬT ĐÈN PHÒNG (nghe thấy tiếng gõ cửa)")]
    public DialogueLine[] lightOnDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Ai lại gõ cửa vào giờ này nhỉ?",
            englishDialogue = "Who could be knocking at this hour?",
            holdDuration = 3.0f
        }
    };

    [Header("=== 9. CẮT CẢNH KẾT THÚC GAME (ENDGAME SEQUENCE) ===")]
    [Tooltip("Thời gian Fade màn hình từ từ tối đen khi mở cửa (giây, Mặc định: 2.5s)")]
    public float endgameFadeDuration = 2.5f;

    [Tooltip("Panel UI hiển thị chữ kết thúc game (để trống sẽ tự động tìm/tạo)")]
    public GameObject endgamePanel;

    [Tooltip("Text hiển thị nội dung kết thúc game")]
    public TextMeshProUGUI endgameTextUI;

    [TextArea(4, 10)]
    public string endgameTextVI = "Ác mộng đã kết thúc... hay chỉ vừa mới bắt đầu?\n\nCẢM ƠN BẠN ĐÃ TRẢI NGHIỆM DEATH FOREST!\n\n[ Nhấn phím bất kỳ hoặc Click chuột để quay về Menu Chính ]";

    [TextArea(4, 10)]
    public string endgameTextEN = "The nightmare is over... or has it just begun?\n\nTHANK YOU FOR PLAYING DEATH FOREST!\n\n[ Press any key or Click to return to Main Menu ]";

    [Tooltip("Tốc độ gõ chữ kết thúc game (giây/ký tự)")]
    public float endgameTypewriterSpeed = 0.035f;

    [Tooltip("Âm thanh ma mị / kết thúc khi màn hình tối đen (Tùy chọn)")]
    public AudioClip endgameMusicOrAmbience;

    [Tooltip("Tên Scene Menu chính để chuyển về (Mặc định: 'MainMenu')")]
    public string mainMenuSceneName = "MainMenu";

    // --- Private Fields ---
    private AudioSource audioSource;
    private bool isWakingUp = false;
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private bool skipWaitRequested = false;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;
    private Vector3 originalCameraLocalPos = Vector3.zero;

    private bool hasTriggeredLightOnDialogue = false;
    private bool isEnding = false;
    private bool isEndingFinished = false;
    private bool isReturningToMenu = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (standingCameraLocalY > 1.5f)
        {
            standingCameraLocalY = 0.8f;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        MovePl player = Object.FindFirstObjectByType<MovePl>();
        if (player != null && player.cameraTransform != null)
        {
            originalCameraLocalPos = player.cameraTransform.localPosition;
        }

        AutoFindReferences();
        ApplyDefaultRoomState();
    }

    void Start()
    {
        AutoFindReferences();
        ApplyDefaultRoomState();

        if (playWakeUpOnStart)
        {
            StartWakeUpSequence();
        }
    }

    void Update()
    {
        if (isWakingUp)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
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
    }

    // =========================================================================
    // 1. THIẾT LẬP TRẠNG THÁI PHÒNG MẶC ĐỊNH
    // =========================================================================

    public void ApplyDefaultRoomState()
    {
        AutoFindReferences();

        // 1. Tự động tắt đèn chính sang chế độ đèn ngủ
        if (autoTurnOffRoomLight)
        {
            if (roomLightSwitch != null)
            {
                roomLightSwitch.SetLightState(false);
            }
            else
            {
                RoomLightSwitch lightSwitch = Object.FindFirstObjectByType<RoomLightSwitch>(FindObjectsInactive.Include);
                if (lightSwitch != null) lightSwitch.SetLightState(false);
            }
        }

        // 2. Tự động đóng cửa chính & khóa ban đầu (chờ bật đèn mới mở khóa)
        if (autoCloseDoor)
        {
            if (roomDoor != null)
            {
                roomDoor.lockOnStart = true;
                roomDoor.EnsurePositionsInitialized();
                roomDoor.CloseDoor(true);
            }
            else
            {
                DoorExit door = Object.FindFirstObjectByType<DoorExit>(FindObjectsInactive.Include);
                if (door != null)
                {
                    door.lockOnStart = true;
                    door.EnsurePositionsInitialized();
                    door.CloseDoor(true);
                }
            }
        }

        // 3. Tắt các GameObjects cần tắt của Map 01 cũ (Cockroach, TWNpc, Johnson, Camera10sDoorEvent...)
        DisableUnusedMap01Objects();

        if (objectsToDisableOnStart != null)
        {
            foreach (var obj in objectsToDisableOnStart)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        // 4. Bật các GameObjects cần bật
        if (objectsToEnableOnStart != null)
        {
            foreach (var obj in objectsToEnableOnStart)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // 5. Đảm bảo dừng tiếng gõ cửa ban đầu trước khi tỉnh dậy
        if (doorKnocker != null)
        {
            doorKnocker.StopKnocking();
        }

        // 6. Xóa toàn bộ túi đồ và vô hiệu hóa Camera UI, Đèn pin, Flash stun, Night Vision
        ResetAllEquipmentAndInventoryForMap05();

        Debug.Log("[Map05Manager] 🏠 Đã thiết lập trạng thái phòng mặc định (Cửa đóng khóa, Đèn ngủ bật, Đồ đạc và thiết bị đã làm sạch)!");
    }

    private void DisableUnusedMap01Objects()
    {
        string[] disableNames = new string[] {
            "Cockroach", "CockroachNightmareWakeUp", "CockroachManager", "CockroachWR",
            "Camera10sDoorEvent", "TWNpc", "Johnson", "TriggerMeow"
        };

        foreach (string n in disableNames)
        {
            GameObject found = GameObject.Find(n);
            if (found != null)
            {
                found.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Vô hiệu hóa toàn bộ thiết bị (Camera UI, Flashlight, Night Vision, Flash Stun) và xóa sạch túi đồ khi ở Map 05
    /// </summary>
    public void ResetAllEquipmentAndInventoryForMap05()
    {
        // 1. Xóa sạch túi đồ và reset dữ liệu balo
        InventoryManager.ResetInventoryData();
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearInventory();
        }

        // 2. Vô hiệu hóa và ẩn giao diện máy quay (Camcorder UI & CameraOverlay)
        CamcorderUI.ResetPickedUpCameraState();
        CamcorderUI[] camUIs = Object.FindObjectsByType<CamcorderUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in camUIs)
        {
            if (c != null) c.gameObject.SetActive(false);
        }

        SimpleCameraOverlay overlay = Object.FindFirstObjectByType<SimpleCameraOverlay>(FindObjectsInactive.Include);
        if (overlay != null)
        {
            overlay.ResetCameraView();
        }

        // 3. Vô hiệu hóa đèn pin & đèn chói (Flash burst)
        FlashlightToggle.ResetFlashlightData();
        FlashlightToggle ft = FlashlightToggle.Instance ?? Object.FindFirstObjectByType<FlashlightToggle>(FindObjectsInactive.Include);
        if (ft != null)
        {
            ft.hasFlashlight = false;
            ft.SetFlashlightState(false, false);
            ft.UpdateUI();
        }

        // 4. Vô hiệu hóa đèn nhìn trong đêm (Night Vision)
        if (NightVisionCamera.Instance != null)
        {
            NightVisionCamera.Instance.SetNightVision(false);
        }

        Debug.Log("[Map05Manager] 🚫 ĐÃ XÓA TÚI ĐỒ VÀ VÔ HIỆU HÓA TOÀN BỘ THIẾT BỊ (Camera UI, Flashlight, Night Vision, Flash Stun) CHO MAP 05!");
    }

    // =========================================================================
    // 2. CHUỖI CẮT CẢNH BỪNG TỈNH DẬY TRÊN ĐỆM
    // =========================================================================

    public void StartWakeUpSequence()
    {
        if (isWakingUp) return;
        StartCoroutine(WakeUpFromNightmareRoutine());
    }

    private IEnumerator WakeUpFromNightmareRoutine()
    {
        isWakingUp = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        AutoFindReferences();

        // Chờ 1 frame để toàn bộ Start() của các script khác chạy xong rồi áp lại trạng thái phòng chuẩn
        yield return null;
        ApplyDefaultRoomState();

        MovePl player = Object.FindFirstObjectByType<MovePl>();
        if (player == null)
        {
            Debug.LogWarning("[Map05Manager] ⚠️ Không tìm thấy MovePl trong Scene!");
            isWakingUp = false;
            SmartInteractionDialogue.isAnyDialoguePlaying = false;
            yield break;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        Transform camTrans = (player.cameraTransform != null) ? player.cameraTransform : (Camera.main != null ? Camera.main.transform : null);

        // Lưu lại vị trí mắt đứng chuẩn ban đầu nếu chưa có
        if (originalCameraLocalPos == Vector3.zero && camTrans != null)
        {
            originalCameraLocalPos = camTrans.localPosition;
        }

        float finalStandY = 0.8f;
        if (originalCameraLocalPos != Vector3.zero && originalCameraLocalPos.y > 0.1f && originalCameraLocalPos.y < 1.5f)
        {
            finalStandY = originalCameraLocalPos.y;
        }
        else if (standingCameraLocalY > 0.1f && standingCameraLocalY < 1.5f)
        {
            finalStandY = standingCameraLocalY;
        }

        // 1. KHÓA PLAYER VÀ CAMERA
        player.SetMovementState(false);
        player.isCameraLocked = true;
        if (cc != null) cc.enabled = false;

        // Màn hình đen kịt ban đầu
        EnsureFadeImage();
        if (fadeScreenImage != null)
        {
            fadeScreenImage.gameObject.SetActive(true);
            fadeScreenImage.color = Color.black;
        }

        // 2. DỊCH CHUYỂN PLAYER VÀ CAMERA VỀ ĐỆM TRONG TỐI
        if (sitCameraPoint != null)
        {
            Vector3 targetPlayerPos = sitCameraPoint.position;
            targetPlayerPos.y = player.transform.position.y;
            player.transform.position = targetPlayerPos;
            player.transform.rotation = Quaternion.Euler(0f, sitCameraPoint.eulerAngles.y, 0f);
        }

        if (pillowCameraPoint != null && camTrans != null)
        {
            camTrans.position = pillowCameraPoint.position;
            camTrans.rotation = pillowCameraPoint.rotation;
        }

        // Chờ trong bóng tối nín thở
        if (darkPauseDuration > 0f)
        {
            yield return new WaitForSeconds(darkPauseDuration);
        }

        // 3. MỞ MẮT CHỚP MÍ DẦN DẦN (CINEMATIC EYE BLINK FADE IN)
        if (wakeUpGaspAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(wakeUpGaspAudio, soundVolume);
        }

        if (fadeScreenImage != null)
        {
            fadeScreenImage.gameObject.SetActive(true);
            Color fadeCol = Color.black;

            // Nhịp 1: Hé mắt chớp lần 1 (1.0 -> 0.65 -> 0.90)
            float t1 = 0f;
            while (t1 < 0.45f)
            {
                t1 += Time.deltaTime;
                fadeCol.a = Mathf.Lerp(1.0f, 0.65f, t1 / 0.45f);
                fadeScreenImage.color = fadeCol;
                yield return null;
            }
            float t1b = 0f;
            while (t1b < 0.2f)
            {
                t1b += Time.deltaTime;
                fadeCol.a = Mathf.Lerp(0.65f, 0.90f, t1b / 0.2f);
                fadeScreenImage.color = fadeCol;
                yield return null;
            }

            // Nhịp 2: Hé mắt mở to hơn lần 2 (0.90 -> 0.30 -> 0.60)
            float t2 = 0f;
            while (t2 < 0.55f)
            {
                t2 += Time.deltaTime;
                fadeCol.a = Mathf.Lerp(0.90f, 0.30f, t2 / 0.55f);
                fadeScreenImage.color = fadeCol;
                yield return null;
            }
            float t2b = 0f;
            while (t2b < 0.25f)
            {
                t2b += Time.deltaTime;
                fadeCol.a = Mathf.Lerp(0.30f, 0.60f, t2b / 0.25f);
                fadeScreenImage.color = fadeCol;
                yield return null;
            }

            // Mở to hẳn sáng rõ (0.60 -> 0.0)
            float t3 = 0f;
            while (t3 < openEyesFadeInDuration)
            {
                t3 += Time.deltaTime;
                fadeCol.a = Mathf.Lerp(0.60f, 0.0f, t3 / openEyesFadeInDuration);
                fadeScreenImage.color = fadeCol;
                yield return null;
            }

            fadeCol.a = 0f;
            fadeScreenImage.color = fadeCol;
            fadeScreenImage.gameObject.SetActive(false);
        }

        // 4. BẬT NGỒI DẬY Ở MÉP ĐỆM (STARTLE SIT UP)
        Debug.Log("[Map05Manager] 🧘 Bật ngồi dậy ở mép đệm...");
        if (rustlingGroundAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(rustlingGroundAudio, soundVolume);
        }

        if (sitCameraPoint != null && pillowCameraPoint != null && camTrans != null)
        {
            Vector3 fromPos = camTrans.position;
            Quaternion fromRot = camTrans.rotation;
            Vector3 toPos = sitCameraPoint.position;
            Quaternion toRot = sitCameraPoint.rotation;

            float sitElapsed = 0f;
            while (sitElapsed < startleSitUpDuration)
            {
                sitElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(sitElapsed / startleSitUpDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                camTrans.position = Vector3.Lerp(fromPos, toPos, smoothT);
                camTrans.rotation = Quaternion.Slerp(fromRot, toRot, smoothT);
                yield return null;
            }

            camTrans.position = toPos;
            camTrans.rotation = toRot;
        }

        // Phát âm thanh thở dốc khi ngồi hoàn hồn
        if (heavyBreathingAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(heavyBreathingAudio, soundVolume);
        }

        // 5. PHÁT THOẠI TỈNH DẬY (WAKE-UP DIALOGUES)
        if (wakeUpDialogues != null && wakeUpDialogues.Length > 0)
        {
            foreach (var line in wakeUpDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(line));
                }
            }
        }

        ClearSubtitleUI();

        if (sitBreathingDuration > 0f)
        {
            yield return new WaitForSeconds(sitBreathingDuration);
        }

        // 6. ĐỨNG DẬY VỀ ĐÚNG CHIỀU CAO MẮT PHÒNG (0, finalStandY, 0)
        Debug.Log($"[Map05Manager] 🚶 Nâng người đứng dậy từ nệm về độ cao mắt ({finalStandY})...");
        if (player != null && camTrans != null)
        {
            Vector3 fromPos = camTrans.position;
            Quaternion fromRot = camTrans.rotation;

            Vector3 finalEyeWorldPos = player.transform.TransformPoint(new Vector3(0f, finalStandY, 0f));
            Quaternion finalEyeWorldRot = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);

            float standElapsed = 0f;
            while (standElapsed < standUpDuration)
            {
                standElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(standElapsed / standUpDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                camTrans.position = Vector3.Lerp(fromPos, finalEyeWorldPos, smoothT);
                camTrans.rotation = Quaternion.Slerp(fromRot, finalEyeWorldRot, smoothT);
                yield return null;
            }

            camTrans.localPosition = new Vector3(0f, finalStandY, 0f);
            camTrans.localRotation = Quaternion.identity;
        }

        // 7. TRẢ LẠI QUYỀN ĐIỀU KHIỂN CHO NGƯỜI CHƠI
        if (player != null)
        {
            player.SetStandingCamY(finalStandY);
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

        isWakingUp = false;
        SmartInteractionDialogue.isAnyDialoguePlaying = false;

        Debug.Log("[Map05Manager] 🎮 CẮT CẢNH HOÀN TẤT! ĐÃ TRẢ DI CHUYỂN TỰ DO CHO NGƯỜI CHƠI TRONG MAP 05.");

        // 8. KÍCH HOẠT TIẾNG GÕ CỬA LIÊN TỤC (CONTINUOUS DOOR KNOCKING)
        if (startKnockingAfterWakeUp)
        {
            StartCoroutine(StartKnockingAfterDelayRoutine());
        }
    }

    private IEnumerator StartKnockingAfterDelayRoutine()
    {
        if (delayBeforeKnock > 0f)
        {
            yield return new WaitForSeconds(delayBeforeKnock);
        }

        if (doorKnocker == null)
        {
            doorKnocker = Object.FindFirstObjectByType<ContinuousDoorKnocker>();
        }

        if (doorKnocker != null)
        {
            doorKnocker.StartKnocking();
            Debug.Log("[Map05Manager] 🚪 ĐÃ BẮT ĐẦU TIẾNG GÕ CỬA LIÊN TỤC SAU KHI TỈNH DẬY!");
        }
        else
        {
            Debug.LogWarning("[Map05Manager] ⚠️ Không tìm thấy ContinuousDoorKnocker trong Scene để phát tiếng gõ cửa!");
        }
    }

    // =========================================================================
    // 3. SỰ KIỆN BẬT ĐÈN PHÒNG (MỞ KHÓA CỬA CHÍNH)
    // =========================================================================

    /// <summary>
    /// Được gọi từ RoomLightSwitch khi người chơi gạt công tắc BẬT đèn chính
    /// </summary>
    public void OnRoomLightTurnedOn()
    {
        // 1. Mở khóa tương tác cho cửa chính
        if (roomDoor == null)
        {
            roomDoor = Object.FindFirstObjectByType<DoorExit>();
        }

        if (roomDoor != null)
        {
            roomDoor.UnlockDoor();
            Debug.Log("[Map05Manager] 🔓 ĐÃ MỞ KHÓA CỬA CHÍNH SAU KHI BẬT ĐÈN!");
        }

        // 2. Phát câu thoại nghi vấn tiếng gõ cửa (nếu chưa từng phát)
        if (!hasTriggeredLightOnDialogue && lightOnDialogues != null && lightOnDialogues.Length > 0)
        {
            hasTriggeredLightOnDialogue = true;
            StartCoroutine(PlayLightOnDialogueRoutine());
        }
    }

    private IEnumerator PlayLightOnDialogueRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        foreach (var line in lightOnDialogues)
        {
            if (line != null)
            {
                yield return StartCoroutine(PlaySingleLineRoutine(line));
            }
        }
        ClearSubtitleUI();
    }

    // =========================================================================
    // 4. CHUỖI CẮT CẢNH KẾT THÚC GAME (ENDGAME SEQUENCE)
    // =========================================================================

    /// <summary>
    /// Được gọi từ DoorExit khi người chơi tương tác với cánh cửa đã mở khóa
    /// </summary>
    public void TriggerEndGameSequence()
    {
        if (isEnding) return;
        StartCoroutine(EndGameSequenceRoutine());
    }

    private IEnumerator EndGameSequenceRoutine()
    {
        isEnding = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;
        Debug.Log("[Map05Manager] 🎬 BẮT ĐẦU CHUỖI KẾT THÚC GAME (ENDGAME SEQUENCE)...");

        // 1. DỪNG TIẾNG GÕ CỬA NGAY LẬP TỨC
        if (doorKnocker != null)
        {
            doorKnocker.StopKnocking();
        }
        else
        {
            ContinuousDoorKnocker knk = Object.FindFirstObjectByType<ContinuousDoorKnocker>();
            if (knk != null) knk.StopKnocking();
        }

        // 2. KHÓA DI CHUYỂN VÀ GÓC NHÌN NGƯỜI CHƠI
        MovePl player = Object.FindFirstObjectByType<MovePl>();
        CharacterController cc = (player != null) ? player.GetComponent<CharacterController>() : Object.FindFirstObjectByType<CharacterController>();
        if (player != null)
        {
            player.SetMovementState(false);
            player.isCameraLocked = true;
        }
        if (cc != null) cc.enabled = false;

        // 3. ẨN TẤT CẢ UI TƯƠNG TÁC, CHẤM TRÒN VÀ HUD
        InteractPro interactPro = Object.FindFirstObjectByType<InteractPro>();
        if (interactPro != null && interactPro.dotObject != null)
        {
            interactPro.dotObject.SetActive(false);
        }

        PauseMenuManager.SetInGameHUDActive(false);

        // 4. MỞ CỬA CHÍNH (TRƯỢT MỞ)
        if (roomDoor == null)
        {
            roomDoor = Object.FindFirstObjectByType<DoorExit>();
        }
        if (roomDoor != null)
        {
            roomDoor.ToggleDoor();
        }

        // 5. HIỆU ỨNG FADE MÀN HÌNH TỐI ĐEN DẦN DẦN (FADE OUT TO BLACK)
        EnsureFadeImage();
        if (fadeScreenImage != null)
        {
            EnsureParentsActive(fadeScreenImage);
            fadeScreenImage.transform.SetAsLastSibling();
            fadeScreenImage.gameObject.SetActive(true);
            fadeScreenImage.raycastTarget = true;

            float fadeElapsed = 0f;
            Color fadeColor = Color.black;
            while (fadeElapsed < endgameFadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                fadeColor.a = Mathf.Clamp01(fadeElapsed / endgameFadeDuration);
                fadeScreenImage.color = fadeColor;
                yield return null;
            }
            fadeColor.a = 1f;
            fadeScreenImage.color = fadeColor;
        }
        else
        {
            yield return new WaitForSeconds(endgameFadeDuration);
        }

        // Giữ bóng tối 0.8s
        yield return new WaitForSeconds(0.8f);

        // Phát âm thanh ma mị kết thúc nếu có
        if (endgameMusicOrAmbience != null && audioSource != null)
        {
            audioSource.clip = endgameMusicOrAmbience;
            audioSource.volume = soundVolume;
            audioSource.loop = false;
            audioSource.Play();
        }

        // 6. HIỂN THỊ CHỮ KẾT THÚC GAME VỚI TYPEWRITER
        yield return StartCoroutine(PlayEndgameTextRoutine());

        // 7. CHỜ NGƯỜI CHƠI NHẤN PHÍM BẤT KỲ HOẶC CLICK CHUỘT ĐỂ QUAY VỀ MENU CHÍNH
        isEndingFinished = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        while (!isReturningToMenu)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.anyKeyDown)
            {
                isReturningToMenu = true;
                break;
            }
            yield return null;
        }

        // 8. FADE OUT CHỮ VÀ CHUYỂN VỀ MAIN MENU
        yield return StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator PlayEndgameTextRoutine()
    {
        EnsureEndgameUI();

        if (endgamePanel != null)
        {
            EnsureParentsActive(endgamePanel.GetComponent<Image>() ?? fadeScreenImage);
            endgamePanel.SetActive(true);
        }

        string lang = SettingsManager.currentLanguage;
        string fullContent = (lang == "VI") ? endgameTextVI : endgameTextEN;
        if (string.IsNullOrEmpty(fullContent)) fullContent = endgameTextVI;

        if (endgameTextUI != null)
        {
            if (endgameTextUI.transform.parent != null) endgameTextUI.transform.parent.gameObject.SetActive(true);
            endgameTextUI.gameObject.SetActive(true);
            endgameTextUI.color = Color.white;
            endgameTextUI.text = "";

            if (dialogueSound != null && audioSource != null)
            {
                audioSource.clip = dialogueSound;
                audioSource.volume = soundVolume;
                audioSource.loop = true;
                audioSource.time = 0f;
                audioSource.Play();
            }

            bool skipEndgameTypewriter = false;
            for (int i = 1; i <= fullContent.Length; i++)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    skipEndgameTypewriter = true;
                    endgameTextUI.text = fullContent;
                    break;
                }

                endgameTextUI.text = fullContent.Substring(0, i);
                yield return new WaitForSeconds(endgameTypewriterSpeed);
            }

            if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
            {
                audioSource.Stop();
            }

            if (!skipEndgameTypewriter)
            {
                endgameTextUI.text = fullContent;
            }
        }
        else
        {
            Debug.LogWarning("[Map05Manager] ⚠️ Không tìm thấy endgameTextUI!");
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        Debug.Log($"[Map05Manager] 🎬 Đang chuyển về Scene Menu Chính: '{mainMenuSceneName}'...");

        // Dọn dẹp toàn bộ dữ liệu gameplay
        CamcorderUI.ResetTimer();
        GameSaveManager.ResetAllGameplayRuntimeData();

        // Fade out đen mượt mà
        if (fadeScreenImage != null)
        {
            float elapsed = 0f;
            Color c = fadeScreenImage.color;
            while (elapsed < 1.0f)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / 1.0f);
                fadeScreenImage.color = c;
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(0.2f);

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void EnsureEndgameUI()
    {
        if (endgameTextUI != null) return;

        // Thử tìm EndingText đã có trong Canvas
        GameObject found = GameObject.Find("EndgameText") ?? GameObject.Find("EndingText") ?? GameObject.Find("WaterEndingText");
        if (found != null)
        {
            endgameTextUI = found.GetComponent<TextMeshProUGUI>();
            if (endgameTextUI != null) return;
        }

        // Nếu chưa có, sử dụng SubtitleText nhưng chỉnh canh giữa màn hình đẹp mắt
        if (subtitleTextUI != null)
        {
            endgameTextUI = subtitleTextUI;
            endgameTextUI.alignment = TextAlignmentOptions.Center;
            endgameTextUI.fontSize = 24;
            return;
        }

        // Hoặc tạo mới một TextMeshProUGUI trên FadeCanvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject txtObj = new GameObject("EndgameTextUI");
            txtObj.transform.SetParent(canvas.transform, false);
            RectTransform rt = txtObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.1f);
            rt.anchorMax = new Vector2(0.9f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            endgameTextUI = txtObj.AddComponent<TextMeshProUGUI>();
            endgameTextUI.alignment = TextAlignmentOptions.Center;
            endgameTextUI.fontSize = 26;
            endgameTextUI.color = Color.white;
            endgameTextUI.lineSpacing = 15;
            endgameTextUI.gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // 5. DIALOGUE TYPEWRITER & CLICK SKIP
    // =========================================================================

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
            audioSource.clip = dialogueSound;
            audioSource.volume = soundVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        if (subtitleTextUI != null)
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

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound)
        {
            audioSource.Stop();
        }

        isTyping = false;
        skipRequested = false;

        // Chờ 1 frame tránh double-click ăn nhầm
        yield return null;

        if (subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

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
    // 6. AUTO FIND HELPERS
    // =========================================================================

    private void AutoFindReferences()
    {
        if (sitCameraPoint == null)
        {
            GameObject sitObj = GameObject.Find("SitCameraPoint");
            if (sitObj != null) sitCameraPoint = sitObj.transform;
        }

        if (pillowCameraPoint == null)
        {
            GameObject pillowObj = GameObject.Find("PillowCameraPoint");
            if (pillowObj != null) pillowCameraPoint = pillowObj.transform;
        }

        if (doorKnocker == null)
        {
            doorKnocker = Object.FindFirstObjectByType<ContinuousDoorKnocker>();
        }

        if (roomLightSwitch == null)
        {
            roomLightSwitch = Object.FindFirstObjectByType<RoomLightSwitch>();
        }

        if (roomDoor == null)
        {
            roomDoor = Object.FindFirstObjectByType<DoorExit>();
        }

        if (wakeUpGaspAudio == null)
        {
            CockroachNightmareWakeUp cockroach = Object.FindFirstObjectByType<CockroachNightmareWakeUp>(FindObjectsInactive.Include);
            if (cockroach != null && cockroach.wakeUpGaspAudio != null) wakeUpGaspAudio = cockroach.wakeUpGaspAudio;
        }

        if (heavyBreathingAudio == null)
        {
            CockroachNightmareWakeUp cockroach = Object.FindFirstObjectByType<CockroachNightmareWakeUp>(FindObjectsInactive.Include);
            if (cockroach != null && cockroach.heavyBreathingAudio != null) heavyBreathingAudio = cockroach.heavyBreathingAudio;
        }

        if (rustlingGroundAudio == null)
        {
            Map02IntroSequence intro02 = Object.FindFirstObjectByType<Map02IntroSequence>(FindObjectsInactive.Include);
            if (intro02 != null && intro02.rustlingGroundAudio != null) rustlingGroundAudio = intro02.rustlingGroundAudio;
        }

        if (dialogueSound == null)
        {
            SmartInteractionDialogue smart = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
            if (smart != null && smart.dialogueSound != null) dialogueSound = smart.dialogueSound;
        }

        FindSubtitleUI();
        EnsureFadeImage();
    }

    private void FindSubtitleUI()
    {
        if (subtitleTextUI != null) return;

        SmartInteractionDialogue smart = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
        if (smart != null && smart.subtitleTextUI != null)
        {
            subtitleTextUI = smart.subtitleTextUI;
            return;
        }

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

    private void EnsureFadeImage()
    {
        if (fadeScreenImage != null) return;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            Transform fadeT = c.transform.Find("FadeImage") ?? c.transform.Find("FadePanel") ?? c.transform.Find("BlackScreen") ?? c.transform.Find("FadeScreen");
            if (fadeT != null)
            {
                fadeScreenImage = fadeT.GetComponent<Image>();
                if (fadeScreenImage != null) return;
            }
        }
    }

    private void EnsureParentsActive(Image img)
    {
        if (img == null) return;
        Transform curr = img.transform.parent;
        while (curr != null)
        {
            curr.gameObject.SetActive(true);
            curr = curr.parent;
        }
    }
}

