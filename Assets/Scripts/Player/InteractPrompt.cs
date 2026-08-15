using UnityEngine;
using TMPro;

public class InteractPrompt : MonoBehaviour
{
    [Header("1. Chữ Nhắc Tương Tác Song Ngữ")]
    public string englishPrompt = "Use Computer";
    public string vietnamesePrompt = "Xem máy tính";

    [Header("2. Canvas Gợi Ý (World Space UI - Tùy chọn)")]
    public GameObject pressFHintUI;
    public TextMeshProUGUI worldSpaceText;

    void Start()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(false);
        UpdateText();
    }

    void OnEnable()
    {
        SettingsManager.onLanguageChanged += UpdateText;
    }

    void OnDisable()
    {
        SettingsManager.onLanguageChanged -= UpdateText;
    }

    public void ShowPrompt()
    {
        UpdateText();
        if (pressFHintUI != null) pressFHintUI.SetActive(true);
    }

    public void HidePrompt()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(false);
    }

    public string GetPrompt()
    {
        string p = (SettingsManager.currentLanguage == "VI") ? vietnamesePrompt : englishPrompt;
        if (string.IsNullOrEmpty(p)) return "";
        return p.Replace("[F] ", "").Replace("[F]", "").Trim();
    }

    public void UpdateText()
    {
        string prompt = GetPrompt();
        if (worldSpaceText != null) worldSpaceText.text = prompt;
    }
}
