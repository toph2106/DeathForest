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

    [Header("Stamina & Sprint Settings (Giới Hạn Thể Lực Chạy Nhanh)")]
    [Tooltip("Thời gian chạy nhanh tối đa liên tục (Mặc định: 5 giây)")]
    public float maxSprintDuration = 5.0f;

    [Tooltip("Thời điểm bắt đầu thở dốc & giảm tốc độ (Mặc định: 4.0 giây)")]
    public float pantingStartDuration = 4.0f;

    [Tooltip("Thời gian hồi thể lực / Cooldown khi hết sức (Mặc định: 10 giây)")]
    public float sprintCooldown = 10.0f;

    [Tooltip("Âm thanh thở hộc hộc / thở dốc khi mệt")]
    public AudioClip heavyBreathingSound;

    [Tooltip("Âm thanh uống nước hồi phục thể lực")]
    public AudioClip drinkSound;

    [HideInInspector] public float currentSprintTime = 0f;
    [HideInInspector] public float currentCooldownTime = 0f;
    [HideInInspector] public bool isExhausted = false;
    private AudioSource breathingAudioSource;

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

    [Header("Camera Clipping Settings (Chống Thủng Tường)")]
    [Tooltip("Khoảng cách gần nhất Camera bắt đầu vẽ vật thể. 0.01m (1cm) giúp đứng sát tường không bị thủng nhìn xuyên ra ngoài trời")]
    public float cameraNearClip = 0.01f;

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
            if (standingCamY <= 0f || standingCamY == 0.6f)
            {
                float currentY = cameraTransform.localPosition.y;
                if (currentY > 0.1f) standingCamY = currentY;
            }
            if (standingCamY <= 0f) standingCamY = 0.6f;

            // Tự động chia 3 chiều cao camera khi ngồi
            if (crouchHeightMultiplier == 0.5f || crouchHeightMultiplier <= 0f)
            {
                crouchHeightMultiplier = 1f / 3f;
            }

            crouchCamY = standingCamY * crouchHeightMultiplier;
        }

        // TỰ ĐỘNG CHỈNH NEAR CLIP PLANE ĐỂ KHÔNG BỊ CẮT THỦNG TƯỜNG KHI ĐỨNG SÁT
        Camera cam = (cameraTransform != null) ? cameraTransform.GetComponent<Camera>() : Camera.main;
        if (cam != null && cameraNearClip > 0f)
        {
            cam.nearClipPlane = cameraNearClip;
        }

        // Tạo AudioSource chuyên phát âm thanh thở dốc
        breathingAudioSource = gameObject.AddComponent<AudioSource>();
        breathingAudioSource.spatialBlend = 0f;
        breathingAudioSource.loop = true;
        breathingAudioSource.playOnAwake = false;
        if (heavyBreathingSound != null) breathingAudioSource.clip = heavyBreathingSound;

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

            // 4.1. TÍNH TOÁN THỂ LỰC & TỐC ĐỘ: Chạy nhanh tối đa 5s, thở dốc từ giây thứ 4 và Cooldown 10s
            float speed = currentWalkSpeed;
            bool isMoving = move.sqrMagnitude > 0.001f;
            bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift) && !isCrouching && isMoving;

            if (isExhausted)
            {
                // Đang trong thời gian Cooldown 10s -> Ép đi bộ, không được chạy nhanh
                currentCooldownTime -= Time.deltaTime;
                speed = currentWalkSpeed;

                if (currentCooldownTime <= 0f)
                {
                    isExhausted = false;
                    currentSprintTime = 0f;
                    currentCooldownTime = 0f;
                    if (breathingAudioSource != null && breathingAudioSource.isPlaying)
                    {
                        breathingAudioSource.Stop();
                    }
                }
            }
            else
            {
                if (isTryingToSprint)
                {
                    currentSprintTime += Time.deltaTime;

                    if (currentSprintTime >= maxSprintDuration)
                    {
                        // Hết 5 giây chạy liên tục -> Kiệt sức, chuyển sang Cooldown 10s
                        isExhausted = true;
                        currentCooldownTime = sprintCooldown;
                        speed = currentWalkSpeed;

                        if (breathingAudioSource != null && heavyBreathingSound != null && !breathingAudioSource.isPlaying)
                        {
                            breathingAudioSource.clip = heavyBreathingSound;
                            breathingAudioSource.Play();
                        }
                    }
                    else if (currentSprintTime >= pantingStartDuration)
                    {
                        // Từ giây thứ 4.0 đến 5.0: Bắt đầu thở dốc hộc hộc và giảm dần tốc độ về WalkSpeed
                        if (breathingAudioSource != null && heavyBreathingSound != null && !breathingAudioSource.isPlaying)
                        {
                            breathingAudioSource.clip = heavyBreathingSound;
                            breathingAudioSource.Play();
                        }

                        float t = (currentSprintTime - pantingStartDuration) / Mathf.Max(0.01f, maxSprintDuration - pantingStartDuration);
                        speed = Mathf.Lerp(currentSprintSpeed, currentWalkSpeed, t);
                    }
                    else
                    {
                        speed = currentSprintSpeed;
                    }
                }
                else
                {
                    // Nhả Shift hoặc không di chuyển: Thể lực hồi phục dần
                    if (currentSprintTime > 0f)
                    {
                        currentSprintTime -= Time.deltaTime * (maxSprintDuration / Mathf.Max(1f, sprintCooldown * 0.5f));
                        if (currentSprintTime < 0f) currentSprintTime = 0f;

                        if (currentSprintTime < pantingStartDuration && breathingAudioSource != null && breathingAudioSource.isPlaying)
                        {
                            breathingAudioSource.Stop();
                        }
                    }

                    if (isCrouching)
                    {
                        speed = currentWalkSpeed * crouchSpeedMultiplier; // Chia đôi tốc độ khi ngồi
                    }
                    else
                    {
                        speed = currentWalkSpeed;
                    }
                }
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

            // Thân người (Main) luôn đứng thẳng: X = 0, Z = 0, chỉ xoay trục Y
            transform.rotation = Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f);

            // Camera nhận góc nghiêng X (Pitch) từ spawnPoint
            if (cameraTransform != null)
            {
                float pitch = spawnPoint.eulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
                xRotation = Mathf.Clamp(pitch, -89f, 89f);
                cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }

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

    public void SetStandingCamY(float y)
    {
        if (y > 0.1f)
        {
            standingCamY = y;
            if (crouchHeightMultiplier <= 0f) crouchHeightMultiplier = 1f / 3f;
            crouchCamY = standingCamY * crouchHeightMultiplier;
        }
    }

    /// <summary>
    /// Uống lon nước hồi phục thể lực ngay lập tức, xóa bỏ Cooldown mệt mỏi!
    /// </summary>
    public void RestoreStaminaInstant()
    {
        isExhausted = false;
        currentSprintTime = 0f;
        currentCooldownTime = 0f;

        if (breathingAudioSource != null && breathingAudioSource.isPlaying)
        {
            breathingAudioSource.Stop();
        }

        Debug.Log("[MovePl] 🥤 Đã uống lon nước! Thể lực hồi phục 100%, có thể chạy nhanh ngay lập tức!");
    }

    void LateUpdate()
    {
        // Khi bị quái vật khóa Camera, tự động hướng thẳng góc nhìn vào xương đầu của quái vật
        if (isCameraLocked && forcedLookTarget != null && cameraTransform != null)
        {
            Vector3 targetDir = forcedLookTarget.position - cameraTransform.position;
            if (targetDir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(targetDir);
                cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, lookRot, 15f * Time.deltaTime);

                Vector3 localEuler = cameraTransform.localEulerAngles;
                float pitch = localEuler.x;
                if (pitch > 180f) pitch -= 360f;
                xRotation = pitch;
            }
        }
    }
}