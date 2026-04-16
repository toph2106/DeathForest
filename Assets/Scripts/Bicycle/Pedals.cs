using UnityEngine;

public class Pedals : MonoBehaviour
{
    public float gearRatio = 1.5f;
    public bool reverseRotation = false;

    private Move bikeController;

    void Start()
    {
        bikeController = GetComponentInParent<Move>();
    }

    void Update()
    {
        if (bikeController != null)
        {
            float rotationDegrees = bikeController.CurrentSpeed * gearRatio * 50f * Time.deltaTime;

            if (reverseRotation)
            {
                rotationDegrees = -rotationDegrees;
            }

            transform.Rotate(rotationDegrees, 0, 0);
        }
    }
}