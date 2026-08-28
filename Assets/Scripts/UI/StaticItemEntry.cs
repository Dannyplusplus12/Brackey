using UnityEngine;

/// <summary>
/// Component nhỏ gắn lên entry prefab StaticItem.
/// Implement IItemSlot để ItemTooltipTrigger tự đọc item khi hover mà không cần
/// gán tay trong Inspector.
/// </summary>
public class StaticItemEntry : MonoBehaviour, IItemSlot
{
    ItemData _item;

    public void SetItem(ItemData item) { _item = item; }

    // ── IItemSlot ─────────────────────────────────────────────────────────────
    public ItemData GetCurrentItem() => _item;
    public bool     IsSellable       => false;
}
