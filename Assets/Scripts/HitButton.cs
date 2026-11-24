using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
public sealed class ForestioHitButton : MonoBehaviour
{
    [SerializeField] private GameObject door;
    [SerializeField] private Color activatedColor = new Color(0.6f, 0.95f, 0.75f, 1f);

    private SpriteRenderer visual;
    private Color initialColor;
    public bool IsActivated { get; private set; }

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        initialColor = visual.color;
        GetComponent<CircleCollider2D>().isTrigger = true;
        IsActivated = false;

        if (door != null)
            door.SetActive(true);
        else
            Debug.LogWarning("Assign the Door field on this button before testing.", this);
    }

    public void ResetAfterDefeat()
    {
        IsActivated = false;
        visual.color = initialColor;
        if (door != null) door.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsActivated || door == null)
            return;

        ForestioStoneProjectile projectile = other.GetComponentInParent<ForestioStoneProjectile>();
        if (projectile == null || !projectile.CanHitTarget)
            return;

        IsActivated = true;
        door.SetActive(false);
        visual.color = activatedColor;
        projectile.ConsumeOnHit();
    }
}
