using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Bắt buộc để dùng các sự kiện chuột (Hover, Click)
using TMPro; // Bắt buộc để dùng TextMeshPro
using DG.Tweening; // Phép thuật DOTween

[RequireComponent(typeof(TMP_Text))]
public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Cài đặt khi di chuột vào (Hover)")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.6f, 0f, 0f); // Đỏ sẫm máu
    public float hoverScale = 1.15f; // Phóng to 15%
    public float hoverCharacterSpacing = 10f; // Khoảng cách giãn chữ
    public float animationDuration = 0.25f; // Thời gian chuyển động

    [Header("Âm thanh (Audio SFX)")]
    [Tooltip("Kéo file âm thanh khi BẤM CHUỘT vào nút (Click) vào đây")]
    public AudioClip clickSound;

    [Tooltip("Âm lượng tiếng bấm nút (0.0 đến 1.0)")]
    [Range(0f, 1f)]
    public float soundVolume = 0.6f;

    [Tooltip("Kéo AudioSource dùng để phát âm thanh vào đây (nếu để trống script sẽ tự tạo)")]
    public AudioSource sfxAudioSource;

    private TMP_Text buttonText;
    private RectTransform rectTransform;
    
    private Vector3 originalScale;
    private float originalSpacing;

    void Start()
    {
        buttonText = GetComponent<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();
        
        originalScale = rectTransform.localScale;
        originalSpacing = buttonText.characterSpacing;
        
        buttonText.color = normalColor;

        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = GetComponentInParent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // KHI CHUỘT CHẠM VÀO CHỮ
    public void OnPointerEnter(PointerEventData eventData)
    {
        rectTransform.DOScale(originalScale * hoverScale, animationDuration).SetEase(Ease.OutBack).SetUpdate(true);
        buttonText.DOColor(hoverColor, animationDuration).SetUpdate(true);
        DOTween.To(() => buttonText.characterSpacing, x => buttonText.characterSpacing = x, hoverCharacterSpacing, animationDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    // KHI CHUỘT RỜI KHỎI CHỮ
    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.DOScale(originalScale, animationDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        buttonText.DOColor(normalColor, animationDuration).SetUpdate(true);
        DOTween.To(() => buttonText.characterSpacing, x => buttonText.characterSpacing = x, originalSpacing, animationDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    // KHI BẤM CHUỘT VÀO
    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. Nút rung lên bần bật (Shake)
        rectTransform.DOShakePosition(0.2f, strength: 7f, vibrato: 20).SetUpdate(true);
        
        // 2. Chữ lóe sáng lên màu trắng rồi từ từ đỏ lại
        buttonText.DOColor(Color.white, 0.05f).OnComplete(() => {
            buttonText.DOColor(hoverColor, 0.15f).SetUpdate(true);
        }).SetUpdate(true);

        // Phát âm thanh Click khi bấm nút với âm lượng soundVolume vừa phải
        if (clickSound != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(clickSound, soundVolume);
        }

        // 3. KÍCH HOẠT SỰ KIỆN ON CLICK () CHỈ KHI BUTTON NẰM Ở OBJECT CHA
        // (Nếu Button nằm cùng object này thì Unity đã tự động gọi onClick rồi, KHÔNG CẦN gọi lại!)
        Button sameButton = GetComponent<Button>();
        if (sameButton != null)
        {
            // Button nằm cùng object → Unity tự xử lý → KHÔNG LÀM GÌ THÊM
            return;
        }

        Button parentButton = GetComponentInParent<Button>();
        if (parentButton != null && parentButton.interactable)
        {
            parentButton.onClick.Invoke();
        }
    }
    
    void OnDisable()
    {
        if (rectTransform != null) rectTransform.DOKill();
        if (buttonText != null) buttonText.DOKill();
        
        if (rectTransform != null) rectTransform.localScale = originalScale;
        if (buttonText != null) buttonText.color = normalColor;
        if (buttonText != null) buttonText.characterSpacing = originalSpacing;
    }
}
