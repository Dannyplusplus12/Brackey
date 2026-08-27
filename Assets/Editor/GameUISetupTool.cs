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
