using UnityEngine;

public class AmbientManager : MonoBehaviour
{
    public AudioSource source;
    public float targetVolume = 0.5f;
    public float fadeSpeed = 0.5f;

    void Start()
    {
        source.volume = 0;
    }

    void Update()
    {
        if (source.volume < targetVolume)
        {
            source.volume += fadeSpeed * Time.deltaTime;
        }
    }
}