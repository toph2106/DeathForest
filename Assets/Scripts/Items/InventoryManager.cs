using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // MẢNG STATIC LƯU GIỮ CÁC VẬT PHẨM KHI CHUYỂN MAP
    private static string[] savedHeldItems = null;

    public static InventoryManager Instance;

    [Header("UI Hotbar (Các ô Tiêu Hoa)")]
    public GameObject inventoryPanel;
    public Transform[] slotTransforms; // Mảng động các ô Slot trên UI

    [Header("Cấu Hình Phím Bấm")]
    [Tooltip("Phím dùng để SỬ DỤNG vật phẩm đang chọn (Mặc định: Phím R)")]
    public KeyCode useKey = KeyCode.R;
    [Tooltip("Phím dùng để NÉM / BỎ vật phẩm ra sàn (Mặc định: Phím Q)")]
    public KeyCode dropKey = KeyCode.Q;
    [Tooltip("Âm thanh khi ném/thả vật phẩm xuống sàn")]
    public AudioClip dropSound;
    [Tooltip("Âm thanh khi uống lon nước hồi thể lực")]
    public AudioClip drinkSound;

    [Header("Cấu Hình Mở Rộng Túi Đồ (Balo)")]
    [Tooltip("Số ô mặc định ban đầu khi chưa nhặt Balo (Mặc định: 5)")]
    public int defaultSlotCount = 5;
    [Tooltip("Số ô tối đa khi đã nhặt Balo mở rộng (Mặc định: 9)")]
    public int expandedSlotCount = 9;
    [Tooltip("Âm thanh khi nhặt mở khóa Balo")]
    public AudioClip backpackUnlockSound;

    // Biến static lưu trạng thái mở khóa Balo xuyên suốt các scene/map
    public static bool hasUnlockedBackpack = false;

    public int CurrentCapacity => hasUnlockedBackpack 
        ? ((slotTransforms != null && slotTransforms.Length > 0) ? Mathf.Min(expandedSlotCount, slotTransforms.Length) : expandedSlotCount)
        : ((slotTransforms != null && slotTransforms.Length > 0) ? Mathf.Min(defaultSlotCount, slotTransforms.Length) : defaultSlotCount);

    [System.Serializable]
    public class Item3DPrefabEntry
    {
        public string itemName = "Pin";
        public GameObject prefab;
    }

    [Header("3D Model Preview Trong Ô Slot")]
    [Tooltip("Bật chế độ hiển thị Model 3D thật xoay xoay trong từng ô slot")]
    public bool enable3DItemPreview = true;
    [Tooltip("Danh sách Prefab 3D gán sẵn theo tên (VD: Tên 'Pin' -> Kéo Prefab Battery vào đây)")]
    public Item3DPrefabEntry[] default3DPrefabs;
    [Tooltip("Khoảng cách đặt Model 3D trước Camera")]
    public float previewDistance = 0.35f;
    [Tooltip("Kích thước hiển thị của Model 3D trong ô")]
    public float previewItemScale = 0.035f;
    [Tooltip("Tốc độ xoay của Model 3D (Đặt 0 để đứng yên không xoay)")]
    public float previewRotateSpeed = 0f;
    [Tooltip("Góc nghiêng của Model 3D khi hiển thị")]
    public Vector3 previewTiltEuler = new Vector3(20f, 35f, -15f);

    [HideInInspector] public GameObject questProgressTextObject;
    [HideInInspector] public TextMeshProUGUI questProgressText;
    [HideInInspector] public int totalQuestItemsNeeded = 3;

    [HideInInspector] public string[] heldItems;
    private GameObject[] heldItemObjects;
    private Sprite[] heldItemSprites;
    private InteractableItem.ItemType[] heldItemTypes;
    private GameObject[] slot3DModels; // Mảng lưu các GameObject 3D preview
    private Vector3[] slotBaseScales; // Lưu Scale chuẩn để Zoom khi chọn ô
    private Image[] slotIconImages; // Các hình Icon con bên trong ô Slot UI

    private int selectedIndex = -1;
    private int currentQuestItemCount = 0;

    private Vector3 normalScale = Vector3.one;
    private Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (slotTransforms != null && slotTransforms.Length > 0 && slotTransforms[0] != null)
            {
                Instance.inventoryPanel = inventoryPanel;
                Instance.slotTransforms = slotTransforms;
            }
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (questProgressTextObject != null) questProgressTextObject.SetActive(false);

        int slotCount = (slotTransforms != null && slotTransforms.Length > 0) ? slotTransforms.Length : expandedSlotCount;
        heldItems = new string[slotCount];
        heldItemObjects = new GameObject[slotCount];
        heldItemSprites = new Sprite[slotCount];
        heldItemTypes = new InteractableItem.ItemType[slotCount];
        slot3DModels = new GameObject[slotCount];
        slotBaseScales = new Vector3[slotCount];
        slotIconImages = new Image[slotCount];

        // Đọc Scale gốc từ ô đầu tiên
        if (slotCount > 0 && slotTransforms[0] != null)
        {
            normalScale = slotTransforms[0].localScale;
            selectedScale = normalScale * 1.2f;
        }

        for (int i = 0; i < slotCount; i++)
        {
            heldItems[i] = "";
            heldItemObjects[i] = null;
            heldItemSprites[i] = null;
            slot3DModels[i] = null;

            if (slotTransforms[i] != null)
            {
                slotTransforms[i].localScale = normalScale;
                EnsureSlotIcon(i);
            }
        }

        // KHÔI PHỤC DỮ LIỆU TÚI ĐỒ VÀ SỐ PIN CỘNG DỒN NẾU CHUYỂN MAP
        if (savedHeldItems != null && savedHeldItems.Length == slotCount)
        {
            for (int i = 0; i < slotCount; i++)
            {
                heldItems[i] = savedHeldItems[i];
            }
        }

        UpdateUISlots();
    }

    private void EnsureSlotIcon(int i)
    {
        if (slotTransforms == null || i >= slotTransforms.Length || slotTransforms[i] == null) return;

        if (slotIconImages[i] == null)
        {
            Image[] childImgs = slotTransforms[i].GetComponentsInChildren<Image>(true);
            foreach (Image img in childImgs)
            {
                if (img.transform != slotTransforms[i])
                {
                    slotIconImages[i] = img;
                    break;
                }
            }

            // Nếu ô Slot chưa có Image con hiển thị Icon -> Tự động sinh GameObject Image con
            if (slotIconImages[i] == null)
            {
                GameObject iconObj = new GameObject("ItemIcon_" + (i + 1));
                iconObj.transform.SetParent(slotTransforms[i], false);
                RectTransform rt = iconObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.12f, 0.12f);
                rt.anchorMax = new Vector2(0.88f, 0.88f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image newImg = iconObj.AddComponent<Image>();
                newImg.preserveAspect = true;
                newImg.raycastTarget = false;
                slotIconImages[i] = newImg;
            }
        }
    }

    void OnDisable()
    {
        // LƯU DỮ LIỆU TRƯỚC KHI CHUYỂN MAP
        if (heldItems != null)
        {
            savedHeldItems = (string[])heldItems.Clone();
        }

        // Hủy các 3D preview models
        if (slot3DModels != null)
        {
            for (int i = 0; i < slot3DModels.Length; i++)
            {
                Destroy3DPreview(i);
            }
        }
    }

    void Update()
    {
        if (PauseMenuManager.isPaused) return;

        HandleCheatInput(); // Phím B nhặt pin cheat demo, F9 nạp đầy pin
        HandleSelectionInput();
        HandleUseInput(); // Phím R dùng pin
        HandleDropInput(); // Phím Q ném/bỏ đồ ra sàn
    }

    void LateUpdate()
    {
        if (!enable3DItemPreview || slot3DModels == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        bool panelActive = (inventoryPanel == null || inventoryPanel.activeInHierarchy);

        for (int i = 0; i < slot3DModels.Length; i++)
        {
            if (slot3DModels[i] == null) continue;

            bool shouldShow = panelActive && (heldItems != null && i < heldItems.Length && !string.IsNullOrEmpty(heldItems[i])) && (i < CurrentCapacity);
            if (slot3DModels[i].activeSelf != shouldShow)
            {
                slot3DModels[i].SetActive(shouldShow);
            }

            if (!shouldShow) continue;

            if (slotTransforms != null && i < slotTransforms.Length && slotTransforms[i] != null)
            {
                Vector3 screenPos = slotTransforms[i].position;
                Vector3 targetWorldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, previewDistance));
                slot3DModels[i].transform.position = targetWorldPos;

                // Xoay tuyệt đối theo hệ quy chiếu của Camera (Camera Space):
                // - Hoàn toàn không bị ảnh hưởng/đánh nhau khi người chơi xoay chuột hoặc quay đầu!
                // - Đồng bộ nhịp xoay mượt mà cho toàn bộ tất cả các slot (1 đến 9)!
                if (previewRotateSpeed > 0.01f)
                {
                    float currentYaw = (Time.time * previewRotateSpeed) % 360f;
                    Quaternion rotationInCamSpace = Quaternion.Euler(previewTiltEuler.x, previewTiltEuler.y + currentYaw, previewTiltEuler.z);
                    slot3DModels[i].transform.rotation = cam.transform.rotation * rotationInCamSpace;
                }
                else
                {
                    Quaternion rotationInCamSpace = Quaternion.Euler(previewTiltEuler);
                    slot3DModels[i].transform.rotation = cam.transform.rotation * rotationInCamSpace;
                }

                // Phóng to nhẹ Model 3D khi đang rê chuột / chọn ô này (Zoom to 1.35x)
                float targetScaleFactor = (i == selectedIndex) ? 1.35f : 1.0f;
                if (slotBaseScales != null && i < slotBaseScales.Length && slotBaseScales[i] != Vector3.zero)
                {
                    Vector3 targetScale = slotBaseScales[i] * targetScaleFactor;
                    slot3DModels[i].transform.localScale = Vector3.Lerp(slot3DModels[i].transform.localScale, targetScale, Time.deltaTime * 12f);
                }
            }
        }
    }

    private void HandleCheatInput()
    {
        // Bấm phím B -> Nhận ngay 1 Cục Pin (Tự cộng dồn số lượng)
        if (Input.GetKeyDown(KeyCode.B))
        {
            bool success = AddConsumableItem("Pin", null);
            if (success)
            {
                Debug.Log("⚡ [DEMO CHEAT] Đã thêm 1 Cục Pin!");
            }
        }

        // Bấm phím F9 -> Nạp đầy 100% Pin Đèn Pin ngay lập tức
        if (Input.GetKeyDown(KeyCode.F9))
        {
            if (FlashlightToggle.Instance != null)
            {
                FlashlightToggle.Instance.hasFlashlight = true;
                FlashlightToggle.Instance.RechargeBattery(100f);
                Debug.Log("⚡ [DEMO CHEAT] Đã nạp đầy 100% Pin Đèn Pin!");
            }
        }
    }

    /// <summary>
    /// Hàm nhặt item: Mỗi vật phẩm (kể cả Cục Pin) chiếm đúng 1 ô Slot riêng biệt để người chơi quản lý kho đồ!
    /// </summary>
    public bool AddConsumableItem(string itemName, GameObject itemObj, Sprite itemSprite = null, InteractableItem.ItemType itemType = InteractableItem.ItemType.Consumable)
    {
        if (heldItems == null) return false;
        int slotCount = heldItems.Length;
        int activeCap = CurrentCapacity;
        if (heldItemSprites == null || heldItemSprites.Length != slotCount) heldItemSprites = new Sprite[slotCount];
        if (heldItemObjects == null || heldItemObjects.Length != slotCount) heldItemObjects = new GameObject[slotCount];
        if (heldItemTypes == null || heldItemTypes.Length != slotCount) heldItemTypes = new InteractableItem.ItemType[slotCount];
        if (slot3DModels == null || slot3DModels.Length != slotCount) slot3DModels = new GameObject[slotCount];

        // TÌM Ô TRỐNG ĐẦU TIÊN TRONG SỐ Ô ĐANG ĐƯỢC MỞ KHÓA (5 Ô HOẶC 9 Ô)
        for (int i = 0; i < activeCap; i++)
        {
            if (string.IsNullOrEmpty(heldItems[i]))
            {
                heldItems[i] = itemName;
                heldItemObjects[i] = itemObj;
                heldItemSprites[i] = itemSprite;
                heldItemTypes[i] = itemType;

                // Tạo Model 3D xoay trong ô Slot
                Create3DPreviewForSlot(i, itemName, itemObj);

                if (itemObj != null) itemObj.SetActive(false);

                if (selectedIndex == -1) ToggleSelect(i);
                else UpdateUISlots();

                Debug.Log($"[Inventory] 🎒 Đã nhặt '{itemName}' vào ô Slot {i + 1} (Sức chứa: {activeCap} ô)");
                return true;
            }
        }

        Debug.Log($"⚠️ Túi đồ đã đầy (Đủ {activeCap} ô)! Không thể nhặt thêm " + itemName);
        return false;
    }

    /// <summary>
    /// Kiểm tra xem toàn bộ các ô Slot túi đồ đang mở khóa đã bị lấp đầy hay chưa
    /// </summary>
    public bool IsInventoryFull()
    {
        if (heldItems == null || heldItems.Length == 0) return false;
        int activeCap = CurrentCapacity;
        for (int i = 0; i < activeCap; i++)
        {
            if (string.IsNullOrEmpty(heldItems[i])) return false;
        }
        return true;
    }

    /// <summary>
    /// Lấy số lượng ô trống còn lại trong túi đồ đang mở khóa
    /// </summary>
    public int GetFreeSlotCount()
    {
        if (heldItems == null) return 0;
        int count = 0;
        int activeCap = CurrentCapacity;
        for (int i = 0; i < activeCap; i++)
        {
            if (string.IsNullOrEmpty(heldItems[i])) count++;
        }
        return count;
    }

    // --- XỬ LÝ SỬ DỤNG VẬT PHẨM (PHÍM R) ---
    private void HandleUseInput()
    {
        if (Input.GetKeyDown(useKey))
        {
            if (selectedIndex >= 0 && heldItems != null && selectedIndex < heldItems.Length && !string.IsNullOrEmpty(heldItems[selectedIndex]))
            {
                string itemName = heldItems[selectedIndex];
                string lower = itemName.ToLower().Trim();
                bool isBattery = lower.Contains("pin") || lower.Contains("battery") || lower.Contains("thu thap");
                bool isDrink = lower.Contains("nuoc") || lower.Contains("can") || lower.Contains("lon") || lower.Contains("energy") || lower.Contains("soda") || lower.Contains("drink") || lower.Contains("water");

                if (isBattery)
                {
                    if (FlashlightToggle.Instance != null)
                    {
                        if (!FlashlightToggle.Instance.hasFlashlight)
                        {
                            Debug.LogWarning("[Inventory] Bạn chưa sở hữu Đèn Pin! Không thể nạp Pin.");
                            return;
                        }

                        FlashlightToggle.Instance.RechargeBattery(50f);
                        Debug.Log($"[Inventory] ⚡ Đã sử dụng 1 Cục Pin ở ô Slot {selectedIndex + 1}! Nạp +50% Pin.");
                    }

                    // Xóa cục pin ở ô đang chọn
                    if (selectedIndex < heldItemObjects.Length && heldItemObjects[selectedIndex] != null)
                    {
                        Destroy(heldItemObjects[selectedIndex]);
                    }

                    heldItems[selectedIndex] = "";
                    heldItemObjects[selectedIndex] = null;
                    if (heldItemSprites != null && selectedIndex < heldItemSprites.Length) heldItemSprites[selectedIndex] = null;
                    Destroy3DPreview(selectedIndex);

                    // Tự động dồn các ô slot sang trái để không bị trống ở giữa
                    ConsolidateSlots();
                }
                else if (isDrink)
                {
                    MovePl player = Object.FindFirstObjectByType<MovePl>();
                    if (player != null)
                    {
                        player.RestoreStaminaInstant();
                    }

                    // Phát âm thanh uống nước
                    AudioClip soundToPlay = drinkSound;
                    if (soundToPlay == null && player != null && player.drinkSound != null)
                    {
                        soundToPlay = player.drinkSound;
                    }

                    if (soundToPlay != null)
                    {
                        AudioSource aSrc = GetComponent<AudioSource>();
                        if (aSrc == null) aSrc = gameObject.AddComponent<AudioSource>();
                        aSrc.PlayOneShot(soundToPlay, 0.9f);
                    }

                    Debug.Log($"[Inventory] 🥤 Đã uống '{itemName}' ở ô Slot {selectedIndex + 1}! Hồi phục 100% thể lực ngay lập tức.");

                    // Xóa lon nước khỏi ô
                    if (selectedIndex < heldItemObjects.Length && heldItemObjects[selectedIndex] != null)
                    {
                        Destroy(heldItemObjects[selectedIndex]);
                    }

                    heldItems[selectedIndex] = "";
                    heldItemObjects[selectedIndex] = null;
                    if (heldItemSprites != null && selectedIndex < heldItemSprites.Length) heldItemSprites[selectedIndex] = null;
                    Destroy3DPreview(selectedIndex);

                    // Tự động dồn các ô slot sang trái
                    ConsolidateSlots();
                }
                else
                {
                    Debug.Log($"[Inventory] Chưa có logic dùng cho vật phẩm '{itemName}'");
                }
            }
        }
    }

    // --- XỬ LÝ NÉM / BỎ VẬT PHẨM RA SÀN (PHÍM Q) ---
    private void HandleDropInput()
    {
        if (Input.GetKeyDown(dropKey))
        {
            int slotToDrop = GetHoveredOrSelectedSlotIndex();
            if (slotToDrop >= 0 && heldItems != null && slotToDrop < heldItems.Length && !string.IsNullOrEmpty(heldItems[slotToDrop]))
            {
                DropItem(slotToDrop);
            }
        }
    }

    private int GetHoveredOrSelectedSlotIndex()
    {
        // 1. Kiểm tra xem con trỏ chuột có đang rê qua ô Slot nào trên UI không
        if (slotTransforms != null)
        {
            Vector2 mousePos = Input.mousePosition;
            for (int i = 0; i < slotTransforms.Length; i++)
            {
                if (slotTransforms[i] != null)
                {
                    RectTransform rt = slotTransforms[i] as RectTransform;
                    if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos))
                    {
                        if (heldItems != null && i < heldItems.Length && !string.IsNullOrEmpty(heldItems[i]))
                        {
                            return i;
                        }
                    }
                }
            }
        }

        // 2. Nếu không rê chuột thì lấy ô đang được chọn (selectedIndex)
        if (selectedIndex >= 0 && heldItems != null && selectedIndex < heldItems.Length && !string.IsNullOrEmpty(heldItems[selectedIndex]))
        {
            return selectedIndex;
        }

        return -1;
    }

    /// <summary>
    /// Ném vật phẩm tại ô slotIndex ra mặt sàn trước mặt người chơi và cố định nằm tại đó
    /// </summary>
    public void DropItem(int slotIndex)
    {
        if (heldItems == null || slotIndex < 0 || slotIndex >= heldItems.Length) return;
        string itemName = heldItems[slotIndex];
        if (string.IsNullOrEmpty(itemName)) return;

        GameObject sourceObj = (heldItemObjects != null && slotIndex < heldItemObjects.Length) ? heldItemObjects[slotIndex] : null;
        Sprite itemSprite = (heldItemSprites != null && slotIndex < heldItemSprites.Length) ? heldItemSprites[slotIndex] : null;

        // 1. Tìm Player và Camera
        Transform playerT = null;
        MovePl movePl = Object.FindFirstObjectByType<MovePl>();
        if (movePl != null) playerT = movePl.transform;
        if (playerT == null)
        {
            GameObject pl = GameObject.FindGameObjectWithTag("Player");
            if (pl != null) playerT = pl.transform;
        }

        Camera cam = Camera.main;
        Vector3 playerPos = (playerT != null) ? playerT.position : transform.position;

        // Lấy hướng nhìn ngang (không có Y) từ Camera hoặc Player
        Vector3 forwardDir = Vector3.forward;
        if (cam != null)
        {
            forwardDir = cam.transform.forward;
        }
        else if (playerT != null)
        {
            forwardDir = playerT.forward;
        }
        forwardDir.y = 0f;
        if (forwardDir.sqrMagnitude < 0.001f) forwardDir = Vector3.forward;
        forwardDir.Normalize();

        // Vị trí spawn: trước mặt người chơi, ngang ngực
        Vector3 dropPos = playerPos + forwardDir * 1.6f + Vector3.up * 1.0f;
        // Góc nghiêng ban đầu: nằm ngang tự nhiên thay vì dựng đứng thẳng đơ
        float playerYAngle = (playerT != null) ? playerT.eulerAngles.y : 0f;
        Quaternion dropRot = Quaternion.Euler(80f, playerYAngle + Random.Range(-25f, 25f), Random.Range(-15f, 15f));

        // 2. Tạo hoặc kích hoạt lại GameObject
        GameObject droppedObj = null;
        if (sourceObj != null)
        {
            sourceObj.transform.SetParent(null);
            sourceObj.transform.position = dropPos;
            sourceObj.transform.rotation = dropRot;
            sourceObj.SetActive(true);
            droppedObj = sourceObj;
        }
        else
        {
            // Tìm trong default3DPrefabs
            GameObject defaultPrefab = null;
            if (default3DPrefabs != null)
            {
                foreach (var entry in default3DPrefabs)
                {
                    if (entry != null && entry.prefab != null && !string.IsNullOrEmpty(entry.itemName))
                    {
                        if (entry.itemName.ToLower().Trim() == itemName.ToLower().Trim())
                        {
                            defaultPrefab = entry.prefab;
                            break;
                        }
                    }
                }
            }

            if (defaultPrefab != null)
            {
                droppedObj = Instantiate(defaultPrefab, dropPos, dropRot);
                droppedObj.transform.localScale = defaultPrefab.transform.localScale;
            }
        }

        if (droppedObj != null)
        {
            droppedObj.name = itemName;

            // Xử lý nếu scale bị âm (ví dụ model import bị âm scale) để PhysX tính toán chuẩn
            Vector3 curScale = droppedObj.transform.localScale;
            if (curScale.x < 0f || curScale.y < 0f || curScale.z < 0f)
            {
                droppedObj.transform.localScale = new Vector3(Mathf.Abs(curScale.x), Mathf.Abs(curScale.y), Mathf.Abs(curScale.z));
            }

            // Đảm bảo có InteractableItem và RESET lại trạng thái để nhặt lại được
            InteractableItem itemComp = droppedObj.GetComponent<InteractableItem>();
            if (itemComp == null) itemComp = droppedObj.AddComponent<InteractableItem>();

            itemComp.itemNameOrQuestName = itemName;
            itemComp.itemIcon = itemSprite;
            itemComp.ResetPickupState();
            itemComp.enabled = true;

            // Khôi phục chính xác ItemType (Key, Consumable, Battery...)
            if (heldItemTypes != null && slotIndex < heldItemTypes.Length)
            {
                itemComp.itemType = heldItemTypes[slotIndex];
            }
            else
            {
                string lower = itemName.ToLower();
                if (lower.Contains("pin") || lower.Contains("battery")) itemComp.itemType = InteractableItem.ItemType.Battery;
                else if (lower.Contains("key") || lower.Contains("khoa")) itemComp.itemType = InteractableItem.ItemType.Key;
                else itemComp.itemType = InteractableItem.ItemType.Consumable;
            }

            // Gắn DroppedItemPhysics MỚI để xử lý rơi tự do + chạm đất khóa cố định cho TẤT CẢ các loại item (Key, Consumable, Pin...)
            DroppedItemPhysics phys = droppedObj.GetComponent<DroppedItemPhysics>();
            if (phys == null) phys = droppedObj.AddComponent<DroppedItemPhysics>();
            phys.LaunchDrop(playerT);
        }

        // 3. Xóa vật phẩm khỏi ô Inventory
        heldItems[slotIndex] = "";
        if (heldItemObjects != null && slotIndex < heldItemObjects.Length) heldItemObjects[slotIndex] = null;
        if (heldItemSprites != null && slotIndex < heldItemSprites.Length) heldItemSprites[slotIndex] = null;
        if (heldItemTypes != null && slotIndex < heldItemTypes.Length) heldItemTypes[slotIndex] = InteractableItem.ItemType.Consumable;
        Destroy3DPreview(slotIndex);

        // 4. Dồn các ô slot lại gọn gàng
        ConsolidateSlots();

        // 5. Phát âm thanh ném/thả
        if (dropSound != null)
        {
            AudioSource.PlayClipAtPoint(dropSound, dropPos, 0.8f);
        }

        Debug.Log($"[Inventory] 🗑️ Đã ném '{itemName}' ra sàn đất trước mặt người chơi!");
    }

    private void Create3DPreviewForSlot(int slotIndex, string itemName, GameObject itemSource)
    {
        if (!enable3DItemPreview || slotIndex < 0 || slot3DModels == null || slotIndex >= slot3DModels.Length) return;

        Destroy3DPreview(slotIndex);

        // 1. Ưu tiên tìm trong default3DPrefabs (hỗ trợ cả Pin <-> Battery)
        GameObject prefabToUse = null;
        if (default3DPrefabs != null)
        {
            foreach (var entry in default3DPrefabs)
            {
                if (entry != null && entry.prefab != null && !string.IsNullOrEmpty(entry.itemName))
                {
                    string sName = itemName.ToLower().Trim();
                    string eName = entry.itemName.ToLower().Trim();

                    if (sName == eName || sName.Contains(eName) || eName.Contains(sName)
                        || ((sName.Contains("pin") || sName.Contains("battery")) && (eName.Contains("pin") || eName.Contains("battery"))))
                    {
                        prefabToUse = entry.prefab;
                        break;
                    }
                }
            }
        }

        // 2. Nếu không có trong default3DPrefabs -> Dùng itemSource
        if (prefabToUse == null) prefabToUse = itemSource;
        if (prefabToUse == null) return;

        // Tạo root GameObject để căn tâm tuyệt đối cho Model 3D
        GameObject previewRoot = new GameObject("Preview3D_" + itemName + "_Slot" + (slotIndex + 1));
        GameObject previewChild = Instantiate(prefabToUse);
        previewChild.name = "ModelMesh";

        // Đặt previewChild làm con của previewRoot và giữ nguyên tỉ lệ hình học của Prefab gốc
        previewChild.transform.SetParent(previewRoot.transform, false);
        previewChild.transform.localPosition = Vector3.zero;
        previewChild.transform.localRotation = Quaternion.identity;

        // Giữ nguyên tỉ lệ hình học tự nhiên của Prefab (ví dụ: Cục pin dài thon, không bị ép béo)
        Vector3 origScale = prefabToUse.transform.localScale;
        float maxComp = Mathf.Max(Mathf.Abs(origScale.x), Mathf.Abs(origScale.y), Mathf.Abs(origScale.z));
        Vector3 naturalRatio = (maxComp > 0.0001f) ? (origScale / maxComp) : Vector3.one;
        previewChild.transform.localScale = naturalRatio;

        // Xóa các thành phần logic/vật lý để chỉ giữ lại hình ảnh 3D Mesh
        foreach (var col in previewChild.GetComponentsInChildren<Collider>(true)) Destroy(col);
        foreach (var rb in previewChild.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        foreach (var audio in previewChild.GetComponentsInChildren<AudioSource>(true)) Destroy(audio);
        foreach (var light in previewChild.GetComponentsInChildren<Light>(true)) Destroy(light);
        foreach (var script in previewChild.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(script);

        // Gán Layer UI cho toàn bộ preview để không bị đèn Flashlight hay Point Light trong cảnh chiếu chói lóa
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;
        previewRoot.layer = uiLayer;
        foreach (Transform t in previewRoot.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = uiLayer;
        }

        // Bật tất cả Renderers và chuyển sang Unlit để hoàn toàn không bị chói lóa từ đèn pin hay Global Volume
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") 
                           ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                           ?? Shader.Find("Unlit/Texture") 
                           ?? Shader.Find("Unlit/Color");

        Renderer[] rends = previewChild.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            r.enabled = true;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            if (unlitShader != null && r.materials != null)
            {
                Material[] newMats = new Material[r.materials.Length];
                for (int m = 0; m < r.materials.Length; m++)
                {
                    Material origMat = r.materials[m];
                    if (origMat == null) continue;

                    Material unlitMat = new Material(unlitShader);
                    Texture mainTex = origMat.mainTexture;
                    if (mainTex == null && origMat.HasProperty("_BaseMap")) mainTex = origMat.GetTexture("_BaseMap");
                    if (mainTex == null && origMat.HasProperty("_BaseColorTexture")) mainTex = origMat.GetTexture("_BaseColorTexture");

                    Color col = Color.white;
                    if (origMat.HasProperty("_BaseColor")) col = origMat.GetColor("_BaseColor");
                    else if (origMat.HasProperty("_Color")) col = origMat.GetColor("_Color");

                    if (mainTex != null)
                    {
                        unlitMat.mainTexture = mainTex;
                        if (unlitMat.HasProperty("_BaseMap")) unlitMat.SetTexture("_BaseMap", mainTex);
                    }
                    if (unlitMat.HasProperty("_BaseColor")) unlitMat.SetColor("_BaseColor", col);
                    else if (unlitMat.HasProperty("_Color")) unlitMat.SetColor("_Color", col);

                    newMats[m] = unlitMat;
                }
                r.materials = newMats;
            }
        }

        // CĂN CHỈNH TÂM VÀ KÍCH THƯỚC MODEL 3D
        previewRoot.transform.position = Vector3.zero;
        previewRoot.transform.rotation = Quaternion.identity;
        previewRoot.transform.localScale = Vector3.one;

        Bounds totalBounds = new Bounds();
        bool hasBounds = false;
        if (rends != null && rends.Length > 0)
        {
            foreach (var r in rends)
            {
                if (r == null) continue;
                if (!hasBounds)
                {
                    totalBounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    totalBounds.Encapsulate(r.bounds);
                }
            }
        }

        if (hasBounds)
        {
            // Dời vị trí local của child để tâm hình học của mesh trùng với gốc tọa độ (0,0,0) của Root
            previewChild.transform.localPosition = -totalBounds.center;

            float maxDim = Mathf.Max(totalBounds.size.x, totalBounds.size.y, totalBounds.size.z);
            if (maxDim > 0.0001f)
            {
                float factor = previewItemScale / maxDim;
                previewRoot.transform.localScale = Vector3.one * factor;
            }
            else
            {
                previewRoot.transform.localScale = Vector3.one * previewItemScale;
            }
        }
        else
        {
            previewRoot.transform.localScale = Vector3.one * previewItemScale;
        }

        // Đặt góc nghiêng ban đầu
        previewRoot.transform.eulerAngles = previewTiltEuler;

        // Lưu Scale chuẩn của slot để phục vụ zoom khi rê chuột / chọn ô
        if (slotBaseScales == null || slotBaseScales.Length != slot3DModels.Length)
        {
            slotBaseScales = new Vector3[slot3DModels.Length];
        }
        slotBaseScales[slotIndex] = previewRoot.transform.localScale;

        slot3DModels[slotIndex] = previewRoot;
        previewRoot.SetActive(true);
        previewChild.SetActive(true);

        Debug.Log($"[Inventory] ✨ Đã tạo Model 3D Slot {slotIndex + 1} cho '{itemName}' từ nguồn '{prefabToUse.name}' (Mesh count: {(rends != null ? rends.Length : 0)})");
    }

    private void Destroy3DPreview(int slotIndex)
    {
        if (slot3DModels != null && slotIndex >= 0 && slotIndex < slot3DModels.Length)
        {
            if (slot3DModels[slotIndex] != null)
            {
                Destroy(slot3DModels[slotIndex]);
                slot3DModels[slotIndex] = null;
            }
        }
    }

    private void HandleSelectionInput()
    {
        if (heldItems == null) return;
        int activeCap = CurrentCapacity;

        if (Input.GetKeyDown(KeyCode.Alpha1) && activeCap > 0) ToggleSelect(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && activeCap > 1) ToggleSelect(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) && activeCap > 2) ToggleSelect(2);
        if (Input.GetKeyDown(KeyCode.Alpha4) && activeCap > 3) ToggleSelect(3);
        if (Input.GetKeyDown(KeyCode.Alpha5) && activeCap > 4) ToggleSelect(4);
        if (Input.GetKeyDown(KeyCode.Alpha6) && activeCap > 5) ToggleSelect(5);
        if (Input.GetKeyDown(KeyCode.Alpha7) && activeCap > 6) ToggleSelect(6);
        if (Input.GetKeyDown(KeyCode.Alpha8) && activeCap > 7) ToggleSelect(7);
        if (Input.GetKeyDown(KeyCode.Alpha9) && activeCap > 8) ToggleSelect(8);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (activeCap == 0) return;

            if (selectedIndex == -1 || selectedIndex >= activeCap) ToggleSelect(0);
            else
            {
                int newIndex = selectedIndex;
                if (scroll > 0f) newIndex--;
                else newIndex++;

                if (newIndex < 0) newIndex = activeCap - 1;
                if (newIndex >= activeCap) newIndex = 0;

                ToggleSelect(newIndex);
            }
        }
    }

    private void ToggleSelect(int index)
    {
        if (selectedIndex == index)
        {
            selectedIndex = -1;
        }
        else
        {
            selectedIndex = index;
        }

        UpdateUISlots();
    }

    private void UpdateUISlots()
    {
        if (slotTransforms == null || heldItems == null) return;
        int slotCount = slotTransforms.Length;
        int itemCount = heldItems.Length;
        int activeCap = CurrentCapacity;

        // Đảm bảo khung túi đồ luôn luôn hiển thị trên màn hình
        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(true);
        }

        for (int i = 0; i < slotCount; i++)
        {
            if (slotTransforms[i] == null) continue;

            // Luôn luôn hiển thị đủ 9 ô trên màn hình để không bị dịch chuyển Layout
            if (!slotTransforms[i].gameObject.activeSelf)
            {
                slotTransforms[i].gameObject.SetActive(true);
            }

            bool isSlotActive = (i < activeCap);

            // Nếu ô này chưa được mở khóa (chưa nhặt Balo)
            if (!isSlotActive)
            {
                slotTransforms[i].localScale = normalScale;
                EnsureSlotIcon(i);
                if (slotIconImages != null && i < slotIconImages.Length && slotIconImages[i] != null)
                {
                    slotIconImages[i].gameObject.SetActive(false);
                }
                if (slot3DModels != null && i < slot3DModels.Length && slot3DModels[i] != null)
                {
                    slot3DModels[i].SetActive(false);
                }
                continue;
            }

            // 1. Phóng to ô đang được chọn
            if (i == selectedIndex)
            {
                slotTransforms[i].localScale = selectedScale;
            }
            else
            {
                slotTransforms[i].localScale = normalScale;
            }

            // 2. Tắt/bật Icon con nếu ô đó có vật phẩm
            EnsureSlotIcon(i);
            if (slotIconImages != null && i < slotIconImages.Length && slotIconImages[i] != null)
            {
                bool hasItem = (i < itemCount) && !string.IsNullOrEmpty(heldItems[i]);
                slotIconImages[i].gameObject.SetActive(hasItem);

                if (hasItem)
                {
                    Sprite icon = (heldItemSprites != null && i < heldItemSprites.Length) ? heldItemSprites[i] : null;

                    slotIconImages[i].sprite = icon;
                    slotIconImages[i].color = (icon != null) ? Color.white : Color.clear;
                }
            }
        }
    }

    /// <summary>
    /// Mở khóa Balo -> Mở rộng sức chứa túi đồ từ 5 ô lên tối đa 9 ô!
    /// </summary>
    public void UnlockBackpack()
    {
        hasUnlockedBackpack = true;
        Debug.Log($"[InventoryManager] 🎒 ĐÃ MỞ KHÓA BALO! Sức chứa túi đồ mở rộng lên {CurrentCapacity} ô.");

        if (backpackUnlockSound != null)
        {
            AudioSource aSrc = GetComponent<AudioSource>();
            if (aSrc == null) aSrc = gameObject.AddComponent<AudioSource>();
            aSrc.PlayOneShot(backpackUnlockSound, 0.9f);
        }

        UpdateUISlots();
    }

    public void AddQuestItem(string questName)
    {
        currentQuestItemCount++;
        if (questProgressTextObject != null)
        {
            questProgressTextObject.SetActive(true);
            if (questProgressText != null)
            {
                questProgressText.text = questName + ": " + currentQuestItemCount + "/" + totalQuestItemsNeeded;
            }
        }
        if (currentQuestItemCount >= totalQuestItemsNeeded && questProgressText != null)
        {
            questProgressText.text = questName + ": Hoàn thành!";
        }
    }

    public static void ResetInventoryData()
    {
        savedHeldItems = null;
        hasUnlockedBackpack = false;
    }
    // Hàm kiểm tra xem trong túi có món đồ này chưa (Không phân biệt hoa thường và khoảng trắng)
    public bool HasItem(string itemName)
    {
        if (heldItems == null || string.IsNullOrEmpty(itemName)) return false;

        string target = itemName.Trim().ToLower();
        foreach (string item in heldItems)
        {
            if (string.IsNullOrEmpty(item)) continue;
            string current = item.Trim().ToLower();

            // Khớp chính xác hoặc chứa tên (ví dụ 'Key' khớp 'key1Map02', 'key')
            if (current == target || current.Contains(target) || target.Contains(current))
            {
                return true;
            }
        }
        return false;
    }

    // Hàm xóa món đồ sau khi dùng (VD: Chìa khóa mở cửa)
    public void RemoveItem(string itemName)
    {
        if (heldItems == null || string.IsNullOrEmpty(itemName)) return;
        string target = itemName.Trim().ToLower();

        for (int i = 0; i < heldItems.Length; i++)
        {
            if (string.IsNullOrEmpty(heldItems[i])) continue;
            string current = heldItems[i].Trim().ToLower();

            if (current == target || current.Contains(target) || target.Contains(current))
            {
                heldItems[i] = ""; // Xóa tên item
                if (heldItemSprites != null && i < heldItemSprites.Length) heldItemSprites[i] = null;
                if (heldItemObjects[i] != null) Destroy(heldItemObjects[i]); // Hủy object
                heldItemObjects[i] = null;
                Destroy3DPreview(i);

                // Tự động dồn các ô slot sang trái để không bị trống ở giữa
                ConsolidateSlots();
                break;
            }
        }
    }

    /// <summary>
    /// Tự động dồn tất cả vật phẩm còn lại sang trái, lấp đầy các ô trống
    /// </summary>
    public void ConsolidateSlots()
    {
        if (heldItems == null) return;
        int slotCount = heldItems.Length;

        System.Collections.Generic.List<string> itemNames = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<GameObject> itemObjs = new System.Collections.Generic.List<GameObject>();
        System.Collections.Generic.List<Sprite> itemSprites = new System.Collections.Generic.List<Sprite>();
        System.Collections.Generic.List<InteractableItem.ItemType> itemTypes = new System.Collections.Generic.List<InteractableItem.ItemType>();

        for (int i = 0; i < slotCount; i++)
        {
            if (!string.IsNullOrEmpty(heldItems[i]))
            {
                itemNames.Add(heldItems[i]);
                itemObjs.Add(heldItemObjects != null && i < heldItemObjects.Length ? heldItemObjects[i] : null);
                itemSprites.Add(heldItemSprites != null && i < heldItemSprites.Length ? heldItemSprites[i] : null);
                itemTypes.Add(heldItemTypes != null && i < heldItemTypes.Length ? heldItemTypes[i] : InteractableItem.ItemType.Consumable);
            }
        }

        // Hủy 3D preview cũ của tất cả các ô
        for (int i = 0; i < slotCount; i++)
        {
            Destroy3DPreview(i);
            heldItems[i] = "";
            if (heldItemObjects != null && i < heldItemObjects.Length) heldItemObjects[i] = null;
            if (heldItemSprites != null && i < heldItemSprites.Length) heldItemSprites[i] = null;
            if (heldItemTypes != null && i < heldItemTypes.Length) heldItemTypes[i] = InteractableItem.ItemType.Consumable;
        }

        // Đẩy toàn bộ item vào lại từ ô 0 trở đi
        for (int i = 0; i < itemNames.Count; i++)
        {
            heldItems[i] = itemNames[i];
            if (heldItemObjects != null && i < heldItemObjects.Length) heldItemObjects[i] = itemObjs[i];
            if (heldItemSprites != null && i < heldItemSprites.Length) heldItemSprites[i] = itemSprites[i];
            if (heldItemTypes != null && i < heldItemTypes.Length) heldItemTypes[i] = itemTypes[i];
            Create3DPreviewForSlot(i, itemNames[i], itemObjs[i]);
        }

        // Chỉnh lại ô đang chọn nếu bị vượt quá số lượng item
        if (selectedIndex >= itemNames.Count)
        {
            selectedIndex = (itemNames.Count > 0) ? itemNames.Count - 1 : -1;
        }

        UpdateUISlots();
    }
}
