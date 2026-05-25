using UnityEngine;

/// <summary>
/// PerformanceOptimizer - Tối ưu hiệu năng tổng thể cho game.
/// Gắn vào 1 GameObject trống trong Scene (ví dụ: "GameManager").
/// </summary>
public class PerformanceOptimizer : MonoBehaviour
{
    [Header("=== CÀI ĐẶT FPS ===")]
    [Tooltip("FPS mục tiêu (khuyến nghị 60 cho PC, 30 cho máy yếu)")]
    public int targetFrameRate = 60;

    [Tooltip("Tắt VSync để FPS không bị khóa theo tần số màn hình")]
    public bool disableVSync = true;

    [Header("=== TỰ ĐỘNG ĐIỀU CHỈNH CHẤT LƯỢNG ===")]
    [Tooltip("Bật tính năng tự động giảm chất lượng khi FPS thấp")]
    public bool enableAdaptiveQuality = true;

    [Tooltip("Nếu FPS xuống dưới ngưỡng này, sẽ tự động giảm chất lượng")]
    public int lowFPSThreshold = 30;

    [Tooltip("Nếu FPS trên ngưỡng này, sẽ tự động tăng lại chất lượng")]
    public int highFPSThreshold = 55;

    [Header("=== SHADOW (ĐỔ BÓNG) ===")]
    [Tooltip("Khoảng cách shadow tối đa (giảm = tăng FPS đáng kể)")]
    public float maxShadowDistance = 40f;

    [Tooltip("Khoảng cách shadow tối thiểu khi FPS thấp")]
    public float minShadowDistance = 15f;

    [Header("=== LOD (Level of Detail) ===")]
    [Tooltip("LOD Bias tối đa (chất lượng cao)")]
    public float maxLODBias = 2f;

    [Tooltip("LOD Bias tối thiểu khi FPS thấp")]
    public float minLODBias = 0.5f;

    [Header("=== TEXTURE STREAMING ===")]
    [Tooltip("Bật Texture Streaming để giảm VRAM (rất hiệu quả với map lớn)")]
    public bool enableTextureStreaming = true;

    [Tooltip("Giới hạn bộ nhớ cho Texture Streaming (MB)")]
    public int textureStreamingBudgetMB = 512;

    [Header("=== DEBUG ===")]
    [Tooltip("Hiển thị FPS trên màn hình")]
    public bool showFPSCounter = true;

    // --- Internal ---
    // Dùng mảng lưu FPS nhiều frame để tính trung bình → hiển thị ổn định hơn
    private const int FPS_SAMPLE_COUNT = 60;
    private float[] fpsSamples;
    private int fpsSampleIndex;
    private float smoothFPS;

    private float currentShadowDistance;
    private float currentLODBias;
    private float targetShadowDistance;
    private float targetLODBias;
    private float adaptiveTimer;
    private const float ADAPTIVE_CHECK_INTERVAL = 3f;

    private GUIStyle fpsStyle;
    private GUIStyle shadowStyle;

    void Awake()
    {
        // Đảm bảo chỉ có 1 instance
        var all = FindObjectsByType<PerformanceOptimizer>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 1. Cài đặt FPS Target
        Application.targetFrameRate = targetFrameRate;

        // 2. Tắt VSync nếu cần
        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }

        // 3. Bật Texture Streaming
        if (enableTextureStreaming)
        {
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = textureStreamingBudgetMB;
            QualitySettings.streamingMipmapsAddAllCameras = true;
        }

        // 4. Tối ưu Physics
        Physics.defaultSolverIterations = 4;
        Physics.defaultSolverVelocityIterations = 1;

        // 5. Khởi tạo giá trị shadow và LOD
        currentShadowDistance = maxShadowDistance;
        currentLODBias = maxLODBias;
        targetShadowDistance = maxShadowDistance;
        targetLODBias = maxLODBias;

        // 6. Khởi tạo mảng FPS samples
        fpsSamples = new float[FPS_SAMPLE_COUNT];
        for (int i = 0; i < FPS_SAMPLE_COUNT; i++)
        {
            fpsSamples[i] = targetFrameRate;
        }

        // GUI Style
        fpsStyle = new GUIStyle();
        fpsStyle.fontSize = 22;
        fpsStyle.fontStyle = FontStyle.Bold;

