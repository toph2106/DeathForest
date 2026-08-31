using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Cửa trượt mượt mà (Smooth Sliding Door):
/// Kế thừa IInteractable -> Tự động nhận diện chuột và hiện icon Bàn Tay (Hand) qua InteractPro!
/// Có bộ đệm chống đúp click (Debounce) để bấm 1 lần là mở ngay, không bị kẹt hay phải bấm 2 lần.
/// </summary>
public class SmoothSlidingDoor : MonoBehaviour, IInteractable
{
    [Header("Door Element")]
    [Tooltip("Kéo phần cánh cửa thực sự dịch chuyển vào đây")]
    public Transform doorBody;

    [Header("UI Interaction Hint")]
    public GameObject doorHintUI; // Cái Canvas "Press F" dính trên cửa (nếu có)

    [Header("Sliding Settings")]
    [Tooltip("Khoảng cách và hướng cửa sẽ trượt đi (Ví dụ: X = 1.1 nghĩa là trượt sang phải 1.1 mét)")]
    public Vector3 slideDirection = new Vector3(1.1f, 0f, 0f);
    public float doorSpeed = 3f;

    [Header("Audio Settings (Tùy Chọn)")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("Door Events (Sự Kiện Đóng/Mở)")]
    public UnityEvent onDoorOpened;
    public UnityEvent onDoorClosed;

    public bool isDoorOpen { get; private set; } = false;
    public System.Action<bool> onDoorStateChanged;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private float lastToggleTime = -1f; // Chống gọi đúp 2 lần trong cùng 1 click

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (openSound != null || closeSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D Sound
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        // Ghi nhớ vị trí đóng ban đầu của cửa (tọa độ Local để không bị lỗi khi quay Map)
        if (doorBody != null)
        {
            closedPosition = doorBody.localPosition;
            // Vị trí mở bằng vị trí đóng cộng thêm khoảng cách trượt
            openPosition = closedPosition + slideDirection;
        }

        if (doorHintUI != null) doorHintUI.SetActive(false);
    }

    void Update()
    {
        if (doorBody == null) return;

        // Nội suy dịch chuyển vị trí mượt mà (Lerp) dựa trên trạng thái đóng/mở
        if (isDoorOpen)
        {
            doorBody.localPosition = Vector3.Lerp(doorBody.localPosition, openPosition, Time.deltaTime * doorSpeed);
        }
        else
        {
            doorBody.localPosition = Vector3.Lerp(doorBody.localPosition, closedPosition, Time.deltaTime * doorSpeed);
        }
    }

    /// <summary>
    /// Được gọi tự động bởi hệ thống tương tác khi nhìn vào cửa và bấm Chuột Trái
    /// </summary>
    public void Interact()
    {
        ToggleDoor();
    }

    // =================================================================
    // CÁC HÀM CÔNG KHAI ĐỂ CAMERA NHÌN VÀO GỌI ĐƯỢC
    // =================================================================

    public void ToggleDoor()
    {
        // CHỐNG GỌI ĐÚP: Nếu vừa được gọi trong vòng 0.2s thì bỏ qua lệnh thừa
        if (Time.time - lastToggleTime < 0.2f) return;
        lastToggleTime = Time.time;

        isDoorOpen = !isDoorOpen;

        // Phát âm thanh mở / đóng cửa (nếu có)
        if (audioSource != null)
        {
            AudioClip clipToPlay = isDoorOpen ? openSound : closeSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay, soundVolume);
            }
        }

        if (isDoorOpen) onDoorOpened?.Invoke();
        else onDoorClosed?.Invoke();

        onDoorStateChanged?.Invoke(isDoorOpen);
    }

    public void OpenDoor()
    {
        if (!isDoorOpen) ToggleDoor();
    }

    public void CloseDoor()
    {
        if (isDoorOpen) ToggleDoor();
    }

    public void ShowPrompt()
    {
        if (doorHintUI != null) doorHintUI.SetActive(true);
    }

    public void HidePrompt()
    {
        if (doorHintUI != null) doorHintUI.SetActive(false);
    }
}