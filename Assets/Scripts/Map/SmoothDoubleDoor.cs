using UnityEngine;

public class SmoothDoubleDoor : MonoBehaviour
{
    [Header("Door Elements")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("UI Interaction Hint")]
    public GameObject doorHintUI; // Cái DoorCanvas (World Space) dính trên cửa

    [Header("Rotation Settings")]
    public float openAngleLeft = -90f;
    public float openAngleRight = 90f;
    public float doorSpeed = 3f;

    private bool isDoorOpen = false;
    private Quaternion closedRotationLeft;
    private Quaternion closedRotationRight;
    private Quaternion openRotationLeft;
    private Quaternion openRotationRight;

    void Start()
    {
        closedRotationLeft = leftDoor.localRotation;
        closedRotationRight = rightDoor.localRotation;

        openRotationLeft = closedRotationLeft * Quaternion.Euler(0, openAngleLeft, 0);
        openRotationRight = closedRotationRight * Quaternion.Euler(0, openAngleRight, 0);

        if (doorHintUI != null) doorHintUI.SetActive(false);
    }

    void Update()
    {
        // Nội suy xoay cửa mượt mà
        if (isDoorOpen)
        {
            leftDoor.localRotation = Quaternion.Slerp(leftDoor.localRotation, openRotationLeft, Time.deltaTime * doorSpeed);
            rightDoor.localRotation = Quaternion.Slerp(rightDoor.localRotation, openRotationRight, Time.deltaTime * doorSpeed);
        }
        else
        {
            leftDoor.localRotation = Quaternion.Slerp(leftDoor.localRotation, closedRotationLeft, Time.deltaTime * doorSpeed);
            rightDoor.localRotation = Quaternion.Slerp(rightDoor.localRotation, closedRotationRight, Time.deltaTime * doorSpeed);
        }
    }

    // =================================================================
    // BẮT BUỘC PHẢI CÓ 3 HÀM PUBLIC NÀY ĐỂ PLAYERINTERACTION GỌI ĐƯỢC
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