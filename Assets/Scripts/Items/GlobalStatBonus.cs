using System.Collections.Generic;
using UnityEngine;

// Lớp bổ sung chỉ số runtime, tách biệt khỏi CharacterStats (ScriptableObject — bất biến).
// Item StatBoost cộng vào đây; CharacterBase đọc effective stat = base + bonus khi tính toán.
//
// Hai cấp bonus:
//   global  — áp cho tất cả nhân vật (mọi loại)
//   perType — áp riêng cho 1 loại, key = CharacterStats asset
//
// Cách dùng từ item:
//   GlobalStatBonus.damage += 5f;                          // buff tất cả
//   GlobalStatBonus.AddTypeBonus(warriorStats, new StatDelta { damage = 10f }); // buff 1 loại
//
// Reset() tự gọi mỗi lần bắt đầu domain (tắt Domain Reload an toàn).
public static class GlobalStatBonus
{
    // ── Bonus áp toàn bộ nhân vật ──────────────────────────────────────────
    public static float damage;
    public static float moveSpeed;
    public static float maxHP;
    public static float attackInterval; // âm = đánh nhanh hơn (giảm cooldown)
    public static float attackRange;

    // ── Bonus áp riêng từng loại ────────────────────────────────────────────
    static readonly Dictionary<CharacterStats, StatDelta> perType = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        damage = 0f;
        moveSpeed = 0f;
        maxHP = 0f;
        attackInterval = 0f;
        attackRange = 0f;
        perType.Clear();
    }

    public static StatDelta GetTypeBonus(CharacterStats type)
        => type != null && perType.TryGetValue(type, out StatDelta d) ? d : default;

    public static void AddTypeBonus(CharacterStats type, StatDelta delta)
    {
        if (type == null) return;
        if (!perType.TryGetValue(type, out StatDelta cur)) cur = default;
        cur.Add(delta);
        perType[type] = cur;
    }

    // Gọi khi bán / unequip item để hoàn lại bonus đã cộng.
    public static void RemoveTypeBonus(CharacterStats type, StatDelta delta)
    {
        if (type == null || !perType.TryGetValue(type, out StatDelta cur)) return;
        cur.Subtract(delta);
        perType[type] = cur;
    }
}

// ── Struct chứa lượng thay đổi chỉ số ─────────────────────────────────────
// Dùng trong GlobalStatBonus (per-type) và ItemData (mô tả effect của item).
[System.Serializable]
public struct StatDelta
{
    public float damage;
    public float moveSpeed;
    public float maxHP;
    public float attackInterval; // âm = đánh nhanh hơn
    public float attackRange;

    public void Add(StatDelta other)
    {
        damage       += other.damage;
        moveSpeed    += other.moveSpeed;
        maxHP        += other.maxHP;
        attackInterval += other.attackInterval;
        attackRange  += other.attackRange;
    }

    public void Subtract(StatDelta other)
    {
        damage       -= other.damage;
        moveSpeed    -= other.moveSpeed;
        maxHP        -= other.maxHP;
        attackInterval -= other.attackInterval;
        attackRange  -= other.attackRange;
    }
}
