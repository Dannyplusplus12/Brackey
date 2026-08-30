using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool: Tools > Shop-Arena Setup > Create Enemy HP Bar
/// Tự động tạo EnemyHPBar UI trong Canvas với đầy đủ cấu trúc và wire script.
/// </summary>
public static class EnemyHPBarSetupTool
{
    [MenuItem("Tools/Shop-Arena Setup/Create Enemy HP Bar")]
    public static void CreateEnemyHPBar()
    {
        // ── 1. Tìm Canvas ────────────────────────────────────────────────────
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas trong scene!", "OK");
            return;
        }

        // ── 2. Kiểm tra đã tồn tại chưa ─────────────────────────────────────
        Transform existing = canvas.transform.Find("EnemyHPBar");
        if (existing != null)
        {
            Debug.Log("[EnemyHPBar] EnemyHPBar đã tồn tại — đang chọn nó.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // ── 3. Tạo GO nền (background) ───────────────────────────────────────
        var barGO = new GameObject("EnemyHPBar", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(barGO, "Create EnemyHPBar");
        barGO.transform.SetParent(canvas.transform, false);
        // Đặt lên đầu hierarchy để hiển thị trên các element khác
        barGO.transform.SetAsLastSibling();

        var barRt = barGO.GetComponent<RectTransform>();
        // Anchor: top-stretch (kéo full chiều ngang, ghim đỉnh)
        barRt.anchorMin        = new Vector2(0f, 1f);
        barRt.anchorMax        = new Vector2(1f, 1f);
        barRt.pivot            = new Vector2(0.5f, 1f);
        barRt.offsetMin        = new Vector2(0f,  -28f); // left + bottom → height = 28px
        barRt.offsetMax        = new Vector2(0f,    0f); // right + top

        var barImg = barGO.GetComponent<Image>();
        barImg.color          = new Color(0.12f, 0f, 0f, 0.88f); // nền đỏ tối
        barImg.raycastTarget  = false;

        // ── 4. Tạo Fill con ──────────────────────────────────────────────────
        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(fillGO, "Create EnemyHPBar Fill");
        fillGO.transform.SetParent(barGO.transform, false);

        var fillRt = fillGO.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f,  2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);

        var fillImg = fillGO.GetComponent<Image>();
        fillImg.color         = new Color(0.88f, 0.08f, 0.08f, 1f); // đỏ tươi
        fillImg.type          = Image.Type.Filled;
        fillImg.fillMethod    = Image.FillMethod.Horizontal;
        fillImg.fillOrigin    = 0; // Left
        fillImg.fillAmount    = 1f;
        fillImg.raycastTarget = false;

        // ── 5. Gắn EnemyHealthBar script & wire fillImage ────────────────────
        var script = barGO.AddComponent<EnemyHealthBar>();
        var so     = new SerializedObject(script);
        var prop   = so.FindProperty("fillImage");
        if (prop != null)
        {
            prop.objectReferenceValue = fillImg;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("[EnemyHPBar] Không tìm thấy field 'fillImage' — hãy kéo Fill vào Inspector thủ công.");
        }

        // ── 6. Mark dirty & select ───────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = barGO;

        Debug.Log("[EnemyHPBar] ✓ Tạo xong EnemyHPBar trong Canvas.\n" +
                  "Thanh sẽ tự hiện/ẩn khi wave bắt đầu/kết thúc.");
    }

    // Validate: chỉ enable menu khi đang có scene mở
    [MenuItem("Tools/Shop-Arena Setup/Create Enemy HP Bar", true)]
    static bool ValidateCreate() => Application.isPlaying == false;
}
