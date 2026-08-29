using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Tạo CharacterStats asset + Prefab cho tất cả nhân vật trong Assets/Char/.
// Menu: Tools/Characters/Create All Character Prefabs
// Idempotent: chạy lại chỉ cập nhật sprite, không xoá prefab cũ.
public static class CharacterPrefabCreator
{
    const string CharRootFolder = "Assets/Char";

    // Map: tên folder → tên class C#
    static readonly Dictionary<string, string> FolderToClass = new()
    {
        { "BAGGER",    "Bagger"   },
        { "DOG Z",     "DogZ"     },
        { "FRANK Z",   "FrankZ"   },
        { "NGUOI SOI", "NguoiSoi" },
        { "PIRATE",    "Pirate"   },
        { "SUMO",      "Sumo"     },
        { "VIKING",    "Viking"   },
        { "ZOMIBIE",   "Zombie"   },
    };

    // Menu item chạy đơn lẻ cho PIRATE — không đụng đến prefab/asset các char khác.
    [MenuItem("Tools/Characters/Create PIRATE Prefab")]
    public static void CreatePirate()
    {
        string folder = CharRootFolder + "/PIRATE";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EditorUtility.DisplayDialog("Error", $"Folder không tồn tại: {folder}", "OK");
            return;
        }

        bool wasNew = ProcessCharacter(folder, "PIRATE", "Pirate");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Tạo / cập nhật ItemData để PIRATE xuất hiện trong shop & gacha pool
        CreateOrUpdateItemData(folder, "PIRATE");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = wasNew ? "Tạo mới prefab + ItemData PIRATE thành công!" : "Cập nhật prefab + ItemData PIRATE thành công!";
        Debug.Log($"[CharPrefab] {msg}");
        EditorUtility.DisplayDialog("PIRATE Prefab", msg, "OK");
    }

    // Tạo ItemData asset (Character type) và wire characterPrefab nếu prefab đã tồn tại.
    // Idempotent: chạy lại chỉ re-wire prefab, không đổi buyCost / rarity do user chỉnh tay.
    static void CreateOrUpdateItemData(string charFolder, string displayName)
    {
        string itemDataPath = $"{CharRootFolder}/{displayName}_ItemData.asset";
        string prefabPath   = $"{charFolder}/{displayName}.prefab";

        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(itemDataPath);
        bool isNew = item == null;

        if (isNew)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            item.displayName = displayName;
            item.itemType    = ItemType.Character;
            item.rarity      = ItemRarity.Rare;
            item.buyCost     = 8;
            item.sellValue   = 1;
            item.description = "Cướp biển lắm mưu. Mỗi đòn chẵn: 5% kiếm 5 corn. Mỗi wave sống sót tỉ lệ tăng thêm 1%.";
            AssetDatabase.CreateAsset(item, itemDataPath);
        }

        // Wire prefab (luôn cập nhật phòng khi prefab vừa được tạo mới)
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            var so = new SerializedObject(item);
            so.FindProperty("characterPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning($"[CharPrefab] PIRATE: chưa tìm thấy prefab tại {prefabPath}. Wire characterPrefab thủ công sau khi tạo prefab.");
        }

        EditorUtility.SetDirty(item);
        Debug.Log($"[CharPrefab] ItemData PIRATE: {(isNew ? "tạo mới" : "cập nhật")} → {itemDataPath}");
    }

    [MenuItem("Tools/Characters/Create All Character Prefabs")]
    public static void CreateAll()
    {
        int created = 0, updated = 0;

        foreach (var kv in FolderToClass)
        {
            string folderName = kv.Key;
            string className  = kv.Value;
            string folder     = $"{CharRootFolder}/{folderName}";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"[CharPrefab] Folder không tồn tại: {folder}");
                continue;
            }

            bool wasNew = ProcessCharacter(folder, folderName, className);
            if (wasNew) created++; else updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CharPrefab] Xong! Tạo mới: {created}, Cập nhật: {updated}");
        EditorUtility.DisplayDialog("Character Prefabs", $"Tạo mới: {created}\nCập nhật: {updated}", "OK");
    }

    // Trả về true nếu prefab vừa tạo mới, false nếu đã update
    static bool ProcessCharacter(string folder, string displayName, string className)
    {
        // --- 1. Tìm sprites ---
        Sprite idleSprite = FindSprite(folder, "IDLE");
        Sprite atkSprite  = FindSprite(folder, "ATK");

        if (idleSprite == null)
            Debug.LogWarning($"[CharPrefab] {displayName}: không tìm thấy IDLE sprite trong {folder}");

        // --- 2. CharacterStats asset ---
        string statsPath = $"{folder}/{displayName}.asset";
        CharacterStats stats = AssetDatabase.LoadAssetAtPath<CharacterStats>(statsPath);
        bool statsNew = stats == null;

        if (statsNew)
        {
            stats = ScriptableObject.CreateInstance<CharacterStats>();
            AssetDatabase.CreateAsset(stats, statsPath);
        }

        // Gán sprites (không ghi đè nếu user đã gán skillSprite tay)
        stats.idleSprite   = idleSprite;
        stats.attackSprite = atkSprite;
        // stats.skillSprite để user gán sau
        EditorUtility.SetDirty(stats);

        // --- 3. Prefab ---
        string prefabPath = $"{folder}/{displayName}.prefab";
        bool prefabExists = File.Exists(Path.Combine(Application.dataPath, "../", prefabPath));

        GameObject root;

        if (prefabExists)
        {
            // Load prefab hiện có, chỉ update stats reference
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            root = PrefabUtility.InstantiatePrefab(existing) as GameObject;
        }
        else
        {
            // Tạo mới từ đầu
            root = BuildPrefabHierarchy(displayName, className, stats, idleSprite);
        }

        if (root == null)
        {
            Debug.LogError($"[CharPrefab] {displayName}: không tạo được root GO.");
            return false;
        }

        // Update stats trên instance (cả prefab cũ lẫn mới)
        var charBase = root.GetComponent<CharacterBase>();
        if (charBase != null)
        {
            var so = new SerializedObject(charBase);
            so.FindProperty("stats").objectReferenceValue = stats;
            so.ApplyModifiedProperties();
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        return !prefabExists;
    }

    static GameObject BuildPrefabHierarchy(string displayName, string className, CharacterStats stats, Sprite idleSprite)
    {
        // Root
        GameObject root = new GameObject(displayName);

        // Character script (resolve by class name)
        System.Type charType = GetCharacterType(className);
        CharacterBase charBase;
        if (charType != null)
            charBase = (CharacterBase)root.AddComponent(charType);
        else
        {
            Debug.LogWarning($"[CharPrefab] Không tìm thấy class '{className}', dùng SampleWarrior fallback.");
            charBase = root.AddComponent<SampleWarrior>();
        }

        // Stats
        var so = new SerializedObject(charBase);
        so.FindProperty("stats").objectReferenceValue = stats;

        // Faction mặc định Ally
        so.FindProperty("faction").enumValueIndex = (int)Faction.Ally;
        so.ApplyModifiedProperties();

        // Collider (trigger, dùng Box)
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 1f);

        // Drag handler
        root.AddComponent<CharacterDragHandler>();

        // Child: Visual
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = idleSprite;

        // Wire spriteRenderer lên CharacterBase
        var soChar = new SerializedObject(charBase);
        soChar.FindProperty("spriteRenderer").objectReferenceValue = sr;
        soChar.FindProperty("visualRoot").objectReferenceValue = visual.transform;
        soChar.ApplyModifiedProperties();

        // Child: Shadow (render dưới character, không bị ảnh hưởng sway)
        GameObject shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(root.transform, false);
        var shadowSr = shadowGO.AddComponent<SpriteRenderer>();
        shadowSr.sortingOrder = sr.sortingOrder - 1;
        var shadowComp = shadowGO.AddComponent<CharacterShadow>();

        // Wire shadow lên CharacterBase
        var soShadow = new SerializedObject(charBase);
        soShadow.FindProperty("shadow").objectReferenceValue = shadowComp;
        soShadow.ApplyModifiedProperties();

        // Child: VFXPoint
        GameObject vfxPoint = new GameObject("VFXPoint");
        vfxPoint.transform.SetParent(root.transform, false);

        return root;
    }

    static Sprite FindSprite(string folder, string prefix)
    {
        // Tìm file bắt đầu bằng prefix (không phân biệt hoa thường) trong folder
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path).ToUpper();
            if (fileName.StartsWith(prefix))
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    static System.Type GetCharacterType(string className)
    {
        // Tìm trong Assembly-CSharp (assembly mặc định của Unity)
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = assembly.GetType(className);
            if (t != null && typeof(CharacterBase).IsAssignableFrom(t))
                return t;
        }
        return null;
    }
}
