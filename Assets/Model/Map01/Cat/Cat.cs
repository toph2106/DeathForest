using UnityEngine;
using System.Collections;

public class Cat : MonoBehaviour, IInteractable
{
    [Header("1. Chuyển Động Xoay Nhìn Player (Look At Player)")]
    [Tooltip("Tích chọn để con mèo tự động xoay đầu/thân hướng về phía Player khi Player di chuyển xung quanh")]
    public bool enableLookAtPlayer = true;

    [Tooltip("Tốc độ xoay mượt mà của con mèo")]
    public float rotationSpeed = 5f;

    [Tooltip("Kéo Player Transform vào đây. Nếu để trống sẽ tự động tìm GameObject có tag 'Player' hoặc component MovePl")]
    public Transform playerTransform;

    [Header("2. Cấu Hình Nhảy & Di Chuyển (Oiia Oiia Dance Area)")]
    [Tooltip("Kéo BoxCollider đại diện cho vùng di chuyển trên giường/nệm vào đây")]
    public BoxCollider moveAreaBounds;

    [Tooltip("Thời gian chờ ban đầu sau khi tương tác trước khi bắt đầu bật nhạc và di chuyển (giây, Mặc định: 2.0s)")]
    public float initialInteractDelay = 2.0f;

    [Tooltip("Thời gian Mèo thực hiện mỗi đợt di chuyển (giây, Mặc định: 6.0s)")]
    public float animDuration = 6.0f;

    [Tooltip("Thời gian tạm dừng di chuyển giữa các đợt (giây, Mặc định: 2.0s)")]
    public float pauseBetweenDance = 2.0f;

    [Tooltip("Tốc độ di chuyển của con mèo trong vùng (Mặc định: 1.5)")]
    public float moveSpeed = 1.5f;

    [Header("3. Âm Thanh & Animation Khi Tương Tác")]
    [Tooltip("Âm thanh meme Oiia Oiia của con mèo")]
    public AudioClip catSound;

    [Range(0f, 1f)]
    public float catSoundVolume = 0.8f;

    [Header("4. Cấu Hình Không Gian Âm Thanh 3D (3D Sound)")]
    [Tooltip("Khoảng cách tối đa để nghe thấy tiếng mèo (Mặc định: 15m)")]
    public float audioMaxDistance = 15f;

    [Tooltip("Khoảng cách bắt đầu giảm dần âm lượng (Mặc định: 1m)")]
    public float audioMinDistance = 1f;

    [Header("5. Chữ Nhắc Tương Tác (Prompt UI)")]
    public string englishPrompt = "Shoo the cat away";
    public string vietnamesePrompt = "Đuổi mèo đi chỗ khác";

    [Header("6. Mở Khóa Tương Tác Ban Đầu (Tùy chọn)")]
    [Tooltip("Tích chọn nếu muốn khóa tương tác Mèo lúc đầu (Mặc định: false - Cho phép tương tác tự do)")]
    public bool lockOnStart = false;

    [Tooltip("Kéo Collider của Nệm Ngủ vào đây (Tùy chọn)")]
    public Collider bedColliderToEnable;

