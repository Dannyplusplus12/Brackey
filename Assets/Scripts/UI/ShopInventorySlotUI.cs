using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1 trong 4 ô Inventory lúc Shop: click 1 ô rồi click ô khác để đổi vị trí (swap),
// click phải để bán. Lúc Arena dùng ArenaHotbarSlotUI riêng (kích hoạt bằng phím/click) vì
// cách tương tác khác hẳn - 2 script cùng đọc/ghi chung PlayerInventory.
public class ShopInventorySlotUI : MonoBehaviour, IPointerClickHandler, IItemSlot
{
    [SerializeField] int slotIndex;
    [SerializeField] Image icon;

    static ShopInventorySlotUI selected;

    void OnEnable()
    {
        PlayerInventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        PlayerInventory.OnInventoryChanged -= Refresh;
        if (selected == this) selected = null;
    }

    void Refresh()
    {
        ItemData item = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetSlot(slotIndex) : null;
        icon.sprite = item != null ? item.icon : null;
        icon.enabled = item != null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (PlayerInventory.Instance == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            PlayerInventory.Instance.SellSlot(slotIndex);
            if (selected == this) selected = null;
            return;
        }

        if (selected == null)
        {
            selected = this;
            return;
        }

        if (selected != this)
            PlayerInventory.Instance.SwapSlots(selected.slotIndex, slotIndex);

        selected = null;
    }

    // ── IItemSlot ─────────────────────────────────────────────────────────────
    public ItemData GetCurrentItem() =>
        PlayerInventory.Instance != null ? PlayerInventory.Instance.GetSlot(slotIndex) : null;

    // Inventory slot → có thể bán → hiện sell value trong tooltip
    public bool IsSellable => true;
}
