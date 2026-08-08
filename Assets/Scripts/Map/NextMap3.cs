using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class NextMap3 : MonoBehaviour
{
    [Header("1. Chuyển Cảnh Mờ Đen (Fade To Black)")]
    [Tooltip("Tên Scene tiếp theo cần nạp (Mặc định: Map03)")]
    public string targetSceneName = "Map03";

    [Tooltip("Thời gian màn hình tối mờ dần ra đen (giây)")]
    public float fadeOutDuration = 1.5f;

    [Tooltip("Thời gian giữ màn hình tối đen ngòm hồi hộp (giây) - Mặc định: 2.0s")]
    public float darkDelay = 2.0f;

    [Header("2. Khóa Cổng Hầm (Bắt Buộc Đọc Giấy Mới Cho Qua Map 03)")]
    [Tooltip("Bật ô này: Bắt buộc người chơi phải nhặt đọc tờ giấy trước mới cho phép tiến vào Map 03!")]
    public bool requirePaperRead = true;

    [Header("3. Âm Thanh Chuyển Cảnh (Tùy Chọn - Để Trống Vẫn Chạy Mượt 100%)")]
    [Tooltip("Tùy chọn: Kéo file âm thanh nếu muốn phát lúc vào hầm (Để trống nếu không dùng âm thanh)")]
    public AudioClip tunnelSoundClip;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng tối đa của âm thanh (nếu có gán)")]
    public float maxSoundVolume = 0.8f;

    [Tooltip("Thời gian âm thanh vang to dần lên (giây)")]
    public float soundFadeInDuration = 2.0f;

    private AudioSource audioSource;
    private bool isTransitioning = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 0f; // 2D sound nghe nét 100%
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            // NẾU BẬT YÊU CẦU ĐỌC GIẤY MÀ NGUỜI CHƠI CHƯA ĐỌC GIẤY -> KHÓA CỔNG HẦM, KHÔNG CHO QUA MAP 03!
            if (requirePaperRead && !ReadablePaper.HasReadPaper)
            {
                Debug.Log("[NextMap3] ⚠️ Cổng hầm đã bị khóa! Bạn phải nhặt đọc tờ giấy trước mới được tiến vào Map 03!");
                return;
            }

            StartCoroutine(MapTransitionRoutine());
        }
    }

    IEnumerator MapTransitionRoutine()
    {
        isTransitioning = true;

        float targetVolume = maxSoundVolume > 0f ? maxSoundVolume : 0.8f;

        // 1. KHÓA TẠM THỜI DI CHUYỂN VÀ CON TRỎ CHUỘT PLAYER
        MovePl playerMove = FindFirstObjectByType<MovePl>();
        if (playerMove != null) playerMove.enabled = false;

        CharacterController playerController = FindFirstObjectByType<CharacterController>();
        if (playerController != null) playerController.enabled = false;

        // 2. PHÁT ÂM THANH (NẾU CÓ GÁN - NẾU ĐỂ TRỐNG TỰ BỎ QUA)
        if (tunnelSoundClip != null && audioSource != null)
        {
            audioSource.clip = tunnelSoundClip;
            audioSource.volume = 0f;
            audioSource.Play();
            StartCoroutine(FadeInAudioRoutine(targetVolume, soundFadeInDuration));
        }

        // 3. TỐI ĐEN HOÀN TOÀN MÀN HÌNH (FADE TO PITCH BLACK)
        Image fadePanel = GetFadeImage();
        if (fadePanel != null)
        {
            PauseMenuManager.BringFadeToFront(fadePanel);
            PauseMenuManager.SetInGameHUDActive(false);

            float t = 0f;
            Color startC = new Color(0, 0, 0, 0f);
            Color targetC = new Color(0, 0, 0, 1f);

            while (t < fadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                if (fadePanel != null) fadePanel.color = Color.Lerp(startC, targetC, t / fadeOutDuration);
                yield return null;
            }
            if (fadePanel != null) fadePanel.color = targetC;
        }

        // 4. GIỮ MÀN HÌNH TỐI ĐEN HOÀN TOÀN TRONG 2.0 GIÂY HỒI HỘP
        yield return new WaitForSecondsRealtime(darkDelay);

        // 5. TỰ ĐỘNG LƯU TIẾN TRÌNH: Mở khóa Map 03 khi qua màn Map 02
        GameSaveManager.UnlockLevel(3);

        // 6. CHUYỂN SANG MAP 03
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    IEnumerator FadeInAudioRoutine(float targetVol, float duration)
    {
        float timer = 0f;
        while (timer < duration && audioSource != null)
        {
            timer += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVol, timer / duration);
            yield return null;
        }
        if (audioSource != null) audioSource.volume = targetVol;
    }

    private void EnsureParentsActive(Image img)
    {
        if (img == null) return;
        Transform curr = img.transform.parent;
        while (curr != null)
        {
            curr.gameObject.SetActive(true);
            curr = curr.parent;
        }
    }

    private Image GetFadeImage()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.fadePanel != null)
        {
            return PauseMenuManager.Instance.fadePanel;
        }

        Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Image img in images)
        {
            if (img.gameObject.name.Contains("FadePanel") || img.gameObject.name.Contains("Fade"))
            {
                return img;
            }
        }

        return null;
    }
}