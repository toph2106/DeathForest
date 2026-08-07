using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class Truck : MonoBehaviour
{
    public float speed = 15f;
    public float waitTime = 1f;
    public Transform player;

    [Header("1. Điểm Hồi Sinh & Ghim Tầm Nhìn Camera")]
    [Tooltip("Kéo Object Spawn trong Scene vào đây để khi bị tông sẽ dịch chuyển Player về đây")]
    public Transform spawnPoint;

    [Tooltip("Kéo Object (VD: Cục Cube / tờ giấy lơ lửng trên đền) vào đây để khi tỉnh dậy camera sẽ tự động ghim nhìn thẳng vào mục tiêu này!")]
    public Transform respawnLookTarget;

    [Header("2. Âm Thanh & Đèn Xe")]
    public AudioSource engineSound;
    public AudioSource impactSound;
    public AudioSource rezeroSound;
    public Light leftHeadlight;
    public Light rightHeadlight;
    public Light bounceLight;

    [Header("3. Tùy Chỉnh Thời Gian Fade Chuyển Cảnh (3 Giai Đoạn)")]
    [Tooltip("1. THỜI GIAN NHẮM MẮT: Màn hình từ từ mờ đen hoàn toàn khi bị tông (giây). Mặc định: 1.5s")]
    public float fadeOutDuration = 1.5f;

    [Tooltip("2. THỜI GIAN ĐEN XÌ NGÒM: Thời gian giữ tối đen ngòm để Player rơi bám sàn trong tối (giây). Mặc định: 2.0s")]
    public float darkPauseDuration = 2.0f;

    [Tooltip("3. THỜI GIAN MỞ MẮT: Màn hình từ từ mờ sáng mở dần ra lại (giây). Mặc định: 3.0s cho mở siêu đằm mượt")]
    public float fadeInDuration = 3.0f;

    private Vector3 startPos;
    private Quaternion startRot;
    private bool isRunning = false;
    private bool hasTriggered = false;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;

        if (leftHeadlight != null) leftHeadlight.enabled = false;
        if (rightHeadlight != null) rightHeadlight.enabled = false;
        if (bounceLight != null) bounceLight.enabled = false;
    }

    public void StartTruckSequence()
    {
        if (!hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(EngineStartup());
        }
    }

    IEnumerator EngineStartup()
    {
        if (leftHeadlight != null) leftHeadlight.enabled = true;
        if (rightHeadlight != null) rightHeadlight.enabled = true;
        if (bounceLight != null) bounceLight.enabled = true;

        if (engineSound != null)
        {
            engineSound.Play();
        }

        yield return new WaitForSeconds(waitTime);
        isRunning = true;
    }

    void Update()
    {
        if (isRunning && player != null)
        {
            Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            ExecuteDeath();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponentInParent<MovePl>() != null)
        {
            ExecuteDeath();
        }
    }

    void ExecuteDeath()
    {
        if (!isRunning && hasTriggered && transform.position == startPos) return;

        isRunning = false;

        if (engineSound != null && engineSound.isPlaying)
        {
            engineSound.Stop();
        }

        if (impactSound != null)
        {
            impactSound.Play();
        }

        // TẮT ĐÈN XE VÀ ĐƯA XE VỀ VỊ TRÍ BAN ĐẦU
        transform.position = startPos;
        transform.rotation = startRot;

        if (leftHeadlight != null) leftHeadlight.enabled = false;
        if (rightHeadlight != null) rightHeadlight.enabled = false;
        if (bounceLight != null) bounceLight.enabled = false;

        StartCoroutine(RespawnSequenceRoutine());
    }

    IEnumerator RespawnSequenceRoutine()
    {
        MovePl playerMove = FindFirstObjectByType<MovePl>();
        CharacterController cc = (player != null) ? player.GetComponent<CharacterController>() : FindFirstObjectByType<CharacterController>();
        Transform mainCam = (playerMove != null && playerMove.cameraTransform != null) ? playerMove.cameraTransform : (Camera.main != null ? Camera.main.transform : null);

        // 1. KHÓA TẠM THỜI GÓC NHÌN CHUỘT VÀ BÀN PHÍM PLAYER
        if (playerMove != null)
        {
            playerMove.isCameraLocked = true;
            playerMove.SetMovementState(false);
            playerMove.enabled = true;
        }
        if (cc != null) cc.enabled = true;

        Image fadeImg = GetFadeImage();

        // 2. GIAI ĐOẠN 1: FADE OUT MỜ ĐEN MẮT VỚI DOTWEEN (FADE OUT: 1.5S)
        if (fadeImg != null)
        {
            PauseMenuManager.BringFadeToFront(fadeImg);
            PauseMenuManager.SetInGameHUDActive(false);

            fadeImg.DOKill();
            fadeImg.color = new Color(0, 0, 0, 0f);
            fadeImg.DOFade(1f, fadeOutDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            yield return new WaitForSecondsRealtime(fadeOutDuration);
        }
        else
        {
            yield return new WaitForSecondsRealtime(fadeOutDuration);
        }

        // 3. DỊCH CHUYỂN PLAYER VỀ VỊ TRÍ SPAWN TRONG BÓNG TỐI
        if (player != null && spawnPoint != null)
        {
            if (cc != null) cc.enabled = false;
            player.position = spawnPoint.position;
            player.rotation = spawnPoint.rotation;
            if (cc != null) cc.enabled = true;
        }

        // ÉP CỤC CAMERA VÀ THÂN NGƯỜI NHÌN THẲNG VÀO MỤC TIÊU CUBE NGAY TRONG BÓNG TỐI
        AlignCameraToTarget(mainCam, player, respawnLookTarget);
        if (playerMove != null) playerMove.SyncRotationWithCurrentCamera();

        // GIAI ĐOẠN 2: ÉP TỐI ĐEN NGÒM 100% VÀ GIỮ NGHỈ TRONG DARK PAUSE DURATION
        if (fadeImg != null)
        {
            fadeImg.DOKill();
            fadeImg.color = new Color(0, 0, 0, 1f);
        }

        float darkTimer = 0f;
        while (darkTimer < darkPauseDuration)
        {
            darkTimer += Time.unscaledDeltaTime;
            AlignCameraToTarget(mainCam, player, respawnLookTarget);
            yield return null;
        }

        // RESET LẠI TRẠNG THÁI TRIGGER VÀ XE ĐỂ CHO PHÉP KÍCH HOẠT LẠI LẦN SAU
        hasTriggered = false;
        TriggerEventTruck.ResetAllTriggers();

        // 4. GIAI ĐOẠN 3: FADE IN MỜ SÁNG MỞ DẦN RA LẠI (CAMERA LIÊN TỤC GHIM VÀO CỤC CUBE)
        if (fadeImg != null)
        {
            fadeImg.DOKill();
            fadeImg.color = new Color(0, 0, 0, 1f);
            fadeImg.DOFade(0f, fadeInDuration).SetEase(Ease.InOutCubic).SetUpdate(true);
        }

        float fadeTimer = 0f;
        while (fadeTimer < fadeInDuration)
        {
            fadeTimer += Time.unscaledDeltaTime;
            AlignCameraToTarget(mainCam, player, respawnLookTarget);
            yield return null;
        }

        if (fadeImg != null)
        {
            fadeImg.color = new Color(0, 0, 0, 0f);
            fadeImg.raycastTarget = false;
            fadeImg.gameObject.SetActive(false);
        }

        // SAU KHÍ MỜ SÁNG HOÀN TOÀN MỚI MỞ LẠI HUD INGAME
        PauseMenuManager.SetInGameHUDActive(true);

        // 5. ĐỒNG BỘ CHÍNH XÁC GÓC XOAY CAMERA VÀO MOVEPL RỒI MỚI THẢ CHUỘT TỰ DO
        if (playerMove != null)
        {
            playerMove.SyncRotationWithCurrentCamera();
            playerMove.isCameraLocked = false;
            playerMove.SetMovementState(true);
        }

        // 6. SAU KHÍ FADE IN MỜ SÁNG MÀN HÌNH HOÀN TOÀN -> MỚI PHÁT NHẠC REZERO!
        if (rezeroSound != null)
        {
            rezeroSound.Play();
        }
    }

    private void AlignCameraToTarget(Transform mainCam, Transform playerTransform, Transform target)
    {
        if (target == null || mainCam == null) return;

        Vector3 direction = (target.position - mainCam.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            mainCam.rotation = targetRotation;

            if (playerTransform != null)
            {
                playerTransform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
            }
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
}