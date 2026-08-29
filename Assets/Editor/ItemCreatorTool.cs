using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor Window tạo ItemData asset + auto-generate TMP rich text description.
// Menu: Tools/Items/Item Creator
//
// Workflow:
//   1. Điền thông tin item (tên, loại, stat delta, v.v.)
//   2. Nhấn "Auto-gen Description" → description được sinh tự động
//   3. Chỉnh tay nếu muốn thêm lore/note
//   4. Nhấn "Create Asset" → tạo file .asset vào Assets/Data/Items/
public class ItemCreatorTool : EditorWindow
{
    // ── Identity ──────────────────────────────────────────────────────────────
    string itemName   = "New Item";
    ItemType itemType = ItemType.StatBoost;
    ItemRarity rarity = ItemRarity.Common;
    int buyCost       = 3;
    int sellValue     = 1;
    string loreText   = "";

    // ── Stat Target ───────────────────────────────────────────────────────────
    ItemTargetType targetType = ItemTargetType.AllCharacters;
    CharacterStats targetCharacterType;

    // ── Stat Delta — Flat ─────────────────────────────────────────────────────
    float d_damage;
    float d_maxHP;
    float d_moveSpeed;
    float d_attackSpeed;
    float d_attackRange;
    float d_foodCost;

    // ── Stat Delta — Percent (0.1 = 10%) ─────────────────────────────────────
    float d_damagePercent;
    float d_maxHPPercent;
    float d_moveSpeedPercent;
    float d_attackSpeedPercent;

    // ── Description ───────────────────────────────────────────────────────────
    string generatedDesc = "";
    Vector2 descScroll;

    // ── Foldout state ─────────────────────────────────────────────────────────
    bool showFlat    = true;
    bool showPercent = true;
    bool showDesc    = true;

    [MenuItem("Tools/Items/Item Creator")]
    public static void Open() => GetWindow<ItemCreatorTool>("Item Creator");

    // Quét toàn bộ Assets/Data/Items/, thêm mọi ItemData vào ShopOfferManager.itemPool,
    // và đảm bảo ItemEffectHandler có mặt trong scene. Bấm 1 lần sau khi tôi tạo item mới.
    [MenuItem("Tools/Items/Sync Item Pool")]
    public static void SyncItemPool()
    {
        var manager = FindFirstObjectByType<ShopOfferManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Sync Item Pool", "Không tìm thấy ShopOfferManager trong scene.", "OK");
            return;
        }

        // Gắn ItemEffectHandler nếu chưa có
        if (manager.gameObject.GetComponent<ItemEffectHandler>() == null)
        {
            manager.gameObject.AddComponent<ItemEffectHandler>();
            Debug.Log("[SyncItemPool] Đã gắn ItemEffectHandler.");
        }

        // Quét tất cả ItemData trong Assets/Data/Items/
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Data/Items" });
        var so   = new SerializedObject(manager);
        var pool = so.FindProperty("itemPool");

