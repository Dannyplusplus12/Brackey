// Pirate — Gold-hungry scoundrel. Luck grows with every battle survived.
//
// SKILL 1 — Plunder
//   Every even attack (2nd, 4th, 6th...): _earnChance% to earn 5 corn.
//   Chance starts at 5% and grows with Skill 2.
//
// SKILL 2 — Sea Dog's Fortune (permanent for the run)
//   Each wave survived as Ally: +1% plunder chance (no cap).

using UnityEngine;

public class Pirate : CharacterBase
{
    const int   PlunderReward       = 5;
    const float BaseEarnChance      = 0.05f;  // 5% starting
    const float ChanceGainPerWave   = 0.01f;  // +1% per wave survived

    float _earnChance = BaseEarnChance;

    // Debug / UI readable
    public float EarnChance => _earnChance;

    // ── Skill 1 — Plunder ─────────────────────────────────────────────────────

    protected override void ExecuteAttack(CharacterBase target)
    {
        base.ExecuteAttack(target); // deals damage, increments attackCount

        // attackCount is post-incremented: fires on 2nd, 4th, 6th...
        if (attackCount % 2 == 0)
        {
            if (Random.value < _earnChance)
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

        // Chỉ tính khi còn sống và vẫn là Ally cuối wave
        if (!IsDead && Faction == Faction.Ally)
            _earnChance += ChanceGainPerWave;
    }
}
