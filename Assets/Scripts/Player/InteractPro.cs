using UnityEngine;
using TMPro; 

public class InteractPro : MonoBehaviour
{
    public float interactDistance = 3.5f;
    public LayerMask interactLayer;

    public GameObject interactionUI;      
    public TextMeshProUGUI interactText; 

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (interactionUI != null) interactionUI.SetActive(true);
                if (Input.GetKeyDown(KeyCode.F))
                {
                    interactable.Interact();
                }
                return;
            }
        }
        if (interactionUI != null) interactionUI.SetActive(false);
    }
}