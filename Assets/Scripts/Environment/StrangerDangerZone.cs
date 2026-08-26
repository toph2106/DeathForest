using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Gắn script này vào GameObject 'ZoneStranger' (chứa BoxCollider với Is Trigger = true).
/// Quản lý khu vực hoạt động của quái Stranger (Người Lạ):
/// 1. Khi Player bước vào vùng -> Đánh thức Stranger bắt đầu săn & (tùy chọn) phát câu thoại cảnh báo.
/// 2. Khi Player chạy thoát ra khỏi vùng -> Stranger dừng săn và tự động quay về vị trí ban đầu (trong hang/lều).
/// 3. Hỗ trợ hệ thống thoại cảnh báo gõ chữ, chống spam thoại, click chuột skip thoại.
/// </summary>
public class StrangerDangerZone : MonoBehaviour
{
    public static bool isAnyDialogueActive = false;
    public static float lastDialogueEndTime = -999f;

    [Header("1. Cấu Hình Quái Vật Trong & Ngoài Zone")]
    [Tooltip("Tên vùng (để hiển thị debug)")]
    public string zoneName = "Khu Vực Người Lạ (Stranger Zone)";

    [Tooltip("Kéo con Stranger đã đặt sẵn trong Scene vào đây")]
    public StrangerBehavior assignedStranger;

    [Tooltip("Kéo con Yoshie đã đặt sẵn trong Scene vào đây (để ra lệnh lùi lại khi vào zone)")]
    public YoshieBehavior assignedYoshie;

    [Header("2. Phụ Đề Thoại Cảnh Báo (Tùy Chọn)")]
    public bool enableWarningDialogue = true;

    [Tooltip("Thời gian chờ sau khi bước vào vùng mới cất lời thoại (giây - Mặc định: 1.0s)")]
    public float delayBeforeWarningDialogue = 1.0f;

    [Tooltip("Thời gian hồi chiêu giữa 2 lần thoại (giây - Mặc định: 3.0s)")]
    public float dialogueCooldown = 3.0f;

    [TextArea(2, 3)]
    public string vietnameseWarningDialogue = "Có cảm giác ai đó đang đứng sau lưng mình...";
    [TextArea(2, 3)]
    public string englishWarningDialogue = "I feel like someone is standing behind me...";

    public float dialogueHoldDuration = 2.5f;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.035f;
    public bool showBlinkingCursor = true;
    public bool useFadeEffect = true;
    public float fadeTextDuration = 0.2f;

    [Tooltip("Âm thanh gõ chữ phụ đề (Tùy chọn)")]
    public AudioClip dialogueBlipSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    [Header("3. Âm Thanh Rùng Rợn Khi Vào Vùng (Tùy Chọn)")]
    public AudioClip enterZoneSFX;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    // --- Private Fields ---
    private Collider zoneCollider;
    private bool isPlayerInside = false;
    private float timerInside = 0f;
    private bool hasScheduledDialogue = false;
    private Coroutine dialogueCoroutine;
    private Coroutine cursorBlinkCoroutine;
    private string currentFullText = "";
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;

