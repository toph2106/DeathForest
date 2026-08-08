using UnityEngine;

public class Map02IntroSequence : MonoBehaviour
{
    [Header("1. Vị Trí Spawn Người Chơi (Spawn Point)")]
    [Tooltip("Kéo Object SpawnPoint ban đầu trong Map 02 vào đây")]
    public Transform spawnPoint;

    [Header("2. Kích Hoạt Trigger Xe / Ending")]
    [Tooltip("Kéo Object TruckTrigger vào đây để kích hoạt sẵn sàng")]
    public GameObject truckTriggerObject;

    void Start()
    {
        MovePl playerMove = FindFirstObjectByType<MovePl>();
        if (playerMove != null)
        {
            CharacterController cc = playerMove.GetComponent<CharacterController>();
            if (spawnPoint != null)
            {
                if (cc != null) cc.enabled = false;
                playerMove.transform.position = spawnPoint.position;
                playerMove.transform.rotation = spawnPoint.rotation;
                if (cc != null) cc.enabled = true;
            }

            // TRẢ TOÀN BỘ QUYỀN DI CHUYỂN VÀ XOAY CHUỘT CHO NGƯỜI CHƠI NGAY KHI VÀO MAP 02
            playerMove.isCameraLocked = false;
            playerMove.enabled = true;
        }

        // KÍCH HOẠT TRUCK TRIGGER NGAY LẬP TỨC
        if (truckTriggerObject != null)
        {
            truckTriggerObject.SetActive(true);
        }

        // ĐÈN PIN GIỮ NGUYÊN TRẠNG THÁI SÁNG TỪ MAP 01 SANG, KHÔNG ÉP TẮT
        FlashlightToggle flashlight = FindFirstObjectByType<FlashlightToggle>();
        if (flashlight != null && flashlight.hasFlashlight && flashlight.currentBattery > 0f)
        {
            flashlight.SetFlashlightState(true, false);
        }

        // TẮT SCRIPT INTRO NÀY ĐỂ BỎ QUA HOÀN TOÀN ĐOẠN ĐI BỘ TỰ ĐỘNG
        gameObject.SetActive(false);
    }
}
