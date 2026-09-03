using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý hành vi quái 'Stranger' (Người Lạ) trong Map 03:
/// 1. Cơ chế Đèn Pin: Khi bị chớp flash Chuột Phải -> Bất động hóa đá vĩnh viễn và spawn con tiếp theo.
/// 2. Context Steering AI (Bản đồ hứng thú/nguy hiểm 360°):
///    Quét 16 hướng xung quanh, tính điểm hứng thú (gần Player) và nguy hiểm (gần vật cản),
///    chọn hướng tối ưu nhất để lách qua mọi địa hình phức tạp.
/// 3. Anti-Stuck cưỡng chế: Kẹt > 0.3s -> Quét 360° tìm hướng thoáng nhất, thoát kẹt rồi mới quay lại đuổi.
/// 4. Jumpscare In-Camera: Khi bắt được Player -> Bật mô hình Stranger trong Camera + Rung lắc + GodMode Check.
/// </summary>
public class StrangerBehavior : MonoBehaviour
{
    [Header("1. Cấu Hình Tốc Độ & Bắt Người Chơi")]
    [Tooltip("Tốc độ trượt siêu nhanh khi săn đuổi (Mặc định: 60 - 80)")]
    public float fastSpeed = 70f;

    [Tooltip("Khoảng cách kích hoạt bắt / Game Over khi áp sát người chơi")]
    public float killDistance = 2.5f;

    [Tooltip("Tốc độ rơi từ trên không xuống đất / Trọng lực (Mặc định: 35 - 50)")]
    public float fallSpeed = 35f;

    [Tooltip("Độ cao bù trừ khi không dùng CharacterController (Mặc định: 1.2)")]
    public float groundOffset = 1.2f;

    [Tooltip("Góc xoay bù trừ để dựng đứng mô hình (Ví dụ: -90, 0, 0 hoặc 0, 0, 0)")]
    public Vector3 modelRotationOffset = new Vector3(0f, 0f, 0f);

    [Header("2. Context Steering - Tránh Vật Cản Thông Minh 360°")]
    [Tooltip("Layer các vật thể là vật cản cần né (Vách núi, tường, đá to). Bỏ chọn Fence/Rope để quái lướt xuyên qua không né)")]
    public LayerMask obstacleLayerMask = ~0;

    [Tooltip("Khoảng cách quét phát hiện vật cản (Mặc định: 4.0m)")]
    public float sensorDistance = 4.0f;

    [Tooltip("Bán kính tia cảm biến SphereCast (Mặc định: 0.3m)")]
    public float sensorRadius = 0.3f;

    [Tooltip("Độ cao gốc bắn tia tính từ chân lên (Mặc định: 0.8m)")]
    public float sensorHeight = 0.8f;

    [Tooltip("Tốc độ chuyển hướng mượt mà (Mặc định: 10)")]
    public float steerSmoothSpeed = 10f;

    [Header("3. Giới Hạn Khu Vực (Zone Boundary)")]
    [Tooltip("Chỉ hoạt động khi có Player trong Zone (Được StrangerDangerZone quản lý)")]
    public bool isPlayerInZone = false;

    [Tooltip("Khóa Stranger không được chạy vượt ra khỏi ranh giới BoxCollider của Zone")]
    public bool clampToZoneBounds = true;

    [Header("4. Âm Thanh Rùng Rợn (SFX)")]
    public AudioClip creepSound;
    public AudioClip catchJumpscareSound;

    [Header("5. Thời Gian Đứng Nhìn Khi Player Rời Zone")]
    [Tooltip("Thời gian Stranger đứng nhìn theo Player khi Player rời Zone trước khi quay về chỗ cũ (giây - Mặc định: 1.5s)")]
    public float stareDurationBeforeReset = 1.5f;

    [Header("6. Chế Độ Bất Động Do Đèn Nháy Flash (Flashlight Stun)")]
    [Tooltip("Bật nếu muốn Stranger bị làm choáng hóa đá vĩnh viễn khi bị chớp đèn pin Chuột Phải")]
    public bool freezeOnFlashlightStun = true;
    public bool isPermanentlyFrozen = false;
    [HideInInspector] public StrangerMinigameManager minigameManager;

    public bool permanentFreezeOnLook
    {
        get => freezeOnFlashlightStun;
        set => freezeOnFlashlightStun = value;
    }

