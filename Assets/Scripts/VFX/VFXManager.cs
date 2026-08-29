using UnityEngine;

// Singleton quản lý toàn bộ VFX game-play.
// Gắn lên Managers GO trong scene. Gán VFXLibrary trong Inspector.
// Các system khác gọi VFXManager.Play*() — không cần giữ ref riêng.
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [SerializeField] VFXLibrary library;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Combat ───────────────────────────────────────────────────────────────

    /// <summary>Máu văng khi bị đánh. Particle count tự scale theo damage/maxHP.</summary>
    public static void PlayBloodHit(Vector2 position, float damage, float maxHP)
    {
        if (!TryGetLib(out var lib) || lib.bloodHit == null) return;
        var go = Instantiate(lib.bloodHit, position, Quaternion.identity);
        var ps = go.GetComponent<ParticleSystem>();
        if (ps == null) return;

        // Scale burst count: damage nhỏ → 3 hạt, damage = full maxHP → 20 hạt
        float ratio = Mathf.Clamp01(damage / Mathf.Max(maxHP, 1f));
        short count = (short)Mathf.RoundToInt(Mathf.Lerp(3f, 20f, ratio));
        var emission = ps.emission;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, count));
        ps.Play();
    }

    /// <summary>Máu + khói khi chết.</summary>
    public static void PlayDeathBurst(Vector2 position)
        => SpawnAt(TryGetLib(out var lib) ? lib.deathBurst : null, position);

    // ── Buff / Heal ───────────────────────────────────────────────────────────

    // Màu chuẩn cho từng loại buff — dùng khi gọi PlayBuffArrow
    public static readonly Color ColorHP     = new Color(0.2f, 0.9f, 0.2f); // xanh lá — HP / heal
    public static readonly Color ColorDamage = new Color(1f,   0.2f, 0.2f); // đỏ      — sát thương
    public static readonly Color ColorSpeed  = new Color(0.2f, 0.6f, 1f);   // xanh dương — tốc độ

    /// <summary>
    /// Mũi tên bay lên nhanh khi được buff hoặc hồi chỉ số.
    /// Truyền VFXManager.ColorHP / ColorDamage / ColorSpeed để chọn màu đúng loại.
    /// </summary>
    public static void PlayBuffArrow(Vector2 position, Color color)
    {
        if (!TryGetLib(out var lib) || lib.buffArrow == null) return;
        var go = Instantiate(lib.buffArrow, position, Quaternion.identity);
        var ps = go.GetComponent<ParticleSystem>();
        if (ps == null) return;
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color);
        ps.Play();
    }

    // ── Feed ─────────────────────────────────────────────────────────────────

    /// <summary>Icon vui bay lên chậm khi được feed đủ corn.</summary>
    public static void PlayFeedHappy(Vector2 position)
        => SpawnAt(TryGetLib(out var lib) ? lib.feedHappy : null, position);

    /// <summary>Icon tức bay lên chậm khi bị skip feed.</summary>
    public static void PlayFeedAngry(Vector2 position)
        => SpawnAt(TryGetLib(out var lib) ? lib.feedAngry : null, position);

    // ── Stun ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn StunStars làm child của target. Tự hủy sau duration giây.
    /// Trả về GO để caller có thể hủy sớm nếu cần.
    /// </summary>
    public static GameObject PlayStunStars(Transform parent, float duration = -1f)
    {
        if (!TryGetLib(out var lib) || lib.stunStars == null) return null;
        var go = Instantiate(lib.stunStars, parent.position, Quaternion.identity, parent);
        if (duration > 0f) Destroy(go, duration);
        return go;
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    /// <summary>Sparkle nhỏ khi nhân vật được mua vào trận.</summary>
    public static void PlaySpawnPop(Vector2 position)
        => SpawnAt(TryGetLib(out var lib) ? lib.spawnPop : null, position);

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void SpawnAt(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return;
        Instantiate(prefab, position, Quaternion.identity);
    }

    static bool TryGetLib(out VFXLibrary lib)
    {
        lib = Instance != null ? Instance.library : null;
        return lib != null;
    }
}
