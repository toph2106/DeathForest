using UnityEngine;
using System.Collections;

public class Truck : MonoBehaviour
{
    public float speed = 15f;
    public float waitTime = 1f;
    public Transform player;
    public Animator eyeAnimator;
    public AudioSource engineSound;
    public AudioSource impactSound;
    public AudioSource rezeroSound;
    public Light leftHeadlight;
    public Light rightHeadlight;
    public Light bounceLight;

    private Vector3 startPos;
    private Quaternion startRot;
    private bool isRunning = false;
    private bool hasTriggered = false;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;

        if (leftHeadlight != null) leftHeadlight.enabled = false;
        if (rightHeadlight != null) rightHeadlight.enabled = false;
        if (bounceLight != null) bounceLight.enabled = false;
    }

    public void StartTruckSequence()
    {
        if (!hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(EngineStartup());
        }
    }

    IEnumerator EngineStartup()
    {
        if (leftHeadlight != null) leftHeadlight.enabled = true;
        if (rightHeadlight != null) rightHeadlight.enabled = true;
        if (bounceLight != null) bounceLight.enabled = true;

        if (engineSound != null)
        {
            engineSound.Play();
        }

        yield return new WaitForSeconds(waitTime);
        isRunning = true;
    }

    void Update()
    {
        if (isRunning && player != null)
        {
            Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ExecuteDeath();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            ExecuteDeath();
        }
    }

    void ExecuteDeath()
    {
        isRunning = false;

        if (engineSound != null && engineSound.isPlaying)
        {
            engineSound.Stop();
        }

        if (impactSound != null)
        {
            impactSound.Play();
        }

        transform.position = startPos;
        transform.rotation = startRot;

        if (leftHeadlight != null) leftHeadlight.enabled = false;
        if (rightHeadlight != null) rightHeadlight.enabled = false;
        if (bounceLight != null) bounceLight.enabled = false;

        StartCoroutine(EyeCanvasRoutine());
    }

    IEnumerator EyeCanvasRoutine()
    {
        if (eyeAnimator != null)
        {
            eyeAnimator.gameObject.SetActive(true);
            eyeAnimator.Play("EyeOpen", -1, 0f);
        }

        yield return new WaitForSeconds(2f);

        if (rezeroSound != null)
        {
            rezeroSound.Play();
        }

        yield return new WaitForSeconds(9f);

        if (eyeAnimator != null)
        {
            eyeAnimator.gameObject.SetActive(false);
        }

        hasTriggered = false;
    }
}