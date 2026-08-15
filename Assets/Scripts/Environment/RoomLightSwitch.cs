using UnityEngine;
using System.Collections;

public class RoomLightSwitch : MonoBehaviour, IInteractable
{
    [Header("1. Đèn Phòng (Point Light)")]
    [Tooltip("Kéo Point Light vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM Point Light trong Scene!")]
    public Light roomPointLight;

    [Tooltip("Trạng thái đèn lúc bắt đầu game (Mặc định: Bật đèn chính)")]
    public bool isLightOn = true;

    [Header("2. Cài Đặt Đèn Chính (Lúc BẬT ĐÈN)")]
    [Tooltip("Kiểu đèn chính (Mặc định: Point Light)")]
    public LightType mainLightType = LightType.Point;

    [Tooltip("Màu ánh sáng đèn chính (Màu vàng/trắng ấm)")]
    public Color mainLightColor = new Color(1f, 0.96f, 0.78f);

    [Tooltip("Độ sáng lúc BẬT đèn chính (Mặc định: 20.0)")]
    public float mainLightIntensity = 20.0f;

    [Tooltip("Bán kính chiếu sáng của đèn chính (Range)")]
    public float mainLightRange = 10.0f;

    [Header("3. Cài Đặt Đèn Ngủ (Lúc TẮT ĐÈN CHÍNH - Tự Đổi Sang Spot Không Chói Trần)")]
    [Tooltip("Tự động đổi sang Spot Light khi tắt đèn để không bị chói trần")]
    public LightType nightLightType = LightType.Spot;

    [Tooltip("Màu ánh sáng đèn ngủ lúc tắt đèn chính")]
    public Color nightLightColor = new Color(0.85f, 0.22f, 0.12f);

    [Tooltip("Độ sáng đèn ngủ (Mặc định: 5.0)")]
    public float nightLightIntensity = 5.0f;

    [Tooltip("Bán kính chiếu sáng đèn ngủ (Range, Mặc định: 30)")]
    public float nightLightRange = 30.0f;

    [Tooltip("Góc chiếu sáng bên trong (Inner Spot Angle, Mặc định: 107)")]
    [Range(1f, 179f)] public float nightInnerSpotAngle = 107.0f;

    [Tooltip("Góc chiếu sáng bao phủ (Outer Spot Angle, Mặc định: 179)")]
    [Range(1f, 179f)] public float nightOuterSpotAngle = 179.0f;

    [Tooltip("Thời gian tối đen hoàn toàn (bụp 1 phát tắt ngúm) trước khi đèn ngủ đỏ bắt đầu sáng (giây, Mặc định: 0.2s)")]
    public float darkPauseBeforeNightLight = 0.2f;

    [Tooltip("Thời gian ánh sáng đèn ngủ đỏ từ từ sáng mờ dần lên (giây, Mặc định: 0.8s)")]
    public float nightLightFadeInDuration = 0.8f;

    [Header("4. Cần Gạt Công Tắc (Switch Lever - Object_8)")]
    [Tooltip("Kéo Object_8 (cần gạt) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM Object_8 con!")]
    public Transform switchLever;

    [Tooltip("Góc xoay (Local Euler) khi BẬT đèn")]
    public Vector3 onLocalRotation = new Vector3(0f, 0f, 0f);

    [Tooltip("Góc xoay (Local Euler) khi TẮT đèn (Lật cần gạt xuống)")]
    public Vector3 offLocalRotation = new Vector3(18f, 0f, 0f);

    [Tooltip("Thời gian gạt cần công tắc (giây)")]
    public float switchLeverSpeed = 0.12f;

    [Header("5. Chữ Nhắc Tương Tác (Prompt UI)")]
    public string turnOffPromptVi = "Tắt đèn";
    public string turnOffPromptEn = "Turn Off Light";
    public string turnOnPromptVi = "Bật đèn";
    public string turnOnPromptEn = "Turn On Light";

    [Header("6. Âm Thanh Công Tắc (Tùy Chọn)")]
    [Tooltip("Kéo âm thanh click công tắc vào đây nếu có (để trống sẽ không phát âm thanh lạ)")]
    public AudioClip switchClickSound;
    [Range(0f, 1f)] public float soundVolume = 0.85f;

    [Header("7. Liên Kết Với Nệm Ngủ (Yêu Cầu Tắt Đèn Để Ngủ)")]
    [Tooltip("Tích chọn để bắt buộc phải TẮT ĐÈN CHÍNH thì mới mở khóa nệm cho Player nằm ngủ")]
    public bool requireLightOffToSleep = true;

    [Tooltip("Kéo BedSleepCutscene vào đây. Nếu để trống sẽ tự động tìm")]
    public BedSleepCutscene bedCutscene;

    private AudioSource audioSource;
    private InteractPrompt interactPrompt;
    private BoxCollider col;
    private Coroutine switchCoroutine;

    void Awake()
    {
        col = GetComponent<Collider>() as BoxCollider;
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.size = new Vector3(0.3f, 0.3f, 0.15f);
        }

