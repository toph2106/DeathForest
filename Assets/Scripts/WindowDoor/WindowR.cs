using UnityEngine;

public class WindowR : MonoBehaviour, IInteractable
{
    public float slideDistance = 1.5f;
    public float slideSpeed = 5f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private Vector3 targetPos;
    private bool isOpen = false;

    void Start()
    {
        closedPos = transform.position;

        openPos = closedPos + (transform.right * slideDistance);

        targetPos = closedPos;
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * slideSpeed);
    }

    public void Interact()
    {
        isOpen = !isOpen;
        targetPos = isOpen ? openPos : closedPos;
    }
}