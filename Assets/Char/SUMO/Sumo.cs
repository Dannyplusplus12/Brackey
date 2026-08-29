// Sumo — Fat guardian. Power comes from standing beside allies.
//
// SKILL 1 — Guardian Aura (temporary, active while near an ally)
//   When at least 1 ally is within range: +20% max HP & +20% damage.
//   Buff drops the moment no ally is nearby. Stripped at round end regardless.
//
// SKILL 2 — Well Fed (permanent for the run)
//   Each time he is fed: +1% max HP forever.
//
// ATTACK — Sumo Charge
//   Lunges toward the target, deals damage on impact, knocks target back.

using System.Collections;
using UnityEngine;

public class Sumo : CharacterBase
{
    [Header("Skill 1 — Guardian Aura")]
    [SerializeField] float auraRange = 100f;

    [Header("Charge Attack")]
    [SerializeField] float chargeSpeed    = 450f;  // pixels/s during lunge
    [SerializeField] float knockbackForce = 350f;  // pixels/s initial knockback
    [SerializeField] float chargeDuration = 0.25f; // how long the lunge lasts

    static readonly StatDelta AuraDelta = new StatDelta { maxHPPercent = 0.2f, damagePercent = 0.2f };

    private bool _auraActive;
    private bool _isCharging; // true trong lúc lunge — chặn TickAttacking reset sprite
    private int  _feedCount;

    // Debug props
    public bool  AuraActive     => _auraActive;
    public int   FeedCount      => _feedCount;
    public float PermHpBonusPct => GlobalStatBonus.GetTypeBonus(stats).maxHPPercent * 100f;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(AuraLoop());
    }

    // ── Charge Attack ─────────────────────────────────────────────────────────

    protected override void ExecuteAttack(CharacterBase target)
    {
        // Capture position before base moves anything
        Vector2 targetPos = target.BodyCenter;
        Vector2 knockDir  = (target.BodyCenter - BodyCenter).normalized;

        base.ExecuteAttack(target); // deals damage + sprite flash + attackCount++

        // Knockback the target
        target.ApplyKnockback(knockDir, knockbackForce);

        // Visual lunge toward where the target was
        StartCoroutine(ChargeLunge(targetPos));
    }

    protected override void TickAttacking()
    {
        if (_isCharging) return; // lunge đang chạy — không check range, không reset sprite
        base.TickAttacking();
    }

    IEnumerator ChargeLunge(Vector2 targetPos)
    {
        _isCharging = true;

        Vector2 dir     = (targetPos - (Vector2)transform.position).normalized;
        float   elapsed = 0f;

        while (elapsed < chargeDuration && !IsDead)
        {
            transform.position += (Vector3)(dir * chargeSpeed * Time.deltaTime);
            CharacterGrid.UpdatePosition(this);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _isCharging = false;
    }

    // ── Guardian Aura ─────────────────────────────────────────────────────────

    IEnumerator AuraLoop()
    {
        var wait = new WaitForSeconds(0.25f);
        while (!IsDead)
        {
            RefreshAura();
            yield return wait;
        }
        SetAura(false);
    }

    void RefreshAura()
    {
        var nearest = CharacterGrid.FindNearest(BodyCenter, Faction.Ally, exclude: this);
        bool shouldBeOn = nearest != null &&
                          Vector2.Distance(BodyCenter, nearest.BodyCenter) <= auraRange;
        SetAura(shouldBeOn);
    }

    void SetAura(bool on)
    {
        if (on == _auraActive) return;
        if (on) GlobalStatBonus.AddTypeBonus(stats, AuraDelta);
        else    GlobalStatBonus.RemoveTypeBonus(stats, AuraDelta);
        _auraActive = on;
    }

    // ── Well Fed ──────────────────────────────────────────────────────────────

    public override void Feed()
    {
        base.Feed();
        GlobalStatBonus.AddTypeBonus(stats, new StatDelta { maxHPPercent = 0.01f });
        _feedCount++;
    }

    public override void ExitCombat()
    {
        SetAura(false); // Skill 1 is temporary — strip at round end
        base.ExitCombat();
    }
}
