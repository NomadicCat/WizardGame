using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour, IDamageable
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
        if (player == null)
        {
            Debug.LogError("EnemyAI: Player GameObject not found! Make sure there's a GameObject named 'Player' in the scene.");
        }
        else
        {
            Debug.Log("EnemyAI: Player found at " + player.position);
        }
        
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        agent.updateRotation = true;
        agent.updateUpAxis = true;
        
        // Configure agent for proper movement
        agent.speed = 3.5f; // Set a reasonable speed
        agent.acceleration = 8f;
        agent.angularSpeed = 120f;
        agent.stoppingDistance = 0.1f;
        
        Debug.Log($"NavMeshAgent configured - Speed: {agent.speed}, Acceleration: {agent.acceleration}");

        // If a Rigidbody exists, make it kinematic so it doesn't fight the agent
        var existingRb = GetComponent<Rigidbody>();
        if (existingRb != null)
        {
            existingRb.isKinematic = true;
        }
    }

    private void Update()
    {
        if (player == null) return; // Don't run if player is not found
        
        // Check if player is in sight range or attack range
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        // Debug information
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (playerInSightRange)
        {
            Debug.Log($"Player in sight range! Distance: {distanceToPlayer:F2}, Sight Range: {sightRange}");
        }
        if (playerInAttackRange)
        {
            Debug.Log($"Player in attack range! Distance: {distanceToPlayer:F2}, Attack Range: {attackRange}");
        }
        
        // Debug current state
        if (!playerInSightRange && !playerInAttackRange)
        {
            Debug.Log("Patrolling - no player detected");
        }
        else if (playerInSightRange && !playerInAttackRange)
        {
            Debug.Log("Chasing player");
        }
        else if (playerInSightRange && playerInAttackRange)
        {
            Debug.Log("Attacking player");
        }

        if (!playerInSightRange && !playerInAttackRange) Patroling();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInSightRange && playerInAttackRange) AttackPlayer();
    }

    private void Patroling()
    {
        // Let NavMeshAgent handle all rotation during patrol
        agent.updateRotation = true;
        agent.isStopped = false;
        
        if (!waklkPointSet) SearchWalkPoint();

        if (waklkPointSet)
        {
            agent.SetDestination(walkPoint);
            Debug.Log("Patrolling to: " + walkPoint);
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;
        // Patrol to walk point
        if (distanceToWalkPoint.magnitude < 1f)
        {
            waklkPointSet = false;
            Debug.Log("Reached walk point, searching for new one");
        }
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
        // Re-enable agent control for smooth movement
        agent.isStopped = false;
        agent.updateRotation = true; // Let agent handle rotation during movement
        agent.SetDestination(player.position);
        Debug.Log($"Chasing player to position: {player.position}");
    }

    private void AttackPlayer()
    {
        // Stop movement while attacking
        agent.isStopped = true;
        
        // Disable agent rotation control to prevent stretching
        agent.updateRotation = false;

        // Calculate direction to player for aiming
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0f; // Keep rotation on horizontal plane
        
        if (directionToPlayer.sqrMagnitude > 0.0001f)
        {
            // Rotate to face player
            var targetRot = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
        }

        if (!alreadyAttacked)
        {
            // Attack code here
            Debug.Log("Attacking player...");
            
            // Calculate fresh direction to player for shooting
            Vector3 shootDirection = (player.position - transform.position).normalized;
            
            // Debug the direction calculation
            Debug.Log($"Enemy position: {transform.position}");
            Debug.Log($"Player position: {player.position}");
            Debug.Log($"Raw direction vector: {player.position - transform.position}");
            Debug.Log($"Normalized direction: {shootDirection}");
            
            // Spawn projectile at a safe distance from enemy to avoid self-collision
            Vector3 spawnPosition = transform.position + shootDirection * 1.5f; // 1.5 units away from enemy
            spawnPosition.y = transform.position.y + 1f; // Slightly above ground
            
            var spawned = Instantiate(projectile, spawnPosition, Quaternion.identity);
            Debug.Log($"Projectile spawned: {spawned.name} at {spawnPosition}");
            
            var simple = spawned.GetComponent<SimpleProjectile>();
            if (simple != null)
            {
                Debug.Log("SimpleProjectile component found!");
                // Launch projectile towards player with slight upward arc
                var dir = (shootDirection + Vector3.up * 0.1f).normalized;
                simple.Launch(dir);
                Debug.Log($"Shooting at player from {spawnPosition} to {player.position}, direction: {dir}");
                
                // Draw debug line to show where we're aiming
                Debug.DrawLine(spawnPosition, spawnPosition + dir * 10f, Color.green, 2f);
            }
            else
            {
                Debug.LogWarning("SimpleProjectile component NOT found! Trying fallback...");
                // Fallback: try physics if prefab still uses Rigidbody
                var projRb = spawned.GetComponent<Rigidbody>() ?? spawned.GetComponentInChildren<Rigidbody>();
                if (projRb != null)
                {
                    Debug.Log("Using Rigidbody fallback");
                    projRb.AddForce(shootDirection * 32f, ForceMode.Impulse);
                    projRb.AddForce(Vector3.up * 8f, ForceMode.Impulse);
                }
                else
                {
                    Debug.LogError($"Projectile '{spawned.name}' has no SimpleProjectile or Rigidbody. Check your projectile prefab setup!");
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
    
    // Helper method to ensure agent is properly configured for movement
    private void EnsureAgentMovement()
    {
        if (agent != null)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
        }
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