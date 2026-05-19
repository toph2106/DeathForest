using UnityEngine;

public class CameraMenuMove : MonoBehaviour
{
    public float moveAmount = 0.5f;
    public float smoothSpeed = 5f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        
        float mouseX = (Input.mousePosition.x / Screen.width) - 0.5f;
        float mouseY = (Input.mousePosition.y / Screen.height) - 0.5f;

       
        Vector3 targetPos = startPos + new Vector3(
            mouseX * moveAmount,
            mouseY * moveAmount,
            0
        );

        
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );
    }
}