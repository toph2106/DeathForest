using UnityEngine;
using UnityEngine.UI;

public class ComputerSystem : MonoBehaviour
{
    [Header("UI Canvas")]
    public GameObject computerCanvas; // Chính là cái ComputerUI_Panel cha

    [Header("Tabs Configuration")]
    public GameObject[] tabPanels = new GameObject[3]; // Kéo 3 Panel_Tab1, 2, 3 vào đây
    public Image[] tabButtonImages = new Image[3];      // Kéo Component Image của 3 Button vào đây

    [Header("Visual State Colors")]
    public Color activeColor = Color.white;            // Màu của tab đang chọn (sáng rõ, Alpha = 1)
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.4f); // Màu của tab bị mờ đi (Alpha thấp)

    [HideInInspector]
    public bool isUsingComputer = false;

    // Lưu trữ Script di chuyển của nhân vật để khóa di chuyển khi đang dùng máy tính
    private MonoBehaviour playerMovementScript;

    void Start()
    {
        if (computerCanvas != null) computerCanvas.SetActive(false);

        // Mẹo tự tìm Script di chuyển của nhân vật (Sếp thay "PlayerController" bằng tên Script di chuyển của sếp nhé)
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerMovementScript = player.GetComponent<MonoBehaviour>(); // Lấy script di chuyển
        }
    }

    void Update()
    {
        // Nếu đang dùng máy tính, bấm ESC hoặc bấm F một lần nữa để thoát
        if (isUsingComputer)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F))
            {
                CloseComputer();
            }
        }
    }

    public void OpenComputer()
    {
        isUsingComputer = true;
        if (computerCanvas != null) computerCanvas.SetActive(true);

        // 1. Khóa di chuyển của nhân vật để không bị đi xuyên tường khi đang đọc máy tính
        if (playerMovementScript != null) playerMovementScript.enabled = false;

        // 2. Hiện con trỏ chuột và mở khóa khỏi tâm màn hình để người chơi click tab
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Mặc định mở Tab 1 (Index = 0) khi vừa bật máy lên
        SelectTab(0);
    }

    public void CloseComputer()
    {
        isUsingComputer = false;
        if (computerCanvas != null) computerCanvas.SetActive(false);

        // 1. Cho phép nhân vật di chuyển lại bình thường
        if (playerMovementScript != null) playerMovementScript.enabled = true;

        // 2. Khóa chuột lại vào giữa màn hình cho góc nhìn FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Hàm đổi Tab (Sẽ được gọi khi click vào các nút trên màn hình)
    public void SelectTab(int tabIndex)
    {
        for (int i = 0; i < 3; i++)
        {
            // Bật Panel được chọn, tắt các Panel còn lại
            if (tabPanels[i] != null)
            {
                tabPanels[i].SetActive(i == tabIndex);
            }

            // Đổi độ mờ của Button: Được chọn thì sáng (Active), còn lại mờ đi (Inactive)
            if (tabButtonImages[i] != null)
            {
                tabButtonImages[i].color = (i == tabIndex) ? activeColor : inactiveColor;
            }
        }
    }
}