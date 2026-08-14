using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class CockroachFlyAttack : MonoBehaviour
{
    [Header("1. Mục Tiêu Nhắm Tới (Player Target)")]
    [Tooltip("Kéo Player Transform hoặc Main Camera vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM Main Camera!")]
    public Transform playerTarget;

    [Header("2. Tên Các Animation Trong Animator")]
    [Tooltip("Tên state đứng im")]
    public string idleStateName = "giant_cockroach_armature|idle";

    [Tooltip("Tên state chuẩn bị & bám mặt (dạng chân ra)")]
    public string straightStateName = "giant_cockroach_armature|flying_straight";

    [Tooltip("Tên state bay phi vào người chơi")]
    public string kidnappingFlyStateName = "giant_cockroach_armature|flying_kidnapping";

    [Header("3. Cấu Hình Hướng Đầu & Góc Xoay Khi Bay")]
    [Tooltip("Bù góc xoay của mô hình 3D lúc bay. Mặc định bù 180 độ trục Y để hướng đúng đầu con gián vào người chơi")]
    public Vector3 modelRotationOffset = new Vector3(0f, 180f, 0f);

    [Tooltip("Tốc độ xoay đầu hướng theo đường bay uốn lượn")]
    public float rotationSpeed = 16.0f;

    [Header("4. Quỹ Đạo Bay Chao Đảo / Lượn Bất Định Như Gián Thật")]
    [Tooltip("Bật quỹ đạo bay chao đảo, ngoằn ngoèo, trồi sụt bất định như gián ngoài đời")]
    public bool enableErraticFlight = true;

    [Tooltip("Biên độ chao đảo lượn sang 2 bên trái/phải (mét - Mặc định: 0.75m)")]
    public float zigzagAmplitude = 0.75f;

    [Tooltip("Tần số lượn qua lại trái/phải (Mặc định: 8.0)")]
    public float zigzagFrequency = 8.0f;

    [Tooltip("Biên độ trồi sụt độ cao lên/xuống (mét - Mặc định: 0.5m)")]
    public float wobbleAmplitude = 0.5f;

    [Tooltip("Tần số trồi sụt độ cao (Mặc định: 10.0)")]
    public float wobbleFrequency = 10.0f;

    [Tooltip("Độ nghiêng thân (Roll/Banking) khi gián lượn chao cánh (độ - Mặc định: 28 độ)")]
    public float bankingTiltAmount = 28.0f;

    [Tooltip("Độ hỗn loạn bất định ngẫu nhiên (Noise)")]
    public float randomNoiseAmount = 0.25f;

    [Header("5. Cấu Hình Bám Dính Trước Camera Người Chơi")]
    [Tooltip("Tích chọn để tự động bám dính vào trước mặt Camera người chơi khi chạm tới")]
    public bool attachToCameraOnHit = true;

    [Tooltip("Khoảng cách trước mắt camera (mét - Mặc định: 0.38m)")]
    public float attachDistanceInFront = 0.38f;

    [Tooltip("Độ lệch trục ngang X so với tâm camera")]
    public float attachOffsetX = 0f;

    [Tooltip("Độ lệch trục dọc Y so với tâm camera (Chỉnh hơi thấp xuống chút để thấy rõ đầu/thân gián)")]
    public float attachOffsetY = -0.12f;

    [Tooltip("Góc xoay khi bám vào mặt kính camera (Xoay trục X ngửa lên để áp trọn chân/bụng vào màn hình)")]
    public Vector3 attachRotationOffset = new Vector3(75f, 180f, 0f);

    [Header("6. Hiệu Ứng Hoảng Loạn Camera Khi Va Chạm (Panic Camera Spasms)")]
    [Tooltip("Tích chọn để kích hoạt hiệu ứng camera giật nảy và lắc lư quơ quạng hoảng loạn khi gián bám mặt")]
    public bool enablePanicCameraShake = true;

    [Tooltip("Thời gian bo góc & giật nảy hoảng loạn trước khi phủ đen (giây - Mặc định: 0.85s)")]
    public float panicDuration = 0.85f;

    [Tooltip("Cường độ rung nảy vị trí Camera (Mặc định: 0.08)")]
    public float shakeMagnitude = 0.08f;

    [Tooltip("Cường độ lắc giật góc nhìn (Pitch, Yaw, Roll) như quơ quạng hất gián (độ - Mặc định: 4.5 độ)")]
    public float panicRotationSpasm = 4.5f;

    [Header("7. Cử Động Nhúc Nhích/Giãy Giụa Của Gián Trên Kính (Micro Wiggle)")]
    [Tooltip("Tích chọn để con gián rung râu & nhúc nhích chân sống động khi đang bám trên camera")]
    public bool enableMicroWiggle = true;

    [Tooltip("Tần số nhúc nhích/giãy giụa (Mặc định: 15)")]
    public float wiggleFrequency = 15f;

    [Tooltip("Biên độ rung lắc vị trí (Mặc định: 0.012m)")]
    public float wigglePositionAmount = 0.012f;

    [Tooltip("Biên độ lắc góc xoay (Mặc định: 3.5 độ)")]
    public float wiggleRotationAmount = 3.5f;

    [Header("8. Hiệu Ứng Hậu Kỳ Bo Góc Tối (Post-Processing Volume)")]
    [Tooltip("Tự động điều chỉnh Volume (Vignette viền tối, Chromatic méo màu) lúc hoảng loạn")]
    public bool enableVolumeDistortion = true;

    [Tooltip("Kéo Global Volume vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM Volume trong Scene!")]
    public Volume postProcessVolume;

    [Tooltip("Độ tối viền đen vừa phải (Vignette: 0.0 -> 0.42, giữ góc nhìn rộng không bị bóp bé xíu)")]
    public float maxVignetteIntensity = 0.42f;

    [Tooltip("Độ nhòe tách sắc màu lúc sốc/choáng (Chromatic: 0.0 -> 0.65)")]
    public float maxChromaticIntensity = 0.65f;

    [Header("9. Fade Màn Hình Đen (Mất Ý Thức Chuyển Cảnh)")]
    [Tooltip("Bật màn hình đen ngay sau nhịp bo góc hoảng loạn để chuyển sang trạng thái bất tỉnh")]
    public bool enableFadeToBlack = true;

    [Tooltip("Thời gian phủ đen màn hình nhanh gọn, mượt mà (giây - Mặc định: 1.0s)")]
    public float fadeToBlackDuration = 1.0f;

    [Tooltip("Kéo FadeScreen Image vào đây. Nếu để trống sẽ tự động tìm hoặc liên kết chung")]
    public Image fadeScreenImage;

    [Header("10. Cấu Hình Thời Gian & Tốc Độ Di Chuyển Cất Cánh")]
    [Tooltip("Thời gian đứng im thở (Idle) trước khi chuyển qua chuẩn bị (giây)")]
    public float idleDuration = 1.5f;

    [Tooltip("Thời gian đứng thẳng chuẩn bị (Straight) trước khi cất cánh (giây)")]
    public float straightDuration = 1.5f;

    [Tooltip("Độ cao gián bay nhấc lên khỏi mặt đất trước khi lao tới (mét)")]
    public float liftHeight = 0.8f;

    [Tooltip("Thời gian nhấc bổng lên cao trước khi phi thẳng (giây)")]
    public float liftDuration = 0.4f;

    [Tooltip("Tốc độ bay phi thẳng vào người chơi (m/s)")]
    public float flySpeed = 9.5f;

    [Tooltip("Khoảng cách chạm vào người chơi (mét)")]
    public float arriveDistance = 0.8f;

    [Header("11. Cấu Hình Âm Thanh (Tùy Chọn)")]
    [Tooltip("Tiếng xòe/đập cánh chuẩn bị lúc Straight")]
    public AudioClip prepareSound;

    [Tooltip("Tiếng đập cánh vè vè dồn dập lúc bay Kidnapping")]
    public AudioClip flyingSound;

    [Tooltip("Tiếng Jumpscare / va chạm lúc chạm vào màn hình")]
    public AudioClip hitJumpscareSound;

    [Tooltip("Tiếng tim đập dồn dập / nghẹt thở lúc bị gián bám mặt (Tùy chọn)")]
    public AudioClip panicHeartbeatSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.9f;

    [Header("12. Tùy Chọn: Kích Hoạt Bởi Sự Kiện Cửa Mở 10s")]
    [Tooltip("Tích chọn để tự động kích hoạt gián khi cửa chính tự mở ra ở mốc 10s máy quay")]
    public bool triggerOnDoorOpen10s = false;

    [Tooltip("Độ trễ sau khi cửa mở rồi gián mới bắt đầu hành động (giây)")]
    public float delayAfterDoorOpen = 0.5f;

    [Header("13. Phím Tắt Kích Hoạt Test (Debug Shortcut Key)")]
    [Tooltip("Bấm phím này trong lúc Play để kích hoạt ngay chuỗi gián bay (Mặc định: Phím K)")]
    public KeyCode debugTriggerKey = KeyCode.K;

    [Tooltip("Bấm phím này để reset gián về vị trí ban đầu để test lại (Mặc định: Phím R)")]
    public KeyCode debugResetKey = KeyCode.R;

    public static event System.Action OnCockroachNightmareEnded;

    private Animator animator;
    private AudioSource audioSource;
    private bool isSequenceRunning = false;
    private bool hasHitPlayer = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Transform initialParent;
    private Coroutine currentSequenceCoroutine;
    private Coroutine panicCoroutine;

    // Biến lưu giá trị banking đã được làm mượt qua Lerp để tránh giật cục
    private float smoothedBankRoll = 0f;

    // Offset Perlin Noise riêng biệt cho từng instance
    private float perlinSeedX;
    private float perlinSeedY;

    // Post processing variables
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private float initialVignetteIntensity = 0f;
    private float initialChromaticIntensity = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // Âm thanh 3D chân thực
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 20f;
        audioSource.playOnAwake = false;

        initialParent = transform.parent;
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        int id = gameObject.GetInstanceID();
        perlinSeedX = id * 137.5f;
        perlinSeedY = id * 281.3f;
    }

    void Start()
    {
        EnsurePlayerTarget();
        SetupVolumeEffects();
        EnsureNightmareWakeUpExists();

        // Mặc định cho gián đứng im ở idle
        PlayAnimState(idleStateName);
    }

    void EnsureNightmareWakeUpExists()
    {
        CockroachNightmareWakeUp wakeUp = Object.FindFirstObjectByType<CockroachNightmareWakeUp>(FindObjectsInactive.Include);
        if (wakeUp == null)
        {
            GameObject wakeObj = new GameObject("CockroachNightmareWakeUp");
            wakeUp = wakeObj.AddComponent<CockroachNightmareWakeUp>();
        }
    }

    void SetupVolumeEffects()
    {
        if (!enableVolumeDistortion) return;

        if (postProcessVolume == null)
        {
            postProcessVolume = Object.FindFirstObjectByType<Volume>();
        }

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            if (postProcessVolume.profile.TryGet(out vignette))
            {
                initialVignetteIntensity = vignette.intensity.value;
                vignette.intensity.overrideState = true;
            }

            if (postProcessVolume.profile.TryGet(out chromaticAberration))
            {
                initialChromaticIntensity = chromaticAberration.intensity.value;
                chromaticAberration.intensity.overrideState = true;
            }
        }
    }

    void Update()
    {
        // PHÍM TẮT KÍCH HOẠT NHANH (MẶC ĐỊNH: BẤM PHÍM K)
        if (debugTriggerKey != KeyCode.None && Input.GetKeyDown(debugTriggerKey))
        {
            Debug.Log($"[CockroachFlyAttack] ⌨️ Bấm phím [{debugTriggerKey}]! Kích hoạt ngay chuỗi gián bay...");
            StartCockroachSequence();
        }

        // PHÍM TẮT RESET VỀ VỊ TRÍ CŨ ĐỂ TEST LẠI (MẶC ĐỊNH: BẤM PHÍM R)
        if (debugResetKey != KeyCode.None && Input.GetKeyDown(debugResetKey))
        {
            ResetCockroach();
        }

        // HIỆU ỨNG NHÚC NHÍCH / GIÃY GIỤA KHI ĐANG BÁM TRÊN CAMERA
        if (hasHitPlayer && attachToCameraOnHit && enableMicroWiggle && transform.parent != null)
        {
            ApplyMicroWiggleOnLens();
        }
    }

    void ApplyMicroWiggleOnLens()
    {
        float time = Time.time * wiggleFrequency;
        
        // Dao động nhẹ tọa độ X và Y
        float offsetX = Mathf.Sin(time) * wigglePositionAmount;
        float offsetY = Mathf.Cos(time * 1.3f) * (wigglePositionAmount * 0.7f);
        
        // Dao động nhẹ góc lắc Z và X
        float rotZ = Mathf.Sin(time * 0.8f) * wiggleRotationAmount;
        float rotX = Mathf.Cos(time * 1.1f) * (wiggleRotationAmount * 0.5f);

        transform.localPosition = new Vector3(attachOffsetX + offsetX, attachOffsetY + offsetY, attachDistanceInFront);
        transform.localRotation = Quaternion.Euler(attachRotationOffset.x + rotX, attachRotationOffset.y, attachRotationOffset.z + rotZ);
    }

    void OnEnable()
    {
        if (triggerOnDoorOpen10s)
        {
            CamcorderUI.OnTimerReached10s += HandleDoorOpen10sEvent;
        }
    }

    void OnDisable()
    {
        if (triggerOnDoorOpen10s)
        {
            CamcorderUI.OnTimerReached10s -= HandleDoorOpen10sEvent;
        }
    }

    void HandleDoorOpen10sEvent()
    {
        StartCoroutine(DelayedStartRoutine(delayAfterDoorOpen));
    }

    IEnumerator DelayedStartRoutine(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        StartCockroachSequence();
    }

    void EnsurePlayerTarget()
    {
        if (playerTarget != null) return;

        if (Camera.main != null)
        {
            playerTarget = Camera.main.transform;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    /// <summary>
    /// Kích hoạt chuỗi hành vi hoàn chỉnh: Idle -> Straight -> Flying Kidnapping -> Bám vào trước mặt Player
    /// </summary>
    public void StartCockroachSequence()
    {
        if (isSequenceRunning) return;
        if (currentSequenceCoroutine != null) StopCoroutine(currentSequenceCoroutine);
        currentSequenceCoroutine = StartCoroutine(CockroachBehaviorRoutine());
    }

    IEnumerator CockroachBehaviorRoutine()
    {
        isSequenceRunning = true;
        hasHitPlayer = false;
        EnsurePlayerTarget();

        // ========================================================
        // BƯỚC 1: ANIMATION IDLE (ĐỨNG IM THỞ/RUNG RÂU)
        // ========================================================
        PlayAnimState(idleStateName);
        Debug.Log("[Cockroach] 🪳 Giai đoạn 1: Idle (Đứng im tại chỗ)...");

        if (idleDuration > 0f)
        {
            yield return new WaitForSeconds(idleDuration);
        }

        // ========================================================
        // BƯỚC 2: ANIMATION STRAIGHT (CHUẨN BỊ BAY, VẪN ĐỨNG IM TẠI CHỖ)
        // ========================================================
        PlayAnimState(straightStateName);
        Debug.Log("[Cockroach] 🪳 Giai đoạn 2: Straight (Chuẩn bị cất cánh, đứng im tại chỗ)...");

        if (prepareSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(prepareSound, soundVolume);
        }

        // Xoay đầu đúng hướng về phía người chơi trong lúc đứng thẳng
        float straightTimer = 0f;
        while (straightTimer < straightDuration)
        {
            straightTimer += Time.deltaTime;
            LookAtTargetSmooth();
            yield return null;
        }

        // ========================================================
        // BƯỚC 3: ANIMATION KIDNAPPING (BAY LƯỢN CHAO ĐẢO BẤT ĐỊNH NHƯ GIÁN THẬT)
        // ========================================================
        PlayAnimState(kidnappingFlyStateName);
        Debug.Log("[Cockroach] 🪳 Giai đoạn 3: Flying Kidnapping (Bay lượn chao đảo ngoằn ngoèo nhắm thẳng Player)!");

        if (flyingSound != null && audioSource != null)
        {
            audioSource.clip = flyingSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // 3a. Nhấc bổng lên cao nhẹ 1 xíu
        Vector3 startPos = transform.position;
        Vector3 liftedPos = startPos + Vector3.up * liftHeight;
        float liftTimer = 0f;
        while (liftTimer < liftDuration)
        {
            liftTimer += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, liftedPos, liftTimer / liftDuration);
            LookAtTargetSmooth();
            yield return null;
        }

        // 3b. Bay lượn chao đảo ngoằn ngoèo với gia tốc và góc nghiêng cánh
        float maxFlyTime = 6.0f;
        float flyElapsed = 0f;
        Vector3 previousPos = transform.position;
        smoothedBankRoll = 0f;

        while (flyElapsed < maxFlyTime)
        {
            flyElapsed += Time.deltaTime;
            EnsurePlayerTarget();

            if (playerTarget == null) break;

            Vector3 targetHeadPos = playerTarget.position;
            float distanceToPlayer = Vector3.Distance(transform.position, targetHeadPos);

            // Kiểm tra nếu đã chạm vào người chơi
            if (distanceToPlayer <= arriveDistance)
            {
                OnHitPlayer();
                break;
            }

            // Hướng cơ bản thẳng về phía Player
            Vector3 baseDir = (targetHeadPos - transform.position).normalized;
            Vector3 flyRight = Vector3.Cross(Vector3.up, baseDir).normalized;
            Vector3 flyUp = Vector3.Cross(baseDir, flyRight).normalized;

            // Tính toán độ lượn sóng ngang và trồi sụt (Giảm dần độ lượn khi áp sát để bổ nhào chính xác vào mặt)
            float approachFactor = Mathf.Clamp01(distanceToPlayer / 2.0f);

            // Additive harmonics sóng sin tạo quỹ đạo bay hữu cơ
            float waveX = (Mathf.Sin(flyElapsed * zigzagFrequency) * 0.55f
                         + Mathf.Sin(flyElapsed * zigzagFrequency * 1.73f + 0.7f) * 0.30f
                         + Mathf.Sin(flyElapsed * zigzagFrequency * 2.51f + 2.1f) * 0.15f)
                         * zigzagAmplitude * approachFactor;

            float waveY = (Mathf.Sin(flyElapsed * wobbleFrequency + 1.2f) * 0.55f
                         + Mathf.Sin(flyElapsed * wobbleFrequency * 1.62f + 3.4f) * 0.30f
                         + Mathf.Sin(flyElapsed * wobbleFrequency * 2.37f + 5.0f) * 0.15f)
                         * wobbleAmplitude * approachFactor;

            // Perlin noise với instance seed riêng biệt
            float noiseX = (Mathf.PerlinNoise(Time.time * 5f + perlinSeedX, perlinSeedY) - 0.5f) * 2f * randomNoiseAmount * approachFactor;
            float noiseY = (Mathf.PerlinNoise(perlinSeedX, Time.time * 5f + perlinSeedY) - 0.5f) * 2f * randomNoiseAmount * approachFactor;

            Vector3 lateralOffset = (flyRight * (waveX + noiseX)) + (flyUp * (waveY + noiseY));

            // Vận tốc di chuyển thực tế (lateral trôi dạt 0.35)
            Vector3 targetVelocity = (baseDir * flySpeed) + (lateralOffset * (flySpeed * 0.35f));
            transform.position += targetVelocity * Time.deltaTime;

            // Tính góc xoay đầu hướng theo đường bay thực tế + nghiêng cánh khi lượn (Banking Tilt)
            Vector3 actualFlyDir = (transform.position - previousPos).normalized;
            if (actualFlyDir != Vector3.zero)
            {
                float targetBankRoll = -waveX * bankingTiltAmount;
                smoothedBankRoll = Mathf.Lerp(smoothedBankRoll, targetBankRoll, Time.deltaTime * 6f);

                Quaternion lookRot = Quaternion.LookRotation(actualFlyDir, Vector3.up);
                Quaternion offsetRot = Quaternion.Euler(modelRotationOffset.x, modelRotationOffset.y, modelRotationOffset.z + smoothedBankRoll);

                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot * offsetRot, Time.deltaTime * rotationSpeed * 2.5f);
            }

            previousPos = transform.position;
            yield return null;
        }

        isSequenceRunning = false;
    }

    void OnHitPlayer()
    {
        if (hasHitPlayer) return;
        hasHitPlayer = true;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (hitJumpscareSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitJumpscareSound, soundVolume);
        }

        // 1. CHUYỂN VỀ ANIMATION STRAIGHT (DẠNG CHÂN RA BÁM MẶT)
        PlayAnimState(straightStateName);

        // 2. GẮN DÍNH TRỰC TIẾP VÀO TRƯỚC MẶT CAMERA NGƯỜI CHƠI
        if (attachToCameraOnHit && playerTarget != null)
        {
            transform.SetParent(playerTarget);
            transform.localPosition = new Vector3(attachOffsetX, attachOffsetY, attachDistanceInFront);
            transform.localRotation = Quaternion.Euler(attachRotationOffset);
        }

        // 3. KÍCH HOẠT CHUỖI HIỆU ỨNG HOẢNG LOẠN CAMERA + POST PROCESSING + FADE ĐEN MẤT Ý THỨC
        if (panicCoroutine != null) StopCoroutine(panicCoroutine);
        panicCoroutine = StartCoroutine(PanicAndFadeRoutine());

        Debug.Log("[Cockroach] 💥 Đã phi trúng, BẮT ĐẦU CHUỖI HOẢNG LOẠN & FADE ĐEN BẤT TỈNH!");
    }

    IEnumerator PanicAndFadeRoutine()
    {
        Camera cam = Camera.main;
        Transform camTrans = (cam != null) ? cam.transform : null;
        Vector3 originalCamPos = (camTrans != null) ? camTrans.localPosition : Vector3.zero;
        Quaternion originalCamRot = (camTrans != null) ? camTrans.localRotation : Quaternion.identity;

        // Khóa di chuyển người chơi & góc nhìn chuột để script điều khiển hiệu ứng giãy giụa
        MovePl playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (playerMovePl != null)
        {
            playerMovePl.SetMovementState(false);
            playerMovePl.isCameraLocked = true;
        }

        // Phát âm thanh tim đập dồn dập / nghẹt thở hoảng loạn
        if (panicHeartbeatSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(panicHeartbeatSound, soundVolume);
        }

        // ========================================================
        // GIAI ĐOẠN 1: HOẢNG LOẠN BO GÓC CHỚP NHOÁNG
        // ========================================================
        float elapsed = 0f;

        while (elapsed < panicDuration)
        {
            elapsed += Time.deltaTime;
            float t = (panicDuration > 0f) ? Mathf.Clamp01(elapsed / panicDuration) : 1f;

            // Rung lắc camera hoảng loạn (cả vị trí và góc nhìn Pitch/Yaw/Roll như người giãy giụa)
            if (camTrans != null && enablePanicCameraShake)
            {
                float posStrength = Mathf.Lerp(shakeMagnitude, shakeMagnitude * 0.5f, t);
                float rotStrength = Mathf.Lerp(panicRotationSpasm, panicRotationSpasm * 0.4f, t);

                // Độ rung giật vị trí
                float posX = (Mathf.PerlinNoise(Time.time * 26f, 0f) - 0.5f) * 2f * posStrength;
                float posY = (Mathf.PerlinNoise(0f, Time.time * 26f) - 0.5f) * 2f * posStrength;

                // Lắc giật góc nhìn giãy giụa quơ tay
                float rotX = (Mathf.PerlinNoise(Time.time * 20f, 15f) - 0.5f) * 2f * rotStrength;
                float rotY = (Mathf.PerlinNoise(15f, Time.time * 20f) - 0.5f) * 2f * rotStrength;
                float rotZ = (Mathf.PerlinNoise(Time.time * 18f, 35f) - 0.5f) * 2f * rotStrength * 1.6f;

                camTrans.localPosition = originalCamPos + new Vector3(posX, posY, 0f);
                camTrans.localRotation = originalCamRot * Quaternion.Euler(rotX, rotY, rotZ);
            }

            // Tăng nhẹ hiệu ứng Volume theo thông số Inspector
            if (enableVolumeDistortion)
            {
                if (vignette != null)
                {
                    vignette.intensity.value = Mathf.Lerp(initialVignetteIntensity, maxVignetteIntensity, t);
                }
                if (chromaticAberration != null)
                {
                    chromaticAberration.intensity.value = Mathf.Lerp(initialChromaticIntensity, maxChromaticIntensity, t);
                }
            }

            yield return null;
        }

        // ========================================================
        // GIAI ĐOẠN 2: FADE MÀN HÌNH ĐEN (MẤT Ý THỨC)
        // ========================================================
        if (enableFadeToBlack)
        {
            EnsureFadeImageExists();

            if (fadeScreenImage != null)
            {
                fadeScreenImage.gameObject.SetActive(true);
                Color color = fadeScreenImage.color;
                color.a = 0f;
                fadeScreenImage.color = color;

                float fadeElapsed = 0f;
                while (fadeElapsed < fadeToBlackDuration)
                {
                    fadeElapsed += Time.deltaTime;
                    float fadeT = (fadeToBlackDuration > 0f) ? Mathf.Clamp01(fadeElapsed / fadeToBlackDuration) : 1f;

                    color.a = fadeT;
                    fadeScreenImage.color = color;

                    // Giảm dần âm lượng âm thanh gián
                    if (audioSource != null && audioSource.isPlaying)
                    {
                        audioSource.volume = Mathf.Lerp(soundVolume, 0f, fadeT);
                    }

                    yield return null;
                }

                color.a = 1f;
                fadeScreenImage.color = color;
            }
        }

        // ========================================================
        // GIAI ĐOẠN 3: KẾT THÚC CƠN ÁC MỘNG GIÁN TRONG BÓNG TỐI
        // ========================================================
        // Khôi phục camera về gốc
        if (camTrans != null)
        {
            camTrans.localPosition = originalCamPos;
            camTrans.localRotation = originalCamRot;
        }

        // Reset Volume profile về ban đầu
        if (vignette != null) vignette.intensity.value = initialVignetteIntensity;
        if (chromaticAberration != null) chromaticAberration.intensity.value = initialChromaticIntensity;

        // KÍCH HOẠT NGAY CHUỖI TỈNH DẬY TRÊN NỆM
        CockroachNightmareWakeUp wakeUp = Object.FindFirstObjectByType<CockroachNightmareWakeUp>(FindObjectsInactive.Include);
        if (wakeUp != null)
        {
            if (fadeScreenImage != null) wakeUp.fadeScreenImage = fadeScreenImage;
            wakeUp.StartWakeUpSequence();
        }

        OnCockroachNightmareEnded?.Invoke();

        // Ẩn con gián mẹ và trả lại parent ban đầu
        transform.SetParent(initialParent);
        gameObject.SetActive(false);

        Debug.Log("[CockroachFlyAttack] 🌑 Màn hình đã đen hoàn toàn, đã kích hoạt chuỗi bừng tỉnh dậy trên nệm!");
    }

    void EnsureFadeImageExists()
    {
        if (fadeScreenImage != null) return;

        // Tìm Image FadeScreen có sẵn trong Scene
        GameObject fadeObj = GameObject.Find("FadeScreen");
        if (fadeObj == null) fadeObj = GameObject.Find("FadeImage");
        if (fadeObj == null) fadeObj = GameObject.Find("BlackScreen");
        if (fadeObj != null)
        {
            fadeScreenImage = fadeObj.GetComponent<Image>();
            if (fadeScreenImage != null) return;
        }

        // Tìm từ các script khác trong scene
        BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>();
        if (bed != null && bed.fadeScreenImage != null)
        {
            fadeScreenImage = bed.fadeScreenImage;
            return;
        }

        NPCDeliveryBox box = Object.FindFirstObjectByType<NPCDeliveryBox>();
        if (box != null && box.fadeScreenImage != null)
        {
            fadeScreenImage = box.fadeScreenImage;
            return;
        }

        // Nếu chưa có, tự tạo 1 Image FadeScreen trên Canvas hiện tại
        Canvas existingCanvas = Object.FindFirstObjectByType<Canvas>();
        if (existingCanvas != null)
        {
            GameObject newFade = new GameObject("FadeScreen");
            newFade.transform.SetParent(existingCanvas.transform, false);
            fadeScreenImage = newFade.AddComponent<Image>();
            fadeScreenImage.color = Color.black;
            RectTransform rt = fadeScreenImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    public void ResetCockroach()
    {
        if (currentSequenceCoroutine != null) StopCoroutine(currentSequenceCoroutine);
        if (panicCoroutine != null) StopCoroutine(panicCoroutine);

        isSequenceRunning = false;
        hasHitPlayer = false;

        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        transform.SetParent(initialParent);
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        PlayAnimState(idleStateName);

        // Reset volume
        if (vignette != null) vignette.intensity.value = initialVignetteIntensity;
        if (chromaticAberration != null) chromaticAberration.intensity.value = initialChromaticIntensity;

        // Tắt fade nếu đang mở
        if (fadeScreenImage != null)
        {
            Color c = fadeScreenImage.color;
            c.a = 0f;
            fadeScreenImage.color = c;
            fadeScreenImage.gameObject.SetActive(false);
        }

        // Khôi phục Player Move
        MovePl playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (playerMovePl != null)
        {
            playerMovePl.SetMovementState(true);
            playerMovePl.isCameraLocked = false;
        }

        Debug.Log("[Cockroach] 🔄 Đã reset con gián về vị trí ban đầu!");
    }

    void LookAtTargetSmooth()
    {
        if (playerTarget == null) return;

        Vector3 dir = playerTarget.position - transform.position;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(modelRotationOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed * 2.5f);
        }
    }

    void PlayAnimState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        animator.CrossFadeInFixedTime(stateName, 0.1f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHitPlayer) return;

        if (other.CompareTag("Player") || other.GetComponent<MovePl>() != null || other.GetComponent<CharacterController>() != null)
        {
            OnHitPlayer();
        }
    }
}
