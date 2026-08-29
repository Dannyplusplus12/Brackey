using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gắn lên CornPanel. Khi corn thay đổi, spawn floating "+X" / "-X" + icon corn
// tại vị trí deltaAnchor (đặt bên ngoài panel, child của Canvas root).
public class CornDeltaPopup : MonoBehaviour
{
    [Header("Spawn point — child của Canvas root, đặt bên ngoài panel")]
    [SerializeField] RectTransform deltaAnchor;

    [Header("Icon")]
    [SerializeField] Sprite cornIcon;

    [Header("Tuning")]
    [SerializeField] float floatDistance = 60f;
    [SerializeField] float duration      = 1.1f;
    [SerializeField] float fontSize      = 22f;
    [SerializeField] float iconSize      = 24f;

    RectTransform _canvasRt;

    void Awake()
    {
        Canvas root = GetComponentInParent<Canvas>();
        if (root != null && !root.isRootCanvas) root = root.rootCanvas;
        if (root != null) _canvasRt = root.GetComponent<RectTransform>();
    }

    void OnEnable()  => PlayerWallet.OnCornDelta += OnDelta;
    void OnDisable() => PlayerWallet.OnCornDelta -= OnDelta;

    void OnDelta(int delta)
    {
        if (delta == 0 || deltaAnchor == null || _canvasRt == null) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Shop) return;
        StartCoroutine(Animate(delta));
    }

    IEnumerator Animate(int delta)
    {
        bool positive = delta > 0;
        Color textCol = positive
            ? new Color(0.25f, 0.95f, 0.35f)   // xanh lá
            : new Color(1f,    0.35f, 0.2f);    // đỏ cam

        string label = positive ? $"+{delta}" : $"{delta}";

        // ── Tạo popup GO ─────────────────────────────────────────────────────
        GameObject popup = new GameObject("_CornDelta", typeof(RectTransform));
        popup.transform.SetParent(_canvasRt, false);
        popup.transform.SetAsLastSibling(); // render trên cùng

        // Copy y hệt anchor: cùng anchors + pivot + anchoredPosition
        // → đặt đúng vị trí bạn kéo anchor trong Scene view
        RectTransform popupRt = popup.GetComponent<RectTransform>();
        popupRt.anchorMin        = deltaAnchor.anchorMin;
        popupRt.anchorMax        = deltaAnchor.anchorMax;
        popupRt.pivot            = deltaAnchor.pivot;
        popupRt.anchoredPosition = deltaAnchor.anchoredPosition;
        popupRt.sizeDelta        = Vector2.zero;

        Vector2 startPos = deltaAnchor.anchoredPosition;

        // ── Text ─────────────────────────────────────────────────────────────
        GameObject textGO = new GameObject("T", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(popup.transform, false);

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = fontSize;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = textCol;
        tmp.alignment        = TextAlignmentOptions.MidlineRight;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;

        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin        = new Vector2(0f, 0.5f);
        textRt.anchorMax        = new Vector2(0f, 0.5f);
        textRt.pivot            = new Vector2(0f, 0.5f);
        textRt.sizeDelta        = new Vector2(fontSize * 4f, fontSize * 1.5f); // tự scale theo fontSize
        textRt.anchoredPosition = Vector2.zero;

        // ── Icon (bên phải text) ──────────────────────────────────────────────
        Image iconImg = null;
        if (cornIcon != null)
        {
            GameObject iconGO = new GameObject("I", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(popup.transform, false);

            iconImg              = iconGO.GetComponent<Image>();
            iconImg.sprite       = cornIcon;
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;

            RectTransform iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.anchorMin        = new Vector2(0f, 0.5f);
            iconRt.anchorMax        = new Vector2(0f, 0.5f);
            iconRt.pivot            = new Vector2(0f, 0.5f);
            iconRt.sizeDelta        = new Vector2(iconSize, iconSize);
            iconRt.anchoredPosition = new Vector2(fontSize * 4f + 2f, 0f); // bám theo chiều rộng text
        }

        // ── Animate: float up + fade ──────────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float norm  = Mathf.Clamp01(elapsed / duration);
            float alpha = 1f - Mathf.Pow(norm, 1.5f);

            popupRt.anchoredPosition = startPos + Vector2.up * (floatDistance * norm);
            tmp.color = new Color(textCol.r, textCol.g, textCol.b, alpha);
            if (iconImg != null)
                iconImg.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        Destroy(popup);
    }
}
