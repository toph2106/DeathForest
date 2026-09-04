using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;

/// <summary>
/// Quản lý AI & Hành vi của Ma Nữ Tóc Dài (LongHairWomen) trong Map 03:
/// 1. ĐỨNG IM TẠI CHỖ (Không săn đuổi, không di chuyển, chỉ xoay mặt nhìn Player).
/// 2. ĐÃ XÓA BỎ HOÀN TOÀN cơ chế SafeZone.
/// 3. CƠ CHẾ ÂM THANH, DIỄN XUẤT & JUMPSCARE:
///    - ĐỢT 1: Phát tiếng cười cảnh báo từ xa (đang tàng hình 100%).
///    - KHOẢNG NGHỈ: Hết tiếng cười đợt 1 -> Chờ đúng 3 GIÂY (3s).
///    - ĐỢT 2: Hiện hình 3D sát mép vòng quanh Player -> Diễn hết Animation Cười (smileLaughAnimation).
///             Hết Animation Cười -> Mới phát Tiếng Cười ĐỢT 2!
///             (Trong giai đoạn này nếu bị chớp đèn Flash -> Biến mất ngay).
///    - KHỰNG & BAY: Hết tiếng cười đợt 2 -> Kích hoạt Animation Appear & KHỰNG TẠI CHỖ (appearHoldDuration) -> Rồi mới BAY LIÊN TỤC (flightDurationBeforeJumpscare)!
///    - BÙNG NỔ JUMPSCARE (LongHairWomenJC): Sau khi bay xong -> Kích hoạt Jumpscare trước mặt Camera trong Main kèm âm thanh thét!
///    - HẾT JUMPSCARE: Biến mất cả 2 đối tượng và bắt đầu quay lại bộ đếm 3 phút (180s).
/// 4. ĐIỀU KIỆN SỰ KIỆN: Người chơi phải nhặt đủ 2 CÁNH TAY (ArmsL & ArmsR) mới bắt đầu đếm giờ!
/// 5. RANDOM SPAWN 3 PHÚT 1 LẦN (Tỉ lệ 33.3% / 100% ~ 3/10).
/// 6. PHÍM TẮT DEMO: Bấm phím 'L' để kích hoạt xuất hiện con ma ngay lập tức quanh Player!
/// </summary>
public class LongHairWomenBehavior : MonoBehaviour
{
    [Header("1. Cấu Hình Xuất Hiện Ngẫu Nhiên (Random Spawner)")]
    [Tooltip("Bật tính năng tự động đếm giờ và random xuất hiện quanh Player")]
    public bool enableAutoSpawning = true;

    [Tooltip("Bắt buộc phải nhặt đủ 2 Cánh Tay (ArmsL & ArmsR) trong túi đồ mới bắt đầu đếm giờ xuất hiện")]
    public bool requireBothArmsToSpawn = true;

    [Tooltip("Khoảng thời gian giữa mỗi lần random kiểm tra xuất hiện (giây - Mặc định: 180s = 3 phút)")]
    public float checkInterval = 180f;

    [Tooltip("Tỉ lệ phần trăm xuất hiện mỗi 3 phút (Mặc định: 33.3% tương đương 3/10)")]
    [Range(1f, 100f)]
    public float spawnChancePercent = 33.3f;

    [Tooltip("Khoảng cách xuất hiện sát mép vòng tròn quanh Player (mét - Mặc định: 3.0m - 4.0m)")]
    public float minSpawnDistance = 3.0f;

    [Tooltip("Khoảng cách xuất hiện xa nhất (mét - Mặc định: 4.0m sát mép vòng)")]
    public float maxSpawnDistance = 4.0f;

    [Header("2. Phím Tắt Demo Test Nhanh (Debug Key)")]
    [Tooltip("Bấm phím này để ép con ma xuất hiện ngay lập tức quanh Player (Mặc định: Phím L)")]
    public KeyCode debugSpawnKey = KeyCode.L;

