using UnityEngine;

public class CorpseLoot : MonoBehaviour
{
    public enum LootType { Consumable, Quest }

    [Header("Loot Settings")]
    public LootType lootType;
    public string itemNameOrQuestName = "Chìa khóa gỉ sét";

    [Tooltip("Kéo Prefab cục Cube/Item nhỏ vào đây để làm hình nhân thế mạng khi vứt ra bằng phím Q")]
    public GameObject itemPrefabToGive;

    [Header("UI Hint")]
    public GameObject pressFHintUI; // Cái Canvas "Press F" lơ lửng trên xác

    private InventoryManager inventoryManager;

    void Start()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(false);
        inventoryManager = Object.FindFirstObjectByType<InventoryManager>();
    }

    public void ShowPrompt()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(true);
    }

    public void HidePrompt()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(false);
    }

    public void LootCorpse()
    {
        if (inventoryManager == null) return;

        if (lootType == LootType.Consumable)
        {
            if (itemPrefabToGive == null)
            {
                Debug.LogError("Lỗi: Chưa có Item Prefab để nhét vào túi!");
                return;
            }

            // 1. Sinh ra vật phẩm nhỏ (ẩn) để đưa vào túi
            GameObject spawnedItem = Instantiate(itemPrefabToGive, transform.position, Quaternion.identity);

            // 2. Nhét vào túi
            bool isPickedUp = inventoryManager.AddConsumableItem(itemNameOrQuestName, spawnedItem);

            // 3. Xử lý kết quả
            if (isPickedUp)
            {
                Destroy(gameObject); // Nhặt thành công -> Xóa cái xác
            }
            else
            {
                Destroy(spawnedItem); // Túi đầy -> Hủy vật phẩm ảo đi
                Debug.Log("Túi đầy, không thể nhặt thêm đồ từ xác!");
            }
        }
        else if (lootType == LootType.Quest)
        {
            // Đồ quest thì cộng điểm thẳng và xóa xác luôn
            inventoryManager.AddQuestItem(itemNameOrQuestName);
            Destroy(gameObject);
        }
    }
}