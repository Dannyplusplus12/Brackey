// Sumo — Fat guardian. Power grows from loss and being well fed.
//
// ATTACK — Sumo Charge
//   Lao tới mục tiêu, deal damage khi chạm, đẩy ngược target ra.
//
// SKILL 1 — Grief Strength (permanent for the run)
//   Each ally death: +5 flat damage & +0.1 flat attack speed permanently (this Sumo only).
//
// SKILL 2 — Well Fed (permanent for the run)
//   Each time fed: +10 flat max HP permanently (this Sumo only).

using System.Collections;
using UnityEngine;

public class Sumo : CharacterBase
{
    [Header("Charge Attack")]
    [SerializeField] float chargeSpeed    = 450f;  // pixels/s during lunge
    [SerializeField] float knockbackForce = 350f;  // pixels/s initial knockback
    [SerializeField] float chargeDuration = 0.25f; // how long the lunge lasts

    private bool _isCharging;
    private int  _allyDeathCount;
    private int  _feedCount;

    // Debug props
    public int AllyDeathCount => _allyDeathCount;
    public int FeedCount      => _feedCount;

    protected override void OnEnable()
    {
        base.OnEnable();
        OnAllyDied += HandleAllyDeath;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        OnAllyDied -= HandleAllyDeath;
    }

    // ── Charge Attack ─────────────────────────────────────────────────────────

    protected override void ExecuteAttack(CharacterBase target)
    {
        Vector2 knockDir = (target.BodyCenter - BodyCenter).normalized;
        Vector2 targetPos = target.BodyCenter;

        base.ExecuteAttack(target); // deals damage + sprite flash + attackCount++

        target.ApplyKnockback(knockDir, knockbackForce);
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

    // ── Skill 1 — Grief Strength ──────────────────────────────────────────────

    void HandleAllyDeath(CharacterBase deadAlly)
    {
        if (IsDead || Faction != Faction.Ally) return;
        AddInstanceBonus(new StatDelta { damage = 5f, attackSpeed = 0.1f });
        _allyDeathCount++;
    }

    // ── Skill 2 — Well Fed ────────────────────────────────────────────────────

    public override void Feed()
    {
        base.Feed();
        AddInstanceBonus(new StatDelta { maxHP = 10f });
        _feedCount++;
    }
}
