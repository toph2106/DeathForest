using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Quản lý Minigame 'Stranger Zone':
/// 1. Khi Player ở TRONG ZONE: Kích hoạt minigame liên tục (Endless).
///    - Chờ enterZoneDelay (Mặc định 3.0s) tạo sự tĩnh lặng rùng rợn trước khi con đầu tiên phi tới.
///    - Tính toán vị trí spawn NGHIÊM NGẶT NẰM TRỌN TRONG ZONE (không bao giờ spawn ra ngoài vách núi).
///    - Cứ mỗi lần Player nhìn trúng làm 1 con hóa đá ➔ 2s sau con tiếp theo từ hướng khác lại phi tới.
///    - Càng ở lại lâu trong Zone ➔ Càng nhiều tượng Stranger hóa đá xuất hiện xung quanh.
/// 2. Khi Player RỜI KHỎI ZONE: Tự động dừng spawn ngay lập tức.
/// 3. Khi Player QUAY LẠI ZONE: Tiếp tục kích hoạt vòng lặp spawn.
/// </summary>
public class StrangerMinigameManager : MonoBehaviour
{
    [Header("1. Cấu Hình Đối Tượng (Prefabs & Player)")]
    [Tooltip("Kéo Prefab con Stranger (death_forest_-_stranger) vào đây")]
    public GameObject strangerPrefab;

    [Tooltip("Player Transform (Nếu để trống sẽ tự động tìm Player)")]
    public Transform playerTransform;

    [Header("2. Chế Độ Vùng Kích Hoạt (Zone Settings)")]
    [Tooltip("Chế độ Zone Vô Tận: Ở trong vùng thì spawn liên tục không giới hạn, ra khỏi vùng là dừng ngay")]
    public bool isZoneEndlessMode = true;

    [Tooltip("Thời gian chờ (giây) sau khi Player vừa bước chân vào Zone trước khi con đầu tiên xuất hiện (Mặc định: 3.0s)")]
    public float enterZoneDelay = 3.0f;

    [Tooltip("Khoảng cách đệm an toàn từ mép ranh giới Zone vào trong (mét - Mặc định: 4.0m để tránh spawn dính vách núi)")]
    public float zoneEdgePadding = 4.0f;

    [Tooltip("Tự động bắt đầu ngay khi Play (nếu không dùng Trigger Collider của Zone)")]
    public bool autoStartOnPlay = false;

    [Tooltip("Xóa toàn bộ tượng cũ khi Player rời khỏi Zone (nếu tắt: giữ nguyên làm bãi tượng đá ma quái)")]
    public bool clearStatuesOnExit = false;

    [Header("3. Cấu Hình Spawn & Độ Khó")]
    [Tooltip("Khoảng cách Spawn xung quanh Player (Mặc định: 25 - 35 mét)")]
    public float spawnDistance = 25f;

    [Tooltip("Ưu tiên Spawn phía sau lưng Player để tạo kịch tính (buộc Player phải quay lại tìm)")]
    public bool spawnBehindPlayer = true;

    [Tooltip("Góc lệch phía sau lưng (Độ: ví dụ 60 độ phía sau lưng)")]
    public float behindAngleSpread = 60f;

    [Tooltip("Thời gian tĩnh (nghỉ ngơi) sau khi 1 con bị phong ấn xong mới Spawn con tiếp theo (giây - Mặc định: 2.0s)")]
    public float delayBetweenSpawns = 2.0f;

    [Tooltip("Tốc độ phi của Stranger trong Minigame (Mặc định: 20 - 40)")]
    public float statueSpeed = 20f;

    [Tooltip("Độ cao Spawn trên không trung so với mặt đất (Mặc định: 3.0m - có thể tăng lên 5m, 10m tùy ý)")]
    public float spawnHeightOffset = 3.0f;

    [Tooltip("Tốc độ rơi từ trên trời xuống đất / Trọng lực (Mặc định: 35 - 50 để quái rơi cắm thẳng xuống đất thật nhanh)")]
    public float fallSpeed = 35.0f;

