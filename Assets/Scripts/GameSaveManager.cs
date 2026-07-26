using UnityEngine;

public static class GameSaveManager
{
    private const string UNLOCKED_LEVEL_KEY = "UnlockedLevel";

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
            Debug.Log($"[GameSaveManager] Tuyệt vời! Đã mở khóa Màn chơi mới: Map 0{levelIndex}");
        }
    }

    /// <summary>
    /// Lấy cấp độ màn chơi cao nhất đã được mở khóa (Mặc định là 1)
    /// </summary>
    public static int GetUnlockedLevel()
    {
        return PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);
    }

    /// <summary>
    /// Xóa tiến trình chơi để test lại từ đầu
    /// </summary>
    public static void ResetProgress()
    {
        PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[GameSaveManager] Đã đặt lại tiến trình về Map 01!");
    }
}
