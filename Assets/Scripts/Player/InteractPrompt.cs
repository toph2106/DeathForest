using UnityEngine;
using TMPro;

public class InteractPrompt : MonoBehaviour
{
    [Header("1. Chữ Nhắc Phím Tương Tác Song Ngữ")]
    public string englishPrompt = "[F] Use Computer";
    public string vietnamesePrompt = "[F] Xem máy tính";

    [Header("2. Canvas Chữ F Bay Lơ Lửng (PressF World Space UI - Tùy chọn)")]
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
        return (SettingsManager.currentLanguage == "VI") ? vietnamesePrompt : englishPrompt;
    }

    public void UpdateText()
    {
        string prompt = GetPrompt();
        if (worldSpaceText != null) worldSpaceText.text = prompt;
    }
}