    [Tooltip("Tổng số tượng cần phong ấn nếu TẮT chế độ Zone Endless (Mặc định: 5 con)")]
    public int totalStatuesToWin = 5;

    [Header("4. Âm Thanh (Audio)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh cảnh báo khi một con Stranger mới vừa xuất hiện")]
    public AudioClip spawnAlertSound;
    [Tooltip("Âm thanh chiến thắng khi phong ấn đủ số lượng (khi tắt Endless)")]
    public AudioClip winSound;
    [Tooltip("Âm thanh thua cuộc khi bị bắt")]
    public AudioClip loseSound;

    [Header("5. Giao Diện (UI - Tùy Chọn)")]
    [Tooltip("Kéo TextMeshProUGUI hiển thị số lượng (ví dụ: 'Tượng đã phong ấn: 5')")]
    public TMPro.TextMeshProUGUI progressTextUI;

    [Header("6. Chế Độ Thử Nghiệm (Test Mode)")]
    [Tooltip("Tắt (Bỏ tích) để không bị Thua cuộc khi đang test demo. Bật (Tích chọn) khi muốn kích hoạt tính năng Game Over thực tế.")]
    public bool enableGameOver = false;

    [Header("7. Sự Kiện (Events)")]
    public UnityEvent onMinigameStart;
    public UnityEvent<int, int> onProgressUpdated;
    public UnityEvent onMinigameWin;
    public UnityEvent onMinigameLose;

    // --- Private State ---
    private int currentFrozenCount = 0;
    private int currentSpawnedCount = 0;
    private bool isGameActive = false;
    private bool isGameWon = false;
    private bool isGameLost = false;

