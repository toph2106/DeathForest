using UnityEngine;

public class Grass : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioSource source;
    public AudioClip clip1;
    public AudioClip clip2;
    [Range(0f, 1f)] public float volume = 0.5f;

    [Header("Interval Settings")]
    public float walkInterval = 0.4f;
    public float sprintInterval = 0.2f;

    public CharacterController player;
    private float nextSoundTime;

    private Vector3 lastPosition;

    public bool isOnGrass = false;

    void Start()
    {
        if (player == null) player = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Grass")) isOnGrass = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Grass")) isOnGrass = false;
    }

    void Update()
    {
        Vector3 currentPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 previousPos = new Vector3(lastPosition.x, 0, lastPosition.z);
        float distanceMoved = Vector3.Distance(currentPos, previousPos);

        if (isOnGrass)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if ((h != 0 || v != 0) && distanceMoved > 0.01f)
            {
                if (Time.time > nextSoundTime)
                {
                    PlayRandomSound();

                    float currentInterval = Input.GetKey(KeyCode.LeftShift) ? sprintInterval : walkInterval;
                    nextSoundTime = Time.time + currentInterval;
                }
            }
        }
        lastPosition = transform.position;
    }

    void PlayRandomSound()
    {
        AudioClip soundToPlay = (Random.value > 0.5f) ? clip1 : clip2;
        if (soundToPlay == null) return;
        source.pitch = Random.Range(0.8f, 1.2f);
        source.PlayOneShot(soundToPlay, volume);
    }
}