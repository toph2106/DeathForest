using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3f; 
    public LayerMask interactableLayer; 

    private SmoothDoubleDoor currentDoor = null;

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            SmoothDoubleDoor door = hit.collider.GetComponentInParent<SmoothDoubleDoor>();

            if (door != null)
            {
                if (currentDoor != door)
                {
                    if (currentDoor != null) currentDoor.HidePrompt();
                    currentDoor = door;
                    currentDoor.ShowPrompt();
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentDoor.ToggleDoor();
                }
            }
            else
            {
                ClearCurrentDoor();
            }
        }
        else
        {
            ClearCurrentDoor();
        }
    }

    void ClearCurrentDoor()
    {
        if (currentDoor != null)
        {
            currentDoor.HidePrompt();
            currentDoor = null;
        }
    }
}