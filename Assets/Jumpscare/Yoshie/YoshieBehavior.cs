using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý hành vi quái 'Yoshie' (Mặt Quỷ Bay) trong Map 03:
/// 1. Săn đuổi Player liên tục trong rừng.
/// 2. ÂM THANH TIẾP CẬN 3D (PROXIMITY THEME):
///    - 100% 3D Sound (spatialBlend = 1.0f).
///    - Tính toán khoảng cách thực tế: Ngoài phạm vi proximityMaxDistance (ví dụ 20m) âm lượng tắt hoàn toàn = 0.
///    - Càng tiến lại gần trong phạm vi proximityMaxDistance -> proximityMinDistance thì âm lượng càng to dần.
/// 3. KHI BẮT ĐƯỢC NGƯỜI CHƠI (JUMPSCARE & GAME OVER):
///    - Tổng thời lượng Jumpscare = 3.0s (inCameraJumpscareDuration).
///    - 1.5s đầu: Mặt quỷ bay từ dưới lên, rung lắc camera, nhạc Theme Yoshie chuyển sang 2D Full Volume.
///    - 1.5s sau: Bắt đầu Fade mờ đen dần PHỦ LÊN MẶT QUỶ VẪN ĐANG GIỮ TRÊN MÀN HÌNH cho đến khi tối đen hoàn toàn.
///    - Màn hình đen tinh khiết (100% không có bất kỳ chữ nào), bấm phím/chuột bất kỳ để quay về MainMenu.
///    - Nếu bật GodMode: Tha chết, mở lại điều khiển, reset Yoshie về vị trí xuất phát để tiếp tục chơi.
/// </summary>
public class YoshieBehavior : MonoBehaviour
{
    public enum YoshieState
    {
        Chasing,            // Đang săn đuổi Player
        Stunned,            // Bị choáng do đèn pin chớp sáng (Flash Burst Stun)
        StaringAtSafeZone,  // Đứng nhìn chằm chằm Player ở ngoài ranh giới SafeZone
        RetreatingHome,     // Đang bay bỏ đi về vị trí mặc định
        IdleAtHome,         // Đã về đến chỗ mặc định, đứng chờ
        Attacking           // Bắt được Player -> Đang diễn Jumpscare
    }

    [Header("1. AI Settings (Cấu Hình Săn Đuổi)")]
    [Tooltip("Tốc độ bay đuổi theo Player của Yoshie")]
    public float moveSpeed = 24f;

    [Tooltip("Tốc độ bay bỏ đi về vị trí mặc định")]
    public float retreatSpeed = 40f;

    [Tooltip("Khoảng cách kích hoạt Game Over / Bị bắt (khi ở ngoài SafeZone)")]
    public float killDistance = 3.5f;

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

    [Header("3. Âm Thanh Tiếp Cận 3D (Proximity Theme Music)")]
    [Tooltip("AudioSource phát nhạc Theme 3D xung quanh Yoshie")]
    public AudioSource themeAudioSource;

    [Tooltip("Nhạc Theme tiếp cận của Yoshie (Death-Forest-OST-Yoshie-Kimura-Theme...) - Tự động phát to dần khi lại gần Player")]
    public AudioClip proximityThemeClip;

    [Range(0f, 1f)] public float proximityVolume = 0.75f;
    [Tooltip("Khoảng cách bắt đầu nghe thấy tiếng nhạc Theme (mét - Mặc định: 20m)")]
    public float proximityMaxDistance = 20f;
    [Tooltip("Khoảng cách nhạc Theme to nhất 100% (mét - Mặc định: 1m - 3m)")]
    public float proximityMinDistance = 1f;

