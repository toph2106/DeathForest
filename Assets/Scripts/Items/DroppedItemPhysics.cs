using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Script gắn vào vật phẩm khi ném ra sàn:
/// - Gán layer "DroppedItem" và tắt va chạm với Player qua Physics Layer Matrix & Physics.IgnoreCollision.
/// - Kích hoạt Rigidbody rơi tự do theo trọng lực.
/// - Khi chạm mặt sàn (OnCollisionEnter) → Tự động khóa cố định vị trí (isKinematic = true).
/// - Hỗ trợ tái sử dụng (reuse) vô số lần: Ném → Nhặt → Ném lại liên tục mà không bị lỗi đơ/bay.
/// </summary>
public class DroppedItemPhysics : MonoBehaviour
{
    private Rigidbody rb;
    private bool hasLanded = false;
    private float timeoutTimer = 0f;
    private float maxFallTime = 2.5f;
    private bool launched = false;

    // Layer indices (phải khớp với TagManager.asset)
    private const int LAYER_PLAYER = 7;         // Layer "Player"
    private const int LAYER_DROPPED_ITEM = 15;  // Layer "DroppedItem"

    // Lưu lại trạng thái isTrigger gốc để khôi phục sau khi tiếp đất
    private Dictionary<Collider, bool> originalTriggerStates = new Dictionary<Collider, bool>();

    public void LaunchDrop(Transform playerTransform)
    {
        enabled = true;
        launched = true;
        hasLanded = false;
        timeoutTimer = 0f;
        originalTriggerStates.Clear();

        // === 1. GÁN LAYER "DroppedItem" VÀ TẮT HOÀN TOÀN VA CHẠM VỚI PLAYER ===
        int droppedLayer = LayerMask.NameToLayer("DroppedItem");
        if (droppedLayer < 0) droppedLayer = LAYER_DROPPED_ITEM;

        SetLayerRecursive(gameObject, droppedLayer);

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0) playerLayer = LAYER_PLAYER;
        Physics.IgnoreLayerCollision(droppedLayer, playerLayer, true);

        // Bỏ qua trực tiếp va chạm với tất cả Colliders và CharacterController của Player
        Collider[] myCols = GetComponentsInChildren<Collider>();
        if (playerTransform != null)
        {
            Transform pRoot = playerTransform.root;
            Collider[] playerCols = pRoot.GetComponentsInChildren<Collider>();
            CharacterController playerCC = pRoot.GetComponentInChildren<CharacterController>();

            foreach (var myC in myCols)
            {
                if (myC == null) continue;
                foreach (var pC in playerCols)
                {
                    if (pC != null) Physics.IgnoreCollision(myC, pC, true);
                }
                if (playerCC != null)
                {
                    Physics.IgnoreCollision(myC, playerCC, true);
                }
            }
        }

        // === 2. LƯU LẠI IS TRIGGER GỐC VÀ TẠM TẮT ĐỂ TIẾP ĐẤT VẬT LÝ ===
        foreach (var c in myCols)
        {
            if (c != null)
            {
                originalTriggerStates[c] = c.isTrigger;
                c.isTrigger = false; // Tạm tắt trong lúc rơi để Rigidbody chạm sàn
            }
        }

        // === 3. CẤU HÌNH RIGIDBODY (TÁI SỬ DỤNG HOẶC TẠO MỚI) ===
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = 2f;
        rb.linearDamping = 0.3f;
        rb.angularDamping = 1.2f;
        rb.constraints = RigidbodyConstraints.None; // Cho phép lộn và đổ nằm bẹp xuống sàn
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp(); // Đánh thức PhysX simulation

        // === 4. NÉM RA PHÍA TRƯỚC VÀ RƠI XUỐNG ===
        if (playerTransform != null)
        {
            Vector3 throwDir = playerTransform.forward;
            throwDir.y = 0f;
            throwDir.Normalize();
            rb.linearVelocity = throwDir * 4.5f + Vector3.down * 3.5f;
        }
        else
        {
            rb.linearVelocity = Vector3.down * 5f;
        }

        // Tạo lực xoay mạnh để vật phẩm lộn và đổ nằm bẹp hoàn toàn xuống mặt sàn
        rb.angularVelocity = new Vector3(
            Random.Range(4f, 8f),
            Random.Range(-3f, 3f),
            Random.Range(-3f, 3f)
        );

        StopAllCoroutines();
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    void Update()
    {
        if (!launched || hasLanded) return;

        timeoutTimer += Time.deltaTime;
        if (timeoutTimer >= maxFallTime)
        {
            FreezeAndCleanup();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!launched || hasLanded) return;
        if (collision.gameObject.CompareTag("Player")) return;
        if (collision.gameObject.GetComponent<MovePl>() != null) return;

        // Chạm sàn → chờ 0.6s để item lộn và nằm bẹp hoàn toàn xuống sàn trước khi khóa
        StartCoroutine(DelayedFreeze(0.6f));
    }

    private IEnumerator DelayedFreeze(float delay)
    {
        yield return new WaitForSeconds(delay);
        FreezeAndCleanup();
    }

    private void FreezeAndCleanup()
    {
        if (hasLanded) return;
        hasLanded = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Khôi phục lại trạng thái Is Trigger gốc mà bạn đã setup ngoài Inspector
        foreach (var pair in originalTriggerStates)
        {
            if (pair.Key != null)
            {
                pair.Key.isTrigger = pair.Value;
            }
        }

        // Trả lại layer Default để raycast tương tác nhặt lại hoạt động bình thường
        SetLayerRecursive(gameObject, 0); // Layer 0 = Default

        // Đảm bảo InteractableItem hoạt động để nhặt lại
        InteractableItem item = GetComponent<InteractableItem>();
        if (item != null)
        {
            item.ResetPickupState();
            item.enabled = true;
        }

        Debug.Log($"[DroppedItemPhysics] ✅ '{gameObject.name}' đã tiếp đất và khóa cố định vị trí!");

        // Vô hiệu hóa script thay vì Destroy để sẵn sàng tái sử dụng cho các lần ném sau
        enabled = false;
    }
}
