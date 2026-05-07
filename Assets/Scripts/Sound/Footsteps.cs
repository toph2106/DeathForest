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

    void Start()
    {
        if (player == null) player = GetComponent<CharacterController>();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Road"))
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (h != 0 || v != 0)
            {
                if (Time.time > nextSoundTime)
                {
                    PlayStepSound();
                    float currentInterval = Input.GetKey(KeyCode.LeftShift) ? sprintInterval : walkInterval;
                    nextSoundTime = Time.time + currentInterval;
                }
            }
        }
    }

    void PlayStepSound()
    {
        if (walkClip == null) return;
        source.pitch = Random.Range(0.9f, 1.1f);
        source.PlayOneShot(walkClip, volume);
    }
}