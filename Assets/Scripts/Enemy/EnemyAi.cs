using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    private Animator anim;

    // Tuần tra
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange;

    // Trạng thái
    public float sightRange, attackRange;
    public bool playerInSightRange, playerInAttackRange;

    // ---- THÊM BIẾN CHO ĐÒN ĐÁNH ----
    public float timeBetweenAttacks = 2f; // Thời gian giãn cách giữa 2 đòn đánh
    bool alreadyAttacked;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if (!playerInSightRange && !playerInAttackRange) Patrolling();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInSightRange && playerInAttackRange) AttackPlayer();

        // Animation di chuyển
        if (anim != null && agent != null)
        {
            float currentSpeed = agent.velocity.magnitude;
            anim.SetFloat("Speed", currentSpeed);
        }
    }

    private void AttackPlayer()
    {
        // Quái dừng lại để đánh
        agent.SetDestination(transform.position);
        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            // --- KÍCH HOẠT ANIMATION ĐÁNH ---
            anim.SetTrigger("Attack");

            alreadyAttacked = true;
            // Gọi hàm Reset đòn đánh sau một khoảng thời gian chờ
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    // Các hàm di chuyển giữ nguyên...
    private void Patrolling() { /* code tuần tra */ }
    private void ChasePlayer() { agent.SetDestination(player.position); }
}