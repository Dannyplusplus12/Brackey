using System.Collections.Generic;
using UnityEngine;

// Theo dõi các item có điều kiện kích hoạt dựa trên sự kiện per-character.
//
// ── Food items (chung cho mọi loại nhân vật) ─────────────────────────────────
//   Mushroom  — mỗi 10 damage nhận được: +1 MaxHP vĩnh viễn (per-char)
//   Lemon     — mỗi 10 angry tích lũy: +0.01 APS vĩnh viễn (per-char)
//
// ── Character-specific items ──────────────────────────────────────────────────
//   Shield    — [Sumo] mỗi lần bị đánh: gây 5% MaxHP lên kẻ tấn công
//   Lightning — [Sumo] mỗi lần bị đánh: +0.1 APS tạm thời đến hết wave (max 0.5/item)
//   Serum     — [Werewolf] mỗi lần bị bỏ đói (SkipFeed): +2 MaxHP vĩnh viễn
//   Claw      — [Werewolf] tính lại theo angry hiện tại: floor(angry/10) * 5% damage (động)
//   Flag Axe  — [Viking] mỗi 20 MaxHP instance bonus nhận được: +0.01 APS vĩnh viễn (per-char)
//   Hammer    — [Viking] mỗi wave sống sót: +1 damage vĩnh viễn (per-char)
//   Clover    — [Bagger] mỗi lần TriggerBeg: +10 MaxHP vĩnh viễn (per-Bagger)
//   Boots     — [Bagger] mỗi lần TriggerBeg: +0.04 APS + 5 damage vĩnh viễn (per-Bagger)
//
// Gắn script này lên GameObject Managers (cùng chỗ PlayerInventory).
// Tools > Items > Sync Item Pool tự gắn script này vào scene.
public class SpecialItemTracker : MonoBehaviour
{
    // ── Bộ đếm tích lũy cả game (Mushroom / Lemon / Serum) ────────────────────
    readonly Dictionary<CharacterBase, float> mushroomDamageAccum = new();
    readonly Dictionary<CharacterBase, float> lemonAngryAccum     = new();

    // ── Lightning: APS bonus đang áp per-character (để xoá khi hết wave) ──────
    readonly Dictionary<CharacterBase, float> lightningApsBonus   = new();

    // ── Claw: damage% bonus đang áp per-character (để diff khi tính lại) ──────
    readonly Dictionary<CharacterBase, float> clawDmgBonus        = new();

    // ── Flag Axe: MaxHP instance bonus tích lũy per-Viking ────────────────────
    readonly Dictionary<CharacterBase, float> flagAxeHpAccum      = new();

    void OnEnable()
    {
        CharacterBase.OnDamageTaken      += HandleDamageTaken;
        CharacterBase.OnAngryAdded       += HandleAngryAdded;
        CharacterBase.OnSkipFeed         += HandleSkipFeed;
        CharacterBase.OnInstanceMaxHPAdded += HandleInstanceMaxHPAdded;
        Bagger.OnBegTriggered            += HandleBegTriggered;
        WaveManager.OnWaveEnd            += ResetRoundBuffs;
    }

    void OnDisable()
    {
        CharacterBase.OnDamageTaken      -= HandleDamageTaken;
        CharacterBase.OnAngryAdded       -= HandleAngryAdded;
        CharacterBase.OnSkipFeed         -= HandleSkipFeed;
        CharacterBase.OnInstanceMaxHPAdded -= HandleInstanceMaxHPAdded;
        Bagger.OnBegTriggered            -= HandleBegTriggered;
        WaveManager.OnWaveEnd            -= ResetRoundBuffs;
    }

    void Update()
    {
        TickClaw();
    }

    // ── Mushroom ──────────────────────────────────────────────────────────────

    void HandleDamageTaken(CharacterBase victim, float amount, CharacterBase attacker)
    {
        if (victim == null || victim.IsDead) return;

        // Mushroom — chung cho mọi ally
        if (victim.Faction == Faction.Ally && HasStaticItem("Mushroom"))
            TickMushroom(victim, amount);

        // Shield — Sumo specific
        if (victim.Faction == Faction.Ally && attacker != null)
            TickShield(victim, attacker);

        // Lightning — Sumo specific (bị đánh thì tăng APS)
        if (victim.Faction == Faction.Ally)
            TickLightning(victim);
    }

