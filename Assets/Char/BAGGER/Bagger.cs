// Bagger — Cheap coward. Useless alone, but begs his way into making allies stronger.
//
// SKILL 1 — Beg
//   Triggers on: every 4th hit OR when any ally dies.
//   Effect: +50% damage to the nearest ally (temporary — stripped at round end).
//   Can stack: each trigger buffs whoever is nearest at that moment.
//
// SKILL 2 — Terrified Growth (permanent for the run)
//   Each time angry increases (any amount = 1 trigger): +0.1 flat attack speed.
//
// STUB — Starved Trigger
//   When SkipFeed fires, something special happens. Not designed yet.

using System.Collections.Generic;
using UnityEngine;

public class Bagger : CharacterBase
{
    // Tracks every Beg buff applied this round: specific ally instance → delta applied.
    // Dùng per-instance bonus để buff đúng 1 nhân vật, không lan sang cùng loại.
    private readonly List<(CharacterBase ally, StatDelta delta)> _begBuffs = new();
    private int _rageTriggers;

    // Debug props
    public int   BegBuffCount => _begBuffs.Count;
    public int   RageTriggers => _rageTriggers;
    public float PermSpdBonus => GlobalStatBonus.GetTypeBonus(stats).attackSpeed;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        OnAllyDied += HandleBegOnAllyDeath;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        OnAllyDied -= HandleBegOnAllyDeath;
    }

    // ── Skill 1 — Beg ──────────────────────────────────────────────────────────

    protected override void ExecuteAttack(CharacterBase target)
    {
        base.ExecuteAttack(target); // deals damage, increments attackCount

        if (attackCount % 4 == 0) // fires on 4th, 8th, 12th... (post-increment)
            TriggerBeg();
    }

    void HandleBegOnAllyDeath(CharacterBase deadAlly)
    {
        if (IsDead || Faction != Faction.Ally) return;
        TriggerBeg();
    }

    void TriggerBeg()
    {
        var ally = CharacterGrid.FindNearest(BodyCenter, Faction.Ally, exclude: this);
        if (ally == null) return;

        var buff = new StatDelta { damagePercent = 0.5f };
        ally.AddInstanceBonus(buff);          // buff đúng 1 instance, không lan type khác
        _begBuffs.Add((ally, buff));

        VFXManager.PlayBuffArrow(ally.BodyCenter, VFXManager.ColorDamage); // VFX trên ally
    }

    // ── Skill 2 — Terrified Growth ─────────────────────────────────────────────

    public override void AddAngry(float amount, AngryReason reason)
    {
        base.AddAngry(amount, reason);
        if (amount <= 0f) return;

        GlobalStatBonus.AddTypeBonus(stats, new StatDelta { attackSpeed = 0.1f });
        _rageTriggers++;

        // STUB — Starved Trigger
        // if (reason == AngryReason.Hungry)
        //     TriggerStarvedEffect(); // TODO: design this
    }

    // ── Round End ──────────────────────────────────────────────────────────────

    public override void ExitCombat()
    {
        // Strip all Beg buffs — Skill 1 is temporary
        foreach (var (ally, delta) in _begBuffs)
            if (ally != null) ally.RemoveInstanceBonus(delta);
        _begBuffs.Clear();

        base.ExitCombat();
    }
}
