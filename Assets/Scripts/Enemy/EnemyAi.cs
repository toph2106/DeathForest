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

    [Header("Status Ranges")]
    public float sightRange = 15f;
    public float attackRange = 2f;
    public bool playerInSightRange;
    public bool playerInAttackRange;

    [Header("Góc Nhìn Khử Góc Khuất (FOV)")]
    public float viewAngle = 90f;
    public LayerMask obstacleMask;

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
        // 1. Kiểm tra khoảng cách thô trước
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        // 2. Nếu ở trong tầm nhìn thô, check tiếp góc mắt và vật cản
        if (playerInSightRange)
        {
            if (!IsPlayerInFieldOfView())
            {
                playerInSightRange = false; // Mất dấu nếu ở sau lưng hoặc khuất tường
            }
        }

        // 3. Xử lý trạng thái di chuyển/tấn công độc lập
        if (!playerInSightRange && !playerInAttackRange) Patrolling();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInSightRange && playerInAttackRange) AttackPlayer();

        // 4. ĐỒNG BỘ ANIMATION DI CHUYỂN CHUẨN XÁC THEO TỐC ĐỘ THỰC TẾ
        if (anim != null && agent != null)
        {
            // Kiểm tra nếu quái đang bị dừng chân (để đánh) thì ép Speed về 0
            if (agent.isStopped)
            {
                anim.SetFloat("Speed", 0f);
            }
            else
            {
                // Lấy vận tốc thực tế của cơ thể quái để truyền vào Animator
                float currentSpeed = agent.velocity.magnitude;
                anim.SetFloat("Speed", currentSpeed);
            }
        }
    }

    private bool IsPlayerInFieldOfView()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        if (Vector3.Angle(transform.forward, directionToPlayer) < viewAngle / 2)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Bắn tia kiểm tra vật cản (cách mặt đất 1 mét để tránh đụng cỏ)
            if (!Physics.Raycast(transform.position + Vector3.up * 1f, directionToPlayer, distanceToPlayer, obstacleMask))
            {
                return true;
            }
        }
        return false;
    }

    private void Patrolling()
    {
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
        if (agent.isStopped) agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        agent.isStopped = true;
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

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

        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * sightRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * sightRange);
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal) angleInDegrees += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}