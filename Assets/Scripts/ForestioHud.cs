using UnityEngine;

public sealed class ForestioHud : MonoBehaviour
{
    [SerializeField] private ForestioPlayerController player;
    [SerializeField] private ForestioPlayerHealth health;
    [SerializeField] private ForestioHitButton button;
    [SerializeField] private ForestioBranchBarrier branches;
    [SerializeField] private ForestioAbilityNotice notice;
    [SerializeField] private ForestioLevelCompletion completion;
    private GUIStyle label;
    private GUIStyle small;
    private GUIStyle heading;
    private GUIStyle centered;

    public bool PickupHintVisible => player != null && player.CanPickUpStone && Time.timeScale > 0f
        && (notice == null || !notice.IsOpen) && (completion == null || !completion.IsCompleted);
    public string ObjectiveText
    {
        get
        {
            if (branches != null && branches.IsBroken) return "Пройди ліворуч до м’ятного телепорту.";
            if (player != null && player.HasChargedThrow) return "Повернися ліворуч і зруйнуй сухі гілки.";
            if (button != null && button.IsActivated) return "Пройди крізь двері та знайди жовтий модуль.";
            return "Знайди кнопку на верхній гілці праворуч.";
        }
    }

    private void OnGUI()
    {
        if (player == null || health == null || Time.timeScale == 0f
            || (notice != null && notice.IsOpen) || (completion != null && completion.IsCompleted)) return;
        EnsureStyles();
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        try
        {
            float scale = Mathf.Max(0.1f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            Panel(new Rect(18f, 18f, 300f, 112f));
            GUI.Label(new Rect(32f, 27f, 130f, 26f), "Здоров’я", heading);
            for (int i = 0; i < ForestioPlayerHealth.MaxHealth; i++)
            {
                GUI.color = i < health.CurrentHealth ? new Color(0.91f, 0.43f, 0.61f) : new Color(0.77f, 0.73f, 0.79f);
                GUI.DrawTexture(new Rect(32f + i * 35f, 61f, 28f, 15f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            GUI.Label(new Rect(151f, 54f, 130f, 28f), health.CurrentHealth + " / 3 HP", label);
            GUI.Label(new Rect(32f, 88f, 272f, 27f), player.HasStone ? "Камінець у пащі" : "Камінця немає", small);
            Panel(new Rect(width - 458f, 18f, 440f, 112f));
            GUI.Label(new Rect(width - 444f, 27f, 412f, 26f), "Шлях додому", heading);
            GUI.Label(new Rect(width - 444f, 59f, 412f, 57f), ObjectiveText, label);

            string controls = "A/D або ←/→ — рух     Space — стрибок     E — взяти камінець     ЛКМ/F — кидок";
            if (player.HasChargedThrow) controls += "\nЗаряджений кидок: утримуй ЛКМ/F 0,7 с і відпусти.";
            Panel(new Rect(18f, height - 72f, width - 36f, 54f));
            GUI.Label(new Rect(31f, height - 68f, width - 62f, 48f), controls, small);
            string context = ContextHint();
            if (!string.IsNullOrEmpty(context))
            {
                Panel(new Rect((width - 700f) / 2f, height - 146f, 700f, 60f));
                GUI.Label(new Rect((width - 674f) / 2f, height - 140f, 674f, 48f), context, centered);
            }
            if (PickupHintVisible && Camera.main != null)
            {
                Transform pile = player.NearbyStonePile;
                if (pile != null)
                {
                    Vector3 screen = Camera.main.WorldToScreenPoint(pile.position + Vector3.up * 0.95f);
                    if (screen.z > 0f)
                    {
                        float x = Mathf.Clamp(screen.x / scale - 110f, 18f, width - 238f);
                        float y = Mathf.Clamp((Screen.height - screen.y) / scale, 145f, height - 190f);
                        Panel(new Rect(x, y, 220f, 36f));
                        GUI.Label(new Rect(x + 5f, y + 2f, 210f, 32f), "E — взяти камінець", centered);
                    }
                }
            }
        }
        finally { GUI.matrix = previousMatrix; GUI.color = previousColor; }
    }

    private string ContextHint()
    {
        Vector3 position = player.transform.position;
        if (branches != null && !branches.IsBroken && Time.time - branches.LastNormalHitTime < 2.5f)
            return "Звичайного кидка недостатньо. Знайди модуль зарядженого кидка.";
        if (branches != null && !branches.IsBroken && Vector2.Distance(position, branches.transform.position - Vector3.up * 2.5f) < 3f)
            return player.HasChargedThrow ? "Дивись у бік гілок. Утримуй ЛКМ/F 0,7 с і відпусти." : "Сухі гілки перекривають шлях. Досліди іншу гілку рівня.";
        if (button != null && !button.IsActivated && Vector2.Distance(position, button.transform.position) < 3f)
            return "Влуч камінцем у рожеву кнопку, щоб відкрити двері.";
        if (position.x > 9.5f && position.x < 15.5f && position.y < 0f)
            return "Перестрибни яму. Камінцем можна вимкнути робота на іншому боці.";
        if (position.x > -10.6f && position.x < -7f && position.y < -1f)
            return "Space — перестрибни рожеві шипи. Дотик забирає 1 HP.";
        if (position.x < -12f && position.y < 0f && !player.HasChargedThrow)
            return "Котик прокинувся всередині старої гри. Допоможи йому знайти шлях додому!";
        return null;
    }

    private void EnsureStyles()
    {
        if (label != null) return;
        Color ink = new Color(0.23f, 0.16f, 0.29f);
        label = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
        label.normal.textColor = ink;
        small = new GUIStyle(label) { fontSize = 17 };
        heading = new GUIStyle(label) { fontSize = 20, fontStyle = FontStyle.Bold };
        centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
    }
    private static void Panel(Rect rect)
    {
        GUI.color = new Color(1f, 0.96f, 0.98f, 0.94f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
