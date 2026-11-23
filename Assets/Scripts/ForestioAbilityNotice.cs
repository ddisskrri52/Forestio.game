using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ForestioAbilityNotice : MonoBehaviour
{
    private const float CloseDelay = 0.5f;
    private ForestioPlayerController player;
    private float openedAt;
    private float previousTimeScale = 1f;
    private Texture2D overlayTexture;
    private Texture2D panelTexture;
    private Texture2D buttonTexture;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle footerStyle;
    private GUIStyle buttonStyle;

    public bool IsOpen { get; private set; }

    public bool Show(ForestioPlayerController target)
    {
        if (IsOpen || target == null)
            return false;

        player = target;
        player.ClearPendingInput();
        previousTimeScale = Time.timeScale;
        openedAt = Time.unscaledTime;
        IsOpen = true;
        Time.timeScale = 0f;
        return true;
    }

    private void Update()
    {
        if (!IsOpen || !Application.isFocused)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            TryClose();
    }

    public bool TryClose()
    {
        if (!IsOpen || Time.unscaledTime - openedAt < CloseDelay)
            return false;

        ResumeGame();
        return true;
    }

    private void ResumeGame()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        Time.timeScale = previousTimeScale;
        if (player != null)
            player.ClearPendingInput();
        player = null;
    }

    private void OnGUI()
    {
        if (!IsOpen)
            return;

        EnsureStyles();
        Matrix4x4 oldMatrix = GUI.matrix;
        bool oldEnabled = GUI.enabled;
        try
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTexture);
            float scale = Mathf.Max(0.1f, Mathf.Min(Screen.width / 800f, Screen.height / 450f));
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            Rect panel = new Rect((Screen.width / scale - 620f) / 2f, (Screen.height / scale - 290f) / 2f, 620f, 290f);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 32f, panel.y + 25f, 556f, 45f), "Заряджений кидок відкрито!", titleStyle);
            GUI.Label(new Rect(panel.x + 40f, panel.y + 85f, 540f, 90f),
                "Утримуй ЛКМ або F протягом 0,7 с і відпусти.\nЗаряджений камінець руйнує сухі гілки.", bodyStyle);
            bool canClose = Time.unscaledTime - openedAt >= CloseDelay;
            GUI.Label(new Rect(panel.x + 40f, panel.y + 173f, 540f, 27f),
                canClose ? "Продовжити — E або Space" : "Нову здібність отримано…", footerStyle);
            GUI.enabled = canClose;
            if (GUI.Button(new Rect(panel.x + 160f, panel.y + 218f, 300f, 45f), "Продовжити", buttonStyle))
                TryClose();
        }
        finally
        {
            GUI.matrix = oldMatrix;
            GUI.enabled = oldEnabled;
        }
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        overlayTexture = MakeTexture(new Color(0.08f, 0.04f, 0.13f, 0.68f));
        panelTexture = MakeTexture(new Color(1f, 0.95f, 0.96f));
        buttonTexture = MakeTexture(new Color(0.96f, 0.7f, 0.81f));
        Color ink = new Color(0.2f, 0.12f, 0.25f);
        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        titleStyle.normal.textColor = ink;
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        bodyStyle.normal.textColor = ink;
        footerStyle = new GUIStyle(bodyStyle) { fontSize = 17 };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        buttonStyle.normal.background = buttonTexture;
        buttonStyle.normal.textColor = ink;
        buttonStyle.hover.background = buttonTexture;
        buttonStyle.hover.textColor = ink;
        buttonStyle.active.background = buttonTexture;
        buttonStyle.active.textColor = ink;
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnDisable()
    {
        ResumeGame();
    }

    private void OnDestroy()
    {
        ResumeGame();
        if (overlayTexture != null) Destroy(overlayTexture);
        if (panelTexture != null) Destroy(panelTexture);
        if (buttonTexture != null) Destroy(buttonTexture);
    }
}
