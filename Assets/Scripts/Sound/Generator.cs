using UnityEngine;

public class Generator : MonoBehaviour
{
    public AudioSource source;
    public AudioClip runClip;

    private bool isRunning = false;

    public void StartMachine()
    {
        if (isRunning) return;

        isRunning = true;

        source.clip = runClip;
        source.loop = true;
        source.Play();
    }
}
