using UnityEngine;

/// <summary>
/// Gắn vào Prefab hoặc GameObject con Stranger trong Minigame:
/// Cơ chế:
/// 1. Khi được Spawn ra: Tự động lao thẳng về phía Player với tốc độ cao.
/// 2. Ngay khi bị Player LIẾC NHÌN TRÚNG (nằm trong tầm mắt Camera và không bị cản tường):
///    -> ĐỨNG BẤT ĐỘNG VĨNH VIỄN (Freeze Permanently) và nằm im/đứng im tại đúng vị trí đó như một bức tượng đá.
/// 3. Nếu Player không kịp nhìn và bị nó chạm trúng -> Kích hoạt Bắt người chơi (Game Over).
/// </summary>
public class StrangerMinigameTarget : MonoBehaviour
{
    [Header("1. Cấu Hình Tốc Độ & Khoảng Cách")]
    [Tooltip("Tốc độ phi về phía người chơi khi chưa bị nhìn (Mặc định: 35 - 50)")]
    public float rushSpeed = 40f;

    [Tooltip("Khoảng cách bắt người chơi (Game Over)")]
    public float killDistance = 2.2f;

    [Tooltip("Độ cao bù trừ để chân bám đất (Mặc định: 0)")]
    public float groundOffset = 0f;

    [Tooltip("Góc xoay bù trừ để dựng đứng mô hình (Mặc định: 0, 0, 0 hoặc -90, 0, 0)")]
    public Vector3 modelRotationOffset = new Vector3(0f, 0f, 0f);

    [Header("2. Âm Thanh (Audio)")]
    [Tooltip("Âm thanh hóa đá / khựng lại khi bị người chơi nhìn trúng")]
    public AudioClip freezeSound;
    [Tooltip("Âm thanh jumpscare khi bắt được người chơi")]
    public AudioClip catchSound;

    [Header("3. Trạng Thái (State)")]
    public bool isPermanentlyFrozen = false;
    public bool hasCaughtPlayer = false;

    // --- Private Variables ---
    private Transform player;
    private Camera mainCam;
    private Animator anim;
    private AudioSource audioSource;
    private CharacterController characterController;
    private StrangerMinigameManager manager;
    private bool isInitialized = false;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D Sound
        audioSource.playOnAwake = false;

