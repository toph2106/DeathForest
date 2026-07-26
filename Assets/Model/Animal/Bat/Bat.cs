using UnityEngine;

/// <summary>
/// Bat - Đàn dơi bay tự động trong vùng bay.
/// SETUP ĐƠN GIẢN:
/// 1. Tạo Empty GameObject, gắn script này vào
/// 2. Thêm BoxCollider lên chính GameObject đó (làm vùng bay)
/// 3. Kéo Bat Prefab vào ô "Bat Prefab"
/// 4. Kéo Player vào ô "Player"
/// 5. Xong! Dơi sẽ tự spawn và bay khi vào game.
/// </summary>
public class Bat : MonoBehaviour
{
    [Header("=== CÀI ĐẶT ===")]
    [Tooltip("Kéo Prefab con dơi vào đây")]
    public GameObject batPrefab;

    [Tooltip("Số lượng dơi")]
    public int spawnCount = 5;

    [Tooltip("Kéo Player vào đây")]
    public Transform player;

    [Header("=== CẤU HÌNH BAY ===")]
    public float flySpeed = 8f;

    [Tooltip("Biên độ vỗ cánh lên xuống")]
    public float flapAmplitude = 0.4f;
    [Tooltip("Tốc độ vỗ cánh")]
    public float flapSpeed = 12f;
    [Tooltip("Lắc lư ngang")]
    public float swayAmplitude = 0.3f;
    [Tooltip("Tốc độ lắc lư")]
    public float swaySpeed = 3f;
    [Tooltip("Tốc độ lượn cong (thấp = mượt hơn)")]
    public float turnSpeed = 3f;
    [Tooltip("Mỗi con khác nhau bao nhiêu % tốc độ")]
    [Range(0f, 0.5f)]
    public float speedVariation = 0.3f;

    [Header("=== CHỈNH HƯỚNG MODEL ===")]
    [Tooltip("Xoay model dơi cho đúng hướng bay. Nếu dơi bay ngang như cua → thử đổi Y = 90 hoặc -90")]
    public Vector3 modelRotationOffset = new Vector3(0f, 45f, 0f);

    [Header("=== NÉ PLAYER ===")]
    [Tooltip("Bật tính năng dơi bay né khi Player lại gần")]
    public bool enableAvoidPlayer = true;
    [Tooltip("Khoảng cách phát hiện Player để bắt đầu né (mét)")]
    public float avoidDistance = 6f;
    [Tooltip("Lực đẩy né (cao = bay né nhanh hơn)")]
    public float avoidForce = 12f;

    [Header("=== DƠI BAY QUA MẶT PLAYER ===")]
    [Tooltip("Bao nhiêu giây thì có 1 con bay qua mặt Player")]
    public float scareIntervalMin = 15f;
    public float scareIntervalMax = 40f;
    [Tooltip("Tốc độ bay lướt qua mặt")]
    public float scareSpeed = 18f;
    [Tooltip("Khoảng cách bay trước mặt Player (mét)")]
    public float scareFrontDistance = 3f;

    // Internal
    private BoxCollider flightZone;
    private BatData[] bats;
    private float scareTimer;
    private int scareBatIndex = -1;
    private Vector3 scareTarget;
    private bool scareReturning;

    private class BatData
    {
        public GameObject gameObject;
        public Transform transform;
        public Vector3 targetPos;
        public Vector3 velocity;
        public float speed;
        public float flapOffset;
        public float swayOffset;
        public float flapAmp;
        public float targetChangeTimer;
        public float nextTargetTime;
        public bool isScareing;
    }

    void Start()
    {
        flightZone = GetComponent<BoxCollider>();
        if (flightZone == null)
        {
            flightZone = gameObject.AddComponent<BoxCollider>();
            flightZone.size = new Vector3(20f, 10f, 20f);
            Debug.LogWarning("[Bat] Tự tạo BoxCollider làm vùng bay. Hãy chỉnh size trong Inspector.");
        }
        flightZone.isTrigger = true;

        SpawnBats();
        scareTimer = Random.Range(scareIntervalMin * 0.3f, scareIntervalMax * 0.5f);
    }

