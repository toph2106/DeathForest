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
    private SmoothSlidingDoor currentSlidingDoor = null;
    private InteractableItem currentItem = null;
    private CorpseLoot currentCorpse = null;
    private DesktopComputer currentComputer = null;
    void Update()
    {
        // Đặt đoạn này ở dòng đầu tiên của hàm Update() để nếu đang dùng máy tính thì tắt tia quét mắt
        ComputerSystem compSys = Object.FindFirstObjectByType<ComputerSystem>();
        if (compSys != null && compSys.isUsingComputer)
        {
            ClearAll(); // Ẩn hết các chữ F lơ lửng xung quanh (nếu có)
            return;     // Khóa toàn bộ tia quét mắt lại
        }
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
            // 4. XỬ LÝ NHÌN VÀO CỬL KÉO NGANG (TÍNH NĂNG MỚI)
            else if (hit.collider.GetComponentInParent<SmoothSlidingDoor>() != null)
            {
                SmoothSlidingDoor slidingDoor = hit.collider.GetComponentInParent<SmoothSlidingDoor>();

                ClearCurrentDoor();
                ClearCurrentPaper();
                ClearCurrentSingleDoor();

                if (currentSlidingDoor != slidingDoor)
                {
                    if (currentSlidingDoor != null) currentSlidingDoor.HidePrompt();
                    currentSlidingDoor = slidingDoor;
                    currentSlidingDoor.ShowPrompt();
                }
                if (Input.GetKeyDown(KeyCode.F)) currentSlidingDoor.ToggleDoor();
            }
            // XỬ LÝ NHÌN VÀO ITEM NHẶT (TÍNH NĂNG MỚI)
            else if (hit.collider.GetComponentInParent<InteractableItem>() != null)
            {
                InteractableItem item = hit.collider.GetComponentInParent<InteractableItem>();

                ClearCurrentDoor();
                ClearCurrentPaper();
                if (typeof(PlayerInteraction).GetField("currentSingleDoor") != null) ClearCurrentSingleDoor();
                if (typeof(PlayerInteraction).GetField("currentSlidingDoor") != null) ClearCurrentSlidingDoor();

                if (currentItem != item)
                {
                    ClearCurrentItem();
                    currentItem = item;
                    currentItem.ShowPrompt();
                }

                // Bấm F để nhặt item
                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentItem.HidePrompt();
                    currentItem.Pickup();
                    currentItem = null; // Gán về null vì vật thể đã bị Destroy
                }
            }
            // XỬ LÝ NHÌN VÀO XÁC CHẾT
            else if (hit.collider.GetComponentInParent<CorpseLoot>() != null)
            {
                CorpseLoot corpse = hit.collider.GetComponentInParent<CorpseLoot>();

                // Xóa tất cả các focus khác
                ClearCurrentDoor();
                ClearCurrentPaper();
                ClearCurrentItem(); // Hàm dọn item thường của bạn
                if (typeof(PlayerInteraction).GetField("currentSingleDoor") != null) ClearCurrentSingleDoor();
                if (typeof(PlayerInteraction).GetField("currentSlidingDoor") != null) ClearCurrentSlidingDoor();

                if (currentCorpse != corpse)
                {
                    ClearCurrentCorpse();
                    currentCorpse = corpse;
                    currentCorpse.ShowPrompt();
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentCorpse.HidePrompt();
                    currentCorpse.LootCorpse();
                    currentCorpse = null;
                }
            }
            // XỬ LÝ NHÌN VÀO MÁY TÍNH ĐỂ BÀN
            else if (hit.collider.GetComponentInParent<DesktopComputer>() != null)
            {
                DesktopComputer computer = hit.collider.GetComponentInParent<DesktopComputer>();

                ClearAll();

                if (currentComputer != computer)
                {
                    ClearCurrentComputer();
                    currentComputer = computer;
                    currentComputer.ShowPrompt();
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentComputer.Interact();
                    currentComputer = null; // Reset biến tạm
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
    void ClearCurrentComputer()
    {
        if (currentComputer != null) { currentComputer.HidePrompt(); currentComputer = null; }
    }

    // Nhớ kéo thả lệnh ClearCurrentComputer(); vào bên trong hàm ClearAll() tổng của sếp nhé!
    void ClearCurrentDoor()
    {
        if (currentDoor != null) { currentDoor.HidePrompt(); currentDoor = null; }
    }
    void ClearCurrentItem()
    {
        if (currentItem != null) { currentItem.HidePrompt(); currentItem = null; }
    }

    // Nhớ thêm ClearCurrentItem(); vào bên trong hàm ClearAll() cuối script của bạn nhé!
    void ClearCurrentPaper()
    {
        if (currentPaper != null) { currentPaper.HidePrompt(); currentPaper = null; }
    }

    // Hàm dọn dẹp trạng thái cửa đơn
    void ClearCurrentSingleDoor()
    {
        if (currentSingleDoor != null) { currentSingleDoor.HidePrompt(); currentSingleDoor = null; }
    }
    void ClearCurrentSlidingDoor()
    {
        if (currentSlidingDoor != null) { currentSlidingDoor.HidePrompt(); currentSlidingDoor = null; }
    }
    void ClearCurrentCorpse()
    {
        if (currentCorpse != null) 
        { 
            currentCorpse.HidePrompt();
            currentCorpse = null; 
        }
    }
    void ClearAll()
    {
        ClearCurrentDoor();
        ClearCurrentPaper();
        ClearCurrentSingleDoor(); // Thêm vào hàm xóa tổng hợp
        ClearCurrentSlidingDoor();
        ClearCurrentItem();
        ClearCurrentCorpse();
        ClearCurrentComputer();
    }
}