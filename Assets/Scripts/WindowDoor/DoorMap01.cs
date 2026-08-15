using UnityEngine;
using System.Collections;

public class DoorExit : MonoBehaviour, IInteractable
{
    [Header("1. Tham Chiếu Cánh Cửa (Door Transform)")]
    [Tooltip("Kéo Transform của cánh cửa trượt vào đây")]
    public Transform doorBody;

    [Header("2. Hướng & Khoảng Cách Trượt Mở Cửa (Local Offset)")]
    [Tooltip("Hướng và khoảng cách trượt cửa (Mặc định X: 1.25m sang bên phải)")]
    public Vector3 slideDirection = new Vector3(1.25f, 0f, 0f);

    [Tooltip("Tích chọn: Di chuyển theo hướng xoay Local của Cửa (chuẩn). Bỏ tích: Di chuyển theo trục World tĩnh")]
    public bool followDoorLocalRotation = true;

    [Tooltip("Tốc độ trượt mở cửa mượt mà")]
    public float doorSpeed = 3f;

    [Header("3. Khóa Tương Tác Ban Đầu & Sau Khi Mở")]
    [Tooltip("Tích chọn để khóa tương tác Cửa Chính lúc đầu (Chờ ngủ dậy mới mở khóa)")]
    public bool lockOnStart = true;

    [Tooltip("Tích chọn: Sau khi mở cửa ra nhận đồ xong sẽ CHẶN KHÔNG CHO BẤM F TƯƠNG TÁC LẠI VỚI CỬA NỮA!")]
    public bool disableInteractionAfterOpen = true;

    [Tooltip("Kéo Object NPC Johnson vào đây để tự động mở khóa tương tác cho NPC khi vừa mở cửa!")]
    public GameObject npcToEnableOnOpen;

    [Header("4. Âm Thanh Cửa Trượt (Tùy chọn)")]
    [Tooltip("Kéo 1 file âm thanh tiếng trượt cửa vào đây (dùng chung cho cả mở & đóng)")]
    public AudioClip doorSlideSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.8f;

    public GameObject doorHintUI;

