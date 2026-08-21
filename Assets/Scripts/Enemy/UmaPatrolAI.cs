using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// AI Tuần Tra & Săn Mồi Của Uma:
/// 1. Player vào vùng cấm -> Uma đi bộ chậm (walk_normal)
/// 2. Player lọt bán kính phát hiện:
///    - Thân người ĐỨNG BẤT ĐỘNG giữ nguyên hướng bò lúc phát hiện
///    - CỔ DÀI (neg_013) xoay ngang ngoái nhìn theo Player (quay quanh trục thẳng đứng)
/// 3. Phóng dash lao thẳng vào Player
/// 4. Khi tóm được:
///    - Chuyển sang Jumpscare Camera
///    - Mở ngoác mồm NGAY LẬP TỨC (0s)
///    - BÙNG NỔ NGAY LẬP TỨC chuỗi Jumpscare hoảng loạn + FADE ĐEN CHẠY ĐỒNG THỜI SONG SONG:
///      + Rung nảy Camera dữ dội (Position + Pitch/Yaw/Roll Spasms)
///      + Tăng cực đại Post-Processing (Vignette + Chromatic)
///      + Màn hình mờ đen dần trong lúc đang rung nảy kinh hoàng
///      + Đen 100% -> Mở chuột -> Nhấp bất kỳ về Menu!
/// </summary>
public class UmaPatrolAI : MonoBehaviour
{
    public enum UmaAIState
    {
        IdleAtHome,
        PatrolToPlayer,
        AlertStare,
        DashCharge,
        JumpscareAttack,
        ReturningHome
    }

    [Header("1. Hoạt Ảnh (Chỉ Cần 2 Clip)")]
    [Tooltip("Kéo clip 'walk_normal' vào đây")]
    public AnimationClip walkClip;

    [Tooltip("Kéo clip 'dash' vào đây")]
    public AnimationClip dashClip;

    [Header("2. Bán Kính & Tốc Độ")]
    public float detectionRadius = 12.0f;
    public float patrolWalkSpeed = 3.0f;
    public float stareDuration = 1.5f;
    public float dashSpeed = 26.0f;
    public float attackDistance = 1.8f;
    public float returnWalkSpeed = 3.0f;

    [Header("3. Âm Thanh")]
    public AudioClip alertStareSound;
    public AudioClip dashRoarSound;
    public AudioClip jumpscareSound;

    [Header("4. Bám Đất")]
    public bool snapToGround = true;
    public float groundOffsetY = 0.0f;
    public float raycastHeightAbove = 10f;
    public float raycastDistance = 25f;
    public LayerMask groundLayerMask = ~0;

    [Header("5. Setup Xương & Camera (Kéo Thủ Công)")]
    [Tooltip("Kéo Camera Jumpscare đã tạo trước mặt Uma vào đây")]
    public Camera jumpscareCamera;

    [Tooltip("Kéo xương CỔ (neg_013) vào đây - để quái vật chỉ ngoái cổ nhìn theo Player")]
    public Transform neckBone;

    [Tooltip("Kéo xương ĐẦU (head_014) vào đây")]
    public Transform headBone;

    [Tooltip("Kéo xương HÀM/MIỆNG (mause_015) vào đây")]
    public Transform mouthBone;

    [Header("6. Cấu Hình Há Mồm Tức Thì (Instant Jaw Drop)")]
    [Tooltip("Vector dịch chuyển xương mause_015 (X: Bù lệch trái/phải, Y: Độ há sâu, Z: Trước/sau). Mặc định: (0.02, -0.25, 0)")]
    public Vector3 jawDropOffset = new Vector3(0.02f, -0.25f, 0f);

    [Header("7. Hiệu Ứng Jumpscare & Fade Song Song")]
    [Tooltip("Thời gian rung giật hoảng loạn Jumpscare (giây - Mặc định: 1.8s)")]
    public float panicDuration = 1.8f;

    [Tooltip("Thời gian mờ đen màn hình (giây - Fade chạy SONG SONG ngay lúc Jumpscare bắt đầu)")]
    public float fadeToBlackDuration = 1.4f;

