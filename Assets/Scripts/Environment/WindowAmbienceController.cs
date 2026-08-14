using UnityEngine;
using System.Collections;

public class WindowAmbienceController : MonoBehaviour, IInteractable
{
    [Header("1. Nguồn Âm Thanh Thành Phố (City Ambience AudioSource)")]
    [Tooltip("Kéo AudioSource phát tiếng thành phố đêm lặp đi lặp lại vào đây")]
    public AudioSource cityAmbienceAudioSource;

    [Header("2. Mức Âm Lượng Khi Đóng & Mở Cửa Sổ")]
    [Range(0f, 1f)]
    public float closedVolume = 0.35f;

    [Range(0f, 1f)]
    public float openedVolume = 0.8f;

    public float volumeFadeDuration = 1.5f;

    [Header("3. Trạng Thái Mặc Định Ban Đầu")]
    [Tooltip("Tích chọn nếu cửa sổ MẶC ĐỊNH MỞ khi vào game để phát âm thanh môi trường to sẵn")]
    public bool startOpened = false;

    [Header("4. Âm Thanh Thao Tác Cửa Sổ & Âm Lượng")]
    [Range(0f, 1f)]
    public float windowSfxVolume = 0.35f;

    public AudioClip windowOpenSound;
    public AudioClip windowCloseSound;

    [Header("5. Mở Khóa Case PC Sau Khi Đóng Cửa Sổ")]
    [Tooltip("Kéo Collider của Vỏ Case PC vào đây để mở khóa tương tác Case PC sau khi ĐÓNG CỬA SỔ!")]
    public Collider caseColliderToEnable;
    public GameObject caseObjectToEnable;

    private bool isOpen = false;
    private AudioSource sfxAudioSource;
    private Coroutine fadeCoroutine;

    void Start()
    {
        sfxAudioSource = GetComponent<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();

        sfxAudioSource.spatialBlend = 0f;
        sfxAudioSource.playOnAwake = false;

        isOpen = startOpened;

        // Cấu hình âm lượng ban đầu cho tiếng thành phố theo startOpened
        if (cityAmbienceAudioSource != null)
        {
            cityAmbienceAudioSource.loop = true;
            cityAmbienceAudioSource.volume = startOpened ? openedVolume : closedVolume;
            if (!cityAmbienceAudioSource.isPlaying)
            {
                cityAmbienceAudioSource.Play();
            }
        }
    }

    public void Interact()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            if (windowOpenSound != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(windowOpenSound, windowSfxVolume);
            }
            StartVolumeFade(openedVolume);
        }
        else
        {
            if (windowCloseSound != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(windowCloseSound, windowSfxVolume);
            }
            StartVolumeFade(closedVolume);

            // MỞ KHÓA CASE PC KHI ĐÓNG CỬA SỔ
            if (caseColliderToEnable != null)
            {
                caseColliderToEnable.enabled = true;
            }
            if (caseObjectToEnable != null)
            {
                caseObjectToEnable.SetActive(true);
            }

            // Tự động tìm và mở khóa PCPowerButton trong Scene (Fail-safe)
            PCPowerButton pcPower = Object.FindFirstObjectByType<PCPowerButton>();
            if (pcPower != null)
            {
                pcPower.UnlockCase();
            }

            Debug.Log("[WindowAmbienceController] 🔓 ĐÃ ĐÓNG CỬA SỔ! Mở khóa tương tác với Case PC.");
        }
    }

    void EnsureCityAudioSource()
    {
        if (cityAmbienceAudioSource != null) return;
        GameObject cityObj = GameObject.Find("CityAudio");
        if (cityObj != null) cityAmbienceAudioSource = cityObj.GetComponent<AudioSource>();
    }

    public void StartVolumeFade(float targetVol)
    {
        EnsureCityAudioSource();
        if (cityAmbienceAudioSource == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeVolumeRoutine(targetVol));
    }

    IEnumerator FadeVolumeRoutine(float targetVol)
    {
        float startVol = cityAmbienceAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < volumeFadeDuration)
        {
            elapsed += Time.deltaTime;
            cityAmbienceAudioSource.volume = Mathf.Lerp(startVol, targetVol, elapsed / volumeFadeDuration);
            yield return null;
        }

        cityAmbienceAudioSource.volume = targetVol;
    }
}
