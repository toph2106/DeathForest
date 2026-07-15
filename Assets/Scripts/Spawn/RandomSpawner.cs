using UnityEngine;
using System.Collections.Generic;

public class RandomSpawner : MonoBehaviour
{
    [Header("Cài đặt Vật thể")]
    public GameObject corpsePrefab; // Kéo Prefab xác quái vật (Capsule) vào đây
    public int spawnCount = 3;      // Số lượng xác muốn spawn

    [Header("Danh sách Điểm Spawn")]
    public List<Transform> spawnPoints; // Kéo các điểm Empty Object vào đây

    void Start()
    {
        SpawnItemsRandomly();
    }

    void SpawnItemsRandomly()
    {
        if (corpsePrefab == null)
        {
            Debug.LogError("Lỗi: Chưa kéo Prefab xác quái vật vào ô Corpse Prefab!");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("Lỗi: Chưa có điểm Spawn nào trong danh sách!");
            return;
        }

        // Đảm bảo số lượng spawn không vượt quá tổng số điểm spawn (tránh lỗi vòng lặp)
        int actualSpawnCount = Mathf.Min(spawnCount, spawnPoints.Count);

        // Tạo một danh sách tạm thời copy từ danh sách gốc
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < actualSpawnCount; i++)
        {
            // Bốc thăm ngẫu nhiên 1 con số từ 0 đến số lượng điểm đang còn trống
            int randomIndex = Random.Range(0, availablePoints.Count);

            // Lấy ra điểm tương ứng với con số vừa bốc thăm
            Transform selectedPoint = availablePoints[randomIndex];

            // Thả xác quái vật xuống vị trí và góc xoay của điểm đó
            Instantiate(corpsePrefab, selectedPoint.position, selectedPoint.rotation);

            // XÓA điểm này khỏi danh sách tạm để vòng lặp sau không bốc trúng nó nữa (không bị đè)
            availablePoints.RemoveAt(randomIndex);
        }
    }
}