    [Header("3. Bám Sát Mặt Đất (Ground Snapping) & Góc Nhìn")]
    public bool snapToGround = true;
    public float groundOffsetY = 0.0f;
    public float raycastHeightAbove = 3.0f;
    public float raycastDistance = 15.0f;
    public LayerMask groundLayerMask = ~0;

    [Tooltip("Góc xoay bù trừ mô hình nếu bị lệch hướng (X, Y, Z)")]
    public Vector3 modelRotationOffset = Vector3.zero;

    [Tooltip("Tốc độ xoay mặt hướng theo Player")]
    public float lookAtPlayerSpeed = 5.0f;

    [Header("4. Âm Thanh Tiếng Cười Ma Quái 2 Đợt (Audio Sfx)")]
    public AudioSource audioSource;

    [Tooltip("Tiếng cười ĐỢT 1: Cười cảnh báo từ xa khi còn tàng hình (Để trống sẽ dùng chung Laugh Sound)")]
    public AudioClip warningLaughSound;

    [Tooltip("Tiếng cười ĐỢT 2: Cười sau khi Animation Cười diễn xong")]
    public AudioClip laughSound;

    [Tooltip("Thời gian chờ sau khi hết tiếng cười Đợt 1 rồi mới kích hoạt animation cười (giây - Mặc định: 3.0s)")]
    public float delayAfterWarningLaugh = 3.0f;

    [Range(0f, 1f)]
    public float soundVolume = 1.0f;

    [Tooltip("Tỉ lệ không gian 3D (0.5 = vừa có hướng 3D trái/phải vừa nghe to rõ trong rừng)")]
    [Range(0f, 1f)]
    public float spatialBlend = 0.5f;

    [Tooltip("Khoảng cách tối thiểu âm thanh to nhất 100% (mét - Mặc định: 15m)")]
    public float audioMinDistance = 15f;

    [Tooltip("Khoảng cách tối đa tiếng cười vang xa (mét - Mặc định: 120m)")]
    public float audioMaxDistance = 120f;

    [Header("5. Hai Ô Hoạt Ảnh (Animation Clips)")]
    [Tooltip("Ô 1: Kéo Animation Cười (VD: LHW_evnt_smile hoặc ENM_LHW_hyokkori) vào đây - diễn hết mới cười đợt 2")]
    public AnimationClip smileLaughAnimation;

    [Tooltip("Ô 2: Kéo Animation Xuất Hiện / Bay (VD: LHW_CH_evnt_appear) vào đây - khựng rồi bay liên tục")]
    public AnimationClip appearDashAnimation;

    [Header("6. Cấu Hình Khựng & Bay (Appear Hold & Flight)")]
    [Tooltip("Thời gian khựng tại chỗ để kích hoạt/diễn Animation Appear trước khi bay lên (giây - Mặc định: 1.0s)")]
    public float appearHoldDuration = 1.0f;

    [Tooltip("Thời gian bay liên tục trước khi kích hoạt Jumpscare (giây - Mặc định: 5.0s)")]
    public float flightDurationBeforeJumpscare = 5.0f;

    [Tooltip("Tốc độ bay lướt sang phải (m/s - Mặc định: 20m/s)")]
    public float dashRightSpeed = 20.0f;

    [Tooltip("Tốc độ bay nhẹ lên cao khi lướt (m/s - Mặc định: 2.5m/s)")]
    public float flyUpSpeed = 2.5f;

    [Header("7. Cấu Hình Jumpscare (LongHairWomenJC)")]
    [Tooltip("Kéo GameObject Jumpscare (LongHairWomenJC trong Main/Jumpscare) vào đây (Để trống sẽ tự động tìm)")]
    public GameObject jumpscareTargetObject;

    [Tooltip("Animation Clip Jumpscare trước mặt Camera (Tùy chọn, để trống sẽ dùng Controller mặc định của JC)")]
    public AnimationClip jumpscareAnimation;

    [Tooltip("Âm thanh Jumpscare hét thẳng vào mặt Player")]
    public AudioClip jumpscareSound;

