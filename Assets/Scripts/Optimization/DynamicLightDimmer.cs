using UnityEngine;

public class DynamicLightDimmer : MonoBehaviour
{
    [Header("1. Cấu Hình Tự Động Giảm Chói Khi Áp Sát Tường")]
    [Tooltip("Khoảng cách tính từ tường bắt đầu tự động giảm độ sáng (Mặc định: 3 mét)")]
    public float checkDistance = 3f;

    [Tooltip("Tỉ lệ giảm sáng tối đa khi đứng sát vách tường (Mặc định: 0.25 = Giảm còn 25% độ sáng gốc)")]
    [Range(0.05f, 0.9f)]
    public float minIntensityMultiplier = 0.25f;

    [Tooltip("Tốc độ tăng/giảm sáng mượt mà (Mặc định: 10)")]
    public float smoothSpeed = 10f;

    [Header("2. Mặt Đất / Tường Cần Nhận Biết (Layer Mask)")]
    [Tooltip("Chọn các Layer tường/vật cản (Để Default / Everything)")]
    public LayerMask obstacleLayer = ~0;

    private Light targetLight;
    private float originalIntensity;

    void Start()
    {
        targetLight = GetComponent<Light>();
        if (targetLight != null)
        {
            originalIntensity = targetLight.intensity;
        }
    }

    void Update()
    {
        if (targetLight == null) return;

        // Nếu bạn vừa tự chỉnh Intensity trong Inspector lúc đang chơi -> Cập nhật lại giá trị gốc
        if (!IsInvoking() && Mathf.Abs(targetLight.intensity - originalIntensity) > 50f && !Physics.Raycast(transform.position, transform.forward, checkDistance, obstacleLayer))
        {
            originalIntensity = targetLight.intensity;
        }

        float targetIntensity = originalIntensity;

        // Chiếu tia Raycast ra phía trước theo hướng chiếu của Đèn
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, checkDistance, obstacleLayer))
        {
            // Tính tỉ lệ khoảng cách từ 0m -> checkDistance (VD: 3m)
            float distanceRatio = Mathf.Clamp01(hit.distance / checkDistance);

            // Giảm độ sáng mượt mà theo khoảng cách gần tường
            float multiplier = Mathf.Lerp(minIntensityMultiplier, 1f, distanceRatio);
            targetIntensity = originalIntensity * multiplier;
        }

        // Biến thiên độ sáng cực kỳ mượt mà không bị giật
        targetLight.intensity = Mathf.Lerp(targetLight.intensity, targetIntensity, Time.deltaTime * smoothSpeed);
    }
}
