using UnityEngine;

/// <summary>
/// Quản lý hành vi quái 'Yoshie' (Mặt Quỷ Bay) trong Map 03:
/// 1. Tự động lưu tọa độ Spawn ban đầu (tại ngôi Miếu) trong Awake().
/// 2. Khi bình thường: Lượn trên không trung săn đuổi người chơi.
/// 3. Khi người chơi bước vào Zone của Stranger:
///    - Dừng săn đuổi.
///    - Vừa quay mặt nhìn người chơi vừa BAY LÙI LẠI về vị trí Spawn ban đầu (ngôi Miếu).
/// 4. Khi người chơi rời khỏi Zone của Stranger:
///    - Tiếp tục truy đuổi người chơi từ vị trí hiện tại.
/// </summary>
public class YoshieBehavior : MonoBehaviour
{
    [Header("1. AI Settings (Cấu Hình Săn Đuổi)")]
    [Tooltip("Tốc độ bay đuổi theo Player của Yoshie")]
    public float moveSpeed = 50f;

    [Tooltip("Tốc độ bay lùi lại về vị trí Spawn ban đầu khi Player vào Zone Stranger")]
    public float retreatSpeed = 45f;
    
    [Tooltip("Khoảng cách kích hoạt Game Over / Bị bắt")]
    public float killDistance = 3f;
    
    [Tooltip("Độ cao bay lơ lửng so với mặt đất")]
    public float hoverHeightOffset = 3f;

    [Tooltip("Góc xoay bù trừ để dựng đứng mô hình (Mặc định: -90, 0, 0)")]
    public Vector3 modelRotationOffset = new Vector3(-90f, 0f, 0f);

    [Header("2. Trạng Thái (State)")]
    [Tooltip("Đang bay lùi lại về Miếu khi Player vào Zone Stranger")]
    public bool isRetreating = false;

    [Header("3. Thời Gian Đứng Nhìn Trước Khi Lùi")]
    [Tooltip("Thời gian Yoshie đứng yên nhìn trừng trừng Player trước khi bắt đầu bay lùi về Miếu (giây - Mặc định: 1.5s)")]
    public float stareDurationBeforeRetreat = 1.5f;

    // --- Private Variables ---
    private Transform player;
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private float retreatTimer = 0f;
    private bool isWaitingToRetreat = false;

    void Awake()
    {
        // 1. Tự động lưu lại vị trí và góc quay Spawn ban đầu tại ngôi Miếu
        initialSpawnPosition = transform.position;
        initialSpawnRotation = transform.rotation;
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

        if (isRetreating)
        {
            // Trạng thái LÙI LẠI về Spawn ban đầu (ngôi Miếu)
            RetreatToSpawn();
        }
        else
        {
            // Trạng thái SĂN ĐUỔI bình thường
            ChasePlayer();
            CheckCatch();
        }
    }

    /// <summary>
    /// Được gọi khi Player bước vào Zone của Stranger: Ra lệnh Yoshie đứng nhìn 1 lúc rồi lùi lại về Spawn
    /// </summary>
    public void OnPlayerEnteredStrangerZone()
    {
        isRetreating = true;
        isWaitingToRetreat = true;
        retreatTimer = 0f;
        Debug.Log("[YoshieBehavior] ⛩️ Player vào Zone Stranger -> Yoshie đứng nhìn chằm chằm trước khi lùi lại!");
    }

    /// <summary>
    /// Được gọi khi Player rời khỏi Zone của Stranger: Ra lệnh Yoshie tiếp tục săn đuổi
    /// </summary>
    public void OnPlayerExitedStrangerZone()
    {
        isRetreating = false;
        isWaitingToRetreat = false;
        retreatTimer = 0f;
        Debug.Log("[YoshieBehavior] 👹 Player ra khỏi Zone Stranger -> Yoshie tiếp tục săn đuổi Player!");
    }

