#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tools > Game > Setup Level Award Panel
///
/// Tạo AwardPanel trong ShopRoot hiển thị "Award: +X 🌽".
///
/// Hierarchy tạo ra:
///   ShopRoot
///   └── AwardPanel          [LevelAwardPanelUI]
///       ├── BgImage         Image nền (optional, tự kéo sprite vào)
///       ├── CornIcon        Image icon corn (optional)
///       └── AwardText       TMP_Text: "Award: +5"
/// </summary>
public static class LevelAwardPanelSetupTool
{
    [MenuItem("Tools/Game/Setup Level Award Panel")]
    static void Setup()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogError("[AwardPanel] Không tìm thấy Canvas."); return; }

        var shopRootT = FindInHierarchy(canvas.transform, "ShopRoot");
        if (shopRootT == null) { Debug.LogError("[AwardPanel] Không tìm thấy ShopRoot."); return; }

        // ── AwardPanel root ───────────────────────────────────────────────────
        var panel   = EnsureChild(shopRootT, "AwardPanel");
        var panelRT = EnsureRT(panel);
        // Góc dưới-phải ShopRoot — chỉnh vị trí tay nếu muốn
        panelRT.anchorMin        = new Vector2(1f, 0f);
        panelRT.anchorMax        = new Vector2(1f, 0f);
        panelRT.pivot            = new Vector2(1f, 0f);
        panelRT.anchoredPosition = new Vector2(-10f, 10f);
        panelRT.sizeDelta        = new Vector2(220f, 60f);
        var ui = EnsureComponent<LevelAwardPanelUI>(panel);

        // ── BgImage (nền tối nhẹ, optional) ──────────────────────────────────
        var bg   = EnsureChild(panel.transform, "BgImage");
        var bgRT = EnsureRT(bg);
        Stretch(bgRT);
        var bgImg = EnsureComponent<Image>(bg);
        bgImg.color = new Color(0f, 0f, 0f, 0.45f);
        bgImg.raycastTarget = false;

        // ── CornIcon ──────────────────────────────────────────────────────────
        var icon   = EnsureChild(panel.transform, "CornIcon");
        var iconRT = EnsureRT(icon);
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot     = new Vector2(0f, 0.5f);
        iconRT.anchoredPosition = new Vector2(8f, 0f);
        iconRT.sizeDelta        = new Vector2(40f, 40f);
        var iconImg = EnsureComponent<Image>(icon);
        iconImg.raycastTarget = false;
        iconImg.color = Color.white;
        // Kéo sprite Corn vào CornIcon > Image.Sprite trong Inspector

        // ── AwardText ─────────────────────────────────────────────────────────
        var txt   = EnsureChild(panel.transform, "AwardText");
        var txtRT = EnsureRT(txt);
        txtRT.anchorMin        = new Vector2(0f, 0f);
        txtRT.anchorMax        = new Vector2(1f, 1f);
        txtRT.offsetMin        = new Vector2(54f, 0f);   // để trống bên trái cho icon
        txtRT.offsetMax        = new Vector2(-8f, 0f);
        var tmp = EnsureComponent<TextMeshProUGUI>(txt);
        tmp.text      = "Award: +5";
        tmp.fontSize  = 26f;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        tmp.color     = new Color(1f, 0.9f, 0.2f);       // vàng
        tmp.raycastTarget = false;

        // ── Wire LevelAwardPanelUI ────────────────────────────────────────────
        // Dùng SerializedObject để gán private SerializeField
        var so = new SerializedObject(ui);
        so.FindProperty("awardText").objectReferenceValue = tmp;
        so.FindProperty("cornIcon").objectReferenceValue  = iconImg;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(ui);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[AwardPanel] ✓ AwardPanel tạo trong ShopRoot.\n" +
                  "• Kéo sprite Corn vào AwardPanel > CornIcon > Image.Sprite\n" +
                  "• Chỉnh vị trí AwardPanel trong Inspector (anchoredPosition)\n" +
                  "• Mỗi LevelData asset có field 'Wave Win Reward' để set riêng per-level");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static Transform FindInHierarchy(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root) { var f = FindInHierarchy(c, name); if (f != null) return f; }
        return null;
    }

    static GameObject EnsureChild(Transform parent, string name)
    {
        var e = parent.Find(name);
        if (e != null) return e.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    static RectTransform EnsureRT(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    { var c = go.GetComponent<T>(); if (c == null) c = go.AddComponent<T>(); return c; }
}
#endif
