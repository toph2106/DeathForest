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
    public float mouseSensitivity = 100f;

    [Header("Smooth Mouse Look (Xử Lý Xoay Camera Mượt Như Nhung AAA)")]
    [Tooltip("Bật ô này để khử hoàn toàn vi giật (Micro-stutter) giúp góc nhìn mượt như game AAA")]
    public bool enableSmoothLook = true;

    [Tooltip("Tốc độ lướt mượt góc nhìn (Mặc định: 18.0 - Càng cao càng nhạy tức thì, càng thấp càng đằm tay)")]
    public float smoothLookSpeed = 18.0f;

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

    private Vector2 targetMouseDelta;
    private Vector2 smoothMouseDelta;

    void Start()
    {
        LockCursor();
        SyncRotationWithCurrentCamera();
    }

    /// <summary>
    /// Đồng bộ góc xoay xRotation nội bộ của MovePl trùng khớp với góc nhìn thực tế của Camera (Chống giật góc khi trả lại quyền điều khiển)
    /// </summary>
    public void SyncRotationWithCurrentCamera()
    {
        if (cameraTransform != null)
        {
            Vector3 euler = cameraTransform.localEulerAngles;
            float pitch = euler.x;
            if (pitch > 180f) pitch -= 360f;
            xRotation = pitch;
        }
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
                float sensMultiplier = SettingsManager.mouseSensitivity > 0 ? SettingsManager.mouseSensitivity : PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
                float effectiveSensitivity = mouseSensitivity * sensMultiplier;

                // TÍNH TOÁN DỮ LIỆU CHUỘT GỐC
                targetMouseDelta.x = Input.GetAxis("Mouse X") * effectiveSensitivity;
                targetMouseDelta.y = Input.GetAxis("Mouse Y") * effectiveSensitivity;

                // LỌC KHỬ VI GIẬT KHUNG HÌNH (MICRO-STUTTER) MƯỢT MÀ BẰNG LERP AAA
                if (enableSmoothLook)
                {
                    smoothMouseDelta = Vector2.Lerp(smoothMouseDelta, targetMouseDelta, Time.deltaTime * smoothLookSpeed);
                }
                else
                {
                    smoothMouseDelta = targetMouseDelta;
                }

                float mouseX = smoothMouseDelta.x * Time.deltaTime;
                float mouseY = smoothMouseDelta.y * Time.deltaTime;

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