    /// <summary>
    /// Cơ chế bay lùi lại về vị trí Spawn (ngôi Miếu) trong khi vẫn hướng mặt nhìn chằm chằm Player
    /// </summary>
    private void RetreatToSpawn()
    {
        // 1. ĐỨNG YÊN NHÌN TRỪNG TRỪNG PLAYER MỘT LÚC TRƯỚC KHI BẮT ĐẦU LÙI
        if (isWaitingToRetreat)
        {
            retreatTimer += Time.deltaTime;
            RotateTowardsPlayer();

            if (retreatTimer >= stareDurationBeforeRetreat)
            {
                isWaitingToRetreat = false;
            }
            return; // Đứng yên tại chỗ nhìn
        }

        // 2. TÍNH VECTOR VỀ TỌA ĐỘ SPAWN
        Vector3 dirToSpawn = (initialSpawnPosition - transform.position);
        dirToSpawn.y = 0f;
        float distToSpawn = dirToSpawn.magnitude;

        if (distToSpawn > 0.5f)
        {
            Vector3 moveDir = dirToSpawn.normalized;
            Vector3 nextPos = transform.position + moveDir * retreatSpeed * Time.deltaTime;

            // Lướt bám theo địa hình
            RaycastHit[] hits = Physics.RaycastAll(nextPos + Vector3.up * 50f, Vector3.down, 100f);
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
                nextPos.y = Mathf.Lerp(transform.position.y, bestY + hoverHeightOffset, 5f * Time.deltaTime);
            }
            else
            {
                nextPos.y = Mathf.Lerp(transform.position.y, initialSpawnPosition.y, 5f * Time.deltaTime);
            }

            transform.position = nextPos;
        }
        else
        {
            // Đã về đến Spawn point: giữ lơ lửng tại chỗ
            transform.position = Vector3.Lerp(transform.position, initialSpawnPosition, 5f * Time.deltaTime);
        }

        // 2. MẶT VẪN HƯỚNG VỀ PHÍA PLAYER (VỪA NHÌN VỪA LÙI LẠI)
        RotateTowardsPlayer();
    }

    private void ChasePlayer()
    {
        // 1. HƯỚNG DI CHUYỂN NGANG
        Vector3 directionToPlayer = (player.position - transform.position);
        directionToPlayer.y = 0f; 
        directionToPlayer.Normalize();

        Vector3 nextPos = transform.position + directionToPlayer * moveSpeed * Time.deltaTime;

        // 2. LƯỚT QUA ĐỊA HÌNH VÀ TẢNG ĐÁ
        RaycastHit[] hits = Physics.RaycastAll(nextPos + Vector3.up * 50f, Vector3.down, 100f);
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
            nextPos.y = Mathf.Lerp(transform.position.y, bestY + hoverHeightOffset, 5f * Time.deltaTime);
        }
        else
        {
            nextPos.y = Mathf.Lerp(transform.position.y, player.position.y + hoverHeightOffset, 5f * Time.deltaTime);
        }

        transform.position = nextPos;

        // 3. XOAY MẶT VỀ PHÍA PLAYER THEO MẶT PHẲNG NGANG (CHỐNG LỖI NGHIÊNG / LẬT NGANG)
        RotateTowardsPlayer();
    }

    private void RotateTowardsPlayer()
    {
        if (player == null) return;
        Vector3 horizontalLook = (player.position - transform.position);
        horizontalLook.y = 0f;

        if (horizontalLook.sqrMagnitude > 0.001f)
        {
            Quaternion yawRot = Quaternion.LookRotation(horizontalLook.normalized, Vector3.up);
            transform.rotation = yawRot * Quaternion.Euler(modelRotationOffset);
        }
    }

    private void CheckCatch()
    {
        if (player == null) return;
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= killDistance)
        {
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        Debug.Log("💀 YOSHIE ĐÃ BẮT ĐƯỢC BẠN!");
        this.enabled = false; 
    }
}
