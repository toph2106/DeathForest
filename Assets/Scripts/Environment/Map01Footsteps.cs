using UnityEngine;

public class Map01Footsteps : MonoBehaviour
{
    [Header("1. File Âm Thanh 1 Bước Chân (Single Footstep WAV)")]
    [Tooltip("Kéo file âm thanh 1 bước chân (.wav) vào đây")]
    public AudioClip singleFootstepSound;

    [Header("2. Khoảng Cách Giữa 2 Bước Chân Đi Bộ (Walk Step Interval)")]
    [Tooltip("Thời gian giữa 2 bước chân khi bước đi bộ (Mặc định: 0.6s)")]
    public float stepInterval = 0.6f;

    [Header("3. Cấu Hình Bước Chân Khi Ngồi (Crouch Step Interval)")]
    [Tooltip("Hệ số nhân thời gian bước chân khi ngồi (2.0 = nhân đôi thời gian từ 0.6s lên 1.2s vì tốc độ giảm nửa)")]
    public float crouchStepIntervalMultiplier = 2.0f;

    [Tooltip("Hệ số âm lượng khi ngồi (0.6 = âm thanh êm ái rón rén hơn)")]
    public float crouchVolumeMultiplier = 0.6f;

    [Header("4. Cấu Hình Bước Chân Khi Chạy Nhanh (Sprint Step Interval)")]
    [Tooltip("Hệ số nhân thời gian bước chân khi chạy nhanh (0.65 = nhịp chân dồn dập hơn)")]
    public float sprintStepIntervalMultiplier = 0.65f;

    [Header("5. Âm Lượng & Biến Tấu Cao Độ (Tạo Cảm Giác Chân Thực)")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng bước chân chuẩn (Mặc định: 0.8)")]
    public float footstepVolume = 0.8f;

    [Tooltip("Tự động ngẫu nhiên hóa cao độ (Pitch) để 1 file .wav bước chân nghe cực kỳ tự nhiên")]
    public bool randomizePitch = true;
    public float minPitch = 0.92f;
    public float maxPitch = 1.08f;

    private CharacterController controller;
    private MovePl movePl;
    private AudioSource audioSource;
    private float stepTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null) controller = GetComponentInParent<CharacterController>();

        movePl = GetComponent<MovePl>();
        if (movePl == null) movePl = GetComponentInParent<MovePl>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 0f; // Âm thanh 2D nghe rõ ràng trực tiếp cho người chơi
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (singleFootstepSound == null) return;

        // 1. Kiểm tra bấm phím di chuyển WASD / Mũi tên
        bool isPressingKeys = (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f);

        // 2. Kiểm tra vận tốc di chuyển thực tế
        bool hasVelocity = false;
        if (controller != null && controller.enabled)
        {
            Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
            hasVelocity = horizontalVelocity.magnitude > 0.05f;
        }

        bool isMoving = isPressingKeys || hasVelocity;

        if (isMoving)
        {
            stepTimer += Time.deltaTime;

            // TÍNH TOÁN KHOẢNG THỜI GIAN (INTERVAL) & ÂM LƯỢNG (VOLUME) THEO TRẠNG THÁI
            float currentInterval = stepInterval;
            float currentVolume = footstepVolume;

            bool isCrouching = (movePl != null && movePl.isCrouching);
            bool isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching;

            if (isCrouching)
            {
                currentInterval *= crouchStepIntervalMultiplier; // Nhân đôi khoảng thời gian giữa 2 bước (VD: 0.6s -> 1.2s)
                currentVolume *= crouchVolumeMultiplier;         // Giảm bớt âm lượng cho tiếng bước chân rón rén
            }
            else if (isSprinting)
            {
                currentInterval *= sprintStepIntervalMultiplier; // Nhịp chân dồn dập khi chạy
            }

            if (stepTimer >= currentInterval)
            {
                PlayFootstep(currentVolume);
                stepTimer = 0f;
            }
        }
        else
        {
            // Sẵn sàng phát tiếng bước chân ngay khi vừa bấm phím di chuyển
            float currentInterval = stepInterval;
            if (movePl != null && movePl.isCrouching) currentInterval *= crouchStepIntervalMultiplier;
            stepTimer = currentInterval * 0.8f;
        }
    }

    void PlayFootstep(float volume)
    {
        if (audioSource == null || singleFootstepSound == null) return;

        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            audioSource.pitch = 1.0f;
        }

        audioSource.PlayOneShot(singleFootstepSound, volume);
    }
}
