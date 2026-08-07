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

    [Header("2. Danh Sách Các UI Khác Cần Ẩn Khi Đọc Giấy (Camcorder, HUD...)")]
    [Tooltip("Kéo các UI như Camcorder, Thanh đồ Inventory, Tâm ngắm vào đây để tự động ẩn khi nhấc giấy lên đọc")]
    public GameObject[] uisToHideOnRead;

    [Header("3. Khóa Người Chơi Khi Đọc Giấy")]
    [Tooltip("Kéo script MovePl / PlayerController vào đây để khóa lại khi đang đọc")]
    public MonoBehaviour[] playerScriptsToFreeze;

    [Header("4. Tốc Độ Xoay Tờ Giấy 3D")]
    public float rotationSpeed = 5f;

    [Header("5. Hiệu Ứng Chữ Đọc Giấy (Typewriter & Fade In)")]
    [Tooltip("Bật hiệu ứng gõ chữ / hiện chữ mượt từng ký tự khi nhấc giấy lên")]
    public bool useTypewriterEffect = true;
    [Tooltip("Tốc độ hiện từng ký tự (Mặc định: 0.015 giây)")]
    public float typewriterSpeed = 0.015f;

    [Tooltip("Bật hiệu ứng mờ dần hiện rõ (Fade In) khi mở giấy")]
    public bool useFadeIn = true;
    [Tooltip("Thời gian mờ dần (Mặc định: 0.3 giây)")]
    public float fadeInDuration = 0.3f;

    [Tooltip("Kéo âm thanh sột soạt lật giấy (Paper Rustle SFX) vào đây (Tùy chọn)")]
    public AudioClip paperRustleSound;

    [Header("6. Cấu Hình Vị Trí & Góc Xoay Mặc Định Khi Nhấc Giấy")]
    [Tooltip("Góc xoay mặc định khi vừa nhấc giấy lên để tờ giấy NẰM DỌC CHUẨN (Mặc định: 0, 0, 90)")]
    public Vector3 defaultPaperRotationOffset = new Vector3(0f, 0f, 90f);

    [Tooltip("Độ lệch vị trí khi nhấc giấy để NHÍCH SANG BÊN TRÁI MÀN HÌNH (Mặc định: -0.25, 0, 0)")]
    public Vector3 defaultPaperPositionOffset = new Vector3(-0.25f, 0f, 0f);

    private GameObject currentPaper;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    public bool isReading { get; private set; }
    private float pickupTime = 0f;

    private List<GameObject> previouslyActiveUI = new List<GameObject>();
    private AudioSource audioSource;
    private Coroutine textCoroutine;
    private bool isTypingText = false;
    private string fullTextContent = "";

    private Vector3 dragStartPos;
    private bool wasDragging = false;
    private bool wasCamcorderActiveOnRead = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (isReading)
        {
            // 1. Nếu chữ đang gõ mà bấm chuột trái -> Nhảy hiện full ngay
            if (isTypingText && Input.GetMouseButtonDown(0) && Time.time > pickupTime + 0.1f)
            {
                CompleteTextInstantly();
            }

            // 2. Bấm phím F, ESC hoặc chuột phải để đặt giấy xuống
            if (Time.time > pickupTime + 0.25f && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1)))
            {
                StopReading();
                return;
            }

            // 3. Ghi nhận vị trí bấm chuột để phân biệt CLICK nhẹ vs KÉO DI CHUỘT XOAY GIẤY
            if (Input.GetMouseButtonDown(0))
            {
                dragStartPos = Input.mousePosition;
                wasDragging = false;
            }

            // 4. Giữ chuột trái để xoay tờ giấy 3D
            if (Input.GetMouseButton(0))
            {
                if (Vector3.Distance(Input.mousePosition, dragStartPos) > 8f)
                {
                    wasDragging = true;
                }

                float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
                float rotY = Input.GetAxis("Mouse Y") * rotationSpeed;

                if (currentPaper != null && (Mathf.Abs(rotX) > 0.01f || Mathf.Abs(rotY) > 0.01f))
                {
                    currentPaper.transform.Rotate(Camera.main.transform.up, -rotX, Space.World);
                    currentPaper.transform.Rotate(Camera.main.transform.right, rotY, Space.World);
                }
            }

            // 5. Khi nhả chuột trái sau khi xoay giấy -> Giữ cờ wasDragging trong 1 frame để chặn việc tự động thoát
            if (Input.GetMouseButtonUp(0))
            {
                StartCoroutine(ResetDragStateRoutine());
            }
        }
    }

    IEnumerator ResetDragStateRoutine()
    {
        yield return new WaitForEndOfFrame();
        wasDragging = false;
    }

    public void StartReading(GameObject paperObj, string content)
    {
        // GHI NHẬN CHẮC CHẮN ĐÃ ĐỌC GIẤY
        ReadablePaper.HasReadPaper = true;
        Debug.Log("[PaperReaderManager] 📜 Đã mở đọc giấy! Ghi nhận HasReadPaper = TRUE");

        currentPaper = paperObj;
        isReading = true;
        pickupTime = Time.time;
        fullTextContent = content;

        if (currentPaper != null)
        {
            originalPosition = currentPaper.transform.position;
            originalRotation = currentPaper.transform.rotation;

            Rigidbody rb = currentPaper.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Collider col = currentPaper.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (paperAnchor != null)
            {
                // ÉP ĐẶT VỊ TRÍ NHÍCH SANG BÊN TRÁI VÀ NẰM DỌC CHUẨN ĐIỆN ẢNH
                Vector3 targetPos = paperAnchor.position + paperAnchor.TransformDirection(defaultPaperPositionOffset);
                Quaternion targetRot = paperAnchor.rotation * Quaternion.Euler(defaultPaperRotationOffset);

                currentPaper.transform.position = targetPos;
                currentPaper.transform.rotation = targetRot;
            }
        }

        if (readingCanvas != null) readingCanvas.SetActive(true);

        // ẨN CẢM BIẾN MÁY QUAY CAMCORDER SỐNG SÓT QUA SCENE (DONTDESTROYONLOAD)
        if (CamcorderUI.Instance != null && CamcorderUI.Instance.gameObject.activeSelf)
        {
            wasCamcorderActiveOnRead = true;
            CamcorderUI.Instance.gameObject.SetActive(false);
        }
        else
        {
            wasCamcorderActiveOnRead = false;
        }

        // PHÁT TIẾNG SỘT SOẠT LẬT GIẤY
        if (paperRustleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(paperRustleSound, 0.8f);
        }

        // CHẠY HIỆU ỨNG CHỮ TYPEWRITER & FADE IN
        if (textCoroutine != null) StopCoroutine(textCoroutine);
        textCoroutine = StartCoroutine(DisplayTextRoutine(content));

        // ẨN DANH SÁCH CÁC UI INGAME KHÁC (HUD, Crosshair...)
        previouslyActiveUI.Clear();
        if (uisToHideOnRead != null)
        {
            foreach (GameObject ui in uisToHideOnRead)
            {
                if (ui != null && ui.activeSelf)
                {
                    previouslyActiveUI.Add(ui);
                    ui.SetActive(false);
                }
            }
        }

        // BẬT CON TRỎ CHUỘT VÀ KHÓA NGƯỜI CHƠI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerScriptsToFreeze != null)
        {
            foreach (var script in playerScriptsToFreeze)
            {
                if (script != null) script.enabled = false;
            }
        }
    }

    IEnumerator DisplayTextRoutine(string targetText)
    {
        if (paperTextUI == null) yield break;

        // FADE IN MỜ DẦN HIỆN CHỮ
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

        // TYPEWRITER EFFECT
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
            paperTextUI.text = fullTextContent;
            Color c = paperTextUI.color;
            c.a = 1f;
            paperTextUI.color = c;
        }
    }

    public void StopReading()
    {
        if (!isReading) return;

        if (textCoroutine != null) StopCoroutine(textCoroutine);
        isTypingText = false;

        if (currentPaper != null)
        {
            Rigidbody rb = currentPaper.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            Collider col = currentPaper.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            currentPaper.transform.position = originalPosition;
            currentPaper.transform.rotation = originalRotation;
        }

        if (readingCanvas != null) readingCanvas.SetActive(false);
        isReading = false;
        currentPaper = null;

        // BẬT LẠI MÁY QUAY CAMCORDER NẾU TRƯỚC ĐÓ ĐANG BẬT
        if (wasCamcorderActiveOnRead && CamcorderUI.Instance != null)
        {
            CamcorderUI.Instance.gameObject.SetActive(true);
            wasCamcorderActiveOnRead = false;
        }

        // KHÔI PHỤC LẠI CÁC UI INGAME TRƯỚC ĐÓ
        if (previouslyActiveUI != null)
        {
            foreach (GameObject ui in previouslyActiveUI)
            {
                if (ui != null) ui.SetActive(true);
            }
            previouslyActiveUI.Clear();
        }

        // KHÓA LẠI CON TRỎ CHUỘT VÀ MỞ KHÓA NGƯỜI CHƠI
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerScriptsToFreeze != null)
        {
            foreach (var script in playerScriptsToFreeze)
            {
                if (script != null) script.enabled = true;
            }
        }
    }

    // HÀM GÁN CHO BACKGROUND NỀN CANVAS ĐỂ BẤM OUTSIDE THOÁT RA NGOÀI
    public void OnClickBackgroundToClose()
    {
        // NẾU VỪA KÉO DI CHUỘT XOAY GIẤY XONG NHẢ CHUỘT -> CẤM TỰ ĐỘNG THOÁT!
        if (wasDragging) return;

        if (isReading && Time.time > pickupTime + 0.25f)
        {
            StopReading();
        }
    }
}