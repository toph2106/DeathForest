using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class FlashlightToggle : MonoBehaviour
{
    public static FlashlightToggle Instance { get; private set; }

    // DỮ LIỆU STATIC TỰ ĐỘNG GIỮ SỐ PIN & THỜI LƯỢNG KHI SỐNG SÓT QUA CÁC MAP (1 -> 2 -> 3)
    private static float savedBattery = -1f;
    private static int savedHasFlashlight = -1;

    [Header("0. Trạng Thái Sở Hữu Đèn Pin")]
    [Tooltip("Tích chọn nếu người chơi đã có Đèn Pin trong tay. Nếu chưa có -> Bật E hay nạp Pin sẽ bị khóa!")]
    public bool hasFlashlight = true;

    [Header("1. Kéo 3 Spot Light vào đây")]
    public Light spotHotspot;      // SpotLight_Hotspot
    public Light spotMidRing;      // Spot Light (1)
    public Light spotAmbient;      // Spot Light (2)

    [Header("2. Quản Lý % Pin Đèn Pin (Battery System)")]
    [Tooltip("Lượng Pin tối đa (Mặc định: 100%)")]
    public float maxBattery = 100f;

    [Tooltip("Lượng Pin hiện tại")]
    public float currentBattery = 100f;

    [Tooltip("Tốc độ tiêu hao Pin mỗi giây (0.3333% / giây -> Đúng 5 phút = 300s từ 100% về 0%)")]
    public float drainRate = 0.3333f;

    [Tooltip("Bật ô này để đèn nhấp nháy chập chờn khi Pin yếu (Mặc định còn dưới 5% mới nhấp nháy)")]
    public float lowBatteryThreshold = 5f;

    [Header("3. Giao Diện UI Pin (Tùy chọn)")]
    public Slider batterySliderUI;
    public Image batteryFillImage;
    public TMP_Text batteryTextUI;

    [Header("4. Âm thanh Bật/Tắt & Nạp Pin")]
    public AudioClip turnOnClip;
    public AudioClip turnOffClip;
    public AudioClip clickClip;
    public AudioClip reloadBatteryClip;

    [Range(0f, 1f)]
    public float soundVolume = 1.0f;

    [Header("5. Khóa Chống Spam Phím Bật/Tắt Đèn")]
    public float toggleCooldown = 0.4f;

    private AudioSource audioSource;
    private bool isOn = true;
    private float lastToggleTime = 0f;

    private float origHotspotIntensity;
    private float origMidRingIntensity;
    private float origAmbientIntensity;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Lưu cường độ sáng gốc (đảm bảo không bị lưu số 0)
        if (spotHotspot != null && spotHotspot.intensity > 0.1f) origHotspotIntensity = spotHotspot.intensity;
        else if (origHotspotIntensity <= 0.1f) origHotspotIntensity = 2.0f;

        if (spotMidRing != null && spotMidRing.intensity > 0.1f) origMidRingIntensity = spotMidRing.intensity;
        else if (origMidRingIntensity <= 0.1f) origMidRingIntensity = 1.2f;

        if (spotAmbient != null && spotAmbient.intensity > 0.1f) origAmbientIntensity = spotAmbient.intensity;
        else if (origAmbientIntensity <= 0.1f) origAmbientIntensity = 0.6f;

        // Loại trừ Layer UI để đèn pin không chiếu chói lóa vào các vật phẩm 3D trên Hotbar
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
        {
            if (spotHotspot != null) spotHotspot.cullingMask &= ~(1 << uiLayer);
            if (spotMidRing != null) spotMidRing.cullingMask &= ~(1 << uiLayer);
            if (spotAmbient != null) spotAmbient.cullingMask &= ~(1 << uiLayer);
        }

        // KHI CHUYỂN MAP: KHÔI PHỤC LẠI SỐ % PIN VÀ TRẠNG THÁI TỪ MAP TRƯỚC
        if (savedBattery >= 0f)
        {
            currentBattery = savedBattery;
        }
        else
        {
            currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
        }

        if (savedHasFlashlight >= 0)
        {
            hasFlashlight = (savedHasFlashlight == 1);
        }

        // Đồng bộ trạng thái đèn lúc đầu
        SetFlashlightState(hasFlashlight && isOn, false);

        UpdateUI();
    }

    void OnDisable()
    {
        // TRƯỚC KHI TẮT HOẶC CHUYỂN MAP: LƯU LẠI % PIN HIỆN TẠI VÀ TRẠNG THÁI ĐÈN
        savedBattery = currentBattery;
        savedHasFlashlight = hasFlashlight ? 1 : 0;
    }

    void Update()
    {
        if (!hasFlashlight) return;

        // 1. PHÍM E BẬT/TẮT ĐÈN PIN NHẠY 100%
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Time.time - lastToggleTime >= toggleCooldown)
            {
                if (!isOn && currentBattery <= 0f)
                {
                    PlayClickSoundOnly();
                }
                else
                {
                    ToggleFlashlight();
                }
            }
        }

        // 2. TIÊU HAO PIN KHI BẬT ĐÈN
        if (isOn)
        {
            if (currentBattery > 0f)
            {
                currentBattery -= drainRate * Time.deltaTime;
                currentBattery = Mathf.Max(0f, currentBattery);

                if (currentBattery <= lowBatteryThreshold && currentBattery > 0f)
                {
                    ApplyFlickerEffect();
                }
                else
                {
                    ResetLightIntensities();
                }

                // HẾT PIN: CHỈ TẮT BỤP ĐÈN PIN (GIỮ NGUYÊN GIAO DIỆN MÁY QUAY CAMCORDER)
                if (currentBattery <= 0f)
                {
                    SetFlashlightState(false, true);
                    Debug.Log("[FlashlightToggle] 🔋 Đèn pin đã cạn sạch pin! Tắt đèn pin.");
                }
            }

            UpdateUI();
        }
    }

    public void ToggleFlashlight()
    {
        if (!hasFlashlight) return;
        SetFlashlightState(!isOn, true);
    }

    public void SetFlashlightState(bool state, bool playSound = true)
    {
        if (!hasFlashlight && state) return;
        if (state && currentBattery <= 0f) state = false;

        bool stateChanged = (isOn != state);
        lastToggleTime = Time.time;
        isOn = state;

        if (spotHotspot != null)
        {
            spotHotspot.gameObject.SetActive(isOn);
            spotHotspot.enabled = isOn;
        }
        if (spotMidRing != null)
        {
            spotMidRing.gameObject.SetActive(isOn);
            spotMidRing.enabled = isOn;
        }
        if (spotAmbient != null)
        {
            spotAmbient.gameObject.SetActive(isOn);
            spotAmbient.enabled = isOn;
        }

        if (isOn)
        {
            ResetLightIntensities();
        }

        if (playSound && stateChanged && audioSource != null)
        {
            AudioClip clipToPlay = isOn ? (turnOnClip != null ? turnOnClip : clickClip) : (turnOffClip != null ? turnOffClip : clickClip);
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay, soundVolume);
            }
        }
    }

    private void PlayClickSoundOnly()
    {
        lastToggleTime = Time.time;
        if (audioSource != null && clickClip != null)
        {
            audioSource.PlayOneShot(clickClip, soundVolume);
        }
    }

    public void EquipFlashlight(bool playSound = true, float initialBattery = -1f)
    {
        hasFlashlight = true;
        savedHasFlashlight = 1;
        if (initialBattery >= 0f)
        {
            currentBattery = Mathf.Clamp(initialBattery, 0f, maxBattery);
        }
        else
        {
            currentBattery = maxBattery;
        }
        savedBattery = currentBattery;
        SetFlashlightState(true, playSound);

        if (CamcorderUI.Instance != null)
        {
            CamcorderUI.Instance.gameObject.SetActive(true);
        }
    }

    public void UnequipFlashlight()
    {
        hasFlashlight = false;
        savedHasFlashlight = 0;
        SetFlashlightState(false, true);
        if (batterySliderUI != null) batterySliderUI.gameObject.SetActive(false);
        if (batteryTextUI != null) batteryTextUI.gameObject.SetActive(false);
    }

    public void RechargeBattery(float amount)
    {
        if (!hasFlashlight) return;

        bool wasDepleted = (currentBattery <= 0f);
        currentBattery = Mathf.Min(maxBattery, currentBattery + amount);
        savedBattery = currentBattery;
        UpdateUI();

        ResetLightIntensities();

        // KHI NẠP PIN VÀO MÀ TRƯỚC ĐÓ HẾT PIN -> HIỆN LẠI UI CAMCORDER!
        if (wasDepleted && currentBattery > 0f && CamcorderUI.Instance != null)
        {
            CamcorderUI.Instance.gameObject.SetActive(true);
        }

        if (audioSource != null && reloadBatteryClip != null)
        {
            audioSource.PlayOneShot(reloadBatteryClip, soundVolume);
        }
    }

    private void ApplyFlickerEffect()
    {
        float factor = currentBattery / lowBatteryThreshold;
        float flicker = Random.Range(0.2f, 1.0f) * factor;

        if (spotHotspot != null) spotHotspot.intensity = origHotspotIntensity * flicker;
        if (spotMidRing != null) spotMidRing.intensity = origMidRingIntensity * flicker;
        if (spotAmbient != null) spotAmbient.intensity = origAmbientIntensity * flicker;
    }

    private void ResetLightIntensities()
    {
        if (spotHotspot != null) spotHotspot.intensity = origHotspotIntensity;
        if (spotMidRing != null) spotMidRing.intensity = origMidRingIntensity;
        if (spotAmbient != null) spotAmbient.intensity = origAmbientIntensity;
    }

    private void UpdateUI()
    {
        if (batterySliderUI != null)
        {
            batterySliderUI.maxValue = maxBattery;
            batterySliderUI.value = currentBattery;
        }

        if (batteryTextUI != null)
        {
            int pct = Mathf.CeilToInt((currentBattery / maxBattery) * 100f);
            batteryTextUI.text = pct + "%";
        }

        if (batteryFillImage != null)
        {
            float pct = currentBattery / maxBattery;
            if (pct > 0.5f) batteryFillImage.color = Color.green;
            else if (pct > 0.2f) batteryFillImage.color = Color.yellow;
            else batteryFillImage.color = Color.red;
        }
    }

    public bool IsOn()
    {
        return isOn;
    }

    public static void ResetFlashlightData()
    {
        savedBattery = -1f;
        savedHasFlashlight = -1;
    }
}