using UnityEngine;

public class FootstepManager : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioSource source;
    public AudioClip walkClip;
    [Range(0f, 1f)] public float volume = 0.5f;

    [Header("Interval Settings")]
    public float walkInterval = 0.5f;
    public float sprintInterval = 0.3f;

    public CharacterController player;
    private float nextSoundTime;

    private Vector3 lastPosition;
    private bool isOnRoad = false;

    void Start()
    {
        if (player == null) player = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Road")) isOnRoad = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Road")) isOnRoad = false;
    }

    void Update()
    {
        Vector3 currentPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 previousPos = new Vector3(lastPosition.x, 0, lastPosition.z);
        float distanceMoved = Vector3.Distance(currentPos, previousPos);

        if (isOnRoad && player.isGrounded)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if ((h != 0 || v != 0) && distanceMoved > 0.01f)
            {
                if (Time.time > nextSoundTime)
                {
                    PlayStepSound();
                    float currentInterval = Input.GetKey(KeyCode.LeftShift) ? sprintInterval : walkInterval;
                    nextSoundTime = Time.time + currentInterval;
                }
            }
        }

        lastPosition = transform.position;
    }

    void PlayStepSound()
    {
        if (walkClip == null) return;
        source.pitch = Random.Range(0.9f, 1.1f);
        source.PlayOneShot(walkClip, volume);
    }
}