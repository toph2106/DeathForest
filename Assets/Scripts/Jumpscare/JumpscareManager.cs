using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class JumpscareManager : MonoBehaviour
{
    [Tooltip("Thêm các sự kiện (rớt nhện, tắt đèn...) vào danh sách này")]
    public List<UnityEvent> randomJumpscares;

    public float baseMinTime = 10f;
    public float baseMaxTime = 30f;

    [Tooltip("Độ sợ hãi của người chơi (0 = bình tĩnh, 100 = hoảng loạn)")]
    [Range(0f, 100f)] public float playerFearLevel = 0f;

    public KeyCode testKey = KeyCode.T;

    private float timer;
    private int lastEventIndex = -1; // Nhớ sự kiện vừa ra để không bị lặp lại

    void Start()
    {
        ResetTimer();
    }

    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            if (!IsAnyJumpscareActive()) TriggerRandomEvent();
            return;
        }

        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            // Nếu trên màn hình vẫn còn quái vật thì tạm ngưng, đợi nó biến mất mới gọi sự kiện mới
            if (IsAnyJumpscareActive())
            {
                return; 
            }
            
            TriggerRandomEvent();
        }
    }

    // Hàm kiểm tra xem có con nhện hay Manthing nào đang tồn tại không
    private bool IsAnyJumpscareActive()
    {
        bool hasManthing = FindObjectOfType<ManthingBehavior>() != null;
        bool hasSpider = FindObjectOfType<SpiderBehavior>() != null;
        
        return hasManthing || hasSpider;
    }

    private void TriggerRandomEvent()
    {
        if (randomJumpscares.Count > 0)
        {
            int randomIndex = Random.Range(0, randomJumpscares.Count);
            
            // Nếu lỡ bốc trúng sự kiện vừa nãy (và danh sách có nhiều hơn 1 sự kiện) -> bốc lại
            while (randomIndex == lastEventIndex && randomJumpscares.Count > 1)
            {
                randomIndex = Random.Range(0, randomJumpscares.Count);
            }

            lastEventIndex = randomIndex;
            randomJumpscares[randomIndex]?.Invoke();
        }

        ResetTimer();
    }

    private void ResetTimer()
    {
        float randomBaseTime = Random.Range(baseMinTime, baseMaxTime);
        float fearMultiplier = 1f - (playerFearLevel / 200f);
        timer = randomBaseTime * fearMultiplier;
    }
}