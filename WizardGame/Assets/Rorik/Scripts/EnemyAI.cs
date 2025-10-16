using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;

    [SerializeField] private Transform player;

    [SerializeField] private LayerMask whatIsGround, whatIsPlayer;

    [SerializeField] private float health;

    // Patrolling 
    [SerializeField] private Vector3 walkPoint;
    bool waklkPointSet;
    [SerializeField] private float walkPointRange;

    // Attacking
    [SerializeField] private float timeBetweenAttacks;
    bool alreadyAttacked;
    [SerializeField] private GameObject projectile;

    // States
    [SerializeField] private float sightRange, attackRange;
    [SerializeField] private bool playerInSightRange, playerInAttackRange;

    private void Awake()
    {
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        agent.updateRotation = true;
        agent.updateUpAxis = true;

        // If a Rigidbody exists, make it kinematic so it doesn't fight the agent
        var existingRb = GetComponent<Rigidbody>();
        if (existingRb != null)
        {
            existingRb.isKinematic = true;
        }
    }

    private void Update()
    {
        // Check if player is in sight range or attack range
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if (!playerInSightRange && !playerInAttackRange) Patroling();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInSightRange && playerInAttackRange) AttackPlayer();
    }

    private void Patroling()
    {
        if (!waklkPointSet) SearchWalkPoint();

        if (waklkPointSet)
            agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;
        // Patrol to walk point
        if (distanceToWalkPoint.magnitude < 1f)
            waklkPointSet = false;
    }
    private void SearchWalkPoint()
    {
        // Find a random walk point in range
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);
        // if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
        // {
        //     waklkPointSet = true;
        // }

        // Replace the Physics.Raycast ground check in SearchWalkPoint with:
    if (UnityEngine.AI.NavMesh.SamplePosition(walkPoint, out var hit, 2f, NavMesh.AllAreas)) {
        walkPoint = hit.position; // snap to nearest navmesh point
        waklkPointSet = true;
        }
    }

    private void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        // Stop movement while attacking
        agent.isStopped = true;

        var look = player.position - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(look, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
        }

        if (!alreadyAttacked)
        {
            // Attack code here
            Debug.Log("Attacking player...");
            var spawned = Instantiate(projectile, transform.position, Quaternion.identity);
            var simple = spawned.GetComponent<SimpleProjectile>();
            if (simple != null)
            {
                // launch slightly upward to mimic the old impulse arc
                var dir = (transform.forward + Vector3.up * 0.25f).normalized;
                simple.Launch(dir);
            }
            else
            {
                // Fallback: try physics if prefab still uses Rigidbody
                var projRb = spawned.GetComponent<Rigidbody>() ?? spawned.GetComponentInChildren<Rigidbody>();
                if (projRb != null)
                {
                    projRb.AddForce(transform.forward * 32f, ForceMode.Impulse);
                    projRb.AddForce(transform.up * 8f, ForceMode.Impulse);
                }
                else
                {
                    Debug.LogWarning($"Projectile '{spawned.name}' has no SimpleProjectile or Rigidbody.");
                }
            }

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    // NavMeshAgent handles pathfinding and avoidance.

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0) Invoke(nameof(DestroyEnemy), 0.5f);
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}