using UnityEngine;

// Blob shadow tự generate texture gradient circle.
// Đặt làm child của character root (không phải visualRoot) để không bị ảnh hưởng bởi sway.
// CharacterBase.UpdateVisualSway() gọi UpdateShadow(bounce) mỗi frame.
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterShadow : MonoBehaviour
{
    [Tooltip("Scale ngang của blob")]
    public float baseScaleX = 1f;
    [Tooltip("Scale dọc — ép dẹt để giống bóng top-down")]
    public float baseScaleY = 0.35f;
    [Tooltip("Mức độ thu nhỏ bóng khi char nảy lên (0 = không đổi, 1 = biến mất hoàn toàn)")]
    [Range(0f, 1f)]
    public float bounceScaleReduction = 0.3f;
    [Tooltip("1 = rìa mềm (blur nhiều), giá trị cao hơn = rìa cứng hơn (ít blur)")]
    [Range(1f, 8f)]
    public float edgeSharpness = 3f;
    public Color shadowColor = new Color(0f, 0f, 0f, 0.35f);

    SpriteRenderer sr;

    void OnEnable() => Refresh();

    // Chạy trong Editor mỗi khi thay đổi value trong Inspector
    void OnValidate() => Refresh();

    void Refresh()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.sprite = CreateShadowSprite(edgeSharpness);
        sr.color = shadowColor;
        sr.sortingOrder = -1;
        transform.localScale = new Vector3(baseScaleX, baseScaleY, 1f);
    }

    // Gọi từ CharacterBase.UpdateVisualSway với bounceFactor 0→1 (normalized, không phụ thuộc units).
    public void UpdateShadow(float bounceFactor)
    {
        float s = 1f - bounceFactor * bounceScaleReduction;
        transform.localScale = new Vector3(baseScaleX * s, baseScaleY * s, 1f);
    }

    static Sprite CreateShadowSprite(float sharpness)
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        // DontSave để texture không bị Unity GC trong editor
        tex.hideFlags = HideFlags.DontSave;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float r = size * 0.5f;

        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
        {
            float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
            float t = Mathf.Clamp01(1f - dist / r);
            // Pow > 1 → rìa cứng hơn; edgeSharpness điều chỉnh mức độ
            t = Mathf.Pow(t, 1f / sharpness);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, t));
        }

        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
