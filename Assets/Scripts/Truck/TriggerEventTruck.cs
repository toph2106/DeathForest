using UnityEngine;

public class TriggerEventTruck : MonoBehaviour
{
    public Truck truckScript;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            truckScript.StartTruckSequence();
        }
    }
}