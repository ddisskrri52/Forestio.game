using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class ForestioRobotController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float patrolSpeed = 2f;
    [SerializeField, Min(0f)] private float turnPause = 0.3f;
    [SerializeField] private SpriteRenderer eye;
    private readonly RaycastHit2D[] sensorHits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private BoxCollider2D robotCollider;
    private SpriteRenderer visual;
    private Vector3 startPosition;
    private Color initialColor;
    private Color initialEyeColor;
    private float direction = 1f;
    private float resumeAt;

    public bool IsDisabled { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        robotCollider = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        if (visual != null) initialColor = visual.color;
        if (eye != null) initialEyeColor = eye.color;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 3f;
        body.linearDamping = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        if (IsDisabled) return;
        if (Time.time < resumeAt)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        Bounds bounds = robotCollider.bounds;
        bool grounded = HasTerrain(new Vector2(bounds.center.x, bounds.min.y + 0.05f), Vector2.down, 0.2f);
        if (!grounded)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        float front = bounds.center.x + direction * (bounds.extents.x + 0.04f);
        bool groundAhead = HasTerrain(new Vector2(front, bounds.min.y + 0.1f), Vector2.down, 0.35f);
        bool wallAhead = HasTerrain(new Vector2(front, bounds.center.y), Vector2.right * direction, 0.12f);
        if (!groundAhead || wallAhead)
        {
            direction = -direction;
            resumeAt = Time.time + turnPause;
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }
        else
            body.linearVelocity = new Vector2(direction * patrolSpeed, body.linearVelocity.y);

        if (eye != null)
        {
            Vector3 position = eye.transform.localPosition;
            position.x = Mathf.Abs(position.x) * direction;
            eye.transform.localPosition = position;
        }
    }

    private bool HasTerrain(Vector2 origin, Vector2 direction, float distance)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        int count = Physics2D.Raycast(origin, direction, filter, sensorHits, distance);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = sensorHits[i].collider;
            if (hit == null || hit.attachedRigidbody == body
                || hit.GetComponentInParent<ForestioPlayerController>() != null
                || hit.GetComponentInParent<ForestioStoneProjectile>() != null
                || hit.GetComponentInParent<ForestioRobotController>() != null)
                continue;
            return true;
        }
        return false;
    }

    public void ResetAfterDefeat()
    {
        gameObject.SetActive(true);
        transform.position = startPosition;
        body.position = startPosition;
        body.simulated = true;
        body.linearVelocity = Vector2.zero;
        robotCollider.enabled = true;
        direction = 1f;
        resumeAt = Time.time + turnPause;
        IsDisabled = false;
        if (visual != null) visual.color = initialColor;
        if (eye != null) eye.color = initialEyeColor;
    }

    public void DisableRobot()
    {
        if (IsDisabled) return;
        IsDisabled = true;
        body.linearVelocity = Vector2.zero;
        robotCollider.enabled = false;
        body.simulated = false;
        if (visual != null) visual.color = new Color(0.35f, 0.37f, 0.42f, 1f);
        if (eye != null) eye.color = new Color(0.25f, 0.22f, 0.3f, 1f);
    }
}
