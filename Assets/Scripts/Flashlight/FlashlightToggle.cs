using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class FlashlightToggle : MonoBehaviour
{
    public static FlashlightToggle Instance { get; private set; }

    // DỮ LIỆU STATIC TỰ ĐỘNG GIỮ SỐ PIN & THỜI LƯỢNG KHI SỐNG SÓT QUA CÁC MAP (1 -> 2 -> 3 -> 4 -> 5)
    private static float savedBattery = -1f;
    private static int savedHasFlashlight = -1;

    public static float SavedBattery
    {
        get => savedBattery;
        set => savedBattery = value;
    }

    public static int SavedHasFlashlight
    {
        get => savedHasFlashlight;
        set => savedHasFlashlight = value;
    }

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

    [Header("6. Tự Động Chống Chói Khi Soi Gần (Auto-Dimming Anti-Glare)")]
    [Tooltip("Bật tính năng tự động giảm độ chói của đèn pin khi đi sát tường/vật thể/quái")]
    public bool enableAutoDimming = true;

    [Tooltip("Khoảng cách bắt đầu giảm sáng (mét - Mặc định: 3.5m)")]
    public float dimmingMaxDistance = 3.5f;

    [Tooltip("Khoảng cách gần nhất (mét - Mặc định: 0.5m)")]
    public float dimmingMinDistance = 0.5f;

    [Tooltip("Hệ số độ sáng tối thiểu khi dí sát mặt (0.3 = giảm còn 30% độ sáng không bị chói)")]
    [Range(0.1f, 1f)]
    public float minDimmingMultiplier = 0.3f;

    [Tooltip("Tốc độ chuyển đổi độ sáng mượt mà")]
    public float dimmingSmoothSpeed = 10f;

    public LayerMask dimmingLayerMask = ~0;

    [Header("7. Chớp Sáng Chói Lóa Làm Choáng (Chuột Phải / Flash Burst)")]
    [Tooltip("Bật tính năng chớp sáng chói lóa bằng Chuột Phải để làm choáng quái/chó")]
    public bool enableFlashBurst = true;

    [Tooltip("Âm thanh chớp flash máy ảnh/đèn pin (camera_flash.wav)")]
    public AudioClip cameraFlashSound;

    [Tooltip("Thời gian hồi chiêu chớp sáng (giây - Mặc định: 1.5s)")]
    public float flashBurstCooldown = 1.5f;

    [Tooltip("Khoảng cách tối đa chớp sáng làm choáng (mét - Mặc định: 25m)")]
    public float flashBurstDistance = 25.0f;

    [Tooltip("Góc nón chớp sáng trước mặt người chơi (độ - Mặc định: 90 độ - bao trọn màn hình)")]
    public float flashBurstAngle = 90.0f;

    [Tooltip("Lượng Pin tiêu hao cho mỗi lần chớp flash (Mặc định: 2%)")]
    public float flashBatteryCost = 2.0f;

    public LayerMask flashObstacleMask = ~0;

    private AudioSource audioSource;
    private bool isOn = true;
    private float lastToggleTime = 0f;
    private float lastFlashBurstTime = -999f;
    private UnityEngine.UI.Image screenFlashImage;

    private float origHotspotIntensity;
    private float origMidRingIntensity;
    private float origAmbientIntensity;

    private float currentDimMultiplier = 1.0f;
    private Camera cachedMainCam;

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

        // Tự động tìm âm thanh camera_flash nếu chưa được gán
        if (cameraFlashSound == null)
        {
            AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();
            foreach (var c in clips)
            {
                if (c != null && c.name.ToLower().Contains("camera_flash"))
                {
                    cameraFlashSound = c;
                    break;
                }
            }
        }

        // Tạo UI Overlay chớp trắng toàn màn hình
        CreateScreenFlashOverlay();

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

        // KHÔI PHỤC % PIN VÀ TRẠNG THÁI TỪ MAP TRƯỚC
        RestoreFlashlightState();

        // Đồng bộ trạng thái đèn lúc đầu
        SetFlashlightState(hasFlashlight && isOn, false);

        UpdateUI();
    }

    /// <summary>
    /// Khôi phục số % Pin và trạng thái đèn pin từ bộ nhớ RAM tĩnh hoặc PlayerPrefs
    /// </summary>
    public void RestoreFlashlightState()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Map01" || sceneName == "Map02")
        {
            ResetFlashlightData();
            currentBattery = maxBattery;
            hasFlashlight = false;
            savedHasFlashlight = 0;
            SetFlashlightState(false, false);
            UpdateUI();
            Debug.Log($"[FlashlightToggle] 🌟 {sceneName} đã nạp: Khởi tạo ĐÈN PIN MỚI (Chưa nhặt đèn pin, % Pin ẩn)!");
            return;
        }

        // Với Map 03, Map 04, Map 05: Nhân vật mặc định ĐÃ CÓ ĐÈN PIN nhặt từ Map 02
        bool isMapWithFlashlight = (sceneName == "Map03" || sceneName == "Map04" || sceneName == "Map05");
        if (isMapWithFlashlight)
        {
            hasFlashlight = true;
            savedHasFlashlight = 1;
            PlayerPrefs.SetInt("Global_Has_Flashlight", 1);
            PlayerPrefs.Save();
        }

        if (GameSaveManager.HasSaveFile())
        {
            GameSaveData data = GameSaveManager.LoadGame();
            if (data != null)
            {
                currentBattery = data.flashlightBattery;
                if (!hasFlashlight) hasFlashlight = data.hasFlashlight;
            }
        }
        else if (savedBattery >= 0f)
        {
            currentBattery = savedBattery;
        }
        else if (PlayerPrefs.HasKey("Global_Flashlight_Battery"))
        {
            currentBattery = PlayerPrefs.GetFloat("Global_Flashlight_Battery");
        }
        else
        {
            currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
        }

        if (isMapWithFlashlight && currentBattery <= 0f)
        {
            currentBattery = maxBattery;
        }

        if (savedHasFlashlight >= 0)
        {
            hasFlashlight = (savedHasFlashlight == 1);
        }
        else if (PlayerPrefs.HasKey("Global_Has_Flashlight"))
        {
            hasFlashlight = (PlayerPrefs.GetInt("Global_Has_Flashlight") == 1);
        }
        else if (isMapWithFlashlight)
        {
            hasFlashlight = true;
        }

        // Tự động tìm UI Pin nếu chưa được gán
        if (batteryTextUI == null)
        {
            CamcorderUI cam = CamcorderUI.Instance ?? Object.FindFirstObjectByType<CamcorderUI>(FindObjectsInactive.Include);
            if (cam != null && cam.batteryText != null)
            {
                batteryTextUI = cam.batteryText;
            }
        }

        if (batteryTextUI == null)
        {
            TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (var t in allTexts)
            {
                if (t != null && t.gameObject.scene.isLoaded && (t.name.ToLower().Contains("battery") || t.name.ToLower().Contains("pin")))
                {
                    batteryTextUI = t;
                    break;
                }
            }
        }

        UpdateUI();
        Debug.Log($"[FlashlightToggle] 🔄 RestoreFlashlightState() hoàn tất! % Pin: {currentBattery:F1}%, Đã có đèn: {hasFlashlight}");
    }

    public void SaveFlashlightData()
    {
        if (GameSaveManager.isResettingData) return;

        savedBattery = currentBattery;
        savedHasFlashlight = hasFlashlight ? 1 : 0;
        PlayerPrefs.SetFloat("Global_Flashlight_Battery", currentBattery);
        PlayerPrefs.SetInt("Global_Has_Flashlight", hasFlashlight ? 1 : 0);
        PlayerPrefs.Save();
        GameSaveManager.SaveGame();
        Debug.Log($"[FlashlightToggle] 🔦 Đã lưu % Pin: {currentBattery:F1}% và sở hữu đèn: {hasFlashlight}!");
    }

    void OnDisable()
    {
        // TRƯỚC KHI TẮT HOẶC CHUYỂN MAP: LƯU LẠI % PIN HIỆN TẠI VÀ TRẠNG THÁI ĐÈN (Nếu không trong trạng thái Reset)
        if (!GameSaveManager.isResettingData)
        {
            SaveFlashlightData();
        }
    }

    void Update()
    {
        if (!hasFlashlight) return;

        // 1. PHÍM E BẬT/TẮT ĐÈN PIN NHẠY 100%
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Time.time - lastToggleTime >= toggleCooldown)
            {
                if (currentBattery <= 0f)
                {
                    PlayClickSoundOnly();
                }
                else
                {
                    // Nếu đang bật Night Vision mà bấm E -> Tự tắt Night Vision & Bật lại Đèn Pin
                    if (NightVisionCamera.Instance != null && NightVisionCamera.Instance.IsNightVisionOn)
                    {
                        NightVisionCamera.Instance.SetNightVision(false);
                        SetFlashlightState(true, true);
                    }
                    else
                    {
                        ToggleFlashlight();
                    }
                }
            }
        }

        // 1.5. CHUỘT PHẢI: CHỚP SÁNG CHÓI MẮT LÀM CHOÁNG QUÁI (FLASH BURST)
        if (enableFlashBurst && Input.GetMouseButtonDown(1))
        {
            TryTriggerFlashBurst();
        }

        // 2. TIÊU HAO PIN CHO NIGHT VISION (x2) HOẶC ĐÈN PIN (x1)
        bool isNVOn = (NightVisionCamera.Instance != null && NightVisionCamera.Instance.IsNightVisionOn);

        if (isNVOn)
        {
            // BẬT NIGHT VISION -> TIÊU HAO PIN GẤP ĐÔI (x2.0)
            if (currentBattery > 0f)
            {
                currentBattery -= drainRate * 2.0f * Time.deltaTime;
                currentBattery = Mathf.Max(0f, currentBattery);

                if (currentBattery <= 0f)
                {
                    currentBattery = 0f;
                    NightVisionCamera.Instance.SetNightVision(false);
                    SetFlashlightState(false, true);
                    Debug.Log("[FlashlightToggle] 🔋 Đã cạn sạch pin! Tự động tắt Night Vision.");
                }
            }

            UpdateUI();
        }
        else if (isOn)
        {
            // BẬT ĐÈN PIN THƯỜNG -> TIÊU HAO PIN GỐC (x1.0) & TỰ ĐỘNG CHỐNG CHÓI
            if (enableAutoDimming)
            {
                if (cachedMainCam == null) cachedMainCam = Camera.main;
                Transform rayOrigin = (cachedMainCam != null) ? cachedMainCam.transform : transform;
                float targetMultiplier = 1.0f;

                // Dùng SphereCast hình cầu đường kính chùm tia đèn pin để bắt chính xác mọi vật thể/quái vật trước mặt
                if (Physics.SphereCast(rayOrigin.position, 0.4f, rayOrigin.forward, out RaycastHit hit, dimmingMaxDistance, dimmingLayerMask, QueryTriggerInteraction.Collide))
                {
                    float dist = hit.distance;
                    float t = Mathf.InverseLerp(dimmingMinDistance, dimmingMaxDistance, dist);
                    targetMultiplier = Mathf.Lerp(minDimmingMultiplier, 1.0f, t);
                }

                currentDimMultiplier = Mathf.Lerp(currentDimMultiplier, targetMultiplier, Time.deltaTime * dimmingSmoothSpeed);
            }
            else
            {
                currentDimMultiplier = 1.0f;
            }

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

                if (currentBattery <= 0f)
                {
                    SetFlashlightState(false, true);
                    Debug.Log("[FlashlightToggle] 🔋 Đèn pin đã cạn sạch pin! Tắt đèn pin.");
                }
            }

            UpdateUI();
        }
    }

    private void CreateScreenFlashOverlay()
    {
        if (screenFlashImage != null) return;

        GameObject canvasObj = new GameObject("ScreenFlashCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject imgObj = new GameObject("FlashOverlayImage");
        imgObj.transform.SetParent(canvasObj.transform, false);

        screenFlashImage = imgObj.AddComponent<UnityEngine.UI.Image>();
        screenFlashImage.color = new Color(1f, 1f, 1f, 0f);
        screenFlashImage.raycastTarget = false;

        RectTransform rt = screenFlashImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void TryTriggerFlashBurst()
    {
        if (!hasFlashlight) return;

        if (Time.time - lastFlashBurstTime < flashBurstCooldown)
        {
            return;
        }

        if (currentBattery < flashBatteryCost && currentBattery <= 0f)
        {
            PlayClickSoundOnly();
            return;
        }

        lastFlashBurstTime = Time.time;
        currentBattery = Mathf.Max(0f, currentBattery - flashBatteryCost);
        UpdateUI();

        // 1. Phát âm thanh chớp flash máy ảnh
        if (cameraFlashSound != null)
        {
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.PlayOneShot(cameraFlashSound, soundVolume);
        }

        // 2. Hiệu ứng ánh sáng bùng sáng chói lòa (Màn hình trắng chớp nhẹ + 3D Spot Light)
        StartCoroutine(FlashBurstVisualRoutine());

        // 3. Quét tất cả quái/chó trong tầm nhìn phía trước để làm choáng
        StunEnemiesInCone();
    }

    private System.Collections.IEnumerator FlashBurstVisualRoutine()
    {
        if (cachedMainCam == null) cachedMainCam = Camera.main;
        Transform camTrans = (cachedMainCam != null) ? cachedMainCam.transform : transform;

        // 1. Chớp sáng trắng toàn màn hình
        if (screenFlashImage == null) CreateScreenFlashOverlay();

        // 2. Tạo nguồn sáng chớp nháy siêu sáng tạm thời trong thế giới 3D
        GameObject burstObj = new GameObject("FlashBurstLight");
        burstObj.transform.position = camTrans.position + camTrans.forward * 0.2f;
        burstObj.transform.rotation = camTrans.rotation;

        Light burstLight = burstObj.AddComponent<Light>();
        burstLight.type = LightType.Spot;
        burstLight.spotAngle = flashBurstAngle + 20f;
        burstLight.range = flashBurstDistance + 5f;
        burstLight.intensity = 15.0f;
        burstLight.color = Color.white;
        burstLight.shadows = LightShadows.None;

        float flashDuration = 0.22f;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;

            // Fade trắng màn hình từ 0.75 xuống 0
            if (screenFlashImage != null)
            {
                float alpha = Mathf.Lerp(0.75f, 0f, t);
                screenFlashImage.color = new Color(1f, 1f, 1f, alpha);
            }

            // Giảm độ sáng đèn 3D
            if (burstObj != null && camTrans != null)
            {
                burstObj.transform.position = camTrans.position + camTrans.forward * 0.2f;
                burstObj.transform.rotation = camTrans.rotation;
                burstLight.intensity = Mathf.Lerp(15.0f, 0f, t);
            }

            yield return null;
        }

        if (screenFlashImage != null)
        {
            screenFlashImage.color = new Color(1f, 1f, 1f, 0f);
        }

        Destroy(burstObj);
    }

    public Camera GetActiveCamera()
    {
        if (cachedMainCam != null && cachedMainCam.enabled && cachedMainCam.gameObject.activeInHierarchy)
            return cachedMainCam;

        if (Camera.main != null && Camera.main.enabled && Camera.main.gameObject.activeInHierarchy)
        {
            cachedMainCam = Camera.main;
            return cachedMainCam;
        }

        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in cams)
        {
            if (c.enabled && c.gameObject.activeInHierarchy && c.targetTexture == null)
            {
                cachedMainCam = c;
                return cachedMainCam;
            }
        }
        if (cams.Length > 0) cachedMainCam = cams[0];
        return cachedMainCam;
    }

    private void StunEnemiesInCone()
    {
        Camera activeCam = GetActiveCamera();
        Transform camTrans = (activeCam != null) ? activeCam.transform : transform;

        // 1. LÀM CHOÁNG CON CHÓ (DOG)
        DogChaseBehavior[] dogs = Object.FindObjectsByType<DogChaseBehavior>(FindObjectsSortMode.None);
        foreach (var dog in dogs)
        {
            if (dog == null || !dog.gameObject.activeInHierarchy) continue;

            // Lấy tâm thân chó (bao gồm cả groundOffset và sensorHeight)
            Vector3 dogCenter = dog.transform.position + Vector3.up * Mathf.Max(0.5f, dog.sensorHeight);
            Vector3 dirToDog = dogCenter - camTrans.position;
            float dist = dirToDog.magnitude;

            // 1. Kiểm tra cự ly chớp flash (Mặc định: 25 mét siêu rộng)
            if (dist <= flashBurstDistance)
            {
                // 2. Kiểm tra góc nhìn: Bằng cả góc nón (Dot/Angle) HOẶC nằm trong khung nhìn màn hình Camera
                float angle = Vector3.Angle(camTrans.forward, dirToDog.normalized);
                bool inCone = (angle <= flashBurstAngle * 0.5f);

                bool inScreen = false;
                if (activeCam != null)
                {
                    Vector3 vp = activeCam.WorldToViewportPoint(dogCenter);
                    inScreen = (vp.z > 0 && vp.x >= -0.25f && vp.x <= 1.25f && vp.y >= -0.25f && vp.y <= 1.25f);
                }

                if (inCone || inScreen)
                {
                    // 3. Kiểm tra không bị tường dày ở giữa che khuất
                    if (Physics.Linecast(camTrans.position, dogCenter, out RaycastHit hit, flashObstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider == null || hit.collider.transform.root == dog.transform.root || hit.collider.transform.IsChildOf(dog.transform) || hit.distance >= dist - 1.5f)
                        {
                            dog.OnCameraFlashStunned();
                        }
                    }
                    else
                    {
                        dog.OnCameraFlashStunned();
                    }
                }
            }
        }

        // 2. LÀM CHOÁNG HÓA ĐÁ CON STRANGER
        StrangerBehavior[] strangers = Object.FindObjectsByType<StrangerBehavior>(FindObjectsSortMode.None);
        foreach (var stranger in strangers)
        {
            if (stranger == null || !stranger.gameObject.activeInHierarchy || stranger.isPermanentlyFrozen) continue;

            Vector3 strangerCenter = stranger.transform.position + Vector3.up * 1.0f;
            Vector3 dirToStranger = strangerCenter - camTrans.position;
            float dist = dirToStranger.magnitude;

            if (dist <= flashBurstDistance)
            {
                float angle = Vector3.Angle(camTrans.forward, dirToStranger.normalized);
                bool inCone = (angle <= flashBurstAngle * 0.5f);

                bool inScreen = false;
                if (activeCam != null)
                {
                    Vector3 vp = activeCam.WorldToViewportPoint(strangerCenter);
                    inScreen = (vp.z > 0 && vp.x >= -0.25f && vp.x <= 1.25f && vp.y >= -0.25f && vp.y <= 1.25f);
                }

                if (inCone || inScreen)
                {
                    if (Physics.Linecast(camTrans.position, strangerCenter, out RaycastHit hit, flashObstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider == null || hit.collider.transform.root == stranger.transform.root || hit.collider.transform.IsChildOf(stranger.transform) || hit.distance >= dist - 1.5f)
                        {
                            stranger.OnCameraFlashStunned();
                        }
                    }
                    else
                    {
                        stranger.OnCameraFlashStunned();
                    }
                }
            }
        }

        // 3. LÀM CHOÁNG YOSHIE (MẶT QUỶ BAY)
        YoshieBehavior[] yoshies = Object.FindObjectsByType<YoshieBehavior>(FindObjectsSortMode.None);
        foreach (var yoshie in yoshies)
        {
            if (yoshie == null || !yoshie.gameObject.activeInHierarchy) continue;

            Vector3 yoshieCenter = yoshie.transform.position;
            Vector3 dirToYoshie = yoshieCenter - camTrans.position;
            float dist = dirToYoshie.magnitude;

            if (dist <= flashBurstDistance)
            {
                float angle = Vector3.Angle(camTrans.forward, dirToYoshie.normalized);
                bool inCone = (angle <= flashBurstAngle * 0.5f);

                bool inScreen = false;
                if (activeCam != null)
                {
                    Vector3 vp = activeCam.WorldToViewportPoint(yoshieCenter);
                    inScreen = (vp.z > 0 && vp.x >= -0.25f && vp.x <= 1.25f && vp.y >= -0.25f && vp.y <= 1.25f);
                }

                if (inCone || inScreen)
                {
                    if (Physics.Linecast(camTrans.position, yoshieCenter, out RaycastHit hit, flashObstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider == null || hit.collider.transform.root == yoshie.transform.root || hit.collider.transform.IsChildOf(yoshie.transform) || hit.distance >= dist - 1.5f)
                        {
                            yoshie.OnCameraFlashStunned();
                        }
                    }
                    else
                    {
                        yoshie.OnCameraFlashStunned();
                    }
                }
            }
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
        UpdateUI();

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
        UpdateUI();
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
        float mult = (enableAutoDimming ? currentDimMultiplier : 1.0f) * flicker;

        if (spotHotspot != null) spotHotspot.intensity = origHotspotIntensity * mult;
        if (spotMidRing != null) spotMidRing.intensity = origMidRingIntensity * mult;
        if (spotAmbient != null) spotAmbient.intensity = origAmbientIntensity * mult;
    }

    private void ResetLightIntensities()
    {
        float mult = enableAutoDimming ? currentDimMultiplier : 1.0f;
        if (spotHotspot != null) spotHotspot.intensity = origHotspotIntensity * mult;
        if (spotMidRing != null) spotMidRing.intensity = origMidRingIntensity * mult;
        if (spotAmbient != null) spotAmbient.intensity = origAmbientIntensity * mult;
    }

    public void UpdateUI()
    {
        if (batterySliderUI != null)
        {
            batterySliderUI.gameObject.SetActive(hasFlashlight);
            batterySliderUI.maxValue = maxBattery;
            batterySliderUI.value = currentBattery;
        }

        if (batteryTextUI == null)
        {
            CamcorderUI cam = CamcorderUI.Instance ?? Object.FindFirstObjectByType<CamcorderUI>(FindObjectsInactive.Include);
            if (cam != null && cam.batteryText != null)
            {
                batteryTextUI = cam.batteryText;
            }
        }

        if (batteryTextUI != null)
        {
            // % PIN CHỈ HIỆN KHI NGƯỜI CHƠI ĐÃ CÓ ĐÈN PIN TRÊN TAY
            if (!hasFlashlight)
            {
                batteryTextUI.gameObject.SetActive(false);
            }
            else
            {
                batteryTextUI.gameObject.SetActive(true);
                int pct = Mathf.CeilToInt((currentBattery / maxBattery) * 100f);
                batteryTextUI.text = pct + "%";
            }
        }

        if (batteryFillImage != null)
        {
            batteryFillImage.gameObject.SetActive(hasFlashlight);
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
        savedBattery = 100f;
        savedHasFlashlight = -1;
        PlayerPrefs.DeleteKey("Global_Flashlight_Battery");
        PlayerPrefs.DeleteKey("Global_Has_Flashlight");
        PlayerPrefs.Save();
        if (Instance != null)
        {
            Instance.currentBattery = Instance.maxBattery;
            Instance.hasFlashlight = false;
            Instance.SetFlashlightState(false, false);
            Instance.UnequipFlashlight();
        }
    }
}