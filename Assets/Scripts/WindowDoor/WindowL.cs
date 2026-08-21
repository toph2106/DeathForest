using UnityEngine;

public class WindowL : MonoBehaviour, IInteractable
{
    public float slideDistance = 0.5f;
    public float slideSpeed = 2f;

    [Tooltip("Tích chọn nếu muốn cửa sổ MẶC ĐỊNH MỞ NGAY KHI VÀO GAME (Mặc định: true)")]
    public bool startOpened = true;

    [Header("Âm Thanh Thao Tác Cửa Sổ (Tùy Chọn)")]
    public AudioClip windowOpenSound;
    public AudioClip windowCloseSound;
    [Range(0f, 1f)] public float windowSfxVolume = 0.35f;

    [Header("Mở Khóa Case PC Sau Khi Đóng Cửa Sổ")]
    [Tooltip("Kéo Collider của Case PC vào đây để mở khóa tương tác khi đóng cửa sổ")]
    public Collider caseColliderToEnable;
    public GameObject caseObjectToEnable;

    private Vector3 closedPos;
    private Vector3 openPos;
    private Vector3 targetPos;
    private bool isOpen = true;
    public bool IsOpen => isOpen;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        closedPos = transform.position;
        openPos = closedPos - (transform.right * slideDistance);

        isOpen = startOpened;
        targetPos = startOpened ? openPos : closedPos;

        if (startOpened)
        {
            transform.position = openPos;
        }

        WindowAmbienceController.SyncAllWindowAmbience();
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * slideSpeed);
    }

    public void CloseWindow(bool snapInstantly = false)
    {
        if (!isOpen) return;
        isOpen = false;
        targetPos = closedPos;
        if (snapInstantly)
        {
            transform.position = closedPos;
        }
        WindowAmbienceController.SyncAllWindowAmbience();
    }

    public void Interact()
    {
        isOpen = !isOpen;
        targetPos = isOpen ? openPos : closedPos;

        if (isOpen)
        {
            if (windowOpenSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(windowOpenSound, windowSfxVolume);
            }
        }
        else
        {
            if (windowCloseSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(windowCloseSound, windowSfxVolume);
            }
        }

        WindowAmbienceController.SyncAllWindowAmbience();
    }
}