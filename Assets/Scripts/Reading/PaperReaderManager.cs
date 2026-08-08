using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PaperReaderManager : MonoBehaviour
{
    [Header("1. Khung Giao Diện Đọc Giấy")]
    public Transform paperAnchor;
    public GameObject readingCanvas;
    public TextMeshProUGUI paperTextUI;

    [Header("2. Các UI Khác Cần Ẩn")]
    public GameObject[] uisToHideOnRead;

    [Header("3. Khóa Người Chơi")]
    public MonoBehaviour[] playerScriptsToFreeze;

    [Header("4. Cấu Hình Zoom (Nếu muốn)")]
    public bool enableZoom = true;
    public float zoomSpeed = 2f;
    public float minZoomOffset = -0.15f;
    public float maxZoomOffset = 0.35f;
    public float smoothMoveSpeed = 15f;

    [Header("5. Hiệu Ứng Chữ Đọc Giấy")]
    public bool useTypewriterEffect = true;
    public float typewriterSpeed = 0.015f;
    public bool useFadeIn = true;
    public float fadeInDuration = 0.3f;
    public AudioClip paperRustleSound;

    [Header("6. Offset Vị Trí & Góc Xoay Cố Định Tờ Giấy")]
    public Vector3 defaultPaperRotationOffset = new Vector3(0f, 0f, 90f);
    public Vector3 defaultPaperPositionOffset = new Vector3(-0.25f, 0f, 0f);

    [Header("7. Tùy Chọn Texture / Material Đọc Giấy (Dành Cho Bạn)")]
    [Tooltip("Kéo bức ảnh Texture 2D của tờ giấy vào đây nếu muốn ném ảnh riêng!")]
    public Texture2D customPaperTexture;

    [Tooltip("Kéo Material Unlit tùy chỉnh vào đây nếu muốn tự thiết lập giao diện!")]
    public Material customUnlitMaterial;

    // --- BIẾN TRẠNG THÁI ---
    private GameObject currentPaper;
    private GameObject originalTableObject;
    private bool isUsingSpawnedPrefab = false;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    public bool isReading { get; private set; }
    private float pickupTime = 0f;

    private List<GameObject> previouslyActiveUI = new List<GameObject>();
    private AudioSource audioSource;
    private Coroutine textCoroutine;
    private Coroutine slideCoroutine;
    private bool isTypingText = false;
    private string currentFullDialogue = "";

    private bool wasCamcorderActiveOnRead = false;

    private float currentZoomZ = 0f;
    private Vector3 activePosOffset;
    private Vector3 activeRotOffset;

    // --- BIẾN MULTI-PAGE & CẤU TRÚC STEP MỚI CHÚA CẢ XẤP 4 TỜ GIẤY ---
    private ReadablePaper.PaperPageStep[] activePaperSteps;
    private GameObject mainPaperPrefab;
    private int currentStepIndex = 0;
    private int currentLineIndex = 0;

    // Biến tương thích cũ
    private string[] legacyPages;
    private GameObject[] legacyPagePrefabs;

    private Dictionary<Renderer, Material[]> originalPaperMaterials = new Dictionary<Renderer, Material[]>();

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (!isReading) return;

        // 1. PHÍM ESC ĐỂ THOÁT ĐỌC GIẤY BẤM BẤT CỨ LÚC NÀO
        if (Time.time > pickupTime + 0.25f && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            StopReading();
            return;
        }

        // 2. GIỮ XẤP GIẤY CỐ ĐỊNH HOÀN TOÀN TRƯỚC MẮT CAMERA
        if (currentPaper != null && paperAnchor != null)
        {
            if (enableZoom)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    currentZoomZ = Mathf.Clamp(currentZoomZ + scroll * zoomSpeed, minZoomOffset, maxZoomOffset);
                }
            }

            Vector3 targetOffset = activePosOffset + new Vector3(0f, 0f, currentZoomZ);
            Vector3 targetPos = paperAnchor.position + paperAnchor.TransformDirection(targetOffset);
            currentPaper.transform.position = Vector3.Lerp(currentPaper.transform.position, targetPos, Time.deltaTime * smoothMoveSpeed);
            currentPaper.transform.rotation = paperAnchor.rotation * Quaternion.Euler(activeRotOffset);
        }

        // 3. BẤM CHUỘT TRÁI ĐỂ CHẠY HẾT CHỮ HOẶC TRÁO TỜ GIẤY BAY VỀ CUỐI XẤP
        if (Input.GetMouseButtonDown(0) && Time.time > pickupTime + 0.15f)
        {
            AdvanceDialogue();
        }
    }

    // ==========================================
    // BẮT ĐẦU ĐỌC GIẤY CHÚA CẢ XẤP 4 TỜ (CHÍNH)
    // ==========================================
    public void StartReadingSteps(GameObject tableObj, GameObject mainPrefab, ReadablePaper.PaperPageStep[] steps, Vector3? customPosOffset = null, Vector3? customRotOffset = null)
    {
        isReading = true;
        pickupTime = Time.time;
        currentZoomZ = 0f;
        currentStepIndex = 0;
        currentLineIndex = 0;

        mainPaperPrefab = mainPrefab;
        activePaperSteps = steps;
        legacyPages = null;
        legacyPagePrefabs = null;

        activePosOffset = customPosOffset ?? defaultPaperPositionOffset;
        activeRotOffset = customRotOffset ?? defaultPaperRotationOffset;

        originalTableObject = tableObj;
        if (originalTableObject != null)
        {
            originalPosition = originalTableObject.transform.position;
            originalRotation = originalTableObject.transform.rotation;
        }

        if (readingCanvas != null) readingCanvas.SetActive(true);

        if (CamcorderUI.Instance != null && CamcorderUI.Instance.gameObject.activeSelf)
        {
            wasCamcorderActiveOnRead = true;
            CamcorderUI.Instance.gameObject.SetActive(false);
        }
        else { wasCamcorderActiveOnRead = false; }

        if (paperRustleSound != null && audioSource != null) audioSource.PlayOneShot(paperRustleSound, 0.8f);

        previouslyActiveUI.Clear();
        if (uisToHideOnRead != null)
        {
            foreach (GameObject ui in uisToHideOnRead)
            {
                if (ui != null && ui.activeSelf) { previouslyActiveUI.Add(ui); ui.SetActive(false); }
            }
        }

        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;

        if (playerScriptsToFreeze != null)
        {
            foreach (var script in playerScriptsToFreeze) { if (script != null) script.enabled = false; }
        }

        // Sinh Model xấp giấy tổng ban đầu (Nâng cả xấp 4 tờ lên)
        SpawnMainStackModel();

        // TỰ ĐỘNG ĐƯA TRANG 1 NẰM TRÊN CÙNG KHI MỚI MỞ ĐỌC GIẤY
        if (currentPaper != null && activePaperSteps != null && activePaperSteps.Length > 0)
        {
            string firstNode = activePaperSteps[0].subMeshNodeName;
            if (!string.IsNullOrEmpty(firstNode))
            {
                Transform foundFirst = currentPaper.transform.Find(firstNode);
                if (foundFirst != null)
                {
                    foundFirst.SetAsFirstSibling();
                }
            }
        }

        ShowCurrentLine();
    }

    public void StartReadingSteps(GameObject tableObj, ReadablePaper.PaperPageStep[] steps, Vector3? customPosOffset = null, Vector3? customRotOffset = null)
    {
        StartReadingSteps(tableObj, null, steps, customPosOffset, customRotOffset);
    }

    void SpawnMainStackModel()
    {
        GameObject prefabToSpawn = mainPaperPrefab;
        if (prefabToSpawn == null && activePaperSteps != null && activePaperSteps.Length > 0)
        {
            prefabToSpawn = activePaperSteps[0].paperPrefab;
        }

        GameObject oldPaper = currentPaper;
        if (oldPaper != null && oldPaper != originalTableObject)
        {
            RestoreOriginalShaderOnPaper();
            Destroy(oldPaper);
        }

        if (prefabToSpawn != null)
        {
            isUsingSpawnedPrefab = true;
            if (originalTableObject != null) originalTableObject.SetActive(false);
            currentPaper = Instantiate(prefabToSpawn);
            currentPaper.transform.localPosition = Vector3.zero;
        }
        else
        {
            isUsingSpawnedPrefab = false;
            currentPaper = originalTableObject;
            if (currentPaper != null) currentPaper.SetActive(true);
        }

        if (currentPaper != null)
        {
            Rigidbody rb = currentPaper.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Collider col = currentPaper.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Texture2D texToUse = (activePaperSteps != null && activePaperSteps.Length > 0) ? activePaperSteps[0].paperTexture : null;
            ApplyUnlitShaderToPaper(currentPaper, texToUse);
        }
    }

    void ShowCurrentLine()
    {
        if (activePaperSteps == null || activePaperSteps.Length == 0)
        {
            ShowLegacyLine();
            return;
        }

        if (currentStepIndex >= activePaperSteps.Length)
        {
            StopReading();
            return;
        }

        ReadablePaper.PaperPageStep step = activePaperSteps[currentStepIndex];

        if (step.dialogueLines == null || step.dialogueLines.Length == 0)
        {
            NextStep();
            return;
        }

        if (currentLineIndex >= step.dialogueLines.Length)
        {
            NextStep();
            return;
        }

        ReadablePaper.DialogueLine line = step.dialogueLines[currentLineIndex];
        currentFullDialogue = (SettingsManager.currentLanguage == "VI") ? line.vietnameseDialogue : line.englishDialogue;

        if (line.dialogueAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(line.dialogueAudio);
        }

        if (textCoroutine != null) StopCoroutine(textCoroutine);
        textCoroutine = StartCoroutine(DisplayTextRoutine(currentFullDialogue));
    }

    void NextStep()
    {
        // THỰC HIỆN HIỆU ỨNG CHẬM RÃI: TỜ VỪA ĐỌC BAY SANG TRÁI, TỜ ĐẰNG SAU ĐẨY NHẸ TIẾN LÊN
        AnimateCurrentSheetLeft();

        currentStepIndex++;
        currentLineIndex = 0;

        if (activePaperSteps != null && currentStepIndex < activePaperSteps.Length)
        {
            if (paperRustleSound != null && audioSource != null) audioSource.PlayOneShot(paperRustleSound, 0.7f);
            ShowCurrentLine();
        }
        else if (legacyPages != null && currentStepIndex < legacyPages.Length)
        {
            if (paperRustleSound != null && audioSource != null) audioSource.PlayOneShot(paperRustleSound, 0.7f);
            ShowLegacyStep();
        }
        else
        {
            StopReading();
        }
    }

    // ==========================================
    // HIỆU ỨNG MỚI: CHẬM RÃI TRÁO TỜ GIẤY BAY SANG TRÁI & ĐẨY TỜ SAU TIẾN LÊN
    // ==========================================
    void AnimateCurrentSheetLeft()
    {
        if (currentPaper == null) return;

        Transform topSheet = null;
        Transform nextSheet = null;

        // 1. Tìm theo tên node mesh do bạn cài trong Inspector (nếu có)
        if (activePaperSteps != null && currentStepIndex < activePaperSteps.Length)
        {
            string nodeName = activePaperSteps[currentStepIndex].subMeshNodeName;
            if (!string.IsNullOrEmpty(nodeName))
            {
                Transform foundNode = currentPaper.transform.Find(nodeName);
                if (foundNode != null) topSheet = foundNode;
            }

            if (currentStepIndex + 1 < activePaperSteps.Length)
            {
                string nextName = activePaperSteps[currentStepIndex + 1].subMeshNodeName;
                if (!string.IsNullOrEmpty(nextName))
                {
                    Transform foundNext = currentPaper.transform.Find(nextName);
                    if (foundNext != null) nextSheet = foundNext;
                }
            }
        }

        // 2. Nếu không cài tên, tự động lấy theo thứ tự con
        if (topSheet == null && currentPaper.transform.childCount > 0)
        {
            int childIndex = Mathf.Min(currentStepIndex, currentPaper.transform.childCount - 1);
            topSheet = currentPaper.transform.GetChild(childIndex);
            if (childIndex + 1 < currentPaper.transform.childCount)
            {
                nextSheet = currentPaper.transform.GetChild(childIndex + 1);
            }
        }

        if (topSheet != null)
        {
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideSheetLeftRoutine(topSheet, nextSheet));
        }
    }

    IEnumerator SlideSheetLeftRoutine(Transform sheetTransform, Transform nextSheetTransform)
    {
        if (sheetTransform == null) yield break;

        Vector3 startLocalPos = sheetTransform.localPosition;
        Quaternion startLocalRot = sheetTransform.localRotation;

        // Bay trượt chậm sang bên trái và xoay nghiêng nhẹ cực đẹp
        Vector3 targetLeftPos = startLocalPos + new Vector3(-0.7f, 0.08f, -0.05f);
        Quaternion targetLeftRot = startLocalRot * Quaternion.Euler(0f, 0f, 15f);

        Vector3 nextStartPos = (nextSheetTransform != null) ? nextSheetTransform.localPosition : Vector3.zero;
        Vector3 nextTargetPos = nextStartPos + new Vector3(0f, 0f, -0.03f); // Đẩy nhẹ tờ phía sau tiến về trước

        float duration = 0.45f; // Hiệu ứng mượt mà kéo dài 0.45 giây
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (sheetTransform == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 1. Tờ vừa đọc xong trượt mượt sang bên trái
            sheetTransform.localPosition = Vector3.Lerp(startLocalPos, targetLeftPos, smoothT);
            sheetTransform.localRotation = Quaternion.Slerp(startLocalRot, targetLeftRot, smoothT);

            // 2. Tờ đằng sau nhích nhẹ tiến lên phía trước
            if (nextSheetTransform != null)
            {
                nextSheetTransform.localPosition = Vector3.Lerp(nextStartPos, nextTargetPos, smoothT);
            }

            yield return null;
        }

        // Tắt tờ đã đọc xong sang bên trái để lộ rõ 100% tờ tiếp theo
        sheetTransform.gameObject.SetActive(false);
    }

    void AdvanceDialogue()
    {
        if (isTypingText)
        {
            CompleteTextInstantly();
            return;
        }

        if (activePaperSteps != null && activePaperSteps.Length > 0)
        {
            ReadablePaper.PaperPageStep step = activePaperSteps[currentStepIndex];
            if (step.dialogueLines != null && currentLineIndex < step.dialogueLines.Length - 1)
            {
                currentLineIndex++;
                ShowCurrentLine();
            }
            else
            {
                NextStep();
            }
        }
        else if (legacyPages != null && legacyPages.Length > 0)
        {
            if (currentStepIndex < legacyPages.Length - 1)
            {
                currentStepIndex++;
                ShowLegacyStep();
            }
            else
            {
                StopReading();
            }
        }
        else
        {
            StopReading();
        }
    }

    // ==========================================
    // TƯƠNG THÍCH LẠI CODE CŨ
    // ==========================================
    public void StartReading(GameObject tableObj, string content, Vector3? pos = null, Vector3? rot = null)
    { StartReading(tableObj, (GameObject[])null, new string[] { content }, pos, rot); }

    public void StartReading(GameObject tableObj, string[] pages, Vector3? pos = null, Vector3? rot = null)
    { StartReading(tableObj, (GameObject[])null, pages, pos, rot); }

    public void StartReading(GameObject tableObj, GameObject singlePrefab, string content, Vector3? pos = null, Vector3? rot = null)
    { StartReading(tableObj, singlePrefab != null ? new GameObject[] { singlePrefab } : null, new string[] { content }, pos, rot); }

    public void StartReading(GameObject tableObj, GameObject singlePrefab, string[] pages, Vector3? pos = null, Vector3? rot = null)
    { StartReading(tableObj, singlePrefab != null ? new GameObject[] { singlePrefab } : null, pages, pos, rot); }

    public void StartReading(GameObject tableObj, GameObject[] displayPrefabs, string[] pages, Vector3? customPosOffset = null, Vector3? customRotOffset = null)
    {
        isReading = true;
        pickupTime = Time.time;
        currentZoomZ = 0f;
        currentStepIndex = 0;
        currentLineIndex = 0;

        activePaperSteps = null;
        legacyPages = (pages != null && pages.Length > 0) ? pages : new string[] { "" };
        legacyPagePrefabs = displayPrefabs;

        activePosOffset = customPosOffset ?? defaultPaperPositionOffset;
        activeRotOffset = customRotOffset ?? defaultPaperRotationOffset;

        originalTableObject = tableObj;
        if (originalTableObject != null)
        {
            originalPosition = originalTableObject.transform.position;
            originalRotation = originalTableObject.transform.rotation;
        }

        if (readingCanvas != null) readingCanvas.SetActive(true);

        if (CamcorderUI.Instance != null && CamcorderUI.Instance.gameObject.activeSelf)
        {
            wasCamcorderActiveOnRead = true;
            CamcorderUI.Instance.gameObject.SetActive(false);
        }
        else { wasCamcorderActiveOnRead = false; }

        if (paperRustleSound != null && audioSource != null) audioSource.PlayOneShot(paperRustleSound, 0.8f);

        previouslyActiveUI.Clear();
        if (uisToHideOnRead != null)
        {
            foreach (GameObject ui in uisToHideOnRead)
            {
                if (ui != null && ui.activeSelf) { previouslyActiveUI.Add(ui); ui.SetActive(false); }
            }
        }

        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;

        if (playerScriptsToFreeze != null)
        {
            foreach (var script in playerScriptsToFreeze) { if (script != null) script.enabled = false; }
        }

        ShowLegacyStep();
    }

    void ShowLegacyStep()
    {
        if (legacyPages == null || legacyPages.Length == 0) return;

        GameObject prefabToSpawn = null;
        if (legacyPagePrefabs != null && legacyPagePrefabs.Length > 0)
        {
            int pIndex = Mathf.Min(currentStepIndex, legacyPagePrefabs.Length - 1);
            prefabToSpawn = legacyPagePrefabs[pIndex];
        }

        SpawnModelForStep(prefabToSpawn, null);
        ShowLegacyLine();
    }

    void ShowLegacyLine()
    {
        if (legacyPages == null || currentStepIndex >= legacyPages.Length) return;
        currentFullDialogue = legacyPages[currentStepIndex];

        if (textCoroutine != null) StopCoroutine(textCoroutine);
        textCoroutine = StartCoroutine(DisplayTextRoutine(currentFullDialogue));
    }

    void SpawnModelForStep(GameObject prefabToSpawn, Texture2D stepTex)
    {
        GameObject oldPaper = currentPaper;
        if (oldPaper != null && oldPaper != originalTableObject)
        {
            RestoreOriginalShaderOnPaper();
            Destroy(oldPaper);
        }

        if (prefabToSpawn != null)
        {
            isUsingSpawnedPrefab = true;
            if (originalTableObject != null) originalTableObject.SetActive(false);
            currentPaper = Instantiate(prefabToSpawn);
            currentPaper.transform.localPosition = Vector3.zero;
        }
        else
        {
            isUsingSpawnedPrefab = false;
            currentPaper = originalTableObject;
            if (currentPaper != null) currentPaper.SetActive(true);
        }

        if (currentPaper != null)
        {
            Rigidbody rb = currentPaper.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Collider col = currentPaper.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            ApplyUnlitShaderToPaper(currentPaper, stepTex);
        }
    }

    private void ApplyUnlitShaderToPaper(GameObject paperObj, Texture2D stepTex = null)
    {
        if (paperObj == null) return;

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Texture");
        if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

        Renderer[] renderers = paperObj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            Material[] origMats = r.sharedMaterials;
            if (!originalPaperMaterials.ContainsKey(r))
            {
                originalPaperMaterials[r] = origMats;
            }

            Material[] unlitMats = new Material[origMats.Length];
            for (int i = 0; i < origMats.Length; i++)
            {
                if (origMats[i] != null)
                {
                    if (customUnlitMaterial != null)
                    {
                        unlitMats[i] = new Material(customUnlitMaterial);
                    }
                    else
                    {
                        Texture tex = stepTex != null ? stepTex : customPaperTexture;
                        if (tex == null) tex = origMats[i].mainTexture;
                        if (tex == null && origMats[i].HasProperty("_BaseMap")) tex = origMats[i].GetTexture("_BaseMap");
                        if (tex == null && origMats[i].HasProperty("_MainTex")) tex = origMats[i].GetTexture("_MainTex");

                        unlitMats[i] = new Material(unlitShader != null ? unlitShader : origMats[i].shader);
                        if (tex != null)
                        {
                            unlitMats[i].mainTexture = tex;
                            if (unlitMats[i].HasProperty("_BaseMap")) unlitMats[i].SetTexture("_BaseMap", tex);
                            if (unlitMats[i].HasProperty("_MainTex")) unlitMats[i].SetTexture("_MainTex", tex);
                        }
                    }
                }
            }
            r.materials = unlitMats;
        }
    }

    private void RestoreOriginalShaderOnPaper()
    {
        foreach (var kvp in originalPaperMaterials)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Key.materials = kvp.Value;
            }
        }
        originalPaperMaterials.Clear();
    }

    // ==========================================
    // HIỂN THỊ CHỮ
    // ==========================================
    IEnumerator DisplayTextRoutine(string targetText)
    {
        if (paperTextUI == null) yield break;

        if (useFadeIn)
        {
            float elapsed = 0f;
            Color c = paperTextUI.color;
            c.a = 0f;
            paperTextUI.color = c;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                paperTextUI.color = c;
                yield return null;
            }
            c.a = 1f;
            paperTextUI.color = c;
        }

        if (useTypewriterEffect)
        {
            isTypingText = true;
            paperTextUI.text = "";

            for (int i = 0; i <= targetText.Length; i++)
            {
                if (!isTypingText) break;
                paperTextUI.text = targetText.Substring(0, i);
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }
        CompleteTextInstantly();
    }

    void CompleteTextInstantly()
    {
        isTypingText = false;
        if (paperTextUI != null)
        {
            paperTextUI.text = currentFullDialogue;
            Color c = paperTextUI.color; c.a = 1f; paperTextUI.color = c;
        }
    }

    // ==========================================
    // HÀM KẾT THÚC ĐỌC
    // ==========================================
    public void StopReading()
    {
        if (!isReading) return;

        if (textCoroutine != null) StopCoroutine(textCoroutine);
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        isTypingText = false;

        RestoreOriginalShaderOnPaper();

        if (isUsingSpawnedPrefab && currentPaper != null && currentPaper != originalTableObject)
        {
            Destroy(currentPaper);
        }

        if (originalTableObject != null)
        {
            originalTableObject.SetActive(true);

            Rigidbody rb = originalTableObject.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            Collider col = originalTableObject.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            originalTableObject.transform.position = originalPosition;
            originalTableObject.transform.rotation = originalRotation;
        }

        if (readingCanvas != null) readingCanvas.SetActive(false);
        isReading = false;
        currentPaper = null;

        if (wasCamcorderActiveOnRead && CamcorderUI.Instance != null)
        {
            CamcorderUI.Instance.gameObject.SetActive(true);
            wasCamcorderActiveOnRead = false;
        }

        if (previouslyActiveUI != null)
        {
            foreach (GameObject ui in previouslyActiveUI)
            {
                if (ui != null) ui.SetActive(true);
            }
            previouslyActiveUI.Clear();
        }

        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;

        if (playerScriptsToFreeze != null)
        {
            foreach (var script in playerScriptsToFreeze) { if (script != null) script.enabled = true; }
        }
    }
}