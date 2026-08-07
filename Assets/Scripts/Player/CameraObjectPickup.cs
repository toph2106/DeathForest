using UnityEngine;
using System.Collections;

public class CameraObjectPickup : MonoBehaviour, IInteractable
{
    [Header("1. UI Màn Hình Máy Quay (Camera UI Canvas)")]
    [Tooltip("Kéo Canvas chứa giao diện kính ngắm & đếm giờ vào đây")]
    public GameObject cameraUICanvas;

    [Header("2. Âm Thanh Trang Bị Máy Quay (Click -> Beep -> Hiện UI)")]
    [Tooltip("Kéo âm thanh lật mở màn hình máy quay (Cạch cạch) vào đây")]
    public AudioClip camcorderClickSound;

    [Tooltip("Kéo âm thanh bíp khởi động máy quay (Beep) vào đây")]
    public AudioClip camcorderBeepSound;

    [Range(0f, 1f)]
    [Tooltip("Thanh trượt điều chỉnh âm lượng tiếng máy quay (Mặc định: 0.8)")]
    public float soundVolume = 0.8f;

    [Header("3. Khóa nhặt máy quay (Yêu cầu xem xong máy tính)")]
    public bool requireComputerCutscene = true;
    public static bool isComputerCutsceneFinished = false;

    private SimpleCameraOverlay overlayManager;
    private AudioSource audioSource;
    private bool isEquipping = false;

    void Start()
    {
        if (cameraUICanvas != null) cameraUICanvas.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Âm thanh 2D rõ nét cho người đeo tai nghe
        audioSource.playOnAwake = false;

        if (Camera.main != null)
        {
            overlayManager = Camera.main.GetComponent<SimpleCameraOverlay>();
        }
    }

    public void Interact()
    {
        CrouchInteractable crouchComp = GetComponent<CrouchInteractable>();
        if (crouchComp == null)
        {
            Pickup();
        }
    }

    public void Pickup()
    {
        if (isEquipping) return;

        // Nếu bật khóa và chưa dùng xong máy tính -> KHÔNG CHO NHẶT
        if (requireComputerCutscene && !isComputerCutsceneFinished)
        {
            Debug.Log("[CameraObjectPickup] ⚠️ Bạn phải sử dụng xong máy tính mới được nhặt máy quay!");
            return;
        }

        StartCoroutine(EquipSequenceRoutine());
    }

    IEnumerator EquipSequenceRoutine()
    {
        isEquipping = true;

        // 1. Ẩn hiển thị hình ảnh 3D máy tính dưới đất ngay lập tức
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = false;

        // 2. BƯỚC 1: Phát tiếng "CẠCH CẠCH" mở nắp màn hình máy quay
        if (camcorderClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(camcorderClickSound, soundVolume);
            yield return new WaitForSeconds(camcorderClickSound.length);
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        // 3. BƯỚC 2: Phát tiếng "BÍP" khởi động máy quay
        if (camcorderBeepSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(camcorderBeepSound, soundVolume);
            yield return new WaitForSeconds(0.15f);
        }

        // 4. BƯỚC 3: KÍCH HOẠT GIAO DIỆN KÍNH NGẮM VÀ UI CAMCORDER + MẶC ĐỊNH 100% PIN
        CamcorderUI.MarkCameraPickedUp();

        if (FlashlightToggle.Instance != null)
        {
            FlashlightToggle.Instance.EquipFlashlight();
        }

        if (CamcorderUI.Instance != null)
        {
            CamcorderUI.Instance.gameObject.SetActive(true);
        }

        if (overlayManager != null)
        {
            overlayManager.TurnOnCameraView();
        }

        if (cameraUICanvas != null)
        {
            cameraUICanvas.SetActive(true);
        }

        // 5. Xóa object an toàn sau 2.0s
        Destroy(gameObject, 2.0f);
    }
}