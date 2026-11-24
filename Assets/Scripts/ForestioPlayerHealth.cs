using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ForestioPlayerController), typeof(Rigidbody2D))]
public sealed class ForestioPlayerHealth : MonoBehaviour
{
    public const int MaxHealth = 3;
    private ForestioPlayerController player;
    private Rigidbody2D body;
    private SpriteRenderer visual;
    private Color normalColor;
    private Vector3 startPosition;
    private float invulnerableUntil;
    private float movementLockedUntil;
    private float fade;
    private GUIStyle healthStyle;

    public int CurrentHealth { get; private set; } = MaxHealth;
    public bool IsDefeated { get; private set; }
    public bool IsInvulnerable => IsDefeated || Time.time < invulnerableUntil;
    public bool MovementLocked => IsDefeated || Time.time < movementLockedUntil;

    private void Awake()
    {
        player = GetComponent<ForestioPlayerController>();
        body = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        if (visual != null) normalColor = visual.color;
        startPosition = transform.position;
    }

    private void Update()
    {
        if (visual == null) return;
        Color color = normalColor;
        if (!IsDefeated && IsInvulnerable && Mathf.FloorToInt(Time.time * 12f) % 2 == 0)
            color.a *= 0.35f;
        visual.color = color;
    }

    private void OnCollisionEnter2D(Collision2D collision) => CheckRobot(collision);
    private void OnCollisionStay2D(Collision2D collision) => CheckRobot(collision);

    private void CheckRobot(Collision2D collision)
    {
        ForestioRobotController robot = collision.collider.GetComponentInParent<ForestioRobotController>();
        if (robot != null && !robot.IsDisabled) TryTakeDamage(robot.transform.position);
    }

    public bool TryTakeDamage(Vector2 source)
    {
        if (IsInvulnerable || Time.timeScale == 0f) return false;
        CurrentHealth--;
        player.ClearPendingInput();
        invulnerableUntil = Time.time + 1f;
        movementLockedUntil = Time.time + 0.2f;
        if (CurrentHealth == 0)
        {
            IsDefeated = true;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            player.TryConsumeStone();
            StartCoroutine(Respawn());
        }
        else
        {
            float direction = transform.position.x < source.x ? -1f : 1f;
            body.linearVelocity = new Vector2(direction * 4f, 3f);
        }
        return true;
    }

    public bool TryHeal()
    {
        if (IsDefeated || CurrentHealth >= MaxHealth) return false;
        CurrentHealth++;
        return true;
    }

    private IEnumerator Respawn()
    {
        float started = Time.time;
        while (Time.time - started < 0.25f)
        {
            fade = Mathf.Clamp01((Time.time - started) / 0.25f);
            yield return null;
        }
        fade = 1f;
        foreach (ForestioStoneProjectile stone in FindObjectsByType<ForestioStoneProjectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            stone.gameObject.SetActive(false);
            Destroy(stone.gameObject);
        }
        foreach (ForestioHitButton button in FindObjectsByType<ForestioHitButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            button.ResetAfterDefeat();
        foreach (ForestioRobotController robot in FindObjectsByType<ForestioRobotController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            robot.ResetAfterDefeat();
        foreach (ForestioFood food in FindObjectsByType<ForestioFood>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            food.ResetAfterDefeat();

        transform.position = startPosition;
        body.position = startPosition;
        CurrentHealth = MaxHealth;
        started = Time.time;
        while (Time.time - started < 0.25f)
        {
            fade = 1f - Mathf.Clamp01((Time.time - started) / 0.25f);
            yield return null;
        }
        fade = 0f;
        body.simulated = true;
        body.linearVelocity = Vector2.zero;
        IsDefeated = false;
        invulnerableUntil = Time.time + 1f;
        movementLockedUntil = Time.time;
        player.ClearPendingInput();
    }

    private void OnGUI()
    {
        if (healthStyle == null)
        {
            healthStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            healthStyle.normal.textColor = new Color(1f, 0.85f, 0.91f);
        }
        GUI.Label(new Rect(20f, 15f, 180f, 40f), "HP: " + CurrentHealth + " / " + MaxHealth, healthStyle);
        if (fade <= 0f) return;
        Color previous = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = -100;
        GUI.color = new Color(0f, 0f, 0f, fade);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.depth = previousDepth;
    }

    private void OnDisable()
    {
        if (visual != null) visual.color = normalColor;
    }
}
