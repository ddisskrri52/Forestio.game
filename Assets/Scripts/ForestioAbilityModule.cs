using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class ForestioAbilityModule : MonoBehaviour
{
    [SerializeField] private ForestioAbilityNotice notice;
    private bool collected;

    public bool IsCollected => collected;

    private void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        ForestioPlayerController player = other.GetComponentInParent<ForestioPlayerController>();
        if (player == null)
            return;

        if (player.HasChargedThrow)
        {
            collected = true;
            gameObject.SetActive(false);
            return;
        }

        if (notice == null || notice.IsOpen)
            return;

        if (!player.UnlockChargedThrow())
            return;

        collected = true;
        notice.Show(player);
        gameObject.SetActive(false);
    }
}
