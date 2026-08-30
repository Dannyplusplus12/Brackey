// Pirate — Gold-hungry scoundrel. Luck grows with every battle survived.
//
// SKILL 1 — Plunder
//   Every even attack (2nd, 4th, 6th...): earnChance% to earn 5 corn.
//   Base chance = 5%, grows +4% per wave survived (Skill 2).
//   Key item: each Key in StaticItems adds +1% on top.
//
// SKILL 2 — Sea Dog's Fortune (permanent for the run)
//   Each wave survived as Ally: +1% plunder chance (no cap).

using UnityEngine;

public class Pirate : CharacterBase
{
    const int   PlunderReward     = 5;
    const float BaseEarnChance    = 0.05f;  // 5% base
    const float ChanceGainPerWave = 0.04f;  // +4% per wave survived
    const float KeyBonusPerKey    = 0.01f;  // +1% per Key item

    [Header("Key Item")]
    [Tooltip("Kéo Key ItemData vào đây — Pirate tự đếm số Key trong inventory")]
    [SerializeField] ItemData keyItemData;

    float _earnChance = BaseEarnChance;

    // Debug / UI readable — tính luôn bonus từ Key hiện tại
    public float EarnChance => _earnChance + CountKeyBonus();

    // ── Skill 1 — Plunder ─────────────────────────────────────────────────────

    protected override void ExecuteAttack(CharacterBase target)
    {
        base.ExecuteAttack(target); // deals damage, increments attackCount

        // attackCount post-incremented: fires on 2nd, 4th, 6th...
        if (attackCount % 2 == 0)
        {
            float chance = _earnChance + CountKeyBonus();
            if (Random.value < chance)
            {
                PlayerWallet.Instance?.Earn(PlunderReward);
                PlaySkillVFX(VFXManager.ColorHP);
            }
        }
    }

    // ── Skill 2 — Sea Dog's Fortune ───────────────────────────────────────────

    public override void ExitCombat()
    {
        base.ExitCombat();

        if (!IsDead && Faction == Faction.Ally)
            _earnChance += ChanceGainPerWave;
    }

    // ── Key bonus helper ───────────────────────────────────────────────────────

    float CountKeyBonus()
    {
        if (keyItemData == null || PlayerInventory.Instance == null) return 0f;

        int count = 0;
        foreach (var item in PlayerInventory.Instance.StaticItems)
            if (item == keyItemData) count++;

        return count * KeyBonusPerKey;
    }
}
