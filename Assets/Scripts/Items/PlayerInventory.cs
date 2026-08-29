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
        ApplyStatDelta(item, add: true);
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
        PlayerWallet.Instance?.Earn(item.buyCost / 2);
        OnInventoryChanged?.Invoke();
    }

    // Bán 1 StatBoost item (không cần slot index, xoá theo reference).
    public void SellStaticItem(ItemData item)
    {
        if (!staticItems.Remove(item)) return;
        ApplyStatDelta(item, add: false);
        PlayerWallet.Instance?.Earn(item.buyCost / 2);
        OnStaticItemsChanged?.Invoke();
    }

    // Áp hoặc hoàn StatDelta của item vào GlobalStatBonus.
    // Active item KHÔNG áp qua đây (handler tự xử lý).
    static void ApplyStatDelta(ItemData item, bool add)
    {
        if (item == null || item.itemType == ItemType.Active) return;

        var d = item.statDelta;
        int sign = add ? 1 : -1;

        if (item.targetType == ItemTargetType.SpecificType && item.targetCharacterType != null)
        {
            // Per-type: dùng AddTypeBonus / RemoveTypeBonus
            if (add) GlobalStatBonus.AddTypeBonus(item.targetCharacterType, d);
            else      GlobalStatBonus.RemoveTypeBonus(item.targetCharacterType, d);
        }
        else
        {
            // Global flat
            GlobalStatBonus.damage      += d.damage      * sign;
            GlobalStatBonus.moveSpeed   += d.moveSpeed   * sign;
            GlobalStatBonus.maxHP       += d.maxHP       * sign;
            GlobalStatBonus.attackSpeed += d.attackSpeed * sign;
            GlobalStatBonus.attackRange += d.attackRange * sign;
            GlobalStatBonus.foodCost    += d.foodCost    * sign;
            // Global percent
            GlobalStatBonus.damagePercent      += d.damagePercent      * sign;
            GlobalStatBonus.moveSpeedPercent   += d.moveSpeedPercent   * sign;
            GlobalStatBonus.maxHPPercent       += d.maxHPPercent       * sign;
            GlobalStatBonus.attackSpeedPercent += d.attackSpeedPercent * sign;
        }
    }

    // Fire khi slot được kích hoạt — handler (ItemEffectHandler) subscribe để áp effect.
    // Item bị consume (xóa khỏi slot) ngay sau khi event fire.
    public static event System.Action<int, ItemData> OnSlotActivated;

    public void ActivateSlot(int index)
    {
        ItemData item = slots[index];
        if (item == null || item.itemType != ItemType.Active) return;

        OnSlotActivated?.Invoke(index, item);
        ConsumeSlot(index);
    }

    // Xóa item khỏi slot sau khi dùng (không hoàn corn — đã dùng rồi).
    void ConsumeSlot(int index)
    {
        slots[index] = null;
        OnInventoryChanged?.Invoke();
    }
}
