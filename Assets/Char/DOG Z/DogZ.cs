// DogZ — Fast, erratic fighter. No on-hit skill, but switches targets to keep enemies off-balance.
//
// MECHANIC — Frantic Chase
//   After each attack there is a 30% chance to abandon the current target
//   and sprint toward a random living enemy instead.
//
// MOVEMENT — Momentum Steering
//   Movement direction lerps toward the desired direction each frame, giving
//   DogZ a "skidding dog" feel — it may overshoot and have to curve back when
//   targets switch suddenly.
//
// VISUAL — Asymmetric Sway
//   Head bobs noticeably higher than the tail; gives a galloping, dog-like silhouette.

using UnityEngine;
using System.Collections.Generic;

public class DogZ : CharacterBase
{
    [Header("Frantic Chase")]
    [SerializeField, Range(0f, 1f)] float targetSwitchChance = 0.30f;

    [Header("Momentum Steering")]
    [SerializeField] float turnSpeed = 6f; // higher = sharper turns; lower = more skidding

    [Header("Bounce Feel")]
    [SerializeField] float bounceGravity = 0.45f; // <1 = floaty (lingers at peak); 1 = normal; >1 = heavy/snappy

    private Vector2 _smoothDir; // current momentum direction, lerped each frame

    // ── Mechanic — Frantic Chase ───────────────────────────────────────────────

    protected override void ExecuteAttack(CharacterBase target)
    {
        base.ExecuteAttack(target);

        if (Random.value >= targetSwitchChance) return;

        // Collect living enemies, excluding the one just hit
        Faction opposing  = Faction == Faction.Ally ? Faction.Enemy : Faction.Ally;
        List<CharacterBase> candidates = CharacterGrid.FindAllAlive(opposing);
        candidates.RemoveAll(e => e == target);
        if (candidates.Count == 0) return;

        currentTarget = candidates[Random.Range(0, candidates.Count)];
        EnterState(CharacterState.Seeking); // sprint toward new target (will curve smoothly)
    }

    // ── Movement — Momentum Steering ──────────────────────────────────────────
    //
    // MoveToward is called every Seeking frame with the raw dir to target.
    // We lerp _smoothDir toward it so the dog curves rather than snapping direction.
    // If the target switches, _smoothDir carries previous momentum and curves around.

    protected override Vector2 MoveToward(Vector2 target)
    {
        Vector2 desired = base.MoveToward(target); // normalized dir, or zero if already there
        _smoothDir = Vector2.Lerp(_smoothDir, desired, Time.deltaTime * turnSpeed);
        return _smoothDir;
    }

    public override void ExitCombat()
    {
        _smoothDir = Vector2.zero; // clear momentum between rounds
        base.ExitCombat();
    }

    // ── Visual — Asymmetric Sway ───────────────────────────────────────────────
    //
    // Standard bounce = Abs(sin) * h — perfectly symmetric.
    // Dog bounce: when sin > 0 the head side rises (×1.8); when sin < 0 the tail
    // side rises (×0.35), so the tail barely lifts while the head lunges upward.

    protected override float ComputeSwayBounce(float sinValue, bool moving)
    {
        if (!moving) return 0f;
        float h = stats.swayBounceHeight;
        // Positive tilt (sin > 0) = clockwise = RIGHT side rises.
        // Facing left (flipX=false): head on LEFT → head rises when sin < 0 → negate.
        // Facing right (flipX=true): head on RIGHT → head rises when sin > 0 → keep.
        float s         = (spriteRenderer != null && spriteRenderer.flipX) ? sinValue : -sinValue;
        // Apply bounceGravity: power < 1 = floaty (curve hugs the peak longer).
        float headPhase = Mathf.Pow(Mathf.Max(0f, s),  bounceGravity);
        float tailPhase = Mathf.Pow(Mathf.Max(0f, -s), bounceGravity);
        return headPhase * h * 1.8f + tailPhase * h * 0.35f;
    }
}