    [Tooltip("Độ rung nảy vị trí Camera (Mặc định: 0.06)")]
    public float shakePositionStrength = 0.06f;

    [Tooltip("Độ lắc giật góc nhìn Pitch, Yaw, Roll như đang giãy giụa (độ - Mặc định: 4.5 độ)")]
    public float panicRotationSpasm = 4.5f;

    [Tooltip("Tự động kích hoạt méo màu Chromatic & bo viền tối Vignette")]
    public bool enablePostProcessingEffects = true;

    [Tooltip("Độ tối viền đen Vignette cực đại lúc hoảng loạn (0.0 -> 1.0)")]
    [Range(0f, 1f)] public float maxVignetteIntensity = 0.65f;

    [Tooltip("Độ méo màu Chromatic Aberration cực đại lúc hoảng loạn (0.0 -> 1.0)")]
    [Range(0f, 1f)] public float maxChromaticIntensity = 1.0f;

    [Header("8. Game Over Chuyển Menu")]
    [Tooltip("Bật màn hình đen & bấm bất kỳ để về Menu")]
    public bool enableGameOverSequence = true;

    [Header("9. Debug")]
    public UmaAIState currentState = UmaAIState.IdleAtHome;

    // --- Private ---
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Transform playerTransform;
    private MovePl playerScript;
    private Camera playerCam;
    private Animator animator;
    private AudioSource audioSource;
    private PlayableGraph playableGraph;
    private Coroutine aiCoroutine;
    private bool isPlayerInZone = false;
    private AnimationClip currentPlayingClip;
    private float currentJawDropProgress = 0f;

    // Vị trí & góc xoay gốc
    private Vector3 jumpscareCamStartLocalPos;
    private Quaternion jumpscareCamStartLocalRot;
    private Vector3 mouthOriginalLocalPos;
    private Quaternion mouthOriginalLocalRot;
    private Quaternion headOriginalLocalRot;
    private Quaternion neckOriginalLocalRot;
    private Quaternion neckStareBaseWorldRot;
    private float currentNeckYawAngle = 0f;
    private bool bonesInitialized = false;

    // Post-Processing
    private Volume globalVolume;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private float initialVignette = 0f;
    private float initialChromatic = 0f;

    void Awake()
    {
        // Tìm Animator trên con
        Animator[] allAnimators = GetComponentsInChildren<Animator>(true);
        foreach (var a in allAnimators)
        {
            if (a.gameObject != gameObject) { animator = a; break; }
        }
        if (animator == null && allAnimators.Length > 0) animator = allAnimators[0];
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        animator.applyRootMotion = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.4f;
        audioSource.minDistance = 5f;
        audioSource.maxDistance = 60f;

        homePosition = transform.position;
        homeRotation = transform.rotation;

        if (jumpscareCamera != null)
        {
            jumpscareCamStartLocalPos = jumpscareCamera.transform.localPosition;
            jumpscareCamStartLocalRot = jumpscareCamera.transform.localRotation;
            jumpscareCamera.gameObject.SetActive(false);

            UniversalAdditionalCameraData camData = jumpscareCamera.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                camData.volumeLayerMask = ~0;
            }
        }

