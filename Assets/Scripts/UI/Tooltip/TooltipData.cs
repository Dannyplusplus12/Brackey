using UnityEngine;

/// <summary>
/// Hướng panel tooltip xuất hiện so với slot nguồn.
///   Top    → panel phía TRÊN slot
///   Bottom → panel phía DƯỚI slot
///   Left   → panel bên TRÁI slot
///   Right  → panel bên PHẢI slot
/// </summary>
public enum TooltipDirection { Top, Bottom, Left, Right }

/// <summary>
/// Data tooltip cần để hiển thị và định vị đúng vị trí.
///
/// ── Ví dụ ────────────────────────────────────────────────────────────────
/// Direction=Top, alignEnd=false → panel phía trên, cạnh TRÁI panel = cạnh trái ô
/// Direction=Top, alignEnd=true  → panel phía trên, cạnh PHẢI panel = cạnh phải ô
/// Direction=Right, alignEnd=false → panel bên phải, cạnh TRÊN panel = cạnh trên ô
/// Direction=Right, alignEnd=true  → panel bên phải, cạnh DƯỚI panel = cạnh dưới ô
///
/// ── TMP Rich Text trong ItemData.description ─────────────────────────────
/// "Tăng <color=#4CAF50>10</color> <sprite name=\"stat_damage\">"
/// Màu: Buff #4CAF50 | Debuff #E82020
/// Sprites: stat_damage | stat_hp | stat_speed | stat_atkspeed | stat_food | stat_angry
/// </summary>
public struct TooltipData
{
    public string           richDescription;
    public RectTransform    sourceRect;   // slot nguồn để tính vị trí panel
    public TooltipDirection direction;
    public bool             alignEnd;    // false=canh đầu(trái/trên)  true=canh cuối(phải/dưới)
    public float            gap;         // khoảng cách giữa 2 cạnh gần nhau nhất

    public static TooltipData FromItem(
        ItemData        item,
        RectTransform   source,
        TooltipDirection direction,
        bool            alignEnd = false,
        float           gap      = 8f)
    {
        if (item == null) return default;
        return new TooltipData
        {
            richDescription = item.description,
            sourceRect      = source,
            direction       = direction,
            alignEnd        = alignEnd,
            gap             = gap,
        };
    }
}
