using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ContinueManager : MonoBehaviour
{
    [Header("1. Cài đặt các Nút Màn chơi (Map Buttons)")]
    [Tooltip("Kéo Nút Map 01 vào đây")]
    public Button map1Button;

    [Tooltip("Kéo Nút Map 02 vào đây")]
    public Button map2Button;

    [Tooltip("Kéo Nút Map 03 vào đây")]
    public Button map3Button;

    [Header("2. Cài đặt Khung Xem Trước (Preview Image)")]
    [Tooltip("Kéo cái ô Image hiển thị ảnh bên phải vào đây")]
    public Image previewImage;

    [Tooltip("Kéo ảnh Sprite xem trước của Map 01 vào đây")]
    public Sprite map1Sprite;

    [Tooltip("Kéo ảnh Sprite xem trước của Map 02 vào đây")]
    public Sprite map2Sprite;

    [Tooltip("Kéo ảnh Sprite xem trước của Map 03 vào đây")]
    public Sprite map3Sprite;

    [Header("3. Chỉnh Tông Màu Ảnh Xem Trước")]
    [Tooltip("Màu sắc khi Màn chơi ĐÃ MỞ KHÓA (Nổi bật rõ nét)")]
    public Color unlockedColor = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Màu sắc khi Màn chơi CHƯA MỞ KHÓA (Mờ xám mờ ảo)")]
    public Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);

    private CanvasGroup previewCanvasGroup;
    private RectTransform previewRect;
    private Vector3 originalPreviewScale = Vector3.one;

    void Awake()
    {
        if (previewImage != null)
        {
            previewCanvasGroup = previewImage.GetComponent<CanvasGroup>();
            if (previewCanvasGroup == null) previewCanvasGroup = previewImage.gameObject.AddComponent<CanvasGroup>();
            
            previewRect = previewImage.GetComponent<RectTransform>();
            if (previewRect != null) originalPreviewScale = previewRect.localScale;

            previewCanvasGroup.alpha = 0f;
            previewImage.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        UpdateMapLockStatus();
        HidePreviewImmediate();
    }

    public void UpdateMapLockStatus()
    {
        int unlockedLevel = GameSaveManager.GetUnlockedLevel();

        if (map1Button != null) map1Button.interactable = true;
        if (map2Button != null) map2Button.interactable = (unlockedLevel >= 2);
        if (map3Button != null) map3Button.interactable = (unlockedLevel >= 3);
    }

    public void ShowPreview(int mapIndex)
    {
        if (previewImage == null) return;

        int unlockedLevel = GameSaveManager.GetUnlockedLevel();
        Sprite targetSprite = null;
        bool isUnlocked = true;

        if (mapIndex == 1)
        {
            targetSprite = map1Sprite;
            isUnlocked = true;
        }
        else if (mapIndex == 2)
        {
            targetSprite = map2Sprite;
            isUnlocked = (unlockedLevel >= 2);
        }
        else if (mapIndex == 3)
        {
            targetSprite = map3Sprite;
            isUnlocked = (unlockedLevel >= 3);
        }

        if (targetSprite != null)
        {
            previewImage.sprite = targetSprite;
            previewImage.color = isUnlocked ? unlockedColor : lockedColor;
            previewImage.gameObject.SetActive(true);
            
            if (previewCanvasGroup != null)
            {
                previewCanvasGroup.DOKill();
                previewCanvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
            }

            if (previewRect != null)
            {
                previewRect.DOKill();
                previewRect.localScale = originalPreviewScale * 0.95f;
                previewRect.DOScale(originalPreviewScale * 1.03f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }
    }

    public void HidePreview()
    {
        if (previewImage == null) return;

        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.DOKill();
            previewCanvasGroup.DOFade(0f, 0.15f).SetUpdate(true).OnComplete(() =>
            {
                previewImage.gameObject.SetActive(false);
            });
        }
        else
        {
            previewImage.gameObject.SetActive(false);
        }
    }

    void HidePreviewImmediate()
    {
        if (previewImage != null)
        {
            if (previewCanvasGroup != null) previewCanvasGroup.alpha = 0f;
            if (previewRect != null) previewRect.localScale = originalPreviewScale;
            previewImage.gameObject.SetActive(false);
        }
    }

    public void ResetProgress()
    {
        GameSaveManager.ResetProgress();
        UpdateMapLockStatus();
    }
}
