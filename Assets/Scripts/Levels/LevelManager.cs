using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Quản lý hệ thống level: spawn enemy theo LevelData khi bắt đầu Shop.
// Level 0 được spawn ngay khi game bắt đầu (wave 1).
// Mỗi lần vào Shop → dọn enemy thừa → spawn level tiếp theo.
// Gắn lên bất kỳ GO nào trong scene (khuyên gắn vào Managers).
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Levels — kéo LevelData theo thứ tự từ trên xuống")]
    [SerializeField] LevelData[] levels;

    [Header("Spawn Bounds (hình chữ nhật — vàng trong Gizmos)")]
    [Tooltip("Hình chữ nhật giới hạn vùng spawn (X/Y = góc dưới-trái, W/H = kích thước)")]
    [SerializeField] Rect spawnBounds = new Rect(-12f, -8f, 24f, 16f);

    [Header("Exclusion Zone (hình tròn đỏ — lấy từ ShopArea nếu có)")]
    [Tooltip("Fallback radius nếu ShopArea.Instance == null")]
    [SerializeField] float fallbackExcludeRadius = 4f;

    // ─── state ───────────────────────────────────────────────────────────────
    int currentLevelIndex = -1; // -1 = chưa spawn lần nào
    bool _harderUsedThisRound;  // true nếu đã bấm Harder trong lượt Shop này
    readonly List<CharacterBase> spawnedEnemies = new();

    // ─── lifecycle ───────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()  => GameManager.OnGameStateChanged += OnGameStateChanged;
    void OnDisable() => GameManager.OnGameStateChanged -= OnGameStateChanged;

    void Start()
    {
        // Wave đầu tiên: spawn ngay (không cần qua Shop)
        AdvanceAndSpawn();
    }

    // ─── event handler ───────────────────────────────────────────────────────
    void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Shop) return;
        _harderUsedThisRound = false;
        DestroyLeftoverEnemies();
        AdvanceAndSpawn();
    }

    // ─── spawn logic ─────────────────────────────────────────────────────────
    void AdvanceAndSpawn()
    {
        // Wave đầu tiên (currentLevelIndex == -1): không tăng hard (hard bắt đầu ở 0%).
        // Từ wave thứ 2 trở đi: mỗi lần advance = 1 wave thường hoàn thành → +5% hard.
        if (currentLevelIndex >= 0)
            EnemyHardSystem.IncrementWave();

        currentLevelIndex++;
        if (levels == null || currentLevelIndex >= levels.Length)
        {
            Debug.Log($"[LevelManager] Không còn level nào (index {currentLevelIndex}). Dừng spawn.");
            return;
        }
        Debug.Log($"[LevelManager] Wave {currentLevelIndex} — Hard {EnemyHardSystem.HardPercent * 100f:F0}%");
        StartCoroutine(SpawnRoutine(levels[currentLevelIndex]));
    }

    /// <summary>
    /// Spawn thêm quái từ level tiếp theo mà không advance currentLevelIndex và không tăng hard%.
    /// Gọi bởi nút "Harder" trong Shop. Chỉ khả dụng khi còn level tiếp theo và chưa dùng trong lượt này.
    /// </summary>
    public void SpawnHarder()
    {
        int nextIndex = currentLevelIndex + 1;
        if (levels == null || nextIndex >= levels.Length)
        {
            Debug.Log("[LevelManager] SpawnHarder: không còn level tiếp theo.");
            return;
        }
        if (_harderUsedThisRound)
        {
            Debug.Log("[LevelManager] SpawnHarder: đã dùng Harder lần này rồi.");
            return;
        }
        _harderUsedThisRound = true;
        Debug.Log($"[LevelManager] HARDER — spawn thêm từ level {nextIndex} (hard không tăng).");
        StartCoroutine(SpawnRoutine(levels[nextIndex]));
    }

    IEnumerator SpawnRoutine(LevelData data)
    {
        if (data == null || data.groups == null) yield break;
        Debug.Log($"[LevelManager] Spawning: {data.levelName}");

        foreach (var group in data.groups)
        {
            if (group == null || group.prefab == null) continue;
            for (int i = 0; i < group.count; i++)
            {
                SpawnOneEnemy(group.prefab);
                if (group.spawnInterval > 0f)
                    yield return new WaitForSeconds(group.spawnInterval);
            }
        }
    }

    void SpawnOneEnemy(GameObject prefab)
    {
        Vector2 pos = GetRandomSpawnPoint();
        var go = Instantiate(prefab, pos, Quaternion.identity);
        var cb = go.GetComponent<CharacterBase>();
        if (cb == null)
        {
            Debug.LogWarning($"[LevelManager] Prefab '{prefab.name}' không có CharacterBase!");
            return;
        }

        // Force enemy trước khi Start() chạy để CharacterGrid register đúng faction
        cb.ForceSetFaction(Faction.Enemy);
        cb.SetSpawnPosition(pos);
        spawnedEnemies.Add(cb);
    }

    void DestroyLeftoverEnemies()
    {
        foreach (var e in spawnedEnemies)
            if (e != null) Destroy(e.gameObject);
        spawnedEnemies.Clear();
    }

    // ─── sampling ────────────────────────────────────────────────────────────
    Vector2 GetRandomSpawnPoint()
    {
        Vector2 excludeCenter = ShopArea.Instance != null ? ShopArea.Instance.Center : Vector2.zero;
        float   excludeRadius = ShopArea.Instance != null ? ShopArea.Instance.clusterRadius : fallbackExcludeRadius;

        // Rejection sampling: random trong Rect, bỏ nếu nằm trong vòng tròn exclusion
        for (int attempt = 0; attempt < 50; attempt++)
        {
            float x = Random.Range(spawnBounds.xMin, spawnBounds.xMax);
            float y = Random.Range(spawnBounds.yMin, spawnBounds.yMax);
            Vector2 candidate = new Vector2(x, y);
            if (Vector2.Distance(candidate, excludeCenter) >= excludeRadius)
                return candidate;
        }

        // Fallback an toàn: góc bounds
        Debug.LogWarning("[LevelManager] GetRandomSpawnPoint: không tìm được điểm hợp lệ sau 50 lần thử, dùng fallback.");
        return new Vector2(spawnBounds.xMax, spawnBounds.yMax);
    }

    // ─── gizmos ──────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        // Hình chữ nhật vàng = spawn bounds
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
        Vector3 boundsCenter = new Vector3(spawnBounds.center.x, spawnBounds.center.y, 0f);
        Vector3 boundsSize   = new Vector3(spawnBounds.width,    spawnBounds.height,   0f);
        Gizmos.DrawWireCube(boundsCenter, boundsSize);

        // Hình tròn đỏ = exclusion zone (lấy từ ShopArea nếu tìm được)
