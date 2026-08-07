using UnityEngine;

public class ReadablePaper : MonoBehaviour, IInteractable
{
    [TextArea(5, 15)]
    [Tooltip("Nhập nội dung tờ giấy / tài liệu vào đây")]
    public string content;

    public GameObject paperHintUI;

    // BIẾN STATIC TOÀN CỤC ĐỒNG BỘ PLAYERPREFS GHI NHỚ NGƯỜI CHƠI ĐÃ ĐỌC GIẤY CHƯA
    public static bool HasReadPaper
    {
        get
        {
            return PlayerPrefs.GetInt("HasReadPaper_Map02", 0) == 1;
        }
        set
        {
            PlayerPrefs.SetInt("HasReadPaper_Map02", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    void Start()
    {
        // Reset trạng thái chưa đọc khi vừa nạp Map 02 mới
        HasReadPaper = false;
        if (paperHintUI != null) paperHintUI.SetActive(false);
    }

    // TƯƠNG TÁC PHÍM F CHUẨN INTERACTPRO
    public void Interact()
    {
        HasReadPaper = true; // Ghi nhận 100% đã đọc tờ giấy!
        Debug.Log("[ReadablePaper] 📜 Đã bấm F đọc tờ giấy! HasReadPaper = TRUE");

        PaperReaderManager reader = FindFirstObjectByType<PaperReaderManager>();
        if (reader != null)
        {
            reader.StartReading(gameObject, content);
        }
        else
        {
            Debug.LogError("[ReadablePaper] ⚠️ Không tìm thấy PaperReaderManager trong Scene!");
        }
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