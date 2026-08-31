using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Hệ thống tương tác leo thang và dịch chuyển tầng chuyển nghiệp (Cinema-Grade Transition):
/// 1. Tự động nhận diện chuột trái thông qua IInteractable (hiện icon Hand khi nhìn vào thang).
/// 2. Hiệu ứng Fade màn hình đen mềm mại, tùy chỉnh tự do để khớp hoàn hảo với bất kỳ file âm thanh nào.
/// 3. Khóa điều khiển Player tạm thời khi đang leo thang để tránh trượt ngã / xoay nhầm hướng.
/// 4. Tự động đồng bộ góc xoay Yaw & Pitch để khi xuất hiện nhân vật nhìn thẳng vào phòng chuẩn xác.
/// 5. Tự động tìm kiếm hoặc khởi tạo Fade Screen nếu chưa có trong Scene.
/// </summary>
public class LadderInteractTeleport : MonoBehaviour, IInteractable
{
    [Header("1. Điểm Dịch Chuyển Đến (Target Point)")]
    [Tooltip("Kéo Empty GameObject (Point_Top hoặc Point_Bottom) mà Player sẽ xuất hiện sau khi leo")]
    public Transform targetPoint;

    [Header("2. Cài Đặt Âm Thanh (Audio)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh leo thang gỗ cộp cộp")]
    public AudioClip climbSound;
    [Range(0f, 1f)]
    [Tooltip("Âm lượng phát âm thanh leo thang")]
    public float soundVolume = 1.0f;

    [Header("3. Hiệu Ứng Fade Chuyển Cảnh Mượt Mà (Fade Settings)")]
    [Tooltip("Bật hiệu ứng mờ dần màn hình đen khi leo thang")]
    public bool enableFade = true;

    [Tooltip("Màu của màn hình Fade (Mặc định: Đen)")]
    public Color fadeColor = Color.black;

    [Tooltip("Thời gian màn hình tối dần lúc bắt đầu leo (giây - Mặc định: 0.35s)")]
    public float fadeOutDuration = 0.35f;

    [Tooltip("Thời gian GIỮ MÀN HÌNH ĐEN lúc đang leo thang (giây - Tùy chỉnh để khớp đúng độ dài âm thanh)")]
    public float blackScreenHoldDuration = 0.6f;

    [Tooltip("Tự động chỉnh thời gian giữ màn hình đen khớp đúng với độ dài của file âm thanh climbSound")]
    public bool matchHoldToAudioLength = false;

    [Tooltip("Thời gian màn hình sáng dần lại sau khi đã tới nơi (giây - Mặc định: 0.45s)")]
    public float fadeInDuration = 0.45f;

    [Header("4. Khóa Điều Khiển & Camera (Player Controls)")]
    [Tooltip("Tạm thời khóa di chuyển và chuột của người chơi khi đang leo để tránh bị trượt lệch vị trí")]
    public bool lockControlsDuringClimb = true;

    [Tooltip("Tự động xoay người chơi theo hướng nhìn (Trục Z - Mũi tên xanh) của Target Point")]
    public bool syncYawRotation = true;

    [Tooltip("Dựng thẳng góc nhìn mắt về phương ngang (không bị ngước lên trần hay cúi xuống đất)")]
    public bool resetPitchLook = true;

    [Header("5. Sự Kiện Mở Rộng (Events - Tùy Chọn)")]
    public UnityEvent onClimbStart;
    public UnityEvent onTeleportReached;
    public UnityEvent onClimbFinished;

