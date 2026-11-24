using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class ForestioFood : MonoBehaviour
{
    public bool IsCollected { get; private set; }

    private void Awake() => GetComponent<CircleCollider2D>().isTrigger = true;
    private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    private void TryCollect(Collider2D other)
    {
        if (IsCollected) return;
        ForestioPlayerHealth health = other.GetComponentInParent<ForestioPlayerHealth>();
        if (health == null || !health.TryHeal()) return;
        IsCollected = true;
        gameObject.SetActive(false);
    }

    public void ResetAfterDefeat()
    {
        IsCollected = false;
        gameObject.SetActive(true);
    }
}
