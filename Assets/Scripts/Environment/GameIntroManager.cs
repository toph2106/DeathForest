using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameIntroManager : MonoBehaviour
{
    [Header("1. Tham Chiếu Cần Thiết (References)")]
    public MovePl playerMovePl;
    public Transform cameraTransform;
    public TextMeshProUGUI subtitleTextUI;
    public GameObject subtitleCanvasObject;

    [Header("2. Cấu Hình Tư Thế Tựa Cửa Sổ (Window Leaning Pose)")]
    [Tooltip("Độ cao Camera khi đứng bình thường (Mặc định: 0.6)")]
    public float standingCamY = 0.6f;

    [Tooltip("Độ cao Camera khi hạ thấp chống cằm/tựa cửa sổ (Mặc định: 0.5)")]
    public float leaningCamY = 0.5f;

    [Tooltip("Góc nghiêng nhẹ nhìn xuống khi chống cằm tựa cửa sổ (Độ, Mặc định: 20.0 cho góc nhìn đẹp chuẩn)")]
    public float tiltDownAngle = 20.0f;

    [Tooltip("Thời gian trượt mượt góc camera (giây)")]
    public float poseTransitionDuration = 1.2f;

    [Header("3. Lời Thoại Nội Tâm Mở Màn (Intro Dialogue Lines)")]
    public float delayBeforeFirstLine = 1.0f;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.03f;
    public float holdTimePerLine = 3.2f;

    [System.Serializable]
    public class IntroLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "";
        [TextArea(2, 4)]
        public string englishDialogue = "";
    }

    public IntroLine[] introLines = new IntroLine[]
    {
        new IntroLine
        {
            vietnameseDialogue = "Lại 2 giờ sáng rồi... Dạo này mình bế tắc quá, chẳng nghĩ ra được chút ý tưởng nào ra hồn cả.",
            englishDialogue = "It's 2 AM again... I'm so stuck lately, can't come up with any decent ideas at all."
        },
        new IntroLine
        {
            vietnameseDialogue = "Thôi thì... qua bàn bật máy tính lên lướt web xem có kiếm được chút cảm hứng nào không vậy.",
            englishDialogue = "Well then... guess I'll turn on the computer and browse the web for some inspiration."
        }
    };

    [Header("4. Âm Thanh Thoại Chung (Dialogue SFX)")]
    [Tooltip("Gói âm thanh lồng tiếng / tiếng thở / gõ phím chung cho toàn bộ intro thoại (Tùy chọn)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("5. Mở Khóa Tương Tác Cửa Sổ Sau Khi Hết Intro")]
    [Tooltip("Kéo Collider (hoặc GameObject) của Cửa Sổ vào đây để mở khóa tương tác đóng cửa sổ sau khi xem xong Intro")]
    public Collider windowColliderToUnlock;
    public GameObject windowObjectToUnlock;
    public bool lockWindowOnStart = true;

    [Header("6. Tự Động Chạy Khi Vào Game")]
    public bool playOnStart = true;

    private AudioSource audioSource;
    private bool isIntroRunning = false;

    // --- BIẾN ĐIỀU KHIỂN CLICK CHUỘT QUA THOẠI ---
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private string currentFullText = "";

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (playerMovePl == null) playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (playerMovePl != null && cameraTransform == null) cameraTransform = playerMovePl.cameraTransform;

        if (lockWindowOnStart)
        {
            if (windowColliderToUnlock != null) windowColliderToUnlock.enabled = false;
        }

        if (playOnStart)
        {
            StartIntroSequence();
        }
    }

    void Update()
    {
        if (!isIntroRunning) return;

        // Bấm chuột trái (Mouse 0) hoặc Space để qua thoại nhanh
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // BẤM LẦN 1 KHI ĐANG GÕ ➔ HIỆN TOÀN BỘ CHỮ NGAY LẬP TỨC
                isTyping = false;
                if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;
            }
            else if (isWaitingForNextLine)
            {
                // BẤM LẦN 2 KHI CHỮ ĐÃ ĐẦY ĐỦ ➔ SANG CÂU THOẠI TIẾP THEO NGAY LẬP TỨC
                skipRequested = true;
            }
        }
    }

    public void StartIntroSequence()
    {
        if (isIntroRunning) return;
        StartCoroutine(IntroSequenceRoutine());
    }

    IEnumerator IntroSequenceRoutine()
    {
        isIntroRunning = true;

        // 1. KHÓA DI CHUYỂN & CAMERA CỦA PLAYER
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = true;
            playerMovePl.SetMovementState(false);
        }

        // 2. HẠ THẤP CAMERA TỪ 0.6 XUỐNG 0.5 VÀ NGHIÊNG NHẸ XUỐNG 12 ĐỘ (TƯ THẾ TỰA CỬA SỔ)
        if (cameraTransform != null)
        {
            Vector3 startPos = cameraTransform.localPosition;
            Quaternion startRot = cameraTransform.localRotation;

            Vector3 targetPos = new Vector3(startPos.x, leaningCamY, startPos.z);
            Quaternion targetRot = Quaternion.Euler(tiltDownAngle, 0f, 0f);

            float elapsed = 0f;
            while (elapsed < poseTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / poseTransitionDuration);
                cameraTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                cameraTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            cameraTransform.localPosition = targetPos;
            cameraTransform.localRotation = targetRot;
        }

        // 3. BẬT UI PHỤ ĐỀ VÀ CHỜ 1.0S ĐẦU TIÊN
        if (subtitleCanvasObject != null) subtitleCanvasObject.SetActive(true);
        if (subtitleTextUI != null) subtitleTextUI.text = "";

        if (delayBeforeFirstLine > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFirstLine);
        }

        // 4. CHẠY LẦN LƯỢT CÁC CÂU THOẠI NỘI TÂM (HỖ TRỢ BẤM CHUỘT TRÁI QUA MAU)
        foreach (var line in introLines)
        {
            if (line == null) continue;

            string lang = SettingsManager.currentLanguage;
            currentFullText = (lang == "VI") ? line.vietnameseDialogue : line.englishDialogue;
            if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.vietnameseDialogue;
            if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.englishDialogue;

            if (string.IsNullOrEmpty(currentFullText)) continue;

            skipRequested = false;

            // BẬT ÂM THANH THOẠI TRONG SUỐT LÚC ĐANG GÕ CHỮ
            if (dialogueSound != null && audioSource != null)
            {
                audioSource.clip = dialogueSound;
                audioSource.volume = soundVolume;
                audioSource.loop = true;
                if (!audioSource.isPlaying) audioSource.Play();
            }

            // GÕ CHỮ TỪNG KÝ TỰ (TYPEWRITER)
            if (useTypewriterEffect && subtitleTextUI != null)
            {
                isTyping = true;
                subtitleTextUI.text = "";

                for (int i = 0; i <= currentFullText.Length; i++)
                {
                    if (!isTyping || skipRequested) break;
                    subtitleTextUI.text = currentFullText.Substring(0, i);
                    yield return new WaitForSeconds(typewriterSpeed);
                }

                subtitleTextUI.text = currentFullText;
                isTyping = false;
            }
            else if (subtitleTextUI != null)
            {
                subtitleTextUI.text = currentFullText;
            }

            // DỪNG ÂM THANH MƯỢT MÀ KHI ĐÃ GÕ XONG (HOẶC SKIP)
            if (audioSource != null && audioSource.isPlaying)
            {
                StartCoroutine(FadeAudioOutRoutine(audioSource, 0.08f));
            }

            // CHỜ ĐỌC XONG HOẶC CHỜ BẤM CHUỘT SANG CÂU TIẾP THEO
            isWaitingForNextLine = true;
            float waitTimer = 0f;
            while (waitTimer < holdTimePerLine && !skipRequested)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }
            isWaitingForNextLine = false;
        }

        // Tắt chữ phụ đề sau khi đọc xong
        if (subtitleTextUI != null) subtitleTextUI.text = "";
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        // 5. TRẢ CAMERA TỪ 0.5 NÂNG LÊN LẠI 0.6 VÀ THẲNG LẠI GÓC XOAY BAN ĐẦU
        if (cameraTransform != null)
        {
            Vector3 startPos = cameraTransform.localPosition;
            Quaternion startRot = cameraTransform.localRotation;

            Vector3 targetPos = new Vector3(startPos.x, standingCamY, startPos.z);
            Quaternion targetRot = Quaternion.Euler(0f, 0f, 0f);

            float elapsed = 0f;
            while (elapsed < poseTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / poseTransitionDuration);
                cameraTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                cameraTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            cameraTransform.localPosition = targetPos;
            cameraTransform.localRotation = targetRot;
        }

        // 6. THẢ TỰ DO DI CHUYỂN & CAMERA CHO PLAYER
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = false;
            playerMovePl.SetMovementState(true);
            playerMovePl.SyncRotationWithCurrentCamera();
        }

        // 7. MỞ KHÓA TƯƠNG TÁC CỬA SỔ
        if (windowColliderToUnlock != null)
        {
            windowColliderToUnlock.enabled = true;
            Debug.Log("[GameIntroManager] 🔓 Đã mở khóa tương tác cho Cửa Sổ!");
        }
        if (windowObjectToUnlock != null)
        {
            windowObjectToUnlock.SetActive(true);
        }

        isIntroRunning = false;
        Debug.Log("[GameIntroManager] ✅ Đã hoàn tất Intro mở màn! Mở lại di chuyển cho Player.");
    }

    IEnumerator FadeAudioOutRoutine(AudioSource src, float duration)
    {
        if (src == null || !src.isPlaying) yield break;
        float startVol = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        src.Stop();
        src.volume = startVol;
    }
}
