using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý AI & Hành vi của Người Đàn Bà Trắng (Wman - DF2) trong Map 04:
/// 1. CONTEXT STEERING AI 16 HƯỚNG (360°):
///    - Không cần NavMesh, quét 16 hướng xung quanh để né tường, vách núi, đá to, tượng Phật, bàn thờ.
/// 2. ANTI-STUCK CƯỠNG CHẾ:
///    - Kẹt > 0.3s -> Quét 360° tìm hướng thoáng nhất để thoát kẹt rồi mới quay lại đuổi tiếp.
/// 3. CHỐNG BAY LÊN TRỜI & CHỐNG LEO BÀN THỜ/TƯỢNG PHẬT (Ground Alignment & Max Step-Up):
///    - Bám sát mặt đất với trọng lực mượt mà.
///    - Nếu gặp vật cản cao quá 0.6m (bàn thờ, tượng) -> Chặn không cho nhảy vọt lên, tự động lách sang hông đi vòng qua.
/// 4. HOẠT ẢNH MƯỢT MÀ (PlayableGraph):
///    - Đứng im (Idle ở Home) & Bị Choáng (Stunned) -> Loop animation 'idleClip' (ENM_WMAN_idle).
///    - Di chuyển (Chasing & Returning) -> Loop animation 'runClip' (ENM_WMAN_run).
/// 5. CƠ CHẾ BỊ CHOÁNG BỞI ĐÈN PIN (Flashlight Stun):
///    - Khi bị người chơi chớp flash Chuột Phải -> Bị đẩy lùi nhẹ và khựng bất động trong 3.5s.
/// 6. BẮT NGƯỜI CHƠI & IN-CAMERA JUMPSCARE:
///    - Tương thích GodMode (phím M) & Game Over chuyển cảnh ảnh tử nạn 'End' (nhấp chuột để về Menu).
/// </summary>
public class WmanBehavior : MonoBehaviour
{
    public enum WmanState
    {
        IdleAtHome,
        Chasing,
        Stunned,
        StareBeforeReturn,
        ReturningHome,
        JumpscareAttack
    }

    [Header("1. Hoạt Ảnh (Animation Clips)")]
    [Tooltip("Kéo Animation Idle (ENM_WMAN_idle) vào đây - Dùng khi đứng im tại chỗ & khi bị choáng")]
    public AnimationClip idleClip;

    [Tooltip("Kéo Animation Run (ENM_WMAN_run) vào đây - Dùng khi di chuyển rượt đuổi hoặc quay về")]
    public AnimationClip runClip;

    [Tooltip("Tốc độ phát animation")]
    public float animSpeed = 1.0f;

    [Header("2. Tốc Độ & Bắt Người Chơi")]
    [Tooltip("Tốc độ chạy săn đuổi người chơi (Mặc định: 18 - 22)")]
    public float moveSpeed = 20.0f;

    [Tooltip("Tốc độ chạy khi quay về vị trí cũ (Mặc định: 12)")]
    public float returnSpeed = 12.0f;

    [Tooltip("Khoảng cách bắt / Game Over khi áp sát người chơi (mét - Mặc định: 2.5m)")]
    public float killDistance = 2.5f;

    [Tooltip("Tốc độ chuyển hướng mượt mà (Mặc định: 12)")]
    public float turnSpeed = 12.0f;

    [Tooltip("Góc xoay bù trừ mô hình nếu bị lệch (X, Y, Z)")]
    public Vector3 modelRotationOffset = Vector3.zero;

    [Header("3. Context Steering - Tránh Vật Cản 360° (Không Dùng NavMesh)")]
    [Tooltip("Layer các vật cản cần né (Vách núi, tường, đá to, tượng Phật, bàn thờ)")]
    public LayerMask obstacleLayerMask = ~0;

    [Tooltip("Khoảng cách quét phát hiện vật cản (Mặc định: 3.5m)")]
    public float sensorDistance = 3.5f;

