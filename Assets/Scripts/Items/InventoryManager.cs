using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        int slotCount = (slotTransforms != null && slotTransforms.Length > 0) ? slotTransforms.Length : 5;
        heldItems = new string[slotCount];
        heldItemObjects = new GameObject[slotCount];
        heldItemSprites = new Sprite[slotCount];
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

            bool shouldShow = panelActive && (heldItems != null && i < heldItems.Length && !string.IsNullOrEmpty(heldItems[i]));
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

                // Nếu RotateSpeed > 0 thì mới xoay, ngược lại giữ nguyên góc nghiêng tĩnh 3D
                if (previewRotateSpeed > 0.01f)
                {
                    slot3DModels[i].transform.Rotate(Vector3.up, previewRotateSpeed * Time.deltaTime, Space.Self);
                }
                else
                {
                    slot3DModels[i].transform.localRotation = Quaternion.Euler(previewTiltEuler);
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
    public bool AddConsumableItem(string itemName, GameObject itemObj, Sprite itemSprite = null)
    {
        if (heldItems == null) return false;
        int slotCount = heldItems.Length;
        if (heldItemSprites == null || heldItemSprites.Length != slotCount) heldItemSprites = new Sprite[slotCount];
        if (slot3DModels == null || slot3DModels.Length != slotCount) slot3DModels = new GameObject[slotCount];

        // TÌM Ô TRỐNG ĐẦU TIÊN TRONG 5 Ô ĐỂ NHÉT ITEM VÀO
        for (int i = 0; i < slotCount; i++)
        {
            if (string.IsNullOrEmpty(heldItems[i]))
            {
                heldItems[i] = itemName;
                heldItemObjects[i] = itemObj;
                heldItemSprites[i] = itemSprite;

                // Tạo Model 3D xoay trong ô Slot
                Create3DPreviewForSlot(i, itemName, itemObj);

                if (itemObj != null) itemObj.SetActive(false);

                if (selectedIndex == -1) ToggleSelect(i);
                else UpdateUISlots();

                Debug.Log($"[Inventory] 🎒 Đã nhặt '{itemName}' vào ô Slot {i + 1}");
                return true;
            }
        }

        Debug.Log("⚠️ Túi đồ đã đầy (Đủ 5 ô)! Không thể nhặt thêm " + itemName);
        return false;
    }

    // --- XỬ LÝ SỬ DỤNG VẬT PHẨM (PHÍM R) ---
    private void HandleUseInput()
    {
        if (Input.GetKeyDown(useKey))
        {
            if (selectedIndex >= 0 && heldItems != null && selectedIndex < heldItems.Length && !string.IsNullOrEmpty(heldItems[selectedIndex]))
            {
                string itemName = heldItems[selectedIndex];
                bool isBattery = itemName.ToLower().Contains("pin") || itemName.ToLower().Contains("battery") || itemName.ToLower().Contains("thu thap");

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
                else
                {
                    Debug.Log($"[Inventory] Chưa có logic dùng cho vật phẩm '{itemName}'");
                }
            }
        }
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
        int count = heldItems.Length;

        if (Input.GetKeyDown(KeyCode.Alpha1) && count > 0) ToggleSelect(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && count > 1) ToggleSelect(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) && count > 2) ToggleSelect(2);
        if (Input.GetKeyDown(KeyCode.Alpha4) && count > 3) ToggleSelect(3);
        if (Input.GetKeyDown(KeyCode.Alpha5) && count > 4) ToggleSelect(4);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (count == 0) return;

            if (selectedIndex == -1) ToggleSelect(0);
            else
            {
                int newIndex = selectedIndex;
                if (scroll > 0f) newIndex--;
                else newIndex++;

                if (newIndex < 0) newIndex = count - 1;
                if (newIndex >= count) newIndex = 0;

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

        // Đảm bảo khung 5 ô túi đồ luôn luôn hiển thị trên màn hình (như Minecraft)
        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(true);
        }

        for (int i = 0; i < slotCount; i++)
        {
            if (slotTransforms[i] == null) continue;

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

        for (int i = 0; i < slotCount; i++)
        {
            if (!string.IsNullOrEmpty(heldItems[i]))
            {
                itemNames.Add(heldItems[i]);
                itemObjs.Add(heldItemObjects != null && i < heldItemObjects.Length ? heldItemObjects[i] : null);
                itemSprites.Add(heldItemSprites != null && i < heldItemSprites.Length ? heldItemSprites[i] : null);
            }
        }

        // Hủy 3D preview cũ của tất cả các ô
        for (int i = 0; i < slotCount; i++)
        {
            Destroy3DPreview(i);
            heldItems[i] = "";
            if (heldItemObjects != null && i < heldItemObjects.Length) heldItemObjects[i] = null;
            if (heldItemSprites != null && i < heldItemSprites.Length) heldItemSprites[i] = null;
        }

        // Đẩy toàn bộ item vào lại từ ô 0 trở đi
        for (int i = 0; i < itemNames.Count; i++)
        {
            heldItems[i] = itemNames[i];
            if (heldItemObjects != null && i < heldItemObjects.Length) heldItemObjects[i] = itemObjs[i];
            if (heldItemSprites != null && i < heldItemSprites.Length) heldItemSprites[i] = itemSprites[i];
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