    private List<GameObject> activeStatues = new List<GameObject>();
    private Coroutine spawnLoopCoroutine;
    private Collider zoneCollider;

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
    }

    void Start()
    {
        FindPlayer();

        // Nếu bật autoStartOnPlay hoặc không có BoxCollider trigger thì chạy luôn
        if (autoStartOnPlay || zoneCollider == null)
        {
            StartMinigame(0.5f);
        }
    }

    void FindPlayer()
    {
        if (playerTransform != null) return;
        GameObject pl = GameObject.FindGameObjectWithTag("Player");
        if (pl != null) playerTransform = pl.transform;
        else
        {
            MovePl movePl = Object.FindFirstObjectByType<MovePl>();
            if (movePl != null) playerTransform = movePl.transform;
        }
    }

    // ==================== ZONE TRIGGER TRIGGER ====================

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            Debug.Log($"[StrangerMinigameManager] ⚠️ Player đã bước vào Zone Mini-game -> Chờ {enterZoneDelay}s rồi kích hoạt dồn dập!");
            StartMinigame(enterZoneDelay);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            Debug.Log("[StrangerMinigameManager] 🟢 Player đã rời khỏi Zone Mini-game -> Dừng spawn!");
            StopMinigame();
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        return other.CompareTag("Player") || other.GetComponent<MovePl>() != null || other.GetComponentInParent<MovePl>() != null;
    }

    // ==================== FLOW CONTROL ====================

    /// <summary>
    /// Kích hoạt Minigame với độ trễ tùy chỉnh
    /// </summary>
    public void StartMinigame()
    {
        StartMinigame(enterZoneDelay);
    }

    public void StartMinigame(float customDelay)
    {
        FindPlayer();
        if (playerTransform == null)
        {
            Debug.LogError("[StrangerMinigameManager] Không tìm thấy Player để bắt đầu Minigame!");
            return;
        }

        isGameActive = true;
        isGameWon = false;
        isGameLost = false;

        UpdateUI();
        onMinigameStart?.Invoke();

        float initialDelay = (customDelay >= 0f) ? customDelay : enterZoneDelay;
        Debug.Log($"[StrangerMinigameManager] 🎮 BẮT ĐẦU VÒNG LẶP MINIGAME STRANGER! Quái đầu tiên sẽ xuất hiện sau {initialDelay}s...");

        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
        spawnLoopCoroutine = StartCoroutine(SpawnNextStatueRoutine(initialDelay));
    }

    /// <summary>
    /// Tạm dừng Minigame khi Player đi ra khỏi Zone
    /// </summary>
    public void StopMinigame()
    {
        isGameActive = false;

        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }

        // Xử lý các con đang phi dở dang khi Player ra ngoài
        for (int i = activeStatues.Count - 1; i >= 0; i--)
        {
            if (activeStatues[i] != null)
            {
                StrangerBehavior ai = activeStatues[i].GetComponent<StrangerBehavior>();
                if (ai != null && !ai.isPermanentlyFrozen)
                {
                    // Con nào đang phi dở chưa hóa đá -> Xóa bỏ để tránh đuổi ra ngoài zone
                    Destroy(activeStatues[i]);
                    activeStatues.RemoveAt(i);
                }
                else if (clearStatuesOnExit)
                {
                    // Nếu bật tùy chọn xóa sạch tượng khi rời đi
                    Destroy(activeStatues[i]);
                    activeStatues.RemoveAt(i);
                }
            }
        }
    }

    IEnumerator SpawnNextStatueRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        if (!isGameActive || isGameWon || isGameLost) yield break;

        SpawnOneStatue();
    }

    void SpawnOneStatue()
    {
        if (strangerPrefab == null)
        {
            Debug.LogError("[StrangerMinigameManager] Chưa kéo strangerPrefab vào Manager!");
            return;
        }

        currentSpawnedCount++;

        // 1. TÍNH VỊ TRÍ SPAWN NẰM CHUẨN XÁC TRONG ZONE
        Vector3 spawnPos = CalculateSpawnPosition();

        // 2. INSTANTIATE VÀ GIỮ NGUYÊN 100% SCALE GỐC CỦA PREFAB
        GameObject statueObj = Instantiate(strangerPrefab, spawnPos, Quaternion.identity);
        statueObj.name = $"Stranger_Minigame_Target_{currentSpawnedCount}";
        statueObj.transform.localScale = strangerPrefab.transform.localScale;
        activeStatues.Add(statueObj);

        // 3. SỬ DỤNG TRỰC TIẾP STRANGER BEHAVIOR VỚI CHẾ ĐỘ PHONG ẤN VĨNH VIỄN
        StrangerBehavior strangerAI = statueObj.GetComponent<StrangerBehavior>();
        if (strangerAI == null) strangerAI = statueObj.AddComponent<StrangerBehavior>();

        strangerAI.enabled = true;
        strangerAI.fallSpeed = fallSpeed;
        strangerAI.InitializeForMinigame(playerTransform, this, statueSpeed);

        // 4. ÂM THANH CẢNH BÁO
        if (spawnAlertSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(spawnAlertSound, 0.8f);
        }

        Debug.Log($"[StrangerMinigameManager] ⚡ Đã Spawn Stranger #{currentSpawnedCount} bên trong Zone!");
    }

    /// <summary>
    /// Tính toán vị trí spawn quanh Player nhưng ĐẢM BẢO 100% NẰM TRỌN TRONG ZONE COLLIDER
    /// </summary>
    private Vector3 CalculateSpawnPosition()
    {
        Vector3 playerPos = playerTransform.position;
        Vector3 playerForward = playerTransform.forward;
        playerForward.y = 0f;
        if (playerForward.sqrMagnitude < 0.01f) playerForward = Vector3.forward;
        playerForward.Normalize();

        Vector3 bestSpawnPos = playerPos - playerForward * spawnDistance;

        // 1. NẾU CÓ ZONE COLLIDER: TÌM HƯỚNG SPAWN NẰM TRỌN TRONG ZONE
        if (zoneCollider != null)
        {
            bool foundValidInsideZone = false;
            float baseAngle = spawnBehindPlayer ? 180f : 0f;

            // Thử quét 12 góc khác nhau quanh người chơi để tìm vị trí thoáng bên trong Zone
            for (int i = 0; i < 12; i++)
            {
                float testAngle;
                if (i == 0)
                {
                    testAngle = baseAngle;
                }
                else
                {
                    float angleSpread = spawnBehindPlayer ? behindAngleSpread : 180f;
                    float sign = (i % 2 == 0) ? 1f : -1f;
                    float step = (i + 1) / 2f / 6f;
                    testAngle = baseAngle + sign * step * angleSpread;
                }

                Quaternion rot = Quaternion.Euler(0f, testAngle, 0f);
                Vector3 candidateDir = rot * playerForward;
                Vector3 candidatePos = playerPos + candidateDir * spawnDistance;

                if (IsInsideZone(candidatePos, zoneEdgePadding))
                {
                    bestSpawnPos = candidatePos;
                    foundValidInsideZone = true;
                    break;
                }
            }

            // Nếu người chơi đứng quá sát vách khiến tất cả các góc đều lọt ra ngoài -> Ép điểm vào trong Zone
            if (!foundValidInsideZone)
            {
                bestSpawnPos = ClampPositionInsideZone(bestSpawnPos, zoneEdgePadding);
            }
        }
        else
        {
            // Nếu không gắn Collider -> Random góc thông thường
            float spawnAngle = spawnBehindPlayer 
                ? 180f + Random.Range(-behindAngleSpread, behindAngleSpread) 
                : Random.Range(0f, 360f);

            Quaternion rot = Quaternion.Euler(0f, spawnAngle, 0f);
            bestSpawnPos = playerPos + (rot * playerForward) * spawnDistance;
        }

        // 2. TÌM ĐỘ CAO MẶT ĐẤT CHUẨN XÁC
        RaycastHit[] hits = Physics.RaycastAll(bestSpawnPos + Vector3.up * 100f, Vector3.down, 200f);
        float bestY = playerPos.y;
        bool found = false;

        foreach (var h in hits)
        {
            if (h.collider.CompareTag("Player") || h.collider.isTrigger) continue;
            if (h.point.y > bestY - 50f)
            {
                bestY = h.point.y;
                found = true;
            }
        }

        bestSpawnPos.y = (found ? bestY : playerPos.y) + spawnHeightOffset;

        return bestSpawnPos;
    }

    /// <summary>
    /// Kiểm tra 1 tọa độ có nằm hoàn toàn bên trong ranh giới của Zone hay không
    /// </summary>
    private bool IsInsideZone(Vector3 worldPos, float padding)
    {
        if (zoneCollider == null) return true;

        BoxCollider box = zoneCollider as BoxCollider;
        if (box != null)
        {
            Vector3 localPos = box.transform.InverseTransformPoint(worldPos);
            Vector3 halfSize = (box.size * 0.5f) - Vector3.one * padding;
            if (halfSize.x < 1f) halfSize.x = 1f;
            if (halfSize.z < 1f) halfSize.z = 1f;

            Vector3 center = box.center;
            return Mathf.Abs(localPos.x - center.x) <= halfSize.x &&
                   Mathf.Abs(localPos.z - center.z) <= halfSize.z;
        }

        Bounds b = zoneCollider.bounds;
        return worldPos.x >= b.min.x + padding && worldPos.x <= b.max.x - padding &&
               worldPos.z >= b.min.z + padding && worldPos.z <= b.max.z - padding;
    }

    /// <summary>
    /// Tự động ép vị trí lọt vào bên trong Zone an toàn
    /// </summary>
    private Vector3 ClampPositionInsideZone(Vector3 worldPos, float padding)
    {
        if (zoneCollider == null) return worldPos;

        BoxCollider box = zoneCollider as BoxCollider;
        if (box != null)
        {
            Vector3 localPos = box.transform.InverseTransformPoint(worldPos);
            Vector3 halfSize = (box.size * 0.5f) - Vector3.one * padding;
            if (halfSize.x < 1f) halfSize.x = 1f;
            if (halfSize.z < 1f) halfSize.z = 1f;

            Vector3 center = box.center;
            localPos.x = Mathf.Clamp(localPos.x, center.x - halfSize.x, center.x + halfSize.x);
            localPos.z = Mathf.Clamp(localPos.z, center.z - halfSize.z, center.z + halfSize.z);

            return box.transform.TransformPoint(localPos);
        }

        Bounds b = zoneCollider.bounds;
        worldPos.x = Mathf.Clamp(worldPos.x, b.min.x + padding, b.max.x - padding);
        worldPos.z = Mathf.Clamp(worldPos.z, b.min.z + padding, b.max.z - padding);
        return worldPos;
    }

    /// <summary>
    /// Được gọi bởi StrangerBehavior khi bị Player nhìn trúng và phong ấn vĩnh viễn
    /// </summary>
    public void OnStrangerFrozen(StrangerBehavior stranger)
    {
        if (!isGameActive || isGameLost) return;

        currentFrozenCount++;
        UpdateUI();
        onProgressUpdated?.Invoke(currentFrozenCount, totalStatuesToWin);

        Debug.Log($"[StrangerMinigameManager] 🗿 ĐÃ PHONG ẤN THÀNH CÔNG! Tổng số tượng hiện có: {currentFrozenCount}");

        // Nếu KHÔNG BẬT chế độ Endless và đã đủ số lượng -> Chiến thắng
        if (!isZoneEndlessMode && currentFrozenCount >= totalStatuesToWin)
        {
            WinMinigame();
        }
        else
        {
            // Trong chế độ Zone Endless: Ở trong vùng là spawn dồn dập sau mỗi delayBetweenSpawns
            if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = StartCoroutine(SpawnNextStatueRoutine(delayBetweenSpawns));
        }
    }

    public void OnPlayerCaught()
    {
        OnPlayerCaught((StrangerBehavior)null);
    }

    public void OnPlayerCaught(StrangerBehavior stranger)
    {
        if (!isGameActive || isGameWon || isGameLost) return;

        // NẾU ĐANG Ở CHẾ ĐỘ TEST (CHƯA BẬT GAME OVER): BỎ QUA THUA CUỘC, TỰ ĐỘNG HÓA ĐÁ VÀ TIẾP TỤC SPAWN
        if (!enableGameOver)
        {
            Debug.Log("[StrangerMinigameManager] ⚠️ Stranger chạm vào Player (Chế độ Test: Bỏ qua Thua cuộc -> Tự động hóa đá và spawn tiếp sau 2s!)");
            if (stranger != null)
            {
                OnStrangerFrozen(stranger);
            }
            else
            {
                if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
                spawnLoopCoroutine = StartCoroutine(SpawnNextStatueRoutine(delayBetweenSpawns));
            }
            return;
        }

        isGameActive = false;
        isGameLost = true;

        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);

        if (loseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(loseSound, 1.0f);
        }

        Debug.Log("[StrangerMinigameManager] 💀 THUA CUỘC! Stranger đã bắt được bạn!");
        onMinigameLose?.Invoke();
    }

    public void OnPlayerCaught(StrangerMinigameTarget target)
    {
        OnPlayerCaught((StrangerBehavior)null);
    }

    public void OnTargetFrozen(StrangerMinigameTarget target)
    {
        OnStrangerFrozen(null);
    }

    private void WinMinigame()
    {
        isGameActive = false;
        isGameWon = true;

        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);

        if (winSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(winSound, 1.0f);
        }

        Debug.Log("[StrangerMinigameManager] 🏆 CHIẾN THẮNG MINIGAME! Toàn bộ tượng Stranger đã bị phong ấn!");
        onMinigameWin?.Invoke();
    }

    void UpdateUI()
    {
        if (progressTextUI != null)
        {
            if (isZoneEndlessMode)
                progressTextUI.text = $"Tượng đã phong ấn: {currentFrozenCount}";
            else
                progressTextUI.text = $"Phong ấn: {currentFrozenCount} / {totalStatuesToWin}";
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerTransform.position, spawnDistance);

        if (spawnBehindPlayer)
        {
            Gizmos.color = Color.red;
            Vector3 back = -playerTransform.forward;
            Gizmos.DrawRay(playerTransform.position, back * spawnDistance);
        }
    }
}
