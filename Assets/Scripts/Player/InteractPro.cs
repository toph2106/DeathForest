using UnityEngine;
using System.Collections;

public class InteractPro : MonoBehaviour
{
    [Header("1. Cài Đặt Tương Tác (Distance & Layer)")]
    public float interactDistance = 3.5f;
    public LayerMask interactLayer;

    [Header("2. Tâm Ngắm Tương Tác (Crosshair Icons)")]
    [Tooltip("Kéo GameObject Peroid (chấm tròn) vào đây")]
    public GameObject dotObject;
    [Tooltip("Kéo GameObject Hand (bàn tay) vào đây")]
    public GameObject handObject;

    [Header("3. Thời Gian Chống Spam Click Tương Tác (Cooldown)")]
    [Tooltip("Thời gian vô hiệu hóa click chuột sau khi vừa tương tác (Mặc định: 0.4 giây)")]
    public float interactCooldown = 0.4f;

    [HideInInspector] public GameObject interactionUI;
    [HideInInspector] public TMPro.TextMeshProUGUI interactText;

    private bool isCooldown = false;
    private MovePl cachedMovePl;
    private bool wasDialogueOrCutsceneActive = false;

    void Start()
    {
        // Nếu layer để Nothing (0) thì tự động chuyển sang Everything (~0)
        if (interactLayer.value == 0) interactLayer = ~0;
        else interactLayer |= (1 << 0); // Luôn đảm bảo nhận diện cả layer Default (Layer 0)

        cachedMovePl = Object.FindFirstObjectByType<MovePl>();

        // Trạng thái ban đầu: Hiện chấm tròn, ẩn bàn tay
        if (dotObject != null) dotObject.SetActive(true);
        if (handObject != null) handObject.SetActive(false);
    }

    void Update()
    {
        if (cachedMovePl == null) cachedMovePl = Object.FindFirstObjectByType<MovePl>();

        NPCDialogueCutscene npcCutscene = Object.FindFirstObjectByType<NPCDialogueCutscene>();
        bool isPlayerDisabled = (cachedMovePl != null && (!cachedMovePl.enabled || cachedMovePl.isCameraLocked));
        bool isDialogueActive = SmartInteractionDialogue.isAnyDialoguePlaying ||
                                GameIntroManager.isIntroRunning ||
                                BedSleepCutscene.isSleeping ||
                                InWorldComputerCutscene.isUsingComputer ||
                                (npcCutscene != null && npcCutscene.isInCutscene) ||
                                isPlayerDisabled;

        // NẾU VỪA MỚI CHẠY XONG THOẠI / CẮT CẢNH -> BẮT ĐẦU COOLDOWN NGAY
        if (wasDialogueOrCutsceneActive && !isDialogueActive)
        {
            StartCoroutine(CooldownRoutine());
        }
        wasDialogueOrCutsceneActive = isDialogueActive;

        // 1. KHI ĐANG TRONG THOẠI HOẶC CẮT CẢNH -> KHÓA HOÀN TOÀN TƯƠNG TÁC VÀ ẨN TÂM NGẮM
        if (isDialogueActive)
        {
            ClearSelection();
            if (dotObject != null) dotObject.SetActive(false);
            if (handObject != null) handObject.SetActive(false);
            return;
        }

        // 2. KHI ĐANG HỒI CHIÊU: CHUYỂN BÀN TAY VỀ LẠI CHẤM TRÒN
        if (isCooldown)
        {
            ClearSelection();
            return;
        }

        // BẮT TIA NHÌN THEO CHÍNH XÁC TẦM MẮT CAMERA (GÓC NHÌN CHUỘT LÊN/XUỐNG)
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = GetComponentInChildren<Camera>();
        Transform rayOrigin = (mainCam != null) ? mainCam.transform : transform;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactLayer, QueryTriggerInteraction.Collide);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int hIdx = 0; hIdx < hits.Length; hIdx++)
            {
                RaycastHit currentHit = hits[hIdx];

                // Bỏ qua các Box Collider vùng tường
                if (currentHit.collider.gameObject.name.Contains("CockroachW") && currentHit.collider.isTrigger)
                {
                    continue;
                }

                IInteractable[] interactables = currentHit.collider.GetComponents<IInteractable>();
                if (interactables == null || interactables.Length == 0)
                {
                    interactables = currentHit.collider.GetComponentsInParent<IInteractable>();
                }
                if (interactables == null || interactables.Length == 0)
                {
                    interactables = currentHit.collider.GetComponentsInChildren<IInteractable>();
                }

                if (interactables != null && interactables.Length > 0)
                {
                    // BẬT ICON BÀN TAY, TẮT CHẤM TRÒN (KHÔNG HIỆN CHỮ)
                    if (dotObject != null) dotObject.SetActive(false);
                    if (handObject != null) handObject.SetActive(true);

                    // Bấm Chuột Trái (Mouse 0) để tương tác
                    if (Input.GetMouseButtonDown(0))
                    {
                        Debug.Log("[InteractPro] ⚡ Click Chuột Trái! Tia nhìn trúng: " + currentHit.collider.gameObject.name + " (IInteractable count: " + interactables.Length + ")");

                        StartCoroutine(CooldownRoutine());

                        foreach (IInteractable interactable in interactables)
                        {
                            Debug.Log("[InteractPro] → Gọi Interact() trên: " + interactable.GetType().Name);
                            interactable.Interact();
                        }
                    }
                    return;
                }

                // Nếu chạm vào vật thể đặc cản tầm nhìn mà không có IInteractable -> Dừng tia nhìn
                if (!currentHit.collider.isTrigger)
                {
                    break;
                }
            }
        }

        ClearSelection();
    }

    IEnumerator CooldownRoutine()
    {
        isCooldown = true;
        ClearSelection();
        yield return new WaitForSeconds(interactCooldown);
        isCooldown = false;
    }

    void ClearSelection()
    {
        // TRẢ VỀ: HIỆN CHẤM TRÒN, TẮT BÀN TAY
        if (dotObject != null) dotObject.SetActive(true);
        if (handObject != null) handObject.SetActive(false);
    }
}