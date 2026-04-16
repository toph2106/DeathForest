using UnityEngine;

public class Wheel : MonoBehaviour
{
    public float wheelRadius = 0.35f;
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
            float circumference = 2f * Mathf.PI * wheelRadius;

            float rotationDegrees = (bikeController.CurrentSpeed / circumference) * 360f * Time.deltaTime;
            transform.Rotate(0, rotationDegrees, 0);
        }
    }
}
