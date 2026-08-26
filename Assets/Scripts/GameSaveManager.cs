using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý Lưu Trữ Tiến Trình Game & Reset Dữ Liệu Runtime Toàn Diện:
/// 1. Lưu cấp độ mở khóa màn chơi (Map01 -> Map02 -> Map03)
/// 2. Hỗ trợ hệ thống nút Continue & Chọn Map
/// 3. Reset toàn bộ dữ liệu tạm thời (Túi đồ, Đèn pin, Máy quay, Cắt cảnh, Màn hình đen...)
///    giúp mỗi lần chơi lại luôn sạch sẽ 100% như mới mở game!
/// </summary>
public static class GameSaveManager
{
    private const string UNLOCKED_LEVEL_KEY = "UnlockedLevel";
    private const string CURRENT_LEVEL_KEY = "CurrentLevel";

    /// <summary>
    /// Gọi hàm này khi người chơi hoàn thành màn chơi để mở khóa màn tiếp theo.
    /// Ví dụ: Hoàn thành Map 1 -> GameSaveManager.UnlockLevel(2);
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

    /// <summary>
    /// Lấy cấp độ màn chơi cao nhất đã được mở khóa (Mặc định: 1 - Map 01)
    /// </summary>
    public static int GetUnlockedLevel()
    {
        return PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);
    }

    /// <summary>
    /// Lưu màn chơi gần nhất mà người chơi đang chơi
    /// </summary>
    public static void SetCurrentLevel(int levelIndex)
    {
        PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, levelIndex);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Lấy màn chơi gần nhất mà người chơi đang chơi (Mặc định: 1)
    /// </summary>
    public static int GetCurrentLevel()
    {
        return PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 1);
    }

    /// <summary>
    /// Xóa toàn bộ tiến trình chơi để chơi lại từ đầu (Mở lại chỉ Map 01)
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
    /// RESET TOÀN BỘ DỮ LIỆU RUNTIME TRONG GAME:
    /// Dọn dẹp sạch sẽ túi đồ, đèn pin, máy quay, cắt cảnh máy tính, trạng thái cửa sổ,
    /// đếm giờ, UI, và các Canvas DontDestroyOnLoad còn sót lại.
    /// </summary>
    public static void ResetAllGameplayRuntimeData()
    {
        // 1. Phục hồi thời gian Game & Pause
        Time.timeScale = 1f;
        PauseMenuManager.isPaused = false;

        // 2. Reset Túi đồ (Inventory)
        InventoryManager.ResetInventoryData();
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.heldItems = null;
        }

        // 3. Reset Đèn Pin & Máy Quay
        FlashlightToggle.ResetFlashlightData();
        CamcorderUI.ResetPickedUpCameraState();
        CamcorderUI.ResetTimer();

        // 4. Reset Các Biến Cắt Cảnh & Sự Kiện Tĩnh (Static Cutscene Triggers)
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
        PlayerPrefs.DeleteKey("HasReadPaper_Map02");

        // 5. Dọn dẹp các Canvas / Fade Runner rác nếu còn sót trong DontDestroyOnLoad
        CleanupLingeringCanvases();

        Debug.Log("[GameSaveManager] 🧹 ĐÃ DỌN DẸP & RESET SẠCH SẼ TOÀN BỘ RUNTIME DATA CỦA GAME!");
    }

    private static void CleanupLingeringCanvases()
    {
        GameObject fadeCanvas = GameObject.Find("DeliveryFadeCanvas");
        if (fadeCanvas != null) Object.Destroy(fadeCanvas);

        GameObject fadeRunner = GameObject.Find("DeliveryFadeRunner");
        if (fadeRunner != null) Object.Destroy(fadeRunner);
    }
}
