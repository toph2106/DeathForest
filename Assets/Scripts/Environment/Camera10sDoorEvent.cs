using UnityEngine;
using System.Collections;

public class Camera10sDoorEvent : MonoBehaviour
{
    [Header("1. Thời Điểm Kích Hoạt Sự Kiện (Giây)")]
    [Tooltip("Thời gian đếm trên máy quay để kích hoạt sự kiện mở cửa (Mặc định: 10.0s)")]
    public float triggerTime = 10.0f;

    [Header("2. Cánh Cửa Cần Mở Tự Động")]
    [Tooltip("Kéo Object Cửa Chính (chứa script DoorExit) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM trong Scene!")]
    public DoorExit doorToOpen;

    [Header("3. Âm Thanh Mở Cửa & Hiệu Ứng Kinh Dị (Tùy chọn)")]
    [Tooltip("Âm thanh cọt kẹt / trượt mở cửa kinh dị. Nếu để trống sẽ dùng tiếng mở cửa mặc định của DoorExit")]
    public AudioClip customDoorOpenSound;

    [Tooltip("Âm thanh báo hiệu / tiếng giật mình rùng rợn phát ra ngay khi vừa chạm mốc 10s")]
    public AudioClip eerieCueSound;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng hiệu ứng âm thanh")]
    public float soundVolume = 0.85f;

    [Header("4. Con Gián Xuất Hiện Lao Vào (Tùy chọn)")]
    [Tooltip("Kéo con gián (chứa script CockroachFlyAttack) vào đây để kích hoạt sau khi cửa mở")]
    public CockroachFlyAttack cockroachToTrigger;

    [Tooltip("Thời gian chờ sau khi cửa mở ra rồi gián mới bắt đầu chuỗi hành động (giây)")]
    public float cockroachTriggerDelay = 0.5f;

    private AudioSource audioSource;
    private bool hasTriggered = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (doorToOpen == null)
        {
            doorToOpen = Object.FindFirstObjectByType<DoorExit>();
        }
    }

    void OnEnable()
    {
        CamcorderUI.OnTimerReached10s += OnTriggerEvent;
    }

    void OnDisable()
    {
        CamcorderUI.OnTimerReached10s -= OnTriggerEvent;
    }

    void Update()
    {
        // Quét dự phòng theo thời gian đếm thực của máy quay
        if (!hasTriggered && CamcorderUI.Instance != null && CamcorderUI.Instance.gameObject.activeInHierarchy)
        {
            if (CamcorderUI.Instance.CurrentActiveTime >= triggerTime)
            {
                OnTriggerEvent();
            }
        }
    }

    public void OnTriggerEvent()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log("[Camera10sDoorEvent] 🎬 BỘ ĐẾM MÁY QUAY ĐẠT MỐC 10S! Khởi động sự kiện cánh cửa tự mở ra...");

        StartCoroutine(ExecuteDoorOpenSequence());
    }

    IEnumerator ExecuteDoorOpenSequence()
    {
        // 1. Phát âm thanh Sting / Eerie Cue rùng rợn nếu có
        if (eerieCueSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(eerieCueSound, soundVolume);
        }

        // 2. TỰ ĐỘNG MỞ CÁNH CỬA CHÍNH (Kèm âm thanh mở cửa & tăng âm lượng thành phố)
        if (doorToOpen == null)
        {
            doorToOpen = Object.FindFirstObjectByType<DoorExit>();
        }

        if (doorToOpen != null)
        {
            doorToOpen.OpenDoorAutomatically(customDoorOpenSound);
            Debug.Log("[Camera10sDoorEvent] 🚪 Đã mở toang cánh cửa chính ra ngoài!");
        }

        // 3. TỰ ĐỘNG KÍCH HOẠT CON GIÁN MẸ SAU KHI CỬA MỞ (Nếu có)
        if (cockroachToTrigger != null)
        {
            if (cockroachTriggerDelay > 0f)
            {
                yield return new WaitForSeconds(cockroachTriggerDelay);
            }
            cockroachToTrigger.gameObject.SetActive(true);
            cockroachToTrigger.StartCockroachSequence();
        }

        // 4. TỰ ĐỘNG KÍCH HOẠT CÁC CON GIÁN CON BÒ VÀO NHÀ CHẠY QUANH PHÒNG
        BabyCockroachCrawler[] babyCockroaches = Object.FindObjectsByType<BabyCockroachCrawler>(FindObjectsSortMode.None);
        foreach (var baby in babyCockroaches)
        {
            if (baby != null) baby.StartCrawling();
        }
    }
}
