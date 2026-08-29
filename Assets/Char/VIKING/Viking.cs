// Viking — Battle-hardened warrior. Gets stronger the longer he survives.
//
// SKILL 1 — Power Strike
//   Every 3rd hit deals +100% damage (doubled for that strike only).
//
// SKILL 2 — Battle-Hardened (permanent for the run)
//   Each wave survived: +4 flat damage & +4 max HP.

public class Viking : CharacterBase
{
    protected override void ExecuteAttack(CharacterBase target)
    {
        // Skill 1: 3rd hit (attackCount is pre-increment here: 0, 1, 2, 3...)
        bool powerStrike = attackCount % 3 == 2;

        StatDelta bonus = default;
        if (powerStrike)
        {
            bonus = new StatDelta { damagePercent = 1.0f }; // +100% = double
            GlobalStatBonus.AddTypeBonus(stats, bonus);
        }

        base.ExecuteAttack(target); // deals damage, increments attackCount

        if (powerStrike)
            GlobalStatBonus.RemoveTypeBonus(stats, bonus);
    }

    public override void ExitCombat()
    {
        base.ExitCombat();

        // Skill 2: only reward if alive and still an ally at wave end
        if (!IsDead && Faction == Faction.Ally)
            GlobalStatBonus.AddTypeBonus(stats, new StatDelta { damage = 4f, maxHP = 4f });
    }
}
