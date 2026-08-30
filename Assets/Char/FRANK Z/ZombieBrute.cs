// ZombieBrute (FRANK Z) — Shambling titan that calls for reinforcements.
//
// SKILL — Summon Pack
//   Every even attack (2nd, 4th, 6th...): spawns 1 Dog Z at a random
//   position near the Brute (faction Enemy). The summoned Dog Z enters
//   combat immediately if the wave is active.

using UnityEngine;

public class ZombieBrute : CharacterBase
{
    [Header("Summon")]
    [Tooltip("Kéo Dog Z prefab vào đây")]
    [SerializeField] GameObject dogZPrefab;
    [Tooltip("Bán kính spawn ngẫu nhiên quanh Brute")]
    [SerializeField] float summonRadius = 0.8f;

    // ── Skill — Summon Pack ────────────────────────────────────────────────────

    protected override void ExecuteAttack(CharacterBase target)
    {
        base.ExecuteAttack(target); // deals damage, increments attackCount

        // attackCount post-incremented: fires on 2nd, 4th, 6th...
        if (attackCount % 2 == 0)
            SummonDogZ();
    }

    void SummonDogZ()
    {
        if (dogZPrefab == null)
        {
            UnityEngine.Debug.LogWarning("[ZombieBrute] dogZPrefab chưa gán trong Inspector!");
            return;
        }

        Vector2 offset   = Random.insideUnitCircle.normalized * Random.Range(0.3f, summonRadius);
        Vector2 spawnPos = (Vector2)transform.position + offset;

        GameObject go = Instantiate(dogZPrefab);
        var character = go.GetComponent<CharacterBase>();
        if (character == null) { Destroy(go); return; }

        // Phải gọi trước Start() chạy (Start mới register vào CharacterGrid)
        character.ForceSetFaction(Faction.Enemy);
        character.SetSpawnPosition(spawnPos);   // tự EnterCombat() nếu wave đang chạy

        PlaySkillVFX(VFXManager.ColorDamage);
    }
}
