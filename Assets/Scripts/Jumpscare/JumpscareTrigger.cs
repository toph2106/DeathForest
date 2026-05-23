using UnityEngine;
using UnityEngine.Rendering; // Quản lý hệ thống Volume cơ bản
using System.Collections;

public class JumpscareTrigger : MonoBehaviour
{
    [Header("References")]
    public GameObject monster;               // Slenderman đang đứng trên map
    public Volume postProcessingVolume;      // Kéo Global Volume vào đây để làm nhòe

    [Header("Jumpscare Settings")]
    public float duration = 5f;              // Thời gian jumpscare (5 giây)
    public float cameraPanSpeed = 5f;        // Tốc độ lia cam hướng về quái vật

    [Header("Blur Settings (Kinh Dị)")]
    public float blurIntensity = 1f;         // Độ nhòe khi dính jumpscare (Số càng cao càng nhòe)
    public float shakeIntensity = 0.05f;     // Độ mạnh của cú rung cam hoảng loạn

    [Header("Audio Settings")]
    public AudioSource jumpscareAudioSource; // Kéo Object Audio Source vào đây

    // Không cần hiển thị ngoài Inspector để tránh kéo thả thủ công lỗi Prefab
    private PlayerTestJumpscare playerScript; 
    private Vector3 originalCamLocalPos;     
    private bool isJumpscareActive = false;
    private bool hasTriggered = false;
    
    // Gọi thẳng trực tiếp qua lớp Universal để không bị lỗi RenderGraph trên Unity 6
    private UnityEngine.Rendering.Universal.ChromaticAberration chromaticAberration;

    void Start()
    {
        if (monster != null)
        {
            monster.SetActive(true); // Quái xuất hiện sẵn trên map
        }

        // Tự động tìm hiệu ứng Chromatic Aberration từ Volume Profile
        if (postProcessingVolume != null && postProcessingVolume.profile != null)
        {
            if (postProcessingVolume.profile.TryGet<UnityEngine.Rendering.Universal.ChromaticAberration>(out var ca))
            {
                chromaticAberration = ca;
                chromaticAberration.intensity.Override(0f); // Ban đầu game bình thường, không nhòe
            }
        }

        if (jumpscareAudioSource != null)
        {
            jumpscareAudioSource.playOnAwake = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Tự động nhận diện Player khi va chạm để phục vụ làm Prefab
        if (other.CompareTag("Player") && !hasTriggered)
        {
            playerScript = other.GetComponent<PlayerTestJumpscare>();

            if (playerScript != null && playerScript.playerCamera != null)
            {
                hasTriggered = true;
                
                // Ghi nhớ vị trí gốc của Camera trước khi bắt đầu rung lắc
                originalCamLocalPos = playerScript.playerCamera.transform.localPosition;

                // Tước quyền di chuyển và điều khiển chuột của người chơi
                playerScript.canMove = false;
                playerScript.canLook = false;
                
                isJumpscareActive = true;

                // Kích hoạt nhòe màn hình ngay lập tức
                if (chromaticAberration != null)
                {
                    chromaticAberration.intensity.Override(blurIntensity); 
                }

                // Phát âm thanh tiếng thét
                if (jumpscareAudioSource != null)
                {
                    jumpscareAudioSource.Play();
                }

                StartCoroutine(ExecuteJumpscareRoutine());
            }
        }
    }

    void LateUpdate()
    {
        if (isJumpscareActive && playerScript != null && monster != null)
        {
            Camera playerCam = playerScript.playerCamera;

            // 1. Ép thân Player quay trục ngang về phía quái vật
            Vector3 targetPostionLook = new Vector3(monster.transform.position.x, playerScript.transform.position.y, monster.transform.position.z);
            Vector3 playerToTarget = targetPostionLook - playerScript.transform.position;
            if (playerToTarget != Vector3.zero)
            {
                Quaternion targetPlayerRot = Quaternion.LookRotation(playerToTarget);
                playerScript.transform.rotation = Quaternion.Slerp(playerScript.transform.rotation, targetPlayerRot, cameraPanSpeed * Time.deltaTime);
            }

            // 2. Ép góc ngẩng Camera nhìn thẳng vào quái vật
            Vector3 camToTarget = monster.transform.position - playerCam.transform.position;
            if (camToTarget != Vector3.zero)
            {
                Quaternion targetCamRot = Quaternion.LookRotation(camToTarget);
                playerCam.transform.rotation = Quaternion.Slerp(playerCam.transform.rotation, targetCamRot, cameraPanSpeed * Time.deltaTime);
            }

            // 3. Xử lý rung lắc camera (Giữ nguyên không Zoom FOV)
            Vector3 randomShakeOffset = Random.insideUnitSphere * shakeIntensity;
            playerCam.transform.localPosition = originalCamLocalPos + randomShakeOffset;

            // Hiệu ứng nhòe nhiễu màu chớp tắt liên tục (Giống hình mẫu bạn gửi)
            if (chromaticAberration != null)
            {
                float ghostFlicker = Random.Range(blurIntensity * 0.6f, blurIntensity * 1.0f);
                chromaticAberration.intensity.Override(ghostFlicker);
            }
        }
    }

    IEnumerator ExecuteJumpscareRoutine()
    {
        yield return new WaitForSeconds(duration);

        isJumpscareActive = false;
        
        if (monster != null)
        {
            monster.SetActive(false);
        }

        // Tắt hiệu ứng nhòe màn hình
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.Override(0f);
        }

        // Đưa camera về lại vị trí gốc ban đầu (Hết rung lắc)
        if (playerScript != null && playerScript.playerCamera != null)
        {
            playerScript.playerCamera.transform.localPosition = originalCamLocalPos;
        }

        // Trả lại quyền di chuyển tự do cho Player
        if (playerScript != null)
        {
            playerScript.canMove = true;
            playerScript.canLook = true;
        }

        // Tự hủy vùng bẫy này
        gameObject.SetActive(false);
    }
}