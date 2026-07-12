using UnityEngine;

public class FlashlightToggle : MonoBehaviour
{
    [Header("Kéo 3 Spot Light vào đây")]
    public Light spotHotspot;      // SpotLight_Hotspot
    public Light spotMidRing;      // Spot Light (1)
    public Light spotAmbient;      // Spot Light (2)

    [Header("Âm thanh bật/tắt (tùy chọn)")]
    public AudioClip clickClip;
    private AudioSource audioSource;

    private bool isOn = true;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            isOn = !isOn;
            spotHotspot.enabled = isOn;
            spotMidRing.enabled = isOn;
            spotAmbient.enabled = isOn;

            if (clickClip != null)
                audioSource.PlayOneShot(clickClip);
        }
    }
}