using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ForestioStoneProjectile : MonoBehaviour
{
    [SerializeField, Min(0f)] private float bounceSpeed = 3f;
    [SerializeField, Min(0f)] private float bounceUpSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float fallingGravity = 3f;
    [SerializeField, Min(0.1f)] private float fallingLifetime = 4f;

    private readonly RaycastHit2D[] spawnHits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private CircleCollider2D stoneCollider;
    private float flightDirection;
    private float flightSpeed;
    private float expiresAt;
    private bool launched;
    private bool bouncing;
    private bool finished;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        stoneCollider = GetComponent<CircleCollider2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.linearDamping = 0f;
        body.angularVelocity = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void Launch(float direction, float speed, float distance, Transform owner = null)
    {
        flightDirection = direction < 0f ? -1f : 1f;
        flightSpeed = Mathf.Max(0.1f, speed);
        expiresAt = Time.time + Mathf.Max(0.1f, distance) / flightSpeed;
        launched = true;
        bouncing = false;
        finished = false;
        body.gravityScale = 0f;

        if (owner != null)
        {
            // Ignore the cat both in flight and during the rebound.
            foreach (Collider2D ownerCollider in owner.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(stoneCollider, ownerCollider);
        }

        body.linearVelocity = new Vector2(flightDirection * flightSpeed, 0f);
        if (owner != null)
            CheckSpawnPath(owner);
    }

    private void FixedUpdate()
    {
        if (launched && !bouncing && !finished)
            body.linearVelocity = new Vector2(flightDirection * flightSpeed, 0f);
    }

    private void Update()
    {
        if (launched && !finished && Time.time >= expiresAt)
            Finish();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    private void HandleCollision(Collision2D collision)
    {
        if (!launched || finished || collision.contactCount == 0)
            return;

        if (collision.collider.GetComponentInParent<ForestioStoneProjectile>() != null)
        {
            Physics2D.IgnoreCollision(stoneCollider, collision.collider);
            return;
        }

        // An upward-facing contact is a landing, including at a wall corner.
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                Finish();
                return;
            }
        }

        ReactToSurface(collision.GetContact(0).normal);
    }

    private void ReactToSurface(Vector2 normal)
    {
        if (normal.y > 0.5f)
        {
            Finish();
            return;
        }

        if (bouncing || finished)
            return;

        bouncing = true;
        body.gravityScale = fallingGravity;
        expiresAt = Time.time + fallingLifetime;

        // One small wall rebound. A ceiling hit sends the stone downward.
        float horizontal = Mathf.Abs(normal.x) > 0.25f
            ? Mathf.Sign(normal.x) * bounceSpeed
            : flightDirection * bounceSpeed;
        float vertical = normal.y < -0.5f ? -bounceUpSpeed : bounceUpSpeed;
        body.linearVelocity = new Vector2(horizontal, vertical);
    }

    private void CheckSpawnPath(Transform owner)
    {
        // Prevent spawning beyond a thin wall when the cat is very close to it.
        Vector2 origin = owner.position;
        Vector2 path = body.position - origin;
        float length = path.magnitude;
        if (length < 0.001f)
            return;

        float radius = stoneCollider.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        int count = Physics2D.CircleCast(origin, radius, path / length, filter, spawnHits, length);

        int nearest = -1;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = spawnHits[i].collider;
            if (hit == null || hit.attachedRigidbody == body || hit.transform.IsChildOf(owner)
                || hit.GetComponentInParent<ForestioStoneProjectile>() != null)
                continue;

            if (spawnHits[i].distance < nearestDistance)
            {
                nearest = i;
                nearestDistance = spawnHits[i].distance;
            }
        }

        if (nearest >= 0)
        {
            body.position = origin + path.normalized * Mathf.Max(0f, nearestDistance - 0.01f);
            ReactToSurface(spawnHits[nearest].normal);
        }
    }

    private void Finish()
    {
        finished = true;
        stoneCollider.enabled = false;
        Destroy(gameObject);
    }
}
