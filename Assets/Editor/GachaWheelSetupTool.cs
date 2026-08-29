#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tools > Gacha > Setup Gacha Wheel In Scene
///
/// - Wheel nằm trong GachaPanel (ShopRoot), luôn hiện khi Shop
/// - 6 slot được tạo sẵn trong Editor → thấy ngay, không cần Play
/// - Kéo Slice Sprite vào GachaWheelUI.sliceSprite → 6 ô cập nhật tức thì (OnValidate)
/// - CenterImage = Button + cha của CostText (không cần nút riêng)
/// - GachaResultPopup = overlay cấp Canvas (ẩn mặc định)
///
/// Hierarchy:
///   GachaPanel  [GachaWheelUI]
///   ├── WheelArea
///   │   ├── WheelContainer      (xoay khi spin)
///   │   │   ├── Slice_0 .. 5   [GachaSlotUI] — 6 ô, sẵn trong editor
///   │   └── CenterImage         [Image][Button] — ấn để quay
///   │       └── CostText        [TextMeshProUGUI] — overlay lên center
///   └── (PackButtonRow di chuyển nếu đã có)
///
///   Canvas
///   └── GachaResultPopup        — full-screen overlay kết quả
/// </summary>
public static class GachaWheelSetupTool
{
    const int    SlotCount       = 6;
    const string ResultPopupName = "GachaResultPopup";

