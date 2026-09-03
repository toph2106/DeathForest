using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Script Jumpscare Quái Gari chạy vụt ngang qua cửa sổ / hàng rào
/// Kích hoạt khi người chơi bước vào Box Trigger (sau khi đã lấy chìa khóa từ Búp Bê)
/// </summary>
[RequireComponent(typeof(Collider))]
public class GariDoorPassJumpscare : MonoBehaviour
{
    [Header("1. Đối Tượng Quái Gari")]
    [Tooltip("Kéo GameObject Gari (con quái vật) vào đây")]
    public GameObject gariObject;

    [Tooltip("Kéo AnimationClip chạy 'ENM_GARI_run A' vào đây")]
    public AnimationClip runClip;

    [Tooltip("Tốc độ phát Animation (Mặc định: 1.0)")]
    public float animSpeedMultiplier = 1.0f;

    [Header("2. Điểm Xuất Phát & Đích Đến (Transform Points)")]
    [Tooltip("Kéo GameObject Điểm Đầu (SP) vào đây")]
    public Transform startPoint;

    [Tooltip("Kéo GameObject Điểm Cuối (EP) vào đây")]
    public Transform endPoint;

    [Header("3. Cấu Hình Tốc Độ & Hướng")]
    [Tooltip("Tốc độ chạy vụt qua cửa (m/s - Mặc định: 25.0)")]
    public float moveSpeed = 25.0f;

    [Tooltip("Tự động xoay mặt Gari theo hướng từ SP sang EP (Nếu tắt sẽ lấy theo góc xoay của SP)")]
    public bool autoRotateTowardsTarget = true;

    [Header("4. Điều Kiện Kích Hoạt (Condition)")]
    [Tooltip("Chỉ kích hoạt jumpscare sau khi người chơi đã lấy được chìa khóa trong con búp bê")]
    public bool requireKeyFromDoll = true;

    [Tooltip("Tên chìa khóa trong túi đồ để kiểm tra (Mặc định: 'KhoaDen' / 'KeyBL')")]
    public string keyItemName = "KhoaDen";

    [Tooltip("Chỉ kích hoạt 1 lần duy nhất")]
    public bool triggerOnce = true;

    [Header("5. Âm Thanh Jumpscare (Audio SFX)")]
    [Tooltip("Âm thanh dọa ma / tiếng quái chạy / tiếng thở khi vụt qua cửa (Tùy chọn)")]
    public AudioClip jumpscareSound;

    [Range(0f, 1f)] public float soundVolume = 1.0f;

    [Header("6. Sau Khi Chạy Xong")]
    [Tooltip("Tự động ẩn Gari sau khi chạy qua khỏi cửa")]
    public bool disableGariAfterPass = true;

    [Tooltip("Thời gian chờ trước khi ẩn Gari (giây)")]
    public float vanishDelay = 0.2f;

    private bool isTriggered = false;
    private AudioSource audioSource;
    private List<PlayableGraph> activeGraphs = new List<PlayableGraph>();
    private List<AnimationClipPlayable> activePlayables = new List<AnimationClipPlayable>();

    void Start()
    {
        // Đảm bảo Collider là dạng Trigger
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0.5f;
        audioSource.playOnAwake = false;

        // Ban đầu ẩn Gari đi và đặt sẵn ở vị trí startPoint
        if (gariObject != null && startPoint != null)
        {
            gariObject.transform.position = startPoint.position;
            if (!autoRotateTowardsTarget)
            {
                gariObject.transform.rotation = startPoint.rotation;
            }
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

        // 1. Kiểm tra người chơi
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
                Debug.Log("[GariJumpscare] 🔒 Người chơi bước vào trigger nhưng CHƯA lấy chìa khóa búp bê -> Chưa kích hoạt.");
                return;
            }
        }