    // --- Private State ---
    private static bool isTransitioning = false;
    private Image fadeImage;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D Sound
            audioSource.playOnAwake = false;
        }

        EnsureFadeImage();
    }

    void EnsureFadeImage()
    {
        if (fadeImage != null) return;

        // 1. Tìm Image FadeScreen có sẵn trong Scene
        GameObject fadeObj = GameObject.Find("FadeScreen") ?? 
                             GameObject.Find("FadePanel") ?? 
                             GameObject.Find("FadeImage") ?? 
                             GameObject.Find("ScreenFade") ??
                             GameObject.Find("BlackScreen");

        if (fadeObj != null)
        {
            fadeImage = fadeObj.GetComponent<Image>();
            if (fadeImage != null)
            {
                fadeImage.raycastTarget = false;
                return;
            }
        }

        // 2. Tìm Canvas trong Scene để tự tạo FadeScreen nếu chưa có
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject newFadeObj = new GameObject("Ladder_FadeScreen");
            newFadeObj.transform.SetParent(canvas.transform, false);
            newFadeObj.transform.SetAsLastSibling();

            fadeImage = newFadeObj.AddComponent<Image>();
            Color initialColor = fadeColor;
            initialColor.a = 0f;
            fadeImage.color = initialColor;
            fadeImage.raycastTarget = false;

            RectTransform rt = fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// Được gọi tự động khi Player bấm Chuột Trái (IInteractable) vào Trigger này
    /// </summary>
    public void Interact()
    {
        if (isTransitioning) return;

        if (targetPoint == null)
        {
            Debug.LogWarning($"[LadderInteractTeleport] Chưa gán 'Target Point' trên {gameObject.name}!");
            return;
        }

        StartCoroutine(ClimbTransitionRoutine());
    }

    private IEnumerator ClimbTransitionRoutine()
    {
        isTransitioning = true;
        onClimbStart?.Invoke();

        // 1. PHÁT ÂM THANH LEO THANG
        if (climbSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(climbSound, soundVolume);
        }

        // 2. TÌM PLAYER & MOVEPL
        MovePl movePl = Object.FindFirstObjectByType<MovePl>();
        Transform playerTrans = (movePl != null) ? movePl.transform : null;
        if (playerTrans == null)
        {
            GameObject plObj = GameObject.FindGameObjectWithTag("Player");
            if (plObj != null) playerTrans = plObj.transform;
        }

        if (playerTrans == null)
        {
            Debug.LogError("[LadderInteractTeleport] Không tìm thấy Player trong Scene!");
            isTransitioning = false;
            yield break;
        }

        // 3. TẠM KHÓA ĐIỀU KHIỂN NẾU ĐƯỢC BẬT
        if (lockControlsDuringClimb && movePl != null)
        {
            movePl.isCameraLocked = true;
        }

        // 4. PHA 1: MÀN HÌNH TỐI DẦN (FADE OUT)
        EnsureFadeImage();
        if (enableFade && fadeImage != null)
        {
            float elapsed = 0f;
            Color c = fadeColor;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeOutDuration);
                c.a = t;
                fadeImage.color = c;
                yield return null;
            }
            c.a = 1f;
            fadeImage.color = c;
        }
        else if (enableFade)
        {
            yield return new WaitForSeconds(fadeOutDuration);
        }

        // 5. PHA 2: THỰC HIỆN DỊCH CHUYỂN VỊ TRÍ AN TOÀN TRONG BÓNG TỐI
        CharacterController cc = playerTrans.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Gán vị trí
        playerTrans.position = targetPoint.position;

        // Gán góc xoay nhân vật
        if (syncYawRotation)
        {
            float targetYaw = targetPoint.eulerAngles.y;
            playerTrans.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        }

        if (cc != null) cc.enabled = true;

        // Đồng bộ Camera
        if (movePl != null)
        {
            if (resetPitchLook && movePl.cameraTransform != null)
            {
                movePl.cameraTransform.localRotation = Quaternion.identity;
            }
            movePl.SyncRotationWithCurrentCamera();
        }

        onTeleportReached?.Invoke();

        // 6. PHA 3: GIỮ MÀN HÌNH ĐEN ĐỂ CHỜ ÂM THANH LEO CHẠY XONG
        float holdTime = blackScreenHoldDuration;
        if (matchHoldToAudioLength && climbSound != null)
        {
            // Trừ đi thời gian fade out và fade in để tổng thời gian khớp đúng file âm thanh
            holdTime = Mathf.Max(0.1f, climbSound.length - fadeOutDuration - fadeInDuration);
        }

        if (holdTime > 0f)
        {
            yield return new WaitForSeconds(holdTime);
        }

        // 7. PHA 4: MÀN HÌNH SÁNG DẦN LẠI (FADE IN)
        if (enableFade && fadeImage != null)
        {
            float elapsed = 0f;
            Color c = fadeColor;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
                c.a = 1f - t;
                fadeImage.color = c;
                yield return null;
            }
            c.a = 0f;
            fadeImage.color = c;
        }
        else if (enableFade)
        {
            yield return new WaitForSeconds(fadeInDuration);
        }

        // 8. MỞ LẠI QUYỀN ĐIỀU KHIỂN CHO PLAYER
        if (lockControlsDuringClimb && movePl != null)
        {
            movePl.isCameraLocked = false;
            movePl.SyncRotationWithCurrentCamera();
        }

        onClimbFinished?.Invoke();
        isTransitioning = false;
    }

    void OnDrawGizmosSelected()
    {
        if (targetPoint != null)
        {
            // Điểm gốc và đường nối
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawLine(transform.position, targetPoint.position);

            // Điểm đến
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPoint.position, 0.4f);

            // Mũi tên hướng nhìn
            Gizmos.color = Color.yellow;
            Vector3 forwardRay = targetPoint.position + targetPoint.forward * 1.2f;
            Gizmos.DrawLine(targetPoint.position, forwardRay);
            Gizmos.DrawWireSphere(forwardRay, 0.1f);
        }
    }
}
