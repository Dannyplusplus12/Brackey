using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh máu tổng kẻ thù.
///
/// Logic:
/// - Wave start → snapshot tổng maxHP + currentHP của tất cả enemy hiện có.
/// - Enemy mới spawn thêm → cộng maxHP + currentHP vào tổng (max tăng).
/// - Damage → trừ thẳng _totalCurrentHP (không recalc toàn bộ).
/// - Enemy chết → currentHP của nó đã bị trừ hết từ damage, max không đổi.
/// - Ví dụ: 2 enemy 100HP mỗi con. 1 con mất 50HP, 1 con chết (100HP) → còn 50/200 = 25%.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image fillImage;

    [Header("Settings")]
    [SerializeField] bool hideWhenNoWave = true;

    CanvasGroup _group;
    float _totalMaxHP;
    float _totalCurrentHP;
    bool  _waveActive;

    // Track enemy đã đăng ký để không cộng trùng
    readonly HashSet<CharacterBase> _tracked = new();

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable   = false;
    }

    void OnEnable()
    {
        WaveManager.OnWaveStart     += HandleWaveStart;
        WaveManager.OnWaveEnd       += HandleWaveEnd;
        CharacterBase.OnDamageTaken += HandleDamageTaken;

        _waveActive = WaveManager.IsWaveActive;
        if (_waveActive) SnapshotAllEnemies();
        Refresh();
    }

    void OnDisable()
    {
        WaveManager.OnWaveStart     -= HandleWaveStart;
        WaveManager.OnWaveEnd       -= HandleWaveEnd;
        CharacterBase.OnDamageTaken -= HandleDamageTaken;
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    void HandleWaveStart()
    {
        _waveActive = true;
        SnapshotAllEnemies();
        Refresh();
    }

    void HandleWaveEnd()
    {
        _waveActive = false;
        Refresh();
    }

    void HandleDamageTaken(CharacterBase target, float amount, CharacterBase _attacker)
    {
        if (target == null || target.Faction != Faction.Enemy) return;

        // Enemy mới spawn chưa được track → đăng ký trước khi trừ máu
        if (!_tracked.Contains(target))
            RegisterEnemy(target);

        // OnDamageTaken bắn TRƯỚC khi CurrentHP bị trừ → target.CurrentHP vẫn là giá trị cũ
        // Clamp để tránh trừ thừa khi overkill
        float actual = Mathf.Min(amount, target.CurrentHP);
        _totalCurrentHP = Mathf.Max(0f, _totalCurrentHP - actual);
        Refresh();
    }

    // ── LateUpdate: chỉ bắt enemy mới spawn chưa từng bị đánh ────────────────

    void LateUpdate()
    {
        if (!_waveActive) return;

        bool anyNew = false;
        foreach (var e in CharacterGrid.GetAll(Faction.Enemy))
        {
            if (e != null && !_tracked.Contains(e))
            {
                RegisterEnemy(e);
                anyNew = true;
            }
        }
        if (anyNew) Refresh();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Lấy snapshot tất cả enemy hiện có — gọi 1 lần khi wave start.</summary>
    void SnapshotAllEnemies()
    {
        _totalMaxHP     = 0f;
        _totalCurrentHP = 0f;
        _tracked.Clear();

        foreach (var e in CharacterGrid.GetAll(Faction.Enemy))
            if (e != null) RegisterEnemy(e);
    }

    /// <summary>Thêm 1 enemy vào tracking, cộng HP vào tổng.</summary>
    void RegisterEnemy(CharacterBase e)
    {
        _tracked.Add(e);
        _totalMaxHP     += e.MaxHP;
        _totalCurrentHP += e.CurrentHP;
    }

    void Refresh()
    {
        bool shouldShow = !hideWhenNoWave || (_waveActive && _totalMaxHP > 0f);
        if (_group != null)
            _group.alpha = shouldShow ? 1f : 0f;

        float ratio = (_totalMaxHP > 0f) ? Mathf.Clamp01(_totalCurrentHP / _totalMaxHP) : 0f;

        if (fillImage != null)
        {
            // anchorMax.x = ratio — đáng tin hơn fillAmount, không phụ thuộc Image Type
            RectTransform rt = fillImage.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(ratio, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
