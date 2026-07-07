using UnityEngine;

public class SmoothSlidingDoor : MonoBehaviour
{
    [Header("Door Element")]
    public Transform doorBody; // Kéo phần cánh cửa thực sự dịch chuyển vào đây

    [Header("UI Interaction Hint")]
    public GameObject doorHintUI; // Cái Canvas "Press F" dính trên cửa

    [Header("Sliding Settings")]
    [Tooltip("Khoảng cách và hướng cửa sẽ trượt đi (Ví dụ: X = 2 nghĩa là trượt sang phải 2 mét)")]
    public Vector3 slideDirection = new Vector3(2f, 0f, 0f);
    public float doorSpeed = 3f;

    private bool isDoorOpen = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    void Start()
    {
        // Ghi nhớ vị trí đóng ban đầu của cửa (tọa độ Local để không bị lỗi khi quay Map)
        if (doorBody != null)
        {
            closedPosition = doorBody.localPosition;
            // Vị trí mở bằng vị trí đóng cộng thêm khoảng cách trượt
            openPosition = closedPosition + slideDirection;
        }

        if (doorHintUI != null) doorHintUI.SetActive(false);
    }

    void Update()
    {
        if (doorBody == null) return;

        // Nội suy dịch chuyển vị trí mượt mà (Lerp) dựa trên trạng thái đóng/mở
        if (isDoorOpen)
        {
            doorBody.localPosition = Vector3.Lerp(doorBody.localPosition, openPosition, Time.deltaTime * doorSpeed);
        }
        else
        {
            doorBody.localPosition = Vector3.Lerp(doorBody.localPosition, closedPosition, Time.deltaTime * doorSpeed);
        }
    }

    // =================================================================
    // 3 HÀM CÔNG KHAI ĐỂ CAMERA NHÌN VÀO GỌI ĐƯỢC
    // =================================================================

    public void ToggleDoor()
    {
        isDoorOpen = !isDoorOpen;
    }

    public void ShowPrompt()
    {
        if (doorHintUI != null) doorHintUI.SetActive(true);
    }

    public void HidePrompt()
    {
        if (doorHintUI != null) doorHintUI.SetActive(false);
    }
}