        shadowStyle = new GUIStyle();
        shadowStyle.fontSize = 16;
        shadowStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        Debug.Log("[PerformanceOptimizer] Khởi tạo thành công! Target FPS: " + targetFrameRate);
    }

    void Update()
    {
        // --- Tính FPS trung bình mượt (rolling average) ---
        float currentFrameFPS = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        fpsSamples[fpsSampleIndex] = currentFrameFPS;
        fpsSampleIndex = (fpsSampleIndex + 1) % FPS_SAMPLE_COUNT;

        float sum = 0f;
        for (int i = 0; i < FPS_SAMPLE_COUNT; i++)
        {
            sum += fpsSamples[i];
        }
        smoothFPS = sum / FPS_SAMPLE_COUNT;

        // --- Adaptive Quality ---
        if (enableAdaptiveQuality)
        {
            adaptiveTimer += Time.unscaledDeltaTime;
            if (adaptiveTimer >= ADAPTIVE_CHECK_INTERVAL)
            {
                AdaptiveQualityAdjust();
                adaptiveTimer = 0f;
            }

            // Lerp mượt mà tới giá trị mục tiêu thay vì nhảy đột ngột
            currentShadowDistance = Mathf.Lerp(currentShadowDistance, targetShadowDistance, Time.unscaledDeltaTime * 2f);
            currentLODBias = Mathf.Lerp(currentLODBias, targetLODBias, Time.unscaledDeltaTime * 2f);

            QualitySettings.shadowDistance = currentShadowDistance;
            QualitySettings.lodBias = currentLODBias;
        }
    }

    /// <summary>
    /// Tự động điều chỉnh mục tiêu Shadow Distance và LOD Bias dựa trên FPS.
    /// Dùng Lerp để chuyển đổi mượt, tránh giật.
    /// </summary>
    void AdaptiveQualityAdjust()
    {
        if (smoothFPS < lowFPSThreshold)
        {
            // FPS thấp → giảm chất lượng
            targetShadowDistance = Mathf.Max(targetShadowDistance - 3f, minShadowDistance);
            targetLODBias = Mathf.Max(targetLODBias - 0.15f, minLODBias);
        }
        else if (smoothFPS > highFPSThreshold)
        {
            // FPS ổn → tăng lại chất lượng (tăng chậm hơn giảm)
            targetShadowDistance = Mathf.Min(targetShadowDistance + 1f, maxShadowDistance);
            targetLODBias = Mathf.Min(targetLODBias + 0.05f, maxLODBias);
        }
    }

    void OnGUI()
    {
        if (!showFPSCounter) return;

        // Đổi màu theo FPS
        if (smoothFPS >= 50)
            fpsStyle.normal.textColor = Color.green;
        else if (smoothFPS >= 30)
            fpsStyle.normal.textColor = Color.yellow;
        else
            fpsStyle.normal.textColor = Color.red;

        GUI.Label(new Rect(10, 10, 300, 30), $"FPS: {smoothFPS:F0}", fpsStyle);

        if (enableAdaptiveQuality)
        {
            GUI.Label(new Rect(10, 38, 300, 25),
                $"Shadow: {currentShadowDistance:F0}m | LOD: {currentLODBias:F1}", shadowStyle);
        }
    }

    /// <summary>
    /// Gọi từ Menu Settings để cho phép người chơi tùy chỉnh chất lượng.
    /// quality: 0 = Thấp, 1 = Trung Bình, 2 = Cao
    /// </summary>
    public void SetQualityPreset(int quality)
    {
        switch (quality)
        {
            case 0: // Thấp
                targetFrameRate = 30;
                maxShadowDistance = 15f;
                maxLODBias = 0.5f;
                break;
            case 1: // Trung bình
                targetFrameRate = 60;
                maxShadowDistance = 30f;
                maxLODBias = 1f;
                break;
            case 2: // Cao
                targetFrameRate = 120;
                maxShadowDistance = 50f;
                maxLODBias = 2f;
                break;
        }

        Application.targetFrameRate = targetFrameRate;
        targetShadowDistance = maxShadowDistance;
        targetLODBias = maxLODBias;

        Debug.Log($"[PerformanceOptimizer] Chuyển sang Quality Preset: {quality}");
    }
}
