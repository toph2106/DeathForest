using UnityEngine;

public class BreathingEffect : MonoBehaviour
{
    [Header("Cài đặt nhịp thở")]
    [Tooltip("Tốc độ thở. Số càng nhỏ thở càng chậm (Gợi ý: 0.8 - 1.5).")]
    public float breatheSpeed = 1.2f;
    
    [Tooltip("Độ phóng to tối đa. Vd: 1.03 tức là phóng to thêm 3%.")]
    public float maxScaleMultiplier = 1.03f;

    [Header("Di chuyển nhẹ (Creepy Drift)")]
    [Tooltip("Bật lên để hình nền hơi dịch chuyển nhè nhẹ khi thở, tạo cảm giác bất ổn")]
    public bool enableDrift = true;
    [Tooltip("Biên độ dịch chuyển tối đa (pixel)")]
    public float driftAmount = 3f;
    [Tooltip("Tốc độ trôi (nên khác tốc độ thở để tạo cảm giác ngẫu nhiên hơn)")]
    public float driftSpeed = 0.7f;

    private Vector3 originalScale;
    private Vector3 originalPosition;

    void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        // --- HIỆU ỨNG THỞ (Scale) ---
        float sinValue = (Mathf.Sin(Time.unscaledTime * breatheSpeed) + 1f) / 2f;
        float currentScale = Mathf.Lerp(1.0f, maxScaleMultiplier, sinValue);

        transform.localScale = new Vector3(
            originalScale.x * currentScale,
            originalScale.y * currentScale,
            originalScale.z
        );

        // --- HIỆU ỨNG TRÔI NHẸ (Drift) ---
        if (enableDrift)
        {
            // Dùng 2 sóng Sin với tốc độ khác nhau cho X và Y để chuyển động không bị lặp lại
            float driftX = Mathf.Sin(Time.unscaledTime * driftSpeed) * driftAmount;
            float driftY = Mathf.Sin(Time.unscaledTime * driftSpeed * 0.7f) * driftAmount * 0.5f;

            transform.localPosition = originalPosition + new Vector3(driftX, driftY, 0);
        }
    }
}
