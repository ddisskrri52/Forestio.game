using UnityEngine;
using UnityEngine.UI;

public sealed class ForestioHud : MonoBehaviour
{
    [SerializeField] private ForestioPlayerController player;
    [SerializeField] private ForestioPlayerHealth health;
    [SerializeField] private ForestioHitButton button;
    [SerializeField] private ForestioBranchBarrier branches;
    [SerializeField] private ForestioAbilityNotice notice;
    [SerializeField] private ForestioLevelCompletion completion;
    [SerializeField] private Sprite heartIcon;
    [SerializeField] private Sprite stoneIcon;
    private RectTransform canvas;
    private Image healthPanel, goalPanel, hintPanel, stone;
    private readonly Image[] hearts = new Image[3];
    private Text goal, hint;
    private Vector3 initialPosition;
    private Rigidbody2D body;
    private bool moved, jumped, threw, previouslyHeldStone;
    public bool PickupHintVisible => player != null && player.CanPickUpStone && Time.timeScale > 0f
        && (notice == null || !notice.IsOpen) && (completion == null || !completion.IsCompleted);
    public string ObjectiveText => branches != null && branches.IsBroken ? "До телепорту ←"
        : player != null && player.HasChargedThrow ? "Повернися до завалу ←"
        : button != null && button.IsActivated ? "Знайди жовтий модуль →" : "Знайди кнопку ↑ →";
    private void Start()
    {
        initialPosition = player.transform.position;
        body = player.GetComponent<Rigidbody2D>();
        canvas = ForestioUi.Root(transform, "CompactHud", 500);
        healthPanel = ForestioUi.Panel(canvas, "Health", ForestioUi.Cream);
        for (int i = 0; i < 3; i++) hearts[i] = ForestioUi.Picture(canvas, "Heart" + i, heartIcon, Color.white);
        stone = ForestioUi.Picture(canvas, "HeldStone", stoneIcon, Color.white);
        goalPanel = ForestioUi.Panel(canvas, "Goal", ForestioUi.Cream);
        goal = ForestioUi.Label(canvas, "Objective", 20);
        hintPanel = ForestioUi.Panel(canvas, "SingleHint", ForestioUi.Cream);
        hint = ForestioUi.Label(canvas, "HintText", 19);
    }
    private void LateUpdate()
    {
        if (canvas == null) return;
        bool visible = Time.timeScale > 0f && (notice == null || !notice.IsOpen) && (completion == null || !completion.IsCompleted);
        canvas.gameObject.SetActive(visible);
        if (!visible) return;
        moved |= Mathf.Abs(player.transform.position.x - initialPosition.x) > 0.7f;
        jumped |= body != null && body.linearVelocity.y > 1f;
        threw |= previouslyHeldStone && !player.HasStone && !health.IsDefeated;
        previouslyHeldStone = player.HasStone;
        float s = ForestioUi.Scale;
        ForestioUi.Place(healthPanel.rectTransform, new Rect(14f * s, 14f * s, 150f * s, 44f * s));
        for (int i = 0; i < 3; i++)
        {
            ForestioUi.Place(hearts[i].rectTransform, new Rect((24f + i * 30f) * s, 23f * s, 25f * s, 25f * s));
            hearts[i].color = i < health.CurrentHealth ? Color.white : new Color(0.5f, 0.45f, 0.55f, 0.28f);
        }
        ForestioUi.Place(stone.rectTransform, new Rect(123f * s, 26f * s, 20f * s, 20f * s));
        stone.color = player.HasStone ? Color.white : new Color(0.6f, 0.55f, 0.65f, 0.25f);
        Rect goalRect = new Rect(Screen.width - 284f * s, 14f * s, 270f * s, 44f * s);
        ForestioUi.Place(goalPanel.rectTransform, goalRect); ForestioUi.Place(goal.rectTransform, goalRect);
        goal.fontSize = Mathf.Max(15, Mathf.RoundToInt(20f * s)); goal.text = ObjectiveText;
        string active = ActiveHint();
        bool showHint = !string.IsNullOrEmpty(active);
        hintPanel.gameObject.SetActive(showHint); hint.gameObject.SetActive(showHint);
        if (!showHint) return;
        hint.fontSize = Mathf.Max(15, Mathf.RoundToInt(19f * s)); hint.text = active;
        float width = Mathf.Min(Screen.width - 28f * s, hint.preferredWidth + 34f * s);
        hint.rectTransform.sizeDelta = new Vector2(width - 24f * s, 100f);
        float height = hint.preferredHeight + 18f * s;
        Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 14f * s, width, height);
        ForestioUi.Place(hintPanel.rectTransform, panel);
        ForestioUi.Place(hint.rectTransform, new Rect(panel.x + 12f * s, panel.y + 7f * s, panel.width - 24f * s, panel.height - 14f * s));
    }
    private string ActiveHint()
    {
        if (branches != null && !branches.IsBroken && Time.time - branches.LastNormalHitTime < 2.5f) return "Потрібен заряджений кидок. Знайди модуль.";
        if (PickupHintVisible) return "E — взяти камінець";
        if (!moved) return "Рух — A/D або ←/→";
        Vector3 p = player.transform.position;
        if (!jumped && p.x < -10.5f) return "Стрибок — Space";
        if (branches != null && !branches.IsBroken && Vector2.Distance(p, branches.transform.position - Vector3.up * 2.5f) < 3f)
            return player.HasChargedThrow ? "Утримуй ЛКМ/F 0,7 с і відпусти в бік колод." : "Шлях закритий. Досліди гілку праворуч.";
        if (button != null && !button.IsActivated && Vector2.Distance(p, button.transform.position) < 3f) return "ЛКМ/F — влуч камінцем у кнопку.";
        if (p.x > -10.6f && p.x < -7f && p.y < -1f) return "Space — перестрибни шипи.";
        if (p.x > 10f && p.x < 15f && p.y < 0f) return "Space — перестрибни яму.";
        if (player.HasStone && !threw && p.x < -10.5f) return "Кинути камінець — ЛКМ або F";
        return null;
    }
}
