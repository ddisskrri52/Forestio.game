using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class ForestioAbilityNotice : MonoBehaviour
{
    private const float CloseDelay = 0.5f;
    private ForestioPlayerController player;
    private float openedAt;
    private float previousTimeScale = 1f;
    private RectTransform canvas;
    private Image shade, panel;
    private Text title, body, shortcut;
    private Button continueButton;
    public bool IsOpen { get; private set; }

    public bool Show(ForestioPlayerController target)
    {
        if (IsOpen || target == null) return false;
        player = target; player.ClearPendingInput();
        previousTimeScale = Time.timeScale;
        openedAt = Time.unscaledTime; IsOpen = true; Time.timeScale = 0f;
        if (canvas == null)
        {
            canvas = ForestioUi.Root(transform, "AbilityPopup", 800);
            shade = ForestioUi.Picture(canvas, "Shade", null, new Color(0.15f, 0.08f, 0.2f, 0.55f));
            panel = ForestioUi.Panel(canvas, "Panel", ForestioUi.Cream);
            title = ForestioUi.Label(canvas, "Title", 25, TextAnchor.MiddleCenter, true);
            title.text = "Заряджений кидок відкрито!";
            body = ForestioUi.Label(canvas, "Explanation", 20);
            body.text = "Утримуй ЛКМ або F 0,7 с і відпусти.\nЦей кидок руйнує сухі гілки.";
            continueButton = ForestioUi.Button(canvas, "Continue", "Продовжити");
            continueButton.onClick.AddListener(() => TryClose());
            shortcut = ForestioUi.Label(canvas, "CloseKeys", 16);
            shortcut.text = "E або Space";
        }
        canvas.gameObject.SetActive(true);
        UpdateUi();
        return true;
    }
    private void Update()
    {
        if (!IsOpen) return;
        UpdateUi();
        if (!Application.isFocused) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) TryClose();
    }
    private void UpdateUi()
    {
        float s = ForestioUi.Scale;
        ForestioUi.Place(shade.rectTransform, new Rect(0f, 0f, Screen.width, Screen.height));
        float width = Mathf.Min(580f * s, Screen.width - 28f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - 240f * s) * 0.5f, width, 240f * s);
        ForestioUi.Place(panel.rectTransform, rect);
        ForestioUi.Place(title.rectTransform, new Rect(rect.x + 16f * s, rect.y + 18f * s, width - 32f * s, 34f * s));
        ForestioUi.Place(body.rectTransform, new Rect(rect.x + 20f * s, rect.y + 65f * s, width - 40f * s, 64f * s));
        ForestioUi.Place(continueButton.GetComponent<RectTransform>(), new Rect(rect.center.x - 105f * s, rect.y + 149f * s, 210f * s, 42f * s));
        ForestioUi.Place(shortcut.rectTransform, new Rect(rect.x + 20f * s, rect.y + 199f * s, width - 40f * s, 24f * s));
        continueButton.interactable = Time.unscaledTime - openedAt >= CloseDelay;
    }
    public bool TryClose()
    {
        if (!IsOpen || Time.unscaledTime - openedAt < CloseDelay) return false;
        ResumeGame(); return true;
    }
    private void ResumeGame()
    {
        if (!IsOpen) return;
        IsOpen = false; Time.timeScale = previousTimeScale;
        if (canvas != null) canvas.gameObject.SetActive(false);
        if (player != null) player.ClearPendingInput();
        player = null;
    }
    private void OnDisable() => ResumeGame();
    private void OnDestroy() => ResumeGame();
}
