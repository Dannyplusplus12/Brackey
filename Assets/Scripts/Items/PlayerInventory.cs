using System.Collections.Generic;
using UnityEngine;

// 4 slot item kích hoạt của người chơi - dùng chung giữa Shop (đọc info/bán/đổi vị trí) và
// Arena (kích hoạt bằng phím 1-4/click). UI tự subscribe OnInventoryChanged để refresh, không
// cần biết ai gọi thay đổi.
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }
    public static event System.Action OnInventoryChanged;

    public const int SlotCount = 4;

    readonly ItemData[] slots = new ItemData[SlotCount];

    public ItemData GetSlot(int index) => slots[index];

    // Item StatBoost mua từ Shop rơi vào đây (không giới hạn số lượng, không có "slot" như
    // item Active) - chỉ để hiển thị (khung "All of static item" lúc Shop / thanh ngang lúc Arena).
    public IReadOnlyList<ItemData> StaticItems => staticItems;
    public static event System.Action OnStaticItemsChanged;

    readonly List<ItemData> staticItems = new();

    public void AddStaticItem(ItemData item)
    {
        staticItems.Add(item);
        OnStaticItemsChanged?.Invoke();
    }

    void Awake()
    {
        Instance = this;
    }

    public bool TryAddItem(ItemData item)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null) continue;

            slots[i] = item;
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false; // đầy 4 slot
    }

    public void SwapSlots(int indexA, int indexB)
    {
        (slots[indexA], slots[indexB]) = (slots[indexB], slots[indexA]);
        OnInventoryChanged?.Invoke();
    }

    // Xoá khỏi slot và hoàn lại sellValue corn.
    public void SellSlot(int index)
    {
        ItemData item = slots[index];
        if (item == null) return;

        slots[index] = null;
        PlayerWallet.Instance?.Earn(item.sellValue);
        OnInventoryChanged?.Invoke();
    }

    // Bán 1 StatBoost item (không cần slot index, xoá theo reference).
    public void SellStaticItem(ItemData item)
    {
        if (!staticItems.Remove(item)) return;
        PlayerWallet.Instance?.Earn(item.sellValue);
        OnStaticItemsChanged?.Invoke();
    }

    // Stub: chỉ báo hiệu "slot này vừa được kích hoạt", chưa xử lý hiệu ứng item
    // (để combat system sau này tự subscribe và áp dụng effect).
    public static event System.Action<int, ItemData> OnSlotActivated;

    public void ActivateSlot(int index)
    {
        ItemData item = slots[index];
        if (item == null || item.itemType != ItemType.Active) return;

        OnSlotActivated?.Invoke(index, item);
    }
}
