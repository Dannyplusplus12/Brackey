using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vẽ outline màu xanh lên tất cả Image có raycastTarget=true trong Scene view.
/// Bật/tắt qua menu hoặc phím tắt.
///
/// Menu: Tools > UI > Toggle Raycast Area Overlay   (Ctrl+Shift+R)
/// </summary>
[InitializeOnLoad]
public static class RaycastAreaVisualizer
{
    const string PrefKey     = "RaycastAreaViz_Enabled";
    const string MenuPath    = "Tools/UI/Toggle Raycast Area Overlay";

    static bool _enabled;

    static RaycastAreaVisualizer()
    {
        _enabled = EditorPrefs.GetBool(PrefKey, false);
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem(MenuPath, priority = 100)]
    static void Toggle()
    {
        _enabled = !_enabled;
        EditorPrefs.SetBool(PrefKey, _enabled);
        // Làm tươi Scene view ngay
        SceneView.RepaintAll();
        Debug.Log($"[RaycastArea] Raycast overlay: {(_enabled ? "BẬT" : "TẮT")}");
    }

    // ── Scene GUI ─────────────────────────────────────────────────────────────

    static void OnSceneGUI(SceneView sv)
    {
        if (!_enabled) return;
        if (!Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            DrawOverlay();
        }
        else
        {
            // Cũng vẽ lúc Play để dễ debug
            DrawOverlay();
        }
    }

    static void DrawOverlay()
    {
        var images = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
        var corners = new Vector3[4];

        foreach (var img in images)
        {
            if (!img.raycastTarget) continue;

            img.rectTransform.GetWorldCorners(corners);

            // Màu phân biệt theo loại component gắn trên cùng GO
            Color fill    = new Color(0f, 1f, 0.4f, 0.08f);
            Color outline = new Color(0f, 1f, 0.4f, 0.9f);

            if (img.GetComponent<ItemTooltipTrigger>() != null)
            {
                fill    = new Color(0f, 0.7f, 1f, 0.12f);
                outline = new Color(0f, 0.7f, 1f, 1f);
            }

            Handles.DrawSolidRectangleWithOutline(corners, fill, outline);

            // Nhãn tên GO ở giữa rect
            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            Handles.Label(center, img.gameObject.name,
                new GUIStyle(EditorStyles.miniLabel)
                {
                    normal  = { textColor = outline },
                    fontSize = 9,
                    alignment = TextAnchor.MiddleCenter,
                });
        }
    }
}
