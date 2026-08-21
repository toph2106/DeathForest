using UnityEngine;
using System.Collections;

public class PCPowerButton : MonoBehaviour, IInteractable
{
    [Header("1. Kéo Màn Hình 3D / Script InWorldComputerCutscene")]
    [Tooltip("Kéo cái 3D Computer Screen (hoặc Object chứa script InWorldComputerCutscene) vào đây")]
    public InWorldComputerCutscene computerCutscene;

    [Header("2. Chữ Nhắc Tương Tác (Prompt UI)")]
    public string englishPrompt = "Turn On PC";
    public string vietnamesePrompt = "Bật máy tính";
    public string englishTurnOffPrompt = "Turn Off PC";
    public string vietnameseTurnOffPrompt = "Tắt máy tính";

    [Header("3. Điều Kiện Bắt Buộc Đóng Cửa Sổ (Require Window Closed)")]
    [Tooltip("Tích chọn để bắt buộc phải ĐÓNG CỬA SỔ thì mới được bật máy tính")]
    public bool requireWindowClosed = true;

    [Tooltip("Thoại khi chưa đóng cửa sổ (Locked Dialogue)")]
    public SmartInteractionDialogue.DialogueLine[] lockedNeedCloseWindowDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Gió lạnh quá... Mình nên đóng cửa sổ lại trước đã.",
            englishDialogue = "It's too cold... I should close the window first.",
            holdDuration = 2.5f
        }
    };

    [Header("3.1. Âm Thanh Thoại Gõ Chữ (Dialogue SFX)")]
    [Tooltip("Gói âm thanh lồng tiếng / gõ chữ khi hiện phụ đề nhắc nhở (5s blip)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)]
    [Tooltip("Âm lượng âm thanh thoại gõ chữ (Mặc định: 0.8)")]
    public float dialogueVolume = 0.8f;

    [Header("4. Âm Thanh Nút Bật/Tắt Case PC (unfa__short-ping.wav)")]
    [Tooltip("Kéo âm thanh click phím nguồn (unfa__short-ping.wav) vào đây")]
    public AudioClip powerClickSound;

    [Header("5. Âm Thanh Quạt PC Chạy Rì Rầm (loud-computer.wav)")]
    [Tooltip("Kéo tiếng quạt PC chạy vòng lặp (loud-computer.wav) vào đây")]
    public AudioClip fanHummingSound;

    [Header("6. Âm Lượng")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng bíp/click nguồn (Mặc định: 0.6)")]
    public float clickVolume = 0.6f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng quạt Case PC chạy rì rầm (Mặc định: 0.5)")]
    public float fanVolume = 0.5f;

    [Header("7. Cấu Hình Vùng Âm Thanh 3D (Spatial Sound Radius)")]
    [Tooltip("Bật âm thanh 3D (Lại gần gầm bàn nghe to, đi ra xa phòng nhỏ dần và mất hẳn)")]
    public bool use3DSound = true;

    [Tooltip("Bán kính tối đa nghe thấy tiếng quạt Case PC (Mặc định: 5m)")]
    public float maxSoundDistance = 5f;

    [Header("8. Mở Khóa Mèo Sau Khi Tắt PC")]
    [Tooltip("Kéo Collider của Con Mèo vào đây để tự động mở khóa tương tác đuổi Mèo sau khi TẮT CASE PC!")]
    public Collider catColliderToEnable;
    public GameObject catObjectToEnable;

    public static bool IsPCPowerOn { get; private set; } = false;

    private Collider caseCollider;
    private InteractPrompt interactPrompt;
    private AudioSource audioSource;
    private AudioSource fanAudioSource;

    void Awake()
    {
        caseCollider = GetComponent<Collider>();
        if (caseCollider == null) caseCollider = gameObject.AddComponent<BoxCollider>();
        caseCollider.enabled = true; // Luôn mở collider để người chơi nhìn vào hiện bàn tay và click được

        interactPrompt = GetComponent<InteractPrompt>();
        if (interactPrompt == null) interactPrompt = gameObject.AddComponent<InteractPrompt>();
    }

    void Start()
    {
        IsPCPowerOn = false;

        if (caseCollider != null) caseCollider.enabled = true;

        // Tự động tìm màn hình PC nếu chưa kéo
        if (computerCutscene == null)
        {
            computerCutscene = Object.FindFirstObjectByType<InWorldComputerCutscene>();
        }

        // Tự động tìm Con Mèo nếu chưa kéo
        if (catColliderToEnable == null)
        {
            Cat cat = Object.FindFirstObjectByType<Cat>();
            if (cat != null) catColliderToEnable = cat.GetComponent<Collider>();
        }

        // Source 1: Phát tiếng bíp/click nguồn
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Source 2: Phát tiếng quạt PC chạy vòng lặp
        fanAudioSource = gameObject.AddComponent<AudioSource>();
        fanAudioSource.loop = true;
        fanAudioSource.playOnAwake = false;

        Update3DSoundSettings();
        UpdatePromptText();
    }

    public void UnlockCase()
    {
        if (caseCollider == null) caseCollider = GetComponent<Collider>();
        if (caseCollider != null) caseCollider.enabled = true;
    }

    void Update3DSoundSettings()
    {
        if (use3DSound)
        {
            if (audioSource != null)
            {
                audioSource.spatialBlend = 1f;
                audioSource.minDistance = 0.5f;
                audioSource.maxDistance = maxSoundDistance;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            }

            if (fanAudioSource != null)
            {
                fanAudioSource.spatialBlend = 1f;
                fanAudioSource.minDistance = 0.5f;
                fanAudioSource.maxDistance = maxSoundDistance;
                fanAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            }
        }
        else
        {
            if (audioSource != null) audioSource.spatialBlend = 0f;
            if (fanAudioSource != null) fanAudioSource.spatialBlend = 0f;
        }
    }

    public bool IsWindowStillOpen()
    {
        return WindowAmbienceController.CheckIfAnyWindowOpen();
    }

    void PlayLockedDialogue()
    {
        if (lockedNeedCloseWindowDialogues != null && lockedNeedCloseWindowDialogues.Length > 0)
        {
            StartCoroutine(PlayLockedDialogueRoutine());
        }
    }

    IEnumerator PlayLockedDialogueRoutine()
    {
        TMPro.TextMeshProUGUI subUI = null;
        BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
        if (bed != null && bed.subtitleTextUI != null) subUI = bed.subtitleTextUI;
        if (subUI == null)
        {
            GameIntroManager intro = Object.FindFirstObjectByType<GameIntroManager>(FindObjectsInactive.Include);
            if (intro != null && intro.subtitleTextUI != null) subUI = intro.subtitleTextUI;
        }
        if (subUI == null)
        {
            GameObject subObj = GameObject.Find("SubtitleText") ?? GameObject.Find("Subtitle") ?? GameObject.Find("DialogueText");
            if (subObj != null) subUI = subObj.GetComponent<TMPro.TextMeshProUGUI>();
        }

        if (subUI == null) yield break;

        AudioSource aSource = audioSource;
        if (aSource == null) aSource = GetComponent<AudioSource>();

        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        if (subUI.transform.parent != null) subUI.transform.parent.gameObject.SetActive(true);
        subUI.gameObject.SetActive(true);

        // Chờ 1 frame để click tương tác ban đầu trôi qua
        yield return null;

        for (int i = 0; i < lockedNeedCloseWindowDialogues.Length; i++)
        {
            var line = lockedNeedCloseWindowDialogues[i];
            if (line == null) continue;

            string fullText = (SettingsManager.currentLanguage == "VI") ? line.vietnameseDialogue : line.englishDialogue;
            if (string.IsNullOrEmpty(fullText)) fullText = line.vietnameseDialogue;
            if (string.IsNullOrEmpty(fullText)) fullText = line.englishDialogue;
            if (string.IsNullOrEmpty(fullText)) continue;

            Color sc = subUI.color;
            sc.a = 1f;
            subUI.color = sc;

            if (dialogueSound != null && aSource != null)
            {
                aSource.spatialBlend = 0f;
                aSource.clip = dialogueSound;
                aSource.volume = dialogueVolume;
                aSource.loop = true;
                aSource.time = 0f;
                aSource.Play();
            }

            float lineStartTime = Time.time;
            subUI.text = "";
            for (int c = 1; c <= fullText.Length; c++)
            {
                if (Time.time - lineStartTime > 0.2f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                {
                    subUI.text = fullText;
                    break;
                }
                subUI.text = fullText.Substring(0, c) + "_";
                yield return new WaitForSeconds(0.03f);
            }

            if (aSource != null && aSource.isPlaying && aSource.clip == dialogueSound) aSource.Stop();
            subUI.text = fullText;

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
                    subUI.text = fullText + (blink ? " _" : "  ");
                }
                yield return null;
            }

            float fadeElapsed = 0f;
            while (fadeElapsed < 0.2f)
            {
                fadeElapsed += Time.deltaTime;
                sc.a = Mathf.Lerp(1f, 0f, fadeElapsed / 0.2f);
                subUI.color = sc;
                yield return null;
            }
        }

        subUI.text = "";
        Color finalColor = subUI.color;
        finalColor.a = 1f;
        subUI.color = finalColor;

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
    }

    public void UpdatePromptText()
    {
        if (interactPrompt == null) return;
        if (IsPCPowerOn)
        {
            interactPrompt.englishPrompt = englishTurnOffPrompt;
            interactPrompt.vietnamesePrompt = vietnameseTurnOffPrompt;
        }
        else
        {
            interactPrompt.englishPrompt = englishPrompt;
            interactPrompt.vietnamesePrompt = vietnamesePrompt;
        }
        interactPrompt.UpdateText();
    }

    // Khi người chơi nhìn vào Case PC bấm Chuột Trái (Mouse 0)
    public void Interact()
    {
        if (SmartInteractionDialogue.isAnyDialoguePlaying) return;
        Update3DSoundSettings();

        if (!IsPCPowerOn)
        {
            // KIỂM TRA ĐIỀU KIỆN ĐÓNG CỬA SỔ
            if (requireWindowClosed && IsWindowStillOpen())
            {
                Debug.Log("[PCPowerButton] 🔒 Cửa sổ chưa đóng -> Hiện thoại nhắc người chơi đóng cửa sổ trước!");
                PlayLockedDialogue();
                return;
            }

            // BẤM LẦN 1 -> BẬT MÁY TÍNH
            IsPCPowerOn = true;
            UpdatePromptText();

            // 1. Phát tiếng bíp nguồn
            if (powerClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(powerClickSound, clickVolume);
            }

            // 2. Bắt đầu phát tiếng quạt PC chạy rì rầm theo volume cài đặt
            if (fanHummingSound != null && fanAudioSource != null)
            {
                fanAudioSource.clip = fanHummingSound;
                fanAudioSource.volume = fanVolume;
                fanAudioSource.Play();
            }

            // 3. Bật sáng màn hình 3D
            if (computerCutscene != null)
            {
                computerCutscene.PowerOnPC();
            }
        }
        else
        {
            // BẤM LẦN 2 -> TẮT MÁY TÍNH
            IsPCPowerOn = false;
            UpdatePromptText();

            // 1. Phát tiếng bíp tắt nguồn
            if (powerClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(powerClickSound, clickVolume);
            }

            // 2. Dừng tiếng quạt PC
            if (fanAudioSource != null && fanAudioSource.isPlaying)
            {
                fanAudioSource.Stop();
            }

            // 3. Tắt màn hình 3D
            if (computerCutscene != null)
            {
                computerCutscene.PowerOffPC();
            }

            // 4. MỞ KHÓA TƯƠNG TÁC CON MÈO
            if (catColliderToEnable != null)
            {
                catColliderToEnable.enabled = true;
                Cat catComp = catColliderToEnable.GetComponent<Cat>();
                if (catComp != null) catComp.UnlockCat();
                Debug.Log("[PCPowerButton] 🔓 Đã tắt Case PC! Mở khóa tương tác cho Con Mèo.");
            }
            if (catObjectToEnable != null)
            {
                catObjectToEnable.SetActive(true);
            }
        }
    }
}
