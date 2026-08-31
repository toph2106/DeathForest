using UnityEngine;

/// <summary>
/// Quản lý sự kiện quái vật Stalker rình rập ngoài cửa sổ khi đọc giấy:
/// 1. Bạn CÓ THỂ TẮT DẤU TÍCH [ ] death_forest_-_stalker LÚC ĐẦU GAME THOẢI MÁI.
/// 2. Khi được kích hoạt (OnEnable): Bật Stalker lên, khóa Animator (anim.speed = 0) để giữ tư thế rùng rợn, tự động đóng cửa trượt.
/// 3. Stalker luôn xoay mặt nhìn thẳng chằm chằm theo Player.
/// 4. Sau khi đọc xong giấy: Khi người chơi mở lại cánh cửa, Stalker sẽ tự động BIẾN MẤT!
/// </summary>
public class WindowStalkerEvent : MonoBehaviour
{
    [Header("1. Tham Chiếu Cửa Trượt (Sliding Door)")]
    [Tooltip("Kéo cánh cửa trượt DoorR (chứa SmoothSlidingDoor) vào đây")]
    public SmoothSlidingDoor slidingDoor;

    [Header("2. Tham Chiếu Tờ Giấy Đọc (Inspectable 3D Paper)")]
    [Tooltip("Kéo tờ giấy trên bàn (chứa Inspectable3DPaper) vào đây")]
    public Inspectable3DPaper targetPaper;

    [Header("3. Cài Đặt Xoay Nhìn Player (Look Settings)")]
    [Tooltip("Tốc độ xoay mặt nhìn theo Player (Số càng cao xoay càng nhanh)")]
    public float lookRotationSpeed = 15f;
    [Tooltip("Bù trừ góc xoay nếu mô hình bị lệch hướng gốc (Ví dụ: Y: 180 nếu nó quay lưng)")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("4. Cài Đặt Hoạt Ảnh & Âm Thanh (Animation & Audio)")]
    public Animator animator;
    [Tooltip("Tốc độ chạy animation (0 = đóng băng tư thế dang tay rùng rợn)")]
    public float freezeAnimationSpeed = 0f;

    public AudioSource audioSource;
    [Tooltip("Âm thanh ma mị khi Stalker xuất hiện ngoài cửa sổ")]
    public AudioClip appearSound;
    [Tooltip("Âm thanh gió thoảng / biến mất khi mở cửa")]
    public AudioClip disappearSound;
    [Range(0f, 1f)] public float soundVolume = 0.9f;

    // --- Private State ---
    private Transform playerTransform;
    private bool hasCompletedReading = false;
    private bool hasDisappeared = false;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (appearSound != null || disappearSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D Sound
            audioSource.playOnAwake = false;
        }

        FindReferences();
    }

    void OnEnable()
    {
        FindReferences();

        // 1. Khóa Animation ở tư thế dang tay rùng rợn
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.speed = freezeAnimationSpeed;
        }

        // 2. Tự động đóng sập cánh cửa trượt
        if (slidingDoor == null) slidingDoor = Object.FindFirstObjectByType<SmoothSlidingDoor>();
        if (slidingDoor != null)
        {
            slidingDoor.CloseDoor();
            slidingDoor.onDoorOpened.RemoveListener(OnDoorOpened);
            slidingDoor.onDoorOpened.AddListener(OnDoorOpened);
        }

        // 3. Đăng ký nhận thông báo đọc xong giấy
        if (targetPaper == null) targetPaper = Object.FindFirstObjectByType<Inspectable3DPaper>();
        if (targetPaper != null)
        {
            targetPaper.onReadCompleted.RemoveListener(OnPaperReadFinished);
            targetPaper.onReadCompleted.AddListener(OnPaperReadFinished);
        }

        // 4. Phát âm thanh xuất hiện
        if (appearSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(appearSound, soundVolume);
        }

        Debug.Log("[WindowStalkerEvent] 👁️ Stalker đã thức tỉnh ngoài cửa sổ và tự đóng cửa lại!");
    }

    void FindReferences()
    {
        if (playerTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null) playerTransform = mainCam.transform;
            else
            {
                GameObject pl = GameObject.FindGameObjectWithTag("Player");
                if (pl != null) playerTransform = pl.transform;
            }
        }

        if (slidingDoor == null) slidingDoor = Object.FindFirstObjectByType<SmoothSlidingDoor>();
        if (targetPaper == null) targetPaper = Object.FindFirstObjectByType<Inspectable3DPaper>();
    }

    void Update()
    {
        // Khi đang active: Luôn xoay mặt nhìn thẳng theo Player
        if (!hasDisappeared && playerTransform != null)
        {
            Vector3 lookDir = playerTransform.position - transform.position;
            lookDir.y = 0f; // Khóa trục Y để Stalker đứng thẳng trên sàn, không bị nghiêng

            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir) * Quaternion.Euler(rotationOffset);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookRotationSpeed);
            }
        }

        // Kiểm tra dự phòng: Nếu đã đọc xong giấy mà cửa đang mở -> Biến mất ngay
        if (hasCompletedReading && !hasDisappeared && slidingDoor != null && slidingDoor.isDoorOpen)
        {
            Disappear();
        }
    }

    /// <summary>
    /// Được gọi khi người chơi đọc xong văn bản và cất tờ giấy vào túi
    /// </summary>
    public void OnPaperReadFinished()
    {
        hasCompletedReading = true;
        Debug.Log("[WindowStalkerEvent] 📄 Người chơi đã đọc xong giấy, mở cửa sổ sẽ khiến Stalker biến mất!");
    }

    /// <summary>
    /// Được gọi khi cánh cửa trượt được mở ra
    /// </summary>
    public void OnDoorOpened()
    {
        if (hasCompletedReading && !hasDisappeared)
        {
            Disappear();
        }
    }

    private void Disappear()
    {
        if (hasDisappeared) return;
        hasDisappeared = true;

        if (disappearSound != null)
        {
            AudioSource.PlayClipAtPoint(disappearSound, transform.position, soundVolume);
        }

        Debug.Log("[WindowStalkerEvent] 💨 Cửa đã mở, Stalker đã biến mất trong bóng tối!");
        gameObject.SetActive(false);
    }
}