        FindBonesIfNull();
        InitBones();
        InitPostProcessing();
    }

    void Start()
    {
        FindPlayer();
        ForceSnapToGround();
        FindBonesIfNull();
        InitBones();
    }

    private void FindBonesIfNull()
    {
        if (neckBone == null)
        {
            neckBone = FindBoneRecursive(transform, "neg_013");
            if (neckBone == null) neckBone = FindBoneRecursive(transform, "neg");
            if (neckBone == null) neckBone = FindBoneRecursive(transform, "neck");
        }
        if (headBone == null)
        {
            headBone = FindBoneRecursive(transform, "head_014");
            if (headBone == null) headBone = FindBoneRecursive(transform, "head");
        }
        if (mouthBone == null)
        {
            mouthBone = FindBoneRecursive(transform, "mause_015");
            if (mouthBone == null) mouthBone = FindBoneRecursive(transform, "mause");
        }
    }

    private Transform FindBoneRecursive(Transform parent, string boneNameKey)
    {
        if (parent == null) return null;
        if (parent.name.ToLower().Contains(boneNameKey.ToLower()))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindBoneRecursive(parent.GetChild(i), boneNameKey);
            if (found != null) return found;
        }
        return null;
    }

    private void InitBones()
    {
        if (bonesInitialized) return;

        if (neckBone != null)
        {
            neckOriginalLocalRot = neckBone.localRotation;
        }

        if (headBone != null)
        {
            headOriginalLocalRot = headBone.localRotation;
        }

        if (mouthBone != null)
        {
            mouthOriginalLocalPos = mouthBone.localPosition;
            mouthOriginalLocalRot = mouthBone.localRotation;
        }

        if (mouthBone != null && headBone != null && neckBone != null)
        {
            bonesInitialized = true;
        }
    }

    private void InitPostProcessing()
    {
        if (!enablePostProcessingEffects) return;

        globalVolume = Object.FindFirstObjectByType<Volume>();
        if (globalVolume == null)
        {
            GameObject volObj = new GameObject("UmaGlobalVolume");
            globalVolume = volObj.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 100f;
            globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        if (globalVolume != null && globalVolume.profile != null)
        {
            // Vignette
            if (!globalVolume.profile.TryGet(out vignette))
            {
                vignette = globalVolume.profile.Add<Vignette>(true);
            }
            if (vignette != null)
            {
                vignette.intensity.overrideState = true;
                initialVignette = vignette.intensity.value;
            }

            // Chromatic Aberration
            if (!globalVolume.profile.TryGet(out chromaticAberration))
            {
                chromaticAberration = globalVolume.profile.Add<ChromaticAberration>(true);
            }
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.overrideState = true;
                initialChromatic = chromaticAberration.intensity.value;
            }
        }
    }

    void LateUpdate()
    {
        // 1. Giữ child model ở gốc local
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (jumpscareCamera != null && (child == jumpscareCamera.transform || jumpscareCamera.transform.IsChildOf(child))) continue;
            if (child.localPosition != Vector3.zero)
                child.localPosition = Vector3.zero;
        }

        // 2. Há mồm: Gán trực tiếp theo jawDropOffset
        if (mouthBone != null)
        {
            if (currentJawDropProgress > 0.001f)
            {
                mouthBone.localPosition = mouthOriginalLocalPos + jawDropOffset * currentJawDropProgress;
            }
            else
            {
                mouthBone.localPosition = mouthOriginalLocalPos;
            }
        }

        // 3. Xoay cổ (neg_013) ngoái nhìn theo Player quanh trục thẳng đứng Vector3.up (100% không bị chổng lên trời)
        if (currentState == UmaAIState.AlertStare && playerTransform != null && neckBone != null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                float targetAngle = Vector3.SignedAngle(transform.forward, toPlayer, Vector3.up);
                targetAngle = Mathf.Clamp(targetAngle, -85f, 85f);
                currentNeckYawAngle = Mathf.Lerp(currentNeckYawAngle, targetAngle, 6f * Time.deltaTime);

                neckBone.rotation = Quaternion.AngleAxis(currentNeckYawAngle, Vector3.up) * neckStareBaseWorldRot;
            }
        }
    }

    private void FindPlayer()
    {
        if (playerScript == null) playerScript = Object.FindFirstObjectByType<MovePl>();
        if (playerScript != null)
        {
            playerTransform = playerScript.transform;
            if (playerScript.cameraTransform != null)
                playerCam = playerScript.cameraTransform.GetComponent<Camera>();
        }
        else
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
            {
                playerTransform = pObj.transform;
                playerScript = pObj.GetComponent<MovePl>();
                if (playerScript != null && playerScript.cameraTransform != null)
                    playerCam = playerScript.cameraTransform.GetComponent<Camera>();
            }
        }
        if (playerCam == null) playerCam = Camera.main;
    }

    // ====================================================================
    // API
    // ====================================================================

    public void OnPlayerEnteredZone(Transform player)
    {
        playerTransform = player;
        isPlayerInZone = true;
        if (aiCoroutine != null) StopCoroutine(aiCoroutine);
        aiCoroutine = StartCoroutine(PatrolAndHuntRoutine());
    }

    public void OnPlayerExitedZone()
    {
        isPlayerInZone = false;
        if (currentState != UmaAIState.JumpscareAttack)
        {
            if (aiCoroutine != null) StopCoroutine(aiCoroutine);
            aiCoroutine = StartCoroutine(ReturnHomeRoutine());
        }
    }

    // ====================================================================
    // CHUỖI HÀNH VI
    // ====================================================================

    IEnumerator PatrolAndHuntRoutine()
    {
        FindPlayer();
        if (playerTransform == null) yield break;

        // ĐI BỘ
        currentState = UmaAIState.PatrolToPlayer;
        PlayAnimationSafe(walkClip);
        currentJawDropProgress = 0f;

        while (isPlayerInZone && playerTransform != null)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) <= detectionRadius)
                break;
            RotateTowards(playerTransform.position, 5f);
            MoveForward(patrolWalkSpeed);
            ForceSnapToGround();
            yield return null;
        }
        if (!isPlayerInZone) yield break;

        // ========== ĐỨNG NHÌN (THÂN BẤT ĐỘNG, CHỈ XOAY NGANG CỔ NHÌN PLAYER) ==========
        currentState = UmaAIState.AlertStare;
        StopAnimation();

        currentNeckYawAngle = 0f;
        if (neckBone != null)
        {
            neckStareBaseWorldRot = neckBone.rotation;
        }

        if (alertStareSound != null && audioSource != null)
            audioSource.PlayOneShot(alertStareSound, 1.0f);

        float stareTimer = 0f;
        while (stareTimer < stareDuration && isPlayerInZone)
        {
            stareTimer += Time.deltaTime;
            ForceSnapToGround();
            yield return null;
        }
        if (!isPlayerInZone) yield break;

        // Khôi phục cổ trước khi phóng Dash
        if (neckBone != null && bonesInitialized)
        {
            neckBone.localRotation = neckOriginalLocalRot;
        }

        // ========== DASH LAO VÀO PLAYER ==========
        currentState = UmaAIState.DashCharge;
        PlayAnimationSafe(dashClip);

        if (dashRoarSound != null && audioSource != null)
        {
            audioSource.spatialBlend = 0.2f;
            audioSource.PlayOneShot(dashRoarSound, 1.0f);
        }

        while (currentState == UmaAIState.DashCharge)
        {
            if (playerTransform == null) break;
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            RotateTowards(playerTransform.position, 15f);
            MoveForward(dashSpeed);
            ForceSnapToGround();
            if (dist <= attackDistance) break;
            yield return null;
        }

        // JUMPSCARE TỨC THÌ
        yield return StartCoroutine(InstantCockroachJumpscareRoutine());
    }

    // ====================================================================
    // JUMPSCARE TỨC THÌ (MỞ MỒM NGAY 0S + FADE ĐEN CHẠY ĐỒNG THỜI SONG SONG VỚI EFFECT)
    // ====================================================================

    IEnumerator InstantCockroachJumpscareRoutine()
    {
        currentState = UmaAIState.JumpscareAttack;
        StopAnimation();

        // 1. Khóa Player
        if (playerScript != null)
        {
            playerScript.isCameraLocked = true;
            playerScript.forcedLookTarget = null;
            playerScript.SetMovementState(false);
        }
        CharacterController pcc = playerScript?.GetComponent<CharacterController>();
        if (pcc != null) pcc.enabled = false;

        // 2. Bật âm thanh Jumpscare đinh tai
        if (jumpscareSound != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.PlayOneShot(jumpscareSound, 1.0f);
        }

        FindPlayer();

        if (jumpscareCamera == null)
        {
            Debug.LogWarning("[UmaPatrolAI] Chưa kéo jumpscareCamera vào Inspector!");
            yield break;
        }

        // 3. Chuyển sang Jumpscare Camera
        jumpscareCamera.gameObject.SetActive(true);
        jumpscareCamera.enabled = true;
        jumpscareCamera.transform.localPosition = jumpscareCamStartLocalPos;
        jumpscareCamera.transform.localRotation = jumpscareCamStartLocalRot;

        if (playerCam != null) playerCam.enabled = false;

        Vector3 baseCamPos = jumpscareCamStartLocalPos;
        Quaternion baseCamRot = jumpscareCamStartLocalRot;

        // Giữ đầu & cổ thẳng ngay ngắn
        if (headBone != null && bonesInitialized) headBone.localRotation = headOriginalLocalRot;
        if (neckBone != null && bonesInitialized) neckBone.localRotation = neckOriginalLocalRot;

        InitPostProcessing();

        // 4. MỞ NGOÁC MỒM NGAY LẬP TỨC (0s)
        currentJawDropProgress = 1f;

        // 5. KÍCH HOẠT FADE ĐEN NGAY LẬP TỨC ĐỂ CHẠY SONG SONG TRONG LÚC ĐANG RUNG GIẬT!
        if (enableGameOverSequence)
        {
            if (GameOverJumpscareManager.Instance != null)
            {
                GameOverJumpscareManager.Instance.TriggerGameOver(fadeToBlackDuration);
            }
            else
            {
                GameOverJumpscareManager mgr = Object.FindFirstObjectByType<GameOverJumpscareManager>();
                if (mgr == null)
                {
                    GameObject mgrObj = new GameObject("GameOverJumpscareManager");
                    mgr = mgrObj.AddComponent<GameOverJumpscareManager>();
                }
                mgr.TriggerGameOver(fadeToBlackDuration);
            }
        }

        // 6. CHUỖI RUNG NẢY & MÉO MÀU CHẠY SONG SONG CÙNG LÚC MÀN HÌNH ĐANG MỜ ĐEN DẦN
        float panicElapsed = 0f;

        while (panicElapsed < panicDuration)
        {
            panicElapsed += Time.deltaTime;
            float t = (panicDuration > 0f) ? Mathf.Clamp01(panicElapsed / panicDuration) : 1f;

            // Rung nảy vị trí camera (X, Y)
            float posX = (Mathf.PerlinNoise(Time.time * 28f, 0f) - 0.5f) * 2f * shakePositionStrength;
            float posY = (Mathf.PerlinNoise(0f, Time.time * 28f) - 0.5f) * 2f * shakePositionStrength;

            // Lắc giật góc nhìn 3 trục (Pitch, Yaw, Roll)
            float rotX = (Mathf.PerlinNoise(Time.time * 22f, 15f) - 0.5f) * 2f * panicRotationSpasm;
            float rotY = (Mathf.PerlinNoise(15f, Time.time * 22f) - 0.5f) * 2f * panicRotationSpasm;
            float rotZ = (Mathf.PerlinNoise(Time.time * 20f, 35f) - 0.5f) * 2f * panicRotationSpasm * 1.5f;

            jumpscareCamera.transform.localPosition = baseCamPos + new Vector3(posX, posY, 0f);
            jumpscareCamera.transform.localRotation = baseCamRot * Quaternion.Euler(rotX, rotY, rotZ);

            // Post Processing cực đại
            if (enablePostProcessingEffects)
            {
                if (vignette != null)
                {
                    vignette.intensity.value = Mathf.Lerp(maxVignetteIntensity, maxVignetteIntensity * 0.75f, t);
                }
                if (chromaticAberration != null)
                {
                    chromaticAberration.intensity.value = Mathf.Lerp(maxChromaticIntensity, maxChromaticIntensity * 0.75f, t);
                }
            }

            yield return null;
        }

        Debug.Log("[UmaPatrolAI] 💀 JUMPSCARE HOÀN TẤT: Màn hình đã tối đen & chuyển giao sang GameOverJumpscareManager!");
    }

    // ====================================================================
    // QUAY VỀ NHÀ
    // ====================================================================

    IEnumerator ReturnHomeRoutine()
    {
        currentState = UmaAIState.ReturningHome;
        currentJawDropProgress = 0f;

        // Khôi phục Post-Processing
        if (enablePostProcessingEffects)
        {
            if (vignette != null) vignette.intensity.value = initialVignette;
            if (chromaticAberration != null) chromaticAberration.intensity.value = initialChromatic;
        }

        if (mouthBone != null) mouthBone.localPosition = mouthOriginalLocalPos;
        if (headBone != null) headBone.localRotation = headOriginalLocalRot;
        if (neckBone != null) neckBone.localRotation = neckOriginalLocalRot;

        if (jumpscareCamera != null)
        {
            jumpscareCamera.transform.localPosition = jumpscareCamStartLocalPos;
            jumpscareCamera.transform.localRotation = jumpscareCamStartLocalRot;
            jumpscareCamera.gameObject.SetActive(false);
        }
        if (playerCam != null) playerCam.enabled = true;

        PlayAnimationSafe(walkClip);

        while (Vector3.Distance(transform.position, homePosition) > 0.5f)
        {
            if (isPlayerInZone) yield break;
            RotateTowards(homePosition, 5f);
            MoveForward(returnWalkSpeed);
            ForceSnapToGround();
            yield return null;
        }

        transform.position = homePosition;
        transform.rotation = homeRotation;
        currentState = UmaAIState.IdleAtHome;
        StopAnimation();
    }

    // ====================================================================
    // HELPER
    // ====================================================================

    private void RotateTowards(Vector3 targetPos, float speed)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f),
            speed * Time.deltaTime
        );
    }

    private void MoveForward(float speed)
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        transform.position += forward * speed * Time.deltaTime;
    }

    private void ForceSnapToGround()
    {
        if (!snapToGround) return;
        Vector3 pos = transform.position;
        Vector3 rayOrigin = new Vector3(pos.x, pos.y + raycastHeightAbove, pos.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.isTrigger || hit.collider.transform.IsChildOf(transform) || hit.collider.CompareTag("Player"))
            {
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastDistance, groundLayerMask, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    if (h.collider.isTrigger) continue;
                    if (h.collider.transform.IsChildOf(transform)) continue;
                    if (h.collider.CompareTag("Player")) continue;
                    if (h.normal.y > 0.25f)
                    {
                        pos.y = h.point.y + groundOffsetY;
                        transform.position = pos;
                        return;
                    }
                }
                return;
            }
            pos.y = hit.point.y + groundOffsetY;
            transform.position = pos;
        }
    }

    // ====================================================================
    // ANIMATION
    // ====================================================================

    private void PlayAnimationSafe(AnimationClip clip)
    {
        if (clip == null || animator == null) return;
        if (clip == currentPlayingClip) return;
        try
        {
            if (playableGraph.IsValid()) playableGraph.Destroy();
            AnimationPlayableUtilities.PlayClip(animator, clip, out playableGraph);
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            currentPlayingClip = clip;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UmaPatrolAI] PlayClip: {ex.Message}");
        }
    }

    private void StopAnimation()
    {
        if (playableGraph.IsValid()) playableGraph.Destroy();
        currentPlayingClip = null;
    }

    void OnDestroy()
    {
        if (playableGraph.IsValid()) playableGraph.Destroy();
        if (enablePostProcessingEffects)
        {
            if (vignette != null) vignette.intensity.value = initialVignette;
            if (chromaticAberration != null) chromaticAberration.intensity.value = initialChromatic;
        }
        if (playerCam != null) playerCam.enabled = true;
        if (jumpscareCamera != null) jumpscareCamera.gameObject.SetActive(false);
        if (playerScript != null)
        {
            playerScript.isCameraLocked = false;
            playerScript.forcedLookTarget = null;
            playerScript.SetMovementState(true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        if (snapToGround)
        {
            Gizmos.color = Color.green;
            Vector3 origin = transform.position + Vector3.up * raycastHeightAbove;
            Gizmos.DrawLine(origin, origin + Vector3.down * raycastDistance);
        }

        if (neckBone != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(neckBone.position, 0.12f);
        }

        if (headBone != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(headBone.position, 0.15f);
        }

        if (mouthBone != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(mouthBone.position, 0.1f);
        }
    }
}
