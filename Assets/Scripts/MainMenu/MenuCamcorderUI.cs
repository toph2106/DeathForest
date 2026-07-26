using UnityEngine;
using TMPro;
using System;

public class MenuCamcorderUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text hiển thị Giờ (VD: AM 00:00)")]
    public TMP_Text recTimeText; 
    
    [Tooltip("Text hiển thị Ngày tháng (VD: Jan. 01 2006)")]
    public TMP_Text clockText;

    void Update()
    {
        // Lấy thời gian thực
        DateTime now = DateTime.Now;

        // 1. Dòng trên (Giờ: AM/PM hh:mm)
        if (recTimeText != null)
        {
            recTimeText.text = now.ToString("tt hh:mm").ToUpper();
        }

        // 2. Dòng dưới (Ngày: Jan. 01 2006)
        if (clockText != null)
        {
            // Lấy 3 chữ cái đầu của tháng (vd: Jan), thêm dấu chấm, rồi Ngày và Năm
            string month = now.ToString("MMM"); 
            string dayYear = now.ToString("dd yyyy");
            
            // Ép viết hoa chữ cái đầu cho đúng chuẩn "Jan."
            if (month.Length > 0)
            {
                month = char.ToUpper(month[0]) + month.Substring(1).ToLower();
            }

            clockText.text = $"{month}. {dayYear}";
        }
    }
}
