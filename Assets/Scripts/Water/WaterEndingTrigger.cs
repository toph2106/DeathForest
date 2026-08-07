using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WaterEndingTrigger : MonoBehaviour
{
    [Header("1. Thời Gian Cho Phép Ở Trong Nước")]
    [Tooltip("Thời gian (giây) đếm ngược khi lội xuống nước. Nếu quá số giây này mà không nhảy lên bờ -> Kích hoạt Ending! (Mặc định: 5s)")]
    public float drownTimeLimit = 5.0f;

    [Header("2. Cấu Hình Panel Water Ending")]
    [Tooltip("Kéo Panel UI Ending nước (hoặc Panel Canvas/Ending01) vào đây")]
    public GameObject waterEndingPanel;

    [Tooltip("Kéo TextMeshProUGUI dùng để hiển thị chữ thoại Ending nước vào đây")]
    public TextMeshProUGUI endingTextUI;

    [TextArea(3, 10)]
    [Tooltip("Nội dung câu thoại kết thúc khi bị chìm dưới nước")]
    public string endingTextContent = "Bộ đồ của bạn đã bị ướt và trở lên khó chịu, bạn hợp tức đi về.\nEnding: Sự Chìm Đắm.";

    [Tooltip("Tốc độ gõ chữ Typewriter (Mặc định: 0.04s)")]
    public float textSpeed = 0.04f;

    [Header("3. Kiểm Tra Độ Cao Mặt Nước (Chống Kích Hoạt Nhầm Khi Đứng Trên Bờ)")]
    [Tooltip("Bật ô này để chỉ khi Player thực sự bước chìm bên dưới độ cao mặt nước mới đếm ngược")]
    public bool useWaterHeightCheck = true;

    [Tooltip("Độ cao Y tối đa của mặt nước (Mặc định: 83.8). Nếu Player đứng ở độ cao Y lớn hơn số này (đang đứng trên bờ đất) -> Không đếm!")]
    public float maxWaterYLevel = 83.8f;

    [Header("4. Ẩn Các UI Khác Khi Hiện Ending")]
    [Tooltip("Kéo các UI ingame (Camcorder, HUD...) vào đây để tự động ẩn khi hiện Ending")]
    public GameObject[] uisToHideOnEnding;

    [Header("5. Chuyển Cảnh Về MainMenu")]
    [Tooltip("Tên Scene MainMenu để quay về (Mặc định: MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    private float currentWaterTime = 0f;
    private bool isPlayerInWater = false;
    private bool hasEndingTriggered = false;
    private bool isEndingFinished = false;
    private bool isTypingText = false;
    private bool isReturningToTitle = false;

    void Start()
    {
        if (waterEndingPanel != null) waterEndingPanel.SetActive(false);
    }

    void Update()
    {
        // 1. TÍNH TOÁN BỘ ĐẾM 5 GIÂY LIÊN TỤC KHI PLAYER ĐANG Ở TRONG NƯỚC THỰC SỰ
        if (isPlayerInWater && !hasEndingTriggered)
        {
            MovePl player = FindFirstObjectByType<MovePl>();

            // NẾU BẬT KIỂM TRA ĐỘ CAO MÀ PLAYER ĐANG ĐỨNG TRÊN BỜ (Y > 83.8) -> BỎ QUA KHÔNG ĐẾM NƯỚC!
            if (useWaterHeightCheck && player != null && player.transform.position.y > maxWaterYLevel)
            {
                currentWaterTime = 0f;
                return;
            }

            currentWaterTime += Time.deltaTime;

            if (currentWaterTime >= drownTimeLimit)
            {
                hasEndingTriggered = true;
                StartCoroutine(WaterEndingRoutine());
            }
        }

        // 2. XỬ LÝ CLICK QUAY VỀ MAIN MENU HOẶC BẢO BỎ QUA CHỮ TYPEWRITER
        if (isEndingFinished && !isReturningToTitle)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.anyKeyDown)
            {
                isReturningToTitle = true;
                StartCoroutine(FadeAndReturnToMainMenu());
            }
        }
        else if (isTypingText)
        {
            if (Input.GetMouseButtonDown(0) && Time.time > 0.5f)
            {
                CompleteTextInstantly();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggeredCheck(other))
        {
            isPlayerInWater = true;
            currentWaterTime = 0f;
            Debug.Log("[WaterEndingTrigger] 🌊 Player vừa lội xuống nước! Bắt đầu đếm ngược 5 giây...");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (hasTriggeredCheck(other))
        {
            if (!isPlayerInWater)
            {
                isPlayerInWater = true;
                currentWaterTime = 0f;
                Debug.Log("[WaterEndingTrigger] 🌊 Player đang ở trong nước! Bắt đầu đếm ngược 5 giây...");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (hasEndingTriggered) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            isPlayerInWater = false;
            currentWaterTime = 0f;
            Debug.Log("[WaterEndingTrigger] 🟢 Player đã nhảy thoát lên bờ an toàn! Hủy đếm ngược.");
        }
    }

    private bool hasTriggeredCheck(Collider other)
    {
        if (hasEndingTriggered) return false;
        return other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null;
    }

    IEnumerator WaterEndingRoutine()
    {
        Debug.Log("[WaterEndingTrigger] 💀 Đã quá 5 giây ở dưới nước! Kích hoạt Water Ending...");

        // 1. KHÓA BÀN PHÍM VÀ CHUỘT PLAYER
        MovePl playerMove = FindFirstObjectByType<MovePl>();
        CharacterController cc = FindFirstObjectByType<CharacterController>();

        if (playerMove != null)
        {
            playerMove.isCameraLocked = true;
            playerMove.enabled = false;
        }
        if (cc != null) cc.enabled = false;

        // 2. FADE MÀN HÌNH MỜ ĐEN MƯỢT MÀ
        Image fadeImg = GetFadeImage();
        if (fadeImg != null)
        {
            EnsureParentsActive(fadeImg);

            fadeImg.transform.SetAsLastSibling();
            fadeImg.gameObject.SetActive(true);
            fadeImg.raycastTarget = false;

            float fadeDur = 1.2f;
            float t = 0f;
            Color startC = new Color(0, 0, 0, 0f);
            Color targetC = new Color(0, 0, 0, 1f);

            while (t < fadeDur)
            {
                t += Time.unscaledDeltaTime;
                if (fadeImg != null) fadeImg.color = Color.Lerp(startC, targetC, t / fadeDur);
                yield return null;
            }
            if (fadeImg != null) fadeImg.color = targetC;
        }

        // 3. ẨN MÁY QUAY CAMCORDER VÀ CÁC UI INGAME KHÁC
        if (CamcorderUI.Instance != null)
        {
            CamcorderUI.Instance.gameObject.SetActive(false);
        }

        if (uisToHideOnEnding != null)
        {
            foreach (GameObject ui in uisToHideOnEnding)
            {
                if (ui != null) ui.SetActive(false);
            }
        }

        // 4. BẬT PANEL WATER ENDING
        if (waterEndingPanel != null)
        {
            waterEndingPanel.SetActive(true);
        }

        yield return new WaitForSeconds(0.3f);

        // 5. CHẠY HIỆU ỨNG CHỮ TYPEWRITER CỦA ENDING NƯỚC
        if (endingTextUI != null)
        {
            isTypingText = true;
            endingTextUI.text = "";

            for (int i = 0; i <= endingTextContent.Length; i++)
            {
                if (!isTypingText) break;
                endingTextUI.text = endingTextContent.Substring(0, i);
                yield return new WaitForSeconds(textSpeed);
            }
        }

        CompleteTextInstantly();
    }

    void CompleteTextInstantly()
    {
        isTypingText = false;
        if (endingTextUI != null)
        {
            endingTextUI.text = endingTextContent;
        }

        StartCoroutine(EnableClickToReturn());
    }

    IEnumerator EnableClickToReturn()
    {
        yield return new WaitForSeconds(0.3f);
        isEndingFinished = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    IEnumerator FadeAndReturnToMainMenu()
    {
        isEndingFinished = false;

        Image fadeImg = GetFadeImage();
        if (fadeImg != null)
        {
            EnsureParentsActive(fadeImg);

            fadeImg.transform.SetAsLastSibling();
            fadeImg.gameObject.SetActive(true);
            fadeImg.raycastTarget = true;

            float duration = 1.0f;
            float elapsed = 0f;
            Color startColor = new Color(0, 0, 0, 0f);
            Color targetColor = new Color(0, 0, 0, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (fadeImg != null)
                {
                    fadeImg.color = Color.Lerp(startColor, targetColor, elapsed / duration);
                }
                yield return null;
            }

            if (fadeImg != null) fadeImg.color = targetColor;
        }

        yield return new WaitForSecondsRealtime(0.2f);

        CamcorderUI.ResetTimer();
        SceneManager.LoadScene(mainMenuSceneName);
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
