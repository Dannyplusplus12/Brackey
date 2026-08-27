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
        tmp.enableWordWrapping = false;
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
}
