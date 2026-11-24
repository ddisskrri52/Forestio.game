using UnityEngine;

// Select the pose before the controller attaches the held stone in LateUpdate.
[DefaultExecutionOrder(-50)]
public sealed class ForestioFrameAnimation : MonoBehaviour
{
    public Sprite[] frames;
    public Vector2[] mouthOffsets;
    public bool robot;

    private SpriteRenderer visual;
    private Rigidbody2D body;
    private ForestioPlayerController player;
    private ForestioRobotController patrol;
    private float throwUntil;
    private float disabledAt;
    private bool wasDisabled;

    public int CurrentFrame { get; private set; }
    public bool HasMouthOffsets => frames != null && mouthOffsets != null && mouthOffsets.Length == frames.Length;
    public Vector2 MouthOffset => HasMouthOffsets ? mouthOffsets[CurrentFrame] : new Vector2(.5f, .08f);

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        player = GetComponent<ForestioPlayerController>();
        patrol = GetComponent<ForestioRobotController>();
        if (player != null) player.StoneThrown += PrepareThrowPose;
    }

    private void OnDestroy()
    {
        if (player != null) player.StoneThrown -= PrepareThrowPose;
    }

    public void PrepareThrowPose()
    {
        if (robot || frames == null || frames.Length < 16) return;
        throwUntil = Time.time + .18f;
        SetFrame(14);
    }

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
                : Mathf.Abs(body.linearVelocity.x) > .1f ? (int)(Time.time * 9f) % 4 : 0;
            visual.color = Color.white;
        }
        else if (Time.time < throwUntil)
            index = throwUntil - Time.time > .09f ? 14 : 15;
        else if (!player.IsOnGround)
            index = body.linearVelocity.y > .1f ? 9 : 10;
        else if (Mathf.Abs(body.linearVelocity.x) > .1f)
            index = 4 + (int)(Time.time * 10f) % 4;
        else
            index = player.IsCharging ? 0 : (int)(Time.time * 3f) % 4;

        SetFrame(index);
    }

    private void SetFrame(int index)
    {
        CurrentFrame = Mathf.Clamp(index, 0, frames.Length - 1);
        visual.sprite = frames[CurrentFrame];
    }
}
