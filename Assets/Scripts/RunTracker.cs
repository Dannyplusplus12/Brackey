using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour: theo dõi toàn bộ stats trong một run.
/// Tự subscribe các static event khi scene load, KHÔNG cần gắn thêm gì trong Editor.
/// </summary>
public class RunTracker : MonoBehaviour
{
    public static RunTracker Instance { get; private set; }

    // ── Stats ─────────────────────────────────────────────────────────────────
    public int   WavesStarted  { get; private set; }
    public int   EnemiesKilled { get; private set; }
    public int   CharsBought   { get; private set; }
    public int   ItemsBought   { get; private set; }
    public float DamageDealt   { get; private set; }
    public int   CornEarned    { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        WaveManager.OnWaveStart            += OnWaveStart;
        CharacterBase.OnEnemyDied          += OnEnemyDied;
        CharacterBase.OnDamageTaken        += OnDamageTaken;
        ShopOfferManager.OnItemBought      += OnItemBought;
        PlayerWallet.OnCornDelta           += OnCornDelta;
    }

    void OnDisable()
    {
        WaveManager.OnWaveStart            -= OnWaveStart;
        CharacterBase.OnEnemyDied          -= OnEnemyDied;
        CharacterBase.OnDamageTaken        -= OnDamageTaken;
        ShopOfferManager.OnItemBought      -= OnItemBought;
        PlayerWallet.OnCornDelta           -= OnCornDelta;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    void OnWaveStart() => WavesStarted++;

    void OnEnemyDied(CharacterBase _) => EnemiesKilled++;

    void OnDamageTaken(CharacterBase victim, float amount, CharacterBase attacker)
    {
        // Chỉ đếm sát thương do ally gây ra
        if (attacker != null && attacker.Faction == Faction.Ally)
            DamageDealt += amount;
    }

    void OnItemBought(ItemData item)
    {
        if (item == null) return;
        if (item.itemType == ItemType.Character)
            CharsBought++;
        else
            ItemsBought++;
    }

    void OnCornDelta(int delta)
    {
        if (delta > 0) CornEarned += delta;
    }
}
