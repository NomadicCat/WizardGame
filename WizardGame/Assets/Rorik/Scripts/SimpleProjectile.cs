using UnityEngine;

public class SimpleProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 25f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float radius = 0.2f;
    [SerializeField] private LayerMask hitMask = ~0;

    private Vector3 _direction = Vector3.forward;
    private float _timeAlive;

    public void Launch(Vector3 direction, float overrideSpeed = -1f)
    {
        if (direction.sqrMagnitude > 0f)
        {
            _direction = direction.normalized;
        }
        if (overrideSpeed > 0f)
        {
            speed = overrideSpeed;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector3 start = transform.position;
        Vector3 end = start + _direction * speed * dt;

        if (Physics.SphereCast(start, radius, _direction, out RaycastHit hit, (end - start).magnitude, hitMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
            OnHit(hit);
            return;
        }

        transform.position = end;

        _timeAlive += dt;
        if (_timeAlive >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnHit(RaycastHit hit)
    {
        // Extend: damage, VFX, events
        Destroy(gameObject);
    }
}


