#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>
/// Tự động scan Assets/Sprites/StatIcons/, build atlas, tạo TMP Sprite Asset.
///
/// THÊM ICON MỚI:
///   1. Thả PNG vào Assets/Sprites/StatIcons/ (tên bất kỳ, đuôi .png)
///   2. Tools > UI > Create Stat Icon Sprite Asset
///   3. Dùng ngay: <sprite name="tên_file_không_đuôi">
///
/// THAY ICON: đè file PNG cũ → chạy lại menu.
/// </summary>
public static class StatIconSpriteAssetCreator
{
    const string IconFolder = "Assets/Sprites/StatIcons";
    const string AtlasPath  = "Assets/Sprites/StatIcons/StatIconAtlas.png";
    const string AssetPath  = "Assets/Sprites/StatIcons/StatIconSpriteAsset.asset";
    const int    SpriteSize = 64;

    // Truyền dữ liệu giữa 2 phase qua static fields
    static List<string> s_Names;

    // ── Phase 1: build atlas PNG, schedule phase 2 ────────────────────────────
    [MenuItem("Tools/UI/Create Stat Icon Sprite Asset")]
    static void Phase1_BuildAtlas()
    {
        // Tìm icon PNGs
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder });
        var iconPaths = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".png") && p != AtlasPath)
            .OrderBy(p => Path.GetFileNameWithoutExtension(p))
            .ToList();

        if (iconPaths.Count == 0)
        {
            Debug.LogError($"[StatIconSpriteAsset] Không tìm thấy PNG nào trong {IconFolder}");
            return;
        }

        // Đảm bảo readable trước khi đọc pixel
        bool needRefresh = false;
        foreach (var path in iconPaths)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && !imp.isReadable)
            {
                imp.isReadable = true;
                imp.SaveAndReimport();
                needRefresh = true;
            }
        }
        if (needRefresh) AssetDatabase.Refresh();

        // Build atlas
        int count = iconPaths.Count;
        var atlas = new Texture2D(SpriteSize * count, SpriteSize, TextureFormat.RGBA32, false);
        atlas.SetPixels(Enumerable.Repeat(Color.clear, SpriteSize * SpriteSize * count).ToArray());

        s_Names = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var src     = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPaths[i]);
            var resized = ResizeTexture(src, SpriteSize, SpriteSize);
            atlas.SetPixels(i * SpriteSize, 0, SpriteSize, SpriteSize, resized.GetPixels());
            s_Names.Add(Path.GetFileNameWithoutExtension(iconPaths[i]));
        }
        atlas.Apply();

        // Ghi PNG ra disk — KHÔNG gọi ImportAsset/SaveAssets ở đây
        var absPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AtlasPath));
        File.WriteAllBytes(absPath, atlas.EncodeToPNG());

        // Đánh dấu atlas cần readable rồi schedule phase 2 sau khi Unity import xong
        var atlasImp = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
        if (atlasImp != null) atlasImp.isReadable = true;

        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);

        // Dùng delayCall để chạy phase 2 sau khi import pipeline hoàn tất
        EditorApplication.delayCall += Phase2_CreateSpriteAsset;
        Debug.Log("[StatIconSpriteAsset] Atlas built. Đang tạo Sprite Asset...");
    }

    // ── Phase 2: tạo TMP_SpriteAsset (chạy sau 1 frame, ngoài import context) ─
    static void Phase2_CreateSpriteAsset()
    {
        EditorApplication.delayCall -= Phase2_CreateSpriteAsset;

        if (s_Names == null || s_Names.Count == 0)
        {
            Debug.LogError("[StatIconSpriteAsset] s_Names trống — chạy lại menu.");
            return;
        }

        var atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        if (atlasTexture == null)
        {
            Debug.LogError($"[StatIconSpriteAsset] Không load được atlas tại {AtlasPath}");
            return;
        }

        // Tạo asset
        var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        spriteAsset.spriteSheet = atlasTexture;

        var shader = Shader.Find("TextMeshPro/Sprite")
                  ?? Shader.Find("Hidden/TextMeshPro/Sprite")
                  ?? Shader.Find("Sprites/Default");
        if (shader == null) { Debug.LogError("[StatIconSpriteAsset] Không tìm thấy TMP Sprite shader!"); return; }
        var mat = new Material(shader);
        mat.mainTexture = atlasTexture;
        mat.name        = "StatIconAtlas_Mat";
        spriteAsset.material = mat;

        AssetDatabase.DeleteAsset(AssetPath);
        AssetDatabase.CreateAsset(spriteAsset, AssetPath);
        AssetDatabase.AddObjectToAsset(mat, AssetPath);

        // Dùng reflection để set backing fields — tránh guess SerializedProperty paths
        // (paths thay đổi giữa các version TMP/Unity)
        int count     = s_Names.Count;
        var assetType = typeof(TMP_SpriteAsset);

        var glyphList = new List<TMP_SpriteGlyph>();
        var charList  = new List<TMP_SpriteCharacter>();

        for (int i = 0; i < count; i++)
        {
            var glyph = new TMP_SpriteGlyph();
            SetField(glyph, "m_Index",      (uint)i);
            SetField(glyph, "m_Scale",      1f);
            SetField(glyph, "m_AtlasIndex", 0);
            SetField(glyph, "m_GlyphRect",  new GlyphRect(i * SpriteSize, 0, SpriteSize, SpriteSize));
            SetField(glyph, "m_Metrics",    new GlyphMetrics(SpriteSize, SpriteSize, 0, SpriteSize, SpriteSize));
            glyphList.Add(glyph);

            var ch = new TMP_SpriteCharacter((uint)(0xE000 + i), glyph);
            SetField(ch, "m_Name",       s_Names[i]);
            SetField(ch, "m_HashCode",   TMP_TextUtilities.GetSimpleHashCode(s_Names[i]));
            SetField(ch, "m_Scale",      1f);
            charList.Add(ch);
        }

        // Set vào TMP_SpriteAsset backing fields
        // Unity 6: field thực là m_GlyphTable (FormerlySerializedAs "m_SpriteGlyphTable")
        SetField(spriteAsset, "m_GlyphTable",           glyphList);
        SetField(spriteAsset, "m_SpriteCharacterTable", charList);

        // QUAN TRỌNG: Set m_Version = "1.1.0" TRƯỚC UpdateLookupTables.
        // Nếu thiếu, TMP gọi UpgradeSpriteAsset() → clear tables → crash vì spriteInfoList null.
        SetField(spriteAsset, "m_Version", "1.1.0");

        spriteAsset.UpdateLookupTables();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StatIconSpriteAsset] Done! {count} sprites → {AssetPath}\n" +
                  "Tags:\n" + string.Join("\n", s_Names.Select(n => $"  <sprite name=\"{n}\">")));

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = spriteAsset;
        s_Names = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void SetField(object obj, string fieldName, object value)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
        var type  = obj.GetType();
        while (type != null)
        {
            var f = type.GetField(fieldName, flags);
            if (f != null) { f.SetValue(obj, value); return; }
            type = type.BaseType;
        }
        Debug.LogWarning($"[StatIconSpriteAsset] Field not found: {fieldName} on {obj.GetType().Name}");
    }

    static Texture2D ResizeTexture(Texture2D src, int w, int h)
    {
        if (src.width == w && src.height == h) return src;
        var rt  = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        var old = RenderTexture.active;
        RenderTexture.active = rt;
        Graphics.Blit(src, rt);
        var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
        dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        dst.Apply();
        RenderTexture.active = old;
        RenderTexture.ReleaseTemporary(rt);
        return dst;
    }
}
#endif