        characterController = GetComponent<CharacterController>();
    }

    public void Initialize(Transform targetPlayer, StrangerMinigameManager gameManager, float speed, float gOffset, Vector3 rotOffset, bool useCustomScale, Vector3 customScale)
    {
        player = targetPlayer;
        manager = gameManager;
        rushSpeed = speed;
        groundOffset = gOffset;
        modelRotationOffset = rotOffset;

        if (useCustomScale)
        {
            transform.localScale = customScale;
        }

        isPermanentlyFrozen = false;
        hasCaughtPlayer = false;
        isInitialized = true;

        FindMainCamera();

        if (anim != null) anim.speed = 1f;
        if (characterController != null) characterController.enabled = true;
    }

    void Start()
    {
        FindMainCamera();
        if (player == null) FindPlayer();
    }

    void FindMainCamera()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null)
        {
            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null) mainCam = cam;
        }
    }

    void FindPlayer()
    {
        GameObject pl = GameObject.FindGameObjectWithTag("Player");
        if (pl != null) player = pl.transform;
        else
        {
            MovePl movePl = Object.FindFirstObjectByType<MovePl>();
            if (movePl != null) player = movePl.transform;
        }
    }

    void Update()
    {
        // Nếu chưa khởi tạo xong hoặc đã bị phong ấn đứng im vĩnh viễn hoặc đã bắt người chơi -> Dừng hoàn toàn
        if (!isInitialized || isPermanentlyFrozen || hasCaughtPlayer) return;

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (mainCam == null)
        {
            FindMainCamera();
            if (mainCam == null) return;
        }

        // 1. KIỂM TRA XEM PLAYER CÓ ĐANG NHÌN VÀO CON NÀY KHÔNG
        if (CheckIfLookedAtByPlayer())
        {
            FreezePermanently();
            return;
        }

        // 2. NẾU CHƯA BỊ NHÌN: LAO THẲNG VỀ PHÍA PLAYER
        RushToPlayer();

        // 3. KIỂM TRA ĐÃ CHẠM VÀO PLAYER CHƯA
        CheckCatchPlayer();
    }

    /// <summary>
    /// Kiểm tra xem đối tượng có nằm trong khung nhìn của Camera và không bị che khuất không
    /// </summary>
    private bool CheckIfLookedAtByPlayer()
    {
        float checkHeight = (characterController != null) ? characterController.height * 0.75f : 1.5f;
        Vector3 checkPoint = transform.position + Vector3.up * checkHeight;
        Vector3 viewportPoint = mainCam.WorldToViewportPoint(checkPoint);

        // Nằm trong khung nhìn Camera
        bool onScreen = viewportPoint.z > 0 &&
                        viewportPoint.x > -0.05f && viewportPoint.x < 1.05f &&
                        viewportPoint.y > -0.05f && viewportPoint.y < 1.05f;

        if (onScreen)
        {
            Vector3 camPos = mainCam.transform.position;
            Vector3 dirToTarget = (checkPoint - camPos);
            float dist = dirToTarget.magnitude;

            // Bắn tia kiểm tra xem có bị tường che không
            if (Physics.Raycast(camPos, dirToTarget.normalized, out RaycastHit hit, dist))
            {
                // Nếu tia chạm trúng vật cản khác (không phải Stranger và không phải Player) -> Bị che khuất
                if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform) && !hit.collider.CompareTag("Player"))
                {
                    return false;
                }
            }

            // ĐANG ĐƯỢC NHÌN TRỰC DIỆN!
            return true;
        }

        return false;
    }

    /// <summary>
    /// ĐỨNG BẤT ĐỘNG VĨNH VIỄN TẠI VỊ TRÍ HIỆN TẠI (ĐÃ BỊ PHONG ẤN THÀNH TƯỢNG ĐÁ)
    /// </summary>
    public void FreezePermanently()
    {
        if (isPermanentlyFrozen) return;
        isPermanentlyFrozen = true;

        // Dừng toàn bộ hoạt ảnh
        if (anim != null) anim.speed = 0f;

        // Phát âm thanh đóng băng / hóa đá
        if (freezeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(freezeSound, 1.0f);
        }

        Debug.Log($"[StrangerMinigameTarget] 🗿 {gameObject.name} ĐÃ BỊ NHÌN TRÚNG! ĐỨNG BẤT ĐỘNG VĨNH VIỄN TẠI CHỖ!");

        // Thông báo cho Minigame Manager biết con này đã bị tiêu diệt/phong ấn
        if (manager != null)
        {
            manager.OnTargetFrozen(this);
        }
    }

    /// <summary>
    /// Lao thẳng về phía Player với tốc độ cao
    /// </summary>
    private void RushToPlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position);
        dirToPlayer.y = 0f;

        if (dirToPlayer.sqrMagnitude < 0.01f) return;
        dirToPlayer.Normalize();

        if (characterController != null && characterController.enabled)
        {
            characterController.SimpleMove(dirToPlayer * rushSpeed);
        }
        else
        {
            Vector3 nextPos = transform.position + dirToPlayer * rushSpeed * Time.deltaTime;

            // Bám sát mặt đất
            RaycastHit[] hits = Physics.RaycastAll(nextPos + Vector3.up * 50f, Vector3.down, 100f);
            float bestY = -9999f;
            bool foundGround = false;

            foreach (var h in hits)
            {
                if (h.collider.CompareTag("Player") || h.collider.gameObject == gameObject || h.collider.transform.IsChildOf(transform)) continue;
                if (h.collider.isTrigger) continue;
                if (h.point.y > bestY) { bestY = h.point.y; foundGround = true; }
            }

            nextPos.y = foundGround
                ? Mathf.Lerp(transform.position.y, bestY + groundOffset, 15f * Time.deltaTime)
                : Mathf.Lerp(transform.position.y, player.position.y + groundOffset, 15f * Time.deltaTime);

            transform.position = nextPos;
        }

        // Xoay mặt thẳng về phía Player theo trục đứng Y
        if (dirToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion yawRot = Quaternion.LookRotation(dirToPlayer, Vector3.up);
            transform.rotation = yawRot * Quaternion.Euler(modelRotationOffset);
        }
    }

    private void CheckCatchPlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= killDistance)
        {
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        if (hasCaughtPlayer) return;
        hasCaughtPlayer = true;

        if (anim != null) anim.speed = 0f;

        if (catchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(catchSound, 1.0f);
        }

        Debug.Log("💀 STRANGER TRONG MINIGAME ĐÃ BẮT ĐƯỢC BẠN!");

        if (manager != null)
        {
            manager.OnPlayerCaught(this);
        }
    }
}
