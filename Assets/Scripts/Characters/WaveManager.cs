using UnityEngine;

// Trigger chuyển Idle <-> Attacking cho toàn quân. Gọi WaveManager.StartWave()/EndWave()
// từ bất kỳ đâu (nút UI của shop, script khác...), hoặc bấm nút test trong Inspector lúc Play.
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    public static event System.Action OnWaveStart;
    public static event System.Action OnWaveEnd;
    public static bool IsWaveActive { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad()
    {
        IsWaveActive = false;
    }

    void Awake()
    {
        Instance = this;
    }

    [ContextMenu("Start Wave (Test)")]
    public void StartWave()
    {
        IsWaveActive = true;
        OnWaveStart?.Invoke();
    }

    [ContextMenu("End Wave (Test)")]
    public void EndWave()
    {
        IsWaveActive = false;
        OnWaveEnd?.Invoke();
    }
}