    private MovePl playerScript;
    private Transform playerTransform;
    private TextMeshProUGUI subtitleTextUI;
    private AudioSource audioSource;

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        if (assignedStranger == null) assignedStranger = Object.FindFirstObjectByType<StrangerBehavior>();
        if (assignedYoshie == null) assignedYoshie = Object.FindFirstObjectByType<YoshieBehavior>();
    }

    void Start()
    {
        FindPlayer();
        FindSubtitleUI();
    }

    void Update()
    {
        if (isPlayerInside && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)))
        {
            if (isTyping)
            {
                isTyping = false;
                if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;
                if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueBlipSound) audioSource.Stop();
            }
            else if (isWaitingForNextLine)
            {
                skipRequested = true;
            }
        }

        if (isPlayerInside)
        {
            timerInside += Time.deltaTime;

            if (enableWarningDialogue && !hasScheduledDialogue && timerInside >= delayBeforeWarningDialogue)
            {
                if (!isAnyDialogueActive && Time.time >= lastDialogueEndTime + dialogueCooldown)
                {
                    hasScheduledDialogue = true;
                    if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
                    dialogueCoroutine = StartCoroutine(PlayWarningDialogueRoutine());
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            isPlayerInside = true;
            timerInside = 0f;
            FindPlayer();
            FindSubtitleUI();

            Debug.Log($"[StrangerDangerZone] ⚠️ Player đã bước vào '{zoneName}'! Stranger thức tỉnh, Yoshie lùi lại!");

            if (enterZoneSFX != null && audioSource != null)
            {
                audioSource.PlayOneShot(enterZoneSFX, sfxVolume);
            }

            // 1. Kích hoạt Stranger săn người chơi
            if (assignedStranger != null)
            {
                assignedStranger.OnPlayerEnteredZone(other.transform, zoneCollider);
            }

            // 2. Ra lệnh cho Yoshie bay LÙI LẠI về vị trí Spawn ban đầu (ngôi Miếu)
            if (assignedYoshie != null)
            {
                assignedYoshie.OnPlayerEnteredStrangerZone();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            isPlayerInside = false;
            hasScheduledDialogue = false;

            Debug.Log($"[StrangerDangerZone] 🟢 Player đã rời khỏi '{zoneName}'! Stranger về chỗ cũ, Yoshie đuổi tiếp.");

            // 1. Ra lệnh Stranger quay về vị trí Spawn ban đầu (trong lều / hang)
            if (assignedStranger != null)
            {
                assignedStranger.OnPlayerExitedZone();
            }

            // 2. Ra lệnh Yoshie tiếp tục truy đuổi người chơi
            if (assignedYoshie != null)
            {
                assignedYoshie.OnPlayerExitedStrangerZone();
            }
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        return other.CompareTag("Player") || other.GetComponent<MovePl>() != null || other.GetComponentInParent<MovePl>() != null;
    }

    private void FindPlayer()
    {
        if (playerScript == null) playerScript = Object.FindFirstObjectByType<MovePl>();
        if (playerScript != null) playerTransform = playerScript.transform;
    }

    private void FindSubtitleUI()
    {
        if (subtitleTextUI != null) return;

        SmartInteractionDialogue sid = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
        if (sid != null && sid.subtitleTextUI != null)
        {
            subtitleTextUI = sid.subtitleTextUI;
            return;
        }

        TextMeshProUGUI[] tmps = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in tmps)
        {
            if (tmp != null && (tmp.gameObject.name.ToLower().Contains("subtitle") || tmp.gameObject.name.ToLower().Contains("sub")))
            {
                subtitleTextUI = tmp;
                return;
            }
        }
    }

    IEnumerator PlayWarningDialogueRoutine()
    {
        if (subtitleTextUI == null) FindSubtitleUI();
        if (subtitleTextUI == null) yield break;

        isAnyDialogueActive = true;

        EnsureParentsActive(subtitleTextUI);
        subtitleTextUI.gameObject.SetActive(true);
        Color sc = subtitleTextUI.color;
        sc.a = 1f;
        subtitleTextUI.color = sc;

        string lang = SettingsManager.currentLanguage;
        currentFullText = (lang == "VI") ? vietnameseWarningDialogue : englishWarningDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = vietnameseWarningDialogue;
        if (string.IsNullOrEmpty(currentFullText))
        {
            isAnyDialogueActive = false;
            yield break;
        }

        if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
        skipRequested = false;

        if (dialogueBlipSound != null && audioSource != null)
        {
            audioSource.clip = dialogueBlipSound;
            audioSource.volume = dialogueVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        if (useTypewriterEffect)
        {
            isTyping = true;
            subtitleTextUI.text = "";

            for (int i = 0; i <= currentFullText.Length; i++)
            {
                if (!isTyping) break;
                string typed = currentFullText.Substring(0, i);
                if (showBlinkingCursor) typed += "_";
                subtitleTextUI.text = typed;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            subtitleTextUI.text = currentFullText;
            isTyping = false;
        }
        else
        {
            subtitleTextUI.text = currentFullText;
        }

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == dialogueBlipSound)
        {
            audioSource.Stop();
        }

        if (showBlinkingCursor)
        {
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

        isWaitingForNextLine = true;
        float holdTimer = 0f;
        while (holdTimer < dialogueHoldDuration && !skipRequested)
        {
            holdTimer += Time.deltaTime;
            yield return null;
        }
        isWaitingForNextLine = false;

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        if (useFadeEffect)
        {
            float fadeElapsed = 0f;
            Color c = subtitleTextUI.color;
            while (fadeElapsed < fadeTextDuration)
            {
                fadeElapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, fadeElapsed / fadeTextDuration);
                subtitleTextUI.color = c;
                yield return null;
            }
            c.a = 0f;
            subtitleTextUI.color = c;
        }

        subtitleTextUI.text = "";
        subtitleTextUI.gameObject.SetActive(false);

        isAnyDialogueActive = false;
        lastDialogueEndTime = Time.time;
    }

    private void EnsureParentsActive(Component comp)
    {
        if (comp == null) return;
        Transform curr = comp.transform;
        while (curr != null)
        {
            curr.gameObject.SetActive(true);
            curr = curr.parent;
        }
    }

    IEnumerator BlinkCursorRoutine(TextMeshProUGUI txt, string baseText)
    {
        bool showUnderscore = true;
        while (true)
        {
            if (txt != null)
            {
                txt.text = baseText + (showUnderscore ? " _" : "  ");
            }
            showUnderscore = !showUnderscore;
            yield return new WaitForSeconds(0.35f);
        }
    }
}
