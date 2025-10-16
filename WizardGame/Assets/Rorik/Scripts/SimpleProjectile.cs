using UnityEngine;

public class SimpleProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 25f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float radius = 0.2f;
    [SerializeField] private LayerMask hitMask = ~0;
    
    [Header("Damage Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private bool destroyOnHit = true;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject trailEffect;

    private Vector3 _direction = Vector3.forward;
    private float _timeAlive;
    private bool _hasLaunched = false;
    private float _launchDelay = 0.1f; // Small delay to avoid immediate collision

    public void Launch(Vector3 direction, float overrideSpeed = -1f)
    {
        Debug.Log($"Projectile Launch called with direction: {direction}");
        
        if (direction.sqrMagnitude > 0f)
        {
            _direction = direction.normalized;
        }
        if (overrideSpeed > 0f)
        {
            speed = overrideSpeed;
        }
        _hasLaunched = true;
        
        Debug.Log($"Projectile launched with final direction: {_direction}, speed: {speed}, position: {transform.position}");
        
        // Spawn trail effect if assigned
        if (trailEffect != null)
        {
            Instantiate(trailEffect, transform.position, transform.rotation, transform);
        }
    }

    private void Update()
    {
        if (!_hasLaunched) return;
        
        float dt = Time.deltaTime;
        _timeAlive += dt;
        
        // Small delay to avoid immediate collision with shooter
        if (_timeAlive < _launchDelay) return;
        
        Vector3 start = transform.position;
        Vector3 end = start + _direction * speed * dt;

        // Debug the projectile movement
        Debug.DrawRay(start, _direction * speed * dt, Color.red, 0.1f);
        Debug.DrawRay(start, _direction * 5f, Color.blue, 0.1f); // Show overall direction

        if (Physics.SphereCast(start, radius, _direction, out RaycastHit hit, (end - start).magnitude, hitMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"Projectile hit: {hit.collider.name} at distance {hit.distance}");
            transform.position = hit.point;
            OnHit(hit);
            return;
        }

        transform.position = end;

        if (_timeAlive >= lifetime)
        {
            OnLifetimeExpired();
        }
    }

    private void OnHit(RaycastHit hit)
    {
        // Apply damage to hit object if it has a health component
        var healthComponent = hit.collider.GetComponent<IDamageable>();
        if (healthComponent != null)
        {
            healthComponent.TakeDamage(damage);
        }
        
        // Try to find health component in parent objects
        if (healthComponent == null)
        {
            var parentHealth = hit.collider.GetComponentInParent<IDamageable>();
            if (parentHealth != null)
            {
                parentHealth.TakeDamage(damage);
            }
        }
        
        // Spawn hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
        }
        
        // Debug log for testing
        Debug.Log($"Projectile hit: {hit.collider.name} at {hit.point}");
        
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
    
    private void OnLifetimeExpired()
    {
        Debug.Log("Projectile lifetime expired");
        Destroy(gameObject);
    }
    
    // Method to set damage from external sources
    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }
    
    // Method to set hit mask from external sources
    public void SetHitMask(LayerMask newHitMask)
    {
        hitMask = newHitMask;
    }
}


