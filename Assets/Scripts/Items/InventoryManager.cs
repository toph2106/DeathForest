using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    // MẢNG STATIC LƯU GIỮ CÁC VẬT PHẨM VÀ SỐ LƯỢNG PIN CỘNG DỒN NẠP QUA SCENE
    private static string[] savedHeldItems = null;
    private static int savedBatteryStackCount = 0;

    public static InventoryManager Instance;

    [Header("UI Hotbar (Các ô Tiêu Hoa)")]
    public GameObject inventoryPanel;
    public Transform[] slotTransforms; // Mảng động các ô Slot trên UI

    [Header("Cấu Hình Icon Pin (Kéo Sprite hình Pin vào đây nếu có)")]
    [Tooltip("Kéo hình Sprite Cục Pin vào đây để tự hiển thị đẹp mắt khi chuyển Map")]
    public Sprite batteryIconSprite;

    [Header("Cấu Hình Phím Bấm")]
    [Tooltip("Phím dùng để SỬ DỤNG vật phẩm đang chọn (Mặc định: Phím R)")]
    public KeyCode useKey = KeyCode.R;

    [Header("UI Bộ Đếm Số Lượng Pin (Kéo BatteryText vào đây)")]
    [Tooltip("Kéo TextMeshProUGUI hiển thị số Pin (VD: BatteryText) vào đây")]
    public TextMeshProUGUI batteryCountText;

    [Header("UI Loại 2 - Vật phẩm Quest")]
    public GameObject questProgressTextObject;
    public TextMeshProUGUI questProgressText;
    public int totalQuestItemsNeeded = 3;

    public string[] heldItems;
    private GameObject[] heldItemObjects;
    private Image[] slotIconImages; // Các hình Icon con bên trong ô Slot UI

    private int selectedIndex = -1;
    private int currentQuestItemCount = 0;

    private Vector3 normalScale = Vector3.one;
    private Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f);

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        if (questProgressTextObject != null) questProgressTextObject.SetActive(false);

        int slotCount = (slotTransforms != null && slotTransforms.Length > 0) ? slotTransforms.Length : 5;
        heldItems = new string[slotCount];
        heldItemObjects = new GameObject[slotCount];
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

            if (slotTransforms[i] != null)
            {
                slotTransforms[i].localScale = normalScale;

                Image[] childImgs = slotTransforms[i].GetComponentsInChildren<Image>(true);
                foreach (Image img in childImgs)
                {
                    if (img.transform != slotTransforms[i])
                    {
                        slotIconImages[i] = img;
                        break;
                    }
                }
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

    void OnDisable()
    {
        // LƯU DỮ LIỆU TRƯỚC KHI CHUYỂN MAP
        if (heldItems != null)
        {
            savedHeldItems = (string[])heldItems.Clone();
        }
    }

    void Update()
    {
        if (PauseMenuManager.isPaused) return;

        HandleCheatInput(); // Phím B nhặt pin cheat demo, F9 nạp đầy pin
        HandleSelectionInput();
        HandleUseInput(); // Phím R dùng pin
    }

    private void HandleCheatInput()
    {
        // Bấm phím B -> Nhận ngay 1 Cục Pin (Tự cộng dồn số lượng)
        if (Input.GetKeyDown(KeyCode.B))
        {
            bool success = AddConsumableItem("Pin", null);
            if (success)
            {
                Debug.Log($"⚡ [DEMO CHEAT] Đã thêm 1 Cục Pin! Tổng số Pin dồn: {savedBatteryStackCount}");
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
    /// Hàm nhặt item: Riêng PIN sẽ tự động CỘNG DỒN vào 1 ô duy nhất trên Hotbar!
    /// </summary>
    public bool AddConsumableItem(string itemName, GameObject itemObj)
    {
        if (heldItems == null) return false;
        int slotCount = heldItems.Length;
        bool isBattery = itemName.ToLower().Contains("pin") || itemName.ToLower().Contains("battery") || itemName.ToLower().Contains("thu thap");

        // 1. NẾU LÀ PIN -> TÌM XEM ĐÃ CÓ Ô CHỨA PIN CHƯA ĐỂ CỘNG DỒN SỐ LƯỢNG VÀO Ô ĐÓ
        if (isBattery)
        {
            int existingPinSlot = -1;
            for (int i = 0; i < slotCount; i++)
            {
                if (!string.IsNullOrEmpty(heldItems[i]) && (heldItems[i].ToLower().Contains("pin") || heldItems[i].ToLower().Contains("battery") || heldItems[i].ToLower().Contains("thu thap")))
                {
                    existingPinSlot = i;
                    break;
                }
            }

            // Đã có ô chứa Pin -> Tăng số lượng dồn lên +1
            if (existingPinSlot != -1)
            {
                savedBatteryStackCount++;
                if (itemObj != null) itemObj.SetActive(false);

                UpdateUISlots();
                Debug.Log($"[Inventory] Đã cộng dồn Pin vào ô {existingPinSlot + 1}. Tổng số pin: {savedBatteryStackCount}");
                return true;
            }
        }

        // 2. NẾU CHƯA CÓ Ô PIN HOẶC LÀ VẬT PHẨM KHÁC -> CHỜ TÌM Ô TRỐNG
        for (int i = 0; i < slotCount; i++)
        {
            if (string.IsNullOrEmpty(heldItems[i]))
            {
                heldItems[i] = itemName;
                heldItemObjects[i] = itemObj;

                if (isBattery)
                {
                    savedBatteryStackCount = 1; // Khởi tạo 1 cục pin
                }

                if (itemObj != null) itemObj.SetActive(false);

                if (selectedIndex == -1) ToggleSelect(i);
                else UpdateUISlots();

                Debug.Log($"[Inventory] Đã nhặt '{itemName}' vào ô {i + 1}");
                return true;
            }
        }

        Debug.Log("Túi đồ đã đầy!");
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
                        Debug.Log("[Inventory] Đã sử dụng 1 Cục Pin! Nạp +50% Pin Đèn Pin.");
                    }

                    // GIẢM SỐ LƯỢNG PIN CỘNG DỒN ĐI 1 CỤC
                    savedBatteryStackCount--;

                    // NẾU HẾT PIN TRONG TÚI (VỀ 0) -> DỌN RÁC Ô VÀ BỎ FOCUS
                    if (savedBatteryStackCount <= 0)
                    {
                        savedBatteryStackCount = 0;

                        if (selectedIndex < heldItemObjects.Length && heldItemObjects[selectedIndex] != null)
                        {
                            Destroy(heldItemObjects[selectedIndex]);
                        }

                        heldItems[selectedIndex] = "";
                        heldItemObjects[selectedIndex] = null;
                        selectedIndex = -1;
                    }

                    UpdateUISlots();
                }
                else
                {
                    Debug.Log($"[Inventory] Chưa có logic dùng cho vật phẩm '{itemName}'");
                }
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
        if (Input.GetKeyDown(KeyCode.Alpha4) && count > 3) ToggleSelect(4);
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

        // KIỂM TRA SỐ PIN VỀ 0 -> TỰ DỌN SẠCH CHỮ "Pin" KHI NẠP SCENE MỚI
        if (savedBatteryStackCount <= 0)
        {
            savedBatteryStackCount = 0;
            for (int i = 0; i < itemCount; i++)
            {
                if (!string.IsNullOrEmpty(heldItems[i]) && (heldItems[i].ToLower().Contains("pin") || heldItems[i].ToLower().Contains("battery") || heldItems[i].ToLower().Contains("thu thap")))
                {
                    heldItems[i] = "";
                }
            }
        }

        // TỰ ĐỘNG BẬT/TẮT NỀN KHUNG TÚI ĐỒ HOTBAR: CÓ ĐỒ MỚI HIỆN, KHÔNG CÓ ĐỒ TỰ ẨN SẠCH 5 Ô XÁM!
        bool hasAnyItem = false;
        if (heldItems != null)
        {
            foreach (string item in heldItems)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    hasAnyItem = true;
                    break;
                }
            }
        }

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(hasAnyItem);
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
            if (slotIconImages != null && i < slotIconImages.Length && slotIconImages[i] != null && slotIconImages[i].transform != slotTransforms[i])
            {
                bool hasItem = (i < itemCount) && !string.IsNullOrEmpty(heldItems[i]);
                slotIconImages[i].gameObject.SetActive(hasItem);

                // Gán Sprite Icon Cục Pin nếu có cài trên Inspector
                if (hasItem && (heldItems[i].ToLower().Contains("pin") || heldItems[i].ToLower().Contains("battery")))
                {
                    if (batteryIconSprite != null)
                    {
                        slotIconImages[i].sprite = batteryIconSprite;
                        slotIconImages[i].color = Color.white;
                    }
                }
            }
        }

        // TỰ ĐỘNG TÌM TEXT BỘ ĐẾM NẾU SANG MAP MỚI MÀ CHƯA KÉO INSPECTOR
        if (batteryCountText == null)
        {
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in allTexts)
            {
                if (txt.gameObject.name.ToLower().Contains("batterytext") || txt.gameObject.name.ToLower().Contains("pin"))
                {
                    batteryCountText = txt;
                    break;
                }
            }
        }

        // 3. HIỂN THỊ SỐ PIN CỘNG DỒN (KHI SỐ PIN > 0 MỚI HIỆN, VỀ 0 TỰ ẨN SẠCH)
        if (batteryCountText != null)
        {
            if (savedBatteryStackCount > 0)
            {
                batteryCountText.gameObject.SetActive(true);
                batteryCountText.text = savedBatteryStackCount.ToString();
            }
            else
            {
                batteryCountText.gameObject.SetActive(false); // VỀ 0 LÀ ẨN SẠCH 100%!
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
        savedBatteryStackCount = 0;
    }
    // Hàm kiểm tra xem trong túi có món đồ này chưa
    public bool HasItem(string itemName)
    {
        foreach (string item in heldItems)
        {
            if (item == itemName) return true;
        }
        return false;
    }

    // Hàm xóa món đồ sau khi dùng
    public void RemoveItem(string itemName)
    {
        for (int i = 0; i < heldItems.Length; i++)
        {
            if (heldItems[i] == itemName)
            {
                heldItems[i] = ""; // Xóa tên item
                if (heldItemObjects[i] != null) Destroy(heldItemObjects[i]); // Hủy object
                heldItemObjects[i] = null;
                UpdateUISlots(); // Cập nhật lại UI
                break;
            }
        }
    }
}
