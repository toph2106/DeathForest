using UnityEngine;
using System.Collections;

public class Jumpscare01 : MonoBehaviour
{
    public GameObject monster;
    public Light flickerLight;
    public MovePl playerScript;

    [Header("Flicker Settings")]
    public float flickerSpeed = 0.07f;

    private bool hasTriggered = false;

    void Start()
    {
        if (monster != null)
        {
            monster.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(PlayJumpscare());
        }
    }

    IEnumerator PlayJumpscare()
    {
        if (monster != null)
        {
            monster.SetActive(true);
        }

        if (playerScript != null)
        {
            playerScript.forcedLookTarget = monster.transform;
            playerScript.isCameraLocked = true;
        }

        float timer = 0f;
        while (timer < 2f)
        {
            if (flickerLight != null)
            {
                flickerLight.enabled = !flickerLight.enabled;
            }
            yield return new WaitForSeconds(flickerSpeed);
            timer += flickerSpeed;
        }

        if (monster != null)
        {
            monster.SetActive(false);
        }

        if (flickerLight != null)
        {
            flickerLight.enabled = false;
        }

        if (playerScript != null)
        {
            playerScript.isCameraLocked = false;
            playerScript.forcedLookTarget = null;
        }

        gameObject.SetActive(false);
    }
}