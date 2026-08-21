using UnityEngine;
using TMPro;

public class AdvancedDoorInteraction : MonoBehaviour
{
    [Header("Cấu hình Raycast")]
    public float interactDistance = 3f;
    public LayerMask doorLayer;

    [Header("UI Gợi ý Tương Tác")]
    public GameObject interactionUI;
    public TextMeshProUGUI interactText;

    private AdvancedDoor currentDoor;

    void Update()
    {
        // NẾU BẢNG NHẬP MÃ SỐ ĐANG MỞ -> TỰ ĐỘNG ẨN CHỮ VÀ DỪNG QUÉT
        if (DoorPasscodeUI.Instance != null && DoorPasscodeUI.Instance.uiPanel != null && DoorPasscodeUI.Instance.uiPanel.activeSelf)
        {
            if (currentDoor != null)
            {
                currentDoor = null;
                HideUI();
            }
            return;
        }

        CheckRaycast();

        // TƯƠNG TÁC BẰNG CLICK CHUỘT TRÁI (HOẶC PHÍM F DỰ PHÒNG)
        if (currentDoor != null && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F)))
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
                    ShowUI(currentDoor.isLocked ? "Kiểm tra cửa" : "Mở cửa");
                }
                return;
            }
        }

        if (currentDoor != null)
        {
            currentDoor = null;
            HideUI();
        }
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