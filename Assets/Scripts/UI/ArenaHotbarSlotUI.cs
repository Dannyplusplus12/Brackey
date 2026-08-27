using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1 trong 4 ô Inventory lúc Arena (kiểu hotbar Minecraft): kích hoạt bằng phím 1-4 hoặc click chuột.
public class ArenaHotbarSlotUI : MonoBehaviour, IPointerClickHandler
{
    static readonly KeyCode[] hotkeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };

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

    void Update()
    {
        if (Input.GetKeyDown(hotkeys[slotIndex]))
            Activate();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Activate();
    }

    void Activate()
    {
        PlayerInventory.Instance?.ActivateSlot(slotIndex);
    }

    void Refresh()
    {
        ItemData item = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetSlot(slotIndex) : null;
        icon.sprite = item != null ? item.icon : null;
        icon.enabled = item != null;
    }
}
