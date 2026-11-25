using UnityEngine;

public sealed class ForestioVineGate : MonoBehaviour
{
    public Transform left, right;
    [SerializeField] private Vector3 leftClosed = new Vector3(-.4f, 0f, 0f);
    [SerializeField] private Vector3 rightClosed = new Vector3(.4f, 0f, 0f);
    private float progress;
    private bool opening;
    public bool IsOpen => progress >= 1f;
    public void Open() { opening = true; }
    public void ResetClosed()
    {
        opening = false; progress = 0f;
        GetComponent<BoxCollider2D>().enabled = true;
        Place();
    }
    private void Awake() { ResetClosed(); }
    private void Update()
    {
        if (!opening || progress >= 1f) return;
        progress = Mathf.MoveTowards(progress, 1f, Time.deltaTime / 0.45f);
        GetComponent<BoxCollider2D>().enabled = progress < 0.85f;
        Place();
    }
    private void Place()
    {
        if (left == null || right == null) return;
        float height = Mathf.SmoothStep(0f, 2.6f, progress);
        left.localPosition = leftClosed + Vector3.up * height;
        right.localPosition = rightClosed + Vector3.up * height;
        left.gameObject.SetActive(progress < 1f); right.gameObject.SetActive(progress < 1f);
    }
}
