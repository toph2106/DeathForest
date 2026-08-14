using UnityEngine;
using System.Collections;

public class BabyCockroachCrawler : MonoBehaviour
{
    [Header("1. Mục Tiêu Nhắm Tới (Player)")]
    [Tooltip("Kéo Player vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM Player / MovePl trong Scene!")]
    public Transform playerTarget;

    [Header("2. Kích Hoạt Bởi Sự Kiện Cửa Mở 10s")]
    [Tooltip("Tự động kích hoạt khi cửa chính mở ra ở mốc 10s máy quay")]
    public bool triggerOnDoorOpen10s = true;

    [Tooltip("Thời gian chờ sau khi cửa mở rồi gián con mới bắt đầu bò vào nhà (giây)")]
    public float delayAfterDoorOpen = 0.8f;

    [Tooltip("Tích chọn để ẩn con gián lúc đầu, khi cửa mở mới hiện lên bò vào")]
    public bool hideUntilTriggered = true;

    [Header("3. Điểm Dẫn Hướng Qua Cửa (Doorpoint Checkpoint)")]
    [Tooltip("Kéo Object Doorpoint vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM GameObject 'Doorpoint' trong Scene!")]
    public Transform doorWaypoint;

    [Header("4. Thời Gian Chạy Quanh Người Chơi Trước Khi Rời Đi")]
    [Tooltip("Thời gian tối thiểu chạy quanh người chơi trước khi rút lui về cửa (giây - Mặc định: 10s)")]
    public float minScrambleDuration = 10.0f;

    [Tooltip("Thời gian tối đa chạy quanh người chơi trước khi rút lui về cửa (giây - Mặc định: 15s)")]
    public float maxScrambleDuration = 15.0f;

    [Header("5. Cấu Hình Bò Xung Quanh Chân Người Chơi (Scramble)")]
    [Tooltip("Khoảng cách tối thiểu tới chân người chơi (mét)")]
    public float minDistanceToPlayer = 0.7f;

    [Tooltip("Khoảng cách tối đa lượn quanh người chơi (mét)")]
    public float maxDistanceToPlayer = 2.2f;

    [Tooltip("Tốc độ bò cơ bản khi lượn quanh Player (m/s)")]
    public float baseCrawlSpeed = 2.8f;

    [Tooltip("Tốc độ chạy rút lui về lại cửa (m/s)")]
    public float returnSpeed = 3.5f;

    [Tooltip("Tốc độ xoay đầu chuyển hướng")]
    public float turnSpeed = 16.0f;

    [Tooltip("Bù góc xoay mô hình 3D. Mặc định bù 180 độ trục Y để đầu hướng về phía trước")]
    public Vector3 modelRotationOffset = new Vector3(0f, 180f, 0f);

    [Header("6. Chống Đi Xuyên Tường (Wall Avoidance)")]
    [Tooltip("Layer của tường / chướng ngại vật để tránh đi xuyên qua. Mặc định chỉ layer Default (0), tránh dính Player/UI/Trigger")]
    public LayerMask obstacleLayer = 1 << 0; // FIX #3: Chỉ layer Default, tránh raycast trúng Player/Cockroach/UI

    [Header("7. Tên Animation Trong Animator")]
    [Tooltip("Tên state animation bò trong Animator")]
    public string walkAnimState = "giant_cockroach_armature|walking";

    [Header("8. Âm Thanh Bò Sột Soạt 3D (Tùy Chọn)")]
    [Tooltip("Tiếng chân gián bò rột rẹt / lạo xạo trên sàn")]
    public AudioClip skitterSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.65f;

    [Header("9. Phím Tắt Kích Hoạt Test Nhanh")]
    [Tooltip("Bấm phím này trong lúc Play để kích hoạt ngay gián con bò vào nhà (Mặc định: Phím J)")]
    public KeyCode debugTriggerKey = KeyCode.J;

    public static event System.Action OnAllBabiesDisappeared;

    private Animator animator;
    private AudioSource audioSource;
    private bool isCrawling = false;
    private Vector3 initialSpawnPos;
    private Quaternion initialSpawnRot;
    private Coroutine crawlRoutine;

