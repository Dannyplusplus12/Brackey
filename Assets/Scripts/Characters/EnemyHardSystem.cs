using UnityEngine;

/// <summary>
/// Hệ thống Hard% cho Enemy — mỗi wave thường (không phải Harder) tăng +5% buff.
/// HP và Damage nhân thêm (1 + HardPercent).
/// APS cộng thêm HardFlatAPS (flat, +0.05 mỗi wave).
/// Reset tự động khi load lại domain (bắt đầu Play mode mới).
/// </summary>
public static class EnemyHardSystem
{
    /// <summary>Số wave thường đã hoàn thành (không tính Harder).</summary>
    public static int WaveCount { get; private set; }

    /// <summary>Hệ số buff HP/Damage của enemy (5% mỗi wave). VD: wave 2 → 0.10f.</summary>
    public static float HardPercent => WaveCount * 0.05f;

    /// <summary>APS cộng thêm flat cho enemy (0.05 mỗi wave).</summary>
    public static float HardFlatAPS => WaveCount * 0.05f;

    /// <summary>Gọi khi bắt đầu spawn level mới (AdvanceAndSpawn, không gọi khi SpawnHarder).</summary>
    public static void IncrementWave() => WaveCount++;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => WaveCount = 0;
}
