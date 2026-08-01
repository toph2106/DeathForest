using UnityEngine;

public class PCPowerButton : MonoBehaviour, IInteractable
{
    [Header("1. Kéo Màn Hình 3D / Script InWorldComputerCutscene")]
    [Tooltip("Kéo cái 3D Computer Screen (hoặc Object chứa script InWorldComputerCutscene) vào đây")]
    public InWorldComputerCutscene computerCutscene;

    [Header("2. Âm Thanh Nút Bật/Tắt Case PC (unfa__short-ping.wav)")]
    [Tooltip("Kéo âm thanh click phím nguồn (unfa__short-ping.wav) vào đây")]
    public AudioClip powerClickSound;

    [Header("3. Âm Thanh Quạt PC Chạy Rì Rầm (loud-computer.wav)")]
    [Tooltip("Kéo tiếng quạt PC chạy vòng lặp (loud-computer.wav) vào đây")]
    public AudioClip fanHummingSound;

    [Header("4. Âm Lượng")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng bíp/click nguồn (Mặc định: 0.6)")]
    public float clickVolume = 0.6f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng quạt Case PC chạy rì rầm (Mặc định: 0.5)")]
    public float fanVolume = 0.5f;

    [Header("5. Cấu Hình Vùng Âm Thanh 3D (Spatial Sound Radius)")]
    [Tooltip("Bật âm thanh 3D (Lại gần gầm bàn nghe to, đi ra xa phòng nhỏ dần và mất hẳn)")]
    public bool use3DSound = true;

    [Tooltip("Bán kính tối đa nghe thấy tiếng quạt Case PC (Mặc định: 5m)")]
    public float maxSoundDistance = 5f;

    public static bool IsPCPowerOn { get; private set; } = false;

    private AudioSource audioSource;
    private AudioSource fanAudioSource;

    void Start()
    {
        IsPCPowerOn = false;

        // Source 1: Phát tiếng bíp/click nguồn
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Source 2: Phát tiếng quạt PC chạy vòng lặp
        fanAudioSource = gameObject.AddComponent<AudioSource>();
        fanAudioSource.loop = true;
        fanAudioSource.playOnAwake = false;

        Update3DSoundSettings();
    }

    void Update3DSoundSettings()
    {
        if (use3DSound)
        {
            if (audioSource != null)
            {
                audioSource.spatialBlend = 1f;
                audioSource.minDistance = 0.5f;
                audioSource.maxDistance = maxSoundDistance;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            }

            if (fanAudioSource != null)
            {
                fanAudioSource.spatialBlend = 1f;
                fanAudioSource.minDistance = 0.5f;
                fanAudioSource.maxDistance = maxSoundDistance;
                fanAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            }
        }
        else
        {
            if (audioSource != null) audioSource.spatialBlend = 0f;
            if (fanAudioSource != null) fanAudioSource.spatialBlend = 0f;
        }
    }

    // Khi người chơi nhìn vào Case PC bấm phím F
    public void Interact()
    {
        Update3DSoundSettings();

        if (!IsPCPowerOn)
        {
            // BẤM LẦN 1 -> BẬT MÁY TÍNH
            IsPCPowerOn = true;

            // 1. Phát tiếng bíp nguồn
            if (powerClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(powerClickSound, clickVolume);
            }

            // 2. Bắt đầu phát tiếng quạt PC chạy rì rầm theo volume cài đặt
            if (fanHummingSound != null && fanAudioSource != null)
            {
                fanAudioSource.clip = fanHummingSound;
                fanAudioSource.volume = fanVolume;
                fanAudioSource.Play();
            }

            // 3. Bật sáng màn hình 3D
            if (computerCutscene != null)
            {
                computerCutscene.PowerOnPC();
            }
        }
        else
        {
            // BẤM LẦN 2 -> TẮT MÁY TÍNH
            IsPCPowerOn = false;

            // 1. Phát tiếng bíp tắt nguồn
            if (powerClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(powerClickSound, clickVolume);
            }

            // 2. Dừng tiếng quạt PC
            if (fanAudioSource != null && fanAudioSource.isPlaying)
            {
                fanAudioSource.Stop();
            }

            // 3. Tắt màn hình 3D
            if (computerCutscene != null)
            {
                computerCutscene.PowerOffPC();
            }
        }
    }
}
