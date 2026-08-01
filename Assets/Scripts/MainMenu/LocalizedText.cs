using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Header("Nội dung Song Ngữ")]
    [TextArea(1, 3)]
    public string englishText = "PLAY";
    
    [TextArea(1, 3)]
    public string vietnameseText = "BẮT ĐẦU";

    private TMP_Text textComponent;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện đổi ngôn ngữ
        SettingsManager.onLanguageChanged += RefreshText;
        RefreshText();
    }

    void OnDisable()
    {
        // Hủy đăng ký khi object bị tắt
        SettingsManager.onLanguageChanged -= RefreshText;
    }

    public void RefreshText()
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();
        if (textComponent == null) return;

        if (SettingsManager.currentLanguage == "VI")
        {
            if (!string.IsNullOrEmpty(vietnameseText))
            {
                textComponent.text = vietnameseText;
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(englishText))
            {
                textComponent.text = englishText;
            }
        }
    }
}
