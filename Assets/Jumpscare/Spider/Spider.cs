using UnityEngine;

public class Spider : MonoBehaviour
{
    [Header("Prefab & Player")]
    public GameObject spiderPrefab;
    public Transform playerTransform;

    [Header("Spawn Settings")]
    public int spiderAmount = 20;
    public float spawnDistanceMin = 15f;
    public float spawnDistanceMax = 30f;
    public float spawnSpread = 8f;

    [Header("Size Settings")]
    public float minSize = 0.5f;
    public float maxSize = 1.5f;

    [Header("Giant Spider (Đột biến)")]
    [Tooltip("Kích thước nhện khổng lồ")]
    public float giantScale = 4f;
    [Tooltip("Khoảng cách tụt lại phía sau để nhện chúa đi cuối cùng (mét)")]
    public float giantDelayDistance = 15f;

    public void SpawnSpider()
    {
        if (playerTransform == null || spiderPrefab == null) return;

        for (int i = 0; i < spiderAmount; i++)
        {
            // Cứ 20 con thì con cuối cùng (con số 20, 40, 60...) sẽ là khổng lồ
            bool isGiant = ((i + 1) % 20 == 0);

            float distance;
            float offsetX;

            if (isGiant)
            {
                // Nhện khổng lồ đi sau cùng, khoảng cách xa hơn và đi ở chính giữa
                distance = spawnDistanceMax + giantDelayDistance;
                offsetX = 0f;
            }
            else
            {
                // Nhện thường tản mác
                distance = Random.Range(spawnDistanceMin, spawnDistanceMax);
                offsetX = Random.Range(-spawnSpread, spawnSpread);
            }

            Vector3 spawnPos = playerTransform.position
                + playerTransform.forward * distance
                + playerTransform.right * offsetX;

            spawnPos.y = playerTransform.position.y;

            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f))
            {
                spawnPos.y = hit.point.y + 0.05f;
            }

            GameObject newSpider = Instantiate(spiderPrefab, spawnPos, Quaternion.identity);

            float scaleMultiplier = isGiant ? giantScale : Random.Range(minSize, maxSize);
            newSpider.transform.localScale = spiderPrefab.transform.localScale * scaleMultiplier;

            if (isGiant)
            {
                SpiderBehavior behavior = newSpider.GetComponent<SpiderBehavior>();
                if (behavior != null)
                {
                    behavior.forceDirectTarget = true; // Ép nhện khổng lồ cắm thẳng vào player
                }
            }
        }
    }
}