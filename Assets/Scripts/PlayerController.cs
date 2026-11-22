using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class ForestioPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float jumpHeight = 2.5f;
    [SerializeField] private LayerMask groundLayers = 1; // Default layer initially.

    [Header("Stone pickup")]
    [SerializeField] private Transform stonePile;
    [SerializeField] private GameObject heldStone;
    [SerializeField, Min(0f)] private float pickupDistance = 1.3f;
    [SerializeField] private Vector2 mouthOffset = new Vector2(0.5f, 0.08f);

    [Header("Stone throw")]
    [SerializeField] private ForestioStoneProjectile projectilePrefab;
    [SerializeField, Min(0.1f)] private float throwSpeed = 12f;
    [SerializeField, Min(0.1f)] private float throwDistance = 9f;

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
    private Rigidbody2D body;
    private SpriteRenderer visual;
    private float moveInput;
    private bool jumpRequested;

    public bool HasStone { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visual = GetComponentInChildren<SpriteRenderer>();

        HasStone = false;
        if (heldStone != null)
        {
            heldStone.SetActive(false);
            SpriteRenderer stoneRenderer = heldStone.GetComponent<SpriteRenderer>();
            if (stoneRenderer != null && visual != null)
            {
                stoneRenderer.sortingLayerID = visual.sortingLayerID;
                stoneRenderer.sortingOrder = visual.sortingOrder + 1;
            }
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !Application.isFocused || Time.timeScale == 0f)
        {
            moveInput = 0f;
            jumpRequested = false;
            return;
        }

        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        moveInput = (right ? 1f : 0f) - (left ? 1f : 0f);

        if (keyboard.spaceKey.wasPressedThisFrame)
            jumpRequested = true;

        if (visual != null && moveInput != 0f)
            visual.flipX = moveInput < 0f;

        if (keyboard.eKey.wasPressedThisFrame)
            TryPickUpStone();

        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (keyboard.fKey.wasPressedThisFrame || mousePressed)
            TryThrowStone();
    }

    private void LateUpdate()
    {
        if (HasStone && heldStone != null)
            PositionHeldStone();
    }

    private void FixedUpdate()
    {
        Vector2 velocity = body.linearVelocity;
        velocity.x = moveInput * moveSpeed;

        if (jumpRequested && velocity.y <= 0.05f && IsGrounded())
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
            velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
        }

        jumpRequested = false;
        body.linearVelocity = velocity;
    }

    private bool IsGrounded()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayers);
        int count = body.GetContacts(filter, contacts);

        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > 0.5f)
                return true;
        }

        return false;
    }

    private void TryPickUpStone()
    {
        if (HasStone || stonePile == null || heldStone == null || !stonePile.gameObject.activeInHierarchy)
            return;

        Vector2 difference = (Vector2)stonePile.position - (Vector2)transform.position;
        if (difference.sqrMagnitude > pickupDistance * pickupDistance)
            return;

        HasStone = true;
        PositionHeldStone();
        heldStone.SetActive(true);
    }

    private void PositionHeldStone()
    {
        float direction = visual != null && visual.flipX ? -1f : 1f;
        Vector3 offset = new Vector3(Mathf.Abs(mouthOffset.x) * direction, mouthOffset.y, 0f);
        heldStone.transform.position = transform.TransformPoint(offset);
    }

    private void TryThrowStone()
    {
        if (!HasStone || heldStone == null || projectilePrefab == null)
            return;

        PositionHeldStone();
        float direction = visual != null && visual.flipX ? -1f : 1f;
        ForestioStoneProjectile projectile = Instantiate(projectilePrefab, heldStone.transform.position, Quaternion.identity);
        SpriteRenderer heldRenderer = heldStone.GetComponent<SpriteRenderer>();
        SpriteRenderer projectileRenderer = projectile.GetComponent<SpriteRenderer>();
        if (heldRenderer != null && projectileRenderer != null)
        {
            projectileRenderer.sortingLayerID = heldRenderer.sortingLayerID;
            projectileRenderer.sortingOrder = heldRenderer.sortingOrder;
        }
        projectile.Launch(direction, throwSpeed, throwDistance, transform);
        TryConsumeStone();
    }

    public bool TryConsumeStone()
    {
        if (!HasStone)
            return false;

        HasStone = false;
        if (heldStone != null)
            heldStone.SetActive(false);
        return true;
    }

    private void OnDisable()
    {
        moveInput = 0f;
        jumpRequested = false;
    }
}
