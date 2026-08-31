using UnityEngine;

/// <summary>
/// Script gắn vào Vùng An Toàn (SafeZone):
/// Khi Player bước vào hoặc rời khỏi SafeZone, tự động thông báo cho YoshieBehavior.
/// Đồng thời đóng vai trò như một TƯỜNG RÀO VÔ HÌNH chặn đứng Yoshie ở mép ngoài ranh giới!
/// </summary>
[RequireComponent(typeof(Collider))]
public class YoshieSafeZone : MonoBehaviour
{
    [Header("Tham Chiếu Quái Vật Yoshie (Tùy Chọn)")]
    [Tooltip("Kéo Yoshie vào đây (Để trống sẽ tự động tìm trong Scene)")]
    public YoshieBehavior targetYoshie;

    private Collider myCollider;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        EnsureColliderIsTrigger();
        FindYoshie();
    }

    void Start()
    {
        myCollider = GetComponent<Collider>();
        EnsureColliderIsTrigger();
        FindYoshie();
    }

    void EnsureColliderIsTrigger()
    {
        if (myCollider != null && !myCollider.isTrigger)
        {
            myCollider.isTrigger = true;
        }
    }

    void FindYoshie()
    {
        if (targetYoshie == null)
        {
            targetYoshie = Object.FindFirstObjectByType<YoshieBehavior>();
        }

        if (targetYoshie != null && myCollider != null)
        {
            // Đăng ký ranh giới Collider này với Yoshie
            targetYoshie.SetPlayerInSafeZone(targetYoshie.isPlayerInSafeZone, myCollider);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Nếu là Player bước vào
        if (IsPlayer(other))
        {
            FindYoshie();
            if (targetYoshie != null)
            {
                targetYoshie.SetPlayerInSafeZone(true, myCollider);
                Debug.Log("[YoshieSafeZone] 🟢 Player đã bước vào SafeZone! Yoshie sẽ bị chặn lại ở ranh giới.");
            }
        }
        // 2. Nếu là Yoshie chạm vào ranh giới
        else if (IsYoshie(other))
        {
            FindYoshie();
            if (targetYoshie != null)
            {
                targetYoshie.OnHitSafeZoneBarrier();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            FindYoshie();
            if (targetYoshie != null)
            {
                targetYoshie.SetPlayerInSafeZone(false, myCollider);
                Debug.Log("[YoshieSafeZone] 🔴 Player đã rời khỏi SafeZone! Yoshie sẽ tiếp tục săn đuổi.");
            }
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        return other.CompareTag("Player") || other.GetComponent<MovePl>() != null || other.GetComponentInParent<MovePl>() != null;
    }

    private bool IsYoshie(Collider other)
    {
        if (other == null) return false;
        return other.GetComponent<YoshieBehavior>() != null || other.GetComponentInParent<YoshieBehavior>() != null || (targetYoshie != null && other.gameObject == targetYoshie.gameObject);
    }
}
