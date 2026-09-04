using UnityEngine;

public class ContinuousDoorKnocker : MonoBehaviour
{
    public static ContinuousDoorKnocker Instance { get; private set; }

    [Header("Nguồn Âm Thanh Gõ Cửa 3D")]
    public AudioSource doorAudioSource;
    public AudioClip knockClip;
    [Range(0f, 1f)] public float volume = 1.0f;

    [Tooltip("Khoảng cách thời gian giữa các đợt gõ cửa liên tục (giây, Mặc định: 3.5s)")]
    public float repeatInterval = 3.5f;

    public bool IsKnocking => isKnocking;

    private bool isKnocking = false;
    private float timer = 0f;

    void Awake()
    {
        Instance = this;
        EnsureAudioComponents();
    }

    void Start()
    {
        EnsureAudioComponents();

        isKnocking = false;
        timer = 0f;

        if (doorAudioSource != null)
        {
            doorAudioSource.playOnAwake = false;
            if (doorAudioSource.isPlaying)
            {
                doorAudioSource.Stop();
            }
        }
    }

    public void EnsureAudioComponents()
    {
        // 1. Tự động lấy AudioSource trên chính Object này nếu ô đang trống
        if (doorAudioSource == null)
        {
            doorAudioSource = GetComponent<AudioSource>();
        }

        // 2. Nếu vẫn trống, tự tìm GameObject tên "KnockSound" trong Scene
        if (doorAudioSource == null)
        {
            GameObject knockObj = GameObject.Find("KnockSound");
            if (knockObj != null)
            {
                doorAudioSource = knockObj.GetComponent<AudioSource>();
            }
        }

        // 3. Nếu knockClip trống, thử lấy từ clip gắn sẵn của AudioSource
        if (knockClip == null && doorAudioSource != null && doorAudioSource.clip != null)
        {
            knockClip = doorAudioSource.clip;
        }

        if (doorAudioSource != null)
        {
            doorAudioSource.spatialBlend = 1f;
            doorAudioSource.minDistance = 1f;
            doorAudioSource.maxDistance = 15f;
            doorAudioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// BẮT ĐẦU GÕ CỬA LIÊN TỤC (Được gọi khi mở mắt/ngồi dậy)
    /// </summary>
    public void StartKnocking()
    {
        EnsureAudioComponents();
        isKnocking = true;
        timer = 0f;
        PlayKnock();
        Debug.Log("[ContinuousDoorKnocker] 🚪 BẮT ĐẦU LẶP LẠI TIẾNG GÕ CỬA LIÊN TỤC! AudioSource: " + (doorAudioSource != null ? doorAudioSource.name : "Null") + ", Clip: " + (knockClip != null ? knockClip.name : "Null"));
    }

    /// <summary>
    /// DỪNG TIẾNG GÕ CỬA KHI NGƯỜI CHƠI TƯƠNG TÁC CỬA
    /// </summary>
    public void StopKnocking()
    {
        isKnocking = false;
        timer = 0f;
        if (doorAudioSource != null && doorAudioSource.isPlaying)
        {
            doorAudioSource.Stop();
        }
        Debug.Log("[ContinuousDoorKnocker] 🛑 Đã dừng tiếng gõ cửa hoàn toàn!");
    }

    void Update()
    {
        if (!isKnocking) return;

        timer += Time.deltaTime;
        if (timer >= repeatInterval)
        {
            timer = 0f;
            PlayKnock();
        }
    }

    void PlayKnock()
    {
        EnsureAudioComponents();

        if (doorAudioSource != null && knockClip != null)
        {
            doorAudioSource.PlayOneShot(knockClip, volume);
            Debug.Log("[ContinuousDoorKnocker] 🚪 Đã phát tiếng gõ cửa liên tục!");
        }
        else if (knockClip != null)
        {
            AudioSource.PlayClipAtPoint(knockClip, transform.position, volume);
            Debug.Log("[ContinuousDoorKnocker] 🚪 Phát tiếng gõ qua PlayClipAtPoint!");
        }
        else if (doorAudioSource != null && doorAudioSource.clip != null)
        {
            doorAudioSource.Play();
            Debug.Log("[ContinuousDoorKnocker] 🚪 Phát tiếng gõ qua Play()!");
        }
        else
        {
            Debug.LogWarning("[ContinuousDoorKnocker] ⚠️ Không tìm thấy Clip âm thanh gõ cửa để phát!");
        }
    }
}
