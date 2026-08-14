using UnityEngine;
using System.Collections;

public class CockroachDoorAttackTrigger : MonoBehaviour
{
    [Header("1. Con Gián Mẹ Sẽ Tấn Công (CockroachM)")]
    [Tooltip("Kéo GameObject CockroachM (chứa script CockroachFlyAttack) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM trong Scene!")]
    public CockroachFlyAttack motherCockroach;

    [Header("2. Cài Đặt Kích Hoạt")]
    [Tooltip("Tích chọn để TẮT HOÀN TOÀN collider lúc đầu, chỉ khi gián con biến mất thì mới BẬT LÊN")]
    public bool armOnlyAfterBabiesDisappear = true;

    [Tooltip("Độ trễ sau khi người chơi chạm trigger rồi gián mẹ mới bắt đầu xuất hiện/lao tới (giây)")]
    public float delayBeforeAttack = 0.1f;

    [Tooltip("Âm thanh giật mình / tiếng rùng rợn khi người chơi vừa bước tới gần cửa (Tùy chọn)")]
    public AudioClip jumpScareCueSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.9f;

    [Header("3. Phím Tắt Kích Hoạt Test (Tùy Chọn)")]
    [Tooltip("Bấm phím này để test ngay việc kích hoạt trigger (Mặc định: Phím L)")]
    public KeyCode debugTriggerKey = KeyCode.L;

    private bool isArmed = false;
    private bool hasTriggered = false;
    private AudioSource audioSource;
    private BoxCollider col;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        col = GetComponent<BoxCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        col.isTrigger = true;

        // TẮT COLLIDER NGAY TỪ AWAKE ĐỂ TRÁNH MỌI LỖI VẶT
        if (armOnlyAfterBabiesDisappear)
        {
            isArmed = false;
            col.enabled = false;
        }
        else
        {
            isArmed = true;
            col.enabled = true;
        }
    }

    void Start()
    {
        if (motherCockroach == null)
        {
            motherCockroach = Object.FindFirstObjectByType<CockroachFlyAttack>(FindObjectsInactive.Include);
        }

        if (armOnlyAfterBabiesDisappear)
        {
            isArmed = false;
            if (col != null) col.enabled = false;
        }
    }

    void Update()
    {
        // PHÍM TẮT TEST NHANH (MẶC ĐỊNH: PHÍM L)
        if (debugTriggerKey != KeyCode.None && Input.GetKeyDown(debugTriggerKey))
        {
            Debug.Log($"[CockroachDoorAttackTrigger] ⌨️ Bấm phím [{debugTriggerKey}]! Kích hoạt ngay Gián Mẹ tấn công...");
            TriggerMotherAttack();
        }
    }

    void OnEnable()
    {
        BabyCockroachCrawler.OnAllBabiesDisappeared += HandleBabiesDisappeared;
    }

    void OnDisable()
    {
        BabyCockroachCrawler.OnAllBabiesDisappeared -= HandleBabiesDisappeared;
    }

    void HandleBabiesDisappeared()
    {
        ArmTrigger();
    }

    /// <summary>
    /// Bật kích hoạt Trigger sẵn sàng đón Player bước lại gần cửa (chỉ gọi khi gián con đã biến mất)
    /// </summary>
    public void ArmTrigger()
    {
        isArmed = true;
        gameObject.SetActive(true);
        if (col != null) col.enabled = true;
        Debug.Log("[CockroachDoorAttackTrigger] 🎯 ĐÀN GIÁN CON ĐÃ BIẾN MẤT! TRIGGER CỬA CHÍNH THỨC ĐƯỢC BẬT HOẠT ĐỘNG! Đợi Player bước tới gần cửa...");
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !isArmed) return;

        if (other.CompareTag("Player") || other.GetComponent<MovePl>() != null || other.GetComponent<CharacterController>() != null || other.GetComponentInParent<MovePl>() != null)
        {
            TriggerMotherAttack();
        }
    }

    public void TriggerMotherAttack()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log("[CockroachDoorAttackTrigger] ⚠️ Người chơi đã chạm vào Trigger gần cửa! KÍCH HOẠT GIÁN MẸ TẤN CÔNG!");

        StartCoroutine(ExecuteMotherAttackRoutine());
    }

    IEnumerator ExecuteMotherAttackRoutine()
    {
        if (jumpScareCueSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpScareCueSound, soundVolume);
        }

        if (delayBeforeAttack > 0f)
        {
            yield return new WaitForSeconds(delayBeforeAttack);
        }

        if (motherCockroach == null)
        {
            motherCockroach = Object.FindFirstObjectByType<CockroachFlyAttack>(FindObjectsInactive.Include);
        }

        if (motherCockroach != null)
        {
            motherCockroach.gameObject.SetActive(true);
            motherCockroach.StartCockroachSequence();
        }
        else
        {
            Debug.LogWarning("[CockroachDoorAttackTrigger] Không tìm thấy CockroachFlyAttack trong Scene!");
        }

        if (col != null) col.enabled = false;
    }

    void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            // Trong chế độ Play: chỉ hiện màu đỏ khi đã được BẬT (Armed)
            if (Application.isPlaying && !isArmed) return;

            Gizmos.color = isArmed ? new Color(1f, 0f, 0f, 0.4f) : new Color(0f, 1f, 0.5f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