    void SpawnBats()
    {
        if (batPrefab == null)
        {
            Debug.LogError("[Bat] Chưa gắn Bat Prefab!");
            return;
        }

        bats = new BatData[spawnCount];

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 startPos = PickRandomPosition();
            GameObject go = Instantiate(batPrefab, startPos, Random.rotation);

            BatData bat = new BatData();
            bat.gameObject = go;
            bat.transform = go.transform;
            bat.targetPos = PickRandomPosition();
            bat.speed = flySpeed * Random.Range(1f - speedVariation, 1f + speedVariation);
            bat.flapOffset = Random.Range(0f, Mathf.PI * 2f);
            bat.swayOffset = Random.Range(0f, Mathf.PI * 2f);
            bat.flapAmp = flapAmplitude * Random.Range(0.7f, 1.3f);
            bat.velocity = (bat.targetPos - startPos).normalized * bat.speed;
            bat.nextTargetTime = Random.Range(1.5f, 4f);
            bat.targetChangeTimer = 0f;
            bat.isScareing = false;

            bats[i] = bat;
        }
    }

    void Update()
    {
        if (bats == null) return;

        float dt = Time.deltaTime;
        float time = Time.time;

        // Đếm thời gian cho scare event
        if (player != null && scareBatIndex < 0)
        {
            scareTimer -= dt;
            if (scareTimer <= 0f)
            {
                TriggerScare();
                scareTimer = Random.Range(scareIntervalMin, scareIntervalMax);
            }
        }

        // Cập nhật từng con dơi
        for (int i = 0; i < bats.Length; i++)
        {
            BatData bat = bats[i];
            if (bat == null || bat.gameObject == null) continue;

            if (bat.isScareing)
            {
                UpdateScareBat(bat, dt, time);
            }
            else
            {
                UpdateNormalBat(bat, dt, time);
            }
        }
    }

    void UpdateNormalBat(BatData bat, float dt, float time)
    {
        // Đổi đích theo thời gian
        bat.targetChangeTimer += dt;
        if (bat.targetChangeTimer >= bat.nextTargetTime)
        {
            bat.targetPos = PickRandomPosition();
            bat.nextTargetTime = Random.Range(1.5f, 4f);
            bat.targetChangeTimer = 0f;
        }

        // Gần đích → đổi sớm
        if (Vector3.Distance(bat.transform.position, bat.targetPos) < 2f)
        {
            bat.targetPos = PickRandomPosition();
            bat.targetChangeTimer = 0f;
        }

        // === Tính hướng bay mong muốn ===
        Vector3 desiredDir = (bat.targetPos - bat.transform.position).normalized;
        Vector3 desiredVelocity = desiredDir * bat.speed;

        // === NÉ PLAYER ===
        if (enableAvoidPlayer && player != null)
        {
            Vector3 toPlayer = bat.transform.position - player.position;
            float distToPlayer = toPlayer.magnitude;

            if (distToPlayer < avoidDistance)
            {
                // Càng gần → lực đẩy càng mạnh (tỷ lệ nghịch)
                float urgency = 1f - (distToPlayer / avoidDistance);
                urgency = urgency * urgency; // Mũ 2 cho phản ứng mạnh hơn khi rất gần

                Vector3 avoidDir = toPlayer.normalized;
                // Thêm chút lực lên trên để dơi bay vọt lên khi né
                avoidDir.y += 0.5f;
                avoidDir.Normalize();

                desiredVelocity += avoidDir * avoidForce * urgency;
            }
        }

        // Steering lượn cong
        bat.velocity = Vector3.Lerp(bat.velocity, desiredVelocity, dt * turnSpeed);

        // Giới hạn tốc độ (có thể tạm vượt khi né)
        float maxSpeed = bat.speed * 1.8f;
        if (bat.velocity.magnitude > maxSpeed)
        {
            bat.velocity = bat.velocity.normalized * maxSpeed;
        }
        // Đảm bảo tối thiểu
        if (bat.velocity.magnitude < bat.speed * 0.5f)
        {
            bat.velocity = bat.velocity.normalized * bat.speed;
        }

        // Di chuyển
        Vector3 movement = bat.velocity * dt;

        // Vỗ cánh
        float flapY = Mathf.Sin((time * flapSpeed) + bat.flapOffset) * bat.flapAmp;
        movement.y += flapY * dt * flapSpeed;

        // Lắc lư ngang
        Vector3 right = Vector3.Cross(Vector3.up, bat.velocity.normalized);
        if (right.sqrMagnitude > 0.001f)
        {
            float sway = Mathf.Sin((time * swaySpeed) + bat.swayOffset) * swayAmplitude;
            movement += right.normalized * sway * dt;
        }

        bat.transform.position += movement;
        bat.transform.position = ClampToFlightZone(bat.transform.position);

        ApplyRotation(bat, dt);
    }

    void TriggerScare()
    {
        if (bats == null || bats.Length == 0 || player == null) return;

        int idx = Random.Range(0, bats.Length);
        if (bats[idx] == null || bats[idx].gameObject == null) return;

        scareBatIndex = idx;
        bats[idx].isScareing = true;
        scareReturning = false;

        float sideOffset = Random.Range(-2f, 2f);
        float heightOffset = Random.Range(-0.5f, 1f);
        scareTarget = player.position
            + player.forward * scareFrontDistance
            + player.right * sideOffset
            + Vector3.up * heightOffset;
    }

    void UpdateScareBat(BatData bat, float dt, float time)
    {
        float currentSpeed = scareSpeed;
        Vector3 target;

        if (!scareReturning)
        {
            target = scareTarget;
            float dist = Vector3.Distance(bat.transform.position, target);

            if (dist < 2f)
            {
                scareReturning = true;
                bat.targetPos = PickRandomPosition();
            }
        }
        else
        {
            target = bat.targetPos;
            currentSpeed = scareSpeed * 0.8f;
            float dist = Vector3.Distance(bat.transform.position, target);

            if (dist < 3f)
            {
                bat.isScareing = false;
                scareBatIndex = -1;
                bat.velocity = (bat.targetPos - bat.transform.position).normalized * bat.speed;
                return;
            }
        }

        Vector3 desiredDir = (target - bat.transform.position).normalized;
        Vector3 desiredVelocity = desiredDir * currentSpeed;
        bat.velocity = Vector3.Lerp(bat.velocity, desiredVelocity, dt * turnSpeed * 1.5f);
        bat.velocity = bat.velocity.normalized * currentSpeed;

        Vector3 movement = bat.velocity * dt;
        float flapY = Mathf.Sin((time * flapSpeed * 1.5f) + bat.flapOffset) * bat.flapAmp * 0.5f;
        movement.y += flapY * dt * flapSpeed;

        bat.transform.position += movement;

        ApplyRotation(bat, dt);
    }

    /// <summary>
    /// Xoay dơi theo hướng bay + nghiêng cánh + áp dụng model offset.
    /// </summary>
    void ApplyRotation(BatData bat, float dt)
    {
        if (bat.velocity.sqrMagnitude < 0.01f) return;

        // Hướng bay chính
        Quaternion flyRotation = Quaternion.LookRotation(bat.velocity.normalized, Vector3.up);

        // Nghiêng cánh khi lượn (banking)
        Vector3 cross = Vector3.Cross(bat.transform.forward, bat.velocity.normalized);
        float bankAngle = cross.y * -30f;
        flyRotation *= Quaternion.Euler(0f, 0f, bankAngle);

        // Áp dụng offset xoay model (để sửa dơi bay ngang)
        flyRotation *= Quaternion.Euler(modelRotationOffset);

        bat.transform.rotation = Quaternion.Slerp(bat.transform.rotation, flyRotation, dt * turnSpeed * 2f);
    }

    Vector3 PickRandomPosition()
    {
        if (flightZone == null) return transform.position;

        Bounds bounds = flightZone.bounds;
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    Vector3 ClampToFlightZone(Vector3 pos)
    {
        if (flightZone == null) return pos;

        Bounds bounds = flightZone.bounds;
        float margin = 0.5f;
        pos.x = Mathf.Clamp(pos.x, bounds.min.x + margin, bounds.max.x - margin);
        pos.y = Mathf.Clamp(pos.y, bounds.min.y + margin, bounds.max.y - margin);
        pos.z = Mathf.Clamp(pos.z, bounds.min.z + margin, bounds.max.z - margin);
        return pos;
    }
}