#if UNITY_EDITOR
        ShopArea shopArea = FindFirstObjectByType<ShopArea>();
        Vector2 excCenter = shopArea != null ? shopArea.Center : Vector2.zero;
        float   excRadius = shopArea != null ? shopArea.clusterRadius : fallbackExcludeRadius;
#else
        Vector2 excCenter = ShopArea.Instance != null ? ShopArea.Instance.Center : Vector2.zero;
        float   excRadius = ShopArea.Instance != null ? ShopArea.Instance.clusterRadius : fallbackExcludeRadius;
#endif
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        Gizmos.DrawWireSphere(new Vector3(excCenter.x, excCenter.y, 0f), excRadius);
    }

    // ─── public helpers ──────────────────────────────────────────────────────
    public int CurrentLevelIndex => currentLevelIndex;
    public bool HasMoreLevels => levels != null && currentLevelIndex + 1 < levels.Length;
    /// <summary>True nếu nút Harder có thể bấm được: còn level kế và chưa dùng lượt này.</summary>
    public bool CanSpawnHarder => HasMoreLevels && !_harderUsedThisRound;
    /// <summary>True nếu Harder đã được dùng trong wave vừa kết thúc — GameManager dùng để tính reward.</summary>
    public bool HarderWasUsed => _harderUsedThisRound;

    /// <summary>LevelData của wave đang diễn ra (enemies đã spawn).</summary>
    public LevelData GetCurrentLevelData() =>
        (levels != null && currentLevelIndex >= 0 && currentLevelIndex < levels.Length)
        ? levels[currentLevelIndex] : null;

    /// <summary>LevelData của wave tiếp theo (chưa spawn — dùng để preview award).</summary>
    public LevelData GetNextLevelData() =>
        (levels != null && currentLevelIndex + 1 < levels.Length)
        ? levels[currentLevelIndex + 1] : null;
}
