using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gắn script này vào GameObject Cha (ví dụ: 'FeetR', 'ArmsL', 'Head'...).
/// - Bạn chỉ cần đặt và căn chỉnh góc xoay/vị trí của các Item con sẵn trong Scene Editor.
/// - Khi Play game: Script sẽ bốc thăm ngẫu nhiên và CHỈ BẬT ĐÚNG 1 ITEM CON duy nhất, các item con còn lại sẽ tự động bị ẩn/xóa!
/// </summary>
public class RandomChildActivator : MonoBehaviour
{
    [Header("Cấu Hình")]
    [Tooltip("Tự động nhận diện tất cả các GameObject con trực tiếp bên dưới làm danh sách vị trí ngẫu nhiên")]
    public bool autoDetectChildren = true;

    [Tooltip("Danh sách các Item con (nếu không dùng tự động thì kéo thủ công vào đây)")]
    public List<GameObject> candidateItems = new List<GameObject>();

    [Tooltip("Xóa hẳn các Item con không được chọn khỏi game để tối ưu bộ nhớ")]
    public bool destroyUnusedChildren = true;

    void Awake()
    {
        // 1. Tự động lấy danh sách con nếu bật autoDetectChildren
        if (autoDetectChildren)
        {
            candidateItems.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                candidateItems.Add(transform.GetChild(i).gameObject);
            }
        }

        if (candidateItems == null || candidateItems.Count == 0)
        {
            Debug.LogWarning($"[RandomChildActivator] '{gameObject.name}' không có Item con nào để kích hoạt!");
            return;
        }

        // 2. Bốc thăm ngẫu nhiên đúng 1 Item con
        int chosenIndex = Random.Range(0, candidateItems.Count);

        // 3. Kích hoạt Item được chọn và loại bỏ các Item còn lại
        for (int i = 0; i < candidateItems.Count; i++)
        {
            if (candidateItems[i] == null) continue;

            if (i == chosenIndex)
            {
                candidateItems[i].SetActive(true);
                Debug.Log($"[RandomChildActivator] 🎲 '{gameObject.name}' đã kích hoạt vị trí: '{candidateItems[i].name}'");
            }
            else
            {
                if (destroyUnusedChildren)
                {
                    candidateItems[i].SetActive(false);
                    Destroy(candidateItems[i]);
                }
                else
                {
                    candidateItems[i].SetActive(false);
                }
            }
        }
    }
}
