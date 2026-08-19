using UnityEngine;
using TMPro;
using System.Collections;

public class AdvancedDoorInteraction : MonoBehaviour
{
    public static AdvancedDoorInteraction Instance;

    [Header("Cấu hình Raycast")]
    public float interactDistance = 3f;
    public LayerMask doorLayer;

    [Header("UI Gợi ý [F]")]
    public GameObject interactionUI;
    public TextMeshProUGUI interactText;

    private AdvancedDoor currentDoor;
    private Coroutine tempMessageCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // NẾU BẢNG NHẬP MÃ SỐ ĐANG MỞ -> TỰ ĐỘNG ẨN CHỮ [F] VÀ DỪNG QUÉT
        if (DoorPasscodeUI.Instance != null && DoorPasscodeUI.Instance.uiPanel != null && DoorPasscodeUI.Instance.uiPanel.activeSelf)
        {
            if (currentDoor != null)
            {
                currentDoor = null;
                HideUI();
            }
            return;
        }

        // Nếu đang hiện thông báo tạm thời thì bỏ qua việc quét Raycast bình thường
        if (tempMessageCoroutine != null) return;

        CheckRaycast();

        if (currentDoor != null && Input.GetKeyDown(KeyCode.F))
        {
            // Chỉ cần gọi hàm Interact, không cần truyền GameObject vào nữa
            currentDoor.Interact();
        }
    }

    void CheckRaycast()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, doorLayer))
        {
            AdvancedDoor door = hit.collider.GetComponentInParent<AdvancedDoor>();

            if (door != null)
            {
                if (currentDoor != door)
                {
                    currentDoor = door;
                    // Anh comment lại dòng cũ của em (không xóa đi nhé)
                    // ShowUI(currentDoor.isLocked ? "[F] Kiểm tra cửa" : "[F] Mở cửa");
                }

                // ================= BẮT ĐẦU THÊM ĐOẠN NÀY =================
                // Quét liên tục xem cửa đang đóng hay mở để đổi chữ ngay lập tức
                if (currentDoor.isOpen)
                {
                    ShowUI("[F] Đóng cửa");
                }
                else
                {
                    ShowUI(currentDoor.isLocked ? "[F] Kiểm tra cửa" : "[F] Mở cửa");
                }
                // ================= KẾT THÚC THÊM =========================

                return;
            }
            InteractableItem item = hit.collider.GetComponent<InteractableItem>();
            if (item != null)
            {
                ShowUI("[F] Nhặt " + item.itemNameOrQuestName);
                if (Input.GetKeyDown(KeyCode.F))
                {
                    item.Interact();
                    HideUI();
                }   
            }
            else
            {
                HideUI();
            }
            return;
        }

        if (currentDoor != null)
        {
            currentDoor = null;
            HideUI();
        }
    }

    // --- HÀM HIỂN THỊ THÔNG BÁO TẠM THỜI TRONG 2 GIÂY ---
    public void ShowTemporaryMessage(string message, float duration = 2f)
    {
        if (tempMessageCoroutine != null)
        {
            StopCoroutine(tempMessageCoroutine);
        }
        tempMessageCoroutine = StartCoroutine(TempMessageRoutine(message, duration));
    }

    private IEnumerator TempMessageRoutine(string message, float duration)
    {
        if (interactionUI != null) interactionUI.SetActive(true);
        if (interactText != null) interactText.text = message;

        yield return new WaitForSeconds(duration);

        HideUI();
        tempMessageCoroutine = null;
    }

    private void ShowUI(string message)
    {
        if (interactionUI != null) interactionUI.SetActive(true);
        if (interactText != null) interactText.text = message;
    }

    private void HideUI()
    {
        if (interactionUI != null) interactionUI.SetActive(false);
    }
}   