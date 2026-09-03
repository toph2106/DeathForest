using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using TMPro;

/// <summary>
/// Script Tương Tác Cắt Dây Búp Bê Rơi Xuống Sàn & Rạch Búp Bê Nhận Chìa Khóa (KeyBL)
/// Gồm 2 Giai Đoạn Tương Tác:
/// - Giai đoạn 1 (Treo trên xà): Cần có "Dao" -> Bấm vào cắt đứt dây, búp bê rơi vật lý xuống sàn.
/// - Giai đoạn 2 (Dưới sàn): Bấm vào búp bê trên sàn -> Màn hình Fade tối -> Phát âm thanh rạch búp bê -> Trao KeyBL vào túi đồ -> Fade sáng trở lại!
/// </summary>
public class HangingDollInteraction : MonoBehaviour, IInteractable
{
    [Header("1. Yêu Cầu Vật Phẩm (Giai Đoạn 1: Cắt Dây Treo)")]
    [Tooltip("Tên vật phẩm cần có trong túi đồ để cắt (Mặc định: 'Dao')")]
    public string requiredItemName = "Dao";

    [Tooltip("Có tiêu hao/xóa Dao sau khi cắt xong không? (Mặc định: False - không mất dao)")]
    public bool consumeKnifeOnUse = false;

    [Header("2. Đối Tượng Búp Bê & Dây Treo")]
    [Tooltip("Đối tượng Búp Bê sẽ rơi xuống (Nếu để trống tự lấy chính GameObject này hoặc root)")]
    public GameObject dollPhysicsObject;

    [Tooltip("GameObject sợi dây treo (Sẽ bị ẩn/biến mất khi cắt)")]
    public GameObject ropeObject;

    [Header("3. Lực Rơi & Vật Lý (Fall Physics)")]
    [Tooltip("Hệ số tốc độ rơi / Trọng lực phụ (Mặc định: 5.0 - tăng lên 15, 25 hoặc 50 nếu map scale lớn để rơi nhanh như thật)")]
    public float fallGravityMultiplier = 5.0f;

    [Tooltip("Góc xoay trục X khi nằm xuống sàn (Mặc định: -90 độ)")]
    public float targetFallRotationX = -90f;

    [Tooltip("Thời gian xoay dần từ thẳng đứng sang nằm phẳng khi rơi (giây - Mặc định: 0.4s)")]
    public float fallRotateDuration = 0.4f;

    [Tooltip("Trọng lượng búp bê khi rơi (kg)")]
    public float dollMass = 2.0f;

    [Tooltip("Vận tốc lao xuống ban đầu lúc vừa đứt dây (Mặc định: Y = -5)")]
    public Vector3 initialDropVelocity = new Vector3(0f, -5f, 0f);

    [Header("4. Âm Thanh Giai Đoạn 1 (Audio SFX)")]
    [Tooltip("Âm thanh tiếng cắt đứt dây (xoẹt / phập)")]
    public AudioClip cutRopeSound;

    [Tooltip("Âm thanh búp bê rơi bịch xuống sàn")]
    public AudioClip dollLandingSound;

    [Tooltip("Âm thanh khi bị khóa / chưa có dao")]
    public AudioClip lockedSound;

    [Range(0f, 1f)] public float soundVolume = 1.0f;

    [Header("5. Thoại Nhắc Nhở Khi Chưa Có Dao")]
    [Tooltip("Có hiện phụ đề nhắc nhở khi bấm vào mà chưa có Dao không?")]
    public bool showLockedDialogue = true;

    [TextArea(2, 3)]
    public string lockedDialogueText = "Sợi dây thừng treo quá chắc... mình cần một con dao để cắt nó.";
    public float dialogueDuration = 3.5f;

    [Header("6. Giai Đoạn 2: Tương Tác Dưới Sàn & Nhận Chìa Khóa (KeyBL)")]
    [Tooltip("Bật tương tác lần 2 khi búp bê đã nằm dưới sàn")]
    public bool enableFloorInteraction = true;

