using UnityEngine;
using TMPro;
using UnityEngine.UI; // Cần thêm dòng này để làm việc với UI

public class PaperReaderManager : MonoBehaviour
{
    [Header("Setup")]
    public Transform paperAnchor;
    public GameObject readingCanvas;
    public TextMeshProUGUI paperTextUI;

    [Header("Player Controllers (To Disable)")]
    [Tooltip("Kéo các Script di chuyển và xoay Camera của Player vào đây để khóa lại khi đọc giấy")]
    public MonoBehaviour[] playerScriptsToFreeze;

    [Header("Settings")]
    public float rotationSpeed = 5f;

    private GameObject currentPaper;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    public bool isReading { get; private set; }

    private float pickupTime = 0f;

    void Update()
    {
        if (isReading)
        {
            // GIỮ chuột trái để xoay giấy (nhả ra để di chuột bấm nút X)
            if (Input.GetMouseButton(0))
            {
                float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
                float rotY = Input.GetAxis("Mouse Y") * rotationSpeed;

                currentPaper.transform.Rotate(Camera.main.transform.up, -rotX, Space.World);
                currentPaper.transform.Rotate(Camera.main.transform.right, rotY, Space.World);
            }

            // Vẫn giữ phím Esc làm phương án dự phòng để thoát
            if (Time.time > pickupTime + 0.2f && Input.GetKeyDown(KeyCode.Escape))
            {
                StopReading();
            }
        }
    }

    public void StartReading(GameObject paperObj, string content)
    {
        currentPaper = paperObj;
        isReading = true;
        pickupTime = Time.time;

        originalPosition = currentPaper.transform.position;
        originalRotation = currentPaper.transform.rotation;

        Rigidbody rb = currentPaper.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = currentPaper.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        currentPaper.transform.position = paperAnchor.position;
        currentPaper.transform.rotation = paperAnchor.rotation;

        paperTextUI.text = content;
        readingCanvas.SetActive(true);

        // --- TÍNH NĂNG MỚI: HIỆN CHUỘT VÀ KHÓA DI CHUYỂN ---
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (var script in playerScriptsToFreeze)
        {
            if (script != null) script.enabled = false;
        }
    }

    public void StopReading()
    {
        Rigidbody rb = currentPaper.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        Collider col = currentPaper.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        currentPaper.transform.position = originalPosition;
        currentPaper.transform.rotation = originalRotation;

        readingCanvas.SetActive(false);
        isReading = false;
        currentPaper = null;

        // --- TÍNH NĂNG MỚI: ẨN CHUỘT VÀ MỞ KHÓA DI CHUYỂN ---
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (var script in playerScriptsToFreeze)
        {
            if (script != null) script.enabled = true;
        }
    }

    // --- TÍNH NĂNG MỚI: HÀM DÀNH CHO NÚT BẤM (X) TẠI GIAO DIỆN ---
    public void OnCloseButtonClicked()
    {
        if (isReading && Time.time > pickupTime + 0.2f)
        {
            StopReading();
        }
    }
}