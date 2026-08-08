using UnityEngine;

public class SmoothDoubleDoor : MonoBehaviour, IInteractable
{
    [Header("Door Elements")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("UI Interaction Hint (Tùy chọn)")]
    public GameObject doorHintUI;

    [Header("Rotation Settings")]
    public float openAngleLeft = -90f;
    public float openAngleRight = 90f;
    public float doorSpeed = 3f;

    [Header("Audio Settings")]
    public AudioSource doorAudioSource;
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)] public float volume = 1f;

    private bool isDoorOpen = false;
    private Quaternion closedRotationLeft;
    private Quaternion closedRotationRight;
    private Quaternion openRotationLeft;
    private Quaternion openRotationRight;
    private float lastToggleTime = 0f;

    void Start()
    {
        if (leftDoor != null) closedRotationLeft = leftDoor.localRotation;
        if (rightDoor != null) closedRotationRight = rightDoor.localRotation;

        if (leftDoor != null) openRotationLeft = closedRotationLeft * Quaternion.Euler(0, openAngleLeft, 0);
        if (rightDoor != null) openRotationRight = closedRotationRight * Quaternion.Euler(0, openAngleRight, 0);

        if (doorHintUI != null) doorHintUI.SetActive(false);
        if (doorAudioSource == null) doorAudioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (leftDoor != null)
        {
            Quaternion targetLeft = isDoorOpen ? openRotationLeft : closedRotationLeft;
            leftDoor.localRotation = Quaternion.Slerp(leftDoor.localRotation, targetLeft, Time.deltaTime * doorSpeed);
        }

        if (rightDoor != null)
        {
            Quaternion targetRight = isDoorOpen ? openRotationRight : closedRotationRight;
            rightDoor.localRotation = Quaternion.Slerp(rightDoor.localRotation, targetRight, Time.deltaTime * doorSpeed);
        }
    }

    // TƯƠNG TÁC PHÍM F CHUẨN INTERACTPRO
    public void Interact()
    {
        ToggleDoor();
    }

    public void ToggleDoor()
    {
        // CHỐNG NHẤP ĐÚP LẶP LỆNH TRONG CÙNG 1 FRAME (ANTI-DOUBLE TRIGGER GUARD)
        if (Time.time - lastToggleTime < 0.3f) return;
        lastToggleTime = Time.time;

        isDoorOpen = !isDoorOpen;

        if (doorAudioSource != null)
        {
            if (isDoorOpen && openSound != null)
            {
                doorAudioSource.PlayOneShot(openSound, volume);
            }
            else if (!isDoorOpen && closeSound != null)
            {
                doorAudioSource.PlayOneShot(closeSound, volume);
            }
        }
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