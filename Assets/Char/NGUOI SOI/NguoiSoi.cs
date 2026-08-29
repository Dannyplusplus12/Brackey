// NguoiSoi — Semi-tamed beast. Scales with combat and hunger.
//
// SKILL 1 — Per-Hit Frenzy (temporary, resets each round)
//   Each attack: +0.1 flat attack speed, +5% damage, +10 flat move speed
//   — all stripped at round end.
//
// SKILL 2 — Hunger Rage (temporary, resets each round)
//   Each time SkipFeed fires (hungry, amount > 0): +20% damage until round ends.

public class NguoiSoi : CharacterBase
{
    private StatDelta _frenzyDelta;  // tổng buff per-hit tích lũy trong round
    private StatDelta _hungryDelta;  // tổng buff từ đói tích lũy trong round
    private int       _frenzyHits;

    // Debug props
    public int   FrenzyHits   => _frenzyHits;
    public float FrenzyDmgPct => _frenzyDelta.damagePercent;
    public float FrenzySpd    => _frenzyDelta.moveSpeed;
    public float FrenzyAPS    => _frenzyDelta.attackSpeed;

    protected override void ExecuteAttack(CharacterBase target)
    {
        base.ExecuteAttack(target);

        // Skill 1: mỗi đòn +0.1 atkspd flat, +5% damage, +10 speed — hết round bị xóa
        var hit = new StatDelta { attackSpeed = 0.1f, damagePercent = 0.05f, moveSpeed = 10f };
        AddInstanceBonus(hit);
        _frenzyDelta.Add(hit);
        _frenzyHits++;
    }

    public override void AddAngry(float amount, AngryReason reason)
    {
        base.AddAngry(amount, reason);
        if (amount <= 0f) return;

        // Skill 2: chỉ kích hoạt khi lý do là đói (angryPerHunger > 0 mới đến đây)
        if (reason == AngryReason.Hungry)
        {
            var buff = new StatDelta { damagePercent = 0.2f };
            AddInstanceBonus(buff);
            _hungryDelta.Add(buff);
        }
    }

    public override void ExitCombat()
    {
        // Xóa toàn bộ buff tạm thời của round vừa xong
        RemoveInstanceBonus(_frenzyDelta);
        RemoveInstanceBonus(_hungryDelta);
        _frenzyDelta = default;
        _hungryDelta = default;
        _frenzyHits  = 0;
        base.ExitCombat();
    }
}
