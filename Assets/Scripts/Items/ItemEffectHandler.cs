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
    // Fried Egg buff tạm thời đang active — cần xoá khi hết wave.
    bool friedEggBuffActive;

    void OnEnable()
    {
        PlayerInventory.OnSlotActivated += HandleActivate;
        WaveManager.OnWaveEnd           += RemoveFriedEggBuff;
    }

    void OnDisable()
    {
        PlayerInventory.OnSlotActivated -= HandleActivate;
        WaveManager.OnWaveEnd           -= RemoveFriedEggBuff;
    }

    void HandleActivate(int slotIndex, ItemData item)
    {
        if (item == null) return;

        switch (item.displayName)
        {
            case "War Flask":
                HealAllAllies(item.statDelta.maxHP);
                break;

            case "Fish":
                ReduceAngryAllAllies(10f);
                break;

            case "Bread":
                HealAllAlliesPercent(0.6f);
                break;

            case "Fried Egg":
                ApplyFriedEggBuff();
                break;

            case "Gold Coin":
                ApplyGoldCoin();
                break;

            case "Damage Coin":
                ApplyDamageCoin();
                break;

            case "Health Coin":
                ApplyHealthCoin();
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

    // ── Fried Egg — buff tạm thời đến hết wave ────────────────────────────────

    void ApplyFriedEggBuff()
    {
        // Stack thêm nếu kích hoạt nhiều lần trong cùng 1 wave
        GlobalStatBonus.attackSpeedPercent += 0.5f;
        GlobalStatBonus.damagePercent      += 0.3f;
        friedEggBuffActive = true;
        Debug.Log("[ItemEffectHandler] Fried Egg: +50% APS, +30% damage đến hết wave.");
    }

    void RemoveFriedEggBuff()
    {
        if (!friedEggBuffActive) return;
        GlobalStatBonus.attackSpeedPercent -= 0.5f;
        GlobalStatBonus.damagePercent      -= 0.3f;
        friedEggBuffActive = false;
        Debug.Log("[ItemEffectHandler] Fried Egg: buff hết hạn.");
    }

    // ── Gold Coin — 50/50 double hoặc mất nửa corn ───────────────────────────

    static void ApplyGoldCoin()
    {
        var wallet = PlayerWallet.Instance;
        if (wallet == null) return;

        if (UnityEngine.Random.value < 0.5f)
        {
            wallet.Earn(wallet.Corn); // double: earn thêm bằng số hiện có
            Debug.Log($"[ItemEffectHandler] Gold Coin: WIN — corn x2 = {wallet.Corn}");
        }
        else
        {
            int lose = wallet.Corn / 2;
            wallet.TrySpend(lose);   // luôn thành công vì lose <= Corn
            Debug.Log($"[ItemEffectHandler] Gold Coin: LOSE — mất {lose} corn, còn {wallet.Corn}");
        }
    }

    // ── Damage Coin — 50/50 +5 damage hoặc tất cả ally +10 angry ─────────────

    static void ApplyDamageCoin()
    {
        if (UnityEngine.Random.value < 0.5f)
        {
            GlobalStatBonus.damage += 5f;
            Debug.Log("[ItemEffectHandler] Damage Coin: WIN — +5 damage vĩnh viễn.");
        }
        else
        {
            var allies = CharacterGrid.FindAllAlive(Faction.Ally);
            foreach (var ally in allies)
                ally.AddAngry(10f, AngryReason.Debug);
            Debug.Log($"[ItemEffectHandler] Damage Coin: LOSE — +10 angry cho {allies.Count} ally.");
        }
    }

    // ── Health Coin — 50% +10 MaxHP, 50% không có gì ─────────────────────────

    static void ApplyHealthCoin()
    {
        if (UnityEngine.Random.value < 0.5f)
        {
            GlobalStatBonus.maxHP += 10f;
            Debug.Log("[ItemEffectHandler] Health Coin: WIN — +10 MaxHP vĩnh viễn.");
        }
        else
        {
            Debug.Log("[ItemEffectHandler] Health Coin: LOSE — không có gì.");
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    // Hồi % MaxHP cho toàn bộ ally còn sống (0.6 = 60% MaxHP mỗi char).
    static void HealAllAlliesPercent(float pct)
    {
        if (pct <= 0f) return;
        var allies = CharacterGrid.FindAllAlive(Faction.Ally);
        foreach (var ally in allies)
            ally.Heal(ally.MaxHP * pct);
        Debug.Log($"[ItemEffectHandler] Bread: heal {pct * 100f:0}% MaxHP cho {allies.Count} ally.");
    }

    // Hồi HP flat cho toàn bộ ally còn sống, không vượt quá MaxHP hiện tại.
    static void HealAllAllies(float amount)
    {
        if (amount <= 0f) return;

        var allies = CharacterGrid.FindAllAlive(Faction.Ally);
        foreach (var ally in allies)
            ally.Heal(amount);

        Debug.Log($"[ItemEffectHandler] Heal {amount} HP cho {allies.Count} ally.");
    }

    // Giảm angry cho toàn bộ ally còn sống (pass số dương, hàm tự đổi sang âm).
    static void ReduceAngryAllAllies(float amount)
    {
        if (amount <= 0f) return;

        var allies = CharacterGrid.FindAllAlive(Faction.Ally);
        foreach (var ally in allies)
            ally.DebugAddAngry(-amount);

        Debug.Log($"[ItemEffectHandler] Fish: giảm {amount} angry cho {allies.Count} ally.");
    }
}
