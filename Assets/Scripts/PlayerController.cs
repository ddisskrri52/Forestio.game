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

    [Header("Charged throw")]
    [SerializeField, Min(0.1f)] private float chargeDuration = 0.7f;
    [SerializeField, Min(0f)] private float chargingMoveSpeed = 3f;
    [SerializeField, Min(0.1f)] private float chargedThrowSpeed = 16f;
    [SerializeField, Min(0.1f)] private float chargedThrowDistance = 16f;
    [SerializeField] private Color fullChargeColor = new Color(1f, 0.85f, 0.4f, 1f);

    private ForestioStonePile[] stonePiles;
    private SpriteRenderer heldStoneRenderer;
    private Color normalStoneColor = Color.white;
    private bool isCharging;
    private float chargeStartedAt;

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
    private ForestioPlayerHealth health;
    private Rigidbody2D body;
    private SpriteRenderer visual;
    private float moveInput;
    private bool jumpRequested;

    [Header("Ability")]
    [SerializeField] private bool chargedThrowUnlocked;
    private int ignoreInputThroughFrame = -1;

    public event System.Action StoneThrown;
    public bool IsOnGround => IsGrounded();
    public bool HasStone { get; private set; }
    public bool HasChargedThrow => chargedThrowUnlocked;
    public bool IsCharging => isCharging;
    public float ChargeProgress => isCharging
        ? Mathf.Clamp01((Time.time - chargeStartedAt) / Mathf.Max(0.1f, chargeDuration))
        : 0f;

    private void Awake()
    {
        ignoreInputThroughFrame = Time.frameCount;
        stonePiles = FindObjectsByType<ForestioStonePile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        health = GetComponent<ForestioPlayerHealth>();
        body = GetComponent<Rigidbody2D>();
        visual = GetComponentInChildren<SpriteRenderer>();

        HasStone = false;
        chargedThrowUnlocked = false;
        if (heldStone != null)
        {
            heldStone.SetActive(false);
            heldStoneRenderer = heldStone.GetComponent<SpriteRenderer>();
            if (heldStoneRenderer != null)
            {
                normalStoneColor = heldStoneRenderer.color;
                if (visual != null)
                {
                    heldStoneRenderer.sortingLayerID = visual.sortingLayerID;
                    heldStoneRenderer.sortingOrder = visual.sortingOrder + 1;
                }
            }
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !Application.isFocused || Time.timeScale == 0f || Time.frameCount <= ignoreInputThroughFrame
            || (health != null && health.MovementLocked))
        {
            moveInput = 0f;
            jumpRequested = false;
            CancelCharge();
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

        HandleThrowInput(keyboard);
        if (heldStoneRenderer != null)
            heldStoneRenderer.color = isCharging && ChargeProgress >= 1f ? fullChargeColor : normalStoneColor;
    }

    private void LateUpdate()
    {
        if (HasStone && heldStone != null)
            PositionHeldStone();
    }

    private void FixedUpdate()
    {
        if (health != null && health.MovementLocked)
        {
            jumpRequested = false;
            return;
        }
        Vector2 velocity = body.linearVelocity;
        velocity.x = moveInput * (isCharging ? chargingMoveSpeed : moveSpeed);

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

    public Transform NearbyStonePile => FindNearbyStonePile();
    public bool CanPickUpStone => !HasStone && heldStone != null && Time.timeScale > 0f
        && (health == null || !health.MovementLocked) && FindNearbyStonePile() != null;

    private Transform FindNearbyStonePile()
    {
        float nearestSquared = pickupDistance * pickupDistance;
        Transform nearest = null;
        foreach (ForestioStonePile pile in stonePiles)
        {
            if (pile == null || !pile.IsAvailable) continue;
            float squared = ((Vector2)pile.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (squared > nearestSquared) continue;
            nearestSquared = squared;
            nearest = pile.transform;
        }
        // Keep old mechanic-test scenes working with their original single-pile reference.
        if (stonePiles.Length == 0 && nearest == null && stonePile != null && stonePile.gameObject.activeInHierarchy
            && ((Vector2)stonePile.position - (Vector2)transform.position).sqrMagnitude <= nearestSquared)
            nearest = stonePile;
        return nearest;
    }

    private void TryPickUpStone()
    {
        if (!CanPickUpStone) return;
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

    private void HandleThrowInput(Keyboard keyboard)
    {
        Mouse mouse = Mouse.current;
        bool pressed = keyboard.fKey.wasPressedThisFrame || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        bool held = keyboard.fKey.isPressed || (mouse != null && mouse.leftButton.isPressed);

        if (!HasStone)
        {
            CancelCharge();
            return;
        }

        if (!chargedThrowUnlocked)
        {
            if (pressed) TryThrowStone(false);
            return;
        }

        // Both bindings control one action. A second press does not restart it.
        if (pressed && !isCharging)
        {
            isCharging = true;
            chargeStartedAt = Time.time;
        }

        if (isCharging && !held)
        {
            bool charged = ChargeProgress >= 1f;
            TryThrowStone(charged);
            CancelCharge();
        }
    }

    private void TryThrowStone(bool charged)
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
            projectileRenderer.color = charged ? fullChargeColor : normalStoneColor;
        }
        projectile.Launch(direction,
            charged ? chargedThrowSpeed : throwSpeed,
            charged ? chargedThrowDistance : throwDistance,
            transform, charged);
        TryConsumeStone();
        StoneThrown?.Invoke();
    }

    public bool TryConsumeStone()
    {
        if (!HasStone)
            return false;

        CancelCharge();
        HasStone = false;
        if (heldStone != null)
            heldStone.SetActive(false);
        return true;
    }

    public bool UnlockChargedThrow()
    {
        if (chargedThrowUnlocked)
            return false;
        chargedThrowUnlocked = true;
        return true;
    }

    public void CancelCharge()
    {
        isCharging = false;
        chargeStartedAt = 0f;
        if (heldStoneRenderer != null)
            heldStoneRenderer.color = normalStoneColor;
    }

    public void ClearPendingInput()
    {
        CancelCharge();
        moveInput = 0f;
        jumpRequested = false;
        ignoreInputThroughFrame = Time.frameCount;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ClearPendingInput();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) ClearPendingInput();
    }

    private void OnDisable()
    {
        ClearPendingInput();
    }
}
