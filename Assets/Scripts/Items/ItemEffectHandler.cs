using UnityEngine;

// Xử lý hiệu ứng Active item khi player kích hoạt (phím 1-4 hoặc click hotbar).
// Subscribe PlayerInventory.OnSlotActivated, đọc ItemData và áp effect tương ứng.
//
// Quy ước heal amount: đọc từ item.statDelta.maxHP (dương = heal X HP).
// Active item KHÔNG áp statDelta vào GlobalStatBonus — PlayerInventory.ApplyStatDelta đã bỏ qua loại Active.
//
// Thêm item mới: thêm case vào switch bên dưới hoặc match theo item.displayName / itemType.
public class ItemEffectHandler : MonoBehaviour
{
    void OnEnable()  => PlayerInventory.OnSlotActivated += HandleActivate;
    void OnDisable() => PlayerInventory.OnSlotActivated -= HandleActivate;

    void HandleActivate(int slotIndex, ItemData item)
    {
        if (item == null) return;

        switch (item.displayName)
        {
            case "War Flask":
                HealAllAllies(item.statDelta.maxHP);
                break;

            // Thêm item mới ở đây:
            // case "Item Name":
            //     DoEffect(item);
            //     break;

            default:
                Debug.Log($"[ItemEffectHandler] Không có handler cho: {item.displayName}");
                break;
        }
    }

    // Hồi HP cho toàn bộ ally còn sống, không vượt quá MaxHP hiện tại.
    static void HealAllAllies(float amount)
    {
        if (amount <= 0f) return;

        var allies = CharacterGrid.FindAllAlive(Faction.Ally);
        foreach (var ally in allies)
            ally.Heal(amount);

        Debug.Log($"[ItemEffectHandler] War Flask: heal {amount} HP cho {allies.Count} ally.");
    }
}
