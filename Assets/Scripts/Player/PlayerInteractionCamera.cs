using UnityEngine;

public class PlayerInteractionCamera : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 3f;
    public LayerMask interactableLayer;

    private CameraObjectPickup currentCameraItem = null;

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            CameraObjectPickup cameraItem = hit.collider.GetComponentInParent<CameraObjectPickup>();

            if (cameraItem != null)
            {
                if (currentCameraItem != cameraItem)
                {
                    if (currentCameraItem != null) currentCameraItem.HidePrompt();
                    currentCameraItem = cameraItem;
                    currentCameraItem.ShowPrompt();
                }

                // Khi bấm F thì nhặt máy quay
                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentCameraItem.HidePrompt();
                    currentCameraItem.Pickup();
                    currentCameraItem = null;
                }
            }
            else
            {
                ClearSelection();
            }
        }
        else
        {
            ClearSelection();
        }
    }

    void ClearSelection()
    {
        if (currentCameraItem != null)
        {
            currentCameraItem.HidePrompt();
            currentCameraItem = null;
        }
    }
}