using UnityEngine;

public class StalkerBehavior : MonoBehaviour
{
    [Header("AI Settings")]
    [Tooltip("Tốc độ khi rình rập đi dạo (Rất chậm)")]
    public float walkSpeed = 10f;
    
    [Tooltip("Tốc độ rượt đuổi khi phát điên (Ví dụ: 120)")]
    public float sprintSpeed = 120f;
    
    [Tooltip("Khoảng cách vồ lấy người chơi")]
    public float killDistance = 2.5f;

    [Tooltip("Ngưỡng âm thanh: Vận tốc của Player phải LỚN HƠN số này thì Stalker mới nghe thấy (Nên để 45 vì Walk Speed của Player đang là 40)")]
    public float sprintThreshold = 45f;

    [Tooltip("Thời gian Stalker hoang mang đứng tìm bạn nếu bạn đột ngột đi bộ (Giây)")]
    public float calmDownTime = 0.5f;

    [Tooltip("Khoảng cách giữ cự ly rình rập khi người chơi đi bộ (Nó sẽ lờ đờ đi vòng quanh bạn)")]
    public float stalkDistance = 15f;

    [Header("Model Fixes")]
    [Tooltip("Độ cao bù trừ để chân không bị lún (Thử 1.5 hoặc 2)")]
    public float groundOffset = 1.5f;

    [Tooltip("Góc bù trừ để xoay thẳng mô hình (Thử -90, 0, 0 nếu nó nằm sấp)")]
    public Vector3 modelRotationOffset = new Vector3(0f, 0f, 0f); 

    private Transform player;
    private Animator anim;
    
    // Biến đo vận tốc người chơi
    private Vector3 playerLastPos;
    private bool hasInitialized = false; // Bỏ qua frame đầu tiên

    // Trạng thái AI
    private bool isAggro = false;
    private float calmTimer = 0f;
    private float sprintTimer = 0f; // Bộ đệm chống nhiễu lag

    void Start()
    {
        anim = GetComponentInChildren<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerLastPos = player.position;
        }
        else
        {
            Debug.LogError("Stalker không tìm thấy Player!");
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateAggroState();
        MoveStalker();
        CheckCatch();
    }

    private void UpdateAggroState()
    {
        // Bỏ qua frame đầu tiên (tránh bị spike do Player rơi xuống đất khi khởi động)
        if (!hasInitialized)
        {
            hasInitialized = true;
            playerLastPos = player.position;
            return;
        }

        // Tính vận tốc thực tế của Player
        Vector3 displacement = player.position - playerLastPos;
        displacement.y = 0f; 
        
        // Bảo vệ deltaTime quá nhỏ (tránh chia cho 0 hoặc spike khi máy lag)
        float playerSpeed = 0f;
        if (Time.deltaTime > 0.001f)
        {
            playerSpeed = displacement.magnitude / Time.deltaTime;
        }

        // BỘ ĐỆM CHỐNG NHIỄU: Nếu vận tốc cao, phải giữ nguyên trên 0.15 giây thì mới kích hoạt
        if (playerSpeed > sprintThreshold)
        {
            sprintTimer += Time.deltaTime;
            if (sprintTimer > 0.15f)
            {
                // VẬN TỐC CAO ĐƯỢC DUY TRÌ -> NỔI ĐIÊN LAO VÀO
                isAggro = true;
                calmTimer = calmDownTime; 
            }
        }
        else
        {
            sprintTimer = 0f; // Reset bộ đệm nếu đi chậm lại
            
            // VẬN TỐC THẤP -> CHẬM LẠI RÌNH RẬP
            if (isAggro)
            {
                calmTimer -= Time.deltaTime;
                if (calmTimer <= 0f)
                {
                    isAggro = false;
                }
            }
        }

        playerLastPos = player.position;
    }

    private void MoveStalker()
    {
        float currentSpeed = walkSpeed;
        Vector3 targetPos = transform.position;

        if (isAggro)
        {
            currentSpeed = sprintSpeed;
            targetPos = player.position; // Đuổi thẳng người chơi
        }
        else
        {
            currentSpeed = walkSpeed;
            
            // Logic Rình Rập (Đi vòng quanh giữ khoảng cách)
            Vector3 dirToPlayer = player.position - transform.position;
            dirToPlayer.y = 0;
            float dist = dirToPlayer.magnitude;

            if (dist > 0.1f)
            {
                Vector3 dirNormalized = dirToPlayer.normalized;
                
                if (dist > stalkDistance + 2f)
                {
                    // Quá xa -> Tiến lại gần
                    targetPos = transform.position + dirNormalized * 5f;
                }
                else if (dist < stalkDistance - 2f)
                {
                    // Quá gần -> Lùi ra xa (Duy trì áp lực)
                    targetPos = transform.position - dirNormalized * 5f;
                }
                else
                {
                    // Ở đúng cự ly -> Đi lượn vòng quanh người chơi (Quay góc 90 độ)
                    Vector3 orbitDir = Quaternion.Euler(0, 90, 0) * dirNormalized;
                    targetPos = transform.position + orbitDir * 5f;
                }
            }
        }
        
        if (anim != null)
        {
            anim.speed = isAggro ? 3f : 0.5f; 
        }

        Vector3 directionToTarget = (targetPos - transform.position);
        directionToTarget.y = 0f;
        
        if (directionToTarget.magnitude > 0.1f)
        {
            directionToTarget.Normalize();
        }

        Vector3 nextPos = transform.position + directionToTarget * currentSpeed * Time.deltaTime;

        RaycastHit[] hits = Physics.RaycastAll(nextPos + Vector3.up * 500f, Vector3.down, 1000f);
        float bestY = -9999f;
        bool foundGround = false;

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Player") || hit.collider.gameObject == gameObject) continue;
            
            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            nextPos.y = Mathf.Lerp(transform.position.y, bestY + groundOffset, 10f * Time.deltaTime);
        }
        else
        {
            nextPos.y = Mathf.Lerp(transform.position.y, player.position.y + groundOffset, 10f * Time.deltaTime);
        }

        transform.position = nextPos;

        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(directionToTarget);
            transform.rotation = lookRot * Quaternion.Euler(modelRotationOffset);
        }
    }

    private void CheckCatch()
    {
        // THUẬT TOÁN KINH DỊ: 
        // Nếu Stalker không bị Aggro (Bạn đang đi bộ rón rén), nó sẽ mù hoàn toàn.
        // Bạn có thể đi bộ lướt sát qua mặt nó hoặc đứng nhìn nó mà nó không cắn bạn!
        if (!isAggro) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= killDistance)
        {
            Debug.Log("💀 STALKER ĐÃ XÉ XÁC BẠN VÌ TỘI CHẠY LỚN TIẾNG!");
            this.enabled = false;
        }
    }
}