    [Header("7. Jumpscare Trong Camera (In-Camera Jumpscare)")]
    [Tooltip("GameObject Stranger nằm trong Main Camera > Jumpscare > Stranger")]
    public GameObject strangerInCameraObject;
    public AnimationClip jumpscareClip;

    [Tooltip("Thời điểm animation chạy tới đoạn há mồm rùng rợn và dừng lại (giây - Mặc định: 4.8s tương ứng frame 288)")]
    public float animMouthOpenStopTime = 4.8f;

    [Tooltip("Thời gian đứng đơ giữ nguyên tư thế há mồm trước khi bắt đầu Fade đen (giây - Mặc định: 1.0s)")]
    public float holdMouthOpenDuration = 1.0f;

    [Tooltip("Thời gian màn hình Fade chuyển dần sang màu đen (giây - Mặc định: 1.0s)")]
    public float fadeToBlackDuration = 1.0f;

    [Tooltip("Cường độ rung lắc Camera khi bị jumpscare")]
    public float cameraShakeIntensity = 0.25f;

    public bool hideUIOnAttack = true;

    // --- Private ---
    private Transform player;
    private Camera mainCam;
    private Animator anim;
    private AudioSource audioSource;
    private CharacterController characterController;
    private Collider boundZoneCollider;

    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private bool isFrozen = false;
    public bool IsFrozen => isFrozen;
    private bool hasCaughtPlayer = false;

    private bool isWaitingToReset = false;
    private float exitZoneTimer = 0f;

    // Context Steering
    private const int NUM_DIRECTIONS = 16; // 360° / 16 = 22.5° mỗi hướng
    private Vector3[] directionVectors;
    private float[] interestMap;
    private float[] dangerMap;
    private Vector3 currentMoveDirection;

    // Anti-Stuck
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private float stuckEscapeTimer = 0f; // Thời gian còn lại trong pha thoát kẹt
    private Vector3 escapeDirection;
    private float verticalVelocity = 0f;

    void Awake()
    {
        initialSpawnPosition = transform.position;
        initialSpawnRotation = transform.rotation;
        lastPosition = transform.position;
        currentMoveDirection = transform.forward;
        escapeDirection = Vector3.zero;

        anim = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();

        // Tạo 16 hướng quét đều quanh vòng tròn 360°
        directionVectors = new Vector3[NUM_DIRECTIONS];
        interestMap = new float[NUM_DIRECTIONS];
        dangerMap = new float[NUM_DIRECTIONS];

        for (int i = 0; i < NUM_DIRECTIONS; i++)
        {
            float angle = i * (360f / NUM_DIRECTIONS);
            directionVectors[i] = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        }

        SetupChildTouchDetectors();
    }

    void Start()
    {
        FindMainCamera();
        FindPlayer();
        FindStrangerInCameraObject();
        Unfreeze();
    }

    private void SetupChildTouchDetectors()
    {
        Collider[] childCols = GetComponentsInChildren<Collider>(true);
        foreach (var col in childCols)
        {
            StrangerTouchDetector st = col.GetComponent<StrangerTouchDetector>();
            if (st == null) st = col.gameObject.AddComponent<StrangerTouchDetector>();
            st.Init(this);
        }
    }

