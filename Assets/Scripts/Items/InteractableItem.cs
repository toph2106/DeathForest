using UnityEngine;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public enum ItemType { Consumable, Quest, Battery, Paper, Key }

    [Header("Item Configuration")]
    public ItemType itemType; // Chọn loại Item trên Inspector
    public string itemNameOrQuestName = "Pin"; // Tên item hoặc Tên Quest / Chìa khóa

    [Header("Battery Settings (Nếu chọn ItemType = Battery)")]
    [Tooltip("Lượng % Pin được nạp khi nhặt Cục Pin này (Mặc định: +50%)")]
    public float batteryRechargeAmount = 50f;

    private InventoryManager inventoryManager;

    void Start()
    {
        // Tự động tìm InventoryManager trong Map
        inventoryManager = Object.FindFirstObjectByType<InventoryManager>();
    }

    public void ShowPrompt()
    {
        // Hàm tương thích hiển thị UI [F] Tương tác
    }

    public void HidePrompt()
    {
        // Hàm tương thích ẩn UI [F] Tương tác
    }

    // Tích hợp IInteractable để tự động nhận diện phím F
    public void Interact()
    {
        Pickup();
    }

    public void Pickup()
    {
        if (inventoryManager == null)
        {
            inventoryManager = Object.FindFirstObjectByType<InventoryManager>();
            if (inventoryManager == null) return;
        }

        if (itemType == ItemType.Consumable || itemType == ItemType.Key)
        {
            // Cả Consumable và Key đều được đưa vào ô Hotbar của InventoryManager
            bool isPickedUp = inventoryManager.AddConsumableItem(itemNameOrQuestName, gameObject);

            if (!isPickedUp)
            {
                Debug.Log("Túi đồ đầy, không thể nhặt thêm " + itemNameOrQuestName);
            }
        }
        else if (itemType == ItemType.Quest)
        {
            inventoryManager.AddQuestItem(itemNameOrQuestName);
            Destroy(gameObject); // Item Quest nhặt xong là mất luôn
        }
        else if (itemType == ItemType.Battery)
        {
            // Nhặt sạc pin trực tiếp cho đèn pin Flashlight
            if (FlashlightToggle.Instance != null)
            {
                FlashlightToggle.Instance.RechargeBattery(batteryRechargeAmount);
            }
            Destroy(gameObject); // Cục pin nạp xong tự biến mất
        }
        else if (itemType == ItemType.Paper)
        {
            // Xử lý tạm thời cho Giấy/Tài liệu (Sếp có thể mở UI đọc tài liệu ở đây nếu muốn)
            Debug.Log("Đã đọc tài liệu: " + itemNameOrQuestName);
            Destroy(gameObject);
        }
    }
}