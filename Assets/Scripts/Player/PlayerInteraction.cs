using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3f;
    public LayerMask interactableLayer;

    [Header("References")]
    public PaperReaderManager readManager; // Kéo Object chứa script PaperReaderManager vào đây

    private SmoothDoubleDoor currentDoor = null;
    private ReadablePaper currentPaper = null;

    void Update()
    {
        // QUAN TRỌNG: Nếu đang đọc giấy thì khóa tia nhìn lại, không cho tương tác linh tinh
        if (readManager != null && readManager.isReading) return;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            SmoothDoubleDoor door = hit.collider.GetComponentInParent<SmoothDoubleDoor>();
            ReadablePaper paper = hit.collider.GetComponentInParent<ReadablePaper>();

            // Xử lý nhìn vào CỬA
            if (door != null)
            {
                ClearCurrentPaper(); // Bỏ focus giấy nếu có
                if (currentDoor != door)
                {
                    ClearCurrentDoor();
                    currentDoor = door;
                    currentDoor.ShowPrompt();
                }
                if (Input.GetKeyDown(KeyCode.F)) currentDoor.ToggleDoor();
            }
            // Xử lý nhìn vào GIẤY
            else if (paper != null)
            {
                ClearCurrentDoor(); // Bỏ focus cửa nếu có
                if (currentPaper != paper)
                {
                    ClearCurrentPaper();
                    currentPaper = paper;
                    currentPaper.ShowPrompt();
                }

                // Bấm F để nhặt giấy lên đọc
                if (Input.GetKeyDown(KeyCode.F))
                {
                    Debug.Log("-> [BƯỚC 1]: Đã nhận phím F từ bàn phím!");
                    currentPaper.HidePrompt();
                    if (readManager == null)
                    {
                        Debug.LogError("-> LỖI: Bạn chưa kéo PaperReaderManager vào ô Read Manager trên Camera!");
                        return;
                    }

                    Debug.Log("-> [BƯỚC 2]: Chuẩn bị chuyển dữ liệu sang Manager để nhấc giấy lên.");
                    readManager.StartReading(currentPaper.gameObject, currentPaper.content);
                }
            }
            else
            {
                ClearAll();
            }
        }
        else
        {
            ClearAll();
        }
    }

    void ClearCurrentDoor()
    {
        if (currentDoor != null) { currentDoor.HidePrompt(); currentDoor = null; }
    }

    void ClearCurrentPaper()
    {
        if (currentPaper != null) { currentPaper.HidePrompt(); currentPaper = null; }
    }

    void ClearAll()
    {
        ClearCurrentDoor();
        ClearCurrentPaper();
    }
}   