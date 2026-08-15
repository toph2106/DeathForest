using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InWorldComputerCutscene : MonoBehaviour, IInteractable
{
    public static bool isUsingComputer = false;
    public static bool hasCompletedComputer = false;

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string vietnameseDialogue;
        [TextArea(2, 4)]
        public string englishDialogue;
    }

    [System.Serializable]
    public class ComputerImageStep
    {
        [Header("1. Hình ảnh hiển thị trên Màn Hình 3D Máy Tính")]
        [Tooltip("Kéo ảnh Texture (Desktop, YouTube, Twitter DM, Twitter Post...) vào đây.")]
        public Texture screenTexture;

        [Header("2. Ảnh phát sáng Emissive (Tùy chọn)")]
        [Tooltip("Nếu Shader Graph dùng ô Emissive riêng, kéo cùng bức ảnh ở trên vào đây hoặc để trống code tự gán")]
        public Texture emissiveTexture;

        [Header("3. Danh sách các câu thoại dành cho bức ảnh này")]
        [Tooltip("Bấm (+) để thêm nhiều câu thoại chạy lần lượt cho bức ảnh này")]
        public DialogueLine[] dialogueLines;
    }

    [Header("1. Đối Tượng Màn Hình 3D (3D Screen Renderer)")]
    [Tooltip("Kéo cái 3D Screen (Quad / MeshRenderer của màn hình máy tính) vào đây")]
    public Renderer monitorScreenRenderer;
    [Tooltip("Vị trí Material của màn hình (Mặc định: 0)")]
    public int materialIndex = 0;

    [Header("2. Trạng Thái Bật / Tắt Nguồn")]
    [Tooltip("Màn hình 3D đen ngòm lúc mới vào game cho đến khi ấn nút bật Case PC")]
    public bool startPitchBlack = true;
    [Tooltip("Kéo bức ảnh màu đen xì vào đây (hoặc để trống code tự tạo màu đen)")]
    public Texture pitchBlackTexture;
    [Tooltip("Trạng thái máy tính đã được bật nguồn chưa")]
    public bool isPoweredOn = false;

    [Header("3. Màn Hình Khởi Động (Windows Boot Sequence)")]
    [Tooltip("Ảnh 0: Màn hình Windows Boot / Desktop lúc mới nhấn nút nguồn Case PC")]
    public Texture windowsBootTexture;

    [Header("4. Âm Thanh Thoại & Click Chuột")]
    [Tooltip("Gói âm thanh lồng tiếng / tiếng thở / gõ phím chung khi chạy các câu thoại PC")]
    public AudioClip generalDialogueSound;
    [Tooltip("Kéo âm thanh click chuột (taira-komori__click.wav) vào đây")]
    public AudioClip mouseClickSound;

    [Header("5. Danh Sách Các Bức Ảnh & Chuỗi Thoại Đi Kèm")]
    public ComputerImageStep[] imageSteps;

    [Header("6. Phụ Đề Hiển Thị Lời Thoại (Subtitle Text)")]
    [Tooltip("Kéo cái TextMeshProUGUI hiển thị lời thoại vào đây")]
    public TextMeshProUGUI subtitleText;
    [Tooltip("Dòng chữ nhắc người chơi bấm phím (VD: Click chuột trái để tiếp tục - Tùy chọn)")]
    public TextMeshProUGUI promptText;

    [Header("7. Camera Focus & Di Chuyển Trực Diện")]
    [Tooltip("Transform điểm nhìn Camera nhìn trực diện màn hình PC (Tùy chọn - Nếu trống code tự hướng thẳng màn hình)")]
    public Transform cameraFocusPoint;
    [Tooltip("Tốc độ trượt Camera lại gần màn hình máy tính")]
    public float cameraSmoothSpeed = 4f;

    [Header("8. Danh Sách Các UI Khác Cần ẨN Khi Đang Xem Máy Tính")]
    [Tooltip("Kéo các UI như Tâm ngắm, Nút [F], HUD, Camcorder UI... vào đây để ẩn đi khi đang xem PC")]
    public GameObject[] uisToHide;

    [Header("9. Tên Thuộc Tính Shader Tùy Chỉnh (Nếu Shader Graph đặc biệt)")]
    public string customBaseTextureProperty = "";
    public string customEmissiveProperty = "";

    [Header("10. Cấu Hình Hiệu Ứng Gõ Chữ, Mờ Dần & Con Trỏ Nhấp Nháy")]
    [Tooltip("Bật hiệu ứng gõ chữ từng ký tự (Typewriter Effect)")]
    public bool useTypewriterEffect = true;
    [Tooltip("Tốc độ gõ từng chữ (Mặc định: 0.03s/chữ)")]
    public float typingSpeed = 0.03f;

    [Tooltip("Bật hiệu ứng mờ dần Fade In khi xuất hiện & Fade Out khi biến mất/chuyển thoại")]
    public bool useFadeEffect = true;
    [Tooltip("Thời gian mờ dần Fade In / Fade Out (Mặc định: 0.25 giây)")]
    public float fadeDuration = 0.25f;

    [Tooltip("Bật hiệu ứng con trỏ '_' nhấp nháy cuối câu thoại")]
    public bool showBlinkingCursor = true;
    [Tooltip("Tiếng gõ bàn phím / tiếng click nhỏ theo từng ký tự (Tùy chọn)")]
    public AudioClip typingSound;

    private int currentStepIndex = 0;
    private int currentLineIndex = 0;
    private bool isCutsceneRunning = false;
    private bool isReturningCamera = false;

    private Material screenMaterial;
    private AudioSource audioSource;
    private AudioSource typingAudioSource;
    private Transform mainCameraTransform;
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;

    private MovePl playerMoveScript;
    private CharacterController playerController;

    private Coroutine dialogueCoroutine;
    private Coroutine cursorBlinkCoroutine;
    private bool isTyping = false;
    private string currentFullDialogue = "";
    private float cutsceneStartTime = 0f;

    void Start()
    {
        SetupComponents();

        if (subtitleText != null) subtitleText.gameObject.SetActive(false);
        if (promptText != null) promptText.gameObject.SetActive(false);

        if (startPitchBlack && !isPoweredOn)
        {
            TurnScreenOffImmediate();
        }
    }

    void SetupComponents()
    {
        if (monitorScreenRenderer != null)
        {
            if (materialIndex < monitorScreenRenderer.materials.Length)
            {
                screenMaterial = monitorScreenRenderer.materials[materialIndex];
            }
            else
            {
                screenMaterial = monitorScreenRenderer.material;
            }
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        typingAudioSource = gameObject.AddComponent<AudioSource>();
        typingAudioSource.playOnAwake = false;

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
            playerMoveScript = mainCameraTransform.GetComponentInParent<MovePl>();
            playerController = mainCameraTransform.GetComponentInParent<CharacterController>();
        }
    }

    public void PowerOnPC()
    {
        isPoweredOn = true;
        ApplyColorToScreen(Color.white);

        if (windowsBootTexture != null)
        {
            ApplyTextureToScreen(windowsBootTexture, windowsBootTexture);
        }
        else if (imageSteps != null && imageSteps.Length > 0 && imageSteps[0].screenTexture != null)
        {
            ApplyTextureToScreen(imageSteps[0].screenTexture, imageSteps[0].screenTexture);
        }
    }

    public void PowerOffPC()
    {
        isPoweredOn = false;
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        TurnScreenOffImmediate();
    }

    void TurnScreenOffImmediate()
    {
        Texture blackTex = pitchBlackTexture != null ? pitchBlackTexture : Texture2D.blackTexture;
        ApplyTextureToScreen(blackTex, blackTex);
        ApplyColorToScreen(Color.black);
    }

    public void Interact()
    {
        if (!isPoweredOn)
        {
            Debug.Log("[InWorldComputerCutscene] ⚠️ Bạn chưa bấm nút nguồn trên Case PC để bật máy!");
            return;
        }

        if (isCutsceneRunning || isReturningCamera) return;
        StartComputerSequence();
    }

    void StartComputerSequence()
    {
        isCutsceneRunning = true;
        isUsingComputer = true;
        isReturningCamera = false;
        currentStepIndex = 0;
        currentLineIndex = 0;

        ApplyColorToScreen(Color.white);

        cutsceneStartTime = Time.unscaledTime;

        if (playerMoveScript != null) playerMoveScript.enabled = false;
        if (playerController != null) playerController.enabled = false;

        if (mainCameraTransform != null)
        {
            originalCamPos = mainCameraTransform.position;
            originalCamRot = mainCameraTransform.rotation;
        }

        ToggleOtherUIs(false);
        ShowCurrentStep();
    }

    void Update()
    {
        if (isCutsceneRunning && mainCameraTransform != null)
        {
            Vector3 targetPos;
            Quaternion targetRot;

            if (cameraFocusPoint != null)
            {
                targetPos = cameraFocusPoint.position;
                targetRot = cameraFocusPoint.rotation;
            }
            else if (monitorScreenRenderer != null)
            {
                targetPos = originalCamPos;
                targetRot = Quaternion.LookRotation(monitorScreenRenderer.transform.position - originalCamPos);
            }
            else
            {
                targetPos = originalCamPos;
                targetRot = originalCamRot;
            }

            mainCameraTransform.position = Vector3.Lerp(mainCameraTransform.position, targetPos, Time.deltaTime * cameraSmoothSpeed);
            mainCameraTransform.rotation = Quaternion.Slerp(mainCameraTransform.rotation, targetRot, Time.deltaTime * cameraSmoothSpeed);

            // BẤM CHUỘT TRÁI ĐỂ CHẠY HẾT CHỮ HOẶC SANG THOẠI MỚI (Lọc click đầu)
            if (Input.GetMouseButtonDown(0) && (Time.unscaledTime - cutsceneStartTime >= 0.2f))
            {
                AdvanceDialogue();
            }

            // BẤM PHÍM [F] ĐỂ THOÁT CẮT CẢNH MÁY TÍNH
            if (Input.GetKeyDown(KeyCode.F))
            {
                EndSequence();
            }
        }
    }

    void ShowCurrentStep()
    {
        if (imageSteps == null || imageSteps.Length == 0)
        {
            EndSequence();
            return;
        }

        if (currentStepIndex >= imageSteps.Length)
        {
            EndSequence();
            return;
        }

        ComputerImageStep currentImageStep = imageSteps[currentStepIndex];

        Texture mainTex = currentImageStep.screenTexture;
        Texture emTex = (currentImageStep.emissiveTexture != null) ? currentImageStep.emissiveTexture : mainTex;

        if (mainTex != null)
        {
            ApplyTextureToScreen(mainTex, emTex);
        }

        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        ComputerImageStep currentImageStep = imageSteps[currentStepIndex];

        if (currentImageStep.dialogueLines == null || currentImageStep.dialogueLines.Length == 0)
        {
            NextImageStep();
            return;
        }

        if (currentLineIndex >= currentImageStep.dialogueLines.Length)
        {
            NextImageStep();
            return;
        }

        DialogueLine line = currentImageStep.dialogueLines[currentLineIndex];
        currentFullDialogue = (SettingsManager.currentLanguage == "VI") ? line.vietnameseDialogue : line.englishDialogue;

        if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
        if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);

        dialogueCoroutine = StartCoroutine(DisplayDialogueRoutine(currentFullDialogue, generalDialogueSound));
    }

    IEnumerator DisplayDialogueRoutine(string targetDialogue, AudioClip dialogueAudio)
    {
        if (subtitleText == null)
        {
            GameObject subObj = GameObject.Find("SubtitleText");
            if (subObj == null) subObj = GameObject.Find("Subtitle");
            if (subObj != null) subtitleText = subObj.GetComponent<TextMeshProUGUI>();
        }

        if (subtitleText != null)
        {
            if (subtitleText.transform.parent != null)
                subtitleText.transform.parent.gameObject.SetActive(true);
            subtitleText.gameObject.SetActive(true);

            Color c = subtitleText.color;
            c.a = 1f;
            subtitleText.color = c;
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = (SettingsManager.currentLanguage == "VI") ? "Click chuột trái để tiếp tục..." : "Left Click to continue...";
        }

        // BƯỚC 2: HIỆU ỨNG GÕ CHỮ TỪNG KÝ TỰ (TYPEWRITER EFFECT)
        isTyping = true;

        if (dialogueAudio != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.clip = dialogueAudio;
            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        if (useTypewriterEffect && subtitleText != null)
        {
            subtitleText.text = "";
            for (int i = 0; i <= targetDialogue.Length; i++)
            {
                if (!isTyping) break;

                string typedText = targetDialogue.Substring(0, i);
                if (showBlinkingCursor) typedText += "_";
                subtitleText.text = typedText;

                if (typingSound != null && typingAudioSource != null && i % 2 == 0 && i < targetDialogue.Length)
                {
                    typingAudioSource.PlayOneShot(typingSound, 0.4f);
                }

                yield return new WaitForSeconds(typingSpeed);
            }
        }
        else if (subtitleText != null)
        {
            subtitleText.text = targetDialogue;
        }

        isTyping = false;
        if (subtitleText != null) subtitleText.text = targetDialogue;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // BƯỚC 3: CON TRỎ NHẤP NHÁY '_' KHI GÕ XONG DÒNG THOẠI
        if (showBlinkingCursor)
        {
            cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(targetDialogue));
        }
    }

    IEnumerator BlinkCursorRoutine(string baseText)
    {
        bool showUnderscore = true;
        while (true)
        {
            subtitleText.text = baseText + (showUnderscore ? "_" : " ");
            showUnderscore = !showUnderscore;
            yield return new WaitForSeconds(0.4f);
        }
    }

    void AdvanceDialogue()
    {
        // 1. NẾU ĐANG GÕ DỞ CHỮ -> NICK VÀO CHẠY RA HẾT TOÀN BỘ CHỮ NAY LẬP TỨC VÀ NGẮT ÂM THANH
        if (isTyping)
        {
            isTyping = false;
            if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

            subtitleText.text = currentFullDialogue;
            Color c = subtitleText.color;
            c.a = 1f;
            subtitleText.color = c;

            if (showBlinkingCursor)
            {
                if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
                cursorBlinkCoroutine = StartCoroutine(BlinkCursorRoutine(currentFullDialogue));
            }
            return;
        }

        // 2. NẾU ĐÃ GÕ XONG HOÀN TOÀN -> CHẠY FADE OUT MỜ DẦN BIẾN MẤT VÀ SANG CÂU MỚI / ẢNH MỚI
        StartCoroutine(TransitionToNextLineRoutine());
    }

    IEnumerator TransitionToNextLineRoutine()
    {
        if (cursorBlinkCoroutine != null) StopCoroutine(cursorBlinkCoroutine);
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        // BƯỚC 4: FADE OUT MỜ DẦN BIẾN MẤT KHI CHUYỂN THOẠI
        if (useFadeEffect && subtitleText != null)
        {
            float elapsed = 0f;
            Color c = subtitleText.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                subtitleText.color = c;
                yield return null;
            }
            c.a = 0f;
            subtitleText.color = c;
        }

        ComputerImageStep currentImageStep = imageSteps[currentStepIndex];

        if (currentImageStep.dialogueLines != null && currentLineIndex < currentImageStep.dialogueLines.Length - 1)
        {
            currentLineIndex++;
            ShowCurrentLine();
        }
        else
        {
            NextImageStep();
        }
    }

    void NextImageStep()
    {
        currentStepIndex++;
        currentLineIndex = 0;

        if (currentStepIndex < imageSteps.Length)
        {
            if (mouseClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(mouseClickSound);
            }

            ShowCurrentStep();
        }
        else
        {
            EndSequence();
        }
    }

    void ToggleOtherUIs(bool show)
    {
        if (uisToHide != null && uisToHide.Length > 0)
        {
            foreach (GameObject uiObj in uisToHide)
            {
                if (uiObj != null)
                {
                    uiObj.SetActive(show);
                }
            }
        }
    }

    void ApplyColorToScreen(Color color)
    {
        if (screenMaterial == null) return;

        string[] colorProps = new string[] { "_BaseColor", "_Color", "_EmissiveColor", "_EmissionColor", "Base Color", "Emissive Color" };
        foreach (string prop in colorProps)
        {
            if (screenMaterial.HasProperty(prop))
            {
                try { screenMaterial.SetColor(prop, color); } catch {}
            }
        }
    }

    void ApplyTextureToScreen(Texture tex, Texture emissiveTex = null)
    {
        if (screenMaterial == null) return;

        Texture emissiveToUse = (emissiveTex != null) ? emissiveTex : tex;

        if (!string.IsNullOrEmpty(customBaseTextureProperty))
        {
            try { screenMaterial.SetTexture(customBaseTextureProperty, tex); } catch {}
        }

        if (!string.IsNullOrEmpty(customEmissiveProperty))
        {
            try { screenMaterial.SetTexture(customEmissiveProperty, emissiveToUse); } catch {}
        }

        string[] baseProps = new string[] {
            "_BaseMap", "_MainTex", "_BaseColorTex", "_BaseColorMap",
            "_BaseColor", "_Texture", "Base Color Tex", "Base Map", "_Main_Texture"
        };

        string[] emissiveProps = new string[] {
            "_EmissiveTex", "_EmissionMap", "_EmissiveColorTex", "_EmissiveMap",
            "_EmissionTex", "Emissive Tex", "Emissive Map", "_Emission_Texture"
        };

        foreach (string prop in baseProps)
        {
            if (screenMaterial.HasProperty(prop))
            {
                try { screenMaterial.SetTexture(prop, tex); } catch {}
            }
        }

        foreach (string prop in emissiveProps)
        {
            if (screenMaterial.HasProperty(prop))
            {
                try { screenMaterial.SetTexture(prop, emissiveToUse); } catch {}
            }
        }

        Shader shader = screenMaterial.shader;
        if (shader != null)
        {
            int propCount = shader.GetPropertyCount();
            for (int i = 0; i < propCount; i++)
            {
                if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    string pName = shader.GetPropertyName(i);

                    if (pName.StartsWith("unity_") || pName.StartsWith("Unity")) continue;

                    if (shader.GetPropertyTextureDimension(i) == UnityEngine.Rendering.TextureDimension.Tex2D)
                    {
                        try { screenMaterial.SetTexture(pName, emissiveToUse); } catch {}
                    }
                }
            }
        }

        try { screenMaterial.mainTexture = tex; } catch {}
    }

    void EndSequence()
    {
        isCutsceneRunning = false;
        isUsingComputer = false;
        hasCompletedComputer = true;

        CameraObjectPickup.isComputerCutsceneFinished = true;

        if (subtitleText != null) subtitleText.gameObject.SetActive(false);
        if (promptText != null) promptText.gameObject.SetActive(false);

        ToggleOtherUIs(true);
        StartCoroutine(ReturnCameraRoutine());
    }

    IEnumerator ReturnCameraRoutine()
    {
        isReturningCamera = true;
        float elapsed = 0f;
        float duration = 1.0f / cameraSmoothSpeed;

        Vector3 startPos = mainCameraTransform.position;
        Quaternion startRot = mainCameraTransform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            mainCameraTransform.position = Vector3.Lerp(startPos, originalCamPos, t);
            mainCameraTransform.rotation = Quaternion.Slerp(startRot, originalCamRot, t);

            yield return null;
        }

        mainCameraTransform.position = originalCamPos;
        mainCameraTransform.rotation = originalCamRot;

        if (playerMoveScript != null) playerMoveScript.enabled = true;
        if (playerController != null) playerController.enabled = true;

        isReturningCamera = false;
    }
}
