using UnityEngine;

public class SmoothDoubleDoor : MonoBehaviour
{
    [Header("Door Elements")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("UI Interaction Hint")]
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

    void Start()
    {
        closedRotationLeft = leftDoor.localRotation;
        closedRotationRight = rightDoor.localRotation;

        openRotationLeft = closedRotationLeft * Quaternion.Euler(0, openAngleLeft, 0);
        openRotationRight = closedRotationRight * Quaternion.Euler(0, openAngleRight, 0);

        if (doorHintUI != null) doorHintUI.SetActive(false);

        if (doorAudioSource == null) doorAudioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
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

    public void ToggleDoor()
    {
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