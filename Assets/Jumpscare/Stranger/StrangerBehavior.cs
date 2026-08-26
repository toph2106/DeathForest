using UnityEngine;

/// <summary>
/// Quản lý hành vi quái 'Stranger' (Người Lạ) trong Map 03:
/// 1. Cơ chế Weeping Angel: Khi bị nhìn -> Freeze, khi quay lưng -> Lướt siêu nhanh.
/// 2. Context Steering AI (Bản đồ hứng thú/nguy hiểm 360°):
///    Quét 16 hướng xung quanh, tính điểm hứng thú (gần Player) và nguy hiểm (gần vật cản),
///    chọn hướng tối ưu nhất để lách qua mọi địa hình phức tạp.
/// 3. Anti-Stuck cưỡng chế: Kẹt > 0.3s -> Quét 360° tìm hướng thoáng nhất, thoát kẹt rồi mới quay lại đuổi.
/// </summary>
public class StrangerBehavior : MonoBehaviour
{
    [Header("1. Cấu Hình Tốc Độ & Bắt Người Chơi")]
    [Tooltip("Tốc độ trượt siêu nhanh khi bị khuất tầm nhìn (Mặc định: 60 - 80)")]
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

    [Header("6. Chế Độ Mini-game (Phong Ấn Vĩnh Viễn)")]
    [Tooltip("Bật nếu muốn Stranger hóa đá vĩnh viễn ngay khi bị nhìn trúng lần đầu")]
    public bool permanentFreezeOnLook = false;
    public bool isPermanentlyFrozen = false;
    [HideInInspector] public StrangerMinigameManager minigameManager;

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
    }

    void Start()
    {
        FindMainCamera();
        FindPlayer();
        Freeze();
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

        if (isPermanentlyFrozen) return;
        if (!isPlayerInZone || player == null) return;

        if (mainCam == null || !mainCam.enabled || !mainCam.gameObject.activeInHierarchy)
        {
            FindMainCamera();
            if (mainCam == null) return;
        }

        CheckIfBeingLookedAt();

        if (!isFrozen && !isPermanentlyFrozen)
        {
            UpdateStuckDetection();
            ChasePlayer();
        }

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

    // ==================== WEEPING ANGEL: NHÌN = ĐÔNG CỨNG ====================

    private void CheckIfBeingLookedAt()
    {
        if (isPermanentlyFrozen) return;

        if (mainCam == null || !mainCam.enabled || !mainCam.gameObject.activeInHierarchy)
        {
            FindMainCamera();
            if (mainCam == null) return;
        }

        Vector3 camPos = mainCam.transform.position;

        // Quét 3 điểm: Đầu, Ngực, và Thân
        Vector3 headPoint = transform.position + Vector3.up * 1.8f;
        Vector3 chestPoint = transform.position + Vector3.up * 1.0f;
        Vector3 centerPoint = transform.position + Vector3.up * 0.4f;

        bool isSeen = IsPointVisible(headPoint, camPos) ||
                      IsPointVisible(chestPoint, camPos) ||
                      IsPointVisible(centerPoint, camPos);

        if (isSeen)
        {
            Freeze();

            // NẾU ĐANG Ở CHẾ ĐỘ MINIGAME: HÓA ĐÁ VĨNH VIỄN VÀ BÁO CHO MANAGER
            if (permanentFreezeOnLook && !isPermanentlyFrozen)
            {
                isPermanentlyFrozen = true;
                Debug.Log($"[StrangerBehavior] 🗿 {gameObject.name} ĐÃ BỊ NHÌN TRÚNG! HÓA ĐÁ VĨNH VIỄN TẠI CHỖ!");
                if (minigameManager != null)
                {
                    minigameManager.OnStrangerFrozen(this);
                }
            }
        }
        else
        {
            Unfreeze();
        }
    }

    private bool IsPointVisible(Vector3 point, Vector3 camPos)
    {
        Vector3 viewportPoint = mainCam.WorldToViewportPoint(point);

        bool onScreen = viewportPoint.z > 0 &&
                        viewportPoint.x > -0.05f && viewportPoint.x < 1.05f &&
                        viewportPoint.y > -0.05f && viewportPoint.y < 1.05f;

        if (!onScreen) return false;

        Vector3 dir = (point - camPos);
        float dist = dir.magnitude;

        if (Physics.Raycast(camPos, dir.normalized, out RaycastHit hit, dist))
        {
            // Nếu tia đụng trúng tường (không phải Stranger và không phải Player)
            if (hit.collider.gameObject != gameObject &&
                !hit.collider.transform.IsChildOf(transform) &&
                !hit.collider.CompareTag("Player") &&
                (player == null || (!hit.collider.transform.IsChildOf(player) && hit.collider.gameObject != player.gameObject)))
            {
                return false;
            }
        }

        return true;
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
            if (Physics.SphereCast(origin, sensorRadius, dir, out RaycastHit hit, sensorDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                // Bỏ qua Player và chính mình
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

            if (Physics.SphereCast(origin, sensorRadius, testDir, out RaycastHit hit, sensorDistance * 2f, ~0, QueryTriggerInteraction.Ignore))
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

    // ==================== BẮT NGƯỜI CHƠI ====================

    private void CheckCatch()
    {
        if (isPermanentlyFrozen || hasCaughtPlayer) return;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= killDistance)
        {
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        if (hasCaughtPlayer || isPermanentlyFrozen) return;

        // Nếu đang trong Minigame và tắt Game Over (Chế độ Test Demo)
        if (minigameManager != null && !minigameManager.enableGameOver)
        {
            isPermanentlyFrozen = true;
            Freeze();
            minigameManager.OnPlayerCaught(this);
            return;
        }

        hasCaughtPlayer = true;

        Debug.Log("💀 STRANGER ĐÃ BẮT ĐƯỢC BẠN TỪ PHÍA SAU!");
        Freeze();

        if (catchJumpscareSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(catchJumpscareSound, 1.0f);
        }

        if (minigameManager != null)
        {
            minigameManager.OnPlayerCaught(this);
            return;
        }

        if (GameOverJumpscareManager.Instance != null)
        {
            GameOverJumpscareManager.Instance.TriggerGameOver();
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
