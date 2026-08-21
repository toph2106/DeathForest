using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3.5f;
    public LayerMask interactableLayer;

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
                }
                if (Input.GetMouseButtonDown(0)) currentDoor.ToggleDoor();
            }
            // 2. Xử lý nhìn vào GIẤY
            else if (paper != null)
            {
                ClearAllExceptPaper();

                if (currentPaper != paper)
                {
                    ClearCurrentPaper();
                    currentPaper = paper;
                }

                if (Input.GetMouseButtonDown(0))
                {
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
                }
                if (Input.GetMouseButtonDown(0)) currentSingleDoor.ToggleDoor();
            }
            // 4. XỬ LÝ NHÌN VÀO CỬA THOÁT RA MAP TIẾP THEO (DOOR EXIT)
            else if (doorExit != null)
            {
                ClearAllExceptDoorExit();

                if (currentDoorExit != doorExit)
                {
                    ClearCurrentDoorExit();
                    currentDoorExit = doorExit;
                }

                if (Input.GetMouseButtonDown(0))
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
                }
                if (Input.GetMouseButtonDown(0)) slidingDoor.ToggleDoor();
            }
            // 6. XỬ LÝ NHÌN VÀO NPC THOẠI
            else if (npc != null)
            {
                ClearAllExceptNPC();

                if (currentNPC != npc)
                {
                    ClearCurrentNPC();
                    currentNPC = npc;
                }

                if (Input.GetMouseButtonDown(0))
                {
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
                }

                if (Input.GetMouseButtonDown(0))
                {
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
                }

                if (Input.GetMouseButtonDown(0))
                {
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
                }

                if (Input.GetMouseButtonDown(0))
                {
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