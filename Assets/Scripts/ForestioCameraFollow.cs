using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class ForestioCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Rect worldBounds = new Rect(-19.5f, -4f, 43f, 14f);
    [SerializeField, Min(0.01f)] private float smoothTime = 0.2f;
    [SerializeField] private float verticalOffset = 1.2f;
    private Camera view;
    private Vector3 velocity;

    private void Awake() => view = GetComponent<Camera>();
    private void Start() => Follow(true);
    private void LateUpdate() => Follow(false);

    private void Follow(bool snap)
    {
        if (target == null) return;
        float halfHeight = view.orthographicSize;
        float halfWidth = halfHeight * view.aspect;
        float x = ClampCenter(target.position.x, worldBounds.xMin, worldBounds.xMax, halfWidth);
        float y = ClampCenter(target.position.y + verticalOffset, worldBounds.yMin, worldBounds.yMax, halfHeight);
        Vector3 desired = new Vector3(x, y, transform.position.z);
        if (snap || Vector2.Distance(transform.position, desired) > 8f)
        {
            transform.position = desired;
            velocity = Vector3.zero;
        }
        else
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    private static float ClampCenter(float value, float minimum, float maximum, float extent)
    {
        if (maximum - minimum <= extent * 2f) return (minimum + maximum) * 0.5f;
        return Mathf.Clamp(value, minimum + extent, maximum - extent);
    }
}
