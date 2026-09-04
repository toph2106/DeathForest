using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Script Jumpscare Gari 2: Chạm vào Trigger (sau khi đã lấy chìa khóa) -> Gari xuất hiện và lao thẳng vào Camera người chơi!
/// Chạm vào người chơi/camera là biến mất ngay lập tức!
/// </summary>
[RequireComponent(typeof(Collider))]
public class GariCameraChargeJumpscare : MonoBehaviour
{
    [Header("1. Đối Tượng Quái Gari")]
    [Tooltip("Kéo GameObject Gari2 vào đây")]
    public GameObject gariObject;

    [Tooltip("Vị trí xuất phát của Gari (Tùy chọn, để trống sẽ lấy vị trí ban đầu của Gari2)")]
    public Transform spawnPoint;

    [Tooltip("Kéo AnimationClip chạy 'ENM_GARI_run A' vào đây")]
    public AnimationClip runClip;

    [Tooltip("Tốc độ phát Animation (Mặc định: 1.2)")]
    public float animSpeedMultiplier = 1.2f;

    [Header("2. Mục Tiêu Camera Người Chơi")]
    [Tooltip("Kéo Main Camera / Camcorder Camera của người chơi vào đây")]
    public Transform playerCamera;

    [Tooltip("Khoảng cách chạm vào người chơi để lập tức biến mất (mét - Mặc định: 0.8m)")]
    public float touchVanishDistance = 0.8f;

    [Tooltip("Độ cao lệch so với tâm Camera (Mặc định: -0.2m để mặt quái vừa tầm mắt nhìn)")]
    public float heightOffset = -0.2f;

    [Header("3. Cấu Hình Tốc Độ & Lao Tới")]
    [Tooltip("Tốc độ lao thẳng vào Camera (m/s - Mặc định: 32.0 cực nhanh và thót tim)")]
    public float rushSpeed = 32.0f;

    [Header("4. Điều Kiện Kích Hoạt (Condition)")]
    [Tooltip("Chỉ kích hoạt jumpscare sau khi người chơi đã lấy được chìa khóa trong con búp bê")]
    public bool requireKeyFromDoll = true;

    [Tooltip("Tên chìa khóa trong túi đồ để kiểm tra (Mặc định: 'KhoaDen' / 'KeyBL')")]
    public string keyItemName = "KhoaDen";

    [Tooltip("Chỉ kích hoạt 1 lần duy nhất")]
    public bool triggerOnce = true;

    [Header("5. Âm Thanh Jumpscare (Audio SFX)")]
    [Tooltip("Âm thanh tiếng hét / gầm / dọa ma khi lao thẳng vào mặt")]
    public AudioClip jumpscareSound;

    [Range(0f, 1f)] public float soundVolume = 1.0f;

    private bool isTriggered = false;
    private bool hasPlayerTouched = false;
    private AudioSource audioSource;
    private List<PlayableGraph> activeGraphs = new List<PlayableGraph>();
    private List<AnimationClipPlayable> activePlayables = new List<AnimationClipPlayable>();
    private Vector3 initialGariPosition;
    private Quaternion initialGariRotation;

