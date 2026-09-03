using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;

/// <summary>
/// Quản lý AI Chó Săn (Dog Enemy) trong Map 03:
/// 1. Không dùng NavMesh -> Dùng Context Steering AI 360° để né vách đá, cây cối, tường nhà không lo kẹt góc.
/// 2. Cơ chế Zone:
///    - Player vào Zone -> Chó sủa & lao vút tới săn đuổi người chơi với animation Run.
///    - Player ra khỏi Zone -> Chó dừng lại nhìn rồi tự động chạy về vị trí ban đầu và chuyển sang Idle.
/// 3. Xử lý Animation chuyên nghiệp: Dùng PlayableGraph / Mixer tự động hòa trộn mượt mà giữa Idle và Run.
/// 4. Anti-Stuck & Raycast bám mặt đất dốc tự nhiên.
/// </summary>
public class DogChaseBehavior : MonoBehaviour
{
    public enum DogState { Idle, WakingUp, Chasing, Stunned, Returning, Attacking }

    [Header("1. Cấu Hình Tốc Độ, Bán Kính & Thức Tỉnh")]
    [Tooltip("Bán kính phát hiện người chơi (mét) - Khi Player vào zone và đến gần trong bán kính này thì chó mới bắt đầu thức tỉnh (Mặc định: 15m)")]
    public float detectionRadius = 15f;

    [Tooltip("Tự động đợi âm thanh sủa phát xong hết rồi mới bắt đầu lao tới đuổi (Nếu có file Bark Sound)")]
    public bool waitAudioToFinishBeforeChase = true;

    [Tooltip("Thời gian đứng sủa/chuẩn bị trước khi lao tới đuổi (giây - Dùng khi không có Audio hoặc tắt tự động theo Audio) (Mặc định: 1.2s)")]
    public float wakeUpDelay = 1.2f;

    [Tooltip("Tốc độ chạy đuổi theo Player (m/s - Mặc định: 20 - 45)")]
    public float chaseSpeed = 20f;

    [Tooltip("Tốc độ chạy quay về vị trí ban đầu khi Player thoát khỏi zone")]
    public float returnSpeed = 30f;

    [Tooltip("Khoảng cách bắt được người chơi -> Game Over / Jumpscare (mét)")]
    public float killDistance = 3.0f;

    [Tooltip("Khoảng cách dừng lại khi đã về gần vị trí gốc (mét)")]
    public float arriveAtSpawnDistance = 1.5f;

    [Tooltip("Tốc độ xoay hướng nhìn (Mặc định: 12)")]
    public float turnSpeed = 12f;

    [Header("2. Bám Địa Hình & Trọng Lực (Ground Alignment)")]
    [Tooltip("Độ cao bù trừ tính từ mặt đất lên tâm của Chó")]
    public float groundOffset = 0.5f;

    [Tooltip("Khoảng cách quét tia tìm mặt đất (mét)")]
    public float groundCheckDistance = 10f;

    [Tooltip("Layer địa hình (Mặt đất / Terrain / Floor)")]
    public LayerMask groundLayerMask = ~0;

    [Header("3. Context Steering - Tránh Vật Cản & Chống Xuyên Tường")]
    [Tooltip("Layer các vật thể là vật cản cần né (Tường, đá to, cây). Bỏ qua Trigger / SafeZone")]
    public LayerMask obstacleLayerMask = ~0;

    [Tooltip("Bán kính thân chó để chặn va chạm vật lý chống xuyên tường (mét - Mặc định: 0.8)")]
    public float bodyRadius = 0.8f;

    [Tooltip("Khoảng cách quét phát hiện vật cản phía trước (mét)")]
    public float sensorDistance = 6.0f;

    [Tooltip("Bán kính tia SphereCast quét vật cản (mét)")]
    public float sensorRadius = 0.5f;

    [Tooltip("Độ cao bắn tia cảm biến tính từ chân lên")]
    public float sensorHeight = 0.8f;

    [Tooltip("Tốc độ bẻ lái mượt mà khi gặp vật cản")]
    public float steerSmoothSpeed = 12f;

    [Header("4. Tinh Chỉnh & Đồng Bộ Tốc Độ Animation")]
    [Tooltip("Kéo file AnimationClip 'ENM_DOG_idle' vào đây")]
    public AnimationClip idleClip;

    [Tooltip("Kéo file AnimationClip 'ENM_DOG_run' vào đây")]
    public AnimationClip runClip;

    [Tooltip("Kéo file AnimationClip 'ENM_DOG_stay' vào đây (Animation bị choáng lóa mắt)")]
    public AnimationClip stayClip;

    [Tooltip("Tự động đồng bộ tốc độ đập chân của Animation theo tốc độ di chuyển thực tế (Chống trượt băng)")]
    public bool autoSyncAnimSpeedWithMovement = true;

    [Tooltip("Hệ số bước chạy (Stride Multiplier) - Giảm số này xuống nếu chân đập quá nhanh (Ví dụ: 0.025 với Chase Speed = 40)")]
    [Range(0.001f, 0.2f)]
    public float animStrideMultiplier = 0.025f;

    [Tooltip("Hệ số nhân tốc độ cơ bản cho Animation Run (Mặc định: 1.0)")]
    public float baseRunAnimMultiplier = 1.0f;

    [Tooltip("Tốc độ phát Animation tối thiểu")]
    public float minAnimSpeed = 0.5f;

    [Tooltip("Tốc độ phát Animation tối đa")]
    public float maxAnimSpeed = 3.5f;

