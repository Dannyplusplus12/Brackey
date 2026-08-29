#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Tools > UI > Build Defeat Screen
///
/// Dựng toàn bộ hierarchy Defeat Screen trong scene hiện tại.
/// Chạy lại: tool kiểm tra từng GO, chỉ tạo phần còn thiếu — safe để re-run.
///
/// Kết quả trong Canvas (Screen Space Overlay):
///   DefeatScreen  [CanvasGroup, DefeatScreenUI, inactive]
///   └── Overlay   [Image màu xám mờ, raycast target]
///       └── Panel [Image trắng bo góc, VerticalLayoutGroup]
///           ├── TitleText     "YOU LOSE"
///           ├── StatsGrid     [GridLayoutGroup — 2 cột label/value]
///           │   ├── LabelWaves / ValueWaves
///           │   ├── LabelKills / ValueKills
///           │   ├── LabelChars / ValueChars
///           │   ├── LabelItems / ValueItems
///           │   ├── LabelDmg   / ValueDmg
///           │   └── LabelCorn  / ValueCorn
///           └── RestartButton [Button + TMP_Text]
/// </summary>
public static class DefeatScreenSetupTool
{
    [MenuItem("Tools/UI/Build Defeat Screen")]
    public static void Build()
    {
        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType == null)
        {
            Debug.LogError("[DefeatScreen] Không tìm thấy TextMeshProUGUI — import TMP trước.");
            return;
        }

