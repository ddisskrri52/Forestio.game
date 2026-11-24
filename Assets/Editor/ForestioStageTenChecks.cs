using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ForestioStageTenChecks
{
    private const string RunningKey = "Forestio.Stage10.Checks";
    private static int step;
    private static float nextAt;
    private static double timeout;
    private static ForestioPlayerHealth health;
    private static ForestioPlayerController player;
    private static ForestioSafePoint safePoint;
    private static Rigidbody2D body;
    private static Vector3 expectedSafe;
    private static Vector3 start;

    static ForestioStageTenChecks() => EditorApplication.update += Tick;

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

    private static void Place(Vector3 position)
    {
        player.transform.position = position;
        body.position = position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        if (timeout == 0) timeout = EditorApplication.timeSinceStartup + 90;
        try
        {
            Require(EditorApplication.timeSinceStartup < timeout, "Hazard checks timed out.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || Time.time < nextAt) return;
            switch (step)
            {
                case 0:
                    health = UnityEngine.Object.FindFirstObjectByType<ForestioPlayerHealth>();
                    if (health == null) return;
                    player = health.GetComponent<ForestioPlayerController>();
                    safePoint = health.GetComponent<ForestioSafePoint>();
                    body = health.GetComponent<Rigidbody2D>();
                    start = safePoint.SafePosition;
                    Require(safePoint != null && health.CurrentHealth == 3, "Initial hazard state incorrect.");
                    Place(GameObject.Find("StonePile").transform.position + Vector3.up * 0.2f);
                    player.SendMessage("TryPickUpStone", SendMessageOptions.RequireReceiver);
                    Require(player.HasStone, "Test stone pickup failed.");
                    nextAt = Time.time + 0.8f;
                    step++;
                    break;
                case 1:
                    expectedSafe = safePoint.SafePosition;
                    Require(Mathf.Abs(expectedSafe.x + 2f) < 0.05f, "Safe position did not update on stable ground.");
                    // Falling through the real opening, not invoking the hazard directly.
                    Place(new Vector3(-6.4f, -2f, 0f));
                    nextAt = Time.time + 0.9f;
                    step++;
                    break;
                case 2:
                    Require(health.CurrentHealth == 2 && !health.IsReturningFromHazard, "Pit did not remove one HP and finish returning.");
                    Require(Mathf.Abs(player.transform.position.x - expectedSafe.x) < 0.05f, "Pit return missed safe position.");
                    Require(player.HasStone, "Nonfatal pit removed the held stone.");
                    Require(health.IsInvulnerable, "Invulnerability was not granted after return.");
                    Place(new Vector3(-6.4f, -4f, 0f));
                    nextAt = Time.time + 0.45f;
                    step++;
                    break;
                case 3:
                    Require(health.CurrentHealth == 2 && !health.IsReturningFromHazard, "Invulnerable pit cost HP or failed to return.");
                    Require(Mathf.Abs(player.transform.position.x - expectedSafe.x) < 0.05f, "Invulnerable pit return missed safe position.");
                    nextAt = Time.time + 1.1f;
                    step++;
                    break;
                case 4:
                    Place(new Vector3(3.7f, -2f, 0f));
                    nextAt = Time.time + 0.45f;
                    step++;
                    break;
                case 5:
                    Require(health.CurrentHealth == 1 && !health.IsReturningFromHazard && player.HasStone, "Spikes did not preserve stone or remove exactly one HP.");
                    Require(Mathf.Abs(player.transform.position.x - expectedSafe.x) < 0.05f, "Spikes overwrote safe position.");
                    nextAt = Time.time + 1.1f;
                    step++;
                    break;
                case 6:
                    Place(new Vector3(3.7f, -2f, 0f));
                    nextAt = Time.time + 0.7f;
                    step++;
                    break;
                case 7:
                    Require(health.CurrentHealth == 3 && !health.IsDefeated && !player.HasStone, "Lethal spikes failed to use full defeat reset.");
                    Require(Mathf.Abs(player.transform.position.x - start.x) < 0.05f, "Defeat did not return to level start.");
                    Require(Mathf.Abs(safePoint.SafePosition.x - start.x) < 0.05f, "Defeat left stale safe position.");
                    Debug.Log("FORESTIO_STAGE10_PLAYCHECK_OK: real pit/spike triggers, safe-point update, held stone, invulnerable return and lethal defeat verified.");
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
