using UnityEngine;

/// <summary>
/// Quản lý hành vi quái 'Yoshie' (Mặt Quỷ Bay) trong Map 03:
/// 1. Săn đuổi Player liên tục trong rừng.
/// 2. Khi Player bước vào Vùng An Toàn (SafeZone):
///    - Yoshie vẫn tiếp tục đuổi theo đến khi chạm tới RANH GIỚI mép SafeZone.
///    - BỊ CHẶN LẠI NGOÀI RANH GIỚI (tuyệt đối không được phép đi vào trong SafeZone).
///    - Đứng trừng trừng nhìn Player trong stareDuration (3.5s).
///    - Sau đó quay đầu bỏ đi về vị trí mặc định (defaultHomePoint hoặc vị trí Spawn ban đầu).
/// 3. Khi đã về đến vị trí mặc định: Yoshie đứng canh giữ ở đó.
/// 4. Khi Player RỜI KHỎI SafeZone: Yoshie mới tiếp tục săn đuổi trở lại!
/// </summary>
public class YoshieBehavior : MonoBehaviour
{
    public enum YoshieState
    {
        Chasing,            // Đang săn đuổi Player
        StaringAtSafeZone,  // Đứng nhìn chằm chằm Player ở ngoài ranh giới SafeZone
        RetreatingHome,     // Đang bay bỏ đi về vị trí mặc định
        IdleAtHome          // Đã về đến chỗ mặc định, đứng chờ
    }

    [Header("1. AI Settings (Cấu Hình Săn Đuổi)")]
    [Tooltip("Tốc độ bay đuổi theo Player của Yoshie")]
    public float moveSpeed = 50f;

    [Tooltip("Tốc độ bay bỏ đi về vị trí mặc định")]
    public float retreatSpeed = 40f;

    [Tooltip("Khoảng cách kích hoạt Game Over / Bị bắt (khi ở ngoài SafeZone)")]
    public float killDistance = 3f;

    [Tooltip("Độ cao bay lơ lửng so với mặt đất")]
    public float hoverHeightOffset = 3f;

    [Tooltip("Góc xoay bù trừ để dựng đứng mô hình (Mặc định: -90, 0, 0)")]
    public Vector3 modelRotationOffset = new Vector3(-90f, 0f, 0f);

    [Header("2. Cấu Hình SafeZone (Vùng An Toàn)")]
    [Tooltip("Điểm mốc mặc định Yoshie sẽ bỏ đi về đó (Để trống sẽ tự động lấy vị trí Spawn ban đầu)")]
    public Transform defaultHomePoint;

    [Tooltip("Khoảng cách Yoshie dừng lại khi tiếp cận ranh giới SafeZone")]
    public float safeZoneStopDistance = 3.5f;

    [Tooltip("Thời gian Yoshie đứng trừng trừng nhìn Player ở SafeZone trước khi bỏ đi (giây - Mặc định: 3.5s)")]
    public float stareDurationAtSafeZone = 3.5f;

