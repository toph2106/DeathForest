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
        // KHÔNG KHÓA CHUỘT NẾU GAME ĐANG TRONG TRẠNG THÁI PAUSE MENU HOẶC ĐANG ĐỌC TÀI LIỆU
        if (PauseMenuManager.isPaused) return;

        if (Input.GetMouseButtonDown(0)) LockCursor();
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) UnlockCursor();

        if (Cursor.lockState == CursorLockMode.Locked && canMove)
        {
            if (!isCameraLocked)
            {
                // ĐỒNG BỘ ĐỘ NHẠY CHUỘT TỪ SETTINGSMANAGER VÀ PLAYERPREFS
                float sensMultiplier = SettingsManager.mouseSensitivity > 0 ? SettingsManager.mouseSensitivity : PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
                float effectiveSensitivity = mouseSensitivity * sensMultiplier;

                float mouseX = Input.GetAxis("Mouse X") * effectiveSensitivity * Time.deltaTime;
                float mouseY = Input.GetAxis("Mouse Y") * effectiveSensitivity * Time.deltaTime;

                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                transform.Rotate(Vector3.up * mouseX);
            }
        }

        // TÍNH TOÁN DI CHUYỂN
        if (controller != null && controller.enabled)
        {
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 move = transform.right * x + transform.forward * z;

            float currentWalkSpeed = isSlowed ? slowWalkSpeed : walkSpeed;
            float currentSprintSpeed = isSlowed ? slowSprintSpeed : sprintSpeed;
            float speed = Input.GetKey(KeyCode.LeftShift) ? currentSprintSpeed : currentWalkSpeed;

            if (canMove)
            {
                controller.Move(move * speed * Time.deltaTime);
            }

            if (Input.GetButtonDown("Jump") && isGrounded && canMove)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }

    public void TeleportToSpawn()
    {
        if (spawnPoint != null && controller != null)
        {
            controller.enabled = false;
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
            controller.enabled = true;
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetMovementState(bool state)
    {
        canMove = state;
    }
}