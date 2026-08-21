using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Gắn script này vào các GameObject có BoxCollider (Is Trigger = true) tại 3 Vùng Cấm trên Map.
/// Nhiệm vụ:
/// 1. Khi Player bước vào -> Kích hoạt con Uma của vùng này (assignedUma) đi tuần tra săn Player + Hiện thoại cảnh báo sau 1s.
/// 2. Chống spam thoại: Đi ra đi vào liên tục chỉ chạy đúng 1 lần thoại hoàn chỉnh, hết thoại cooldown 2s mới có thể kích hoạt lại.
/// 3. Khi Player chạy thoát ra ngoài -> Ra lệnh cho con Uma quay trở về vị trí cũ an toàn.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ForbiddenDangerZone : MonoBehaviour
{
    // Biến static dùng chung cho tất cả các Zone để không bao giờ bị đè âm thanh/thoại
    public static bool isAnyDialogueActive = false;
    public static float lastDialogueEndTime = -999f;

    [Header("1. Cấu Hình Vùng Cấm (Zone Settings)")]
    [Tooltip("Tên vùng cấm (để hiển thị debug)")]
    public string zoneName = "Khu Rừng Cấm";

    [Tooltip("Kéo con Uma đã đặt sẵn trên Scene cho vùng cấm này vào đây")]
    public UmaPatrolAI assignedUma;

    [Header("2. Phụ Đề Thoại Cảnh Báo (Warning Dialogue)")]
    [Tooltip("Thời gian chờ sau khi bước chân vào vùng cấm mới cất lời thoại cảnh báo (giây - Mặc định: 1.0s)")]
    public float delayBeforeWarningDialogue = 1.0f;

    [Tooltip("Thời gian hồi chiêu sau khi hết thoại mới được phép chạy thoại tiếp (giây - Mặc định: 2.0s)")]
    public float dialogueCooldown = 2.0f;

    [TextArea(2, 3)]
    public string vietnameseWarningDialogue = "Mình đi vào sâu quá rồi, nên quay lại...";
    [TextArea(2, 3)]
    public string englishWarningDialogue = "I've gone too far into the woods, I should turn back...";

    public float dialogueHoldDuration = 2.5f;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.035f;
    public bool showBlinkingCursor = true;
    public bool useFadeEffect = true;
    public float fadeTextDuration = 0.2f;

    [Tooltip("Âm thanh gõ chữ phụ đề (Tùy chọn)")]
    public AudioClip dialogueBlipSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

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
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
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

            // Kiểm tra điều kiện chạy thoại: Đủ thời gian chờ + Chưa có thoại nào đang phát + Đã qua thời gian cooldown 2s
            if (!hasScheduledDialogue && timerInside >= delayBeforeWarningDialogue)
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

            Debug.Log($"[ForbiddenDangerZone] ⚠️ Player bước vào '{zoneName}'! Đã kích hoạt Uma tuần tra!");

            // Kích hoạt Uma tuần tra
            if (assignedUma != null)
            {
                assignedUma.OnPlayerEnteredZone(other.transform);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            isPlayerInside = false;
            hasScheduledDialogue = false;

            Debug.Log($"[ForbiddenDangerZone] 🟢 Player đã rời khỏi '{zoneName}'! Ra lệnh cho Uma quay về.");

            // Ra lệnh cho Uma quay về
            if (assignedUma != null)
            {
                assignedUma.OnPlayerExitedZone();
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

        // Đánh dấu thoại đang phát để chống spam / chồng tiếng
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
                if (showBlinkingCursor) typed += " _";
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

        if (showBlinkingCursor && subtitleTextUI != null)
        {
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

        isWaitingForNextLine = true;
        float waitTimer = 0f;
        while (waitTimer < dialogueHoldDuration && !skipRequested)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }
        isWaitingForNextLine = false;

        if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);

        if (useFadeEffect && subtitleTextUI != null)
        {
            float elapsed = 0f;
            Color c = subtitleTextUI.color;
            while (elapsed < fadeTextDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsed / fadeTextDuration);
                subtitleTextUI.color = c;
                yield return null;
            }
        }

        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;
            subtitleTextUI.gameObject.SetActive(false);
        }

        // Thoại hoàn tất -> Bắt đầu đếm cooldown 2s
        isAnyDialogueActive = false;
        lastDialogueEndTime = Time.time;
    }

    IEnumerator BlinkCursorRoutine(TextMeshProUGUI txt, string baseText)
    {
        bool showUnderscore = true;
        while (true)
        {
            if (txt != null) txt.text = baseText + (showUnderscore ? " _" : "  ");
            showUnderscore = !showUnderscore;
            yield return new WaitForSeconds(0.4f);
        }
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

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);

            Gizmos.color = new Color(1f, 0f, 0f, 0.85f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
