#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Tạo ItemData asset cho toàn bộ bộ Food items.
// Menu: Tools > Items > Create Food Items
//
// Quy tắc:
//   - Asset đã tồn tại → BỎ QUA (giữ nguyên chỉnh sửa trong Inspector).
//   - Asset trong folder KHÔNG CÓ trong list → XÓA (dọn item cũ không còn dùng).
//   - Asset chưa tồn tại → TẠO MỚI.
public static class FoodItemCreator
{
    const string OutputFolder = "Assets/Data/Items";

    // Danh sách tên file hợp lệ (không có .asset). Tool sẽ xóa file nào không có trong list này.
    static readonly HashSet<string> KnownFileNames = new()
    {
        "Apple", "ChickenLeg", "Candy", "Coffee",
        "Fish", "FriedEgg", "Bread",
        "Mushroom", "Lemon",
    };

    [MenuItem("Tools/Items/Create Food Items")]
    public static void CreateFoodItems()
    {
        EnsureFolder(OutputFolder);
        DeleteUnknownAssets();

        // ── StatBoost — permanent flat bonus ──────────────────────────────────

        CreateStatBoost(
            fileName:    "Apple",
            displayName: "Apple",
            description: "<color=#4CAF50>+5 Max HP permanently.</color>\nRarity: Common",
            delta:       new StatDelta { maxHP = 5f }
        );

        CreateStatBoost(
            fileName:    "ChickenLeg",
            displayName: "Chicken Leg",
            description: "<color=#B48EE0>+1 damage permanently.</color>\nRarity: Common",
            delta:       new StatDelta { damage = 1f }
        );

        CreateStatBoost(
            fileName:    "Candy",
            displayName: "Candy",
            description: "<color=#00BFFF>+0.03 attack speed permanently.</color>\nRarity: Common",
            delta:       new StatDelta { attackSpeed = 0.03f }
        );

        CreateStatBoost(
            fileName:    "Coffee",
            displayName: "Coffee",
            description: "<color=#FF69B4>+10 speed permanently.</color>\nRarity: Common",
            delta:       new StatDelta { moveSpeed = 10f }
        );

        // ── StatBoost — conditional (logic trong SpecialItemTracker.cs) ───────
        // statDelta để trống — buff áp qua AddInstanceBonus per-character.

        CreateStatBoost(
            fileName:    "Mushroom",
            displayName: "Mushroom",
            description: "Every 10 damage taken: <color=#4CAF50>+1 Max HP permanently.</color>\nRarity: Uncommon",
            delta:       default,
            rarity:      ItemRarity.Uncommon
        );

        CreateStatBoost(
            fileName:    "Lemon",
            displayName: "Lemon",
            description: "Every 10 angry: <color=#00BFFF>+0.01 attack speed.</color>\nRarity: Uncommon",
            delta:       default,
            rarity:      ItemRarity.Uncommon
        );

        // ── Active ─────────────────────────────────────────────────────────────

        CreateActive(
            fileName:    "Fish",
            displayName: "Fish",
            description: "On activation: All allies <color=#E82020>-10 angry.</color>\nRarity: Common"
        );

        CreateActive(
            fileName:    "FriedEgg",
            displayName: "Fried Egg",
            description: "On activation: <color=#00BFFF>+50% attack speed</color> and <color=#B48EE0>+30% damage</color> until end of round.\nRarity: Uncommon",
            rarity:      ItemRarity.Uncommon
        );

        CreateActive(
            fileName:    "Bread",
            displayName: "Bread",
            description: "On activation: All allies heal <color=#4CAF50>+30 HP.</color>\nRarity: Common"
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FoodItemCreator] Hoàn tất. Assets trong {OutputFolder}.");
    }

    // Xóa .asset file trong folder không có trong KnownFileNames.
    static void DeleteUnknownAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { OutputFolder });
        foreach (string guid in guids)
        {
            string path     = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (!KnownFileNames.Contains(fileName))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[FoodItemCreator] Đã xóa item cũ: {path}");
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static void CreateStatBoost(string fileName, string displayName, string description,
                                StatDelta delta, ItemRarity rarity = ItemRarity.Common)
    {
        string path = $"{OutputFolder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null) return; // giữ nguyên nếu đã có

        var item = ScriptableObject.CreateInstance<ItemData>();
        item.displayName = displayName;
        item.description = description;
        item.itemType    = ItemType.StatBoost;
        item.rarity      = rarity;
        item.buyCost     = 2;
        item.sellValue   = 1;
        item.targetType  = ItemTargetType.AllCharacters;
        item.statDelta   = delta;

        AssetDatabase.CreateAsset(item, path);
        Debug.Log($"[FoodItemCreator] Tạo mới: {path}");
    }

    static void CreateActive(string fileName, string displayName, string description,
                             ItemRarity rarity = ItemRarity.Common)
    {
        string path = $"{OutputFolder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null) return; // giữ nguyên nếu đã có

        var item = ScriptableObject.CreateInstance<ItemData>();
        item.displayName = displayName;
        item.description = description;
        item.itemType    = ItemType.Active;
        item.rarity      = rarity;
        item.buyCost     = 2;
        item.sellValue   = 1;

        AssetDatabase.CreateAsset(item, path);
        Debug.Log($"[FoodItemCreator] Tạo mới: {path}");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
