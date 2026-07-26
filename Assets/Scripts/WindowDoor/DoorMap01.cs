using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class DoorExit : MonoBehaviour, IInteractable
{
    [Header("1. Âm Thanh Mở & Đóng Khóa Cửa (Chuỗi 2 Tiếng Cạch)")]
    [Tooltip("Tiếng cạch 1: Tiếng xoay tay nắm / mở lẫy cửa (VD: door_handle_click.wav)")]
    public AudioClip doorOpenClickSound;

    [Tooltip("Tiếng cạch 2: Tiếng đóng sập cửa & sập ổ khóa (VD: door_lock_click.wav)")]
    public AudioClip doorCloseLockSound;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng cửa (Mặc định: 0.8)")]
    public float soundVolume = 0.8f;

    [Header("2. Chuyển Cảnh Mờ Đen (Fade To Black)")]
    [Tooltip("Tên Scene tiếp theo cần nạp (Mặc định: Map02)")]
    public string targetSceneName = "Map02";

    [Tooltip("Thời gian màn hình tối mờ dần ra đen (giây)")]
    public float fadeOutDuration = 1.2f;

    [Header("3. Khóa Cửa (2 Điều Kiện Bắt Buộc Để Qua Map 02)")]
    [Tooltip("Điều kiện 1: Bắt buộc phải trang bị Camcorder mới cho mở cửa")]
    public bool requireCamcorder = true;

    [Tooltip("Điều kiện 2: Bắt buộc phải bấm tắt Case PC (nút nguồn máy tính) mới cho mở cửa")]
    public bool requirePCTurnedOff = true;

    private AudioSource audioSource;
    private bool isTransitioning = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 0f; // Âm thanh 2D nghe rõ nét 100%
        audioSource.playOnAwake = false;
    }

    public void Interact()
    {
        if (isTransitioning) return;

        // Đảm bảo âm lượng luôn nghe rõ (nếu Inspector lỡ để 0 sẽ tự dùng 0.8)
        float effectiveVolume = soundVolume > 0f ? soundVolume : 0.8f;

        // KIỂM TRA ĐIỀU KIỆN 1: Trang bị máy quay
        bool hasCamcorder = !requireCamcorder || CamcorderUI.HasPickedUpCamera;

        // KIỂM TRA ĐIỀU KIỆN 2: Tắt Case PC (Nút nguồn máy tính)
        bool isPCOff = !requirePCTurnedOff || !PCPowerButton.IsPCPowerOn;

        // NẾU CHƯA THỎA MÃN ĐỦ CẢ 2 ĐIỀU KIỆN -> BẤM F CHỈ PHÁT TIẾNG "CẠCH CẠCH" TAY NẮM CỬA BỊ KHÓA, KHÔNG CHUYỂN SCENE
        if (!hasCamcorder || !isPCOff)
        {
            if (doorOpenClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(doorOpenClickSound, effectiveVolume);
            }

            if (!hasCamcorder && !isPCOff)
            {
                Debug.Log("[DoorExit] ⚠️ Cửa đã bị khóa! Bạn phải nhặt máy quay VÀ nhấn tắt Case PC trước khi mở cửa qua Map 02!");
            }
            else if (!hasCamcorder)
            {
                Debug.Log("[DoorExit] ⚠️ Cửa đã bị khóa! Bạn phải nhặt máy quay trước khi mở cửa qua Map 02!");
            }
            else if (!isPCOff)
            {
                Debug.Log("[DoorExit] ⚠️ Cửa đã bị khóa! Bạn phải nhấn nút tắt Case PC trước khi mở cửa qua Map 02!");
            }

            return;
        }

        // ĐÃ THỎA MÃN ĐỦ CẢ 2 ĐIỀU KIỆN -> QUY TRÌNH NẠP CỬA SANG MAP 02 CỰC NGHỆ
        StartCoroutine(DoorTransitionRoutine(effectiveVolume));
    }

    IEnumerator DoorTransitionRoutine(float volume)
    {
        isTransitioning = true;

        // 0. KHÓA TẠM THỜI DI CHUYỂN PLAYER
        MovePl playerMove = FindFirstObjectByType<MovePl>();
        if (playerMove != null) playerMove.enabled = false;

        CharacterController playerController = FindFirstObjectByType<CharacterController>();
        if (playerController != null) playerController.enabled = false;

        // 1. TIẾNG CẠCH 1: PHÁT TIẾNG XOAY TAY NẮM CỬA MỞ
        if (doorOpenClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(doorOpenClickSound, volume);
            yield return new WaitForSeconds(0.2f);
        }

        // 2. TỐI ĐEN HOÀN TOÀN MÀN HÌNH (FADE TO PITCH BLACK)
        Image fadePanel = null;
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.fadePanel != null)
        {
            fadePanel = PauseMenuManager.Instance.fadePanel;
        }

        if (fadePanel != null)
        {
            fadePanel.transform.SetAsLastSibling();
            fadePanel.gameObject.SetActive(true);
            fadePanel.color = new Color(0, 0, 0, 0f);
            fadePanel.raycastTarget = true;
            fadePanel.DOFade(1f, fadeOutDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        // 3. GIỮ MÀN HÌNH TỐI ĐEN HOÀN TOÀN TRONG 2.0 GIÂY HỒI HỘP
        yield return new WaitForSeconds(2.0f);

        // 4. PHÁT TIẾNG CẠCH 2 (ÂM THANH ĐÓNG CỬA & SẬP Ổ KHÓA CỬA CẠCH CẠCH)
        if (doorCloseLockSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(doorCloseLockSound, volume);
            yield return new WaitForSeconds(doorCloseLockSound.length > 0.5f ? doorCloseLockSound.length : 0.5f);
        }

        // 5. ẨN TOÀN BỘ GIAO DIỆN UI [F] TRƯỚC KHI SANG SCENE
        InteractPro interactPro = FindFirstObjectByType<InteractPro>();
        if (interactPro != null && interactPro.interactionUI != null)
        {
            interactPro.interactionUI.SetActive(false);
        }

        // 6. MỞ KHÓA SCENE 02 VÀ NẠP SANG MAP 02
        GameSaveManager.UnlockLevel(2);

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneAsync(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}