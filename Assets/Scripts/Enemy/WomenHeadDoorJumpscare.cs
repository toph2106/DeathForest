using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;

/// <summary>
/// Chuỗi Jumpscare Đầu Nữ Trước Cửa & Trong Camera (WomenHead Door & In-Camera Jumpscare)
/// Kịch bản:
/// 1. Xuất hiện trước cửa (WomenHead2 active).
/// 2. Bật animation move liên tục, đứng im 1 chỗ và xoay mặt theo Player trong 5 giây.
/// 3. Sau 5 giây: Phóng thẳng xuyên qua Camera của Player.
/// 4. Đợi tiếp 2 giây: Kích hoạt WomenHead trong Camera (WomenHeadPoint) áp sát mặt!
/// </summary>
public class WomenHeadDoorJumpscare : MonoBehaviour
{
    public static WomenHeadDoorJumpscare Instance { get; private set; }

    [Header("0. Tham Chiếu Camera Người Chơi")]
    [Tooltip("Kéo Main Camera vào đây (Tự tìm nếu để trống)")]
    public Transform playerCamera;

    [Header("1. Cấu Hình Đầu Nữ Trước Cửa (Door Head - WomenHead2)")]
    [Tooltip("GameObject đầu nữ trước cửa (Nếu gắn script lên chính WomenHead2 thì để trống sẽ tự lấy)")]
    public GameObject doorHeadObject;
    public AnimationClip moveAnimationClip;
    public string animationStateName = "ENM_women_head_move";
    public Animator headAnimator;

    [Tooltip("Thời gian đứng im trước cửa nhìn chằm chằm vào Player (Mặc định: 5.0 giây)")]
    public float stareDuration = 5.0f;