    [Tooltip("Kéo Prefab chìa khóa 'KeyBL' vào ô này")]
    public GameObject rewardItemPrefab;

    [Tooltip("Tên chìa khóa nhận được (Mặc định: KhoaDen)")]
    public string rewardItemName = "KhoaDen";

    [Tooltip("Loại vật phẩm khi thêm vào túi (Key / Quest / Consumable)")]
    public InteractableItem.ItemType rewardItemType = InteractableItem.ItemType.Key;

    [Tooltip("Icon Sprite của chìa khóa (Tùy chọn, để trống sẽ tự đọc từ Prefab)")]
    public Sprite rewardItemIcon;

    [Header("7. Cấu Hình Hiệu Ứng Chuyển Cảnh (Screen Fade)")]
    [Tooltip("Image đen dùng để Fade toàn màn hình (Nếu để trống code sẽ tự tìm hoặc tự tạo)")]
    public Image fadeScreenImage;

    [Tooltip("Thời gian màn hình mờ đen dần (giây)")]
    public float fadeOutDuration = 0.8f;

    [Tooltip("Thời gian giữ màn hình đen để thực hiện rạch búp bê & nhận đồ (giây)")]
    public float blackScreenWaitDuration = 1.2f;

    [Tooltip("Thời gian màn hình sáng rõ trở lại (giây)")]
    public float fadeInDuration = 0.8f;

    [Tooltip("Âm thanh rạch bụng búp bê / xé vải khi màn hình tối")]
    public AudioClip cutDollOpenSound;

    [Tooltip("Âm thanh nhận được chìa khóa")]
    public AudioClip keyAcquiredSound;

    [Tooltip("Phụ đề thông báo khi nhận chìa khóa")]
    public string keyAcquiredDialogue = "Đã rạch con búp bê và tìm thấy một chiếc Chìa Khóa Đen!";
    public float keyDialogueDuration = 3.5f;

    [Header("8. Sự Kiện Kích Hoạt (Unity Events)")]
    [Tooltip("Sự kiện khi cắt dây đứt")]
    public UnityEvent onDollCut;

    [Tooltip("Sự kiện khi búp bê rơi chạm sàn")]
    public UnityEvent onDollLanded;

    [Tooltip("Sự kiện khi rạch búp bê nhận chìa khóa thành công")]
    public UnityEvent onKeyAcquired;

    public static bool IsKeyAcquired { get; set; } = false;

    private bool isCut = false;
    private bool isLanded = false;
    private bool isRewardClaimed = false;

    private AudioSource audioSource;
    private Rigidbody dollRb;
    private TextMeshProUGUI subtitleTextUI;

    void Awake()
    {
        IsKeyAcquired = false;
    }

    void Start()
    {
        if (dollPhysicsObject == null) dollPhysicsObject = gameObject;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        FindSubtitleTextUI();
        FindFadeScreenImage();
    }

    /// <summary>
    /// Hàm tương tác IInteractable (Được gọi khi người chơi bấm chuột vào búp bê)
    /// </summary>
    public void Interact()
    {
        // GIAI ĐOẠN 2: TƯƠNG TÁC KHI ĐÃ RƠI XUỐNG SÀN
        if (isLanded)
        {
            if (isRewardClaimed) return;
            StartFloorInteraction();
            return;
        }

        // GIAI ĐOẠN 1: TƯƠNG TÁC KHI ĐANG TREO TRÊN XÀ
        if (isCut) return;

        bool hasKnife = CheckHasKnife();
        if (!hasKnife)
        {
            Debug.Log($"[HangingDoll] 🔒 Chưa có '{requiredItemName}' trong túi đồ -> Không thể cắt búp bê!");
            PlayLockedFeedback();
            return;
        }

        CutAndDropDoll();
    }

    private bool CheckHasKnife()
    {
        if (InventoryManager.Instance != null)
        {
            return InventoryManager.Instance.HasItem(requiredItemName);
        }

        InventoryManager inv = Object.FindFirstObjectByType<InventoryManager>();
        if (inv != null)
        {
            return inv.HasItem(requiredItemName);
        }

        return false;
    }

