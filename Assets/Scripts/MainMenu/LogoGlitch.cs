using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class LogoGlitch : MonoBehaviour
{
    [Header("Cài đặt Giật (Glitch)")]
    [Tooltip("Bật lên nếu muốn Logo tự giật độc lập. Tắt đi nếu muốn FlickerEffect điều khiển giật đồng bộ.")]
    public bool autoGlitch = false;

    [Tooltip("Thời gian yên bình tối thiểu (giây) khi chạy tự do")]
    public float minWaitTime = 5f;
    [Tooltip("Thời gian yên bình tối đa (giây) khi chạy tự do")]
    public float maxWaitTime = 12f;
    
    [Tooltip("Độ giật văng xa nhất (pixel)")]
    public float maxGlitchOffset = 15f;

    [Header("Hiệu ứng bản sao nhiễu màu (Chromatic Split)")]
    [Tooltip("Tạo 2 bản sao mờ lệch màu Đỏ và Xanh khi bị Glitch.")]
    public bool enableChromaticSplit = true;
    [Tooltip("Khoảng cách lệch của bản sao so với Logo gốc (pixel)")]
    public float splitOffset = 5f;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;

    private GameObject redClone;
    private GameObject cyanClone;
    private Coroutine activeGlitchCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalAnchoredPos = rectTransform.anchoredPosition;
        }
    }

    void Start()
    {
        if (enableChromaticSplit)
        {
            redClone = CreateColorClone("RedClone", new Color(1f, 0f, 0f, 0.4f));
            cyanClone = CreateColorClone("CyanClone", new Color(0f, 1f, 1f, 0.4f));
            
            if (redClone != null) redClone.SetActive(false);
            if (cyanClone != null) cyanClone.SetActive(false);
        }

        if (autoGlitch)
        {
            StartCoroutine(GlitchRoutine());
        }
    }

    // HÀM ĐỒNG BỘ: Kích hoạt giật Logo an toàn (chỉ chạy khi GameObject đang Active)
    public void TriggerGlitchPulse(int count = 3)
    {
        // KIỂM TRA AN TOÀN: Nếu Logo đang bị ẩn (như khi mở Settings), KHÔNG chạy Coroutine để tránh lỗi đỏ Console
        if (!gameObject.activeInHierarchy || !enabled) return;

        if (activeGlitchCoroutine != null)
        {
            StopCoroutine(activeGlitchCoroutine);
        }

        activeGlitchCoroutine = StartCoroutine(SingleGlitchSequence(Mathf.Max(2, count)));
    }

    IEnumerator SingleGlitchSequence(int count)
    {
        if (enableChromaticSplit && redClone != null && cyanClone != null)
        {
            redClone.SetActive(true);
            cyanClone.SetActive(true);
        }

        for (int i = 0; i < count; i++)
        {
            float randomX = Random.Range(-maxGlitchOffset, maxGlitchOffset);
            float randomY = Random.Range(-3f, 3f);
            rectTransform.anchoredPosition = originalAnchoredPos + new Vector2(randomX, randomY);

            if (enableChromaticSplit && redClone != null && cyanClone != null)
            {
                float offset = Random.Range(splitOffset * 0.5f, splitOffset * 1.5f);
                redClone.GetComponent<RectTransform>().anchoredPosition = rectTransform.anchoredPosition + new Vector2(-offset, 0);
                cyanClone.GetComponent<RectTransform>().anchoredPosition = rectTransform.anchoredPosition + new Vector2(offset, 0);
            }

            yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.09f));

            rectTransform.anchoredPosition = originalAnchoredPos;

            yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.09f));
        }

        ResetGlitchState();
        activeGlitchCoroutine = null;
    }

    IEnumerator GlitchRoutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(minWaitTime, maxWaitTime));
            yield return StartCoroutine(SingleGlitchSequence(Random.Range(3, 6)));
        }
    }

    GameObject CreateColorClone(string cloneName, Color tintColor)
    {
        GameObject clone = new GameObject(cloneName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        clone.transform.SetParent(transform.parent, false);

        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        
        cloneRect.anchorMin = rectTransform.anchorMin;
        cloneRect.anchorMax = rectTransform.anchorMax;
        cloneRect.pivot = rectTransform.pivot;
        cloneRect.sizeDelta = rectTransform.sizeDelta;
        cloneRect.anchoredPosition = rectTransform.anchoredPosition;
        cloneRect.localScale = rectTransform.localScale;
        cloneRect.localRotation = rectTransform.localRotation;

        Image myImage = GetComponent<Image>();
        Image cloneImage = clone.GetComponent<Image>();

        if (myImage != null && cloneImage != null)
        {
            cloneImage.sprite = myImage.sprite;
            cloneImage.color = tintColor;
            cloneImage.type = myImage.type;
            cloneImage.preserveAspect = myImage.preserveAspect;
            cloneImage.raycastTarget = false;
        }

        clone.transform.SetSiblingIndex(transform.GetSiblingIndex());
        return clone;
    }

    void ResetGlitchState()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalAnchoredPos;
        }
        if (redClone != null) redClone.SetActive(false);
        if (cyanClone != null) cyanClone.SetActive(false);
    }

    void OnDisable()
    {
        ResetGlitchState();
        activeGlitchCoroutine = null;
    }

    void OnDestroy()
    {
        if (redClone != null) Destroy(redClone);
        if (cyanClone != null) Destroy(cyanClone);
    }
}
