using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class HorrorFlashlight : MonoBehaviour
{
    private Light flashlight;
    private bool isOn = true;
    private float defaultIntensity;

    [Header("--- BẬT / TẮT CHÍNH ---")]
    [Tooltip("Phím để bật tắt đèn")]
    public KeyCode toggleKey = KeyCode.F;
    [Tooltip("Âm thanh bật/tắt đèn (Click click)")]
    public AudioClip toggleSound;
    private AudioSource audioSource;

    [Header("--- HỆ THỐNG PIN (BATTERY) ---")]
    public bool useBattery = true;
    public float maxBattery = 100f;
    public float currentBattery;
    [Tooltip("Tốc độ hao pin mỗi giây")]
    public float batteryDrainSpeed = 2f;

    [Header("--- NHẤP NHÁY KINH DỊ (FLICKER) ---")]
    [Tooltip("Mức pin bắt đầu bị nhấp nháy yếu")]
    public float lowBatteryThreshold = 20f;
    [Tooltip("Bật tính năng này khi muốn ma làm đèn nhấp nháy bằng code ngoài")]
    public bool isJammedByMonster = false;

    [Range(0.01f, 0.5f)] public float flickerSpeedMin = 0.05f;
    [Range(0.1f, 1f)] public float flickerSpeedMax = 0.3f;

    private float flickerTimer;

    void Start()
    {
        flashlight = GetComponent<Light>();
        defaultIntensity = flashlight.intensity;
        currentBattery = maxBattery;

        // Tự động thêm AudioSource để phát âm thanh
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        // 1. Bấm phím F để Bật/Tắt đèn
        if (Input.GetKeyDown(toggleKey) && currentBattery > 0 && !isJammedByMonster)
        {
            ToggleLight();
        }

        // 2. Xử lý hao pin khi đèn đang bật
        if (isOn && useBattery && !isJammedByMonster)
        {
            currentBattery -= batteryDrainSpeed * Time.deltaTime;
            currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);

            // Hết pin thì tự tắt
            if (currentBattery <= 0)
            {
                PowerOff();
            }
        }

        // 3. Xử lý Nhấp nháy (Khi sắp hết pin HOẶC khi bị ma ám)
        if (isOn && (isJammedByMonster || (useBattery && currentBattery <= lowBatteryThreshold)))
        {
            HandleFlicker();
        }
        else if (isOn && !isJammedByMonster)
        {
            // Nếu pin bình thường thì trả lại độ sáng mặc định
            flashlight.intensity = defaultIntensity;
        }
    }

    void ToggleLight()
    {
        isOn = !isOn;
        flashlight.enabled = isOn;
        PlaySound(toggleSound);
    }

    void PowerOff()
    {
        isOn = false;
        flashlight.enabled = false;
        PlaySound(toggleSound); // Tiếng đèn sập nguồn
    }

    void HandleFlicker()
    {
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0)
        {
            // Bật/Tắt ngẫu nhiên bóng đèn để tạo hiệu ứng chập chờn
            flashlight.enabled = !flashlight.enabled;
            // Tự động đổi thời gian nhấp nháy tiếp theo cho tự nhiên
            flickerTimer = Random.Range(flickerSpeedMin, flickerSpeedMax);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // --- HÀM PUBLIC ĐỂ CODE KHÁC GỌI VÀO ---

    // Gọi hàm này khi nhặt được bình pin trong game
    public void RechargeBattery(float amount)
    {
        currentBattery += amount;
        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
        if (!isOn && currentBattery > 0)
        {
            isOn = true;
            flashlight.enabled = true;
        }
    }

    // Gọi hàm này khi muốn ép đèn nhấp nháy (ví dụ: Ma đang ở gần)
    public void SetMonsterProximity(bool near)
    {
        isJammedByMonster = near;
        if (!near && isOn)
        {
            flashlight.enabled = true;
            flashlight.intensity = defaultIntensity;
        }
    }
}