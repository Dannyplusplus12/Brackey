using System.Collections.Generic;
using UnityEngine;

// Lớp bổ sung chỉ số runtime, tách biệt khỏi CharacterStats (ScriptableObject — bất biến).
// Item StatBoost cộng vào đây; CharacterBase đọc effective stat = base + bonus khi tính toán.
//
// Hai cấp bonus:
//   global  — áp cho tất cả nhân vật (mọi loại)
//   perType — áp riêng cho 1 loại, key = CharacterStats asset
//
// Flat vs Percent:
//   flat    — cộng thẳng vào base  (damage += 10)
//   percent — hệ số nhân SAU flat  (0.1 = +10%, stack cộng: 2 item +10% = +20%)
//   Công thức: Effective = (base + flat) * (1 + percent)
//
// Reset() tự gọi mỗi lần bắt đầu domain (tắt Domain Reload an toàn).
public static class GlobalStatBonus
{
    // ── Flat bonus áp toàn bộ nhân vật ────────────────────────────────────────
    public static float damage;
    public static float moveSpeed;
    public static float maxHP;
    public static float attackSpeed;    // APS, dương = đánh nhanh hơn
    public static float attackRange;
    public static float foodCost;       // dương = tốn thêm corn/round

    // ── Percent bonus áp toàn bộ nhân vật (0.1 = +10%) ────────────────────────
    public static float damagePercent;
    public static float moveSpeedPercent;
    public static float maxHPPercent;
    public static float attackSpeedPercent;

    // ── Bonus áp riêng từng loại ────────────────────────────────────────────────
    static readonly Dictionary<CharacterStats, StatDelta> perType = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        damage      = 0f; moveSpeed      = 0f; maxHP      = 0f;
        attackSpeed = 0f; attackRange    = 0f; foodCost   = 0f;
        damagePercent = 0f; moveSpeedPercent = 0f;
        maxHPPercent  = 0f; attackSpeedPercent = 0f;
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

    public static void RemoveTypeBonus(CharacterStats type, StatDelta delta)
    {
        if (type == null || !perType.TryGetValue(type, out StatDelta cur)) return;
        cur.Subtract(delta);
        perType[type] = cur;
    }
}

// ── Struct chứa lượng thay đổi chỉ số ──────────────────────────────────────────
// Flat: cộng thẳng. Percent: hệ số nhân (0.1 = +10%), stack cộng dồn.
[System.Serializable]
public struct StatDelta
{
    [Header("Flat")]
    public float damage;
    public float moveSpeed;
    public float maxHP;
    public float attackSpeed;   // APS, dương = đánh nhanh hơn
    public float attackRange;
    public float foodCost;      // dương = tốn thêm corn/round

    [Header("Percent (0.1 = +10%)")]
    public float damagePercent;
    public float moveSpeedPercent;
    public float maxHPPercent;
    public float attackSpeedPercent;

    public void Add(StatDelta other)
    {
        damage             += other.damage;
        moveSpeed          += other.moveSpeed;
        maxHP              += other.maxHP;
        attackSpeed        += other.attackSpeed;
        attackRange        += other.attackRange;
        foodCost           += other.foodCost;
        damagePercent      += other.damagePercent;
        moveSpeedPercent   += other.moveSpeedPercent;
        maxHPPercent       += other.maxHPPercent;
        attackSpeedPercent += other.attackSpeedPercent;
    }

    public void Subtract(StatDelta other)
    {
        damage             -= other.damage;
        moveSpeed          -= other.moveSpeed;
        maxHP              -= other.maxHP;
        attackSpeed        -= other.attackSpeed;
        attackRange        -= other.attackRange;
        foodCost           -= other.foodCost;
        damagePercent      -= other.damagePercent;
        moveSpeedPercent   -= other.moveSpeedPercent;
        maxHPPercent       -= other.maxHPPercent;
        attackSpeedPercent -= other.attackSpeedPercent;
    }
}
