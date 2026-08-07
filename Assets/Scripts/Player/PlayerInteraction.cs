using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3f;
    public LayerMask interactableLayer;

    [Header("References")]
    public PaperReaderManager readManager; // Kéo Object chứa script PaperReaderManager vào đây

    [Header("UI Gợi Ý [F] Tương Tác (Kéo vào đây để đảm bảo 100% hiển thị)")]
    [Tooltip("Kéo GameObject UI chứa chữ [F] (VD: PressF hoặc InteractionUI) vào đây")]
    public GameObject interactionUI;

    [Tooltip("Kéo TextMeshProUGUI hiển thị nội dung chữ (VD: [F] Tương tác) vào đây")]
    public TextMeshProUGUI interactText;

    private SmoothDoubleDoor currentDoor = null;
    private ReadablePaper currentPaper = null;
    private SmoothSingleDoor currentSingleDoor = null;
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
            ClearAll();
            return;
        }

        // QUAN TRỌNG: Nếu đang đọc giấy thì khóa tia nhìn lại, không cho tương tác linh tinh
        if (readManager != null && readManager.isReading) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // BẮT BẰNG TIA NHÌN CỦA CAMERA CHÍNH XÁC THEO HƯỚNG TẦM MẮT CHUỘT
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
        RaycastHit hit;

        Debug.DrawRay(mainCam.transform.position, mainCam.transform.forward * interactRange, Color.red);

        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            SmoothDoubleDoor door = hit.collider.GetComponentInParent<SmoothDoubleDoor>();
            ReadablePaper paper = hit.collider.GetComponentInParent<ReadablePaper>();
            SmoothSingleDoor singleDoor = hit.collider.GetComponentInParent<SmoothSingleDoor>();

            // 1. Xử lý nhìn vào CỬA ĐÔI
            if (door != null)
            {
                ClearCurrentPaper();
                ClearCurrentSingleDoor();

                if (currentDoor != door)
                {
                    ClearCurrentDoor();
                    currentDoor = door;
                    currentDoor.ShowPrompt();
                    ShowPromptUI("[F] Mở cửa");
                }
                if (Input.GetKeyDown(KeyCode.F)) currentDoor.ToggleDoor();
            }
            // 2. Xử lý nhìn vào GIẤY
            else if (paper != null)
            {
                ClearCurrentDoor();
                ClearCurrentSingleDoor();

                if (currentPaper != paper)
                {
                    ClearCurrentPaper();
                    currentPaper = paper;
                    currentPaper.ShowPrompt();
                    ShowPromptUI("[F] Đọc tài liệu");
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentPaper.HidePrompt();
                    HidePromptUI();
                    if (readManager == null) return;
                    readManager.StartReading(currentPaper.gameObject, currentPaper.content);
                }
            }
            // 3. XỬ LÝ NHÌN VÀO CỬA ĐƠN
            else if (singleDoor != null)
            {
                ClearCurrentDoor();
                ClearCurrentPaper();

                if (currentSingleDoor != singleDoor)
                {
                    ClearCurrentSingleDoor();
                    currentSingleDoor = singleDoor;
                    currentSingleDoor.ShowPrompt();
                    ShowPromptUI("[F] Mở cửa");
                }
                if (Input.GetKeyDown(KeyCode.F)) currentSingleDoor.ToggleDoor();
            }
            // 4. XỬ LÝ NHÌN VÀO CỬA KÉO NGANG
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
                    ShowPromptUI("[F] Mở cửa");
                }
                if (Input.GetKeyDown(KeyCode.F)) slidingDoor.ToggleDoor();
            }
            // 5. XỬ LÝ NHÌN VÀO ITEM NHẶT (VÍ DỤ: CỤC PIN)
            else if (hit.collider.GetComponentInParent<InteractableItem>() != null || hit.collider.GetComponent<InteractableItem>() != null)
            {
                InteractableItem item = hit.collider.GetComponentInParent<InteractableItem>();
                if (item == null) item = hit.collider.GetComponent<InteractableItem>();

                ClearCurrentDoor();
                ClearCurrentPaper();
                ClearCurrentSingleDoor();
                ClearCurrentSlidingDoor();

                if (currentItem != item)
                {
                    ClearCurrentItem();
                    currentItem = item;
                    currentItem.ShowPrompt();

                    string promptMsg = (item != null && item.itemType == InteractableItem.ItemType.Battery) ? "[F] Nhặt Pin" : "[F] Nhặt đồ";
                    ShowPromptUI(promptMsg);
                }

                // Bấm F để nhặt item
                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentItem.HidePrompt();
                    HidePromptUI();
                    currentItem.Pickup();
                    currentItem = null;
                }
            }
            // 6. XỬ LÝ NHÌN VÀO XÁC CHẾT
            else if (hit.collider.GetComponentInParent<CorpseLoot>() != null || hit.collider.GetComponent<CorpseLoot>() != null)
            {
                CorpseLoot corpse = hit.collider.GetComponentInParent<CorpseLoot>();
                if (corpse == null) corpse = hit.collider.GetComponent<CorpseLoot>();

                ClearCurrentDoor();
                ClearCurrentPaper();
                ClearCurrentItem();
                ClearCurrentSingleDoor();
                ClearCurrentSlidingDoor();

                if (currentCorpse != corpse)
                {
                    ClearCurrentCorpse();
                    currentCorpse = corpse;
                    currentCorpse.ShowPrompt();
                    ShowPromptUI("[F] Kiểm tra xác");
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentCorpse.HidePrompt();
                    HidePromptUI();
                    currentCorpse.LootCorpse();
                    currentCorpse = null;
                }
            }
            // 7. XỬ LÝ NHÌN VÀO MÁY TÍNH ĐỂ BÀN
            else if (hit.collider.GetComponentInParent<DesktopComputer>() != null)
            {
                DesktopComputer computer = hit.collider.GetComponentInParent<DesktopComputer>();

                ClearAll();

                if (currentComputer != computer)
                {
                    ClearCurrentComputer();
                    currentComputer = computer;
                    currentComputer.ShowPrompt();
                    ShowPromptUI("[F] Sử dụng máy tính");
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    HidePromptUI();
                    currentComputer.Interact();
                    currentComputer = null;
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

    private void ShowPromptUI(string text = "[F] Tương tác")
    {
        if (interactionUI != null) interactionUI.SetActive(true);
        if (interactText != null)
        {
            string lang = PlayerPrefs.GetString("Language", SettingsManager.currentLanguage);
            if (lang == "EN")
            {
                if (text.Contains("Nhặt Pin")) text = "[F] Pick up Battery";
                else if (text.Contains("Nhặt đồ")) text = "[F] Pick up Item";
                else if (text.Contains("Đọc tài liệu")) text = "[F] Read Note";
                else if (text.Contains("Mở cửa")) text = "[F] Open Door";
                else if (text.Contains("Kiểm tra xác")) text = "[F] Examine Corpse";
                else if (text.Contains("Sử dụng máy tính")) text = "[F] Use Computer";
                else text = "[F] Interact";
            }
            interactText.text = text;
        }
    }

    private void HidePromptUI()
    {
        if (interactionUI != null) interactionUI.SetActive(false);
    }

    void ClearCurrentComputer()
    {
        if (currentComputer != null) { currentComputer.HidePrompt(); currentComputer = null; }
    }

    void ClearCurrentDoor()
    {
        if (currentDoor != null) { currentDoor.HidePrompt(); currentDoor = null; }
    }

    void ClearCurrentItem()
    {
        if (currentItem != null) { currentItem.HidePrompt(); currentItem = null; }
    }

    void ClearCurrentPaper()
    {
        if (currentPaper != null) { currentPaper.HidePrompt(); currentPaper = null; }
    }

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
        HidePromptUI();
        ClearCurrentDoor();
        ClearCurrentPaper();
        ClearCurrentSingleDoor();
        ClearCurrentSlidingDoor();
        ClearCurrentItem();
        ClearCurrentCorpse();
        ClearCurrentComputer();
    }
}