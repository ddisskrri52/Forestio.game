using UnityEngine;
using UnityEngine.UI;

public sealed class ForestioHomeEnding : MonoBehaviour
{
    public const float Duration = 4.8f;
    [SerializeField] private Texture2D room;
    [SerializeField] private Sprite walkingCat;
    [SerializeField] private Sprite[] walkingFrames;
    [SerializeField] private Sprite restingCat;
    private float startedAt;
    private RectTransform canvas, roomGroup;
    private Image matte, fade, walking, resting;
    private RawImage background;
    private Text caption, shortcut;
    private Button replay;
    private ForestioLevelCompletion completion;
    public bool HasStarted { get; private set; }
    public float Elapsed => HasStarted ? Time.unscaledTime - startedAt : 0f;
    public bool IsReady => HasStarted && Elapsed >= Duration;
    public void Begin()
    {
        HasStarted = true; startedAt = Time.unscaledTime;
        completion = GetComponent<ForestioLevelCompletion>();
        canvas = ForestioUi.Root(transform, "HomeReunion", 900);
        matte = ForestioUi.Picture(canvas, "Matte", null, new Color(0.19f, 0.12f, 0.21f));
        roomGroup = ForestioUi.Element(canvas, "RoomFrame"); roomGroup.gameObject.AddComponent<RectMask2D>();
        background = ForestioUi.Element(roomGroup, "FamilyRoom").gameObject.AddComponent<RawImage>();
        background.texture = room; background.raycastTarget = false;
        walking = ForestioUi.Picture(roomGroup, "ReturningCat", walkingCat, Color.white);
        resting = ForestioUi.Picture(roomGroup, "RestingCat", restingCat, Color.white);
        caption = ForestioUi.Label(canvas, "HomeMessage", 25, TextAnchor.MiddleCenter, true);
        caption.text = "Ти знову вдома, зі своєю родиною!"; caption.color = new Color(1f, 0.9f, 0.91f);
        replay = ForestioUi.Button(canvas, "PlayAgain", "Грати знову");
        replay.onClick.AddListener(() => completion.TryRestart());
        shortcut = ForestioUi.Label(canvas, "RestartKey", 16, TextAnchor.MiddleLeft);
        shortcut.text = "або R"; shortcut.color = new Color(0.91f, 0.84f, 0.92f);
        fade = ForestioUi.Picture(canvas, "Transition", null, new Color(0.1f, 0.05f, 0.14f));
        Update();
    }
    private void Update()
    {
        if (!HasStarted || canvas == null) return;
        float s = ForestioUi.Scale, t = Elapsed;
        Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
        ForestioUi.Place(matte.rectTransform, screen); ForestioUi.Place(fade.rectTransform, screen);
        bool showRoom = t >= 0.4f;
        matte.gameObject.SetActive(showRoom); roomGroup.gameObject.SetActive(showRoom);
        float aspect = room != null ? room.width / (float)room.height : 16f / 9f;
        float height = Mathf.Min(Screen.height - 112f * s, Screen.width / aspect), width = height * aspect;
        ForestioUi.Place(roomGroup, new Rect((Screen.width - width) * 0.5f, (Screen.height - 112f * s - height) * 0.5f, width, height));
        ForestioUi.Place(background.rectTransform, new Rect(0f, 0f, width, height));
        float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.65f) / 2.7f));
        float settle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 3.4f) / 0.55f));
        float walkingWidth = width * 0.14f, walkingHeight = walkingWidth * walkingCat.rect.height / walkingCat.rect.width;
        float bob = Mathf.Sin(t * 14f) * 1.5f * s * (1f - progress);
        ForestioUi.Place(walking.rectTransform, new Rect(width * Mathf.Lerp(-0.13f, 0.67f, progress) - walkingWidth * 0.5f, height * 0.85f - walkingHeight + bob, walkingWidth, walkingHeight));
        if (walkingFrames != null && walkingFrames.Length > 0)
            walking.sprite = walkingFrames[(int)(t * 10f) % walkingFrames.Length];
        walking.color = new Color(1f, 1f, 1f, 1f - settle);
        float restingWidth = width * 0.11f, restingHeight = restingWidth * restingCat.rect.height / restingCat.rect.width;
        ForestioUi.Place(resting.rectTransform, new Rect(width * 0.67f - restingWidth * 0.5f, height * 0.85f - restingHeight, restingWidth, restingHeight));
        resting.color = new Color(1f, 1f, 1f, settle);
        float alpha = t < 0.4f ? Mathf.SmoothStep(0f, 1f, t / 0.4f) : 1f - Mathf.Clamp01((t - 0.4f) / 0.45f);
        fade.color = new Color(0.1f, 0.05f, 0.14f, alpha); fade.gameObject.SetActive(alpha > 0f);
        caption.gameObject.SetActive(IsReady); replay.gameObject.SetActive(IsReady); shortcut.gameObject.SetActive(IsReady);
        ForestioUi.Place(caption.rectTransform, new Rect(15f * s, Screen.height - 108f * s, Screen.width - 30f * s, 40f * s));
        Rect button = new Rect((Screen.width - 220f * s) * 0.5f, Screen.height - 58f * s, 220f * s, 43f * s);
        ForestioUi.Place(replay.GetComponent<RectTransform>(), button);
        ForestioUi.Place(shortcut.rectTransform, new Rect(button.xMax + 14f * s, button.y, 95f * s, button.height));
        replay.interactable = completion.CanRestart;
    }
}
