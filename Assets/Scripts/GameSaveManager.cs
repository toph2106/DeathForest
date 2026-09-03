using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Dữ liệu toàn diện của người chơi được lưu trữ theo chuẩn JSON
/// </summary>
[System.Serializable]
public class GameSaveData
{
    public string currentScene = "Map01";
    public List<string> inventoryItems = new List<string>();
    public List<int> inventoryTypes = new List<int>();
    public bool hasBackpack = false;
    public float flashlightBattery = 100f;
    public bool hasFlashlight = true;
    public bool isFlashlightOn = true;
    public bool hasCamcorder = true;
    public int unlockedLevel = 1;
    public int currentLevel = 1;
    public string saveTimestamp = "";
}

/// <summary>
/// Quản lý Lưu Trữ Tiến Trình Game Bằng File JSON & Reset Dữ Liệu Toàn Diện:
/// 1. Lưu trữ trạng thái Túi đồ (Inventory), % Pin Đèn Pin, Máy Quay vào file JSON: save_data.json
/// 2. Lưu cấp độ mở khóa màn chơi (Map01 -> Map02 -> Map03 -> Map04 -> Map05)
/// 3. Reset 100% sạch sẽ dữ liệu khi bắt đầu Chơi Mới (New Game)
/// 4. Hỗ trợ Menu Editor trên thanh công cụ Unity để xóa Save tiện lợi khi test
/// </summary>
public static class GameSaveManager
{
    private const string UNLOCKED_LEVEL_KEY = "UnlockedLevel";
    private const string CURRENT_LEVEL_KEY = "CurrentLevel";