    void TickMushroom(CharacterBase ch, float amount)
    {
        mushroomDamageAccum.TryGetValue(ch, out float prev);
        float curr = prev + amount;
        int gained = Mathf.FloorToInt(curr / 10f) - Mathf.FloorToInt(prev / 10f);
        if (gained > 0)
        {
            ch.AddInstanceBonus(new StatDelta { maxHP = gained });
            Debug.Log($"[Tracker] Mushroom: {ch.name} +{gained} MaxHP");
        }
        mushroomDamageAccum[ch] = curr;
    }

    // ── Shield ────────────────────────────────────────────────────────────────

    void TickShield(CharacterBase victim, CharacterBase attacker)
    {
        if (!TryGetCharItem("Shield", out var shieldItem)) return;
        if (victim.Stats != shieldItem.targetCharacterType) return;

        int count = CountStaticItems("Shield");
        float thorns = victim.MaxHP * 0.05f * count;
        // Không truyền attacker để tránh recursion (thorns không trigger Shield/Lightning)
        attacker.TakeDamage(thorns);
        Debug.Log($"[Tracker] Shield: {victim.name} phản {thorns:F1} lên {attacker.name}");
    }

    // ── Lightning ─────────────────────────────────────────────────────────────

    void TickLightning(CharacterBase victim)
    {
        if (!TryGetCharItem("Lightning", out var lightItem)) return;
        if (victim.Stats != lightItem.targetCharacterType) return;

        int count = CountStaticItems("Lightning");
        float maxBonus = 0.5f * count;

        lightningApsBonus.TryGetValue(victim, out float current);
        if (current >= maxBonus) return; // đã đạt cap

        float add = Mathf.Min(0.1f, maxBonus - current);
        victim.AddInstanceBonus(new StatDelta { attackSpeed = add });
        lightningApsBonus[victim] = current + add;
        Debug.Log($"[Tracker] Lightning: {victim.name} +{add:F2} APS (total {current + add:F2}/{maxBonus})");
    }

    // ── Lemon ─────────────────────────────────────────────────────────────────

    void HandleAngryAdded(CharacterBase ch, float amount)
    {
        if (ch == null || ch.Faction != Faction.Ally) return;
        if (!HasStaticItem("Lemon")) return;

        lemonAngryAccum.TryGetValue(ch, out float prev);
        float curr = prev + amount;
        int gained = Mathf.FloorToInt(curr / 10f) - Mathf.FloorToInt(prev / 10f);
        if (gained > 0)
        {
            ch.AddInstanceBonus(new StatDelta { attackSpeed = gained * 0.01f });
            Debug.Log($"[Tracker] Lemon: {ch.name} +{gained * 0.01f:F2} APS");
        }
        lemonAngryAccum[ch] = curr;
    }

    // ── Serum ─────────────────────────────────────────────────────────────────

    void HandleSkipFeed(CharacterBase ch)
    {
        if (ch == null || ch.Faction != Faction.Ally) return;
        if (!TryGetCharItem("Serum", out var serumItem)) return;
        if (ch.Stats != serumItem.targetCharacterType) return;

        int count = CountStaticItems("Serum");
        ch.AddInstanceBonus(new StatDelta { maxHP = 2f * count });
        Debug.Log($"[Tracker] Serum: {ch.name} +{2 * count} MaxHP (hungry)");
    }

    // ── Claw (Update mỗi frame) ───────────────────────────────────────────────

    void TickClaw()
    {
        if (!TryGetCharItem("Claw", out var clawItem)) return;
        int count = CountStaticItems("Claw");

        var allies = CharacterGrid.FindAllAlive(Faction.Ally);
        foreach (var ch in allies)
        {
            if (ch.Stats != clawItem.targetCharacterType) continue;

            float newBonus = Mathf.Floor(ch.CurrentAngry / 10f) * 0.05f * count;
            clawDmgBonus.TryGetValue(ch, out float prev);

            if (Mathf.Approximately(newBonus, prev)) continue;

            ch.RemoveInstanceBonus(new StatDelta { damagePercent = prev });
            ch.AddInstanceBonus(new StatDelta { damagePercent = newBonus });
            clawDmgBonus[ch] = newBonus;
        }
    }

