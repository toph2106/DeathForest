using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Night Vision Camera v4 — Phím F bật/tắt.
/// Mô phỏng camera nhìn đêm hồng ngoại thật:
/// - Xanh lục rực rỡ, sáng đều toàn cảnh (Point Light + Post Exposure cao)
/// - Vignette tròn đen viền ống kính
/// - Nhìn xuyên sương mù
/// - Nhiễu hạt + Chromatic Aberration
/// </summary>
public class NightVisionCamera : MonoBehaviour
{
    public static NightVisionCamera Instance { get; private set; }

    [Header("══ PHÍM TẮT ══")]
    public KeyCode toggleKey = KeyCode.F;
    [Tooltip("Bắt buộc phải nhặt Máy Quay (Camera) mới được bấm F bật Night Vision")]
    public bool requireCameraEquipped = true;

    [Header("══ ĐỘ SÁNG NIGHT VISION ══")]
    [Tooltip("Post Exposure — cần cao (3.5-5.0) để cảnh tối đen thành sáng rõ")]
    [Range(1f, 8f)]
    public float nvPostExposure = 3.2f;

    [Header("══ MÀU SẮC — Xanh Lục Dạ Quang (#28C850) ══")]
    [Tooltip("Color Filter — mã màu #28C850 (R:40, G:200, B:80)")]
    public Color nvColorFilter = new Color(40f / 255f, 200f / 255f, 80f / 255f, 1f); // #28C850

    [Tooltip("Màu Ambient môi trường khi bật NV (Mã hex: #28C850)")]
    public Color nvAmbientColor = new Color(40f / 255f, 200f / 255f, 80f / 255f, 1f); // #28C850

    [Tooltip("Saturation — giữ nguyên hoặc tăng nhẹ để xanh rực hơn")]
    [Range(-100f, 30f)]
    public float nvSaturation = -30f;

    [Tooltip("Contrast")]
    [Range(-20f, 50f)]
    public float nvContrast = 20f;

    [Header("══ VIGNETTE (Ống Kính Tròn) ══")]
    [Range(0.2f, 0.7f)]
    public float nvVignetteIntensity = 0.45f;
    public Color nvVignetteColor = new Color(0.0f, 0.02f, 0.0f, 1f);

    [Header("══ FILM GRAIN ══")]
    [Range(0f, 1f)]
    public float nvFilmGrainIntensity = 0.5f;

    [Header("══ BLOOM ══")]
    [Range(0f, 3f)]
    public float nvBloomIntensity = 1.0f;

    [Header("══ CHROMATIC ABERRATION ══")]
    [Range(0f, 1f)]
    public float nvChromaticAberration = 0.12f;

    [Header("══ SƯƠNG MÙ ══")]
    [Range(40f, 200f)]
    public float nvFogEnd = 55f;
    [Range(0f, 40f)]
    public float nvFogStart = 12f;

    [Header("══ ĐÈN HỒNG NGOẠI (Point Light — Sáng Đều Xung Quanh) ══")]
    public Light irLight;
    [Tooltip("Cường độ đèn IR — sáng đều mọi hướng, KHÔNG chỉ 1 hướng")]
    [Range(0.5f, 5f)]
    public float irIntensity = 1.5f;
    [Tooltip("Tầm chiếu IR (mét)")]
    [Range(10f, 50f)]
    public float irRange = 18f;
    [Tooltip("Màu đèn IR — xanh lục theo mã #28C850")]
    public Color irColor = new Color(40f / 255f, 200f / 255f, 80f / 255f, 1f);

    [Header("══ ÂM THANH ══")]
    public AudioClip nvOnSound;
    public AudioClip nvOffSound;
    [Range(0f, 1f)] public float soundVolume = 0.7f;

    [Header("══ CẢM BIẾN QUÁI VẬT ══")]
    public bool enableMonsterGlitch = true;
    [Range(5f, 25f)] public float monsterDetectRadius = 12f;

    // ─── TRẠNG THÁI ───
    public bool IsNightVisionOn { get; private set; } = false;
    private bool wasFlashlightOnBeforeNV = false;

    // Volume riêng cho NV (không sửa Volume gốc)
    private Volume nvVolume;
    private VolumeProfile nvProfile;
    private ColorAdjustments nvColorAdj;
    private FilmGrain nvFilmGrain;
    private Vignette nvVignette;
    private Bloom nvBloom;
    private ChromaticAberration nvChromatic;

    // Fog gốc
    private bool origFogEnabled;
    private Color origFogColor;
    private float origFogStart, origFogEnd, origFogDensity;
    private bool fogSaved = false;

