using UnityEngine;

public class FlashLight : MonoBehaviour
{
    public GameObject lightSource;
    private bool isOn = true;

    void Start()
    {
        if (lightSource != null)
        {
            lightSource.SetActive(isOn);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isOn = !isOn;
            if (lightSource != null)
            {
                lightSource.SetActive(isOn);
            }
        }
    }
}
