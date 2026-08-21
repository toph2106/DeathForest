using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;
using TMPro;

public class Map03IntroSequence : MonoBehaviour
{
    public static Map03IntroSequence Instance { get; private set; }
    public static bool isCutsceneRunning { get; private set; } = false;

    [Header("1. Tham Chiếu Player & UI Phụ Đề (References)")]
    [Tooltip("Kéo GameObject Main (chứa MovePl) vào đây (Tự tìm nếu để trống)")]
    public MovePl playerMain;

    [Tooltip("Kéo TextMeshProUGUI (Subtitle Text) vào đây (Tự tìm nếu để trống)")]
    public TextMeshProUGUI subtitleTextUI;

    [Tooltip("Kéo FadePanel (Image đen) vào đây (Tự tìm nếu để trống)")]
    public Image fadeScreenImage;

    [Header("2. Mở Màn & Khoảng Lặng (Phase 1 - Fade & Silence)")]
    [Tooltip("Thời gian giữ đen ngắn lúc nạp map trước khi mở sáng (giây - Mặc định: 0.5s)")]
    public float initialBlackDuration = 0.5f;

    [Tooltip("Thời gian màn hình từ đen xì sáng dần lên rõ cảnh (giây - Mặc định: 1.5s)")]
    public float fadeInDuration = 1.5f;

