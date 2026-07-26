using UnityEngine;

public class ManthingSpawner : MonoBehaviour
{
    [Header("Prefab & Player")]
    public GameObject manthingPrefab;
    public Transform playerTransform;

    [Header("Spawn Settings")]
    [Tooltip("Khoảng cách xuất hiện phía trước người chơi (mét)")]
    public float spawnDistance = 40f; 

    public void SpawnManthing()
    {
        if (playerTransform == null || manthingPrefab == null) return;

        // Sinh ra từ rất xa phía trước Camera
        Vector3 spawnPos = playerTransform.position + playerTransform.forward * spawnDistance;
        
        // Bắn tia dò mặt đất để đặt Manthing nằm đúng trên đất
        if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
        {
            spawnPos.y = hit.point.y;
        }
        else
        {
            spawnPos.y = playerTransform.position.y;
        }

        Instantiate(manthingPrefab, spawnPos, Quaternion.identity);
    }
}