    [Header("5. Cơ Chế Bị Choáng Do Đèn Pin (Flashlight Stun)")]
    [Tooltip("Bật tính năng bị choáng rên rỉ khi bị rọi đèn pin vào mắt giống game gốc Death Forest 2")]
    public bool enableFlashlightStun = true;

    [Tooltip("Âm thanh chó rên rỉ khi bị lóa mắt (dog_unasare_lp.wav)")]
    public AudioClip whimperSound;

    [Tooltip("Thời gian bị choáng mỗi lần dính chớp flash (giây - Mặc định: 3.5s)")]
    public float stunDuration = 3.5f;

    [Header("6. Âm Thanh (Audio SFX)")]
    [Tooltip("Âm thanh tiếng chó sủa/gầm khi phát hiện người chơi")]
    public AudioClip barkSound;

    [Tooltip("Âm thanh chạy đuổi / thở dốc lặp lại khi đang săn")]
    public AudioClip chaseLoopSound;

    [Tooltip("Âm thanh tiếng táp cắn khi bắt được Player")]
    public AudioClip attackBiteSound;

    [Range(0f, 1f)] public float soundVolume = 1.0f;

    [Header("7. Thời Gian Đứng Nhìn Khi Player Rời Zone")]
    [Tooltip("Thời gian đứng nhìn theo Player trước khi quay đầu về chỗ cũ (giây)")]
    public float stareDurationBeforeReturn = 1.5f;

    [Header("8. Jumpscare & Hiệu Ứng Bị Táp (Camera Shake, Hide UI & Fade To Black)")]
    [Tooltip("Kéo GameObject 'DogPoint' (trong Main Camera > Jumpscare > DogPoint) vào đây")]
    public GameObject dogInCameraObject;

    [Tooltip("Kéo AnimationClip 'ENM_DOG hyokkori' vào đây")]
    public AnimationClip hyokkoriClip;

    [Tooltip("Âm thanh Jumpscare chó thò mặt vào camera (hyokkori_DOG.wav)")]
    public AudioClip hyokkoriSound;

    [Tooltip("Cường độ rung lắc Camera khi bị cắn xé (Mặc định: 0.18)")]
    public float cameraShakeIntensity = 0.18f;

    [Tooltip("Tự động ẩn toàn bộ UI/HUD (REC, Pin, Tâm ngắm...) trong lúc bị táp")]
    public bool hideUIOnAttack = true;

    [Tooltip("Thời gian hiện đầu chó trong camera trước khi Game Over (giây - Mặc định: 1.8s)")]
    public float inCameraJumpscareDuration = 1.8f;

    [Tooltip("Có tự động gọi Game Over sau khi jumpscare xong không? (Mặc định: True)")]
    public bool triggerGameOverAfterJumpscare = true;

    // --- State Variables ---
    public DogState currentState { get; private set; } = DogState.Idle;
    private bool isPlayerInZone = false;
    private bool hasAggroed = false;
    private bool hasCaughtPlayer = false;
    private float stareTimer = 0f;

    private Transform playerTransform;
    private Camera cachedPlayerCamera;
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private AudioSource audioSource;
    private AudioSource loopAudioSource;
    private Animator animator;
    private PlayableGraph playableGraph;

    // Context Steering 360°
    private const int NUM_DIRECTIONS = 16;
    private Vector3[] directionVectors;
    private float[] interestMap;
    private float[] dangerMap;
    private Vector3 currentMoveDirection;

    // Anti-Stuck System
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private float escapeTimer = 0f;
    private Vector3 escapeDirection = Vector3.zero;

    // Đồng bộ tốc độ Animation
    private Vector3 lastPosForAnimSpeed;

    void Awake()
    {
        initialSpawnPosition = transform.position;
        initialSpawnRotation = transform.rotation;
        lastPosition = transform.position;
        lastPosForAnimSpeed = transform.position;
        currentMoveDirection = transform.forward;

        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.5f;
        audioSource.playOnAwake = false;

        // Tạo nguồn âm thanh riêng cho tiếng gầm gừ lặp lại
        loopAudioSource = gameObject.AddComponent<AudioSource>();
        loopAudioSource.spatialBlend = 0.6f;
        loopAudioSource.loop = true;
        loopAudioSource.playOnAwake = false;

        InitContextSteering();

        // Tự động tìm stayClip và whimperSound nếu chưa được kéo thủ công
        if (stayClip == null)
        {
            AnimationClip[] allClips = Resources.FindObjectsOfTypeAll<AnimationClip>();
            foreach (var c in allClips)
            {
                if (c != null && c.name.ToLower().Contains("stay"))
                {
                    stayClip = c;
                    break;
                }
            }
        }

        if (whimperSound == null)
        {
            AudioClip[] allAudio = Resources.FindObjectsOfTypeAll<AudioClip>();
            foreach (var a in allAudio)
            {
                if (a != null && a.name.ToLower().Contains("unasare"))
                {
                    whimperSound = a;
                    break;
                }
            }
        }
    }

