#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Chạy 1 lần: Tools > VFX > Setup VFX In Scene
// Tự wire prefab vào VFXLibrary và thêm VFXManager vào Managers GO trong scene.
public static class VFXSetupTool
{
    const string LibraryPath = "Assets/VFX/VFXLibrary.asset";
    const string PrefabDir   = "Assets/VFX/Prefabs";

    [MenuItem("Tools/VFX/Setup VFX In Scene")]
    static void Setup()
    {
        // 1. Tạo hoặc load VFXLibrary
        var lib = AssetDatabase.LoadAssetAtPath<VFXLibrary>(LibraryPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<VFXLibrary>();
            AssetDatabase.CreateAsset(lib, LibraryPath);
        }

        // 2. Wire prefabs theo tên file
        lib.bloodHit   = Load("BloodHit");
        lib.deathBurst = Load("DeathBurst");
        lib.buffArrow  = Load("BuffArrow");
        lib.feedHappy  = Load("FeedHappy");
        lib.feedAngry  = Load("FeedAngry");
        lib.stunStars  = Load("StunStars");
        lib.spawnPop   = Load("SpawnPop");

        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();

        // 3. Tìm Managers GO trong scene, thêm VFXManager nếu chưa có
        var managers = GameObject.Find("Managers");
        if (managers == null)
        {
            Debug.LogWarning("[VFXSetup] Không tìm thấy GO tên 'Managers' trong scene. Tạo mới.");
            managers = new GameObject("Managers");
        }

        var vfxMgr = managers.GetComponent<VFXManager>();
        if (vfxMgr == null)
            vfxMgr = managers.AddComponent<VFXManager>();

        // 4. Gán library vào VFXManager qua SerializedObject
        var so = new SerializedObject(vfxMgr);
        so.FindProperty("library").objectReferenceValue = lib;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(managers.scene);
        EditorUtility.SetDirty(managers);

        Debug.Log("[VFXSetup] Xong! VFXLibrary đã wire đủ prefab, VFXManager đã gắn vào Managers.");
        Selection.activeObject = lib;
        EditorGUIUtility.PingObject(lib);
    }

    static GameObject Load(string name)
    {
        string path   = $"{PrefabDir}/{name}.prefab";
        var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) Debug.LogWarning($"[VFXSetup] Không tìm thấy prefab: {path}");
        return prefab;
    }
}
#endif
