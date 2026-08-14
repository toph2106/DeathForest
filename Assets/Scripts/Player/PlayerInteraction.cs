using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3.5f;
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
    private DoorExit currentDoorExit = null;
    private NPCDialogueCutscene currentNPC = null;
    private InteractableItem currentItem = null;
    private CorpseLoot currentCorpse = null;
    private DesktopComputer currentComputer = null;

    void Update()
    {
        // 1. TẮT TƯƠNG TÁC KHI ĐANG DÙNG MÁY TÍNH HOẶC TRONG BẤT KỲ CUTSCENE/FADE NÀO
        ComputerSystem compSys = Object.FindFirstObjectByType<ComputerSystem>();
        if (compSys != null && compSys.isUsingComputer)
        {
            ClearAll();
            return;
        }

        MovePl movePl = Object.FindFirstObjectByType<MovePl>();
        if (movePl != null && movePl.isCameraLocked)
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

        InteractPro interactPro = GetComponent<InteractPro>();
        float currentRange = (interactPro != null) ? interactPro.interactDistance : interactRange;

        Debug.DrawRay(mainCam.transform.position, mainCam.transform.forward * currentRange, Color.red);

        if (Physics.Raycast(ray, out hit, currentRange, interactableLayer))
        {
            SmoothDoubleDoor door = hit.collider.GetComponentInParent<SmoothDoubleDoor>();
            ReadablePaper paper = hit.collider.GetComponentInParent<ReadablePaper>();
            SmoothSingleDoor singleDoor = hit.collider.GetComponentInParent<SmoothSingleDoor>();
            DoorExit doorExit = hit.collider.GetComponentInParent<DoorExit>();
            if (doorExit == null) doorExit = hit.collider.GetComponent<DoorExit>();

            NPCDialogueCutscene npc = hit.collider.GetComponentInParent<NPCDialogueCutscene>();
            if (npc == null) npc = hit.collider.GetComponent<NPCDialogueCutscene>();

            // 1. Xử lý nhìn vào CỬA ĐÔI
            if (door != null)
            {
                ClearAllExceptDoor();

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
                ClearAllExceptPaper();

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
                    currentPaper.Interact();
                }
            }
            // 3. XỬ LÝ NHÌN VÀO CỬA ĐƠN
            else if (singleDoor != null)
            {
                ClearAllExceptSingleDoor();

                if (currentSingleDoor != singleDoor)
                {
                    ClearCurrentSingleDoor();
                    currentSingleDoor = singleDoor;
                    currentSingleDoor.ShowPrompt();
                    ShowPromptUI("[F] Mở cửa");
                }
                if (Input.GetKeyDown(KeyCode.F)) currentSingleDoor.ToggleDoor();
            }
            // 4. XỬ LÝ NHÌN VÀO CỬA KÉO MAP01 (DOOR EXIT)
            else if (doorExit != null)
            {
                ClearAllExceptDoorExit();

                if (currentDoorExit != doorExit)
                {
                    ClearCurrentDoorExit();
                    currentDoorExit = doorExit;
                    currentDoorExit.ShowPrompt();
                    ShowPromptUI("[F] Mở cửa");
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentDoorExit.Interact();
                }
            }
            // 5. XỬ LÝ NHÌN VÀO CỬA KÉO NGANG (SMOOTH SLIDING DOOR)
            else if (hit.collider.GetComponentInParent<SmoothSlidingDoor>() != null)
            {
                SmoothSlidingDoor slidingDoor = hit.collider.GetComponentInParent<SmoothSlidingDoor>();

                ClearAllExceptSlidingDoor();

                if (currentSlidingDoor != slidingDoor)
                {
                    if (currentSlidingDoor != null) currentSlidingDoor.HidePrompt();
                    currentSlidingDoor = slidingDoor;
                    currentSlidingDoor.ShowPrompt();
                    ShowPromptUI("[F] Mở cửa");
                }
                if (Input.GetKeyDown(KeyCode.F)) slidingDoor.ToggleDoor();
            }
            // 6. XỬ LÝ NHÌN VÀO NPC THOẠI (JOHNSON)
            else if (npc != null)
            {
                ClearAllExceptNPC();

                if (currentNPC != npc)
                {
                    ClearCurrentNPC();
                    currentNPC = npc;
                    currentNPC.ShowPrompt();
                    ShowPromptUI("[F] Nói chuyện");
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    HidePromptUI();
                    currentNPC.Interact();
                    currentNPC = null;
                }
            }
            // 7. XỬ LÝ NHÌN VÀO ITEM NHẶT (VÍ DỤ: HỘP HÀNG / CỤC PIN)
            else if (hit.collider.GetComponentInParent<InteractableItem>() != null || hit.collider.GetComponent<InteractableItem>() != null)
            {
                InteractableItem item = hit.collider.GetComponentInParent<InteractableItem>();
                if (item == null) item = hit.collider.GetComponent<InteractableItem>();

                ClearAllExceptItem();

                if (currentItem != item)
                {
                    ClearCurrentItem();
                    currentItem = item;
                    currentItem.ShowPrompt();

                    string promptMsg = (item != null && item.itemType == InteractableItem.ItemType.Battery) ? "[F] Nhặt Pin" : "[F] Nhặt đồ";
                    ShowPromptUI(promptMsg);
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentItem.HidePrompt();
                    HidePromptUI();
                    currentItem.Pickup();
                    currentItem = null;
                }
            }
            // 8. XỬ LÝ NHÌN VÀO XÁC CHẾT
            else if (hit.collider.GetComponentInParent<CorpseLoot>() != null || hit.collider.GetComponent<CorpseLoot>() != null)
            {
                CorpseLoot corpse = hit.collider.GetComponentInParent<CorpseLoot>();
                if (corpse == null) corpse = hit.collider.GetComponent<CorpseLoot>();

                ClearAllExceptCorpse();

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
            // 9. XỬ LÝ NHÌN VÀO MÁY TÍNH ĐỂ BÀN
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
                else if (text.Contains("Nói chuyện")) text = "[F] Talk";
                else text = "[F] Interact";
            }
            interactText.text = text;
        }
    }

    private void HidePromptUI()
    {
        if (GetComponent<InteractPro>() != null) return;
        if (interactionUI != null) interactionUI.SetActive(false);
    }

    void ClearCurrentComputer() { if (currentComputer != null) { currentComputer.HidePrompt(); currentComputer = null; } }
    void ClearCurrentDoor() { if (currentDoor != null) { currentDoor.HidePrompt(); currentDoor = null; } }
    void ClearCurrentItem() { if (currentItem != null) { currentItem.HidePrompt(); currentItem = null; } }
    void ClearCurrentPaper() { if (currentPaper != null) { currentPaper.HidePrompt(); currentPaper = null; } }
    void ClearCurrentSingleDoor() { if (currentSingleDoor != null) { currentSingleDoor.HidePrompt(); currentSingleDoor = null; } }
    void ClearCurrentSlidingDoor() { if (currentSlidingDoor != null) { currentSlidingDoor.HidePrompt(); currentSlidingDoor = null; } }
    void ClearCurrentDoorExit() { if (currentDoorExit != null) { currentDoorExit.HidePrompt(); currentDoorExit = null; } }
    void ClearCurrentNPC() { if (currentNPC != null) { currentNPC.HidePrompt(); currentNPC = null; } }
    void ClearCurrentCorpse() { if (currentCorpse != null) { currentCorpse.HidePrompt(); currentCorpse = null; } }

    void ClearAllExceptDoor() { ClearCurrentPaper(); ClearCurrentSingleDoor(); ClearCurrentSlidingDoor(); ClearCurrentDoorExit(); ClearCurrentNPC(); ClearCurrentItem(); ClearCurrentCorpse(); ClearCurrentComputer(); }
    void ClearAllExceptPaper() { ClearCurrentDoor(); ClearCurrentSingleDoor(); ClearCurrentSlidingDoor(); ClearCurrentDoorExit(); ClearCurrentNPC(); ClearCurrentItem(); ClearCurrentCorpse(); ClearCurrentComputer(); }
    void ClearAllExceptSingleDoor() { ClearCurrentDoor(); ClearCurrentPaper(); ClearCurrentSlidingDoor(); ClearCurrentDoorExit(); ClearCurrentNPC(); ClearCurrentItem(); ClearCurrentCorpse(); ClearCurrentComputer(); }
    void ClearAllExceptSlidingDoor() { ClearCurrentDoor(); ClearCurrentPaper(); ClearCurrentSingleDoor(); ClearCurrentDoorExit(); ClearCurrentNPC(); ClearCurrentItem(); ClearCurrentCorpse(); ClearCurrentComputer(); }
    void ClearAllExceptDoorExit() { ClearCurrentDoor(); ClearCurrentPaper(); ClearCurrentSingleDoor(); ClearCurrentSlidingDoor(); ClearCurrentNPC(); ClearCurrentItem(); ClearCurrentCorpse(); ClearCurrentComputer(); }
    void ClearAllExceptNPC() { ClearCurrentDoor(); ClearCurrentPaper(); ClearCurrentSingleDoor(); ClearCurrentSlidingDoor(); ClearCurrentDoorExit(); ClearCurrentItem(); ClearCurrentCorpse(); ClearCurrentComputer(); }
    void ClearAllExceptItem() { ClearCurrentDoor(); ClearCurrentPaper(); ClearCurrentSingleDoor(); ClearCurrentSlidingDoor(); ClearCurrentDoorExit(); ClearCurrentNPC(); ClearCurrentCorpse(); ClearCurrentComputer(); }
    void ClearAllExceptCorpse() { ClearCurrentDoor(); ClearCurrentPaper(); ClearCurrentSingleDoor(); ClearCurrentSlidingDoor(); ClearCurrentDoorExit(); ClearCurrentNPC(); ClearCurrentItem(); ClearCurrentComputer(); }

    void ClearAll()
    {
        HidePromptUI();
        ClearCurrentDoor();
        ClearCurrentPaper();
        ClearCurrentSingleDoor();
        ClearCurrentSlidingDoor();
        ClearCurrentDoorExit();
        ClearCurrentNPC();
        ClearCurrentItem();
        ClearCurrentCorpse();
        ClearCurrentComputer();
    }
}