    private void CutAndDropDoll()
    {
        isCut = true;
        Debug.Log($"[HangingDoll] ✂️ Đã dùng '{requiredItemName}' cắt đứt dây treo búp bê!");

        if (consumeKnifeOnUse && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(requiredItemName);
        }

        if (cutRopeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cutRopeSound, soundVolume);
        }

        if (ropeObject != null)
        {
            ropeObject.SetActive(false);
        }

        if (dollPhysicsObject != null)
        {
            dollPhysicsObject.transform.SetParent(null);

            // Lưu góc ban đầu và góc đích khi rơi (X = -90 độ)
            Quaternion startRot = dollPhysicsObject.transform.rotation;
            Vector3 curEuler = dollPhysicsObject.transform.eulerAngles;
            Quaternion targetRot = Quaternion.Euler(targetFallRotationX, curEuler.y, 0f);

            dollRb = dollPhysicsObject.GetComponent<Rigidbody>();
            if (dollRb == null) dollRb = dollPhysicsObject.AddComponent<Rigidbody>();

            dollRb.isKinematic = false;
            dollRb.useGravity = true;
            dollRb.mass = dollMass;
            dollRb.interpolation = RigidbodyInterpolation.Interpolate;
            dollRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Khóa các trục xoay để rơi thẳng tuột bụp xuống sàn, không bị lăn lóc nghiêng ngả
            dollRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            Collider[] cols = dollPhysicsObject.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                c.isTrigger = false;
                c.enabled = true;
            }

            dollRb.linearVelocity = initialDropVelocity;