    // Cờ báo hiệu đang thực hiện Reset Game / New Game, ngăn chặn các hàm OnDisable vô tình lưu đè dữ liệu cũ
    public static bool isResettingData = false;

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, "SaveData", "game_save.json");
    public static string Map02CheckpointPath => Path.Combine(Application.persistentDataPath, "SaveData", "map02_checkpoint.json");

    /// <summary>
    /// Kiểm tra xem đã có file lưu trữ JSON chưa
    /// </summary>
    public static bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }

    /// <summary>
    /// Kiểm tra xem đã có file checkpoint Map 02 chưa
    /// </summary>
    public static bool HasMap02Checkpoint()
    {
        return File.Exists(Map02CheckpointPath);
    }

    /// <summary>
    /// Lưu toàn bộ dữ liệu game hiện tại ra file JSON
    /// </summary>
    public static void SaveGame()
    {
        if (isResettingData)
        {
            Debug.Log("[GameSaveManager] ⚠️ Bỏ qua SaveGame() vì đang trong trạng thái Reset Game / New Game!");
            return;
        }

        try
        {
            string dir = Path.GetDirectoryName(SaveFilePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            GameSaveData data = new GameSaveData();
            string scene = SceneManager.GetActiveScene().name;
            int lvl = GetCurrentLevel();
            if (scene == "Map02" && lvl >= 3)
            {
                scene = "Map03";
            }
            data.currentScene = scene;
            data.unlockedLevel = GetUnlockedLevel();
            data.currentLevel = lvl;
            data.saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 1. Lưu Túi đồ (Inventory)
            data.hasBackpack = InventoryManager.hasUnlockedBackpack;
            string[] items = InventoryManager.SavedHeldItems;
            if (InventoryManager.Instance != null && InventoryManager.Instance.heldItems != null)
            {
                items = InventoryManager.Instance.heldItems;
            }

            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    data.inventoryItems.Add(items[i] ?? "");
                }
            }

            var types = InventoryManager.SavedHeldItemTypes;
            if (InventoryManager.Instance != null && InventoryManager.Instance.heldItemTypes != null)
            {
                types = InventoryManager.Instance.heldItemTypes;
            }
            if (types != null)
            {
                for (int i = 0; i < types.Length; i++)
                {
                    data.inventoryTypes.Add((int)types[i]);
                }
            }

            // 2. Lưu Đèn Pin & % Pin
            if (FlashlightToggle.Instance != null)
            {
                data.flashlightBattery = FlashlightToggle.Instance.currentBattery;
                data.hasFlashlight = FlashlightToggle.Instance.hasFlashlight;
                data.isFlashlightOn = FlashlightToggle.Instance.IsOn();
            }
            else
            {
                data.flashlightBattery = FlashlightToggle.SavedBattery >= 0f ? FlashlightToggle.SavedBattery : 100f;
                data.hasFlashlight = FlashlightToggle.SavedHasFlashlight != 0;
            }

            // 3. Lưu Máy Quay
            data.hasCamcorder = CamcorderUI.HasPickedUpCamera;

            // Chuyển đối tượng thành chuỗi JSON định dạng đẹp
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json, System.Text.Encoding.UTF8);

            Debug.Log($"[GameSaveManager] 💾 ĐÃ LƯU DỮ LIỆU JSON THÀNH CÔNG!\nĐường dẫn: {SaveFilePath}\nSố món đồ: {data.inventoryItems.Count}, % Pin: {data.flashlightBattery:F1}%");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameSaveManager] ❌ Lỗi khi ghi file JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Đọc dữ liệu từ file JSON nếu có
    /// </summary>
    public static GameSaveData LoadGame()
    {
        if (!HasSaveFile()) return null;

        try
        {
            string json = File.ReadAllText(SaveFilePath, System.Text.Encoding.UTF8);
            if (string.IsNullOrEmpty(json)) return null;

            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log($"[GameSaveManager] 📂 ĐÃ NẠP DỮ LIỆU TỪ JSON ({data.saveTimestamp})! Scene: {data.currentScene}, Đồ: {data.inventoryItems.Count}, % Pin: {data.flashlightBattery:F1}%");
            return data;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameSaveManager] ❌ Lỗi khi đọc file JSON: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Lưu checkpoint riêng của Map 02 khi chạm vào cửa chuyển sang Map 03.
    /// Tự động ghi đè mỗi lần người chơi hoàn thành lại Map 02.
    /// </summary>
    public static void SaveMap02Checkpoint()
    {
        try
        {
            string dir = Path.GetDirectoryName(Map02CheckpointPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            GameSaveData data = new GameSaveData();
            data.currentScene = "Map03";
            data.unlockedLevel = Mathf.Max(GetUnlockedLevel(), 3);
            data.currentLevel = 3;
            data.saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 1. Túi đồ khi kết thúc Map 02
            data.hasBackpack = InventoryManager.hasUnlockedBackpack;
            string[] items = InventoryManager.SavedHeldItems;
            if (InventoryManager.Instance != null && InventoryManager.Instance.heldItems != null)
            {
                items = InventoryManager.Instance.heldItems;
            }

            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    data.inventoryItems.Add(items[i] ?? "");
                }
            }

            var types = InventoryManager.SavedHeldItemTypes;
            if (InventoryManager.Instance != null && InventoryManager.Instance.heldItemTypes != null)
            {
                types = InventoryManager.Instance.heldItemTypes;
            }
            if (types != null)
            {
                for (int i = 0; i < types.Length; i++)
                {
                    data.inventoryTypes.Add((int)types[i]);
                }
            }

            // 2. % Pin đèn pin thực tế khi chạm cửa
            if (FlashlightToggle.Instance != null)
            {
                data.flashlightBattery = FlashlightToggle.Instance.currentBattery;
                data.hasFlashlight = true;
                data.isFlashlightOn = FlashlightToggle.Instance.IsOn();
            }
            else
            {
                data.flashlightBattery = FlashlightToggle.SavedBattery >= 0f ? FlashlightToggle.SavedBattery : 100f;
                data.hasFlashlight = true;
            }

            // 3. Máy quay mặc định đã có từ Map 02
            data.hasCamcorder = true;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(Map02CheckpointPath, json, System.Text.Encoding.UTF8);

            // Đồng thời cập nhật luôn vào save chính game_save.json cho phiên chơi hiện tại
            File.WriteAllText(SaveFilePath, json, System.Text.Encoding.UTF8);

            // Đồng bộ trực tiếp vào bộ nhớ RAM static
            InventoryManager.SavedHeldItems = data.inventoryItems.ToArray();
            List<InteractableItem.ItemType> typeList = new List<InteractableItem.ItemType>();
            foreach (int t in data.inventoryTypes) typeList.Add((InteractableItem.ItemType)t);
            InventoryManager.SavedHeldItemTypes = typeList.ToArray();
            InventoryManager.hasUnlockedBackpack = data.hasBackpack;

            FlashlightToggle.SavedBattery = data.flashlightBattery;
            FlashlightToggle.SavedHasFlashlight = 1;
            CamcorderUI.MarkCameraPickedUp();

            // Đồng bộ vào PlayerPrefs làm lớp dự phòng bền vững
            PlayerPrefs.SetFloat("Global_Flashlight_Battery", data.flashlightBattery);
            PlayerPrefs.SetInt("Global_Has_Flashlight", 1);
            PlayerPrefs.SetInt("Global_Has_Camera", 1);
            PlayerPrefs.SetInt("Global_Has_Backpack", data.hasBackpack ? 1 : 0);
            if (data.inventoryItems != null && data.inventoryItems.Count > 0)
            {
                PlayerPrefs.SetString("Global_Inventory_Items", string.Join("|;;|", data.inventoryItems));
            }
            if (data.inventoryTypes != null && data.inventoryTypes.Count > 0)
            {
                PlayerPrefs.SetString("Global_Inventory_Types", string.Join(",", data.inventoryTypes));
            }
            PlayerPrefs.Save();

            Debug.Log($"[GameSaveManager] 🏁 ĐÃ LƯU CHECKPOINT MAP 02 THÀNH CÔNG!\nĐường dẫn: {Map02CheckpointPath}\nSố món đồ: {data.inventoryItems.Count}, % Pin: {data.flashlightBattery:F1}%");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameSaveManager] ❌ Lỗi khi ghi Checkpoint Map 02: {ex.Message}");
        }
    }

    /// <summary>
    /// Đọc dữ liệu từ file Checkpoint Map 02
    /// </summary>
    public static GameSaveData LoadMap02Checkpoint()
    {
        if (!HasMap02Checkpoint()) return null;

        try
        {
            string json = File.ReadAllText(Map02CheckpointPath, System.Text.Encoding.UTF8);
            if (string.IsNullOrEmpty(json)) return null;

            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log($"[GameSaveManager] 📂 ĐÃ NẠP CHECKPOINT MAP 02 ({data.saveTimestamp})! Đồ: {data.inventoryItems.Count}, % Pin: {data.flashlightBattery:F1}%");
            return data;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameSaveManager] ❌ Lỗi khi đọc Checkpoint Map 02: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Nạp dữ liệu checkpoint Map 02 vào save hiện tại và bộ nhớ RAM để bắt đầu Cảnh 03
    /// </summary>
    public static bool ApplyMap02CheckpointToCurrentSave()
    {
        if (!HasMap02Checkpoint()) return false;

        GameSaveData data = LoadMap02Checkpoint();
        if (data == null) return false;

        try
        {
            string dir = Path.GetDirectoryName(SaveFilePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            data.currentScene = "Map03";
            data.currentLevel = 3;
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json, System.Text.Encoding.UTF8);

            // Đồng bộ bộ nhớ RAM
            InventoryManager.SavedHeldItems = data.inventoryItems.ToArray();
            List<InteractableItem.ItemType> typeList = new List<InteractableItem.ItemType>();
            foreach (int t in data.inventoryTypes) typeList.Add((InteractableItem.ItemType)t);
            InventoryManager.SavedHeldItemTypes = typeList.ToArray();
            InventoryManager.hasUnlockedBackpack = data.hasBackpack;

            FlashlightToggle.SavedBattery = data.flashlightBattery;
            FlashlightToggle.SavedHasFlashlight = 1;
            CamcorderUI.MarkCameraPickedUp();

            // Đồng bộ PlayerPrefs
            PlayerPrefs.SetFloat("Global_Flashlight_Battery", data.flashlightBattery);
            PlayerPrefs.SetInt("Global_Has_Flashlight", 1);
            PlayerPrefs.SetInt("Global_Has_Camera", 1);
            PlayerPrefs.SetInt("Global_Has_Backpack", data.hasBackpack ? 1 : 0);
            if (data.inventoryItems != null && data.inventoryItems.Count > 0)
            {
                PlayerPrefs.SetString("Global_Inventory_Items", string.Join("|;;|", data.inventoryItems));
            }
            if (data.inventoryTypes != null && data.inventoryTypes.Count > 0)
            {
                PlayerPrefs.SetString("Global_Inventory_Types", string.Join(",", data.inventoryTypes));
            }
            PlayerPrefs.Save();

            Debug.Log($"[GameSaveManager] 🔄 Đã áp dụng Checkpoint Map 02 cho Cảnh 03: {data.inventoryItems.Count} món, {data.flashlightBattery:F1}% pin.");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameSaveManager] ❌ Lỗi khi áp dụng Checkpoint Map 02: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Xóa file JSON lưu trữ
    /// </summary>
    public static void DeleteSaveFile()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log($"[GameSaveManager] 🗑️ Đã xóa file lưu JSON: {SaveFilePath}");
            }
            if (File.Exists(Map02CheckpointPath))
            {
                File.Delete(Map02CheckpointPath);
                Debug.Log($"[GameSaveManager] 🗑️ Đã xóa file checkpoint Map 02: {Map02CheckpointPath}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameSaveManager] ❌ Lỗi khi xóa file JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Gọi hàm này khi người chơi hoàn thành màn chơi để mở khóa màn tiếp theo.
    /// </summary>
    public static void UnlockLevel(int levelIndex)
    {
        int currentUnlocked = GetUnlockedLevel();
        if (levelIndex > currentUnlocked)
        {
            PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, levelIndex);
            PlayerPrefs.Save();
            Debug.Log($"[GameSaveManager] 🌟 MỞ KHÓA MÀN MỚI THÀNH CÔNG: Map 0{levelIndex}!");
        }
    }

    public static int GetUnlockedLevel()
    {
        return PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);
    }

    public static void SetCurrentLevel(int levelIndex)
    {
        PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, levelIndex);
        PlayerPrefs.Save();
    }

    public static int GetCurrentLevel()
    {
        return PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 1);
    }

    /// <summary>
    /// Xóa toàn bộ tiến trình chơi để chơi lại từ đầu
    /// </summary>
    public static void ResetProgress()
    {
        PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, 1);
        PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, 1);
        PlayerPrefs.DeleteKey("HasReadPaper_Map02");
        PlayerPrefs.Save();
        ResetAllGameplayRuntimeData();
        Debug.Log("[GameSaveManager] 🔄 Đã đặt lại toàn bộ tiến trình về Map 01!");
    }

    /// <summary>
    /// Bắt đầu Game Mới hoàn toàn: Xóa toàn bộ dữ liệu file JSON, Registry & Memory
    /// </summary>
    public static void NewGame()
    {
        ResetAllGameplayRuntimeData();
    }

    /// <summary>
    /// RESET TOÀN BỘ DỮ LIỆU RUNTIME TRONG GAME CHO GAME MỚI (NEW GAME):
    /// 1. Xóa sạch file JSON save_data.json
    /// 2. Dọn dẹp sạch sẽ túi đồ trong RAM và Registry
    /// 3. Reset Đèn pin về 100% pin, Reset Máy quay
    /// 4. Dọn dẹp các biến cắt cảnh & trigger
    /// </summary>
    public static void ResetAllGameplayRuntimeData()
    {
        isResettingData = true;
        try
        {
            // 1. Phục hồi thời gian Game & Pause
            Time.timeScale = 1f;
            PauseMenuManager.isPaused = false;

            // 2. Xóa file JSON lưu trữ
            DeleteSaveFile();

            // 3. Reset Túi đồ (Inventory)
            InventoryManager.ResetInventoryData();
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.heldItems = null;
            }

            // 4. Reset Đèn Pin & Máy Quay
            FlashlightToggle.ResetFlashlightData();
            CamcorderUI.ResetPickedUpCameraState();
            CamcorderUI.ResetTimer();

            // 5. Xóa triệt để các khóa lưu trong PlayerPrefs
            PlayerPrefs.DeleteKey("Global_Inventory_Items");
            PlayerPrefs.DeleteKey("Global_Inventory_Types");
            PlayerPrefs.DeleteKey("Global_Has_Backpack");
            PlayerPrefs.DeleteKey("Global_Flashlight_Battery");
            PlayerPrefs.DeleteKey("Global_Has_Flashlight");
            PlayerPrefs.DeleteKey("Global_Has_Camera");
            PlayerPrefs.DeleteKey("HasReadPaper_Map02");
            PlayerPrefs.Save();

            // 6. Reset Các Biến Cắt Cảnh & Sự Kiện Tĩnh (Static Cutscene Triggers)
            ResetTransientCutscenes();

            Debug.Log("[GameSaveManager] 🧹 ĐÃ DỌN DẸP & RESET SẠCH SẼ 100% DỮ LIỆU GAME (SẴN SÀNG CHO NEW GAME)!");
        }
        finally
        {
            isResettingData = false;
        }
    }

    /// <summary>
    /// Chỉ dọn dẹp các biến sự kiện và cắt cảnh tạm thời, KHÔNG xóa file save đồ đạc (Dùng cho Continue/Menu Boot)
    /// </summary>
    public static void ResetTransientCutscenes()
    {
        Time.timeScale = 1f;
        PauseMenuManager.isPaused = false;
        InWorldComputerCutscene.isUsingComputer = false;
        InWorldComputerCutscene.hasCompletedComputer = false;
        PCPowerButton.ResetPCPowerState();
        CameraObjectPickup.isComputerCutsceneFinished = false;
        Camera10sDoorEvent.hasCompletedDoorOpenDialogue = false;
        Map02CamcorderManager.hasTriggeredBlackout = false;
        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        ForbiddenDangerZone.isAnyDialogueActive = false;
        ForbiddenDangerZone.lastDialogueEndTime = -999f;
        TriggerEventTruck.ResetAllTriggers();
        CleanupLingeringCanvases();
    }

    private static void CleanupLingeringCanvases()
    {
        GameObject fadeCanvas = GameObject.Find("DeliveryFadeCanvas");
        if (fadeCanvas != null) Object.Destroy(fadeCanvas);

        GameObject fadeRunner = GameObject.Find("DeliveryFadeRunner");
        if (fadeRunner != null) Object.Destroy(fadeRunner);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("DeathForest/Save System/Clear Save Data (Reset Game) %&r", priority = 1)]
    public static void ClearSaveDataMenuItem()
    {
        ResetAllGameplayRuntimeData();
        UnityEditor.EditorUtility.DisplayDialog("DeathForest Save System", "✅ ĐÃ XÓA SẠCH DỮ LIỆU LƯU (File JSON, Registry & Memory)!\n\nLần bấm Play tiếp theo sẽ là GAME MỚI 100% (túi đồ trống, pin 100%).", "OK");
    }

    [UnityEditor.MenuItem("DeathForest/Save System/Open Save Folder", priority = 2)]
    public static void OpenSaveFolderMenuItem()
    {
        string dir = Path.GetDirectoryName(SaveFilePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        System.Diagnostics.Process.Start("explorer.exe", dir.Replace("/", "\\"));
    }
#endif
}
