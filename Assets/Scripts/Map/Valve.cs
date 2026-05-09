using UnityEngine;

public class ValveController : MonoBehaviour
{
    public AudioSource generatorSound;
    public GameObject powerGroup;
    public KeyCode interactKey = KeyCode.E;

    [Header("Raycast Settings")]
    public float interactDistance = 3f;
    public LayerMask valveLayer;
    public GameObject interactUI;

    private bool isActivated = false;
    private bool playerInRange = false;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        if (powerGroup != null)
        {
            powerGroup.SetActive(false);
        }

        if (interactUI != null)
        {
            interactUI.SetActive(false);
        }
    }

    void Update()
    {
        if (isActivated) return;

        bool isLookingAtValve = false;

        if (playerInRange)
        {
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactDistance, valveLayer))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    isLookingAtValve = true;
                }
            }
        }

        if (interactUI != null)
        {
            interactUI.SetActive(isLookingAtValve);
        }

        if (isLookingAtValve && Input.GetKeyDown(interactKey))
        {
            ActivateGenerator();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactUI != null) interactUI.SetActive(false);
        }
    }

    void ActivateGenerator()
    {
        isActivated = true;

        if (interactUI != null)
        {
            interactUI.SetActive(false);
        }

        if (generatorSound != null)
        {
            generatorSound.Play();
        }

        if (powerGroup != null)
        {
            powerGroup.SetActive(true);
        }
    }
}