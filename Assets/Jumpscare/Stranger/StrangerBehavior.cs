using UnityEngine;

public class StrangerBehavior : MonoBehaviour
{
    [Header("AI Settings")]
    [Tooltip("Tốc độ trượt siêu nhanh khi bị khuất tầm nhìn (Ví dụ: 80 - 100)")]
    public float fastSpeed = 100f;
    
    [Tooltip("Khoảng cách bắt người chơi")]
    public float killDistance = 2.5f;

    [Tooltip("Độ cao bù trừ để chân không bị lún (Nếu quái bị lún nửa người, hãy tăng số này lên, ví dụ: 1.5 hoặc 2)")]
    public float groundOffset = 1.5f;

    [Tooltip("Góc bù trừ để xoay thẳng mô hình (Thử -90, 0, 0 nếu nó nằm sấp)")]
    public Vector3 modelRotationOffset = new Vector3(0f, 0f, 0f); 

    private Transform player;
    private Camera mainCam;
    private Animator anim;
    private bool isFrozen = false;

    void Start()
    {
        mainCam = Camera.main;
        anim = GetComponentInChildren<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Stranger không tìm thấy Player!");
        }
    }

    void Update()
    {
        if (player == null || mainCam == null) return;

        CheckIfBeingLookedAt();

        if (!isFrozen)
        {
            ChasePlayer();
        }

        CheckCatch();
    }

    private void CheckIfBeingLookedAt()
    {
        Vector3 checkPoint = transform.position + Vector3.up * 1.5f;
        Vector3 viewportPoint = mainCam.WorldToViewportPoint(checkPoint);

        bool onScreen = viewportPoint.z > 0 && 
                        viewportPoint.x > -0.1f && viewportPoint.x < 1.1f && 
                        viewportPoint.y > -0.1f && viewportPoint.y < 1.1f;

        if (onScreen)
        {
            Vector3 dirToCam = mainCam.transform.position - checkPoint;
            float distanceToCam = dirToCam.magnitude;

            if (Physics.Raycast(checkPoint, dirToCam.normalized, out RaycastHit hit, distanceToCam))
            {
                if (!hit.collider.CompareTag("Player") && hit.collider.gameObject != gameObject)
                {
                    Unfreeze();
                    return;
                }
            }
            
            Freeze();
        }
        else
        {
            Unfreeze();
        }
    }

    private void Freeze()
    {
        isFrozen = true;
        if (anim != null)
        {
            anim.speed = 0f;
        }
    }

    private void Unfreeze()
    {
        isFrozen = false;
        if (anim != null)
        {
            anim.speed = 1f;
        }
    }

    private void ChasePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position);
        directionToPlayer.y = 0f;
        directionToPlayer.Normalize();

        Vector3 nextPos = transform.position + directionToPlayer * fastSpeed * Time.deltaTime;

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
            // Cộng thêm groundOffset để nhấc nó lên khỏi mặt đất
            nextPos.y = Mathf.Lerp(transform.position.y, bestY + groundOffset, 10f * Time.deltaTime);
        }
        else
        {
            nextPos.y = Mathf.Lerp(transform.position.y, player.position.y + groundOffset, 10f * Time.deltaTime);
        }

        transform.position = nextPos;

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = lookRot * Quaternion.Euler(modelRotationOffset);
        }
    }

    private void CheckCatch()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= killDistance)
        {
            Debug.Log("💀 STRANGER ĐÃ BẮT ĐƯỢC BẠN TỪ PHÍA SAU!");
            this.enabled = false;
        }
    }
}