        // ── Tìm Canvas ────────────────────────────────────────────────────────
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[DefeatScreen] Không tìm thấy Canvas trong scene.");
            return;
        }

        // ── Root: DefeatScreen ────────────────────────────────────────────────
        GameObject root = EnsureChild(canvas.gameObject, "DefeatScreen");
        Undo.RegisterFullObjectHierarchyUndo(root, "Build DefeatScreen");

        // Stretch full
        var rootRt = root.GetComponent<RectTransform>();
        StretchFull(rootRt);

        // CanvasGroup
        var cg = EnsureComponent<CanvasGroup>(root);
        cg.alpha          = 0f;
        cg.interactable   = false;
        cg.blocksRaycasts = false;

        // Inactive mặc định — DefeatScreenUI.Show() sẽ SetActive(true)
        root.SetActive(false);

        // ── Overlay (Image xám mờ phủ màn hình) ──────────────────────────────
        GameObject overlay = EnsureChild(root, "Overlay");
        StretchFull(overlay.GetComponent<RectTransform>());
        var overlayImg = EnsureComponent<Image>(overlay);
        overlayImg.color         = new Color(0f, 0f, 0f, 0.55f);
        overlayImg.raycastTarget = true;

        // ── Panel (hộp nội dung ở giữa) ──────────────────────────────────────
        GameObject panel = EnsureChild(overlay, "Panel");
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRt.pivot            = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta        = new Vector2(460f, 520f);
        panelRt.anchoredPosition = Vector2.zero;

        var panelImg = EnsureComponent<Image>(panel);
        panelImg.color         = new Color(0.12f, 0.08f, 0.08f, 0.96f);
        panelImg.raycastTarget = false;

        var vlg = EnsureComponent<VerticalLayoutGroup>(panel);
        vlg.childAlignment          = TextAnchor.UpperCenter;
        vlg.spacing                 = 18f;
        vlg.padding                 = new RectOffset(30, 30, 36, 30);
        vlg.childControlWidth       = true;
        vlg.childControlHeight      = false;
        vlg.childForceExpandWidth   = true;
        vlg.childForceExpandHeight  = false;

        var csf = EnsureComponent<ContentSizeFitter>(panel);
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Title: YOU LOSE ────────────────────────────────────────────────────
        GameObject titleGO = EnsureChild(panel, "TitleText");
        SetRectHeight(titleGO, 80f);
        var titleTmp = EnsureComponentByType(titleGO, tmpType);
        SetTmpProps(tmpType, titleTmp, "YOU LOSE", 68f, new Color(0.9f, 0.18f, 0.18f));
        SetTmpBold(tmpType, titleTmp);
        SetTmpAlignment(tmpType, titleTmp, "Center");

        // ── Divider ────────────────────────────────────────────────────────────
        GameObject divider = EnsureChild(panel, "Divider");
        SetRectHeight(divider, 2f);
        var divImg = EnsureComponent<Image>(divider);
        divImg.color = new Color(0.9f, 0.18f, 0.18f, 0.6f);
        divImg.raycastTarget = false;

        // ── Stats Grid ─────────────────────────────────────────────────────────
        GameObject grid = EnsureChild(panel, "StatsGrid");
        SetRectHeight(grid, 220f);
        var glg = EnsureComponent<GridLayoutGroup>(grid);
        glg.cellSize            = new Vector2(185f, 32f);
        glg.spacing             = new Vector2(8f, 10f);
        glg.startCorner         = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis           = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment      = TextAnchor.UpperCenter;
        glg.constraint          = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount     = 2;

        var gridCsf = EnsureComponent<ContentSizeFitter>(grid);
        gridCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Tạo 6 cặp label/value
        var statDefs = new (string label, string fieldLabel, string fieldValue)[]
        {
            ("Wave đã chơi",       "LabelWaves",  "ValueWaves"),
            ("Địch đã giết",       "LabelKills",  "ValueKills"),
            ("Nhân vật đã mua",    "LabelChars",  "ValueChars"),
            ("Item đã mua",        "LabelItems",  "ValueItems"),
            ("Sát thương gây ra",  "LabelDmg",    "ValueDmg"),
            ("Corn kiếm được",     "LabelCorn",   "ValueCorn"),
        };

        Component[] valueTexts = new Component[6];
        for (int i = 0; i < statDefs.Length; i++)
        {
            // Label (cột trái)
            GameObject labelGO = EnsureChild(grid, statDefs[i].fieldLabel);
            var lTmp = EnsureComponentByType(labelGO, tmpType);
            SetTmpProps(tmpType, lTmp, statDefs[i].label, 17f, new Color(0.78f, 0.72f, 0.72f));
            SetTmpAlignment(tmpType, lTmp, "MidlineLeft");

            // Value (cột phải)
            GameObject valGO = EnsureChild(grid, statDefs[i].fieldValue);
            var vTmp = EnsureComponentByType(valGO, tmpType);
            SetTmpProps(tmpType, vTmp, "0", 20f, Color.white);
            SetTmpBold(tmpType, vTmp);
            SetTmpAlignment(tmpType, vTmp, "MidlineRight");
            valueTexts[i] = vTmp;
        }

        // ── Restart Button ─────────────────────────────────────────────────────
        GameObject btnGO = EnsureChild(panel, "RestartButton");
        SetRectHeight(btnGO, 54f);
        var btn      = EnsureComponent<Button>(btnGO);
        var btnImage = EnsureComponent<Image>(btnGO);
        btnImage.color = new Color(0.85f, 0.22f, 0.22f);
        ColorBlock cb  = btn.colors;
        cb.highlightedColor = new Color(1f, 0.35f, 0.35f);
        cb.pressedColor     = new Color(0.6f, 0.1f, 0.1f);
        btn.colors = cb;
        btn.targetGraphic = btnImage;

        GameObject btnTextGO = EnsureChild(btnGO, "Text");
        StretchFull(btnTextGO.GetComponent<RectTransform>());
        var btnTmp = EnsureComponentByType(btnTextGO, tmpType);
        SetTmpProps(tmpType, btnTmp, "Chơi lại", 24f, Color.white);
        SetTmpBold(tmpType, btnTmp);
        SetTmpAlignment(tmpType, btnTmp, "Center");

        // ── Wire DefeatScreenUI ────────────────────────────────────────────────
        var ui = EnsureComponent<DefeatScreenUI>(root);
        var so = new SerializedObject(ui);
        SetRef(so, "canvasGroup",      cg);
        SetRef(so, "wavesText",        valueTexts[0]);
        SetRef(so, "killsText",        valueTexts[1]);
        SetRef(so, "charsBoughtText",  valueTexts[2]);
        SetRef(so, "itemsBoughtText",  valueTexts[3]);
        SetRef(so, "damageText",       valueTexts[4]);
        SetRef(so, "cornText",         valueTexts[5]);
        SetRef(so, "restartButton",    btn);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);

        // ── RunTracker (nếu chưa có trong scene) ──────────────────────────────
        if (Object.FindFirstObjectByType<RunTracker>() == null)
        {
            var rt = new GameObject("RunTracker");
            Undo.RegisterCreatedObjectUndo(rt, "Create RunTracker");
            rt.AddComponent<RunTracker>();
            Debug.Log("[DefeatScreen] Đã tạo RunTracker GameObject trong scene.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[DefeatScreen] Xong!\n" +
                  "  • DefeatScreen đã inactive — sẽ hiện khi GameManager.OnDefeat fire.\n" +
                  "  • Chỉnh màu sắc / font / kích thước trong Inspector nếu cần.\n" +
                  "  • Đảm bảo RunTracker có trong scene (tool đã tạo nếu chưa có).");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject EnsureChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null) return t.gameObject;

        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null)
        {
            c = Undo.AddComponent<T>(go);
        }
        return c;
    }

    static Component EnsureComponentByType(GameObject go, System.Type type)
    {
        var c = go.GetComponent(type);
        if (c == null)
        {
            c = Undo.AddComponent(go, type);
        }
        return c;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
    }

    static void SetRectHeight(GameObject go, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        var le = EnsureComponent<LayoutElement>(go);
        le.preferredHeight = h;
        le.flexibleWidth   = 1f;
    }

    static void SetTmpProps(System.Type t, Component c, string text, float size, Color color)
    {
        t.GetProperty("text")?.SetValue(c, text);
        t.GetProperty("fontSize")?.SetValue(c, size);
        t.GetProperty("color")?.SetValue(c, color);
        t.GetProperty("raycastTarget")?.SetValue(c, false);
        // Overflow: Overflow
        var overflowType = System.Type.GetType("TMPro.TextOverflowModes, Unity.TextMeshPro");
        if (overflowType != null)
            t.GetProperty("overflowMode")?.SetValue(c, System.Enum.Parse(overflowType, "Overflow"));
    }

    static void SetTmpBold(System.Type t, Component c)
    {
        var styleType = System.Type.GetType("TMPro.FontStyles, Unity.TextMeshPro");
        if (styleType != null)
            t.GetProperty("fontStyle")?.SetValue(c, System.Enum.Parse(styleType, "Bold"));
    }

    static void SetTmpAlignment(System.Type t, Component c, string alignName)
    {
        var alignType = System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
        if (alignType == null) return;
        try { t.GetProperty("alignment")?.SetValue(c, System.Enum.Parse(alignType, alignName)); }
        catch { }
    }

    static void SetRef(SerializedObject so, string propName, Object value)
    {
        var prop = so.FindProperty(propName);
        if (prop != null) prop.objectReferenceValue = value;
    }
}
#endif
