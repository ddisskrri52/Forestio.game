using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class ForestioLevelCompletion : MonoBehaviour
{
    [SerializeField] private ForestioHomeEnding homeEnding;
    private ForestioPlayerController player;
    private bool playerWasEnabled;
    private float previousTimeScale;
    private bool restartRequested;
    private float restartReadyAt;
    public bool CanRestart => isActiveAndEnabled && IsCompleted && !restartRequested && Time.unscaledTime >= restartReadyAt
        && (homeEnding == null || homeEnding.IsReady);
    public bool IsCompleted { get; private set; }

    public bool TryComplete(ForestioPlayerController target)
    {
        if (!isActiveAndEnabled || IsCompleted || target == null || Time.timeScale <= 0f) return false;
        ForestioPlayerHealth health = target.GetComponent<ForestioPlayerHealth>();
        if (health == null || health.IsDefeated || health.IsReturningFromHazard) return false;
        player = target;
        playerWasEnabled = player.enabled;
        previousTimeScale = Time.timeScale;
        player.ClearPendingInput();
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        player.enabled = false;
        IsCompleted = true;
        if (homeEnding != null) homeEnding.Begin();
        restartReadyAt = Time.unscaledTime + (homeEnding != null ? ForestioHomeEnding.Duration : 0.5f);
        Time.timeScale = 0f;
        return true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (Application.isFocused && keyboard != null && keyboard.rKey.wasPressedThisFrame)
            TryRestart();
    }

    public bool TryRestart()
    {
        if (!CanRestart) return false;
        int index = gameObject.scene.buildIndex;
        if (index < 0)
        {
            Debug.LogError("Add the playable scene to the build scene list before restarting.", this);
            return false;
        }
        restartRequested = true;
        if (player != null) player.ClearPendingInput();
        // Scene loading works while paused. OnDisable restores time as the old scene unloads.
        AsyncOperation loading = SceneManager.LoadSceneAsync(index, LoadSceneMode.Single);
        if (loading != null) return true;
        restartRequested = false;
        return false;
    }

    private void OnGUI()
    {
        if (!IsCompleted || homeEnding != null) return;
        Color previousColor = GUI.color;
        Matrix4x4 previousMatrix = GUI.matrix;
        int previousDepth = GUI.depth;
        bool previousEnabled = GUI.enabled;
        try
        {
            GUI.depth = -50;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = new Color(0.19f, 0.12f, 0.21f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float s = ForestioGui.Scale;
            GUIStyle caption = ForestioGui.Text(25, TextAnchor.MiddleCenter, true);
            caption.normal.textColor = new Color(1f, 0.9f, 0.91f);
            GUI.Label(new Rect(15f * s, Screen.height - 108f * s, Screen.width - 30f * s, 40f * s),
                "Ти знову вдома, зі своєю родиною!", caption);
            Rect playAgain = new Rect((Screen.width - 220f * s) * 0.5f, Screen.height - 58f * s, 220f * s, 43f * s);
            ForestioGui.Panel(playAgain, new Color(0.78f, 0.95f, 0.83f));
            GUIStyle button = ForestioGui.Text(20, TextAnchor.MiddleCenter, true);
            button.normal.background = null;
            button.hover.background = null;
            button.active.background = null;
            GUI.enabled = CanRestart;
            if (GUI.Button(playAgain, restartRequested ? "Починаємо…" : "Грати знову", button)) TryRestart();
            GUIStyle shortcut = ForestioGui.Text(16, TextAnchor.MiddleLeft);
            shortcut.normal.textColor = new Color(0.91f, 0.84f, 0.92f);
            GUI.Label(new Rect(playAgain.xMax + 14f * s, playAgain.y, 95f * s, playAgain.height), "або R", shortcut);
        }
        finally
        {
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
            GUI.depth = previousDepth;
            GUI.enabled = previousEnabled;
        }
    }

    private void OnDisable()
    {
        if (!IsCompleted) return;
        Time.timeScale = previousTimeScale;
        if (player != null)
        {
            player.ClearPendingInput();
            player.enabled = playerWasEnabled;
        }
        IsCompleted = false;
        player = null;
    }
}
