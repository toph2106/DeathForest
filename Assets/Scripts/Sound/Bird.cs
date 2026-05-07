using UnityEngine;
using System.Collections;
public class Bird : MonoBehaviour
{
    [Header("Cài đặt Audio")]
    public AudioSource source;
    public AudioClip clip1;
    public AudioClip clip2;
    public AudioClip clip3;

    [Header("Thời gian chờ (Giây)")]
    public float minWait = 15f;
    public float maxWait = 45f;

    [Header("Độ to nhỏ")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    void Start()
    {
        StartCoroutine(PlaySoundLoop());
    }

    IEnumerator PlaySoundLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(minWait, maxWait);
            yield return new WaitForSeconds(waitTime);

            AudioClip selectedClip = null;
            int rand = Random.Range(0, 3);

            if (rand == 0) selectedClip = clip1;
            else if (rand == 1) selectedClip = clip2;
            else selectedClip = clip3;

            if (selectedClip != null)
            {
                source.pitch = Random.Range(0.9f, 1.1f);
                source.PlayOneShot(selectedClip, volume);
            }
        }
    }
}
