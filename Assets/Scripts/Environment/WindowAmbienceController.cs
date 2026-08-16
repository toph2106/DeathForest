using UnityEngine;
using System.Collections;

public class WindowAmbienceController : MonoBehaviour, IInteractable
{
    [Header("1. Nguồn Âm Thanh Thành Phố (City Ambience AudioSource)")]
    [Tooltip("Kéo AudioSource phát tiếng thành phố đêm lặp đi lặp lại vào đây")]
    public AudioSource cityAmbienceAudioSource;

    [Header("2. Mức Âm Lượng Khi Đóng & Mở Cửa Sổ")]
    [Range(0f, 1f)] public float closedVolume = 0.35f;
    [Range(0f, 1f)] public float openedVolume = 0.8f;
    public float volumeFadeDuration = 1.5f;

    [Header("3. Trạng Thái Mặc Định Ban Đầu")]
    [Tooltip("Tích chọn nếu cửa sổ MẶC ĐỊNH MỞ khi vào game để phát âm thanh môi trường to sẵn")]
    public bool startOpened = true;

    [Header("4. Âm Thanh Thao Tác Cửa Sổ & Âm Lượng")]
    [Range(0f, 1f)] public float windowSfxVolume = 0.35f;
    public AudioClip windowOpenSound;
    public AudioClip windowCloseSound;

    [Header("5. Mở Khóa Case PC Sau Khi Đóng Cửa Sổ")]
    [Tooltip("Kéo Collider của Vỏ Case PC vào đây để mở khóa tương tác Case PC sau khi ĐÓNG CỬA SỔ!")]
    public Collider caseColliderToEnable;
    public GameObject caseObjectToEnable;

    private bool isOpen = false;
    public bool IsOpen => isOpen;
    private AudioSource sfxAudioSource;
    private Coroutine fadeCoroutine;

    private static WindowAmbienceController instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        sfxAudioSource = GetComponent<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();

        sfxAudioSource.spatialBlend = 0f;
        sfxAudioSource.playOnAwake = false;

        isOpen = startOpened;
        EnsureCityAudioSource();

        // Kiểm tra toàn diện tất cả các cửa sổ trong phòng để đồng bộ âm lượng lúc Start
        bool anyOpen = CheckIfAnyWindowOpen();
        if (cityAmbienceAudioSource != null)
        {
            cityAmbienceAudioSource.loop = true;
            cityAmbienceAudioSource.volume = anyOpen ? openedVolume : closedVolume;
            if (!cityAmbienceAudioSource.isPlaying)
            {
                cityAmbienceAudioSource.Play();
            }
        }
    }

    public static bool CheckIfAnyWindowOpen()
    {
        // 1. Kiểm tra WindowAmbienceController chính
        if (instance != null && instance.isOpen) return true;

        // 2. Kiểm tra tất cả WindowL trong Scene
        WindowL[] winLs = Object.FindObjectsByType<WindowL>(FindObjectsSortMode.None);
        foreach (var w in winLs)
        {
            if (w != null && w.IsOpen) return true;
        }

        // 3. Kiểm tra tất cả WindowR trong Scene
        WindowR[] winRs = Object.FindObjectsByType<WindowR>(FindObjectsSortMode.None);
        foreach (var w in winRs)
        {
            if (w != null && w.IsOpen) return true;
        }

        return false;
    }

    public static void SyncAllWindowAmbience()
    {
        if (instance != null)
        {
            bool anyOpen = CheckIfAnyWindowOpen();
            instance.StartVolumeFade(anyOpen ? instance.openedVolume : instance.closedVolume);

            if (!anyOpen)
            {
                // Khi TẤT CẢ cửa sổ đã đóng kín: Mở khóa Case PC
                if (instance.caseColliderToEnable != null) instance.caseColliderToEnable.enabled = true;
                if (instance.caseObjectToEnable != null) instance.caseObjectToEnable.SetActive(true);

                PCPowerButton pcPower = Object.FindFirstObjectByType<PCPowerButton>();
                if (pcPower != null) pcPower.UnlockCase();

                Debug.Log("[WindowAmbienceController] 🔓 TẤT CẢ CỬA SỔ ĐÃ ĐÓNG KÍN! Âm thanh giảm nhỏ & Đã mở khóa Case PC.");
            }
            else
            {
                Debug.Log("[WindowAmbienceController] 🪟 VẪN CÒN CỬA SỔ ĐANG MỞ! Âm thanh thành phố vẫn to.");
            }
        }
    }

    public static void CloseAllWindows(bool snapInstantly = false)
    {
        if (instance != null)
        {
            instance.isOpen = false;
        }

        WindowL[] winLs = Object.FindObjectsByType<WindowL>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var w in winLs)
        {
            if (w != null) w.CloseWindow(snapInstantly);
        }

        WindowR[] winRs = Object.FindObjectsByType<WindowR>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var w in winRs)
        {
            if (w != null) w.CloseWindow(snapInstantly);
        }

        SyncAllWindowAmbience();
        Debug.Log("[WindowAmbienceController] 🪟 ĐÃ TỰ ĐỘNG ĐÓNG TẤT CẢ CỬA SỔ TRONG PHÒNG!");
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
        }
        else
        {
            if (windowCloseSound != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(windowCloseSound, windowSfxVolume);
            }
        }

        SyncAllWindowAmbience();
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
        fadeCoroutine = null;
    }
}
