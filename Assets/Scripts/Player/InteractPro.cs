using UnityEngine;
using System.Collections;
using TMPro; 

public class InteractPro : MonoBehaviour
{
    public float interactDistance = 3.5f;
    public LayerMask interactLayer;

    public GameObject interactionUI;      
    public TextMeshProUGUI interactText; 

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
    }

    void OnDestroy()
    {
        SettingsManager.onLanguageChanged -= UpdateCurrentPromptText;
    }

    void Update()
    {
        if (cachedMovePl == null) cachedMovePl = Object.FindFirstObjectByType<MovePl>();

        // 1. TỰ ĐỘNG ẨN GIAO DIỆN [F] KHI ĐANG COOLDOWN, ĐANG DÙNG PC, HOẶC TRONG BẤT KỲ CUTSCENE/FADE NÀO (CAMERA LOCKED)
        if (isCooldown || InWorldComputerCutscene.isUsingComputer || (cachedMovePl != null && cachedMovePl.isCameraLocked))
        {
            ClearSelection();
            return;
        }

        // Nếu đang trong cutscene thoại NPC thì không quét tia
        NPCDialogueCutscene npcCutscene = Object.FindFirstObjectByType<NPCDialogueCutscene>();
        if (npcCutscene != null && npcCutscene.isInCutscene)
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
                // Bật UI hiển thị chữ [F]
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

                // Bấm phím F để tương tác
                if (Input.GetKeyDown(KeyCode.F))
                {
                    Debug.Log("[InteractPro] ⚡ Bấm F! Tia nhìn trúng: " + hit.collider.gameObject.name + " (IInteractable count: " + interactables.Length + ")");

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
            interactText.text = (SettingsManager.currentLanguage == "VI") ? "[F] Tương tác" : "[F] Interact";
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
    }
}