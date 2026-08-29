using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameUISetupTool
{
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
                                 anchorCorner: new Vector2(0f, 0f),
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
            badgeGO.transform.SetAsLastSibling();
            dirty = true;
        }

        RectTransform badgeRt = badgeGO.GetComponent<RectTransform>();
        Undo.RecordObject(badgeRt, "Setup PriceBadge RectTransform");
        badgeRt.anchorMin        = anchorCorner;
        badgeRt.anchorMax        = anchorCorner;
        badgeRt.pivot            = pivotCorner;
        badgeRt.anchoredPosition = offset;
        badgeRt.sizeDelta        = new Vector2(56f, 26f);

        Image badgeImg = badgeGO.GetComponent<Image>();
        Undo.RecordObject(badgeImg, "Setup PriceBadge Image");
        badgeImg.raycastTarget = false;
        badgeImg.color         = Color.white;

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
}