    [Tooltip("Bán kính tia cảm biến SphereCast (Mặc định: 0.35m)")]
    public float sensorRadius = 0.35f;

    [Tooltip("Độ cao gốc bắn tia tính từ chân lên (Mặc định: 0.8m)")]
    public float sensorHeight = 0.8f;

    [Header("4. Bám Địa Hình & Chống Bay Lên Trời (Ground Alignment)")]
    [Tooltip("Độ cao bù trừ khi chân chạm đất")]
    public float groundOffset = 0.0f;

    [Tooltip("Độ cao tối đa cho phép bước lên mỗi bước (mét - Mặc định: 0.6m). Cao hơn sẽ tính là vật cản và lách sang bên")]
    public float maxStepUpHeight = 0.6f;

    [Tooltip("Tốc độ rơi / bám chặt xuống đất (Mặc định: 35 - 50)")]
    public float fallSpeed = 40.0f;

    public LayerMask groundLayerMask = ~0;

    [Header("5. Cấu Hình Vùng Cấm (Zone Settings)")]
    [Tooltip("Chỉ đuổi khi Player ở trong Zone (Được ForbiddenDangerZone quản lý)")]
    public bool isPlayerInZone = false;

    [Tooltip("Vị trí gốc ban đầu để Wman quay về (Để trống tự lấy vị trí lúc Start)")]
    public Transform defaultHomePoint;

    [Tooltip("Thời gian đứng nhìn theo Player khi Player vừa rời Zone trước khi quay về (giây)")]
    public float stareDurationBeforeReturn = 1.5f;

    [Header("6. Cơ Chế Bị Choáng Do Đèn Pin (Flashlight Stun)")]
    [Tooltip("Bật nếu muốn Wman bị choáng khi bị chớp đèn pin Chuột Phải")]
    public bool enableFlashlightStun = true;

    [Tooltip("Thời gian bị choáng bất động (giây - Mặc định: 3.5s)")]
    public float stunDuration = 3.5f;

    [Tooltip("Khoảng cách bị đẩy lùi nhẹ khi bị chớp flash (mét - Mặc định: 2.0m)")]
    public float stunPushbackDistance = 2.0f;

    [Tooltip("Âm thanh khi bị chớp flash làm choáng")]
    public AudioClip stunSound;
    [Range(0f, 1f)] public float stunVolume = 1.0f;

    [Header("7. Âm Thanh Săn Đuổi & Jumpscare (Audio SFX)")]
    public AudioSource audioSource;
    public AudioClip creepSound;
    public AudioClip chaseSound;
    public AudioClip jumpscareSound;
    [Range(0f, 1f)] public float soundVolume = 1.0f;

    [Header("8. Jumpscare & Game Over (Bắt Được Người Chơi)")]
    [Tooltip("GameObject WmanHead nằm trong Main Camera > Jumpscare > WmanHead (Để trống tự động tìm)")]
    public GameObject wmanInCameraObject;

    [Tooltip("Thời gian hù dọa jumpscare trước khi bắt đầu Fade đen (giây - Mặc định: 1.5s)")]
    public float inCameraJumpscareDuration = 1.5f;

    [Tooltip("Cường độ rung lắc Camera khi bị jumpscare (Mặc định: 0.25)")]
    public float cameraShakeIntensity = 0.25f;

    [Tooltip("Kéo GameObject 'EndG' (trong Canvas UI > EndG) vào đây (Để trống tự động tìm)")]
    public GameObject endScreenObject;

    [Tooltip("Kéo Sprite ảnh tử nạn 'End' (trong Assets/UI/End) vào đây (Để trống tự động tìm)")]
    public Sprite endScreenSprite;

