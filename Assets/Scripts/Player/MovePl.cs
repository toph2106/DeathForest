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

    [Header("Crouch Settings (Tính Năng Ngồi Phím C)")]
    [Tooltip("Phím bấm để bật/tắt ngồi (Mặc định: Phím C)")]
    public KeyCode crouchKey = KeyCode.C;

    [Tooltip("Tỷ lệ hạ thấp Camera khi ngồi (0.333 = chia 3 chiều cao Camera từ 0.6 xuống 0.2)")]
    public float crouchHeightMultiplier = 0.333333f;

    [Tooltip("Tỷ lệ giảm tốc độ di chuyển khi ngồi (0.5 = chia đôi tốc độ di chuyển)")]
    public float crouchSpeedMultiplier = 0.5f;

    [Tooltip("Tốc độ chuyển đổi nâng/hạ camera mượt mà")]
    public float crouchTransitionSpeed = 10f;

    [HideInInspector]
    public bool isCrouching = false;

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

    private float standingCamY = 0.6f;
    private float crouchCamY = 0.2f;

    void Start()
    {
        LockCursor();

        if (cameraTransform != null)
        {
            standingCamY = cameraTransform.localPosition.y;
            if (standingCamY == 0f) standingCamY = 0.6f;

            // Tự động chia 3 chiều cao camera khi ngồi
            if (crouchHeightMultiplier == 0.5f || crouchHeightMultiplier <= 0f)
            {
                crouchHeightMultiplier = 1f / 3f;
            }

            crouchCamY = standingCamY * crouchHeightMultiplier;
        }

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

        // 1. XỬ LÝ PHÍM NGỒI (C) & GIỮ SHIFT ĐỨNG DẬY CHẠY NHANH
        if (canMove && !isCameraLocked)
        {
            // Bấm phím C để bật/tắt trạng thái ngồi
            if (Input.GetKeyDown(crouchKey))
            {
                isCrouching = !isCrouching;
            }

            // Giữ Shift chạy nhanh -> Tự động đứng dậy (hủy trạng thái ngồi)
            if (Input.GetKey(KeyCode.LeftShift))
            {
                isCrouching = false;
            }
        }

        // 2. NÂNG / HẠ CAMERA MƯỢT MÀ BẰNG LERP (0.6f -> 0.3f)
        if (cameraTransform != null && !isCameraLocked)
        {
            float targetY = isCrouching ? crouchCamY : standingCamY;
            Vector3 camLocalPos = cameraTransform.localPosition;
            camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetY, Time.deltaTime * crouchTransitionSpeed);
            cameraTransform.localPosition = camLocalPos;
        }

        // 3. XỬ LÝ GÓC NHÌN CHUỘT
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

        // 4. TÍNH TOÁN DI CHUYỂN & TỐC ĐỘ (KHI NGỒI CHIA ĐÔI TỐC ĐỘ)
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

            // Tính toán tốc độ: Chạy nhanh (Shift), Đi bộ chuẩn, hoặc Ngồi (Chia đôi tốc độ)
            float speed = currentWalkSpeed;

            if (Input.GetKey(KeyCode.LeftShift) && !isCrouching)
            {
                speed = currentSprintSpeed;
            }
            else if (isCrouching)
            {
                speed = currentWalkSpeed * crouchSpeedMultiplier; // Chia đôi tốc độ khi ngồi
            }

            if (canMove)
            {
                controller.Move(move * speed * Time.deltaTime);
            }

            // Bấm Phím Nhảy (Space) -> Tự động đứng dậy nếu đang ngồi
            if (Input.GetButtonDown("Jump") && isGrounded && canMove)
            {
                if (isCrouching) isCrouching = false;
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