            DollLandingNotifier landingNotifier = dollPhysicsObject.GetComponent<DollLandingNotifier>();
            if (landingNotifier == null) landingNotifier = dollPhysicsObject.AddComponent<DollLandingNotifier>();
            landingNotifier.Init(this, dollLandingSound, soundVolume, fallGravityMultiplier, startRot, targetRot, fallRotateDuration);
        }

        onDollCut?.Invoke();
    }

    /// <summary>
    /// Được gọi khi búp bê rơi chạm sàn
    /// </summary>
    public void OnDollLandedOnFloor()
    {
        if (isLanded) return;
        isLanded = true;
        Debug.Log("[HangingDoll] 🎯 Búp bê đã rơi chạm sàn thành công! Sẵn sàng tương tác lần 2 để nhận chìa khóa.");

        onDollLanded?.Invoke();
    }

    /// <summary>
    /// Bắt đầu chuỗi tương tác trên sàn (Fade đen -> Nhận KeyBL -> Fade sáng)
    /// </summary>
    public void StartFloorInteraction()
    {
        if (isRewardClaimed) return;
        isRewardClaimed = true;

        StartCoroutine(FloorInteractionRoutine());
    }

    private IEnumerator FloorInteractionRoutine()
    {
        Debug.Log("[HangingDoll] 🖤 Bắt đầu hiệu ứng Fade màn hình & rạch búp bê lấy chìa khóa...");

        // 1. Fade màn hình tối đen
        yield return StartCoroutine(FadeToBlack(fadeOutDuration));

        // 2. Trong lúc màn hình tối đen: Phát âm thanh rạch búp bê
        if (cutDollOpenSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cutDollOpenSound, soundVolume);
        }

        yield return new WaitForSeconds(blackScreenWaitDuration);

        // 3. Trao chìa khóa KeyBL cho người chơi vào Inventory
        GiveKeyToPlayer();
        IsKeyAcquired = true;

        // Phát âm thanh nhận chìa khóa
        if (keyAcquiredSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(keyAcquiredSound, soundVolume);
        }

        // Hiện câu thoại nhận chìa khóa
        if (!string.IsNullOrEmpty(keyAcquiredDialogue))
        {
            TriggerDialogue(keyAcquiredDialogue, keyDialogueDuration);
        }

        // GHIM CỐ ĐỊNH BÚP BÊ NẰM YÊN TRÊN SÀN (GIỮ NGUYÊN HÌNH ẢNH, KHÔNG XÓA / KHÔNG TẮT)
        if (dollPhysicsObject != null)
        {
            // Cố định vật lý Rigidbody (Gán velocity về 0 trước khi đặt isKinematic)
            if (dollRb != null)
            {
                dollRb.linearVelocity = Vector3.zero;
                dollRb.angularVelocity = Vector3.zero;
                dollRb.isKinematic = true;
            }

            // Đảm bảo hình ảnh Renderer vẫn luôn hiển thị rõ nét
            Renderer[] rends = dollPhysicsObject.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends) r.enabled = true;

            // Tắt Collider để tâm ngắm bàn tay không còn nhận diện tương tác búp bê nữa
            Collider[] cols = dollPhysicsObject.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols) c.enabled = false;
        }

        // 4. Fade sáng rõ trở lại
        yield return StartCoroutine(FadeFromBlack(fadeInDuration));

        onKeyAcquired?.Invoke();
        Debug.Log("[HangingDoll] ✨ Đã nhận Chìa Khóa Đen (KeyBL) thành công! Búp bê được ghim nằm cố định trên sàn.");

        // Hủy bỏ component này để biến búp bê thành vật thể tĩnh hoàn toàn
        Destroy(this);
    }

    private void GiveKeyToPlayer()
    {
        string itemName = !string.IsNullOrEmpty(rewardItemName) ? rewardItemName : "KhoaDen";
        Sprite itemIcon = rewardItemIcon;
        InteractableItem.ItemType itemType = rewardItemType;
        GameObject itemInstance = null;

        if (rewardItemPrefab == null)
        {
            InteractableItem[] allItems = Resources.FindObjectsOfTypeAll<InteractableItem>();
            foreach (var it in allItems)
            {
                if (it.gameObject.name.Contains("KeyBL") || it.itemNameOrQuestName.Contains("KhoaDen"))
                {
                    rewardItemPrefab = it.gameObject;
                    break;
                }
            }
        }

        if (rewardItemPrefab != null)
        {
            itemInstance = Instantiate(rewardItemPrefab);
            itemInstance.name = itemName;

            InteractableItem interactScript = itemInstance.GetComponent<InteractableItem>() ?? itemInstance.GetComponentInChildren<InteractableItem>();
            if (interactScript != null)
            {
                if (!string.IsNullOrEmpty(interactScript.itemNameOrQuestName)) itemName = interactScript.itemNameOrQuestName;
                if (interactScript.itemIcon != null && itemIcon == null) itemIcon = interactScript.itemIcon;
                itemType = interactScript.itemType;
            }
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddConsumableItem(itemName, itemInstance, itemIcon, itemType);
        }
        else
        {
            InventoryManager inv = Object.FindFirstObjectByType<InventoryManager>();
            if (inv != null) inv.AddConsumableItem(itemName, itemInstance, itemIcon, itemType);
        }
    }

    private IEnumerator FadeToBlack(float duration)
    {
        Image fadeImg = FindFadeScreenImage();
        if (fadeImg == null) yield break;

        fadeImg.gameObject.SetActive(true);
        Color c = Color.black;
        c.a = 0f;
        fadeImg.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / duration);
            fadeImg.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImg.color = c;
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        Image fadeImg = FindFadeScreenImage();
        if (fadeImg == null) yield break;

        Color c = Color.black;
        c.a = 1f;
        fadeImg.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(1f - (elapsed / duration));
            fadeImg.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImg.color = c;
        fadeImg.gameObject.SetActive(false);
    }

    private Image FindFadeScreenImage()
    {
        if (fadeScreenImage != null) return fadeScreenImage;

        GameObject fadeObj = GameObject.Find("FadeImage") ?? GameObject.Find("FadeScreen") ?? GameObject.Find("BlackScreen") ?? GameObject.Find("ScreenFade");
        if (fadeObj != null)
        {
            fadeScreenImage = fadeObj.GetComponent<Image>();
            if (fadeScreenImage != null) return fadeScreenImage;
        }

        // Tự tạo một Black Image trên Canvas nếu chưa có
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject newFade = new GameObject("DollCut_FadeImage");
            newFade.transform.SetParent(canvas.transform, false);
            newFade.transform.SetAsLastSibling();
            fadeScreenImage = newFade.AddComponent<Image>();
            fadeScreenImage.color = new Color(0f, 0f, 0f, 0f);
            RectTransform rt = fadeScreenImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            newFade.SetActive(false);
            return fadeScreenImage;
        }

        return null;
    }

    private void PlayLockedFeedback()
    {
        if (lockedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(lockedSound, soundVolume);
        }

        if (showLockedDialogue && !string.IsNullOrEmpty(lockedDialogueText))
        {
            TriggerDialogue(lockedDialogueText, dialogueDuration);
        }
    }

    private void TriggerDialogue(string text, float duration)
    {
        FindSubtitleTextUI();
        if (subtitleTextUI != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowDialogueRoutine(text, duration));
        }
    }

    private IEnumerator ShowDialogueRoutine(string text, float duration)
    {
        EnsureParentsActive(subtitleTextUI.gameObject);
        subtitleTextUI.gameObject.SetActive(true);
        subtitleTextUI.text = text;

        yield return new WaitForSeconds(duration);

        if (subtitleTextUI.text == text)
        {
            subtitleTextUI.text = "";
            subtitleTextUI.gameObject.SetActive(false);
        }
    }

    private void EnsureParentsActive(GameObject target)
    {
        if (target == null) return;
        Transform current = target.transform.parent;
        while (current != null)
        {
            if (!current.gameObject.activeSelf) current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    private void FindSubtitleTextUI()
    {
        if (subtitleTextUI != null) return;

        SmartInteractionDialogue smart = Object.FindFirstObjectByType<SmartInteractionDialogue>();
        if (smart != null && smart.subtitleTextUI != null)
        {
            subtitleTextUI = smart.subtitleTextUI;
            return;
        }

        GameObject subObj = GameObject.Find("Subtitle Text") ?? GameObject.Find("SubtitleText") ?? GameObject.Find("DocumentSubtitleText");
        if (subObj != null) subtitleTextUI = subObj.GetComponent<TextMeshProUGUI>();
    }
}

/// <summary>
/// Component phụ nhận diện va chạm sàn & tăng tốc trọng lực
/// </summary>
public class DollLandingNotifier : MonoBehaviour
{
    private HangingDollInteraction parentScript;
    private AudioClip landingClip;
    private float volume;
    private bool hasLanded = false;
    private Rigidbody rb;
    private float gravityMult = 1.0f;
    private Quaternion startRot;
    private Quaternion targetRot;
    private float rotateDuration = 0.4f;
    private float timeInAir = 0f;

    public void Init(HangingDollInteraction parent, AudioClip clip, float vol, float gravMult, Quaternion startRotation, Quaternion targetRotation, float rotDuration)
    {
        parentScript = parent;
        landingClip = clip;
        volume = vol;
        gravityMult = gravMult;
        startRot = startRotation;
        targetRot = targetRotation;
        rotateDuration = rotDuration;
        timeInAir = 0f;
        hasLanded = false;
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!hasLanded)
        {
            if (rb != null && gravityMult > 1.0f)
            {
                rb.AddForce(Physics.gravity * (gravityMult - 1.0f), ForceMode.Acceleration);
            }

            // Xoay dần mượt mà trục X từ thẳng đứng sang nằm phẳng (-90 độ) trong lúc rơi
            if (rotateDuration > 0f)
            {
                timeInAir += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(timeInAir / rotateDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;
        hasLanded = true;

        // Rơi bụp một cái xuống sàn -> Dừng và ghim cố định ngay lập tức, không cho nảy hay lăn lóc
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        transform.rotation = targetRot;

        if (landingClip != null)
        {
            AudioSource.PlayClipAtPoint(landingClip, transform.position, volume);
        }

        if (parentScript != null)
        {
            parentScript.OnDollLandedOnFloor();
        }
    }
}
