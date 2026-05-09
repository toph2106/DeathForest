using UnityEngine;
using System.Collections;

public class FlickerLight : MonoBehaviour
{
    public Light lightSource;

    [Header("Tốc độ nháy (Giây)")]
    public float minTime = 0.05f;
    public float maxTime = 0.3f;

    [Header("Thời gian nghỉ (Giây)")]
    public float minStayTime = 1f;
    public float maxStayTime = 5f;

    void Start()
    {
        if (lightSource == null) lightSource = GetComponent<Light>();
        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            int flickerCount = Random.Range(3, 8);
            for (int i = 0; i < flickerCount; i++)
            {
                lightSource.enabled = !lightSource.enabled;
                yield return new WaitForSeconds(Random.Range(minTime, maxTime));
            }
            lightSource.enabled = (Random.value > 0.5f);

            yield return new WaitForSeconds(Random.Range(minStayTime, maxStayTime));
        }
    }
}