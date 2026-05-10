using UnityEngine;

public class FlashlightPickup : MonoBehaviour
{
    public GameObject heldFlashlight;
    public GameObject interactUI;
    public float rayDistance = 4f;
    public LayerMask interactLayer;

    private bool inRange = false;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (interactUI != null) interactUI.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = true;
            if (interactUI != null)
            {
                interactUI.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = false;
            if (interactUI != null) interactUI.SetActive(false);
        }
    }

    void Update()
    {
        if (!inRange) return;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, interactLayer, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject == gameObject)
            {
                if (interactUI != null) interactUI.SetActive(true);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (heldFlashlight != null) heldFlashlight.SetActive(true);
                    if (interactUI != null) interactUI.SetActive(false);
                    gameObject.SetActive(false);
                }
                return;
            }
        }

        if (interactUI != null) interactUI.SetActive(false);
    }
}