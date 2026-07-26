using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CamcorderUIAnimation : MonoBehaviour
{
    [Header("1. Các UI Thành Phần")]
    [Tooltip("Text REC 00:00:00 ở góc trên màn hình")]
    public TMP_Text recText;

    [Tooltip("Text Đồng hồ & Ngày tháng ở góc dưới màn hình")]
    public TMP_Text clockText;

    [Tooltip("Khung ảnh nhiễu sóng VHS / Glitch Noise (Tùy chọn)")]
    public Image noiseOverlayImage;

    [Header("2. Cấu Hình Animation Khởi Động")]
    [Tooltip("Thời gian màn hình bừng sáng & nháy nhiễu sóng VHS (Mặc định: 0.4s)")]
    public float powerOnDuration = 0.4f;

    [Tooltip("Bật hiệu ứng nháy chấm đỏ ● REC điện ảnh")]
    public bool enableBlinkingRecDot = true;

    [Tooltip("Thời gian nháy chấm đỏ trong mấy giây đầu (Mặc định: 3.0 giây)")]
    public float blinkDuration = 3.0f;

    [Tooltip("Tốc độ nháy chấm đỏ ● REC (Mặc định: 0.5s)")]
    public float recBlinkInterval = 0.5f;

    private CanvasGroup canvasGroup;
    private Coroutine blinkCoroutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        // Tự động chạy Animation bừng sáng VHS mỗi khi UI Máy Quay được bật lên
        StopAllCoroutines();
        StartCoroutine(StartupAnimationRoutine());
    }

    IEnumerator StartupAnimationRoutine()
    {
        // BƯỚC 1: FADE IN MÀN HÌNH BỪNG SÁNG (POWER ON FLASH)
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < powerOnDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / powerOnDuration;

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);

            // Hiệu ứng giật nháy nhiễu sóng nhẹ VHS khi vừa bật màn hình
            if (noiseOverlayImage != null)
            {
                noiseOverlayImage.color = new Color(1f, 1f, 1f, Random.Range(0.2f, 0.7f));
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;

        if (noiseOverlayImage != null)
        {
            noiseOverlayImage.color = new Color(1f, 1f, 1f, 0.08f);
        }

        // BƯỚC 2: NHÁY ĐÈN ĐỎ ● REC TRONG 3 GIÂY ĐẦU TỒI TRỞ VỀ MÀU TRẮNG CỐ ĐỊNH
        if (enableBlinkingRecDot && recText != null)
        {
            blinkCoroutine = StartCoroutine(BlinkRecDotRoutine());
        }
    }

    IEnumerator BlinkRecDotRoutine()
    {
        float elapsed = 0f;

        while (elapsed < blinkDuration && gameObject.activeInHierarchy)
        {
            // Màu đỏ mờ nháy ● REC
            recText.color = new Color(1f, 0.2f, 0.2f, 1f);
            yield return new WaitForSeconds(recBlinkInterval);
            elapsed += recBlinkInterval;

            // Màu trắng chuẩn
            recText.color = Color.white;
            yield return new WaitForSeconds(recBlinkInterval);
            elapsed += recBlinkInterval;
        }

        // ĐỦ 3 GIÂY THÌ ÉP TRỞ VỀ MÀU TRẮNG NGUYÊN BẢN SẠCH SẼ
        if (recText != null)
        {
            recText.color = Color.white;
        }
    }
}
