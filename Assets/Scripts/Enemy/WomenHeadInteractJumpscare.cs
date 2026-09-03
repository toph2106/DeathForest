using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;

/// <summary>
/// Script Tương Tác Jumpscare cho Đầu Nữ (WomenHead)
/// Tính năng:
/// 1. Kế thừa IInteractable -> Tự động nhận diện bởi tâm ngắm bàn tay (InteractPro).
/// 2. Khi Click: Ngay lập tức TẮT BoxCollider để không đẩy Player hoặc kẹt nhà.
/// 3. Kích hoạt Animation di chuyển (ENM_women_head_move).
/// 4. Nghiêng góc X về -20 độ (ngước nhìn thẳng vào mặt người chơi).
/// 5. Bay thẳng xuyên qua Camera của Player một đoạn rồi biến mất (Destroy / Inactive).
/// 6. Phát âm thanh Jumpscare / Tiếng hét rùng rợn.
/// </summary>
public class WomenHeadInteractJumpscare : MonoBehaviour, IInteractable
{
    [Header("0. Tham Chiếu Camera Người Chơi (Player Camera)")]
    [Tooltip("Kéo Main Camera của Player vào đây để nhận diện chuẩn xác 100% vị trí và hướng mặt người chơi (Tự tìm nếu để trống)")]
    public Transform playerCamera;

    [Header("1. Cấu Hình Animation (Clip Hoặc State)")]
    [Tooltip("Kéo AnimationClip 'ENM_women_head_move' vào đây")]
    public AnimationClip moveAnimationClip;
    [Tooltip("Hoặc tên State trong Animator Controller (nếu có gán)")]
    public string animationStateName = "ENM_women_head_move";
    public Animator headAnimator;

    [Header("2. Cấu Hình Bay & Tọa Độ (Fly & Trajectory)")]
    [Tooltip("Tốc độ bay lao tới mặt Player (m/s)")]
    public float flySpeed = 25f;

    [Tooltip("Khoảng cách bay xuyên qua sau lưng Camera trước khi biến mất (mét - Nhập tùy ý)")]
    public float flyPastDistance = 15f;

    [Tooltip("Bù độ cao điểm ngắm Camera (âm = hạ thấp xuống để đập trúng mặt/mắt vào camera, VD: -0.5 hoặc -1.0)")]
    public float targetHeightOffset = -0.5f;

    [Tooltip("Góc nghiêng trục X khi bay (Mặc định: -20 độ ngước nhìn lên mặt Player)")]
    public float tiltRotationX = -20f;

    [Tooltip("Góc bù xoay phụ (X, Y, Z) nếu model FBX bị ngược hướng")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("3. Âm Thanh Hét / Jumpscare")]
    [Tooltip("Âm thanh tiếng hét / Jumpscare khi đầu nữ phóng tới")]
    public AudioClip jumpscareAudio;
    [Range(0f, 1f)] public float audioVolume = 1.0f;

    [Header("4. Sau Khi Bay Xuyên Qua Camera")]
    [Tooltip("Ẩn GameObject (SetActive false) hay Xóa hẳn (Destroy)?")]
    public bool destroyOnComplete = false;

    [Tooltip("Sự kiện kích hoạt sau khi Jumpscare kết thúc (Tùy chọn)")]
    public UnityEvent onJumpscareComplete;

    [Header("5. Vật Phẩm Mở Khóa Sau Jumpscare (Unlocked Item)")]
    [Tooltip("Kéo GameObject Đầu Trắng (Head) vào đây. Lúc đầu nó VẪN HIỆN HÌNH nhưng bị KHÓA TƯƠNG TÁC & COLLIDER, sau khi tương tác đầu nữ xong mới mở khóa cho nhặt!")]
    public GameObject unlockItemObject;

    [Tooltip("Chỉ tắt Collider & Script nhặt đồ (vẫn nhìn thấy model đầu trắng 100%)")]
    public bool disableColliderOnly = true;

    [Tooltip("Mở khóa vật phẩm sau khi đầu nữ bay xong (True) hay Mở khóa ngay khi vừa bấm click (False)?")]
    public bool showAfterFly = true;

    // Trạng thái nội bộ
    private bool isTriggered = false;
    private AudioSource audioSource;
    private PlayableGraph playableGraph;

