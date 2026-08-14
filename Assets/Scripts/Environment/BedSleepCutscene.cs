using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BedSleepCutscene : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class SleepDialogueLine
    {
        [TextArea(2, 4)]
        public string englishDialogue;
        [TextArea(2, 4)]
        public string vietnameseDialogue;

        [Tooltip("Âm thanh lồng tiếng / ngáp / ư ử tương ứng (Tùy chọn)")]
        public AudioClip dialogueAudio;
    }

    // =================================================================
    // 1. ĐIỂM VỊ TRÍ
    // =================================================================
    [Header("1. Điểm Vị Trí (Camera & Player Points)")]
    [Tooltip("Point 1: Vị trí mép đệm (Ngồi). Bấm F → cả Player (Main) sẽ di chuyển & xoay đúng theo Point này.")]
    public Transform sitCameraPoint;

    [Tooltip("Point 2: Vị trí nằm sát gối (Sleep) - Rotation xoay nghiêng sang trái")]
    public Transform pillowCameraPoint;

    // =================================================================
    // 2. THỜI GIAN NẰM
    // =================================================================
    [Header("2. Thời Gian Chuỗi Động Tác Nằm")]
    [Tooltip("Di chuyển cụm Main đến SitPoint (2.0s)")]
    public float approachDuration = 2.0f;

    [Tooltip("Hạ thấp Camera Y (ngồi xuống) (1.0s)")]
    public float lowerToSitDuration = 1.0f;

    [Tooltip("Xoay Camera X ngửa lên -60° nhìn trần (1.0s)")]
    public float tiltToCeilingDuration = 1.0f;

    [Tooltip("Hạ chậm Camera xuống Pillow Point (1.5s)")]
    public float descendToPillowDuration = 1.5f;

    [Tooltip("Xoay Camera X từ -60 lên -90° nhìn thẳng trần (1.0s)")]
    public float tiltToFullCeilingDuration = 1.0f;

    [Tooltip("Tạm dừng nằm ngửa nhìn trần (2.0s)")]
    public float holdOnBackDuration = 2.0f;

    [Tooltip("Xoay đầu nghiêng sang trái (1.5s)")]
    public float turnHeadLeftDuration = 1.5f;

    [Tooltip("Giữ mở mắt nằm nghiêng trước khi ngủ (5.0s)")]
    public float holdEyesOpenDuration = 5.0f;

    // =================================================================
    // 3. PROMPT
    // =================================================================
    [Header("3. Prompt UI")]
    public string englishPrompt = "[F] Go to Sleep";
    public string vietnamesePrompt = "[F] Nằm xuống ngủ";

    // =================================================================
    // 4. FADE
    // =================================================================
    [Header("4. Fade Màn Hình Đen")]
    [Tooltip("Tắt để căn chỉnh Camera & Mèo")]
    public bool fadeToBlackAfterSleep = true;

    [Tooltip("Kéo Image đen full màn hình (nằm DƯỚI text thoại & đồng hồ trong Hierarchy)")]
    public Image fadeScreenImage;

    [Tooltip("Thời gian nhắm mắt dần vào bóng tối (Mặc định: 4.0s - Chậm rãi dịu mắt)")]
    public float fadeOutDuration = 4.0f;

    // =================================================================
    // 5. THOẠI (HỖ TRỢ BẤM CHUỘT TRÁI QUA THOẠI NHANH)
    // =================================================================
    [Header("5. Thoại & Hiệu Ứng Chữ (Hỗ trợ Click Chuột Skip Nhanh)")]
    public TMPro.TextMeshProUGUI subtitleTextUI;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.03f;

    [Tooltip("[0] Trước khi ngủ\n[1] Ư ử bị đánh thức (Đợt 2)\n[2] Sau khi ngồi dậy")]
    public SleepDialogueLine[] sleepDialogues = new SleepDialogueLine[]
    {
        new SleepDialogueLine { vietnameseDialogue = "Mệt mỏi quá... Cuối cùng cũng được chợp mắt.", englishDialogue = "So tired... Finally I can get some sleep." },
        new SleepDialogueLine { vietnameseDialogue = "Ưm... Ai gõ cửa giờ này thế...?", englishDialogue = "Ugh... Who is knocking at this hour...?" },
        new SleepDialogueLine { vietnameseDialogue = "Chuyện gì đang xảy ra vậy...?", englishDialogue = "What's going on...?" }
    };

    // =================================================================
    // 6. ÂM THANH
    // =================================================================
    [Header("6. Âm Thanh Khi Nằm Ngủ (Tùy chọn)")]
    public AudioClip sleepAudio;
    [Range(0f, 1f)] public float audioVolume = 0.8f;

    // =================================================================
    // 7. MÈO
    // =================================================================
    [Header("7. Mèo Chạy Tới Meow")]
    public Cat catScript;
    public Transform catMeowTargetPoint;
    public float delayBeforeCatRuns = 2.0f;
    public float catRunSpeed = 1.5f;

    // =================================================================
    // 8. MỞ KHÓA
    // =================================================================
    [Header("8. Mở Khóa Tương Tác")]
    public bool lockOnStart = true;

    // =================================================================
    // 9. ĐỒNG HỒ
    // =================================================================
    [Header("9. Đồng Hồ Tua Thời Gian")]
    public TMPro.TextMeshProUGUI clockTextUI;
    public bool enableClockFastForward = true;
    public int startHour = 21;
    public int startMinute = 53;
    public int endHour = 23;
    public int endMinute = 54;
    public float clockTickSpeed = 0.04f;
    [Tooltip("Thời gian hiện dần (Fade in) đồng hồ điện tử lúc bắt đầu bóng tối (1.5s)")]
    public float clockFadeInDuration = 1.5f;

    // =================================================================
    // 10. GÕ CỬA
    // =================================================================
    [Header("10. Chuỗi Gõ Cửa")]
    public bool enableKnockSequence = true;
    public AudioClip doorKnockSound;
    [Range(0f, 1f)] public float knockVolume = 0.5f;
    public AudioSource doorAudioSource;
    public AudioClip wakeUpGroanAudio;

    // =================================================================
    // 10b. THỜI GIAN GÕ CỬA & TỈNH DẬY
    // =================================================================
    [Header("Thời Gian Gõ Cửa & Tỉnh Dậy")]
    [Tooltip("Delay đợt gõ 1 → 2 (4.0s)")]
    public float delayBetweenKnock1And2 = 4.0f;

    [Tooltip("Delay đợt gõ 2 → 3 (3.5s) - ĐỦ LÂU cho thoại ư ử")]
    public float delayBetweenKnock2And3 = 3.5f;

    [Tooltip("Mở mắt chậm rãi lơ mơ đồng thời làm mờ đồng hồ (3.5s)")]
    public float wakeUpFadeInDuration = 3.5f;

    [Tooltip("Góc quay đầu trái-phải lơ mơ (15°)")]
    public float groggyHeadTurnAngle = 15f;
    [Tooltip("Thời gian mỗi lượt quay đầu (1.5s)")]
    public float groggyHeadTurnDuration = 1.5f;
    [Tooltip("Nghỉ giữa các lượt quay (0.4s)")]
    public float groggyHeadTurnPause = 0.4f;

    [Tooltip("Bẩy ngồi dậy chậm rãi từ gối → mép đệm (3.0s)")]
    public float sitUpDuration = 3.0f;

    [Tooltip("Ngồi thở ở mép đệm trước khi có thoại (2.0s)")]
    public float sitBreathingDelay = 2.0f;

    [Tooltip("Delay sau thoại ngồi trước khi đứng dậy (2.5s)")]
    public float delayBeforeStandUp = 2.5f;

    [Tooltip("Thời gian nâng người đứng dậy từ đệm (1.2s - mượt mà không giật)")]
    public float standUpDuration = 1.2f;

    // =================================================================
    // 11. GÕ CỬA LIÊN TỤC
    // =================================================================
    [Header("11. Gõ Cửa Liên Tục Cho Đến Khi Tương Tác Cửa")]
    public ContinuousDoorKnocker continuousKnocker;

    [Header("12. Mở Khóa Cửa Chính Sau Khi Thức Dậy")]
    [Tooltip("Kéo Collider của Cửa Chính vào đây để tự động mở khóa tương tác mở cửa sau khi tỉnh dậy!")]
    public Collider doorColliderToEnable;
    public GameObject doorObjectToEnable;

    // =================================================================
    // PRIVATE
    // =================================================================
    private Collider bedCollider;
    private InteractPrompt interactPrompt;
    private bool isSleeping = false;
    private AudioSource localAudioSource;

    // --- BIẾN ĐIỀU KHIỂN TYPEWRITER & CLICK SKIP THOẠI ---
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private bool skipWaitRequested = false;
    private string currentFullText = "";

    // Lưu vị trí ban đầu
    private Vector3 originalCameraLocalPos;

    void Awake()
    {
        bedCollider = GetComponent<Collider>();
        interactPrompt = GetComponent<InteractPrompt>();
        localAudioSource = GetComponent<AudioSource>();
        if (localAudioSource == null) localAudioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        if (interactPrompt == null)
            interactPrompt = gameObject.AddComponent<InteractPrompt>();
        interactPrompt.englishPrompt = englishPrompt;
        interactPrompt.vietnamesePrompt = vietnamesePrompt;

        if (lockOnStart && bedCollider != null)
            bedCollider.enabled = false;

        if (clockTextUI != null)
        {
            Color c = clockTextUI.color;
            c.a = 0f;
            clockTextUI.color = c;
            clockTextUI.gameObject.SetActive(false);
        }

        if (fadeScreenImage != null)
        {
            fadeScreenImage.color = new Color(0f, 0f, 0f, 0f);
            fadeScreenImage.gameObject.SetActive(false);
        }
        if (subtitleTextUI != null) subtitleTextUI.text = "";
    }

    void Update()
    {
        if (!isSleeping) return;

        // BẤM CHUỘT TRÁI (Mouse 0), SPACE HOẶC F ĐỂ SKIP / QUA THOẠI NHANH
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F))
        {
            if (isTyping)
            {
                // BẤM LẦN 1 KHI ĐANG GÕ ➔ HIỆN TOÀN BỘ CÂU NGAY LẬP TỨC
                isTyping = false;
                skipRequested = true;
                if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;
            }
            else if (isWaitingForNextLine)
            {
                // BẤM LẦN 2 KHI CHỮ ĐÃ HIỆN ĐẦY ĐỦ ➔ BỎ QUA THỜI GIAN CHỜ SANG BƯỚC TIẾP THEO
                skipWaitRequested = true;
            }
        }
    }

    public void Interact()
    {
        if (isSleeping) return;
        Debug.Log("[BedSleepCutscene] 🛌 Bắt đầu chuỗi nằm ngủ!");
        StartCoroutine(SleepSequenceRoutine());
    }

    // =================================================================
    // CHUỖI CHÍNH
    // =================================================================
    IEnumerator SleepSequenceRoutine()
    {
        isSleeping = true;
        if (bedCollider != null) bedCollider.enabled = false;
        HidePrompt();

        MovePl player = Object.FindFirstObjectByType<MovePl>();
        Transform camTrans = Camera.main != null ? Camera.main.transform : null;
        CharacterController cc = player != null ? player.controller : null;

        if (player == null || camTrans == null || sitCameraPoint == null || pillowCameraPoint == null)
        {
            Debug.LogError("[BedSleepCutscene] ❌ Thiếu Player/Camera/Points!");
            yield break;
        }

        // Khóa điều khiển
        player.isCameraLocked = true;
        player.SetMovementState(false);

        // Lưu camera local standing position
        originalCameraLocalPos = camTrans.localPosition;

        // Tắt CharacterController để có thể di chuyển bằng transform
        if (cc != null) cc.enabled = false;

        // ============================
        // BƯỚC 1: DI CHUYỂN CẢ CỤM MAIN (PLAYER) ĐẾN SIT POINT
        // ============================
        {
            Vector3 playerStartPos = player.transform.position;
            Quaternion playerStartRot = player.transform.rotation;

            Vector3 playerTargetPos = new Vector3(
                sitCameraPoint.position.x,
                playerStartPos.y,
                sitCameraPoint.position.z
            );
            Quaternion playerTargetRot = Quaternion.Euler(0f, sitCameraPoint.eulerAngles.y, 0f);

            float elapsed = 0f;
            while (elapsed < approachDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / approachDuration);
                player.transform.position = Vector3.Lerp(playerStartPos, playerTargetPos, t);
                player.transform.rotation = Quaternion.Slerp(playerStartRot, playerTargetRot, t);
                yield return null;
            }
            player.transform.position = playerTargetPos;
            player.transform.rotation = playerTargetRot;

            Debug.Log("[BedSleepCutscene] ✅ Player (Main) đã đến vị trí SitPoint.");
        }

        // ============================
        // TỪ ĐÂY TRỞ ĐI: CHỈ DI CHUYỂN CAMERA
        // ============================

        // BƯỚC 2: HẠ THẤP CAMERA Y (NGỒI XUỐNG)
        {
            Vector3 startLocal = camTrans.localPosition;
            Vector3 targetLocal = new Vector3(startLocal.x, startLocal.y - 0.4f, startLocal.z);

            float elapsed = 0f;
            while (elapsed < lowerToSitDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / lowerToSitDuration);
                camTrans.localPosition = Vector3.Lerp(startLocal, targetLocal, t);
                yield return null;
            }
            camTrans.localPosition = targetLocal;
        }

        // BƯỚC 3: XOAY CAMERA X THÀNH -60° (NGỬA TRẦN NHÀ)
        {
            Quaternion startRot = camTrans.rotation;
            Vector3 euler = startRot.eulerAngles;
            Quaternion targetRot = Quaternion.Euler(-60f, euler.y, euler.z);

            float elapsed = 0f;
            while (elapsed < tiltToCeilingDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / tiltToCeilingDuration);
                camTrans.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            camTrans.rotation = targetRot;
        }

        // BƯỚC 4+5: HẠ CAMERA XUỐNG PILLOW + XOAY X → -90°
        {
            Vector3 startPos = camTrans.position;
            Quaternion startRot = camTrans.rotation;
            Vector3 targetPos = pillowCameraPoint.position;
            Vector3 euler = startRot.eulerAngles;
            Quaternion targetRot = Quaternion.Euler(-90f, euler.y, euler.z);

            float elapsed = 0f;
            float duration = descendToPillowDuration + tiltToFullCeilingDuration;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                camTrans.position = Vector3.Lerp(startPos, targetPos, t);
                camTrans.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            camTrans.position = targetPos;
            camTrans.rotation = targetRot;
        }

        // BƯỚC 6: NẰM NGỬA NHÌN TRẦN
        if (holdOnBackDuration > 0f)
            yield return new WaitForSeconds(holdOnBackDuration);

        // BƯỚC 7: GIẢM DẦN NHẠC MÈO VỀ 0 (FADE OUT) VÀ XOAY ĐẦU NGHIÊNG TRÁI
        if (catScript != null)
        {
            catScript.FadeOutCatSoundAndStop(turnHeadLeftDuration);
        }

        {
            Quaternion startRot = camTrans.rotation;
            Quaternion targetRot = pillowCameraPoint.rotation;
            float elapsed = 0f;
            while (elapsed < turnHeadLeftDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / turnHeadLeftDuration);
                camTrans.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            camTrans.rotation = targetRot;
        }

        // BƯỚC 8: THOẠI TRƯỚC KHI NGỦ (HỖ TRỢ CLICK SKIP) + MÈO CHẠY VỀ MEOW
        if (sleepAudio != null)
            AudioSource.PlayClipAtPoint(sleepAudio, pillowCameraPoint.position, audioVolume);

        if (catScript != null && catMeowTargetPoint != null)
            StartCoroutine(TriggerCatRunSequence());

        // Chạy thoại trước khi ngủ và chờ người chơi bấm click hoặc hết thời gian
        yield return StartCoroutine(PlayDialogueLineRoutine(0, holdEyesOpenDuration));

        ClearSubtitle();

        // =================================================================
        // CHƯƠNG 2: FADE → HIỆN ĐỒNG HỒ & TUA → GÕ CỬA → TỈNH DẬY
        // =================================================================
        if (!fadeToBlackAfterSleep || fadeScreenImage == null)
        {
            Debug.Log("[BedSleepCutscene] 👁️ Tắt fade - giữ mở mắt để căn chỉnh!");
            yield break;
        }

        // FADE NHẮM MẮT CHẬM RÃI VÀO BÓNG TỐI
        yield return StartCoroutine(FadeToBlack(fadeOutDuration));

        // Ẩn dưới màn đen: trả camera về nằm ngửa nhìn trần (cho lúc mở mắt)
        camTrans.rotation = Quaternion.Euler(-90f, sitCameraPoint.eulerAngles.y, 0f);

        // HIỆN DẦN & TUA ĐỒNG HỒ (GIỮ NGUYÊN TRONG BÓNG TỐI)
        if (enableClockFastForward && clockTextUI != null)
            yield return StartCoroutine(RunClockFadeInAndFastForwardRoutine());

        // CHUỖI GÕ CỬA & TỈNH DẬY
        if (enableKnockSequence)
            yield return StartCoroutine(KnockAndWakeUpRoutine(player, cc, camTrans));
    }

    // =================================================================
    // HIỆN DẦN VÀ TUA ĐỒNG HỒ TRONG BÓNG TỐI (GIẢM TỐC 4 PHÚT CUỐI TẠO CĂNG THẲNG)
    // =================================================================
    IEnumerator RunClockFadeInAndFastForwardRoutine()
    {
        if (clockTextUI == null) yield break;

        clockTextUI.gameObject.SetActive(true);
        if (clockTextUI.transform.parent != null)
            clockTextUI.transform.parent.gameObject.SetActive(true);

        clockTextUI.text = string.Format("{0:D2}:{1:D2}", startHour, startMinute);

        // Hiện dần (Fade in) đồng hồ
        float elapsedFade = 0f;
        Color c = clockTextUI.color;
        c.a = 0f;
        clockTextUI.color = c;

        while (elapsedFade < clockFadeInDuration)
        {
            elapsedFade += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsedFade / clockFadeInDuration);
            clockTextUI.color = c;
            yield return null;
        }
        c.a = 1f;
        clockTextUI.color = c;

        // Tua thời gian
        int h = startHour, m = startMinute;
        while (h != endHour || m != endMinute)
        {
            clockTextUI.text = string.Format("{0:D2}:{1:D2}", h, m);
            m++;
            if (m >= 60) { m = 0; h = (h + 1) % 24; }

            // Tính khoảng cách phút còn lại đến đích
            int remMinutes = GetRemainingMinutes(h, m, endHour, endMinute);
            float currentSpeed = clockTickSpeed;

            // KHI CÒN <= 4 PHÚT TRƯỚC ĐÍCH: CHẠY CHẬM DẦN TỪ TỪ ĐỂ TẠO CẢM GIÁC HỒI HỘP
            if (remMinutes <= 4)
            {
                float t = Mathf.Clamp01((4f - remMinutes) / 4f);
                currentSpeed = Mathf.Lerp(0.18f, 0.75f, t);
            }

            yield return new WaitForSeconds(currentSpeed);
        }

        clockTextUI.text = string.Format("{0:D2}:{1:D2}", endHour, endMinute);
        yield return new WaitForSeconds(1.0f);
    }

    int GetRemainingMinutes(int currentH, int currentM, int targetH, int targetM)
    {
        int currentTotal = currentH * 60 + currentM;
        int targetTotal = targetH * 60 + targetM;
        if (targetTotal < currentTotal) targetTotal += 24 * 60;
        return targetTotal - currentTotal;
    }

    // =================================================================
    // CHUỖI GÕ CỬA + TỈNH DẬY TỰ NHIÊN
    // =================================================================
    IEnumerator KnockAndWakeUpRoutine(MovePl player, CharacterController cc, Transform camTrans)
    {
        // ─── ĐỢT 1: GÕ CỬA (IM LẶNG - ĐỒNG HỒ VẪN SÁNG) ───
        PlayDoorKnock(knockVolume);
        Debug.Log("[BedSleepCutscene] 🚪 Gõ cửa ĐỢT 1");
        yield return new WaitForSeconds(delayBetweenKnock1And2);

        // ─── ĐỢT 2: GÕ CỬA (Ư Ử + THOẠI 1 CÓ CLICK SKIP) ───
        PlayDoorKnock(knockVolume);
        Debug.Log("[BedSleepCutscene] 🚪 Gõ cửa ĐỢT 2");

        if (wakeUpGroanAudio != null && localAudioSource != null)
            localAudioSource.PlayOneShot(wakeUpGroanAudio, audioVolume);

        yield return StartCoroutine(PlayDialogueLineRoutine(1, delayBetweenKnock2And3));
        ClearSubtitle();

        // ─── ĐỢT 3: GÕ CỬA → BẮT ĐẦU MỞ MẮT & KÍCH HOẠT GÕ CỬA LIÊN TỤC NGAY LÚC NÀY! ───
        PlayDoorKnock(knockVolume);
        Debug.Log("[BedSleepCutscene] 🚪 Gõ cửa ĐỢT 3 → Bắt đầu mở mắt & kích hoạt gõ cửa liên tục!");

        // KÍCH HOẠT GÕ CỬA LIÊN TỤC NGAY LÚC MỞ MẮT
        TriggerContinuousKnocker();

        // Mở mắt chậm rãi và làm mờ dần đồng hồ cùng lúc
        yield return StartCoroutine(FadeFromBlackWithClock(wakeUpFadeInDuration));

        // ─── LƠ MƠ QUAY ĐẦU TRÁI-PHẢI NẰM NHÌN TRẦN ───
        {
            Quaternion centerRot = camTrans.rotation;
            float yaw = sitCameraPoint.eulerAngles.y;
            float pitch = -90f;

            Quaternion leftRot = Quaternion.Euler(pitch, yaw - groggyHeadTurnAngle, 0f);
            yield return StartCoroutine(SmoothRotate(camTrans, centerRot, leftRot, groggyHeadTurnDuration));
            yield return new WaitForSeconds(groggyHeadTurnPause);

            Quaternion rightRot = Quaternion.Euler(pitch, yaw + groggyHeadTurnAngle, 0f);
            yield return StartCoroutine(SmoothRotate(camTrans, leftRot, rightRot, groggyHeadTurnDuration * 1.2f));
            yield return new WaitForSeconds(groggyHeadTurnPause);

            yield return StartCoroutine(SmoothRotate(camTrans, rightRot, centerRot, groggyHeadTurnDuration * 0.8f));
        }

        // ─── BẨY NGỒI DẬY CHẬM (GỐI → MÉP ĐỆM) ───
        {
            Vector3 startPos = camTrans.position;
            Quaternion startRot = camTrans.rotation;
            Vector3 sitPos = sitCameraPoint.position;
            Quaternion sitRot = sitCameraPoint.rotation;

            float elapsed = 0f;
            while (elapsed < sitUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / sitUpDuration);
                camTrans.position = Vector3.Lerp(startPos, sitPos, t);
                camTrans.rotation = Quaternion.Slerp(startRot, sitRot, t);
                yield return null;
            }
            camTrans.position = sitPos;
            camTrans.rotation = sitRot;
        }

        // ─── NGỒI THỞ Ở MÉP ĐỆM (sitBreathingDelay) ───
        if (sitBreathingDelay > 0f)
            yield return new WaitForSeconds(sitBreathingDelay);

        // ─── THOẠI 2: "Chuyện gì đang xảy ra vậy...?" (HỖ TRỢ CLICK SKIP) ───
        yield return StartCoroutine(PlayDialogueLineRoutine(2, delayBeforeStandUp));
        ClearSubtitle();

        // ─── ĐỨNG DẬY TẠI ĐỆM: NÂNG CAMERA TỪ NGỒI LÊN ĐỨNG MƯỢT MÀ KHÔNG GIẬT GÓC ───
        {
            player.transform.rotation = Quaternion.Euler(0f, sitCameraPoint.eulerAngles.y, 0f);

            float pitch = sitCameraPoint.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, -90f, 90f);

            Vector3 startLocal = camTrans.localPosition;
            Vector3 targetLocal = originalCameraLocalPos;
            Quaternion targetLocalRot = Quaternion.Euler(pitch, 0f, 0f);
            Quaternion startLocalRot = camTrans.localRotation;

            float elapsed = 0f;
            while (elapsed < standUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / standUpDuration);
                camTrans.localPosition = Vector3.Lerp(startLocal, targetLocal, t);
                camTrans.localRotation = Quaternion.Slerp(startLocalRot, targetLocalRot, t);
                yield return null;
            }
            camTrans.localPosition = targetLocal;
            camTrans.localRotation = targetLocalRot;
        }

        // Bật lại CharacterController
        if (cc != null) cc.enabled = true;

        // MỞ KHÓA TƯƠNG TÁC CHO CỬA CHÍNH
        if (doorColliderToEnable != null)
        {
            doorColliderToEnable.enabled = true;
            Debug.Log("[BedSleepCutscene] 🔓 Đã mở khóa tương tác cho Cửa Chính!");
        }
        if (doorObjectToEnable != null)
        {
            doorObjectToEnable.SetActive(true);
        }

        DoorExit doorExit = Object.FindFirstObjectByType<DoorExit>();
        if (doorExit != null)
        {
            doorExit.UnlockDoor();
        }

        // Trả lại quyền điều khiển
        player.isCameraLocked = false;
        player.SetMovementState(true);
        player.SyncRotationWithCurrentCamera();

        isSleeping = false;
        Debug.Log("[BedSleepCutscene] 🌅 Đứng dậy hoàn tất! Quyền điều khiển đã mở lại, Cửa Chính đã mở khóa.");
    }

    void TriggerContinuousKnocker()
    {
        if (continuousKnocker == null)
        {
            continuousKnocker = Object.FindFirstObjectByType<ContinuousDoorKnocker>();
        }
        if (continuousKnocker != null)
        {
            if (continuousKnocker.knockClip == null && doorKnockSound != null)
            {
                continuousKnocker.knockClip = doorKnockSound;
            }
            if (continuousKnocker.doorAudioSource == null && doorAudioSource != null)
            {
                continuousKnocker.doorAudioSource = doorAudioSource;
            }
            continuousKnocker.StartKnocking();
            Debug.Log("[BedSleepCutscene] 🚪 Đã kích hoạt gõ cửa liên tục ngay khi bắt đầu mở mắt!");
        }
        else
        {
            Debug.LogWarning("[BedSleepCutscene] ⚠️ Không tìm thấy script ContinuousDoorKnocker trong Scene!");
        }
    }

    // =================================================================
    // FADE
    // =================================================================
    IEnumerator FadeToBlack(float duration)
    {
        if (fadeScreenImage == null) yield break;
        fadeScreenImage.gameObject.SetActive(true);
        Color c = new Color(0f, 0f, 0f, 0f);
        fadeScreenImage.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / duration);
            fadeScreenImage.color = c;
            yield return null;
        }
        c.a = 1f;
        fadeScreenImage.color = c;
    }

    IEnumerator FadeFromBlackWithClock(float duration)
    {
        float elapsed = 0f;
        Color fadeColor = (fadeScreenImage != null) ? fadeScreenImage.color : Color.black;
        Color clockColor = (clockTextUI != null) ? clockTextUI.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(1f - (elapsed / duration));

            if (fadeScreenImage != null)
            {
                fadeColor.a = t;
                fadeScreenImage.color = fadeColor;
            }

            if (clockTextUI != null)
            {
                clockColor.a = t;
                clockTextUI.color = clockColor;
            }

            yield return null;
        }

        if (fadeScreenImage != null)
        {
            fadeColor.a = 0f;
            fadeScreenImage.color = fadeColor;
            fadeScreenImage.gameObject.SetActive(false);
        }

        if (clockTextUI != null)
        {
            clockColor.a = 0f;
            clockTextUI.color = clockColor;
            clockTextUI.gameObject.SetActive(false);
        }
    }

    // =================================================================
    // SMOOTH ROTATE
    // =================================================================
    IEnumerator SmoothRotate(Transform t, Quaternion from, Quaternion to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            t.rotation = Quaternion.Slerp(from, to, s);
            yield return null;
        }
        t.rotation = to;
    }

    // =================================================================
    // TIẾNG GÕ CỬA
    // =================================================================
    void PlayDoorKnock(float vol)
    {
        if (doorKnockSound == null) return;
        if (doorAudioSource != null)
            doorAudioSource.PlayOneShot(doorKnockSound, vol);
        else
            AudioSource.PlayClipAtPoint(doorKnockSound, transform.position, vol);
    }

    // =================================================================
    // THOẠI VỚI HIỆU ỨNG TYPEWRITER & CLICK SKIP CHUẨN DỰ ÁN
    // =================================================================
    IEnumerator PlayDialogueLineRoutine(int index, float holdTime = 3.0f)
    {
        if (sleepDialogues == null || index < 0 || index >= sleepDialogues.Length) yield break;

        SleepDialogueLine line = sleepDialogues[index];
        string text = (SettingsManager.currentLanguage == "VI") ? line.vietnameseDialogue : line.englishDialogue;
        if (string.IsNullOrEmpty(text)) yield break;

        currentFullText = text;
        skipRequested = false;
        skipWaitRequested = false;

        if (line.dialogueAudio != null && localAudioSource != null)
            localAudioSource.PlayOneShot(line.dialogueAudio, audioVolume);

        if (subtitleTextUI != null)
        {
            if (!subtitleTextUI.gameObject.activeInHierarchy)
            {
                if (subtitleTextUI.transform.parent != null)
                    subtitleTextUI.transform.parent.gameObject.SetActive(true);
                subtitleTextUI.gameObject.SetActive(true);
            }

            if (useTypewriterEffect)
            {
                isTyping = true;
                subtitleTextUI.text = "";

                for (int i = 0; i <= currentFullText.Length; i++)
                {
                    if (!isTyping || skipRequested) break;
                    subtitleTextUI.text = currentFullText.Substring(0, i);
                    yield return new WaitForSeconds(typewriterSpeed);
                }

                subtitleTextUI.text = currentFullText;
                isTyping = false;
            }
            else
            {
                subtitleTextUI.text = currentFullText;
            }
        }

        // CHỜ ĐỌC XONG HOẶC BẤM CHUỘT QUA NHANH
        isWaitingForNextLine = true;
        float waitTimer = 0f;
        while (waitTimer < holdTime && !skipWaitRequested)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }
        isWaitingForNextLine = false;
    }

    void ClearSubtitle()
    {
        isTyping = false;
        isWaitingForNextLine = false;
        skipRequested = false;
        skipWaitRequested = false;
        if (subtitleTextUI != null) subtitleTextUI.text = "";
    }

    // =================================================================
    // MÈO
    // =================================================================
    IEnumerator TriggerCatRunSequence()
    {
        if (delayBeforeCatRuns > 0f) yield return new WaitForSeconds(delayBeforeCatRuns);
        if (catScript != null && catMeowTargetPoint != null)
            catScript.MoveToPointAndStop(catMeowTargetPoint, catRunSpeed, 0f);
    }

    // =================================================================
    // PROMPT
    // =================================================================
    public void ShowPrompt()
    {
        if (isSleeping) return;
        if (interactPrompt != null) interactPrompt.ShowPrompt();
    }

    public void HidePrompt()
    {
        if (interactPrompt != null) interactPrompt.HidePrompt();
    }
}
