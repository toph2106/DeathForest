using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Script quản lý tổng thể Map 05:
/// 1. Tự động thiết lập trạng thái phòng khi vừa vào Scene (Đóng sẵn cửa sổ, hạ âm thanh thành phố, tắt sẵn đèn sang đèn ngủ, đóng cửa chính).
/// 2. Kích hoạt chuỗi cắt cảnh bừng tỉnh dậy trên đệm sau cơn ác mộng bị xe tải đâm (Map 04).
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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Tự động sửa lại nếu Inspector đang bị kẹt giá trị 2.5 cũ
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
        if (!isWakingUp) return;

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

    // =========================================================================
    // 1. THIẾT LẬP TRẠNG THÁI PHÒNG MẶC ĐỊNH
    // =========================================================================

    public void ApplyDefaultRoomState()
    {
        // 1. Tự động tắt đèn chính sang chế độ đèn ngủ
        if (autoTurnOffRoomLight)
        {
            RoomLightSwitch roomLight = Object.FindFirstObjectByType<RoomLightSwitch>(FindObjectsInactive.Include);
            if (roomLight != null)
            {
                roomLight.SetLightState(false);
            }
        }

        // 3. Tự động đóng cửa chính
        if (autoCloseDoor)
        {
            DoorExit door = Object.FindFirstObjectByType<DoorExit>(FindObjectsInactive.Include);
            if (door != null)
            {
                door.CloseDoor(true);
            }
        }

        // 4. Tắt các GameObjects cần tắt
        if (objectsToDisableOnStart != null)
        {
            foreach (var obj in objectsToDisableOnStart)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        // 5. Bật các GameObjects cần bật
        if (objectsToEnableOnStart != null)
        {
            foreach (var obj in objectsToEnableOnStart)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        Debug.Log("[Map05Manager] 🏠 Đã thiết lập trạng thái phòng mặc định (Cửa sổ đóng, Âm thanh thành phố nhỏ, Đèn ngủ bật, Cửa chính đóng)!");
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

        // 7. TRẢ LẠI QUYỀN ĐIỀU KHIỂN
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
    }

    // =========================================================================
    // 3. DIALOGUE TYPEWRITER & CLICK SKIP
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
    // 4. AUTO FIND HELPERS
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
}
