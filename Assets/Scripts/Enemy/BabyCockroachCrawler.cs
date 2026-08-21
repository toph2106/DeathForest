using UnityEngine;
using System.Collections;

public class BabyCockroachCrawler : MonoBehaviour, IInteractable
{
    [Header("1. Box Collider Vùng Bò Trên Tường (Movement Area)")]
    [Tooltip("Kéo GameObject Box Collider (như CockroachWL, CockroachWR) vào đây để giới hạn gián 100% chỉ bò trên tường trong khuôn Box này")]
    public BoxCollider movementArea;

    [Header("2. Tốc Độ & Thời Gian Di Chuyển Ngắt Quãng (Stop & Go)")]
    [Tooltip("Tốc độ bò cơ bản trên tường (m/s)")]
    public float crawlSpeed = 1.8f;
    [Tooltip("Tốc độ xoay đầu chuyển hướng")]
    public float turnSpeed = 14.0f;
    [Tooltip("Thời gian bò tối thiểu trước khi dừng (giây)")]
    public float minMoveDuration = 1.0f;
    [Tooltip("Thời gian bò tối đa trước khi dừng (giây)")]
    public float maxMoveDuration = 2.5f;
    [Tooltip("Thời gian dừng lại đứng im tối thiểu (giây)")]
    public float minPauseDuration = 0.5f;
    [Tooltip("Thời gian dừng lại đứng im tối đa (giây)")]
    public float maxPauseDuration = 1.4f;

    [Header("3. Cấu Hình Góc Xoay Mô Hình")]
    [Tooltip("Bù góc xoay mô hình 3D. Mặc định bù 180 độ trục Y để đầu hướng đúng về phía trước")]
    public Vector3 modelRotationOffset = new Vector3(0f, 180f, 0f);

    [Header("4. Cài Đặt Con Gián Cuối Cùng (Jumpscare Bay Vào Mặt)")]
    [Tooltip("Nếu tích chọn (hoặc tự động chỉ định), khi người chơi bấm đập con này sẽ phóng thẳng vào mặt Camera")]
    public bool isLastJumpscareCockroach = false;
    [Tooltip("Thời gian gián phóng vút vào mặt Camera (giây - Mặc định: 0.45s)")]
    public float jumpscareFlyDuration = 0.45f;

    [Header("5. Tên Animation Trong Animator")]
    public string walkAnimState = "giant_cockroach_armature|walking";
    public string flyAnimState = "giant_cockroach_armature|flying_kidnapping";

