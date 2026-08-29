using UnityEngine;

/// <summary>
/// Quản lý gacha packs. Nhận lệnh mở từ GachaPackSlotUI → chuyển cho GachaWheelUI.
/// Gán GachaWheelUI reference trong Inspector (hoặc Setup Tool tự gán).
/// </summary>
public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    [SerializeField] GachaPackData[] packs = new GachaPackData[2];

    [Tooltip("GachaWheelUI trong scene — Setup Tool tạo và gán tự động")]
    [SerializeField] GachaWheelUI wheelUI;

    public GachaPackData GetPack(int index) =>
        (packs != null && index < packs.Length) ? packs[index] : null;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Mở spin wheel cho pack index. Gọi từ GachaPackSlotUI.OnClickOpen().
    /// </summary>
    public void OpenPack(int index)
    {
        GachaPackData pack = GetPack(index);
        if (pack == null)
        {
            Debug.LogWarning($"[GachaManager] Pack index {index} không hợp lệ.");
            return;
        }

        if (wheelUI == null)
        {
            Debug.LogError("[GachaManager] wheelUI chưa được gán — chạy Tools > Gacha > Setup Gacha Wheel In Scene.");
            return;
        }

        wheelUI.OpenWheel(pack);
    }

    // Editor utility: gán wheelUI từ code (Setup Tool gọi vào)
    public void SetWheelUI(GachaWheelUI ui) => wheelUI = ui;
}