    [Tooltip("Tên Scene Menu chính (Mặc định: MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    [Header("9. Trạng Thái Hiện Tại (Debug)")]
    public WmanState currentState = WmanState.IdleAtHome;

    [Tooltip("Được bật khi người chơi đã dâng đủ 2 đền thờ Gore -> Wman bị hóa giải và không tấn công nữa")]
    public bool isPacified = false;

    // --- Private Fields ---
    private Transform player;
    private MovePl playerMove;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private bool hasCaughtPlayer = false;
    private float stareTimer = 0f;
    private Coroutine stunCoroutine = null;

    // Context Steering
    private const int NUM_DIRECTIONS = 16;
    private Vector3[] directionVectors;
    private Vector3 currentMoveDirection;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private float stuckEscapeTimer = 0f;
    private Vector3 escapeDirection;

    // PlayableGraph Animation System
    private PlayableGraph playableGraph;
    private AnimationPlayableOutput playableOutput;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable runPlayable;
    private AnimationMixerPlayable mixerPlayable;
    private Animator anim;
    private bool isCurrentRunning = false;
    private float runAnimTimer = 0f;
    private float idleAnimTimer = 0f;

    void Awake()
    {
        if (defaultHomePoint != null)
        {
            homePosition = defaultHomePoint.position;
            homeRotation = defaultHomePoint.rotation;
        }
        else
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        lastPosition = transform.position;
        currentMoveDirection = transform.forward;
        escapeDirection = Vector3.zero;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f;
        audioSource.playOnAwake = false;

        anim = GetComponentInChildren<Animator>();

        // Khởi tạo 16 hướng quét 360 độ
        directionVectors = new Vector3[NUM_DIRECTIONS];
        for (int i = 0; i < NUM_DIRECTIONS; i++)
        {
            float angle = i * (360f / NUM_DIRECTIONS);
            directionVectors[i] = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        }

        InitPlayableGraph();
    }

    void Start()
    {
        FindPlayer();
        FindWmanInCamera();
        SetAnimationState(false);
    }

    private void InitPlayableGraph()
    {
        if (anim == null) return;
        if (idleClip == null && runClip == null) return;

        if (idleClip != null) idleClip.wrapMode = WrapMode.Loop;
        if (runClip != null) runClip.wrapMode = WrapMode.Loop;

        playableGraph = PlayableGraph.Create("WmanPlayableGraph");
        playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", anim);

        mixerPlayable = AnimationMixerPlayable.Create(playableGraph, 2);
        playableOutput.SetSourcePlayable(mixerPlayable);

        if (idleClip != null)
        {
            idlePlayable = AnimationClipPlayable.Create(playableGraph, idleClip);
            idlePlayable.SetSpeed(animSpeed);
            playableGraph.Connect(idlePlayable, 0, mixerPlayable, 0);
        }

        if (runClip != null)
        {
            runPlayable = AnimationClipPlayable.Create(playableGraph, runClip);
            runPlayable.SetSpeed(animSpeed);
            playableGraph.Connect(runPlayable, 0, mixerPlayable, 1);
        }

        mixerPlayable.SetInputWeight(0, 1f);
        mixerPlayable.SetInputWeight(1, 0f);

        playableGraph.Play();
    }

    private void SetAnimationState(bool isRunning)
    {
        isCurrentRunning = isRunning;
        if (playableGraph.IsValid())
        {
            if (isRunning)
            {
                mixerPlayable.SetInputWeight(0, 0f);
                mixerPlayable.SetInputWeight(1, 1f);
            }
            else
            {
                mixerPlayable.SetInputWeight(0, 1f);
                mixerPlayable.SetInputWeight(1, 0f);
            }
        }
        else if (anim != null)
        {
            anim.speed = animSpeed;
            anim.SetBool("IsRunning", isRunning);
        }
    }

    private void UpdateAnimationLoops()
    {
        if (!playableGraph.IsValid()) return;

        float dt = Time.deltaTime * animSpeed;

        if (isCurrentRunning)
        {
            if (runClip != null && runClip.length > 0.001f && runPlayable.IsValid())
            {
                runAnimTimer += dt;
                double loopTime = runAnimTimer % (double)runClip.length;
                runPlayable.SetTime(loopTime);
            }
        }
        else
        {
            if (idleClip != null && idleClip.length > 0.001f && idlePlayable.IsValid())
            {
                idleAnimTimer += dt;
                double loopTime = idleAnimTimer % (double)idleClip.length;
                idlePlayable.SetTime(loopTime);
            }
        }
    }

    void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }

    void FindPlayer()
    {
        if (player != null) return;
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
            playerMove = pObj.GetComponent<MovePl>() ?? pObj.GetComponentInParent<MovePl>();
        }
    }

