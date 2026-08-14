using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CockroachNightmareWakeUp : MonoBehaviour
{
    [Header("1. Điểm Tọa Độ Nệm (Bed Points)")]
    [Tooltip("Kéo SitCameraPoint (mép đệm) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM trong Scene!")]
    public Transform sitCameraPoint;

    [Tooltip("Kéo PillowCameraPoint (sát gối) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM trong Scene!")]
    public Transform pillowCameraPoint;

    [Header("2. Thời Gian & Hiệu Ứng Bật Dậy Ác Mộng (Nightmare Startle)")]
    [Tooltip("Thời gian nín thở trong bóng tối trước khi mở mắt (giây - Mặc định: 1.0s)")]
    public float darkPauseDuration = 1.0f;

    [Tooltip("Thời gian mở mắt chớp mí dần dần (giây - Mặc định: 1.5s)")]
    public float openEyesFadeInDuration = 1.5f;

    [Tooltip("Thời gian bật ngồi dậy nhanh như bị giật mình (giây - Mặc định: 0.75s)")]
    public float startleSitUpDuration = 0.75f;

    [Tooltip("Độ giật nảy của Camera lúc vừa bật dậy (Mặc định: 0.05)")]
    public float startleJoltMagnitude = 0.05f;

    [Tooltip("Thời gian ngồi thở dốc / hoàn hồn ở mép đệm trước khi đứng lên (giây - Mặc định: 1.2s)")]
    public float sitBreathingDuration = 1.2f;

    [Tooltip("Thời gian nâng người đứng dậy từ nệm (giây - Mặc định: 0.85s)")]
    public float standUpDuration = 0.85f;

    [Header("3. Âm Thanh Thở Dốc / Giật Mình (Tùy Chọn)")]
    [Tooltip("Tiếng hít hà / thở hắt lúc vừa mở mắt bật dậy")]
    public AudioClip wakeUpGaspAudio;

    [Tooltip("Tiếng thở dốc / tim đập lúc ngồi hoàn hồn ở mép đệm")]
    public AudioClip heavyBreathingAudio;

    [Range(0f, 1f)]
    public float audioVolume = 0.85f;

    [Header("4. Tự Động Đóng Cửa Chính")]
    [Tooltip("Kéo Cửa chính (chứa DoorExit) vào đây. Nếu để trống sẽ TỰ ĐỘNG TÌM trong Scene!")]
    public DoorExit doorToClose;

    [Header("5. Fade Screen Image")]
    [Tooltip("Kéo FadeScreen Image vào đây. Nếu để trống sẽ tự động liên kết")]
    public Image fadeScreenImage;

    [Header("6. Phím Tắt Debug Test Nhanh")]
    [Tooltip("Bấm phím này trong lúc Play để kích hoạt ngay chuỗi tỉnh dậy thử nghiệm (Mặc định: Phím U)")]
    public KeyCode debugWakeUpKey = KeyCode.U;

    private AudioSource audioSource;
    private bool isWakingUp = false;
    private Vector3 originalCameraLocalPos;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Âm thanh 2D chân thực sát tai
    }

    void Start()
    {
        AutoFindPoints();

        MovePl player = Object.FindFirstObjectByType<MovePl>();
        if (player != null && player.cameraTransform != null)
        {
            originalCameraLocalPos = player.cameraTransform.localPosition;
        }
        else if (Camera.main != null)
        {
            originalCameraLocalPos = Camera.main.transform.localPosition;
        }
    }

    void AutoFindPoints()
    {
        if (sitCameraPoint == null)
        {
            GameObject sitObj = GameObject.Find("SitCameraPoint");
            if (sitObj != null) sitCameraPoint = sitObj.transform;
        }

        if (pillowCameraPoint == null)
        {
            GameObject pillowObj = GameObject.Find("PillowCameraPoint");
            if (pillowObj != null) pillowCameraPoint = pillowObj.transform;
        }

        if (fadeScreenImage == null)
        {
            // 1. Tìm từ các script khác đã có sẵn
            CockroachFlyAttack fly = Object.FindFirstObjectByType<CockroachFlyAttack>(FindObjectsInactive.Include);
            if (fly != null && fly.fadeScreenImage != null) fadeScreenImage = fly.fadeScreenImage;

            if (fadeScreenImage == null)
            {
                BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
                if (bed != null && bed.fadeScreenImage != null) fadeScreenImage = bed.fadeScreenImage;
            }

            if (fadeScreenImage == null)
            {
                NPCDeliveryBox box = Object.FindFirstObjectByType<NPCDeliveryBox>(FindObjectsInactive.Include);
                if (box != null && box.fadeScreenImage != null) fadeScreenImage = box.fadeScreenImage;
            }

            if (fadeScreenImage == null)
            {
                GameObject fadeObj = GameObject.Find("FadeScreen");
                if (fadeObj == null) fadeObj = GameObject.Find("FadeImage");
                if (fadeObj == null) fadeObj = GameObject.Find("BlackScreen");
                if (fadeObj != null) fadeScreenImage = fadeObj.GetComponent<Image>();
            }
        }

        // Tự động mượn âm thanh thở/rên nếu có trong BedSleepCutscene
        if (wakeUpGaspAudio == null)
        {
            BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
            if (bed != null && bed.wakeUpGroanAudio != null)
            {
                wakeUpGaspAudio = bed.wakeUpGroanAudio;
            }
        }
    }

    void OnEnable()
    {
        CockroachFlyAttack.OnCockroachNightmareEnded += HandleCockroachNightmareEnded;
    }

    void OnDisable()
    {
        CockroachFlyAttack.OnCockroachNightmareEnded -= HandleCockroachNightmareEnded;
    }

    void Update()
    {
        if (debugWakeUpKey != KeyCode.None && Input.GetKeyDown(debugWakeUpKey))
        {
            Debug.Log($"[CockroachNightmareWakeUp] ⌨️ Bấm phím [{debugWakeUpKey}]! Test chuỗi bật dậy sau ác mộng...");
            StartWakeUpSequence();
        }
    }

    void HandleCockroachNightmareEnded()
    {
        Debug.Log("[CockroachNightmareWakeUp] 🛏️ Nhận được tín hiệu kết thúc ác mộng gián! Bắt đầu chuỗi bừng tỉnh dậy trên nệm...");
        StartWakeUpSequence();
    }

    public void StartWakeUpSequence()
    {
        if (isWakingUp) return;
        StartCoroutine(WakeUpFromNightmareRoutine());
    }

    IEnumerator WakeUpFromNightmareRoutine()
    {
        isWakingUp = true;
        AutoFindPoints();

        MovePl player = Object.FindFirstObjectByType<MovePl>();
        if (player == null)
        {
            Debug.LogWarning("[CockroachNightmareWakeUp] ⚠️ Không tìm thấy MovePl Player!");
            isWakingUp = false;
            yield break;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        Transform camTrans = (player.cameraTransform != null) ? player.cameraTransform : Camera.main.transform;

        // ========================================================
        // BƯỚC 1: KHÓA PLAYER & DỊCH CHUYỂN VỀ NỆM TRONG BÓNG TỐI
        // ========================================================
        player.SetMovementState(false);
        player.isCameraLocked = true;

        if (cc != null) cc.enabled = false;

        // Teleport Player body về vị trí SitPoint trên nệm
        if (sitCameraPoint != null)
        {
            Vector3 targetPlayerPos = sitCameraPoint.position;
            targetPlayerPos.y = player.transform.position.y;
            player.transform.position = targetPlayerPos;
            player.transform.rotation = Quaternion.Euler(0f, sitCameraPoint.eulerAngles.y, 0f);
        }

        // Đặt Camera nằm ngửa sát gối nhìn lên trần nhà
        if (pillowCameraPoint != null)
        {
            camTrans.position = pillowCameraPoint.position;
            camTrans.rotation = pillowCameraPoint.rotation;
        }

        // ẨN TOÀN BỘ GIÁN VÀ TRIGGER CỬA (An toàn không làm tắt chính script này)
        GameObject roachGroup = GameObject.Find("Cockroach");
        if (roachGroup != null && roachGroup != gameObject && !transform.IsChildOf(roachGroup.transform))
        {
            roachGroup.SetActive(false);
        }

        CockroachDoorAttackTrigger doorTrigger = Object.FindFirstObjectByType<CockroachDoorAttackTrigger>(FindObjectsInactive.Include);
        if (doorTrigger != null && doorTrigger.gameObject != gameObject)
        {
            doorTrigger.gameObject.SetActive(false);
        }

        // TỰ ĐỘNG ĐÓNG CỬA CHÍNH LẠI TRONG BÓNG TỐI
        if (doorToClose == null) doorToClose = Object.FindFirstObjectByType<DoorExit>();
        if (doorToClose != null)
        {
            doorToClose.CloseDoor(true);
            Debug.Log("[CockroachNightmareWakeUp] 🚪 Đã tự động đóng cửa chính lại trong bóng tối!");
        }

        // Chờ trong bóng tối tĩnh lặng (khoảng lặng hồi hộp sau cơn ác mộng)
        if (darkPauseDuration > 0f)
        {
            yield return new WaitForSeconds(darkPauseDuration);
        }

        // ========================================================
        // BƯỚC 2: MỞ MẮT SÁNG DẦN TỪ BÓNG TỐI + THỞ HẮT (OPEN EYES FADE IN)
        // ========================================================
        if (wakeUpGaspAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(wakeUpGaspAudio, audioVolume);
        }

        if (fadeScreenImage != null)
        {
            fadeScreenImage.gameObject.SetActive(true);
            Color color = fadeScreenImage.color;
            color.a = 1f;
            fadeScreenImage.color = color;

            float elapsed = 0f;
            while (elapsed < openEyesFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / openEyesFadeInDuration);

                // Mở mắt mượt mà từ 1.0 xuống 0.0
                color.a = 1f - progress;
                fadeScreenImage.color = color;
                yield return null;
            }

            color.a = 0f;
            fadeScreenImage.color = color;
            fadeScreenImage.gameObject.SetActive(false);
        }

        // ========================================================
        // BƯỚC 3: BẬT NGỒI DẬY NHANH NHƯ GẶP ÁC MỘNG (STARTLED SIT UP)
        // ========================================================
        if (sitCameraPoint != null && pillowCameraPoint != null)
        {
            Vector3 startPos = camTrans.position;
            Quaternion startRot = camTrans.rotation;

            Vector3 sitPos = sitCameraPoint.position;
            Quaternion sitRot = sitCameraPoint.rotation;

            float sitElapsed = 0f;
            while (sitElapsed < startleSitUpDuration)
            {
                sitElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(sitElapsed / startleSitUpDuration);

                // Dùng đường cong gia tốc nhanh ở đầu (giật mình bật dậy) rồi hãm lại ở mép đệm
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);

                // Rung nhẹ camera lúc bật dậy
                float joltY = Mathf.Sin(t * Mathf.PI) * startleJoltMagnitude;

                camTrans.position = Vector3.Lerp(startPos, sitPos, smoothT) + new Vector3(0f, joltY, 0f);
                camTrans.rotation = Quaternion.Slerp(startRot, sitRot, smoothT);
                yield return null;
            }

            camTrans.position = sitPos;
            camTrans.rotation = sitRot;
        }

        // ========================================================
        // BƯỚC 4: NGỒI THỞ DỐC Ở MÉP ĐỆM HOÀN HỒN
        // ========================================================
        if (heavyBreathingAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(heavyBreathingAudio, audioVolume * 0.8f);
        }

        if (sitBreathingDuration > 0f)
        {
            // Camera hơi nhấp nhô nhẹ theo nhịp thở dốc
            float breatheElapsed = 0f;
            Vector3 sitBasePos = camTrans.position;

            while (breatheElapsed < sitBreathingDuration)
            {
                breatheElapsed += Time.deltaTime;
                float breathOffset = Mathf.Sin(breatheElapsed * 6f) * 0.015f;
                camTrans.position = sitBasePos + new Vector3(0f, breathOffset, 0f);
                yield return null;
            }

            camTrans.position = sitBasePos;
        }

        // ========================================================
        // BƯỚC 5: NÂNG NGƯỜI ĐỨNG LÊN & TRẢ LẠI QUYỀN ĐIỀU KHIỂN CHO PLAYER
        // ========================================================
        if (sitCameraPoint != null)
        {
            player.transform.rotation = Quaternion.Euler(0f, sitCameraPoint.eulerAngles.y, 0f);

            float targetPitch = sitCameraPoint.eulerAngles.x;
            if (targetPitch > 180f) targetPitch -= 360f;
            targetPitch = Mathf.Clamp(targetPitch, -90f, 90f);

            Vector3 startLocalPos = camTrans.localPosition;
            Vector3 targetLocalPos = (originalCameraLocalPos != Vector3.zero) ? originalCameraLocalPos : new Vector3(0f, 0.6f, 0f);
            Quaternion startLocalRot = camTrans.localRotation;
            Quaternion targetLocalRot = Quaternion.Euler(targetPitch, 0f, 0f);

            float standElapsed = 0f;
            while (standElapsed < standUpDuration)
            {
                standElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, standElapsed / standUpDuration);

                camTrans.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
                camTrans.localRotation = Quaternion.Slerp(startLocalRot, targetLocalRot, t);
                yield return null;
            }

            camTrans.localPosition = targetLocalPos;
            camTrans.localRotation = targetLocalRot;
        }

        // Bật lại CharacterController và mở khóa Player
        if (cc != null) cc.enabled = true;

        player.SyncRotationWithCurrentCamera();
        player.isCameraLocked = false;
        player.SetMovementState(true);

        isWakingUp = false;
        Debug.Log("[CockroachNightmareWakeUp] ✅ Player đã bừng tỉnh dậy trên nệm! Đã mở khóa hoàn toàn di chuyển & góc nhìn chuột!");
    }
}
