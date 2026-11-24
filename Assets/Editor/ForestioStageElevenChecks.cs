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
    private static readonly HashSet<int> CatFrames = new HashSet<int>();
    private static readonly HashSet<int> RobotFrames = new HashSet<int>();
    private const string RestartCheckKey = "Forestio.Stage13.CheckRestart";
    private static int restartStage;
    private static int oldPlayerId;
    private static float restartedAt = -1f;

    static ForestioStageElevenChecks() => EditorApplication.update += Tick;
    private const string PortalCheckKey = "Forestio.Stage12.CheckPortal";
    public static void Run() => StartChecks(false);
    public static void RunToPortal() => StartChecks(true);
    public static void RunWithRestart() => StartChecks(true, true);
    private static void StartChecks(bool checkPortal, bool checkRestart = false)
    {
        SessionState.SetBool(PortalCheckKey, checkPortal);
        SessionState.SetBool(RestartCheckKey, checkRestart);
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
            ForestioHud hud = UnityEngine.Object.FindFirstObjectByType<ForestioHud>();
            if (SessionState.GetBool(RestartCheckKey, false))
                Require(hud != null && hud.PickupHintVisible, "Pickup hint missing near available pile.");
            player.SendMessage("TryPickUpStone", SendMessageOptions.RequireReceiver);
            Require(player.HasStone, "Nearby pile did not give a stone at " + body.position);
            if (hud != null) Require(!hud.PickupHintVisible, "Pickup hint remained after taking stone.");
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
        Throw(false, false); Wait(0.65f);
        Do(() => Require(button.IsActivated && UnityEngine.Object.FindFirstObjectByType<ForestioVineGate>().IsOpen, "Button did not open module route."));
        Move("descend from button", 6f, 0.5f);
        Move("return to lower junction", 0.2f, -2.5f);
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
        if (SessionState.GetBool(RestartCheckKey, false))
        {
            // Carry a fresh stone into the ending so restart must reset inventory too.
            Move("refill after opening final gate", -5.3f, 0.5f);
            Move("refill at hub", 0.2f, -2.5f); Pickup();
            Move("return final first step with stone", -2.5f, -1f);
            Move("return final second step with stone", -5.3f, 0.5f);
            Move("return final landing with stone", -7.8f, 2f);
        }
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
                    ForestioHomeEnding home = UnityEngine.Object.FindFirstObjectByType<ForestioHomeEnding>();
                    if (home != null) Require(home.HasStarted && !home.IsReady, "Home cutscene did not begin before restart became available.");
                    if (SessionState.GetBool(RestartCheckKey, false))
                    {
                        Require(player.HasStone, "Restart inventory check has no held stone.");
                        Require(!ending.CanRestart && !ending.TryRestart(), "Restart ignored short ending delay.");
                    }
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
    private static void ClickUiButton(UnityEngine.UI.Button target)
    {
        Canvas.ForceUpdateCanvases();
        var events = UnityEngine.EventSystems.EventSystem.current;
        Require(events != null, "UI EventSystem is missing.");
        RectTransform rect = target.GetComponent<RectTransform>();
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(Camera.main, rect.TransformPoint(rect.rect.center));
        var pointer = new UnityEngine.EventSystems.PointerEventData(events)
        {
            position = screen,
            button = UnityEngine.EventSystems.PointerEventData.InputButton.Left
        };
        var hits = new List<UnityEngine.EventSystems.RaycastResult>();
        events.RaycastAll(pointer, hits);
        Require(hits.Exists(hit => hit.gameObject == target.gameObject), "UI button is not reachable by pointer: " + target.name);
        UnityEngine.EventSystems.ExecuteEvents.Execute(target.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        Debug.Log("UI_POINTER_CLICK_OK: " + target.name);
    }

    private static void CheckRestart()
    {
        if (restartStage == 1)
        {
            ForestioLevelCompletion ending = UnityEngine.Object.FindFirstObjectByType<ForestioLevelCompletion>();
            if (!ending.CanRestart) return;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StoneProjectile.prefab");
            UnityEngine.Object.Instantiate(prefab, new Vector3(0f, 20f, 0f), Quaternion.identity);
            UnityEngine.UI.Button replay = Array.Find(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None), item => item.name == "PlayAgain");
            if (replay != null)
            {
                if (!replay.interactable) return;
                ClickUiButton(replay);
                Require(!ending.CanRestart, "Replay button did not request restart.");
            }
            else Require(ending.TryRestart(), "Restart request rejected.");
            Require(!ending.TryRestart(), "Duplicate restart request accepted.");
            restartStage = 2;
            return;
        }
        ForestioPlayerController fresh = UnityEngine.Object.FindFirstObjectByType<ForestioPlayerController>();
        if (fresh == null || fresh.GetInstanceID() == oldPlayerId) return;
        if (restartedAt < 0f) restartedAt = Time.unscaledTime;
        if (Time.unscaledTime - restartedAt < 0.15f) return;
        ForestioPlayerHealth freshHealth = fresh.GetComponent<ForestioPlayerHealth>();
        ForestioLevelCompletion completion = UnityEngine.Object.FindFirstObjectByType<ForestioLevelCompletion>();
        Require(Time.timeScale == 1f && fresh.enabled, "Restart left gameplay paused or input disabled.");
        Require(freshHealth.CurrentHealth == 3 && !freshHealth.IsDefeated && !freshHealth.IsReturningFromHazard, "Restart did not reset health.");
        Require(!fresh.HasStone && !fresh.HasChargedThrow && !fresh.IsCharging, "Restart retained inventory or ability.");
        Require(Mathf.Abs(fresh.transform.position.x + 14.5f) < 0.05f, "Restart missed level start.");
        Require(!completion.IsCompleted && !completion.CanRestart && !completion.TryRestart(), "Restart available outside ending.");
        Require(!UnityEngine.Object.FindFirstObjectByType<ForestioHitButton>().IsActivated && GameObject.Find("Door") != null, "Restart retained open door.");
        var freshGate = UnityEngine.Object.FindFirstObjectByType<ForestioVineGate>();
        if (freshGate != null) Require(!freshGate.IsOpen && freshGate.GetComponent<Collider2D>().enabled
            && freshGate.left.gameObject.activeSelf && freshGate.right.gameObject.activeSelf, "Restart failed to close vine gate.");
        ForestioBranchBarrier freshBranches = UnityEngine.Object.FindFirstObjectByType<ForestioBranchBarrier>();
        Require(freshBranches != null && !freshBranches.IsBroken && freshBranches.gameObject.activeSelf, "Restart retained broken branches.");
        ForestioAbilityModule module = UnityEngine.Object.FindFirstObjectByType<ForestioAbilityModule>();
        Require(module != null && !module.IsCollected && module.gameObject.activeSelf, "Restart did not restore module.");
        ForestioRobotController freshRobot = UnityEngine.Object.FindFirstObjectByType<ForestioRobotController>();
        Require(freshRobot != null && !freshRobot.IsDisabled && freshRobot.GetComponent<Collider2D>().enabled, "Restart did not restore robot.");
        ForestioFood food = UnityEngine.Object.FindFirstObjectByType<ForestioFood>();
        Require(food != null && !food.IsCollected && food.gameObject.activeSelf, "Restart did not restore food.");
        Require(UnityEngine.Object.FindObjectsByType<ForestioStoneProjectile>(FindObjectsSortMode.None).Length == 0, "Restart retained projectiles.");
        ForestioStonePile[] piles = UnityEngine.Object.FindObjectsByType<ForestioStonePile>(FindObjectsSortMode.None);
        Require(piles.Length == 3 && Array.TrueForAll(piles, pile => pile.IsAvailable), "Restart lost stone piles.");
        Require(!UnityEngine.Object.FindFirstObjectByType<ForestioAbilityNotice>().IsOpen, "Restart retained ability notice.");
        Require(UnityEngine.Object.FindFirstObjectByType<ForestioHud>().PickupHintVisible, "Restart did not restore starting pickup hint.");
        ForestioHomeEnding freshHome = UnityEngine.Object.FindFirstObjectByType<ForestioHomeEnding>();
        if (freshHome != null)
        {
            Require(!freshHome.HasStarted, "Restart retained ending cutscene state.");
            Debug.Log("FORESTIO_STAGE14_PLAYCHECK_OK: forest level route, timed home reunion and complete restart verified.");
        }
        Debug.Log("FORESTIO_STAGE13_PLAYCHECK_OK: contextual pickup hints, full route, ending delay and one-shot scene restart verified; HP, input, inventory, ability, button, door, module, branches, robot, food and projectiles reset.");
        SessionState.SetBool(RestartCheckKey, false);
        SessionState.SetBool(PortalCheckKey, false);
        SessionState.SetBool(RunningKey, false);
        EditorApplication.Exit(0);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        if (timeout == 0) timeout = EditorApplication.timeSinceStartup + 180;
        try
        {
            Require(EditorApplication.timeSinceStartup < timeout, "Level route checks timed out.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (restartStage != 0) { CheckRestart(); return; }
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
            var animation = player.GetComponent<ForestioFrameAnimation>();
            if (animation != null) CatFrames.Add(animation.CurrentFrame);
            var robotAnimation = robot.GetComponent<ForestioFrameAnimation>();
            if (robotAnimation != null) RobotFrames.Add(robotAnimation.CurrentFrame);
            Require(!health.IsDefeated, "Navigation caused defeat.");
            if (notice.IsOpen)
            {
                UnityEngine.UI.Button close = Array.Find(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None), item => item.name == "Continue");
                if (close == null) notice.TryClose();
                else if (close.interactable) ClickUiButton(close);
                return;
            }
            if (command < Commands.Count)
            {
                if (Commands[command]()) { body.linearVelocity = new Vector2(0f, body.linearVelocity.y); command++; }
                return;
            }
            if (player.GetComponent<ForestioFrameAnimation>() != null)
            {
                Require(CatFrames.Contains(4) && CatFrames.Contains(5) && CatFrames.Contains(9) && CatFrames.Contains(10)
                    && CatFrames.Contains(12) && CatFrames.Contains(14), "Cat walk, jump or throw frames did not animate.");
                Require(RobotFrames.Contains(4) && RobotFrames.Contains(7), "Robot power-down did not animate.");
                Debug.Log("FORESTIO_ANIMATION_CHECK_OK: actual walk, jump, throw and robot shutdown frames observed.");
            }
            Debug.Log("FORESTIO_STAGE11_PLAYCHECK_OK: both branches, all three piles, platform jumps, pit round trip, button, robot hit, actual ability pickup, charged gate and final landing verified.");
            if (SessionState.GetBool(PortalCheckKey, false))
            {
                ForestioLevelCompletion ending = UnityEngine.Object.FindFirstObjectByType<ForestioLevelCompletion>();
                Require(ending != null && ending.IsCompleted, "Portal route ended without completion.");
                if (SessionState.GetBool(RestartCheckKey, false))
                {
                    oldPlayerId = player.GetInstanceID();
                    restartStage = 1;
                    return;
                }
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