    // Các biến tạo tính cách & quỹ đạo riêng biệt cho từng con gián
    private float individualSpeedMultiplier = 1f;
    private float currentOrbitAngle = 0f;
    private float orbitDirection = 1f;
    private float preferredRadius = 1.5f;
    private float randomizedStayDuration = 12f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // Âm thanh 3D
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = 10f;
        audioSource.playOnAwake = false;

        // Lưu lại vị trí xuất phát ban đầu ngoài hành lang
        initialSpawnPos = transform.position;
        initialSpawnRot = transform.rotation;

        // TỰ ĐỘNG RANDOM HÓA THÔNG SỐ ĐỂ MỖI CON GIÁN CÓ ĐƯỜNG ĐI VÀ THỜI GIAN RỜI ĐI LỆCH NHAU
        individualSpeedMultiplier = Random.Range(0.85f, 1.35f);
        currentOrbitAngle = Random.Range(0f, 360f);
        orbitDirection = (Random.value > 0.5f) ? 1f : -1f;
        preferredRadius = Random.Range(minDistanceToPlayer, maxDistanceToPlayer);

        // Random thời gian chạy quanh người chơi từ 10s đến 15s độc lập cho từng con
        randomizedStayDuration = Random.Range(minScrambleDuration, maxScrambleDuration);
    }

    void Start()
    {
        EnsurePlayerTarget();

        // Tự động tìm GameObject Doorpoint nếu chưa kéo
        if (doorWaypoint == null)
        {
            GameObject dp = GameObject.Find("Doorpoint");
            if (dp != null) doorWaypoint = dp.transform;
        }

        // Ẩn ban đầu nếu được cài đặt
        if (hideUntilTriggered)
        {
            SetVisible(false);
        }
    }

    void Update()
    {
        // PHÍM TẮT TEST NHANH (MẶC ĐỊNH: PHÍM J)
        if (debugTriggerKey != KeyCode.None && Input.GetKeyDown(debugTriggerKey))
        {
            Debug.Log($"[BabyCockroachCrawler] ⌨️ Bấm phím [{debugTriggerKey}]! Kích hoạt ngay gián con bò vào quanh Player...");
            StartCrawling();
        }
    }

    void OnEnable()
    {
        if (triggerOnDoorOpen10s)
        {
            CamcorderUI.OnTimerReached10s += HandleDoorOpen10sEvent;
        }
    }

    void OnDisable()
    {
        if (triggerOnDoorOpen10s)
        {
            CamcorderUI.OnTimerReached10s -= HandleDoorOpen10sEvent;
        }
    }

    void HandleDoorOpen10sEvent()
    {
        StartCoroutine(DelayedStartRoutine(delayAfterDoorOpen));
    }

    IEnumerator DelayedStartRoutine(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        StartCrawling();
    }

    void EnsurePlayerTarget()
    {
        if (playerTarget != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            return;
        }

        MovePl movePl = Object.FindFirstObjectByType<MovePl>();
        if (movePl != null)
        {
            playerTarget = movePl.transform;
            return;
        }

        if (Camera.main != null)
        {
            playerTarget = Camera.main.transform;
        }
    }

    /// <summary>
    /// Kích hoạt gián con xuất hiện tại vị trí ban đầu ngoài hành lang và bắt đầu lộ trình
    /// </summary>
    public void StartCrawling()
    {
        gameObject.SetActive(true);
        SetVisible(true);

        transform.position = initialSpawnPos;
        transform.rotation = initialSpawnRot;

        // Tạo lại thời gian ở lại ngẫu nhiên mới cho đợt chạy này
        randomizedStayDuration = Random.Range(minScrambleDuration, maxScrambleDuration);

        if (crawlRoutine != null) StopCoroutine(crawlRoutine);
        crawlRoutine = StartCoroutine(CrawlSequenceRoutine());
    }

    IEnumerator CrawlSequenceRoutine()
    {
        isCrawling = true;
        EnsurePlayerTarget();

        if (animator != null && !string.IsNullOrEmpty(walkAnimState))
        {
            animator.CrossFadeInFixedTime(walkAnimState, 0.1f);
        }

        if (skitterSound != null && audioSource != null)
        {
            audioSource.clip = skitterSound;
            audioSource.loop = true;
            audioSource.volume = soundVolume;
            audioSource.Play();
        }

        Debug.Log($"[BabyCockroachCrawler] 🪳 Gián con bắt đầu bò từ ngoài hành lang qua Doorpoint vào phòng...");

        // ========================================================
        // BƯỚC 1: BÒ TỪ NGOÀI HÀNH LANG QUA CHECKPOINT DOORPOINT (ĐỂ KHÔNG ĐÂM VÀO TƯỜNG CỬA)
        // ========================================================
        if (doorWaypoint != null)
        {
            yield return StartCoroutine(MoveToPositionRoutine(doorWaypoint.position, baseCrawlSpeed * individualSpeedMultiplier));
        }

        // ========================================================
        // BƯỚC 2: TIẾN VÀO KHU VỰC GẦN PLAYER TRONG PHÒNG
        // ========================================================
        if (playerTarget != null)
        {
            Vector3 approachPos = GetWaypointAroundPlayer(preferredRadius, currentOrbitAngle);
            yield return StartCoroutine(MoveToPositionRoutine(approachPos, baseCrawlSpeed * individualSpeedMultiplier));
        }

        // ========================================================
        // BƯỚC 3: BÒ LƯỢN QUANH CHÂN PLAYER TRONG 10S - 15S
        // ========================================================
        float scrambleElapsed = 0f;

        while (isCrawling && scrambleElapsed < randomizedStayDuration)
        {
            float stepStartTime = Time.time;
            EnsurePlayerTarget();

            if (playerTarget == null)
            {
                yield return new WaitForSeconds(0.5f);
                scrambleElapsed += (Time.time - stepStartTime);
                continue;
            }

            // FIX #5: Góc bước nhỏ hơn (25-75°) để chuyển hướng mượt mà, tránh giật cục
            float angleStep = Random.Range(25f, 75f) * orbitDirection;
            currentOrbitAngle += angleStep;

            // Thỉnh thoảng đảo chiều quay (Clockwise <-> Counter-clockwise)
            if (Random.value < 0.25f)
            {
                orbitDirection *= -1f;
            }

            // Thay đổi bán kính linh hoạt (lúc chạy sát chân 0.8m, lúc lượn xa 2.0m)
            preferredRadius = Random.Range(minDistanceToPlayer, maxDistanceToPlayer);

            // Tính điểm đến mới xung quanh vị trí thực tế của Player
            Vector3 nextWaypoint = GetWaypointAroundPlayer(preferredRadius, currentOrbitAngle);

            // Di chuyển tới điểm đó
            yield return StartCoroutine(MoveToPositionRoutine(nextWaypoint, baseCrawlSpeed * individualSpeedMultiplier));

            // Thỉnh thoảng dừng giật nhẹ 0.1s - 0.3s đổi hướng như gián thật
            if (Random.value < 0.35f)
            {
                float microPause = Random.Range(0.1f, 0.35f);
                yield return new WaitForSeconds(microPause);
            }

            scrambleElapsed += (Time.time - stepStartTime);
        }

        // ========================================================
        // BƯỚC 4: HẾT GIỜ (10S - 15S) ➔ BÒ TỪ TRONG PHÒNG VỀ CHECKPOINT DOORPOINT (ĐỂ QUA CỬA AN TOÀN)
        // ========================================================
        Debug.Log("[BabyCockroachCrawler] 🚪 Đã hết giờ (10-15s)! Gián con bò qua Doorpoint ra ngoài hành lang...");

        if (doorWaypoint != null)
        {
            yield return StartCoroutine(MoveToPositionRoutine(doorWaypoint.position, returnSpeed));
        }

        // ========================================================
        // BƯỚC 5: TỪ DOORPOINT BÒ TIẾP RA NGOÀI HÀNH LANG VỀ LẠI ĐIỂM CŨ RỒI BIẾN MẤT
        // ========================================================
        yield return StartCoroutine(MoveToPositionRoutine(initialSpawnPos, returnSpeed));

        // TẮT ÂM THANH VÀ BIẾN MẤT HOÀN TOÀN ĐỂ NHƯỜNG SÂN KHẤU CHO GIÁN MẸ!
        if (skitterSound != null && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isCrawling = false;
        Debug.Log("[BabyCockroachCrawler] 💨 Gián con đã về lại ngoài hành lang và BIẾN MẤT HOÀN TOÀN! Nhường sân chơi cho Gián Mẹ!");
        
        SetVisible(false);
        gameObject.SetActive(false);

        // Bật ngay Trigger cửa để đón Player bước ra kích hoạt Gián Mẹ
        CockroachDoorAttackTrigger doorTrigger = Object.FindFirstObjectByType<CockroachDoorAttackTrigger>(FindObjectsInactive.Include);
        if (doorTrigger != null)
        {
            doorTrigger.ArmTrigger();
        }

        OnAllBabiesDisappeared?.Invoke();
    }

    IEnumerator MoveToPositionRoutine(Vector3 targetPos, float speed)
    {
        // Cố định độ cao Y theo mặt sàn (dùng Y của chính con gián, không dùng Y của target)
        targetPos.y = transform.position.y;

        // FIX #1: Tính timeout động theo khoảng cách thực tế, tối thiểu 8 giây
        // Đảm bảo gián luôn có đủ thời gian bò tới đích dù xa bao nhiêu
        float distanceToTarget = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(targetPos.x, 0f, targetPos.z)
        );
        // Timeout = (khoảng cách / tốc độ) * 2.0 hệ số an toàn, tối thiểu 8 giây
        float moveTimeout = Mathf.Max(8.0f, (distanceToTarget / Mathf.Max(speed, 0.1f)) * 2.0f);
        float elapsed = 0f;

        while (elapsed < moveTimeout)
        {
            elapsed += Time.deltaTime;

            Vector3 currentPos2D = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPos2D = new Vector3(targetPos.x, 0, targetPos.z);

            if (Vector3.Distance(currentPos2D, targetPos2D) <= 0.2f)
            {
                break;
            }

            Vector3 moveDir = (targetPos - transform.position);
            moveDir.y = 0;

            if (moveDir != Vector3.zero)
            {
                Vector3 normalizedDir = moveDir.normalized;

                // TÍNH NĂNG CHỐNG XUYÊN TƯỜNG: Bắn tia Raycast phía trước kiểm tra vật cản/tường
                // FIX #3: obstacleLayer giờ chỉ chứa layer tường/sàn, không bắn trúng Player/gián khác
                Ray ray = new Ray(transform.position + Vector3.up * 0.1f, normalizedDir);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 0.35f, obstacleLayer))
                {
                    // FIX #4: Khi gặp tường, pha trộn hướng trượt (slide) với hướng tới đích
                    // để gián vừa trượt dọc tường VỪA tiến dần về phía mục tiêu, không bị xoay vòng tại chỗ
                    Vector3 slideDir = Vector3.ProjectOnPlane(normalizedDir, hit.normal).normalized;
                    if (slideDir != Vector3.zero)
                    {
                        // Trộn 60% hướng trượt + 40% hướng tới đích → vẫn tiến về mục tiêu
                        normalizedDir = (slideDir * 0.6f + normalizedDir * 0.4f).normalized;
                    }
                }

                // Xoay đầu hướng theo hướng đang bò + bù modelRotationOffset
                Quaternion targetRot = Quaternion.LookRotation(normalizedDir) * Quaternion.Euler(modelRotationOffset);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

                transform.position = Vector3.MoveTowards(transform.position, transform.position + normalizedDir, speed * Time.deltaTime);
            }

            yield return null;
        }
    }

    // FIX #2: Dùng transform.position.y (Y mặt sàn của gián) thay vì playerTarget.position.y (Y đứng của Player)
    Vector3 GetWaypointAroundPlayer(float radius, float angleDegrees)
    {
        if (playerTarget == null) return transform.position;

        float rad = angleDegrees * Mathf.Deg2Rad;
        float x = Mathf.Cos(rad) * radius;
        float z = Mathf.Sin(rad) * radius;

        Vector3 playerPos = playerTarget.position;
        // Luôn dùng Y của chính con gián (mặt sàn), không dùng Y của Player (chiều cao đứng)
        return new Vector3(playerPos.x + x, transform.position.y, playerPos.z + z);
    }

    void SetVisible(bool isVisible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = isVisible;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders)
        {
            if (c != null) c.enabled = isVisible;
        }
    }
}