    void Start()
    {
        if (playerCamera == null)
        {
            if (Camera.main != null) playerCamera = Camera.main.transform;
            else
            {
                MovePl pl = Object.FindFirstObjectByType<MovePl>();
                if (pl != null)
                {
                    Camera c = pl.GetComponentInChildren<Camera>();
                    playerCamera = (c != null) ? c.transform : pl.transform;
                }
            }
        }

        if (headAnimator == null)
        {
            headAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Âm thanh 2D rõ nét trực diện
        audioSource.playOnAwake = false;

        // TẠM KHÓA COLLIDER & TƯƠNG TÁC CỦA ĐẦU TRẮNG LÚC ĐẦU (MODEL VẪN HIỆN 100%)
        if (unlockItemObject != null)
        {
            if (disableColliderOnly)
            {
                unlockItemObject.SetActive(true); // Đảm bảo model vẫn hiện hình
                Collider[] itemCols = unlockItemObject.GetComponentsInChildren<Collider>(true);
                foreach (var c in itemCols) c.enabled = false;

                InteractableItem itemScript = unlockItemObject.GetComponent<InteractableItem>();
                if (itemScript != null) itemScript.enabled = false;
            }
            else
            {
                unlockItemObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Hàm tương tác IInteractable (Được gọi khi người chơi bấm chuột trái vào WomenHead)
    /// </summary>
    public void Interact()
    {
        if (isTriggered) return;
        isTriggered = true;

        Debug.Log("[WomenHeadInteract] 👻 Người chơi đã click tương tác vào Đầu Nữ -> Kích hoạt Jumpscare phóng tới mặt!");

        // 1. NGAY LẬP TỨC TẮT TOÀN BỘ COLLIDER ĐỂ TRÁNH ĐẨY NGƯỜI CHƠI HOẶC KẸT TƯỜNG
        DisableAllColliders();

        // 2. PHÁT ÂM THANH JUMPSCARE
        PlayJumpscareSound();

        // 3. KÍCH HOẠT ANIMATION
        PlayMoveAnimation();

        // 4. NẾU CẤU HÌNH HIỆN VẬT PHẨM NGAY KHI CLICK
        if (!showAfterFly)
        {
            EnableUnlockedItem();
        }

        // 5. BẮT ĐẦU CHUỖI COROUTINE BAY XUYÊN QUA CAMERA
        StartCoroutine(FlyThroughCameraRoutine());
    }

    private void DisableAllColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            col.isTrigger = true; // Chuyển sang Trigger: Không đẩy Player hay kẹt nhà, nhưng SphereCast vẫn bắt được để tự động giảm sáng!
            col.enabled = true;
        }
    }

    private void PlayJumpscareSound()
    {
        if (jumpscareAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpscareAudio, audioVolume);
        }
    }

    private void PlayMoveAnimation()
    {
        if (headAnimator == null) return;
        headAnimator.enabled = true;

        // Ưu tiên 1: Dùng PlayClip trực tiếp nếu có gắn Clip
        if (moveAnimationClip != null)
        {
            try
            {
                AnimationPlayableUtilities.PlayClip(headAnimator, moveAnimationClip, out playableGraph);
                playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[WomenHeadInteract] PlayClip: {ex.Message}");
            }
        }

        // Ưu tiên 2: Play State trong Animator Controller
        if (headAnimator.runtimeAnimatorController != null && !string.IsNullOrEmpty(animationStateName))
        {
            headAnimator.Play(animationStateName, 0, 0f);
        }
    }

    private IEnumerator FlyThroughCameraRoutine()
    {
        // 1. Lấy vị trí Camera đã kéo vào hoặc tự tìm
        Transform camTransform = playerCamera;
        if (camTransform == null && Camera.main != null) camTransform = Camera.main.transform;
        if (camTransform == null)
        {
            MovePl pl = Object.FindFirstObjectByType<MovePl>();
            if (pl != null)
            {
                Camera c = pl.GetComponentInChildren<Camera>();
                camTransform = (c != null) ? c.transform : pl.transform;
            }
        }

        if (camTransform == null)
        {
            Debug.LogWarning("[WomenHeadInteract] ⚠️ Không tìm thấy Camera của Player để phóng tới!");
            yield break;
        }

        Vector3 startPos = transform.position;
        // Điểm ngắm Camera có bù độ cao (hạ thấp xuống nếu tóc làm lệch tâm mặt)
        Vector3 camPos = camTransform.position + Vector3.up * targetHeightOffset;
        
        // Vector hướng từ đầu nữ bay thẳng vào mắt Camera
        Vector3 toCamDir = (camPos - startPos).normalized;

        // Điểm đích: Bay xuyên qua sau lưng Camera thêm 1 đoạn (flyPastDistance)
        Vector3 targetPos = camPos + toCamDir * flyPastDistance;

        // Tổng quãng đường & thời gian bay
        float totalDistance = Vector3.Distance(startPos, targetPos);
        float duration = totalDistance / Mathf.Max(flySpeed, 1f);
        float elapsed = 0f;

        // Tính góc xoay nhìn về phía người chơi + nghiêng X = -20 độ
        Vector3 flatDir = toCamDir;
        flatDir.y = 0f;
        float targetYaw = (flatDir.sqrMagnitude > 0.001f) ? Quaternion.LookRotation(flatDir).eulerAngles.y : transform.eulerAngles.y;
        Quaternion finalFlyRotation = Quaternion.Euler(tiltRotationX, targetYaw, 0f) * Quaternion.Euler(rotationOffset);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Di chuyển mượt mà lao thẳng xuyên qua Camera
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = finalFlyRotation;

            yield return null;
        }

        // Đảm bảo tới đích cuối
        transform.position = targetPos;

        // Dọn dẹp PlayableGraph
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }

        // Kích hoạt sự kiện kết thúc
        onJumpscareComplete?.Invoke();

        // 5. NẾU CẤU HÌNH HIỆN VẬT PHẨM SAU KHI BAY XONG
        if (showAfterFly)
        {
            EnableUnlockedItem();
        }

        // 6. BIẾN MẤT (Ẩn hoặc Xóa)
        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

        Debug.Log("[WomenHeadInteract] ✨ Jumpscare hoàn tất! Đầu Nữ đã biến mất an toàn.");
    }

    private void EnableUnlockedItem()
    {
        if (unlockItemObject == null) return;

        unlockItemObject.SetActive(true);

        Collider[] itemCols = unlockItemObject.GetComponentsInChildren<Collider>(true);
        foreach (var c in itemCols) c.enabled = true;

        InteractableItem itemScript = unlockItemObject.GetComponent<InteractableItem>();
        if (itemScript != null) itemScript.enabled = true;

        Debug.Log($"[WomenHeadInteract] 🎁 Đã mở khóa Collider & Tương tác cho vật phẩm: {unlockItemObject.name}!");
    }

    void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }
}
