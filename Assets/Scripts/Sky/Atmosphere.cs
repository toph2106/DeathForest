using UnityEngine;

public class Atmosphere : MonoBehaviour
{
    [Range(0f, 1f)]
    public float darkIntensity = 0f;
    public Color darknessColor = Color.black;

    void Start()
    {
        ApplyHardcoreDarkness();
    }

    void OnValidate()
    {
        ApplyHardcoreDarkness();
    }

    void ApplyHardcoreDarkness()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = darknessColor;

        RenderSettings.reflectionIntensity = darkIntensity;

        RenderSettings.subtractiveShadowColor = darknessColor;

        DynamicGI.UpdateEnvironment();
    }
}