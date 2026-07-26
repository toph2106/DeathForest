using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class CrouchInteractable : MonoBehaviour, IInteractable
{
    [Header("1. Chiều Cao Ngồi & Đứng")]
    [Tooltip("Chiều cao camera ban đầu khi đứng (Mặc định: 0.6f)")]
    public float standingY = 0.6f;

    [Tooltip("Chiều cao camera khi cúi/ngồi xuống (Mặc định: 0.1f)")]
    public float crouchY = 0.1f;

    [Header("2. Tốc Độ & Độ Trễ Tương Tác")]
    [Tooltip("Tốc độ hạ người & đứng dậy (Mặc định: 5.0f)")]
    public float moveSpeed = 5.0f;

    [Tooltip("Thời gian ngồi ở vị trí 0.1f RỒI MỚI KÍCH HOẠT NHẶT ĐỒ (Mặc định: 1.5 giây)")]
    public float delayBeforeAction = 1.5f;

    [Tooltip("Thời gian khựng lại sau khi nhặt đồ RỒI MỚI ĐỨNG DẬY (Mặc định: 0.5 giây)")]
    public float delayAfterAction = 0.5f;

    [Header("3. Kích Hoạt Tương Tác / Nhặt Đồ Sau Khi Cúi Đầy Đủ")]
    [Tooltip("Kéo script nhặt đồ (VD: CameraObjectPickup) vào đây để nó ĐỢI CÚI XONG NƠI MỚI NHẶT!")]
    public MonoBehaviour targetInteractableScript;

    [Tooltip("Hoặc thêm các sự kiện UnityEvent chạy sau khi cúi xong (Tùy chọn)")]
    public UnityEvent onCrouchedAction;

    [Tooltip("Tự động xóa vật thể sau khi cúi nhặt xong (Tùy chọn)")]
    public bool destroyAfterPickup = false;

    [Header("4. Ghim Góc Nhìn Camera Vào Điểm Chỉ Định (Pin Camera Target)")]
    [Tooltip("Tự động hướng góc nhìn Camera ghim chặt vào điểm này khi cúi xuống")]
    public bool pinCameraToItem = true;
    [Tooltip("Kéo 1 GameObject/Empty Object làm điểm nhìn ghim camera vào đây (Tùy chọn - Nếu trống code tự ghim vào vị trí hiện tại)")]
    public Transform customPinTarget;
    [Tooltip("Tốc độ ghim xoay góc nhìn Camera hướng về món đồ")]
    public float lookAtSpeed = 6.0f;

    private Transform mainCameraTransform;
    private bool isCrouching = false;
    private MovePl playerMovePl;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
            playerMovePl = mainCameraTransform.GetComponentInParent<MovePl>();
        }
    }

    public void Interact()
    {
        if (isCrouching) return;
        StartCoroutine(CrouchRoutine());
    }

    IEnumerator CrouchRoutine()
    {
        isCrouching = true;

        if (mainCameraTransform == null && Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        if (playerMovePl == null && mainCameraTransform != null)
        {
            playerMovePl = mainCameraTransform.GetComponentInParent<MovePl>();
        }

        // LƯU TRỮ VỊ TRÍ MỤC TIÊU CỐ ĐỊNH TRƯỚC KHI VẬT THỂ BỊ XÓA/ẨN
        Vector3 targetWorldPos = (customPinTarget != null) ? customPinTarget.position : transform.position;

        // 0. KHÓA XOAY CHUỘT CỦA PLAYER KHI BẮT ĐẦU CÚI NGUỜI
        if (playerMovePl != null)
        {
            playerMovePl.enabled = false;
        }

        if (mainCameraTransform != null)
        {
            Vector3 startPos = mainCameraTransform.localPosition;
            Vector3 targetCrouchPos = new Vector3(startPos.x, crouchY, startPos.z);

            // BƯỚC 1: Hạ mượt mà camera local Y từ 0.6f xuống 0.1f KÈM GHIM GÓC NHÌN VÀO ĐIỂM CHỈ ĐỊNH
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                mainCameraTransform.localPosition = Vector3.Lerp(startPos, targetCrouchPos, t);

                if (pinCameraToItem)
                {
                    Vector3 lookDir = targetWorldPos - mainCameraTransform.position;
                    if (lookDir != Vector3.zero)
                    {
                        Quaternion targetLookRot = Quaternion.LookRotation(lookDir);
                        mainCameraTransform.rotation = Quaternion.Slerp(mainCameraTransform.rotation, targetLookRot, Time.deltaTime * lookAtSpeed);
                    }
                }

                yield return null;
            }
            mainCameraTransform.localPosition = targetCrouchPos;

            // BƯỚC 2: Ngồi yên ở vị trí 0.1f trong thời gian delayBeforeAction KÈM GIỮ GHIM GÓC NHÌN
            float elapsed = 0f;
            while (elapsed < delayBeforeAction)
            {
                elapsed += Time.deltaTime;
                if (pinCameraToItem)
                {
                    Vector3 lookDir = targetWorldPos - mainCameraTransform.position;
                    if (lookDir != Vector3.zero)
                    {
                        Quaternion targetLookRot = Quaternion.LookRotation(lookDir);
                        mainCameraTransform.rotation = Quaternion.Slerp(mainCameraTransform.rotation, targetLookRot, Time.deltaTime * lookAtSpeed);
                    }
                }
                yield return null;
            }

            // BƯỚC 3: SAU KHI CÚI XONG MỚI KÍCH HOẠT NHẶT ĐỒ / TƯƠNG TÁC!
            if (targetInteractableScript != null)
            {
                if (targetInteractableScript is CameraObjectPickup cameraPickup)
                {
                    cameraPickup.Pickup();
                }
                else if (targetInteractableScript is IInteractable interactable)
                {
                    interactable.Interact();
                }
            }

            onCrouchedAction?.Invoke();

            // BƯỚC 4: Khựng lại 0.5s sau khi nhặt đồ
            if (delayAfterAction > 0f)
            {
                yield return new WaitForSeconds(delayAfterAction);
            }

            // BƯỚC 5: NÂNG MƯỢT MÀ CAMERA VỀ CHIỀU CAO ĐỨNG 0.6f
            Vector3 currentPos = mainCameraTransform.localPosition;
            Vector3 targetStandPos = new Vector3(currentPos.x, standingY, currentPos.z);

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                mainCameraTransform.localPosition = Vector3.Lerp(currentPos, targetStandPos, t);
                yield return null;
            }
            mainCameraTransform.localPosition = targetStandPos;
        }

        // MỞ KHÓA XOAY CHUỘT CHO PLAYER ĐỨNG DẬY AN TOÀN TRƯỚC KHI XÓA OBJECT
        if (playerMovePl != null)
        {
            playerMovePl.enabled = true;
        }

        isCrouching = false;

        if (destroyAfterPickup)
        {
            Destroy(gameObject);
        }
    }
}
