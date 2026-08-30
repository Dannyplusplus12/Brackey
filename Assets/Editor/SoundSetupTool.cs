#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Chạy 1 lần: Tools > Sound > Setup Sound In Scene
// Tự tạo SoundLibrary asset và thêm SoundManager vào Managers GO trong scene.
public static class SoundSetupTool
{
    const string LibraryPath = "Assets/Sound/SoundLibrary.asset";

    [MenuItem("Tools/Sound/Setup Sound In Scene")]
    static void Setup()
    {
        // 1. Tạo folder nếu chưa có
        if (!AssetDatabase.IsValidFolder("Assets/Sound"))
            AssetDatabase.CreateFolder("Assets", "Sound");

        // 2. Tạo hoặc load SoundLibrary
        var lib = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(lib, LibraryPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Sound] Tạo SoundLibrary tại {LibraryPath}");
        }
        else
        {
            Debug.Log($"[Sound] Dùng SoundLibrary sẵn có tại {LibraryPath}");
        }

        // 3. Tìm hoặc tạo Managers GO
        var managers = GameObject.Find("Managers");
        if (managers == null)
        {
            managers = new GameObject("Managers");
            Undo.RegisterCreatedObjectUndo(managers, "Create Managers");
        }

        // 4. Thêm SoundManager nếu chưa có
        var sm = managers.GetComponent<SoundManager>();
        if (sm == null)
        {
            sm = Undo.AddComponent<SoundManager>(managers);
            Debug.Log("[Sound] Thêm SoundManager vào Managers GO.");
        }
        else
        {
            Debug.Log("[Sound] SoundManager đã tồn tại trên Managers GO.");
        }

        // 5. Wire SoundLibrary vào SoundManager qua SerializedObject
        var so = new SerializedObject(sm);
        var libProp = so.FindProperty("library");
        if (libProp != null && libProp.objectReferenceValue == null)
        {
            libProp.objectReferenceValue = lib;
            so.ApplyModifiedProperties();
            Debug.Log("[Sound] Wire SoundLibrary vào SoundManager.");
        }

        // 6. Điền sẵn tất cả SoundId entries vào library (chỉ thêm entry còn thiếu)
        PopulateLibraryEntries(lib);

        EditorSceneManager.MarkSceneDirty(managers.scene);
        Debug.Log("[Sound] Setup hoàn tất! Kéo AudioClip vào từng ô trong SoundLibrary.");

        // 7. Ping asset để user thấy ngay
        EditorGUIUtility.PingObject(lib);
        Selection.activeObject = lib;
    }
    // Điền sẵn entries cho tất cả SoundId — chỉ thêm entry chưa có, không ghi đè.
    // Pitch defaults khác nhau theo loại sound để nghe tự nhiên hơn.
    static void PopulateLibraryEntries(SoundLibrary lib)
    {
        var libSO = new SerializedObject(lib);
        var entries = libSO.FindProperty("entries");

        // Định nghĩa pitch range hợp lý cho từng sound
        var defaults = new (SoundId id, float vol, float pMin, float pMax)[]
        {
            (SoundId.UIHover,   0.4f,  0.95f, 1.05f),
            (SoundId.UIClick,   0.6f,  0.92f, 1.08f),
            (SoundId.Reroll,    0.7f,  0.90f, 1.10f),
            (SoundId.Buy,       0.8f,  0.93f, 1.07f),
            (SoundId.WaveStart, 1.0f,  0.97f, 1.03f),
            (SoundId.Attack,    0.8f,  0.88f, 1.12f),
            (SoundId.Hit,       0.7f,  0.85f, 1.15f),
            (SoundId.Death,     0.9f,  0.90f, 1.10f),
            (SoundId.Heal,      0.7f,  0.93f, 1.07f),
            (SoundId.WheelSpin, 0.8f,  0.97f, 1.03f),
        };

        bool changed = false;
        foreach (var def in defaults)
        {
            // Kiểm tra entry đã tồn tại chưa
            bool exists = false;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                if ((SoundId)e.FindPropertyRelative("id").enumValueIndex == def.id)
                { exists = true; break; }
            }
            if (exists) continue;

            // Thêm entry mới
            entries.InsertArrayElementAtIndex(entries.arraySize);
            var newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            newEntry.FindPropertyRelative("id").enumValueIndex      = (int)def.id;
            newEntry.FindPropertyRelative("clips").ClearArray();    // clips trống, user tự kéo vào
            newEntry.FindPropertyRelative("volume").floatValue      = def.vol;
            newEntry.FindPropertyRelative("pitchMin").floatValue    = def.pMin;
            newEntry.FindPropertyRelative("pitchMax").floatValue    = def.pMax;
            changed = true;
        }

        if (changed)
        {
            libSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sound] Đã điền sẵn tất cả SoundId entries trong SoundLibrary.");
        }
    }
    // ── Add UI Sounds to All Buttons ────────────────────────────────────────

    [MenuItem("Tools/Sound/Add UI Sounds to All Buttons")]
    static void AddUISoundsToAll()
    {
        int added = 0;

        // Quét tất cả Button trong scene (kể cả inactive)
        var buttons = Object.FindObjectsByType<UnityEngine.UI.Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var btn in buttons)
        {
            if (btn.GetComponent<UISoundTrigger>() != null) continue;
            Undo.AddComponent<UISoundTrigger>(btn.gameObject);
            added++;
        }

        // Quét thêm các item slot UI (có thể không có Button nhưng cần hover/click)
        AddToType<ShopOfferSlotUI>(ref added);
        AddToType<ShopInventorySlotUI>(ref added);
        AddToType<ArenaHotbarSlotUI>(ref added);
        AddToType<GachaPackSlotUI>(ref added);
        AddToType<RerollButtonUI>(ref added);

        if (added > 0)
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[Sound] Đã thêm UISoundTrigger vào {added} object. Nếu = 0 thì tất cả đã có rồi.");
    }

    static void AddToType<T>(ref int count) where T : Component
    {
        var comps = Object.FindObjectsByType<T>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in comps)
        {
            if (c.GetComponent<UISoundTrigger>() != null) continue;
            // Chỉ thêm nếu GO chưa có Button (Button đã được quét ở trên)
            if (c.GetComponent<UnityEngine.UI.Button>() != null) continue;
            Undo.AddComponent<UISoundTrigger>(c.gameObject);
            count++;
        }
    }
}
#endif
