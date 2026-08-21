using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class AdvancedDoor : MonoBehaviour, IInteractable
{
    public enum DoorOpenType { SingleHinge, DoubleHinge, Sliding }
    public enum DoorLockType { Unlocked, KeyItem, Passcode }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue = "";
        [TextArea(2, 4)]
        public string englishDialogue = "";
        public float holdDuration = 3.0f;
    }

    [Header("1. LOẠI CỬA & LOẠI KHÓA")]
    public DoorOpenType doorType = DoorOpenType.SingleHinge;
    public DoorLockType lockType = DoorLockType.Unlocked;

    [Header("2. CẤU HÌNH CHÌA KHÓA (TÊN TRONG INVENTORY)")]
    [Tooltip("Tên vật phẩm chìa khóa trong túi đồ (VD: Key, Key_PhongKham, Chìa khóa...)")]
    public string requiredKeyName = "Key";
    [Tooltip("Có trừ/xóa chìa khóa khỏi túi đồ sau khi mở cửa thành công không?")]
    public bool removeKeyOnUse = true;

    [Header("2.1. CẤU HÌNH HIỆU ỨNG MỞ XÍCH (FADE & ĐỔI KHÓA)")]
    [Tooltip("Đối tượng dây xích đang khóa (sẽ bị Destroy hoặc ẩn khi mở khóa)")]
    public GameObject lockedChainObject;

    [Tooltip("Đối tượng dây xích đã mở khóa trong Scene (Bật Active khi mở xong)")]
    public GameObject unlockedChainObject;

    [Tooltip("Prefab dây xích đã mở rơi dưới đất (Nếu dùng Prefab thay vì Scene Object)")]
    public GameObject unlockedChainPrefab;

    [Header("Âm Thanh Mở Xích & Thanh Trượt Âm Lượng (Sliders)")]
    [Tooltip("1. Âm thanh mở ổ khóa (kéo file small-padlock)")]
    public AudioClip unlockPadlockSound;
    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng mở ổ khóa (Mặc định: 0.8)")]
    public float padlockSoundVolume = 0.8f;

    [Tooltip("2. Âm thanh tháo xích / rung rào sắt (kéo file chainlinkfence)")]
    public AudioClip chainRattleSound;
    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng tháo xích rung rào sắt (Mặc định: 0.8)")]
    public float chainRattleSoundVolume = 0.8f;

    [Tooltip("3. Âm thanh xích rơi xuống đất (kéo file steel-combination-drop)")]
    public AudioClip chainDropSound;
    [Range(0f, 1f)]
    [Tooltip("Âm lượng tiếng xích rơi xuống đất (Mặc định: 0.8)")]
    public float chainDropSoundVolume = 0.8f;

    [Tooltip("Âm thanh tra chìa mở khóa (Dùng nếu chỉ có 1 file tổng hợp)")]
    public AudioClip unlockKeySound;

    [Header("Thời Gian Fade & Cooldown Mở Cửa")]
    [Tooltip("Thời gian làm tối màn hình (Fade Out)")]
    public float unlockFadeOutDuration = 0.5f;

    [Tooltip("Thời gian giữ màn hình đen phát chuỗi tiếng mở xích")]
    public float unlockHoldBlackDuration = 1.2f;

    [Tooltip("Thời gian sáng màn hình trở lại (Fade In)")]
    public float unlockFadeInDuration = 0.5f;

    [Tooltip("Thời gian chờ Cooldown sau khi mở khóa xong mới cho tương tác đóng mở cửa")]
    public float unlockCooldown = 0.8f;

    [Header("3. CẤU HÌNH CÁNH CỬA")]
    public Transform doorLeft;
    public Transform doorRight; // Dùng cho cửa 2 cánh hoặc trượt 2 bên
    public float openSpeed = 2f;

    [Header("4. GÓC XOAY & VỊ TRÍ MỞ (TỰ DO X-Y-Z)")]
    public Vector3 openRotationLeft = new Vector3(0f, 90f, 0f);
    public Vector3 openRotationRight = new Vector3(0f, -90f, 0f);
    public Vector3 slideOffsetLeft = new Vector3(1.5f, 0f, 0f);
    public Vector3 slideOffsetRight = new Vector3(-1.5f, 0f, 0f);

    [Header("5. MẬT MÃ KHÓA SỐ")]
    public string correctPasscode = "1234";

    [Header("6. ÂM THANH & ÂM LƯỢNG")]
    public AudioSource audioSource;
    [Range(0f, 1f)]
    [Tooltip("Chỉnh âm lượng tiếng mở / đóng cửa (Mặc định: 0.4)")]
    public float doorSoundVolume = 0.4f;

    [Range(0f, 1f)]
    [Tooltip("Chỉnh âm lượng tiếng kẹt khóa / mở khóa (Mặc định: 0.8)")]
    public float lockSoundVolume = 0.8f;

    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
    public AudioClip unlockedSound;

    [Header("7. CẤU HÌNH THOẠI KHI CỬA KHÓA (CHƯA CÓ CHÌA)")]
    [Tooltip("Danh sách câu thoại khi người chơi tương tác lúc chưa có chìa khóa")]
    public DialogueLine[] lockedDialogues = new DialogueLine[]
    {
        new DialogueLine
        {
            vietnameseDialogue = "Cánh cổng này đã bị khóa chặt rồi... Mình cần tìm chìa khóa để mở nó.",
            englishDialogue = "This gate is locked tightly... I need to find the key to open it.",
            holdDuration = 3.2f
        }
    };

    [Header("8. CẤU HÌNH PHỤ ĐỀ GÕ CHỮ TYPEWRITER")]
    public TextMeshProUGUI subtitleTextUI;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.03f;
    public float holdTimePerLine = 3.2f;
    public bool showBlinkingCursor = true;
    public bool useFadeEffect = true;
    public float fadeDuration = 0.2f;
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    [HideInInspector] public bool isOpen = false;
    [HideInInspector] public bool isLocked = false;
    private bool isMoving = false;
    private bool isUnlocking = false;
    private bool isDialoguePlaying = false;
    private Image fadeScreenImage;

    private Quaternion closedRotLeft, closedRotRight;
    private Vector3 closedPosLeft, closedPosRight;
    private Coroutine moveCoroutine;
    private Coroutine dialogueCoroutine;

    // Biến hỗ trợ skip thoại
    private bool isTyping = false;
    private bool isWaitingForNextLine = false;
    private bool skipRequested = false;
    private string currentFullText = "";
    private Coroutine cursorBlinkCoroutine;

    void Start()
    {
        isLocked = (lockType != DoorLockType.Unlocked);

        // Nếu cửa đang khóa thì đảm bảo xích mở ban đầu phải ẩn đi
        if (isLocked && unlockedChainObject != null)
        {
            unlockedChainObject.SetActive(false);
        }

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        if (doorLeft != null)
        {
            closedRotLeft = doorLeft.localRotation;
            closedPosLeft = doorLeft.localPosition;
        }
        if (doorRight != null)
        {
            closedRotRight = doorRight.localRotation;
            closedPosRight = doorRight.localPosition;
        }

        if (subtitleTextUI == null)
        {
            subtitleTextUI = FindSubtitleTextUI();
        }
    }

    void Update()
    {
        if (!isDialoguePlaying) return;

        // Bấm Chuột Trái hoặc Space để qua nhanh thoại
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                isTyping = false;
                if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;
            }
            else if (isWaitingForNextLine)
            {
                skipRequested = true;
            }
        }
    }

    // =========================================================================
    // IINTERACTABLE: TƯƠNG TÁC KHI NGƯỜI CHƠI CLICK CHUỘT TRÁI
    // =========================================================================
    public void Interact()
    {
        if (isMoving || isUnlocking) return;

        // 1. CỬA KHÔNG KHÓA -> MỞ / ĐÓNG TỰ DO
        if (!isLocked)
        {
            ToggleDoor();
            return;
        }

        // 2. CỬA KHÓA MÃ SỐ (PASSCODE)
        if (lockType == DoorLockType.Passcode)
        {
            PlaySound(lockedSound, lockSoundVolume);
            if (DoorPasscodeUI.Instance != null)
            {
                DoorPasscodeUI.Instance.OpenUI(this);
            }
            return;
        }

        // 3. CỬA KHÓA CHÌA (KEY ITEM)
        if (lockType == DoorLockType.KeyItem)
        {
            InventoryManager inventory = InventoryManager.Instance ?? Object.FindFirstObjectByType<InventoryManager>();

            // NẾU ĐÃ CÓ CHÌA KHÓA TRONG TÚI ĐỒ -> BẮT ĐẦU CHUỖI FADE & MỞ KHÓA XÍCH
            if (inventory != null && inventory.HasItem(requiredKeyName))
            {
                Debug.Log($"[AdvancedDoor] 🔑 Đã tìm thấy chìa khóa '{requiredKeyName}' trong túi! Bắt đầu mở khóa xích...");
                StartCoroutine(UnlockDoorWithFadeRoutine());
            }
            // NẾU CHƯA CÓ CHÌA KHÓA -> PHÁT TIẾNG KHÓA VÀ HIỆN THOẠI BÁO CỬA BỊ KHÓA
            else
            {
                PlaySound(lockedSound, lockSoundVolume);
                Debug.Log($"[AdvancedDoor] 🔒 Cửa bị khóa! Cần chìa khóa '{requiredKeyName}'.");

                if (!isDialoguePlaying && lockedDialogues != null && lockedDialogues.Length > 0)
                {
                    if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
                    dialogueCoroutine = StartCoroutine(PlayLockedDialogueRoutine());
                }
            }
        }
    }

    private IEnumerator UnlockDoorWithFadeRoutine()
    {
        isUnlocking = true;
        Image fadeImg = GetFadeImage();

        MovePl playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (playerMovePl != null)
        {
            playerMovePl.SetMovementState(false);
            playerMovePl.isCameraLocked = true;
        }

        // Ẩn toàn bộ UI Ingame (Camcorder, ItemUI, Hotbar...) và đưa fade panel lên lớp cao nhất
        PauseMenuManager.BringFadeToFront(fadeImg);
        PauseMenuManager.SetInGameHUDActive(false);

        // 1. Fade màn hình tối dần sang đen
        if (fadeImg != null)
        {
            fadeImg.gameObject.SetActive(true);
            fadeImg.raycastTarget = true;
            float elapsed = 0f;
            Color c = Color.black;
            while (elapsed < unlockFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(elapsed / unlockFadeOutDuration);
                fadeImg.color = c;
                yield return null;
            }
            c.a = 1f;
            fadeImg.color = c;
        }

        // 2. Phát chuỗi âm thanh: Mở ổ khóa -> Tháo xích -> Xích rơi xuống đất
        AudioClip firstClip = unlockPadlockSound != null ? unlockPadlockSound : (unlockKeySound != null ? unlockKeySound : (unlockedSound != null ? unlockedSound : lockedSound));
        if (firstClip != null)
        {
            PlaySound(firstClip, padlockSoundVolume);
        }

        if (chainRattleSound != null)
        {
            yield return new WaitForSeconds(0.25f);
            PlaySound(chainRattleSound, chainRattleSoundVolume);
        }

        if (chainDropSound != null)
        {
            yield return new WaitForSeconds(0.35f);
            PlaySound(chainDropSound, chainDropSoundVolume);
        }

        // 3. Trong lúc màn hình đen: Xóa dây xích đang khóa và hiện dây xích đã mở
        if (lockedChainObject != null)
        {
            Destroy(lockedChainObject);
        }

        if (unlockedChainObject != null)
        {
            unlockedChainObject.SetActive(true);
        }
        else if (unlockedChainPrefab != null)
        {
            Vector3 spawnPos = (lockedChainObject != null) ? lockedChainObject.transform.position : transform.position;
            Quaternion spawnRot = (lockedChainObject != null) ? lockedChainObject.transform.rotation : transform.rotation;
            Instantiate(unlockedChainPrefab, spawnPos, spawnRot);
        }

        // Đánh dấu cửa đã mở khóa (cửa vẫn giữ nguyên trạng thái đóng ban đầu)
        isLocked = false;

        // Trừ chìa khóa khỏi túi đồ
        if (removeKeyOnUse)
        {
            InventoryManager inventory = InventoryManager.Instance ?? Object.FindFirstObjectByType<InventoryManager>();
            if (inventory != null)
            {
                inventory.RemoveItem(requiredKeyName);
            }
        }

        // 4. Giữ màn hình đen một khoảng thời gian để chuỗi âm thanh phát xong trọn vẹn
        if (unlockHoldBlackDuration > 0f)
        {
            yield return new WaitForSeconds(unlockHoldBlackDuration);
        }

        // 5. Fade màn hình sáng dần trở lại
        if (fadeImg != null)
        {
            float elapsed = 0f;
            Color c = Color.black;
            while (elapsed < unlockFadeInDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(1f - (elapsed / unlockFadeInDuration));
                fadeImg.color = c;
                yield return null;
            }
            c.a = 0f;
            fadeImg.color = c;
            fadeImg.raycastTarget = false;
            fadeImg.gameObject.SetActive(false);
        }

        // Mở lại toàn bộ UI Ingame sau khi màn hình sáng hoàn toàn
        PauseMenuManager.SetInGameHUDActive(true);

        // 6. Trả lại quyền di chuyển và camera cho người chơi
        if (playerMovePl != null)
        {
            playerMovePl.SetMovementState(true);
            playerMovePl.isCameraLocked = false;
            playerMovePl.SyncRotationWithCurrentCamera();
        }

        // 7. Chờ Cooldown tương tác trước khi cho phép người chơi bấm mở/đóng cửa
        if (unlockCooldown > 0f)
        {
            yield return new WaitForSeconds(unlockCooldown);
        }

        isUnlocking = false;
        Debug.Log("[AdvancedDoor] 🔓 Mở khóa thành công! Cửa đã sẵn sàng để người chơi tự do đóng/mở.");
    }

    private Image GetFadeImage()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.fadePanel != null)
        {
            return PauseMenuManager.Instance.fadePanel;
        }

        if (fadeScreenImage != null) return fadeScreenImage;

        EnsureFadeImageExists();
        return fadeScreenImage;
    }

    void EnsureFadeImageExists()
    {
        if (fadeScreenImage != null) return;

        GameObject canvasObj = new GameObject("DoorFadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject imageObj = new GameObject("DoorFadePanel");
        imageObj.transform.SetParent(canvasObj.transform, false);

        fadeScreenImage = imageObj.AddComponent<Image>();
        fadeScreenImage.color = new Color(0f, 0f, 0f, 0f);
        fadeScreenImage.raycastTarget = false;

        RectTransform rect = fadeScreenImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        imageObj.SetActive(false);
    }

    IEnumerator PlayLockedDialogueRoutine()
    {
        isDialoguePlaying = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        yield return null; // Chờ frame click ban đầu

        if (lockedDialogues != null && lockedDialogues.Length > 0)
        {
            foreach (DialogueLine line in lockedDialogues)
            {
                if (line != null)
                {
                    yield return StartCoroutine(PlaySingleLineRoutine(line));
                }
            }
        }

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        isDialoguePlaying = false;
    }

    // =========================================================================
    // ENGINE GÕ CHỮ TYPEWRITER PHỤ ĐỀ
    // =========================================================================
    IEnumerator PlaySingleLineRoutine(DialogueLine line)
    {
        if (line == null) yield break;

        if (subtitleTextUI == null) subtitleTextUI = FindSubtitleTextUI();

        string lang = SettingsManager.currentLanguage;
        currentFullText = (lang == "VI") ? line.vietnameseDialogue : line.englishDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.vietnameseDialogue;
        if (string.IsNullOrEmpty(currentFullText)) currentFullText = line.englishDialogue;

        if (string.IsNullOrEmpty(currentFullText)) yield break;

        if (subtitleTextUI != null)
        {
            if (subtitleTextUI.transform.parent != null && !subtitleTextUI.transform.parent.gameObject.activeSelf)
            {
                subtitleTextUI.transform.parent.gameObject.SetActive(true);
            }
            subtitleTextUI.gameObject.SetActive(true);

            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;
        }

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        skipRequested = false;

        if (dialogueSound != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.clip = dialogueSound;
            audioSource.volume = dialogueVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        if (useTypewriterEffect && subtitleTextUI != null)
        {
            isTyping = true;
            subtitleTextUI.text = "";

            for (int i = 0; i <= currentFullText.Length; i++)
            {
                if (!isTyping || skipRequested) break;
                string typed = currentFullText.Substring(0, i);
                if (showBlinkingCursor) typed += "_";
                subtitleTextUI.text = typed;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            subtitleTextUI.text = currentFullText;
            isTyping = false;
        }
        else if (subtitleTextUI != null)
        {
            subtitleTextUI.text = currentFullText;
        }

        if (subtitleTextUI != null) subtitleTextUI.text = currentFullText;

        if (audioSource != null && audioSource.isPlaying)
        {
            StartCoroutine(FadeAudioOutRoutine(audioSource, 0.08f));
        }

        if (showBlinkingCursor && subtitleTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(subtitleTextUI, currentFullText));
        }

        isWaitingForNextLine = true;
        float holdTime = (line.holdDuration > 0f) ? line.holdDuration : holdTimePerLine;
        float waitTimer = 0f;
        while (waitTimer < holdTime && !skipRequested)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }
        isWaitingForNextLine = false;

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        if (useFadeEffect && subtitleTextUI != null)
        {
            yield return StartCoroutine(FadeTextOutRoutine(subtitleTextUI, fadeDuration));
        }

        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;
            subtitleTextUI.gameObject.SetActive(false);
        }

        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
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
            yield return new WaitForSeconds(0.4f);
        }
    }

    IEnumerator FadeTextOutRoutine(TextMeshProUGUI txt, float duration)
    {
        if (txt == null) yield break;
        float elapsed = 0f;
        Color c = txt.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            txt.color = c;
            yield return null;
        }
        c.a = 0f;
        txt.color = c;
    }

    IEnumerator FadeAudioOutRoutine(AudioSource src, float duration)
    {
        if (src == null || !src.isPlaying) yield break;
        float startVol = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
        src.Stop();
        src.volume = startVol;
    }

    TextMeshProUGUI FindSubtitleTextUI()
    {
        if (Map02IntroSequence.Instance != null && Map02IntroSequence.Instance.subtitleTextUI != null)
            return Map02IntroSequence.Instance.subtitleTextUI;

        SmartInteractionDialogue sid = Object.FindFirstObjectByType<SmartInteractionDialogue>(FindObjectsInactive.Include);
        if (sid != null && sid.subtitleTextUI != null) return sid.subtitleTextUI;

        GameObject subObj = GameObject.Find("SubtitlesText") ?? GameObject.Find("Subtitle Text") ?? GameObject.Find("SubtitleText") ?? GameObject.Find("Subtitle") ?? GameObject.Find("DialogueText");
        if (subObj != null) return subObj.GetComponent<TextMeshProUGUI>();

        TextMeshProUGUI[] tmps = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in tmps)
        {
            if (tmp.gameObject.name.ToLower().Contains("sub")) return tmp;
        }

        return null;
    }

    public void UnlockDoor()
    {
        isLocked = false;
        PlaySound(unlockedSound, lockSoundVolume);
        ToggleDoor();
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        PlaySound(isOpen ? openSound : closeSound, doorSoundVolume);

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(AnimateDoor(isOpen));
    }

    private IEnumerator AnimateDoor(bool opening)
    {
        isMoving = true;
        float elapsed = 0f;

        Quaternion startRotL = doorLeft != null ? doorLeft.localRotation : Quaternion.identity;
        Quaternion startRotR = doorRight != null ? doorRight.localRotation : Quaternion.identity;
        Vector3 startPosL = doorLeft != null ? doorLeft.localPosition : Vector3.zero;
        Vector3 startPosR = doorRight != null ? doorRight.localPosition : Vector3.zero;

        Quaternion targetRotL = opening ? closedRotLeft * Quaternion.Euler(openRotationLeft) : closedRotLeft;
        Quaternion targetRotR = opening ? closedRotRight * Quaternion.Euler(openRotationRight) : closedRotRight;
        Vector3 targetPosL = opening ? closedPosLeft + slideOffsetLeft : closedPosLeft;
        Vector3 targetPosR = opening ? closedPosRight + slideOffsetRight : closedPosRight;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openSpeed;
            float t = Mathf.SmoothStep(0, 1, elapsed);

            if (doorType == DoorOpenType.SingleHinge || doorType == DoorOpenType.DoubleHinge)
            {
                if (doorLeft != null) doorLeft.localRotation = Quaternion.Slerp(startRotL, targetRotL, t);
                if (doorType == DoorOpenType.DoubleHinge && doorRight != null)
                {
                    doorRight.localRotation = Quaternion.Slerp(startRotR, targetRotR, t);
                }
            }
            else if (doorType == DoorOpenType.Sliding)
            {
                if (doorLeft != null) doorLeft.localPosition = Vector3.Lerp(startPosL, targetPosL, t);
                if (doorRight != null) doorRight.localPosition = Vector3.Lerp(startPosR, targetPosR, t);
            }

            yield return null;
        }

        isMoving = false;
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, volume);
    }
}