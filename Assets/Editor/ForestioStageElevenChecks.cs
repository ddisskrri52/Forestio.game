using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ForestioStageElevenChecks
{
    private const string RunningKey = "Forestio.Stage11.Checks";
    private static readonly ContactPoint2D[] Contacts = new ContactPoint2D[16];
    private static readonly List<Func<bool>> Commands = new List<Func<bool>>();
    private static ForestioPlayerController player;
    private static ForestioPlayerHealth health;
    private static Rigidbody2D body;
    private static Collider2D catCollider;
    private static ForestioRobotController robot;
    private static ForestioBranchBarrier barrier;
    private static ForestioHitButton button;
    private static ForestioAbilityNotice notice;
    private static int command;
    private static double timeout;
    private static bool initialized;

    static ForestioStageElevenChecks() => EditorApplication.update += Tick;
    private const string PortalCheckKey = "Forestio.Stage12.CheckPortal";
    public static void Run() => StartChecks(false);
    public static void RunToPortal() => StartChecks(true);
    private static void StartChecks(bool checkPortal)
    {
        SessionState.SetBool(PortalCheckKey, checkPortal);
        EditorSceneManager.OpenScene(ForestioStageElevenSetup.LevelPath, OpenSceneMode.Single);
        SessionState.SetBool(RunningKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Do(Action action) => Commands.Add(() => { action(); return true; });
    private static void Wait(float seconds)
    {
        float until = -1f;
        Commands.Add(() => { if (until < 0f) until = Time.time + seconds; return Time.time >= until; });
    }
    private static bool Grounded()
    {
        int count = body.GetContacts(Contacts);
        for (int i = 0; i < count; i++) if (Contacts[i].normal.y > 0.5f) return true;
        return false;
    }
    private static void Move(string label, float x, float top, bool jump = false)
    {
        bool jumped = false;
        float until = -1f;
        Commands.Add(() =>
        {
            if (until < 0f) { until = Time.time + 8f; Debug.Log("LEVEL_ROUTE: " + label); }
            Require(Time.time < until, "Route stuck at " + label + "; position=" + body.position);
            if (health.MovementLocked) return false;
            Vector2 velocity = body.linearVelocity;
            float difference = x - body.position.x;
            velocity.x = Mathf.Abs(difference) < 0.08f ? 0f : Mathf.Sign(difference) * 5f;
            bool grounded = Grounded();
            float foot = catCollider.bounds.min.y;
            if (!jumped && grounded && (jump || top - foot > 0.3f))
            {
                velocity.y = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y * body.gravityScale) * 2.5f);
                jumped = true;
            }
            body.linearVelocity = velocity;
            return grounded && Mathf.Abs(difference) < 0.08f && Mathf.Abs(foot - top) < 0.08f;
        });
    }
    private static void Pickup()
    {
        Do(() =>
        {
            Require(!player.HasStone, "Pickup check began with a held stone.");
            player.SendMessage("TryPickUpStone", SendMessageOptions.RequireReceiver);
            Require(player.HasStone, "Nearby pile did not give a stone at " + body.position);
        });
    }
    private static void Throw(bool left, bool charged)
    {
        Do(() =>
        {
            Require(player.HasStone, "Throw has no held stone.");
            Require(!charged || player.HasChargedThrow, "Charged throw not unlocked.");
            player.GetComponent<SpriteRenderer>().flipX = left;
            typeof(ForestioPlayerController).GetMethod("TryThrowStone", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(player, new object[] { charged });
            Require(!player.HasStone, "Throw did not consume held stone.");
        });
    }
    private static void PrepareRoute()
    {
        Require(UnityEngine.Object.FindObjectsByType<ForestioStonePile>(FindObjectsSortMode.None).Length == 3, "Expected three piles.");
        Require(!player.HasChargedThrow && !button.IsActivated && !barrier.IsBroken, "Level did not start locked.");
        if (SessionState.GetBool(PortalCheckKey, false))
        {
            ForestioPortal portal = UnityEngine.Object.FindFirstObjectByType<ForestioPortal>();
            Require(portal != null && !portal.TryEnter(player), "Portal allowed early completion.");
        }
        // Drive the same velocity/jump height as the controller, independent of editor focus.
        // Disable the robot for navigation first, then restore it for a real projectile hit.
        player.enabled = false;
        robot.DisableRobot();
        Wait(0.15f);
        Move("start pile", -14.3f, -2.5f); Pickup();
        Move("training jump", -11.8f, -1f);
        Move("ground before spikes", -10f, -2.5f);
        Move("jump over spikes", -7f, -2.5f, true);
        Move("central junction", 0.2f, -2.5f);
        Move("left first step", -2.5f, -1f);
        Move("left second step", -5.3f, 0.5f);
        Move("locked branches", -7.8f, 2f);
        Throw(true, false); Wait(0.4f);
        Do(() => Require(!barrier.IsBroken, "Normal throw bypassed ability gate."));
        Move("return left second step", -5.3f, 0.5f);
        Move("return junction", 0.2f, -2.5f); Pickup();
        Move("button first step", 3f, -1f);
        Move("button second step", 6f, 0.5f);
        Move("button landing", 8f, 2f);
        Throw(false, false); Wait(0.3f);
        Do(() => Require(button.IsActivated && !GameObject.Find("Door"), "Button did not open module route."));
        Move("third pile before pit", 11f, -2.5f); Pickup();
        Do(() => robot.ResetAfterDefeat());
        Throw(false, false); Wait(0.8f);
        Do(() => Require(robot.IsDisabled, "Stone did not disable robot across pit."));
        Pickup();
        Move("jump across pit", 15f, -2.5f, true);
        Move("food and module", 20.3f, -2.5f);
        Do(() =>
        {
            Require(player.HasChargedThrow, "Actual module trigger did not unlock ability.");
            if (SessionState.GetBool(PortalCheckKey, false))
                Require(!UnityEngine.Object.FindFirstObjectByType<ForestioPortal>().TryEnter(player), "Portal allowed completion before branches were broken.");
            Require(Camera.main.transform.position.x > 10f, "Camera did not follow into module branch.");
            CheckCamera();
        });
        Move("return to pit", 15f, -2.5f);
        Move("jump back across pit", 11f, -2.5f, true);
        Move("return to central junction", 0.2f, -2.5f);
        Move("final first step", -2.5f, -1f);
        Move("final second step", -5.3f, 0.5f);
        Move("return to ability gate", -7.8f, 2f);
        Throw(true, true); Wait(0.3f);
        Do(() => Require(barrier.IsBroken, "Charged throw did not open final path."));
        Move("final landing", -13.3f, 2f);
        Wait(0.5f);
        Do(() =>
        {
            Require(Camera.main.transform.position.x < -9f, "Camera did not return into final branch.");
            CheckCamera();
            Require(health.CurrentHealth == 3, "Route caused unexpected damage.");
        });
        if (SessionState.GetBool(PortalCheckKey, false))
        {
            float enterDeadline = -1f;
            Commands.Add(() =>
            {
                if (enterDeadline < 0f) enterDeadline = Time.unscaledTime + 3f;
                Require(Time.unscaledTime < enterDeadline, "Entering portal timed out.");
                ForestioLevelCompletion ending = UnityEngine.Object.FindFirstObjectByType<ForestioLevelCompletion>();
                if (ending.IsCompleted)
                {
                    Require(Time.timeScale == 0f && !player.enabled && !player.IsCharging, "Ending did not stop gameplay.");
                    Require(body.linearVelocity.sqrMagnitude < 0.001f, "Cat kept moving after completion.");
                    int hp = health.CurrentHealth;
                    Require(!health.TryTakeDamage(Vector2.zero), "Cat took damage during ending.");
                    health.ReturnFromHazard();
                    Require(health.CurrentHealth == hp && !health.IsReturningFromHazard, "Hazard interrupted ending.");
                    Require(!ending.TryComplete(player), "Ending triggered twice.");
                    return true;
                }
                body.linearVelocity = new Vector2(-5f, body.linearVelocity.y);
                return false;
            });
        }
    }
    private static void CheckCamera()
    {
        Camera camera = Camera.main;
        Vector3 viewport = camera.WorldToViewportPoint(player.transform.position);
        Require(viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f, "Camera lost cat.");
        float height = camera.orthographicSize;
        float width = height * camera.aspect;
        Vector3 center = camera.transform.position;
        Require(center.x - width >= -19.51f && center.x + width <= 23.51f
            && center.y - height >= -4.01f && center.y + height <= 10.01f, "Camera crossed level bounds.");
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        if (timeout == 0) timeout = EditorApplication.timeSinceStartup + 180;
        try
        {
            Require(EditorApplication.timeSinceStartup < timeout, "Level route checks timed out.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (!initialized)
            {
                player = UnityEngine.Object.FindFirstObjectByType<ForestioPlayerController>();
                if (player == null) return;
                body = player.GetComponent<Rigidbody2D>();
                health = player.GetComponent<ForestioPlayerHealth>();
                catCollider = player.GetComponent<Collider2D>();
                robot = UnityEngine.Object.FindFirstObjectByType<ForestioRobotController>();
                barrier = UnityEngine.Object.FindFirstObjectByType<ForestioBranchBarrier>();
                button = UnityEngine.Object.FindFirstObjectByType<ForestioHitButton>();
                notice = UnityEngine.Object.FindFirstObjectByType<ForestioAbilityNotice>();
                PrepareRoute();
                initialized = true;
            }
            Require(!health.IsDefeated, "Navigation caused defeat.");
            if (notice.IsOpen) { notice.TryClose(); return; }
            if (command < Commands.Count)
            {
                if (Commands[command]()) { body.linearVelocity = new Vector2(0f, body.linearVelocity.y); command++; }
                return;
            }
            Debug.Log("FORESTIO_STAGE11_PLAYCHECK_OK: both branches, all three piles, platform jumps, pit round trip, button, robot hit, actual ability pickup, charged gate and final landing verified.");
            if (SessionState.GetBool(PortalCheckKey, false))
            {
                ForestioLevelCompletion ending = UnityEngine.Object.FindFirstObjectByType<ForestioLevelCompletion>();
                Require(ending != null && ending.IsCompleted, "Portal route ended without completion.");
                ending.enabled = false;
                Require(Time.timeScale == 1f, "Completion left global time paused after teardown.");
                Debug.Log("FORESTIO_STAGE12_PLAYCHECK_OK: full route entered actual portal trigger; early completion rejected, gameplay frozen, damage blocked and time restored on teardown.");
                SessionState.SetBool(PortalCheckKey, false);
            }
            SessionState.SetBool(RunningKey, false);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            SessionState.SetBool(RunningKey, false);
            EditorApplication.Exit(1);
        }
    }
}
