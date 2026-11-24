using UnityEngine;

// Fixed sprite pivots keep animation independent of the physics collider.
public sealed class ForestioFrameAnimation : MonoBehaviour
{
    public Sprite[] frames;
    public bool robot;
    private SpriteRenderer visual;
    private Rigidbody2D body;
    private ForestioPlayerController player;
    private ForestioRobotController patrol;
    private float throwUntil, disabledAt;
    private bool wasDisabled;
    public int CurrentFrame { get; private set; }
    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>(); body = GetComponent<Rigidbody2D>();
        player = GetComponent<ForestioPlayerController>(); patrol = GetComponent<ForestioRobotController>();
        if (player != null) player.StoneThrown += OnThrow;
    }
    private void OnDestroy() { if (player != null) player.StoneThrown -= OnThrow; }
    private void OnThrow() { throwUntil = Time.time + 0.32f; }
    private void LateUpdate()
    {
        if (frames == null || frames.Length == 0 || Time.timeScale == 0f) return;
        int index;
        if (robot)
        {
            bool off = patrol != null && patrol.IsDisabled;
            if (off && !wasDisabled) disabledAt = Time.time;
            wasDisabled = off;
            index = off ? 4 + Mathf.Min(3, (int)((Time.time - disabledAt) * 9f))
                : Mathf.Abs(body.linearVelocity.x) > 0.1f ? (int)(Time.time * 9f) % 4 : 0;
            visual.color = Color.white;
        }
        else if (Time.time < throwUntil) index = 12 + Mathf.Clamp((int)((0.32f - (throwUntil - Time.time)) * 12.5f), 0, 3);
        else if (player.IsCharging) index = 13;
        else if (!player.IsOnGround) index = body.linearVelocity.y > 0.1f ? 9 : 10;
        else index = Mathf.Abs(body.linearVelocity.x) > 0.1f ? 4 + (int)(Time.time * 10f) % 4 : (int)(Time.time * 3f) % 4;
        CurrentFrame = Mathf.Clamp(index, 0, frames.Length - 1);
        visual.sprite = frames[CurrentFrame];
    }
}
