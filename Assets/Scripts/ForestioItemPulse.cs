using UnityEngine;

public sealed class ForestioItemPulse : MonoBehaviour
{
    private Vector3 initialScale;
    private void Awake() { initialScale = transform.localScale; }
    private void Update() { transform.localScale = initialScale * (1f + Mathf.Sin(Time.time * 2.5f) * 0.035f); }
}