    [Header("7. Lời Thoại Khi Tương Tác Với Mèo")]
    [Tooltip("Danh sách thoại khi tương tác với mèo. Thêm (+) hoặc để trống")]
    public SmartInteractionDialogue.DialogueLine[] catDialogueLines = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Ngoan nào... đi ra chỗ khác chơi nhé.",
            englishDialogue = "Good kitty... go play over there."
        }
    };

    [Header("7.1. Âm Thanh Thoại Gõ Chữ (Dialogue SFX)")]
    [Tooltip("Gói âm thanh lồng tiếng / gõ chữ khi hiện phụ đề tương tác với mèo (5s blip)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)]
    [Tooltip("Âm lượng âm thanh thoại gõ chữ (Mặc định: 0.8)")]
    public float dialogueVolume = 0.8f;

    [Header("8. TriggerMeow Cần Ẩn Khi Mèo Tới Hộp Giấy")]
    [Tooltip("Kéo GameObject TriggerMeow vào đây để tự động ẩn khi mèo tới điểm Meow")]
    public GameObject triggerMeowToDisable;

    public bool hasArrivedAtTargetPoint { get; private set; } = false;

    private Collider catCollider;
    private Animator catAnimator;
    private AudioSource catAudioSource;
    private InteractPrompt interactPrompt;
    private bool isCatActive = false;
    private Coroutine catLoopCoroutine;

    private bool isFadingOut = false;

    void Awake()
    {
        catCollider = GetComponent<Collider>();
        catAnimator = GetComponent<Animator>();
        if (catAnimator == null) catAnimator = GetComponentInChildren<Animator>();

        catAudioSource = GetComponent<AudioSource>();
        if (catAudioSource == null) catAudioSource = gameObject.AddComponent<AudioSource>();

        catAudioSource.volume = catSoundVolume;
        interactPrompt = GetComponent<InteractPrompt>();
    }

    void OnValidate()
    {
        if (catAudioSource != null)
        {
            catAudioSource.volume = catSoundVolume;
            UpdateAudio3DSettings();
        }
    }

    void Start()
    {
        if (lockOnStart && catCollider != null)
        {
            catCollider.enabled = false;
        }
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                MovePl movePl = Object.FindFirstObjectByType<MovePl>();
                if (movePl != null) playerObj = movePl.gameObject;
            }

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        if (bedColliderToEnable == null)
        {
            BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>();
            if (bed != null) bedColliderToEnable = bed.GetComponent<Collider>();
        }

        if (interactPrompt == null)
        {
            interactPrompt = gameObject.AddComponent<InteractPrompt>();
        }
        interactPrompt.englishPrompt = englishPrompt;
        interactPrompt.vietnamesePrompt = vietnamesePrompt;

        UpdateAudio3DSettings();
        if (catAudioSource != null)
        {
            catAudioSource.volume = catSoundVolume;
        }
    }

    /// <summary>
    /// GỌI HÀM NÀY ĐỂ MỞ KHÓA TƯƠNG TÁC CON MÈO (Được gọi khi TẮT CASE PC)
    /// </summary>
    public void UnlockCat()
    {
        if (catCollider == null) catCollider = GetComponent<Collider>();
        if (catCollider != null)
        {
            catCollider.enabled = true;
            Debug.Log("[Cat] 🔓 ĐÃ MỞ KHÓA TƯƠNG TÁC CHO CON MÈO!");
        }
    }

    void UpdateAudio3DSettings()
    {
        if (catAudioSource != null)
        {
            catAudioSource.spatialBlend = 1f;
            catAudioSource.rolloffMode = AudioRolloffMode.Linear;
            catAudioSource.minDistance = audioMinDistance;
            catAudioSource.maxDistance = audioMaxDistance;
            catAudioSource.dopplerLevel = 0f;
            catAudioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        // Đồng bộ âm lượng thời gian thực
        if (catAudioSource != null && !isFadingOut)
        {
            catAudioSource.volume = catSoundVolume;
        }

        // TỰ ĐỘNG XOAY MẶT HƯỚNG VỀ PHÍA PLAYER (Khi không nhảy)
        if (enableLookAtPlayer && playerTransform != null)
        {
            Vector3 direction = playerTransform.position - transform.position;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    // TƯƠNG TÁC CLICK CHUỘT TRÁI VÀO CON MÈO (OIIAOIIA MEME CAT)
    public void Interact()
    {
        // Phát thoại tương tác với Mèo nếu có setup
        if (catDialogueLines != null && catDialogueLines.Length > 0)
        {
            SmartInteractionDialogue dialoguePlayer = GetComponent<SmartInteractionDialogue>();
            if (dialoguePlayer == null) dialoguePlayer = gameObject.AddComponent<SmartInteractionDialogue>();
            dialoguePlayer.dialogueSound = dialogueSound;
            dialoguePlayer.soundVolume = dialogueVolume;
            dialoguePlayer.PlayCustomLines(catDialogueLines);
        }

        // 1. Mở khóa tương tác cho Nệm Ngủ khi tương tác đuổi Mèo
        if (bedColliderToEnable != null)
        {
            bedColliderToEnable.enabled = true;
            Debug.Log("[Cat] 🔓 Đã mở khóa tương tác cho Nệm Ngủ!");
        }

        if (isCatActive) return;

        UpdateAudio3DSettings();

        // 2. NẾU MÈO ĐÃ TỚI TRÊN HỘP GIẤY: CHỈ XOAY TẠI CHỖ 1 ĐỢT RỒI DỪNG, TẮT LOOP
        if (hasArrivedAtTargetPoint)
        {
            if (catLoopCoroutine != null) StopCoroutine(catLoopCoroutine);
            catLoopCoroutine = StartCoroutine(SingleDanceRoutine(3.5f));
            return;
        }

        // Tắt xoay nhìn player trong lúc đang nhảy dance trên giường
        enableLookAtPlayer = false;

        // BẮT ĐẦU NHẢY MÚA VÀ DI CHUYỂN NGAY LẬP TỨC
        if (catLoopCoroutine != null) StopCoroutine(catLoopCoroutine);
        catLoopCoroutine = StartCoroutine(CatBehaviorLoopRoutine());

        // BẬT NHẠC SAU DELAY 2.0s TỪ LÚC TƯƠNG TÁC
        if (catMusicCoroutine != null) StopCoroutine(catMusicCoroutine);
        catMusicCoroutine = StartCoroutine(PlayCatMusicDelayedRoutine(initialInteractDelay));
    }

    private Coroutine catMusicCoroutine;

    IEnumerator PlayCatMusicDelayedRoutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (catSound != null && catAudioSource != null && isCatActive)
        {
            catAudioSource.loop = true;
            catAudioSource.clip = catSound;
            catAudioSource.volume = catSoundVolume;
            catAudioSource.time = 0f;
            catAudioSource.Play();
        }
    }

    IEnumerator SingleDanceRoutine(float duration)
    {
        isCatActive = true;
        enableLookAtPlayer = false;

        if (catAnimator != null)
        {
            catAnimator.SetTrigger("PlayAnim");
        }

        // Bật nhạc sau delay
        if (catMusicCoroutine != null) StopCoroutine(catMusicCoroutine);
        catMusicCoroutine = StartCoroutine(PlayCatMusicDelayedRoutine(initialInteractDelay));

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.Rotate(Vector3.up, 360f * (Time.deltaTime / 1.5f));
            yield return null;
        }

        if (catAudioSource != null && catAudioSource.isPlaying) catAudioSource.Stop();
        if (catAnimator != null) catAnimator.ResetTrigger("PlayAnim");

        isCatActive = false;
        enableLookAtPlayer = true;
        Debug.Log("[Cat] 🐈 Hoàn thành 1 lượt nhảy tại chỗ trên hộp giấy!");
    }

    IEnumerator CatBehaviorLoopRoutine()
    {
        isCatActive = true;

        if (catAnimator != null)
        {
            catAnimator.SetTrigger("PlayAnim");
        }

        while (isCatActive)
        {
            float elapsed = 0f;
            Vector3 currentTargetPoint = GetNewTargetPoint();

            enableLookAtPlayer = false;

            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;

                if (catAnimator != null)
                {
                    catAnimator.SetTrigger("PlayAnim");
                }

                if (moveAreaBounds != null)
                {
                    transform.position = Vector3.MoveTowards(transform.position, currentTargetPoint, moveSpeed * Time.deltaTime);

                    Vector3 dir = currentTargetPoint - transform.position;
                    dir.y = 0;
                    if (dir != Vector3.zero)
                    {
                        Quaternion rot = Quaternion.LookRotation(dir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotationSpeed);
                    }

                    if (Vector3.Distance(transform.position, currentTargetPoint) < 0.1f)
                    {
                        currentTargetPoint = GetNewTargetPoint();
                    }
                }

                yield return null;
            }

            // TẠM DỪNG DI CHUYỂN
            enableLookAtPlayer = true;

            if (pauseBetweenDance > 0f)
            {
                yield return new WaitForSeconds(pauseBetweenDance);
            }
        }

        StopCatDanceSoundAndAnimation();
    }

    /// <summary>
    /// Giảm dần âm lượng nhạc Mèo về 0 mượt mà (Fade Out) rồi mới dừng hoàn toàn, tránh ngắt đột ngột
    /// </summary>
    public void FadeOutCatSoundAndStop(float fadeDuration = 1.5f)
    {
        if (catLoopCoroutine != null) StopCoroutine(catLoopCoroutine);
        catLoopCoroutine = StartCoroutine(FadeOutCatSoundRoutine(fadeDuration));
    }

    IEnumerator FadeOutCatSoundRoutine(float fadeDuration)
    {
        isFadingOut = true;
        if (catAnimator != null) catAnimator.ResetTrigger("PlayAnim");
        enableLookAtPlayer = true;

        if (catAudioSource != null && catAudioSource.isPlaying)
        {
            float startVol = catAudioSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                catAudioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }
            catAudioSource.Stop();
            catAudioSource.volume = catSoundVolume; // Reset lại volume mặc định
        }

        isCatActive = false;
        isFadingOut = false;
    }

    /// <summary>
    /// Tắt ngay nhạc và animation nhảy dance của Mèo
    /// </summary>
    public void StopCatDanceSoundAndAnimation()
    {
        if (catLoopCoroutine != null) StopCoroutine(catLoopCoroutine);
        if (catAudioSource != null && catAudioSource.isPlaying) catAudioSource.Stop();
        if (catAnimator != null) catAnimator.ResetTrigger("PlayAnim");
        isCatActive = false;
        enableLookAtPlayer = true;
    }

    /// <summary>
    /// Cho Mèo vừa chạy tới điểm Meow, dừng animation dance và BẬT XOAY MẶT NHÌN THẲNG VÀO PLAYER
    /// </summary>
    public void MoveToPointAndStop(Transform targetPoint, float speed = 1.5f, float arriveStopAnimDelay = 0f)
    {
        StopCatDanceSoundAndAnimation();
        catLoopCoroutine = StartCoroutine(MoveToPointRoutine(targetPoint, speed));
    }

    IEnumerator MoveToPointRoutine(Transform targetPoint, float speed)
    {
        isCatActive = true;
        enableLookAtPlayer = false;

        Vector3 targetPos = targetPoint != null ? targetPoint.position : transform.position;

        float moveTimer = 0f;
        float maxMoveTime = 4.0f;

        while (Vector3.Distance(transform.position, targetPos) > 0.05f && moveTimer < maxMoveTime)
        {
            moveTimer += Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            Vector3 dir = targetPos - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                Quaternion rot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotationSpeed);
            }

            yield return null;
        }

        transform.position = targetPos;
        hasArrivedAtTargetPoint = true;

        // ẨN TRIGGER MEOW KHI MÈO ĐÃ TỚI NƠI TRÊN THÙNG HÀNG
        if (triggerMeowToDisable != null)
        {
            triggerMeowToDisable.SetActive(false);
        }
        else
        {
            GameObject triggerMeow = GameObject.Find("TriggerMeow");
            if (triggerMeow != null) triggerMeow.SetActive(false);
        }

        // Dừng animation và nhạc khi vừa chạm điểm Meow
        if (catAudioSource != null) catAudioSource.Stop();
        if (catAnimator != null) catAnimator.ResetTrigger("PlayAnim");
        isCatActive = false;

        // BẬT LẠI TARGET NHÌN THẲNG VÀO MẶT PLAYER KHI DỪNG Ở ĐIỂM MEOW
        enableLookAtPlayer = true;
        Debug.Log("[Cat] 🐈 Mèo đã tới điểm trên thùng giấy, đã ẩn TriggerMeow, dừng nhảy & bật target nhìn Player!");
    }

    Vector3 GetNewTargetPoint()
    {
        if (moveAreaBounds == null) return transform.position;

        Bounds bounds = moveAreaBounds.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, transform.position.y, randomZ);
    }

    public void ShowPrompt()
    {
        if (isCatActive) return;
        if (interactPrompt != null) interactPrompt.ShowPrompt();
    }

    public void HidePrompt()
    {
        if (interactPrompt != null) interactPrompt.HidePrompt();
    }
}