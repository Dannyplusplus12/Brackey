/// <summary>
/// Interface cho bất kỳ slot nào biết item đang nằm trong đó.
/// Implement interface này trong:
///   - ShopOfferSlotUI   (item đang rao bán)
///   - ShopInventorySlotUI (item trong túi player)
///   - ArenaHotbarSlotUI  (khi dựng sau)
///   - Sprite-based slot bất kỳ (tương lai)
///
/// ItemTooltipTrigger gọi GetCurrentItem() khi hover để lấy data.
/// </summary>
public interface IItemSlot
{
    /// <summary>Trả về ItemData hiện tại, hoặc null nếu slot trống.</summary>
    ItemData GetCurrentItem();

    /// <summary>
    /// True nếu item trong slot này CÓ THỂ bị bán (inventory slot).
    /// False nếu chỉ là offer (chưa mua).
    /// Dùng để quyết định có hiển thị sell value trong tooltip không.
    /// </summary>
    bool IsSellable { get; }
}
