using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3f; // Khoảng cách tối đa để bấm được cửa (Sếp muốn ở gần mới được)
    public LayerMask interactableLayer; // Lớp (Layer) dành riêng cho các vật thể tương tác để tối ưu hiệu năng

    private SmoothDoubleDoor currentDoor = null;

    void Update()
    {
        // Tạo một tia Ray đi từ chính giữa màn hình Camera ra phía trước
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Bắn tia Ray ra không gian
        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            // Kiểm tra xem vật thể nhìn trúng có chứa script điều khiển cửa không
            SmoothDoubleDoor door = hit.collider.GetComponentInParent<SmoothDoubleDoor>();

            if (door != null)
            {
                // Nếu nhìn trúng cửa mới hoàn toàn
                if (currentDoor != door)
                {
                    if (currentDoor != null) currentDoor.HidePrompt(); // Ẩn chữ cửa cũ (nếu có)
                    currentDoor = door;
                    currentDoor.ShowPrompt(); // Hiện chữ Press F của cửa đang nhìn vào
                }

                // Nếu người chơi nhấn phím F khi đang lia camera vào cửa
                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentDoor.ToggleDoor();
                }
            }
            else
            {
                ClearCurrentDoor();
            }
        }
        else
        {
            ClearCurrentDoor();
        }
    }

    void ClearCurrentDoor()
    {
        if (currentDoor != null)
        {
            currentDoor.HidePrompt();
            currentDoor = null;
        }
    }
}