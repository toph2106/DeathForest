using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Tooltip("Tên vật phẩm khớp với 'requiredKeyName' trong AdvancedDoor")]
    public string itemName = "Key_PhongKham";

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem người chơi có va chạm vào không
        if (other.CompareTag("Player"))
        {
            InventoryManager inv = FindAnyObjectByType<InventoryManager>();
            if (inv != null)
            {
                // Thêm vào túi đồ
                if (inv.AddConsumableItem(itemName, this.gameObject))
                {
                    Debug.Log("Đã nhặt: " + itemName);
                    // Không dùng Destroy ở đây vì AddConsumableItem đã setSetActive(false) cho sếp rồi
                }
            }
        }
    }
}