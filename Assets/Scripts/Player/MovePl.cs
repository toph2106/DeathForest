using UnityEngine;
using System.Collections;

public class MovePl : MonoBehaviour
{
    [Header("Movement Settings")]
    public CharacterController controller;
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -19.62f;

    [Header("Look Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 300f;

    [Header("Teleport Settings")]
    public Transform spawnPoint;

    [Header("Cinematic Trigger Settings")]
    public float slowWalkSpeed = 1.5f;
    public float slowSprintSpeed = 1.5f;
    public Transform forcedLookTarget;

    private float xRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;
    private bool canMove = true;
    public bool isSlowed = false;
    public bool isCameraLocked = false;

    void Start()
    {
        LockCursor();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) LockCursor();
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) UnlockCursor();

        if (Cursor.lockState == CursorLockMode.Locked && canMove)
        {
            if (!isCameraLocked)
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                transform.Rotate(Vector3.up * mouseX);
            }
            else if (forcedLookTarget != null)
            {
                Vector3 direction = (forcedLookTarget.position - cameraTransform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, lookRotation.eulerAngles.y, 0f), 5f * Time.deltaTime);

                float targetXRotation = lookRotation.eulerAngles.x;
                if (targetXRotation > 180f) targetXRotation -= 360f;
                xRotation = Mathf.Lerp(xRotation, targetXRotation, 5f * Time.deltaTime);
                cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }
        }

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (canMove)
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            float currentWalk = isSlowed ? slowWalkSpeed : walkSpeed;
            float currentSprint = isSlowed ? slowSprintSpeed : sprintSpeed;
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? currentSprint : currentWalk;

            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * currentSpeed * Time.deltaTime);

            if (Input.GetButtonDown("Jump") && isGrounded && !isSlowed)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TruckTrigger"))
        {
            isSlowed = true;
            isCameraLocked = true;
        }

        if (other.CompareTag("Truck"))
        {
            TeleportToSpawn();
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Truck"))
        {
            TeleportToSpawn();
        }
    }

    void TeleportToSpawn()
    {
        isSlowed = false;
        isCameraLocked = false;

        controller.enabled = false;

        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        xRotation = 0f;
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        velocity.y = 0f;

        Physics.SyncTransforms();

        controller.enabled = true;

        StartCoroutine(FreezeRoutine());
    }

    IEnumerator FreezeRoutine()
    {
        canMove = false;
        yield return new WaitForSeconds(1f);
        canMove = true;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}