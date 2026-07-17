using UnityEngine;

public class SpiderBehavior : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 15f;

    [Header("Lifetime")]
    public float fadeDelay = 5f;
    public float fadeSpeed = 2f;

    [Header("Targeting")]
    [Tooltip("Tỉ lệ (0.0 -> 1.0) nhện lao thẳng vào mặt thay vì chạy song song")]
    [Range(0f, 1f)] public float directTargetChance = 0.2f;

    [HideInInspector] public bool forceDirectTarget = false;

    private Transform player;
    private float passedTimer = 0f;
    private float lifeTimer = 0f;
    private bool isFadingOut = false;
    private Vector3 rushDirection;
    private Animator anim;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.speed = moveSpeed / 15f;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;

            // XÁC ĐỊNH HƯỚNG CHẠY
            if (forceDirectTarget || Random.value < directTargetChance)
            {
                // Nhện khổng lồ HOẶC nhện bị random trúng sẽ cắm thẳng vào player
                rushDirection = player.position - transform.position;
            }
            else
            {
                // Số còn lại chạy dàn hàng ngang (ngược chiều camera)
                rushDirection = -player.forward;
            }

            rushDirection.y = 0f;
            rushDirection.Normalize();

            if (rushDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(rushDirection) * Quaternion.Euler(0, 180, 0);
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector3 nextPos = transform.position + rushDirection * moveSpeed * Time.deltaTime;

        RaycastHit[] hits = Physics.RaycastAll(nextPos + Vector3.up * 5f, Vector3.down, 15f);
        float bestY = -9999f;
        bool foundGround = false;

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Player") || hit.collider.gameObject.name.Contains("Spider")) 
                continue;

            if (hit.normal.y > 0.5f)
            {
                if (hit.point.y > bestY)
                {
                    bestY = hit.point.y;
                    foundGround = true;
                }
            }
        }

        if (foundGround)
        {
            nextPos.y = bestY + 0.05f;
        }

        transform.position = nextPos;

        // LOGIC TỰ ĐỘNG XÓA
        lifeTimer += Time.deltaTime;

        Vector3 toSpider = transform.position - player.position;
        bool isBehindPlayer = Vector3.Dot(toSpider, player.forward) < 0f;

        // Nếu đi khuất sau lưng, HOẶC đã sống đủ 6 giây (chống kẹt) -> Khóa cờ đếm ngược
        if (isBehindPlayer || lifeTimer > 6f)
        {
            isFadingOut = true;
        }

        if (isFadingOut)
        {
            passedTimer += Time.deltaTime;
            
            if (passedTimer >= fadeDelay)
            {
                Destroy(gameObject); // Bốc hơi tức thì, không thu nhỏ
            }
        }
    }
}