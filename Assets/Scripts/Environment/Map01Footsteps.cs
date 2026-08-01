using UnityEngine;

public class Map01Footsteps : MonoBehaviour
{
    [Header("1. File Âm Thanh 1 Bước Chân (Single Footstep WAV)")]
    [Tooltip("Kéo file âm thanh 1 bước chân (.wav) vào đây")]
    public AudioClip singleFootstepSound;

    [Header("2. Khoảng Cách Giữa 2 Bước Chân (Dành cho Walk Speed = 0.5)")]
    [Tooltip("Thời gian giữa 2 bước chân khi bước đi (Nên để 0.5 đến 0.7s cho nhịp bước chân đi tự nhiên)")]
    public float stepInterval = 0.6f;

    [Header("3. Âm Lượng & Biến Tấu Cao Độ (Tạo Cảm Giác Chân Thực)")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng bước chân (Mặc định: 0.8)")]
    public float footstepVolume = 0.8f;

    [Tooltip("Tự động ngẫu nhiên hóa cao độ (Pitch) để 1 file .wav bước chân nghe cực kỳ tự nhiên")]
    public bool randomizePitch = true;
    public float minPitch = 0.92f;
    public float maxPitch = 1.08f;

    private CharacterController controller;
    private AudioSource audioSource;
    private float stepTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null) controller = GetComponentInParent<CharacterController>();

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

            if (stepTimer >= stepInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            // Sẵn sàng phát tiếng bước chân ngay khi bắt đầu bấm WASD di chuyển
            stepTimer = stepInterval * 0.8f;
        }
    }

    void PlayFootstep()
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

        audioSource.PlayOneShot(singleFootstepSound, footstepVolume);
    }
}