    [Header("4. Âm Thanh Hành Vi Khác (Audio Sfx)")]
    public AudioSource sfxAudioSource;
    [Tooltip("Âm thanh gầm gừ / thở dốc khi đứng nhìn ở ranh giới SafeZone")]
    public AudioClip stareSound;
    [Tooltip("Âm thanh ma quái khi quay lưng bỏ đi")]
    public AudioClip retreatSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("5. Cơ Chế Bị Choáng Do Đèn Pin (Flashlight Stun)")]
    [Tooltip("Bật tính năng bị chói mắt/làm choáng khi người chơi bấm Chuột Phải chớp đèn pin")]
    public bool enableFlashlightStun = true;

    [Tooltip("Thời gian Yoshie bị choáng bất động khi bị chớp đèn (giây - Mặc định: 3.5s)")]
    public float stunDuration = 3.5f;

    [Tooltip("Khoảng cách Yoshie bị giật lùi ra sau khi trúng flash (mét - Mặc định: 2.5m)")]
    public float stunPushbackDistance = 2.5f;

    [Tooltip("Âm thanh gầm thét/đau đớn khi bị chớp đèn pin")]
    public AudioClip stunSound;

    [Header("6. Jumpscare & Game Over (Bắt Được Người Chơi)")]
    [Tooltip("Kéo GameObject 'Yoshie' (trong Main Camera > Jumpscare > Yoshie) vào đây (Để trống tự động tìm)")]
    public GameObject yoshieInCameraObject;

    [Tooltip("Kéo GameObject 'EndG' (trong Canvas UI > EndG) vào đây (Để trống tự động tìm)")]
    public GameObject endScreenObject;

    [Tooltip("Kéo Sprite ảnh tử nạn 'End' (trong Assets/UI/End) vào đây (Để trống tự động tìm)")]
    public Sprite endScreenSprite;

    [Tooltip("Âm thanh Jumpscare bổ sung khi bị bắt (Để trống vẫn phát nguyên vẹn bài nhạc Theme Yoshie)")]
    public AudioClip jumpscareSound;

    [Range(0f, 1f)] public float jumpscareVolume = 1.0f;

    [Tooltip("Độ cao bay nhẹ từ dưới lên của mặt quỷ trong camera (mét - Mặc định: 1.2m)")]
    public float floatUpFromBelowDistance = 1.2f;

    [Tooltip("Thời gian bay nhẹ từ dưới lên (giây - Mặc định: 0.4s)")]
    public float floatUpDuration = 0.4f;

    [Tooltip("Cường độ rung lắc Camera khi bị bắt (Mặc định: 0.15)")]
    public float cameraShakeIntensity = 0.15f;

    [Tooltip("Tổng thời gian Jumpscare trước khi tối đen hoàn toàn (giây - Mặc định: 3.0s, chia đôi 1.5s bắt đầu fade)")]
    public float inCameraJumpscareDuration = 3.0f;

    [Tooltip("Tự động ẩn UI khi bị bắt (Mặc định: True)")]
    public bool hideUIOnAttack = true;

    [Tooltip("Tên Scene Menu khi thua (Mặc định: 'MainMenu')")]
    public string mainMenuSceneName = "MainMenu";

    [Header("7. Trạng Thái Hiện Tại (State)")]
    public YoshieState currentState = YoshieState.Chasing;
    public bool isPlayerInSafeZone = false;
    public bool hasCaughtPlayer = false;

    // --- Private Variables ---
    private Transform player;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private float stareTimer = 0f;
    private float stunTimer = 0f;
    private Collider activeSafeZoneCollider;
    private Vector3 initialYoshieLocalPos;

    void Awake()
    {
        homePosition = (defaultHomePoint != null) ? defaultHomePoint.position : transform.position;
        homeRotation = (defaultHomePoint != null) ? defaultHomePoint.rotation : transform.rotation;

        if (sfxAudioSource == null) sfxAudioSource = GetComponent<AudioSource>();
        if (sfxAudioSource == null)
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
            sfxAudioSource.spatialBlend = 1f;
            sfxAudioSource.playOnAwake = false;
        }

        if (themeAudioSource == null)
        {
            themeAudioSource = gameObject.AddComponent<AudioSource>();
        }

        FindYoshieInCameraObject();
        AutoFindAudioClips();
        ConfigureThemeAudio();
    }

    void Start()
    {
        FindPlayer();
        FindYoshieInCameraObject();
        if (yoshieInCameraObject != null)
        {
            initialYoshieLocalPos = yoshieInCameraObject.transform.localPosition;
            yoshieInCameraObject.SetActive(false);
        }

        ConfigureThemeAudio();
    }

    private void OnValidate()
    {
        ConfigureThemeAudio();
    }

    public void ConfigureThemeAudio()
    {
        if (themeAudioSource == null)
        {
            themeAudioSource = GetComponent<AudioSource>();
            if (themeAudioSource == null) return;
        }

        themeAudioSource.loop = true;
        themeAudioSource.spatialBlend = 1.0f; // 100% 3D Sound thuần túy
        themeAudioSource.minDistance = Mathf.Max(0.1f, proximityMinDistance);
        themeAudioSource.maxDistance = Mathf.Max(themeAudioSource.minDistance + 0.5f, proximityMaxDistance);
        themeAudioSource.rolloffMode = AudioRolloffMode.Linear;
        themeAudioSource.dopplerLevel = 0f;
        themeAudioSource.volume = 0f;

        if (proximityThemeClip != null)
        {
            themeAudioSource.clip = proximityThemeClip;
            if (!themeAudioSource.isPlaying && Application.isPlaying && gameObject.activeInHierarchy && !hasCaughtPlayer)
            {
                themeAudioSource.Play();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, proximityMaxDistance);

        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, proximityMinDistance);
    }

    private void FindYoshieInCameraObject()
    {
        if (yoshieInCameraObject != null) return;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.name == "Yoshie" && obj.transform.root != transform.root)
            {
                if (obj.transform.parent != null && obj.transform.parent.name.Contains("Jumpscare"))
                {
                    yoshieInCameraObject = obj;
                    initialYoshieLocalPos = yoshieInCameraObject.transform.localPosition;
                    break;
                }
            }
        }
    }

    private void AutoFindAudioClips()
    {
        AudioClip[] allClips = Resources.FindObjectsOfTypeAll<AudioClip>();

        if (proximityThemeClip == null)
        {
            foreach (var c in allClips)
            {
                if (c == null) continue;
                string n = c.name.ToLower();
                if (n.Contains("death-forest-ost-yoshie") || n.Contains("yoshie-kimura-theme") || n.Contains("tw031"))
                {
                    proximityThemeClip = c;
                    break;
                }
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
            if (movePl != null)
            {
                player = movePl.transform;
            }
        }
    }

    void Update()
    {
        if (hasCaughtPlayer) return;

        if (player == null)
        {
            FindPlayer();
            return;
        }

        if (defaultHomePoint != null)
        {
            homePosition = defaultHomePoint.position;
        }

        UpdateProximityVolume();

        switch (currentState)
        {
            case YoshieState.Chasing:
                UpdateChasing();
                break;

            case YoshieState.Stunned:
                UpdateStunned();
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

    private void UpdateProximityVolume()
    {
        if (themeAudioSource == null || proximityThemeClip == null || player == null) return;

        if (!themeAudioSource.isPlaying)
        {
            themeAudioSource.Play();
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist >= proximityMaxDistance)
        {
            themeAudioSource.volume = 0f;
        }
        else if (dist <= proximityMinDistance)
        {
            themeAudioSource.volume = proximityVolume;
            themeAudioSource.spatialBlend = 1.0f;
        }
        else
        {
            float t = (dist - proximityMinDistance) / (proximityMaxDistance - proximityMinDistance);
            themeAudioSource.volume = Mathf.Lerp(proximityVolume, 0f, t);
            themeAudioSource.spatialBlend = 1.0f;
        }
    }

    // =========================================================================
    // 0. TRẠNG THÁI BỊ CHOÁNG / CHÓI MẮT (STUNNED)
    // =========================================================================

    public void OnCameraFlashStunned()
    {
        if (!enableFlashlightStun || hasCaughtPlayer) return;

        if (currentState == YoshieState.StaringAtSafeZone || currentState == YoshieState.RetreatingHome)
        {
            return;
        }

        currentState = YoshieState.Stunned;
        stunTimer = stunDuration;

        Debug.Log($"<color=yellow><b>[YoshieBehavior] ⚡ Yoshie bị CHỚP ĐÈN PIN LÀM CHÓI MẮT! Bị choáng {stunDuration}s!</b></color>");

        if (stunSound != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(stunSound, soundVolume);
        }
        else if (stareSound != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(stareSound, soundVolume);
        }

        if (player != null && stunPushbackDistance > 0.1f)
        {
            Vector3 pushDir = (transform.position - player.position);
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude > 0.001f)
            {
                transform.position += pushDir.normalized * stunPushbackDistance;
            }
        }
    }

    private void UpdateStunned()
    {
        RotateTowardsPlayer();

        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f)
        {
            Debug.Log("[YoshieBehavior] 👹 Yoshie đã hết choáng -> Tiếp tục lao tới săn đuổi!");
            currentState = YoshieState.Chasing;
        }
    }

    // =========================================================================
    // 1. TRẠNG THÁI SĂN ĐUỔI (CHASING)
    // =========================================================================
    private void UpdateChasing()
    {
        if (hasCaughtPlayer) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (isPlayerInSafeZone)
        {
            if (activeSafeZoneCollider != null)
            {
                Vector3 closestBoundaryPoint = activeSafeZoneCollider.ClosestPoint(transform.position);
                float distToBoundary = Vector3.Distance(transform.position, closestBoundaryPoint);

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
            if (distToPlayer <= killDistance)
            {
                CatchPlayer();
                return;
            }
        }

        ChasePlayer();
    }

    public void OnHitSafeZoneBarrier()
    {
        if (currentState == YoshieState.StaringAtSafeZone || currentState == YoshieState.RetreatingHome) return;

        currentState = YoshieState.StaringAtSafeZone;
        stareTimer = 0f;

        if (stareSound != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(stareSound, soundVolume);
        }

        Debug.Log("[YoshieBehavior] ⛩️ Yoshie bị ranh giới SafeZone chặn lại! Đang đứng nhìn trừng trừng...");
    }

    // =========================================================================
    // 2. TRẠNG THÁI ĐỨNG NHÌN Ở SAFEZONE (STARING)
    // =========================================================================
    private void UpdateStaring()
    {
        if (!isPlayerInSafeZone)
        {
            currentState = YoshieState.Chasing;
            Debug.Log("[YoshieBehavior] 👹 Player vừa rời khỏi SafeZone! Yoshie tiếp tục lao tới rượt đuổi!");
            return;
        }

        RotateTowardsPlayer();

        stareTimer += Time.deltaTime;
        if (stareTimer >= stareDurationAtSafeZone)
        {
            currentState = YoshieState.RetreatingHome;

            if (retreatSound != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(retreatSound, soundVolume);
            }

            Debug.Log("[YoshieBehavior] 💨 Yoshie đã nhìn xong, quay đầu bỏ đi về vị trí mặc định!");
        }
    }

    // =========================================================================
    // 3. TRẠNG THÁI BAY LÙI VỀ CHỖ MẶC ĐỊNH (RETREATING HOME)
    // =========================================================================
    private void UpdateRetreating()
    {
        if (!isPlayerInSafeZone)
        {
            currentState = YoshieState.Chasing;
            Debug.Log("[YoshieBehavior] 👹 Player ra khỏi SafeZone -> Yoshie quay lại săn đuổi!");
            return;
        }

        Vector3 dirToHome = (homePosition - transform.position);
        dirToHome.y = 0f;
        float distToHome = dirToHome.magnitude;

        if (distToHome > 0.8f)
        {
            Vector3 moveDir = dirToHome.normalized;
            Vector3 nextPos = transform.position + moveDir * retreatSpeed * Time.deltaTime;
            nextPos.y = CalculateHoverY(nextPos, homePosition.y);
            transform.position = nextPos;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, homePosition, 5f * Time.deltaTime);
            currentState = YoshieState.IdleAtHome;
            Debug.Log("[YoshieBehavior] 🏡 Yoshie đã lùi về đến vị trí mặc định an toàn!");
        }

        RotateTowardsPlayer();
    }

    // =========================================================================
    // 4. TRẠNG THÁI ĐỨNG CHỜ Ở VỊ TRÍ MẶC ĐỊNH (IDLE AT HOME)
    // =========================================================================
    private void UpdateIdleAtHome()
    {
        RotateTowardsPlayer();

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
            if (hit.collider == null) continue;
            if (hit.collider.CompareTag("Player") || hit.collider.transform.root == transform.root) continue;
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

    // =========================================================================
    // 5. ATTACK & JUMPSCARE (BẮT ĐƯỢC PLAYER -> JUMPSCARE, FADE ĐEN PHỦ LÊN VÀ VỀ MENU)
    // =========================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (hasCaughtPlayer) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            if (!isPlayerInSafeZone)
            {
                CatchPlayer();
            }
        }
        else if (other.GetComponent<YoshieSafeZone>() != null || (activeSafeZoneCollider != null && other == activeSafeZoneCollider))
        {
            if (isPlayerInSafeZone)
            {
                OnHitSafeZoneBarrier();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCaughtPlayer) return;

        if (collision.collider.CompareTag("Player") || collision.collider.GetComponentInParent<MovePl>() != null)
        {
            if (!isPlayerInSafeZone)
            {
                CatchPlayer();
            }
        }
    }

    private void CatchPlayer()
    {
        if (hasCaughtPlayer) return;
        hasCaughtPlayer = true;

        currentState = YoshieState.Attacking;
        Debug.Log("<color=red><b>[YoshieBehavior] 💀 YOSHIE ĐÃ BẮT ĐƯỢC BẠN! Kích hoạt In-Camera Jumpscare & Tiếp tục phát nhạc Theme...</b></color>");

        // CHUYỂN BÀI NHẠC THEME SANG 2D FULL VOLUME THẲNG VÀO TAI
        if (themeAudioSource != null)
        {
            themeAudioSource.spatialBlend = 0f;
            themeAudioSource.volume = jumpscareVolume;
            if (proximityThemeClip != null && !themeAudioSource.isPlaying)
            {
                themeAudioSource.clip = proximityThemeClip;
                themeAudioSource.Play();
            }
        }

        // Ẩn mô hình Yoshie ngoài thế giới
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = false;

        Collider[] cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = false;

        StartCoroutine(YoshieCameraJumpscareRoutine());
    }

    private IEnumerator FloatUpRoutine(Transform targetTrans, Vector3 fromPos, Vector3 toPos, float duration)
    {
        if (targetTrans == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            targetTrans.localPosition = Vector3.Lerp(fromPos, toPos, easeT);
            yield return null;
        }

        targetTrans.localPosition = toPos;
    }

    private IEnumerator CameraShakeRoutine(Transform camTrans, float duration, float intensity)
    {
        if (camTrans == null) yield break;

        Vector3 originalLocalPos = camTrans.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentIntensity = Mathf.Lerp(intensity, 0.02f, elapsed / duration);
            Vector3 randomOffset = Random.insideUnitSphere * currentIntensity;
            camTrans.localPosition = originalLocalPos + randomOffset;
            yield return null;
        }

        camTrans.localPosition = originalLocalPos;
    }

    private IEnumerator YoshieCameraJumpscareRoutine()
    {
        if (yoshieInCameraObject == null) FindYoshieInCameraObject();

        // 1. Khóa di chuyển và góc nhìn của người chơi
        MovePl playerMove = Object.FindFirstObjectByType<MovePl>();
        if (playerMove != null)
        {
            playerMove.isCameraLocked = true;
            playerMove.enabled = false;
        }

        // 2. Ẩn toàn bộ UI / HUD trong lúc bị bắt
        List<Canvas> hiddenCanvases = new List<Canvas>();
        if (hideUIOnAttack)
        {
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c != null && c.enabled && !c.name.Contains("Fade") && !c.name.Contains("GameOver") && !c.name.Contains("Jumpscare"))
                {
                    c.enabled = false;
                    hiddenCanvases.Add(c);
                }
            }
        }

        Transform camToShake = null;
        if (Camera.main != null) camToShake = Camera.main.transform;
        else
        {
            Camera anyCam = Object.FindFirstObjectByType<Camera>();
            if (anyCam != null) camToShake = anyCam.transform;
        }

        float totalDuration = (inCameraJumpscareDuration > 0.5f) ? inCameraJumpscareDuration : 3.0f;
        float halfDuration = totalDuration * 0.5f; // 1.5s đầu sáng rõ, 1.5s sau fade đen

        if (yoshieInCameraObject != null)
        {
            yoshieInCameraObject.SetActive(true);

            Renderer[] rends = yoshieInCameraObject.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends) r.enabled = true;

            // HIỆU ỨNG BAY NHẸ TỪ DƯỚI LÊN
            Vector3 targetLocalPos = (initialYoshieLocalPos != Vector3.zero) ? initialYoshieLocalPos : yoshieInCameraObject.transform.localPosition;
            Vector3 startLocalPos = targetLocalPos + Vector3.down * floatUpFromBelowDistance;
            yoshieInCameraObject.transform.localPosition = startLocalPos;

            StartCoroutine(FloatUpRoutine(yoshieInCameraObject.transform, startLocalPos, targetLocalPos, floatUpDuration));

            // PHÁT ÂM THANH JUMPSCARE BỔ SUNG (NẾU CÓ)
            if (jumpscareSound != null)
            {
                AudioSource camAudio = yoshieInCameraObject.GetComponent<AudioSource>();
                if (camAudio == null) camAudio = yoshieInCameraObject.AddComponent<AudioSource>();
                camAudio.spatialBlend = 0f;
                camAudio.PlayOneShot(jumpscareSound, jumpscareVolume);
            }

            // RUNG LẮC CAMERA TRONG SUỐT THỜI GIAN JUMPSCARE
            if (camToShake != null && cameraShakeIntensity > 0.001f)
            {
                StartCoroutine(CameraShakeRoutine(camToShake, totalDuration, cameraShakeIntensity));
            }

            // CHỜ 1.5S ĐẦU TIÊN KHI MẶT QUỶ HIỆN RÕ NÉT
            yield return new WaitForSeconds(halfDuration);

            // KIỂM TRA CHẾ ĐỘ BẤT TỬ (GOD MODE)
            if (GodModeManager.IsGodModeActive)
            {
                Debug.Log("<color=cyan><b>[YoshieBehavior] 👑 GodMode đang BẬT -> Tha chết cho Player đi tiếp!</b></color>");

                yoshieInCameraObject.SetActive(false);

                if (playerMove != null)
                {
                    playerMove.isCameraLocked = false;
                    playerMove.enabled = true;
                    playerMove.SetMovementState(true);
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                foreach (var c in hiddenCanvases)
                {
                    if (c != null) c.enabled = true;
                }

                // Reset lại Yoshie về vị trí xuất phát ban đầu để tiếp tục chơi/bắt tiếp
                hasCaughtPlayer = false;
                isPlayerInSafeZone = false;
                transform.position = homePosition;
                transform.rotation = homeRotation;

                Renderer[] rList = GetComponentsInChildren<Renderer>(true);
                foreach (var r in rList) r.enabled = true;

                Collider[] cList = GetComponentsInChildren<Collider>(true);
                foreach (var col in cList) col.enabled = true;

                currentState = YoshieState.Chasing;
                this.enabled = true;

                ConfigureThemeAudio();
                yield break;
            }

            // NẾU KHÔNG BẬT GOD MODE -> KÍCH HOẠT CHUỖI GAMEOVER (FADE ĐEN -> BẬT EndG -> FADE IN MỞ ẢNH -> CLICK -> FADE ĐEN VỀ MENU)
            GameOverJumpscareManager.Instance.TriggerGameOverDeathScreen(yoshieInCameraObject, halfDuration, mainMenuSceneName, endScreenObject, endScreenSprite);
            if (themeAudioSource != null) themeAudioSource.Stop();
            yield break;
        }
        else
        {
            yield return new WaitForSeconds(halfDuration);

            if (GodModeManager.IsGodModeActive)
            {
                if (playerMove != null)
                {
                    playerMove.isCameraLocked = false;
                    playerMove.enabled = true;
                    playerMove.SetMovementState(true);
                }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                hasCaughtPlayer = false;
                isPlayerInSafeZone = false;
                transform.position = homePosition;
                transform.rotation = homeRotation;

                Renderer[] rList = GetComponentsInChildren<Renderer>(true);
                foreach (var r in rList) r.enabled = true;

                Collider[] cList = GetComponentsInChildren<Collider>(true);
                foreach (var col in cList) col.enabled = true;

                currentState = YoshieState.Chasing;
                this.enabled = true;

                ConfigureThemeAudio();
                yield break;
            }

            GameOverJumpscareManager.Instance.TriggerGameOverDeathScreen(null, halfDuration, mainMenuSceneName, endScreenObject, endScreenSprite);
            if (themeAudioSource != null) themeAudioSource.Stop();
            yield break;
        }
    }

    private IEnumerator FadeToBlackPureRoutine(float duration)
    {
        // 1. Tạo Canvas Fade Đen thuần khiết (KHÔNG CÓ CHỮ)
        GameObject canvasObj = new GameObject("YoshieGameOverFadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 999999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject imgObj = new GameObject("BlackOverlay");
        imgObj.transform.SetParent(canvasObj.transform, false);

        Image blackImg = imgObj.AddComponent<Image>();
        blackImg.color = new Color(0f, 0f, 0f, 0f);
        blackImg.raycastTarget = false;

        RectTransform rt = blackImg.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float startVolume = (themeAudioSource != null) ? themeAudioSource.volume : 1f;

        // Fade đen phủ lên mặt Yoshie đang hiện
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            blackImg.color = new Color(0f, 0f, 0f, t);

            if (themeAudioSource != null)
            {
                themeAudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            }

            yield return null;
        }

        blackImg.color = Color.black;
    }

    private IEnumerator WaitForClickAndReturnToMenuPureRoutine()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return new WaitForSeconds(0.2f);

        // Chờ người chơi nhấp chuột hoặc bấm bất kỳ phím nào (không cần hiện chữ)
        while (!Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
        {
            yield return null;
        }

        string sceneToLoad = !string.IsNullOrEmpty(mainMenuSceneName) ? mainMenuSceneName : "MainMenu";
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
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
