using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > Shop-Arena Setup > Setup Harder Button
/// Tạo nút "Harder" trong ShopRoot, đặt ngay bên trái StartWaveButton.
/// Chạy lại nếu cần update — sẽ tìm hoặc tạo lại button, không duplicate.
/// </summary>
public static class HarderButtonSetupTool
{
    [MenuItem("Tools/Shop-Arena Setup/Setup Harder Button")]
    public static void SetupHarderButton()
    {
        // ── 1. Tìm StartWaveButton ────────────────────────────────────────────
        var startWaveGO = GameObject.Find("StartWaveButton");
        if (startWaveGO == null)
        {
            Debug.LogError("[HarderButton] Không tìm thấy GameObject 'StartWaveButton' trong scene. " +
                           "Đảm bảo scene đang mở và tên đúng.");
            return;
        }

        var startWaveRT = startWaveGO.GetComponent<RectTransform>();
        if (startWaveRT == null)
        {
            Debug.LogError("[HarderButton] StartWaveButton không có RectTransform.");
            return;
        }

        Transform shopRoot = startWaveGO.transform.parent;
        if (shopRoot == null)
        {
            Debug.LogError("[HarderButton] StartWaveButton không có parent (ShopRoot).");
            return;
        }

        // ── 2. Tìm hoặc tạo HarderButton GO ──────────────────────────────────
        Transform existing = shopRoot.Find("HarderButton");
        GameObject harderGO;
        if (existing != null)
        {
            harderGO = existing.gameObject;
            Debug.Log("[HarderButton] Tìm thấy HarderButton đã tồn tại — cập nhật lại.");
            Undo.RecordObject(harderGO, "Update HarderButton");
        }
        else
        {
            harderGO = new GameObject("HarderButton", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(harderGO, "Create HarderButton");
            harderGO.transform.SetParent(shopRoot, false);
        }

        // ── 3. RectTransform — đặt trái StartWaveButton, cùng height ──────────
        var rt = harderGO.GetComponent<RectTransform>();
        Undo.RecordObject(rt, "Setup HarderButton RectTransform");

        float harderW = 200f;
        float gap     = 10f;

        // Copy cùng anchor/pivot với StartWaveButton
        rt.anchorMin = startWaveRT.anchorMin;
        rt.anchorMax = startWaveRT.anchorMax;
        rt.pivot     = startWaveRT.pivot;

        // Đặt bên trái: pivot (cùng corner với StartWaveButton) nằm ngay trái left edge của nó
        // startWaveRT.pivot.x = 1 → pivot là right edge.
        // Right edge của Harder = Left edge của StartWave - gap
        // Left edge của StartWave (trong anchor coords) = startWaveRT.anchoredPosition.x - startWaveRT.sizeDelta.x
        float harderRightEdge = startWaveRT.anchoredPosition.x - startWaveRT.sizeDelta.x - gap;
        rt.anchoredPosition = new Vector2(harderRightEdge, startWaveRT.anchoredPosition.y);
        rt.sizeDelta = new Vector2(harderW, startWaveRT.sizeDelta.y);

        // ── 4. Image nền (copy màu từ StartWaveButton nếu có) ─────────────────
        var img = harderGO.GetComponent<Image>() ?? harderGO.AddComponent<Image>();
        Undo.RecordObject(img, "Setup HarderButton Image");
        img.raycastTarget = true;

        // Lấy màu nền từ StartWaveButton (nếu có Image)
        var swImg = startWaveGO.GetComponent<Image>();
        if (swImg != null)
        {
            img.sprite = swImg.sprite;
            img.type   = swImg.type;
            // Dùng màu cam/vàng để phân biệt với Start Wave (xanh lá)
            img.color  = new Color(0.95f, 0.55f, 0.1f, 1f); // cam
        }
        else
        {
            img.color = new Color(0.95f, 0.55f, 0.1f, 1f);
        }

        // ── 5. Button component ───────────────────────────────────────────────
        var btn = harderGO.GetComponent<Button>() ?? harderGO.AddComponent<Button>();
        Undo.RecordObject(btn, "Setup HarderButton Button");
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        colors.pressedColor     = new Color(0.7f, 0.35f, 0.05f);
        colors.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        btn.colors = colors;
        btn.targetGraphic = img;

        // ── 6. HarderButton script ────────────────────────────────────────────
        var hb = harderGO.GetComponent<HarderButton>() ?? harderGO.AddComponent<HarderButton>();

        // ── 7. TMP Label ──────────────────────────────────────────────────────
        var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType == null)
        {
            Debug.LogWarning("[HarderButton] TextMeshPro không tìm thấy. Label không được tạo.");
        }
        else
        {
            const string labelName = "Label";
            Transform existingLabel = harderGO.transform.Find(labelName);
            GameObject labelGO;
            if (existingLabel != null)
            {
                labelGO = existingLabel.gameObject;
            }
            else
            {
                labelGO = new GameObject(labelName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(labelGO, "Create HarderButton Label");
                labelGO.transform.SetParent(harderGO.transform, false);
            }

            var labelRT = labelGO.GetComponent<RectTransform>();
            Undo.RecordObject(labelRT, "Setup HarderButton Label RT");
            labelRT.anchorMin  = Vector2.zero;
            labelRT.anchorMax  = Vector2.one;
            labelRT.offsetMin  = new Vector2(4f, 2f);
            labelRT.offsetMax  = new Vector2(-4f, -2f);

            var tmp = labelGO.GetComponent(tmpType) ?? labelGO.AddComponent(tmpType);
            Undo.RecordObject(tmp as Object, "Setup HarderButton Label TMP");
            tmpType.GetProperty("text")?.SetValue(tmp, "HARDER");
            tmpType.GetProperty("fontSize")?.SetValue(tmp, 22f);
            tmpType.GetProperty("color")?.SetValue(tmp, Color.white);
            tmpType.GetProperty("fontStyle")?.SetValue(tmp,
                System.Enum.ToObject(System.Type.GetType("TMPro.FontStyles, Unity.TextMeshPro"), 1)); // Bold
            tmpType.GetProperty("raycastTarget")?.SetValue(tmp, false);
            var alignType = System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
            if (alignType != null)
                tmpType.GetProperty("alignment")?.SetValue(tmp,
                    System.Enum.Parse(alignType, "Center"));

            // Wire label vào HarderButton.label field qua SerializedObject
            var so = new SerializedObject(hb);
            var labelProp = so.FindProperty("label");
            if (labelProp != null)
            {
                labelProp.objectReferenceValue = tmp as Object;
                so.ApplyModifiedProperties();
            }
        }

        // ── 8. Đặt HarderButton cạnh StartWaveButton trong hierarchy ──────────
        // StartWaveButton là sibling — đặt Harder ngay trước nó
        int swIndex = startWaveGO.transform.GetSiblingIndex();
        harderGO.transform.SetSiblingIndex(swIndex);

        EditorUtility.SetDirty(harderGO);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[HarderButton] Xong! HarderButton đã được tạo trong '{shopRoot.name}'.\n" +
                  $"  Vị trí: anchoredPosition = ({rt.anchoredPosition.x:F0}, {rt.anchoredPosition.y:F0}), " +
                  $"size = ({rt.sizeDelta.x:F0} x {rt.sizeDelta.y:F0})\n" +
                  "  Nhớ Ctrl+S để lưu scene.");
    }
}