    [Header("3. Âm Thanh (Audio Sfx)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh gầm gừ / thở dốc khi đứng nhìn ở ranh giới SafeZone")]
    public AudioClip stareSound;
    [Tooltip("Âm thanh ma quái khi quay lưng bỏ đi")]
    public AudioClip retreatSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("4. Trạng Thái Hiện Tại (State)")]
    public YoshieState currentState = YoshieState.Chasing;
    public bool isPlayerInSafeZone = false;

    // --- Private Variables ---
    private Transform player;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private float stareTimer = 0f;
    private Collider activeSafeZoneCollider;

    void Awake()
    {
        // 1. Tự động lưu lại vị trí Spawn ban đầu
        homePosition = (defaultHomePoint != null) ? defaultHomePoint.position : transform.position;
        homeRotation = (defaultHomePoint != null) ? defaultHomePoint.rotation : transform.rotation;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (stareSound != null || retreatSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D Sound
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        FindPlayer();
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
            if (movePl != null)
            {
                player = movePl.transform;
            }
            else
            {
                Debug.LogError("Yoshie không tìm thấy Player!");
            }
        }
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        // Cập nhật vị trí Home nếu có defaultHomePoint
        if (defaultHomePoint != null)
        {
            homePosition = defaultHomePoint.position;
        }

        switch (currentState)
        {
            case YoshieState.Chasing:
                UpdateChasing();
                break;

            case YoshieState.StaringAtSafeZone:
                UpdateStaring();
                break;

            case YoshieState.RetreatingHome:
                UpdateRetreating();
                break;

            case YoshieState.IdleAtHome:
                UpdateIdleAtHome();
                break;
        }
    }

    // =========================================================================
    // 1. TRẠNG THÁI SĂN ĐUỔI (CHASING)
    // =========================================================================
    private void UpdateChasing()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // NẾU PLAYER ĐANG Ở TRONG SAFEZONE:
        if (isPlayerInSafeZone)
        {
            // Kiểm tra khoảng cách tới mép ranh giới của SafeZone
            if (activeSafeZoneCollider != null)
            {
                Vector3 closestBoundaryPoint = activeSafeZoneCollider.ClosestPoint(transform.position);
                float distToBoundary = Vector3.Distance(transform.position, closestBoundaryPoint);

                // Nếu đã chạm hoặc cách mép ranh giới dưới 1.5m -> BỊ CHẶN LẠI NGOÀI RANH GIỚI
                if (distToBoundary <= 1.5f || distToPlayer <= safeZoneStopDistance)
                {
                    OnHitSafeZoneBarrier();
                    return;
                }
            }
            else if (distToPlayer <= safeZoneStopDistance)
            {
                OnHitSafeZoneBarrier();
                return;
            }
        }
        else
        {
            // NẾU PLAYER Ở NGOÀI SAFEZONE -> KIỂM TRA BẮT ĐƯỢC PLAYER (GAME OVER)
            if (distToPlayer <= killDistance)
            {
                CatchPlayer();
                return;
            }
        }

        // Bay lao về phía Player
        ChasePlayer();
    }

    /// <summary>
    /// Được gọi khi Yoshie chạm vào ranh giới ngoài của SafeZone -> Dừng lại và đứng nhìn
    /// </summary>
    public void OnHitSafeZoneBarrier()
    {
        if (currentState == YoshieState.StaringAtSafeZone || currentState == YoshieState.RetreatingHome) return;

        currentState = YoshieState.StaringAtSafeZone;
        stareTimer = 0f;

        if (stareSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(stareSound, soundVolume);
        }

        Debug.Log("[YoshieBehavior] ⛩️ Yoshie bị ranh giới SafeZone chặn lại! Đang đứng nhìn trừng trừng...");
    }

    // =========================================================================
    // 2. TRẠNG THÁI ĐỨNG NHÌN Ở SAFEZONE (STARING)
    // =========================================================================
    private void UpdateStaring()
    {
        // 1. Nếu Player bất ngờ chạy ra khỏi SafeZone trong lúc Yoshie đang đứng nhìn -> Tiếp tục rượt đuổi ngay
        if (!isPlayerInSafeZone)
        {
            currentState = YoshieState.Chasing;
            Debug.Log("[YoshieBehavior] 👹 Player vừa rời khỏi SafeZone! Yoshie tiếp tục lao tới rượt đuổi!");
            return;
        }

        // 2. Đứng yên tại chỗ ngoài ranh giới, mặt luôn xoay trừng trừng nhìn Player
        RotateTowardsPlayer();

        // 3. Đếm thời gian đứng nhìn
        stareTimer += Time.deltaTime;
        if (stareTimer >= stareDurationAtSafeZone)
        {
            // Hết giờ đứng nhìn -> Bắt đầu quay đầu bỏ đi về chỗ mặc định
            currentState = YoshieState.RetreatingHome;

            if (retreatSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(retreatSound, soundVolume);
            }

            Debug.Log("[YoshieBehavior] 💨 Yoshie đã nhìn xong, quay đầu bỏ đi về vị trí mặc định!");
        }
    }

    // =========================================================================
    // 3. TRẠNG THÁI BAY LÙI VỀ CHỖ MẶC ĐỊNH (RETREATING HOME)
    // =========================================================================
    private void UpdateRetreating()
    {
        // Nếu Player tự tin bước ra khỏi SafeZone -> Yoshie lập tức quay lại rượt đuổi
        if (!isPlayerInSafeZone)
        {
            currentState = YoshieState.Chasing;
            Debug.Log("[YoshieBehavior] 👹 Player ra khỏi SafeZone -> Yoshie quay lại săn đuổi!");
            return;
        }

        // Di chuyển lùi về homePosition
        Vector3 dirToHome = (homePosition - transform.position);
        dirToHome.y = 0f;
        float distToHome = dirToHome.magnitude;

        if (distToHome > 0.8f)
        {
            Vector3 moveDir = dirToHome.normalized;
            Vector3 nextPos = transform.position + moveDir * retreatSpeed * Time.deltaTime;

            // Bám theo địa hình
            nextPos.y = CalculateHoverY(nextPos, homePosition.y);
            transform.position = nextPos;
        }
        else
        {
            // Đã về đến nhà -> Đứng chờ tại chỗ
            transform.position = Vector3.Lerp(transform.position, homePosition, 5f * Time.deltaTime);
            currentState = YoshieState.IdleAtHome;
            Debug.Log("[YoshieBehavior] 🏡 Yoshie đã lùi về đến vị trí mặc định an toàn!");
        }

        // MẶT LUÔN HƯỚNG NHÌN TRỪNG TRỪNG VỀ PHÍA PLAYER (VỪA NHÌN VỪA BAY LÙI)
        RotateTowardsPlayer();
    }

    // =========================================================================
    // 4. TRẠNG THÁI ĐỨNG CHỜ Ở VỊ TRÍ MẶC ĐỊNH (IDLE AT HOME)
    // =========================================================================
    private void UpdateIdleAtHome()
    {
        // Giữ vị trí và xoay mặt hướng nhẹ về phía Player từ xa
        RotateTowardsPlayer();

        // Nếu Player rời khỏi SafeZone -> Yoshie từ chỗ mặc định bắt đầu bay ra săn đuổi lại
        if (!isPlayerInSafeZone)
        {
            currentState = YoshieState.Chasing;
            Debug.Log("[YoshieBehavior] 👹 Player đã rời khỏi SafeZone! Yoshie từ chỗ mặc định xuất kích săn đuổi!");
        }
    }

    // =========================================================================
    // CÁC HÀM XỬ LÝ DI CHUYỂN & GÓC NHÌN
    // =========================================================================

    private void ChasePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position);
        directionToPlayer.y = 0f; 
        directionToPlayer.Normalize();

        Vector3 nextPos = transform.position + directionToPlayer * moveSpeed * Time.deltaTime;
        nextPos.y = CalculateHoverY(nextPos, player.position.y);
        transform.position = nextPos;

        RotateTowardsPlayer();
    }

    private float CalculateHoverY(Vector3 checkPos, float fallbackY)
    {
        RaycastHit[] hits = Physics.RaycastAll(checkPos + Vector3.up * 50f, Vector3.down, 100f);
        float bestY = -9999f;
        bool foundGround = false;

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Player") || hit.collider.gameObject == gameObject) continue;
            if (hit.collider.isTrigger) continue;

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            return Mathf.Lerp(transform.position.y, bestY + hoverHeightOffset, 5f * Time.deltaTime);
        }
        else
        {
            return Mathf.Lerp(transform.position.y, fallbackY + hoverHeightOffset, 5f * Time.deltaTime);
        }
    }

    private void RotateTowardsPlayer()
    {
        if (player == null) return;
        Vector3 horizontalLook = (player.position - transform.position);
        horizontalLook.y = 0f;

        if (horizontalLook.sqrMagnitude > 0.001f)
        {
            Quaternion yawRot = Quaternion.LookRotation(horizontalLook.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, yawRot * Quaternion.Euler(modelRotationOffset), Time.deltaTime * 10f);
        }
    }

    private void CatchPlayer()
    {
        Debug.Log("💀 YOSHIE ĐÃ BẮT ĐƯỢC BẠN!");
        this.enabled = false; 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<YoshieSafeZone>() != null || (activeSafeZoneCollider != null && other == activeSafeZoneCollider))
        {
            if (isPlayerInSafeZone)
            {
                OnHitSafeZoneBarrier();
            }
        }
    }

    // =========================================================================
    // GỌI TỪ SAFEZONE TRIGGER & STRANGER DANGER ZONE
    // =========================================================================

    public void SetPlayerInSafeZone(bool inSafeZone, Collider zoneCollider = null)
    {
        isPlayerInSafeZone = inSafeZone;
        if (zoneCollider != null) activeSafeZoneCollider = zoneCollider;
    }

    public void OnPlayerEnteredStrangerZone()
    {
        SetPlayerInSafeZone(true, null);
    }

    public void OnPlayerExitedStrangerZone()
    {
        SetPlayerInSafeZone(false, null);
    }
}