    [MenuItem("Tools/Gacha/Setup Gacha Wheel In Scene")]
    public static void SetupGachaWheel()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogError("[GachaSetup] Không tìm thấy Canvas."); return; }

        var gachaPanelT = FindInHierarchy(canvas.transform, "GachaPanel");
        if (gachaPanelT == null) { Debug.LogError("[GachaSetup] Không tìm thấy GachaPanel."); return; }
        var gachaPanel = gachaPanelT.gameObject;

        var wheelUI = EnsureComponent<GachaWheelUI>(gachaPanel);

        // ── PackButtonRow ─────────────────────────────────────────────────────
        var packRow   = EnsureChild(gachaPanel.transform, "PackButtonRow");
        var packRowRT = EnsureRT(packRow);
        packRowRT.anchorMin = new Vector2(0f, 1f); packRowRT.anchorMax = new Vector2(1f, 1f);
        packRowRT.pivot = new Vector2(0.5f, 1f);
        packRowRT.anchoredPosition = Vector2.zero;
        packRowRT.sizeDelta = new Vector2(0f, 60f);
        var hlg = EnsureComponent<HorizontalLayoutGroup>(packRow);
        hlg.childForceExpandWidth = true;
        foreach (var slot in gachaPanel.GetComponentsInChildren<GachaPackSlotUI>(true))
            slot.transform.SetParent(packRow.transform, false);

        // ── WheelArea ─────────────────────────────────────────────────────────
        var wheelArea   = EnsureChild(gachaPanel.transform, "WheelArea");
        var wheelAreaRT = EnsureRT(wheelArea);
        CenterAnchor(wheelAreaRT);
        wheelAreaRT.anchoredPosition = new Vector2(0f, 10f);
        wheelAreaRT.sizeDelta = new Vector2(460f, 460f);

        // ── WheelContainer ────────────────────────────────────────────────────
        var wheelCont   = EnsureChild(wheelArea.transform, "WheelContainer");
        var wheelContRT = EnsureRT(wheelCont);
        CenterAnchor(wheelContRT);
        wheelContRT.sizeDelta = Vector2.zero;

        // ── 6 Slots (pre-build cho editor) ────────────────────────────────────
        float radius    = wheelUI.wheelRadius;
        float sliceW    = wheelUI.sliceWidth;
        float charSize  = wheelUI.charImageSize;
        float charOff   = wheelUI.charOffset;

        for (int i = 0; i < SlotCount; i++)
        {
            var slotGO = EnsureChild(wheelCont.transform, $"Slice_{i}");
            var slotRT = EnsureRT(slotGO);
            CenterAnchor(slotRT);
            slotRT.sizeDelta        = Vector2.zero;
            slotRT.localEulerAngles = new Vector3(0f, 0f, -i * (360f / SlotCount));

            // SliceImage child
            var sliceImgGO = EnsureChild(slotGO.transform, "SliceImage");
            var sliceImgRT = EnsureRT(sliceImgGO);
            sliceImgRT.pivot            = new Vector2(0.5f, 0f);   // tip ở dưới = tâm bánh xe
            sliceImgRT.anchorMin        = new Vector2(0.5f, 0.5f);
            sliceImgRT.anchorMax        = new Vector2(0.5f, 0.5f);
            sliceImgRT.anchoredPosition = Vector2.zero;
            sliceImgRT.sizeDelta        = new Vector2(sliceW, radius);
            var sliceImg = EnsureComponent<Image>(sliceImgGO);
            sliceImg.sprite          = wheelUI.sliceSprite;
            sliceImg.raycastTarget   = false;
            sliceImg.preserveAspect  = false;

            // CharImage child
            var charImgGO = EnsureChild(slotGO.transform, "CharImage");
            var charImgRT = EnsureRT(charImgGO);
            charImgRT.pivot            = new Vector2(0.5f, 0.5f);
            charImgRT.anchorMin        = new Vector2(0.5f, 0.5f);
            charImgRT.anchorMax        = new Vector2(0.5f, 0.5f);
            charImgRT.anchoredPosition = new Vector2(0f, charOff);
            charImgRT.sizeDelta        = new Vector2(charSize, charSize);
            var charImg = EnsureComponent<Image>(charImgGO);
            charImg.preserveAspect = true;
            charImg.raycastTarget  = false;

            // GachaSlotUI
            var slot = EnsureComponent<GachaSlotUI>(slotGO);
            slot.SetReferences(sliceImg, charImg);
            EditorUtility.SetDirty(slot);
        }

        // ── CenterImage (Button) ──────────────────────────────────────────────
        var centerGO  = EnsureChild(wheelArea.transform, "CenterImage");
        var centerRT  = EnsureRT(centerGO);
        CenterAnchor(centerRT);
        centerRT.sizeDelta = new Vector2(120f, 120f);
        // Center phải ở trên slots
        centerGO.transform.SetAsLastSibling();

        var centerImg2 = EnsureComponent<Image>(centerGO);
        centerImg2.raycastTarget = true; // cần cho Button
        // Chỉ nhận click ở vùng sprite có alpha > 0 → vùng ấn khớp hình tròn
        centerImg2.alphaHitTestMinimumThreshold = 0.1f;

        var centerBtn = EnsureComponent<Button>(centerGO);
        centerBtn.targetGraphic = centerImg2;
        // Xoá persistent listeners cũ rồi thêm mới — AddListener thường không persist qua restart
        centerBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            centerBtn.onClick, wheelUI.OnClickSpin);
        EditorUtility.SetDirty(centerBtn);

        // CostText — con của CenterImage, overlay
        var costGO  = EnsureChild(centerGO.transform, "CostText");
        var costRT  = EnsureRT(costGO);
        costRT.anchorMin = Vector2.zero; costRT.anchorMax = Vector2.one;
        costRT.offsetMin = new Vector2(4f, 4f); costRT.offsetMax = new Vector2(-4f, -4f);
        var costTxt = EnsureComponent<TextMeshProUGUI>(costGO);
        costTxt.text      = "";
        costTxt.fontSize  = 16f;
        costTxt.alignment = TextAlignmentOptions.Center;
        costTxt.color     = Color.white;
        costTxt.raycastTarget = false;

        // ── Wire GachaWheelUI ─────────────────────────────────────────────────
        wheelUI.wheelContainer = wheelContRT;
        wheelUI.centerImage    = centerImg2;
        wheelUI.spinCostText   = costTxt;
        EditorUtility.SetDirty(wheelUI);

        // ── GachaResultPopup (Canvas level) ──────────────────────────────────
        var popupGO     = EnsureChild(canvas.transform, ResultPopupName);
        SetStretch(popupGO);
        popupGO.transform.SetAsLastSibling();
        var resultPopup = EnsureComponent<GachaResultPopup>(popupGO);
        popupGO.SetActive(false);

        BuildResultPopup(popupGO, resultPopup);

        wheelUI.resultPopup = resultPopup;
        EditorUtility.SetDirty(wheelUI);

        // GachaManager
        var gm = Object.FindObjectOfType<GachaManager>();
        if (gm != null) { gm.SetWheelUI(wheelUI); EditorUtility.SetDirty(gm); }
        else Debug.LogWarning("[GachaSetup] GachaManager không tìm thấy — gán tay.");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[GachaSetup] ✓ Done.\n" +
                  "• Kéo Slice Sprite vào GachaPanel > GachaWheelUI.sliceSprite → 6 ô hiện ngay\n" +
                  "• Kéo Center Sprite vào WheelArea > CenterImage > Image.Sprite\n" +
                  "• Kéo card frame vào GachaResultPopup > CardPanel > Image.Sprite\n" +
                  "• Điều chỉnh Wheel Radius / Char Offset trong GachaWheelUI rồi chạy lại tool nếu cần");
    }

    // ── Result Popup builder ──────────────────────────────────────────────────

    static void BuildResultPopup(GameObject popupGO, GachaResultPopup resultPopup)
    {
        // DimOverlay
        var dimGO  = EnsureChild(popupGO.transform, "DimOverlay");
        SetStretch(dimGO);
        var dimImg = EnsureComponent<Image>(dimGO);
        dimImg.color = new Color(0.12f, 0.12f, 0.12f, 0.78f);
        dimImg.raycastTarget = true;

        // CardPanel
        var cardGO  = EnsureChild(popupGO.transform, "CardPanel");
        var cardRT  = EnsureRT(cardGO);
        CenterAnchor(cardRT);
        cardRT.anchoredPosition = new Vector2(0f, 40f);
        cardRT.sizeDelta        = new Vector2(280f, 380f);
        var cardImg = EnsureComponent<Image>(cardGO);
        cardImg.color = Color.white;

        var cardSlot = EnsureComponent<GachaCardSlot>(cardGO);
        cardSlot.popup = resultPopup;
        var trigger = EnsureComponent<ItemTooltipTrigger>(cardGO);
        trigger.Setup(TooltipDirection.Right, false, 8f);
        EditorUtility.SetDirty(cardSlot);

        // CharImage
        var charGO  = EnsureChild(cardGO.transform, "CharImage");
        var charRT  = EnsureRT(charGO);
        CenterAnchor(charRT);
        charRT.anchoredPosition = new Vector2(0f, 20f);
        charRT.sizeDelta        = new Vector2(210f, 240f);
        var charImg = EnsureComponent<Image>(charGO);
        charImg.preserveAspect = true; charImg.raycastTarget = false;

        // CharNameText
        var nameGO  = EnsureChild(cardGO.transform, "CharNameText");
        var nameRT  = EnsureRT(nameGO);
        nameRT.anchorMin = new Vector2(0.5f, 0f); nameRT.anchorMax = new Vector2(0.5f, 0f);
        nameRT.pivot = new Vector2(0.5f, 0f);
        nameRT.anchoredPosition = new Vector2(0f, 18f);
        nameRT.sizeDelta = new Vector2(240f, 46f);
        var nameTxt = EnsureComponent<TextMeshProUGUI>(nameGO);
        nameTxt.text = ""; nameTxt.fontSize = 22f;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.color = new Color(0.15f, 0.08f, 0f);

        // ReceiveButton
        var recvGO  = EnsureChild(popupGO.transform, "ReceiveButton");
        var recvRT  = EnsureRT(recvGO);
        CenterAnchor(recvRT);
        recvRT.anchoredPosition = new Vector2(0f, -240f);
        recvRT.sizeDelta        = new Vector2(200f, 60f);
        EnsureButtonBg(recvGO, new Color(0.18f, 0.72f, 0.34f));
        var recvBtn = EnsureComponent<Button>(recvGO);
        EnsureChildText(recvGO, "RecvText", "Nhận", 28);

        resultPopup.cardPanel    = cardRT;
        resultPopup.charImage    = charImg;
        resultPopup.charNameText = nameTxt;
        resultPopup.receiveButton = recvBtn;
        EditorUtility.SetDirty(resultPopup);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

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
        var rt = go.GetComponent<RectTransform>(); if (rt == null) rt = go.AddComponent<RectTransform>(); return rt;
    }

    static void SetStretch(GameObject go)
    {
        var rt = EnsureRT(go);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one; rt.localEulerAngles = Vector3.zero;
    }

    static void CenterAnchor(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localEulerAngles = Vector3.zero; rt.localScale = Vector3.one;
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    { var c = go.GetComponent<T>(); if (c == null) c = go.AddComponent<T>(); return c; }

    static void EnsureChildText(GameObject parent, string childName, string text, int fontSize)
    {
        var go = EnsureChild(parent.transform, childName);
        var rt = EnsureRT(go);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = EnsureComponent<TextMeshProUGUI>(go);
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
    }

    static void EnsureButtonBg(GameObject go, Color color)
    {
        var img = EnsureComponent<Image>(go); img.color = color;
        EnsureComponent<Button>(go).targetGraphic = img;
    }
}
#endif
