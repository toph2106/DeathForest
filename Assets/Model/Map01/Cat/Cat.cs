using UnityEngine;
using System.Collections;

public class Cat : MonoBehaviour, IInteractable
{
    private Animator catAnimator;
    private Transform playerTransform;
    private AudioSource catAudioSource;
    private Coroutine animLoopCoroutine;

    [Header("1. Âm Thanh Con Mèo (Audio Clip)")]
    [Tooltip("Kéo file âm thanh của con mèo vào đây")]
    public AudioClip catSound;

    [Header("2. Âm Lượng Tiếng Mèo")]
    [Range(0f, 1f)]
    [Tooltip("Thanh trượt điều chỉnh âm lượng tiếng mèo (Mặc định: 0.8)")]
    public float catSoundVolume = 0.8f;

    [Header("3. Thời Gian Mèo Nhảy (giây)")]
    [Tooltip("Thời gian chú Mèo nhảy nhót liên tục trước khi dừng lại (Ví dụ: 24 giây)")]
    public float animDuration = 24.0f;

    [Header("4. Cấu Hình Vùng Âm Thanh 3D (Spatial Sound Radius)")]
    [Tooltip("Bật âm thanh 3D (Lại gần nghe to, đi xa xa bị nhỏ dần và mất hẳn)")]
    public bool use3DSound = true;
    [Tooltip("Bán kính tối đa nghe thấy tiếng Mèo (Mặc định: 6m - đi xa hơn 6m sẽ không nghe thấy)")]
    public float maxSoundDistance = 6f;

    [Header("5. Tốc Độ Mèo Xoay Nhìn Người Chơi")]
    public float rotationSpeed = 5f;

    void Start()
    {
        catAnimator = GetComponent<Animator>();

        catAudioSource = GetComponent<AudioSource>();
        if (catAudioSource == null) catAudioSource = gameObject.AddComponent<AudioSource>();

        UpdateAudio3DSettings();
        catAudioSource.playOnAwake = false;

        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
    }

    void UpdateAudio3DSettings()
    {
        if (catAudioSource == null) return;

        if (use3DSound)
        {
            catAudioSource.spatialBlend = 1f; // Âm thanh 3D vòm
            catAudioSource.minDistance = 1f;  // Dưới 1m nghe to nhất 100%
            catAudioSource.maxDistance = maxSoundDistance; // Đi xa dần và mất hẳn khi vượt quá maxSoundDistance
            catAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
        else
        {
            catAudioSource.spatialBlend = 0f; // Âm thanh 2D toàn màn hình
        }
    }

    void Update()
    {
        if (playerTransform != null)
        {
            Vector3 direction = playerTransform.position - transform.position;

            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    // TƯƠNG TÁC BẤM PHÍM F VÀO CON MÈO (OIIAOIIA MEME CAT)
    public void Interact()
    {
        UpdateAudio3DSettings();

        // 1. Phát bài nhạc Mèo
        if (catSound != null && catAudioSource != null)
        {
            catAudioSource.Stop();
            catAudioSource.PlayOneShot(catSound, catSoundVolume);
        }

        // 2. Chạy Animation nhảy của Mèo ĐÚNG 24 GIÂY THÌ DỪNG
        if (catAnimator != null)
        {
            if (animLoopCoroutine != null) StopCoroutine(animLoopCoroutine);
            animLoopCoroutine = StartCoroutine(KeepAnimPlayingRoutine(animDuration));
        }
    }

    IEnumerator KeepAnimPlayingRoutine(float totalDuration)
    {
        float elapsed = 0f;
        float animRepeatInterval = 1.0f;

        while (elapsed < totalDuration)
        {
            catAnimator.SetTrigger("PlayAnim");
            yield return new WaitForSeconds(animRepeatInterval);
            elapsed += animRepeatInterval;
        }

        if (catAnimator != null)
        {
            catAnimator.ResetTrigger("PlayAnim");
        }
    }
}