using UnityEngine;

public class ReadablePaper : MonoBehaviour
{
    [TextArea(5, 15)]
    [Tooltip("Nhập nội dung tờ giấy vào đây")]
    public string content;

    public GameObject paperHintUI; // Kéo Canvas "Press F" chữ World Space vào đây (giống cái cửa)

    void Start()
    {
        if (paperHintUI != null) paperHintUI.SetActive(false);
    }

    public void ShowPrompt()
    {
        if (paperHintUI != null) paperHintUI.SetActive(true);
    }

    public void HidePrompt()
    {
        if (paperHintUI != null) paperHintUI.SetActive(false);
    }
}