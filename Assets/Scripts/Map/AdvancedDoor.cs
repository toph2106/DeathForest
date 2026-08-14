using UnityEngine;
using System.Collections;

public class AdvancedDoor : MonoBehaviour
{
    public enum DoorOpenType { SingleHinge, DoubleHinge, Sliding }
    public enum DoorLockType { Unlocked, KeyItem, Passcode }

    [Header("1. LOẠI CỬA & LOẠI KHÓA")]
    public DoorOpenType doorType = DoorOpenType.SingleHinge;
    public DoorLockType lockType = DoorLockType.Unlocked;

    [Header("2. CẤU HÌNH CHÌA KHÓA (TÊN TRONG INVENTORY)")]
    [Tooltip("Tên vật phẩm chìa khóa trong túi đồ (VD: Key_PhongKham, Key, Chìa khóa...)")]
    public string requiredKeyName = "Key_PhongKham";
    [Tooltip("Có trừ/xóa chìa khóa khỏi túi đồ sau khi mở cửa thành công không?")]
    public bool removeKeyOnUse = true;

    [Header("3. CẤU HÌNH CÁNH CỬA")]
    public Transform doorLeft;
    public Transform doorRight; // Dùng cho cửa 2 cánh hoặc trượt 2 bên
    public float openSpeed = 2f;

    [Header("4. GÓC XOAY & VỊ TRÍ MỞ (TỰ DO X-Y-Z)")]
    public Vector3 openRotationLeft = new Vector3(0f, 90f, 0f);
    public Vector3 openRotationRight = new Vector3(0f, -90f, 0f);
    public Vector3 slideOffsetLeft = new Vector3(1.5f, 0f, 0f);
    public Vector3 slideOffsetRight = new Vector3(-1.5f, 0f, 0f);

    [Header("5. MẬT MÃ KHÓA SỐ")]
    public string correctPasscode = "1234";

    [Header("6. ÂM THANH")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
    public AudioClip unlockedSound;

    [HideInInspector] public bool isOpen = false;
    [HideInInspector] public bool isLocked = false;
    private bool isMoving = false;

    private Quaternion closedRotLeft, closedRotRight;
    private Vector3 closedPosLeft, closedPosRight;
    private Coroutine moveCoroutine;

    void Start()
    {
        isLocked = (lockType != DoorLockType.Unlocked);

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (doorLeft != null)
        {
            closedRotLeft = doorLeft.localRotation;
            closedPosLeft = doorLeft.localPosition;
        }
        if (doorRight != null)
        {
            closedRotRight = doorRight.localRotation;
            closedPosRight = doorRight.localPosition;
        }
    }

    // Hàm nhận tín hiệu từ AdvancedDoorInteraction khi người chơi bấm F
    public void Interact()
    {
        if (isMoving) return;

        // 1. CỬA KHÔNG KHÓA -> MỞ / ĐÓNG
        if (!isLocked)
        {
            ToggleDoor();
            return;
        }

        // 2. CỬA KHÓA SỐ -> HIỆN UI
        if (lockType == DoorLockType.Passcode)
        {
            PlaySound(lockedSound);
            Debug.Log("Đang cố gắng mở UI Passcode..."); // Thêm dòng này

            if (DoorPasscodeUI.Instance != null)
            {
                DoorPasscodeUI.Instance.OpenUI(this);
            }
            else
            {
                Debug.LogError("LỖI: Chưa tìm thấy DoorPasscodeUI.Instance! Kiểm tra xem đã kéo script vào chưa.");
            }
            return;
        }

        // 3. CỬA KHÓA CHÌA -> KIỂM TRA INVENTORY CỦA SẾP
        // Trong phần kiểm tra chìa khóa của AdvancedDoor.cs
        if (lockType == DoorLockType.KeyItem)
        {
            InventoryManager inventory = FindAnyObjectByType<InventoryManager>();

            if (inventory != null && inventory.HasItem(requiredKeyName))
            {
                if (removeKeyOnUse)
                {
                    inventory.RemoveItem(requiredKeyName); // Tự xóa chìa sau khi mở
                }
                UnlockDoor();
            }
            else
            {
                PlaySound(lockedSound);
                Debug.Log($"Cửa khóa! Cần vật phẩm '{requiredKeyName}' trong túi.");
            }
        }
    }

    // Hàm hỗ trợ quét qua mảng item trong InventoryManager của sếp
    private bool CheckInventoryForKey(InventoryManager inv, string keyName)
    {
        // Truy xuất lén qua Reflection hoặc gọi kiểm tra chuỗi tương tự logic Pin của sếp
        // Vì mảng heldItems trong InventoryManager đang để private, ta có thể dùng cách kiểm tra item đặc thù hoặc chỉnh heldItems thành public trong InventoryManager.
        // NHANH NHẤT: Sếp chỉ cần đổi từ "private string[] heldItems;" thành "public string[] heldItems;" trong InventoryManager.cs!

        // Dưới đây là code tự động check khi heldItems được set thành public:
        /* 
        // Code mẫu nếu heldItems là public:
        for (int i = 0; i < inv.slotTransforms.Length; i++)
        {
            // Kiểm tra theo tên hoặc từ khóa chứa
            // ...
        }
        */

        // Tạm thời nếu dùng chung cơ chế với Pin hoặc các item khác:
        return false; // Đọc lưu ý bên dưới để hoàn thiện tuyệt đối nhé sếp!
    }

    private void RemoveKeyFromInventory(InventoryManager inv, string keyName)
    {
        // Xóa item tương ứng trong túi đồ
    }

    public void UnlockDoor()
    {
        isLocked = false;
        PlaySound(unlockedSound);
        ToggleDoor();
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        PlaySound(isOpen ? openSound : closeSound);

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(AnimateDoor(isOpen));
    }

    private IEnumerator AnimateDoor(bool opening)
    {
        isMoving = true;
        float elapsed = 0f;

        Quaternion startRotL = doorLeft != null ? doorLeft.localRotation : Quaternion.identity;
        Quaternion startRotR = doorRight != null ? doorRight.localRotation : Quaternion.identity;
        Vector3 startPosL = doorLeft != null ? doorLeft.localPosition : Vector3.zero;
        Vector3 startPosR = doorRight != null ? doorRight.localPosition : Vector3.zero;

        Quaternion targetRotL = opening ? closedRotLeft * Quaternion.Euler(openRotationLeft) : closedRotLeft;
        Quaternion targetRotR = opening ? closedRotRight * Quaternion.Euler(openRotationRight) : closedRotRight;
        Vector3 targetPosL = opening ? closedPosLeft + slideOffsetLeft : closedPosLeft;
        Vector3 targetPosR = opening ? closedPosRight + slideOffsetRight : closedPosRight;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openSpeed;
            float t = Mathf.SmoothStep(0, 1, elapsed);

            if (doorType == DoorOpenType.SingleHinge || doorType == DoorOpenType.DoubleHinge)
            {
                if (doorLeft != null) doorLeft.localRotation = Quaternion.Slerp(startRotL, targetRotL, t);
                if (doorType == DoorOpenType.DoubleHinge && doorRight != null)
                {
                    doorRight.localRotation = Quaternion.Slerp(startRotR, targetRotR, t);
                }
            }
            else if (doorType == DoorOpenType.Sliding)
            {
                if (doorLeft != null) doorLeft.localPosition = Vector3.Lerp(startPosL, targetPosL, t);
                if (doorRight != null) doorRight.localPosition = Vector3.Lerp(startPosR, targetPosR, t);
            }

            yield return null;
        }

        isMoving = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }
}