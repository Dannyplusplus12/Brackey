using System;
using UnityEngine;

// Spawn nhân vật vào SpawnArea. Gọi từ ShopOfferManager, GachaManager, hoặc bất kỳ
// hệ thống nào cần tạo nhân vật mới trong game.
// ClaimLeashSlot (trong CharacterBase.Start) sẽ set leashCenter = SpawnArea.Center;
// separation tự đẩy các char ra để có không gian.
public static class CharacterSpawner
{
    // Bất kỳ hệ thống nào muốn biết char mới được tạo đều subscribe vào đây.
    // Ví dụ: CharacterRosterUI.OnCharacterSpawned để thêm entry lên đầu roster.
    public static event Action<CharacterBase> OnCharacterSpawned;

    // Spawn 1 nhân vật từ prefab tại tâm SpawnArea.
    // Trả về CharacterBase vừa tạo (null nếu prefab không hợp lệ).
    public static CharacterBase Spawn(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[CharacterSpawner] prefab là null — bỏ qua.");
            return null;
        }

        Vector2 spawnPos = ShopArea.Instance != null
            ? ShopArea.Instance.Center
            : Vector2.zero;

        GameObject go = UnityEngine.Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        CharacterBase charBase = go.GetComponent<CharacterBase>();

        if (charBase == null)
        {
            Debug.LogError($"[CharacterSpawner] Prefab '{prefab.name}' không có CharacterBase component.");
            UnityEngine.Object.Destroy(go);
            return null;
        }

        OnCharacterSpawned?.Invoke(charBase);
        return charBase;
    }
}
