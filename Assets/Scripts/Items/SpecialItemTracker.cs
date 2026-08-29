using System.Collections.Generic;
using UnityEngine;

// Theo dõi các item có điều kiện kích hoạt dựa trên sự kiện per-character:
//   Mushroom  — mỗi 10 damage nhận được: +1 MaxHP vĩnh viễn cho nhân vật đó.
//   Lemon     — mỗi 10 angry tích lũy: +0.01 APS vĩnh viễn cho nhân vật đó.
//
// Cả hai dùng bộ đếm cộng dồn cả game (không reset theo round).
// Buff áp qua AddInstanceBonus — chỉ ảnh hưởng đúng nhân vật liên quan.
//
// Gắn script này lên GameObject Managers (cùng chỗ PlayerInventory).
public class SpecialItemTracker : MonoBehaviour
{
    // Bộ đếm per-character (key = CharacterBase instance)
    readonly Dictionary<CharacterBase, float> mushroomDamageAccum = new();
    readonly Dictionary<CharacterBase, float> lemonAngryAccum     = new();

    void OnEnable()
    {
        CharacterBase.OnDamageTaken += HandleDamageTaken;
        CharacterBase.OnAngryAdded  += HandleAngryAdded;
    }

    void OnDisable()
    {
        CharacterBase.OnDamageTaken -= HandleDamageTaken;
        CharacterBase.OnAngryAdded  -= HandleAngryAdded;
    }

    // ── Mushroom ──────────────────────────────────────────────────────────────

    void HandleDamageTaken(CharacterBase ch, float amount)
    {
        if (ch == null || ch.Faction != Faction.Ally) return;
        if (!HasStaticItem("Mushroom")) return;

        mushroomDamageAccum.TryGetValue(ch, out float prev);
        float curr = prev + amount;

        int newThresholds = Mathf.FloorToInt(curr / 10f) - Mathf.FloorToInt(prev / 10f);
        if (newThresholds > 0)
        {
            ch.AddInstanceBonus(new StatDelta { maxHP = newThresholds * 1f });
            Debug.Log($"[SpecialItemTracker] Mushroom: {ch.name} +{newThresholds} MaxHP (total damage {curr:F1})");
        }

        mushroomDamageAccum[ch] = curr;
    }

    // ── Lemon ─────────────────────────────────────────────────────────────────

    void HandleAngryAdded(CharacterBase ch, float amount)
    {
        if (ch == null || ch.Faction != Faction.Ally) return;
        if (!HasStaticItem("Lemon")) return;

        lemonAngryAccum.TryGetValue(ch, out float prev);
        float curr = prev + amount;

        int newThresholds = Mathf.FloorToInt(curr / 10f) - Mathf.FloorToInt(prev / 10f);
        if (newThresholds > 0)
        {
            ch.AddInstanceBonus(new StatDelta { attackSpeed = newThresholds * 0.01f });
            Debug.Log($"[SpecialItemTracker] Lemon: {ch.name} +{newThresholds * 0.01f:F2} APS (total angry {curr:F1})");
        }

        lemonAngryAccum[ch] = curr;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    static bool HasStaticItem(string displayName)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var item in PlayerInventory.Instance.StaticItems)
            if (item != null && item.displayName == displayName) return true;
        return false;
    }
}
