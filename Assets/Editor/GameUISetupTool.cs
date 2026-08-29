using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Tool dựng nhanh khung Canvas/GameManager/Arena preview/Roster panel trong scene đang mở.
// Idempotent: chạy lại nhiều lần không tạo trùng object đã có (tìm theo tên cố định bên dưới).
// Chỉ dựng khung UI trống (placeholder trắng) - thay Sprite/màu bằng asset UI có sẵn của bạn sau.
public static class GameUISetupTool
{
    const string CanvasName = "Canvas";
    const string EventSystemName = "EventSystem";
    const string GameManagerName = "GameManager";
    const string RosterPrefabPath = "Assets/Prefabs/UI/RosterEntry.prefab";

    // =========================================================
    // CAMERA SETUP (Simple Move approach)
    // =========================================================

    [MenuItem("Tools/Shop-Arena Setup/Setup Camera (Simple Move)")]
    public static void SetupCamera()
    {
        // 1. Tìm Main Camera và MainCameraStateController
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[CameraSetup] Không tìm thấy Main Camera trong scene.");
            return;
        }

        MainCameraStateController ctrl = mainCam.GetComponent<MainCameraStateController>();
        if (ctrl == null) ctrl = mainCam.gameObject.AddComponent<MainCameraStateController>();

        // 2. Reset Camera.rect về full screen (dọn giá trị cũ)
        mainCam.rect = new Rect(0, 0, 1, 1);

        // 3. Xoá ArenaViewportBounds + ArenaViewportAnchor (không còn cần thiết)
        CleanViewportHelpers();

        // 4. Tạo ArenaCamTarget
        Vector3 camPos = mainCam.transform.position;
        float currentOrtho = mainCam.orthographicSize;

        GameObject arenaTarget = EnsureEmptyGO("ArenaCamTarget", null);
        arenaTarget.transform.position = camPos; // đặt tại vị trí camera hiện tại
        Undo.RegisterCreatedObjectUndo(arenaTarget, "Create ArenaCamTarget");

        // 5. Tạo ShopCamTarget — dịch trái + xuống để arena nằm ở góc trên phải
        //    Offset ~ 30% width + 25% height của view hiện tại
        float aspect = mainCam.aspect > 0 ? mainCam.aspect : 16f / 9f;
        float offsetX = currentOrtho * aspect * 0.35f;
        float offsetY = currentOrtho * 0.28f;
        float shopOrtho = currentOrtho * 1.5f;

        GameObject shopTarget = EnsureEmptyGO("ShopCamTarget", null);
        shopTarget.transform.position = new Vector3(
            camPos.x - offsetX,
            camPos.y - offsetY,
            camPos.z);
        Undo.RegisterCreatedObjectUndo(shopTarget, "Create ShopCamTarget");

