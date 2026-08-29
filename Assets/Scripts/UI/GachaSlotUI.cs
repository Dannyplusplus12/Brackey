using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1 ô (slice) trên spin wheel.
///
/// Hierarchy (tạo bởi GachaWheelUI):
///   SliceRoot (RectTransform, GachaSlotUI)
///     └─ SliceImage   (Image — sprite rẻ quạt, pivot ở đỉnh dưới)
///     └─ CharImage    (Image — hình nhân vật, đầu hướng ra ngoài)
///
/// Toàn bộ SliceRoot xoay quanh tâm bánh xe → CharImage tự đi theo,
/// đầu tự nhiên hướng ra ngoài (không cần counter-rotate thêm).
/// </summary>
public class GachaSlotUI : MonoBehaviour
{
    [SerializeField] Image _sliceImage;
    [SerializeField] Image _charImage;

    ItemData _itemData;
    public ItemData ItemData => _itemData;

    // ── Runtime wiring (khi tạo bằng code) ────────────────────────────────────

    /// <summary>
    /// Gán Image references khi slot được tạo bằng code (GachaWheelUI).
    /// Phải gọi TRƯỚC Setup().
    /// </summary>
    public void SetReferences(Image sliceImage, Image charImage)
    {
        _sliceImage = sliceImage;
        _charImage  = charImage;
    }

    // ── Setup ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi ngay sau khi GachaWheelUI tạo slot này.
    /// </summary>
    /// <param name="item">ItemData của nhân vật trong ô này.</param>
    /// <param name="sliceSprite">Sprite hình rẻ quạt từ artist.</param>
    /// <param name="sliceSize">Kích thước Image ô (px).</param>
    /// <param name="charSize">Kích thước Image nhân vật (px).</param>
    /// <param name="charOffsetFromCenter">
    ///   Khoảng từ tâm bánh xe đến giữa CharImage (px).
    ///   Dương = ra ngoài (về phía "đầu" nhân vật).
    /// </param>
    public void Setup(ItemData item, Sprite sliceSprite,
                      float sliceSize, float charSize, float charOffsetFromCenter)
    {
        _itemData = item;

        // Slice image —————————————————————————————————————————
        if (_sliceImage != null)
        {
            _sliceImage.sprite = sliceSprite;
            _sliceImage.preserveAspect = false;

            // pivot ở đỉnh dưới (tip của rẻ quạt hướng về tâm)
            var sliceRect = _sliceImage.rectTransform;
            sliceRect.pivot         = new Vector2(0.5f, 0f);
            sliceRect.anchorMin     = new Vector2(0.5f, 0.5f);
            sliceRect.anchorMax     = new Vector2(0.5f, 0.5f);
            sliceRect.anchoredPosition = Vector2.zero;
            sliceRect.sizeDelta     = new Vector2(sliceSize, sliceSize);
        }

        // Character image ——————————————————————————————————————
        if (_charImage != null)
        {
            // Idle sprite ưu tiên; fallback về item.icon
            Sprite portrait = null;
            if (item != null)
            {
                if (item.characterPrefab != null)
                {
                    var stats = item.characterPrefab.GetComponent<CharacterBase>()?.Stats;
                    if (stats != null) portrait = stats.idleSprite;
                }
                if (portrait == null) portrait = item.icon;
            }

            _charImage.sprite = portrait;
            _charImage.enabled = portrait != null;
            _charImage.preserveAspect = true;

            var charRect = _charImage.rectTransform;
            charRect.pivot             = new Vector2(0.5f, 0.5f);
            charRect.anchorMin         = new Vector2(0.5f, 0.5f);
            charRect.anchorMax         = new Vector2(0.5f, 0.5f);
            // anchoredPosition: tính từ pivot của SliceRoot (= tâm bánh xe).
            // Slice hướng lên → dương Y = hướng ra ngoài
            charRect.anchoredPosition  = new Vector2(0f, charOffsetFromCenter);
            charRect.sizeDelta         = new Vector2(charSize, charSize);
        }
    }

    // ── Runtime ────────────────────────────────────────────────────────────────

    /// <summary>Chỉ cập nhật sprite slice (dùng khi thay sprite trong Inspector mà không rebuild).</summary>
    public void UpdateSliceSprite(Sprite s)
    {
        if (_sliceImage != null) _sliceImage.sprite = s;
    }

    /// <summary>Chỉ cập nhật hình nhân vật (dùng khi load pack mới, không rebuild layout).</summary>
    public void UpdateCharacter(ItemData item)
    {
        _itemData = item;
        if (_charImage == null) return;

        Sprite portrait = null;
        if (item?.characterPrefab != null)
        {
            var stats = item.characterPrefab.GetComponent<CharacterBase>()?.Stats;
            if (stats != null) portrait = stats.idleSprite;
        }
        if (portrait == null && item != null) portrait = item.icon;

        _charImage.sprite  = portrait;
        _charImage.enabled = portrait != null;
    }

    /// <summary>Highlight ô (khi trúng) — phóng nhẹ để nổi bật.</summary>
    public void SetHighlight(bool on)
    {
        float scale = on ? 1.1f : 1f;
        transform.localScale = Vector3.one * scale;
    }
}
