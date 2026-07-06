using UnityEngine;

public class SmoothSingleDoor : MonoBehaviour
{
    [Header("Door Element")]
    public Transform doorBody; // Kéo phần cánh cửa thực sự xoay vào đây

    [Header("UI Interaction Hint")]
    public GameObject doorHintUI; // Cái Canvas "Press F" dính trên cửa

    [Header("Rotation Settings")]
    [Tooltip("Góc xoay khi cửa mở (Ví dụ: 90 là mở ra, -90 là mở vào)")]
    public float openAngle = 90f;
    public float doorSpeed = 3f;

    private bool isDoorOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        // Ghi nhớ góc đóng ban đầu của cửa
        if (doorBody != null)
        {
            closedRotation = doorBody.localRotation;
            // Tính toán góc mở bằng cách nhân thêm góc xoay quanh trục Y
            openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        }

        if (doorHintUI != null) doorHintUI.SetActive(false);
    }

    void Update()
    {
        if (doorBody == null) return;

        // Nội suy xoay cửa mượt mà (Slerp) dựa trên trạng thái đóng/mở
        if (isDoorOpen)
        {
            doorBody.localRotation = Quaternion.Slerp(doorBody.localRotation, openRotation, Time.deltaTime * doorSpeed);
        }
        else
        {
            doorBody.localRotation = Quaternion.Slerp(doorBody.localRotation, closedRotation, Time.deltaTime * doorSpeed);
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