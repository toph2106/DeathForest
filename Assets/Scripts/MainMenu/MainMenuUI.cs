using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MainMenuUI : MonoBehaviour
{
    public GameObject menuPanel;
    public GameObject settingsPanel;
    public GameObject continuePanel;

    [Tooltip("Kéo cái Logo Death Forest vào đây (để tự động ẩn mượt khi mở Settings / Continue)")]
    public GameObject logoObject;

    [Header("1. Chuyển cảnh khi MỞ MENU (Boot / Từ Game trở về)")]
    [Tooltip("Khoảng thời gian giữ màn hình đen ngòm lúc vừa vào Menu (giây)")]
    public float openMenuBlackPause = 0.8f;

    [Tooltip("Tốc độ màn hình từ đen mở sáng ra (giây) - Chỉnh số lớn hơn để mờ chậm mượt hơn")]
    public float openMenuFadeDuration = 2.0f;

    [Header("2. Chuyển cảnh khi BẤM PLAY / QUIT")]
    [Tooltip("Kéo cái FadePanel (Image đen phủ toàn màn hình) vào đây")]
    public Image fadePanel;

    [Tooltip("Tên Scene/Map muốn nạp khi bấm Play (Mặc định: Map01)")]
    public string defaultMapName = "Map01";

    [Tooltip("Thời gian mờ màn hình thành đen khi bấm Play/Quit (giây)")]
    public float fadeDuration = 1.5f;

    [Tooltip("Khoảng thời gian chờ đen hoàn toàn trước khi nạp Scene (giây)")]
    public float waitBeforeLoad = 0.5f;

    [Header("3. Nhạc nền (BGM)")]
    [Tooltip("Kéo AudioSource nhạc nền chính vào đây")]
    public AudioSource bgmAudio;

    [Tooltip("Danh sách các AudioSource nhạc nền phối thêm")]
    public AudioSource[] bgmAudios;

    [Header("4. Âm thanh Hiệu ứng (SFX Chuyển cảnh)")]
    [Tooltip("Kéo file âm thanh mở camera / Whoosh khi mới vào game hoặc MỞ/ĐÓNG Settings vào đây")]
    public AudioClip transitionSound;

    [Header("5. Glitch Effect (Tuỳ chọn cho Settings/Continue Transition)")]
    [Tooltip("Kéo Global Volume vào đây để tạo hiệu ứng nhói màu khi mở/đóng Panel")]
    public Volume globalVolume;

    private bool isTransitioning = false;
    private CanvasGroup menuCanvasGroup;
    private CanvasGroup settingsCanvasGroup;
    private CanvasGroup continueCanvasGroup;
    private CanvasGroup logoCanvasGroup;

    private RectTransform menuRect;
    private RectTransform settingsRect;
    private RectTransform continueRect;
    private AudioSource sfxAudioSource;

    private Vector2 menuOriginalPos;
    private Vector2 settingsOriginalPos;
    private Vector2 continueOriginalPos;
    private ChromaticAberration chromaticAberration;

    void Awake()
    {
        SetupPanelComponents();
    }

    void SetupPanelComponents()
    {
        if (menuPanel != null)
        {
            menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();
            if (menuCanvasGroup == null) menuCanvasGroup = menuPanel.AddComponent<CanvasGroup>();
            menuRect = menuPanel.GetComponent<RectTransform>();
            if (menuRect != null) menuOriginalPos = menuRect.anchoredPosition;
        }

        if (settingsPanel != null)
        {
            settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (settingsCanvasGroup == null) settingsCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
            settingsRect = settingsPanel.GetComponent<RectTransform>();
            if (settingsRect != null) settingsOriginalPos = settingsRect.anchoredPosition;
        }

        if (continuePanel != null)
        {
            continueCanvasGroup = continuePanel.GetComponent<CanvasGroup>();
            if (continueCanvasGroup == null) continueCanvasGroup = continuePanel.AddComponent<CanvasGroup>();
            continueRect = continuePanel.GetComponent<RectTransform>();
            if (continueRect != null) continueOriginalPos = continueRect.anchoredPosition;
        }

        if (logoObject != null)
        {
            logoCanvasGroup = logoObject.GetComponent<CanvasGroup>();
            if (logoCanvasGroup == null) logoCanvasGroup = logoObject.AddComponent<CanvasGroup>();
        }

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out chromaticAberration);
        }

        sfxAudioSource = GetComponent<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        StartCoroutine(CinematicGameBootSequence());
    }

    IEnumerator CinematicGameBootSequence()
    {
        if (fadePanel != null)
        {
            // TỰ ĐỘNG ĐƯA FADEPANEL LÊN TRÊN CÙNG CANVAS
            fadePanel.transform.SetAsLastSibling();

            fadePanel.gameObject.SetActive(true);
            fadePanel.color = new Color(0, 0, 0, 1);
            fadePanel.raycastTarget = true;

            SetAllBGMVolume(0f);

            yield return new WaitForSecondsRealtime(openMenuBlackPause);

            PlayTransitionSFX();
            FadeInAllBGM(openMenuFadeDuration);

            fadePanel.DOFade(0f, openMenuFadeDuration).SetEase(Ease.InOutQuad).SetUpdate(true).OnComplete(() =>
            {
                fadePanel.raycastTarget = false;
                fadePanel.gameObject.SetActive(false);
            });
        }
    }

    void SetAllBGMVolume(float volume)
    {
        if (bgmAudio != null) bgmAudio.volume = volume;
        if (bgmAudios != null)
        {
            foreach (var audio in bgmAudios)
            {
                if (audio != null) audio.volume = volume;
            }
        }
    }

    void FadeInAllBGM(float duration)
    {
        if (bgmAudio != null) bgmAudio.DOFade(0.4f, duration).SetUpdate(true);
        if (bgmAudios != null)
        {
            foreach (var audio in bgmAudios)
            {
                if (audio != null) audio.DOFade(0.4f, duration).SetUpdate(true);
            }
        }
    }

    public void PlayGame()
    {
        Debug.Log("[MainMenuUI] Nút Play đã được bấm!");
        if (isTransitioning) return;
        StartCoroutine(FadeAndLoad(defaultMapName));
    }

    // --- MỜ VÀO MAP CHỈ ĐỊNH (Bấm Map 01, Map 02, Map 03) ---
    public void LoadMap1()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeAndLoad("Map01"));
    }

    public void LoadMap2()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeAndLoad("Map02"));
    }

    public void LoadMap3()
    {
        if (isTransitioning) return;
        StartCoroutine(FadeAndLoad("Map03"));
    }

    // --- BẢNG CONTINUE (TIẾP TỤC / CHỌN MAP) ---
    public void OpenContinue()
    {
        if (isTransitioning) return;
        StartCoroutine(AnimateOpenContinue());
    }

    public void CloseContinue()
    {
        if (isTransitioning) return;
        StartCoroutine(AnimateCloseContinue());
    }

    IEnumerator AnimateOpenContinue()
    {
        isTransitioning = true;

        PlayTransitionSFX();
        TriggerGlitchSpike();

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.DOFade(0f, 0.18f).SetUpdate(true);
            if (menuRect != null) menuRect.DOAnchorPosX(menuOriginalPos.x - 40f, 0.18f).SetUpdate(true);
        }

        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.DOFade(0f, 0.18f).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.15f);

        if (continuePanel != null)
        {
            continuePanel.SetActive(true);
            if (continueCanvasGroup != null) continueCanvasGroup.alpha = 0f;
            if (continueRect != null) continueRect.anchoredPosition = continueOriginalPos + new Vector2(0, -30f);

            if (continueCanvasGroup != null) continueCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
            if (continueRect != null) continueRect.DOAnchorPos(continueOriginalPos, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.25f);

        if (menuPanel != null) menuPanel.SetActive(false);
        if (logoObject != null) logoObject.SetActive(false);
        isTransitioning = false;
    }

    IEnumerator AnimateCloseContinue()
    {
        isTransitioning = true;

        PlayTransitionSFX();
        TriggerGlitchSpike();

        if (continueCanvasGroup != null)
        {
            continueCanvasGroup.DOFade(0f, 0.18f).SetUpdate(true);
            if (continueRect != null) continueRect.DOAnchorPosY(continueOriginalPos.y - 30f, 0.18f).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.15f);

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            if (menuCanvasGroup != null) menuCanvasGroup.alpha = 0f;
            if (menuRect != null) menuRect.anchoredPosition = menuOriginalPos + new Vector2(-40f, 0);

            if (menuCanvasGroup != null) menuCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
            if (menuRect != null) menuRect.DOAnchorPos(menuOriginalPos, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        if (logoObject != null)
        {
            logoObject.SetActive(true);
            if (logoCanvasGroup != null) logoCanvasGroup.alpha = 0f;
            if (logoCanvasGroup != null) logoCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.25f);

        if (continuePanel != null) continuePanel.SetActive(false);
        isTransitioning = false;
    }

    // --- BẢNG SETTINGS (CAI DAT) ---
    public void OpenSettings()
    {
        if (isTransitioning) return;
        StartCoroutine(AnimateOpenSettings());
    }

    public void CloseSettings()
    {
        if (isTransitioning) return;
        StartCoroutine(AnimateCloseSettings());
    }

    IEnumerator AnimateOpenSettings()
    {
        isTransitioning = true;

        PlayTransitionSFX();
        TriggerGlitchSpike();

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.DOFade(0f, 0.18f).SetUpdate(true);
            if (menuRect != null) menuRect.DOAnchorPosX(menuOriginalPos.x - 40f, 0.18f).SetUpdate(true);
        }

        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.DOFade(0f, 0.18f).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.15f);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            if (settingsCanvasGroup != null) settingsCanvasGroup.alpha = 0f;
            if (settingsRect != null) settingsRect.anchoredPosition = settingsOriginalPos + new Vector2(0, -30f);

            if (settingsCanvasGroup != null) settingsCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
            if (settingsRect != null) settingsRect.DOAnchorPos(settingsOriginalPos, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.25f);

        if (menuPanel != null) menuPanel.SetActive(false);
        if (logoObject != null) logoObject.SetActive(false);
        isTransitioning = false;
    }

    IEnumerator AnimateCloseSettings()
    {
        isTransitioning = true;

        PlayTransitionSFX();
        TriggerGlitchSpike();

        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.DOFade(0f, 0.18f).SetUpdate(true);
            if (settingsRect != null) settingsRect.DOAnchorPosY(settingsOriginalPos.y - 30f, 0.18f).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.15f);

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            if (menuCanvasGroup != null) menuCanvasGroup.alpha = 0f;
            if (menuRect != null) menuRect.anchoredPosition = menuOriginalPos + new Vector2(-40f, 0);

            if (menuCanvasGroup != null) menuCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
            if (menuRect != null) menuRect.DOAnchorPos(menuOriginalPos, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        if (logoObject != null)
        {
            logoObject.SetActive(true);
            if (logoCanvasGroup != null) logoCanvasGroup.alpha = 0f;
            if (logoCanvasGroup != null) logoCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.25f);

        if (settingsPanel != null) settingsPanel.SetActive(false);
        isTransitioning = false;
    }

    void PlayTransitionSFX()
    {
        if (transitionSound != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(transitionSound);
        }
    }

    void TriggerGlitchSpike()
    {
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.overrideState = true;
            DOTween.To(() => chromaticAberration.intensity.value, x => chromaticAberration.intensity.value = x, 1.0f, 0.08f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    DOTween.To(() => chromaticAberration.intensity.value, x => chromaticAberration.intensity.value = x, 0.3f, 0.12f).SetUpdate(true);
                });
        }
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenuUI] Nút Quit đã được bấm!");
        if (isTransitioning) return;
        StartCoroutine(FadeAndQuit());
    }

    void FadeOutAllBGM()
    {
        if (bgmAudio != null)
        {
            bgmAudio.DOFade(0f, fadeDuration).SetUpdate(true);
        }

        if (bgmAudios != null && bgmAudios.Length > 0)
        {
            foreach (var audioSrc in bgmAudios)
            {
                if (audioSrc != null)
                {
                    audioSrc.DOFade(0f, fadeDuration).SetUpdate(true);
                }
            }
        }
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        isTransitioning = true;

        if (fadePanel != null)
        {
            // TỰ ĐỘNG ĐƯA FADEPANEL LÊN TRÊN CÙNG CANVAS ĐỂ PHỦ BẢNG CONTINUE VÀ TẤT CẢ PANEL
            fadePanel.transform.SetAsLastSibling();

            fadePanel.gameObject.SetActive(true);
            fadePanel.color = new Color(0, 0, 0, 0);
            fadePanel.raycastTarget = true;

            FadeOutAllBGM();

            fadePanel.DOFade(1f, fadeDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(fadeDuration);
            yield return new WaitForSecondsRealtime(waitBeforeLoad);
        }

        Debug.Log("[MainMenuUI] Đang nạp Scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeAndQuit()
    {
        isTransitioning = true;

        if (fadePanel != null)
        {
            fadePanel.transform.SetAsLastSibling();

            fadePanel.gameObject.SetActive(true);
            fadePanel.color = new Color(0, 0, 0, 0);
            fadePanel.raycastTarget = true;

            FadeOutAllBGM();

            fadePanel.DOFade(1f, fadeDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(fadeDuration + waitBeforeLoad);
        }

        Debug.Log("[MainMenuUI] Quit Game!");
        Application.Quit();
    }
}