using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Loading Screen (Tùy chọn)")]
    [Tooltip("Kéo 1 cái Panel/Canvas đen toàn màn hình vào đây để làm màn hình Loading")]
    public CanvasGroup loadingScreen;

    [Tooltip("Thời gian Fade In/Out (giây)")]
    public float fadeDuration = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ẩn màn hình loading lúc đầu
        if (loadingScreen != null)
        {
            loadingScreen.alpha = 0f;
            loadingScreen.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gọi hàm này thay vì SceneManager.LoadScene() để chuyển scene mượt mà
    /// Ví dụ: SceneLoader.Instance.LoadSceneAsync("Map02");
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // === BƯỚC 1: Fade màn hình ra đen ===
        if (loadingScreen != null)
        {
            loadingScreen.gameObject.SetActive(true);
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                loadingScreen.alpha = Mathf.Clamp01(timer / fadeDuration);
                yield return null;
            }
            loadingScreen.alpha = 1f;
        }

        // === BƯỚC 2: Load scene mới ở background (KHÔNG bị giật!) ===
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // Giữ lại, chưa cho hiện scene mới

        // Đợi scene load xong (0.9 = đã load xong, chỉ chờ lệnh kích hoạt)
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // === BƯỚC 3: Kích hoạt scene mới ===
        asyncLoad.allowSceneActivation = true;

        // Đợi scene mới thực sự được kích hoạt
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // === BƯỚC 4: Fade từ đen trở lại trong suốt ===
        if (loadingScreen != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                loadingScreen.alpha = Mathf.Clamp01(1f - (timer / fadeDuration));
                yield return null;
            }
            loadingScreen.alpha = 0f;
            loadingScreen.gameObject.SetActive(false);
        }
    }
}
