using UnityEngine;
using TMPro;

public class FontFixer : MonoBehaviour
{
    // Kéo Font bạn đang dùng cho hội thoại vào đây trong Inspector
    public TMP_FontAsset mainFontAsset;

    void Awake()
    {
        if (mainFontAsset != null)
        {
            // Ép Main Thread đọc và ghi nhận toàn bộ thông tin Font trước
            string forceReadName = mainFontAsset.name;
            mainFontAsset.HasCharacter(' '); // Kiểm tra ký tự trống để kích hoạt bộ nhớ cache

            // Đánh dấu font này không bao giờ bị dọn dẹp khỏi bộ nhớ khi chuyển cảnh
            DontDestroyOnLoad(mainFontAsset);

            Debug.Log($"[FontFixer] Đã xử lý an toàn cho Font: {forceReadName}");
        }
    }
}