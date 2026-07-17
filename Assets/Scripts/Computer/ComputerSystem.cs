using UnityEngine;
using UnityEngine.UI;

public class ComputerSystem : MonoBehaviour
{
    [Header("UI Canvas")]
    public GameObject computerCanvas; // Chính là cái ComputerUI_Panel cha

    [Header("Camera UI Integration (Tính năng mới)")]
    [Tooltip("Kéo cái GameObject chứa giao diện/kính lọc của Máy Quay vào đây")]
    public GameObject cameraUI;
    private bool wasCameraUIActive = false; // Biến ghi nhớ trạng thái trước đó của Máy quay

    [Header("Tabs Configuration")]
    public GameObject[] tabPanels = new GameObject[3];
    public Image[] tabButtonImages = new Image[3];

    [Header("Visual State Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

    [HideInInspector]
    public bool isUsingComputer = false;

    private MonoBehaviour playerMovementScript;

    void Start()
    {
        if (computerCanvas != null) computerCanvas.SetActive(false);

        // Tự động tìm Script di chuyển của nhân vật
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerMovementScript = player.GetComponent<MonoBehaviour>();
        }
    }

    void Update()
    {
        if (isUsingComputer && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F)))
        {
            CloseComputer();
        }
    }

    public void OpenComputer()
    {
        isUsingComputer = true;
        if (computerCanvas != null) computerCanvas.SetActive(true);

        // --- MỚI: Kiểm tra và tạm ẩn UI Máy Quay ---
        if (cameraUI != null && cameraUI.activeSelf)
        {
            wasCameraUIActive = true;  // Ghi nhớ là người chơi ĐANG bật máy quay
            cameraUI.SetActive(false); // Tạm thời ẩn UI máy quay đi
        }
        else
        {
            wasCameraUIActive = false; // Người chơi đang KHÔNG bật máy quay
        }

        // Khóa di chuyển của nhân vật
        if (playerMovementScript != null) playerMovementScript.enabled = false;

        // Hiện con trỏ chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Mặc định mở Tab 1
        SelectTab(0);
    }

    public void CloseComputer()
    {
        isUsingComputer = false;
        if (computerCanvas != null) computerCanvas.SetActive(false);

        // --- MỚI: Hoàn trả lại trạng thái UI Máy Quay ---
        if (wasCameraUIActive && cameraUI != null)
        {
            cameraUI.SetActive(true); // Bật lại UI máy quay như cũ cho người chơi
        }

        // Cho phép nhân vật di chuyển lại
        if (playerMovementScript != null) playerMovementScript.enabled = true;

        // Khóa chuột lại vào giữa màn hình
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SelectTab(int tabIndex)
    {
        for (int i = 0; i < 3; i++)
        {
            if (tabPanels[i] != null)
            {
                tabPanels[i].SetActive(i == tabIndex);
            }

            if (tabButtonImages[i] != null)
            {
                tabButtonImages[i].color = (i == tabIndex) ? activeColor : inactiveColor;
            }
        }
    }
}