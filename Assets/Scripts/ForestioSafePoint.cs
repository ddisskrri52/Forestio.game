using UnityEngine;

[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Rigidbody2D), typeof(ForestioPlayerHealth))]
public sealed class ForestioSafePoint : MonoBehaviour
{
    private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    private Rigidbody2D body;
    private Collider2D playerCollider;
    private ForestioPlayerHealth health;
    private ForestioHazard[] hazards;
    private ForestioRobotController[] robots;
    private Vector3 startPosition;
    public Vector3 SafePosition { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        health = GetComponent<ForestioPlayerHealth>();
        startPosition = transform.position;
        SafePosition = startPosition;
    }

    private void Start()
    {
        hazards = FindObjectsByType<ForestioHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        robots = FindObjectsByType<ForestioRobotController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private void FixedUpdate()
    {
        if (health.MovementLocked || !body.simulated || playerCollider == null
            || Mathf.Abs(body.linearVelocity.y) > 0.1f) return;
        Bounds bounds = playerCollider.bounds;
        Vector2 foot = new Vector2(bounds.center.x, bounds.min.y + 0.05f);
        // All three probes must share one static surface, away from either edge.
        Collider2D surface = StaticSurfaceBelow(foot);
        if (surface == null || StaticSurfaceBelow(foot + Vector2.left) != surface
            || StaticSurfaceBelow(foot + Vector2.right) != surface) return;
        foreach (ForestioHazard hazard in hazards)
        {
            if (hazard == null || !hazard.gameObject.activeInHierarchy) continue;
            Collider2D collider = hazard.GetComponent<Collider2D>();
            if (collider != null && Vector2.Distance(foot, collider.ClosestPoint(foot)) < 1f) return;
        }
        foreach (ForestioRobotController robot in robots)
        {
            if (robot == null || robot.IsDisabled || !robot.gameObject.activeInHierarchy) continue;
            Collider2D collider = robot.GetComponent<Collider2D>();
            if (collider != null && Vector2.Distance(body.position, collider.ClosestPoint(body.position)) < 2f) return;
        }
        SafePosition = transform.position;
    }

    private Collider2D StaticSurfaceBelow(Vector2 origin)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        int count = Physics2D.Raycast(origin, Vector2.down, filter, hits, 0.15f);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = hits[i].collider;
            if (collider == null || collider.attachedRigidbody == body || hits[i].normal.y < 0.9f) continue;
            if (collider.attachedRigidbody == null || collider.attachedRigidbody.bodyType == RigidbodyType2D.Static)
                return collider;
        }
        return null;
    }

    public void ResetToStart() => SafePosition = startPosition;
}
