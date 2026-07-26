using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class ChromaticPulse : MonoBehaviour
{
    [Header("Cài đặt nhịp đập Chromatic")]
    [Tooltip("Tốc độ đập. Số lớn đập nhanh hơn (VD: 2.0 - 4.0).")]
    public float pulseSpeed = 3f;
    
    [Tooltip("Cường độ (Intensity) tối thiểu lúc bình thường")]
    public float minIntensity = 0.3f;
    
    [Tooltip("Cường độ (Intensity) tối đa lúc nhói lên")]
    public float maxIntensity = 0.5f;

    private Volume volume;
    private ChromaticAberration chromaticAberration;

    void Start()
    {
        // Lấy component Volume gắn trên cùng GameObject
        volume = GetComponent<Volume>();
        
        // Cố gắng tìm hiệu ứng Chromatic Aberration bên trong Profile
        // volume.profile sẽ tự động tạo một bản copy lúc chạy game để không làm hỏng file gốc
        if (volume.profile.TryGet(out chromaticAberration))
        {
            // Cấp quyền cho code được phép đè thông số
            chromaticAberration.intensity.overrideState = true;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Chromatic Aberration trong Volume Profile. Hãy kiểm tra lại xem đã Add Override chưa nhé!");
        }
    }

    void Update()
    {
        if (chromaticAberration != null)
        {
            // Dùng sóng Sin để tạo nhịp đập mượt mà (chạy theo thời gian thực)
            float sinValue = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) / 2f;
            
            // Tính toán giá trị cường độ mới (nội suy giữa Min và Max)
            float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, sinValue);
            
            // Gán thông số mới vào Chromatic Aberration
            chromaticAberration.intensity.value = currentIntensity;
        }
    }
}
