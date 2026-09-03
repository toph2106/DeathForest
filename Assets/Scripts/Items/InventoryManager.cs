using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // MẢNG STATIC LƯU GIỮ CÁC VẬT PHẨM KHI CHUYỂN MAP
    private static string[] savedHeldItems = null;
    private static InteractableItem.ItemType[] savedHeldItemTypes = null;

    public static string[] SavedHeldItems { get => savedHeldItems; set => savedHeldItems = value; }
    public static InteractableItem.ItemType[] SavedHeldItemTypes { get => savedHeldItemTypes; set => savedHeldItemTypes = value; }

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

    // Biến static lưu trạng thái mở khóa Balo xuyên suốt các scene/map (Mặc định: false)
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
    [Tooltip("Bật chế độ hiển thị Model 3D thật trong từng ô slot")]
    public bool enable3DItemPreview = true;
    [Tooltip("Danh sách Prefab 3D gán sẵn theo tên (VD: Tên 'Pin' -> Kéo Prefab Battery vào đây)")]
    public Item3DPrefabEntry[] default3DPrefabs;
    [Tooltip("Khoảng cách đặt Model 3D trước Camera theo trục Z (Mặc định: 0.35 đồng bộ chuẩn Map 04)")]
    public float previewDistance = 0.35f;
    [Tooltip("Kích thước hiển thị của Model 3D trong ô (Mặc định: 0.035 đồng bộ chuẩn Map 04)")]
    public float previewItemScale = 0.035f;
    [Tooltip("Tốc độ xoay của Model 3D (Mặc định: 45 xoay đều mượt mà như Map 04)")]
    public float previewRotateSpeed = 45f;
    [Tooltip("Góc nghiêng của Model 3D khi hiển thị (Mặc định: (20, 0, 0) như Map 04)")]
    public Vector3 previewTiltEuler = new Vector3(20f, 0f, 0f);

    [HideInInspector] public GameObject questProgressTextObject;
    [HideInInspector] public TextMeshProUGUI questProgressText;
    [HideInInspector] public int totalQuestItemsNeeded = 3;

    [HideInInspector] public string[] heldItems;
    [HideInInspector] public GameObject[] heldItemObjects;
    [HideInInspector] public Sprite[] heldItemSprites;
    [HideInInspector] public InteractableItem.ItemType[] heldItemTypes;
    private GameObject[] slot3DModels; // Mảng lưu các GameObject 3D preview
    private Vector3[] slotBaseScales; // Lưu Scale chuẩn để Zoom khi chọn ô
    private Image[] slotIconImages; // Các hình Icon con bên trong ô Slot UI
    private Color[] slotOriginalColors; // Lưu màu nền gốc của từng ô slot
    private Color lockedSlotColor = new Color(0.12f, 0.12f, 0.12f, 0.22f); // Màu tối cho các ô bị khóa

    private int selectedIndex = -1;
    private int currentQuestItemCount = 0;

    private Vector3 normalScale = Vector3.one;
    private Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1.15f);

    void Awake()
    {
        bool isUI = (GetComponent<RectTransform>() != null || GetComponentInParent<Canvas>() != null);
        if (!isUI)
        {
            Debug.LogWarning($"[InventoryManager] ⚠️ Phát hiện InventoryManager gắn thừa trên '{gameObject.name}'. Tự động hủy.");
            Destroy(this);
            return;
        }

        if (Instance != null && Instance != this)
        {
            bool oldIsUI = (Instance.GetComponent<RectTransform>() != null || Instance.GetComponentInParent<Canvas>() != null);
            if (!oldIsUI)
            {
                Destroy(Instance);
                Instance = this;
            }
            else
            {
                if (slotTransforms != null && slotTransforms.Length > 0 && slotTransforms[0] != null)
                {
                    Instance.inventoryPanel = inventoryPanel;
                    Instance.slotTransforms = slotTransforms;
                }
                Destroy(this);
                return;
            }
        }
        else
        {
            Instance = this;
        }

        SetupStandardResponsiveUI();
    }

    void OnDestroy()
    {
        DestroyAll3DPreviews();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        if (questProgressTextObject != null) questProgressTextObject.SetActive(false);

        SetupStandardResponsiveUI();
        RestoreInventoryState();
    }

    public void SetupStandardResponsiveUI()
    {
        if (default3DPrefabs == null || default3DPrefabs.Length == 0)
        {
            GameObject batPrefab = LoadPrefabFromResources("Battery");
            if (batPrefab != null)
            {
                default3DPrefabs = new Item3DPrefabEntry[]
                {
                    new Item3DPrefabEntry { itemName = "Pin", prefab = batPrefab },
                    new Item3DPrefabEntry { itemName = "Battery", prefab = batPrefab }
                };
            }
        }

        if (inventoryPanel == null)
        {
            if (slotTransforms != null && slotTransforms.Length > 0 && slotTransforms[0] != null)
            {
                Transform parent = slotTransforms[0].parent;
                if (parent != null) inventoryPanel = parent.gameObject;
            }
        }

        if (inventoryPanel != null && (slotTransforms == null || slotTransforms.Length == 0))
        {
            List<Transform> list = new List<Transform>();
            for (int c = 0; c < inventoryPanel.transform.childCount; c++)
            {
                Transform child = inventoryPanel.transform.GetChild(c);
                if (child != null) list.Add(child);
            }
            if (list.Count > 0)
            {
                slotTransforms = list.ToArray();
            }
        }

        if (slotTransforms != null && slotTransforms.Length > 0)
        {
            slotOriginalColors = new Color[slotTransforms.Length];
            for (int i = 0; i < slotTransforms.Length; i++)
            {
                if (slotTransforms[i] != null)
                {
                    Image img = slotTransforms[i].GetComponent<Image>();
                    if (img != null)
                    {
                        slotOriginalColors[i] = img.color;
                    }
                    else
                    {
                        slotOriginalColors[i] = new Color(0f, 0f, 0f, 0.47f);
                    }
                }
            }

            if (slotTransforms[0] != null)
            {
                normalScale = slotTransforms[0].localScale;
                selectedScale = normalScale * 1.15f;
            }
        }
        else
        {
            normalScale = Vector3.one;
            selectedScale = new Vector3(1.15f, 1.15f, 1.15f);
        }
    }
    public void RestoreInventoryState()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // DỌN SẠCH MỌI PREVIEW 3D CŨ TRƯỚC KHI TẠO MỚI (TRÁNH TRÙNG LẶP / CHỒNG CHÉO)
        DestroyAll3DPreviews();

        // 1. Nếu bắt đầu New Game / Reset Game
        if (GameSaveManager.isResettingData)
        {
            ResetInventoryData();
            if (GameSaveManager.HasSaveFile()) GameSaveManager.DeleteSaveFile();
        }

        // 2. Cấu hình mở khóa Balo và Nạp dữ liệu theo Scene:
        if (sceneName == "Map01" || sceneName == "Map02")
        {
            hasUnlockedBackpack = false;
            PlayerPrefs.SetInt("Global_Has_Backpack", 0);

            // Trong Map 01 và Map 02: TUYỆT ĐỐI KHÔNG nạp JSON save của Map 03 / Map 04!
            // Chỉ nạp dữ liệu của chính Map 02 nếu có trong PlayerPrefs
            if (savedHeldItems == null || savedHeldItems.Length == 0)
            {
                LoadFromPlayerPrefs();
            }

            // Lọc sạch 100% các món đồ nhiệm vụ của Map 03/Map 04 (tay, chân, đầu, gore, bùa...)
            if (savedHeldItems != null)
            {
                for (int i = 0; i < savedHeldItems.Length; i++)
                {
                    string it = savedHeldItems[i];
                    if (!string.IsNullOrEmpty(it))
                    {
                        string lower = it.ToLower();
                        if (lower.Contains("gore") || lower.Contains("arm") || lower.Contains("feet") || lower.Contains("head") || lower.Contains("bua"))
                        {
                            savedHeldItems[i] = "";
                            if (savedHeldItemTypes != null && i < savedHeldItemTypes.Length)
                            {
                                savedHeldItemTypes[i] = InteractableItem.ItemType.Consumable;
                            }
                        }
                    }
                }
            }
        }
        else if (sceneName == "Map03")
        {
            if (GameSaveManager.HasMap02Checkpoint())
            {
                GameSaveManager.ApplyMap02CheckpointToCurrentSave();
                hasUnlockedBackpack = false; // Balo nằm trong nhà ở Map03, chưa nhặt thì luôn bị khóa 4 ô
                PlayerPrefs.SetInt("Global_Has_Backpack", 0);
                PlayerPrefs.Save();
            }

            if (GameSaveManager.HasSaveFile())
            {
                LoadFromJson();
            }
            else if (savedHeldItems == null || savedHeldItems.Length == 0)
            {
                LoadFromPlayerPrefs();
            }

            // Khi vào Map 03: Nếu là từ Map 02 sang thì lọc sạch các món quest cũ, chỉ giữ lại pin / nước
            if (GameSaveManager.HasMap02Checkpoint() && savedHeldItems != null)
            {
                for (int i = 0; i < savedHeldItems.Length; i++)
                {
                    string it = savedHeldItems[i];
                    if (!string.IsNullOrEmpty(it))
                    {
                        string lower = it.ToLower();
                        if (lower.Contains("gore") || lower.Contains("arm") || lower.Contains("feet") || lower.Contains("head") || lower.Contains("bua"))
                        {
                            savedHeldItems[i] = "";
                            if (savedHeldItemTypes != null && i < savedHeldItemTypes.Length)
                            {
                                savedHeldItemTypes[i] = InteractableItem.ItemType.Consumable;
                            }
                        }
                    }
                }
            }
        }
        else if (sceneName == "Map04" || sceneName == "Map05")
        {
            hasUnlockedBackpack = true;
            PlayerPrefs.SetInt("Global_Has_Backpack", 1);

            if (GameSaveManager.HasSaveFile())
            {
                LoadFromJson();
                hasUnlockedBackpack = true;
            }
            else if (savedHeldItems == null || savedHeldItems.Length == 0)
            {
                LoadFromPlayerPrefs();
                hasUnlockedBackpack = true;
            }
        }

        // Khi vào Map 04 hoặc Map 05: Bắt buộc đảm bảo có đủ 8 món theo yêu cầu qua cảnh (Bùa, 2 Tay, 2 Chân, Đầu, 2 Gore)
        if (sceneName == "Map04" || sceneName == "Map05")
        {
            hasUnlockedBackpack = true;
            string[] required8 = new string[] { "Bua", "ArmsL", "ArmsR", "FeetL", "FeetR", "Head", "Gore", "Gore" };

            List<string> currentList = new List<string>();
            if (savedHeldItems != null)
            {
                foreach (var it in savedHeldItems)
                {
                    if (!string.IsNullOrEmpty(it)) currentList.Add(it);
                }
            }

            bool hasAll8 = true;
            List<string> tempCheck = new List<string>(currentList);
            foreach (var req in required8)
            {
                int idx = tempCheck.FindIndex(x => x.ToLower().Contains(req.ToLower()));
                if (idx >= 0) tempCheck.RemoveAt(idx);
                else { hasAll8 = false; break; }
            }

            if (!hasAll8)
            {
                List<string> new8List = new List<string>(required8);
                foreach (var extra in currentList)
                {
                    if (extra.ToLower().Contains("pin") || extra.ToLower().Contains("battery") || extra.ToLower().Contains("water"))
                    {
                        if (new8List.Count < 9) new8List.Add(extra);
                    }
                }

                savedHeldItems = new8List.ToArray();
                savedHeldItemTypes = new InteractableItem.ItemType[savedHeldItems.Length];
                for (int i = 0; i < savedHeldItems.Length; i++)
                {
                    string it = savedHeldItems[i].ToLower();
                    if (it.Contains("pin") || it.Contains("battery")) savedHeldItemTypes[i] = InteractableItem.ItemType.Battery;
                    else if (it.Contains("key")) savedHeldItemTypes[i] = InteractableItem.ItemType.Key;
                    else savedHeldItemTypes[i] = InteractableItem.ItemType.Consumable;
                }
            }
        }

        // 4. Khởi tạo mảng dữ liệu túi đồ (sức chứa đủ theo số slot trong prefab / UI, thường là 9)
        int totalCap = (slotTransforms != null && slotTransforms.Length > 0) ? slotTransforms.Length : expandedSlotCount;
        heldItems = new string[totalCap];
        heldItemObjects = new GameObject[totalCap];
        heldItemSprites = new Sprite[totalCap];
        heldItemTypes = new InteractableItem.ItemType[totalCap];
        slot3DModels = new GameObject[totalCap];
        slotBaseScales = new Vector3[totalCap];
        slotIconImages = new Image[totalCap];

        for (int i = 0; i < totalCap; i++)
        {
            heldItems[i] = "";
            heldItemObjects[i] = null;
            heldItemSprites[i] = null;
            heldItemTypes[i] = InteractableItem.ItemType.Consumable;
            slot3DModels[i] = null;

            if (slotTransforms != null && i < slotTransforms.Length && slotTransforms[i] != null)
            {
                slotTransforms[i].localScale = normalScale;
                EnsureSlotIcon(i);
            }
        }

        // 5. Nạp dữ liệu vào các ô đang mở khóa (5 ô nếu chưa có Balo, 9 ô nếu đã có)
        int activeCap = CurrentCapacity;
        if (savedHeldItems != null && savedHeldItems.Length > 0)
        {
            int copyCount = Mathf.Min(savedHeldItems.Length, activeCap);
            for (int i = 0; i < copyCount; i++)
            {
                heldItems[i] = savedHeldItems[i];
                if (savedHeldItemTypes != null && i < savedHeldItemTypes.Length)
                {
                    heldItemTypes[i] = savedHeldItemTypes[i];
                }
            }
        }

        // 6. Khôi phục 3D preview cho các ô có vật phẩm trong số ô đang mở khóa
        if (enable3DItemPreview)
        {
            for (int i = 0; i < activeCap; i++)
            {
                if (!string.IsNullOrEmpty(heldItems[i]))
                {
                    Create3DPreviewForSlot(i, heldItems[i], null);
                }
            }
        }

        UpdateUISlots();
        Debug.Log($"[InventoryManager] 🎒 Đã khôi phục Hotbar ({CurrentCapacity} ô mở khóa, Balo: {hasUnlockedBackpack}, {GetItemCount()} món) cho '{sceneName}'!");
    }

    public int GetItemCount()
    {
        if (heldItems == null) return 0;
        int count = 0;
        int activeCap = CurrentCapacity;
        for (int i = 0; i < activeCap && i < heldItems.Length; i++)
        {
            if (!string.IsNullOrEmpty(heldItems[i])) count++;
        }
        return count;
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
                newImg.color = Color.clear;
                iconObj.SetActive(false);
                slotIconImages[i] = newImg;
            }
        }
    }

    public void SaveInventoryData()
    {
        if (GameSaveManager.isResettingData) return;
        if (heldItems == null || heldItems.Length == 0) return;

        savedHeldItems = (string[])heldItems.Clone();
        if (heldItemTypes != null)
        {
            savedHeldItemTypes = (InteractableItem.ItemType[])heldItemTypes.Clone();
        }

        PlayerPrefs.SetInt("Global_Has_Backpack", hasUnlockedBackpack ? 1 : 0);
        string itemsStr = string.Join("|;;|", heldItems);
        PlayerPrefs.SetString("Global_Inventory_Items", itemsStr);

        if (heldItemTypes != null)
        {
            List<string> typeVals = new List<string>();
            foreach (var t in heldItemTypes) typeVals.Add(((int)t).ToString());
            PlayerPrefs.SetString("Global_Inventory_Types", string.Join(",", typeVals));
        }

        PlayerPrefs.Save();
        SaveToJson();
    }

    private void SaveToJson()
    {
        if (GameSaveManager.isResettingData) return;
        GameSaveManager.SaveGame();
    }

    private void LoadFromJson()
    {
        GameSaveData data = GameSaveManager.LoadGame();
        if (data != null && data.inventoryItems != null && data.inventoryItems.Count > 0)
        {
            savedHeldItems = data.inventoryItems.ToArray();
            if (data.inventoryTypes != null && data.inventoryTypes.Count == savedHeldItems.Length)
            {
                savedHeldItemTypes = new InteractableItem.ItemType[savedHeldItems.Length];
                for (int i = 0; i < savedHeldItems.Length; i++)
                {
                    savedHeldItemTypes[i] = (InteractableItem.ItemType)data.inventoryTypes[i];
                }
            }
            hasUnlockedBackpack = data.hasBackpack;
        }
        else
        {
            LoadFromPlayerPrefs();
        }
    }

    private void LoadFromPlayerPrefs()
    {
        hasUnlockedBackpack = (PlayerPrefs.GetInt("Global_Has_Backpack", 0) == 1);

        if (PlayerPrefs.HasKey("Global_Inventory_Items"))
        {
            string namesStr = PlayerPrefs.GetString("Global_Inventory_Items");
            if (!string.IsNullOrEmpty(namesStr))
            {
                string[] parts = namesStr.Split(new string[] { "|;;|" }, System.StringSplitOptions.None);
                savedHeldItems = parts;

                if (PlayerPrefs.HasKey("Global_Inventory_Types"))
                {
                    string typesStr = PlayerPrefs.GetString("Global_Inventory_Types");
                    string[] tParts = typesStr.Split(',');
                    savedHeldItemTypes = new InteractableItem.ItemType[tParts.Length];
                    for (int i = 0; i < tParts.Length; i++)
                    {
                        if (int.TryParse(tParts[i], out int val))
                        {
                            savedHeldItemTypes[i] = (InteractableItem.ItemType)val;
                        }
                    }
                }
            }
        }
    }

    public static GameObject LoadPrefabFromResources(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        string cleanName = itemName.Trim();
        GameObject p = Resources.Load<GameObject>("ItemPrefabs/" + cleanName);
        if (p != null)
        {
            MeshFilter mf = p.GetComponentInChildren<MeshFilter>(true);
            if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.name.ToLower().Contains("cube"))
            {
                return p;
            }
        }

        string lower = cleanName.ToLower().Replace(" ", "").Replace("_", "");
        string targetPrefab = null;

        if (lower.Contains("bua") || lower.Contains("charm") || lower.Contains("talisman") || lower.Contains("paper")) targetPrefab = "Bua";
        else if (lower.Contains("armsl") || lower.Contains("arml") || lower.Contains("taytrai")) targetPrefab = "ArmsL";
        else if (lower.Contains("armsr") || lower.Contains("armr") || lower.Contains("tayphai")) targetPrefab = "ArmsR";
        else if (lower.Contains("feetl") || lower.Contains("footl") || lower.Contains("chantrai")) targetPrefab = "FeetL";
        else if (lower.Contains("feetr") || lower.Contains("footr") || lower.Contains("chanphai")) targetPrefab = "FeetR";
        else if (lower.Contains("head") || lower.Contains("dau")) targetPrefab = "Head";
        else if (lower.Contains("gore") || lower.Contains("xac") || lower.Contains("ruot")) targetPrefab = "Gore";
        else if (lower.Contains("pin") || lower.Contains("battery")) targetPrefab = "Battery";
        else if (lower.Contains("can") || lower.Contains("nuoc") || lower.Contains("water") || lower.Contains("drink") || lower.Contains("soda") || lower.Contains("lon")) targetPrefab = "Can";
        else if (lower.Contains("key") || lower.Contains("khoa")) targetPrefab = "Key";

        if (!string.IsNullOrEmpty(targetPrefab))
        {
            p = Resources.Load<GameObject>("ItemPrefabs/" + targetPrefab);
            if (p != null)
            {
                MeshFilter mf = p.GetComponentInChildren<MeshFilter>(true);
                if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.name.ToLower().Contains("cube"))
                {
                    return p;
                }
            }
        }

        // Tự động tìm model 3D thật trong Scene nếu Resources thiếu hoặc bị thành Cube
        InteractableItem[] allItems = Object.FindObjectsByType<InteractableItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var it in allItems)
        {
            if (it == null) continue;
            string iname = (it.itemNameOrQuestName ?? it.gameObject.name).ToLower().Replace(" ", "").Replace("_", "");
            if (lower.Contains("pin") || lower.Contains("battery"))
            {
                if ((iname.Contains("pin") || iname.Contains("battery")) && it.GetComponentInChildren<MeshFilter>(true) != null)
                {
                    MeshFilter mf = it.GetComponentInChildren<MeshFilter>(true);
                    if (mf != null && mf.sharedMesh != null && !mf.sharedMesh.name.ToLower().Contains("cube"))
                    {
                        return it.gameObject;
                    }
                }
            }
            else if (lower.Contains("can") || lower.Contains("nuoc") || lower.Contains("drink") || lower.Contains("water") || lower.Contains("soda") || lower.Contains("lon"))
            {
                if (iname.Contains("can") || iname.Contains("nuoc") || iname.Contains("drink") || iname.Contains("water") || iname.Contains("lon"))
                {
                    return it.gameObject;
                }
            }
            else if (!string.IsNullOrEmpty(targetPrefab) && iname.Contains(targetPrefab.ToLower()))
            {
                return it.gameObject;
            }
        }

        return null;
    }

    void OnDisable()
    {
        if (heldItems != null && heldItems.Length > 0)
        {
            SaveInventoryData();
        }
        DestroyAll3DPreviews();
    }

    void Update()
    {
        if (PauseMenuManager.isPaused) return;

        HandleCheatInput();
        HandleSelectionInput();
        HandleUseInput();
        HandleDropInput();
    }

    public Vector2 GetSlotScreenPoint(RectTransform slotRt)
    {
        if (slotRt == null) return new Vector2(Screen.width * 0.5f, Screen.height * 0.1f);

        Canvas canvas = slotRt.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Vector3 centerWorld = slotRt.TransformPoint(slotRt.rect.center);

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, centerWorld);
            }

            RectTransform canvasRt = canvas.transform as RectTransform;
            if (canvasRt != null)
            {
                Vector3 centerInCanvas = canvasRt.InverseTransformPoint(centerWorld);
                float scaleX = (canvasRt.rect.width > 0.0001f) ? ((float)Screen.width / canvasRt.rect.width) : 1f;
                float scaleY = (canvasRt.rect.height > 0.0001f) ? ((float)Screen.height / canvasRt.rect.height) : 1f;

                float screenX = (centerInCanvas.x - canvasRt.rect.xMin) * scaleX;
                float screenY = (centerInCanvas.y - canvasRt.rect.yMin) * scaleY;

                return new Vector2(screenX, screenY);
            }
        }

        return slotRt.position;
    }

    void LateUpdate()
    {
        if (!enable3DItemPreview || slot3DModels == null) return;

        Camera cam = Camera.main;
        if (cam == null) cam = Camera.current;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null) return;

        bool panelActive = (inventoryPanel == null || inventoryPanel.activeInHierarchy);
        int activeCap = CurrentCapacity;

        for (int i = 0; i < slot3DModels.Length; i++)
        {
            if (slot3DModels[i] == null) continue;

            bool slotValid = (slotTransforms != null && i < slotTransforms.Length && slotTransforms[i] != null && slotTransforms[i].gameObject.activeInHierarchy);
            bool shouldShow = panelActive && slotValid && (heldItems != null && i < heldItems.Length && !string.IsNullOrEmpty(heldItems[i])) && (i < activeCap);
            if (slot3DModels[i].activeSelf != shouldShow)
            {
                slot3DModels[i].SetActive(shouldShow);
            }

            if (!shouldShow) continue;

            if (slotTransforms != null && i < slotTransforms.Length && slotTransforms[i] != null)
            {
                RectTransform slotRt = slotTransforms[i] as RectTransform;
                Vector2 screenPoint = GetSlotScreenPoint(slotRt);
                Vector3 targetWorldPos = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, previewDistance));
                slot3DModels[i].transform.position = targetWorldPos;

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
        if (Input.GetKeyDown(KeyCode.B))
        {
            bool success = AddConsumableItem("Pin", null);
            if (success)
            {
                Debug.Log("[DEMO CHEAT] Đã thêm 1 Cục Pin!");
            }
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            if (FlashlightToggle.Instance != null)
            {
                FlashlightToggle.Instance.hasFlashlight = true;
                FlashlightToggle.Instance.RechargeBattery(100f);
                Debug.Log("[DEMO CHEAT] Đã nạp đầy 100% Pin Đèn Pin!");
            }
        }
    }

    public bool AddConsumableItem(string itemName, GameObject itemObj, Sprite itemSprite = null, InteractableItem.ItemType itemType = InteractableItem.ItemType.Consumable)
    {
        if (heldItems == null) return false;
        int slotCount = heldItems.Length;
        int activeCap = CurrentCapacity;
        if (heldItemSprites == null || heldItemSprites.Length != slotCount) heldItemSprites = new Sprite[slotCount];
        if (heldItemObjects == null || heldItemObjects.Length != slotCount) heldItemObjects = new GameObject[slotCount];
        if (heldItemTypes == null || heldItemTypes.Length != slotCount) heldItemTypes = new InteractableItem.ItemType[slotCount];
        if (slot3DModels == null || slot3DModels.Length != slotCount) slot3DModels = new GameObject[slotCount];

        // TÌM Ô TRỐNG ĐẦU TIÊN TRONG SỐ Ô ĐANG MỞ KHÓA (5 Ô KHI CHƯA CÓ BALO, 9 Ô KHI ĐÃ CÓ BALO)
        for (int i = 0; i < activeCap; i++)
        {
            if (string.IsNullOrEmpty(heldItems[i]))
            {
                heldItems[i] = itemName;
                heldItemObjects[i] = itemObj;
                heldItemSprites[i] = itemSprite;
                heldItemTypes[i] = itemType;

                Create3DPreviewForSlot(i, itemName, itemObj);

                if (itemObj != null && itemObj.scene.isLoaded) itemObj.SetActive(false);

                if (selectedIndex == -1) ToggleSelect(i);
                else UpdateUISlots();

                Debug.Log($"[Inventory] 🎒 Đã nhặt '{itemName}' vào ô Slot {i + 1} (Sức chứa hiện tại: {activeCap} ô)");
                SaveInventoryData();
                return true;
            }
        }

        Debug.Log($"⚠️ Túi đồ đã đầy ({activeCap}/{activeCap} ô)! Cần tìm Balo để mở rộng thêm 4 ô.");
        return false;
    }

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

                    if (selectedIndex < heldItemObjects.Length && heldItemObjects[selectedIndex] != null)
                    {
                        Destroy(heldItemObjects[selectedIndex]);
                    }

                    heldItems[selectedIndex] = "";
                    heldItemObjects[selectedIndex] = null;
                    if (heldItemSprites != null && selectedIndex < heldItemSprites.Length) heldItemSprites[selectedIndex] = null;
                    Destroy3DPreview(selectedIndex);

                    ConsolidateSlots();
                    SaveInventoryData();
                }
                else if (isDrink)
                {
                    MovePl player = Object.FindFirstObjectByType<MovePl>();
                    if (player != null)
                    {
                        player.RestoreStaminaInstant();
                    }

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

                    Debug.Log($"[Inventory] 🥤 Đã uống '{itemName}' ở ô Slot {selectedIndex + 1}! Hồi phục 100% thể lực.");

                    if (selectedIndex < heldItemObjects.Length && heldItemObjects[selectedIndex] != null)
                    {
                        Destroy(heldItemObjects[selectedIndex]);
                    }

                    heldItems[selectedIndex] = "";
                    heldItemObjects[selectedIndex] = null;
                    if (heldItemSprites != null && selectedIndex < heldItemSprites.Length) heldItemSprites[selectedIndex] = null;
                    Destroy3DPreview(selectedIndex);

                    ConsolidateSlots();
                    SaveInventoryData();
                }
                else
                {
                    Debug.Log($"[Inventory] Chưa có logic dùng cho vật phẩm '{itemName}'");
                }
            }
        }
    }

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
        int activeCap = CurrentCapacity;
        if (slotTransforms != null)
        {
            Vector2 mousePos = Input.mousePosition;
            for (int i = 0; i < slotTransforms.Length && i < activeCap; i++)
            {
                if (slotTransforms[i] != null && slotTransforms[i].gameObject.activeInHierarchy)
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

        if (selectedIndex >= 0 && selectedIndex < activeCap && heldItems != null && selectedIndex < heldItems.Length && !string.IsNullOrEmpty(heldItems[selectedIndex]))
        {
            return selectedIndex;
        }

        return -1;
    }

    public void DropItem(int slotIndex)
    {
        if (heldItems == null || slotIndex < 0 || slotIndex >= heldItems.Length) return;
        string itemName = heldItems[slotIndex];
        if (string.IsNullOrEmpty(itemName)) return;

        GameObject sourceObj = (heldItemObjects != null && slotIndex < heldItemObjects.Length) ? heldItemObjects[slotIndex] : null;
        Sprite itemSprite = (heldItemSprites != null && slotIndex < heldItemSprites.Length) ? heldItemSprites[slotIndex] : null;

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

        Vector3 forwardDir = Vector3.forward;
        if (cam != null) forwardDir = cam.transform.forward;
        else if (playerT != null) forwardDir = playerT.forward;
        forwardDir.y = 0f;
        if (forwardDir.sqrMagnitude < 0.001f) forwardDir = Vector3.forward;
        forwardDir.Normalize();

        Vector3 dropPos = playerPos + forwardDir * 1.6f + Vector3.up * 1.0f;
        float playerYAngle = (playerT != null) ? playerT.eulerAngles.y : 0f;
        Quaternion dropRot = Quaternion.Euler(80f, playerYAngle + Random.Range(-25f, 25f), Random.Range(-15f, 15f));

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

            if (defaultPrefab == null)
            {
                defaultPrefab = LoadPrefabFromResources(itemName);
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

            Vector3 curScale = droppedObj.transform.localScale;
            if (curScale.x < 0f || curScale.y < 0f || curScale.z < 0f)
            {
                droppedObj.transform.localScale = new Vector3(Mathf.Abs(curScale.x), Mathf.Abs(curScale.y), Mathf.Abs(curScale.z));
            }

            InteractableItem itemComp = droppedObj.GetComponent<InteractableItem>();
            if (itemComp == null) itemComp = droppedObj.AddComponent<InteractableItem>();

            itemComp.itemNameOrQuestName = itemName;
            itemComp.itemIcon = itemSprite;
            itemComp.ResetPickupState();
            itemComp.enabled = true;

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

            DroppedItemPhysics phys = droppedObj.GetComponent<DroppedItemPhysics>();
            if (phys == null) phys = droppedObj.AddComponent<DroppedItemPhysics>();
            phys.LaunchDrop(playerT);
        }

        heldItems[slotIndex] = "";
        if (heldItemObjects != null && slotIndex < heldItemObjects.Length) heldItemObjects[slotIndex] = null;
        if (heldItemSprites != null && slotIndex < heldItemSprites.Length) heldItemSprites[slotIndex] = null;
        if (heldItemTypes != null && slotIndex < heldItemTypes.Length) heldItemTypes[slotIndex] = InteractableItem.ItemType.Consumable;
        Destroy3DPreview(slotIndex);

        ConsolidateSlots();

        if (dropSound != null)
        {
            AudioSource.PlayClipAtPoint(dropSound, dropPos, 0.8f);
        }

        Debug.Log($"[Inventory] 🗑️ Đã ném '{itemName}' ra sàn!");
        SaveInventoryData();
    }

    private Material CreateUnlitPreviewMaterial(Material origMat)
    {
        if (origMat == null) return null;

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                           ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                           ?? Shader.Find("Unlit/Texture")
                           ?? Shader.Find("Unlit/Color");

        Material pMat = (unlitShader != null) ? new Material(unlitShader) : new Material(origMat);

        Texture mainTex = origMat.mainTexture;
        if (mainTex == null && origMat.HasProperty("_BaseMap")) mainTex = origMat.GetTexture("_BaseMap");
        if (mainTex == null && origMat.HasProperty("_MainTex")) mainTex = origMat.GetTexture("_MainTex");
        if (mainTex == null && origMat.HasProperty("_BaseColorTexture")) mainTex = origMat.GetTexture("_BaseColorTexture");
        if (mainTex == null && origMat.HasProperty("diffuseTexture")) mainTex = origMat.GetTexture("diffuseTexture");

        Color col = Color.white;
        if (origMat.HasProperty("_BaseColor")) col = origMat.GetColor("_BaseColor");
        else if (origMat.HasProperty("_Color")) col = origMat.GetColor("_Color");

        // Giữ nguyên độ sáng chuẩn tự nhiên của vật phẩm (như Map 04)
        col.r = Mathf.Clamp01(col.r);
        col.g = Mathf.Clamp01(col.g);
        col.b = Mathf.Clamp01(col.b);
        col.a = 1.0f;

        if (mainTex != null)
        {
            pMat.mainTexture = mainTex;
            if (pMat.HasProperty("_BaseMap")) pMat.SetTexture("_BaseMap", mainTex);
            if (pMat.HasProperty("_MainTex")) pMat.SetTexture("_MainTex", mainTex);
        }
        if (pMat.HasProperty("_BaseColor")) pMat.SetColor("_BaseColor", col);
        if (pMat.HasProperty("_Color")) pMat.SetColor("_Color", col);
        if (pMat.HasProperty("_EmissionColor")) pMat.SetColor("_EmissionColor", Color.black);
        if (pMat.HasProperty("emissiveFactor")) pMat.SetColor("emissiveFactor", Color.black);

        pMat.DisableKeyword("_EMISSION");
        pMat.DisableKeyword("_SPECULAR_SETUP");

        return pMat;
    }

    private void Create3DPreviewForSlot(int slotIndex, string itemName, GameObject itemSource)
    {
        if (!enable3DItemPreview || slotIndex < 0 || slotIndex >= CurrentCapacity) return;

        Destroy3DPreview(slotIndex);

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

        if (prefabToUse == null)
        {
            prefabToUse = LoadPrefabFromResources(itemName);
        }

        if (prefabToUse == null && itemSource != null)
        {
            prefabToUse = itemSource;
        }

        if (prefabToUse == null) return;

        GameObject previewRoot = new GameObject("Preview3D_" + itemName + "_Slot" + (slotIndex + 1));
        GameObject previewChild = Instantiate(prefabToUse);
        previewChild.name = "ModelMesh";

        previewChild.SetActive(true);
        Transform[] allPreviewTrans = previewChild.GetComponentsInChildren<Transform>(true);
        foreach (var t in allPreviewTrans)
        {
            if (t != null) t.gameObject.SetActive(true);
        }

        previewChild.transform.SetParent(previewRoot.transform, false);
        previewChild.transform.localPosition = Vector3.zero;
        previewChild.transform.localRotation = Quaternion.identity;

        Vector3 origScale = prefabToUse.transform.localScale;
        float maxComp = Mathf.Max(Mathf.Abs(origScale.x), Mathf.Abs(origScale.y), Mathf.Abs(origScale.z));
        Vector3 naturalRatio = (maxComp > 0.0001f) ? new Vector3(Mathf.Abs(origScale.x), Mathf.Abs(origScale.y), Mathf.Abs(origScale.z)) / maxComp : Vector3.one;
        previewChild.transform.localScale = naturalRatio;

        foreach (var col in previewChild.GetComponentsInChildren<Collider>(true)) Destroy(col);
        foreach (var rb in previewChild.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        foreach (var audio in previewChild.GetComponentsInChildren<AudioSource>(true)) Destroy(audio);
        foreach (var light in previewChild.GetComponentsInChildren<Light>(true)) Destroy(light);
        foreach (var script in previewChild.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(script);

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;
        previewRoot.layer = uiLayer;
        foreach (Transform t in previewRoot.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = uiLayer;
        }

        Renderer[] rends = previewChild.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (r == null) continue;
            r.enabled = true;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            // Chuyển toàn bộ sang Unlit Material hiển thị rõ ràng vân khối như Map 04
            if (r.materials != null)
            {
                Material[] previewMats = new Material[r.materials.Length];
                for (int m = 0; m < r.materials.Length; m++)
                {
                    previewMats[m] = CreateUnlitPreviewMaterial(r.materials[m]);
                }
                r.materials = previewMats;
            }
        }

        // ĐẶT ROOT VỀ (0,0,0) TRƯỚC KHI TÍNH TOÁN BOUNDS ĐỂ KHÔNG BỊ LỆCH TÂM LÊN TRỜI
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
            // Căn tâm hình học chính xác về gốc tọa độ của previewRoot
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

        previewRoot.transform.eulerAngles = previewTiltEuler;

        if (slotBaseScales == null || slotBaseScales.Length != slot3DModels.Length)
        {
            slotBaseScales = new Vector3[slot3DModels.Length];
        }
        if (slotIndex < slotBaseScales.Length)
        {
            slotBaseScales[slotIndex] = previewRoot.transform.localScale;
        }

        Camera cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (cam != null && slotTransforms != null && slotIndex < slotTransforms.Length && slotTransforms[slotIndex] != null && slotTransforms[slotIndex].gameObject.activeInHierarchy)
        {
            RectTransform slotRt = slotTransforms[slotIndex] as RectTransform;
            Vector2 screenPoint = GetSlotScreenPoint(slotRt);
            previewRoot.transform.position = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, previewDistance));
            previewRoot.SetActive(true);
            previewChild.SetActive(true);
        }
        else
        {
            previewRoot.SetActive(false);
        }

        if (slot3DModels != null && slotIndex < slot3DModels.Length)
        {
            slot3DModels[slotIndex] = previewRoot;
        }
        Debug.Log($"[Inventory] ✨ Đã tạo Model 3D Slot {slotIndex + 1} cho '{itemName}' từ '{prefabToUse.name}'");
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

        string slotSuffix = $"_Slot{slotIndex + 1}";
        GameObject[] allObjs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in allObjs)
        {
            if (obj != null && obj.name.StartsWith("Preview3D_") && obj.name.EndsWith(slotSuffix))
            {
                Destroy(obj);
            }
        }
    }

    public void DestroyAll3DPreviews()
    {
        if (slot3DModels != null)
        {
            for (int i = 0; i < slot3DModels.Length; i++)
            {
                if (slot3DModels[i] != null)
                {
                    Destroy(slot3DModels[i]);
                    slot3DModels[i] = null;
                }
            }
        }

        GameObject[] allObjs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in allObjs)
        {
            if (obj != null && obj.name.StartsWith("Preview3D_"))
            {
                Destroy(obj);
            }
        }
    }

    private void HandleSelectionInput()
    {
        if (heldItems == null) return;
        int activeCap = CurrentCapacity;

        // Chỉ cho phép chọn trong số ô đang mở khóa (1..5 khi chưa có balo, 1..9 khi đã có balo)
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

    public void UpdateUISlots()
    {
        if (slotTransforms == null || heldItems == null) return;
        int slotCount = slotTransforms.Length;
        int itemCount = heldItems.Length;
        int activeCap = CurrentCapacity;

        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(true);
        }

        for (int i = 0; i < slotCount; i++)
        {
            if (slotTransforms[i] == null) continue;

            // LUÔN LUÔN BẬT ĐỦ 9 Ô TRÊN UI ĐỂ HORIZONTAL LAYOUT GROUP (MIDDLE CENTER) KHÔNG BỊ CO GIẬT HOẶC NHẢY RA GIỮA
            if (!slotTransforms[i].gameObject.activeSelf)
            {
                slotTransforms[i].gameObject.SetActive(true);
            }

            bool isSlotUnlocked = (i < activeCap);

            // Cập nhật màu nền: 5 ô đầu sáng bình thường, 4 ô sau tối mờ hơn để biểu thị đang bị khóa
            Image slotBgImg = slotTransforms[i].GetComponent<Image>();
            if (slotBgImg != null)
            {
                if (slotOriginalColors != null && i < slotOriginalColors.Length && slotOriginalColors[i].a > 0.01f)
                {
                    slotBgImg.color = isSlotUnlocked ? slotOriginalColors[i] : lockedSlotColor;
                }
            }

            // Nếu ô đang bị khóa (chưa nhặt Balo)
            if (!isSlotUnlocked)
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
                Sprite icon = (hasItem && heldItemSprites != null && i < heldItemSprites.Length) ? heldItemSprites[i] : null;

                if (hasItem && icon != null)
                {
                    slotIconImages[i].sprite = icon;
                    slotIconImages[i].color = Color.white;
                    slotIconImages[i].gameObject.SetActive(true);
                }
                else
                {
                    slotIconImages[i].sprite = null;
                    slotIconImages[i].color = Color.clear;
                    slotIconImages[i].gameObject.SetActive(false);
                }
            }

            // 3. Cập nhật hiển thị Model 3D preview
            if (slot3DModels != null && i < slot3DModels.Length && slot3DModels[i] != null)
            {
                bool hasItem = (i < itemCount) && !string.IsNullOrEmpty(heldItems[i]);
                slot3DModels[i].SetActive(hasItem);
            }
        }
    }

    /// <summary>
    /// Mở khóa Balo -> Mở rộng sức chứa túi đồ từ 5 ô lên tối đa 9 ô!
    /// </summary>
    public void UnlockBackpack()
    {
        hasUnlockedBackpack = true;
        Debug.Log($"[InventoryManager] 🎒 ĐÃ MỞ KHÓA BALO! Sức chứa túi đồ mở rộng từ 5 lên {CurrentCapacity} ô.");

        if (backpackUnlockSound != null)
        {
            AudioSource aSrc = GetComponent<AudioSource>();
            if (aSrc == null) aSrc = gameObject.AddComponent<AudioSource>();
            aSrc.PlayOneShot(backpackUnlockSound, 0.9f);
        }

        PlayerPrefs.SetInt("Global_Has_Backpack", 1);
        PlayerPrefs.Save();
        SaveInventoryData();
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
        savedHeldItemTypes = null;
        hasUnlockedBackpack = false;
        PlayerPrefs.DeleteKey("Global_Inventory_Items");
        PlayerPrefs.DeleteKey("Global_Inventory_Types");
        PlayerPrefs.DeleteKey("Global_Has_Backpack");
        PlayerPrefs.Save();
    }

    public bool HasItem(string itemName)
    {
        if (heldItems == null || string.IsNullOrEmpty(itemName)) return false;

        string target = itemName.Trim().ToLower();
        for (int i = 0; i < CurrentCapacity && i < heldItems.Length; i++)
        {
            string item = heldItems[i];
            if (string.IsNullOrEmpty(item)) continue;
            string current = item.Trim().ToLower();

            if (current == target || current.Contains(target) || target.Contains(current))
            {
                return true;
            }
        }
        return false;
    }

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
                heldItems[i] = "";
                if (heldItemSprites != null && i < heldItemSprites.Length) heldItemSprites[i] = null;
                if (heldItemObjects != null && i < heldItemObjects.Length && heldItemObjects[i] != null)
                {
                    Destroy(heldItemObjects[i]);
                    heldItemObjects[i] = null;
                }
                if (heldItemTypes != null && i < heldItemTypes.Length)
                {
                    heldItemTypes[i] = InteractableItem.ItemType.Consumable;
                }
                Destroy3DPreview(i);

                ConsolidateSlots();
                SaveInventoryData();
                break;
            }
        }
    }

    public void ConsolidateSlots()
    {
        if (heldItems == null) return;
        int slotCount = heldItems.Length;

        List<string> itemNames = new List<string>();
        List<GameObject> itemObjs = new List<GameObject>();
        List<Sprite> itemSprites = new List<Sprite>();
        List<InteractableItem.ItemType> itemTypes = new List<InteractableItem.ItemType>();

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

        for (int i = 0; i < slotCount; i++)
        {
            Destroy3DPreview(i);
            heldItems[i] = "";
            if (heldItemObjects != null && i < heldItemObjects.Length) heldItemObjects[i] = null;
            if (heldItemSprites != null && i < heldItemSprites.Length) heldItemSprites[i] = null;
            if (heldItemTypes != null && i < heldItemTypes.Length) heldItemTypes[i] = InteractableItem.ItemType.Consumable;
        }

        for (int i = 0; i < itemNames.Count; i++)
        {
            heldItems[i] = itemNames[i];
            if (heldItemObjects != null && i < heldItemObjects.Length) heldItemObjects[i] = itemObjs[i];
            if (heldItemSprites != null && i < heldItemSprites.Length) heldItemSprites[i] = itemSprites[i];
            if (heldItemTypes != null && i < heldItemTypes.Length) heldItemTypes[i] = itemTypes[i];
            Create3DPreviewForSlot(i, itemNames[i], itemObjs[i]);
        }

        if (selectedIndex >= itemNames.Count)
        {
            selectedIndex = (itemNames.Count > 0) ? itemNames.Count - 1 : -1;
        }

        UpdateUISlots();
    }
}
