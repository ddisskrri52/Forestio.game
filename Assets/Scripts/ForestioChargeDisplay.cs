using UnityEngine;

public sealed class ForestioChargeDisplay : MonoBehaviour
{
    [SerializeField] private ForestioPlayerController player;
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private SpriteRenderer fill;
    [SerializeField] private float fillWidth = 0.66f;
    [SerializeField] private float fillHeight = 0.055f;
    [SerializeField] private Color chargingColor = new Color(0.96f, 0.65f, 0.8f, 1f);
    [SerializeField] private Color readyColor = new Color(0.6f, 0.95f, 0.75f, 1f);

    private void Awake()
    {
        Hide();
    }

    private void LateUpdate()
    {
        if (player == null || !player.IsCharging || !player.HasStone)
        {
            Hide();
            return;
        }

        float progress = player.ChargeProgress;
        if (background != null) background.enabled = true;
        if (fill == null) return;
        fill.enabled = progress > 0f;
        fill.color = progress >= 1f ? readyColor : chargingColor;
        fill.transform.localScale = new Vector3(fillWidth * progress, fillHeight, 1f);
        fill.transform.localPosition = new Vector3((progress - 1f) * fillWidth / 2f, 0f, -0.01f);
    }

    private void Hide()
    {
        if (background != null) background.enabled = false;
        if (fill != null) fill.enabled = false;
    }

    private void OnDisable()
    {
        Hide();
    }
}
