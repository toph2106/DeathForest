using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class FlickerEffect : MonoBehaviour
{
    [Header("Cài đặt chớp tắt (Flicker)")]
    [Tooltip("Thời gian chờ tối thiểu giữa các đợt chớp (giây)")]
    public float minWaitTime = 3f;
    [Tooltip("Thời gian chờ tối đa giữa các đợt chớp (giây)")]
    public float maxWaitTime = 8f;
    [Tooltip("Độ đen tối đa khi chớp (0 là trong suốt, 1 là đen đặc)")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.6f;

    [Header("Cài đặt giật hình (Glitch)")]
    [Tooltip("Kéo object muốn bị giật ngang (ví dụ: BackGround) vào đây")]
    public RectTransform targetToGlitch;
    [Tooltip("Khoảng cách dịch chuyển tối đa (pixel)")]
    public float maxGlitchOffset = 15f;

    [Header("Đồng bộ Glitch Logo (Nhịp xen kẽ)")]
    [Tooltip("Kéo Logo (có gắn script LogoGlitch) vào đây để Logo giật xen kẽ giữa các nhịp chớp nền")]
    public LogoGlitch logoGlitch;

    [Header("Hiệu ứng sọc ngang (Scanlines)")]
    [Tooltip("Bật lên để hiện 1 vệt sáng trắng chạy dọc màn hình khi bị chớp")]
    public bool enableScanline = true;
    [Tooltip("Kéo 1 Image mỏng ngang (thanh trắng mờ) vào đây")]
    public RectTransform scanlineBar;

    private Image overlayImage;
    private Color originalColor;
    private Vector2 originalTargetPos;

    void Start()
    {
        overlayImage = GetComponent<Image>();
        overlayImage.raycastTarget = false;
        
        originalColor = overlayImage.color;
        SetAlpha(0f);

        if (targetToGlitch != null)
            originalTargetPos = targetToGlitch.anchoredPosition;
        
        if (scanlineBar != null)
        {
            scanlineBar.gameObject.SetActive(false);
            Image scanlineImg = scanlineBar.GetComponent<Image>();
            if (scanlineImg != null) scanlineImg.raycastTarget = false;
        }

        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // Lấy thời gian chu kỳ ngẫu nhiên (Ví dụ: 6 giây)
            float fullCycleTime = Random.Range(minWaitTime, maxWaitTime);
            float halfCycleTime = fullCycleTime * 0.5f; // Nửa chu kỳ (Ví dụ: 3 giây)

            // --- ĐỢT 1: CHỜ NỬA CHU KỲ NÀY ➔ KÍCH HOẠT LOGO GIẬT (Nếu Logo đang Active) ---
            yield return new WaitForSecondsRealtime(halfCycleTime);

            if (logoGlitch != null && logoGlitch.gameObject.activeInHierarchy)
            {
                logoGlitch.TriggerGlitchPulse(Random.Range(2, 4));
            }

            // --- ĐỢT 2: CHỜ NỬA CHU KỲ CÒN LẠI ➔ KÍCH HOẠT CHỚP NỀN (BACKGROUND FLICKER) ---
            yield return new WaitForSecondsRealtime(halfCycleTime);

            if (enableScanline && scanlineBar != null)
            {
                StartCoroutine(ScanlinePass());
            }

            int flickerCount = Random.Range(1, 4);
            for (int i = 0; i < flickerCount; i++)
            {
                SetAlpha(Random.Range(0.15f, maxAlpha));

                if (targetToGlitch != null)
                {
                    float randomOffsetX = Random.Range(-maxGlitchOffset, maxGlitchOffset);
                    targetToGlitch.anchoredPosition = originalTargetPos + new Vector2(randomOffsetX, 0);
                }

                yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.1f));

                SetAlpha(0f);
                if (targetToGlitch != null)
                    targetToGlitch.anchoredPosition = originalTargetPos;

                yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.08f));
            }
        }
    }

    IEnumerator ScanlinePass()
    {
        if (scanlineBar == null) yield break;

        scanlineBar.gameObject.SetActive(true);

        float startY = 540f; 
        float endY = -540f;  
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float currentY = Mathf.Lerp(startY, endY, t);
            scanlineBar.anchoredPosition = new Vector2(0, currentY);
            yield return null;
        }

        scanlineBar.gameObject.SetActive(false);
    }

    private void SetAlpha(float alpha)
    {
        Color c = originalColor;
        c.a = alpha;
        overlayImage.color = c;
    }
}
