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
        // Kiểm tra khoảng cách bằng Physics.CheckSphere
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        // Chuyển đổi trạng thái chuẩn
        if (!playerInSightRange && !playerInAttackRange) Patrolling();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInSightRange && playerInAttackRange) AttackPlayer();

        // Đồng bộ tốc độ với Animator
        if (anim != null && agent != null)
        {
            // Dùng remainingDistance để check xem nó có thực sự đang di chuyển không
            float currentSpeed = agent.velocity.magnitude;
            anim.SetFloat("Speed", currentSpeed);
        }
    }

    private void Patrolling()
    {
        // Trả lại tốc độ di chuyển bình thường khi đi tuần
        if (agent.isStopped) agent.isStopped = false;

        if (!walkPointSet) SearchWalkPoint();
        if (walkPointSet) agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;
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
        // Trả lại tốc độ di chuyển bình thường khi đuổi theo
        if (agent.isStopped) agent.isStopped = false;

        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        // Thay vì SetDestination liên tục, ta ra lệnh cho Agent dừng hẳn chân lại để đánh
        agent.isStopped = true;
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z)); // Giữ quái không bị ngửa lên trời

        if (!alreadyAttacked)
        {
            if (anim != null) anim.SetTrigger("Attack");

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}