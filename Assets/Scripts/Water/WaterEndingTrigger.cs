using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.Animations;
using TMPro;
using System.Collections;

public class WaterEndingTrigger : MonoBehaviour
{
    [Header("1. Thời Gian Cho Phép Ở Trong Nước")]
    [Tooltip("Thời gian (giây) đếm ngược khi lội xuống nước. Nếu quá số giây này -> Kích hoạt UMA trồi lên tấn công! (Mặc định: 5s)")]
    public float drownTimeLimit = 5.0f;

    [Header("2. Kiểm Tra Độ Cao Mặt Nước")]
    [Tooltip("Bật ô này để chỉ khi Player thực sự bước chìm bên dưới độ cao mặt nước mới đếm ngược")]
    public bool useWaterHeightCheck = true;

    [Tooltip("Độ cao Y tối đa của mặt nước (Mặc định: 83.8). Nếu Player đứng ở độ cao Y lớn hơn số này -> Không đếm!")]
    public float maxWaterYLevel = 83.8f;

    [Header("3. Cấu Hình Quái Vật UMA")]
    [Tooltip("Kéo GameObject death_forest_-_uma trong Scene vào đây")]
    public GameObject umaObject;

    [Tooltip("Animator của UMA (Tự động lấy từ umaObject nếu để trống)")]
    public Animator umaAnimator;

    [Header("4. Animation Clips Của UMA (Kéo Từ File GLB Vào)")]
    [Tooltip("Animation lúc ngóc đầu ngoi từ từ dưới nước lên (walk_normal)")]
    public AnimationClip walkClip;

    [Tooltip("Animation lúc phi thẳng vào người chơi (dash)")]
    public AnimationClip dashClip;

    [Tooltip("Animation lúc áp sát tóm lấy người chơi (END)")]
    public AnimationClip endGrabClip;

    [Header("5. Cấu Hình Khoảng Cách & Tốc Độ Trồi Lên")]
    [Tooltip("Khoảng cách UMA ngoi lên trước mặt người chơi (mét - Mặc định: 5.5m)")]
    public float emergeDistance = 5.5f;

    [Tooltip("Độ sâu bắt đầu ngóc đầu dưới nước (mét - Mặc định: 1.8m)")]
    public float submergeDepth = 1.8f;

    [Tooltip("Thời gian từ từ ngóc đầu ngoi lên khỏi mặt nước (giây - Mặc định: 2.2s)")]
    public float emergeDuration = 2.2f;

    [Tooltip("Thời gian đứng im nhìn chằm chằm người chơi trước khi phi (giây - Mặc định: 0.6s)")]
    public float stareDuration = 0.6f;

    [Tooltip("Tốc độ phi vào người chơi (m/s - Mặc định: 16.0m/s)")]
    public float dashSpeed = 16.0f;

    [Tooltip("Thời gian giữ cảnh tóm lấy bóp nghẹt mặt (giây - Mặc định: 1.5s)")]
    public float grabHoldDuration = 1.5f;

    [Header("6. Âm Thanh Jumpscare")]
    public AudioSource audioSource;
    public AudioClip emergeSound;
    public AudioClip dashScreamSound;
    public AudioClip grabJumpscareSound;

    [Header("7. Cấu Hình Endgame (Bị Tóm -> Hiện Lý Do -> Click Về Menu)")]
    [Tooltip("Kéo Panel UI Ending nước (hoặc Panel Canvas/Ending01) vào đây")]
    public GameObject waterEndingPanel;

    [Tooltip("Kéo TextMeshProUGUI dùng để hiển thị chữ thoại Ending nước vào đây")]
    public TextMeshProUGUI endingTextUI;

    [TextArea(3, 10)]
    [Tooltip("Nội dung câu thoại kết thúc khi bị UMA tóm dưới nước (Tiếng Việt)")]
    public string endingTextContentVI = "Bạn đã ở dưới nước quá lâu và bị quái vật UMA lôi xuống đáy hồ...\nEnding: Sự Chìm Đắm.";

