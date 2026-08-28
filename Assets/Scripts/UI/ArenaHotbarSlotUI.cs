using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1 trong 4 ô Inventory lúc Arena (kiểu hotbar Minecraft): kích hoạt bằng phím 1-4 hoặc click chuột.
public class ArenaHotbarSlotUI : MonoBehaviour, IPointerClickHandler
{
    // Input phím 1-4 được xử lý bởi ItemActivationInput (script riêng trên ShopManagers).
    // Slot UI này chỉ lo hiển thị icon và xử lý click chuột.

    [SerializeField] int slotIndex;
    [SerializeField] Image icon;

    void OnEnable()
    {
        PlayerInventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        PlayerInventory.OnInventoryChanged -= Refresh;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayerInventory.Instance?.ActivateSlot(slotIndex);
    }

    void Refresh()
    {
        ItemData item = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetSlot(slotIndex) : null;
        icon.sprite  = item?.icon;
        icon.enabled = item != null && item.icon != null;
    }
}
