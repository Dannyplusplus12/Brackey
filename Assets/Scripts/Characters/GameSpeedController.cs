using UnityEngine;

// Tăng/giảm tốc độ game trong wave.
// - Phím Space: cycle qua các mức tốc độ (chỉ trong wave)
// - ToggleSpeed(): gọi từ UI button
// - Auto-reset về mức 0 (normalSpeed) khi wave kết thúc hoặc vào Shop
//
// Không xung đột với debug timescale của DebugOverlay (1/2 key):
// debug override khi cần, GameSpeedController chỉ active trong wave.
public class GameSpeedController : MonoBehaviour
{
    public static GameSpeedController Instance { get; private set; }

    [Tooltip("Danh sách mức tốc độ (x). Mức 0 = bình thường, các mức tiếp theo = nhanh hơn.")]
    [SerializeField] float[] speedPresets = { 1f, 2f };

    [Tooltip("Phím bấm để cycle qua các mức tốc độ (chỉ trong wave)")]
    [SerializeField] KeyCode toggleKey = KeyCode.Space;

    // ─── state ───────────────────────────────────────────────────────────────
    int currentPresetIndex = 0;
    bool isWaveActive = false;

    public float CurrentSpeed => speedPresets != null && speedPresets.Length > 0
        ? speedPresets[currentPresetIndex]
        : 1f;

    public int CurrentPresetIndex => currentPresetIndex;
    public int PresetCount => speedPresets?.Length ?? 0;

    // ─── lifecycle ───────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        WaveManager.OnWaveStart  += OnWaveStart;
        WaveManager.OnWaveEnd    += OnWaveEnd;
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    void OnDisable()
    {
        WaveManager.OnWaveStart  -= OnWaveStart;
        WaveManager.OnWaveEnd    -= OnWaveEnd;
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    void Update()
    {
        if (!isWaveActive) return;
        if (Input.GetKeyDown(toggleKey))
            CycleSpeed();
    }

    // ─── event handlers ──────────────────────────────────────────────────────
    void OnWaveStart()
    {
        isWaveActive = true;
        ApplySpeed(); // giữ mức tốc độ từ wave trước nếu player muốn
    }

    void OnWaveEnd()
    {
        isWaveActive = false;
        ResetSpeed();
    }

    void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Shop)
        {
            isWaveActive = false;
            ResetSpeed();
        }
    }

    // ─── public API ──────────────────────────────────────────────────────────

    // Gọi từ UI button (hoạt động cả ngoài wave nhưng chỉ áp khi wave active)
    public void CycleSpeed()
    {
        if (speedPresets == null || speedPresets.Length == 0) return;
        currentPresetIndex = (currentPresetIndex + 1) % speedPresets.Length;
        ApplySpeed();
    }

    // Reset về mức 0 và áp ngay
    public void ResetSpeed()
    {
        currentPresetIndex = 0;
        ApplySpeed();
    }

    // ─── internal ────────────────────────────────────────────────────────────
    void ApplySpeed()
    {
        if (speedPresets == null || speedPresets.Length == 0) return;
        Time.timeScale = speedPresets[currentPresetIndex];
    }
}