    // Ambient gốc
    private Color origAmbientColor;
    private float origAmbientIntensity;
    private AmbientMode origAmbientMode;

    private AudioSource audioSource;
    private float glitchTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        SetupIRLight();
        CreateNVVolume();
        SaveOriginalSettings();
        SetActive(false);

        // Mặc định luôn yêu cầu phải có máy quay
        requireCameraEquipped = true;
    }

    /// <summary>
    /// Kiểm tra người chơi đã sở hữu hoặc UI máy quay đang hoạt động hay chưa.
    /// Nếu UI Camera đang hoạt động hoặc đã nhặt máy quay -> Cho phép bấm F bật Night Vision!
    /// </summary>
    public bool HasCamera()
    {
        // 1. Kiểm tra cờ trạng thái nhặt máy quay
        if (CamcorderUI.HasPickedUpCamera) return true;
        if (PlayerPrefs.GetInt("Global_Has_Camera", 0) == 1) return true;

        // 2. Kiểm tra SimpleCameraOverlay (kính ngắm màn hình)
        SimpleCameraOverlay overlay = GetComponent<SimpleCameraOverlay>();
        if (overlay == null) overlay = Object.FindFirstObjectByType<SimpleCameraOverlay>();
        if (overlay != null)
        {
            if (overlay.HasCamera) return true;
            if (overlay.cameraOverlayCanvas != null && overlay.cameraOverlayCanvas.activeInHierarchy) return true;
        }

        // 3. Kiểm tra GameObject CamcorderUI trong Scene đang Active
        if (CamcorderUI.Instance != null && CamcorderUI.Instance.gameObject.activeInHierarchy)
        {
            return true;
        }

        CamcorderUI[] camUIs = Object.FindObjectsByType<CamcorderUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (camUIs != null && camUIs.Length > 0)
        {
            return true;
        }

        // 4. Các scene mặc định đã có máy quay (Map03, Map04)
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Map03" || sceneName == "Map04")
        {
            return true;
        }

        return false;
    }

    void Update()
    {
        if (requireCameraEquipped)
        {
            if (!HasCamera())
            {
                if (IsNightVisionOn)
                {
                    SetNightVision(false);
                }
                return;
            }
        }

        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }

        if (IsNightVisionOn)
        {
            RenderSettings.ambientLight = nvAmbientColor;

            if (enableMonsterGlitch)
            {
                UpdateMonsterGlitch();
            }
        }
    }

    // ═══════════════════════════════════════════
    //  PUBLIC
    // ═══════════════════════════════════════════

    public void Toggle()
    {
        SetNightVision(!IsNightVisionOn);
    }

    public void SetNightVision(bool on)
    {
        if (IsNightVisionOn == on) return;

        FlashlightToggle ft = FlashlightToggle.Instance ?? Object.FindFirstObjectByType<FlashlightToggle>();

        // Nếu cạn pin thì không cho bật Night Vision
        if (on && ft != null && ft.currentBattery <= 0f)
        {
            PlaySound(null, 300f, 0.05f); // Tiếng tạch hết pin
            return;
        }

        IsNightVisionOn = on;

        if (on)
        {
            // BẬT NV: Kiểm tra xem đèn pin có đang BẬT không -> Lưu lại rồi tự tắt đèn pin
            if (ft != null)
            {
                wasFlashlightOnBeforeNV = ft.IsOn();
                if (wasFlashlightOnBeforeNV)
                {
                    ft.SetFlashlightState(false, false);
                }
            }
        }
        else
        {
            // TẮT NV: Khôi phục lại đèn pin NẾU lúc đầu đèn pin đang bật
            if (ft != null && wasFlashlightOnBeforeNV && ft.currentBattery > 0f)
            {
                ft.SetFlashlightState(true, false);
            }
            wasFlashlightOnBeforeNV = false;
        }

        PlaySound(on ? nvOnSound : nvOffSound, on ? 1200f : 600f, on ? 0.12f : 0.06f);
        SetActive(on);
    }

    // ═══════════════════════════════════════════
    //  BẬT / TẮT
    // ═══════════════════════════════════════════

    private void SetActive(bool on)
    {
        // 1. Volume NV
        if (nvVolume != null) nvVolume.enabled = on;

        // 2. Đèn IR (Point Light — chiếu sáng đều 360 độ)
        if (irLight != null)
        {
            irLight.enabled = on;
            if (on) irLight.intensity = irIntensity;
        }

        // 3. Fog
        if (on)
        {
            if (!fogSaved) SaveOriginalSettings();
            if (RenderSettings.fog)
            {
                if (RenderSettings.fogMode == FogMode.Linear)
                {
                    RenderSettings.fogStartDistance = nvFogStart;
                    RenderSettings.fogEndDistance = nvFogEnd;
                }
                else
                {
                    RenderSettings.fogDensity = Mathf.Min(origFogDensity * 0.2f, 0.005f);
                }
                // Fog color xanh đen
                RenderSettings.fogColor = new Color(0.0f, 0.015f, 0.005f, 1f);
            }

            // 4. Tăng Ambient Light theo màu setup (mã #28C850)
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = nvAmbientColor;
        }
        else
        {
            RestoreOriginalSettings();
        }
    }

    // ═══════════════════════════════════════════
    //  TẠO NV VOLUME (Runtime)
    // ═══════════════════════════════════════════

    private void CreateNVVolume()
    {
        GameObject volObj = new GameObject("NightVision_Volume_RT");
        volObj.transform.SetParent(transform, false);

        nvVolume = volObj.AddComponent<Volume>();
        nvVolume.isGlobal = true;
        nvVolume.priority = 100;
        nvVolume.enabled = false;

        nvProfile = ScriptableObject.CreateInstance<VolumeProfile>();

        // ── Color Adjustments ──
        nvColorAdj = nvProfile.Add<ColorAdjustments>(false);
        nvColorAdj.active = true;
        nvColorAdj.postExposure.overrideState = true;
        nvColorAdj.postExposure.value = nvPostExposure;
        nvColorAdj.colorFilter.overrideState = true;
        nvColorAdj.colorFilter.value = nvColorFilter;
        nvColorAdj.saturation.overrideState = true;
        nvColorAdj.saturation.value = nvSaturation;
        nvColorAdj.contrast.overrideState = true;
        nvColorAdj.contrast.value = nvContrast;

        // ── Film Grain ──
        nvFilmGrain = nvProfile.Add<FilmGrain>(false);
        nvFilmGrain.active = true;
        nvFilmGrain.type.overrideState = true;
        nvFilmGrain.type.value = FilmGrainLookup.Medium3;
        nvFilmGrain.intensity.overrideState = true;
        nvFilmGrain.intensity.value = nvFilmGrainIntensity;
        nvFilmGrain.response.overrideState = true;
        nvFilmGrain.response.value = 0.8f;

        // ── Vignette — Ống kính tròn đen ──
        nvVignette = nvProfile.Add<Vignette>(false);
        nvVignette.active = true;
        nvVignette.color.overrideState = true;
        nvVignette.color.value = nvVignetteColor;
        nvVignette.intensity.overrideState = true;
        nvVignette.intensity.value = nvVignetteIntensity;
        nvVignette.smoothness.overrideState = true;
        nvVignette.smoothness.value = 0.4f;
        nvVignette.rounded.overrideState = true;
        nvVignette.rounded.value = true;

        // ── Bloom — Quầng sáng xanh ──
        nvBloom = nvProfile.Add<Bloom>(false);
        nvBloom.active = true;
        nvBloom.threshold.overrideState = true;
        nvBloom.threshold.value = 0.5f;
        nvBloom.intensity.overrideState = true;
        nvBloom.intensity.value = nvBloomIntensity;
        nvBloom.scatter.overrideState = true;
        nvBloom.scatter.value = 0.7f;
        nvBloom.tint.overrideState = true;
        nvBloom.tint.value = new Color(0.6f, 1f, 0.65f, 1f);

        // ── Chromatic Aberration ──
        nvChromatic = nvProfile.Add<ChromaticAberration>(false);
        nvChromatic.active = true;
        nvChromatic.intensity.overrideState = true;
        nvChromatic.intensity.value = nvChromaticAberration;

        nvVolume.profile = nvProfile;
    }

    // ═══════════════════════════════════════════
    //  MONSTER GLITCH
    // ═══════════════════════════════════════════

    private void UpdateMonsterGlitch()
    {
        float dist = GetNearestMonsterDistance();

        if (dist < monsterDetectRadius)
        {
            float t = 1f - (dist / monsterDetectRadius);
            glitchTimer += Time.deltaTime * (t * 18f + 4f);

            if (irLight != null)
            {
                float flicker = Mathf.PingPong(glitchTimer, 0.25f) * t * 0.6f;
                irLight.intensity = irIntensity - flicker;
            }
            if (nvColorAdj != null)
            {
                float jitter = Mathf.Sin(glitchTimer * 9f) * t * 0.5f;
                nvColorAdj.postExposure.value = nvPostExposure + jitter;
            }
            if (nvChromatic != null)
            {
                nvChromatic.intensity.value = nvChromaticAberration + (t * 0.5f);
            }
        }
        else
        {
            if (irLight != null) irLight.intensity = irIntensity;
            if (nvColorAdj != null) nvColorAdj.postExposure.value = nvPostExposure;
            if (nvChromatic != null) nvChromatic.intensity.value = nvChromaticAberration;
            glitchTimer = 0f;
        }
    }

    private float GetNearestMonsterDistance()
    {
        float min = float.MaxValue;
        Collider[] hits = Physics.OverlapSphere(transform.position, monsterDetectRadius);
        foreach (var col in hits)
        {
            // Nhận diện quái vật hoàn toàn qua tên GameObject (không dùng CompareTag để tránh lỗi Console)
            string n = col.gameObject.name.ToLower();
            bool isMonster = n.Contains("enm") || n.Contains("dog") ||
                             n.Contains("yoshie") || n.Contains("wman") || 
                             n.Contains("gari") || n.Contains("stalker") ||
                             n.Contains("enemy") || n.Contains("monster") || n.Contains("ghost");

            if (isMonster)
            {
                float d = Vector3.Distance(transform.position, col.transform.position);
                if (d < min) min = d;
            }
        }
        return min;
    }

    // ═══════════════════════════════════════════
    //  ĐÈN IR (POINT LIGHT)
    // ═══════════════════════════════════════════

    private void SetupIRLight()
    {
        if (irLight == null)
        {
            GameObject obj = new GameObject("IR_PointLight");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.zero;

            irLight = obj.AddComponent<Light>();
            // *** POINT LIGHT — chiếu sáng ĐỀU 360 độ xung quanh người chơi ***
            // Không phải Spot Light (chỉ chiếu 1 hướng)
            irLight.type = LightType.Point;
            irLight.range = irRange;
            irLight.intensity = irIntensity;
            irLight.color = irColor;
            irLight.shadows = LightShadows.None; // Không bóng đổ = nhẹ máy
            irLight.enabled = false;
        }
    }

    // ═══════════════════════════════════════════
    //  LƯU / KHÔI PHỤC SETTINGS GỐC
    // ═══════════════════════════════════════════

    private void SaveOriginalSettings()
    {
        // Fog
        origFogEnabled = RenderSettings.fog;
        origFogColor = RenderSettings.fogColor;
        origFogStart = RenderSettings.fogStartDistance;
        origFogEnd = RenderSettings.fogEndDistance;
        origFogDensity = RenderSettings.fogDensity;

        // Ambient
        origAmbientMode = RenderSettings.ambientMode;
        origAmbientColor = RenderSettings.ambientLight;
        origAmbientIntensity = RenderSettings.ambientIntensity;

        fogSaved = true;
    }

    private void RestoreOriginalSettings()
    {
        if (!fogSaved) return;

        // Fog
        RenderSettings.fog = origFogEnabled;
        RenderSettings.fogColor = origFogColor;
        RenderSettings.fogStartDistance = origFogStart;
        RenderSettings.fogEndDistance = origFogEnd;
        RenderSettings.fogDensity = origFogDensity;

        // Ambient
        RenderSettings.ambientMode = origAmbientMode;
        RenderSettings.ambientLight = origAmbientColor;
        RenderSettings.ambientIntensity = origAmbientIntensity;
    }

    // ═══════════════════════════════════════════
    //  ÂM THANH
    // ═══════════════════════════════════════════

    private void PlaySound(AudioClip clip, float freq, float dur)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
        else
        {
            StartCoroutine(ProceduralBeep(freq, dur));
        }
    }

    private System.Collections.IEnumerator ProceduralBeep(float freq, float dur)
    {
        int rate = 44100;
        int count = (int)(rate * dur);
        if (count < 1) yield break;

        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / rate;
            float env = 1f - ((float)i / count);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.25f;
        }

        AudioClip beep = AudioClip.Create("NV_Beep", count, 1, rate, false);
        beep.SetData(data, 0);
        audioSource.PlayOneShot(beep, soundVolume);
        yield return null;
    }

    // ═══════════════════════════════════════════
    //  CLEANUP
    // ═══════════════════════════════════════════

    void OnDisable()
    {
        if (IsNightVisionOn)
        {
            SetActive(false);
            IsNightVisionOn = false;
        }
    }

    void OnDestroy()
    {
        if (IsNightVisionOn)
        {
            SetActive(false);
            IsNightVisionOn = false;
        }
        if (nvProfile != null) DestroyImmediate(nvProfile);
    }
}