    [TextArea(3, 10)]
    [Tooltip("Nội dung câu thoại kết thúc khi bị UMA tóm dưới nước (Tiếng Anh)")]
    public string endingTextContentEN = "You stayed underwater for too long and were dragged down by UMA...\nEnding: Drowned in the Depths.";

    [Tooltip("Tốc độ gõ chữ Typewriter (Mặc định: 0.04s)")]
    public float textSpeed = 0.04f;

    [Header("8. Ẩn Các UI Khác Khi Hiện Ending")]
    [Tooltip("Kéo các UI ingame (Camcorder, HUD...) vào đây để tự động ẩn khi hiện Ending")]
    public GameObject[] uisToHideOnEnding;

    [Header("9. Chuyển Cảnh Về MainMenu")]
    [Tooltip("Tên Scene MainMenu để quay về (Mặc định: MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    private float currentWaterTime = 0f;
    private bool isPlayerInWater = false;
    private bool hasAttackTriggered = false;
    private bool isEndingFinished = false;
    private bool isTypingText = false;
    private bool isReturningToTitle = false;
    private PlayableGraph playableGraph;

    void Start()
    {
        if (umaObject != null)
        {
            umaObject.SetActive(false);
            if (umaAnimator == null) umaAnimator = umaObject.GetComponentInChildren<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (waterEndingPanel != null)
        {
            waterEndingPanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }

    void Update()
    {
        // 1. Đếm thời gian ở dưới nước
        if (isPlayerInWater && !hasAttackTriggered)
        {
            MovePl player = FindFirstObjectByType<MovePl>();

            if (useWaterHeightCheck && player != null && player.transform.position.y > maxWaterYLevel)
            {
                currentWaterTime = 0f;
                return;
            }

            currentWaterTime += Time.deltaTime;

            if (currentWaterTime >= drownTimeLimit)
            {
                hasAttackTriggered = true;
                StartCoroutine(UmaAttackSequenceRoutine());
            }
        }

        // 2. Xử lý click chuột quay về MainMenu sau khi chữ Endgame hiện xong
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
        if (hasAttackTriggered) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null || other.GetComponent<CharacterController>() != null)
        {
            isPlayerInWater = true;
            currentWaterTime = 0f;
            Debug.Log("[WaterEndingTrigger] 🌊 Player vừa lội xuống nước! Đếm ngược 5 giây trước khi UMA trồi lên...");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (hasAttackTriggered) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null || other.GetComponent<CharacterController>() != null)
        {
            if (!isPlayerInWater)
            {
                isPlayerInWater = true;
                currentWaterTime = 0f;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (hasAttackTriggered) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null || other.GetComponent<CharacterController>() != null)
        {
            isPlayerInWater = false;
            currentWaterTime = 0f;
            Debug.Log("[WaterEndingTrigger] 🟢 Player đã lên bờ an toàn! Hủy đếm ngược.");
        }
    }

    private void PlayAnimation(AnimationClip clip, string fallbackState)
    {
        if (umaAnimator == null) return;

        if (clip != null)
        {
            if (playableGraph.IsValid()) playableGraph.Destroy();
            AnimationPlayableUtilities.PlayClip(umaAnimator, clip, out playableGraph);
            return;
        }

        if (!string.IsNullOrEmpty(fallbackState))
        {
            umaAnimator.Play(fallbackState);
        }
    }

    private IEnumerator UmaAttackSequenceRoutine()
    {
        Debug.Log("[WaterEndingTrigger] ⚠️ Ở dưới nước quá 5s! Bắt đầu chuỗi UMA trồi lên tấn công!");

        MovePl playerMove = FindFirstObjectByType<MovePl>();
        CharacterController cc = (playerMove != null) ? playerMove.GetComponent<CharacterController>() : FindFirstObjectByType<CharacterController>();
        Transform mainCam = (playerMove != null && playerMove.cameraTransform != null) ? playerMove.cameraTransform : (Camera.main != null ? Camera.main.transform : null);

        // 1. Khóa di chuyển và khóa góc quay của người chơi
        if (playerMove != null)
        {
            playerMove.isCameraLocked = true;
            playerMove.SetMovementState(false);
            playerMove.enabled = true;
        }
        if (cc != null) cc.enabled = true;

        if (umaObject == null)
        {
            umaObject = GameObject.Find("death_forest_-_uma");
        }
        if (umaObject == null)
        {
            Debug.LogError("[WaterEndingTrigger] ❌ Không tìm thấy GameObject death_forest_-_uma trong Scene!");
            yield break;
        }

        if (umaAnimator == null) umaAnimator = umaObject.GetComponentInChildren<Animator>();

        Vector3 playerPos = (playerMove != null) ? playerMove.transform.position : transform.position;
        Vector3 camForward = (mainCam != null) ? mainCam.forward : Vector3.forward;
        Vector3 flatForward = new Vector3(camForward.x, 0f, camForward.z).normalized;
        if (flatForward == Vector3.zero) flatForward = Vector3.forward;

        // Vị trí ngoi lên chuẩn trên mặt nước
        Vector3 targetEmergePos = playerPos + flatForward * emergeDistance;
        targetEmergePos.y = maxWaterYLevel - 0.4f;

        // Vị trí bắt đầu chìm dưới nước (ngóc mỗi nửa đầu lên)
        Vector3 startSubmergedPos = targetEmergePos - new Vector3(0f, submergeDepth, 0f);

        umaObject.transform.SetParent(null);
        umaObject.transform.position = startSubmergedPos;
        umaObject.transform.rotation = Quaternion.LookRotation((playerPos - targetEmergePos).normalized);
        umaObject.SetActive(true);

        // 2. Chạy Animation Walk và từ từ ngoi đầu lên khỏi mặt nước
        PlayAnimation(walkClip, "walk_normal");
        if (emergeSound != null && audioSource != null) audioSource.PlayOneShot(emergeSound);

        float elapsed = 0f;
        while (elapsed < emergeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / emergeDuration);
            umaObject.transform.position = Vector3.Lerp(startSubmergedPos, targetEmergePos, t);

            // Ghim mắt nhìn của người chơi vào đầu UMA
            if (mainCam != null)
            {
                Vector3 lookTarget = umaObject.transform.position + Vector3.up * 1.6f;
                Vector3 lookDir = (lookTarget - mainCam.position).normalized;
                if (lookDir != Vector3.zero)
                {
                    mainCam.rotation = Quaternion.Slerp(mainCam.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 6f);
                }
            }

            yield return null;
        }

        umaObject.transform.position = targetEmergePos;

        // 3. Dừng lại nhìn chằm chằm người chơi trong giây lát
        if (stareDuration > 0f)
        {
            yield return new WaitForSeconds(stareDuration);
        }

        // 4. Kích hoạt Animation Dash và phi thẳng vào mặt người chơi
        PlayAnimation(dashClip, "dash");
        if (dashScreamSound != null && audioSource != null) audioSource.PlayOneShot(dashScreamSound);

        float dashTimeout = 0f;
        while (dashTimeout < 2.0f)
        {
            dashTimeout += Time.deltaTime;
            Vector3 targetHeadPos = (mainCam != null) ? mainCam.position : playerPos + Vector3.up * 1.6f;
            float dist = Vector3.Distance(umaObject.transform.position, targetHeadPos);

            if (dist <= 0.85f)
            {
                break;
            }

            umaObject.transform.position = Vector3.MoveTowards(umaObject.transform.position, targetHeadPos, dashSpeed * Time.deltaTime);
            Vector3 aimDir = (targetHeadPos - umaObject.transform.position).normalized;
            if (aimDir != Vector3.zero)
            {
                umaObject.transform.rotation = Quaternion.LookRotation(aimDir);
            }

            // Lắc nhẹ camera khi lao tới
            if (mainCam != null)
            {
                mainCam.localPosition += new Vector3(
                    Random.Range(-0.02f, 0.02f),
                    Random.Range(-0.02f, 0.02f),
                    0f
                );
            }

            yield return null;
        }

        // 5. CHẠM MẶT -> KÍCH HOẠT ANIMATION 'END' VÀ HIỆU ỨNG TÓM LẤY BÓP NGHẸT MẶT
        PlayAnimation(endGrabClip, "END");
        if (grabJumpscareSound != null && audioSource != null) audioSource.PlayOneShot(grabJumpscareSound);

        // Gắn UMA dính chặt áp sát camera
        if (mainCam != null)
        {
            umaObject.transform.SetParent(mainCam);
            umaObject.transform.localPosition = new Vector3(0f, -0.35f, 0.55f);
            umaObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        // Rung giật cực mạnh mô phỏng cảm giác bị bóp nghẹt tóm lấy trong 1 lúc (grabHoldDuration)
        float grabTimer = 0f;
        Vector3 camBasePos = (mainCam != null) ? mainCam.localPosition : Vector3.zero;
        while (grabTimer < grabHoldDuration)
        {
            grabTimer += Time.deltaTime;
            if (mainCam != null)
            {
                mainCam.localPosition = camBasePos + new Vector3(
                    Random.Range(-0.06f, 0.06f),
                    Random.Range(-0.06f, 0.06f),
                    Random.Range(-0.03f, 0.03f)
                );
            }
            yield return null;
        }
        if (mainCam != null) mainCam.localPosition = camBasePos;

        // 6. FADE MÀN HÌNH TỐI ĐEN VÀ HIỆN ENDGAME
        Image fadeImg = GetFadeImage();
        PauseMenuManager.BringFadeToFront(fadeImg);
        PauseMenuManager.SetInGameHUDActive(false);

        if (fadeImg != null)
        {
            fadeImg.gameObject.SetActive(true);
            fadeImg.raycastTarget = false;
            float fadeElapsed = 0f;
            Color c = Color.black;
            while (fadeElapsed < 1.2f)
            {
                fadeElapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(fadeElapsed / 1.2f);
                fadeImg.color = c;
                yield return null;
            }
            c.a = 1f;
            fadeImg.color = c;
        }

        // Ẩn UMA
        umaObject.transform.SetParent(null);
        umaObject.SetActive(false);

        // Ẩn toàn bộ UI Ingame
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

        // 7. BẬT PANEL ENDING VÀ CHẠY CHỮ TYPEWRITER
        if (waterEndingPanel != null)
        {
            waterEndingPanel.SetActive(true);
        }

        yield return new WaitForSeconds(0.3f);

        if (endingTextUI == null)
        {
            endingTextUI = FindEndingTextUI();
        }

        string activeLang = SettingsManager.currentLanguage;
        string fullContent = (activeLang == "VI") ? endingTextContentVI : endingTextContentEN;
        if (string.IsNullOrEmpty(fullContent)) fullContent = endingTextContentVI;

        if (endingTextUI != null)
        {
            if (endingTextUI.transform.parent != null) endingTextUI.transform.parent.gameObject.SetActive(true);
            endingTextUI.gameObject.SetActive(true);

            isTypingText = true;
            endingTextUI.text = "";

            for (int i = 0; i <= fullContent.Length; i++)
            {
                if (!isTypingText) break;
                endingTextUI.text = fullContent.Substring(0, i);
                yield return new WaitForSeconds(textSpeed);
            }
        }

        CompleteTextInstantly();
    }

    void CompleteTextInstantly()
    {
        isTypingText = false;
        string activeLang = SettingsManager.currentLanguage;
        string fullContent = (activeLang == "VI") ? endingTextContentVI : endingTextContentEN;
        if (string.IsNullOrEmpty(fullContent)) fullContent = endingTextContentVI;

        if (endingTextUI != null)
        {
            endingTextUI.text = fullContent;
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

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private TextMeshProUGUI FindEndingTextUI()
    {
        if (endingTextUI != null) return endingTextUI;

        GameObject textObj = GameObject.Find("WaterEndingText") ?? GameObject.Find("EndingText") ?? GameObject.Find("EndingTextUI");
        if (textObj != null) return textObj.GetComponent<TextMeshProUGUI>();

        return null;
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
