using UnityEngine;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public enum ItemType { Consumable, Quest, Battery, Paper, Key }

    [Header("Item Configuration")]
    public ItemType itemType; // Chọn loại Item trên Inspector
    public string itemNameOrQuestName = "Pin"; // Tên item hoặc Tên Quest / Chìa khóa

    [Header("UI Icon & 3D Model")]
    [Tooltip("Kéo Sprite hình icon của vật phẩm (nếu dùng 2D)")]
    public Sprite itemIcon;
    [Tooltip("Kéo Prefab 3D của vật phẩm (Nếu để trống sẽ tự lấy chính GameObject này)")]
    public GameObject item3DPrefab;

    [Header("Battery Settings (Nếu chọn ItemType = Battery)")]
    [Tooltip("Nếu tích chọn: Pin sẽ vào Kho đồ / Túi đồ Hotbar để dành bấm phím R nạp khi cần. Bỏ tích: Nạp thẳng vào đèn pin ngay lập tức.")]
    public bool addToInventory = true;

    [Tooltip("Lượng % Pin được nạp khi dùng trực tiếp (Mặc định: +50%)")]
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

        GameObject sourceObj = (item3DPrefab != null) ? item3DPrefab : gameObject;

        if (itemType == ItemType.Consumable || itemType == ItemType.Key || itemType == ItemType.Battery)
        {
            // Consumable, Key và Battery đều được đưa trực tiếp vào ô Hotbar của InventoryManager
            bool isPickedUp = inventoryManager.AddConsumableItem(itemNameOrQuestName, sourceObj, itemIcon);

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
        else if (itemType == ItemType.Paper)
        {
            Debug.Log("Đã đọc tài liệu: " + itemNameOrQuestName);
            Destroy(gameObject);
        }
    }
}