    void Start()
    {
        // Đảm bảo Collider trên Trigger là dạng isTrigger
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Âm thanh 2D vào tai người chơi
        audioSource.playOnAwake = false;

        // Tự động tìm Camera nếu chưa kéo vào Inspector
        if (playerCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
            else
            {
                Camera anyCam = Object.FindFirstObjectByType<Camera>();
                if (anyCam != null) playerCamera = anyCam.transform;
            }
        }

        // Lưu vị trí ban đầu và ẩn Gari đi
        if (gariObject != null)
        {
            if (spawnPoint != null)
            {
                initialGariPosition = spawnPoint.position;
                initialGariRotation = spawnPoint.rotation;
            }
            else
            {
                initialGariPosition = gariObject.transform.position;
                initialGariRotation = gariObject.transform.rotation;
            }

            gariObject.transform.position = initialGariPosition;
            gariObject.transform.rotation = initialGariRotation;

            // Gắn bộ bắt va chạm vật lý lên Gari (nếu Gari có Collider)
            SetupTouchDetector(gariObject);

            gariObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        CleanupGraphs();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        // 1. Kiểm tra người chơi bước vào
        if (!other.CompareTag("Player") && other.GetComponentInParent<MovePl>() == null)
        {
            return;
        }

        // 2. Kiểm tra điều kiện: Đã lấy chìa khóa từ búp bê chưa
        if (requireKeyFromDoll)
        {
            bool hasKey = HangingDollInteraction.IsKeyAcquired;

            if (!hasKey && InventoryManager.Instance != null)
            {
                hasKey = InventoryManager.Instance.HasItem(keyItemName) || 
                         InventoryManager.Instance.HasItem("KeyBL") || 
                         InventoryManager.Instance.HasItem("Key");
            }

            if (!hasKey)
            {
                Debug.Log("[GariJumpscare 2] 🔒 Người chơi bước vào trigger nhưng CHƯA lấy chìa khóa búp bê -> Chưa kích hoạt.");
                return;
            }
        }

        if (triggerOnce) isTriggered = true;
        StartCoroutine(ChargeCameraRoutine());
    }

    private IEnumerator ChargeCameraRoutine()
    {
        Debug.Log("[GariJumpscare 2] 😱 KÍCH HOẠT! Gari2 bắt đầu phi thẳng vào mặt Camera!");

        if (gariObject == null)
        {
            Debug.LogWarning("[GariJumpscare 2] ⚠️ Chưa kéo GameObject Gari vào Inspector!");
            yield break;
        }

        // Tìm lại Camera đang hoạt động nếu bị mất reference
        if (playerCamera == null || !playerCamera.gameObject.activeInHierarchy)
        {
            Camera cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }

        // 1. Hiện Gari và đặt tại vị trí xuất phát
        hasPlayerTouched = false;
        gariObject.SetActive(true);
        gariObject.transform.position = (spawnPoint != null) ? spawnPoint.position : initialGariPosition;

        Renderer[] rends = gariObject.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = true;

        // 2. Phát Animation chạy
        PlayGariAnimation();

        // 3. Phát âm thanh hù dọa
        if (jumpscareSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpscareSound, soundVolume);
        }

        // 4. Lao như tên bắn thẳng vào vị trí mặt của Camera
        float animTimer = 0f;
        while (playerCamera != null && !hasPlayerTouched)
        {
            float dt = Time.deltaTime;
            Vector3 targetFacePos = playerCamera.position + Vector3.up * heightOffset;
            float dist = Vector3.Distance(gariObject.transform.position, targetFacePos);

            // Chạm tới cự ly người chơi -> Biến mất ngay lập tức!
            if (dist <= touchVanishDistance)
            {
                hasPlayerTouched = true;
                break;
            }

            // Luôn hướng mặt về phía Camera
            Vector3 lookDir = (playerCamera.position - gariObject.transform.position).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                gariObject.transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // Phi tới tấp vào Camera
            gariObject.transform.position = Vector3.MoveTowards(
                gariObject.transform.position,
                targetFacePos,
                rushSpeed * dt
            );

            // Loop Animation liên tục
            if (runClip != null && runClip.length > 0.01f)
            {
                animTimer += dt * animSpeedMultiplier;
                double loopTime = animTimer % runClip.length;
                for (int i = 0; i < activePlayables.Count; i++)
                {
                    if (activePlayables[i].IsValid()) activePlayables[i].SetTime(loopTime);
                }
            }

            yield return null;
        }

        // 5. CHẠM VÀO LÀ BIẾN MẤT LUÔN NGAY LẬP TỨC (CHỈ HÙ DỌA, KHÔNG GAME OVER)
        CleanupGraphs();

        if (gariObject != null)
        {
            gariObject.SetActive(false);
        }

        Debug.Log("[GariJumpscare 2] 💥 Gari2 đã chạm vào người chơi và biến mất ngay lập tức!");

        if (triggerOnce)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Được gọi ngay khi Collider của Gari va chạm vật lý với Player
    /// </summary>
    public void NotifyPlayerTouched()
    {
        hasPlayerTouched = true;
    }

    private void SetupTouchDetector(GameObject target)
    {
        GariTouchDetector detector = target.GetComponent<GariTouchDetector>();
        if (detector == null) detector = target.AddComponent<GariTouchDetector>();
        detector.Init(this);

        Collider[] childCols = target.GetComponentsInChildren<Collider>(true);
        foreach (var col in childCols)
        {
            if (col.gameObject == target) continue;
            GariTouchDetector childDet = col.GetComponent<GariTouchDetector>();
            if (childDet == null) childDet = col.gameObject.AddComponent<GariTouchDetector>();
            childDet.Init(this);
        }
    }

    private void PlayGariAnimation()
    {
        if (runClip == null || gariObject == null) return;

        CleanupGraphs();
        runClip.wrapMode = WrapMode.Loop;

        Avatar foundAvatar = null;
        Avatar[] allAvatars = Resources.FindObjectsOfTypeAll<Avatar>();
        foreach (var av in allAvatars)
        {
            if (av != null && av.name.ToLower().Contains("gari"))
            {
                foundAvatar = av;
                break;
            }
        }

        Transform sscChild = null;
        foreach (Transform t in gariObject.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name.Contains("SSC_ENM_gari"))
            {
                sscChild = t;
                break;
            }
        }

        if (sscChild != null && sscChild.GetComponent<Animator>() == null)
        {
            Animator sscAnim = sscChild.gameObject.AddComponent<Animator>();
            if (foundAvatar != null) sscAnim.avatar = foundAvatar;
        }

        Animator[] allAnimators = gariObject.GetComponentsInChildren<Animator>(true);
        if (allAnimators == null || allAnimators.Length == 0)
        {
            Animator directAnim = gariObject.GetComponent<Animator>();
            if (directAnim != null) allAnimators = new Animator[] { directAnim };
        }

        if (allAnimators == null || allAnimators.Length == 0) return;

        foreach (var anim in allAnimators)
        {
            if (anim == null) continue;
            anim.enabled = true;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (anim.avatar == null && foundAvatar != null)
            {
                anim.avatar = foundAvatar;
            }

            if (anim.runtimeAnimatorController != null)
            {
                anim.speed = animSpeedMultiplier;
                anim.Play(0, 0, 0f);
            }

            try
            {
                PlayableGraph graph = PlayableGraph.Create("GariChargeAnim_" + anim.gameObject.name);
                var output = AnimationPlayableOutput.Create(graph, "Animation", anim);
                var clipPlayable = AnimationClipPlayable.Create(graph, runClip);
                clipPlayable.SetSpeed(animSpeedMultiplier);
                clipPlayable.SetDuration(double.MaxValue);
                output.SetSourcePlayable(clipPlayable);

                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                graph.Play();

                activeGraphs.Add(graph);
                activePlayables.Add(clipPlayable);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GariJumpscare 2] Lỗi phát Animation trên {anim.gameObject.name}: {ex.Message}");
            }
        }
    }

    private void CleanupGraphs()
    {
        foreach (var g in activeGraphs)
        {
            if (g.IsValid()) g.Destroy();
        }
        activeGraphs.Clear();
        activePlayables.Clear();
    }
}

/// <summary>
/// Helper Component gắn lên Gari để nhận diện va chạm vật lý với Player
/// </summary>
public class GariTouchDetector : MonoBehaviour
{
    private GariCameraChargeJumpscare owner;

    public void Init(GariCameraChargeJumpscare mainScript)
    {
        owner = mainScript;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null)
        {
            owner.NotifyPlayerTouched();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (owner == null) return;
        if (collision.collider.CompareTag("Player") || collision.collider.GetComponentInParent<MovePl>() != null)
        {
            owner.NotifyPlayerTouched();
        }
    }
}
