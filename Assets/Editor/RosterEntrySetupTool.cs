using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tools > Shop-Arena Setup > Setup FeedPopup in RosterEntry
// Chọn RosterEntry prefab root trong Hierarchy (lúc đang edit prefab) rồi chạy.
// Tool tự tạo FeedPopup GO, wire field vào CharacterRosterEntry.
// Sau khi chạy: kéo sprite corn vào field "Feed Corn Icon" > Image > Source Image.
public static class RosterEntrySetupTool
{
    [MenuItem("Tools/Shop-Arena Setup/Setup FeedPopup in RosterEntry")]
    static void SetupFeedPopup()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Setup FeedPopup",
                "Mở prefab RosterEntry, chọn root GO rồi chạy lại.", "OK");
            return;
        }

        CharacterRosterEntry entry = root.GetComponent<CharacterRosterEntry>();
        if (entry == null)
        {
            EditorUtility.DisplayDialog("Setup FeedPopup",
                "GO được chọn không có component CharacterRosterEntry.", "OK");
            return;
        }

        // Xoá FeedPopup cũ nếu có
        Transform old = root.transform.Find("FeedPopup");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old.gameObject);
            Debug.Log("[RosterEntrySetup] Đã xoá FeedPopup cũ.");
        }

        // ── Tạo FeedPopup GO ────────────────────────────────────────────────
        GameObject feedPopupGO = new GameObject("FeedPopup");
        Undo.RegisterCreatedObjectUndo(feedPopupGO, "Create FeedPopup");
        feedPopupGO.transform.SetParent(root.transform, false);

        // RectTransform: anchor right-center, pivot left-center → nằm sát bên phải card
        RectTransform rt   = feedPopupGO.AddComponent<RectTransform>();
        rt.anchorMin       = new Vector2(1f, 0.5f);
        rt.anchorMax       = new Vector2(1f, 0.5f);
        rt.pivot           = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(8f, 0f);
        rt.sizeDelta       = Vector2.zero;

        // LayoutElement ignoreLayout = true → layout group của parent bỏ qua GO này
        LayoutElement le   = feedPopupGO.AddComponent<LayoutElement>();
        le.ignoreLayout    = true;

        // HorizontalLayoutGroup để text + icon xếp ngang
        HorizontalLayoutGroup hlg  = feedPopupGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 4f;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding                = new RectOffset(4, 4, 2, 2);

        // ContentSizeFitter: tự co dãn theo nội dung
        ContentSizeFitter csf = feedPopupGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit     = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit       = ContentSizeFitter.FitMode.PreferredSize;

        // CanvasGroup để fade (code dùng, không cần tự đặt alpha)
        CanvasGroup cg    = feedPopupGO.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        // ── CostText ("-5") ──────────────────────────────────────────────────
        GameObject textGO = new GameObject("CostText");
        textGO.transform.SetParent(feedPopupGO.transform, false);
        TextMeshProUGUI tmp  = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text             = "-5";
        tmp.fontSize         = 18f;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.color            = new Color(1f, 0.85f, 0.15f);
        tmp.alignment        = TextAlignmentOptions.Left;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;

        // ── CornIcon ─────────────────────────────────────────────────────────
        GameObject iconGO = new GameObject("CornIcon");
        iconGO.transform.SetParent(feedPopupGO.transform, false);
        Image img         = iconGO.AddComponent<Image>();
        img.preserveAspect = true;
        img.color          = Color.white;
        LayoutElement iconLE    = iconGO.AddComponent<LayoutElement>();
        iconLE.preferredWidth   = 20f;
        iconLE.preferredHeight  = 20f;
        iconLE.minWidth         = 20f;
        iconLE.minHeight        = 20f;

        // Mặc định ẩn
        feedPopupGO.SetActive(false);

        // ── Wire fields vào CharacterRosterEntry ────────────────────────────
        SerializedObject so = new SerializedObject(entry);
        so.FindProperty("feedPopup").objectReferenceValue    = feedPopupGO;
        so.FindProperty("feedCostText").objectReferenceValue = tmp;
        so.FindProperty("feedCornIcon").objectReferenceValue = img;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(root);

        Debug.Log("[RosterEntrySetup] Xong! Nhớ kéo sprite corn vào " +
                  "CornIcon > Image > Source Image rồi Apply prefab.");
    }

    [MenuItem("Tools/Shop-Arena Setup/Setup FeedPopup in RosterEntry", validate = true)]
    static bool ValidateSetupFeedPopup()
    {
        return Selection.activeGameObject != null
            && Selection.activeGameObject.GetComponent<CharacterRosterEntry>() != null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Setup FoodCost Slot
    // ─────────────────────────────────────────────────────────────────────────
    // Cách dùng:
    //   • Chọn root RosterEntry → tự tìm HorizontalLayoutGroup icon row, thêm vào đó.
    //   • Chọn GO bất kỳ bên trong prefab → thêm FoodCostSlot làm con của GO đó.
    // Sau khi chạy: kéo sprite corn vào FoodCostSlot > CornIcon > Image > Source Image.
    [MenuItem("Tools/Shop-Arena Setup/Setup FoodCost Slot in RosterEntry")]
    static void SetupFoodCostSlot()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Setup FoodCost Slot",
                "Chọn GO trong prefab RosterEntry rồi chạy lại.\n\n" +
                "• Chọn root (CharacterRosterEntry) → tự detect icon row.\n" +
                "• Chọn GO bất kỳ bên trong → thêm slot làm con của GO đó.", "OK");
            return;
        }

        // Tìm CharacterRosterEntry từ selected hoặc cha
        CharacterRosterEntry entry = selected.GetComponent<CharacterRosterEntry>()
            ?? selected.GetComponentInParent<CharacterRosterEntry>();
        if (entry == null)
        {
            EditorUtility.DisplayDialog("Setup FoodCost Slot",
                "Không tìm thấy CharacterRosterEntry.\nChọn GO bên trong prefab RosterEntry.", "OK");
            return;
        }

        // Xác định parent của slot
        GameObject slotParent = selected;

        // Nếu chọn root, tự tìm HorizontalLayoutGroup icon row (ưu tiên cái có nhiều child nhất)
        if (selected == entry.gameObject)
        {
            HorizontalLayoutGroup bestHLG = null;
            int bestChildCount = -1;
            foreach (var hlg in selected.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                int cnt = hlg.transform.childCount;
                if (cnt > bestChildCount) { bestChildCount = cnt; bestHLG = hlg; }
            }
            if (bestHLG != null)
            {
                slotParent = bestHLG.gameObject;
                Debug.Log($"[FoodCost] Auto-detected icon row: '{slotParent.name}' ({bestChildCount} children).");
            }
            else
            {
                Debug.Log("[FoodCost] Không tìm thấy HorizontalLayoutGroup — thêm slot vào root.");
            }
        }

        // Xoá slot cũ nếu tồn tại (idempotent)
        Transform old = slotParent.transform.Find("FoodCostSlot");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old.gameObject);
            Debug.Log("[FoodCost] Đã xoá FoodCostSlot cũ.");
        }

        // ── FoodCostSlot GO ──────────────────────────────────────────────────
        var slotGO = new GameObject("FoodCostSlot");
        Undo.RegisterCreatedObjectUndo(slotGO, "Create FoodCostSlot");
        slotGO.transform.SetParent(slotParent.transform, false);

        var slotRT           = slotGO.AddComponent<RectTransform>();
        slotRT.sizeDelta     = new Vector2(48f, 32f);

        var slotHLG              = slotGO.AddComponent<HorizontalLayoutGroup>();
        slotHLG.childAlignment        = TextAnchor.MiddleLeft;
        slotHLG.spacing               = 3f;
        slotHLG.childForceExpandWidth = false;
        slotHLG.childForceExpandHeight= false;
        slotHLG.childControlWidth     = false;
        slotHLG.childControlHeight    = false;

        // ── CornIcon (placeholder — kéo sprite vào sau) ──────────────────────
        var iconGO      = new GameObject("CornIcon");
        iconGO.transform.SetParent(slotGO.transform, false);
        var iconRT      = iconGO.AddComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(18f, 18f);
        var iconImg     = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.color   = Color.white;
        iconImg.enabled = false;    // tắt cho đến khi có sprite

        // ── Label ─────────────────────────────────────────────────────────────
        var labelGO     = new GameObject("Label");
        labelGO.transform.SetParent(slotGO.transform, false);
        var labelRT     = labelGO.AddComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(26f, 28f);
        var tmp         = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text        = "1";
        tmp.fontSize    = 12f;
        tmp.fontStyle   = FontStyles.Bold;
        tmp.alignment   = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.color       = Color.white;

        EditorUtility.SetDirty(entry.gameObject);

        Debug.Log("[FoodCost] Xong! " +
                  "Kéo sprite corn vào FoodCostSlot > CornIcon > Image > Source Image, " +
                  "bật lại Image.enabled, rồi Apply prefab.");
        Selection.activeGameObject = slotGO;
    }

    [MenuItem("Tools/Shop-Arena Setup/Setup FoodCost Slot in RosterEntry", validate = true)]
    static bool ValidateSetupFoodCostSlot()
    {
        if (Selection.activeGameObject == null) return false;
        return Selection.activeGameObject.GetComponent<CharacterRosterEntry>() != null
            || Selection.activeGameObject.GetComponentInParent<CharacterRosterEntry>() != null;
    }
}
