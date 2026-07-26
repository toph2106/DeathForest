using UnityEngine;

public class DesktopComputer : MonoBehaviour
{
    [Header("UI Hint")]
    public GameObject pressFHintUI; // Canvas "Press F" lơ lửng trên bàn máy tính

    private ComputerSystem computerSystem;

    void Start()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(false);
        computerSystem = Object.FindFirstObjectByType<ComputerSystem>();
    }

    public void ShowPrompt()
    {
        // Chỉ hiện chữ F nếu người chơi chưa bật máy lên
        if (computerSystem != null && !computerSystem.isUsingComputer)
        {
            if (pressFHintUI != null) pressFHintUI.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(false);
    }

    public void Interact()
    {
        if (computerSystem == null) return;

        if (!computerSystem.isUsingComputer)
        {
            HidePrompt(); // Ẩn chữ F đi
            computerSystem.OpenComputer(); // Bật màn hình máy tính lên
        }
    }
}