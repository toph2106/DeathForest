using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TriggerEventTruck : MonoBehaviour
{
    public static TriggerEventTruck Instance { get; private set; }

    [Header("1. Setup Xe Truck (Kích Hoạt Khi ĐÃ ĐỌC GIẤY)")]
    public Truck truckScript;

    [Header("2. Khóa Trigger")]
    [Tooltip("Bật ô này: Nếu ĐÃ ĐỌC GIẤY -> Kích hoạt xe tông. Nếu CHƯA ĐỌC GIẤY -> Kích hoạt Ending 01!")]
    public bool requirePaperRead = true;

    [Header("3. Setup Ending 01 (Kích Hoạt Khi CHƯA ĐỌC GIẤY Quay Lại)")]
    [Tooltip("Kéo Panel Canvas/Ending01 vào đây")]
    public GameObject ending01Panel;

    [Tooltip("Kéo TextMeshProUGUI chứa chữ kết thúc Ending 01 vào đây để chạy hiệu ứng typewriter")]
    public TextMeshProUGUI endingTextUI;

    [TextArea(3, 10)]
    [Tooltip("Nội dung văn bản thoại của Ending 01")]
    public string endingTextContent = "Bạn đã quyết định rời khỏi khu rừng. Một quyết định cực kỳ thông minh.\nEnding 01: Đi về...";

    [Tooltip("Tốc độ hiện chữ Ending (Mặc định: 0.04s)")]
    public float textSpeed = 0.04f;

    [Header("4. Cấu Hình Auto-Walk 2s Trước Khi Fade Đen Ending 01")]
    [Tooltip("Kéo Target Object/Cube lơ lửng phía trước vào đây để khóa tầm nhìn mắt nhân vật")]
    public Transform forcedEndingLookTarget;

    [Tooltip("Tốc độ tự động bước đi bộ về phía trước (Mặc định: 2.0m/s)")]
    public float endingAutoWalkSpeed = 2.0f;

    [Tooltip("Thời gian tự động bước đi bộ trong 2 giây trước khi mờ đen màn hình (Mặc định: 2.0s)")]
    public float walkDurationBeforeFade = 2.0f;

    [Tooltip("Thời gian tự động TẮT ĐÈN PIN sau khi bắt đầu đi bộ về (Mặc định: 1.0s)")]
    public float turnOffFlashlightDelay = 1.0f;

    [Header("5. Ẩn UI Khác Khi Hiện Ending 01")]
    [Tooltip("Kéo các UI trong game (Camcorder, HUD, Crosshair...) vào đây để tự động ẩn khi hiện Ending.")]
    public GameObject[] uisToHideOnEnding;

    [Header("6. Chuyển Cảnh Về MainMenu")]
    [Tooltip("Tên Scene MainMenu để quay về (Mặc định: MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    private bool hasTriggered = false;
    private bool isEndingFinished = false;
    private bool isTypingText = false;
    private bool isReturningToTitle = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (ending01Panel != null) ending01Panel.SetActive(false);
    }

    void Update()
    {
        if (isEndingFinished && !isReturningToTitle)
        {
            // Bấm bất kỳ đâu trên màn hình hoặc phím bất kỳ -> Chạy Fade Out mờ đen rồi quay về MainMenu
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.anyKeyDown)
            {
                isReturningToTitle = true;
                StartCoroutine(FadeAndReturnToMainMenu());
            }
        }
        else if (isTypingText)
        {
            // Nếu chữ đang gõ mà người chơi nhấp chuột -> Hiện full câu chữ ngay lập tức
            if (Input.GetMouseButtonDown(0) && Time.time > 0.5f)
            {
                CompleteTextInstantly();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            hasTriggered = true;

            bool isPaperRead = ReadablePaper.HasReadPaper;
            Debug.Log($"[TriggerEventTruck] 🔍 Kiểm tra trạng thái đã đọc tờ giấy: {isPaperRead}");

            // TRƯỜNG HỢP A: NẾU ĐÃ ĐỌC GIẤY -> KÍCH HOẠT XE TÔNG KICKJUMP
            if (isPaperRead || !requirePaperRead)
            {
                Debug.Log("[TriggerEventTruck] 🚚 ĐÃ ĐỌC GIẤY! Kích hoạt xe tải tông...");
                if (truckScript != null)
                {
                    truckScript.StartTruckSequence();
                }
                else
                {
                    Debug.LogError("[TriggerEventTruck] ⚠️ CHƯA KÉO TRUCK (CAR) VÀO Ô TRUCK SCRIPT CỦA TRUCKTRIGGER!");
                }
            }
            // TRƯỜNG HỢP B: NẾU CHƯA ĐỌC GIẤY QUAY LẠI -> KÍCH HOẠT CHUỖI AUTO-WALK 2S RỒI FADE SANG ENDING 01!
            else
            {
                Debug.Log("[TriggerEventTruck] 🚫 CHƯA ĐỌC GIẤY QUAY LẠI -> Kích hoạt Ending 01!");
                StartCoroutine(Ending01Routine());
            }
        }
    }

    IEnumerator Ending01Routine()
    {
        // 1. KHÓA TẠM THỜI CHUỘT VÀ CHUYỂN SANG CHẾ ĐỘ AUTO-WALK
        MovePl playerMove = FindFirstObjectByType<MovePl>();
        CharacterController cc = FindFirstObjectByType<CharacterController>();
        Transform mainCamTransform = Camera.main != null ? Camera.main.transform : null;

        if (playerMove != null)
        {
            playerMove.isCameraLocked = true;
            playerMove.enabled = false;
        }

        // Bật đếm thời gian 1.0s tự động TẮT ĐÈN PIN
        StartCoroutine(AutoTurnOffFlashlightRoutine());

        // 2. CHO TỰ ĐỘNG BƯỚC ĐI BỘ VỀ PHÍA TRƯỚC VÀ KHÓA TẦM NHÌN TRONG 2.0 GIÂY
        float elapsed = 0f;
        while (elapsed < walkDurationBeforeFade)
        {
            elapsed += Time.deltaTime;

            // Di chuyển tự động về phía trước
            if (cc != null && cc.enabled && playerMove != null)
            {
                Vector3 moveDir = playerMove.transform.forward * endingAutoWalkSpeed;
                moveDir.y = -9.81f * Time.deltaTime;
                cc.Move(moveDir * Time.deltaTime);
            }

            // Xoay camera ghim vào forcedEndingLookTarget
            if (forcedEndingLookTarget != null && mainCamTransform != null)
            {
                Vector3 direction = (forcedEndingLookTarget.position - mainCamTransform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    mainCamTransform.rotation = Quaternion.Slerp(mainCamTransform.rotation, targetRotation, Time.deltaTime * 5f);
                }
            }

            yield return null;
        }

        // 3. FADE MÀN HÌNH TỐI ĐEN MƯỢT MÀ AN TOÀN (TỰ KÍCH HOẠT CHA CỦA FADEPANEL)
        Image fadeImg = GetFadeImage();
        if (fadeImg != null)
        {
            EnsureParentsActive(fadeImg);

            fadeImg.transform.SetAsLastSibling();
            fadeImg.gameObject.SetActive(true);
            fadeImg.raycastTarget = false;

            float fadeDur = 1.0f;
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

        // 4. ẨN MÁY QUAY CAMCORDER VÀ CÁC UI INGAME KHÁC
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

        // 5. BẬT PANEL ENDING 01
        if (ending01Panel != null)
        {
            ending01Panel.SetActive(true);
        }

        yield return new WaitForSeconds(0.3f);

        // 6. CHẠY HIỆU ỨNG CHỮ TYPEWRITER CỦA ENDING 01
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

    IEnumerator AutoTurnOffFlashlightRoutine()
    {
        yield return new WaitForSeconds(turnOffFlashlightDelay);
        FlashlightToggle flashlight = FindFirstObjectByType<FlashlightToggle>();
        if (flashlight != null)
        {
            flashlight.SetFlashlightState(false, true);
        }
    }

    void CompleteTextInstantly()
    {
        isTypingText = false;
        if (endingTextUI != null)
        {
            endingTextUI.text = endingTextContent;
        }

        // ĐÁNH DẤU CHO PHÉP BẤM MỌI NƠI ĐỂ QUAY VỀ MAIN MENU
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

        // FADE MỜ ĐEN CHUYỂN CẢNH VỀ TITLE AN TOÀN (BẢO ĐẢM PARENT DÒNG CHA ĐƯỢC BẬT ACTIVE)
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

        // Reset bộ đếm máy quay
        CamcorderUI.ResetTimer();

        // Nạp về MainMenu
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
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

    public void ResetTriggerState()
    {
        hasTriggered = false;
    }

    public static void ResetAllTriggers()
    {
        TriggerEventTruck[] triggers = FindObjectsByType<TriggerEventTruck>(FindObjectsSortMode.None);
        foreach (var t in triggers)
        {
            if (t != null) t.hasTriggered = false;
        }
    }
}