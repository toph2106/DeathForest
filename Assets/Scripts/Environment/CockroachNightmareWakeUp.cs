using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CockroachNightmareWakeUp : MonoBehaviour
{
    [Header("1. Điểm Tọa Độ Nệm (Bed Points)")]
    [Tooltip("Kéo SitCameraPoint (mép đệm) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM trong Scene!")]
    public Transform sitCameraPoint;

    [Tooltip("Kéo PillowCameraPoint (sát gối) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM trong Scene!")]
    public Transform pillowCameraPoint;

    [Header("2. Thời Gian & Hiệu Ứng Bật Dậy Ác Mộng (Nightmare Startle)")]
    [Tooltip("Thời gian nín thở trong bóng tối trước khi mở mắt (giây - Mặc định: 1.0s)")]
    public float darkPauseDuration = 1.0f;

    [Tooltip("Thời gian mở mắt chớp mí dần dần (giây - Mặc định: 1.5s)")]
    public float openEyesFadeInDuration = 1.5f;

    [Tooltip("Thời gian bật ngồi dậy nhanh như bị giật mình (giây - Mặc định: 0.75s)")]
    public float startleSitUpDuration = 0.75f;

    [Tooltip("Độ giật nảy của Camera lúc vừa bật dậy (Mặc định: 0.05)")]
    public float startleJoltMagnitude = 0.05f;

    [Tooltip("Thời gian ngồi thở dốc / hoàn hồn ở mép đệm trước khi đứng lên (giây - Mặc định: 1.2s)")]
    public float sitBreathingDuration = 1.2f;

    [Tooltip("Thời gian nâng người đứng dậy từ nệm (giây - Mặc định: 0.85s)")]
    public float standUpDuration = 0.85f;

    [Header("3. Âm Thanh Thở Dốc / Giật Mình (Tùy Chọn)")]
    [Tooltip("Tiếng hít hà / thở hắt lúc vừa mở mắt bật dậy")]
    public AudioClip wakeUpGaspAudio;

    [Tooltip("Tiếng thở dốc / tim đập lúc ngồi hoàn hồn ở mép đệm")]
    public AudioClip heavyBreathingAudio;

    [Range(0f, 1f)]
    public float audioVolume = 0.85f;

    [Header("4. Tự Động Đóng Cửa Chính")]
    [Tooltip("Kéo Cửa chính (chứa DoorExit) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM trong Scene!")]
    public DoorExit doorToClose;

    [Header("5. Fade Screen Image")]
    [Tooltip("Kéo FadeScreen Image vào đây. Nếu để trống sẽ tự động liên kết")]
    public Image fadeScreenImage;

    [Header("6. Thoại Khi Bừng Tỉnh Dậy Sau Cơn Ác Mộng (Wake Up Dialogue)")]
    public SmartInteractionDialogue.DialogueLine[] wakeUpDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Hộc... hộc... Chỉ là ác mộng thôi sao... Sợ chết đi được!",
            englishDialogue = "Huff... huff... Was it just a nightmare... That was terrifying!",
            holdDuration = 3.0f
        }
    };

    [Header("7. Âm Thanh Thoại Gõ Chữ (Dialogue SFX)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    [Header("8. Prefab Hộp Mở Chứa Máy Quay (Reset BoxOpen)")]
    [Tooltip("Kéo BoxOpen Prefab vào đây để tự động tái tạo lại hộp mở kèm máy quay sau khi ngất")]
    public GameObject openBoxPrefab;

    [Header("9. Thoại Khi Nhìn Thấy Thùng Hàng Sau Khi Tỉnh Dậy (Look At Box Dialogue)")]
    [Tooltip("Danh sách câu thoại phát khi người chơi nhìn trúng thùng hàng sau khi bừng tỉnh")]
    public SmartInteractionDialogue.DialogueLine[] lookAtBoxDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Hả... sao cái thùng hàng lại vẫn nằm ở đây? Nãy giờ rốt cuộc là mình vừa mơ hay là thật thế này...",
            englishDialogue = "Huh... why is the delivery box still sitting here? Was everything just now a dream or was it real...",
            holdDuration = 3.5f
        }
    };
    [Tooltip("Khoảng cách tối đa tia nhìn quét trúng thùng hàng (Mặc định: 6.0 mét)")]
    public float lookAtBoxMaxDistance = 6.0f;

    [Header("10. Phím Tắt Debug Test Nhanh")]
    [Tooltip("Bấm phím này trong lúc Play để kích hoạt ngay chuỗi tỉnh dậy thử nghiệm (Mặc định: Phím U)")]
    public KeyCode debugWakeUpKey = KeyCode.U;

    [Header("11. Cảnh Choáng Váng Ngất Xỉu & Chuyển Sang Map 02 (Dizzy Faint & Scene Transition)")]
    [Tooltip("Thời gian chờ (giây) sau khi dứt câu thoại nhìn thùng hàng rồi mới bắt đầu ngất (Mặc định: 5.0s)")]
    public float delayBeforeFaint = 5.0f;

    [Tooltip("Câu thoại khi bắt đầu bị choáng ngất")]
    public SmartInteractionDialogue.DialogueLine[] faintDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Chuyện gì thế...",
            englishDialogue = "What is happening...",
            holdDuration = 2.5f
        }
    };

    [Tooltip("Tên Scene tiếp theo cần tải sau khi ngất (Mặc định: 'Map02')")]
    public string nextSceneName = "Map02";

    [Tooltip("Thời gian camera lắc lư choáng váng trước khi đổ sụp (giây - Mặc định: 2.5s)")]
    public float dizzyDuration = 2.5f;

    [Range(0.1f, 2f)]
    [Tooltip("Cường độ lắc lư choáng váng (Mặc định: 0.4 - Lắc nhẹ nhàng êm ái)")]
    public float dizzyShakeIntensity = 0.4f;

    [Tooltip("Thời gian camera đổ gục xuống sàn (giây - Mặc định: 1.2s)")]
    public float collapseDuration = 1.2f;

    [Tooltip("Âm thanh tim đập dồn dập / choáng váng (Tùy chọn)")]
    public AudioClip faintHeartbeatAudio;

    [Tooltip("Âm thanh ngã sụp xuống sàn (Tùy chọn)")]
    public AudioClip faintCollapseAudio;

    [Range(0f, 1f)] public float faintAudioVolume = 0.9f;

    private AudioSource audioSource;
    private bool isWakingUp = false;
    private bool isWaitingForLookAtBox = false;
    private bool hasTriggeredLookAtBox = false;
    private bool isFainting = false;
    private Vector3 originalCameraLocalPos;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Âm thanh 2D chân thực sát tai
    }

    void Start()
    {
        AutoFindPoints();

        MovePl player = Object.FindFirstObjectByType<MovePl>();
        if (player != null && player.cameraTransform != null)
        {
            originalCameraLocalPos = player.cameraTransform.localPosition;
        }
        else if (Camera.main != null)
        {
            originalCameraLocalPos = Camera.main.transform.localPosition;
        }
    }

    void AutoFindPoints()
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

        if (fadeScreenImage == null)
        {
            // 1. Tìm từ các script khác đã có sẵn
            CockroachFlyAttack fly = Object.FindFirstObjectByType<CockroachFlyAttack>(FindObjectsInactive.Include);
            if (fly != null && fly.fadeScreenImage != null) fadeScreenImage = fly.fadeScreenImage;

            if (fadeScreenImage == null)
            {
                BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
                if (bed != null && bed.fadeScreenImage != null) fadeScreenImage = bed.fadeScreenImage;
            }

            if (fadeScreenImage == null)
            {
                NPCDeliveryBox box = Object.FindFirstObjectByType<NPCDeliveryBox>(FindObjectsInactive.Include);
                if (box != null && box.fadeScreenImage != null) fadeScreenImage = box.fadeScreenImage;
            }

            if (fadeScreenImage == null)
            {
                GameObject fadeObj = GameObject.Find("FadeScreen");
                if (fadeObj == null) fadeObj = GameObject.Find("FadeImage");
                if (fadeObj == null) fadeObj = GameObject.Find("BlackScreen");
                if (fadeObj != null) fadeScreenImage = fadeObj.GetComponent<Image>();
            }
        }

        EnsureFadeImageExists();

        // Tự động mượn âm thanh thở/rên nếu có trong BedSleepCutscene
        if (wakeUpGaspAudio == null)
        {
            BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
            if (bed != null && bed.wakeUpGroanAudio != null)
            {
                wakeUpGaspAudio = bed.wakeUpGroanAudio;
            }
        }
    }

    void EnsureFadeImageExists()
    {
        if (fadeScreenImage != null) return;

        GameObject fadeObj = GameObject.Find("FadeScreen") ?? GameObject.Find("FadeImage") ?? GameObject.Find("BlackScreen");
        if (fadeObj != null)
        {
            fadeScreenImage = fadeObj.GetComponent<Image>();
            if (fadeScreenImage != null) return;
        }

        CockroachFlyAttack fly = Object.FindFirstObjectByType<CockroachFlyAttack>(FindObjectsInactive.Include);
        if (fly != null && fly.fadeScreenImage != null) { fadeScreenImage = fly.fadeScreenImage; return; }

        BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
        if (bed != null && bed.fadeScreenImage != null) { fadeScreenImage = bed.fadeScreenImage; return; }

        NPCDeliveryBox box = Object.FindFirstObjectByType<NPCDeliveryBox>(FindObjectsInactive.Include);
        if (box != null && box.fadeScreenImage != null) { fadeScreenImage = box.fadeScreenImage; return; }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas targetCanvas = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay || canvases[i].renderMode == RenderMode.ScreenSpaceCamera)
            {
                targetCanvas = canvases[i];
                break;
            }
        }
        if (targetCanvas == null && canvases.Length > 0) targetCanvas = canvases[0];

        if (targetCanvas != null)
        {
            GameObject newFade = new GameObject("FadeScreen");
            newFade.transform.SetParent(targetCanvas.transform, false);
            fadeScreenImage = newFade.AddComponent<Image>();
            fadeScreenImage.color = Color.black;
            RectTransform rt = fadeScreenImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    void OnEnable()
    {
        CockroachFlyAttack.OnCockroachNightmareEnded += HandleCockroachNightmareEnded;
    }

    void OnDisable()
    {
        CockroachFlyAttack.OnCockroachNightmareEnded -= HandleCockroachNightmareEnded;
    }

    void Update()
    {
        if (debugWakeUpKey != KeyCode.None && Input.GetKeyDown(debugWakeUpKey))
        {
            Debug.Log($"[CockroachNightmareWakeUp] ⌨️ Bấm phím [{debugWakeUpKey}]! Test chuỗi bật dậy sau ác mộng...");
            StartWakeUpSequence();
        }

        // Kiểm tra tia nhìn của người chơi vào chiếc thùng hàng sau khi bừng tỉnh
        if (isWaitingForLookAtBox && !hasTriggeredLookAtBox && !isWakingUp && !SmartInteractionDialogue.isAnyDialoguePlaying)
        {
            CheckPlayerSightAtBox();
        }
    }

    void CheckPlayerSightAtBox()
    {
        if (Camera.main == null) return;

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, lookAtBoxMaxDistance))
        {
            bool isBox = false;
            string objName = hit.collider.gameObject.name;
            Transform parent = hit.collider.transform.parent;

            if (objName.Contains("Box") || objName.Contains("CamCorder") || objName.Contains("Camcorder"))
            {
                isBox = true;
            }
            else if (parent != null && (parent.name.Contains("Box") || parent.name.Contains("CamCorder")))
            {
                isBox = true;
            }
            else if (hit.collider.GetComponent<CameraObjectPickup>() != null || hit.collider.GetComponentInParent<CameraObjectPickup>() != null)
            {
                isBox = true;
            }
            else if (hit.collider.GetComponent<CrouchInteractable>() != null || hit.collider.GetComponentInParent<CrouchInteractable>() != null)
            {
                isBox = true;
            }

            if (isBox)
            {
                hasTriggeredLookAtBox = true;
                isWaitingForLookAtBox = false;
                Debug.Log($"[CockroachNightmareWakeUp] 👁️ Người chơi đã nhìn trúng thùng hàng ({objName})! Bắt đầu chuỗi thoại thùng hàng, 5s sau ngất xỉu sang Map02.");
                StartCoroutine(LookAtBoxThenFaintSequenceRoutine());
            }
        }
    }

    IEnumerator LookAtBoxThenFaintSequenceRoutine()
    {
        // 1. PHÁT THOẠI BẤT NGỜ NHÌN THẤY THÙNG HÀNG
        if (lookAtBoxDialogues != null && lookAtBoxDialogues.Length > 0)
        {
            yield return StartCoroutine(PlayDialogueSequenceRoutine(lookAtBoxDialogues));
        }

        // 2. CHỜ 5 GIÂY SAU CÂU THOẠI
        Debug.Log($"[CockroachNightmareWakeUp] ⏱️ Đang đếm ngược {delayBeforeFaint}s sau câu thoại thùng hàng trước khi bị choáng ngất...");
        if (delayBeforeFaint > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFaint);
        }

        // 3. BẮT ĐẦU CHUỖI CHOÁNG VÁNG NGẤT XỈU VÀ CHUYỂN SANG MAP 02
        yield return StartCoroutine(ExecuteDizzyFaintAndLoadMap02Routine());
    }

    IEnumerator ExecuteDizzyFaintAndLoadMap02Routine()
    {
        if (isFainting) yield break;
        isFainting = true;

        Debug.Log("[CockroachNightmareWakeUp] 😵 BẮT ĐẦU HIỆU ỨNG CHOÁNG VÁNG NGẤT XỈU VÀ CHUYỂN SCENE!");

        MovePl player = Object.FindFirstObjectByType<MovePl>();
        Transform camTrans = (player != null && player.cameraTransform != null) ? player.cameraTransform : (Camera.main != null ? Camera.main.transform : null);

        // Khóa di chuyển và góc nhìn của người chơi
        if (player != null)
        {
            player.SetMovementState(false);
            player.isCameraLocked = true;
        }

        // PHÁT CÂU THOẠI: "Chuyện gì thế..."
        if (faintDialogues != null && faintDialogues.Length > 0)
        {
            StartCoroutine(PlayDialogueSequenceRoutine(faintDialogues));
        }

        // Phát âm thanh tim đập / choáng váng nếu có
        if (faintHeartbeatAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(faintHeartbeatAudio, faintAudioVolume);
        }

        EnsureFadeImageExists();

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
                if (fadeScreenImage != null)
                {
                    fadeScreenImage.gameObject.SetActive(true);
                    float blinkAlpha = Mathf.PingPong(t * 1.5f, 0.45f);
                    fadeScreenImage.color = new Color(0f, 0f, 0f, blinkAlpha);
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

                if (fadeScreenImage != null)
                {
                    fadeScreenImage.gameObject.SetActive(true);
                    fadeScreenImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.3f, 1f, t));
                }

                yield return null;
            }
        }

        // GIAI ĐOẠN 3: MÀN HÌNH ĐEN XÌ HOÀN TOÀN (100% BLACKOUT)
        if (fadeScreenImage != null)
        {
            fadeScreenImage.gameObject.SetActive(true);
            fadeScreenImage.color = Color.black;
        }

        // PHÁT TIẾNG "BỊCH" NẶNG TRỊCH NGAY KHI MÀN HÌNH VỪA ĐEN XÌ HOÀN TOÀN!
        if (faintCollapseAudio != null)
        {
            Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(faintCollapseAudio, soundPos, faintAudioVolume);
            Debug.Log($"[CockroachNightmareWakeUp] 🔊 ĐÃ PHÁT ÂM THANH BỊCH/NGÃ GỤC: {faintCollapseAudio.name} (Âm lượng: {faintAudioVolume})");
        }

        // Chờ 1.5s hồi hộp trong bóng tối sau tiếng bịch
        yield return new WaitForSeconds(1.5f);

        // GIAI ĐOẠN 4: CHUYỂN SCENE SANG MAP 02
        Debug.Log($"[CockroachNightmareWakeUp] 🎬 ĐÃ NGẤT XỈU HOÀN TOÀN! Đang tải chuyển sang Scene [{nextSceneName}]...");

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync(nextSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }

    void HandleCockroachNightmareEnded()
    {
        Debug.Log("[CockroachNightmareWakeUp] 🛏️ Nhận được tín hiệu kết thúc ác mộng gián! Bắt đầu chuỗi bừng tỉnh dậy trên nệm...");
        StartWakeUpSequence();
    }

    public void StartWakeUpSequence()
    {
        if (isWakingUp) return;
        StartCoroutine(WakeUpFromNightmareRoutine());
    }

    IEnumerator WakeUpFromNightmareRoutine()
    {
        isWakingUp = true;
        AutoFindPoints();

        MovePl player = Object.FindFirstObjectByType<MovePl>();
        if (player == null)
        {
            Debug.LogWarning("[CockroachNightmareWakeUp] ⚠️ Không tìm thấy MovePl Player!");
            isWakingUp = false;
            yield break;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        Transform camTrans = (player.cameraTransform != null) ? player.cameraTransform : Camera.main.transform;

        // ========================================================
        // BƯỚC 1: KHÓA PLAYER & DỊCH CHUYỂN VỀ NỆM TRONG BÓNG TỐI
        // ========================================================
        player.SetMovementState(false);
        player.isCameraLocked = true;

        if (cc != null) cc.enabled = false;

        EnsureFadeImageExists();
        if (fadeScreenImage != null)
        {
            fadeScreenImage.gameObject.SetActive(true);
            Color c = Color.black;
            c.a = 1f;
            fadeScreenImage.color = c;
        }

        // Teleport Player body về vị trí SitPoint trên nệm
        if (sitCameraPoint != null)
        {
            Vector3 targetPlayerPos = sitCameraPoint.position;
            targetPlayerPos.y = player.transform.position.y;
            player.transform.position = targetPlayerPos;
            player.transform.rotation = Quaternion.Euler(0f, sitCameraPoint.eulerAngles.y, 0f);
        }

        // Đặt Camera nằm ngửa sát gối nhìn lên trần nhà
        if (pillowCameraPoint != null)
        {
            camTrans.position = pillowCameraPoint.position;
            camTrans.rotation = pillowCameraPoint.rotation;
        }

        // ẨN TOÀN BỘ GIÁN VÀ TRIGGER CỬA (An toàn không làm tắt chính script này)
        GameObject roachGroup = GameObject.Find("Cockroach");
        if (roachGroup != null && roachGroup != gameObject && !transform.IsChildOf(roachGroup.transform))
        {
            roachGroup.SetActive(false);
        }

        CockroachDoorAttackTrigger doorTrigger = Object.FindFirstObjectByType<CockroachDoorAttackTrigger>(FindObjectsInactive.Include);
        if (doorTrigger != null && doorTrigger.gameObject != gameObject)
        {
            doorTrigger.gameObject.SetActive(false);
        }

        // TỰ ĐỘNG ĐÓNG CỬA CHÍNH LẠI TRONG BÓNG TỐI
        if (doorToClose == null) doorToClose = Object.FindFirstObjectByType<DoorExit>();
        if (doorToClose != null)
        {
            doorToClose.CloseDoor(true);
            Debug.Log("[CockroachNightmareWakeUp] 🚪 Đã tự động đóng cửa chính lại trong bóng tối!");
        }

        // TỰ ĐỘNG TẮT ĐÈN PHÒNG TRONG BÓNG TỐI
        RoomLightSwitch roomLight = Object.FindFirstObjectByType<RoomLightSwitch>(FindObjectsInactive.Include);
        if (roomLight != null)
        {
            roomLight.SetLightState(false);
            Debug.Log("[CockroachNightmareWakeUp] 💡 Đã tự động tắt đèn phòng trong bóng tối!");
        }

        // TỰ ĐỘNG ĐÓNG TẤT CẢ CỬA SỔ LẠI TRONG BÓNG TỐI
        WindowAmbienceController.CloseAllWindows(true);
        Debug.Log("[CockroachNightmareWakeUp] 🪟 Đã tự động đóng kín tất cả cửa sổ trong bóng tối!");

        // TẮT UI CAMCORDER VÀ TRẢ LẠI MODEL MÁY QUAY VỀ TRONG HỘP
        RestoreCamcorderInBox();

        // Chờ trong bóng tối tĩnh lặng (khoảng lặng hồi hộp sau cơn ác mộng)
        if (darkPauseDuration > 0f)
        {
            yield return new WaitForSeconds(darkPauseDuration);
        }

        // ========================================================
        // BƯỚC 2: CHỚP MẮT MỞ DẦN TỪ BÓNG TỐI (CINEMATIC EYE BLINK & FADE IN)
        // ========================================================
        if (wakeUpGaspAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(wakeUpGaspAudio, audioVolume);
        }

        if (fadeScreenImage != null)
        {
            fadeScreenImage.gameObject.SetActive(true);
            Color fadeCol = Color.black;

            // Nhịp 1: Hé mắt chớp nhẹ lần 1 (1.0 -> 0.65 -> 0.95)
            float t1 = 0f;
            while (t1 < 0.35f)
            {
                t1 += Time.deltaTime;
                fadeCol.a = Mathf.Lerp(1.0f, 0.65f, t1 / 0.35f);
                fadeScreenImage.color = fadeCol;
                yield return null;
            }
            float t1b = 0f;
            while (t1b < 0.15f)
            {
                t1b += Time.deltaTime;
                fadeCol.a = Mathf.Lerp(0.65f, 0.92f, t1b / 0.15f);
                fadeScreenImage.color = fadeCol;
                yield return null;
            }

            // Nhịp 2: Hé mắt mở to hơn lần 2 (0.92 -> 0.35 -> 0.65)
            float t2 = 0f;
            while (t2 < 0.45f)
            {
                t2 += Time.deltaTime;
                fadeCol.a = Mathf.Lerp(0.92f, 0.35f, t2 / 0.45f);
                fadeScreenImage.color = fadeCol;
                yield return null;
            }
            float t2b = 0f;
            while (t2b < 0.2f)
            {
                t2b += Time.deltaTime;
                fadeCol.a = Mathf.Lerp(0.35f, 0.65f, t2b / 0.2f);
                fadeScreenImage.color = fadeCol;
                yield return null;
            }

            // Nhịp 3: Mở mắt to hoàn toàn và sáng rõ (0.65 -> 0.0)
            float t3 = 0f;
            float finalFadeDuration = (openEyesFadeInDuration > 0f) ? openEyesFadeInDuration : 1.2f;
            while (t3 < finalFadeDuration)
            {
                t3 += Time.deltaTime;
                fadeCol.a = Mathf.Lerp(0.65f, 0.0f, t3 / finalFadeDuration);
                fadeScreenImage.color = fadeCol;
                yield return null;
            }

            fadeCol.a = 0f;
            fadeScreenImage.color = fadeCol;
            fadeScreenImage.gameObject.SetActive(false);
        }

        // ========================================================
        // BƯỚC 3: BẬT NGỒI DẬY NHANH NHƯ GẶP ÁC MỘNG (STARTLED SIT UP)
        // ========================================================
        if (sitCameraPoint != null && pillowCameraPoint != null)
        {
            Vector3 startPos = camTrans.position;
            Quaternion startRot = camTrans.rotation;

            Vector3 sitPos = sitCameraPoint.position;
            Quaternion sitRot = sitCameraPoint.rotation;

            float sitElapsed = 0f;
            while (sitElapsed < startleSitUpDuration)
            {
                sitElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(sitElapsed / startleSitUpDuration);

                // Dùng đường cong gia tốc nhanh ở đầu (giật mình bật dậy) rồi hãm lại ở mép đệm
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);

                // Rung nhẹ camera lúc bật dậy
                float joltY = Mathf.Sin(t * Mathf.PI) * startleJoltMagnitude;

                camTrans.position = Vector3.Lerp(startPos, sitPos, smoothT) + new Vector3(0f, joltY, 0f);
                camTrans.rotation = Quaternion.Slerp(startRot, sitRot, smoothT);
                yield return null;
            }

            camTrans.position = sitPos;
            camTrans.rotation = sitRot;
        }

        // ========================================================
        // BƯỚC 4: NGỒI THỞ DỐC Ở MÉP ĐỆM HOÀN HỒN
        // ========================================================
        if (heavyBreathingAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(heavyBreathingAudio, audioVolume * 0.8f);
        }

        if (sitBreathingDuration > 0f)
        {
            // Camera hơi nhấp nhô nhẹ theo nhịp thở dốc
            float breatheElapsed = 0f;
            Vector3 sitBasePos = camTrans.position;

            while (breatheElapsed < sitBreathingDuration)
            {
                breatheElapsed += Time.deltaTime;
                float breathOffset = Mathf.Sin(breatheElapsed * 6f) * 0.015f;
                camTrans.position = sitBasePos + new Vector3(0f, breathOffset, 0f);
                yield return null;
            }

            camTrans.position = sitBasePos;
        }

        // ========================================================
        // BƯỚC 5: NÂNG NGƯỜI ĐỨNG LÊN & TRẢ LẠI QUYỀN ĐIỀU KHIỂN CHO PLAYER
        // ========================================================
        if (sitCameraPoint != null)
        {
            player.transform.rotation = Quaternion.Euler(0f, sitCameraPoint.eulerAngles.y, 0f);

            float targetPitch = sitCameraPoint.eulerAngles.x;
            if (targetPitch > 180f) targetPitch -= 360f;
            targetPitch = Mathf.Clamp(targetPitch, -90f, 90f);

            Vector3 startLocalPos = camTrans.localPosition;
            Vector3 targetLocalPos = (originalCameraLocalPos != Vector3.zero) ? originalCameraLocalPos : new Vector3(0f, 0.6f, 0f);
            Quaternion startLocalRot = camTrans.localRotation;
            Quaternion targetLocalRot = Quaternion.Euler(targetPitch, 0f, 0f);

            float standElapsed = 0f;
            while (standElapsed < standUpDuration)
            {
                standElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, standElapsed / standUpDuration);

                camTrans.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
                camTrans.localRotation = Quaternion.Slerp(startLocalRot, targetLocalRot, t);
                yield return null;
            }

            camTrans.localPosition = targetLocalPos;
            camTrans.localRotation = targetLocalRot;
        }

        // Bật lại CharacterController và mở khóa Player
        if (cc != null) cc.enabled = true;

        player.SyncRotationWithCurrentCamera();
        player.isCameraLocked = false;
        player.SetMovementState(true);

        isWakingUp = false;
        isWaitingForLookAtBox = true;
        hasTriggeredLookAtBox = false;
        Debug.Log("[CockroachNightmareWakeUp] ✅ Player đã bừng tỉnh dậy trên nệm! Đã mở khóa hoàn toàn di chuyển & góc nhìn chuột!");

        // 6. PHÁT THOẠI BỪNG TỈNH DẬY HOÀN HỒN SAU ÁC MỘNG
        if (wakeUpDialogues != null && wakeUpDialogues.Length > 0)
        {
            StartCoroutine(PlayDialogueSequenceRoutine(wakeUpDialogues));
        }
    }

    IEnumerator PlayDialogueSequenceRoutine(SmartInteractionDialogue.DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;

        TMPro.TextMeshProUGUI subtitleTextUI = FindSubtitleUI();
        if (subtitleTextUI == null) yield break;

        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        if (subtitleTextUI.transform.parent != null) subtitleTextUI.transform.parent.gameObject.SetActive(true);
        subtitleTextUI.gameObject.SetActive(true);

        yield return null;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null) continue;

            string fullText = (SettingsManager.currentLanguage == "VI") ? line.vietnameseDialogue : line.englishDialogue;
            if (string.IsNullOrEmpty(fullText)) fullText = line.vietnameseDialogue;
            if (string.IsNullOrEmpty(fullText)) fullText = line.englishDialogue;
            if (string.IsNullOrEmpty(fullText)) continue;

            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;

            if (dialogueSound != null && audioSource != null)
            {
                audioSource.clip = dialogueSound;
                audioSource.volume = dialogueVolume;
                audioSource.loop = true;
                audioSource.time = 0f;
                audioSource.Play();
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

            if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound) audioSource.Stop();
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
            while (fadeElapsed < 0.25f)
            {
                fadeElapsed += Time.deltaTime;
                Color col = subtitleTextUI.color;
                col.a = 1f - (fadeElapsed / 0.25f);
                subtitleTextUI.color = col;
                yield return null;
            }

            subtitleTextUI.text = "";
        }

        subtitleTextUI.gameObject.SetActive(false);
        if (subtitleTextUI.transform.parent != null) subtitleTextUI.transform.parent.gameObject.SetActive(false);
        SmartInteractionDialogue.isAnyDialoguePlaying = false;
    }

    TMPro.TextMeshProUGUI FindSubtitleUI()
    {
        SmartInteractionDialogue sid = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
        if (sid != null && sid.subtitleTextUI != null) return sid.subtitleTextUI;

        BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
        if (bed != null && bed.subtitleTextUI != null) return bed.subtitleTextUI;

        GameIntroManager intro = Object.FindFirstObjectByType<GameIntroManager>(FindObjectsInactive.Include);
        if (intro != null && intro.subtitleTextUI != null) return intro.subtitleTextUI;

        GameObject subObj = GameObject.Find("SubtitlesText") ?? GameObject.Find("SubtitleText") ?? GameObject.Find("Subtitle") ?? GameObject.Find("DialogueText");
        if (subObj != null) return subObj.GetComponent<TMPro.TextMeshProUGUI>();

        return null;
    }

    void RestoreCamcorderInBox()
    {
        // 1. Tắt toàn bộ UI và trạng thái nhặt máy quay
        CamcorderUI.ResetPickedUpCameraState();
        if (FlashlightToggle.Instance != null) FlashlightToggle.Instance.UnequipFlashlight();

        SimpleCameraOverlay overlay = Camera.main != null ? Camera.main.GetComponent<SimpleCameraOverlay>() : Object.FindFirstObjectByType<SimpleCameraOverlay>();
        if (overlay != null) overlay.ResetCameraView();

        // 2. Tìm vị trí hộp mở cũ trong Scene
        GameObject boxOpenObj = GameObject.Find("BoxOpen(Clone)") ?? GameObject.Find("BoxOpen");
        Vector3 boxPos = new Vector3(222.9168f, 68.2989f, 182.6587f);
        Quaternion boxRot = Quaternion.Euler(-90f, 0f, 0f);
        Vector3 boxScale = new Vector3(0.5f, 0.5f, 0.5f);

        if (boxOpenObj != null)
        {
            boxPos = boxOpenObj.transform.position;
            boxRot = boxOpenObj.transform.rotation;
            boxScale = boxOpenObj.transform.localScale;
            Destroy(boxOpenObj);
        }

        // Tự động tìm openBoxPrefab nếu chưa kéo vào ô Inspector
        if (openBoxPrefab == null)
        {
            OpenablePlacedBox placedBox = Object.FindFirstObjectByType<OpenablePlacedBox>(FindObjectsInactive.Include);
            if (placedBox != null && placedBox.openBoxPrefab != null)
            {
                openBoxPrefab = placedBox.openBoxPrefab;
            }
        }

        // 3. TÁI TẠO (SPAWN) LẠI 1 HỘP MỞ MỚI TINH CHỨA MÔ HÌNH MÁY QUAY NHƯNG KHÓA TƯƠNG TÁC NHẶT!
        if (openBoxPrefab != null)
        {
            GameObject newBox = Instantiate(openBoxPrefab, boxPos, boxRot);
            newBox.name = "BoxOpen";
            newBox.transform.localScale = boxScale;
            newBox.SetActive(true);

            // VÔ HIỆU HÓA HOÀN TOÀN TƯƠNG TÁC NHẶT MÁY QUAY ĐỂ TRÁNH PLAYER NHẶT LÊN LẠI
            CameraObjectPickup[] pickups = newBox.GetComponentsInChildren<CameraObjectPickup>(true);
            foreach (var p in pickups) Destroy(p);

            CrouchInteractable[] crouches = newBox.GetComponentsInChildren<CrouchInteractable>(true);
            foreach (var c in crouches) Destroy(c);

            InteractPrompt[] prompts = newBox.GetComponentsInChildren<InteractPrompt>(true);
            foreach (var pr in prompts) Destroy(pr);

            Debug.Log("[CockroachNightmareWakeUp] 📦 ĐÃ TÁI TẠO BOXOPEN VÀ KHÓA TOÀN BỘ TƯƠNG TÁC CÚI NHẶT MÁY QUAY!");
        }

        // 4. Reset lại sự kiện đếm giờ 10s máy quay mở cửa
        Camera10sDoorEvent doorEvt = Object.FindFirstObjectByType<Camera10sDoorEvent>(FindObjectsInactive.Include);
        if (doorEvt != null)
        {
            doorEvt.ResetEvent();
        }
    }
}
