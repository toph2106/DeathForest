using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Quản lý Minigame 'Stranger Zone' (Bản nâng cấp bảo vệ an toàn 100%):
/// 1. Kích hoạt khi Player BƯỚC VÀO ZONE (Box Collider Trigger).
/// 2. Khi Player ĐI RA NGOÀI: Tự động dừng spawn ngay lập tức.
/// 3. Khi Player ĐI VÀO LẠI: Tự động khởi động lại vòng lặp spawn mượt mà!
/// 4. Giới hạn tối đa đúng 1 con Stranger phi cùng 1 lúc (maxActiveConcurrent = 1).
/// 5. Giới hạn số lượng tượng đá tối đa để chống tràn RAM / Crash Unity (maxTotalStatues = 8).
/// </summary>
public class StrangerMinigameManager : MonoBehaviour
{
    [Header("1. Cấu Hình Đối Tượng (Prefabs & Player)")]
    [Tooltip("Kéo Prefab con Stranger (death_forest_-_stranger) vào đây")]
    public GameObject strangerPrefab;

    [Tooltip("Player Transform (Nếu để trống sẽ tự động tìm Player)")]
    public Transform playerTransform;

    [Header("2. Chế Độ Vùng Kích Hoạt (Zone Settings)")]
    [Tooltip("Zone bắt buộc phải thỏa mãn 2 điều kiện mới được kích hoạt: 1. Đã mổ xác lấy Gore + 2. Đã nhận Gore vào túi đồ")]
    public bool requireUnlock = true;

    [Tooltip("Trạng thái Zone đã được kích hoạt/mở khóa chưa (True = Quái hoạt động khi vào Zone)")]
    public bool isZoneUnlocked = false;

    [Tooltip("Kiểm tra bắt buộc trong túi đồ (Inventory) phải đang sở hữu vật phẩm Gore/Nội Tạng")]
    public bool requireGoreInInventory = true;

    [Tooltip("Tên vật phẩm Gore cần kiểm tra trong túi đồ (Mặc định: 'Nội Tạng')")]
    public string requiredGoreItemName = "Nội Tạng";

    [Tooltip("Kéo GameObject Cái Xác (chứa CorpseGoreHarvest) vào đây. Nếu để trống code tự tìm trong Scene!")]
    public CorpseGoreHarvest corpseHarvestSource;

    [Tooltip("Chế độ Zone Vô Tận: Ở trong vùng thì spawn liên tục không giới hạn, ra khỏi vùng là dừng ngay")]
    public bool isZoneEndlessMode = true;

    [Tooltip("Thời gian chờ (giây) sau khi Player vừa bước chân vào Zone trước khi con đầu tiên xuất hiện (Mặc định: 3.0s)")]
    public float enterZoneDelay = 3.0f;

    [Tooltip("Khoảng cách đệm an toàn từ mép ranh giới Zone vào trong (mét - Mặc định: 4.0m để tránh spawn dính vách núi)")]
    public float zoneEdgePadding = 4.0f;

    [Tooltip("Tự động bắt đầu ngay khi Play (Mặc định: TẮT để chỉ kích hoạt khi bước vào Trigger Zone)")]
    public bool autoStartOnPlay = false;

    [Tooltip("Xóa toàn bộ tượng cũ khi Player rời khỏi Zone (nếu tắt: giữ nguyên làm bãi tượng đá ma quái)")]
    public bool clearStatuesOnExit = false;

    [Header("3. Giới Hạn An Toàn Chống Crash (Safety Limits)")]
    [Tooltip("Số lượng Stranger được phép phi CÙNG MỘT LÚC (Mặc định: 1 con để không bị loạn hay spam)")]
    public int maxActiveConcurrent = 1;

    [Tooltip("Tổng số tượng đá tối đa được phép tồn tại trong bãi (Mặc định: 8 tượng để bảo vệ hiệu năng/FPS)")]
    public int maxTotalStatues = 8;

    [Header("4. Cấu Hình Spawn & Độ Khó")]
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

    [Tooltip("Độ cao Spawn trên không trung so với mặt đất (Mặc định: 3.0m)")]
    public float spawnHeightOffset = 3.0f;

