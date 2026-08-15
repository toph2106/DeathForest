using UnityEngine;
using System.Collections;
using TMPro; 

public class InteractPro : MonoBehaviour
{
    public float interactDistance = 3.5f;
    public LayerMask interactLayer;

    public GameObject interactionUI;      
    public TextMeshProUGUI interactText; 

    [Header("Tâm Ngắm Tương Tác (Crosshair Icons)")]
    [Tooltip("Kéo GameObject Peroid (chấm tròn) vào đây")]
    public GameObject dotObject;
    [Tooltip("Kéo GameObject Hand (bàn tay) vào đây")]
    public GameObject handObject;

    [Header("Chống Spam Phím F (Cooldown Settings)")]
    [Tooltip("Thời gian tạm ẩn chữ [F] và vô hiệu hóa bấm phím F sau khi tương tác (Mặc định: 0.4 giây)")]
    public float interactCooldown = 0.4f;

    private InteractPrompt currentPromptComp = null;
    private bool isCooldown = false;
    private MovePl cachedMovePl;

    void Start()
    {
        // Nếu layer để Nothing (0) thì tự động chuyển sang Everything (~0)
        if (interactLayer.value == 0) interactLayer = ~0;

        cachedMovePl = Object.FindFirstObjectByType<MovePl>();

        // Tự động tìm UI nếu chưa kéo
        if (interactionUI == null)
        {
            GameObject pressF = GameObject.Find("PressF");
            if (pressF != null) interactionUI = pressF;
        }

        if (interactText == null && interactionUI != null)
        {
            interactText = interactionUI.GetComponentInChildren<TextMeshProUGUI>();
        }

        SettingsManager.onLanguageChanged += UpdateCurrentPromptText;

        // Trạng thái ban đầu: Hiện chấm tròn, ẩn bàn tay
        if (dotObject != null) dotObject.SetActive(true);
        if (handObject != null) handObject.SetActive(false);
    }

    void OnDestroy()
    {
        SettingsManager.onLanguageChanged -= UpdateCurrentPromptText;
    }

    void Update()
    {
        if (cachedMovePl == null) cachedMovePl = Object.FindFirstObjectByType<MovePl>();

        // 1. TỰ ĐỘNG ẨN HOÀN TOÀN TÂM NGẮM KHI ĐANG DÙNG PC HOẶC CAMERA BỊ KHÓA TRONG CUTSCENE
        if (InWorldComputerCutscene.isUsingComputer || (cachedMovePl != null && cachedMovePl.isCameraLocked))
        {
            ClearSelection();
            if (dotObject != null) dotObject.SetActive(false);
            if (handObject != null) handObject.SetActive(false);
            return;
        }

        // Nếu đang trong cutscene thoại NPC thì ẩn hoàn toàn tâm ngắm
        NPCDialogueCutscene npcCutscene = Object.FindFirstObjectByType<NPCDialogueCutscene>();
        if (npcCutscene != null && npcCutscene.isInCutscene)
        {
            ClearSelection();
            if (dotObject != null) dotObject.SetActive(false);
            if (handObject != null) handObject.SetActive(false);
            return;
        }

        // 2. KHI ĐANG HỒI CHIÊU (VỪA TƯƠNG TÁC XONG): CHUYỂN BÀN TAY VỀ LẠI CHẤM TRÒN (KHÔNG MẤT TÂM)
        if (isCooldown)
        {
            ClearSelection();
            return;
        }

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            IInteractable[] interactables = hit.collider.GetComponents<IInteractable>();
            if (interactables == null || interactables.Length == 0)
            {
                interactables = hit.collider.GetComponentsInParent<IInteractable>();
            }
            if (interactables == null || interactables.Length == 0)
            {
                interactables = hit.collider.GetComponentsInChildren<IInteractable>();
            }

            if (interactables != null && interactables.Length > 0)
            {
                // BẬT ICON BÀN TAY, TẮT CHẤM TRÒN
                if (dotObject != null) dotObject.SetActive(false);
                if (handObject != null) handObject.SetActive(true);

                // Bật UI hiển thị chữ [F] (nếu có dùng)
                if (interactionUI != null) interactionUI.SetActive(true);

                // Tìm script InteractPrompt trên vật thể
                InteractPrompt promptComp = hit.collider.GetComponent<InteractPrompt>();
                if (promptComp == null) promptComp = hit.collider.GetComponentInParent<InteractPrompt>();
                if (promptComp == null) promptComp = hit.collider.GetComponentInChildren<InteractPrompt>();

                if (currentPromptComp != promptComp)
                {
                    if (currentPromptComp != null) currentPromptComp.HidePrompt();
                    currentPromptComp = promptComp;
                    if (currentPromptComp != null) currentPromptComp.ShowPrompt();
                }

                UpdateCurrentPromptText();

                // Bấm Chuột Trái (Mouse 0) để tương tác
                if (Input.GetMouseButtonDown(0))
                {
                    Debug.Log("[InteractPro] ⚡ Click Chuột Trái! Tia nhìn trúng: " + hit.collider.gameObject.name + " (IInteractable count: " + interactables.Length + ")");

                    StartCoroutine(CooldownRoutine());

                    foreach (IInteractable interactable in interactables)
                    {
                        Debug.Log("[InteractPro] → Gọi Interact() trên: " + interactable.GetType().Name);
                        interactable.Interact();
                    }
                }
                return;
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

    void UpdateCurrentPromptText()
    {
        if (interactText == null) return;

        if (currentPromptComp != null)
        {
            interactText.text = currentPromptComp.GetPrompt();
        }
        else
        {
            interactText.text = (SettingsManager.currentLanguage == "VI") ? "Tương tác" : "Interact";
        }
    }

    void ClearSelection()
    {
        if (currentPromptComp != null)
        {
            currentPromptComp.HidePrompt();
            currentPromptComp = null;
        }

        if (interactionUI != null) interactionUI.SetActive(false);

        // TRẢ VỀ: HIỆN CHẤM TRÒN, TẮT BÀN TAY
        if (dotObject != null) dotObject.SetActive(true);
        if (handObject != null) handObject.SetActive(false);
    }
}