        interactPrompt = GetComponent<InteractPrompt>();
        if (interactPrompt == null) interactPrompt = gameObject.AddComponent<InteractPrompt>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // Âm thanh 3D chân thực tại vị trí công tắc
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = 8f;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        AutoFindReferences();
        ApplyInitialLightState();
        UpdatePromptText();
    }

    void AutoFindReferences()
    {
        // 1. Tìm Point Light trong Scene
        if (roomPointLight == null)
        {
            GameObject lightObj = GameObject.Find("Point Light");
            if (lightObj != null) roomPointLight = lightObj.GetComponent<Light>();
            if (roomPointLight == null) roomPointLight = Object.FindFirstObjectByType<Light>();
        }

        // Lưu lại màu gốc của Point Light nếu có
        if (roomPointLight != null && mainLightColor == new Color(1f, 0.96f, 0.78f))
        {
            mainLightColor = roomPointLight.color;
        }

        // 2. Tìm cần gạt Object_8
        if (switchLever == null)
        {
            Transform leverObj = transform.Find("Object_8");
            if (leverObj != null) switchLever = leverObj;
            else
            {
                Transform[] allChildren = GetComponentsInChildren<Transform>(true);
                foreach (var child in allChildren)
                {
                    if (child.name == "Object_8")
                    {
                        switchLever = child;
                        break;
                    }
                }
            }
        }

        // 3. Tìm BedSleepCutscene
        if (bedCutscene == null)
        {
            bedCutscene = Object.FindFirstObjectByType<BedSleepCutscene>();
        }
    }

    void ApplyInitialLightState()
    {
        // Đồng bộ trạng thái ban đầu của Light
        if (roomPointLight != null)
        {
            roomPointLight.enabled = true;
            if (isLightOn)
            {
                roomPointLight.type = mainLightType;
                roomPointLight.color = mainLightColor;
                roomPointLight.intensity = mainLightIntensity;
                roomPointLight.range = mainLightRange;
            }
            else
            {
                roomPointLight.type = nightLightType;
                roomPointLight.color = nightLightColor;
                roomPointLight.intensity = nightLightIntensity;
                roomPointLight.range = nightLightRange;
                if (nightLightType == LightType.Spot)
                {
                    roomPointLight.spotAngle = nightOuterSpotAngle;
                    roomPointLight.innerSpotAngle = nightInnerSpotAngle;
                }
            }
        }

        // Đồng bộ góc cần gạt ban đầu
        if (switchLever != null)
        {
            switchLever.localRotation = Quaternion.Euler(isLightOn ? onLocalRotation : offLocalRotation);
        }
    }

    public void Interact()
    {
        ToggleLight();
    }

    public void ToggleLight()
    {
        isLightOn = !isLightOn;

        // Phát âm thanh công tắc TẠCH nếu có gán
        if (switchClickSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(switchClickSound, soundVolume);
            else AudioSource.PlayClipAtPoint(switchClickSound, transform.position, soundVolume);
        }

        // Chạy chuyển động gạt cần công tắc & chuyển màu/độ sáng đèn tức thì
        if (switchCoroutine != null) StopCoroutine(switchCoroutine);
        switchCoroutine = StartCoroutine(AnimateSwitchRoutine());

        // Cập nhật chữ nhắc
        UpdatePromptText();

        Debug.Log($"[RoomLightSwitch] 💡 Đã chuyển sang chế độ: {(isLightOn ? "ĐÈN CHÍNH (Sáng rõ)" : "ĐÈN NGỦ (Spot đỏ dịu)")}");
    }

    IEnumerator AnimateSwitchRoutine()
    {
        // 1. CHUYỂN ĐỔI ÁNH SÁNG TỨC THÌ (TẠCH 1 PHÁT CHUYỂN MÀU LUÔN)
        if (roomPointLight != null)
        {
            roomPointLight.enabled = true;

            if (!isLightOn)
            {
                // TẮT ĐÈN CHÍNH -> TỨC THÌ THÀNH SPOT ĐÈN NGỦ ĐỎ DỊU (Inner: 107, Outer: 179, Range: 30, Intensity: 5)
                roomPointLight.type = nightLightType;
                if (nightLightType == LightType.Spot)
                {
                    roomPointLight.spotAngle = nightOuterSpotAngle;
                    roomPointLight.innerSpotAngle = nightInnerSpotAngle;
                }
                roomPointLight.range = nightLightRange;
                roomPointLight.color = nightLightColor;
                roomPointLight.intensity = nightLightIntensity;
            }
            else
            {
                // BẬT LẠI ĐÈN CHÍNH -> TỨC THÌ THÀNH POINT LIGHT SÁNG RÕ
                roomPointLight.type = mainLightType;
                roomPointLight.range = mainLightRange;
                roomPointLight.color = mainLightColor;
                roomPointLight.intensity = mainLightIntensity;
            }
        }

        // 2. Xoay cần gạt công tắc dứt khoát
        Quaternion startLeverRot = (switchLever != null) ? switchLever.localRotation : Quaternion.identity;
        Quaternion targetLeverRot = Quaternion.Euler(isLightOn ? onLocalRotation : offLocalRotation);

        float leverElapsed = 0f;
        while (leverElapsed < switchLeverSpeed)
        {
            leverElapsed += Time.deltaTime;
            float lt = (switchLeverSpeed > 0f) ? Mathf.Clamp01(leverElapsed / switchLeverSpeed) : 1f;
            if (switchLever != null) switchLever.localRotation = Quaternion.Slerp(startLeverRot, targetLeverRot, lt);
            yield return null;
        }
        if (switchLever != null) switchLever.localRotation = targetLeverRot;
    }

    void UpdatePromptText()
    {
        if (interactPrompt == null) return;

        if (isLightOn)
        {
            interactPrompt.vietnamesePrompt = turnOffPromptVi;
            interactPrompt.englishPrompt = turnOffPromptEn;
        }
        else
        {
            interactPrompt.vietnamesePrompt = turnOnPromptVi;
            interactPrompt.englishPrompt = turnOnPromptEn;
        }

        interactPrompt.UpdateText();
    }

    public void ShowPrompt()
    {
        UpdatePromptText();
        if (interactPrompt != null) interactPrompt.ShowPrompt();
    }

    public void HidePrompt()
    {
        if (interactPrompt != null) interactPrompt.HidePrompt();
    }
}
