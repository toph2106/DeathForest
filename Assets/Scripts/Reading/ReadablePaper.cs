using UnityEngine;

public class ReadablePaper : MonoBehaviour, IInteractable
{
    [Header("1. Nội Dung Văn Bản")]
    [TextArea(5, 15)]
    public string content;

    [TextArea(5, 15)]
    public string[] pages;

    public GameObject paperHintUI;

    [Header("2. Tùy Chọn Offset (Tùy chọn)")]
    public bool useCustomOffset = false;
    public Vector3 customPositionOffset = new Vector3(-0.25f, 0f, 0f);
    public Vector3 customRotationOffset = new Vector3(0f, 0f, 90f);

    [Header("3. Model Hiển Thị Cho TỪNG TRANG (Tùy chọn)")]
    [Tooltip("Gán Prefab tờ giấy cho từng trang. VD: Trang 1 giấy phẳng, Trang 2 giấy rách. Nếu mảng này ít hơn số trang, các trang sau sẽ tự dùng lại Model cuối cùng trong mảng.")]
    public GameObject[] pagePrefabs;

    public static bool HasReadPaper
    {
        get { return PlayerPrefs.GetInt("HasReadPaper_Map02", 0) == 1; }
        set
        {
            PlayerPrefs.SetInt("HasReadPaper_Map02", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    void Start()
    {
        HasReadPaper = false;
        if (paperHintUI != null) paperHintUI.SetActive(false);
    }

    public void Interact()
    {
        HasReadPaper = true;

        PaperReaderManager reader = FindFirstObjectByType<PaperReaderManager>();
        if (reader != null)
        {
            string[] finalPages = (pages != null && pages.Length > 0) ? pages : new string[] { content };

            if (useCustomOffset)
            {
                reader.StartReading(gameObject, pagePrefabs, finalPages, customPositionOffset, customRotationOffset);
            }
            else
            {
                reader.StartReading(gameObject, pagePrefabs, finalPages);
            }
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