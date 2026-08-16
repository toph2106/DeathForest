using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class NPCDialogueCutscene : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue;
        [TextArea(2, 4)]
        public string englishDialogue;
    }

    [Header("1. Điểm Vị Trí Camera Lúc Nói Chuyện (Camera Target Point)")]
    [Tooltip("Kéo điểm Transform (Empty GameObject) bạn tạo sẵn vào đây để camera bay mượt tới đó")]
    public Transform cameraTargetPoint;

    [Tooltip("Thời gian camera bay mượt (tới điểm target & bay trở lại người chơi) tính bằng giây")]
    public float cameraTransitionDuration = 0.8f;

    [Header("Cấu Hình Tọa Độ Thủ Công (Nếu KHÔNG kéo cameraTargetPoint)")]
    public bool lockCameraTransform = true;
    public Vector3 lockedCameraLocalPos = new Vector3(0.271f, 0.6f, 0.018f);
    public Vector3 lockedCameraLocalRot = new Vector3(1.494f, 0f, 0f);

    [Header("2. Giao Diện Hiển Thị Lời Thoại (UI)")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI dialogueTextUI;
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.02f;

    [Header("2.1. Hiệu Ứng Con Trỏ '_' & Fade Mờ Dần (Đồng Bộ Chuẩn)")]
    [Tooltip("Bật hiệu ứng con trỏ '_' nhấp nháy cuối câu thoại")]
    public bool showBlinkingCursor = true;
    [Tooltip("Bật hiệu ứng mờ dần Fade Out khi chuyển câu thoại")]
    public bool useFadeEffect = true;
    [Tooltip("Thời gian mờ dần Fade (Mặc định: 0.2 giây)")]
    public float fadeDuration = 0.2f;

    [Header("3. Danh Sách Lời Thoại (Mảng Anh / Việt Dễ Sửa Số Lượng)")]
    public DialogueLine[] dialogueLines;

    [Header("3.1. Âm Thanh Giọng NPC Chung (Dialogue SFX)")]
    [Tooltip("Gói âm thanh lồng tiếng / tiếng nói chuyện chung cho toàn bộ các câu thoại của NPC")]
    public AudioClip npcVoiceSound;
    [Range(0f, 1f)] public float voiceVolume = 0.85f;

    [Header("4. Hộp Hàng Cần Bật Tương Tác Sau Khi Thoại Xong")]
    public GameObject boxInteractObject;
    public Collider boxCollider;

    [Header("5. Prompt UI [F] Khi Nhìn Vào NPC")]
    public GameObject npcHintUI;

    [Header("6. Cấu Hình Cho Phép Đọc Lại Thoại & Khóa Lúc Đầu")]
    [Tooltip("Tích chọn để khóa tương tác NPC lúc đầu (Chờ mở cửa chính mới mở khóa)")]
    public bool lockOnStart = true;

    [Tooltip("Tích chọn để người chơi có thể bấm [F] xem lại thoại NPC nhiều lần tùy thích")]
    public bool allowRepeatDialogue = true;

    // --- BIẾN NỘI BỘ ---
    private int currentLineIndex = 0;
    public bool isInCutscene = false;
    private bool isTyping = false;
    private bool hasFinishedDialogue = false;
    private string currentFullText = "";
    private Coroutine textCoroutine;
    private Coroutine transitionCoroutine;
    private Coroutine cursorBlinkCoroutine;
    private AudioSource audioSource;

    // Lưu vị trí & góc xoay gốc của Camera
    private Transform mainCameraTransform;
    private Vector3 originalCamLocalPos;
    private Quaternion originalCamLocalRot;
    private MovePl playerMovePl;

    private bool isTransitioning = false;
    private Vector3 targetCamPos;
    private Quaternion targetCamRot;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        if (dialogueCanvas != null) dialogueCanvas.SetActive(false);
        if (npcHintUI != null) npcHintUI.SetActive(false);

        // Khóa tương tác NPC lúc đầu game
        if (lockOnStart)
        {
            Collider npcCol = GetComponent<Collider>();
            if (npcCol != null) npcCol.enabled = false;
        }

        DisableBoxInteractionAtStart();
    }

    /// <summary>
    /// GỌI HÀM NÀY ĐỂ MỞ KHÓA TƯƠNG TÁC CHO NPC (Được gọi khi MỞ CỬA CHÍNH)
    /// </summary>
    public void UnlockNPC()
    {
        Collider npcCol = GetComponent<Collider>();
        if (npcCol != null)
        {
            npcCol.enabled = true;
            Debug.Log("[NPCDialogueCutscene] 🔓 ĐÃ MỞ KHÓA TƯƠNG TÁC CHO NPC JOHNSON!");
        }
    }

    void DisableBoxInteractionAtStart()
    {
        if (boxCollider != null) boxCollider.enabled = false;
        if (boxInteractObject != null)
        {
            Collider col = boxInteractObject.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            NPCDeliveryBox deliveryBox = boxInteractObject.GetComponent<NPCDeliveryBox>();
            if (deliveryBox != null) deliveryBox.LockInteraction();

            InteractableItem itemScript = boxInteractObject.GetComponent<InteractableItem>();
            if (itemScript != null) itemScript.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (!isInCutscene || isTransitioning) return;

        // Nếu đã hoàn thành transition thì giữ camera ở vị trí target
        if (mainCameraTransform != null)
        {
            mainCameraTransform.position = targetCamPos;
            mainCameraTransform.rotation = targetCamRot;
        }

        // Bấm Chuột trái (Mouse 0) hoặc Space để chuyển câu thoại mới
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogue();
        }
    }

    public void Interact()
    {
        if (isInCutscene || isTransitioning) return;
        if (!allowRepeatDialogue && hasFinishedDialogue) return;

        Debug.Log("[NPCDialogueCutscene] ✅ Interact() được gọi! Bắt đầu cutscene thoại...");
        StartDialogueCutscene();
    }

    void StartDialogueCutscene()
    {
        isInCutscene = true;
        currentLineIndex = 0;
        HidePrompt();

        // Tìm camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCameraTransform = mainCam.transform;
            originalCamLocalPos = mainCameraTransform.localPosition;
            originalCamLocalRot = mainCameraTransform.localRotation;
        }

        // KHÓA CAMERA CHUỘT & DI CHUYỂN BẰNG CỜ isCameraLocked TRONG MovePl
        playerMovePl = FindFirstObjectByType<MovePl>();
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = true;
            playerMovePl.SetMovementState(false);
        }

        // XÁC ĐỊNH VỊ TRÍ & GÓC XOAY TARGET CHO CAMERA
        if (cameraTargetPoint != null)
        {
            targetCamPos = cameraTargetPoint.position;
            targetCamRot = cameraTargetPoint.rotation;
        }
        else if (lockCameraTransform && playerMovePl != null)
        {
            targetCamPos = playerMovePl.transform.TransformPoint(lockedCameraLocalPos);
            targetCamRot = playerMovePl.transform.rotation * Quaternion.Euler(lockedCameraLocalRot);
        }
        else if (mainCameraTransform != null)
        {
            targetCamPos = mainCameraTransform.position;
            targetCamRot = mainCameraTransform.rotation;
        }

        // BAY CAMERA MƯỢT MÀ TỚI ĐIỂM MỤC TIÊU
        if (mainCameraTransform != null && cameraTransitionDuration > 0f)
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionCameraRoutine(mainCameraTransform.position, mainCameraTransform.rotation, targetCamPos, targetCamRot));
        }
        else if (mainCameraTransform != null)
        {
            mainCameraTransform.position = targetCamPos;
            mainCameraTransform.rotation = targetCamRot;
        }

        // Bật UI thoại
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);

            Canvas canvas = dialogueCanvas.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
            }
        }

        Debug.Log("[NPCDialogueCutscene] 🎬 Cutscene bắt đầu! Camera bay mượt tới điểm target...");

        ShowCurrentLine();
    }

    IEnumerator TransitionCameraRoutine(Vector3 startPos, Quaternion startRot, Vector3 endPos, Quaternion endRot)
    {
        isTransitioning = true;
        float elapsed = 0f;

        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / cameraTransitionDuration);

            if (mainCameraTransform != null)
            {
                mainCameraTransform.position = Vector3.Lerp(startPos, endPos, t);
                mainCameraTransform.rotation = Quaternion.Slerp(startRot, endRot, t);
            }

            yield return null;
        }

        if (mainCameraTransform != null)
        {
            mainCameraTransform.position = endPos;
            mainCameraTransform.rotation = endRot;
        }

        isTransitioning = false;
    }

    void ShowCurrentLine()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            EndDialogueCutscene();
            return;
        }

        if (currentLineIndex >= dialogueLines.Length)
        {
            EndDialogueCutscene();
            return;
        }

        DialogueLine line = dialogueLines[currentLineIndex];
        currentFullText = (SettingsManager.currentLanguage == "VI") ? line.vietnameseDialogue : line.englishDialogue;

        if (textCoroutine != null) StopCoroutine(textCoroutine);
        textCoroutine = StartCoroutine(DisplayTextRoutine(currentFullText));
    }

    void AdvanceDialogue()
    {
        if (isTyping)
        {
            CompleteTextInstantly();
            return;
        }

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        currentLineIndex++;
        if (currentLineIndex < dialogueLines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialogueCutscene();
        }
    }

    IEnumerator DisplayTextRoutine(string targetText)
    {
        if (dialogueTextUI == null) yield break;

        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        Color sc = dialogueTextUI.color;
        sc.a = 1f;
        dialogueTextUI.color = sc;

        isTyping = true;
        dialogueTextUI.text = "";

        // BẬT GIỌNG NÓI TRONG LÚC ĐANG GÕ CHỮ
        if (npcVoiceSound != null && audioSource != null)
        {
            audioSource.clip = npcVoiceSound;
            audioSource.volume = voiceVolume;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        if (useTypewriterEffect)
        {
            for (int i = 0; i <= targetText.Length; i++)
            {
                if (!isTyping) break;
                string typed = targetText.Substring(0, i);
                if (showBlinkingCursor) typed += "_";
                dialogueTextUI.text = typed;
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        CompleteTextInstantly();
    }

    void CompleteTextInstantly()
    {
        isTyping = false;
        if (dialogueTextUI != null)
        {
            dialogueTextUI.text = currentFullText;
            Color sc = dialogueTextUI.color;
            sc.a = 1f;
            dialogueTextUI.color = sc;
        }

        // TẮT GIỌNG NÓI KHI ĐÃ HIỆN XONG CHỮ (HOẶC SKIP)
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // BẬT CON TRỎ NHẤP NHÁY '_' TRONG LÚC CHỜ CLICK
        if (showBlinkingCursor && dialogueTextUI != null)
        {
            if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(dialogueTextUI, currentFullText));
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

    void EndDialogueCutscene()
    {
        if (cursorBlinkCoroutine != null)
        {
            StopCoroutine(cursorBlinkCoroutine);
            cursorBlinkCoroutine = null;
        }

        if (textCoroutine != null) StopCoroutine(textCoroutine);
        if (dialogueCanvas != null) dialogueCanvas.SetActive(false);

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // BAY CAMERA MƯỢT MÀ TRỞ LẠI VỊ TRÍ GỐC CỦA PLAYER
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionCameraBackRoutine());
    }

    IEnumerator TransitionCameraBackRoutine()
    {
        isTransitioning = true;
        float elapsed = 0f;

        Vector3 currentPos = (mainCameraTransform != null) ? mainCameraTransform.position : targetCamPos;
        Quaternion currentRot = (mainCameraTransform != null) ? mainCameraTransform.rotation : targetCamRot;

        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / cameraTransitionDuration);

            if (mainCameraTransform != null && playerMovePl != null)
            {
                Vector3 targetPlayerCamPos = playerMovePl.transform.TransformPoint(originalCamLocalPos);
                Quaternion targetPlayerCamRot = playerMovePl.transform.rotation * originalCamLocalRot;

                mainCameraTransform.position = Vector3.Lerp(currentPos, targetPlayerCamPos, t);
                mainCameraTransform.rotation = Quaternion.Slerp(currentRot, targetPlayerCamRot, t);
            }

            yield return null;
        }

        // Trả Camera về vị trí local chuẩn trên Player
        if (mainCameraTransform != null)
        {
            mainCameraTransform.localPosition = originalCamLocalPos;
            mainCameraTransform.localRotation = originalCamLocalRot;
        }

        // Mở lại di chuyển & camera chuột cho Player
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = false;
            playerMovePl.SetMovementState(true);
            playerMovePl.SyncRotationWithCurrentCamera();
        }

        isTransitioning = false;
        isInCutscene = false;
        hasFinishedDialogue = true;

        EnableBoxInteraction();

        Debug.Log("[NPCDialogueCutscene] ✅ Kết thúc thoại! Camera đã bay mượt trở lại vị trí Player.");
    }

    void EnableBoxInteraction()
    {
        if (boxCollider != null) boxCollider.enabled = true;
        if (boxInteractObject != null)
        {
            boxInteractObject.SetActive(true);

            Collider col = boxInteractObject.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            NPCDeliveryBox deliveryBox = boxInteractObject.GetComponent<NPCDeliveryBox>();
            if (deliveryBox != null) deliveryBox.UnlockInteraction();

            InteractableItem itemScript = boxInteractObject.GetComponent<InteractableItem>();
            if (itemScript != null) itemScript.enabled = true;
        }
    }

    public void ShowPrompt()
    {
        if (isInCutscene || isTransitioning) return;
        if (!allowRepeatDialogue && hasFinishedDialogue) return;
        if (npcHintUI != null) npcHintUI.SetActive(true);
    }

    public void HidePrompt()
    {
        if (npcHintUI != null) npcHintUI.SetActive(false);
    }
}