        // 3. Kích hoạt Jumpscare!
        if (triggerOnce) isTriggered = true;
        StartCoroutine(JumpscareSequenceRoutine());
    }

    private IEnumerator JumpscareSequenceRoutine()
    {
        if (gariObject == null)
        {
            Debug.LogWarning("[GariJumpscare] ⚠️ Chưa kéo GameObject Gari vào Inspector!");
            yield break;
        }

        if (startPoint == null || endPoint == null)
        {
            Debug.LogWarning("[GariJumpscare] ⚠️ Chưa kéo đủ Start Point (SP) và End Point (EP) vào Inspector!");
            yield break;
        }

        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;

        Debug.Log($"[GariJumpscare] 👻 KÍCH HOẠT! Gari chạy từ {startPos} đến {endPos}");

        // 1. Hiện Gari và đặt về tọa độ xuất phát
        gariObject.SetActive(true);
        gariObject.transform.position = startPos;

        // Đảm bảo tất cả Mesh Renderer hiển thị rõ
        Renderer[] rends = gariObject.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = true;

        if (autoRotateTowardsTarget)
        {
            Vector3 lookDir = (endPos - startPos).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                gariObject.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        else
        {
            gariObject.transform.rotation = startPoint.rotation;
        }

        // 2. Phát Animation chạy vụt qua
        PlayGariAnimation();

        // 3. Phát âm thanh Jumpscare
        if (jumpscareSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpscareSound, soundVolume);
        }

        // 4. Di chuyển mượt mà từ startPos đến endPos và LOOP ANIMATION LIÊN TỤC
        float totalDist = Vector3.Distance(startPos, endPos);
        float currentDist = 0f;
        float animTimer = 0f;

        while (currentDist < totalDist)
        {
            float dt = Time.deltaTime;
            float step = moveSpeed * dt;
            currentDist += step;
            gariObject.transform.position = Vector3.MoveTowards(gariObject.transform.position, endPos, step);

            // BẮT BUỘC LOOP ANIMATION MỖI FRAME THEO MODULO CHIỀU DÀI CLIP
            if (runClip != null && runClip.length > 0.01f)
            {
                animTimer += dt * animSpeedMultiplier;
                double loopTime = animTimer % runClip.length;

                for (int i = 0; i < activePlayables.Count; i++)
                {
                    if (activePlayables[i].IsValid())
                    {
                        activePlayables[i].SetTime(loopTime);
                    }
                }
            }

            yield return null;
        }

        gariObject.transform.position = endPos;

        // 5. Chờ một chút rồi ẩn Gari
        if (vanishDelay > 0f) yield return new WaitForSeconds(vanishDelay);

        CleanupGraphs();

        if (disableGariAfterPass && gariObject != null)
        {
            gariObject.SetActive(false);
        }

        Debug.Log("[GariJumpscare] ✨ Gari đã chạy qua cửa và biến mất thành công!");

        if (triggerOnce)
        {
            gameObject.SetActive(false);
        }
    }

    private void PlayGariAnimation()
    {
        if (runClip == null || gariObject == null) return;

        CleanupGraphs();

        runClip.wrapMode = WrapMode.Loop;

        // 1. Tự động tìm Avatar nếu trên Animator đang bị None
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

        // 2. Tìm hoặc gắn Animator lên đúng cấp độ Model (SSC_ENM_gari)
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

            // Nếu Animator có Controller sẵn, cho chạy trực tiếp state
            if (anim.runtimeAnimatorController != null)
            {
                anim.speed = animSpeedMultiplier;
                anim.Play(0, 0, 0f);
            }

            try
            {
                PlayableGraph graph = PlayableGraph.Create("GariAnim_" + anim.gameObject.name);
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
                Debug.LogWarning($"[GariJumpscare] Lỗi phát Animation trên {anim.gameObject.name}: {ex.Message}");
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

    private void OnDrawGizmos()
    {
        if (startPoint == null || endPoint == null) return;

        // Vẽ điểm xuất phát màu xanh ngọc
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(startPoint.position, 0.8f);

        // Vẽ điểm đích đến màu tím hồng
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(endPoint.position, 0.8f);

        // Vẽ đường chạy màu vàng
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint.position, endPoint.position);
    }
}
