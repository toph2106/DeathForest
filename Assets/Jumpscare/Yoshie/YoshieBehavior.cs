using UnityEngine;

public class YoshieBehavior : MonoBehaviour
{
    [Header("AI Settings")]
    [Tooltip("Tốc độ bay của Yoshie")]
    public float moveSpeed = 50f;
    
    [Tooltip("Khoảng cách kích hoạt Game Over / Bị bắt")]
    public float killDistance = 3f;
    
    [Tooltip("Độ cao bay lơ lửng so với mặt đất")]
    public float hoverHeightOffset = 3f;

    [Tooltip("Góc xoay bù trừ để dựng đứng mô hình (Thử đổi X thành 90 hoặc -90 nếu vẫn bị nằm)")]
    public Vector3 modelRotationOffset = new Vector3(-90f, 0f, 0f);

    [Header("Trạng thái (State)")]
    public bool isFleeing = false; // Kích hoạt khi người chơi chạy vào Vùng An Toàn (Safe Zone)

    private Transform player;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Yoshie không tìm thấy Player!");
        }
    }

    void Update()
    {
        if (player == null) return;

        if (isFleeing)
        {
            Destroy(gameObject);
            return;
        }

        ChasePlayer();
        CheckCatch();
    }

    private void ChasePlayer()
    {
        // 1. HƯỚNG DI CHUYỂN NGANG
        Vector3 directionToPlayer = (player.position - transform.position);
        directionToPlayer.y = 0f; 
        directionToPlayer.Normalize();

        Vector3 nextPos = transform.position + directionToPlayer * moveSpeed * Time.deltaTime;

        // 2. LƯỚT QUA ĐỊA HÌNH VÀ TẢNG ĐÁ
        // Bắn tia từ trên trời (cao 50m) cắm thẳng xuống đất để tìm độ cao của địa hình
        RaycastHit[] hits = Physics.RaycastAll(nextPos + Vector3.up * 50f, Vector3.down, 100f);
        float bestY = -9999f;
        bool foundGround = false;
        
        foreach (var hit in hits)
        {
            // Bỏ qua người chơi và chính Yoshie
            if (hit.collider.CompareTag("Player") || hit.collider.gameObject == gameObject) continue;
            
            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            // Trượt mượt mà lên đồi núi / tảng đá
            nextPos.y = Mathf.Lerp(transform.position.y, bestY + hoverHeightOffset, 5f * Time.deltaTime);
        }
        else
        {
            // Nếu không có đất (Rơi xuống vực), cứ bay ngang tầm mắt người chơi
            nextPos.y = Mathf.Lerp(transform.position.y, player.position.y + hoverHeightOffset, 5f * Time.deltaTime);
        }

        transform.position = nextPos;

        // 3. XOAY MẶT VÀ SỬA LỖI NẰM SẤP
        Vector3 lookDirection = player.position + Vector3.up * hoverHeightOffset - transform.position;
        if (lookDirection != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(lookDirection);
            // Nhân thêm góc quay Offset để dựng cái mặt đứng lên
            transform.rotation = lookRot * Quaternion.Euler(modelRotationOffset);
        }
    }

    private void CheckCatch()
    {
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