        // 6. Tìm ShopRoot trong Canvas
        GameObject shopRoot = null;
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform sr = canvas.transform.Find("ShopRoot");
            if (sr != null) shopRoot = sr.gameObject;
        }

        // 7. Wire up MainCameraStateController qua SerializedObject
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("mainCamera").objectReferenceValue = mainCam;
        so.FindProperty("shopRoot").objectReferenceValue = shopRoot;
        so.FindProperty("arenaCamTarget").objectReferenceValue = arenaTarget.transform;
        so.FindProperty("shopCamTarget").objectReferenceValue = shopTarget.transform;
        so.FindProperty("arenaOrthographicSize").floatValue = currentOrtho;
        so.FindProperty("shopOrthographicSize").floatValue = shopOrtho;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(mainCam.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mainCam.gameObject.scene);

        string shopRootInfo = shopRoot != null ? "ShopRoot ✓" : "ShopRoot KHÔNG TÌM THẤY — gán tay";
        Debug.Log($"[CameraSetup] Xong!\n" +
                  $"  ArenaCamTarget: {arenaTarget.transform.position}\n" +
                  $"  ShopCamTarget:  {shopTarget.transform.position}\n" +
                  $"  ArenaOrthoSize: {currentOrtho}\n" +
                  $"  ShopOrthoSize:  {shopOrtho}\n" +
                  $"  {shopRootInfo}\n\n" +
                  $"Tiếp theo: Play game, quan sát Gizmo (cyan = Shop view, đỏ = Arena view),\n" +
                  $"di chuyển ShopCamTarget trong Scene view đến khi arena nằm vừa góc trên phải.");

        Selection.activeGameObject = ctrl.gameObject;
    }

    static void CleanViewportHelpers()
    {
        // Xoá ArenaViewportBounds (và child ArenaViewportAnchor theo)
        GameObject bounds = GameObject.Find("ArenaViewportBounds");
        if (bounds != null)
        {
            Undo.DestroyObjectImmediate(bounds);
            Debug.Log("[CameraSetup] Đã xoá ArenaViewportBounds (không còn cần thiết).");
        }

        // Xoá riêng nếu tồn tại ngoài bounds
        GameObject anchor = GameObject.Find("ArenaViewportAnchor");
        if (anchor != null)
        {
            Undo.DestroyObjectImmediate(anchor);
            Debug.Log("[CameraSetup] Đã xoá ArenaViewportAnchor.");
        }
    }

    static GameObject EnsureEmptyGO(string name, Transform parent)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing;

        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    // =========================================================
    // ORIGINAL UI BUILDER
    // =========================================================

    [MenuItem("Tools/Shop-Arena Setup/Add FeedingManager to Scene")]
    public static void AddFeedingManager()
    {
        if (Object.FindFirstObjectByType<FeedingManager>() != null)
        {
            Debug.Log("[FeedingSetup] FeedingManager đã có trong scene.");
            Selection.activeGameObject = Object.FindFirstObjectByType<FeedingManager>().gameObject;
            return;
        }

        // Gắn vào GO "Managers" nếu có, không thì tạo mới
        GameObject managersGO = GameObject.Find("Managers");
        if (managersGO == null)
        {
            managersGO = new GameObject("Managers");
            Undo.RegisterCreatedObjectUndo(managersGO, "Create Managers");
        }

        Undo.AddComponent<FeedingManager>(managersGO);
        EditorUtility.SetDirty(managersGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(managersGO.scene);

        Selection.activeGameObject = managersGO;
        Debug.Log("[FeedingSetup] Đã thêm FeedingManager vào scene.\n" +
                  "Flow mới: EnterArena → Feed lần lượt từng ally (0.4s/lượt) → wave start.\n" +
                  "Chỉnh Feed Stagger và Post Feed Delay trong Inspector nếu cần.");
    }

    [MenuItem("Tools/Shop-Arena Setup/Build UI In Scene")]
    public static void BuildAll()
    {
        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();
        EnsureGameManager();
        EnsureRosterPanel(canvas);

        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log("[GameUISetupTool] Xong. Kiểm tra Canvas trong Hierarchy, gán sprite/màu asset UI của bạn vào các placeholder.");
    }

    // ---------- Canvas / EventSystem ----------

    static Canvas EnsureCanvas()
    {
        GameObject existing = GameObject.Find(CanvasName);
        if (existing != null && existing.GetComponent<Canvas>() != null)
            return existing.GetComponent<Canvas>();

        GameObject go = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject go = new GameObject(EventSystemName, typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");

#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    // ---------- GameManager ----------

    static void EnsureGameManager()
    {
        if (GameObject.Find(GameManagerName) != null) return;

        GameObject go = new GameObject(GameManagerName, typeof(GameManager));
        Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
    }

    // ---------- Roster panel (ScrollView dọc bên trái) ----------

    static void EnsureRosterPanel(Canvas canvas)
    {
        if (canvas.transform.Find("RosterPanel") != null) return;

        CharacterRosterEntry entryPrefab = EnsureRosterEntryPrefab();

        GameObject panel = new GameObject("RosterPanel", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create RosterPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 0.5f);
        panelRt.offsetMin = new Vector2(20f, 20f);
        panelRt.offsetMax = new Vector2(240f, -20f); // width 220 (240-20), margin top/bottom 20

        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.35f);

        // ScrollView
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(panel.transform, false);
        RectTransform scrollRt = scrollGO.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(8f, 8f);
        scrollRt.offsetMax = new Vector2(-8f, -8f);

        // Viewport
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform viewportRt = viewportGO.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        // Content
        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRt = contentGO.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.spacing = 8f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = false;
        vlg.childForceExpandWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.content = contentRt;
        scrollRect.viewport = viewportRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;

        CharacterRosterUI rosterUI = panel.AddComponent<CharacterRosterUI>();
        SetPrivateField(rosterUI, "entryPrefab", entryPrefab);
        SetPrivateField(rosterUI, "content", contentRt);
    }

    // ── Rebuild Roster Entry Prefab (force recreate) ─────────────────

    [MenuItem("Tools/Shop-Arena Setup/Rebuild Roster Entry Prefab")]
    public static void RebuildRosterEntryPrefab()
    {
        // Xoá prefab cũ nếu có để force rebuild
        if (AssetDatabase.LoadAssetAtPath<Object>(RosterPrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(RosterPrefabPath);
            AssetDatabase.Refresh();
        }

        CharacterRosterEntry prefab = BuildRosterEntryPrefab();
        if (prefab == null) return;

        // Cập nhật reference trong scene
        CharacterRosterUI rosterUI = Object.FindFirstObjectByType<CharacterRosterUI>();
        if (rosterUI != null)
        {
            SetPrivateField(rosterUI, "entryPrefab", prefab);
            EditorUtility.SetDirty(rosterUI);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rosterUI.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[GameUISetupTool] RosterEntry prefab đã được rebuild.\n" +
                  "Layout:\n" +
                  "  RosterEntry (VerticalLayout)\n" +
                  "    ├── TopRow (HorizontalLayout)\n" +
                  "    │   ├── PortraitFrame (Mask) → PortraitImage\n" +
                  "    │   └── AngryBarBG → AngryBarFill  (fillAmount=0 = trống)\n" +
                  "    └── HPBarBG → HPBarFill             (fillAmount=1 = đầy)\n\n" +
                  "Hỗ trợ kéo-thả để đổi thứ tự trong RosterPanel.");
        Selection.activeObject = prefab;
    }

    static CharacterRosterEntry EnsureRosterEntryPrefab()
    {
        CharacterRosterEntry existing = AssetDatabase.LoadAssetAtPath<CharacterRosterEntry>(RosterPrefabPath);
        if (existing != null) return existing;
        return BuildRosterEntryPrefab();
    }

    // Dựng prefab: TopRow (portrait + angry bar dọc) + HP bar ngang bên dưới.
    static CharacterRosterEntry BuildRosterEntryPrefab()
    {
        EnsureFolder(Path.GetDirectoryName(RosterPrefabPath).Replace('\\', '/'));

        const float cardH      = 90f;   // portrait lớn hơn
        const float hpBarH     = 14f;   // HP bar cao hơn
        const float rowSpacing = 3f;
        const float pad        = 4f;
        float totalH = pad + cardH + rowSpacing + hpBarH + pad;

        // ── Root: VerticalLayoutGroup ─────────────────────────────────
        GameObject root = new GameObject("RosterEntry",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, totalH);

        VerticalLayoutGroup rootVlg   = root.GetComponent<VerticalLayoutGroup>();
        rootVlg.padding               = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
        rootVlg.spacing               = rowSpacing;
        rootVlg.childAlignment        = TextAnchor.UpperCenter;
        rootVlg.childControlWidth     = true;
        rootVlg.childForceExpandWidth = true;
        rootVlg.childControlHeight    = false;
        rootVlg.childForceExpandHeight = false;

        LayoutElement rootLe   = root.GetComponent<LayoutElement>();
        rootLe.preferredWidth  = 120f;
        rootLe.minWidth        = 120f;
        rootLe.preferredHeight = totalH;
        rootLe.minHeight       = totalH;

        // ── TopRow: portrait + angry bar ─────────────────────────────
        GameObject topRow = new GameObject("TopRow",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        topRow.transform.SetParent(root.transform, false);
        topRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, cardH);

        HorizontalLayoutGroup hlg = topRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 6f;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        LayoutElement topRowLe   = topRow.GetComponent<LayoutElement>();
        topRowLe.preferredHeight = cardH;
        topRowLe.minHeight       = cardH;
        topRowLe.flexibleWidth   = 1f;

        // Portrait frame (Mask crop)
        GameObject frame = new GameObject("PortraitFrame",
            typeof(RectTransform), typeof(Image), typeof(Mask), typeof(LayoutElement));
        frame.transform.SetParent(topRow.transform, false);
        frame.GetComponent<RectTransform>().sizeDelta = new Vector2(cardH, cardH);
        frame.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
        frame.GetComponent<Mask>().showMaskGraphic = true;
        LayoutElement frameLe   = frame.GetComponent<LayoutElement>();
        frameLe.preferredWidth  = cardH;
        frameLe.preferredHeight = cardH;

        GameObject portrait = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
        portrait.transform.SetParent(frame.transform, false);
        RectTransform pRt = portrait.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one;
        pRt.offsetMin = pRt.offsetMax = Vector2.zero;
        Image portraitImg        = portrait.GetComponent<Image>();
        portraitImg.raycastTarget  = false;
        portraitImg.preserveAspect = false;

        // Angry bar (dọc, phải portrait)
        const float angryW = 14f;
        GameObject angryBg = new GameObject("AngryBarBG",
            typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        angryBg.transform.SetParent(topRow.transform, false);
        angryBg.GetComponent<RectTransform>().sizeDelta = new Vector2(angryW, cardH);
        angryBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        LayoutElement angryLe   = angryBg.GetComponent<LayoutElement>();
        angryLe.preferredWidth  = angryW;
        angryLe.preferredHeight = cardH;

        GameObject angryFill = new GameObject("AngryBarFill", typeof(RectTransform), typeof(Image));
        angryFill.transform.SetParent(angryBg.transform, false);
        RectTransform aFillRt = angryFill.GetComponent<RectTransform>();
        aFillRt.anchorMin = Vector2.zero; aFillRt.anchorMax = Vector2.one;
        aFillRt.offsetMin = aFillRt.offsetMax = Vector2.zero;
        Image angryFillImg         = angryFill.GetComponent<Image>();
        angryFillImg.color         = new Color(0.9f, 0.2f, 0.15f, 1f);
        angryFillImg.type          = Image.Type.Simple;  // solid color, no sprite needed
        angryFillImg.raycastTarget = false;

        // ── HP bar (ngang, bên dưới) ──────────────────────────────────
        GameObject hpBg = new GameObject("HPBarBG",
            typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        hpBg.transform.SetParent(root.transform, false);
        hpBg.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, hpBarH);
        hpBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        LayoutElement hpBgLe   = hpBg.GetComponent<LayoutElement>();
        hpBgLe.preferredHeight = hpBarH;
        hpBgLe.minHeight       = hpBarH;
        hpBgLe.flexibleWidth   = 1f;

        GameObject hpFill = new GameObject("HPBarFill", typeof(RectTransform), typeof(Image));
        hpFill.transform.SetParent(hpBg.transform, false);
        RectTransform hFillRt = hpFill.GetComponent<RectTransform>();
        hFillRt.anchorMin = Vector2.zero; hFillRt.anchorMax = Vector2.one;
        hFillRt.offsetMin = hFillRt.offsetMax = Vector2.zero;
        Image hpFillImg         = hpFill.GetComponent<Image>();
        hpFillImg.color         = new Color(0.15f, 0.85f, 0.3f, 1f);  // xanh lá
        hpFillImg.type          = Image.Type.Simple;  // solid color, no sprite needed
        hpFillImg.raycastTarget = false;

        // ── Wire component ────────────────────────────────────────────
        // angryFillRT và hpFillRT được tìm theo tên tại runtime — không cần wire ở đây
        CharacterRosterEntry entryComp = root.AddComponent<CharacterRosterEntry>();
        SetPrivateField(entryComp, "portraitImage", portraitImg);

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, RosterPrefabPath, out bool ok);
        Object.DestroyImmediate(root);

        if (!ok)
        {
            Debug.LogError("[GameUISetupTool] Lưu RosterEntry prefab thất bại.");
            return null;
        }

        return prefabAsset.GetComponent<CharacterRosterEntry>();
    }

    // =========================================================
    // WAVE COUNTDOWN SETUP
    // =========================================================

    [MenuItem("Tools/Shop-Arena Setup/Setup Wave Countdown")]
    public static void SetupWaveCountdown()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CountdownSetup] Không tìm thấy Canvas trong scene. Chạy 'Build UI In Scene' trước.");
            return;
        }

        // CountdownRoot phải là con trực tiếp của Canvas (full-screen overlay),
        // KHÔNG nằm trong ArenaRoot hay ShopRoot để tránh inherit sizing/offset.
        Transform parent = canvas.transform;

        // Tìm hoặc tạo CountdownRoot
        Transform existing = parent.Find("CountdownRoot");
        if (existing != null)
        {
            Debug.Log("[CountdownSetup] CountdownRoot đã tồn tại — chọn để xem cấu trúc.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // --- CountdownRoot: panel full-screen, căn giữa ---
        GameObject root = new GameObject("CountdownRoot", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Create CountdownRoot");
        root.transform.SetParent(parent, false);

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // --- DisplayImage: Image vuông chính giữa màn hình ---
        // Bạn sẽ gán sprite 3/2/1 vào numberSprites[] trong Inspector.
        // Kích thước mặc định 256x256, điều chỉnh tùy asset.
        GameObject imgGO = new GameObject("NumberImage", typeof(RectTransform), typeof(Image));
        imgGO.transform.SetParent(root.transform, false);

        RectTransform imgRt = imgGO.GetComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0.5f, 0.5f);
        imgRt.anchorMax = new Vector2(0.5f, 0.5f);
        imgRt.pivot = new Vector2(0.5f, 0.5f);
        imgRt.anchoredPosition = Vector2.zero;
        imgRt.sizeDelta = new Vector2(256f, 256f);

        Image img = imgGO.GetComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;

        // Wire WaveCountdown component
        WaveCountdown countdown = canvas.GetComponentInChildren<WaveCountdown>(true);
        if (countdown == null)
        {
            countdown = canvas.gameObject.AddComponent<WaveCountdown>();
            Debug.Log("[CountdownSetup] Đã thêm WaveCountdown vào Canvas.");
        }

        SetPrivateField(countdown, "countdownRoot", root);
        SetPrivateField(countdown, "displayImage", img);
        // numberSprites để trống — tự gán 3 sprite (số 3, 2, 1) trong Inspector

        root.SetActive(false); // ẩn mặc định

        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Selection.activeGameObject = countdown.gameObject;

        Debug.Log("[CountdownSetup] Xong!\n" +
                  "Cấu trúc:\n" +
                  "  Canvas/[ArenaRoot]/CountdownRoot\n" +
                  "    └── NumberImage (Image)\n\n" +
                  "Việc cần làm:\n" +
                  "  1. Chọn Canvas → WaveCountdown component\n" +
                  "  2. Gán 3 sprite vào Number Sprites: [0]=số 3, [1]=số 2, [2]=số 1\n" +
                  "  3. (Tuỳ chọn) Gán TextMeshProUGUI vào Countdown Text thay cho sprites\n" +
                  "  4. Điều chỉnh kích thước NumberImage (hiện 256x256) cho vừa assets");
    }

    [MenuItem("Tools/Shop-Arena Setup/Fix Countdown Layout")]
    public static void FixCountdownLayout()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("Không tìm thấy Canvas."); return; }

        Transform root = canvas.transform.Find("CountdownRoot");
        if (root == null) { Debug.LogError("Không tìm thấy CountdownRoot trong Canvas."); return; }

        // Di chuyển về đúng parent nếu chưa
        if (root.parent != canvas.transform)
        {
            Undo.SetTransformParent(root, canvas.transform, "Fix CountdownRoot parent");
        }

        // Reset CountdownRoot → stretch full canvas
        RectTransform rootRt = root.GetComponent<RectTransform>();
        Undo.RecordObject(rootRt, "Fix CountdownRoot RectTransform");
        rootRt.anchorMin    = Vector2.zero;
        rootRt.anchorMax    = Vector2.one;
        rootRt.pivot        = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta    = Vector2.zero;
        rootRt.offsetMin    = Vector2.zero;
        rootRt.offsetMax    = Vector2.zero;

        // Reset NumberImage → anchor giữa màn hình
        Transform imgTransform = root.Find("NumberImage");
        if (imgTransform != null)
        {
            RectTransform imgRt = imgTransform.GetComponent<RectTransform>();
            Undo.RecordObject(imgRt, "Fix NumberImage RectTransform");
            imgRt.anchorMin         = new Vector2(0.5f, 0.5f);
            imgRt.anchorMax         = new Vector2(0.5f, 0.5f);
            imgRt.pivot             = new Vector2(0.5f, 0.5f);
            imgRt.anchoredPosition  = Vector2.zero;
            imgRt.sizeDelta         = new Vector2(256f, 256f);
        }

        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[CountdownFix] Xong — CountdownRoot và NumberImage đã được reset về đúng vị trí.");
    }

    // =========================================================
    // START WAVE BUTTON
    // =========================================================

    [MenuItem("Tools/Shop-Arena Setup/Setup Start Wave Button")]
    public static void SetupStartWaveButton()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[StartWaveSetup] Không tìm thấy Canvas. Chạy 'Build UI In Scene' trước.");
            return;
        }

        // Tìm ShopRoot — nút phải nằm trong ShopRoot để tự ẩn khi vào Arena
        Transform shopRoot = canvas.transform.Find("ShopRoot");
        if (shopRoot == null)
        {
            // Tạo ShopRoot nếu chưa có
            GameObject sr = new GameObject("ShopRoot", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(sr, "Create ShopRoot");
            sr.transform.SetParent(canvas.transform, false);
            RectTransform srRt = sr.GetComponent<RectTransform>();
            srRt.anchorMin = Vector2.zero;
            srRt.anchorMax = Vector2.one;
            srRt.offsetMin = srRt.offsetMax = Vector2.zero;
            shopRoot = sr.transform;
            Debug.Log("[StartWaveSetup] Đã tạo ShopRoot.");
        }

        // Idempotent — không tạo trùng
        if (shopRoot.Find("StartWaveButton") != null)
        {
            Debug.Log("[StartWaveSetup] StartWaveButton đã tồn tại trong ShopRoot.");
            Selection.activeGameObject = shopRoot.Find("StartWaveButton").gameObject;
            return;
        }

        // --- Button GO ---
        GameObject btnGO = new GameObject("StartWaveButton", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(btnGO, "Create StartWaveButton");
        btnGO.transform.SetParent(shopRoot, false);

        // Vị trí: góc dưới phải ShopRoot, dễ bấm
        RectTransform btnRt = btnGO.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0f);
        btnRt.anchorMax = new Vector2(1f, 0f);
        btnRt.pivot     = new Vector2(1f, 0f);
        btnRt.anchoredPosition = new Vector2(-40f, 40f);
        btnRt.sizeDelta = new Vector2(280f, 80f);

        Image btnImg = btnGO.GetComponent<Image>();
        btnImg.color = new Color(0.18f, 0.65f, 0.25f, 1f); // xanh lá placeholder

        Button btn = btnGO.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.25f, 0.8f, 0.35f);
        cb.pressedColor     = new Color(0.12f, 0.45f, 0.18f);
        btn.colors = cb;

        // StartWaveButton script — tự wire onClick trong Awake
        btnGO.AddComponent<StartWaveButton>();

        // --- Label (Text) ---
        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(btnGO.transform, false);

        RectTransform labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

        // Thử thêm TextMeshProUGUI nếu TMP có trong project
        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null)
        {
            var tmp = labelGO.AddComponent(tmpType);
            tmpType.GetProperty("text")?.SetValue(tmp, "Bắt đầu Wave");
            tmpType.GetProperty("fontSize")?.SetValue(tmp, 32f);
            tmpType.GetProperty("alignment")?.SetValue(tmp, System.Enum.Parse(
                System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro"), "Center"));
            tmpType.GetProperty("color")?.SetValue(tmp, Color.white);
        }
        else
        {
            // Fallback: UI.Text
            Text txt = labelGO.AddComponent<Text>();
            txt.text      = "Bắt đầu Wave";
            txt.fontSize  = 28;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color     = Color.white;
        }

        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = btnGO;

        Debug.Log("[StartWaveSetup] Xong!\n" +
                  "Cấu trúc:\n" +
                  "  Canvas/ShopRoot/StartWaveButton\n" +
                  "    └── Label (TextMeshProUGUI / Text)\n\n" +
                  "Việc cần làm:\n" +
                  "  1. Thay Image màu xanh bằng Sprite nút của bạn (Source Image)\n" +
                  "  2. Di chuyển vị trí nút nếu cần (hiện: góc dưới phải ShopRoot)\n" +
                  "  3. Đảm bảo ShopArenaCanvasController đang quản lý ShopRoot visibility");
    }

    // =========================================================
    // ITEM CARD SLOT SETUP
    // =========================================================

    [MenuItem("Tools/Shop-Arena Setup/Setup Item Card Slots")]
    public static void SetupItemCardSlots()
    {
        int fixed_ = 0;

        // ── ShopOfferSlotUI ───────────────────────────────────────────────────
        foreach (var slot in Object.FindObjectsByType<ShopOfferSlotUI>(FindObjectsSortMode.None))
            if (EnsureCardBg(slot.gameObject, slot, "cardBg")) fixed_++;

        // ── ShopInventorySlotUI ───────────────────────────────────────────────
        foreach (var slot in Object.FindObjectsByType<ShopInventorySlotUI>(FindObjectsSortMode.None))
            if (EnsureCardBg(slot.gameObject, slot, "cardBg")) fixed_++;

        // Lưu scene
        if (fixed_ > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        Debug.Log($"[CardSlotSetup] Xong — đã xử lý {fixed_} slot.\n\n" +
                  "Việc còn lại (gán tay trong Inspector):\n" +
                  "  Chọn từng slot → kéo sprite card vào:\n" +
                  "    Card Active    → sprite item 1-lần\n" +
                  "    Card Stat Boost→ sprite item chỉ số\n" +
                  "    Card Character → sprite nhân vật\n" +
                  "    Card Empty     → để trống hoặc sprite ô rỗng\n\n" +
                  "CardBg đã được đặt trước Icon trong hierarchy (render phía dưới).");
    }

    // Tạo CardBg Image con, đặt ở sibling 0 (dưới cùng), wire vào field 'fieldName'.
    // Sau đó tìm field 'icon' và đảm bảo nó ở sibling cao hơn CardBg.
    // Trả về true nếu có thay đổi.
    static bool EnsureCardBg(GameObject slotGO, Component target, string fieldName)
    {
        const string cardBgName = "CardBg";
        bool dirty = false;

        // ── 1. Tạo hoặc tìm CardBg ───────────────────────────────────────────
        Transform existing = slotGO.transform.Find(cardBgName);
        GameObject cardBgGO;
        if (existing != null)
        {
            cardBgGO = existing.gameObject;
        }
        else
        {
            cardBgGO = new GameObject(cardBgName, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(cardBgGO, "Create CardBg");
            cardBgGO.transform.SetParent(slotGO.transform, false);
            dirty = true;
        }

        // Đặt CardBg ở index 0 — render SAU CÙNG (dưới cùng)
        cardBgGO.transform.SetSiblingIndex(0);

        // Stretch full parent
        RectTransform rt = cardBgGO.GetComponent<RectTransform>();
        Undo.RecordObject(rt, "Setup CardBg RectTransform");
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
        rt.localScale = Vector3.one;

        // Image: raycastTarget tắt để không block click trên slot
        Image img = cardBgGO.GetComponent<Image>();
        Undo.RecordObject(img, "Setup CardBg Image");
        img.raycastTarget  = false;
        img.preserveAspect = false;
        img.color          = Color.white;

        // ── 2. Wire field cardBg ─────────────────────────────────────────────
        SerializedObject   so   = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null && prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = img;
            so.ApplyModifiedProperties();
            dirty = true;
        }

        // ── 3. Đảm bảo Icon child nằm SAU CardBg (sibling index cao hơn) ────
        //       Nếu Icon là Image trên chính slotGO (không phải child) thì bỏ qua.
        SerializedProperty iconProp = so.FindProperty("icon");
        if (iconProp != null && iconProp.objectReferenceValue is Image iconImg)
        {
            if (iconImg.gameObject != slotGO               // icon không phải root GO
                && iconImg.transform.parent == slotGO.transform) // icon là direct child
            {
                int cardIdx = cardBgGO.transform.GetSiblingIndex();
                int iconIdx = iconImg.transform.GetSiblingIndex();
                if (iconIdx <= cardIdx)
                {
                    Undo.RecordObject(iconImg.transform, "Reorder Icon above CardBg");
                    iconImg.transform.SetSiblingIndex(cardIdx + 1);
                    dirty = true;
                }
            }
            else if (iconImg.gameObject == slotGO)
            {
                // Icon là Image trên root GO → children luôn đè lên parent
                // Cần tạo Icon child riêng và update field
                EnsureIconChild(slotGO, target, so, iconImg);
                dirty = true;
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(slotGO);
        return dirty;
    }

    // Tạo child "Icon" Image, copy settings từ rootImage, update field 'icon' trên component.
    static void EnsureIconChild(GameObject slotGO, Component target, SerializedObject so, Image rootImage)
    {
        Transform existingIcon = slotGO.transform.Find("Icon");
        GameObject iconGO;

        if (existingIcon != null)
        {
            iconGO = existingIcon.gameObject;
        }
        else
        {
            iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconGO, "Create Icon child");
            iconGO.transform.SetParent(slotGO.transform, false);
        }

        // Đặt Icon SAU CardBg
        iconGO.transform.SetAsLastSibling();

        // Copy sprite từ root image sang
        Image iconImg          = iconGO.GetComponent<Image>();
        iconImg.sprite         = rootImage.sprite;
        iconImg.color          = rootImage.color;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;

        // Stretch full parent
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.anchorMin  = Vector2.zero;
        iconRt.anchorMax  = Vector2.one;
        iconRt.offsetMin  = new Vector2(8f, 8f);   // padding nhỏ để icon nhỏ hơn card
        iconRt.offsetMax  = new Vector2(-8f, -8f);
        iconRt.localScale = Vector3.one;

        // Xoá Image trên root (tránh chồng lên)
        Undo.RecordObject(rootImage, "Clear root Image sprite");
        rootImage.sprite  = null;
        rootImage.color   = new Color(0, 0, 0, 0); // transparent

        // Update field 'icon' trỏ vào child mới
        SerializedProperty iconProp = so.FindProperty("icon");
        if (iconProp != null)
            iconProp.objectReferenceValue = iconImg;

        Debug.Log($"[CardSlotSetup] {slotGO.name}: Icon Image đã được chuyển sang child 'Icon' " +
                  $"(root Image được transparent). Kiểm tra lại sprite assignment nếu cần.");
    }

    // =========================================================
    // PRICE BADGE SETUP
    // =========================================================

    [MenuItem("Tools/Shop-Arena Setup/Setup Price Badges")]
    public static void SetupPriceBadges()
    {
        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType == null)
        {
            Debug.LogError("[PriceBadge] Không tìm thấy TextMeshProUGUI. Import TMP trước.");
            return;
        }

        int count = 0;

        // ── ShopOfferSlotUI (buy price) ───────────────────────────────────────
        foreach (var slot in Object.FindObjectsByType<ShopOfferSlotUI>(FindObjectsSortMode.None))
            if (EnsurePriceBadge(slot.gameObject, slot, tmpType, "priceBadge", "priceText",
                                 anchorCorner: new Vector2(0f, 0f),   // góc dưới trái
                                 pivotCorner:  new Vector2(0f, 0f),
                                 offset:       new Vector2(4f, 4f)))
                count++;

        // ── ShopInventorySlotUI (sell price) ─────────────────────────────────
        foreach (var slot in Object.FindObjectsByType<ShopInventorySlotUI>(FindObjectsSortMode.None))
            if (EnsurePriceBadge(slot.gameObject, slot, tmpType, "priceBadge", "priceText",
                                 anchorCorner: new Vector2(0f, 0f),
                                 pivotCorner:  new Vector2(0f, 0f),
                                 offset:       new Vector2(4f, 4f)))
                count++;

        if (count > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[PriceBadge] Xong — đã xử lý {count} slot.\n\n" +
                  "Việc cần làm:\n" +
                  "  1. Chọn từng slot → PriceBadge → Image → kéo sprite nền giá của artist vào Source Image\n" +
                  "  2. Chỉnh kích thước/vị trí PriceBadge RectTransform cho vừa thiết kế\n" +
                  "  3. (Tuỳ chọn) Đổi font/size trên PriceText");
    }

    // Tạo PriceBadge GO với Image nền + TMP_Text con.
    // Trả true nếu có thay đổi (tạo mới hoặc wire lại field).
    static bool EnsurePriceBadge(GameObject slotGO, Component target, System.Type tmpType,
                                  string badgeFieldName, string textFieldName,
                                  Vector2 anchorCorner, Vector2 pivotCorner, Vector2 offset)
    {
        const string badgeName = "PriceBadge";
        const string textName  = "PriceText";
        bool dirty = false;

        // ── 1. Tạo hoặc tìm PriceBadge ───────────────────────────────────────
        Transform existingBadge = slotGO.transform.Find(badgeName);
        GameObject badgeGO;
        if (existingBadge != null)
        {
            badgeGO = existingBadge.gameObject;
        }
        else
        {
            badgeGO = new GameObject(badgeName, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(badgeGO, "Create PriceBadge");
            badgeGO.transform.SetParent(slotGO.transform, false);
            badgeGO.transform.SetAsLastSibling(); // đè lên CardBg và Icon
            dirty = true;
        }

        // Anchor ở góc chỉ định, kích thước cố định
        RectTransform badgeRt = badgeGO.GetComponent<RectTransform>();
        Undo.RecordObject(badgeRt, "Setup PriceBadge RectTransform");
        badgeRt.anchorMin        = anchorCorner;
        badgeRt.anchorMax        = anchorCorner;
        badgeRt.pivot            = pivotCorner;
        badgeRt.anchoredPosition = offset;
        badgeRt.sizeDelta        = new Vector2(56f, 26f); // width đủ "999", height label

        Image badgeImg = badgeGO.GetComponent<Image>();
        Undo.RecordObject(badgeImg, "Setup PriceBadge Image");
        badgeImg.raycastTarget = false;
        badgeImg.color         = Color.white; // artist sẽ kéo sprite vào đây

        // ── 2. Tạo hoặc tìm PriceText ────────────────────────────────────────
        Transform existingText = badgeGO.transform.Find(textName);
        GameObject textGO;
        if (existingText != null)
        {
            textGO = existingText.gameObject;
        }
        else
        {
            textGO = new GameObject(textName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGO, "Create PriceText");
            textGO.transform.SetParent(badgeGO.transform, false);
            dirty = true;
        }

        RectTransform textRt = textGO.GetComponent<RectTransform>();
        Undo.RecordObject(textRt, "Setup PriceText RectTransform");
        textRt.anchorMin  = Vector2.zero;
        textRt.anchorMax  = Vector2.one;
        textRt.offsetMin  = new Vector2(2f, 1f);
        textRt.offsetMax  = new Vector2(-2f, -1f);

        // Thêm TMP nếu chưa có
        var existingTmp = textGO.GetComponent(tmpType);
        if (existingTmp == null)
        {
            existingTmp = textGO.AddComponent(tmpType);
            dirty = true;
        }
        tmpType.GetProperty("text")?.SetValue(existingTmp, "0");
        tmpType.GetProperty("fontSize")?.SetValue(existingTmp, 18f);
        tmpType.GetProperty("color")?.SetValue(existingTmp, Color.white);
        var alignType = System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
        if (alignType != null)
            tmpType.GetProperty("alignment")?.SetValue(existingTmp,
                System.Enum.Parse(alignType, "Center"));
        tmpType.GetProperty("raycastTarget")?.SetValue(existingTmp, false);

        // ── 3. Wire fields vào component ─────────────────────────────────────
        SerializedObject so = new SerializedObject(target);

        var badgeProp = so.FindProperty(badgeFieldName);
        if (badgeProp != null && badgeProp.objectReferenceValue == null)
        {
            badgeProp.objectReferenceValue = badgeGO;
            dirty = true;
        }

        var textProp = so.FindProperty(textFieldName);
        if (textProp != null && textProp.objectReferenceValue == null)
        {
            textProp.objectReferenceValue = existingTmp as Object;
            dirty = true;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(slotGO);
        return dirty;
    }

    // =========================================================
    // CORN DISPLAY PANEL
    // =========================================================

    // Dựng CornPanel ở trên cùng ShopRoot + DeltaAnchor bên ngoài panel (bên phải).
    // CornPanel: Image nền + CornIcon + CornText (CornDisplay) + CornDeltaPopup.
    // DeltaAnchor: RectTransform child của Canvas root — nơi popup delta xuất hiện.
    [MenuItem("Tools/Shop-Arena Setup/Setup Corn Display Panel")]
    public static void SetupCornDisplayPanel()
    {
        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType == null)
        {
            Debug.LogError("[CornPanel] Không tìm thấy TextMeshProUGUI. Import TMP trước.");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CornPanel] Không tìm thấy Canvas. Chạy 'Build UI In Scene' trước.");
            return;
        }

        // Tìm ShopRoot
        Transform shopRoot = canvas.transform.Find("ShopRoot");
        if (shopRoot == null)
        {
            Debug.LogError("[CornPanel] Không tìm thấy ShopRoot trong Canvas.");
            return;
        }

        // ── 1. CornPanel ─────────────────────────────────────────────────────
        const string panelName = "CornPanel";
        GameObject panelGO = shopRoot.Find(panelName)?.gameObject;
        if (panelGO == null)
        {
            panelGO = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panelGO, "Create CornPanel");
            // Đặt thẳng vào Canvas root để luôn hiện cả Shop lẫn Arena
            panelGO.transform.SetParent(canvas.transform, false);
        }

        // Anchor: top-left của ShopRoot, chiều ngang stretch, cao cố định
        RectTransform panelRt = panelGO.GetComponent<RectTransform>();
        Undo.RecordObject(panelRt, "Setup CornPanel RectTransform");
        panelRt.anchorMin        = new Vector2(0f, 1f);
        panelRt.anchorMax        = new Vector2(1f, 1f);
        panelRt.pivot            = new Vector2(0.5f, 1f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta        = new Vector2(0f, 54f); // chiều cao panel

        Image panelImg = panelGO.GetComponent<Image>();
        Undo.RecordObject(panelImg, "Setup CornPanel Image");
        panelImg.color        = new Color(0.1f, 0.08f, 0.04f, 0.85f); // nền tối, tự thay sprite
        panelImg.raycastTarget = false;

        // ── 2. CornIcon ──────────────────────────────────────────────────────
        const string iconName = "CornIcon";
        GameObject iconGO = panelGO.transform.Find(iconName)?.gameObject;
        if (iconGO == null)
        {
            iconGO = new GameObject(iconName, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconGO, "Create CornIcon");
            iconGO.transform.SetParent(panelGO.transform, false);
        }

        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        Undo.RecordObject(iconRt, "Setup CornIcon RectTransform");
        iconRt.anchorMin        = new Vector2(0f, 0.5f);
        iconRt.anchorMax        = new Vector2(0f, 0.5f);
        iconRt.pivot            = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(8f, 0f);
        iconRt.sizeDelta        = new Vector2(36f, 36f);

        Image iconImg = iconGO.GetComponent<Image>();
        Undo.RecordObject(iconImg, "Setup CornIcon Image");
        iconImg.color         = Color.white; // kéo sprite corn vào đây
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;

        // ── 3. CornText (TMP + CornDisplay) ──────────────────────────────────
        const string textName = "CornText";
        GameObject textGO = panelGO.transform.Find(textName)?.gameObject;
        if (textGO == null)
        {
            textGO = new GameObject(textName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGO, "Create CornText");
            textGO.transform.SetParent(panelGO.transform, false);
        }

        RectTransform textRt = textGO.GetComponent<RectTransform>();
        Undo.RecordObject(textRt, "Setup CornText RectTransform");
        textRt.anchorMin        = new Vector2(0f, 0f);
        textRt.anchorMax        = new Vector2(1f, 1f);
        textRt.offsetMin        = new Vector2(52f, 4f);  // bên phải icon
        textRt.offsetMax        = new Vector2(-8f, -4f);

        // TMP component
        var existingTmp = textGO.GetComponent(tmpType);
        if (existingTmp == null)
            existingTmp = Undo.AddComponent(textGO, tmpType) as Component;

        tmpType.GetProperty("text")?.SetValue(existingTmp, "0");
        tmpType.GetProperty("fontSize")?.SetValue(existingTmp, 28f);
        tmpType.GetProperty("fontStyle")?.SetValue(existingTmp,
            System.Enum.Parse(System.Type.GetType("TMPro.FontStyles, Unity.TextMeshPro"), "Bold"));
        tmpType.GetProperty("color")?.SetValue(existingTmp, new Color(1f, 0.92f, 0.3f)); // vàng corn
        var alignType = System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
        if (alignType != null)
            tmpType.GetProperty("alignment")?.SetValue(existingTmp,
                System.Enum.Parse(alignType, "MidlineLeft"));
        tmpType.GetProperty("raycastTarget")?.SetValue(existingTmp, false);

        // CornDisplay component (đã có sẵn trong project)
        CornDisplay cornDisplay = textGO.GetComponent<CornDisplay>();
        if (cornDisplay == null)
            cornDisplay = Undo.AddComponent<CornDisplay>(textGO);

        // Set prefix = "" vì icon đã hiển thị riêng
        SerializedObject cdSO = new SerializedObject(cornDisplay);
        var prefixProp = cdSO.FindProperty("prefix");
        if (prefixProp != null) prefixProp.stringValue = "";
        cdSO.ApplyModifiedProperties();

        // ── 4. CornDeltaPopup component trên panel ────────────────────────────
        CornDeltaPopup deltaComp = panelGO.GetComponent<CornDeltaPopup>();
        if (deltaComp == null)
            deltaComp = Undo.AddComponent<CornDeltaPopup>(panelGO);

        // ── 5. DeltaAnchor — child của Canvas root, bên ngoài panel ──────────
        // Vị trí: bên phải ShopOfferPanel (nếu tìm được), không thì dùng offset từ CornPanel
        const string anchorName = "CornDeltaAnchor";
        GameObject anchorGO = canvas.transform.Find(anchorName)?.gameObject;
        if (anchorGO == null)
        {
            anchorGO = new GameObject(anchorName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(anchorGO, "Create CornDeltaAnchor");
            anchorGO.transform.SetParent(canvas.transform, false); // child của Canvas, không clip
        }

        // Tìm ShopOfferPanel để đặt anchor bên phải nó
        RectTransform offerPanelRt = null;
        Transform offerPanel = shopRoot.Find("ShopOfferPanel");
        if (offerPanel != null) offerPanelRt = offerPanel.GetComponent<RectTransform>();

        RectTransform anchorRt = anchorGO.GetComponent<RectTransform>();
        Undo.RecordObject(anchorRt, "Setup CornDeltaAnchor RectTransform");
        if (offerPanelRt != null)
        {
            // Đặt anchor bên phải ShopOfferPanel, cùng độ cao CornPanel
            anchorRt.anchorMin        = offerPanelRt.anchorMin;
            anchorRt.anchorMax        = offerPanelRt.anchorMax;
            anchorRt.pivot            = new Vector2(0f, 1f);
            anchorRt.anchoredPosition = new Vector2(
                offerPanelRt.anchoredPosition.x + offerPanelRt.sizeDelta.x * 0.5f + 16f,
                offerPanelRt.anchoredPosition.y);
            anchorRt.sizeDelta = Vector2.zero;
        }
        else
        {
            // Fallback: bên phải ShopRoot, cùng độ cao top
            anchorRt.anchorMin        = new Vector2(0.15f, 1f);
            anchorRt.anchorMax        = new Vector2(0.15f, 1f);
            anchorRt.pivot            = new Vector2(0f, 1f);
            anchorRt.anchoredPosition = new Vector2(0f, -8f);
            anchorRt.sizeDelta        = Vector2.zero;
        }

        // Wire deltaAnchor vào CornDeltaPopup
        SerializedObject deltaSO = new SerializedObject(deltaComp);
        deltaSO.FindProperty("deltaAnchor").objectReferenceValue = anchorRt;
        deltaSO.ApplyModifiedProperties();

        // Lưu scene
        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = panelGO;

        Debug.Log("[CornPanel] Xong!\n\n" +
                  "Cấu trúc:\n" +
                  "  Canvas/CornPanel  (luôn hiện, tự kéo đến vị trí muốn)\n" +
                  "  ├── CornIcon      (Image — kéo sprite corn)\n" +
                  "  └── CornText      (TMP + CornDisplay)\n" +
                  "  Canvas/CornDeltaAnchor — kéo đến vị trí muốn popup xuất hiện\n\n" +
                  "Việc cần làm:\n" +
                  "  1. CornPanel → Image → kéo sprite nền panel\n" +
                  "  2. CornIcon  → Image → kéo sprite corn icon\n" +
                  "  3. CornPanel → CornDeltaPopup → Corn Icon → kéo cùng sprite corn\n" +
                  "  4. Di chuyển CornDeltaAnchor trong Scene view cho đúng vị trí bên ngoài panel");
    }

    // =========================================================
    // REROLL BUTTON SETUP
    // =========================================================

    // Thêm nhãn "ROLL" và badge giá vào nút Reroll trong ShopOfferPanel.
    // Chọn GO reroll button trong Hierarchy trước khi chạy, hoặc tool tự tìm theo tên.
    [MenuItem("Tools/Shop-Arena Setup/Setup Reroll Button")]
    public static void SetupRerollButton()
    {
        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType == null)
        {
            Debug.LogError("[RerollSetup] Không tìm thấy TextMeshProUGUI. Import TMP trước.");
            return;
        }

        // ── Tìm nút reroll ───────────────────────────────────────────────────
        // Ưu tiên GO đang chọn, nếu không tìm theo tên phổ biến
        GameObject btnGO = Selection.activeGameObject;
        if (btnGO == null || btnGO.GetComponent<UnityEngine.UI.Button>() == null)
        {
            // Thử tìm theo tên
            string[] candidateNames = { "ScrollButton", "RerollButton", "Reroll", "BtnReroll" };
            foreach (var name in candidateNames)
            {
                GameObject found = GameObject.Find(name);
                if (found != null && found.GetComponent<UnityEngine.UI.Button>() != null)
                {
                    btnGO = found;
                    break;
                }
            }
        }

        if (btnGO == null || btnGO.GetComponent<UnityEngine.UI.Button>() == null)
        {
            Debug.LogError("[RerollSetup] Không tìm thấy nút Reroll. " +
                           "Chọn GameObject Button chứa nút reroll trong Hierarchy rồi chạy lại.");
            return;
        }

        bool dirty = false;

        // ── 1. ROLL Label ────────────────────────────────────────────────────
        const string labelName = "RollLabel";
        GameObject labelGO = btnGO.transform.Find(labelName)?.gameObject;
        if (labelGO == null)
        {
            labelGO = new GameObject(labelName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(labelGO, "Create RollLabel");
            labelGO.transform.SetParent(btnGO.transform, false);
            dirty = true;
        }

        // Stretch full button, text căn giữa
        RectTransform labelRt = labelGO.GetComponent<RectTransform>();
        Undo.RecordObject(labelRt, "Setup RollLabel RectTransform");
        labelRt.anchorMin         = Vector2.zero;
        labelRt.anchorMax         = Vector2.one;
        labelRt.offsetMin         = new Vector2(0f, 16f);  // để trống phần dưới cho badge giá
        labelRt.offsetMax         = Vector2.zero;

        var existingTmp = labelGO.GetComponent(tmpType);
        if (existingTmp == null)
        {
            existingTmp = Undo.AddComponent(labelGO, tmpType) as Component;
            dirty = true;
        }
        tmpType.GetProperty("text")?.SetValue(existingTmp, "ROLL");
        tmpType.GetProperty("fontSize")?.SetValue(existingTmp, 28f);
        tmpType.GetProperty("color")?.SetValue(existingTmp, Color.white);
        var alignType = System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
        if (alignType != null)
            tmpType.GetProperty("alignment")?.SetValue(existingTmp,
                System.Enum.Parse(alignType, "Center"));
        tmpType.GetProperty("raycastTarget")?.SetValue(existingTmp, false);

        // ── 2. Price Badge (góc dưới trái, giống ShopOfferSlotUI) ────────────
        const string badgeName = "PriceBadge";
        const string textName  = "PriceText";

        GameObject badgeGO = btnGO.transform.Find(badgeName)?.gameObject;
        if (badgeGO == null)
        {
            badgeGO = new GameObject(badgeName, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            Undo.RegisterCreatedObjectUndo(badgeGO, "Create RerollPriceBadge");
            badgeGO.transform.SetParent(btnGO.transform, false);
            badgeGO.transform.SetAsLastSibling();
            dirty = true;
        }

        RectTransform badgeRt = badgeGO.GetComponent<RectTransform>();
        Undo.RecordObject(badgeRt, "Setup RerollPriceBadge RectTransform");
        badgeRt.anchorMin        = new Vector2(0f, 0f);
        badgeRt.anchorMax        = new Vector2(0f, 0f);
        badgeRt.pivot            = new Vector2(0f, 0f);
        badgeRt.anchoredPosition = new Vector2(4f, 4f);
        badgeRt.sizeDelta        = new Vector2(56f, 26f);

        var badgeImg = badgeGO.GetComponent<UnityEngine.UI.Image>();
        Undo.RecordObject(badgeImg, "Setup RerollPriceBadge Image");
        badgeImg.raycastTarget = false;
        badgeImg.color         = Color.white; // kéo sprite nền vào đây

        // PriceText
        GameObject textGO = badgeGO.transform.Find(textName)?.gameObject;
        if (textGO == null)
        {
            textGO = new GameObject(textName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textGO, "Create RerollPriceText");
            textGO.transform.SetParent(badgeGO.transform, false);
            dirty = true;
        }

        RectTransform textRt = textGO.GetComponent<RectTransform>();
        Undo.RecordObject(textRt, "Setup RerollPriceText RectTransform");
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(2f, 1f);
        textRt.offsetMax = new Vector2(-2f, -1f);

        var textTmp = textGO.GetComponent(tmpType);
        if (textTmp == null)
        {
            textTmp = Undo.AddComponent(textGO, tmpType) as Component;
            dirty = true;
        }
        tmpType.GetProperty("text")?.SetValue(textTmp, "4");
        tmpType.GetProperty("fontSize")?.SetValue(textTmp, 18f);
        tmpType.GetProperty("color")?.SetValue(textTmp, Color.white);
        if (alignType != null)
            tmpType.GetProperty("alignment")?.SetValue(textTmp,
                System.Enum.Parse(alignType, "Center"));
        tmpType.GetProperty("raycastTarget")?.SetValue(textTmp, false);

        // ── 3. RerollButtonUI component + wire fields ─────────────────────────
        RerollButtonUI ui = btnGO.GetComponent<RerollButtonUI>();
        if (ui == null)
        {
            ui = Undo.AddComponent<RerollButtonUI>(btnGO);
            dirty = true;
        }

        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("rollLabel").objectReferenceValue  = existingTmp as Object;
        so.FindProperty("priceBadge").objectReferenceValue = badgeGO;
        so.FindProperty("priceText").objectReferenceValue  = textTmp as Object;
        so.ApplyModifiedProperties();

        // ── Lưu scene ─────────────────────────────────────────────────────────
        EditorUtility.SetDirty(btnGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(btnGO.scene);
        Selection.activeGameObject = btnGO;

        Debug.Log($"[RerollSetup] Xong trên '{btnGO.name}'!\n\n" +
                  "Cấu trúc:\n" +
                  $"  {btnGO.name} (RerollButtonUI)\n" +
                  "  ├── RollLabel  (TMP — text 'ROLL', căn giữa)\n" +
                  "  └── PriceBadge (Image — kéo sprite nền vào Source Image)\n" +
                  "       └── PriceText (TMP — tự cập nhật theo rerollCost)\n\n" +
                  "Việc cần làm:\n" +
                  "  1. Chọn PriceBadge → Source Image → kéo sprite nền badge coin\n" +
                  "  2. Chỉnh font/size RollLabel nếu cần\n" +
                  "  3. Tự thay nền button theo ý bạn");
    }

    // =========================================================
    // STATIC ITEM ENTRY PREFAB
    // =========================================================

    const string StaticEntryPrefabPath = "Assets/Prefabs/UI/StaticItemEntry.prefab";

    [MenuItem("Tools/Shop-Arena Setup/Create Static Item Entry Prefab")]
    public static void CreateStaticItemEntryPrefab()
    {
        EnsureFolder("Assets/Prefabs/UI");

        // Xoá cũ nếu muốn rebuild
        if (AssetDatabase.LoadAssetAtPath<Object>(StaticEntryPrefabPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Tạo Static Item Entry Prefab",
                "Prefab đã tồn tại. Ghi đè?", "Ghi đè", "Huỷ"))
                return;
            AssetDatabase.DeleteAsset(StaticEntryPrefabPath);
        }

        const float size = 80f;

        // ── Root ──────────────────────────────────────────────────────────────
        GameObject root = new GameObject("StaticItemEntry", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(size, size);

        root.AddComponent<StaticItemEntry>();

        var trigger = root.AddComponent<ItemTooltipTrigger>();
        // Direction=Top, AlignEnd=false (hiện phía trên, canh trái)
        // — sẽ được gán qua SerializedObject bên dưới

        // ── CardBg ────────────────────────────────────────────────────────────
        GameObject cardBgGO = new GameObject("CardBg", typeof(RectTransform), typeof(Image));
        cardBgGO.transform.SetParent(root.transform, false);
        SetStretch(cardBgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image cardBgImg       = cardBgGO.GetComponent<Image>();
        cardBgImg.raycastTarget = false;
        cardBgImg.color         = Color.white;

        // ── Icon ──────────────────────────────────────────────────────────────
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(root.transform, false);
        SetStretch(iconGO.GetComponent<RectTransform>(),
            Vector2.zero, Vector2.one,
            new Vector2(8f, 8f), new Vector2(-8f, -8f));
        Image iconImg          = iconGO.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = true; // bubble event lên root's ItemTooltipTrigger

        // ── CountText ─────────────────────────────────────────────────────────
        // Dùng reflection để thêm TMP_Text component
        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null)
        {
            GameObject countGO = new GameObject("CountText", typeof(RectTransform));
            countGO.transform.SetParent(root.transform, false);

            // Anchor góc dưới phải
            RectTransform countRt = countGO.GetComponent<RectTransform>();
            countRt.anchorMin       = new Vector2(1f, 0f);
            countRt.anchorMax       = new Vector2(1f, 0f);
            countRt.pivot           = new Vector2(1f, 0f);
            countRt.anchoredPosition = new Vector2(-2f, 2f);
            countRt.sizeDelta       = new Vector2(48f, 24f);

            var tmp = countGO.AddComponent(tmpType);
            tmpType.GetProperty("text")?.SetValue(tmp, "x2");
            tmpType.GetProperty("fontSize")?.SetValue(tmp, 18f);
            tmpType.GetProperty("fontStyle")?.SetValue(tmp,
                System.Enum.Parse(System.Type.GetType("TMPro.FontStyles, Unity.TextMeshPro"), "Bold"));
            tmpType.GetProperty("color")?.SetValue(tmp, Color.white);
            // Alignment = BottomRight
            var alignType = System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
            if (alignType != null)
                tmpType.GetProperty("alignment")?.SetValue(tmp,
                    System.Enum.Parse(alignType, "BottomRight"));
        }
        else
        {
            Debug.LogWarning("[StaticItemEntry] TMP không tìm thấy — CountText không được tạo. " +
                             "Thêm tay sau khi import TMP.");
        }

        // ── Wire ItemTooltipTrigger ───────────────────────────────────────────
        SerializedObject triggerSO = new SerializedObject(trigger);
        var dirProp = triggerSO.FindProperty("direction");
        if (dirProp != null) dirProp.enumValueIndex = (int)TooltipDirection.Top;
        var endProp = triggerSO.FindProperty("alignEnd");
        if (endProp != null) endProp.boolValue = false;
        var gapProp = triggerSO.FindProperty("gap");
        if (gapProp != null) gapProp.floatValue = 6f;
        triggerSO.ApplyModifiedProperties();

        // ── Lưu prefab ───────────────────────────────────────────────────────
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, StaticEntryPrefabPath, out bool ok);
        Object.DestroyImmediate(root);

        if (!ok)
        {
            Debug.LogError("[StaticItemEntry] Lưu prefab thất bại.");
            return;
        }

        // Wire vào StaticItemListUI trong scene
        var listUI = Object.FindFirstObjectByType<StaticItemListUI>();
        if (listUI != null)
        {
            SerializedObject listSO = new SerializedObject(listUI);
            listSO.FindProperty("entryPrefab").objectReferenceValue = prefab;
            listSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(listUI);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(listUI.gameObject.scene);
            Debug.Log("[StaticItemEntry] Đã wire prefab vào StaticItemListUI trong scene.");
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;

        Debug.Log("[StaticItemEntry] Xong!\n\n" +
                  "Cấu trúc prefab:\n" +
                  "  StaticItemEntry (StaticItemEntry + ItemTooltipTrigger)\n" +
                  "  ├── CardBg  (Image — kéo sprite card vào đây)\n" +
                  "  ├── Icon    (Image — icon item, tự fill)\n" +
                  "  └── CountText (TMP_Text — hiện x2, x3...)\n\n" +
                  "Việc cần làm:\n" +
                  "  1. Chọn CardBg trong prefab → kéo sprite StatBoost card vào Source Image\n" +
                  "  2. Chỉnh size (hiện 80×80) nếu muốn to/nhỏ hơn");
    }

    static void SetStretch(RectTransform rt,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    // ---------- Helpers ----------

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] parts = folderPath.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static void SetPrivateField(Object target, string fieldName, Object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field == null)
        {
            Debug.LogError($"[GameUISetupTool] Không tìm thấy field '{fieldName}' trên {target.GetType().Name}");
            return;
        }
        field.SetValue(target, value);
        EditorUtility.SetDirty(target);
    }
}
