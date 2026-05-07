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

    void Start()
    {
        if (player == null) player = GetComponent<CharacterController>();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Grass"))
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (h != 0 || v != 0)
            {
                if (Time.time > nextSoundTime)
                {
                    PlayRandomSound();

                    float currentInterval = Input.GetKey(KeyCode.LeftShift) ? sprintInterval : walkInterval;
                    nextSoundTime = Time.time + currentInterval;
                }
            }
        }
    }

    void PlayRandomSound()
    {
        AudioClip soundToPlay = (Random.value > 0.5f) ? clip1 : clip2;
        if (soundToPlay == null) return;
        source.pitch = Random.Range(0.8f, 1.2f);
        source.PlayOneShot(soundToPlay, volume);
    }
}