    [Tooltip("Góc nghiêng trục X khi đứng nhìn (Mặc định: -20 độ)")]
    public float tiltRotationX = -20f;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("2. Cấu Hình Bay Lao Tới (Fly Past)")]
    [Tooltip("Tốc độ bay lao vào mặt Player (m/s - Mặc định: 35)")]
    public float flySpeed = 35f;

    [Tooltip("Khoảng cách bay xuyên qua sau lưng Camera (mét)")]
    public float flyPastDistance = 15f;

    [Tooltip("Bù độ cao điểm ngắm Camera (âm = hạ thấp xuống tầm mặt)")]
    public float targetHeightOffset = -0.5f;

    [Tooltip("Âm thanh khi bắt đầu lao tới")]
    public AudioClip flyAttackAudio;

    [Header("3. Cấu Hình Jumpscare Trong Camera (In-Camera Head - WomenHeadPoint)")]
    [Tooltip("Kéo GameObject 'WomenHeadPoint' (con của Main Camera) vào đây")]
    public GameObject inCameraHeadObject;

    [Tooltip("Kéo AnimationClip cho đầu trong Camera (Ví dụ: ENM_WHED_hyokkori)")]
    public AnimationClip inCameraAnimationClip;

    [Tooltip("Hoặc tên State trong Animator Controller của đầu trong Camera (Mặc định: ENM_WHED_hyokkori)")]
    public string inCameraStateName = "ENM_WHED_hyokkori";

    [Tooltip("Thời gian chờ sau khi va chạm rồi mới hiện đầu trong Camera (Mặc định: 2.0 giây)")]
    public float delayBeforeInCamera = 2.0f;

    [Tooltip("Thời gian đầu trong Camera hiện hình trước khi biến mất (Mặc định: 1.8 giây, 0 = hiện mãi mãi)")]
    public float inCameraDuration = 1.8f;

    [Tooltip("Âm thanh Jumpscare đập vào mặt cực mạnh")]
    public AudioClip inCameraJumpscareAudio;

    [Header("4. Tùy Chọn Kích Hoạt Tự Động")]
    [Tooltip("Nếu kéo vật phẩm 'Head' (InteractableItem) vào đây, khi nhặt đầu trắng sẽ TỰ ĐỘNG kích hoạt chuỗi jumpscare này!")]
    public InteractableItem triggerItem;

    [Tooltip("Sự kiện kết thúc toàn bộ chuỗi (Tùy chọn)")]
    public UnityEvent onSequenceComplete;

    private bool isRunning = false;
    private AudioSource audioSource;
    private PlayableGraph playableGraph;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (doorHeadObject == null) doorHeadObject = gameObject;

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        // TỰ ĐỘNG ẨN HÌNH ẢNH CỦA ĐẦU NỮ TRƯỚC CỬA LÚC ĐẦU (Không cần tắt Active gameObject)
        SetVisualsActive(false);

        // Đảm bảo đầu trong Camera ban đầu luôn tắt
        if (inCameraHeadObject != null)
        {
            inCameraHeadObject.SetActive(false);
        }

        // Tự động lắng nghe sự kiện nhặt vật phẩm nếu có gán
        if (triggerItem != null)
        {
            // Kiểm tra trong Coroutine khi vật phẩm được nhặt
            StartCoroutine(WaitForItemPickupRoutine());
        }
    }

    private void SetVisualsActive(bool active)
    {
        if (doorHeadObject != null)
        {
            Renderer[] renderers = doorHeadObject.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) r.enabled = active;

            Collider[] cols = doorHeadObject.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                c.isTrigger = true;
                c.enabled = active;
            }
        }
    }

    private IEnumerator WaitForItemPickupRoutine()
    {
        if (triggerItem == null) yield break;

        // Đợi cho đến khi vật phẩm bị nhặt (bị inactive hoặc destroy)
        while (triggerItem != null && triggerItem.gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(0.2f);
        }

        // Vật phẩm vừa được nhặt -> Kích hoạt Jumpscare!
        if (!isRunning)
        {
            StartSequence();
        }
    }

    /// <summary>
    /// Hàm gọi kích hoạt toàn bộ chuỗi Jumpscare (Có thể gọi từ sự kiện nhặt đầu trắng hoặc qua cửa)
    /// </summary>
    public void StartSequence()
    {
        if (isRunning) return;
        isRunning = true;

        gameObject.SetActive(true);
        if (doorHeadObject != null) doorHeadObject.SetActive(true);

        StartCoroutine(FullJumpscareSequenceRoutine());
    }

    private IEnumerator FullJumpscareSequenceRoutine()
    {
        Debug.Log("[WomenHeadDoorJumpscare] 🚪 Bắt đầu chuỗi Jumpscare: Đầu Nữ xuất hiện trước cửa!");

        // 1. Hiện đầu nữ trước cửa
        if (doorHeadObject != null)
        {
            doorHeadObject.SetActive(true);
            SetVisualsActive(true);

            // Tắt Collider để không chặn đường hay đẩy người chơi
            Collider[] cols = doorHeadObject.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                c.isTrigger = true;
                c.enabled = true;
            }

            // Kích hoạt animation move liên tục
            PlayMoveAnimation();
        }

        // 2. Đứng im 1 chỗ và liên tục xoay mặt nhìn theo Player trong stareDuration (5s)
        float stareTimer = 0f;
        while (stareTimer < stareDuration)
        {
            stareTimer += Time.deltaTime;

            if (doorHeadObject != null)
            {
                RotateTowardsPlayer(doorHeadObject.transform);
            }

            yield return null;
        }

        Debug.Log("[WomenHeadDoorJumpscare] 💨 Hết 5s nhìn chằm chằm -> Đầu Nữ bắt đầu phóng qua người chơi!");

        // 3. Phát âm thanh bay lao tới
        if (flyAttackAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(flyAttackAudio, 1.0f);
        }

        // 4. Bay thẳng xuyên qua Player
        yield return StartCoroutine(FlyPastPlayerRoutine());

        // 5. Ẩn hình ảnh đầu nữ trước cửa sau khi bay xong (Dùng SetVisualsActive để Coroutine KHÔNG BỊ HỦY)
        SetVisualsActive(false);

        Debug.Log($"[WomenHeadDoorJumpscare] ⏳ Đã bay qua người! Đang đợi {delayBeforeInCamera} giây trước cú Jumpscare trong Camera...");

        // 6. Đợi đúng 2 giây (delayBeforeInCamera)
        yield return new WaitForSeconds(delayBeforeInCamera);

        // 7. KÍCH HOẠT ĐẦU NỮ TRONG CAMERA (WomenHeadPoint) & PHÁT ANIMATION
        Debug.Log("[WomenHeadDoorJumpscare] 😱 JUMPSCARE TRONG CAMERA!");
        if (inCameraHeadObject != null)
        {
            // Bật toàn bộ cha mẹ (như GameObject Jumpscare) nếu đang bị tắt
            EnsureParentsActive(inCameraHeadObject);

            inCameraHeadObject.SetActive(true);

            Renderer[] camRends = inCameraHeadObject.GetComponentsInChildren<Renderer>(true);
            foreach (var r in camRends) r.enabled = true;

            // KÍCH HOẠT ANIMATION CHO ĐẦU TRONG CAMERA (ENM_WHED_hyokkori)
            PlayInCameraAnimation();

            // Phát âm thanh tiếng hét Jumpscare cực mạnh
            if (inCameraJumpscareAudio != null && audioSource != null)
            {
                audioSource.PlayOneShot(inCameraJumpscareAudio, 1.0f);
            }

            // Thời gian giữ hình trước khi biến mất
            if (inCameraDuration > 0f)
            {
                yield return new WaitForSeconds(inCameraDuration);
                inCameraHeadObject.SetActive(false);
            }
        }

        // 8. Tắt hoàn toàn sau khi xong hết chuỗi
        if (doorHeadObject != null && doorHeadObject != gameObject)
        {
            doorHeadObject.SetActive(false);
        }

        // Kích hoạt sự kiện hoàn tất
        onSequenceComplete?.Invoke();
        Debug.Log("[WomenHeadDoorJumpscare] ✨ Toàn bộ chuỗi Jumpscare đã hoàn tất thành công!");
    }

    private void EnsureParentsActive(GameObject target)
    {
        if (target == null) return;
        Transform current = target.transform.parent;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }
    }

    private void RotateTowardsPlayer(Transform targetTransform)
    {
        Transform cam = GetCameraTransform();
        if (cam == null) return;

        Vector3 toCam = (cam.position - targetTransform.position).normalized;
        Vector3 flatDir = toCam;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude > 0.001f)
        {
            float targetYaw = Quaternion.LookRotation(flatDir).eulerAngles.y;
            targetTransform.rotation = Quaternion.Euler(tiltRotationX, targetYaw, 0f) * Quaternion.Euler(rotationOffset);
        }
    }

    private IEnumerator FlyPastPlayerRoutine()
    {
        if (doorHeadObject == null) yield break;

        Transform cam = GetCameraTransform();
        if (cam == null) yield break;

        Transform headTr = doorHeadObject.transform;
        Vector3 startPos = headTr.position;
        Vector3 camPos = cam.position + Vector3.up * targetHeightOffset;
        Vector3 toCamDir = (camPos - startPos).normalized;
        Vector3 targetPos = camPos + toCamDir * flyPastDistance;

        float totalDist = Vector3.Distance(startPos, targetPos);
        float duration = totalDist / Mathf.Max(flySpeed, 1f);
        float elapsed = 0f;

        Vector3 flatDir = toCamDir;
        flatDir.y = 0f;
        float targetYaw = (flatDir.sqrMagnitude > 0.001f) ? Quaternion.LookRotation(flatDir).eulerAngles.y : headTr.eulerAngles.y;
        Quaternion finalFlyRot = Quaternion.Euler(tiltRotationX, targetYaw, 0f) * Quaternion.Euler(rotationOffset);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            headTr.position = Vector3.Lerp(startPos, targetPos, t);
            headTr.rotation = finalFlyRot;

            yield return null;
        }

        headTr.position = targetPos;
    }

    private void PlayMoveAnimation()
    {
        if (headAnimator == null && doorHeadObject != null)
        {
            headAnimator = doorHeadObject.GetComponent<Animator>() ?? doorHeadObject.GetComponentInChildren<Animator>();
        }

        if (headAnimator == null) return;
        headAnimator.enabled = true;

        if (moveAnimationClip != null)
        {
            try
            {
                AnimationPlayableUtilities.PlayClip(headAnimator, moveAnimationClip, out playableGraph);
                playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                return;
            }
            catch { }
        }

        if (headAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(animationStateName))
        {
            headAnimator.Play(animationStateName, 0, 0f);
        }
    }

    private void PlayInCameraAnimation()
    {
        if (inCameraHeadObject == null) return;

        Animator camAnim = inCameraHeadObject.GetComponent<Animator>() ?? inCameraHeadObject.GetComponentInChildren<Animator>();
        if (camAnim == null) return;

        camAnim.enabled = true;

        // Ưu tiên 1: Dùng PlayClip nếu kéo AnimationClip vào
        if (inCameraAnimationClip != null)
        {
            try
            {
                AnimationPlayableUtilities.PlayClip(camAnim, inCameraAnimationClip, out _);
                return;
            }
            catch { }
        }

        // Ưu tiên 2: Play State trong Animator Controller (Ví dụ: ENM_WHED_hyokkori)
        if (camAnim.runtimeAnimatorController != null && !string.IsNullOrEmpty(inCameraStateName))
        {
            camAnim.Play(inCameraStateName, 0, 0f);
        }
    }

    private Transform GetCameraTransform()
    {
        if (playerCamera != null) return playerCamera;
        if (Camera.main != null) return Camera.main.transform;
        MovePl pl = Object.FindFirstObjectByType<MovePl>();
        if (pl != null)
        {
            Camera c = pl.GetComponentInChildren<Camera>();
            return c != null ? c.transform : pl.transform;
        }
        return null;
    }

    void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }
}
