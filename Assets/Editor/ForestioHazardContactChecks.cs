using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ForestioHazardContactChecks
{
    private const string Key = "Forestio.HazardContactChecks";
    private static ForestioPlayerHealth health;
    private static Rigidbody2D body;
    private static ForestioPlayerController player;
    private static bool initialized;
    private static bool sawTwoHp;
    private static bool sawOneHp;
    private static bool sawDefeat;
    private static float startedAt;
    private static double timeout;
    static ForestioHazardContactChecks() => EditorApplication.update += Tick;
    public static void RunReadableLevel()
    {
        SessionState.SetBool("Forestio.HazardContactOutskirts", true);
        Run();
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(SessionState.GetBool("Forestio.HazardContactOutskirts", false) ? ForestioStageElevenSetup.LevelPath : "Assets/Scenes/ForestTest.unity", OpenSceneMode.Single);
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }
    private static void Place(float x)
    {
        player.transform.position = new Vector3(x, -2.05f, 0f);
        body.position = player.transform.position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (timeout == 0) timeout = EditorApplication.timeSinceStartup + 90;
        try
        {
            if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Contact probe timed out.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (!initialized)
            {
                health = UnityEngine.Object.FindFirstObjectByType<ForestioPlayerHealth>();
                if (health == null) return;
                player = health.GetComponent<ForestioPlayerController>();
                body = health.GetComponent<Rigidbody2D>();
                // Feed the same 5-unit/s movement as holding D; actual physics,
                // hazard triggers, safe-point tracking and health remain enabled.
                player.enabled = false;
                UnityEngine.Object.FindFirstObjectByType<ForestioRobotController>().DisableRobot();
                Place(SessionState.GetBool("Forestio.HazardContactOutskirts", false) ? -10f : 2f);
                startedAt = Time.time;
                initialized = true;
            }
            if (!health.MovementLocked)
                body.linearVelocity = new Vector2(5f, body.linearVelocity.y);
            sawTwoHp |= health.CurrentHealth == 2;
            sawOneHp |= health.CurrentHealth == 1;
            sawDefeat |= health.IsDefeated && health.CurrentHealth == 0;
            if (sawDefeat && !health.IsDefeated && health.CurrentHealth == 3)
            {
                if (!sawTwoHp || !sawOneHp) throw new Exception("Sustained spike damage skipped HP states.");
                Debug.Log("FORESTIO_SPIKES_HOLD_CHECK_OK: continuous movement into real spikes caused 3 -> 2 -> 1 -> 0 HP and respawn; invulnerability did not refresh forever.");
                SessionState.SetBool("Forestio.HazardContactOutskirts", false);
                SessionState.SetBool(Key, false);
                EditorApplication.Exit(0);
            }
            if (Time.time - startedAt > 12f)
                throw new Exception("Continuous spike contact never reached defeat. HP=" + health.CurrentHealth);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(1);
        }
    }
}
