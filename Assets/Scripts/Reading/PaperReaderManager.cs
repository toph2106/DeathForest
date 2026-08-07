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

    [Header("4. Tốc Độ Xoay & Zoom")]
    public float rotationSpeed = 5f;
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

    [Header("6. Offset Mặc Định")]
    public Vector3 defaultPaperRotationOffset = new Vector3(0f, 0f, 90f);
    public Vector3 defaultPaperPositionOffset = new Vector3(-0.25f, 0f, 0f);

    [Header("7. Lật Trang (Multi-Page & Multi-Model)")]
    public GameObject nextPageButton;
    public GameObject prevPageButton;
    public float pageTurnSlideDistance = 1.2f;
    public float pageTurnDuration = 0.18f;

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
    private bool isTypingText = false;

    private Vector3 dragStartPos;
    private bool wasDragging = false;
    private bool wasCamcorderActiveOnRead = false;

    private float currentZoomZ = 0f;
    private Vector3 activePosOffset;
    private Vector3 activeRotOffset;

    // --- BIẾN MULTI-PAGE ---
    private string[] currentPages;
    private GameObject[] currentPagePrefabs; // Mảng lưu các model cho từng trang
    private int currentPageIndex = 0;
    private bool isTurningPage = false;

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

        if (isTypingText && Input.GetMouseButtonDown(0) && Time.time > pickupTime + 0.1f)
            CompleteTextInstantly();

        if (Time.time > pickupTime + 0.25f && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1)))
        {
            StopReading();
            return;
        }

        if (!isTurningPage)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) NextPage();
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) PrevPage();
        }

        if (Input.GetKeyDown(KeyCode.R) && currentPaper != null && paperAnchor != null && !isTurningPage)
        {
            currentZoomZ = 0f;
            currentPaper.transform.rotation = paperAnchor.rotation * Quaternion.Euler(activeRotOffset);
        }

        if (enableZoom && !isTurningPage)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentZoomZ = Mathf.Clamp(currentZoomZ + scroll * zoomSpeed, minZoomOffset, maxZoomOffset);
            }
        }

        if (currentPaper != null && paperAnchor != null && !isTurningPage)
        {
            Vector3 targetOffset = activePosOffset + new Vector3(0f, 0f, currentZoomZ);
            Vector3 targetPos = paperAnchor.position + paperAnchor.TransformDirection(targetOffset);
            currentPaper.transform.position = Vector3.Lerp(currentPaper.transform.position, targetPos, Time.deltaTime * smoothMoveSpeed);
        }

        if (Input.GetMouseButtonDown(0))
        {
            dragStartPos = Input.mousePosition;
            wasDragging = false;
        }

        if (Input.GetMouseButton(0) && !isTurningPage && currentPaper != null)
        {
            if (Vector3.Distance(Input.mousePosition, dragStartPos) > 8f) wasDragging = true;

            float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotY = Input.GetAxis("Mouse Y") * rotationSpeed;

            if (Mathf.Abs(rotX) > 0.01f || Mathf.Abs(rotY) > 0.01f)
            {
                currentPaper.transform.Rotate(Camera.main.transform.up, -rotX, Space.World);
                currentPaper.transform.Rotate(Camera.main.transform.right, rotY, Space.World);
            }
        }

        if (Input.GetMouseButtonUp(0)) StartCoroutine(ResetDragStateRoutine());
    }

    IEnumerator ResetDragStateRoutine()
    {
        yield return new WaitForEndOfFrame();
        wasDragging = false;
    }

    // ==========================================
    // CÁC HÀM OVERLOAD CHỐNG LỖI CODE CŨ
    // ==========================================
    public void StartReading(GameObject tableObj, string content, Vector3? pos = null, Vector3? rot = null)
    { StartReading(tableObj, (GameObject[])null, new string[] { content }, pos, rot); }

    public void StartReading(GameObject tableObj, string[] pages, Vector3? pos = null, Vector3? rot = null)
    { StartReading(tableObj, (GameObject[])null, pages, pos, rot); }

    public void StartReading(GameObject tableObj, GameObject singlePrefab, string content, Vector3? pos = null, Vector3? rot = null)
    { StartReading(tableObj, singlePrefab != null ? new GameObject[] { singlePrefab } : null, new string[] { content }, pos, rot); }

    public void StartReading(GameObject tableObj, GameObject singlePrefab, string[] pages, Vector3? pos = null, Vector3? rot = null)
    { StartReading(tableObj, singlePrefab != null ? new GameObject[] { singlePrefab } : null, pages, pos, rot); }

    // ==========================================
    // HÀM BẮT ĐẦU ĐỌC (CHÍNH)
    // ==========================================
    public void StartReading(GameObject tableObj, GameObject[] displayPrefabs, string[] pages, Vector3? customPosOffset = null, Vector3? customRotOffset = null)
    {
        isReading = true;
        isTurningPage = false;
        pickupTime = Time.time;
        currentZoomZ = 0f;
        currentPageIndex = 0;

        currentPages = (pages != null && pages.Length > 0) ? pages : new string[] { "" };
        currentPagePrefabs = displayPrefabs;

        activePosOffset = customPosOffset ?? defaultPaperPositionOffset;
        activeRotOffset = customRotOffset ?? defaultPaperRotationOffset;

        // Lưu thông tin khối Cube trên bàn
        originalTableObject = tableObj;
        if (originalTableObject != null)
        {
            originalPosition = originalTableObject.transform.position;
            originalRotation = originalTableObject.transform.rotation;
        }

        // Sinh Model cho Trang Đầu Tiên
        SpawnModelForCurrentPage();

        if (currentPaper != null && paperAnchor != null)
        {
            currentPaper.transform.rotation = paperAnchor.rotation * Quaternion.Euler(activeRotOffset);
            if (isUsingSpawnedPrefab)
            {
                currentPaper.transform.position = paperAnchor.position + paperAnchor.TransformDirection(activePosOffset);
            }
        }

        if (readingCanvas != null) readingCanvas.SetActive(true);

        if (CamcorderUI.Instance != null && CamcorderUI.Instance.gameObject.activeSelf)
        {
            wasCamcorderActiveOnRead = true;
            CamcorderUI.Instance.gameObject.SetActive(false);
        }
        else { wasCamcorderActiveOnRead = false; }

        if (paperRustleSound != null && audioSource != null) audioSource.PlayOneShot(paperRustleSound, 0.8f);

        UpdatePageButtons();

        if (textCoroutine != null) StopCoroutine(textCoroutine);
        textCoroutine = StartCoroutine(DisplayTextRoutine(currentPages[currentPageIndex]));

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
    }

    // ==========================================
    // LOGIC SPAWN TỪNG TRANG & LẬT TRANG
    // ==========================================

    void SpawnModelForCurrentPage()
    {
        GameObject prefabToSpawn = null;

        // Tìm Model tương ứng với số trang
        if (currentPagePrefabs != null && currentPagePrefabs.Length > 0)
        {
            int prefabIndex = Mathf.Min(currentPageIndex, currentPagePrefabs.Length - 1);
            prefabToSpawn = currentPagePrefabs[prefabIndex];
        }

        if (prefabToSpawn != null)
        {
            isUsingSpawnedPrefab = true;
            if (originalTableObject != null) originalTableObject.SetActive(false); // Luôn ẩn khối bàn
            currentPaper = Instantiate(prefabToSpawn);
        }
        else
        {
            isUsingSpawnedPrefab = false;
            currentPaper = originalTableObject;
            if (currentPaper != null) currentPaper.SetActive(true);
        }

        // Tắt vật lý cho Model đang hiển thị
        if (currentPaper != null)
        {
            Rigidbody rb = currentPaper.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Collider col = currentPaper.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    public void NextPage()
    {
        if (!isReading || isTurningPage) return;
        if (currentPageIndex < currentPages.Length - 1) StartCoroutine(TurnPageRoutine(currentPageIndex + 1, true));
    }

    public void PrevPage()
    {
        if (!isReading || isTurningPage) return;
        if (currentPageIndex > 0) StartCoroutine(TurnPageRoutine(currentPageIndex - 1, false));
    }

    IEnumerator TurnPageRoutine(int targetPageIndex, bool goingNext)
    {
        isTurningPage = true;
        if (paperRustleSound != null && audioSource != null) audioSource.PlayOneShot(paperRustleSound, 0.7f);

        Vector3 basePos = paperAnchor.position + paperAnchor.TransformDirection(activePosOffset + new Vector3(0f, 0f, currentZoomZ));
        Vector3 slideOutPos = basePos + (goingNext ? -paperAnchor.right : paperAnchor.right) * pageTurnSlideDistance;

        // 1. Trượt Model CŨ ra ngoài
        float elapsed = 0f;
        Vector3 startPos = currentPaper.transform.position;
        while (elapsed < pageTurnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pageTurnDuration;
            currentPaper.transform.position = Vector3.Lerp(startPos, slideOutPos, t * t);
            yield return null;
        }

        // 2. Xóa Model cũ & Tráo Model MỚI
        currentPageIndex = targetPageIndex;
        UpdatePageButtons();

        GameObject oldPaper = currentPaper; // Lưu tạm model cũ
        SpawnModelForCurrentPage();         // Sinh model mới (Gán đè vào biến currentPaper)

        // Xóa hẳn model cũ (nếu nó là bản copy)
        if (isUsingSpawnedPrefab && oldPaper != currentPaper && oldPaper != originalTableObject && oldPaper != null)
        {
            Destroy(oldPaper);
        }

        // 3. Chuẩn bị vị trí cho Model MỚI để trượt vào
        Quaternion targetRot = paperAnchor.rotation * Quaternion.Euler(activeRotOffset);
        currentPaper.transform.rotation = targetRot;

        if (textCoroutine != null) StopCoroutine(textCoroutine);
        textCoroutine = StartCoroutine(DisplayTextRoutine(currentPages[currentPageIndex]));

        Vector3 slideInStartPos = basePos + (goingNext ? paperAnchor.right : -paperAnchor.right) * pageTurnSlideDistance;
        currentPaper.transform.position = slideInStartPos;

        // 4. Trượt Model MỚI vào giữa
        elapsed = 0f;
        while (elapsed < pageTurnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pageTurnDuration;
            currentPaper.transform.position = Vector3.Lerp(slideInStartPos, basePos, 1f - (1f - t) * (1f - t));
            yield return null;
        }

        currentPaper.transform.position = basePos;
        isTurningPage = false;
    }

    void UpdatePageButtons()
    {
        if (nextPageButton != null) nextPageButton.SetActive(currentPageIndex < currentPages.Length - 1);
        if (prevPageButton != null) prevPageButton.SetActive(currentPageIndex > 0);
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
        if (paperTextUI != null && currentPages != null && currentPageIndex < currentPages.Length)
        {
            paperTextUI.text = currentPages[currentPageIndex];
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
        isTypingText = false;
        isTurningPage = false;

        // Xóa Model đang cầm trên tay
        if (isUsingSpawnedPrefab && currentPaper != null && currentPaper != originalTableObject)
        {
            Destroy(currentPaper);
        }

        // Luôn khôi phục khối Cube trên bàn về trạng thái ban đầu
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