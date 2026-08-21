using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CockroachMinigameManager : MonoBehaviour
{
    public static CockroachMinigameManager Instance { get; private set; }

    [Header("1. Danh Sách Gián Con Trong Phòng (Baby Cockroaches)")]
    [Tooltip("Kéo các con gián con vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM trong Scene!")]
    public List<BabyCockroachCrawler> babyCockroaches = new List<BabyCockroachCrawler>();

    [Header("2. Con Gián Mẹ Phục Kích (CockroachFlyAttack)")]
    [Tooltip("Kéo GameObject CockroachM (chứa script CockroachFlyAttack) vào đây")]
    public CockroachFlyAttack motherCockroach;

    [Header("3. Kích Hoạt Bởi Sự Kiện Cửa Mở 10s")]
    [Tooltip("Tự động kích hoạt đàn gián khi cửa mở ở mốc 10s máy quay")]
    public bool triggerOnDoorOpen10s = true;
    public float delayAfterDoorOpen = 0.8f;

    [Header("4. Thoại Khi Người Chơi Nhìn Trúng Gián Con Lần Đầu")]
    public SmartInteractionDialogue.DialogueLine[] sightCockroachDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Á! Gián ở đâu ra nhiều thế này?! Phải đập hết chúng nó đi mới được!",
            englishDialogue = "Ah! Where did all these cockroaches come from?! I need to crush them all!",
            holdDuration = 2.8f
        }
    };

    [Header("5. Thoại Khi Người Chơi Nhìn Trúng Gián Mẹ")]
    public SmartInteractionDialogue.DialogueLine[] sightMotherCockroachDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Cái quái gì kia... Con gián này to bất thường thế?!",
            englishDialogue = "What the hell is that... That cockroach is gigantic?!",
            holdDuration = 2.8f
        }
    };

    [Header("6. Âm Thanh Thoại Gõ Chữ (Dialogue SFX)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    [Header("7. Độ Trễ Thoại Nhìn Thấy Gián (Sight Delay)")]
    [Tooltip("Thời gian chờ (giây) sau khi lia raycast trúng gián rồi mới phát câu thoại (Mặc định: 1.0s)")]
    public float sightDelayBeforeDialogue = 1.0f;

    [Header("8. Phím Tắt Test Nhanh (Debug Keys)")]
    [Tooltip("Phím kích hoạt đàn gián bắt đầu bò (Mặc định: Phím J)")]
    public KeyCode debugTriggerKey = KeyCode.J;
    [Tooltip("Phím đập thử con gián tiếp theo (Mặc định: Phím K)")]
    public KeyCode debugKillNextKey = KeyCode.K;

    public static event System.Action OnMinigameStarted;
    public static event System.Action OnAllBabiesDefeated;

    private bool isMinigameStarted = false;
    private bool hasTriggeredSightBabyDialogue = false;
    private bool isAllBabiesDefeated = false;
    private bool hasTriggeredMotherSightDialogue = false;
    private bool isWaitingForMotherSight = false;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        CleanUpAndFindBabies();

        if (motherCockroach == null)
        {
            motherCockroach = Object.FindFirstObjectByType<CockroachFlyAttack>(FindObjectsInactive.Include);
        }
    }

    void OnEnable()
    {
        if (triggerOnDoorOpen10s)
        {
            CamcorderUI.OnTimerReached10s += HandleDoorOpen10sEvent;
        }
    }

    void OnDisable()
    {
        if (triggerOnDoorOpen10s)
        {
            CamcorderUI.OnTimerReached10s -= HandleDoorOpen10sEvent;
        }
    }

    void Update()
    {
        if (debugTriggerKey != KeyCode.None && Input.GetKeyDown(debugTriggerKey))
        {
            Debug.Log($"[CockroachMinigameManager] ⌨️ Bấm phím [{debugTriggerKey}]! Kích hoạt ngay Minigame Đập Gián...");
            StartMinigame();
        }

        if (debugKillNextKey != KeyCode.None && Input.GetKeyDown(debugKillNextKey))
        {
            KillNextAliveCockroach();
        }

        if (!isMinigameStarted) return;

        // 1. TIA NHÌN RAYCAST: KIỂM TRA NGƯỜI CHƠI NHÌN TRÚNG GIÁN CON LẦN ĐẦU
        if (!hasTriggeredSightBabyDialogue && !isAllBabiesDefeated)
        {
            CheckPlayerSightAtBabyCockroaches();
        }

        // 2. TIA NHÌN RAYCAST: KIỂM TRA NGƯỜI CHƠI NHÌN TRÚNG GIÁN MẸ
        if (isWaitingForMotherSight && !hasTriggeredMotherSightDialogue)
        {
            CheckPlayerSightAtMotherCockroach();
        }
    }

    void HandleDoorOpen10sEvent()
    {
        StartCoroutine(DelayedStartMinigameRoutine(delayAfterDoorOpen));
    }

    IEnumerator DelayedStartMinigameRoutine(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        StartMinigame();
    }

    public void StartMinigame()
    {
        if (isMinigameStarted) return;
        isMinigameStarted = true;

        CleanUpAndFindBabies();

        Debug.Log($"[CockroachMinigameManager] 🪳 BẮT ĐẦU MINIGAME ĐẬP GIÁN! Tổng số gián con thực tế: {babyCockroaches.Count}");

        // Kích hoạt tất cả gián con bắt đầu bò
        for (int i = 0; i < babyCockroaches.Count; i++)
        {
            if (babyCockroaches[i] != null)
            {
                Transform p = babyCockroaches[i].transform.parent;
                while (p != null)
                {
                    if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
                    p = p.parent;
                }
                babyCockroaches[i].gameObject.SetActive(true);
                babyCockroaches[i].StartCrawling();
            }
        }

        OnMinigameStarted?.Invoke();
    }

    public void CleanUpAndFindBabies()
    {
        List<BabyCockroachCrawler> cleanList = new List<BabyCockroachCrawler>();
        HashSet<BabyCockroachCrawler> uniqueSet = new HashSet<BabyCockroachCrawler>();

        if (babyCockroaches != null)
        {
            for (int i = 0; i < babyCockroaches.Count; i++)
            {
                if (babyCockroaches[i] != null && !uniqueSet.Contains(babyCockroaches[i]))
                {
                    uniqueSet.Add(babyCockroaches[i]);
                    cleanList.Add(babyCockroaches[i]);
                }
            }
        }

        if (cleanList.Count == 0)
        {
            BabyCockroachCrawler[] found = Object.FindObjectsByType<BabyCockroachCrawler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && !uniqueSet.Contains(found[i]))
                {
                    uniqueSet.Add(found[i]);
                    cleanList.Add(found[i]);
                }
            }
        }

        babyCockroaches = cleanList;
    }

    /// <summary>
    /// Kiểm tra nếu tia nhìn người chơi lia trúng vùng tường gián bò HOẶC trúng con gián ➔ Kích hoạt thoại chỉ dẫn
    /// </summary>
    void CheckPlayerSightAtBabyCockroaches()
    {
        if (Camera.main == null) return;

        // ĐIỀU KIỆN: CÂU THOẠI "THÔI ĐỦ RỒI, TẮT MÁY ĐI NGỦ" PHẢI CHẠY XONG HOÀN TOÀN
        if (Camera10sDoorEvent.Instance != null && !Camera10sDoorEvent.hasCompletedDoorOpenDialogue) return;
        if (SmartInteractionDialogue.isAnyDialoguePlaying) return;

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        // Bắn tia nhìn kiểm tra trong khoảng cách tối đa 6 mét
        if (Physics.Raycast(ray, out hit, 6.0f))
        {
            bool isHittingCockroachArea = false;

            // 1. Nhìn trúng vùng tường có gián bò (CockroachWL, CockroachWR, CockroachW...)
            if (hit.collider.gameObject.name.Contains("CockroachW") || hit.collider.gameObject.name.Contains("CockroachWL") || hit.collider.gameObject.name.Contains("CockroachWR"))
            {
                isHittingCockroachArea = true;
            }

            // 2. Hoặc nhìn trúng chính xác con gián con
            BabyCockroachCrawler crawler = hit.collider.GetComponent<BabyCockroachCrawler>() 
                                        ?? hit.collider.GetComponentInParent<BabyCockroachCrawler>() 
                                        ?? hit.collider.GetComponentInChildren<BabyCockroachCrawler>();

            if (crawler != null && !crawler.isDead && crawler.gameObject.activeInHierarchy)
            {
                isHittingCockroachArea = true;
            }

            if (isHittingCockroachArea && GetAliveBabiesCount() > 0)
            {
                hasTriggeredSightBabyDialogue = true;
                Debug.Log($"[CockroachMinigameManager] 👁️ Người chơi nhìn trúng vùng gián bò ({hit.collider.gameObject.name})! Đợi {sightDelayBeforeDialogue}s rồi phát thoại.");
                StartCoroutine(DelayedPlaySightBabyDialogueRoutine());
            }
        }
    }

    IEnumerator DelayedPlaySightBabyDialogueRoutine()
    {
        if (sightDelayBeforeDialogue > 0f)
        {
            yield return new WaitForSeconds(sightDelayBeforeDialogue);
        }

        while (SmartInteractionDialogue.isAnyDialoguePlaying)
        {
            yield return null;
        }

        if (sightCockroachDialogues != null && sightCockroachDialogues.Length > 0)
        {
            yield return StartCoroutine(PlayDialogueSequenceRoutine(sightCockroachDialogues));
        }
    }

    /// <summary>
    /// Được gọi mỗi khi 1 con gián con bị đập chết
    /// </summary>
    public void NotifyCockroachKilled(BabyCockroachCrawler killedCrawler)
    {
        int aliveCount = GetAliveBabiesCount();
        Debug.Log($"[CockroachMinigameManager] 💥 Đã tiêu diệt 1 con gián ({killedCrawler.gameObject.name})! Số gián còn sống thực tế: {aliveCount}");

        if (aliveCount <= 0)
        {
            OnAllBabiesKilled();
        }
    }

    /// <summary>
    /// Test nhanh: Đập thử con gián sống sót tiếp theo
    /// </summary>
    public void KillNextAliveCockroach()
    {
        for (int i = 0; i < babyCockroaches.Count; i++)
        {
            if (babyCockroaches[i] != null && !babyCockroaches[i].isDead)
            {
                Debug.Log($"[CockroachMinigameManager] ⌨️ Bấm phím [{debugKillNextKey}]! Đập thử con gián index [{i}]: {babyCockroaches[i].gameObject.name}");
                babyCockroaches[i].Interact();
                break;
            }
        }
    }

    public int GetAliveBabiesCount()
    {
        int count = 0;
        for (int i = 0; i < babyCockroaches.Count; i++)
        {
            if (babyCockroaches[i] != null && !babyCockroaches[i].isDead)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Tự động cập nhật con gián sống sót duy nhất làm con kích hoạt Jumpscare
    /// </summary>
    void UpdateLastCockroachFlag()
    {
        BabyCockroachCrawler lastCrawler = null;
        int aliveCount = 0;

        for (int i = 0; i < babyCockroaches.Count; i++)
        {
            if (babyCockroaches[i] != null && !babyCockroaches[i].isDead)
            {
                aliveCount++;
                lastCrawler = babyCockroaches[i];
                babyCockroaches[i].isLastJumpscareCockroach = false;
            }
        }

        if (aliveCount == 1 && lastCrawler != null)
        {
            lastCrawler.isLastJumpscareCockroach = true;
            Debug.Log($"[CockroachMinigameManager] 🎯 Chỉ còn 1 con gián duy nhất ({lastCrawler.gameObject.name})! Đã gán cờ Jumpscare!");
        }
    }

    /// <summary>
    /// Toàn bộ gián con đã bị tiêu diệt ➔ Xuất hiện Gián Mẹ
    /// </summary>
    void OnAllBabiesKilled()
    {
        isAllBabiesDefeated = true;

        Debug.Log("[CockroachMinigameManager] 🏆 TẤT CẢ GIÁN CON ĐÃ BỊ TIÊU DIỆT & KÍCH HOẠT JUMPSCARE THẲNG VÀO MẶT!");

        OnAllBabiesDefeated?.Invoke();
    }

    /// <summary>
    /// Kiểm tra người chơi lia tầm mắt nhìn trúng Gián Mẹ ➔ Kích hoạt thoại kinh hãi ➔ Tấn công
    /// </summary>
    void CheckPlayerSightAtMotherCockroach()
    {
        if (motherCockroach == null || Camera.main == null) return;

        // 1. Kiểm tra Raycast thẳng
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;
        bool isLookingAtMother = false;

        if (Physics.Raycast(ray, out hit, 12.0f))
        {
            CockroachFlyAttack mother = hit.collider.GetComponent<CockroachFlyAttack>();
            if (mother == null) mother = hit.collider.GetComponentInParent<CockroachFlyAttack>();
            if (mother == null) mother = hit.collider.GetComponentInChildren<CockroachFlyAttack>();

            if (mother != null)
            {
                isLookingAtMother = true;
            }
        }

        // 2. Hoặc kiểm tra góc nhìn Camera hướng về phía Gián Mẹ trong tầm mắt
        if (!isLookingAtMother)
        {
            Vector3 dirToMother = (motherCockroach.transform.position - Camera.main.transform.position).normalized;
            float dot = Vector3.Dot(Camera.main.transform.forward, dirToMother);
            float dist = Vector3.Distance(Camera.main.transform.position, motherCockroach.transform.position);

            // Góc nhìn trực diện (> 0.85 tương đương góc < 30 độ) và khoảng cách < 8 mét
            if (dot > 0.85f && dist < 8.0f)
            {
                isLookingAtMother = true;
            }
        }

        if (isLookingAtMother)
        {
            hasTriggeredMotherSightDialogue = true;
            isWaitingForMotherSight = false;

            Debug.Log("[CockroachMinigameManager] 👁️ Người chơi đã nhìn thấy Gián Mẹ! Kích hoạt thoại hoảng hốt.");
            StartCoroutine(MotherCockroachSightSequenceRoutine());
        }
    }

    /// <summary>
    /// Kích hoạt thoại khi gián phóng va chạm / bám kính Camera (Mục 5: Cái quái...)
    /// </summary>
    public void TriggerJumpscareHitDialogue()
    {
        if (sightMotherCockroachDialogues != null && sightMotherCockroachDialogues.Length > 0)
        {
            Debug.Log("[CockroachMinigameManager] 💬 Kích hoạt thoại lúc gián va chạm bám mặt!");
            StartCoroutine(PlayDialogueSequenceRoutine(sightMotherCockroachDialogues));
        }
    }

    IEnumerator MotherCockroachSightSequenceRoutine()
    {
        // 1. Phát thoại hoảng hốt khi nhìn thấy Gián Mẹ
        if (sightMotherCockroachDialogues != null && sightMotherCockroachDialogues.Length > 0)
        {
            yield return StartCoroutine(PlayDialogueSequenceRoutine(sightMotherCockroachDialogues));
        }

        // 2. Sau khi dứt thoại, Gián Mẹ kích hoạt chuỗi bay tấn công toàn màn hình!
        if (motherCockroach != null)
        {
            Debug.Log("[CockroachMinigameManager] 🚀 Thoại kết thúc -> GIÁN MẸ BẮT ĐẦU CHUỖI TẤN CÔNG!");
            motherCockroach.StartCockroachSequence();
        }
    }

    IEnumerator PlayDialogueSequenceRoutine(SmartInteractionDialogue.DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;

        TMPro.TextMeshProUGUI subtitleTextUI = FindSubtitleUI();
        if (subtitleTextUI == null) yield break;

        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        if (subtitleTextUI.transform.parent != null) subtitleTextUI.transform.parent.gameObject.SetActive(true);
        subtitleTextUI.gameObject.SetActive(true);

        yield return null;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null) continue;

            string fullText = (SettingsManager.currentLanguage == "VI") ? line.vietnameseDialogue : line.englishDialogue;
            if (string.IsNullOrEmpty(fullText)) fullText = line.vietnameseDialogue;
            if (string.IsNullOrEmpty(fullText)) fullText = line.englishDialogue;
            if (string.IsNullOrEmpty(fullText)) continue;

            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;

            if (dialogueSound != null && audioSource != null)
            {
                audioSource.clip = dialogueSound;
                audioSource.volume = dialogueVolume;
                audioSource.loop = true;
                audioSource.time = 0f;
                audioSource.Play();
            }

            float lineStartTime = Time.time;
            subtitleTextUI.text = "";
            for (int c = 1; c <= fullText.Length; c++)
            {
                if (Time.time - lineStartTime > 0.2f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                {
                    subtitleTextUI.text = fullText;
                    break;
                }
                subtitleTextUI.text = fullText.Substring(0, c) + "_";
                yield return new WaitForSeconds(0.03f);
            }

            if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueSound) audioSource.Stop();
            subtitleTextUI.text = fullText;

            float timer = 0f;
            float hold = (line.holdDuration > 0f) ? line.holdDuration : 2.5f;
            bool blink = true;
            float blinkTimer = 0f;
            while (timer < hold)
            {
                if (timer > 0.2f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))) break;
                timer += Time.deltaTime;
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= 0.4f)
                {
                    blinkTimer = 0f;
                    blink = !blink;
                    subtitleTextUI.text = fullText + (blink ? " _" : "  ");
                }
                yield return null;
            }

            float fadeElapsed = 0f;
            while (fadeElapsed < 0.2f)
            {
                fadeElapsed += Time.deltaTime;
                sc.a = Mathf.Lerp(1f, 0f, fadeElapsed / 0.2f);
                subtitleTextUI.color = sc;
                yield return null;
            }
        }

        subtitleTextUI.text = "";
        Color finalColor = subtitleTextUI.color;
        finalColor.a = 1f;
        subtitleTextUI.color = finalColor;

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
    }

    TMPro.TextMeshProUGUI FindSubtitleUI()
    {
        BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
        if (bed != null && bed.subtitleTextUI != null) return bed.subtitleTextUI;

        GameIntroManager intro = Object.FindFirstObjectByType<GameIntroManager>(FindObjectsInactive.Include);
        if (intro != null && intro.subtitleTextUI != null) return intro.subtitleTextUI;

        GameObject subObj = GameObject.Find("SubtitleText") ?? GameObject.Find("Subtitle") ?? GameObject.Find("DialogueText");
        if (subObj != null) return subObj.GetComponent<TMPro.TextMeshProUGUI>();

        return null;
    }
}
