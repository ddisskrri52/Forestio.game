using UnityEngine;

public static class ForestioGui
{
    private static Texture2D pill;
    public static float Scale => Mathf.Clamp(Screen.height / 720f, 0.75f, 1.5f);

    public static GUIStyle Text(int size, TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
            fontSize = Mathf.Max(15, Mathf.RoundToInt(size * Scale)),
            alignment = alignment,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            wordWrap = true
        };
        style.normal.textColor = new Color(0.23f, 0.14f, 0.28f);
        return style;
    }

    public static void Panel(Rect rect, Color color)
    {
        if (pill == null)
        {
            pill = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - 18f, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - 18f, 0f);
                    pill.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(13.5f - Mathf.Sqrt(dx * dx + dy * dy))));
                }
            pill.Apply();
        }
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pill);
        GUI.color = previous;
    }

    public static void Sprite(Rect rect, Sprite sprite, Color color)
    {
        if (sprite == null) return;
        Rect pixels = sprite.textureRect;
        Rect uv = new Rect(pixels.x / sprite.texture.width, pixels.y / sprite.texture.height,
            pixels.width / sprite.texture.width, pixels.height / sprite.texture.height);
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
        GUI.color = previous;
    }
}
