using UnityEngine;

/// <summary>
/// Gắn script này vào GameObject 'ZoneDog' (chứa Collider với Is Trigger = true).
/// Quản lý khu vực hoạt động của Chó Săn (Dog Chase Area):
/// 1. Khi Player bước vào vùng -> Đánh thức Chó sủa & bắt đầu săn đuổi người chơi.
/// 2. Khi Player chạy thoát ra khỏi vùng -> Chó dừng săn và tự động chạy về vị trí ban đầu.
/// </summary>
public class DogDangerZone : MonoBehaviour
{
    [Header("1. Tham Chiếu Chó Săn (Dog Enemy)")]
    [Tooltip("Kéo con Chó (Dog) trong Scene vào đây. Nếu để trống code sẽ tự tìm!")]
    public DogChaseBehavior assignedDog;

    void Awake()
    {
        if (assignedDog == null)
        {
            assignedDog = Object.FindFirstObjectByType<DogChaseBehavior>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            if (assignedDog != null)
            {
                assignedDog.OnPlayerEnteredZone(other.transform);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            if (assignedDog != null)
            {
                assignedDog.OnPlayerExitedZone();
            }
        }
    }

    private bool IsPlayer(Collider col)
    {
        return col.CompareTag("Player") 
            || col.GetComponent<MovePl>() != null 
            || col.GetComponentInParent<MovePl>() != null
            || col.name.ToLower().Contains("player") 
            || col.name.ToLower().Contains("camera");
    }
}