    void Start()
    {
        FindPlayer();
        FindDogInCameraObject();

        if (dogInCameraObject != null)
        {
            dogInCameraObject.SetActive(false);
        }

        // Tự động tìm âm thanh hyokkori_DOG nếu chưa gán
        if (hyokkoriSound == null)
        {
            AudioClip[] allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
            foreach (var c in allClips)
            {
                if (c != null && c.name.ToLower().Contains("hyokkori_dog"))
                {
                    hyokkoriSound = c;
                    break;
                }
            }
        }

        // Tự động tìm animation hyokkori nếu chưa gán
        if (hyokkoriClip == null)
        {
            AnimationClip[] allClips = Resources.FindObjectsOfTypeAll<AnimationClip>();
            foreach (var c in allClips)
            {
                if (c != null && c.name.ToLower().Contains("hyokkori"))
                {
                    hyokkoriClip = c;
                    break;
                }
            }
        }

        SetupChildTouchDetectors();
        SetState(DogState.Idle);
    }

    private void FindDogInCameraObject()
    {
        if (dogInCameraObject != null) return;

        // 1. Tìm trong tất cả Camera (kể cả inactive)
        Camera[] cams = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (var c in cams)
        {
            if (c == null) continue;
            Transform[] allTrans = c.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTrans)
            {
                if (t != null && t.name.Equals("DogPoint", System.StringComparison.OrdinalIgnoreCase))
                {
                    dogInCameraObject = t.gameObject;
                    return;
                }
            }
        }

