using System.Collections;
using UnityEngine;
using TMPro;

// Hiển thị popup "-X 🌽" trực tiếp trên thân nhân vật (world space) khi FeedingManager feed.
// Gắn lên root của character prefab, kéo cornIcon vào Inspector.
// Nếu chỉ muốn dùng cái này thì có thể xoá FeedPopupCoroutine trong CharacterRosterEntry.
[RequireComponent(typeof(CharacterBase))]
public class CharacterFeedPopup : MonoBehaviour
{
    [SerializeField] Sprite cornIcon;

    [Tooltip("Offset sang phải tính từ BodyCenter")]
    [SerializeField] float rightOffset = 0.5f;

    [Tooltip("Độ cao nổi lên trong suốt animation")]
    [SerializeField] float floatHeight = 0.8f;

    [Tooltip("Thời gian tồn tại (giây)")]
    [SerializeField] float duration = 1.1f;

    [Tooltip("Font size của số (world units — chỉnh theo orthographic size của camera)")]
    [SerializeField] float fontSize = 2.5f;

    [Tooltip("Kích thước icon corn (world units)")]
    [SerializeField] float iconSize = 0.4f;

    [Tooltip("Sorting order — nên cao hơn sprite nhân vật để không bị che")]
    [SerializeField] int sortingOrder = 100;

    CharacterBase _char;

    void Awake() => _char = GetComponent<CharacterBase>();
    void OnEnable()  => FeedingManager.OnFeedResult += OnFeedResult;
    void OnDisable() => FeedingManager.OnFeedResult -= OnFeedResult;

    void OnFeedResult(CharacterBase fed, int cost, bool wasFed)
    {
        if (fed != _char) return;
        StartCoroutine(ShowPopup(cost, wasFed));
    }

    IEnumerator ShowPopup(int cost, bool wasFed)
    {
        Color col = wasFed
            ? new Color(1f, 0.85f, 0.15f)   // vàng: đủ corn
            : new Color(1f, 0.35f, 0.2f);   // đỏ cam: đói / thiếu corn

        Vector3 origin = (Vector3)_char.BodyCenter + Vector3.right * rightOffset;

        // ── Build popup ────────────────────────────────────────────────────────
        var popup = new GameObject("_FeedPopup");
        popup.transform.position = origin;

        // Text "-X"
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(popup.transform, false);
        var tmp                  = textGO.AddComponent<TextMeshPro>();
        tmp.text                 = $"-{cost}";
        tmp.color                = col;
        tmp.fontSize             = fontSize;
        tmp.fontStyle            = FontStyles.Bold;
        tmp.alignment            = TextAlignmentOptions.Left;
        tmp.textWrappingMode     = TMPro.TextWrappingModes.NoWrap;
        tmp.overflowMode         = TextOverflowModes.Overflow;
        tmp.sortingOrder         = sortingOrder;

        // Corn icon (bên phải text)
        SpriteRenderer iconSR = null;
        if (cornIcon != null)
        {
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(popup.transform, false);

            // Ước tính chiều rộng text: ~fontSize * 0.055 per char
            float textWidth = (cost >= 10 ? 3 : 2) * fontSize * 0.055f;
            iconGO.transform.localPosition = new Vector3(textWidth + iconSize * 0.15f, iconSize * -0.05f, 0f);
            iconGO.transform.localScale    = Vector3.one * iconSize;

            iconSR              = iconGO.AddComponent<SpriteRenderer>();
            iconSR.sprite       = cornIcon;
            iconSR.sortingOrder = sortingOrder;
        }

        // ── Animate: float up + fade out ───────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float norm  = elapsed / duration;

            // Chậm đầu, nhanh cuối (ease-in)
            float alpha = 1f - Mathf.Pow(norm, 1.5f);

            popup.transform.position = origin + Vector3.up * (floatHeight * norm);
            tmp.color = new Color(col.r, col.g, col.b, alpha);
            if (iconSR != null) iconSR.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        Destroy(popup);
    }
}
