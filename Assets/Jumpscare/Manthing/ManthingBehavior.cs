using UnityEngine;

public class ManthingBehavior : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 12f; 

    [Header("Animation")]
    [Tooltip("Tốc độ hoạt ảnh của chân. Giảm xuống 0.5 để chạy siêu chậm bệ vệ")]
    public float animSpeed = 0.5f; 

    [Header("Lifetime")]
    public float fadeDelay = 5f; // Giảm thời gian chờ xuống 5s cho nhanh biến mất
    public float fadeSpeed = 2f; // Biến mất nhanh hơn

    [Header("Earthquake (Động đất vật lý)")]
    [Tooltip("Kéo xương bàn chân trái vào đây")]
    public Transform leftFoot;
    [Tooltip("Kéo xương bàn chân phải vào đây")]
    public Transform rightFoot;
    public float shakeRadius = 100f; 
    public float maxShakeIntensity = 3f; 
    [Tooltip("Thời gian dư chấn mỗi khi dậm chân")]
    public float shakeDuration = 0.4f;

    private Transform player;
    private Camera mainCam;
    private float passedTimer = 0f;
    private float fadeCountdown = 0f;
    private bool isFadingOut = false;
    private Vector3 walkDirection;
    private Animator anim;
    
    // Biến theo dõi bàn chân
    private float leftLastLocalY = 0f;
    private bool leftWasMovingDown = false;
    private float rightLastLocalY = 0f;
    private bool rightWasMovingDown = false;
    private float currentShakeTimer = 0f;

    void Start()
    {
        mainCam = Camera.main;

        if (leftFoot != null) leftLastLocalY = transform.InverseTransformPoint(leftFoot.position).y;
        if (rightFoot != null) rightLastLocalY = transform.InverseTransformPoint(rightFoot.position).y;

        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.speed = animSpeed;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;

            walkDirection = -player.forward; 
            walkDirection.y = 0f;
            walkDirection.Normalize();

            if (walkDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(walkDirection);
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector3 nextPos = transform.position + walkDirection * moveSpeed * Time.deltaTime;
        
        RaycastHit[] hits = Physics.RaycastAll(nextPos + Vector3.up * 10f, Vector3.down, 20f);
        float bestY = -9999f;
        bool foundGround = false;
        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Player") || hit.collider.gameObject == gameObject) continue;
            if (hit.normal.y > 0.5f)
            {
                if (hit.point.y > bestY)
                {
                    bestY = hit.point.y;
                    foundGround = true;
                }
            }
        }
        
        if (foundGround) nextPos.y = bestY;
        transform.position = nextPos;

        // 2. LOGIC ĐỘNG ĐẤT BẰNG XƯƠNG CHÂN
        CheckFootstep(leftFoot, ref leftLastLocalY, ref leftWasMovingDown);
        CheckFootstep(rightFoot, ref rightLastLocalY, ref rightWasMovingDown);

        // 3. LOGIC TỰ ĐỘNG XÓA
        passedTimer += Time.deltaTime;

        Vector3 toMonster = transform.position - player.position;
        bool isBehindPlayer = Vector3.Dot(toMonster, player.forward) < 0f;
        
        if (isBehindPlayer || passedTimer > 6f)
        {
            isFadingOut = true;
        }

        if (isFadingOut)
        {
            fadeCountdown += Time.deltaTime;
            if (fadeCountdown >= fadeDelay)
            {
                Destroy(gameObject); 
            }
        }
    }

    private void CheckFootstep(Transform foot, ref float lastLocalY, ref bool wasStomping)
    {
        if (foot == null) return;
        
        float currentLocalY = transform.InverseTransformPoint(foot.position).y;
        float deltaY = currentLocalY - lastLocalY;

        // Bất kỳ chuyển động đi xuống nào dù là nhỏ nhất (1mm mỗi frame) cũng được ghi nhận là vung chân
        if (deltaY < -0.001f)
        {
            wasStomping = true;
        }
        // Chỉ cần ngưng đi xuống hoặc bắt đầu đi lên là bắt dính khoảnh khắc chạm đất
        else if (wasStomping && deltaY > -0.0001f)
        {
            TriggerShake();
            wasStomping = false; 
        }
        
        lastLocalY = currentLocalY;
    }

    public void TriggerShake()
    {
        currentShakeTimer = shakeDuration;
    }

    void LateUpdate()
    {
        if (player == null || mainCam == null) return;

        // CHỈ RUNG KHI CÓ LỆNH (currentShakeTimer > 0)
        if (currentShakeTimer > 0f)
        {
            currentShakeTimer -= Time.deltaTime;
            
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < shakeRadius)
            {
                // Công thức tính khoảng cách
                float distanceFactor = 1f - (distance / shakeRadius);
                
                // GIỮ LẠI ÍT NHẤT 20% SỨC MẠNH: 
                // Tránh tình trạng đứng quá xa thì distanceFactor = 0 làm mất luôn hiệu ứng rung
                distanceFactor = Mathf.Clamp(distanceFactor, 0.2f, 1f);
                
                // Sức rung giảm dần theo thời gian (fade out mượt mà)
                float timeFactor = currentShakeTimer / shakeDuration;
                
                float currentShake = maxShakeIntensity * distanceFactor * timeFactor;

                // LẮC GÓC XOAY CAMERA
                float shakeX = Random.Range(-currentShake, currentShake);
                float shakeZ = Random.Range(-currentShake, currentShake);

                mainCam.transform.localRotation *= Quaternion.Euler(shakeX, 0f, shakeZ);
            }
        }
    }
}