    [Tooltip("Tốc độ rơi từ trên trời xuống đất / Trọng lực (Mặc định: 35 - 50 để quái rơi cắm thẳng xuống đất thật nhanh)")]
    public float fallSpeed = 35.0f;

    [Tooltip("Tổng số tượng cần phong ấn nếu TẮT chế độ Zone Endless (Mặc định: 5 con)")]
    public int totalStatuesToWin = 5;

    [Header("5. Âm Thanh (Audio)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh cảnh báo khi một con Stranger mới vừa xuất hiện")]
    public AudioClip spawnAlertSound;
    [Tooltip("Âm thanh chiến thắng khi phong ấn đủ số lượng (khi tắt Endless)")]
    public AudioClip winSound;
    [Tooltip("Âm thanh thua cuộc khi bị bắt")]
    public AudioClip loseSound;

    [Header("6. Giao Diện (UI - Tùy Chọn)")]
    [Tooltip("Kéo TextMeshProUGUI hiển thị số lượng (ví dụ: 'Tượng đã phong ấn: 5')")]
    public TMPro.TextMeshProUGUI progressTextUI;

    [Header("7. Chế Độ Thử Nghiệm (Test Mode)")]
    [Tooltip("Tắt (Bỏ tích) để không bị Thua cuộc khi đang test demo. Bật (Tích chọn) khi muốn kích hoạt tính năng Game Over thực tế.")]
    public bool enableGameOver = false;