    void FindWmanInCamera()
    {
        if (wmanInCameraObject != null) return;

        Camera c = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (c != null)
        {
            Transform jumpscareParent = c.transform.Find("Jumpscare");
            if (jumpscareParent != null)
            {
                Transform head = jumpscareParent.Find("WmanHead") ?? jumpscareParent.Find("Wman") ?? jumpscareParent.Find("SSC_CH_ENM_WMAN_head_only");
                if (head != null)
                {
                    wmanInCameraObject = head.gameObject;
                    wmanInCameraObject.SetActive(false);
                    return;
                }
            }
        }

        GameObject[] allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjs)
        {
            if (obj != null && (obj.name == "WmanHead" || obj.name == "SSC_CH_ENM_WMAN_head_only"))
            {
                wmanInCameraObject = obj;
                wmanInCameraObject.SetActive(false);
                break;
            }
        }
    }

    void Update()
    {
        if (hasCaughtPlayer) return;

        // Cập nhật bộ đếm chống kẹt
        float distMoved = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(lastPosition.x, 0f, lastPosition.z));
        if (distMoved < 0.15f * Time.deltaTime && (currentState == WmanState.Chasing || currentState == WmanState.ReturningHome))
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = transform.position;

        switch (currentState)
        {
            case WmanState.IdleAtHome:
                HandleIdleState();
                break;

            case WmanState.Chasing:
                HandleChasingState();
                break;

            case WmanState.Stunned:
                break;

            case WmanState.StareBeforeReturn:
                HandleStareState();
                break;

            case WmanState.ReturningHome:
                HandleReturningState();
                break;
        }

        UpdateAnimationLoops();
    }

    // ==================== ZONE API & PACIFICATION ====================

    /// <summary>
    /// Hóa giải Wman hoàn toàn khi người chơi dâng đủ 2 đền thờ Gore:
    /// Dừng săn đuổi ngay lập tức và chạy mượt mà quay về điểm xuất phát ban đầu (homePosition)
    /// </summary>
    public void PacifyWman()
    {
        isPacified = true;
        isPlayerInZone = false;

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            stunCoroutine = null;
        }

        if (currentState != WmanState.JumpscareAttack)
        {
            float distToHome = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(homePosition.x, 0f, homePosition.z));
            if (distToHome > 0.8f)
            {
                currentState = WmanState.ReturningHome;
                SetAnimationState(true);
            }
            else
            {
                transform.position = homePosition;
                transform.rotation = homeRotation;
                currentState = WmanState.IdleAtHome;
                SetAnimationState(false);
            }
        }

        Debug.Log("<color=green><b>[WmanBehavior] 🕊️ Wman đã được hóa giải bởi 2 đền thờ! Không còn săn đuổi người chơi nữa và đang chạy về điểm xuất phát.</b></color>");
    }

    public void OnPlayerEnteredZone(Transform targetPlayer)
    {
        if (hasCaughtPlayer || isPacified) return;
        player = targetPlayer;
        isPlayerInZone = true;

        if (currentState != WmanState.Stunned)
        {
            currentState = WmanState.Chasing;
            SetAnimationState(true);
            if (chaseSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(chaseSound, soundVolume);
            }
        }
    }

    public void OnPlayerExitedZone()
    {
        if (hasCaughtPlayer || isPacified) return;
        isPlayerInZone = false;

        if (currentState == WmanState.Chasing)
        {
            currentState = WmanState.StareBeforeReturn;
            stareTimer = 0f;
            SetAnimationState(false);
        }
    }

    private float recoveryTimer = 0f;

    public bool CheckIfPlayerInZone()
    {
        if (isPacified) return false;
        if (isPlayerInZone) return true;

        if (player == null) FindPlayer();
        if (player == null) return false;

        // 1. Kiểm tra qua các ForbiddenDangerZone trong Scene
        ForbiddenDangerZone[] allZones = Object.FindObjectsByType<ForbiddenDangerZone>(FindObjectsSortMode.None);
        foreach (var z in allZones)
        {
            if (z != null && (z.assignedWman == this || z.assignedWman == null))
            {
                Collider col = z.GetComponent<Collider>();
                if (col != null && col.bounds.Contains(player.position))
                {
                    isPlayerInZone = true;
                    return true;
                }
            }
        }

        // 2. Dự phòng kiểm tra khoảng cách
        if (Vector3.Distance(transform.position, player.position) <= 40f)
        {
            return true;
        }

        return false;
    }

    // ==================== STATE HANDLERS ====================

    private void HandleIdleState()
    {
        SetAnimationState(false);

        if (isPacified) return;

        // Sau khi GodMode reset, có thời gian nghỉ ngắn recoveryTimer (1.5s) để người chơi kịp chạy
        if (recoveryTimer > 0f)
        {
            recoveryTimer -= Time.deltaTime;
            return;
        }

        if (player == null) FindPlayer();

        // Kiểm tra nếu Player đang trong zone -> Lao tới đuổi tiếp!
        if (CheckIfPlayerInZone() && player != null)
        {
            currentState = WmanState.Chasing;
            SetAnimationState(true);
            if (chaseSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(chaseSound, soundVolume);
            }
        }
    }

    private void HandleChasingState()
    {
        if (isPacified)
        {
            currentState = WmanState.ReturningHome;
            SetAnimationState(true);
            return;
        }

        if (player == null) FindPlayer();
        if (player == null) return;

        SetAnimationState(true);

        // 1. Tính hướng mong muốn về phía Player
        Vector3 desiredDir = (player.position - transform.position);
        desiredDir.y = 0f;
        if (desiredDir.sqrMagnitude > 0.001f) desiredDir.Normalize();

        // 2. CONTEXT STEERING 16 HƯỚNG Né Vật Cản Thông Minh
        Vector3 chosenDir = ComputeContextSteering(desiredDir);

        currentMoveDirection = Vector3.Slerp(currentMoveDirection, chosenDir, turnSpeed * Time.deltaTime);

        // 3. DI CHUYỂN & BÁM ĐẤT (CHỐNG BAY LÊN TRỜI & CHỐNG LEO BÀN THỜ/TƯỢNG)
        MoveAndSnapToGround(currentMoveDirection, moveSpeed);

        // 4. XOAY MẶT THEO HƯỚNG DI CHUYỂN
        Vector3 lookDir = currentMoveDirection;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion yawRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            transform.rotation = yawRot * Quaternion.Euler(modelRotationOffset);
        }

        // 5. Kiểm tra khoảng cách bắt Player
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= killDistance)
        {
            CatchPlayer();
        }
    }

    private void HandleStareState()
    {
        if (isPacified)
        {
            currentState = WmanState.ReturningHome;
            SetAnimationState(true);
            return;
        }

        SetAnimationState(false);
        stareTimer += Time.deltaTime;

        if (player != null)
        {
            Vector3 dir = (player.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(modelRotationOffset);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }

        if (isPlayerInZone)
        {
            currentState = WmanState.Chasing;
            SetAnimationState(true);
            return;
        }

        if (stareTimer >= stareDurationBeforeReturn)
        {
            currentState = WmanState.ReturningHome;
            SetAnimationState(true);
        }
    }

    private void HandleReturningState()
    {
        SetAnimationState(true);

        if (isPlayerInZone && !isPacified)
        {
            currentState = WmanState.Chasing;
            SetAnimationState(true);
            return;
        }

        Vector3 toHome = (homePosition - transform.position);
        toHome.y = 0f;
        float distToHome = toHome.magnitude;

        if (distToHome > 0.8f)
        {
            Vector3 desiredDir = toHome.normalized;
            Vector3 chosenDir = ComputeContextSteering(desiredDir);

            currentMoveDirection = Vector3.Slerp(currentMoveDirection, chosenDir, turnSpeed * Time.deltaTime);
            MoveAndSnapToGround(currentMoveDirection, returnSpeed);

            Vector3 lookDir = currentMoveDirection;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion yawRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                transform.rotation = yawRot * Quaternion.Euler(modelRotationOffset);
            }
        }
        else
        {
            transform.position = homePosition;
            transform.rotation = homeRotation;
            currentState = WmanState.IdleAtHome;
            SetAnimationState(false);
        }
    }

    // ==================== CONTEXT STEERING (16 HƯỚNG 360°) ====================

    private Vector3 ComputeContextSteering(Vector3 desiredDir)
    {
        Vector3 origin = transform.position + Vector3.up * sensorHeight;

        // ====== PHA THOÁT KẸT CƯỠNG CHẾ (ANTI-STUCK ESCAPE) ======
        if (stuckTimer >= 0.3f && stuckEscapeTimer <= 0f)
        {
            stuckEscapeTimer = 0.6f;
            escapeDirection = FindBestEscapeDirection(origin);
        }

        if (stuckEscapeTimer > 0f)
        {
            stuckEscapeTimer -= Time.deltaTime;
            return escapeDirection;
        }

        // ====== CHẾ ĐỘ BÌNH THƯỜNG: CONTEXT STEERING ======
        float bestScore = float.MinValue;
        int bestIndex = 0;

        for (int i = 0; i < NUM_DIRECTIONS; i++)
        {
            Vector3 dir = directionVectors[i];

            // 1. Interest: Điểm hướng gần mục tiêu
            float interest = Vector3.Dot(dir, desiredDir);
            interest = Mathf.Clamp01((interest + 1f) * 0.5f);
            interest *= interest;

            // 2. Danger: Quét vật cản bằng SphereCast
            float danger = 0f;
            if (Physics.SphereCast(origin, sensorRadius, dir, out RaycastHit hit, sensorDistance, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("Player") && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
                {
                    danger = 1f - Mathf.Clamp01(hit.distance / sensorDistance);
                    danger *= danger;
                }
            }

            // 3. Step-Up Danger: Kiểm tra xem phía trước có bị vách cao / bàn thờ chặn không
            Vector3 testGroundPos = origin + dir * 1.2f;
            if (Physics.Raycast(testGroundPos + Vector3.up * 1.5f, Vector3.down, out RaycastHit groundHit, 5f, groundLayerMask))
            {
                float heightDiff = groundHit.point.y - transform.position.y;
                if (heightDiff > maxStepUpHeight)
                {
                    // Vách quá cao / leo lên nóc đền thờ -> Đánh dấu là cực kỳ nguy hiểm để lách sang bên
                    danger = Mathf.Max(danger, 0.95f);
                }
            }

            float score = interest - danger * 2.2f;
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return directionVectors[bestIndex];
    }

    private Vector3 FindBestEscapeDirection(Vector3 origin)
    {
        float bestDist = -1f;
        Vector3 bestDir = transform.forward;

        for (int i = 0; i < NUM_DIRECTIONS; i++)
        {
            Vector3 testDir = directionVectors[i];
            if (Physics.SphereCast(origin, sensorRadius, testDir, out RaycastHit hit, sensorDistance * 2f, obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance > bestDist)
                {
                    bestDist = hit.distance;
                    bestDir = testDir;
                }
            }
            else
            {
                bestDist = sensorDistance * 2f;
                bestDir = testDir;
            }
        }

        return bestDir;
    }

    // ==================== BÁM MẶT ĐẤT & TRỌNG LỰC ====================

    private void MoveAndSnapToGround(Vector3 moveDir, float speed)
    {
        Vector3 nextPos = transform.position + moveDir * speed * Time.deltaTime;

        // Bắn tia tìm mặt đất
        RaycastHit[] hits = Physics.RaycastAll(nextPos + Vector3.up * 1.5f, Vector3.down, 10f, groundLayerMask);
        float bestGroundY = -9999f;
        bool foundGround = false;

        foreach (var h in hits)
        {
            if (h.collider.CompareTag("Player") || h.collider.gameObject == gameObject || h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.isTrigger) continue;

            // Kiểm tra nếu điểm tiếp xúc cao hơn chân hiện tại quá mức cho phép -> Bỏ qua không leo lên
            float stepDiff = h.point.y - transform.position.y;
            if (stepDiff > maxStepUpHeight) continue;

            if (h.point.y > bestGroundY)
            {
                bestGroundY = h.point.y;
                foundGround = true;
            }
        }

        float currentY = transform.position.y;
        float targetY = foundGround ? bestGroundY + groundOffset : currentY;

        // Trọng lực / bám đất êm ái
        nextPos.y = Mathf.MoveTowards(currentY, targetY, fallSpeed * Time.deltaTime);
        transform.position = nextPos;
    }

    // ==================== FLASHLIGHT STUN ====================

    public void OnCameraFlashStunned()
    {
        if (!enableFlashlightStun || hasCaughtPlayer) return;

        Debug.Log("[WmanBehavior] ⚡ BỊ CHỚP ĐÈN FLASH CHÓI MẮT! Wman bị choáng bất động và chuyển sang Idle...");
        TriggerStun();
    }

    private void TriggerStun()
    {
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        currentState = WmanState.Stunned;
        idleAnimTimer = 0f; // Bắt đầu diễn animation choáng (Idle) từ đầu
        if (idlePlayable.IsValid()) idlePlayable.SetTime(0);
        SetAnimationState(false);

        if (stunSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(stunSound, stunVolume);
        }

        // Đẩy lùi nhẹ về phía sau
        if (stunPushbackDistance > 0f)
        {
            Vector3 pushDir = -currentMoveDirection;
            pushDir.y = 0f;

            float elapsedPush = 0f;
            float pushTime = 0.25f;
            Vector3 startPos = transform.position;
            Vector3 targetPushPos = startPos + pushDir.normalized * stunPushbackDistance;

            while (elapsedPush < pushTime)
            {
                elapsedPush += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPushPos, elapsedPush / pushTime);
                yield return null;
            }
        }

        // TỰ ĐỘNG TÍNH THỜI GIAN CHOÁNG CHUẨN XÁC THEO ĐỘ DÀI ANIMATION IDLE (2.4s)
        float duration = (idleClip != null && idleClip.length > 0.01f)
            ? (idleClip.length / Mathf.Max(0.1f, animSpeed))
            : stunDuration;

        yield return new WaitForSeconds(duration);

        // HẾT ANIMATION IDLE -> TỰ ĐỘNG HẾT CHOÁNG VÀ TIẾP TỤC HÀNH VI
        if (isPlayerInZone)
        {
            currentState = WmanState.Chasing;
            SetAnimationState(true);
        }
        else
        {
            currentState = WmanState.ReturningHome;
            SetAnimationState(true);
        }

        stunCoroutine = null;
    }

    // ==================== CATCH & JUMPSCARE ====================

    private void OnTriggerEnter(Collider other)
    {
        if (hasCaughtPlayer || isPacified || currentState == WmanState.Stunned) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            CatchPlayer();
        }
    }

    public void CatchPlayer()
    {
        if (hasCaughtPlayer || isPacified || currentState == WmanState.Stunned) return;
        hasCaughtPlayer = true;
        currentState = WmanState.JumpscareAttack;

        Debug.Log("<color=red><b>[WmanBehavior] 💀 Wman đã tóm trúng người chơi -> Kích hoạt In-Camera Jumpscare!</b></color>");

        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = false;

        Collider[] cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = false;

        StartCoroutine(WmanCameraJumpscareRoutine());
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

    private IEnumerator WmanCameraJumpscareRoutine()
    {
        if (wmanInCameraObject == null) FindWmanInCamera();

        if (playerMove != null)
        {
            playerMove.isCameraLocked = true;
            playerMove.enabled = false;
        }

        Transform camToShake = Camera.main != null ? Camera.main.transform : (Camera.current != null ? Camera.current.transform : null);

        if (wmanInCameraObject != null)
        {
            wmanInCameraObject.SetActive(true);

            Renderer[] headRends = wmanInCameraObject.GetComponentsInChildren<Renderer>(true);
            foreach (var r in headRends) r.enabled = true;

            Animator headAnim = wmanInCameraObject.GetComponent<Animator>() ?? wmanInCameraObject.GetComponentInChildren<Animator>();
            if (headAnim != null)
            {
                headAnim.enabled = true;
                headAnim.Play(0, 0, 0f);
            }

            if (jumpscareSound != null)
            {
                AudioSource camAudio = wmanInCameraObject.GetComponent<AudioSource>();
                if (camAudio == null) camAudio = wmanInCameraObject.AddComponent<AudioSource>();
                camAudio.spatialBlend = 0f;
                camAudio.PlayOneShot(jumpscareSound, soundVolume);
            }

            if (camToShake != null && cameraShakeIntensity > 0.001f)
            {
                StartCoroutine(CameraShakeRoutine(camToShake, inCameraJumpscareDuration, cameraShakeIntensity));
            }

            yield return new WaitForSeconds(inCameraJumpscareDuration);

            // KIỂM TRA CHẾ ĐỘ BẤT TỬ (GOD MODE PHÍM M)
            if (GodModeManager.IsGodModeActive)
            {
                Debug.Log("<color=cyan><b>[WmanBehavior] 👑 GodMode đang BẬT -> Thả tự do người chơi và reset Wman!</b></color>");

                if (wmanInCameraObject != null) wmanInCameraObject.SetActive(false);

                if (playerMove != null)
                {
                    playerMove.isCameraLocked = false;
                    playerMove.enabled = true;
                    playerMove.SetMovementState(true);
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // Reset vị trí và trạng thái Wman
                transform.position = homePosition;
                transform.rotation = homeRotation;
                currentMoveDirection = transform.forward;
                lastPosition = homePosition;
                stuckTimer = 0f;
                stuckEscapeTimer = 0f;

                Renderer[] rList = GetComponentsInChildren<Renderer>(true);
                foreach (var r in rList) r.enabled = true;

                Collider[] cList = GetComponentsInChildren<Collider>(true);
                foreach (var col in cList) col.enabled = true;

                // Cho Wman đứng nghỉ 1.5s (Grace Period) để người chơi có thời gian chạy
                recoveryTimer = 1.5f;
                hasCaughtPlayer = false;
                currentState = WmanState.IdleAtHome;
                SetAnimationState(false);
                yield break;
            }

            GameOverJumpscareManager.Instance.TriggerGameOverDeathScreen(wmanInCameraObject, 1.2f, mainMenuSceneName, endScreenObject, endScreenSprite);
            yield break;
        }
        else
        {
            if (jumpscareSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(jumpscareSound, soundVolume);
            }
            yield return new WaitForSeconds(inCameraJumpscareDuration);

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

                transform.position = homePosition;
                transform.rotation = homeRotation;
                currentMoveDirection = transform.forward;
                lastPosition = homePosition;
                stuckTimer = 0f;
                stuckEscapeTimer = 0f;

                recoveryTimer = 1.5f;
                hasCaughtPlayer = false;
                currentState = WmanState.IdleAtHome;
                SetAnimationState(false);
                yield break;
            }

            GameOverJumpscareManager.Instance.TriggerGameOverDeathScreen(null, 1.2f, mainMenuSceneName, endScreenObject, endScreenSprite);
            yield break;
        }
    }
}
