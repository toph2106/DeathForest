using UnityEngine;
using System.Collections;

public class Camera10sDoorEvent : MonoBehaviour
{
    public static Camera10sDoorEvent Instance { get; private set; }
    public static bool hasCompletedDoorOpenDialogue { get; set; } = false;

    [Header("1. Thời Gian Chờ Sau Khi Mở Cửa Sổ (Giây)")]
    [Tooltip("Thời gian chờ sau khi mở cửa sổ rồi mới phát câu thoại 1 (Mặc định: 1.0s)")]
    public float delayAfterWindowOpen = 1.0f;

    [Header("2. Câu Thoại 1: Sau Khi Mở Cửa Sổ (Mặc định: 'Uầy nét phết')")]
    public SmartInteractionDialogue.DialogueLine[] fiveSecondDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Uầy nét phết",
            englishDialogue = "Wow, this looks pretty sharp",
            holdDuration = 2.0f
        }
    };

    [Header("3. Thời Gian Đếm Ngược Mở Cửa Chính (Giây)")]
    [Tooltip("Thời gian đếm ngược sau khi phát câu thoại 1 đến khi CỬA CHÍNH TỰ MỞ (Mặc định: 3.0s)")]
    public float countdownToDoorOpen = 3.0f;

    [Header("4. Cánh Cửa Cần Mở Tự Động")]
    [Tooltip("Kéo Object Cửa Chính (chứa script DoorExit) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM trong Scene!")]
    public DoorExit doorToOpen;

    [Header("5. Âm Thanh Mở Cửa & Hiệu Ứng Kinh Dị (Tùy chọn)")]
    public AudioClip customDoorOpenSound;
    public AudioClip eerieCueSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("6. Câu Thoại 2: Khi Cửa Mở (Mặc định: 'Thôi đủ rồi, tắt máy đi ngủ')")]
    public SmartInteractionDialogue.DialogueLine[] tenSecondDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Thôi đủ rồi, tắt máy đi ngủ",
            englishDialogue = "That's enough, time to turn it off and sleep",
            holdDuration = 3.0f
        }
    };

    [Header("7. Thời Gian Chờ Gián Xuất Hiện")]
    [Tooltip("Thời gian chờ sau khi cửa chính mở ra rồi gián mới bắt đầu xuất hiện bò (giây - Mặc định: 1.0s)")]
    public float cockroachSpawnDelay = 1.0f;

    [Header("8. Âm Thanh Thoại Gõ Chữ (Dialogue SFX)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    private AudioSource audioSource;
    private bool isSequenceRunning = false;
    private bool hasTriggeredWindowDialogue = false;
    private bool hasTriggeredDoorOpen = false;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (doorToOpen == null)
        {
            doorToOpen = Object.FindFirstObjectByType<DoorExit>();
        }
    }

    void Update()
    {
        if (!hasTriggeredWindowDialogue && !isSequenceRunning && CamcorderUI.Instance != null && CamcorderUI.Instance.gameObject.activeInHierarchy)
        {
            bool isWindowOpen = WindowAmbienceController.CheckIfAnyWindowOpen();
            if (isWindowOpen)
            {
                StartCoroutine(WindowOpenedSequenceRoutine());
            }
        }
    }

    IEnumerator WindowOpenedSequenceRoutine()
    {
        hasTriggeredWindowDialogue = true;
        isSequenceRunning = true;

        if (delayAfterWindowOpen > 0f)
        {
            yield return new WaitForSeconds(delayAfterWindowOpen);
        }

        Debug.Log("[Camera10sDoorEvent] 💬 Phát câu thoại 1 sau khi mở cửa sổ...");
        if (fiveSecondDialogues != null && fiveSecondDialogues.Length > 0)
        {
            yield return StartCoroutine(PlayDialogueSequenceRoutine(fiveSecondDialogues));
        }

        Debug.Log($"[Camera10sDoorEvent] ⏱️ Bắt đầu đếm ngược {countdownToDoorOpen}s đến khi cửa chính tự mở...");
        float timer = 0f;
        while (timer < countdownToDoorOpen)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(ExecuteDoorOpenSequence());
    }

    IEnumerator ExecuteDoorOpenSequence()
    {
        hasTriggeredDoorOpen = true;
        hasCompletedDoorOpenDialogue = false;

        if (eerieCueSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(eerieCueSound, soundVolume);
        }

        if (doorToOpen == null) doorToOpen = Object.FindFirstObjectByType<DoorExit>();
        if (doorToOpen != null)
        {
            doorToOpen.OpenDoorAutomatically(customDoorOpenSound);
            Debug.Log("[Camera10sDoorEvent] 🚪 Đã mở toang cánh cửa chính ra ngoài!");
        }

        StartCoroutine(SpawnCockroachesDelayedRoutine());

        if (tenSecondDialogues != null && tenSecondDialogues.Length > 0)
        {
            yield return StartCoroutine(PlayDialogueSequenceRoutine(tenSecondDialogues));
        }

        hasCompletedDoorOpenDialogue = true;
        isSequenceRunning = false;
        Debug.Log("[Camera10sDoorEvent] ✅ Đã nói xong câu thoại mở cửa! Bây giờ cho phép nhìn gián để phát thoại thấy gián.");
    }

    IEnumerator SpawnCockroachesDelayedRoutine()
    {
        if (cockroachSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(cockroachSpawnDelay);
        }

        if (CockroachMinigameManager.Instance != null)
        {
            CockroachMinigameManager.Instance.StartMinigame();
        }
        else
        {
            BabyCockroachCrawler[] babyCockroaches = Object.FindObjectsByType<BabyCockroachCrawler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var baby in babyCockroaches)
            {
                if (baby != null)
                {
                    baby.gameObject.SetActive(true);
                    baby.StartCrawling();
                }
            }
        }
    }

    public void ResetEvent()
    {
        isSequenceRunning = false;
        hasTriggeredWindowDialogue = false;
        hasTriggeredDoorOpen = false;
        hasCompletedDoorOpenDialogue = false;
        Debug.Log("[Camera10sDoorEvent] 🔄 Đã reset lại toàn bộ sự kiện mở cửa & thoại!");
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
            bool skip = false;
            subtitleTextUI.text = "";
            for (int c = 1; c <= fullText.Length; c++)
            {
                if (Time.time - lineStartTime > 0.2f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                {
                    skip = true;
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
            while (fadeElapsed < 0.25f)
            {
                fadeElapsed += Time.deltaTime;
                Color col = subtitleTextUI.color;
                col.a = 1f - (fadeElapsed / 0.25f);
                subtitleTextUI.color = col;
                yield return null;
            }

            subtitleTextUI.text = "";
        }

        subtitleTextUI.gameObject.SetActive(false);
        if (subtitleTextUI.transform.parent != null) subtitleTextUI.transform.parent.gameObject.SetActive(false);
        SmartInteractionDialogue.isAnyDialoguePlaying = false;
    }

    TMPro.TextMeshProUGUI FindSubtitleUI()
    {
        SmartInteractionDialogue sid = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
        if (sid != null && sid.subtitleTextUI != null) return sid.subtitleTextUI;

        BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
        if (bed != null && bed.subtitleTextUI != null) return bed.subtitleTextUI;

        GameIntroManager intro = Object.FindFirstObjectByType<GameIntroManager>(FindObjectsInactive.Include);
        if (intro != null && intro.subtitleTextUI != null) return intro.subtitleTextUI;

        GameObject subObj = GameObject.Find("SubtitlesText") ?? GameObject.Find("SubtitleText") ?? GameObject.Find("Subtitle") ?? GameObject.Find("DialogueText");
        if (subObj != null) return subObj.GetComponent<TMPro.TextMeshProUGUI>();

        return null;
    }
}