    [Header("8. Sự Kiện (Events)")]
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
        if (zoneCollider != null) zoneCollider.isTrigger = true;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
    }

    void Start()
    {
        FindPlayer();

        // Chỉ chạy ngay nếu người dùng cố tình bật autoStartOnPlay
        if (autoStartOnPlay)
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

    // ==================== ZONE TRIGGER & UNLOCK CONDITIONS ====================

    /// <summary>
    /// Kiểm tra 2 điều kiện để kích hoạt Zone:
    /// 1. Tương tác LẤY được Gore từ cái xác (CorpseGoreHarvest.HasHarvested / IsGoreHarvested).
    /// 2. Đã nhận được Gore vào trong túi đồ (InventoryManager.HasItem).
    /// </summary>
    public bool CheckUnlockConditions()
    {
        if (!requireUnlock) return true;
        if (isZoneUnlocked) return true;

        // ĐIỀU KIỆN 1: Đã tương tác mổ xác lấy Gore thành công
        bool condition1_Harvested = CorpseGoreHarvest.IsGoreHarvested;
        if (corpseHarvestSource == null)
        {
            corpseHarvestSource = Object.FindFirstObjectByType<CorpseGoreHarvest>();
        }
        if (corpseHarvestSource != null && corpseHarvestSource.HasHarvested)
        {
            condition1_Harvested = true;
        }

        // ĐIỀU KIỆN 2: Người chơi đã thực sự nhận được Gore và đang sở hữu trong túi đồ (Inventory)
        bool condition2_HasGore = false;
        if (requireGoreInInventory)
        {
            if (InventoryManager.Instance != null)
            {
                condition2_HasGore = InventoryManager.Instance.HasItem(requiredGoreItemName)
                                  || InventoryManager.Instance.HasItem("Nội Tạng")
                                  || InventoryManager.Instance.HasItem("Gore")
                                  || InventoryManager.Instance.HasItem("noi tang");
            }
            else
            {
                condition2_HasGore = condition1_Harvested;
            }
        }
        else
        {
            condition2_HasGore = true;
        }

        // ĐỦ CẢ 2 ĐIỀU KIỆN -> MỞ KHÓA ZONE THÀNH CÔNG!
        if (condition1_Harvested && condition2_HasGore)
        {
            isZoneUnlocked = true;
            return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            if (requireUnlock && !isZoneUnlocked)
            {
                if (!CheckUnlockConditions())
                {
                    Debug.Log("[StrangerMinigameManager] 🔒 Zone Stranger đang KHÓA. Cần đủ 2 điều kiện: (1) Mổ xác lấy Gore + (2) Nhận được Gore trong túi đồ. Player đi lại an toàn 100%.");
                    return;
                }
                else
                {
                    isZoneUnlocked = true;
                    Debug.Log("[StrangerMinigameManager] 🔓 Đã đủ 2 điều kiện (Đã mổ xác + Đang có Gore trong túi đồ) -> Mở khóa Zone Stranger thành công!");
                }
            }

            Debug.Log($"[StrangerMinigameManager] ⚠️ Đã đủ điều kiện & Player bước vào Zone Stranger -> Chờ {enterZoneDelay}s rồi kích hoạt Minigame!");
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

    /// <summary>
    /// Mở khóa và kích hoạt Zone minigame (gọi từ CorpseGoreHarvest sau khi lấy đồ hoặc từ UnityEvent)
    /// </summary>
    public void UnlockAndActivateZone()
    {
        if (!CheckUnlockConditions())
        {
            Debug.LogWarning("[StrangerMinigameManager] ⚠️ Chưa đủ 2 điều kiện (Cần cả mổ xác và nhận Gore vào túi đồ) -> Chưa mở khóa Zone.");
            return;
        }

        isZoneUnlocked = true;
        Debug.Log("[StrangerMinigameManager] 🔓 ZONE STRANGER ĐÃ ĐƯỢC MỞ KHÓA THÀNH CÔNG!");

        // Nếu Player đang đứng bên trong Zone khi vừa mổ xác xong -> Bắt đầu Minigame ngay!
        if (IsPlayerInsideZone())
        {
            Debug.Log("[StrangerMinigameManager] ⚡ Player đang đứng trong Zone khi vừa mổ xác -> Kích hoạt Minigame ngay!");
            StartMinigame(enterZoneDelay);
        }
    }

    /// <summary>
    /// Kiểm tra xem Player hiện tại có đang nằm trong phạm vi của Zone hay không
    /// </summary>
    public bool IsPlayerInsideZone()
    {
        FindPlayer();
        if (playerTransform == null) return false;

        if (zoneCollider == null) zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            return zoneCollider.bounds.Contains(playerTransform.position);
        }

        return false;
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        if (other.GetComponent<MovePl>() != null || other.GetComponentInParent<MovePl>() != null) return true;
        if (other.GetComponent<CharacterController>() != null || other.GetComponentInParent<CharacterController>() != null) return true;
        if (other.gameObject.name == "Main" || (other.transform.root != null && other.transform.root.name == "Main")) return true;
        return false;
    }

    // ==================== FLOW CONTROL ====================

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
                    Destroy(activeStatues[i]);
                    activeStatues.RemoveAt(i);
                }
                else if (clearStatuesOnExit)
                {
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

        // KIỂM TRA SỐ LƯỢNG CON ĐANG PHI CÙNG LÚC:
        int runningCount = 0;
        for (int i = activeStatues.Count - 1; i >= 0; i--)
        {
            if (activeStatues[i] == null)
            {
                activeStatues.RemoveAt(i);
                continue;
            }
            StrangerBehavior ai = activeStatues[i].GetComponent<StrangerBehavior>();
            if (ai != null && !ai.isPermanentlyFrozen)
            {
                runningCount++;
            }
        }

        // Nếu đã có con đang phi -> Chờ tiếp, không spawn chồng chéo
        if (runningCount >= maxActiveConcurrent)
        {
            spawnLoopCoroutine = StartCoroutine(SpawnNextStatueRoutine(1.5f));
            yield break;
        }

        SpawnOneStatue();
    }

    void SpawnOneStatue()
    {
        if (strangerPrefab == null)
        {
            Debug.LogError("[StrangerMinigameManager] Chưa kéo strangerPrefab vào Manager!");
            return;
        }

        // DỌN DẸP NẾU QUÁ GIỚI HẠN TỔNG SỐ TƯỢNG (CHỐNG TRÀN BỘ NHỚ)
        while (activeStatues.Count >= maxTotalStatues)
        {
            if (activeStatues[0] != null)
            {
                Destroy(activeStatues[0]);
            }
            activeStatues.RemoveAt(0);
        }

        currentSpawnedCount++;

        // 1. TÍNH VỊ TRÍ SPAWN NẰM TRỌN TRONG ZONE
        Vector3 spawnPos = CalculateSpawnPosition();

        // 2. INSTANTIATE
        GameObject statueObj = Instantiate(strangerPrefab, spawnPos, Quaternion.identity);
        statueObj.name = $"Stranger_Minigame_Target_{currentSpawnedCount}";
        statueObj.transform.localScale = strangerPrefab.transform.localScale;
        activeStatues.Add(statueObj);

        // 3. KHỞI TẠO STRANGER BEHAVIOR
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

    private Vector3 CalculateSpawnPosition()
    {
        Vector3 playerPos = (playerTransform != null) ? playerTransform.position : transform.position;
        Vector3 playerForward = (playerTransform != null) ? playerTransform.forward : Vector3.forward;
        playerForward.y = 0f;
        if (playerForward.sqrMagnitude < 0.01f) playerForward = Vector3.forward;
        playerForward.Normalize();

        Vector3 bestSpawnPos = playerPos - playerForward * spawnDistance;

        if (zoneCollider != null)
        {
            bool foundValidInsideZone = false;
            float baseAngle = spawnBehindPlayer ? 180f : 0f;

            for (int i = 0; i < 12; i++)
            {
                float testAngle;
                if (i == 0) testAngle = baseAngle;
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

            if (!foundValidInsideZone)
            {
                bestSpawnPos = ClampPositionInsideZone(bestSpawnPos, zoneEdgePadding);
            }
        }

        // Raycast xuống mặt đất để lấy độ cao sàn
        RaycastHit groundHit;
        if (Physics.Raycast(bestSpawnPos + Vector3.up * 100f, Vector3.down, out groundHit, 200f))
        {
            bestSpawnPos.y = groundHit.point.y + spawnHeightOffset;
        }
        else
        {
            bestSpawnPos.y = playerPos.y + spawnHeightOffset;
        }

        return bestSpawnPos;
    }

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

    public void OnStrangerFrozen(StrangerBehavior stranger)
    {
        if (!isGameActive || isGameLost) return;

        currentFrozenCount++;
        UpdateUI();
        onProgressUpdated?.Invoke(currentFrozenCount, totalStatuesToWin);

        Debug.Log($"[StrangerMinigameManager] 🗿 ĐÃ PHONG ẤN THÀNH CÔNG! Tổng số tượng hiện có: {currentFrozenCount}");

        if (!isZoneEndlessMode && currentFrozenCount >= totalStatuesToWin)
        {
            WinMinigame();
        }
        else
        {
            if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = StartCoroutine(SpawnNextStatueRoutine(delayBetweenSpawns));
        }
    }

    public void OnTargetFrozen(StrangerMinigameTarget target)
    {
        OnStrangerFrozen((StrangerBehavior)null);
    }

    public void OnPlayerCaught()
    {
        OnPlayerCaught((StrangerBehavior)null);
    }

    public void OnPlayerCaught(StrangerMinigameTarget target)
    {
        OnPlayerCaught((StrangerBehavior)null);
    }

    public void OnPlayerCaught(StrangerBehavior stranger)
    {
        if (!isGameActive || isGameWon || isGameLost) return;

        if (!enableGameOver)
        {
            Debug.Log("[StrangerMinigameManager] ⚠️ Stranger chạm vào Player (Chế độ Test: Hóa đá và spawn tiếp!)");
            if (stranger != null)
            {
                OnStrangerFrozen(stranger);
            }
            else
            {
                currentFrozenCount++;
                if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
                spawnLoopCoroutine = StartCoroutine(SpawnNextStatueRoutine(delayBetweenSpawns));
            }
            return;
        }

        isGameLost = true;
        isGameActive = false;

        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }

        if (loseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(loseSound, 1f);
        }

        onMinigameLose?.Invoke();
        Debug.Log("[StrangerMinigameManager] 💀 GAME OVER! Bạn đã bị Stranger bắt được!");
    }

    private void WinMinigame()
    {
        isGameWon = true;
        isGameActive = false;

        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }

        if (winSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(winSound, 1f);
        }

        onMinigameWin?.Invoke();
        Debug.Log("[StrangerMinigameManager] 🏆 CHIẾN THẮNG! Bạn đã phong ấn toàn bộ tượng Stranger!");
    }

    private void UpdateUI()
    {
        if (progressTextUI != null)
        {
            if (isZoneEndlessMode)
            {
                progressTextUI.text = $"Tượng đã phong ấn: {currentFrozenCount}";
            }
            else
            {
                progressTextUI.text = $"Phong ấn: {currentFrozenCount}/{totalStatuesToWin}";
            }
        }
    }
}
