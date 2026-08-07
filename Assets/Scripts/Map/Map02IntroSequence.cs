using UnityEngine;
using System.Collections;

public class Map02IntroSequence : MonoBehaviour
{
    [Header("1. Vị Trí Spawn Người Chơi (Spawn Point)")]
    [Tooltip("Kéo Object SpawnPoint ban đầu trong Map 02 vào đây")]
    public Transform spawnPoint;

    [Header("2. Cấu Hình Tự Động Di Chuyển (Cinematic Auto-Walk)")]
    [Tooltip("Tốc độ tự động bước đi về phía trước (Mặc định: 2.0m/s)")]
    public float autoWalkSpeed = 2.0f;

    [Tooltip("Tùy chọn: Kéo 1 Object (hoặc Empty GameObject lơ lửng ngang tầm mắt) ở phía trước con đường để ghim tầm nhìn camera vào đó")]
    public Transform forcedLookTarget;

    [Tooltip("Thời gian mờ mắt nhìn theo mục tiêu (giây)")]
    public float lookSmoothSpeed = 5.0f;

    [Header("3. Cấu Hình Đèn Pin Điện Ảnh")]
    [Tooltip("Thời gian tự động BẬT ĐÈN PIN sau khi đi bộ vào Map (Mặc định: 1.0s)")]
    public float turnOnFlashlightDelay = 1.0f;

    [Header("4. Kích Hoạt Trigger Xe / Ending (Sau Khi Xong Cutscene Intro)")]
    [Tooltip("Kéo Object TruckTrigger vào đây. Script sẽ tự động ẩn nó lúc đầu và BẬT ACTIVE nó lên ngay sau khi nhân vật vừa đi bộ xong đoạn Intro!")]
    public GameObject truckTriggerObject;

    private MovePl playerMove;
    private CharacterController characterController;
    private Transform mainCamTransform;
    private bool isIntroActive = false;

    void Start()
    {
        // Ẩn TruckTrigger ngay khi nạp Scene để không bị kích hoạt nhầm lúc đang đi bộ Intro
        if (truckTriggerObject != null)
        {
            truckTriggerObject.SetActive(false);
        }

        playerMove = FindFirstObjectByType<MovePl>();
        if (playerMove != null)
        {
            characterController = playerMove.GetComponent<CharacterController>();
            if (playerMove.cameraTransform != null)
            {
                mainCamTransform = playerMove.cameraTransform;
            }
        }

        if (Camera.main != null && mainCamTransform == null)
        {
            mainCamTransform = Camera.main.transform;
        }

        // TẮT ĐÈN PIN BAN ĐẦU KHI MỚI BẮT ĐẦU SANG MAP 02
        FlashlightToggle flashlight = FindFirstObjectByType<FlashlightToggle>();
        if (flashlight != null)
        {
            flashlight.SetFlashlightState(false, false);
        }

        // BẮT ĐẦU CHUỖI TỰ ĐỘNG DI CHUYỂN KHI VỪA NẠP MAP 02
        StartCoroutine(StartIntroRoutine());
    }

    IEnumerator StartIntroRoutine()
    {
        // Wait 1 frame để các script khác (PauseMenuManager, Screen Fade) khởi tạo xong
        yield return null;

        if (playerMove != null)
        {
            // 1. DỊCH CHUYỂN PLAYER VỀ VỊ TRÍ SPAWN POINT
            if (spawnPoint != null)
            {
                if (characterController != null) characterController.enabled = false;
                playerMove.transform.position = spawnPoint.position;
                playerMove.transform.rotation = spawnPoint.rotation;
                if (characterController != null) characterController.enabled = true;
            }

            // 2. KHÓA ĐIỀU KHIỂN BÀN PHÍM VÀ CHUỘT NGƯỜI CHƠI
            playerMove.isCameraLocked = true;
            playerMove.enabled = false;
        }

        // Bật đếm thời gian 1s tự động bật đèn pin
        StartCoroutine(AutoTurnOnFlashlightRoutine());

        isIntroActive = true;
    }

    IEnumerator AutoTurnOnFlashlightRoutine()
    {
        yield return new WaitForSeconds(turnOnFlashlightDelay);
        FlashlightToggle flashlight = FindFirstObjectByType<FlashlightToggle>();
        if (flashlight != null)
        {
            // CHỈ BẬT ĐÈN VÀ PHÁT TIẾNG "TÁCH" NẾU ĐÈN ĐANG TẮT!
            if (!flashlight.IsOn())
            {
                flashlight.SetFlashlightState(true, true);
            }
        }
    }

    void Update()
    {
        if (!isIntroActive) return;

        // 1. TỰ ĐỘNG ÉP PLAYER ĐI BỘ VỀ PHÍA TRƯỚC (FORWARD)
        if (characterController != null && characterController.enabled)
        {
            Vector3 moveDir = playerMove.transform.forward * autoWalkSpeed;
            moveDir.y = -9.81f * Time.deltaTime; // Giữ trọng lực bám sàn
            characterController.Move(moveDir * Time.deltaTime);
        }

        // 2. TỰ ĐỘNG KHÓA TẦM NHÌN NGHỆ THUẬT VÀO MỤC TIÊU LƠ LỬNG (NẾU CÓ)
        if (forcedLookTarget != null && mainCamTransform != null)
        {
            Vector3 direction = (forcedLookTarget.position - mainCamTransform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                mainCamTransform.rotation = Quaternion.Slerp(mainCamTransform.rotation, targetRotation, Time.deltaTime * lookSmoothSpeed);

                // Cập nhật hướng quay thân người theo góc nhìn để khi thả chuột ra không bị nhảy góc!
                if (playerMove != null)
                {
                    Vector3 euler = mainCamTransform.eulerAngles;
                    playerMove.transform.rotation = Quaternion.Euler(0, euler.y, 0);
                }
            }
        }
    }

    // KHI PLAYER CHẠM VÀO CỤC CUBE TÀN HÌNH (TARGETWS) -> KẾT THÚC CẮT CẢNH VÀ BẬT TRUCKTRIGGER!
    void OnTriggerEnter(Collider other)
    {
        if (!isIntroActive) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            EndIntroSequence();
        }
    }

    void EndIntroSequence()
    {
        isIntroActive = false;

        if (playerMove != null)
        {
            // ĐỒNG BỘ CHÍNH XÁC GÓC XOAY CAMERA HIỆN TẠI VÀO MOVEPL TRƯỚC KHI TRẢ CHUỘT
            playerMove.SyncRotationWithCurrentCamera();

            // MỞ KHÓA TRẢ LẠI QUYỀN ĐIỀU KHIỂN CHO NGƯỜI CHƠI
            playerMove.isCameraLocked = false;
            playerMove.enabled = true;
        }

        // KÍCH HOẠT BẬT TRUCKTRIGGER LÊN SAU KHI ĐÃ HOÀN THÀNH INTRO
        if (truckTriggerObject != null)
        {
            truckTriggerObject.SetActive(true);
            Debug.Log("[Map02IntroSequence] 🟢 Đã kích hoạt bật TruckTrigger!");
        }

        Debug.Log("[Map02IntroSequence] 🎉 Đã hoàn thành cutscene mở đầu Map 02! Trả lại quyền điều khiển cho Player.");

        // Tắt script/trigger Intro để không bao giờ chạy lại nữa
        gameObject.SetActive(false);
    }
}
