using UnityEngine;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("UI Hotbar (5 Ô Tiêu Hao)")]
    public GameObject inventoryPanel;
    public Transform[] slotTransforms = new Transform[5];

    [Header("UI Loại 2 - Vật phẩm Quest")]
    public GameObject questProgressTextObject;
    public TextMeshProUGUI questProgressText;
    public int totalQuestItemsNeeded = 3;

    // --- MỚI: Thêm mảng lưu trữ vật thể 3D ---
    private string[] heldItems = new string[5];
    private GameObject[] heldItemObjects = new GameObject[5]; // Lưu vật thể thật để vứt ra

    private int selectedIndex = -1;
    private int currentQuestItemCount = 0;

    private Vector3 normalScale = Vector3.one;
    private Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f);

    void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (questProgressTextObject != null) questProgressTextObject.SetActive(false);

        // Đọc Scale gốc từ ô đầu tiên mà bạn đã chỉnh trong Editor
        if (slotTransforms.Length > 0 && slotTransforms[0] != null)
        {
            normalScale = slotTransforms[0].localScale;
            selectedScale = normalScale * 1.2f; // Phóng to 20% khi được chọn
        }

        for (int i = 0; i < 5; i++)
        {
            heldItems[i] = "";
            heldItemObjects[i] = null; // Khởi tạo ô trống
            slotTransforms[i].localScale = normalScale;
        }
    }

    void Update()
    {
        HandleSelectionInput();
        HandleDropInput(); // Gọi hàm xử lý vứt đồ
    }

    // --- SỬA: Thêm tham số GameObject itemObj vào hàm ---
    public bool AddConsumableItem(string itemName, GameObject itemObj)
    {
        for (int i = 0; i < 5; i++)
        {
            if (string.IsNullOrEmpty(heldItems[i]))
            {
                heldItems[i] = itemName;
                heldItemObjects[i] = itemObj; // Lưu vật thể vào túi

                itemObj.SetActive(false); // Ẩn vật thể khỏi mặt đất thay vì xóa đi

                if (selectedIndex == -1) ToggleSelect(i);

                return true;
            }
        }
        Debug.Log("Túi đồ đã đầy 5 món!");
        return false;
    }

    // --- MỚI: Xử lý vứt đồ (Phím Q) ---
    private void HandleDropInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Kiểm tra xem có đang chọn ô nào không, và ô đó có đồ không
            if (selectedIndex != -1 && heldItemObjects[selectedIndex] != null)
            {
                GameObject itemToDrop = heldItemObjects[selectedIndex];

                // Dịch chuyển vật thể ra phía trước Camera 1.5 mét để không bị kẹt vào người
                Transform camTransform = Camera.main.transform;
                itemToDrop.transform.position = camTransform.position + camTransform.forward * 1.5f;

                // Hiện lại vật thể trên mặt đất
                itemToDrop.SetActive(true);

                // Xóa dữ liệu trong ô UI
                heldItems[selectedIndex] = "";
                heldItemObjects[selectedIndex] = null;
            }
        }
    }
    // Hiệu ứng Bật/Tắt chọn ô
    private void HandleSelectionInput()
    {
        // Chọn bằng phím 1-5
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleSelect(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleSelect(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleSelect(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleSelect(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ToggleSelect(4);

        // Chọn bằng con lăn chuột
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (selectedIndex == -1) ToggleSelect(0);
            else
            {
                int newIndex = selectedIndex;
                if (scroll > 0f) newIndex--; // Cuộn lên -> lùi sang trái
                else newIndex++; // Cuộn xuống -> tiến sang phải

                // Chạy vòng lặp nếu vượt quá giới hạn 0-4
                if (newIndex < 0) newIndex = 4;
                if (newIndex > 4) newIndex = 0;

                ToggleSelect(newIndex);
            }
        }
    }
    private void ToggleSelect(int index)
    {
        // Nếu bấm lại chính ô đang chọn -> Bỏ chọn
        if (selectedIndex == index)
        {
            selectedIndex = -1;
        }
        else
        {
            selectedIndex = index;
        }

        UpdateUISlots();
    }

    // Cập nhật giao diện phóng to/thu nhỏ
    private void UpdateUISlots()
    {
        for (int i = 0; i < 5; i++)
        {
            if (i == selectedIndex)
            {
                slotTransforms[i].localScale = selectedScale;
                // Gợi ý: Bạn có thể đổi màu viền ở đây để nhìn rõ hơn
            }
            else
            {
                slotTransforms[i].localScale = normalScale;
            }
        }
    }

    public void AddQuestItem(string questName)
    {
        currentQuestItemCount++;
        if (questProgressTextObject != null && questProgressText != null)
        {
            questProgressTextObject.SetActive(true);
            questProgressText.text = questName + ": " + currentQuestItemCount + "/" + totalQuestItemsNeeded;
        }
        if (currentQuestItemCount >= totalQuestItemsNeeded)
        {
            questProgressText.text = questName + ": Hoàn thành!";
            
        }
    }
}
