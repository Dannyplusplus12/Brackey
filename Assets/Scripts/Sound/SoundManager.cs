using UnityEngine;

// Singleton phát toàn bộ sound trong game.
// Gắn lên Managers GO trong scene. Gán SoundLibrary trong Inspector.
// Các system khác gọi SoundManager.Play(SoundId) — không cần giữ ref.
//
// Dùng pool AudioSource để nhiều sound có thể overlap (hit + attack cùng lúc).
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] SoundLibrary library;

    [Tooltip("Số AudioSource trong pool. Tăng nếu thấy sound bị cắt khi nhiều char đánh cùng lúc.")]
    [SerializeField, Range(4, 32)] int poolSize = 12;

    [Tooltip("Master volume của toàn bộ game sound (0-1).")]
    [SerializeField, Range(0f, 1f)] float masterVolume = 1f;

    AudioSource[] _pool;
    int _poolIndex;

    // AudioSource riêng cho sound looping (ví dụ: wheel spin).
    // Chỉ 1 looping sound tại 1 thời điểm — đủ dùng cho bánh xe gacha.
    AudioSource _loopSource;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Tạo pool AudioSource
        _pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"SoundSource_{i}");
            go.transform.SetParent(transform);
            _pool[i] = go.AddComponent<AudioSource>();
            _pool[i].playOnAwake = false;
        }

        // AudioSource riêng cho looping
        var loopGo = new GameObject("SoundSource_Loop");
        loopGo.transform.SetParent(transform);
        _loopSource = loopGo.AddComponent<AudioSource>();
        _loopSource.playOnAwake = false;
        _loopSource.loop = true;
    }

    void OnEnable()  => SubscribeEvents();
    void OnDisable() => UnsubscribeEvents();

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Event subscription ───────────────────────────────────────────────────

    void SubscribeEvents()
    {
        CharacterBase.OnDamageTaken       += HandleDamageTaken;
        CharacterBase.OnCharacterAttacked += HandleAttack;
        CharacterBase.OnCharacterHealed   += HandleHeal;
        CharacterBase.OnAllyDied          += HandleDeath;
        CharacterBase.OnEnemyDied         += HandleDeath;
        ShopOfferManager.OnItemBought     += HandleBuy;
        ShopOfferManager.OnRerollUsed     += HandleReroll;
        WaveManager.OnWaveStart           += HandleWaveStart;
    }

    void UnsubscribeEvents()
    {
        CharacterBase.OnDamageTaken       -= HandleDamageTaken;
        CharacterBase.OnCharacterAttacked -= HandleAttack;
        CharacterBase.OnCharacterHealed   -= HandleHeal;
        CharacterBase.OnAllyDied          -= HandleDeath;
        CharacterBase.OnEnemyDied         -= HandleDeath;
        ShopOfferManager.OnItemBought     -= HandleBuy;
        ShopOfferManager.OnRerollUsed     -= HandleReroll;
        WaveManager.OnWaveStart           -= HandleWaveStart;
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    void HandleDamageTaken(CharacterBase victim, float amount, CharacterBase attacker) => Play(SoundId.Hit);
    void HandleAttack(CharacterBase attacker)                                          => Play(SoundId.Attack);
    void HandleHeal(CharacterBase character)                                           => Play(SoundId.Heal);
    void HandleDeath(CharacterBase character)                                          => Play(SoundId.Death);
    void HandleBuy(ItemData item)                                                      => Play(SoundId.Buy);
    void HandleReroll()                                                                => Play(SoundId.Reroll);
    void HandleWaveStart()                                                             => Play(SoundId.WaveStart);

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Phát sound theo id. Gọi từ bất kỳ đâu — SoundManager.Play(SoundId.UIClick).
    /// </summary>
    public static void Play(SoundId id)
    {
        if (Instance == null) return;
        Instance.PlayInternal(id);
    }

    // ── Public API (looping) ─────────────────────────────────────────────────

    /// <summary>
    /// Phát sound theo vòng lặp (ví dụ: wheel spin).
    /// Gọi StopLooping() để dừng.
    /// </summary>
    public static void PlayLooping(SoundId id)
    {
        if (Instance == null) return;
        Instance.PlayLoopingInternal(id);
    }

    /// <summary>Dừng sound đang loop, fade out trong <paramref name="fadeDuration"/> giây.</summary>
    public static void StopLooping(float fadeDuration = 0.3f)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.FadeOutLoop(fadeDuration));
    }

    void PlayLoopingInternal(SoundId id)
    {
        if (library == null) return;
        var entry = library.Get(id);
        if (entry == null || entry.clips == null || entry.clips.Length == 0) return;

        var clip = entry.clips[Random.Range(0, entry.clips.Length)];
        if (clip == null) return;

        _loopSource.clip   = clip;
        _loopSource.volume = entry.volume * masterVolume;
        _loopSource.pitch  = Random.Range(entry.pitchMin, entry.pitchMax);
        _loopSource.loop   = true;
        _loopSource.Play();
    }

    System.Collections.IEnumerator FadeOutLoop(float duration)
    {
        float startVol = _loopSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _loopSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        _loopSource.Stop();
        _loopSource.volume = startVol; // reset cho lần sau
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    void PlayInternal(SoundId id)
    {
        if (library == null) return;

        var entry = library.Get(id);
        if (entry == null || entry.clips == null || entry.clips.Length == 0) return;

        // Random clip để tránh lặp đơn điệu
        var clip = entry.clips[Random.Range(0, entry.clips.Length)];
        if (clip == null) return;

        // Lấy AudioSource tiếp theo trong pool (round-robin)
        var source = NextSource();
        source.clip       = clip;
        source.volume     = entry.volume * masterVolume;
        source.pitch      = Random.Range(entry.pitchMin, entry.pitchMax);
        source.Play();
    }

    AudioSource NextSource()
    {
        var src = _pool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _pool.Length;
        return src;
    }
}