    [Header("5. Nguồn Âm Thanh Môi Trường Thành Phố (City Ambience Control)")]
    [Tooltip("Kéo AudioSource phát tiếng thành phố đêm lặp đi lặp lại vào đây (Ví dụ: CityAudio)")]
    public AudioSource cityAmbienceAudioSource;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng thành phố khi CỬA CHÍNH ĐÓNG (Mặc định: 0.35f)")]
    public float closedVolume = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("Âm lượng thành phố khi CỬA CHÍNH MỞ (Mặc định: 0.8f)")]
    public float openedVolume = 0.8f;

    [Tooltip("Thời gian chuyển đổi âm lượng mượt mà khi mở/đóng cửa (giây)")]
    public float volumeFadeDuration = 1.5f;

    [Header("6. Danh Sách Lời Thoại (Locked vs Unlocked Dialogues)")]
    [Tooltip("Thoại phát khi CỬA ĐANG KHÓA (chưa ngủ dậy). Thêm (+) hoặc để trống")]
    public SmartInteractionDialogue.DialogueLine[] lockedDialogueLines = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Đêm muộn rồi, không có việc gì để ra ngoài cả.",
            englishDialogue = "It's late at night, I have no reason to go outside."
        }
    };

    [Tooltip("Thoại phát khi MỞ CỬA THÀNH CÔNG. Thêm (+) hoặc để trống nếu muốn mở ngay lập tức")]
    public SmartInteractionDialogue.DialogueLine[] unlockedDialogueLines;

    [Header("6.1. Âm Thanh Thoại Gõ Chữ (Dialogue SFX)")]
    [Tooltip("Gói âm thanh lồng tiếng / gõ chữ khi hiện phụ đề cửa (5s blip)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)]
    [Tooltip("Âm lượng âm thanh thoại gõ chữ (Mặc định: 0.8)")]
    public float dialogueVolume = 0.8f;

    private bool isDoorOpen = false;
    private bool isInteractionBlocked = false;
    private bool isLocked = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private AudioSource audioSource;
    private Coroutine fadeCoroutine;
    private SmartInteractionDialogue dialoguePlayer;

    void Start()
    {
        isLocked = lockOnStart;

        if (doorBody == null) doorBody = transform;

        // Lưu vị trí đóng ban đầu (tọa độ Local)
        closedPosition = doorBody.localPosition;

        if (followDoorLocalRotation)
        {
            Vector3 worldSlideDir = doorBody.TransformDirection(slideDirection);
            if (doorBody.parent != null)
            {
                openPosition = closedPosition + doorBody.parent.InverseTransformDirection(worldSlideDir);
            }
            else
            {
                openPosition = closedPosition + slideDirection;
            }
        }
        else
        {
            openPosition = closedPosition + slideDirection;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 0.65f; // Hiệu ứng âm thanh 3D chân thực, định vị vị trí cánh cửa
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 15f;
        audioSource.playOnAwake = false;

        if (doorHintUI != null) doorHintUI.SetActive(false);

        // Tự động tìm nguồn âm thanh CityAudio nếu chưa kéo
        EnsureCityAudioSource();

        // Cấu hình ban đầu cho tiếng thành phố khi cửa đang ĐÓNG
        if (cityAmbienceAudioSource != null)
        {
            cityAmbienceAudioSource.loop = true;
            cityAmbienceAudioSource.volume = isDoorOpen ? openedVolume : closedVolume;
            if (!cityAmbienceAudioSource.isPlaying)
            {
                cityAmbienceAudioSource.Play();
            }
        }
    }

    void EnsureCityAudioSource()
    {
        if (cityAmbienceAudioSource != null) return;

        GameObject cityObj = GameObject.Find("CityAudio");
        if (cityObj != null) cityAmbienceAudioSource = cityObj.GetComponent<AudioSource>();

        if (cityAmbienceAudioSource == null)
        {
            WindowAmbienceController win = Object.FindFirstObjectByType<WindowAmbienceController>();
            if (win != null) cityAmbienceAudioSource = win.cityAmbienceAudioSource;
        }
    }

    /// <summary>
    /// GỌI HÀM NÀY ĐỂ MỞ KHÓA TƯƠNG TÁC CHO CỬA CHÍNH (Được gọi khi NGỦ DẬY)
    /// </summary>
    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("[DoorExit] 🔓 ĐÃ MỞ KHÓA TƯƠNG TÁC CHO CỬA CHÍNH!");
    }

    void Update()
    {
        if (doorBody == null) return;

        // Trượt mở / đóng cửa mượt mà theo vị trí mục tiêu
        Vector3 targetPos = isDoorOpen ? openPosition : closedPosition;
        doorBody.localPosition = Vector3.Lerp(doorBody.localPosition, targetPos, Time.deltaTime * doorSpeed);
    }

    // ==========================================
    // TƯƠNG TÁC BẤM [F] ĐỂ MỞ CỬA
    // ==========================================
    public void Interact()
    {
        if (isInteractionBlocked) return;

        if (dialoguePlayer == null)
        {
            dialoguePlayer = GetComponent<SmartInteractionDialogue>();
            if (dialoguePlayer == null) dialoguePlayer = gameObject.AddComponent<SmartInteractionDialogue>();
        }
        dialoguePlayer.dialogueSound = dialogueSound;
        dialoguePlayer.soundVolume = dialogueVolume;

        // Nếu cửa vẫn đang bị khóa (chưa ngủ dậy) thì không cho mở & phát thoại Khóa
        if (isLocked)
        {
            Debug.Log("[DoorExit] 🔒 Cửa đang khóa! Bạn cần nằm ngủ trên nệm trước.");
            if (lockedDialogueLines != null && lockedDialogueLines.Length > 0)
            {
                dialoguePlayer.PlayCustomLines(lockedDialogueLines);
            }
            return;
        }

        // Nếu đã mở khóa -> Kiểm tra có thoại Unlocked không
        if (unlockedDialogueLines != null && unlockedDialogueLines.Length > 0)
        {
            dialoguePlayer.PlayCustomLines(unlockedDialogueLines, () => ExecuteDoorOpen());
        }
        else
        {
            ExecuteDoorOpen();
        }
    }

    void ExecuteDoorOpen()
    {
        // Dừng tiếng gõ cửa liên tục khi tương tác cửa
        ContinuousDoorKnocker knocker = GetComponent<ContinuousDoorKnocker>();
        if (knocker == null) knocker = Object.FindFirstObjectByType<ContinuousDoorKnocker>();
        if (knocker != null) knocker.StopKnocking();

        ToggleDoor();

        if (isDoorOpen && disableInteractionAfterOpen)
        {
            isInteractionBlocked = true;
            HidePrompt();

            // TẮT COLLIDER CỦA CỬA ĐỂ TIA NHÌN XUYÊN QUA TRÚNG NPC PHÍA SAU
            Collider doorCollider = GetComponent<Collider>();
            if (doorCollider != null) doorCollider.enabled = false;

            if (npcToEnableOnOpen != null)
            {
                npcToEnableOnOpen.SetActive(true);
                Collider col = npcToEnableOnOpen.GetComponent<Collider>();
                if (col != null) col.enabled = true;
            }

            // Tự động tìm NPCDialogueCutscene để mở khóa
            NPCDialogueCutscene npcCutscene = Object.FindFirstObjectByType<NPCDialogueCutscene>();
            if (npcCutscene != null)
            {
                npcCutscene.UnlockNPC();
            }
        }
    }

    public void ToggleDoor()
    {
        isDoorOpen = !isDoorOpen;

        if (audioSource != null && doorSlideSound != null)
        {
            audioSource.PlayOneShot(doorSlideSound, soundVolume);
        }

        UpdateAmbienceVolume();
    }

    /// <summary>
    /// Tự động trượt mở cửa (Dùng cho sự kiện kinh dị cán mốc 10s máy quay)
    /// </summary>
    public void OpenDoorAutomatically(AudioClip customSound = null)
    {
        if (isDoorOpen) return;

        isDoorOpen = true;
        isInteractionBlocked = true;
        HidePrompt();

        // Tắt collider để tia nhìn không bị chặn
        Collider doorCollider = GetComponent<Collider>();
        if (doorCollider != null) doorCollider.enabled = false;

        if (audioSource != null)
        {
            AudioClip clip = (customSound != null) ? customSound : doorSlideSound;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, soundVolume);
            }
        }

        UpdateAmbienceVolume();
        Debug.Log("[DoorExit] 🚪 Cánh cửa tự động mở ra do sự kiện máy quay 10s!");
    }

    public bool IsFullyClosed()
    {
        if (doorBody == null) return true;
        return Vector3.Distance(doorBody.localPosition, closedPosition) < 0.05f;
    }

    public void CloseDoor(bool snapInstantly = false)
    {
        isDoorOpen = false;
        isInteractionBlocked = true;
        HidePrompt();

        if (snapInstantly && doorBody != null)
        {
            doorBody.localPosition = closedPosition;
        }

        Collider doorCollider = GetComponent<Collider>();
        if (doorCollider != null) doorCollider.enabled = true;

        if (audioSource != null && doorSlideSound != null)
        {
            audioSource.PlayOneShot(doorSlideSound, soundVolume);
        }

        UpdateAmbienceVolume();
    }

    void UpdateAmbienceVolume()
    {
        EnsureCityAudioSource();

        float targetVol = isDoorOpen ? openedVolume : closedVolume;
        if (cityAmbienceAudioSource != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeAmbienceRoutine(targetVol));
            Debug.Log($"[DoorExit] 🔊 Chuyển âm lượng thành phố về: {targetVol} (isDoorOpen = {isDoorOpen})");
        }
    }

    IEnumerator FadeAmbienceRoutine(float targetVol)
    {
        float startVol = cityAmbienceAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < volumeFadeDuration)
        {
            elapsed += Time.deltaTime;
            cityAmbienceAudioSource.volume = Mathf.Lerp(startVol, targetVol, elapsed / volumeFadeDuration);
            yield return null;
        }

        cityAmbienceAudioSource.volume = targetVol;
    }

    public void ShowPrompt()
    {
        if (isInteractionBlocked || isLocked) return;
        if (doorHintUI != null) doorHintUI.SetActive(true);
    }

    public void HidePrompt()
    {
        if (doorHintUI != null) doorHintUI.SetActive(false);
    }
}