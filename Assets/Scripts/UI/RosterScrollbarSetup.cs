using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach lên RosterPanel (cùng GO với ScrollRect).
/// Tự tạo vertical scrollbar overlay ở mép phải khi game chạy — không cần chỉnh tay trong Inspector.
///
/// Cách dùng:
///   1. Add Component "RosterScrollbarSetup" lên RosterPanel ScrollRect GO.
///   2. Tuỳ chỉnh barWidth / màu trong Inspector nếu muốn.
///   3. Play — scrollbar xuất hiện tự động, wire vào ScrollRect.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class RosterScrollbarSetup : MonoBehaviour
{
    [Header("Kích thước & màu")]
    [SerializeField] float barWidth     = 8f;
    [SerializeField] Color trackColor   = new Color(0.08f, 0.08f, 0.08f, 0.55f);
    [SerializeField] Color handleColor  = new Color(0.80f, 0.80f, 0.80f, 0.90f);
    [SerializeField] int   handlePadX   = 2;   // inset trái/phải của handle (px)
    [SerializeField] int   handlePadY   = 2;   // inset trên/dưới của handle (px)

    [Header("Hiển thị")]
    [Tooltip("AutoHide: ẩn khi nội dung vừa với viewport. Permanent: luôn hiện.")]
    [SerializeField] ScrollRect.ScrollbarVisibility visibility = ScrollRect.ScrollbarVisibility.AutoHide;

    void Awake()
    {
        var sr = GetComponent<ScrollRect>();
        if (sr.verticalScrollbar != null) return;   // đã có sẵn, bỏ qua

        // ── Fix: đảm bảo Viewport block raycasts ─────────────────────────
        // Nếu Viewport không có Image, scroll event ở vùng trống "xuyên qua"
        // UI xuống world → ScrollRect không nhận được event.
        // Giải pháp: thêm Image trong suốt trên Viewport để catch raycasts.
        if (sr.viewport != null)
        {
            var vpImg = sr.viewport.GetComponent<Image>();
            if (vpImg == null)
            {
                vpImg           = sr.viewport.gameObject.AddComponent<Image>();
                vpImg.color     = Color.clear;  // hoàn toàn trong suốt
            }
            vpImg.raycastTarget = true;
        }

        // ── Track (nền thanh scrollbar) ───────────────────────────────────
        var trackGO             = new GameObject("Scrollbar_Vertical", typeof(RectTransform));
        trackGO.transform.SetParent(transform, false);
        trackGO.transform.SetAsLastSibling();   // render đè lên viewport

        var trackRT             = trackGO.GetComponent<RectTransform>();
        // Overlay lên mép phải trong phạm vi ScrollRect, KHÔNG ảnh hưởng layout viewport
        trackRT.anchorMin       = new Vector2(1f, 0f);
        trackRT.anchorMax       = new Vector2(1f, 1f);
        trackRT.pivot           = new Vector2(1f, 0.5f);
        trackRT.sizeDelta       = new Vector2(barWidth, 0f);
        trackRT.anchoredPosition = Vector2.zero;

        var trackImg            = trackGO.AddComponent<Image>();
        trackImg.color          = trackColor;
        trackImg.raycastTarget  = true;

        // ── Scrollbar component ───────────────────────────────────────────
        var sb                  = trackGO.AddComponent<Scrollbar>();
        sb.direction            = Scrollbar.Direction.BottomToTop;

        // ── Sliding Area (vùng handle di chuyển) ─────────────────────────
        var areaGO              = new GameObject("SlidingArea", typeof(RectTransform));
        areaGO.transform.SetParent(trackGO.transform, false);
        var areaRT              = areaGO.GetComponent<RectTransform>();
        areaRT.anchorMin        = Vector2.zero;
        areaRT.anchorMax        = Vector2.one;
        areaRT.offsetMin        = new Vector2(handlePadX,  handlePadY);
        areaRT.offsetMax        = new Vector2(-handlePadX, -handlePadY);

        // ── Handle ────────────────────────────────────────────────────────
        var handleGO            = new GameObject("Handle", typeof(RectTransform));
        handleGO.transform.SetParent(areaGO.transform, false);
        var handleRT            = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin      = Vector2.zero;
        handleRT.anchorMax      = Vector2.one;
        handleRT.offsetMin      = Vector2.zero;
        handleRT.offsetMax      = Vector2.zero;

        var handleImg           = handleGO.AddComponent<Image>();
        handleImg.color         = handleColor;

        // ── Wire Scrollbar ────────────────────────────────────────────────
        sb.handleRect           = handleRT;
        sb.targetGraphic        = handleImg;

        // ── Wire vào ScrollRect ───────────────────────────────────────────
        sr.vertical                    = true;
        sr.verticalScrollbar           = sb;
        sr.verticalScrollbarVisibility = visibility;
        sr.verticalScrollbarSpacing    = 0f;   // overlay, không cần khoảng cách
    }
}
