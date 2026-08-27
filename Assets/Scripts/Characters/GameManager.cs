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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
        if (_pendingShop || _pendingWave) return;
        if (!WaveManager.IsWaveActive) return;
        if (Time.time - _arenaEnterTime < arenaClearCheckDelay) return;

        if (CharacterGrid.CountAlive(Faction.Enemy) == 0)
        {
            _pendingShop = true;
            StartCoroutine(DelayedEnterShop());
        }
    }

    IEnumerator DelayedEnterShop()
    {
        yield return new WaitForSeconds(shopDelay);
        _pendingShop = false;
        PlayerWallet.Instance?.Earn(waveWinReward);
        EnterShop();
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
        WaveManager.Instance?.StartWave();
    }

    IEnumerator StartWaveDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        _pendingWave = false;
        _arenaEnterTime = Time.time;
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
