using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Display layer — panel mô tả item.
///
/// Hệ thống định vị:
///   direction = hướng panel xuất hiện so với slot (Top/Bottom/Left/Right)
///   alignEnd  = false → canh cạnh đầu (trái hoặc trên) của panel với slot
///               true  → canh cạnh cuối (phải hoặc dưới) của panel với slot
///   gap       = khoảng cách giữa 2 cạnh gần nhau nhất của panel và slot
/// </summary>
public class TooltipPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image    panelBg;
    [SerializeField] TMP_Text descText;

    [Header("Layout")]
    [SerializeField] float screenMargin = 12f;

    RectTransform _rect;
    Canvas        _rootCanvas;
    TooltipData   _currentData;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        // Buộc anchor về center để anchoredPosition hoạt động đơn giản
        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);

        _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        TooltipSystem.Register(this);
        gameObject.SetActive(false);
    }

    void OnDestroy() => TooltipSystem.Unregister(this);

    // ── Public API ────────────────────────────────────────────────────────────

    public void Display(TooltipData data)
    {
        _currentData = data;
        if (descText != null) descText.text = data.richDescription ?? "";

        gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
        PlacePanel();
    }

    public void Hide() => gameObject.SetActive(false);

    void PlacePanel()
    {
        if (_currentData.sourceRect == null) return;

        var canvasRect = _rootCanvas.transform as RectTransform;

        // Lấy 4 góc slot nguồn trong world space
        // Với Screen Space Overlay (có CanvasScaler hay không), world space = screen pixel space
        var corners = new Vector3[4];
        _currentData.sourceRect.GetWorldCorners(corners);
        // corners[0]=BL, [1]=TL, [2]=TR, [3]=BR

        // Chuyển world pos → screen pixel (RectTransformUtility xử lý mọi camera mode)
        Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
        Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        float slotLeft   = screenBL.x;
        float slotBottom = screenBL.y;
        float slotRight  = screenTR.x;
        float slotTop    = screenTR.y;

        float w   = _rect.rect.width;
        float h   = _rect.rect.height;
        float gap = _currentData.gap;

        // Tính tâm panel trong screen pixel space
        float cx, cy;

        switch (_currentData.direction)
        {
            case TooltipDirection.Top:
                // Cạnh dưới panel = slotTop + gap
                cy = slotTop + gap + h * 0.5f;
                cx = _currentData.alignEnd
                    ? slotRight - w * 0.5f   // cạnh phải panel = cạnh phải slot
                    : slotLeft  + w * 0.5f;  // cạnh trái  panel = cạnh trái  slot
                break;

            case TooltipDirection.Bottom:
                // Cạnh trên panel = slotBottom - gap
                cy = slotBottom - gap - h * 0.5f;
                cx = _currentData.alignEnd
                    ? slotRight - w * 0.5f
                    : slotLeft  + w * 0.5f;
                break;

            case TooltipDirection.Left:
                // Cạnh phải panel = slotLeft - gap
                cx = slotLeft - gap - w * 0.5f;
                cy = _currentData.alignEnd
                    ? slotBottom + h * 0.5f  // cạnh dưới panel = cạnh dưới slot
                    : slotTop    - h * 0.5f; // cạnh trên panel = cạnh trên slot
                break;

            default: // Right
                // Cạnh trái panel = slotRight + gap
                cx = slotRight + gap + w * 0.5f;
                cy = _currentData.alignEnd
                    ? slotBottom + h * 0.5f
                    : slotTop    - h * 0.5f;
                break;
        }

        // Clamp trong màn hình
        cx = Mathf.Clamp(cx, w * 0.5f + screenMargin, Screen.width  - w * 0.5f - screenMargin);
        cy = Mathf.Clamp(cy, h * 0.5f + screenMargin, Screen.height - h * 0.5f - screenMargin);

        // Chuyển screen pixel → canvas local → world position
        // TransformPoint xử lý đúng mọi CanvasScaler mode và canvas pivot
        Vector2 localPt;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            new Vector2(cx, cy),
            cam,
            out localPt);

        _rect.position = canvasRect.TransformPoint(localPt);
    }
}
