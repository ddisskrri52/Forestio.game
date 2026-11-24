using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class ForestioPortal : MonoBehaviour
{
    [SerializeField] private ForestioLevelCompletion completion;
    [SerializeField] private ForestioBranchBarrier requiredBarrier;

    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    private void OnTriggerEnter2D(Collider2D other) => Touch(other);
    private void OnTriggerStay2D(Collider2D other) => Touch(other);

    private void Touch(Collider2D other)
    {
        ForestioPlayerController player = other.GetComponentInParent<ForestioPlayerController>();
        if (player != null) TryEnter(player);
    }

    public bool TryEnter(ForestioPlayerController player)
    {
        if (player == null || !player.HasChargedThrow || requiredBarrier == null
            || !requiredBarrier.IsBroken || completion == null) return false;
        return completion.TryComplete(player);
    }
}
