using UnityEngine;
using System.Collections;

public class WindowAmbienceController : MonoBehaviour, IInteractable
{
    [Header("1. Nguồn Âm Thanh Thành Phố (City Ambience AudioSource)")]
    [Tooltip("Kéo AudioSource phát tiếng thành phố đêm lặp đi lặp lại vào đây")]
    public AudioSource cityAmbienceAudioSource;

    [Header("2. Mức Âm Lượng Khi Đóng & Mở Cửa Sổ")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng thành phố khi CỬA ĐÓNG (Đã tăng lên 0.35 cho nghe rõ hơn nhưng không át tiếng khác)")]
    public float closedVolume = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng thành phố khi CỬA MỞ (Nên để 0.75 đến 0.85 cho tiếng xe cộ ùa vào chân thực)")]
    public float openedVolume = 0.8f;

    [Tooltip("Thời gian chuyển đổi âm lượng mượt mà khi mở/đóng cửa (giây)")]
    public float volumeFadeDuration = 1.5f;

    [Header("3. Âm Thanh Thao Tác Cửa Sổ & Âm Lượng")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng mở/đóng cửa sổ (Mặc định 0.35 cho êm ái vừa vặn)")]
    public float windowSfxVolume = 0.35f;

    [Tooltip("Kéo file tiếng mở cửa sổ vào đây")]
    public AudioClip windowOpenSound;
    [Tooltip("Kéo file tiếng đóng cửa sổ vào đây (Tùy chọn)")]
    public AudioClip windowCloseSound;

    [Header("4. Chuyển Động Cửa Sổ (Tùy chọn)")]
    [Tooltip("Kéo cánh cửa sổ trượt vào đây để nó tự di chuyển mở ra khi bấm F")]
    public Transform slidingWindowMesh;
    [Tooltip("Khoảng cách cánh cửa trượt sang bên khi mở (Ví dụ: 0.8m)")]
    public Vector3 slideOffset = new Vector3(-0.7f, 0f, 0f);

    private bool isOpen = false;
    private AudioSource sfxAudioSource;
    private Vector3 initialWindowPos;
    private Coroutine fadeCoroutine;

    void Start()
    {
        sfxAudioSource = GetComponent<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();

        sfxAudioSource.spatialBlend = 0f;
        sfxAudioSource.playOnAwake = false;

        if (slidingWindowMesh != null)
        {
            initialWindowPos = slidingWindowMesh.localPosition;
        }

        // Cấu hình ban đầu cho tiếng thành phố khi cửa đang ĐÓNG
        if (cityAmbienceAudioSource != null)
        {
            cityAmbienceAudioSource.loop = true;
            cityAmbienceAudioSource.volume = closedVolume;
            if (!cityAmbienceAudioSource.isPlaying)
            {
                cityAmbienceAudioSource.Play();
            }
        }
    }

    // TƯƠNG TÁC KHI NHÌN CỬA SỔ BẤM PHÍM F
    public void Interact()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            // BẤM F LẦN 1 -> MỞ CỬA SỔ
            if (windowOpenSound != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(windowOpenSound, windowSfxVolume);
            }

            // Tăng âm lượng tiếng thành phố ùa vào phòng
            StartVolumeFade(openedVolume);
        }
        else
        {
            // BẤM F LẦN 2 -> ĐÓNG CỬA SỔ
            if (windowCloseSound != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(windowCloseSound, windowSfxVolume);
            }

            // Giảm âm lượng tiếng thành phố về lại 0.35
            StartVolumeFade(closedVolume);
        }

        // Chuyển động trượt cánh cửa sổ (nếu có gán slidingWindowMesh)
        if (slidingWindowMesh != null)
        {
            StopAllCoroutines();
            Vector3 targetPos = isOpen ? (initialWindowPos + slideOffset) : initialWindowPos;
            StartCoroutine(SlideWindowRoutine(targetPos));
        }
    }

    void StartVolumeFade(float targetVol)
    {
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

    IEnumerator SlideWindowRoutine(Vector3 targetPos)
    {
        float elapsed = 0f;
        float duration = 0.8f;
        Vector3 startPos = slidingWindowMesh.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            slidingWindowMesh.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }

        slidingWindowMesh.localPosition = targetPos;
    }
}
