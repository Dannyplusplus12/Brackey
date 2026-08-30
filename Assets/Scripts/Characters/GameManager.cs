using System.Collections;
using UnityEngine;

// Quản lý state cấp cao Shop <-> Arena.
// Flow:
//   Start: Arena (wave đầu, không feed)
//   Wave hết địch → shopDelay → EnterShop()
//   Bấm nút → EnterArena() → FeedingManager feed → camera zoom → postFeedDelay → StartWave()
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static event System.Action<GameState> OnGameStateChanged;
    /// <summary>Fire khi toàn bộ ally chết trong wave — DefeatScreenUI lắng nghe.</summary>
    public static event System.Action OnDefeat;

    public GameState CurrentState { get; private set; } = GameState.Arena;

    [Tooltip("Delay sau khi hết địch trước khi mở Shop")]
    public float shopDelay = 2f;

    [Tooltip("Thời gian đếm ngược 3-2-1 sau khi feed xong trước khi bắt đầu wave. " +
             "WaveCountdown dùng cùng giá trị này — nên để số nguyên (3).")]
    public float postFeedDelay = 3f;

    [Tooltip("Tránh check hết địch quá sớm khi vừa spawn")]
    public float arenaClearCheckDelay = 0.5f;

    [Header("Economy")]
    public int waveWinReward = 5;

    float _arenaEnterTime;
    bool _pendingShop;
    bool _pendingWave;
    bool _pendingDefeat;
    bool _waveEverStarted; // true sau khi StartWave() được gọi lần đầu

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        CharacterBase.OnAllyDied        += CheckDefeat;
        CharacterBase.OnAllyBecameEnemy += CheckDefeat;
    }
    void OnDisable()
    {
        CharacterBase.OnAllyDied        -= CheckDefeat;
        CharacterBase.OnAllyBecameEnemy -= CheckDefeat;
    }

    // Gọi mỗi khi 1 ally chết hoặc đổi phe — check defeat ngay lập tức.
    // Dùng event-driven thay vì poll trong Update để bắt được cả khi wave chưa/đã active.
    void CheckDefeat(CharacterBase _)
    {
        if (CurrentState != GameState.Arena) return;
        if (_pendingDefeat || _pendingShop) return;
        if (Time.time - _arenaEnterTime < arenaClearCheckDelay) return;
        if (CharacterGrid.CountAlive(Faction.Ally) > 0) return; // còn ally sống

        _pendingDefeat = true;
        StartCoroutine(TriggerDefeat());
    }

    void Start()
    {
        // Wave đầu: không feed, vào thẳng
        CurrentState = GameState.Arena;
        _arenaEnterTime = Time.time;
        OnGameStateChanged?.Invoke(CurrentState);
        _pendingWave = true;
        StartCoroutine(StartWaveDelayed(postFeedDelay));
    }

    void Update()
    {
        if (CurrentState != GameState.Arena) return;
        if (_pendingShop || _pendingWave || _pendingDefeat) return;
        if (!WaveManager.IsWaveActive) return;
        if (Time.time - _arenaEnterTime < arenaClearCheckDelay) return;

        // Thắng: hết địch
        if (CharacterGrid.CountAlive(Faction.Enemy) == 0)
        {
            _pendingShop = true;
            StartCoroutine(DelayedEnterShop());
        }
        // Defeat được xử lý event-driven qua OnAllyDied() — không poll ở đây nữa.
    }

    IEnumerator TriggerDefeat()
    {
        // Đợi một chút để VFX chết cuối cùng play xong
        yield return new WaitForSeconds(1.2f);
        WaveManager.Instance?.EndWave();

        // Tìm DefeatScreenUI kể cả khi inactive (FindObjectsInactive.Include)
        // để không phụ thuộc vào việc object có active hay không trong scene.
        var ui = Object.FindFirstObjectByType<DefeatScreenUI>(FindObjectsInactive.Include);
        if (ui != null)
        {
            ui.gameObject.SetActive(true); // Đảm bảo active trước khi Show
            ui.ShowDefeatScreen();
        }
        else
        {
            Debug.LogWarning("[GameManager] Không tìm thấy DefeatScreenUI trong scene. " +
                             "Chạy Tools > UI > Build Defeat Screen để tạo.");
        }

        OnDefeat?.Invoke(); // Cho các script khác hook vào nếu cần
    }

    IEnumerator DelayedEnterShop()
    {
        yield return new WaitForSeconds(shopDelay);
        _pendingShop = false;
        PlayerWallet.Instance?.Earn(CurrentWaveReward());
        EnterShop();
    }

    /// <summary>
    /// Reward của wave vừa thắng — đọc từ LevelData hiện tại nếu có,
    /// fallback về GameManager.waveWinReward. Nếu wave có Harder: x3 (+200%).
    /// </summary>
    public int CurrentWaveReward()
    {
        var data = LevelManager.Instance?.GetCurrentLevelData();
        int baseReward = data != null ? data.waveWinReward : waveWinReward;
        bool harder = LevelManager.Instance != null && LevelManager.Instance.HarderWasUsed;
        return harder ? baseReward * 3 : baseReward;
    }

    // Gọi từ nút "Bắt đầu Wave"
    [ContextMenu("Enter Arena (Test)")]
    public void EnterArena()
    {
        if (CurrentState == GameState.Arena) return;
        CurrentState = GameState.Arena;
        _arenaEnterTime = Time.time;
        _pendingWave = true;

        FeedingManager fm = FindFirstObjectByType<FeedingManager>();
        if (fm != null)
        {
            // Feed trước — camera chuyển sau khi feed xong
            bool done = false;
            System.Action onDone = null;
            onDone = () =>
            {
                done = true;
                FeedingManager.OnFeedingComplete -= onDone;
            };
            FeedingManager.OnFeedingComplete += onDone;
            fm.StartFeeding();
            StartCoroutine(WaitFeedThenArena(() => done));
        }
        else
        {
            // Không có FeedingManager: chuyển camera + bắt đầu ngay
            OnGameStateChanged?.Invoke(CurrentState);
            StartCoroutine(StartWaveDelayed(postFeedDelay));
        }
    }

    IEnumerator WaitFeedThenArena(System.Func<bool> isDone)
    {
        yield return new WaitUntil(isDone);
        OnGameStateChanged?.Invoke(CurrentState); // camera zoom vào arena
        yield return new WaitForSeconds(postFeedDelay);
        _pendingWave = false;
        _arenaEnterTime = Time.time;
        _waveEverStarted = true;
        WaveManager.Instance?.StartWave();
    }

    IEnumerator StartWaveDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        _pendingWave = false;
        _arenaEnterTime = Time.time;
        _waveEverStarted = true;
        WaveManager.Instance?.StartWave();
    }

    [ContextMenu("Enter Shop (Test)")]
    public void EnterShop()
    {
        if (CurrentState == GameState.Shop) return;
        CurrentState = GameState.Shop;
        if (WaveManager.Instance != null && WaveManager.IsWaveActive)
            WaveManager.Instance.EndWave();
        OnGameStateChanged?.Invoke(CurrentState);
    }
}
