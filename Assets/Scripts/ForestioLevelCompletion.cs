using UnityEngine;

public sealed class ForestioLevelCompletion : MonoBehaviour
{
    private ForestioPlayerController player;
    private bool playerWasEnabled;
    private float previousTimeScale;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    public bool IsCompleted { get; private set; }

    public bool TryComplete(ForestioPlayerController target)
    {
        if (IsCompleted || target == null || Time.timeScale <= 0f) return false;
        ForestioPlayerHealth health = target.GetComponent<ForestioPlayerHealth>();
        if (health == null || health.IsDefeated || health.IsReturningFromHazard) return false;
        player = target;
        playerWasEnabled = player.enabled;
        previousTimeScale = Time.timeScale;
        player.ClearPendingInput();
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        player.enabled = false;
        IsCompleted = true;
        Time.timeScale = 0f;
        return true;
    }

    private void OnGUI()
    {
        if (!IsCompleted) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            titleStyle.normal.textColor = new Color(0.27f, 0.17f, 0.32f);
            bodyStyle = new GUIStyle(titleStyle) { fontSize = 22, fontStyle = FontStyle.Normal };
        }
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        try
        {
            GUI.depth = -50;
            GUI.color = new Color(0.1f, 0.06f, 0.16f, 0.7f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            float scale = Mathf.Max(0.1f, Mathf.Min(Screen.width / 800f, Screen.height / 450f));
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            Rect panel = new Rect((Screen.width / scale - 580f) / 2f, (Screen.height / scale - 230f) / 2f, 580f, 230f);
            GUI.color = new Color(1f, 0.95f, 0.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(0.65f, 0.9f, 0.78f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 7f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 25f, panel.y + 28f, 530f, 48f), "Шлях додому знайдено!", titleStyle);
            GUI.Label(new Rect(panel.x + 35f, panel.y + 93f, 510f, 78f), "Ти знову вдома, зі своєю родиною!", bodyStyle);
            GUI.Label(new Rect(panel.x + 35f, panel.y + 177f, 510f, 35f), "Проходження завершено.", bodyStyle);
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.depth = previousDepth;
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
