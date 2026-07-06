using UnityEngine;

public class SimpleCameraOverlay : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject cameraOverlayCanvas; // Kéo Canvas kính ngắm vào đây

    private bool hasCamera = false;

    void Start()
    {
        // Ban đầu game chưa có máy quay thì ẩn đi
        if (cameraOverlayCanvas != null) cameraOverlayCanvas.SetActive(false);
    }

    // Hàm này sẽ được gọi khi người chơi bấm F nhặt máy quay
    public void TurnOnCameraView()
    {
        hasCamera = true;
        if (cameraOverlayCanvas != null)
        {
            cameraOverlayCanvas.SetActive(true); // Bật hiệu ứng kính ngắm lên mãi mãi
        }
    }
}