    [Tooltip("⏱️ THỜI GIAN DELAY SAU KHI HẾT FADE MỚI BẮT ĐẦU PHÁT CHUÔNG ĐỢT 1 (giây - Tăng số này lên nếu muốn đứng lâu hơn, ví dụ: 3s - 4s)")]
    public float silenceDurationAfterFade = 3.0f;

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "Tiếng gì vậy...?";
        [TextArea(2, 4)]
        public string englishDialogue = "What was that sound...?";
        public float holdDuration = 2.0f;
    }

    [Header("3. Cấu Hình Phụ Đề Thoại Player (Phase 2 - Subtitle)")]
    public DialogueLine[] introDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Tiếng gì vậy...?",
            englishDialogue = "What was that sound...?",
            holdDuration = 2.0f
        }
    };

    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.035f;
    public bool showBlinkingCursor = true;
    public bool useFadeEffect = true;
    public float fadeTextDuration = 0.2f;

    [Tooltip("Âm thanh gõ chữ thoại (Loop trong lúc gõ - Để trống nếu không dùng)")]
    public AudioClip dialogueBlipSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    [Header("4. Cấu Hình Billy Đạp Xe (Phase 3 - Billy Riding)")]
    [Tooltip("Kéo GameObject billy_on_bike trong Scene vào đây")]
    public GameObject billyObject;

    [Tooltip("Kéo Transform vị trí & hướng xuất phát (Point Spawn) của Billy vào đây")]
    public Transform billySpawnPoint;

    [Tooltip("Kéo Transform vị trí đích đến của Billy (Cục NextScene / Hầm đá) vào đây")]
    public Transform destinationTarget;

    [Tooltip("Khoảng cách tiếp cận đích đến NextScene để Billy biến mất (mét - Mặc định: 3.5m)")]
    public float stopDistanceToTarget = 3.5f;

    [Tooltip("Animator của Billy (Tự lấy từ billyObject nếu để trống)")]
    public Animator billyAnimator;

    [Tooltip("Animation clip đạp xe của Billy (Kéo clip Armature|CINEMA_4D_Main vào đây)")]
    public AnimationClip bikeRideClip;

    [Tooltip("Tốc độ di chuyển của Billy về phía NextScene (m/s - Mặc định: 25 m/s)")]
    public float billyMoveSpeed = 25.0f;

    [Tooltip("Tự động bám sát mặt đất khi Billy chạy trên đồi/đường gồ ghề")]
    public bool snapBillyToGround = true;
    public LayerMask groundLayerMask = ~0;

    [Header("5. Âm Thanh Chuông & Tiếng Cười Của Billy")]
    [Tooltip("1. TIẾNG CHUÔNG XE ĐẠP: Kéo file Common-bicycle-bell... vào đây")]
    public AudioClip bikeBellSound;
    [Range(0f, 1f)] public float bikeBellVolume = 1.0f;

    [Tooltip("2. TIẾNG CƯỜI BILLY: Kéo file Billy-the-Puppet-Laughing.wav vào đây")]
    public AudioClip billyLaughSound;
    [Range(0f, 1f)] public float billyLaughVolume = 0.9f;

    [Header("6. Căn Chỉnh Thời Gian Các Nhịp Khi Đang Đạp Xe (Timeline Timers)")]
    [Tooltip("⏱️ Thời gian đệm sau khi dứt tiếng chuông rồi mới bắt đầu hiện thoại Player (giây - Mặc định: 0.15s)")]
    public float delayAfterBellBeforeDialogue = 0.15f;

    [Tooltip("⏱️ Thời gian sau khi thoại kết thúc đến Tiếng Cười của Billy (giây - Mặc định: 0.3s)")]
    public float delayBeforeLaugh = 0.3f;

    [Header("7. Âm Thanh Bao Quanh Player & Vang Vọng Rừng (Surround & Echo)")]
    [Range(0f, 1f)]
    [Tooltip("Độ hòa trộn không gian: 0 = 2D to rõ 100%, 0.25 = Vừa to rõ bao quanh tai vừa có hướng 3D, 1.0 = Hoàn toàn 3D")]
    public float soundSpatialBlend = 0.25f;

    [Tooltip("Khoảng cách tối thiểu âm lượng đạt 100% cực đại (mét - Mặc định: 25m)")]
    public float soundMinDistance = 25.0f;

    [Tooltip("Khoảng cách tối đa âm thanh lan tỏa khắp bản đồ (mét - Mặc định: 150m)")]
    public float soundMaxDistance = 150.0f;

    [Range(0f, 360f)]
    [Tooltip("Độ mở rộng âm thanh vòm Stereo (Spread: 180 độ bao trùm 2 bên tai nghe)")]
    public float soundStereoSpread = 180.0f;

    [Tooltip("Bật hiệu ứng tiếng vọng vang qua rừng cây cho tiếng cười của Billy")]
    public bool enableEchoReverbEffect = true;
    [Range(0.1f, 0.8f)] public float echoDelay = 0.25f;
    [Range(0.1f, 0.9f)] public float echoDecay = 0.45f;

    // --- Private Fields ---
    private AudioSource audioSource;
    private AudioSource billyAudioSource;
    private CharacterController characterController;
    private PlayableGraph playableGraph;

    // --- Biến điều khiển Click chuột qua thoại nhanh chuẩn Map 01 & 02 ---
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D Sound
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        if (audioSource.isPlaying) audioSource.Stop();

        // 1. TÌM VÀ BẬT ĐEN MÀN HÌNH NGAY LẬP TỨC TỪ AWAKE (KHÔNG ĐỂ LỘ HÌNH ẢNH BAN ĐẦU)
        EnsureFadeScreenImage();
        if (fadeScreenImage != null)
        {
            EnsureParentsActive(fadeScreenImage);
            fadeScreenImage.gameObject.SetActive(true);
            fadeScreenImage.color = Color.black;
            fadeScreenImage.raycastTarget = true;
        }

        // 2. KHÓA CHUỘT NGAY TỪ ĐẦU (GIỐNG MAP 02)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. ẨN NGAY TOÀN BỘ UI MÁY QUAY & TÂM NGẮM TỪ AWAKE (FRAME 0)
        HideAllCamUIAndEquipment();
    }

    void Start()
    {
        // 1. KHÓA CHUỘT
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 2. TÌM PLAYER MAIN VÀ CAMERA
        if (playerMain == null) playerMain = Object.FindFirstObjectByType<MovePl>();
        if (playerMain != null)
        {
            characterController = playerMain.GetComponent<CharacterController>();
            playerMain.LockCursor();
        }

        // 3. TÌM SUBTITLE VÀ FADE SCREEN
        if (subtitleTextUI == null) subtitleTextUI = FindSubtitleTextUI();
        if (fadeScreenImage == null) EnsureFadeScreenImage();

        // 4. TỰ ĐỘNG TÌM BILLY VÀ TẠM ẨN TRƯỚC KHI FADE XONG
        EnsureBillyObject();
        if (billyObject != null)
        {
            billyObject.SetActive(false);
        }

        // 5. ẨN TOÀN BỘ UI THIẾT BỊ & TÂM NGẮM
        HideAllCamUIAndEquipment();

        // 6. BẮT ĐẦU CHUỖI CUTSCENE MAP 03
        StartCoroutine(IntroSequenceRoutine());
    }

    void Update()
    {
        if (!isCutsceneRunning) return;

        // Bấm chuột trái (Mouse 0), Space hoặc E để qua thoại nhanh (Chuẩn Map 01 & Map 02)
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                // BẤM LẦN 1 KHI ĐANG GÕ -> HIỆN TOÀN BỘ CHỮ NGAY LẬP TỨC
                isTyping = false;
                if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;
                if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueBlipSound)
                {
                    audioSource.Stop();
                }
            }
            else if (isWaitingForNextLine)
            {
                // BẤM LẦN 2 KHI CHỮ ĐÃ ĐẦY ĐỦ -> QUA CÂU THOẠI TIẾP THEO
                skipRequested = true;
            }
        }
    }

    void HideAllCamUIAndEquipment()
    {
        // 1. Tạm tắt đèn pin trong lúc cắt cảnh mở màn (giữ nguyên quyền sở hữu và số pin)
        FlashlightToggle flashlight = Object.FindFirstObjectByType<FlashlightToggle>(FindObjectsInactive.Include);
        if (flashlight != null)
        {
            flashlight.hasFlashlight = true;
            flashlight.SetFlashlightState(false, false);
        }

        // 2. Tắt TOÀN BỘ GameObject Camcorder trong lúc màn hình đang đen
        GameObject camObj = GameObject.Find("Camcorder");
        if (camObj != null) camObj.SetActive(false);

        CamcorderUI[] camUIs = Object.FindObjectsByType<CamcorderUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in camUIs)
        {
            c.gameObject.SetActive(false);
        }

        // 3. Tắt tâm ngắm chấm tròn & bàn tay
        InteractPro interactPro = Object.FindFirstObjectByType<InteractPro>(FindObjectsInactive.Include);
        if (interactPro != null)
        {
            if (interactPro.dotObject != null) interactPro.dotObject.SetActive(false);
            if (interactPro.handObject != null) interactPro.handObject.SetActive(false);
            if (interactPro.interactionUI != null) interactPro.interactionUI.SetActive(false);
        }
    }

    IEnumerator IntroSequenceRoutine()
    {
        isCutsceneRunning = true;
        Debug.Log("[Map03IntroSequence] 🌲 BẮT ĐẦU CẮT CẢNH MỞ MÀN MAP 03!");

        // 1. KHÓA DI CHUYỂN & CAMERA PLAYER BAN ĐẦU
        if (playerMain != null)
        {
            playerMain.isCameraLocked = true;
            playerMain.SetMovementState(false);
            playerMain.enabled = false;
        }
        if (characterController != null) characterController.enabled = false;

        PauseMenuManager.SetInGameHUDActive(false);

        // 2. BẬT MÀN HÌNH ĐEN BAN ĐẦU
        EnsureFadeScreenImage();
        if (fadeScreenImage != null)
        {
            EnsureParentsActive(fadeScreenImage);
            fadeScreenImage.gameObject.SetActive(true);
            fadeScreenImage.color = Color.black;
            fadeScreenImage.raycastTarget = true;
        }

        yield return new WaitForSeconds(initialBlackDuration);

        // 3. FADE SÁNG DẦN VÀO GAME (1.0 -> 0.0)
        yield return StartCoroutine(FadeScreenInRoutine(fadeInDuration));

        // TRẢ LẠI UI INGAME & MỞ KHÓA CHO PLAYER
        PauseMenuManager.SetInGameHUDActive(true);

        if (playerMain != null)
        {
            playerMain.enabled = true;
            playerMain.isCameraLocked = false;
            playerMain.SetMovementState(true);
            playerMain.LockCursor();
        }
        if (characterController != null) characterController.enabled = true;

        InteractPro interactPro = Object.FindFirstObjectByType<InteractPro>();
        if (interactPro != null && interactPro.dotObject != null)
        {
            interactPro.dotObject.SetActive(true);
        }

        // KHÔI PHỤC ĐÈN PIN & MÁY QUAY CAMCORDER SAU KHI SÁNG
        CamcorderUI.MarkCameraPickedUp();
        GameObject camObj = GameObject.Find("Camcorder");
        if (camObj != null) camObj.SetActive(true);

        CamcorderUI[] camUIs = Object.FindObjectsByType<CamcorderUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in camUIs)
        {
            c.gameObject.SetActive(true);
        }

        FlashlightToggle ft = FlashlightToggle.Instance ?? Object.FindFirstObjectByType<FlashlightToggle>(FindObjectsInactive.Include);
        if (ft != null)
        {
            ft.hasFlashlight = true;
            if (ft.currentBattery > 0f)
            {
                ft.SetFlashlightState(true, false);
            }
        }

        // 4. KHOẢNG LẶNG ĐỂ NGƯỜI CHƠI ĐỨNG ĐỊNH THẦN & QUAN SÁT
        if (silenceDurationAfterFade > 0f)
        {
            yield return new WaitForSeconds(silenceDurationAfterFade);
        }

        // ====================================================================
        // 5. BẮT ĐẦU CHUÔNG VÀ BILLY ĐẠP XE DI CHUYỂN NGAY LẬP TỨC (KHÔNG ĐỨNG KHỰNG)
        // ====================================================================
        yield return StartCoroutine(BillyRideRoutine());

        isCutsceneRunning = false;
        Debug.Log("[Map03IntroSequence] 🎮 CẮT CẢNH MAP 03 HOÀN TẤT!");
    }

    IEnumerator FadeScreenInRoutine(float duration)
    {
        if (fadeScreenImage != null && duration > 0f)
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
            fadeScreenImage.raycastTarget = false;
            fadeScreenImage.gameObject.SetActive(false);
        }
    }

    IEnumerator PlaySingleDialogueLineRoutine(DialogueLine line)
    {
        if (line == null) yield break;

        if (subtitleTextUI == null) subtitleTextUI = FindSubtitleTextUI();
        if (subtitleTextUI == null) yield break;

        // BẬT TOÀN BỘ CÁC PARENT (CANVAS) CỦA SUBTITLE
        EnsureParentsActive(subtitleTextUI);
        subtitleTextUI.gameObject.SetActive(true);
        Color sc = subtitleTextUI.color;
        sc.a = 1f;
        subtitleTextUI.color = sc;

        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        string lang = SettingsManager.currentLanguage;
        currentFullText = (lang == "VI") ? line.vietnameseDialogue : line.englishDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.vietnameseDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.englishDialogue;
        if (string.IsNullOrEmpty(currentFullText)) yield break;

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        skipRequested = false;

        // BẬT ÂM THANH GÕ CHỮ TRONG LÚC ĐANG GÕ
        if (dialogueBlipSound != null && audioSource != null)
        {
            audioSource.clip = dialogueBlipSound;
            audioSource.volume = dialogueVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        // GÕ CHỮ TỪNG KÝ TỰ BẰNG SUBSTRING
        if (useTypewriterEffect)
        {
            isTyping = true;
            subtitleTextUI.text = "";

            for (int i = 0; i <= currentFullText.Length; i++)
            {
                if (!isTyping) break;
                string typed = currentFullText.Substring(0, i);
                if (showBlinkingCursor) typed += " _";
                subtitleTextUI.text = typed;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            subtitleTextUI.text = currentFullText;
            isTyping = false;
        }
        else
        {
            subtitleTextUI.text = currentFullText;
        }

        // DỪNG ÂM THANH GÕ CHỮ NGAY KHI GÕ XONG
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueBlipSound)
        {
            audioSource.Stop();
        }

        // BẬT CON TRỎ NHẤP NHÁY '_' TRONG LÚC CHỜ ĐỌC
        if (showBlinkingCursor && subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

        // CHỜ ĐỌC HOẶC CLICK QUA
        isWaitingForNextLine = true;
        float waitTimer = 0f;
        while (waitTimer < line.holdDuration && !skipRequested)
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

        // FADE CHỮ BIẾN MẤT
        if (useFadeEffect && subtitleTextUI != null)
        {
            yield return StartCoroutine(FadeTextOutRoutine(subtitleTextUI, fadeTextDuration));
        }

        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;
            subtitleTextUI.gameObject.SetActive(false);
        }

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
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

    IEnumerator BillyRideRoutine()
    {
        EnsureBillyObject();

        if (billyObject == null)
        {
            Debug.LogWarning("[Map03IntroSequence] ⚠️ Không tìm thấy GameObject 'billy_on_bike' trong Scene Map 03!");
            yield break;
        }

        if (destinationTarget == null)
        {
            GameObject ns = GameObject.Find("NextScene") ?? GameObject.Find("NextMap");
            if (ns != null) destinationTarget = ns.transform;
        }

        EnsureParentsActive(billyObject.transform);

        // Đặt Billy tại đúng vị trí & hướng xuất phát của Point Spawn
        if (billySpawnPoint != null)
        {
            billyObject.transform.position = billySpawnPoint.position;
            billyObject.transform.rotation = billySpawnPoint.rotation;
        }

        billyObject.SetActive(true);

        // 1. PHÁT ANIMATION ĐẠP XE CỦA BILLY
        if (billyAnimator != null)
        {
            billyAnimator.enabled = true;
            if (billyAnimator.runtimeAnimatorController == null && bikeRideClip != null)
            {
                try
                {
                    AnimationPlayableUtilities.PlayClip(billyAnimator, bikeRideClip, out playableGraph);
                    playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Map03IntroSequence] PlayClip note: {ex.Message}");
                }
            }
        }

        // 2. KÍCH HOẠT CHUÔNG + THOẠI + TIẾNG CƯỜI TRONG LÚC ĐẠP XE
        StartCoroutine(AudioAndDialogueWhileRidingRoutine());

        // 3. DI CHUYỂN BILLY NGAY LẬP TỨC VỀ PHÍA NEXTSCENE (HẦM ĐÁ) - KHÔNG ĐỨNG KHỰNG
        Vector3 startPosition = billyObject.transform.position;

        while (billyObject != null && billyObject.activeSelf)
        {
            Vector3 forwardDir;

            if (destinationTarget != null)
            {
                Vector3 toTarget = destinationTarget.position - billyObject.transform.position;
                toTarget.y = 0f;

                // Xoay mượt về hướng đích đến
                if (toTarget.sqrMagnitude > 0.05f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized);
                    billyObject.transform.rotation = Quaternion.Slerp(billyObject.transform.rotation, lookRot, 5f * Time.deltaTime);
                }

                forwardDir = billyObject.transform.forward;

                // KIỂM TRA ĐÃ CHẠM VÀO / ĐẾN GẦN CỤC NEXTSCENE CHƯA
                float dist = toTarget.magnitude;
                if (dist <= stopDistanceToTarget)
                {
                    Debug.Log($"[Map03IntroSequence] 🚴 Billy đã chạm vào đích NextScene (khoảng cách: {dist:F1}m)!");
                    break;
                }
            }
            else
            {
                forwardDir = billyObject.transform.forward;
                float d = Vector3.Distance(startPosition, billyObject.transform.position);
                if (d >= 150f) break;
            }

            // Di chuyển về phía trước NGAY LẬP TỨC
            billyObject.transform.position += forwardDir * billyMoveSpeed * Time.deltaTime;

            // Bám sát mặt đất
            if (snapBillyToGround)
            {
                Vector3 rayOrigin = billyObject.transform.position + Vector3.up * 1.5f;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 4.0f, groundLayerMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3 pos = billyObject.transform.position;
                    pos.y = hit.point.y;
                    billyObject.transform.position = pos;
                }
            }

            yield return null;
        }

        // KHI CHẠM HẦM ĐÁ NEXTSCENE: DỪNG ÂM THANH VÀ BIẾN MẤT
        if (billyAudioSource != null && billyAudioSource.isPlaying)
        {
            billyAudioSource.Stop();
        }

        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }

        billyObject.SetActive(false);
    }

    /// <summary>
    /// Vừa đạp xe vừa bóp chuông, chạy thoại và cười rùng rợn (Tuần tự: Chuông dứt -> Thoại -> Cười)
    /// </summary>
    IEnumerator AudioAndDialogueWhileRidingRoutine()
    {
        // 1. NGAY KHI BẮT ĐẦU DI CHUYỂN: PHÁT TIẾNG CHUÔNG XE ĐẠP
        PlayBikeBell();

        // 2. CHỜ TIẾNG CHUÔNG KÊU XONG HẲN RỒI MỚI CHẠY THOẠI
        if (bikeBellSound != null)
        {
            yield return new WaitForSeconds(bikeBellSound.length);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        if (delayAfterBellBeforeDialogue > 0f)
        {
            yield return new WaitForSeconds(delayAfterBellBeforeDialogue);
        }

        // 3. CHẠY PHỤ ĐỀ THOẠI PLAYER ("Tiếng gì vậy...?")
        if (introDialogues != null && introDialogues.Length > 0)
        {
            foreach (var line in introDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleDialogueLineRoutine(line));
                }
            }
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        // 4. TIẾNG CƯỜI CỦA BILLY TRONG LÚC ĐẠP XE
        if (delayBeforeLaugh > 0f) yield return new WaitForSeconds(delayBeforeLaugh);
        PlayBillyLaugh();
    }

    private void PlayBikeBell()
    {
        if (bikeBellSound == null) return;
        if (billyAudioSource != null && billyAudioSource.gameObject.activeInHierarchy)
        {
            billyAudioSource.PlayOneShot(bikeBellSound, bikeBellVolume);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(bikeBellSound, bikeBellVolume);
        }
        Debug.Log("[Map03IntroSequence] 🔔 Phát tiếng chuông xe đạp!");
    }

    private void PlayBillyLaugh()
    {
        if (billyLaughSound == null) return;
        SetupBillyEchoFilter();
        if (billyAudioSource != null && billyAudioSource.gameObject.activeInHierarchy)
        {
            billyAudioSource.PlayOneShot(billyLaughSound, billyLaughVolume);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(billyLaughSound, billyLaughVolume);
        }
        Debug.Log("[Map03IntroSequence] 😈 Billy phát tiếng cười vang rừng!");
    }

    private void SetupBillyEchoFilter()
    {
        if (billyObject == null) return;

        if (enableEchoReverbEffect)
        {
            AudioEchoFilter echo = billyObject.GetComponent<AudioEchoFilter>();
            if (echo == null) echo = billyObject.AddComponent<AudioEchoFilter>();
            echo.enabled = true;
            echo.delay = Mathf.Clamp(echoDelay * 1000f, 50f, 1000f); // ms
            echo.decayRatio = Mathf.Clamp01(echoDecay);
            echo.wetMix = 0.45f;
            echo.dryMix = 0.85f;

            AudioReverbFilter reverb = billyObject.GetComponent<AudioReverbFilter>();
            if (reverb == null) reverb = billyObject.AddComponent<AudioReverbFilter>();
            reverb.enabled = true;
            reverb.reverbPreset = AudioReverbPreset.Forest;
        }
    }

    private void EnsureBillyObject()
    {
        if (billyObject == null)
        {
            GameObject bObj = GameObject.Find("billy_on_bike");
            if (bObj != null)
            {
                billyObject = bObj;
            }
            else
            {
                Transform[] allT = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var t in allT)
                {
                    if (t != null && t.gameObject.name.ToLower().Contains("billy"))
                    {
                        billyObject = t.gameObject;
                        break;
                    }
                }
            }
        }

        if (billyObject != null)
        {
            if (billyAnimator == null)
            {
                billyAnimator = billyObject.GetComponentInChildren<Animator>();
            }

            if (billyAudioSource == null)
            {
                billyAudioSource = billyObject.GetComponent<AudioSource>();
                if (billyAudioSource == null) billyAudioSource = billyObject.AddComponent<AudioSource>();
            }

            if (billyAudioSource != null)
            {
                billyAudioSource.spatialBlend = Mathf.Clamp01(soundSpatialBlend);
                billyAudioSource.minDistance = Mathf.Max(5f, soundMinDistance);
                billyAudioSource.maxDistance = Mathf.Max(50f, soundMaxDistance);
                billyAudioSource.spread = Mathf.Clamp(soundStereoSpread, 0f, 360f);
                billyAudioSource.rolloffMode = AudioRolloffMode.Linear;
                billyAudioSource.dopplerLevel = 0.3f;
                billyAudioSource.playOnAwake = false;
                billyAudioSource.loop = false;
            }
        }
    }

    private void EnsureParentsActive(Component comp)
    {
        if (comp == null) return;
        EnsureParentsActive(comp.transform);
    }

    private void EnsureParentsActive(Transform tr)
    {
        if (tr == null) return;
        Transform curr = tr;
        while (curr != null)
        {
            curr.gameObject.SetActive(true);
            curr = curr.parent;
        }
    }

    private void EnsureFadeScreenImage()
    {
        if (fadeScreenImage != null)
        {
            EnsureParentsActive(fadeScreenImage);
            return;
        }

        Image[] allImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allImages)
        {
            if (item != null && (item.gameObject.name.ToLower().Contains("fadepanel") || item.gameObject.name.ToLower().Contains("fade")))
            {
                fadeScreenImage = item;
                EnsureParentsActive(fadeScreenImage);
                return;
            }
        }

        // Tạo Fade Canvas độc lập nếu scene chưa có
        GameObject canvasObj = new GameObject("IntroFadeCanvas");
        Canvas c = canvasObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.overrideSorting = true;
        c.sortingOrder = 32767;
        canvasObj.AddComponent<CanvasScaler>();

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        fadeScreenImage = imgObj.AddComponent<Image>();
        fadeScreenImage.color = Color.black;
        fadeScreenImage.rectTransform.anchorMin = Vector2.zero;
        fadeScreenImage.rectTransform.anchorMax = Vector2.one;
        fadeScreenImage.rectTransform.sizeDelta = Vector2.zero;
    }

    private TextMeshProUGUI FindSubtitleTextUI()
    {
        if (subtitleTextUI != null) return subtitleTextUI;

        SmartInteractionDialogue sid = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
        if (sid != null && sid.subtitleTextUI != null) return sid.subtitleTextUI;

        TextMeshProUGUI[] tmps = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in tmps)
        {
            if (tmp != null && (tmp.gameObject.name.ToLower().Contains("subtitle") || tmp.gameObject.name.ToLower().Contains("sub")))
            {
                return tmp;
            }
        }

        return null;
    }

    void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }
}
