using UnityEngine;

public class Cat : MonoBehaviour, IInteractable
{
    private Animator catAnimator;
    private Transform playerTransform;

    public float rotationSpeed = 5f;

    void Start()
    {
        catAnimator = GetComponent<Animator>();

        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (playerTransform != null)
        {
            Vector3 direction = playerTransform.position - transform.position;

            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    public void Interact()
    {
        if (catAnimator != null)
        {
            catAnimator.SetTrigger("PlayAnim");
        }
    }
}