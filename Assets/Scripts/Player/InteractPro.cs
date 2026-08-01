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
    [Tooltip("Thời gian tạm ẩn chữ [F] và vô hiệu hóa bấm phím F sau khi tương tác (Mặc định: 3.0 giây)")]
    public float interactCooldown = 3.0f;

    private InteractPrompt currentPromptComp = null;
    private bool isCooldown = false;

    void Start()
    {
        SettingsManager.onLanguageChanged += UpdateCurrentPromptText;
    }

    void OnDestroy()
    {
        SettingsManager.onLanguageChanged -= UpdateCurrentPromptText;
    }

    void Update()
    {
        // 1. TỰ ĐỘNG ẨN GIAO DIỆN [F] KHI ĐANG COOLDOWN (3s) HOẶC ĐANG XEM MÁY TÍNH 3D (CUTSCENE)
        if (isCooldown || InWorldComputerCutscene.isUsingComputer)
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

            if (interactables != null && interactables.Length > 0)
            {
                // Bật UI hiển thị chữ [F]
                if (interactionUI != null) interactionUI.SetActive(true);

                // Tìm script InteractPrompt trên vật thể
                InteractPrompt promptComp = hit.collider.GetComponent<InteractPrompt>();
                if (promptComp == null) promptComp = hit.collider.GetComponentInParent<InteractPrompt>();

                if (currentPromptComp != promptComp)
                {
                    if (currentPromptComp != null) currentPromptComp.HidePrompt();
                    currentPromptComp = promptComp;
                    if (currentPromptComp != null) currentPromptComp.ShowPrompt();
                }

                UpdateCurrentPromptText();

                // Bấm phím F để tương tác (Tự động kích hoạt Cooldown 3s chống spam)
                if (Input.GetKeyDown(KeyCode.F))
                {
                    StartCoroutine(CooldownRoutine());

                    foreach (IInteractable interactable in interactables)
                    {
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