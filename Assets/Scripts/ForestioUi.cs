using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class ForestioUi
{
    private static Sprite rounded;
    private static Font font;
    public static float Scale => Mathf.Clamp(Screen.height / 720f, 0.75f, 1.5f);
    public static readonly Color Ink = new Color(0.23f, 0.14f, 0.28f);
    public static readonly Color Cream = new Color(1f, 0.97f, 0.98f, 0.94f);
    public static RectTransform Root(Transform parent, string name, int order)
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem));
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        RectTransform root = Element(parent, name);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1f;
        canvas.overrideSorting = true;
        canvas.sortingOrder = order;
        root.gameObject.AddComponent<GraphicRaycaster>();
        return root;
    }
    public static RectTransform Element(Transform parent, string name)
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        return item.GetComponent<RectTransform>();
    }
    public static void Place(RectTransform item, Rect rect)
    {
        item.anchorMin = item.anchorMax = item.pivot = new Vector2(0f, 1f);
        item.anchoredPosition = new Vector2(rect.x, -rect.y);
        item.sizeDelta = rect.size;
    }
    public static Image Picture(Transform parent, string name, Sprite sprite, Color color)
    {
        Image image = Element(parent, name).gameObject.AddComponent<Image>();
        image.sprite = sprite; image.color = color; image.raycastTarget = false;
        return image;
    }
    public static Image Panel(Transform parent, string name, Color color)
    {
        if (rounded == null)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - 18f, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - 18f, 0f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(13.5f - Mathf.Sqrt(dx * dx + dy * dy))));
                }
            texture.Apply();
            rounded = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect, Vector4.one * 16f);
            rounded.hideFlags = HideFlags.HideAndDontSave;
        }
        Image image = Picture(parent, name, rounded, color);
        image.type = Image.Type.Sliced;
        return image;
    }
    public static Text Label(Transform parent, string name, int size, TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text text = Element(parent, name).gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = Mathf.Max(15, Mathf.RoundToInt(size * Scale));
        text.alignment = alignment; text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.color = Ink; text.supportRichText = false; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
    public static Button Button(Transform parent, string name, string caption)
    {
        Image background = Panel(parent, name, new Color(0.77f, 0.94f, 0.83f));
        background.raycastTarget = true;
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        Text text = Label(background.transform, "Caption", 20, TextAnchor.MiddleCenter, true);
        text.text = caption;
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }
}
