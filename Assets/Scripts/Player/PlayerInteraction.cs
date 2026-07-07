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
    private SmoothSingleDoor currentSingleDoor = null; // Thêm biến lưu cửa đơn hiện tại

    void Update()
    {
        // QUAN TRỌNG: Nếu đang đọc giấy thì khóa tia nhìn lại, không cho tương tác linh tinh
        if (readManager != null && readManager.isReading) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        Debug.DrawRay(mainCam.transform.position, mainCam.transform.forward * interactRange, Color.red);

        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            SmoothDoubleDoor door = hit.collider.GetComponentInParent<SmoothDoubleDoor>();
            ReadablePaper paper = hit.collider.GetComponentInParent<ReadablePaper>();
            SmoothSingleDoor singleDoor = hit.collider.GetComponentInParent<SmoothSingleDoor>(); // Quét tìm cửa đơn

            // 1. Xử lý nhìn vào CỬA ĐÔI
            if (door != null)
            {
                ClearCurrentPaper();
                ClearCurrentSingleDoor(); // Bỏ focus cửa đơn nếu có

                if (currentDoor != door)
                {
                    ClearCurrentDoor();
                    currentDoor = door;
                    currentDoor.ShowPrompt();
                }
                if (Input.GetKeyDown(KeyCode.F)) currentDoor.ToggleDoor();
            }
            // 2. Xử lý nhìn vào GIẤY
            else if (paper != null)
            {
                ClearCurrentDoor();
                ClearCurrentSingleDoor(); // Bỏ focus cửa đơn nếu có

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
            // 3. XỬ LÝ NHÌN VÀO CỬA ĐƠN (TÍNH NĂNG MỚI)
            else if (singleDoor != null)
            {
                ClearCurrentDoor();
                ClearCurrentPaper(); // Bỏ focus giấy nếu có

                if (currentSingleDoor != singleDoor)
                {
                    ClearCurrentSingleDoor();
                    currentSingleDoor = singleDoor;
                    currentSingleDoor.ShowPrompt(); // Hiện chữ Press F của cửa đơn
                }
                if (Input.GetKeyDown(KeyCode.F)) currentSingleDoor.ToggleDoor();
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

    // Hàm dọn dẹp trạng thái cửa đơn
    void ClearCurrentSingleDoor()
    {
        if (currentSingleDoor != null) { currentSingleDoor.HidePrompt(); currentSingleDoor = null; }
    }

    void ClearAll()
    {
        ClearCurrentDoor();
        ClearCurrentPaper();
        ClearCurrentSingleDoor(); // Thêm vào hàm xóa tổng hợp
    }
}