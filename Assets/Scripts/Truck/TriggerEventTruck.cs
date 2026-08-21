using UnityEngine;

public class TriggerEventTruck : MonoBehaviour
{
    public static TriggerEventTruck Instance { get; private set; }

    [Header("Setup Xe Tải")]
    [Tooltip("Kéo đối tượng Truck (Car) vào đây để kích hoạt khi người chơi đi vào vùng")]
    public Truck truckScript;

    private bool hasTriggered = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (truckScript == null)
        {
            truckScript = Object.FindFirstObjectByType<Truck>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<MovePl>() != null || other.GetComponent<CharacterController>() != null)
        {
            hasTriggered = true;
            Debug.Log("[TriggerEventTruck] 🚚 Người chơi đã bước vào vùng Trigger! Kích hoạt xe tải...");

            if (truckScript != null)
            {
                truckScript.StartTruckSequence();
            }
            else
            {
                Debug.LogError("[TriggerEventTruck] ⚠️ CHƯA KÉO TRUCK (CAR) VÀO Ô TRUCK SCRIPT CỦA TRUCKTRIGGER!");
            }
        }
    }

    public void ResetTriggerState()
    {
        hasTriggered = false;
    }

    public static void ResetAllTriggers()
    {
        TriggerEventTruck[] triggers = Object.FindObjectsByType<TriggerEventTruck>(FindObjectsSortMode.None);
        foreach (var t in triggers)
        {
            if (t != null) t.hasTriggered = false;
        }
    }
}