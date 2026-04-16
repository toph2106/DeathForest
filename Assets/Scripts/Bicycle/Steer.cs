using UnityEngine;

public class Steer : MonoBehaviour
{
    public float maxSteerAngle = 35f;

    public enum SteerAxis { X, Y, Z }
    public SteerAxis rotationAxis = SteerAxis.Y;

    public bool reverseSteer = false;

    // Thay Vector3 bằng Quaternion để Unity không bị loạn trục và văng vị trí
    private Quaternion initialRotation;

    void Start()
    {
        // Ghi nhớ chính xác góc xoay 3D ban đầu
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");

        float steerAngle = horizontalInput * maxSteerAngle;
        if (reverseSteer) steerAngle = -steerAngle;

        Quaternion steerRot = Quaternion.identity;

        // Bẻ lái bằng Quaternion
        switch (rotationAxis)
        {
            case SteerAxis.X: steerRot = Quaternion.Euler(steerAngle, 0, 0); break;
            case SteerAxis.Y: steerRot = Quaternion.Euler(0, steerAngle, 0); break;
            case SteerAxis.Z: steerRot = Quaternion.Euler(0, 0, steerAngle); break;
        }

        // Nhân góc gốc với góc bẻ lái để khóa cứng vị trí, chỉ cho phép xoay
        transform.localRotation = initialRotation * steerRot;
    }
}