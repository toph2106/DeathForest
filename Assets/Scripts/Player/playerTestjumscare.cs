using UnityEngine;

public class PlayerTestJumpscare : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;
    public Camera playerCamera; // Kéo Component Camera của Player vào đây

    private float xRotation = 0f;
    private Rigidbody rb;
    private Vector3 moveDirection;

    // Các thuộc tính bật/tắt quyền điều khiển công khai để Script khác gọi tới
    [HideInInspector] public bool canMove = true;
    [HideInInspector] public bool canLook = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Khóa rotation vật lý trục X/Z để tránh nghiêng ngả Player khi di chuyển dốc
        rb.freezeRotation = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Xử lý Input và Nhìn tự do nếu được phép
        if (canMove)
        {
            GatherInput();
        }
        else
        {
            moveDirection = Vector3.zero; // Ép đứng im
        }

        if (canLook)
        {
            LookNormal();
        }
    }

    void FixedUpdate()
    {
        Move();
    }

    void GatherInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveDirection = (transform.right * x + transform.forward * z).normalized;
    }

    void Move()
    {
        Vector3 targetVelocity = moveDirection * moveSpeed;
        
        // Giữ nguyên trọng lực rơi tự do Y để không bị bay lơ lửng
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    void LookNormal()
    {
        if (playerCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }
}