using UnityEngine;
using System.Collections;

public class BoxDropZone : MonoBehaviour, IInteractable
{
    [Header("1. Chữ Nhắc Tương Tác (Prompt UI)")]
    public string englishPrompt = "Drop Box";
    public string vietnamesePrompt = "Đặt thùng hàng xuống";

    [Header("2. Thùng Hàng Trên Tay Player (Cần Kiểm Tra & Ẩn Đi)")]
    [Tooltip("Kéo Object thùng hàng trên tay Player (dưới Main Camera) vào đây")]
    public GameObject playerHandBox;

    [Header("3. Thùng Hàng Thế Giới Sẽ Rơi Xuất Hiện (World Box)")]
    [Tooltip("Kéo Prefab thùng hàng (hoặc 1 Object Thùng Hàng cất sẵn trong Scene) vào đây")]
    public GameObject worldBoxPrefab;

    [Header("4. Cấu Hình Vị Trí, Góc Xoay & Scale Thả Thùng")]
    [Tooltip("Góc xoay Euler khi thả thùng xuống (Mặc định X: -90 để thùng đứng đúng chiều)")]
    public Vector3 spawnRotationEuler = new Vector3(-90f, 0f, 0f);

    [Tooltip("Kích thước Scale của thùng khi thả xuống sàn (Mặc định X, Y, Z: 0.5)")]
    public Vector3 spawnScale = new Vector3(0.5f, 0.5f, 0.5f);

    [Tooltip("Độ cao (Trục Y) thả thùng rơi xuống (Mặc định: 0.5m trên sàn để rơi tự nhiên)")]
    public float dropHeightOffset = 0.5f;

    [Tooltip("Tích chọn: Thả ngay tại điểm tia mắt người chơi nhìn vào thảm. Bỏ tích: Thả cố định tại tâm vùng này")]
    public bool dropAtRaycastPoint = true;

    [Tooltip("Điểm thả cố định tùy chỉnh (Nếu bỏ tích dropAtRaycastPoint)")]
    public Transform fixedDropPoint;

    [Header("5. Khóa Cố Định Vị Trí Thùng Sau Khi Rơi")]
    [Tooltip("Tích chọn để lưu & khóa cố định vị trí thùng hàng sau khi rơi xuống thảm (không bị xô lệch/di chuyển nữa)")]
    public bool lockPositionAfterDrop = true;

    [Tooltip("Thời gian chờ (giây) để thùng rơi tự nhiên chạm thảm trước khi khóa cứng vị trí (Mặc định: 0.8s)")]
    public float lockDelay = 0.8f;

