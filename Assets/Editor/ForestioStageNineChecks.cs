using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ForestioStageNineChecks
{
    private const string RunningKey = "Forestio.Stage9.Checks";
    private static int step;
    private static float nextAt;
    private static double timeout;
    private static ForestioPlayerHealth health;
    private static ForestioPlayerController player;
    private static ForestioRobotController robot;
    private static ForestioHitButton button;
    private static ForestioBranchBarrier barrier;
    private static ForestioFood food;
    private static Vector3 initialPlayerPosition;

    static ForestioStageNineChecks() => EditorApplication.update += Tick;

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ForestTest.unity", OpenSceneMode.Single);
        SessionState.SetBool(RunningKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        if (timeout == 0) timeout = EditorApplication.timeSinceStartup + 90;
        try
        {
            Require(EditorApplication.timeSinceStartup < timeout, "Play-mode checks timed out.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (Time.time < nextAt) return;
            switch (step)
            {
                case 0:
                    health = UnityEngine.Object.FindFirstObjectByType<ForestioPlayerHealth>();
                    if (health == null) return;
                    player = health.GetComponent<ForestioPlayerController>();
                    robot = UnityEngine.Object.FindFirstObjectByType<ForestioRobotController>();
                    button = UnityEngine.Object.FindFirstObjectByType<ForestioHitButton>();
                    barrier = UnityEngine.Object.FindFirstObjectByType<ForestioBranchBarrier>();
                    food = UnityEngine.Object.FindFirstObjectByType<ForestioFood>();
                    initialPlayerPosition = player.transform.position;
                    Require(health.CurrentHealth == 3, "Initial HP must be 3.");
                    player.UnlockChargedThrow();
                    robot.DisableRobot();
                    Require(health.TryTakeDamage(Vector2.left * 10f), "First damage rejected.");
                    Require(health.CurrentHealth == 2 && health.MovementLocked, "Damage or knockback lock failed.");
                    Require(!health.TryTakeDamage(Vector2.zero) && health.CurrentHealth == 2, "Invulnerability failed.");
                    player.GetComponent<Rigidbody2D>().position = food.transform.position;
                    player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                    nextAt = Time.time + 0.15f;
                    step++;
                    break;
                case 1:
                    Require(health.CurrentHealth == 3 && food.IsCollected && !food.gameObject.activeSelf, "Food trigger did not heal and hide.");
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StoneProjectile.prefab");
                    var gateShot = UnityEngine.Object.Instantiate(prefab, button.transform.position, Quaternion.identity).GetComponent<ForestioStoneProjectile>();
                    gateShot.Launch(1f, 12f, 9f);
                    var branchShot = UnityEngine.Object.Instantiate(prefab, new Vector3(-20f, 10f, 0f), Quaternion.identity).GetComponent<ForestioStoneProjectile>();
                    branchShot.Launch(1f, 16f, 16f, null, true);
                    Require(barrier.TryBreak(branchShot), "Charged shot failed to break branches.");
                    nextAt = Time.time + 1.05f;
                    step++;
                    break;
                case 2:
                    Require(button.IsActivated, "Button was not activated by projectile trigger.");
                    Require(health.TryTakeDamage(Vector2.left * 10f) && health.CurrentHealth == 2, "Damage after invulnerability failed.");
                    nextAt = Time.time + 1.05f;
                    step++;
                    break;
                case 3:
                    Require(health.TryTakeDamage(Vector2.left * 10f) && health.CurrentHealth == 1, "Second HP loss failed.");
                    nextAt = Time.time + 1.05f;
                    step++;
                    break;
                case 4:
                    var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StoneProjectile.prefab");
                    // Leave a live projectile in the scene to check defeat cleanup.
                    UnityEngine.Object.Instantiate(projectilePrefab, new Vector3(0f, 20f, 0f), Quaternion.identity);
                    Require(health.TryTakeDamage(Vector2.zero) && health.IsDefeated && health.CurrentHealth == 0, "Defeat did not start.");
                    nextAt = Time.time + 0.6f;
                    step++;
                    break;
                case 5:
                    Require(!health.IsDefeated && health.CurrentHealth == 3 && !player.HasStone, "Respawn state incorrect.");
                    Require(Mathf.Abs(player.transform.position.x - initialPlayerPosition.x) < 0.05f, "Cat did not return to start.");
                    Require(player.HasChargedThrow && barrier.IsBroken && !barrier.gameObject.activeSelf, "Run progress was lost.");
                    Require(!robot.IsDisabled && robot.GetComponent<Collider2D>().enabled, "Robot was not restored.");
                    Require(!button.IsActivated && GameObject.Find("Door") != null, "Button and door were not reset.");
                    Require(!food.IsCollected && food.gameObject.activeSelf, "Food was not restored.");
                    Require(UnityEngine.Object.FindObjectsByType<ForestioStoneProjectile>(FindObjectsSortMode.None).Length == 0, "Projectiles survived defeat.");
                    Debug.Log("FORESTIO_STAGE9_PLAYCHECK_OK: damage, invulnerability, food, defeat, reset and persistent ability/barrier verified.");
                    SessionState.SetBool(RunningKey, false);
                    EditorApplication.Exit(0);
                    break;
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            SessionState.SetBool(RunningKey, false);
            EditorApplication.Exit(1);
        }
    }
}