    // ── Flag Axe ──────────────────────────────────────────────────────────────

    void HandleInstanceMaxHPAdded(CharacterBase ch, float amount)
    {
        if (ch == null || ch.Faction != Faction.Ally) return;
        if (!TryGetCharItem("Flag Axe", out var flagItem)) return;
        if (ch.Stats != flagItem.targetCharacterType) return;

        int count = CountStaticItems("Flag Axe");
        flagAxeHpAccum.TryGetValue(ch, out float prev);
        float curr = prev + amount;
        int steps = Mathf.FloorToInt(curr / 20f) - Mathf.FloorToInt(prev / 20f);
        if (steps > 0)
        {
            float apsGain = steps * 0.01f * count;
            ch.AddInstanceBonus(new StatDelta { attackSpeed = apsGain });
            Debug.Log($"[Tracker] Flag Axe: {ch.name} +{apsGain:F2} APS");
        }
        flagAxeHpAccum[ch] = curr;
    }

    // ── Hammer ────────────────────────────────────────────────────────────────

    void TickHammer()
    {
        if (!TryGetCharItem("Hammer", out var hammerItem)) return;
        int count = CountStaticItems("Hammer");

        var allies = CharacterGrid.FindAllAlive(Faction.Ally);
        foreach (var ch in allies)
        {
            if (ch.Stats != hammerItem.targetCharacterType) continue;
            ch.AddInstanceBonus(new StatDelta { damage = 1f * count });
            Debug.Log($"[Tracker] Hammer: {ch.name} +{count} damage (wave survived)");
        }
    }

    // ── Clover & Boots ────────────────────────────────────────────────────────

    void HandleBegTriggered(Bagger bagger)
    {
        if (bagger == null || bagger.IsDead || bagger.Faction != Faction.Ally) return;

        // Clover — +10 MaxHP per item
        if (TryGetCharItem("Clover", out var cloverItem) && bagger.Stats == cloverItem.targetCharacterType)
        {
            int count = CountStaticItems("Clover");
            bagger.AddInstanceBonus(new StatDelta { maxHP = 10f * count });
            Debug.Log($"[Tracker] Clover: {bagger.name} +{10 * count} MaxHP");
        }

        // Boots — +0.04 APS + 5 damage per item
        if (TryGetCharItem("Boots", out var bootsItem) && bagger.Stats == bootsItem.targetCharacterType)
        {
            int count = CountStaticItems("Boots");
            bagger.AddInstanceBonus(new StatDelta { attackSpeed = 0.04f * count, damage = 5f * count });
            Debug.Log($"[Tracker] Boots: {bagger.name} +{0.04f * count:F2} APS +{5 * count} dmg");
        }
    }

    // ── Wave end: xoá buff tạm thời ──────────────────────────────────────────

    void ResetRoundBuffs()
    {
        // Xoá Lightning APS buff
        foreach (var kv in lightningApsBonus)
            if (kv.Key != null)
                kv.Key.RemoveInstanceBonus(new StatDelta { attackSpeed = kv.Value });
        lightningApsBonus.Clear();

        // Hammer: thưởng wave sống sót
        TickHammer();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static bool HasStaticItem(string displayName)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var item in PlayerInventory.Instance.StaticItems)
            if (item != null && item.displayName == displayName) return true;
        return false;
    }

    // Trả về true và item đầu tiên tìm thấy (để lấy targetCharacterType).
    static bool TryGetCharItem(string displayName, out ItemData result)
    {
        result = null;
        if (PlayerInventory.Instance == null) return false;
        foreach (var item in PlayerInventory.Instance.StaticItems)
            if (item != null && item.displayName == displayName) { result = item; return true; }
        return false;
    }

    static int CountStaticItems(string displayName)
    {
        if (PlayerInventory.Instance == null) return 0;
        int n = 0;
        foreach (var item in PlayerInventory.Instance.StaticItems)
            if (item != null && item.displayName == displayName) n++;
        return n;
    }
}
