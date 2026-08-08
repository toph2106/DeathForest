using UnityEngine;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public enum ItemType { Consumable, Quest, Battery,Paper, Key}

    [Header("Item Configuration")]
    public ItemType itemType; // Chọn loại Item trên Inspector (Consumable, Quest, Battery)
    public string itemNameOrQuestName = "Pin"; // Tên item hoặc Tên Quest

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
        // Hàm tương thích
    }

    public void HidePrompt()
    {
        // Hàm tương thích
    }

    // Tích hợp IInteractable để tự động hiện chữ [F] Tương tác trên màn hình HUD
    public void Interact()
    {
        Pickup();
    }

    public void Pickup()
    {
        if (itemType == ItemType.Consumable)
        {
            if (inventoryManager == null) return;
            bool isPickedUp = inventoryManager.AddConsumableItem(itemNameOrQuestName, gameObject);

            if (!isPickedUp)
            {
                Debug.Log("Túi đồ đầy, không thể nhặt thêm " + itemNameOrQuestName);
            }
        }
        else if (itemType == ItemType.Quest)
        {
            if (inventoryManager == null) return;
            inventoryManager.AddQuestItem(itemNameOrQuestName);
            Destroy(gameObject); // Item Quest nhặt xong là mất luôn nên vẫn Destroy
        }
        else if (itemType == ItemType.Battery)
        {
            // NHẶT SẠC PIN TRỰC TIẾP CHO ĐÈN PIN FLASHLIGHT
            if (FlashlightToggle.Instance != null)
            {
                FlashlightToggle.Instance.RechargeBattery(batteryRechargeAmount);
            }

            Destroy(gameObject); // Cục pin nạp xong tự biến mất
        }
    }
}