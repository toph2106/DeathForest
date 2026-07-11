using UnityEngine;

public class InteractableItem : MonoBehaviour
{
    public enum ItemType { Consumable, Quest }

    [Header("Item Configuration")]
    public ItemType itemType; // Chọn loại Item trên Inspector
    public string itemNameOrQuestName = "Chìa khóa"; // Tên item hoặc Tên Quest

    [Header("UI Hint")]
    public GameObject pressFHintUI; // Canvas "Press F" lơ lửng trên item

    private InventoryManager inventoryManager;

    void Start()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(false);

        // Tự động tìm InventoryManager trong Map
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

    public void Pickup()
    {
        if (inventoryManager == null) return;

        if (itemType == ItemType.Consumable)
        {
            // Truyền thêm gameObject vào để Manager cất giữ
            bool isPickedUp = inventoryManager.AddConsumableItem(itemNameOrQuestName, gameObject);

            // XÓA LỆNH Destroy(gameObject) Ở ĐÂY!
            if (!isPickedUp)
            {
                Debug.Log("Túi đồ đầy, không thể nhặt thêm " + itemNameOrQuestName);
            }
        }
        else if (itemType == ItemType.Quest)
        {
            inventoryManager.AddQuestItem(itemNameOrQuestName);
            Destroy(gameObject); // Item Quest nhặt xong là mất luôn nên vẫn Destroy
        }
    }
}