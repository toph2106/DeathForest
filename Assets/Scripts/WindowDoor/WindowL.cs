using UnityEngine;

public class WindowL : MonoBehaviour, IInteractable
{
    public float slideDistance = 0.5f;
    public float slideSpeed = 2f;

    [Tooltip("Tích chọn nếu muốn cửa sổ MẶC ĐỊNH MỞ NGAY KHI VÀO GAME")]
    public bool startOpened = false;

    [Header("Mở Khóa Case PC Sau Khi Đóng Cửa Sổ")]
    [Tooltip("Kéo Collider của Case PC vào đây để mở khóa tương tác khi đóng cửa sổ")]
    public Collider caseColliderToEnable;
    public GameObject caseObjectToEnable;

    private Vector3 closedPos;
    private Vector3 openPos;
    private Vector3 targetPos;
    private bool isOpen = false;

    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos - (transform.right * slideDistance);

        isOpen = startOpened;
        targetPos = startOpened ? openPos : closedPos;

        if (startOpened)
        {
            transform.position = openPos;
        }
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * slideSpeed);
    }

    public void Interact()
    {
        isOpen = !isOpen;
        targetPos = isOpen ? openPos : closedPos;

        if (!isOpen)
        {
            if (caseColliderToEnable != null) caseColliderToEnable.enabled = true;
            if (caseObjectToEnable != null) caseObjectToEnable.SetActive(true);

            PCPowerButton pcPower = Object.FindFirstObjectByType<PCPowerButton>();
            if (pcPower != null) pcPower.UnlockCase();

            Debug.Log("[WindowL] 🔓 Đã đóng cửa sổ! Mở khóa tương tác với Case PC.");
        }
    }
}