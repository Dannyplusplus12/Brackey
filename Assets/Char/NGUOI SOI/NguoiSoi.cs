// NguoiSoi — Semi-tamed beast. Scales infinitely with rage and time.
//
// SKILL 1 — Per-Hit Frenzy (temporary, resets each round)
//   Each successful attack: +5% attack speed & +5% damage until round ends.
//
// SKILL 2 — Rage Carving (permanent for the run)
//   Each time angry increases (any amount = 1 trigger): +1 flat damage forever.

public class NguoiSoi : CharacterBase
{
    private StatDelta _frenzyDelta;
    private int       _frenzyHits;
    private int       _rageTriggers; // total angry triggers this run

    // Debug props — read by DebugOverlay
    public int   FrenzyHits       => _frenzyHits;
    public float FrenzyDmgPct     => _frenzyDelta.damagePercent;        // e.g. 0.35 = +35%
    public float FrencySpdPct     => _frenzyDelta.attackSpeedPercent;
    public float PermDmgBonus     => GlobalStatBonus.GetTypeBonus(stats).damage;
    public int   RageTriggers     => _rageTriggers;
    public float EffectiveAtkSpeed => EffectiveAPS;                     // expose protected

    protected override void ExecuteAttack(CharacterBase target)
    {
        base.ExecuteAttack(target);

        // Skill 1: +5% atkspd & +5% dmg per hit (stripped at ExitCombat)
        var hit = new StatDelta { attackSpeedPercent = 0.05f, damagePercent = 0.05f };
        GlobalStatBonus.AddTypeBonus(stats, hit);
        _frenzyDelta.Add(hit);
        _frenzyHits++;
    }

    public override void AddAngry(float amount, AngryReason reason)
    {
        base.AddAngry(amount, reason);
        if (amount <= 0f) return;

        // Skill 2: each angry trigger → +1 permanent flat damage
        GlobalStatBonus.AddTypeBonus(stats, new StatDelta { damage = 1f });
        _rageTriggers++;
    }

    public override void ExitCombat()
    {
        // Strip temp frenzy — Skill 1 is this-round only
        GlobalStatBonus.RemoveTypeBonus(stats, _frenzyDelta);
        _frenzyDelta = default;
        _frenzyHits  = 0;
        base.ExitCombat();
    }
}