    [Header("6. Âm Thanh Khi Đặt/Thả Thùng (Tùy chọn)")]
    public AudioClip dropSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("7. Hộp Mở Hàng Khi Bấm Mở (Open Box Settings)")]
    [Tooltip("Kéo Prefab hoặc Object 'BoxOpen' (hộp đã mở) vào đây để cho phép bấm [F] mở hộp sau khi thả")]
    public GameObject openBoxPrefab;

    private Collider zoneCollider;
    private InteractPrompt interactPrompt;
    private bool hasDropped = false;

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        interactPrompt = GetComponent<InteractPrompt>();
    }

    void Start()
    {
        // Tự thêm hoặc cấu hình InteractPrompt để hiển thị chữ [F] Đặt thùng hàng xuống
        if (interactPrompt == null)
        {
            interactPrompt = gameObject.AddComponent<InteractPrompt>();
        }
        interactPrompt.englishPrompt = englishPrompt;
        interactPrompt.vietnamesePrompt = vietnamesePrompt;

        // BAN ĐẦU TẮT COLLIDER VÙNG THẢ - CHỈ BẬT KHI PLAYER CẦM THÙNG HÀNG TRÊN TAY
        if (zoneCollider != null)
        {
            bool isCarryingBox = (playerHandBox != null && playerHandBox.activeSelf);
            zoneCollider.enabled = isCarryingBox;
        }
    }

    void Update()
    {
        if (hasDropped) return;

        // TỰ ĐỘNG BẬT/TẮT TƯƠNG TÁC: CHỈ CHO PHÉP HIỆN [F] VÀ BẤM KHI THÙNG HÀNG TRÊN TAY ĐANG ACTIVE
        bool isCarryingBox = (playerHandBox != null && playerHandBox.activeSelf);
        if (zoneCollider != null && zoneCollider.enabled != isCarryingBox)
        {
            zoneCollider.enabled = isCarryingBox;
            if (!isCarryingBox) HidePrompt();
        }
    }

    public void Interact()
    {
        if (hasDropped) return;

        // Chỉ cho phép thả nếu Player ĐANG CẦM THÙNG HÀNG TRÊN TAY
        if (playerHandBox == null || !playerHandBox.activeSelf)
        {
            Debug.Log("[BoxDropZone] ⚠️ Người chơi chưa cầm thùng hàng trên tay, không thể thả!");
            return;
        }

        Debug.Log("[BoxDropZone] 📦 Đặt thùng hàng xuống chiếu!");

        // 1. Ẩn thùng hàng trên tay Player
        playerHandBox.SetActive(false);

        // 2. Tính toán vị trí & góc xoay thả thùng (X = -90f)
        Vector3 spawnPosition = transform.position + Vector3.up * dropHeightOffset;
        Quaternion targetRotation = Quaternion.Euler(spawnRotationEuler);

        if (dropAtRaycastPoint && Camera.main != null)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 5f))
            {
                spawnPosition = hit.point + Vector3.up * dropHeightOffset;
            }
        }
        else if (fixedDropPoint != null)
        {
            spawnPosition = fixedDropPoint.position + Vector3.up * dropHeightOffset;
            targetRotation = fixedDropPoint.rotation * Quaternion.Euler(spawnRotationEuler);
        }

        // 3. Tạo hoặc kích hoạt Thùng Hàng Thế Giới (World Box)
        GameObject spawnedBox = null;
        if (worldBoxPrefab != null)
        {
            if (worldBoxPrefab.scene.rootCount > 0)
            {
                // Nếu là Object có sẵn trong Scene
                spawnedBox = worldBoxPrefab;
                spawnedBox.transform.position = spawnPosition;
                spawnedBox.transform.rotation = targetRotation;
                spawnedBox.SetActive(true);
            }
            else
            {
                // Nếu là Prefab từ Project
                spawnedBox = Instantiate(worldBoxPrefab, spawnPosition, targetRotation);
            }
        }

        // 4. Đảm bảo Thùng Hàng có Rigidbody & Collider để RƠI TỰ NHIÊN xuống sàn
        if (spawnedBox != null)
        {
            // Tháo ra khỏi Camera để nằm độc lập trên sàn
            if (spawnedBox.transform.parent != null)
            {
                spawnedBox.transform.SetParent(null);
            }

            // Đặt Scale chuẩn (0.5, 0.5, 0.5)
            spawnedBox.transform.localScale = spawnScale;

            Rigidbody rb = spawnedBox.GetComponent<Rigidbody>();
            if (rb == null) rb = spawnedBox.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;

            // Bật lại đổ bóng cho thùng dưới sàn
            MeshRenderer mr = spawnedBox.GetComponent<MeshRenderer>();
            if (mr != null) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            // 5. KHÓA CỐ ĐỊNH VỊ TRÍ SAU KHỊ THÙNG RƠI XUỐNG THẢM
            if (lockPositionAfterDrop)
            {
                StartCoroutine(LockBoxPositionRoutine(spawnedBox));
            }

            // 6. CẤU HÌNH TƯƠNG TÁC MỞ HỘP (BoxOpen) CHO THÙNG VỪA THẢ
            NPCDeliveryBox oldDelivery = spawnedBox.GetComponent<NPCDeliveryBox>();
            if (oldDelivery != null) oldDelivery.enabled = false;

            InteractableItem oldItem = spawnedBox.GetComponent<InteractableItem>();
            if (oldItem != null) oldItem.enabled = false;

            Collider col = spawnedBox.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            if (openBoxPrefab != null)
            {
                OpenablePlacedBox openable = spawnedBox.GetComponent<OpenablePlacedBox>();
                if (openable == null) openable = spawnedBox.AddComponent<OpenablePlacedBox>();
                openable.openBoxPrefab = openBoxPrefab;
                openable.enabled = true;
            }
        }

        // 6. Phát âm thanh thả thùng (nếu có)
        if (dropSound != null)
        {
            AudioSource.PlayClipAtPoint(dropSound, spawnPosition, soundVolume);
        }

        hasDropped = true;

        // Tắt collider vùng thả để không hiện [F] nữa
        if (zoneCollider != null) zoneCollider.enabled = false;
        HidePrompt();
    }

    IEnumerator LockBoxPositionRoutine(GameObject targetBox)
    {
        yield return new WaitForSeconds(lockDelay);

        if (targetBox != null)
        {
            Rigidbody rb = targetBox.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Khóa hoàn toàn chuyển động vật lý
            }

            Debug.Log("[BoxDropZone] 🔒 Đã lưu & khóa cố định vị trí thùng hàng tại: " + targetBox.transform.position);
        }
    }

    public void ShowPrompt()
    {
        if (hasDropped) return;
        // Chỉ hiện chữ [F] khi đang thực sự cầm thùng hàng trên tay
        if (playerHandBox != null && playerHandBox.activeSelf)
        {
            if (interactPrompt != null) interactPrompt.ShowPrompt();
        }
    }

    public void HidePrompt()
    {
        if (interactPrompt != null) interactPrompt.HidePrompt();
    }
}
