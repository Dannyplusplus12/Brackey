using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Thêm StatsRow vào PanelBg (trong TooltipPanel): 4 ô stat bên dưới DescText.
// Chạy: Tools > Shop-Arena Setup > Build StatBar In TooltipPanel
public static class StatBarSetupTool
{
    [MenuItem("Tools/Shop-Arena Setup/Build StatBar In TooltipPanel")]
    public static void BuildStatBar()
    {
        var tooltipPanel = GameObject.Find("TooltipPanel");
        if (tooltipPanel == null)
        {
            Debug.LogError("[StatBar] Không tìm thấy TooltipPanel trong scene.");
            return;
        }

        // Ưu tiên đặt StatsRow vào PanelBg nếu có, không thì đặt thẳng vào TooltipPanel
        var panelBgT = tooltipPanel.transform.Find("PanelBg");
        var statsParent = panelBgT != null ? panelBgT.gameObject : tooltipPanel;

        // ── Xóa TẤT CẢ CharacterStatBar trong scene (kể cả GO cũ ngoài TooltipPanel) ──
#if UNITY_2023_1_OR_NEWER
        var allBars = Object.FindObjectsByType<CharacterStatBar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var allBars = Object.FindObjectsOfType<CharacterStatBar>(true);
#endif
        foreach (var bar in allBars)
        {
            Debug.Log($"[StatBar] Xóa GO cũ: {bar.gameObject.name} (parent: {bar.transform.parent?.name})");
            GameObject.DestroyImmediate(bar.gameObject);
        }

        // ── Tạo StatsRow ─────────────────────────────────────────────────────
        var rowGO = new GameObject("StatsRow");
        rowGO.transform.SetParent(statsParent.transform, false);

        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0f, 32f);

        var rowHLG = rowGO.AddComponent<HorizontalLayoutGroup>();
        rowHLG.childAlignment        = TextAnchor.MiddleLeft;
        rowHLG.spacing               = 12f;
        rowHLG.padding               = new RectOffset(0, 0, 0, 0);
        rowHLG.childForceExpandWidth = false;
        rowHLG.childForceExpandHeight= false;
        rowHLG.childControlWidth     = false;
        rowHLG.childControlHeight    = false;

        // CharacterStatBar script trên StatsRow
        var statBarScript = rowGO.AddComponent<CharacterStatBar>();

        // ── Tạo 4 slot và auto-wire ───────────────────────────────────────────
        string[] names = { "SlotHP", "SlotDMG", "SlotSPD", "SlotAngry" };
        var so = new SerializedObject(statBarScript);
        var slotsArr = so.FindProperty("slots");
        slotsArr.arraySize = 4;

        for (int i = 0; i < 4; i++)
        {
            var slotGO  = CreateSlot(rowGO.transform, names[i]);
            var iconImg = slotGO.transform.Find("Icon").GetComponent<Image>();
            var label   = slotGO.transform.Find("Label").GetComponent<TextMeshProUGUI>();

            var elem = slotsArr.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("icon").objectReferenceValue  = iconImg;
            elem.FindPropertyRelative("label").objectReferenceValue = label;
        }
        so.ApplyModifiedProperties();

        rowGO.transform.SetAsLastSibling();
        // KHÔNG SetActive(false) ở đây — CharacterStatBar.Awake() tự gọi Hide()
        // Nếu GO bắt đầu inactive, Awake không chạy → Instance = null mãi mãi

        EditorUtility.SetDirty(statsParent);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tooltipPanel.scene);

        string loc = panelBgT != null ? "PanelBg" : "TooltipPanel";
        Debug.Log($"[StatBar] Xong! StatsRow đã nằm trong {loc}. Gán icon sprites vào CharacterStatBar trong Inspector.");
        Selection.activeGameObject = rowGO;
    }

    static GameObject CreateSlot(Transform parent, string slotName)
    {
        var slot = new GameObject(slotName);
        slot.transform.SetParent(parent, false);

        var rt = slot.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60f, 32f);

        var hlg = slot.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.spacing               = 3f;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight= false;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = false;

        // Icon
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(slot.transform, false);
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(18f, 18f);
        var img = iconGO.AddComponent<Image>();
        img.preserveAspect = true;
        img.color = Color.white;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(slot.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(38f, 28f);
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "—";
        tmp.fontSize  = 12f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color     = Color.white;

        return slot;
    }
}
