using UnityEngine;

public sealed class ForestioWoodFragment : MonoBehaviour
{
    private float elapsed;
    private SpriteRenderer visual;

    public static void Burst(SpriteRenderer source)
    {
        if (source == null || source.sprite == null) return;
        Sprite sprite = source.sprite;
        Rect rect = sprite.rect;
        for (int i = 0; i < 4; i++)
        {
            Vector2 origin = new Vector2((i % 2) * rect.width / 2f, (i / 2) * rect.height / 2f);
            Rect part = new Rect(rect.position + origin, rect.size / 2f);
            var go = new GameObject("WoodFragment", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(ForestioWoodFragment));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(sprite.texture, part, new Vector2(.5f,.5f), sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = source.sortingOrder + 1;
            go.transform.position = source.transform.TransformPoint((origin + part.size / 2f - sprite.pivot) / sprite.pixelsPerUnit);
            go.transform.localScale = source.transform.lossyScale;
            var rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 1.5f;
            rb.linearVelocity = new Vector2(i % 2 == 0 ? -1.8f : 1.8f, 2f + i * .2f);
            rb.angularVelocity = i % 2 == 0 ? 70f : -70f;
        }
    }
    private void Awake() { visual = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        elapsed += Time.deltaTime;
        visual.color = new Color(1f,1f,1f,1f-Mathf.Clamp01((elapsed-.5f)/.3f));
        if(elapsed>=.8f) Destroy(gameObject);
    }
    private void OnDestroy() { if(visual!=null && visual.sprite!=null) Destroy(visual.sprite); }
}
