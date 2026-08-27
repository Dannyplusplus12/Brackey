using System.Collections;
using UnityEngine;

/// <summary>
/// Static API duy nhất để show/hide tooltip.
/// Mọi trigger (Unity UI hoặc sprite-based) đều gọi vào đây.
///
/// STABLE — không đổi khi artist thay UI.
/// </summary>
public static class TooltipSystem
{
    // Delay trước khi tooltip hiện, tránh flicker khi di chuột qua nhiều slot
    public static float HoverDelay = 0.15f;

    static TooltipPanel _panel;
    static Coroutine    _delayRoutine;

    // Runner luôn active — panel bị SetActive(false) không chạy coroutine được
    static CoroutineRunner _runner;

    static CoroutineRunner Runner
    {
        get
        {
            if (_runner != null) return _runner;
            var go = new GameObject("[TooltipCoroutineRunner]");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<CoroutineRunner>();
            return _runner;
        }
    }

    // ── Panel registration (gọi từ TooltipPanel.Awake) ───────────────────────

    public static void Register(TooltipPanel panel)   => _panel = panel;
    public static void Unregister(TooltipPanel panel) { if (_panel == panel) _panel = null; }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Hiện tooltip sau HoverDelay giây.
    /// Gọi từ OnPointerEnter / OnMouseEnter / hover bất kỳ.
    /// </summary>
    public static void Show(TooltipData data)
    {
        if (_panel == null) return;

        // Huỷ delay cũ nếu có (đang chờ show tooltip khác)
        if (_delayRoutine != null)
            Runner.StopCoroutine(_delayRoutine);

        _delayRoutine = Runner.StartCoroutine(ShowAfterDelay(data));
    }

    /// <summary>
    /// Hiện ngay lập tức, không delay — dùng khi UX cần phản hồi tức thì.
    /// </summary>
    public static void ShowImmediate(TooltipData data)
    {
        if (_panel == null) return;
        CancelDelay();
        _panel.Display(data);
    }

    /// <summary>
    /// Ẩn tooltip. Gọi từ OnPointerExit / OnMouseExit.
    /// </summary>
    public static void Hide()
    {
        if (_panel == null) return;
        CancelDelay();
        _panel.Hide();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    static IEnumerator ShowAfterDelay(TooltipData data)
    {
        yield return new WaitForSecondsRealtime(HoverDelay);
        _panel?.Display(data);
        _delayRoutine = null;
    }

    static void CancelDelay()
    {
        if (_delayRoutine != null)
        {
            Runner.StopCoroutine(_delayRoutine);
            _delayRoutine = null;
        }
    }
}

// Minimal MonoBehaviour chỉ để chạy coroutine — luôn active
public class CoroutineRunner : MonoBehaviour { }
