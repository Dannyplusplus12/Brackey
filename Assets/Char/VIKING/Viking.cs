// Viking — Battle-hardened warrior. Gets stronger the longer he survives.
//
// SKILL 1 — Power Strike
//   Every 3rd hit deals +100% damage (doubled for that strike only).
//
// SKILL 2 — Battle-Hardened (permanent for the run)
//   Each wave survived: +4% damage & +4% max HP permanently (this Viking only).

public class Viking : CharacterBase
{
    protected override void ExecuteAttack(CharacterBase target)
    {
        // Skill 1: 3rd hit (attackCount is pre-increment here: 0, 1, 2, 3...)
        bool powerStrike = attackCount % 3 == 2;

        var bonus = default(StatDelta);
        if (powerStrike)
        {
            bonus = new StatDelta { damagePercent = 1.0f }; // +100% = double
            AddInstanceBonus(bonus); // per-instance: không ảnh hưởng Viking khác
        }

        base.ExecuteAttack(target); // deals damage, increments attackCount

        if (powerStrike)
            RemoveInstanceBonus(bonus);
    }

    public override void ExitCombat()
    {
        base.ExitCombat();

        // Skill 2: chỉ thưởng khi còn sống và vẫn là Ally cuối wave
        if (!IsDead && Faction == Faction.Ally)
            AddInstanceBonus(new StatDelta { damagePercent = 0.04f, maxHPPercent = 0.04f });
    }
}
