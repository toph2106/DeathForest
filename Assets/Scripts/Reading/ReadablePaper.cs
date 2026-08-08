using UnityEngine;

public class ReadablePaper : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string englishDialogue;
        [TextArea(2, 4)]
        public string vietnameseDialogue;

        [Tooltip("Âm thanh lồng tiếng cho câu thoại này (Tùy chọn)")]
        public AudioClip dialogueAudio;
    }

    [System.Serializable]
    public class PaperPageStep
    {
        [Header("1. Trang Giấy Hiển Thị")]
        [Tooltip("Tên của Mesh con đại diện cho trang này (VD: mesh_node, mesh_node.001). Để trống code sẽ tự lấy theo thứ tự con trong xấp giấy!")]
        public string subMeshNodeName;

        [Tooltip("Tùy chọn: Model Prefab riêng nếu không dùng xấp giấy tổng ở trên")]
        public GameObject paperPrefab;

        [Tooltip("Ảnh Texture riêng cho trang giấy này (Tùy chọn)")]
        public Texture2D paperTexture;

        [Header("2. Danh sách các câu thoại cho trang giấy này")]
        public DialogueLine[] dialogueLines;
    }

    [Header("1. Model 3D Xấp Giấy Tổng (Chứa Đủ Cả 4 Trang Giấy)")]
    [Tooltip("Kéo Prefab xấp giấy tổng (VD: NoteRd) vào đây để khi nâng lên đọc sẽ giữ nguyên cả xấp 4 tờ này!")]
    public GameObject mainPaperPrefab;

    [Header("2. Danh Sách Các Trang Giấy & Chuỗi Thoại Đi Kèm")]
    public PaperPageStep[] paperSteps;

    [Header("3. Tùy Chọn Offset Vị Trí & Góc Xoay (Tùy chọn)")]
    public bool useCustomOffset = false;
    public Vector3 customPositionOffset = new Vector3(-0.25f, 0f, 0f);
    public Vector3 customRotationOffset = new Vector3(0f, 0f, 90f);

    public GameObject paperHintUI;

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
            if (useCustomOffset)
            {
                reader.StartReadingSteps(gameObject, mainPaperPrefab, paperSteps, customPositionOffset, customRotationOffset);
            }
            else
            {
                reader.StartReadingSteps(gameObject, mainPaperPrefab, paperSteps);
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