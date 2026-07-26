using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class SettingsManager : MonoBehaviour
{
    [Header("1. Master Volume")]
    public Slider masterVolumeSlider;
    public TMP_Text masterVolumeText;

    [Header("2. Brightness (Độ sáng)")]
    public Slider brightnessSlider;
    public TMP_Text brightnessText;
    public Volume globalVolume;

    [Header("3. Mouse Sensitivity")]
    public Slider sensitivitySlider;
    public TMP_Text sensitivityText;
    public static float mouseSensitivity = 1.5f;

    [Header("4. Fullscreen")]
    public Toggle fullscreenToggle;
    public TMP_Text fullscreenStatusText;

    [Header("5. Language (Ngôn ngữ)")]
    public TMP_Text languageText;
    public static string currentLanguage = "VI"; // "EN" hoặc "VI" (Tự động đọc từ PlayerPrefs)

    [Header("6. Âm thanh Cài đặt (SFX)")]
    [Tooltip("Kéo tiếng click ô Toggle hoặc âm thanh tùy chọn vào đây")]
    public AudioClip toggleSound;
    public AudioSource sfxAudioSource;

    public delegate void OnLanguageChanged();
    public static event OnLanguageChanged onLanguageChanged;

    private ColorAdjustments colorAdjustments;

    // TỰ ĐỘNG NẠP ĐÚNG NGÔN NGỮ ĐÃ LƯU TỪ PLAYERPREFS NGAY KHI SCENE NẠP (KỂ CẢ KHI SETTINGS PANEL ĐANG BỊ TẮT / INACTIVE)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeLanguageBeforeSceneLoad()
    {
        currentLanguage = PlayerPrefs.GetString("Language", "VI");
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.5f);
    }

    public static void RefreshLanguageState()
    {
        currentLanguage = PlayerPrefs.GetString("Language", "VI");
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.5f);

        if (onLanguageChanged != null)
        {
            onLanguageChanged.Invoke();
        }
    }

    void Awake()
    {
        currentLanguage = PlayerPrefs.GetString("Language", "VI");
    }

    IEnumerator Start()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out colorAdjustments);
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        LoadSettings();
        SetupListeners();

        // CHỜ 1 FRAME CHO TẤT CẢ UI KHÁC TRONG SCENE ĐĂNG KÝ XONG LẮNG NGHE SỰ KIỆN ĐỔI NGÔN NGỮ
        yield return null;
        if (onLanguageChanged != null)
        {
            onLanguageChanged.Invoke();
        }
    }

    void SetupListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(SetBrightness);

        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    // --- 1. MASTER VOLUME ---
    public void SetMasterVolume(float value)
    {
        float normalized = (masterVolumeSlider != null && masterVolumeSlider.maxValue > 1f) ? value / masterVolumeSlider.maxValue : value;
        
        AudioListener.volume = normalized;
        if (masterVolumeText != null)
            masterVolumeText.text = Mathf.RoundToInt(normalized * 100f) + "%";

        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    // --- 2. BRIGHTNESS ---
    public void SetBrightness(float value)
    {
        float normalized = (brightnessSlider != null && brightnessSlider.maxValue > 1f) ? value / brightnessSlider.maxValue : value;

        if (brightnessText != null)
            brightnessText.text = Mathf.RoundToInt(normalized * 100f) + "%";

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = Mathf.Lerp(-1.5f, 1.5f, normalized);
        }

        PlayerPrefs.SetFloat("Brightness", value);
    }

    // --- 3. SENSITIVITY ---
    public void SetSensitivity(float value)
    {
        mouseSensitivity = value;
        if (sensitivityText != null)
            sensitivityText.text = value.ToString("F1");

        PlayerPrefs.SetFloat("MouseSensitivity", value);
    }

    // --- 4. FULLSCREEN ---
    public void SetFullscreen(bool isFull)
    {
        Screen.fullScreen = isFull;
        UpdateFullscreenText(isFull);
        PlayerPrefs.SetInt("Fullscreen", isFull ? 1 : 0);

        if (toggleSound != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(toggleSound);
        }
    }

    void UpdateFullscreenText(bool isFull)
    {
        if (fullscreenStatusText != null)
        {
            if (currentLanguage == "VI")
            {
                fullscreenStatusText.text = isFull ? "Bật" : "Tắt";
            }
            else
            {
                fullscreenStatusText.text = isFull ? "On" : "Off";
            }
        }
    }

    // --- 5. LANGUAGE ---
    public void NextLanguage()
    {
        ToggleLanguage();
    }

    public void PrevLanguage()
    {
        ToggleLanguage();
    }

    private void ToggleLanguage()
    {
        if (currentLanguage == "EN")
        {
            currentLanguage = "VI";
        }
        else
        {
            currentLanguage = "EN";
        }

        Debug.Log($"[SettingsManager] Đã đổi Ngôn ngữ thành công sang: {currentLanguage}");

        UpdateLanguageUI();
        PlayerPrefs.SetString("Language", currentLanguage);
        PlayerPrefs.Save();

        if (onLanguageChanged != null)
            onLanguageChanged.Invoke();
    }

    void UpdateLanguageUI()
    {
        if (languageText != null)
        {
            string targetText = (currentLanguage == "EN") ? "English" : "Tiếng Việt";
            languageText.text = targetText;

            // Xử lý đồng bộ nếu object có gắn thêm script LocalizedText
            LocalizedText loc = languageText.GetComponent<LocalizedText>();
            if (loc != null)
            {
                loc.englishText = "English";
                loc.vietnameseText = "Tiếng Việt";
                loc.RefreshText();
            }

            languageText.SetAllDirty();
        }

        if (fullscreenToggle != null)
        {
            UpdateFullscreenText(fullscreenToggle.isOn);
        }
    }

    void LoadSettings()
    {
        float savedVol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        if (masterVolumeSlider != null) masterVolumeSlider.value = savedVol;
        SetMasterVolume(savedVol);

        float savedBright = PlayerPrefs.GetFloat("Brightness", 0.5f);
        if (brightnessSlider != null) brightnessSlider.value = savedBright;
        SetBrightness(savedBright);

        float savedSens = PlayerPrefs.GetFloat("MouseSensitivity", 1.5f);
        if (sensitivitySlider != null) sensitivitySlider.value = savedSens;
        SetSensitivity(savedSens);

        bool savedFull = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        if (fullscreenToggle != null) fullscreenToggle.isOn = savedFull;
        SetFullscreen(savedFull);

        // Đọc chính xác ngôn ngữ đã lưu từ trước (Mặc định "VI" nếu chưa lưu bao giờ)
        currentLanguage = PlayerPrefs.GetString("Language", "VI");
        UpdateLanguageUI();
        if (onLanguageChanged != null) onLanguageChanged.Invoke();
    }
}