        // 2. Tìm toàn bộ GameObject trong Scene
        GameObject[] allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjs)
        {
            if (obj != null && obj.name.Equals("DogPoint", System.StringComparison.OrdinalIgnoreCase) && obj.scene.isLoaded)
            {
                dogInCameraObject = obj;
                return;
            }
        }
    }

    private void SetupChildTouchDetectors()
    {
        Collider[] childCols = GetComponentsInChildren<Collider>(true);
        foreach (var col in childCols)
        {
            DogTouchDetector dt = col.GetComponent<DogTouchDetector>();
            if (dt == null) dt = col.gameObject.AddComponent<DogTouchDetector>();
            dt.Init(this);
        }
    }

    void OnEnable()
    {
        currentPlayingClip = null;
        SetState(currentState);
    }

    void OnDestroy()
    {
        CleanupGraphs();
    }

    void Update()
    {
        UpdateAnimationSpeed();

        if (hasCaughtPlayer) return;

        switch (currentState)
        {
            case DogState.Idle:
                // Đang đứng/nằm canh gác: Kiểm tra nếu Player ở trong zone và tiến gần vào phạm vi phát hiện
                if (isPlayerInZone && !hasAggroed)
                {
                    if (playerTransform == null) FindPlayer();
                    if (playerTransform != null)
                    {
                        float dist = Vector3.Distance(transform.position, playerTransform.position);
                        if (dist <= detectionRadius)
                        {
                            TriggerAggro();
                        }
                    }
                }
                break;

            case DogState.WakingUp:
            case DogState.Stunned:
                // Đang đứng sủa hoặc đang bị choáng rên rỉ: Đứng im tại chỗ
                break;

            case DogState.Chasing:
                UpdateChaseState();
                break;

            case DogState.Returning:
                UpdateReturnState();
                break;
        }
    }

    private void UpdateAnimationSpeed()
    {
        float targetPlaySpeed = 1.0f;

        if (currentState == DogState.WakingUp)
        {
            // Khi đang đứng sủa: Đóng băng/dừng animation (tốc độ = 0) để tạo dáng đứng im chuẩn
            targetPlaySpeed = 0.0f;
        }
        else if (currentState == DogState.Stunned)
        {
            // Animation Stay (bị choáng rên rỉ) phát với tốc độ chuẩn 1.0
            targetPlaySpeed = 1.0f;
        }
        else if (currentState == DogState.Chasing || currentState == DogState.Returning)
        {
            if (autoSyncAnimSpeedWithMovement)
            {
                float actualMoveSpeed = (transform.position - lastPosForAnimSpeed).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
                targetPlaySpeed = Mathf.Clamp(actualMoveSpeed * animStrideMultiplier * baseRunAnimMultiplier, minAnimSpeed, maxAnimSpeed);
            }
            else
            {
                targetPlaySpeed = baseRunAnimMultiplier;
            }
        }
        else
        {
            targetPlaySpeed = 1.0f; // Idle speed
        }

        lastPosForAnimSpeed = transform.position;

        foreach (var g in activeGraphs)
        {
            if (g.IsValid())
            {
                var root = g.GetRootPlayable(0);
                if (root.IsValid())
                {
                    root.SetSpeed(targetPlaySpeed);
                }
            }
        }
    }

    // =================================================================
    // ZONE EVENTS (ĐƯỢC GỌI TỪ DogDangerZone)
    // =================================================================

    public void OnPlayerEnteredZone(Transform player)
    {
        if (hasCaughtPlayer) return;

        playerTransform = player;
        isPlayerInZone = true;
        stareTimer = 0f;

        Debug.Log("[DogChaseBehavior] 🐕 Người chơi đã bước vào Zone (Chó đang Idle canh gác)...");

        // Nếu người chơi bước vào mà đã đứng sẵn trong phạm vi phát hiện -> Kích hoạt ngay
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= detectionRadius)
        {
            TriggerAggro();
        }
    }

    public void OnPlayerExitedZone()
    {
        if (hasCaughtPlayer) return;

        if (wakeUpCoroutine != null)
        {
            StopCoroutine(wakeUpCoroutine);
            wakeUpCoroutine = null;
        }

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }

        isPlayerInZone = false;
        hasAggroed = false;
        stareTimer = stareDurationBeforeReturn;

        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
        }

        Debug.Log("[DogChaseBehavior] 🐕 Người chơi đã chạy thoát khỏi Zone -> Dừng lại quan sát rồi quay về.");
    }

    // =================================================================
    // FLASHLIGHT STUN LOGIC (CƠ CHẾ BỊ CHOÁNG BỞI CHỚP FLASH - STAY ANIMATION)
    // =================================================================

    private Coroutine stunCoroutine = null;

    /// <summary>
    /// Được gọi từ FlashlightToggle khi người chơi bấm Chuột Phải chớp flash lóa mắt
    /// </summary>
    public void OnCameraFlashStunned()
    {
        if (!enableFlashlightStun || hasCaughtPlayer) return;

        // Chỉ cho phép làm choáng khi chó đang hoạt động (không bị lặp khi đang Stunned)
        if (currentState == DogState.Chasing || currentState == DogState.WakingUp || currentState == DogState.Returning || currentState == DogState.Idle)
        {
            Debug.Log("[DogChaseBehavior] ⚡ BỊ CHỚP FLASH CHÓI MẮT! Chó bị choáng, dừng lại và chuyển sang animation 'Stay'...");
            TriggerStun();
        }
    }

    private void TriggerStun()
    {
        if (hasCaughtPlayer) return;

        if (wakeUpCoroutine != null)
        {
            StopCoroutine(wakeUpCoroutine);
            wakeUpCoroutine = null;
        }

        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        SetState(DogState.Stunned);

        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
        }

        if (whimperSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(whimperSound, soundVolume);
        }

        // Đứng im tại chỗ rên rỉ vì bị chói mắt trong khoảng stunDuration
        yield return new WaitForSeconds(stunDuration);

        if (isPlayerInZone && !hasCaughtPlayer)
        {
            if (chaseLoopSound != null && loopAudioSource != null && !loopAudioSource.isPlaying)
            {
                loopAudioSource.clip = chaseLoopSound;
                loopAudioSource.volume = soundVolume * 0.8f;
                loopAudioSource.Play();
            }

            SetState(DogState.Chasing);
            Debug.Log("[DogChaseBehavior] 🐕 Hết thời gian choáng -> Tiếp tục lao tới săn đuổi!");
        }
        else
        {
            SetState(DogState.Returning);
        }

        stunCoroutine = null;
    }

    private Coroutine wakeUpCoroutine = null;

    private void TriggerAggro()
    {
        if (hasAggroed || hasCaughtPlayer) return;
        hasAggroed = true;

        if (wakeUpCoroutine != null) StopCoroutine(wakeUpCoroutine);
        wakeUpCoroutine = StartCoroutine(WakeUpAndChaseRoutine());
    }

    private IEnumerator WakeUpAndChaseRoutine()
    {
        SetState(DogState.WakingUp);
        Debug.Log($"[DogChaseBehavior] 🐕 SỦA! Người chơi đã lọt vào phạm vi ({detectionRadius}m) -> Đứng dậy sủa cảnh báo...");

        float duration = wakeUpDelay;

        if (barkSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(barkSound, soundVolume);
            if (waitAudioToFinishBeforeChase && barkSound.length > 0f)
            {
                duration = barkSound.length;
            }
        }

        // Đứng im tại chỗ sủa và xoay người hướng về phía người chơi
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (playerTransform != null)
            {
                RotateTowards(playerTransform.position);
            }
            yield return null;
        }

        // Tiếng sủa vừa dứt -> Bắt đầu lao tới rượt đuổi
        if (isPlayerInZone && !hasCaughtPlayer)
        {
            if (chaseLoopSound != null && loopAudioSource != null && !loopAudioSource.isPlaying)
            {
                loopAudioSource.clip = chaseLoopSound;
                loopAudioSource.volume = soundVolume * 0.8f;
                loopAudioSource.Play();
            }

            SetState(DogState.Chasing);
            Debug.Log("[DogChaseBehavior] 🐕 BẮT ĐẦU LAO TỚI RƯỢT ĐUỔI NGƯỜI CHƠI!");
        }
        else
        {
            SetState(DogState.Returning);
        }

        wakeUpCoroutine = null;
    }

    // =================================================================
    // CHASE LOGIC
    // =================================================================

    private void UpdateChaseState()
    {
        if (!isPlayerInZone)
        {
            // Player vừa rời khỏi Zone -> Đứng nhìn một lát rồi quay về
            stareTimer -= Time.deltaTime;
            if (playerTransform != null)
            {
                RotateTowards(playerTransform.position);
            }

            if (stareTimer <= 0f)
            {
                SetState(DogState.Returning);
            }
            return;
        }

        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        Vector3 targetPos = playerTransform.position;

        // KIỂM TRA BẮT ĐƯỢC NGƯỜI CHƠI (DÙNG KHOẢNG CÁCH MẶT PHẲNG XZ ĐỂ KHÔNG BỊ LỆCH DO GROUND OFFSET)
        Vector2 dogFlat = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerFlat = new Vector2(targetPos.x, targetPos.z);
        float flatDist = Vector2.Distance(dogFlat, playerFlat);
        float realDist = Vector3.Distance(transform.position, targetPos);

        if (flatDist <= Mathf.Max(killDistance, bodyRadius + 1.2f) || realDist <= killDistance + groundOffset)
        {
            CatchPlayer();
            return;
        }

        // TÍNH TOÁN HƯỚNG CHẠY NÉ VẬT CẢN (CONTEXT STEERING)
        Vector3 moveDir = CalculateBestDirection(targetPos);
        MoveAlongDirection(moveDir, chaseSpeed);
    }

    // =================================================================
    // RETURN LOGIC
    // =================================================================

    private void UpdateReturnState()
    {
        if (isPlayerInZone)
        {
            // Nếu người chơi quay lại Zone và tiến gần vào bán kính phát hiện
            if (playerTransform == null) FindPlayer();
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= detectionRadius)
            {
                TriggerAggro();
                return;
            }
        }

        float distToSpawn = Vector3.Distance(transform.position, initialSpawnPosition);
        if (distToSpawn <= arriveAtSpawnDistance)
        {
            // Đã về đến nơi -> Đứng im quay về hướng ban đầu
            transform.position = initialSpawnPosition;
            transform.rotation = initialSpawnRotation;

            if (loopAudioSource != null && loopAudioSource.isPlaying)
            {
                loopAudioSource.Stop();
            }

            hasAggroed = false;
            SetState(DogState.Idle);
            Debug.Log("[DogChaseBehavior] 🐕 Đã quay về vị trí ban đầu -> Chuyển sang Idle nằm canh gác.");
            return;
        }

        Vector3 moveDir = CalculateBestDirection(initialSpawnPosition);
        MoveAlongDirection(moveDir, returnSpeed);
    }

    // =================================================================
    // MOVEMENT & CONTEXT STEERING (NÉ VẬT CẢN 360°)
    // =================================================================

    private void MoveAlongDirection(Vector3 moveDir, float speed)
    {
        if (moveDir.sqrMagnitude < 0.001f) return;

        // Xoay mặt mượt mà theo hướng di chuyển
        RotateTowards(transform.position + moveDir);

        float moveStep = speed * Time.deltaTime;
        Vector3 desiredDisplacement = moveDir * moveStep;
        Vector3 rayOrigin = transform.position + Vector3.up * sensorHeight;

        // 1. KIỂM TRA CHỐNG XUYÊN TƯỜNG 100%: Quét tia SphereCast kiểm tra chướng ngại vật trước khi bước
        if (Physics.SphereCast(rayOrigin, bodyRadius, moveDir, out RaycastHit hit, moveStep + 0.15f, obstacleLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (!IsSelfOrChild(hit.collider))
            {
                // Trượt dọc theo vách tường (Wall Slide) thay vì đi xuyên qua
                Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, hit.normal).normalized;
                slideDir.y = 0f;

                // Thử di chuyển theo hướng trượt dọc tường
                if (slideDir.sqrMagnitude > 0.01f && !Physics.SphereCast(rayOrigin, bodyRadius, slideDir, out RaycastHit slideHit, moveStep * 0.8f, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    desiredDisplacement = slideDir * (moveStep * 0.8f);
                }
                else
                {
                    // Đâm thẳng góc vào tường hoặc góc chết -> Dừng lại, tuyệt đối KHÔNG đi xuyên qua
                    desiredDisplacement = Vector3.zero;
                }
            }
        }

        Vector3 newPos = transform.position + desiredDisplacement;

        // 2. Bám sát bề mặt địa hình dốc / mặt đất
        newPos.y = GetGroundHeight(newPos);

        // 3. ĐẨY LÙI RA KHỎI TƯỜNG NẾU BỊ LÚM (Depenetration Pushback)
        Collider[] overlaps = Physics.OverlapSphere(newPos + Vector3.up * sensorHeight, bodyRadius, obstacleLayerMask, QueryTriggerInteraction.Ignore);
        foreach (var col in overlaps)
        {
            if (col != null && !col.isTrigger && !IsSelfOrChild(col))
            {
                // Chỉ gọi ClosestPoint trên các Collider hỗ trợ (Tránh cảnh báo Unity trên non-convex MeshCollider)
                if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider || (col is MeshCollider mc && mc.convex))
                {
                    Vector3 closestPt = col.ClosestPoint(newPos + Vector3.up * sensorHeight);
                    Vector3 away = (newPos + Vector3.up * sensorHeight) - closestPt;
                    away.y = 0f;
                    if (away.sqrMagnitude > 0.0001f)
                    {
                        newPos += away.normalized * 0.08f;
                    }
                }
            }
        }

        transform.position = newPos;

        // Anti-Stuck Check
        CheckAntiStuck(speed);
    }

    private bool IsSelfOrChild(Collider col)
    {
        if (col == null) return false;
        return col.transform.root == transform.root || col.transform.IsChildOf(transform);
    }

    private void RotateTowards(Vector3 targetWorldPos)
    {
        Vector3 flatDir = targetWorldPos - transform.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    private float GetGroundHeight(Vector3 samplePos)
    {
        Ray ray = new Ray(samplePos + Vector3.up * (groundCheckDistance * 0.5f), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, groundCheckDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y + groundOffset;
        }
        return samplePos.y;
    }

    private Vector3 CalculateBestDirection(Vector3 targetPos)
    {
        // Xử lý pha thoát kẹt Anti-Stuck cưỡng chế
        if (escapeTimer > 0f)
        {
            escapeTimer -= Time.deltaTime;
            return escapeDirection;
        }

        Vector3 toTarget = (targetPos - transform.position).normalized;
        toTarget.y = 0f;

        Vector3 rayOrigin = transform.position + Vector3.up * sensorHeight;

        // 1. TÍNH ĐIỂM HỨNG THÚ (Interest Map) & NGUY HIỂM (Danger Map)
        for (int i = 0; i < NUM_DIRECTIONS; i++)
        {
            Vector3 dir = directionVectors[i];

            // Điểm hứng thú: Càng gần hướng mục tiêu điểm càng cao (0 -> 1)
            float dot = Vector3.Dot(dir, toTarget);
            interestMap[i] = Mathf.Max(0f, dot);

            // Điểm nguy hiểm: Quét tia SphereCast phát hiện vật cản (bỏ qua collider của chính mình)
            RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, sensorRadius, dir, sensorDistance, obstacleLayerMask, QueryTriggerInteraction.Ignore);
            float nearestDist = sensorDistance;
            bool hitObstacle = false;

            foreach (var h in hits)
            {
                if (!IsSelfOrChild(h.collider))
                {
                    if (h.distance < nearestDist)
                    {
                        nearestDist = h.distance;
                        hitObstacle = true;
                    }
                }
            }

            if (hitObstacle)
            {
                dangerMap[i] = 1f - (nearestDist / sensorDistance);
            }
            else
            {
                dangerMap[i] = 0f;
            }
        }

        // 2. TỔNG HỢP VECTƠ CHỌN HƯỚNG TỐI ƯU NHẤT
        Vector3 optimalDir = Vector3.zero;
        for (int i = 0; i < NUM_DIRECTIONS; i++)
        {
            float weight = Mathf.Clamp01(interestMap[i] - dangerMap[i]);
            optimalDir += directionVectors[i] * weight;
        }

        if (optimalDir.sqrMagnitude < 0.01f)
        {
            // Nếu các hướng chính đều bị chặn -> Chọn hướng ít nguy hiểm nhất
            float minDanger = 999f;
            int bestIdx = 0;
            for (int i = 0; i < NUM_DIRECTIONS; i++)
            {
                if (dangerMap[i] < minDanger)
                {
                    minDanger = dangerMap[i];
                    bestIdx = i;
                }
            }
            optimalDir = directionVectors[bestIdx];
        }

        optimalDir.Normalize();

        // Bẻ lái mượt mà
        currentMoveDirection = Vector3.Slerp(currentMoveDirection, optimalDir, steerSmoothSpeed * Time.deltaTime).normalized;
        return currentMoveDirection;
    }

    private void CheckAntiStuck(float expectedSpeed)
    {
        float movedDist = Vector3.Distance(transform.position, lastPosition);
        float expectedDist = expectedSpeed * Time.deltaTime;

        if (movedDist < expectedDist * 0.2f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 0.25f)
            {
                // Đã bị kẹt -> Kích hoạt thoát kẹt sang hướng thoáng nhất
                stuckTimer = 0f;
                escapeTimer = 0.5f;

                // Quét tìm hướng có khoảng trống xa nhất
                float maxOpenDist = -1f;
                Vector3 bestEscape = -transform.forward;
                Vector3 rayOrigin = transform.position + Vector3.up * sensorHeight;

                for (int i = 0; i < NUM_DIRECTIONS; i++)
                {
                    if (Physics.SphereCast(rayOrigin, sensorRadius, directionVectors[i], out RaycastHit hit, sensorDistance * 2f, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.distance > maxOpenDist)
                        {
                            maxOpenDist = hit.distance;
                            bestEscape = directionVectors[i];
                        }
                    }
                    else
                    {
                        bestEscape = directionVectors[i];
                        break;
                    }
                }

                escapeDirection = bestEscape;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    // =================================================================
    // ATTACK & CATCH PLAYER (GAME OVER & IN-CAMERA JUMPSCARE)
    // =================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (hasCaughtPlayer) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            CatchPlayer();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCaughtPlayer) return;
        if (collision.collider.CompareTag("Player") || collision.collider.GetComponentInParent<MovePl>() != null)
        {
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        if (hasCaughtPlayer) return;
        hasCaughtPlayer = true;

        SetState(DogState.Attacking);
        Debug.Log("[DogChaseBehavior] 💀 Chó đã táp trúng người chơi -> Kích hoạt Dog In-Camera Jumpscare!");

        if (loopAudioSource != null) loopAudioSource.Stop();

        // 1. Ẩn con chó trong thế giới (World Dog)
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = false;

        Collider[] cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = false;

        // 2. Bật Jumpscare trong Camera với hiệu ứng Rung Lắc & Ẩn UI
        StartCoroutine(DogCameraJumpscareRoutine());
    }

    private IEnumerator CameraShakeRoutine(Transform camTrans, float duration, float intensity)
    {
        if (camTrans == null) yield break;

        Vector3 originalLocalPos = camTrans.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentIntensity = Mathf.Lerp(intensity, 0.02f, elapsed / duration);
            Vector3 randomOffset = Random.insideUnitSphere * currentIntensity;
            camTrans.localPosition = originalLocalPos + randomOffset;
            yield return null;
        }

        camTrans.localPosition = originalLocalPos;
    }

    private IEnumerator DogCameraJumpscareRoutine()
    {
        if (dogInCameraObject == null) FindDogInCameraObject();

        // 1. Khóa di chuyển và góc xoay của người chơi
        MovePl playerMove = Object.FindFirstObjectByType<MovePl>();
        if (playerMove != null)
        {
            playerMove.isCameraLocked = true;
            playerMove.enabled = false;
        }

        // 2. Ẩn toàn bộ UI / HUD trong lúc bị tấn công
        System.Collections.Generic.List<Canvas> hiddenCanvases = new System.Collections.Generic.List<Canvas>();
        if (hideUIOnAttack)
        {
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c != null && c.enabled && !c.name.Contains("Fade") && !c.name.Contains("GameOver") && !c.name.Contains("Jumpscare"))
                {
                    c.enabled = false;
                    hiddenCanvases.Add(c);
                }
            }
        }

        Transform camToShake = null;
        if (Camera.main != null) camToShake = Camera.main.transform;
        else
        {
            Camera anyCam = Object.FindFirstObjectByType<Camera>();
            if (anyCam != null) camToShake = anyCam.transform;
        }

        if (dogInCameraObject != null)
        {
            dogInCameraObject.SetActive(true);

            // Bật renderer trên DogPoint
            Renderer[] rends = dogInCameraObject.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends) r.enabled = true;

            // Bật Animator trên DogPoint
            Animator anim = dogInCameraObject.GetComponent<Animator>() ?? dogInCameraObject.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (anim.runtimeAnimatorController != null)
                {
                    anim.Play(0, 0, 0f);
                }
            }

            // Phát animation hyokkori nếu có clip
            if (hyokkoriClip != null && anim != null)
            {
                try
                {
                    PlayableGraph graph = PlayableGraph.Create("DogHyokkoriGraph");
                    var output = AnimationPlayableOutput.Create(graph, "Animation", anim);
                    var clipPlayable = AnimationClipPlayable.Create(graph, hyokkoriClip);
                    output.SetSourcePlayable(clipPlayable);
                    graph.Play();
                    activeGraphs.Add(graph);
                }
                catch { }
            }

            // Phát âm thanh hyokkori_DOG hoặc attackBiteSound
            AudioClip soundToPlay = hyokkoriSound != null ? hyokkoriSound : attackBiteSound;
            if (soundToPlay != null)
            {
                AudioSource camAudio = dogInCameraObject.GetComponent<AudioSource>();
                if (camAudio == null) camAudio = dogInCameraObject.AddComponent<AudioSource>();
                camAudio.spatialBlend = 0f;
                camAudio.PlayOneShot(soundToPlay, soundVolume);
            }

            // Tính toán thời lượng animation
            float animDuration = inCameraJumpscareDuration;
            if (hyokkoriClip != null && hyokkoriClip.length > 0.01f)
            {
                animDuration = hyokkoriClip.length;
            }
            else if (soundToPlay != null && soundToPlay.length > 0.01f)
            {
                animDuration = soundToPlay.length;
            }

            // 3. Kích hoạt hiệu ứng Rung Lắc Camera
            if (camToShake != null && cameraShakeIntensity > 0.001f)
            {
                StartCoroutine(CameraShakeRoutine(camToShake, animDuration, cameraShakeIntensity));
            }

            // Chờ hết thời gian animation
            yield return new WaitForSeconds(animDuration);

            // 4. SAU KHI HẾT ANIMATION: VẪN GIỮ CON CHÓ TRÊN MÀN HÌNH TRONG 1.0 GIÂY
            yield return new WaitForSeconds(1.0f);

            // KẾT THÚC 2 GIÂY -> TẮT ACTIVE ĐẦU CHÓ TRONG CAMERA
            if (dogInCameraObject != null)
            {
                dogInCameraObject.SetActive(false);
            }

            // KIỂM TRA CHẾ ĐỘ BẤT TỬ (GOD MODE / DEMO PHÍM P)
            if (GodModeManager.IsGodModeActive)
            {
                Debug.Log("<color=cyan><b>[DogChaseBehavior] 👑 GodMode đang BẬT -> Tự động thả tự do di chuyển cho Player đi tiếp!</b></color>");

                // 1. Mở lại điều khiển cho Player
                if (playerMove != null)
                {
                    playerMove.isCameraLocked = false;
                    playerMove.enabled = true;
                    playerMove.SetMovementState(true);
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // 2. Mở lại toàn bộ UI / HUD
                foreach (var c in hiddenCanvases)
                {
                    if (c != null) c.enabled = true;
                }

                // 3. Reset lại con chó về vị trí spawn ban đầu để có thể tiếp tục chơi/demo
                hasCaughtPlayer = false;
                hasAggroed = false;
                transform.position = initialSpawnPosition;
                transform.rotation = initialSpawnRotation;

                Renderer[] rList = GetComponentsInChildren<Renderer>(true);
                foreach (var r in rList) r.enabled = true;

                Collider[] cList = GetComponentsInChildren<Collider>(true);
                foreach (var col in cList) col.enabled = true;

                SetState(DogState.Idle);
                CleanupGraphs();

                yield break; // Kết thúc tại đây, KHÔNG về Menu!
            }

            // NẾU KHÔNG BẬT GOD MODE -> BẮT ĐẦU FADE ĐEN TOÀN MÀN HÌNH VÀ VỀ MENU
            yield return StartCoroutine(FadeToBlackRoutine(1.0f));
        }
        else
        {
            if (attackBiteSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackBiteSound, soundVolume);
            }
            yield return new WaitForSeconds(1.0f);

            if (GodModeManager.IsGodModeActive)
            {
                if (playerMove != null)
                {
                    playerMove.isCameraLocked = false;
                    playerMove.enabled = true;
                    playerMove.SetMovementState(true);
                }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                yield break;
            }

            yield return StartCoroutine(FadeToBlackRoutine(1.0f));
        }

        CleanupGraphs();

        // 6. MÀN HÌNH ĐEN HOÀN TOÀN -> NHẬN PHÍM/CHUỘT BẤT KỲ ĐỂ QUAY VỀ MENU
        yield return StartCoroutine(WaitForClickAndReturnToMenuRoutine());

        // Tắt hẳn GameObject con chó ngoài map
        gameObject.SetActive(false);
    }

    private IEnumerator FadeToBlackRoutine(float duration)
    {
        // Tạo Canvas đen toàn màn hình
        GameObject canvasObj = new GameObject("GameOverFadeCanvas");
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

        // Fade đen từ 0 lên 1
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

    private IEnumerator WaitForClickAndReturnToMenuRoutine()
    {
        // Mở khóa chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Chờ 0.25s để tránh ăn phải cú click trước đó
        yield return new WaitForSeconds(0.25f);

        // Lắng nghe người chơi nhấp chuột hoặc bấm bất kỳ phím nào
        while (!Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
        {
            yield return null;
        }

        // Chuyển mượt về Menu chính
        string sceneToLoad = "MainMenu";
        if (GameOverJumpscareManager.Instance != null && !string.IsNullOrEmpty(GameOverJumpscareManager.Instance.mainMenuSceneName))
        {
            sceneToLoad = GameOverJumpscareManager.Instance.mainMenuSceneName;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
    }

    // =================================================================
    // ANIMATION PLAYABLE SYSTEM (IDLE & RUN - AUTO LOOP)
    // =================================================================

    private System.Collections.Generic.List<PlayableGraph> activeGraphs = new System.Collections.Generic.List<PlayableGraph>();
    private AnimationClip currentPlayingClip = null;

    private void PlayClip(AnimationClip clip, float speed = 1.0f)
    {
        if (clip == null) return;
        currentPlayingClip = clip;

        // BẮT BUỘC BẬT CHẾ ĐỘ LOOP ĐỂ ANIMATION LẶP LẠI VĨNH VIỄN
        clip.wrapMode = WrapMode.Loop;

        CleanupGraphs();

        Animator[] allAnimators = GetComponentsInChildren<Animator>(true);
        if (allAnimators == null || allAnimators.Length == 0)
        {
            Animator directAnim = GetComponent<Animator>();
            if (directAnim != null) allAnimators = new Animator[] { directAnim };
        }

        if (allAnimators == null || allAnimators.Length == 0)
        {
            Debug.LogWarning("[DogChaseBehavior] ⚠️ Không tìm thấy Animator nào trên Dog!");
            return;
        }

        foreach (var anim in allAnimators)
        {
            if (anim == null) continue;
            anim.enabled = true;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            try
            {
                PlayableGraph graph = PlayableGraph.Create("DogAnim_" + anim.gameObject.name);
                var output = AnimationPlayableOutput.Create(graph, "Animation", anim);
                var clipPlayable = AnimationClipPlayable.Create(graph, clip);
                clipPlayable.SetSpeed(speed);
                clipPlayable.SetDuration(double.MaxValue);
                output.SetSourcePlayable(clipPlayable);

                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                graph.Play();

                activeGraphs.Add(graph);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DogChaseBehavior] Lỗi phát Animation trên {anim.gameObject.name}: {ex.Message}");
                if (anim.runtimeAnimatorController != null)
                {
                    anim.Play(clip.name, 0, 0f);
                }
            }
        }
    }

    private void CleanupGraphs()
    {
        foreach (var g in activeGraphs)
        {
            if (g.IsValid()) g.Destroy();
        }
        activeGraphs.Clear();
    }

    private void SetState(DogState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case DogState.Idle:
                PlayClip(idleClip, 1.0f);
                break;

            case DogState.WakingUp:
                // Đứng dậy chuyển sang dáng đứng (runClip) nhưng DỪNG/PAUSE animation (tốc độ = 0)
                PlayClip(runClip, 0.0f);
                break;

            case DogState.Stunned:
                // Bị choáng do đèn pin: Phát animation Stay (co rúm rên rỉ)
                if (stayClip != null)
                {
                    PlayClip(stayClip, 1.0f);
                }
                else
                {
                    PlayClip(runClip, 0.0f);
                }
                break;

            case DogState.Chasing:
            case DogState.Returning:
            case DogState.Attacking:
                PlayClip(runClip, baseRunAnimMultiplier);
                break;
        }
    }

    private void InitContextSteering()
    {
        directionVectors = new Vector3[NUM_DIRECTIONS];
        interestMap = new float[NUM_DIRECTIONS];
        dangerMap = new float[NUM_DIRECTIONS];

        for (int i = 0; i < NUM_DIRECTIONS; i++)
        {
            float angle = i * (360f / NUM_DIRECTIONS);
            directionVectors[i] = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        }
    }

    private void FindPlayer()
    {
        if (playerTransform != null) return;

        MovePl pl = Object.FindFirstObjectByType<MovePl>();
        if (pl != null)
        {
            playerTransform = pl.transform;
            return;
        }

        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) playerTransform = pObj.transform;
    }

    public void NotifyPlayerCaught()
    {
        CatchPlayer();
    }

    private void OnDrawGizmosSelected()
    {
        // Vòng tròn vàng: Phạm vi phát hiện người chơi (Detection Radius)
        Gizmos.color = hasAggroed ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Vòng tròn đỏ: Khoảng cách cắn người chơi (Kill Distance)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killDistance);
    }
}

/// <summary>
/// Helper Component gắn lên các Collider con của Dog để bắt va chạm với Player
/// </summary>
public class DogTouchDetector : MonoBehaviour
{
    private DogChaseBehavior owner;

    public void Init(DogChaseBehavior o)
    {
        owner = o;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            owner.NotifyPlayerCaught();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (owner == null) return;
        if (collision.collider.CompareTag("Player") || collision.collider.GetComponentInParent<MovePl>() != null)
        {
            owner.NotifyPlayerCaught();
        }
    }
}
