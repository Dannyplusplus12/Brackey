using System.Collections.Generic;
using UnityEngine;

// Spatial hash grid tĩnh dùng chung cho toàn bộ CharacterBase: tìm địch gần nhất,
// tìm đồng minh để tách khoảng cách, tìm chỗ trống lúc spawn — không dùng Physics2D.
public static class CharacterGrid
{
    public static float CellSize = 1f;

    static readonly Dictionary<Vector2Int, List<CharacterBase>> cells = new();
    static readonly Dictionary<CharacterBase, Vector2Int> memberCell = new();

    // Danh sách phẳng theo phe, dùng để tìm "địch gần nhất" không giới hạn phạm vi
    // mà không phải quét toàn bộ ô lưới (grid chỉ hợp cho truy vấn cục bộ như separation).
    static readonly Dictionary<Faction, List<CharacterBase>> byFaction = new()
    {
        { Faction.Ally, new List<CharacterBase>() },
        { Faction.Enemy, new List<CharacterBase>() }
    };

    // Unity có thể giữ static field qua nhiều lần Play nếu tắt Domain Reload -> reset để tránh dữ liệu rác.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad()
    {
        cells.Clear();
        memberCell.Clear();
        byFaction[Faction.Ally].Clear();
        byFaction[Faction.Enemy].Clear();
    }

    static Vector2Int ToCell(Vector2 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x / CellSize), Mathf.FloorToInt(pos.y / CellSize));
    }

    public static void Register(CharacterBase character)
    {
        Vector2Int cell = ToCell(character.transform.position);
        AddToCell(cell, character);
        memberCell[character] = cell;
        byFaction[character.Faction].Add(character);
    }

    public static void Unregister(CharacterBase character)
    {
        if (memberCell.TryGetValue(character, out Vector2Int cell))
        {
            if (cells.TryGetValue(cell, out List<CharacterBase> list))
                list.Remove(character);
            memberCell.Remove(character);
        }
        byFaction[character.Faction].Remove(character);
    }

    // Tìm địch gần nhất trong toàn bộ danh sách theo phe, không giới hạn phạm vi.
    public static CharacterBase FindNearest(Vector2 position, Faction faction, CharacterBase exclude = null)
    {
        CharacterBase nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (CharacterBase c in byFaction[faction])
        {
            if (c == null || c == exclude || c.IsDead) continue;
            float sqr = (c.BodyCenter - position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = c;
            }
        }
        return nearest;
    }

    // Tìm đồng minh máu thấp nhất trong toàn bộ danh sách theo phe, không giới hạn phạm vi.
    public static CharacterBase FindLowestHp(Faction faction, CharacterBase exclude = null)
    {
        CharacterBase lowest = null;
        float lowestHp = float.MaxValue;
        foreach (CharacterBase c in byFaction[faction])
        {
            if (c == null || c == exclude || c.IsDead) continue;
            if (c.CurrentHP < lowestHp)
            {
                lowestHp = c.CurrentHP;
                lowest = c;
            }
        }
        return lowest;
    }

    // Trả về toàn bộ list của 1 phe (read-only). Dùng cho FeedingManager, UI roster...
    // KHÔNG modify list này khi đang iterating — snapshot trước nếu cần.
    public static System.Collections.Generic.IReadOnlyList<CharacterBase> GetAll(Faction faction)
        => byFaction[faction];

    // Trả về snapshot list các nhân vật còn sống theo phe — an toàn để iterate khi có side-effect.
    public static List<CharacterBase> FindAllAlive(Faction faction)
    {
        var result = new List<CharacterBase>();
        foreach (CharacterBase c in byFaction[faction])
            if (c != null && !c.IsDead) result.Add(c);
        return result;
    }

    // Đếm số nhân vật còn sống theo phe - dùng để GameManager phát hiện đã hết địch (wave clear).
    public static int CountAlive(Faction faction)
    {
        int count = 0;
        foreach (CharacterBase c in byFaction[faction])
        {
            if (c != null && !c.IsDead) count++;
        }
        return count;
    }

    public static void UpdatePosition(CharacterBase character)
    {
        Vector2Int newCell = ToCell(character.transform.position);
        if (memberCell.TryGetValue(character, out Vector2Int oldCell))
        {
            if (oldCell == newCell) return;
            if (cells.TryGetValue(oldCell, out List<CharacterBase> oldList))
                oldList.Remove(character);
        }
        AddToCell(newCell, character);
        memberCell[character] = newCell;
    }

    static void AddToCell(Vector2Int cell, CharacterBase character)
    {
        if (!cells.TryGetValue(cell, out List<CharacterBase> list))
        {
            list = new List<CharacterBase>();
            cells[cell] = list;
        }
        list.Add(character);
    }

    // Ghi kết quả vào "results" do caller truyền vào để tránh cấp phát bộ nhớ mỗi lần gọi.
    public static void GetNearby(Vector2 position, float radius, List<CharacterBase> results, Faction? factionFilter = null, CharacterBase exclude = null)
    {
        results.Clear();
        int cellRadius = Mathf.CeilToInt(radius / CellSize);
        Vector2Int center = ToCell(position);
        float sqrRadius = radius * radius;

        for (int dx = -cellRadius; dx <= cellRadius; dx++)
        {
            for (int dy = -cellRadius; dy <= cellRadius; dy++)
            {
                Vector2Int cell = new Vector2Int(center.x + dx, center.y + dy);
                if (!cells.TryGetValue(cell, out List<CharacterBase> list)) continue;

                foreach (CharacterBase c in list)
                {
                    if (c == null || c == exclude) continue;
                    if (factionFilter.HasValue && c.Faction != factionFilter.Value) continue;
                    if (((Vector2)c.transform.position - position).sqrMagnitude <= sqrRadius)
                        results.Add(c);
                }
            }
        }
    }
}
