using UnityEngine;

public class CameraObjectPickup : MonoBehaviour
{
    public GameObject pressFHintUI; // Kéo Canvas chữ "Press F" dạng World space vào đây

    private SimpleCameraOverlay overlayManager;

    void Start()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(false);

        // Tự động tìm bộ quản lý trên Main Camera
        overlayManager = Camera.main.GetComponent<SimpleCameraOverlay>();
    }

    public void ShowPrompt()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(true);
    }

    public void HidePrompt()
    {
        if (pressFHintUI != null) pressFHintUI.SetActive(false);
    }

    public void Pickup()
    {
        if (overlayManager != null)
        {
            overlayManager.TurnOnCameraView(); // Kích hoạt hiệu ứng kính ngắm
        }

        Destroy(gameObject); // Xóa máy quay dưới đất đi vì đã nhặt lên rồi
    }
}