    [Range(0f, 1f)]
    public float jumpscareVolume = 1.0f;

    [Tooltip("Thời gian diễn Jumpscare trước khi biến mất cả 2 (giây - Mặc định: 2.5s)")]
    public float jumpscareDuration = 2.5f;

    [Header("8. Trạng Thái Hiện Tại (State)")]
    public bool isCurrentlyActive = false;
    public bool isWarningPhase = false;
    public bool isDashing = false;
    public bool hasAcquiredBothArms = false;
    public float timeUntilNextCheck = 180f;

    // --- Private Variables ---
    private Transform player;
    private Animator animator;
    private PlayableGraph playableGraph;
    private Renderer[] allRenderers;
    private Collider[] allColliders;
    private bool isVanishing = false;
    private Coroutine activeLifeCoroutine;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureAudioSource();

        animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = GetComponent<Animator>();

        allRenderers = GetComponentsInChildren<Renderer>(true);
        allColliders = GetComponentsInChildren<Collider>(true);

        AutoFindAudioAndAnimationClips();
        FindJumpscareObject();
    }

    void OnDestroy()
    {
        if (playableGraph.IsValid()) playableGraph.Destroy();
    }

    void OnDisable()
    {
        if (playableGraph.IsValid()) playableGraph.Destroy();
    }

    void Start()
    {
        FindPlayer();
        FindJumpscareObject();
        if (jumpscareTargetObject != null) jumpscareTargetObject.SetActive(false);

        timeUntilNextCheck = checkInterval;

        if (enableAutoSpawning)
        {
            // Ban đầu ẩn con ma đi để chờ người chơi nhặt đủ 2 tay và đếm chu kỳ 3 phút
            HideGhost();
            StartCoroutine(SpawnTimerRoutine());
        }
        else
        {
            // Nếu không bật Auto Spawning (đặt thủ công) -> Kích hoạt ngay tại chỗ
            SpawnAtCurrentPosition();
        }
    }

    private void FindJumpscareObject()
    {
        if (jumpscareTargetObject != null) return;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.name == "LongHairWomenJC" && obj.transform.root != transform.root)
            {
                jumpscareTargetObject = obj;
                break;
            }
        }

        if (jumpscareTargetObject == null)
        {
            GameObject jcParent = GameObject.Find("Jumpscare");
            if (jcParent != null)
            {
                Transform t = jcParent.transform.Find("LongHairWomenJC");
                if (t != null) jumpscareTargetObject = t.gameObject;
            }
        }
    }

    private void AutoFindAudioAndAnimationClips()
    {
        if (laughSound == null || jumpscareSound == null)
        {
            AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();
            foreach (var c in clips)
            {
                if (c == null) continue;
                string n = c.name.ToLower();
                if (laughSound == null && (n.Contains("woman-laugh") || n.Contains("hyokkori_lhw") || n.Contains("laugh")))
                {
                    laughSound = c;
                }
                if (jumpscareSound == null && (n.Contains("female-scream") || n.Contains("scream-2") || n.Contains("scream")))
                {
                    jumpscareSound = c;
                }
            }
        }

        if (smileLaughAnimation == null || appearDashAnimation == null)
        {
            AnimationClip[] anims = Resources.FindObjectsOfTypeAll<AnimationClip>();
            foreach (var a in anims)
            {
                if (a == null) continue;
                string n = a.name.ToLower();
                if (smileLaughAnimation == null && (n.Contains("smile") || n.Contains("hyokkori")))
                {
                    smileLaughAnimation = a;
                }
                if (appearDashAnimation == null && (n.Contains("evnt_appear") || n.Contains("appear")))
                {
                    appearDashAnimation = a;
                }
            }
        }
    }

    void FindPlayer()
    {
        if (player != null) return;
        MovePl movePl = Object.FindFirstObjectByType<MovePl>();
        if (movePl != null)
        {
            player = movePl.transform;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        // 1. PHÍM TẮT L: KÍCH HOẠT DEMO NGAY LẬP TỨC QUANH PLAYER
        if (Input.GetKeyDown(debugSpawnKey))
        {
            Debug.Log($"<color=cyan><b>[LongHairWomenBehavior] 🚀 ĐÃ BẤM PHÍM [{debugSpawnKey}]: Kích hoạt chuỗi xuất hiện ngay lập tức!</b></color>");
            TrySpawnAroundPlayer();
        }

        if (player == null)
        {
            FindPlayer();
            return;
        }

        // Cập nhật trạng thái sở hữu 2 cánh tay
        hasAcquiredBothArms = CheckPlayerHasBothArms();

        // 2. Nếu đang xuất hiện và chưa bay dash / chưa bị tàng hình -> Xoay mặt nhìn Player
        if (isCurrentlyActive && !isVanishing && !isDashing)
        {
            RotateTowardsPlayer();
        }
    }

    // =========================================================================
    // PHÁT ANIMATION CLIP TRỰC TIẾP
    // =========================================================================
    private void PlayDirectClip(AnimationClip clip)
    {
        if (clip == null || animator == null) return;

        if (playableGraph.IsValid()) playableGraph.Destroy();

        animator.enabled = true;
        AnimationPlayableUtilities.PlayClip(animator, clip, out playableGraph);
    }

    // =========================================================================
    // KIỂM TRA ĐIỀU KIỆN SỞ HỮU CẢ 2 CÁNH TAY (ArmsL & ArmsR)
    // =========================================================================
    public static bool HasItemInInventory(string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return false;
        string kw = keyword.ToLower();

        InventoryManager inv = InventoryManager.Instance ?? Object.FindFirstObjectByType<InventoryManager>();
        if (inv != null && inv.heldItems != null)
        {
            foreach (var item in inv.heldItems)
            {
                if (!string.IsNullOrEmpty(item) && item.ToLower().Contains(kw))
                {
                    return true;
                }
            }
        }

        string savedStr = PlayerPrefs.GetString("Global_Inventory_Items", "");
        if (!string.IsNullOrEmpty(savedStr) && savedStr.ToLower().Contains(kw))
        {
            return true;
        }

        return false;
    }

    public bool CheckPlayerHasBothArms()
    {
        bool hasArmsL = HasItemInInventory("armsl") || HasItemInInventory("arm_l") || HasItemInInventory("arml") || HasItemInInventory("tay trái") || HasItemInInventory("taytrai");
        bool hasArmsR = HasItemInInventory("armsr") || HasItemInInventory("arm_r") || HasItemInInventory("armr") || HasItemInInventory("tay phải") || HasItemInInventory("tayphai");
        return hasArmsL && hasArmsR;
    }

    // =========================================================================
    // HỆ THỐNG ĐẾM GIỜ RANDOM SPAWN (CỨ 3 PHÚT KIỂM TRA TỈ LỆ 33.3%)
    // =========================================================================
    private IEnumerator SpawnTimerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);

            if (requireBothArmsToSpawn)
            {
                hasAcquiredBothArms = CheckPlayerHasBothArms();
                if (!hasAcquiredBothArms)
                {
                    timeUntilNextCheck = checkInterval;
                    continue;
                }
            }

            timeUntilNextCheck -= 1.0f;

            if (timeUntilNextCheck <= 0f)
            {
                timeUntilNextCheck = checkInterval;

                if (isCurrentlyActive || isWarningPhase || isDashing) continue;

                float roll = Random.Range(0f, 100f);
                Debug.Log($"[LongHairWomenBehavior] 🎲 Kiểm tra xuất hiện (mỗi 3 phút khi đã có 2 cánh tay): Roll = {roll:F1}%, Tỉ lệ yêu cầu: <= {spawnChancePercent:F1}%");

                if (roll <= spawnChancePercent)
                {
                    TrySpawnAroundPlayer();
                }
            }
        }
    }

    public void TrySpawnAroundPlayer()
    {
        if (player == null) FindPlayer();
        if (player == null) return;

        // Tính vị trí ngẫu nhiên SÁT MÉP VÒNG TRÒN quanh Player (3.0m - 4.0m)
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);

        Vector3 offset = new Vector3(Mathf.Cos(randomAngle) * randomDist, 0f, Mathf.Sin(randomAngle) * randomDist);
        Vector3 targetSpawnPos = player.position + offset;

        targetSpawnPos.y = CalculateGroundY(targetSpawnPos, player.position.y);

        transform.position = targetSpawnPos;
        RotateTowardsPlayer();

        SpawnAtCurrentPosition();
    }

    public void SpawnAtCurrentPosition()
    {
        if (activeLifeCoroutine != null) StopCoroutine(activeLifeCoroutine);
        activeLifeCoroutine = StartCoroutine(TwoStageManifestRoutine());
    }

    // =========================================================================
    // CHUỖI XUẤT HIỆN & HOẠT ẢNH HOÀN CHỈNH:
    // 1. CƯỜI ĐỢT 1 (TÀNG HÌNH) -> 2. CHỜ 3 GIÂY -> 3. HIỆN HÌNH & DIỄN HẾT ANIMATION CƯỜI
    // 4. HẾT ANIMATION CƯỜI MỚI PHÁT TIẾNG CƯỜI ĐỢT 2 -> 5. HẾT TIẾNG CƯỜI ĐỢT 2 KÍCH HOẠT ANIMATION APPEAR & KHỰNG TẠI CHỖ (appearHoldDuration)
    // 6. SAU ĐÓ BAY LIÊN TỤC (flightDurationBeforeJumpscare) -> 7. BÙNG NỔ JUMPSCARE (LongHairWomenJC) KÈM ÂM THANH
    // 8. HẾT JUMPSCARE BIẾN MẤT CẢ 2 & RESET BỘ ĐẾM 3P
    // =========================================================================
    private IEnumerator TwoStageManifestRoutine()
    {
        isVanishing = false;
        isCurrentlyActive = false;
        isWarningPhase = true;
        isDashing = false;

        if (jumpscareTargetObject == null) FindJumpscareObject();
        if (jumpscareTargetObject != null) jumpscareTargetObject.SetActive(false);

        // BƯỚC 1: ĐANG TÀNG HÌNH (Ẩn mô hình 3D, tắt Collider & tắt Animator)
        SetVisualsAndColliders(false);
        if (playableGraph.IsValid()) playableGraph.Destroy();
        if (animator != null) animator.enabled = false;

        // BƯỚC 2: PHÁT TIẾNG CƯỜI ĐỢT 1 (CẢNH BÁO TỪ XA KHI ĐANG TÀNG HÌNH)
        AudioClip clip1 = (warningLaughSound != null) ? warningLaughSound : laughSound;
        if (audioSource != null && clip1 != null)
        {
            ConfigureAudioSource();
            audioSource.clip = clip1;
            audioSource.volume = soundVolume;
            audioSource.loop = false;
            audioSource.Play();

            Debug.Log($"<color=orange><b>[LongHairWomenBehavior] 📢 [ĐỢT 1]: Ma Nữ phát tiếng cười cảnh báo (đang tàng hình)!</b></color>");

            // Chờ cho hết tiếng cười đợt 1
            while (audioSource != null && audioSource.isPlaying)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        // BƯỚC 3: CHỜ ĐÚNG 3 GIÂY TRONG IM LẶNG
        if (delayAfterWarningLaugh > 0f)
        {
            yield return new WaitForSeconds(delayAfterWarningLaugh);
        }

        // BƯỚC 4: SPAWN CHÍNH THỨC HIỆN HÌNH MÔ HÌNH 3D & BẬT COLLIDER
        isWarningPhase = false;
        isCurrentlyActive = true;
        isDashing = false;
        SetVisualsAndColliders(true);

        // Căn chỉnh mặt hướng về Player khi vừa hiện hình
        RotateTowardsPlayer();

        // BƯỚC 5: KÍCH HOẠT VÀ CHẠY HẾT ANIMATION CƯỜI TRƯỚC
        float smileDuration = 2.0f;
        if (smileLaughAnimation != null)
        {
            PlayDirectClip(smileLaughAnimation);
            smileDuration = smileLaughAnimation.length;
            Debug.Log($"<color=yellow><b>[LongHairWomenBehavior] 🎭 Đang diễn Animation Cười [{smileLaughAnimation.name}] trong {smileDuration:F2}s...</b></color>");
        }
        else if (animator != null)
        {
            animator.enabled = true;
        }

        // Chờ hết Animation Cười
        float smileTimer = 0f;
        while (smileTimer < smileDuration)
        {
            if (isVanishing || !isCurrentlyActive) yield break;
            smileTimer += Time.deltaTime;
            yield return null;
        }

        if (isVanishing || !isCurrentlyActive) yield break;

        // BƯỚC 6: SAU KHI HẾT ANIMATION CƯỜI -> MỚI BẮT ĐẦU PHÁT TIẾNG CƯỜI ĐỢT 2!
        AudioClip clip2 = (laughSound != null) ? laughSound : warningLaughSound;
        if (audioSource != null && clip2 != null)
        {
            ConfigureAudioSource();
            audioSource.clip = clip2;
            audioSource.volume = soundVolume;
            audioSource.loop = false;
            audioSource.Play();

            Debug.Log($"<color=red><b>[LongHairWomenBehavior] 👻 [ĐỢT 2]: Hết Animation Cười -> Bắt đầu phát TIẾNG CƯỜI ĐỢT 2! (Trong lúc này chớp Flash vẫn làm biến mất)</b></color>");

            // CHỜ CHO HẾT TIẾNG CƯỜI ĐỢT 2
            while (audioSource != null && audioSource.isPlaying)
            {
                if (isVanishing || !isCurrentlyActive) yield break;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        if (isVanishing || !isCurrentlyActive) yield break;

        // BƯỚC 7: HẾT TIẾNG CƯỜI ĐỢT 2 -> KÍCH HOẠT ANIMATION APPEAR & KHỰNG TẠI CHỖ appearHoldDuration!
        isDashing = true; // KHÓA BIẾN MẤT DO FLASH

        if (appearDashAnimation != null)
        {
            PlayDirectClip(appearDashAnimation);
            Debug.Log($"<color=green><b>[LongHairWomenBehavior] 🎬 Kích hoạt Animation Appear [{appearDashAnimation.name}]! Khựng {appearHoldDuration:F1}s tại chỗ để dùng animation... (Chụp Flash KHÔNG biến mất)</b></color>");
        }
        else if (animator != null)
        {
            animator.enabled = true;
        }

        // KHỰNG TẠI CHỖ appearHoldDuration ĐỂ DÙNG ANIMATION APPEAR TRƯỚC KHI BAY
        if (appearHoldDuration > 0f)
        {
            float holdTimer = 0f;
            while (holdTimer < appearHoldDuration)
            {
                holdTimer += Time.deltaTime;
                yield return null;
            }
        }

        // TÍNH HƯỚNG BAY SANG BÊN PHẢI CỦA CON MA (THEO GÓC NHÌN NGƯỜI CHƠI)
        Vector3 rightDir = transform.right;
        if (player != null)
        {
            Vector3 toGhost = (transform.position - player.position);
            toGhost.y = 0f;
            if (toGhost.sqrMagnitude > 0.001f)
            {
                rightDir = Vector3.Cross(Vector3.up, toGhost.normalized).normalized;
            }
        }

        Debug.Log($"<color=cyan><b>[LongHairWomenBehavior] 🚀 Bắt đầu bay liên tục trong {flightDurationBeforeJumpscare:F1}s trước khi kích hoạt Jumpscare!</b></color>");

        // BƯỚC 8: BAY LIÊN TỤC TRONG ĐÚNG flightDurationBeforeJumpscare GIÂY
        float flyTimer = 0f;
        while (flyTimer < flightDurationBeforeJumpscare)
        {
            flyTimer += Time.deltaTime;

            // Vừa bay lướt sang phải vừa nâng cao độ Y bay nhẹ lên trên
            Vector3 moveDelta = (rightDir * dashRightSpeed + Vector3.up * flyUpSpeed) * Time.deltaTime;
            transform.position += moveDelta;

            yield return null;
        }

        // BƯỚC 9: SAU KHI BAY XONG -> ẨN CON MA NGOÀI BẢN ĐỒ VÀ BÙNG NỔ JUMPSCARE (LongHairWomenJC) TRONG MAIN!
        SetVisualsAndColliders(false);
        if (playableGraph.IsValid()) playableGraph.Destroy();
        if (animator != null) animator.enabled = false;

        Debug.Log("<color=red><b>[LongHairWomenBehavior] 😱 HẾT THỜI GIAN BAY -> BÙNG NỔ JUMPSCARE TRƯỚC MẶT PLAYER (LongHairWomenJC)!</b></color>");

        if (jumpscareTargetObject == null) FindJumpscareObject();

        PlayableGraph jcGraph = default;
        float actualJcDuration = jumpscareDuration;

        if (jumpscareTargetObject != null)
        {
            jumpscareTargetObject.SetActive(true);

            Animator jcAnim = jumpscareTargetObject.GetComponentInChildren<Animator>();
            if (jcAnim != null)
            {
                jcAnim.enabled = true;
                if (jumpscareAnimation != null)
                {
                    AnimationPlayableUtilities.PlayClip(jcAnim, jumpscareAnimation, out jcGraph);
                    actualJcDuration = jumpscareAnimation.length;
                }
                else
                {
                    jcAnim.Play(0, 0, 0f);
                }
            }
        }

        // PHÁT ÂM THANH JUMPSCARE (2D to rõ hét thẳng vào tai)
        if (jumpscareSound != null)
        {
            if (audioSource != null)
            {
                audioSource.spatialBlend = 0f; // 2D Sound thẳng vào tai
                audioSource.clip = jumpscareSound;
                audioSource.volume = jumpscareVolume;
                audioSource.loop = false;
                audioSource.Play();
                actualJcDuration = Mathf.Max(actualJcDuration, jumpscareSound.length);
            }
        }

        // CHỜ HẾT ANIMATION VÀ ÂM THANH JUMPSCARE
        yield return new WaitForSeconds(actualJcDuration);

        // BƯỚC 10: HẾT JUMPSCARE -> BIẾN MẤT CẢ 2 VÀ BẮT ĐẦU QUAY LẠI BỘ ĐẾM 3 PHÚT!
        if (jcGraph.IsValid()) jcGraph.Destroy();
        if (jumpscareTargetObject != null) jumpscareTargetObject.SetActive(false);

        Debug.Log("<color=cyan><b>[LongHairWomenBehavior] 🔄 Hoàn tất Jumpscare! Biến mất cả 2 và bắt đầu quay lại bộ đếm 3 phút!</b></color>");
        HideGhost();
        timeUntilNextCheck = checkInterval; // Reset lại bộ đếm 3 phút
    }

    // =========================================================================
    // CƠ CHẾ DÍNH ĐÈN CHÓI (FLASH BURST / CHUỘT PHẢI)
    // CHỈ CÓ HIỆU LỰC KHI ĐANG DIỄN ANIMATION CƯỜI HOẶC PHÁT TIẾNG CƯỜI ĐỢT 2
    // KHI ĐÃ SANG GIAI ĐOẠN ANIMATION APPEAR, KHỰNG VÀ BAY THÌ KHÔNG BIẾN MẤT NỮA!
    // =========================================================================
    public void OnCameraFlashStunned()
    {
        // Khi chưa kích hoạt, hoặc đang tàng hình, HOẶC ĐANG TRONG GIAI ĐOẠN KHỰNG/BAY -> KHÔNG BIẾN MẤT
        if (!isCurrentlyActive || isVanishing || isDashing) return;

        StartCoroutine(VanishOnFlashRoutine());
    }

    private IEnumerator VanishOnFlashRoutine()
    {
        isVanishing = true;

        Debug.Log("<color=yellow><b>[LongHairWomenBehavior] ⚡ Ma Nữ Tóc Dài DÍNH ĐÈN CHÓI TRONG LÚC CƯỜI! Tàng hình ngay lập tức, tiếng cười phát nốt rồi ẩn!</b></color>");

        // 1. BIẾN MẤT / TÀNG HÌNH NGAY LẬP TỨC (Ẩn toàn bộ 3D & tắt Collider)
        SetVisualsAndColliders(false);

        if (playableGraph.IsValid()) playableGraph.Destroy();
        if (animator != null) animator.enabled = false;
        if (jumpscareTargetObject != null) jumpscareTargetObject.SetActive(false);

        // 2. CHỜ TIẾNG CƯỜI ĐANG PHÁT DỞ CHẠY CHO ĐẾN KHI HẾT HẲN
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.loop = false; // Tắt lặp để tiếng cười kết thúc tự nhiên
            while (audioSource != null && audioSource.isPlaying)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        // 3. Ẩn hoàn toàn và quay lại bộ đếm 3 phút tiếp theo
        HideGhost();
        timeUntilNextCheck = checkInterval;
    }

    private void HideGhost()
    {
        isCurrentlyActive = false;
        isWarningPhase = false;
        isVanishing = false;
        isDashing = false;
        SetVisualsAndColliders(false);
        if (audioSource != null) audioSource.Stop();
        if (playableGraph.IsValid()) playableGraph.Destroy();
        if (animator != null) animator.enabled = false;
        if (jumpscareTargetObject != null) jumpscareTargetObject.SetActive(false);
    }

    private void SetVisualsAndColliders(bool state)
    {
        if (allRenderers == null || allRenderers.Length == 0)
        {
            allRenderers = GetComponentsInChildren<Renderer>(true);
        }
        foreach (var r in allRenderers)
        {
            if (r != null) r.enabled = state;
        }

        if (allColliders == null || allColliders.Length == 0)
        {
            allColliders = GetComponentsInChildren<Collider>(true);
        }
        foreach (var c in allColliders)
        {
            if (c != null) c.enabled = state;
        }
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null) return;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = audioMinDistance;
        audioSource.maxDistance = audioMaxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0f;
    }

    private void RotateTowardsPlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot * Quaternion.Euler(modelRotationOffset), Time.deltaTime * lookAtPlayerSpeed);
        }
    }

    private float CalculateGroundY(Vector3 checkPos, float fallbackY)
    {
        if (!snapToGround) return fallbackY + groundOffsetY;

        float castHeight = (raycastHeightAbove > 0.1f) ? raycastHeightAbove : 30f;
        float castDist = (raycastDistance > 0.1f) ? raycastDistance : 80f;
        Vector3 origin = new Vector3(checkPos.x, checkPos.y + castHeight, checkPos.z);

        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, castDist, groundLayerMask, QueryTriggerInteraction.Ignore);
        float bestY = -99999f;
        bool foundGround = false;

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.isTrigger) continue;
            if (hit.collider.CompareTag("Player") || hit.collider.transform.root == transform.root) continue;

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            return bestY + groundOffsetY;
        }

        // Bắn tia từ độ cao 100m xuống toàn bản đồ nếu địa hình đồi núi dốc
        if (Physics.Raycast(new Vector3(checkPos.x, checkPos.y + 100f, checkPos.z), Vector3.down, out RaycastHit skyHit, 200f, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (!skyHit.collider.isTrigger && !skyHit.collider.CompareTag("Player") && skyHit.collider.transform.root != transform.root)
            {
                return skyHit.point.y + groundOffsetY;
            }
        }

        return fallbackY + groundOffsetY;
    }

    // Vẽ bán kính xuất hiện trong Scene View để dễ căn chỉnh
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, minSpawnDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, maxSpawnDistance);
        }
    }
}