        int added = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item    = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null) continue;

            // Bỏ qua nếu đã có trong pool
            bool exists = false;
            for (int i = 0; i < pool.arraySize; i++)
                if (pool.GetArrayElementAtIndex(i).objectReferenceValue == item) { exists = true; break; }
            if (exists) continue;

            pool.arraySize++;
            pool.GetArrayElementAtIndex(pool.arraySize - 1).objectReferenceValue = item;
            added++;
            Debug.Log($"[SyncItemPool] Thêm: {item.displayName}");
        }

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        EditorUtility.DisplayDialog("Sync Item Pool",
            added > 0 ? $"Đã thêm {added} item vào pool.\nNhớ Save Scene (Ctrl+S)." : "Pool đã đầy đủ, không có gì thay đổi.",
            "OK");
    }

    void OnGUI()
    {
        EditorGUILayout.Space(6);
        GUILayout.Label("Item Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // ── Identity ──────────────────────────────────────────────────────────
        DrawSection("Identity");
        itemName  = EditorGUILayout.TextField("Tên item", itemName);
        itemType  = (ItemType)EditorGUILayout.EnumPopup("Item Type", itemType);
        rarity    = (ItemRarity)EditorGUILayout.EnumPopup("Rarity", rarity);
        buyCost   = EditorGUILayout.IntField("Buy Cost (corn)", buyCost);
        sellValue = EditorGUILayout.IntField("Sell Value (corn)", sellValue);
        loreText  = EditorGUILayout.TextField("Lore (để trống nếu không cần)", loreText);

        EditorGUILayout.Space(6);

        // ── Target ────────────────────────────────────────────────────────────
        DrawSection("Stat Target");
        targetType = (ItemTargetType)EditorGUILayout.EnumPopup("Target", targetType);
        if (targetType == ItemTargetType.SpecificType)
            targetCharacterType = (CharacterStats)EditorGUILayout.ObjectField(
                "Character Type", targetCharacterType, typeof(CharacterStats), false);

        EditorGUILayout.Space(6);

        // ── Flat Delta ────────────────────────────────────────────────────────
        showFlat = EditorGUILayout.Foldout(showFlat, "Stat Flat (cộng thẳng)", true, EditorStyles.foldoutHeader);
        if (showFlat)
        {
            EditorGUI.indentLevel++;
            d_maxHP       = EditorGUILayout.FloatField("HP", d_maxHP);
            d_damage      = EditorGUILayout.FloatField("Damage", d_damage);
            d_moveSpeed   = EditorGUILayout.FloatField("Move Speed", d_moveSpeed);
            d_attackSpeed = EditorGUILayout.FloatField("Attack Speed (APS)", d_attackSpeed);
            d_attackRange = EditorGUILayout.FloatField("Attack Range", d_attackRange);
            d_foodCost    = EditorGUILayout.FloatField("Food Cost (+corn/round)", d_foodCost);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        // ── Percent Delta ─────────────────────────────────────────────────────
        showPercent = EditorGUILayout.Foldout(showPercent, "Stat % (hệ số nhân, 0.1 = +10%)", true, EditorStyles.foldoutHeader);
        if (showPercent)
        {
            EditorGUI.indentLevel++;
            d_maxHPPercent       = EditorGUILayout.FloatField("HP %", d_maxHPPercent);
            d_damagePercent      = EditorGUILayout.FloatField("Damage %", d_damagePercent);
            d_moveSpeedPercent   = EditorGUILayout.FloatField("Move Speed %", d_moveSpeedPercent);
            d_attackSpeedPercent = EditorGUILayout.FloatField("Attack Speed %", d_attackSpeedPercent);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        // ── Description ───────────────────────────────────────────────────────
        showDesc = EditorGUILayout.Foldout(showDesc, "Description (TMP Rich Text)", true, EditorStyles.foldoutHeader);
        if (showDesc)
        {
            EditorGUI.indentLevel++;
            if (GUILayout.Button("Auto-gen Description"))
                generatedDesc = GenerateDescription();

            EditorGUILayout.Space(4);
            descScroll    = EditorGUILayout.BeginScrollView(descScroll, GUILayout.Height(120));
            generatedDesc = EditorGUILayout.TextArea(generatedDesc, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(generatedDesc))
                EditorGUILayout.HelpBox("Chỉnh tay nếu muốn thêm điều kiện đặc biệt, note, v.v.", MessageType.Info);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // ── Buttons ───────────────────────────────────────────────────────────
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Create Asset", GUILayout.Height(36)))
            CreateAsset();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Reset Form"))
            ResetForm();
    }

    // ── Description generator ─────────────────────────────────────────────────

    string GenerateDescription()
    {
        var sb = new StringBuilder();

        if (itemType == ItemType.Active)
            sb.AppendLine("<b>Activate:</b>");

        // Flat stats
        AppendStatLine(sb, d_maxHP,       "stat_hp",       "HP");
        AppendStatLine(sb, d_damage,      "stat_damage",   "Damage");
        AppendStatLine(sb, d_moveSpeed,   "stat_speed",    "Speed");
        AppendStatLine(sb, d_attackSpeed, "stat_atkspeed", "Atk Speed");
        AppendStatLine(sb, d_attackRange, null,            "Atk Range");
        AppendFoodLine(sb, d_foodCost);

        // Percent stats
        AppendPercentLine(sb, d_maxHPPercent,       "stat_hp",       isPosGood: true);
        AppendPercentLine(sb, d_damagePercent,      "stat_damage",   isPosGood: true);
        AppendPercentLine(sb, d_moveSpeedPercent,   "stat_speed",    isPosGood: true);
        AppendPercentLine(sb, d_attackSpeedPercent, "stat_atkspeed", isPosGood: true);

        // Scope note
        if (targetType == ItemTargetType.SpecificType && targetCharacterType != null)
            sb.AppendLine($"<color=#A0A0A0><size=80%>{targetCharacterType.name} only</size></color>");

        // Lore
        if (!string.IsNullOrWhiteSpace(loreText))
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append($"<color=#A0A0A0><i>{loreText.Trim()}</i></color>");
        }

        return sb.ToString().TrimEnd('\n', '\r');
    }

    // Flat stat line: "▲ +X <sprite>"
    static void AppendStatLine(StringBuilder sb, float value, string spriteName, string label)
    {
        if (Mathf.Approximately(value, 0f)) return;
        bool pos     = value > 0f;
        string color = pos ? "#4CAF50" : "#E82020";
        string sign  = pos ? "+" : "";
        string icon  = !string.IsNullOrEmpty(spriteName) ? $"<sprite name=\"{spriteName}\">" : label;
        sb.AppendLine($"<color={color}>{sign}{Fmt(value)}</color> {icon}");
    }

    // Percent stat line: "+10% <sprite>"
    static void AppendPercentLine(StringBuilder sb, float value, string spriteName, bool isPosGood)
    {
        if (Mathf.Approximately(value, 0f)) return;
        bool good    = isPosGood ? value > 0f : value < 0f;
        string color = good ? "#4CAF50" : "#E82020";
        string sign  = value > 0f ? "+" : "";
        string icon  = $"<sprite name=\"{spriteName}\">";
        int pct      = Mathf.RoundToInt(value * 100f);
        sb.AppendLine($"<color={color}>{sign}{pct}%</color> {icon}");
    }

    // Food cost line
    static void AppendFoodLine(StringBuilder sb, float value)
    {
        if (Mathf.Approximately(value, 0f)) return;
        bool expensive = value > 0f;
        string color   = expensive ? "#E82020" : "#4CAF50";
        string sign    = value > 0f ? "+" : "";
        sb.AppendLine($"<color={color}>{sign}{Fmt(value)}</color> <sprite name=\"coin\">/round");
    }

    static string Fmt(float v)
        => Mathf.Approximately(v % 1f, 0f) ? Mathf.Abs(v).ToString("0") : Mathf.Abs(v).ToString("0.#");

    // ── Asset creation ────────────────────────────────────────────────────────

    void CreateAsset()
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            EditorUtility.DisplayDialog("Item Creator", "Chưa nhập tên item.", "OK");
            return;
        }

        const string saveFolder = "Assets/Data/Items";
        if (!AssetDatabase.IsValidFolder(saveFolder))
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Items"));

        string safeName  = string.Concat(itemName.Split(Path.GetInvalidFileNameChars()));
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{saveFolder}/{safeName}.asset");

        var item                 = CreateInstance<ItemData>();
        item.displayName         = itemName;
        item.itemType            = itemType;
        item.rarity              = rarity;
        item.buyCost             = buyCost;
        item.sellValue           = sellValue;
        item.description         = generatedDesc;
        item.targetType          = targetType;
        item.targetCharacterType = targetType == ItemTargetType.SpecificType ? targetCharacterType : null;
        item.statDelta           = new StatDelta
        {
            damage             = d_damage,
            maxHP              = d_maxHP,
            moveSpeed          = d_moveSpeed,
            attackSpeed        = d_attackSpeed,
            attackRange        = d_attackRange,
            foodCost           = d_foodCost,
            damagePercent      = d_damagePercent,
            maxHPPercent       = d_maxHPPercent,
            moveSpeedPercent   = d_moveSpeedPercent,
            attackSpeedPercent = d_attackSpeedPercent,
        };

        AssetDatabase.CreateAsset(item, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Tự gắn vào ShopOfferManager.itemPool trong scene đang mở
        AddToShopPool(item);

        // Đảm bảo ItemEffectHandler có trên ShopManagers GO
        EnsureItemEffectHandler();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = item;

        Debug.Log($"[ItemCreator] Đã tạo: {assetPath}");
        EditorUtility.DisplayDialog("Item Creator", $"Đã tạo và thêm vào shop:\n{assetPath}", "OK");
    }

    // Tìm ShopOfferManager trong scene đang mở, thêm item vào itemPool nếu chưa có.
    static void AddToShopPool(ItemData item)
    {
        var manager = FindFirstObjectByType<ShopOfferManager>();
        if (manager == null)
        {
            Debug.LogWarning("[ItemCreator] Không tìm thấy ShopOfferManager trong scene. Thêm item vào pool thủ công.");
            return;
        }

        var so   = new UnityEditor.SerializedObject(manager);
        var pool = so.FindProperty("itemPool");

        // Kiểm tra trùng
        for (int i = 0; i < pool.arraySize; i++)
            if (pool.GetArrayElementAtIndex(i).objectReferenceValue == item) return;

        pool.arraySize++;
        pool.GetArrayElementAtIndex(pool.arraySize - 1).objectReferenceValue = item;
        so.ApplyModifiedProperties();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        Debug.Log($"[ItemCreator] Đã thêm '{item.displayName}' vào ShopOfferManager.itemPool.");
    }

    // Tìm GO có ShopOfferManager, gắn ItemEffectHandler nếu chưa có.
    static void EnsureItemEffectHandler()
    {
        var manager = FindFirstObjectByType<ShopOfferManager>();
        if (manager == null) return;

        var go = manager.gameObject;
        if (go.GetComponent<ItemEffectHandler>() != null) return;

        go.AddComponent<ItemEffectHandler>();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log($"[ItemCreator] Đã gắn ItemEffectHandler lên '{go.name}'.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void ResetForm()
    {
        itemName = "New Item"; itemType = ItemType.StatBoost; rarity = ItemRarity.Common;
        buyCost  = 3; sellValue = 1; loreText = "";
        targetType = ItemTargetType.AllCharacters; targetCharacterType = null;
        d_damage = d_maxHP = d_moveSpeed = d_attackSpeed = d_attackRange = d_foodCost = 0f;
        d_damagePercent = d_maxHPPercent = d_moveSpeedPercent = d_attackSpeedPercent = 0f;
        generatedDesc = "";
    }

    static void DrawSection(string label)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        var rect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(2);
    }
}
