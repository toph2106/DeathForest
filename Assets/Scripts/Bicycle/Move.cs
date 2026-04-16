using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Move : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float turnSpeed = 60f;
    public float acceleration = 3f;
    public float leanAmount = 10f;

    public float CurrentSpeed { get; private set; }
    private Rigidbody rb;
    private float currentLeanAngle = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    void Update()
    {
        float verticalInput = Input.GetAxis("Vertical");
        if (verticalInput < 0) verticalInput = 0;

        float horizontalInput = Input.GetAxis("Horizontal");

        float targetSpeed = verticalInput * moveSpeed;
        CurrentSpeed = Mathf.Lerp(CurrentSpeed, targetSpeed, Time.deltaTime * acceleration);

        if (Mathf.Abs(CurrentSpeed) > 0.1f)
        {
            float turnDirection = Mathf.Sign(CurrentSpeed);
            transform.Rotate(0, horizontalInput * turnSpeed * turnDirection * Time.deltaTime, 0);
        }

        currentLeanAngle = Mathf.Lerp(currentLeanAngle, -horizontalInput * leanAmount, Time.deltaTime * acceleration);

        transform.localRotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, currentLeanAngle);
    }

    void FixedUpdate()
    {
        Vector3 moveVelocity = transform.forward * CurrentSpeed;
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
    }
}