    [Header("6. Âm Thanh")]
    [Tooltip("Tiếng chân gián bò lạo xạo")]
    public AudioClip skitterSound;
    [Tooltip("Tiếng đập gián bẹp dí (Splat)")]
    public AudioClip squishSound;
    [Tooltip("Tiếng rít / vỗ cánh khi con gián cuối phóng vào mặt")]
    public AudioClip jumpscareFlySound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("7. Tự Động Ẩn Lúc Đầu")]
    public bool hideUntilTriggered = true;

    public bool isDead { get; private set; } = false;
    public bool isCrawling { get; private set; } = false;

    private Animator animator;
    private AudioSource audioSource;
    private Collider cockroachCollider;
    private Coroutine crawlRoutine;
    private Vector3 initialSpawnPos;
    private Quaternion initialSpawnRot;
    private Vector3 currentTargetPoint;
    private Vector3 wallNormalVector = Vector3.forward;

    void Awake()
    {
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        cockroachCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();

        initialSpawnPos = transform.position;
        initialSpawnRot = transform.rotation;
    }

    void Start()
    {
        if (hideUntilTriggered)
        {
            SetVisible(false);
        }

        // Tự động đảm bảo CockroachMinigameManager tồn tại
        if (CockroachMinigameManager.Instance == null)
        {
            CockroachMinigameManager mgr = Object.FindFirstObjectByType<CockroachMinigameManager>(FindObjectsInactive.Include);
            if (mgr == null)
            {
                GameObject mgrObj = new GameObject("CockroachMinigameManager");
                mgrObj.AddComponent<CockroachMinigameManager>();
            }
        }

        // Tự động tìm BoxCollider tường nếu chưa kéo
        if (movementArea == null)
        {
            GameObject wObj = GameObject.Find("CockroachWL") ?? GameObject.Find("CockroachWR") ?? GameObject.Find("CockroachW");
            if (wObj != null) movementArea = wObj.GetComponent<BoxCollider>();
        }

        if (movementArea != null)
        {
            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycastLayer != -1) movementArea.gameObject.layer = ignoreRaycastLayer;
        }
    }

    /// <summary>
    /// Bắt đầu xuất hiện và bò ngắt quãng trong vùng tường quy định
    /// </summary>
    public void StartCrawling()
    {
        if (isDead) return;

        // Bật tất cả GameObject cha nếu cha đang inactive
        Transform p = transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
            p = p.parent;
        }

        gameObject.SetActive(true);
        SetVisible(true);

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[BabyCockroachCrawler] ⚠️ GameObject '{gameObject.name}' chưa thể activeInHierarchy vì một đối tượng cha đang tắt!");
            return;
        }

        isCrawling = true;
        if (cockroachCollider != null) cockroachCollider.enabled = true;

        // Đặt vị trí xuất phát ngẫu nhiên trên mặt tường trong Box
        if (movementArea != null)
        {
            transform.position = GetRandomPointOnWall();
            AlignWithWall();
        }

        if (crawlRoutine != null) StopCoroutine(crawlRoutine);
        crawlRoutine = StartCoroutine(StopAndGoRoutine());
    }

    IEnumerator StopAndGoRoutine()
    {
        if (skitterSound != null && audioSource != null)
        {
            audioSource.clip = skitterSound;
            audioSource.loop = true;
            audioSource.volume = soundVolume * 0.7f;
            audioSource.Play();
        }

        while (!isDead && isCrawling)
        {
            // BƯỚC 1: CHỌN ĐIỂM ĐẾN MỚI TRÊN BỨC TƯỜNG
            currentTargetPoint = GetRandomPointOnWall();

            // BƯỚC 2: BÒ VỀ PHÍA ĐIỂM ĐẾN (BẬT LẠI ANIMATION BƯỚC CHÂN)
            if (animator != null)
            {
                animator.speed = 1f;
                PlayAnim(walkAnimState);
            }
            if (audioSource != null && !audioSource.isPlaying && skitterSound != null) audioSource.Play();

            float moveTimer = 0f;
            float moveDuration = Random.Range(minMoveDuration, maxMoveDuration);

            while (moveTimer < moveDuration && !isDead)
            {
                moveTimer += Time.deltaTime;

                Vector3 toTarget = currentTargetPoint - transform.position;
                float dist = toTarget.magnitude;

                if (dist < 0.12f) break;

                // Xoay đầu hướng về điểm đến trên mặt tường
                Vector3 moveDir = toTarget.normalized;
                Quaternion targetRot = Quaternion.LookRotation(moveDir, wallNormalVector) * Quaternion.Euler(modelRotationOffset);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

                // Di chuyển tới điểm đến
                transform.position = Vector3.MoveTowards(transform.position, currentTargetPoint, crawlSpeed * Time.deltaTime);

                yield return null;
            }

            if (isDead) yield break;

            // BƯỚC 3: DỪNG LẠI ĐỨNG IM TRÊN TƯỜNG (DỪNG ANIMATION CHÂN ĐỂ KHÔNG BỊ TRƯỢT CHÂN TẠI CHỖ)
            if (animator != null)
            {
                animator.speed = 0f; // Đóng băng animation bước chân khi đứng yên
            }
            if (audioSource != null && audioSource.isPlaying && audioSource.clip == skitterSound) audioSource.Pause();

            float pauseDuration = Random.Range(minPauseDuration, maxPauseDuration);
            yield return new WaitForSeconds(pauseDuration);
        }
    }

    /// <summary>
    /// Lấy 1 điểm ngẫu nhiên chính xác trên bề mặt bức tường của BoxCollider
    /// </summary>
    Vector3 GetRandomPointOnWall()
    {
        if (movementArea == null)
        {
            return transform.position + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.5f, 0.5f), 0f);
        }

        Transform boxT = movementArea.transform;
        Vector3 c = movementArea.center;
        Vector3 s = movementArea.size;

        float lx, ly, lz;

        // Xác định mặt phẳng của tường dựa trên trục mỏng nhất
        if (s.x <= s.y && s.x <= s.z)
        {
            // Tường dọc mặt phẳng Y-Z (Bề dày theo trục X)
            lx = c.x;
            ly = Random.Range(c.y - s.y * 0.44f, c.y + s.y * 0.44f);
            lz = Random.Range(c.z - s.z * 0.44f, c.z + s.z * 0.44f);
            wallNormalVector = boxT.TransformDirection(Vector3.right);
        }
        else
        {
            // Tường dọc mặt phẳng X-Y (Bề dày theo trục Z)
            lz = c.z;
            lx = Random.Range(c.x - s.x * 0.44f, c.x + s.x * 0.44f);
            ly = Random.Range(c.y - s.y * 0.44f, c.y + s.y * 0.44f);
            wallNormalVector = boxT.TransformDirection(Vector3.forward);
        }

        return boxT.TransformPoint(new Vector3(lx, ly, lz));
    }

    void AlignWithWall()
    {
        transform.rotation = Quaternion.LookRotation(Vector3.up, wallNormalVector) * Quaternion.Euler(modelRotationOffset);
    }

    // ========================================================
    // TƯƠNG TÁC ĐẬP GIÁN (IINTERACTABLE)
    // ========================================================
    public void Interact()
    {
        if (isDead) return;

        int aliveCount = (CockroachMinigameManager.Instance != null) 
            ? CockroachMinigameManager.Instance.GetAliveBabiesCount() 
            : 1;

        bool isLastOne = (aliveCount <= 1) || isLastJumpscareCockroach;

        Debug.Log($"[BabyCockroachCrawler] ⚡ Người chơi bấm đập con gián: {gameObject.name} (Số gián còn sống: {aliveCount} -> isLastOne: {isLastOne})");

        if (!isLastOne)
        {
            // 1. ĐẬP GIÁN THƯỜNG -> CHẾT BẸP + NẰM NGỬA + BẬT RIGIDBODY RƠI XUỐNG SÀN NHÀ
            KillNormal();
        }
        else
        {
            // 2. ĐẬP CON GIÁN CUỐI CÙNG -> ĐẬP HỤT -> PHÓNG VÚT VÀO MẶT CAMERA -> RƠI XUỐNG SÀN
            StartCoroutine(JumpscareFlyAtPlayerRoutine());
        }
    }

    void KillNormal()
    {
        isDead = true;
        isCrawling = false;

        if (crawlRoutine != null) StopCoroutine(crawlRoutine);
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        // 1. Phát âm thanh đập gián bẹp dí
        if (squishSound != null)
        {
            AudioSource.PlayClipAtPoint(squishSound, transform.position, soundVolume);
        }

        // 2. Dừng animation
        if (animator != null) animator.speed = 0f;

        // 3. Đặt tư thế nằm ngửa bụng (lật ngửa phẳng theo mặt sàn)
        float currentYaw = transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, currentYaw, 180f);

        // 4. Bật Rigidbody rơi tự do theo trọng lực rớt xuống sàn nhà
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.mass = 0.5f;

        // Đẩy nhẹ ra khỏi tường một xíu để rơi thẳng xuống chiếu/sàn mà không bị dính vào mép tường
        Vector3 pushOut = (wallNormalVector != Vector3.zero ? wallNormalVector : transform.forward) * 0.35f;
        rb.linearVelocity = pushOut + Vector3.down * 1.5f;

        // Bỏ qua va chạm với Player
        MovePl player = Object.FindFirstObjectByType<MovePl>();
        if (player != null && cockroachCollider != null)
        {
            Collider[] pCols = player.GetComponentsInChildren<Collider>();
            foreach (var pc in pCols) Physics.IgnoreCollision(cockroachCollider, pc, true);
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) Physics.IgnoreCollision(cockroachCollider, cc, true);
        }

        StartCoroutine(SettleFlatOnFloorRoutine(rb));

        // 5. Báo cho Manager
        if (CockroachMinigameManager.Instance != null)
        {
            CockroachMinigameManager.Instance.NotifyCockroachKilled(this);
        }
    }

    IEnumerator SettleFlatOnFloorRoutine(Rigidbody rb)
    {
        yield return new WaitForSeconds(0.6f);

        // Khi gián đã rơi chạm sàn: Khóa tư thế nằm ngửa hoàn toàn phẳng phiu
        float timer = 0f;
        while (timer < 1.0f)
        {
            timer += Time.deltaTime;
            if (rb != null && rb.linearVelocity.magnitude < 0.1f)
            {
                rb.isKinematic = true;
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 180f);
                break;
            }
            yield return null;
        }
    }

    IEnumerator JumpscareFlyAtPlayerRoutine()
    {
        isDead = true;
        isCrawling = false;

        if (crawlRoutine != null) StopCoroutine(crawlRoutine);
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        Debug.Log("[BabyCockroachCrawler] 😱 ĐẬP HỤT CON GIÁN CUỐI CÙNG! Gián con xòe cánh đập phành phạch phi thẳng vào mặt!");

        // 1. Chuyển gián con sang animation bay xòe cánh
        if (animator != null)
        {
            animator.speed = 1.4f;
            PlayAnim(flyAnimState); // giant_cockroach_armature|flying_kidnapping
        }

        // 2. Phát âm thanh rít bay / đập cánh
        if (jumpscareFlySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpscareFlySound, soundVolume);
        }

        Camera mainCam = Camera.main;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float flyDuration = (jumpscareFlyDuration > 0f) ? jumpscareFlyDuration : 0.42f;
        float elapsed = 0f;

        // 3. Xoay đầu tự nhiên và phóng vút vào mặt Camera
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            float easeT = t * t * t; // Tăng tốc dữ dội về cuối

            if (mainCam != null)
            {
                Vector3 targetCamPos = mainCam.transform.position + mainCam.transform.forward * 0.25f + mainCam.transform.up * -0.04f;
                transform.position = Vector3.Lerp(startPos, targetCamPos, easeT);

                // Hướng đầu theo quỹ đạo bay mượt mà (không bị giật đứng thẳng góc)
                Vector3 flyDir = (targetCamPos - transform.position).normalized;
                if (flyDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(flyDir, Vector3.up) * Quaternion.Euler(modelRotationOffset);
                    transform.rotation = Quaternion.Slerp(startRot, targetRot, Mathf.Clamp01(t * 4f));
                }
            }

            yield return null;
        }

        // 4. NGAY KHI GIÁN CON VỪA CHẠM MẶT CAMERA -> ẨN GIÁN CON VÀ KÍCH HOẠT NGAY JUMPSCARE CỦA GIÁN TO BÁM MÀN HÌNH!
        gameObject.SetActive(false);

        CockroachFlyAttack flyAttack = Object.FindFirstObjectByType<CockroachFlyAttack>(FindObjectsInactive.Include);
        if (flyAttack != null)
        {
            Debug.Log("[BabyCockroachCrawler] 💥 Chạm mặt Camera! KÍCH HOẠT NGAY JUMPSCARE GIÁN TO BÁM KÍNH + HOẢNG LOẠN!");
            flyAttack.TriggerInstantCameraJumpscare();
        }

        // Báo cho Manager hoàn tất
        if (CockroachMinigameManager.Instance != null)
        {
            CockroachMinigameManager.Instance.NotifyCockroachKilled(this);
        }
    }

    void PlayAnim(string stateName)
    {
        if (animator != null && !string.IsNullOrEmpty(stateName))
        {
            animator.CrossFadeInFixedTime(stateName, 0.1f);
        }
    }

    void SetVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = visible;
    }
}
