using System.Collections.Generic;
using UnityEngine;

// Lưu lại danh sách CharacterStats (loại nhân vật) mà player đã từng mua/sở hữu.
// Được dùng bởi ShopOfferManager để lọc item chỉ dành cho loại nhân vật cụ thể:
// item chỉ xuất hiện trong shop nếu player đã từng có loại nhân vật tương ứng.
//
// CharacterBase.Start() gọi Register() tự động cho mọi ally khi spawn.
// Reset() được gọi tự động mỗi lần domain reload (để Play Mode sạch).
public static class PlayerRoster
{
    static readonly HashSet<CharacterStats> ownedTypes = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => ownedTypes.Clear();

    // Gọi từ CharacterBase.Start() khi nhân vật Ally spawn.
    public static void Register(CharacterStats type)
    {
        if (type != null) ownedTypes.Add(type);
    }

    // Trả về true nếu player đã từng sở hữu loại nhân vật này.
    public static bool HasType(CharacterStats type)
        => type != null && ownedTypes.Contains(type);
}
