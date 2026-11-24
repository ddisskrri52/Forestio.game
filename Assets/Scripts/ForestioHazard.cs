using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class ForestioHazard : MonoBehaviour
{
    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    private void OnTriggerEnter2D(Collider2D other) => Touch(other);
    private void OnTriggerStay2D(Collider2D other) => Touch(other);

    private void Touch(Collider2D other)
    {
        ForestioPlayerHealth health = other.GetComponentInParent<ForestioPlayerHealth>();
        if (health != null) health.ReturnFromHazard();
    }
}