    void FindMainCamera()
    {
        // 1. Ưu tiên Camera gắn trên người Player (con của MovePl hoặc Main)
        if (player != null)
        {
            Camera playerCam = player.GetComponentInChildren<Camera>();
            if (playerCam != null && playerCam.enabled && playerCam.gameObject.activeInHierarchy)
            {
                mainCam = playerCam;
                return;
            }
        }

        // 2. Camera.main
        if (Camera.main != null && Camera.main.enabled && Camera.main.gameObject.activeInHierarchy)
        {
            mainCam = Camera.main;
            return;
        }

        // 3. Camera bất kỳ đang active
        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in cams)
        {
            if (c.enabled && c.gameObject.activeInHierarchy)
            {
                mainCam = c;
                return;
            }
        }
    }

    void FindPlayer()
    {
        if (player != null) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            MovePl movePl = Object.FindFirstObjectByType<MovePl>();
            if (movePl != null) player = movePl.transform;
        }
    }

    void Update()
    {
        if (hasCaughtPlayer) return;

        // 1. NẾU PLAYER VỪA RỜI ZONE: ĐỨNG NHÌN THEO PLAYER 1 LÚC TRƯỚC KHI VỀ LẠI CHỖ CŨ
        if (isWaitingToReset)
        {
            exitZoneTimer += Time.deltaTime;
            
            // Xoay mặt nhìn theo Player
            if (player != null)
            {
                Vector3 lookDir = (player.position - transform.position);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion yawRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                    transform.rotation = yawRot * Quaternion.Euler(modelRotationOffset);
                }
            }

            if (exitZoneTimer >= stareDurationBeforeReset)
            {
                isWaitingToReset = false;
                ResetToSpawn();
            }
            return;
        }

        if (isPermanentlyFrozen)
        {
            Freeze();
            return;
        }

        if (!isPlayerInZone || player == null) return;

        UpdateStuckDetection();
        ChasePlayer();
        CheckCatch();
    }

    // ==================== ZONE API (gọi bởi StrangerDangerZone) ====================

    public void OnPlayerEnteredZone(Transform playerTrans, Collider zoneCol)
    {
        isWaitingToReset = false;
        exitZoneTimer = 0f;
        isPlayerInZone = true;
        hasCaughtPlayer = false;
        player = playerTrans;
        boundZoneCollider = zoneCol;
        lastPosition = transform.position;
        stuckTimer = 0f;
        stuckEscapeTimer = 0f;
        FindMainCamera();
        if (!isPermanentlyFrozen) Unfreeze();
        Debug.Log("[StrangerBehavior] 👁️ Stranger đã thức tỉnh!");
    }

    public void OnPlayerExitedZone()
    {
        isPlayerInZone = false;
        isWaitingToReset = true;
        exitZoneTimer = 0f;
        Freeze();
        Debug.Log("[StrangerBehavior] 🛖 Player đã thoát khỏi Zone! Stranger đứng nhìn theo trước khi quay về...");
    }

    public void ResetToSpawn()
    {
        if (characterController != null && characterController.enabled) characterController.enabled = false;
        transform.position = initialSpawnPosition;
        transform.rotation = initialSpawnRotation;
        if (characterController != null) characterController.enabled = true;

        lastPosition = initialSpawnPosition;
        stuckTimer = 0f;
        stuckEscapeTimer = 0f;
        currentMoveDirection = transform.forward;
        Freeze();
    }

    // ==================== CHỚP SÁNG ĐÈN PIN (FLASH BURST STUN) ====================

    /// <summary>
    /// Được gọi từ FlashlightToggle khi người chơi bấm Chuột Phải chớp đèn pin làm chói quái
    /// </summary>
    public void OnCameraFlashStunned()
    {
        if (isPermanentlyFrozen || hasCaughtPlayer) return;

        isPermanentlyFrozen = true;
        isFrozen = true;
        Freeze();

        Debug.Log($"<color=yellow><b>[StrangerBehavior] ⚡ {gameObject.name} ĐÃ BỊ CHỚP ĐÈN PIN LÀM CHÓI MẮT! HÓA ĐÁ BẤT ĐỘNG VĨNH VIỄN!</b></color>");

        if (creepSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(creepSound, 0.9f);
        }

        // Báo cho StrangerMinigameManager tăng số lượng tượng đã phong ấn & spawn con tiếp theo
        if (minigameManager != null)
        {
            minigameManager.OnStrangerFrozen(this);
        }
    }

    private void Freeze()
    {
        isFrozen = true;
        if (anim != null) anim.speed = 0f;
    }

    private void Unfreeze()
    {
        if (isPermanentlyFrozen) return; // Đã hóa đá vĩnh viễn thì không bao giờ rã đông
        isFrozen = false;
        if (anim != null) anim.speed = 1f;
    }

    // ==================== PHÁT HIỆN BỊ KẸT ====================

    private void UpdateStuckDetection()
    {
        float movedDist = Vector3.Distance(transform.position, lastPosition);

        if (movedDist < 0.05f)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    // ==================== TRUY ĐUỔI: CONTEXT STEERING 360° ====================

    private void ChasePlayer()
    {
        // 1. TÍNH HƯỚNG MONG MUỐN NHẮM VÀO PLAYER
        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) return;
        Vector3 desiredDir = toPlayer.normalized;

        // 2. TÍNH HƯỚNG DI CHUYỂN TỐI ƯU BẰNG CONTEXT STEERING
        Vector3 finalDir = ComputeContextSteering(desiredDir);

        // 3. CHUYỂN HƯỚNG MỀM MẠI
        currentMoveDirection = Vector3.Slerp(currentMoveDirection, finalDir, Time.deltaTime * steerSmoothSpeed).normalized;

        // 4. DI CHUYỂN & RƠI NHANH XUỐNG ĐẤT
        if (characterController != null && characterController.enabled)
        {
            if (characterController.isGrounded)
            {
                verticalVelocity = -4f; // Ép chân bám chặt xuống đất
            }
            else
            {
                verticalVelocity -= fallSpeed * Time.deltaTime; // Rơi nhanh như tên bắn
            }

            Vector3 moveVec = currentMoveDirection * fastSpeed;
            moveVec.y = verticalVelocity;
            characterController.Move(moveVec * Time.deltaTime);
        }
        else
        {
            Vector3 nextPos = transform.position + currentMoveDirection * fastSpeed * Time.deltaTime;

            RaycastHit[] hits = Physics.RaycastAll(nextPos + Vector3.up * 100f, Vector3.down, 200f);
            float bestY = -9999f;
            bool foundGround = false;
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("Player") || hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.isTrigger) continue;
                if (hit.point.y > bestY) { bestY = hit.point.y; foundGround = true; }
            }

            float targetY = foundGround ? bestY + groundOffset : player.position.y + groundOffset;
            nextPos.y = Mathf.MoveTowards(transform.position.y, targetY, fallSpeed * Time.deltaTime);

            transform.position = nextPos;
        }

        // 5. GIỚI HẠN VÙNG ZONE
        if (clampToZoneBounds && boundZoneCollider != null)
        {
            Bounds bounds = boundZoneCollider.bounds;
            Vector3 pos = transform.position;
            Vector3 clamped = pos;
            clamped.x = Mathf.Clamp(clamped.x, bounds.min.x, bounds.max.x);
            clamped.z = Mathf.Clamp(clamped.z, bounds.min.z, bounds.max.z);
            if (clamped != pos)
            {
                if (characterController != null && characterController.enabled)
                {
                    characterController.enabled = false;
                    transform.position = clamped;
                    characterController.enabled = true;
                }
                else
                {
                    transform.position = clamped;
                }
            }
        }

        // 6. XOAY MẶT VỀ HƯỚNG DI CHUYỂN (KHÔNG NGHIÊNG)
        Vector3 lookDir = currentMoveDirection;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion yawRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            transform.rotation = yawRot * Quaternion.Euler(modelRotationOffset);
        }
    }

    /// <summary>
    /// CONTEXT STEERING: Quét 16 hướng 360°, tính điểm hứng thú / nguy hiểm, chọn hướng tối ưu.
    /// Khi bị kẹt: Chuyển sang pha thoát kẹt cưỡng chế — chạy theo hướng thoáng nhất bất kể Player ở đâu.
    /// </summary>
    private Vector3 ComputeContextSteering(Vector3 desiredDir)
    {
        Vector3 origin = transform.position + Vector3.up * sensorHeight;

        // ====== PHA THOÁT KẸT CƯỠNG CHẾ (ESCAPE MODE) ======
        // Kẹt > 0.3s: Bật chế độ thoát kẹt trong 0.6s, quét 360° tìm hướng xa vật cản nhất
        if (stuckTimer >= 0.3f && stuckEscapeTimer <= 0f)
        {
            stuckEscapeTimer = 0.6f;
            escapeDirection = FindBestEscapeDirection(origin);
        }

        if (stuckEscapeTimer > 0f)
        {
            stuckEscapeTimer -= Time.deltaTime;
            return escapeDirection;
        }

        // ====== CHẾ ĐỘ BÌNH THƯỜNG: CONTEXT STEERING ======

        float bestScore = float.MinValue;
        int bestIndex = 0;

        for (int i = 0; i < NUM_DIRECTIONS; i++)
        {
            Vector3 dir = directionVectors[i];

            // --- INTEREST: Hướng nào gần Player hơn thì điểm cao hơn ---
            float interest = Vector3.Dot(dir, desiredDir); // -1 (ngược Player) → +1 (về phía Player)
            interest = Mathf.Clamp01((interest + 1f) * 0.5f); // Chuyển thành 0 → 1
            interest *= interest; // Ưu tiên mạnh hơn các hướng gần Player

            // --- DANGER: Quét vật cản bằng SphereCast ---
            float danger = 0f;
            if (Physics.SphereCast(origin, sensorRadius, dir, out RaycastHit hit, sensorDistance, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                // Bỏ qua Player, chính mình và các đối tượng thuộc Fence/Rope nếu có
                if (!hit.collider.CompareTag("Player") && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
                {
                    // Vật cản càng gần thì danger càng cao (0 = xa nhất, 1 = ngay sát)
                    danger = 1f - Mathf.Clamp01(hit.distance / sensorDistance);
                    danger *= danger; // Tăng mạnh nguy hiểm khi rất gần
                }
            }

            // --- TỔNG ĐIỂM: interest cao + danger thấp = tốt nhất ---
            float score = interest - danger * 2.0f;

            // Thưởng thêm cho hướng tiếp nối chuyển động hiện tại (tránh giật lái 180° liên tục)
            float momentum = Vector3.Dot(dir, currentMoveDirection);
            score += Mathf.Clamp01((momentum + 1f) * 0.5f) * 0.15f;

            interestMap[i] = interest;
            dangerMap[i] = danger;

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        // Lấy trung bình trọng số của hướng tốt nhất và 2 hướng kề bên để chuyển hướng mềm mại hơn
        int prevIdx = (bestIndex - 1 + NUM_DIRECTIONS) % NUM_DIRECTIONS;
        int nextIdx = (bestIndex + 1) % NUM_DIRECTIONS;

        float wBest = 1.0f;
        float wPrev = Mathf.Max(0f, interestMap[prevIdx] - dangerMap[prevIdx]);
        float wNext = Mathf.Max(0f, interestMap[nextIdx] - dangerMap[nextIdx]);
        float totalW = wBest + wPrev + wNext;

        Vector3 blended = (directionVectors[bestIndex] * wBest +
                           directionVectors[prevIdx] * wPrev +
                           directionVectors[nextIdx] * wNext) / totalW;
        blended.y = 0f;

        return blended.sqrMagnitude > 0.001f ? blended.normalized : desiredDir;
    }

    /// <summary>
    /// Quét toàn bộ 360° để tìm hướng xa vật cản nhất (dùng khi bị kẹt cứng trong góc)
    /// </summary>
    private Vector3 FindBestEscapeDirection(Vector3 origin)
    {
        float bestDist = -1f;
        Vector3 bestDir = currentMoveDirection;

        // Quét 36 hướng (mỗi 10°) để tìm lối thoát chính xác nhất
        for (int i = 0; i < 36; i++)
        {
            float angle = i * 10f;
            Vector3 testDir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            if (Physics.SphereCast(origin, sensorRadius, testDir, out RaycastHit hit, sensorDistance * 2f, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.CompareTag("Player") || hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    // Player hoặc chính mình: coi như thoáng
                    if (sensorDistance * 2f > bestDist)
                    {
                        bestDist = sensorDistance * 2f;
                        bestDir = testDir;
                    }
                    continue;
                }

                if (hit.distance > bestDist)
                {
                    bestDist = hit.distance;
                    bestDir = testDir;
                }
            }
            else
            {
                // Hoàn toàn thoáng! Chọn luôn hướng này
                bestDist = sensorDistance * 2f;
                bestDir = testDir;
            }
        }

        return bestDir;
    }

    public void InitializeForMinigame(Transform targetPlayer, StrangerMinigameManager manager, float speed)
    {
        player = targetPlayer;
        minigameManager = manager;
        fastSpeed = speed;
        isPlayerInZone = true;
        permanentFreezeOnLook = true;
        isPermanentlyFrozen = false;
        hasCaughtPlayer = false;
        clampToZoneBounds = false;
        isWaitingToReset = false;

        FindMainCamera();
        Unfreeze();
    }

    // ==================== BẮT NGƯỜI CHƠI (CATCH & IN-CAMERA JUMPSCARE) ====================

    private void CheckCatch()
    {
        if (isPermanentlyFrozen || hasCaughtPlayer) return;
        if (player == null) return;

        // Dùng cả khoảng cách 2D phẳng XZ và 3D
        Vector2 strangerFlat = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerFlat = new Vector2(player.position.x, player.position.z);
        float flatDist = Vector2.Distance(strangerFlat, playerFlat);
        float realDist = Vector3.Distance(transform.position, player.position);

        if (flatDist <= killDistance || realDist <= killDistance + groundOffset)
        {
            CatchPlayer();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCaughtPlayer || isPermanentlyFrozen) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            CatchPlayer();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCaughtPlayer || isPermanentlyFrozen) return;
        if (collision.collider.CompareTag("Player") || collision.collider.GetComponentInParent<MovePl>() != null)
        {
            CatchPlayer();
        }
    }

    public void CatchPlayer()
    {
        if (hasCaughtPlayer || isPermanentlyFrozen) return;
        hasCaughtPlayer = true;

        Debug.Log("<color=red><b>[StrangerBehavior] 💀 Stranger đã tóm trúng người chơi -> Kích hoạt In-Camera Jumpscare!</b></color>");

        // 1. Ẩn con Stranger ngoài thế giới
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = false;

        Collider[] cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = false;

        if (characterController != null) characterController.enabled = false;

        // 2. Chạy Coroutine Jumpscare trong Camera
        StartCoroutine(StrangerCameraJumpscareRoutine());
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

    private IEnumerator StrangerCameraJumpscareRoutine()
    {
        if (strangerInCameraObject == null) FindStrangerInCameraObject();

        // 1. Khóa di chuyển và góc xoay của người chơi
        MovePl playerMove = Object.FindFirstObjectByType<MovePl>();
        if (playerMove != null)
        {
            playerMove.isCameraLocked = true;
            playerMove.enabled = false;
        }

        // 2. Ẩn toàn bộ UI / HUD trong lúc bị jumpscare
        List<Canvas> hiddenCanvases = new List<Canvas>();
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

        if (strangerInCameraObject != null)
        {
            strangerInCameraObject.SetActive(true);

            // Bật renderer trên Stranger in-camera
            Renderer[] rends = strangerInCameraObject.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends) r.enabled = true;

            // Bật Animator trên Stranger in-camera
            Animator inCamAnim = strangerInCameraObject.GetComponent<Animator>() ?? strangerInCameraObject.GetComponentInChildren<Animator>();
            if (inCamAnim != null)
            {
                inCamAnim.enabled = true;
                inCamAnim.speed = 1.0f;
                inCamAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (inCamAnim.runtimeAnimatorController != null)
                {
                    inCamAnim.Play(0, 0, 0f);
                }
            }

            // Phát âm thanh jumpscare
            AudioClip soundToPlay = catchJumpscareSound;
            if (soundToPlay != null)
            {
                AudioSource camAudio = strangerInCameraObject.GetComponent<AudioSource>();
                if (camAudio == null) camAudio = strangerInCameraObject.AddComponent<AudioSource>();
                camAudio.spatialBlend = 0f;
                camAudio.PlayOneShot(soundToPlay, 1.0f);
            }

            // 1. Cho animation chạy đúng tới đoạn há mồm (animMouthOpenStopTime = 4.8s / frame 288)
            float runDuration = animMouthOpenStopTime;
            if (camToShake != null && cameraShakeIntensity > 0.001f)
            {
                StartCoroutine(CameraShakeRoutine(camToShake, runDuration, cameraShakeIntensity));
            }

            // Chờ animation chạy đến đoạn há mồm
            yield return new WaitForSeconds(runDuration);

            // 2. DỪNG ĐỨNG YÊN ANIMATION (speed = 0) ĐỂ KHÓA DÁNG HÁ MỒM HÙ DỌA RÙNG RỢN
            if (inCamAnim != null)
            {
                inCamAnim.speed = 0f;
            }

            // 3. GIỮ NGUYÊN DÁNG HÁ MỒM ĐÓ TRONG 1.0 GIÂY
            if (holdMouthOpenDuration > 0f)
            {
                yield return new WaitForSeconds(holdMouthOpenDuration);
            }

            // KIỂM TRA CHẾ ĐỘ BẤT TỬ (GOD MODE PHÍM M)
            if (GodModeManager.IsGodModeActive)
            {
                Debug.Log("<color=cyan><b>[StrangerBehavior] 👑 GodMode đang BẬT -> Tự động thả tự do di chuyển cho Player đi tiếp!</b></color>");

                // Tắt Stranger trong Camera
                if (strangerInCameraObject != null)
                {
                    strangerInCameraObject.SetActive(false);
                }

                // 1. Mở lại điều khiển Player
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

                // 3. Nếu đang trong minigame, thông báo cho manager
                if (minigameManager != null)
                {
                    minigameManager.OnStrangerFrozen(this);
                }

                Destroy(gameObject);
                yield break;
            }

            // 4. SAU 1S -> BẮT ĐẦU FADE ĐEN TOÀN MÀN HÌNH (STRANGER VẪN HÁ MỒM CHÌM DẦN VÀO BÓNG TỐI)
            yield return StartCoroutine(FadeToBlackRoutine(fadeToBlackDuration));

            // Tắt Stranger sau khi màn hình đã đen hoàn toàn
            if (strangerInCameraObject != null)
            {
                strangerInCameraObject.SetActive(false);
            }
        }
        else
        {
            if (catchJumpscareSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(catchJumpscareSound, 1.0f);
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

                if (minigameManager != null)
                {
                    minigameManager.OnStrangerFrozen(this);
                }

                Destroy(gameObject);
                yield break;
            }

            yield return StartCoroutine(FadeToBlackRoutine(1.0f));
        }

        // 6. MÀN HÌNH ĐEN HOÀN TOÀN -> NHẬN PHÍM/CHUỘT BẤT KỲ ĐỂ QUAY VỀ MENU
        yield return StartCoroutine(WaitForClickAndReturnToMenuRoutine());

        gameObject.SetActive(false);
    }

    private IEnumerator FadeToBlackRoutine(float duration)
    {
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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        bool hasClicked = false;
        while (!hasClicked)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.anyKeyDown)
            {
                hasClicked = true;
            }
            yield return null;
        }

        Debug.Log("[StrangerBehavior] 🔄 Đã nhấn nút -> Quay trở về MainMenu...");
        SceneManager.LoadScene("MainMenu");
    }

    private void FindStrangerInCameraObject()
    {
        if (strangerInCameraObject != null) return;

        // 1. Tìm trong Jumpscare parent
        GameObject jumpscareParent = GameObject.Find("Jumpscare");
        if (jumpscareParent != null)
        {
            Transform sTrans = jumpscareParent.transform.Find("Stranger");
            if (sTrans != null)
            {
                strangerInCameraObject = sTrans.gameObject;
                return;
            }
        }

        // 2. Tìm trong con của bất kỳ Camera nào
        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cams)
        {
            Transform[] children = cam.GetComponentsInChildren<Transform>(true);
            foreach (var t in children)
            {
                if (t.name == "Stranger" && t != transform && !t.IsChildOf(transform))
                {
                    strangerInCameraObject = t.gameObject;
                    return;
                }
            }
        }
    }

    // ==================== GIZMOS TRỰC QUAN TRONG SCENE VIEW ====================

    void OnDrawGizmosSelected()
    {
        if (directionVectors == null || directionVectors.Length < NUM_DIRECTIONS)
        {
            directionVectors = new Vector3[NUM_DIRECTIONS];
            for (int j = 0; j < NUM_DIRECTIONS; j++)
            {
                float angle = j * (360f / NUM_DIRECTIONS);
                directionVectors[j] = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            }
        }

        Vector3 origin = transform.position + Vector3.up * sensorHeight;

        for (int i = 0; i < NUM_DIRECTIONS; i++)
        {
            float interest = (interestMap != null && i < interestMap.Length) ? interestMap[i] : 0f;
            float danger = (dangerMap != null && i < dangerMap.Length) ? dangerMap[i] : 0f;

            // Xanh = an toàn & hướng tốt, Đỏ = nguy hiểm & vướng vật cản
            Gizmos.color = Color.Lerp(Color.green, Color.red, danger);

            float lineLength = Mathf.Lerp(0.5f, sensorDistance, interest);
            Gizmos.DrawRay(origin, directionVectors[i] * lineLength);
        }

        // Hiển thị hướng di chuyển hiện tại bằng tia xanh dương đậm
        Gizmos.color = Color.cyan;
        Vector3 moveDir = (currentMoveDirection != Vector3.zero) ? currentMoveDirection : transform.forward;
        Gizmos.DrawRay(origin, moveDir * sensorDistance * 1.2f);
    }
}

/// <summary>
/// Component tự động gắn vào tất cả các collider con của Stranger để phát hiện va chạm với người chơi
/// </summary>
public class StrangerTouchDetector : MonoBehaviour
{
    private StrangerBehavior stranger;

    public void Init(StrangerBehavior owner)
    {
        stranger = owner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (stranger == null) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            stranger.CatchPlayer();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (stranger == null) return;
        if (collision.collider.CompareTag("Player") || collision.collider.GetComponentInParent<MovePl>() != null)
        {
            stranger.CatchPlayer();
        }
    }
}
