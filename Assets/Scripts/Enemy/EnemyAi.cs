using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    private Animator anim;

    [Header("Patrolling")]
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange;

    [Header("Status")]
    public float sightRange;
    public float attackRange;
    public bool playerInSightRange;
    public bool playerInAttackRange;

    [Header("Attack Settings")]
    public float timeBetweenAttacks = 2f;
    bool alreadyAttacked;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        // Kiểm tra xem người chơi có trong tầm nhìn hoặc tầm đánh không
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        // Xử lý chuyển đổi trạng thái AI độc lập
        if (!playerInSightRange && !playerInAttackRange) Patrolling();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInSightRange && playerInAttackRange) AttackPlayer();

        // ĐỒNG BỘ VẬN TỐC THỰC TẾ VÀO ANIMATOR
        if (anim != null && agent != null)
        {
            // agent.velocity.magnitude sẽ lấy tốc độ thực tế của con quái
            float currentSpeed = agent.velocity.magnitude;
            anim.SetFloat("Speed", currentSpeed);
        }
    }

    private void Patrolling()
    {
        // NHẢ THẮNG: Cho phép chân di chuyển và tính vận tốc để chạy animation Walk
        if (agent.isStopped) agent.isStopped = false;

        if (!walkPointSet) SearchWalkPoint();
        if (walkPointSet) agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        // Đã đến điểm tuần tra thì reset để tìm điểm mới
        if (distanceToWalkPoint.magnitude < 1f) walkPointSet = false;
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
            walkPointSet = true;
    }

    private void ChasePlayer()
    {
        // NHẢ THẮNG: Cực kỳ quan trọng để quái không bị trượt đơ sau khi vừa tấn công xong
        if (agent.isStopped) agent.isStopped = false;

        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        // ĐẠP THẮNG: Bắt quái đứng im tại chỗ để thực hiện đòn vung tay đánh
        agent.isStopped = true;

        // Quay mặt về phía Player nhưng giữ trục Y thẳng (không bị ngửa lên/nghiêng xuống)
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        if (!alreadyAttacked)
        {
            // Kích hoạt Trigger Attack trong Animator
            if (anim != null) anim.SetTrigger("Attack");

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    // Vẽ các vòng tròn bán kính tầm nhìn trong giao diện Scene để dễ căn chỉnh thông số
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}