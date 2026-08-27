using UnityEngine;

/// <summary>
/// Trigger tooltip cho sprite-based slot (KHÔNG dùng Unity UI / EventSystem).
/// Dùng khi artist thay toàn bộ UI bằng sprite tự vẽ.
///
/// YÊU CẦU: GO phải có Collider2D (BoxCollider2D hoặc PolygonCollider2D).
///
/// ── Cách dùng ─────────────────────────────────────────────────────────────
/// 1. Tạo GO sprite slot, thêm Collider2D.
/// 2. Add SpriteTooltipTrigger.
/// 3. Gán itemSlotSource (MonoBehaviour implement IItemSlot) HOẶC staticItem.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpriteTooltipTrigger : MonoBehaviour
{
    [Tooltip("Để trống → tự cast GetComponent<IItemSlot>()")]
    [SerializeField] MonoBehaviour itemSlotSource;

    [Tooltip("Dùng nếu slot hiển thị item cố định, không thay đổi")]
    [SerializeField] ItemData staticItem;

    [Tooltip("Hướng panel hiện ra so với slot")]
    [SerializeField] TooltipDirection direction = TooltipDirection.Top;

    [Tooltip("false = canh cạnh đầu (trái/trên)\ntrue  = canh cạnh cuối (phải/dưới)")]
    [SerializeField] bool alignEnd = false;

    [Tooltip("Khoảng cách giữa 2 cạnh gần nhau nhất (pixel)")]
    [SerializeField] float gap = 8f;

    IItemSlot _slot;

    void Awake()
    {
        _slot = itemSlotSource != null
            ? itemSlotSource as IItemSlot
            : GetComponent<IItemSlot>();
    }

    void OnMouseEnter()
    {
        var item = GetItem();
        if (item == null) return;
        // sourceRect = null vì sprite slot không có RectTransform — panel không định vị được
        TooltipSystem.Show(TooltipData.FromItem(item, null, direction, alignEnd, gap));
    }

    void OnMouseExit() => TooltipSystem.Hide();

    ItemData GetItem() => _slot != null ? _slot.GetCurrentItem() : staticItem;
}
