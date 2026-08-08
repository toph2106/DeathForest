using UnityEngine;
using TMPro;

public class DoorPasscodeUI : MonoBehaviour
{
    public static DoorPasscodeUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject uiPanel;
    public TextMeshProUGUI inputDisplayText;
    public int maxPasscodeLength = 4;

    private AdvancedDoor currentDoor;
    private string currentInput = "";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (uiPanel != null) uiPanel.SetActive(false);
    }

    private void Update()
    {
        if (uiPanel != null && uiPanel.activeSelf)
        {
            // Liên tục ép mở chuột trong mỗi frame để chống các script camera khác khóa lại
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Bấm ESC để thoát giao diện nhập mã
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseUI();
            }
        }
    }

    public void OpenUI(AdvancedDoor door)
    {
        currentDoor = door;
        currentInput = "";
        UpdateDisplay();

        uiPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseUI()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
        currentDoor = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void AddNumber(string num)
    {
        if (currentInput.Length < maxPasscodeLength)
        {
            currentInput += num;
            UpdateDisplay();
        }
    }

    public void ClearInput()
    {
        currentInput = "";
        UpdateDisplay();
    }

    public void CheckPasscode()
    {
        if (currentDoor == null) return;

        if (currentInput == currentDoor.correctPasscode)
        {
            currentDoor.UnlockDoor();
            CloseUI();
        }
        else
        {
            Debug.Log("Sai mật mã!");
            ClearInput();
        }
    }

    private void UpdateDisplay()
    {
        if (inputDisplayText != null) inputDisplayText.text = currentInput;
    }
}