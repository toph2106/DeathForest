using UnityEngine;

public class FlashlightSway : MonoBehaviour
{
    [Header("Sway (Độ trễ xoay camera)")]
    public float swaySpeed = 5f;
    public float swayAmount = 2f;

    [Header("Bobbing (Nhịp tay lắc lư)")]
    public float bobbingSpeed = 2f;
    public float bobbingAmount = 0.02f;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private float timer = 0f;

    void Start()
    {
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        // 1. SWAY (Trễ xoay khi lia chuột)
        float mouseX = Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = Input.GetAxis("Mouse Y") * swayAmount;

        Quaternion swayRotation = Quaternion.Euler(-mouseY, mouseX, 0f) * initialRotation;
        
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            swayRotation,
            swaySpeed * Time.deltaTime
        );

        // 2. BOBBING (Lắc lư lên xuống hình số 8)
        float currentBobbingSpeed = bobbingSpeed;
        float currentBobbingAmount = bobbingAmount;

        // Nếu đang bấm phím di chuyển thì tay lắc nhanh và mạnh hơn
        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
            currentBobbingSpeed = bobbingSpeed * 3f;
            currentBobbingAmount = bobbingAmount * 2f;
        }

        timer += Time.deltaTime * currentBobbingSpeed;

        // Tạo sóng Sin/Cos để vị trí đèn pin chạy theo đường cong hình số 8
        float newY = initialPosition.y + Mathf.Sin(timer) * currentBobbingAmount;
        float newX = initialPosition.x + Mathf.Cos(timer / 2f) * currentBobbingAmount * 0.5f;

        Vector3 targetPosition = new Vector3(newX, newY, initialPosition.z);

        // Làm mượt chuyển động tịnh tiến
        transform.localPosition = Vector3.Lerp(
            transform.localPosition, 
            targetPosition, 
            Time.deltaTime * 5f
        );
    }
}