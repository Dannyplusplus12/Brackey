using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn lên Viewport (con trực tiếp của ScrollView).
///
/// Giải quyết 2 vấn đề cùng lúc:
///   1. Scroll chỉ hoạt động khi hover vào card — không hoạt động trên vùng trống.
///      Fix: thêm Image trong suốt lên Viewport GO để block raycasts toàn vùng.
///   2. Scroll ngược chiều / rất chậm / dừng đột ngột.
///      Fix: intercept IScrollHandler tại Viewport (trước khi event leo lên ScrollRect)
///           và tự tính normalizedPosition với chiều đúng, bỏ qua ScrollRect.OnScroll.
///
/// Cách hoạt động:
///   - Raycast trên card   → bubble lên: Image → Entry → Content → Viewport (bắt ở đây) ✓
///   - Raycast vùng trống  → bubble lên: Viewport Image → Viewport (bắt ở đây) ✓
///   - ScrollRect.OnScroll KHÔNG được gọi → không bị lỗi chiều.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ViewportScrollInterceptor : MonoBehaviour, IScrollHandler
{
    ScrollRect _scrollRect;

    void Awake()
    {
        _scrollRect = GetComponentInParent<ScrollRect>();

        // Thêm Image trong suốt để Viewport GO block raycasts ở vùng trống.
        // Card và Content nằm trên (sibling order cao hơn trong Content), nên
        // raycasts trên card vẫn trúng card trước — Image này chỉ catch vùng trống.
        var img = GetComponent<Image>();
        if (img == null) img = gameObject.AddComponent<Image>();
        img.color         = Color.clear;
        img.raycastTarget = true;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (_scrollRect == null || _scrollRect.content == null) return;

        var content  = _scrollRect.content;
        var viewport = _scrollRect.viewport != null
            ? _scrollRect.viewport
            : (RectTransform)transform;

        // Tính khoảng scroll cho phép.
        // Content anchor top (anchorMin.y = anchorMax.y = 1, pivot.y = 1):
        //   anchoredPosition.y = 0        → top list hiển thị (top items visible)
        //   anchoredPosition.y = maxY     → bottom list hiển thị (bottom items visible)
        float maxY = Mathf.Max(0f, content.rect.height - viewport.rect.height);
        if (maxY <= 0f) return;

        // scrollDelta.y > 0  →  wheel lên  →  muốn thấy top items  →  anchoredPosition.y giảm
        // scrollDelta.y < 0  →  wheel xuống →  muốn thấy bottom items → anchoredPosition.y tăng
        // ⚠ Dùng anchoredPosition trực tiếp (KHÔNG dùng verticalNormalizedPosition setter):
        //    verticalNormalizedPosition setter zero hoá velocity → mất inertia → scroll dừng ngay.
        Vector2 pos = content.anchoredPosition;
        pos.y -= eventData.scrollDelta.y * _scrollRect.scrollSensitivity;
        pos.y  = Mathf.Clamp(pos.y, 0f, maxY);

        content.anchoredPosition = pos;
    }
}
